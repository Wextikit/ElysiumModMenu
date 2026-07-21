#nullable disable

using System.Linq;
using System.Text.RegularExpressions;
using Hazel;

namespace ElysiumModMenu
{
    /// <summary>
    /// Handles private chat commands: /w, /pm, and /msg.
    /// </summary>
    public static class HushWhisper
    {
        private static readonly Regex RichTextTag = new Regex("<.*?>");
        private static byte keepTargetId = byte.MaxValue;
        private static string keepTargetName = "";

        /// <summary>
        /// Sends a private message when the chat input contains a whisper command.
        /// Returns true when the input was handled and normal chat sending must be blocked.
        /// </summary>
        public static bool TryHandle(ChatController chat)
        {
            if (chat?.freeChatField?.textArea == null)
                return false;

            string text = chat.freeChatField.Text;
            if (string.IsNullOrWhiteSpace(text))
                return false;

            string lowerText = text.ToLowerInvariant();
            if (lowerText == "/unkeepw" || lowerText.StartsWith("/unkeepw "))
            {
                ClearKeepTarget();
                ShowLocalMessage("<color=#FFAC1C>[WHISPER]</color> Keep whisper disabled.");
                ClearInput(chat);
                return true;
            }

            if (lowerText.StartsWith("/keepw "))
            {
                string keepInput = text.Substring(7).ToLowerInvariant().Trim();
                if (string.IsNullOrWhiteSpace(keepInput))
                {
                    ShowLocalMessage("<color=#FF0000>[ERROR]</color> Usage: /keepw [ID, color, or name]");
                    ClearInput(chat);
                    return true;
                }

                PlayerControl keepTarget = FindTarget(keepInput);
                if (!IsValidTarget(keepTarget))
                {
                    ShowLocalMessage("<color=#FF0000>[ERROR]</color> Player not found. Enter an ID, color, or name.");
                    ClearInput(chat);
                    return true;
                }

                keepTargetId = keepTarget.PlayerId;
                keepTargetName = StripRichText(keepTarget.Data.PlayerName);
                ShowLocalMessage($"<color=#FFAC1C>[WHISPER]</color> Keep target: <b>{keepTargetName}</b>. Use /unkeepw to stop.");
                ClearInput(chat);
                return true;
            }

            if (!lowerText.StartsWith("/w ") &&
                !lowerText.StartsWith("/pm ") &&
                !lowerText.StartsWith("/msg "))
            {
                if (HasKeepTarget() && !lowerText.StartsWith("/"))
                {
                    if (!TrySendToKeepTarget(StripRichText(text)))
                        ClearKeepTarget();

                    ClearInput(chat);
                    return true;
                }

                return false;
            }

            string[] parts = text.Split(new[] { ' ' }, 3);
            if (parts.Length < 3 || string.IsNullOrWhiteSpace(parts[2]))
            {
                if (HasKeepTarget() && parts.Length >= 2 && !string.IsNullOrWhiteSpace(parts[1]))
                {
                    if (!TrySendToKeepTarget(StripRichText(parts[1])))
                        ClearKeepTarget();

                    ClearInput(chat);
                    return true;
                }

                ShowLocalMessage("<color=#FF0000>[ERROR]</color> Usage: /w [ID, color, or name] [message] or /keepw [target]");
                ClearInput(chat);
                return true;
            }

            string targetInput = parts[1].ToLowerInvariant().Trim();
            string safeMessage = StripRichText(parts[2]);
            PlayerControl target = FindTarget(targetInput);

            if (!IsValidTarget(target))
            {
                ShowLocalMessage("<color=#FF0000>[ERROR]</color> Player not found. Enter an ID, color, or name.");
                ClearInput(chat);
                return true;
            }

            SendWhisper(target, safeMessage);

            ClearInput(chat);
            return true;
        }

        private static bool HasKeepTarget()
        {
            return keepTargetId != byte.MaxValue;
        }

        private static void ClearKeepTarget()
        {
            keepTargetId = byte.MaxValue;
            keepTargetName = "";
        }

        private static bool TrySendToKeepTarget(string safeMessage)
        {
            if (string.IsNullOrWhiteSpace(safeMessage))
                return true;

            PlayerControl target = FindTargetById(keepTargetId);
            if (!IsValidTarget(target))
            {
                string name = string.IsNullOrWhiteSpace(keepTargetName) ? "target" : keepTargetName;
                ShowLocalMessage($"<color=#FF0000>[ERROR]</color> Keep whisper target left or is unavailable: {name}.");
                return false;
            }

            SendWhisper(target, safeMessage);
            return true;
        }

        private static void SendWhisper(PlayerControl target, string safeMessage)
        {
            if (AmongUsClient.Instance == null || PlayerControl.LocalPlayer == null || string.IsNullOrWhiteSpace(safeMessage))
                return;

            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(
                PlayerControl.LocalPlayer.NetId,
                13,
                SendOption.Reliable,
                target.OwnerId);

            writer.Write($"Whispers in your ear:\n{safeMessage}");
            AmongUsClient.Instance.FinishRpcImmediately(writer);

            string targetName = StripRichText(target.Data.PlayerName);
            ShowLocalMessage($"<color=#FFAC1C>You whisper to {targetName}:\n{safeMessage}</color>");
        }

        private static PlayerControl FindTarget(string targetInput)
        {
            if (PlayerControl.AllPlayerControls == null)
                return null;

            PlayerControl target = null;

            if (byte.TryParse(targetInput, out byte playerId))
            {
                target = PlayerControl.AllPlayerControls
                    .ToArray()
                    .FirstOrDefault(player => player != null && player.PlayerId == playerId);
            }

            if (target != null)
                return target;

            PlayerControl partialMatch = null;
            int targetColorId = ElysiumModMenuGUI.GetColorIdByName(targetInput);

            foreach (PlayerControl player in PlayerControl.AllPlayerControls)
            {
                if (player == null ||
                    player.Data == null ||
                    player.Data.Disconnected ||
                    player == PlayerControl.LocalPlayer)
                {
                    continue;
                }

                string playerName = StripRichText(player.Data.PlayerName).ToLowerInvariant().Trim();
                int colorId = (int)player.Data.DefaultOutfit.ColorId;

                if (playerName == targetInput || (targetColorId != -1 && colorId == targetColorId))
                    return player;

                if (partialMatch == null && playerName.StartsWith(targetInput))
                    partialMatch = player;
            }

            return partialMatch;
        }

        private static PlayerControl FindTargetById(byte playerId)
        {
            if (PlayerControl.AllPlayerControls == null)
                return null;

            foreach (PlayerControl player in PlayerControl.AllPlayerControls)
            {
                if (player != null && player.PlayerId == playerId)
                    return player;
            }

            return null;
        }

        private static bool IsValidTarget(PlayerControl target)
        {
            return target != null &&
                target != PlayerControl.LocalPlayer &&
                target.Data != null &&
                !target.Data.Disconnected;
        }

        private static string StripRichText(string value)
        {
            return RichTextTag.Replace(value ?? string.Empty, string.Empty)
                .Replace("<", string.Empty)
                .Replace(">", string.Empty);
        }

        private static void ShowLocalMessage(string message)
        {
            if (HudManager.Instance?.Chat != null && PlayerControl.LocalPlayer != null)
                HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, message);
        }

        private static void ClearInput(ChatController chat)
        {
            chat.freeChatField.textArea.SetText(string.Empty, string.Empty);
        }
    }
}
