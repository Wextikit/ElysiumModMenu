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
            if (!lowerText.StartsWith("/w ") &&
                !lowerText.StartsWith("/pm ") &&
                !lowerText.StartsWith("/msg "))
            {
                return false;
            }

            string[] parts = text.Split(new[] { ' ' }, 3);
            if (parts.Length < 3 || string.IsNullOrWhiteSpace(parts[2]))
            {
                ShowLocalMessage("<color=#FF0000>[ERROR]</color> Usage: /w [ID, color, or name] [message]");
                ClearInput(chat);
                return true;
            }

            string targetInput = parts[1].ToLowerInvariant().Trim();
            string safeMessage = StripRichText(parts[2]);
            PlayerControl target = FindTarget(targetInput);

            if (target == null || target == PlayerControl.LocalPlayer)
            {
                ShowLocalMessage("<color=#FF0000>[ERROR]</color> Player not found. Enter an ID, color, or name.");
                ClearInput(chat);
                return true;
            }

            if (AmongUsClient.Instance != null && PlayerControl.LocalPlayer != null)
            {
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

            ClearInput(chat);
            return true;
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
