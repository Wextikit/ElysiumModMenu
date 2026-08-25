#nullable disable
#pragma warning disable CS0162, CS0108, CS0219, CS0661, CS0660, CS8632, CS0168, CS0659
using AmongUs.Data.Player;
using AmongUs.GameOptions;
using AmongUs.InnerNet.GameDataMessages;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using BepInEx.Unity.IL2CPP.Utils;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using ElysiumModMenu;
using HarmonyLib;
using Hazel;
using Il2CppInterop.Runtime.Attributes;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using InnerNet;
using RewiredConsts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.Playables;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using static ElysiumModMenu.ElysiumModMenuGUI;
using static Rewired.UI.ControlMapper.ControlMapper;
using Color = UnityEngine.Color;
using Object = UnityEngine.Object;
using Vector3 = UnityEngine.Vector3;

namespace ElysiumModMenu
{
    public partial class ElysiumModMenuGUI : MonoBehaviour
    {

        private static float identityCardHeight = 175f;

private static string FilterHexInput(string input, int maxChars)
        {
            string value = (input ?? string.Empty).Trim();
            string clean = "";
            bool hasHash = false;

            foreach (char c in value)
            {
                if (c == '#' && clean.Length == 0 && !hasHash)
                {
                    hasHash = true;
                    clean = "#";
                    continue;
                }

                if ((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'))
                {
                    if (clean.Length == 0) clean = "#";
                    clean += char.ToUpperInvariant(c);
                    if (clean.Length >= maxChars) break;
                }
            }

            return clean.Length == 0 ? "#" : clean;
        }

private static string FilterGhostChatColorInput(string input)
        {
            string value = (input ?? string.Empty).Trim();
            if (IsGhostChatKeyword(value))
                return NormalizeGhostChatKeyword(value);

            if (value.StartsWith("#") || value.All(c => (c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f')))
                return FilterHexInput(value, 7);

            string clean = "";
            foreach (char c in value)
            {
                if (char.IsLetter(c))
                {
                    clean += char.ToLowerInvariant(c);
                    if (clean.Length >= 10) break;
                }
            }

            return clean;
        }

private static bool IsGhostChatKeyword(string input)
        {
            string value = NormalizeGhostChatKeyword(input);
            return value == "rainbow" || value == "shimmer";
        }

private static string NormalizeGhostChatKeyword(string input)
        {
            string value = (input ?? string.Empty).Trim().ToLowerInvariant();
            switch (value)
            {
                case "rainbow":
                case "радуга":
                case "раинбов":
                case "lgbt":
                    return "rainbow";
                case "shimmer":
                case "шимер":
                case "шиммер":
                    return "shimmer";
                default:
                    return value;
            }
        }

private static string SanitizeGhostChatColorSetting(string input)
        {
            string value = (input ?? string.Empty).Trim();
            if (IsGhostChatKeyword(value))
                return NormalizeGhostChatKeyword(value);

            string hex = SanitizeHexColor(value, GhostChatDefaultColor);
            return string.Equals(hex, "#D7B8FF", StringComparison.OrdinalIgnoreCase) ? GhostChatDefaultColor : hex;
        }

public static string GetGhostChatColorHex()
        {
            if (isEditingGhostChatColor)
            {
                return SanitizeHexColor(ghostChatColorHex, GhostChatDefaultColor);
            }

            ghostChatColorHex = SanitizeGhostChatColorSetting(ghostChatColorHex);
            if (IsGhostChatKeyword(ghostChatColorHex))
                return GhostChatDefaultColor;
            return ghostChatColorHex;
        }

public static string RenderGhostChatMessageText(string chatText)
        {
            string mode = NormalizeGhostChatKeyword(ghostChatColorHex);
            if (mode == "rainbow")
                return ApplyGhostChatRainbow(chatText);
            if (mode == "shimmer")
                return ApplyMenuShimmer(chatText);

            string hex = GetGhostChatColorHex();
            return $"<color={hex}>{chatText}</color>";
        }

private static string ApplyGhostChatRainbow(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            string result = "";
            int visibleIndex = 0;
            float baseHue = Mathf.Repeat(Time.unscaledTime * 0.18f, 1f);
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (char.IsWhiteSpace(c))
                {
                    result += c;
                    continue;
                }

                float hue = Mathf.Repeat(baseHue + visibleIndex * 0.085f, 1f);
                Color color = Color.HSVToRGB(hue, 0.9f, 1f);
                result += $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{c}</color>";
                visibleIndex++;
            }

            return result;
        }

private static string BuildLocalNameRenderText(string input)
        {
            string value = (input ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n');
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;

            string trimmed = value.TrimStart();
            if (trimmed.StartsWith("shimmer:", StringComparison.OrdinalIgnoreCase))
                return ApplyMenuShimmer(trimmed.Substring("shimmer:".Length).TrimStart());

            Match hexPrefix = Regex.Match(trimmed, @"^#([0-9A-Fa-f]{6})(.*)$");
            if (hexPrefix.Success)
            {
                string payload = hexPrefix.Groups[2].Value.TrimStart(' ', ':', '|', '-', '>');
                if (!string.IsNullOrEmpty(payload))
                    return $"<color=#{hexPrefix.Groups[1].Value}>{payload}</color>";
            }

            return value;
        }

private static string GetDisplayedFriendCode(NetworkedPlayerInfo data, string emptyValue = "Hidden")
        {
            if (data == null) return emptyValue;

            string value = GetCachedOriginalFriendCode(data, emptyValue);
            if (enableLocalFriendCodeSpoof &&
                PlayerControl.LocalPlayer != null &&
                data.PlayerId == PlayerControl.LocalPlayer.PlayerId &&
                !string.IsNullOrEmpty(localFriendCodeInput))
            {
                value = localFriendCodeInput;
            }

            return string.IsNullOrEmpty(value) ? emptyValue : value;
        }

public static string GetCachedOriginalFriendCode(NetworkedPlayerInfo data, string emptyValue = "Hidden")
        {
            if (data == null) return emptyValue;
            try
            {
                SafePlayerIdentitySnapshot snapshot;
                if (safeIdentityByClientId.TryGetValue(data.ClientId, out snapshot) ||
                    safeIdentityByPlayerId.TryGetValue(data.PlayerId, out snapshot))
                    return string.IsNullOrEmpty(snapshot.FriendCode) ? emptyValue : snapshot.FriendCode;
            }
            catch { }
            return emptyValue;
        }

        [HideFromIl2Cpp]
        private void ApplyPublicFriendCode()
        {
            spoofFriendCodeInput = SanitizeSpoofFriendCode(spoofFriendCodeInput);
            if (string.IsNullOrEmpty(spoofFriendCodeInput)) return;

            try
            {
                FriendsListManager mgr = FriendsListManager.Instance;
                if (mgr == null)
                {
                    ShowNotification("<color=#FF4444>[SPOOF FC]</color> Friends manager not ready.");
                    return;
                }

                var callback = Il2CppInterop.Runtime.DelegateSupport.ConvertDelegate<Il2CppSystem.Action<
                    Assets.InnerNet.ResponseState, Assets.InnerNet.Response<Assets.InnerNet.ResponseFriendCode>>>(
                    new System.Action<Assets.InnerNet.ResponseState, Assets.InnerNet.Response<Assets.InnerNet.ResponseFriendCode>>(
                        OnPublicFriendCodeResponse));
                mgr.SetFriendCode(spoofFriendCodeInput, callback);
                ShowNotification("<color=#00FFAA>[SPOOF FC]</color> Sent to server.");
            }
            catch
            {
                ShowNotification("<color=#FF4444>[SPOOF FC]</color> Registration failed.");
            }
        }

        [HideFromIl2Cpp]
        private void OnPublicFriendCodeResponse(Assets.InnerNet.ResponseState state,
            Assets.InnerNet.Response<Assets.InnerNet.ResponseFriendCode> response)
        {
            if (state != Assets.InnerNet.ResponseState.Success || response == null ||
                response.Data == null || response.Data.Attributes == null)
            {
                ShowNotification("<color=#FF4444>[SPOOF FC]</color> Server rejected registration.");
                return;
            }

            string username = response.Data.Attributes.Username ?? string.Empty;
            string discriminator = response.Data.Attributes.Discriminator ?? string.Empty;
            string friendCode = string.IsNullOrEmpty(discriminator) ? username : username + "#" + discriminator;
            try
            {
                EOSManager eos = EOSManager.Instance;
                if (eos != null) eos.FriendCode = friendCode;
                FriendsListUI.Instance?.UpdateFriendCodeUI();
            }
            catch { }

            ShowNotification($"<color=#00FFAA>[SPOOF FC]</color> Registered: <b>{friendCode}</b>");
        }

private static string FormatInputPreview(string value, bool editing, int maxChars = 52)
        {
            string preview = value ?? string.Empty;
            if (preview.Length > maxChars)
                preview = "..." + preview.Substring(preview.Length - (maxChars - 3));

            if (editing) preview += "_";
            return string.IsNullOrEmpty(preview) ? " " : preview;
        }

private static bool HandleClipboardShortcut(Event e, ref string target, int maxLength = -1)
        {
            if (e == null || e.type != EventType.KeyDown) return false;

            bool ctrlOrCmd = e.control || e.command;
            bool pasteAlt = e.shift && e.keyCode == KeyCode.Insert;
            if (!ctrlOrCmd && !pasteAlt) return false;

            target ??= string.Empty;

            if (ctrlOrCmd && e.keyCode == KeyCode.C)
            {
                GUIUtility.systemCopyBuffer = target;
                e.Use();
                return true;
            }

            if (ctrlOrCmd && e.keyCode == KeyCode.X)
            {
                GUIUtility.systemCopyBuffer = target;
                target = string.Empty;
                e.Use();
                return true;
            }

            if ((ctrlOrCmd && e.keyCode == KeyCode.V) || pasteAlt)
            {
                string paste = (GUIUtility.systemCopyBuffer ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n');
                if (paste.Length > 0)
                {
                    target += paste;
                    if (maxLength >= 0 && target.Length > maxLength)
                        target = target.Substring(0, maxLength);
                }
                e.Use();
                return true;
            }

            return false;
        }

private static bool IsBrokenFriendCode(string friendCode)
        {
            if (string.IsNullOrWhiteSpace(friendCode)) return true;
            if (friendCode.Contains(" ")) return true;
            if (friendCode.Contains("<") || friendCode.Contains(">")) return true;
            if (!friendCode.Contains("#")) return true;

            string[] parts = friendCode.Split('#');
            if (parts.Length != 2) return true;
            if (string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1])) return true;
            if (parts[0].Length < 3 || parts[0].Length > 16) return true;
            if (parts[1].Length < 3 || parts[1].Length > 8) return true;
            if (!parts[0].All(char.IsLetterOrDigit)) return true;
            if (!parts[1].All(char.IsDigit)) return true;

            return false;
        }

private void TryAutoBanBrokenFriendCodeTick()
        {
            try
            {
                if (!autoBanBrokenFriendCode)
                {
                    brokenFcScanTimer = 0f;
                    brokenFcPunishedOwners.Clear();
                    return;
                }

                if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost || PlayerControl.AllPlayerControls == null)
                {
                    brokenFcScanTimer = 0f;
                    return;
                }

                if (PlayerControl.AllPlayerControls.Count <= 1)
                    brokenFcPunishedOwners.Clear();

                brokenFcScanTimer += Time.deltaTime;
                if (brokenFcScanTimer < 0.8f) return;
                brokenFcScanTimer = 0f;

                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    if (pc == null || pc == PlayerControl.LocalPlayer || pc.Data == null || pc.Data.Disconnected) continue;
                    if (IsProtectedFromAnticheat(pc)) continue;

                    SafePlayerIdentitySnapshot identity;
                    bool hasIdentity = TryGetSafeIdentity(pc, out identity);
                    string fc = hasIdentity ? identity.FriendCode : "";
                    if (!IsBrokenFriendCode(fc)) continue;

                    int owner = (int)pc.OwnerId;
                    if (brokenFcPunishedOwners.Contains(owner)) continue;
                    brokenFcPunishedOwners.Add(owner);

                    string name = hasIdentity ? identity.Name : $"Player {pc.PlayerId}";
                    string puid = hasIdentity ? identity.Puid : "Unknown";

                    string reason = "Broken FriendCode";
                    AddToBanList(string.IsNullOrWhiteSpace(fc) ? "Unknown" : fc, puid, name, reason);
                    RegisterAntiCheatDisconnectNotice(owner, name, reason, true);
                    AmongUsClient.Instance.KickPlayer(owner, true);
                }
            }
            catch { }
        }

private void TryAutoKickLowLevelTick()
        {
            try
            {
                if (!autoKickLowLevelEnabled)
                {
                    lowLevelKickScanTimer = 0f;
                    lowLevelKickPunishedOwners.Clear();
                    return;
                }

                if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost || PlayerControl.AllPlayerControls == null)
                {
                    lowLevelKickScanTimer = 0f;
                    return;
                }

                if (PlayerControl.AllPlayerControls.Count <= 1)
                    lowLevelKickPunishedOwners.Clear();

                lowLevelKickScanTimer += Time.deltaTime;
                if (lowLevelKickScanTimer < 0.8f) return;
                lowLevelKickScanTimer = 0f;

                int minLevel = Mathf.Clamp(autoKickMinLevel, 1, 300);

                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    if (pc == null || pc == PlayerControl.LocalPlayer || pc.Data == null || pc.Data.Disconnected) continue;
                    if (IsProtectedFromAnticheat(pc)) continue;

                    int level = 1;
                    try
                    {
                        uint rawLevel = pc.Data.PlayerLevel;
                        if (rawLevel != uint.MaxValue && rawLevel < 10000) level = (int)rawLevel + 1;
                    }
                    catch { }

                    if (level >= minLevel) continue;

                    int owner = (int)pc.OwnerId;
                    if (lowLevelKickPunishedOwners.Contains(owner)) continue;
                    lowLevelKickPunishedOwners.Add(owner);

                    string name = string.IsNullOrWhiteSpace(pc.Data.PlayerName) ? "Unknown" : pc.Data.PlayerName;
                    RegisterAntiCheatDisconnectNotice(owner, name, $"Level {level} below minimum {minLevel}", false);
                    AmongUsClient.Instance.KickPlayer(owner, false);
                }
            }
            catch { }
        }

private static void TryAutoGhostAfterStartTick()
        {
            try
            {
                bool gameStarted = AmongUsClient.Instance != null && AmongUsClient.Instance.IsGameStarted;
                if (!gameStarted)
                {
                    wasGameStartedForAutoGhost = false;
                    autoGhostAppliedThisGame = false;
                    return;
                }

                if (!wasGameStartedForAutoGhost)
                {
                    wasGameStartedForAutoGhost = true;
                    autoGhostAppliedThisGame = false;
                }

                if (!autoGhostAfterStart || autoGhostAppliedThisGame || PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data == null)
                    return;

                if (PlayerControl.LocalPlayer.Data.IsDead)
                {
                    autoGhostAppliedThisGame = true;
                    return;
                }

                MakePlayerGhost(PlayerControl.LocalPlayer, false, false);
                autoGhostAppliedThisGame = true;
                ShowNotification("<color=#AA88FF>[AUTO HOST]</color> Auto ghost applied.");
            }
            catch { }
        }

private static void EnsurePlatformBanListLoaded()
        {
            try
            {
                if (string.IsNullOrEmpty(platformBanListPath))
                    platformBanListPath = System.IO.Path.Combine(Plugin.ElysiumFolder, "ElysiumPlatformBanList.txt");

                if (!System.IO.File.Exists(platformBanListPath))
                    System.IO.File.WriteAllText(platformBanListPath, "# One custom platform token per line. Matching PlatformName values are host-banned when enabled.\n# Example: github\n");

                if (Time.unscaledTime < platformBanListNextLoadAt) return;
                platformBanListNextLoadAt = Time.unscaledTime + 3f;

                customPlatformBanTokens.Clear();
                foreach (string rawLine in System.IO.File.ReadAllLines(platformBanListPath))
                {
                    string line = rawLine.Trim();
                    if (line.Length == 0 || line.StartsWith("#")) continue;
                    customPlatformBanTokens.Add(line);
                }
            }
            catch { }
        }

private static bool IsCustomPlatformName(ClientData client, out string platformName)
        {
            platformName = "";
            try
            {
                if (client == null || client.PlatformData == null) return false;
                platformName = client.PlatformData.PlatformName ?? "";
                if (string.IsNullOrWhiteSpace(platformName)) return false;
                if ((int)client.PlatformData.Platform == 112)
                {
                    platformName = "Starlight";
                    return false;
                }

                string enumName = client.PlatformData.Platform.ToString();
                if (platformName.Equals("TESTNAME", StringComparison.OrdinalIgnoreCase)) return false;
                return !platformName.Equals(enumName, StringComparison.OrdinalIgnoreCase) &&
                       !platformName.Equals(GetPlatform(client), StringComparison.OrdinalIgnoreCase);
            }
            catch { }

            return false;
        }

private static bool IsInvalidPlatformData(ClientData client, out string reason)
        {
            reason = "";
            try
            {
                if (client == null || client.PlatformData == null) return false;

                var platform = client.PlatformData;
                string pName = platform.PlatformName ?? "";
                ulong xuid = platform.XboxPlatformId;
                ulong psid = platform.PsnPlatformId;
                bool isValid = true;

                switch (platform.Platform)
                {
                    case Platforms.StandaloneEpicPC:
                    case Platforms.StandaloneSteamPC:
                    case Platforms.StandaloneMac:
                    case Platforms.StandaloneItch:
                    case Platforms.IPhone:
                    case Platforms.Android:
                        isValid = (pName == "TESTNAME" && xuid == 0 && psid == 0);
                        break;
                    case Platforms.StandaloneWin10:
                        isValid = (pName == "TESTNAME" && xuid != 0 && psid == 0);
                        break;
                    case Platforms.Xbox:
                        isValid = (pName != "TESTNAME" && pName.Length >= 3 && xuid != 0 && psid == 0);
                        break;
                    case Platforms.Playstation:
                        isValid = (pName != "TESTNAME" && xuid == 0 && psid != 0);
                        break;
                    case Platforms.Switch:
                        isValid = (pName != "TESTNAME" && xuid == 0 && psid == 0);
                        break;
                }

                if (!isValid)
                {
                    reason = $"Platform Spoof detected ({platform.Platform})";
                    return true;
                }
            }
            catch { }

            return false;
        }

private static bool MatchesPlatformBanTxt(ClientData client, out string platformName, out string matchedToken)
        {
            platformName = "";
            matchedToken = "";
            EnsurePlatformBanListLoaded();

            if (!IsCustomPlatformName(client, out platformName) || customPlatformBanTokens.Count == 0)
                return false;

            foreach (string token in customPlatformBanTokens)
            {
                if (platformName.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    matchedToken = token;
                    return true;
                }
            }

            return false;
        }

private static void HostBanForPlatform(PlayerControl player, string reason)
        {
            try
            {
                if (player == null || player == PlayerControl.LocalPlayer || player.Data == null ||
                    AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
                    return;
                if (IsProtectedFromAnticheat(player)) return;

                int owner = (int)player.OwnerId;
                if (platformSpoofPunishedOwners.Contains(owner)) return;
                platformSpoofPunishedOwners.Add(owner);

                SafePlayerIdentitySnapshot identity;
                bool hasIdentity = TryGetSafeIdentity(player, out identity);
                string name = hasIdentity ? identity.Name : $"Player {player.PlayerId}";
                string fc = hasIdentity ? identity.FriendCode : "Unknown";
                string puid = hasIdentity ? identity.Puid : "Unknown";

                AddToBanList(fc, puid, name, reason);
                RegisterAntiCheatDisconnectNotice(owner, name, reason, true);
                AmongUsClient.Instance.KickPlayer(owner, true);
            }
            catch { }
        }

private static void TryAutoBanCustomPlatformsTick()
        {
            try
            {
                if ((!autoBanPlatformSpoof && !banCustomPlatformsFromTxt) ||
                    AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost || PlayerControl.AllPlayerControls == null)
                {
                    platformBanScanTimer = 0f;
                    return;
                }

                platformBanScanTimer += Time.deltaTime;
                if (platformBanScanTimer < 1f) return;
                platformBanScanTimer = 0f;

                foreach (PlayerControl pc in PlayerControl.AllPlayerControls)
                {
                    if (pc == null || pc == PlayerControl.LocalPlayer || pc.Data == null || pc.Data.Disconnected) continue;

                    ClientData client = null;
                    try { client = AmongUsClient.Instance.GetClientFromCharacter(pc); } catch { }
                    if (client == null) continue;

                    if (banCustomPlatformsFromTxt && MatchesPlatformBanTxt(client, out string platformName, out string token))
                    {
                        HostBanForPlatform(pc, $"Custom platform TXT match '{token}' ({platformName})");
                        continue;
                    }

                    if (autoBanPlatformSpoof && IsInvalidPlatformData(client, out string reason))
                        HostBanForPlatform(pc, reason);
                }
            }
            catch { }
        }

        [HideFromIl2Cpp]
        private bool DrawDeviceRandomButton()
        {
            return GUILayout.Button("*", btnStyle, GUILayout.Width(26f), GUILayout.Height(22f));
        }

        [HideFromIl2Cpp]
        private static void ResetIdentityEditors()
        {
            isEditingName = false;
            isEditingLevel = false;
            isEditingFriendCode = false;
            isEditingLocalFriendCode = false;
            isEditingDeviceId = false;
            isEditingGhostChatColor = false;
        }

        [HideFromIl2Cpp]
        private static bool IsIdentityValueDisabled(string value)
        {
            return string.IsNullOrWhiteSpace(value) || string.Equals(value.Trim(), "0", StringComparison.Ordinal);
        }

        [HideFromIl2Cpp]
        private void DrawIdentityLabel(string label, bool active, float labelWidth)
        {
            identityLabelStyle.normal.textColor = active ? GetMenuAccentColor() : GUI.contentColor;
            GUILayout.Label(label, identityLabelStyle, GUILayout.Width(labelWidth), GUILayout.Height(22f));
        }

        [HideFromIl2Cpp]
        private static bool IsIdentitySubmitKeyPressed(bool editing)
        {
            Event e = Event.current;
            return editing && e != null && e.type == EventType.KeyDown &&
                (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter);
        }

        [HideFromIl2Cpp]
        private void CommitIdentityText(ref bool enabled, ref string value, System.Action apply, System.Action reset)
        {
            value = (value ?? string.Empty).Trim();
            enabled = !IsIdentityValueDisabled(value);

            if (enabled) apply?.Invoke();
            else reset?.Invoke();

            settingsDirty = true;
            SaveConfig();
        }

        [HideFromIl2Cpp]
        private void DrawIdentityTextRow(ref bool enabled, string label, ref string value, ref bool editing,
            System.Action apply, System.Action reset, int maxChars, float labelWidth, bool canRandomize = false)
        {
            GUILayout.BeginHorizontal(GUILayout.Height(22f));
            DrawIdentityLabel(label, enabled, labelWidth);
            GUILayout.Space(4f);

            bool clickedInput = DrawPseudoInputButton(value, editing, 22f, maxChars);
            bool submit = IsIdentitySubmitKeyPressed(editing);
            if (clickedInput || submit)
            {
                bool wasEditing = editing;
                editing = !editing;
                if (wasEditing || submit)
                {
                    editing = false;
                    CommitIdentityText(ref enabled, ref value, apply, reset);
                }
                else
                {
                    ResetIdentityEditors();
                    editing = true;
                }
                if (submit) Event.current.Use();
            }

            if (canRandomize && DrawDeviceRandomButton())
            {
                value = Guid.NewGuid().ToString("N");
                ResetIdentityEditors();
                CommitIdentityText(ref enabled, ref value, apply, reset);
            }

            if (GUILayout.Button(enabled ? "Enabled" : "Disabled", enabled ? activeTabStyle : btnStyle, GUILayout.Width(52f), GUILayout.Height(22f)))
            {
                editing = false;
                if (enabled)
                {
                    enabled = false;
                    reset?.Invoke();
                    settingsDirty = true;
                    SaveConfig();
                }
                else if (!IsIdentityValueDisabled(value))
                {
                    CommitIdentityText(ref enabled, ref value, apply, reset);
                }
            }
            GUILayout.EndHorizontal();
        }

        [HideFromIl2Cpp]
private void CommitIdentityLevel()
        {
            spoofLevelString = (spoofLevelString ?? string.Empty).Trim();
            bool hasLevel = uint.TryParse(spoofLevelString, out uint level) && level > 0;
            bool wasEnabled = enableLevelSpoof;
            enableLevelSpoof = hasLevel;

            if (hasLevel) ApplyLevelSpoofValue(level);
            else if (wasEnabled) RestoreLevelSpoofDefault();

            lastLevelSpoofInput = spoofLevelString;
            settingsDirty = true;
            SaveConfig();
        }

        [HideFromIl2Cpp]
        private void DrawIdentityLevelRow(float labelWidth)
        {
            if (!isEditingLevel)
                lastLevelSpoofInput = spoofLevelString ?? string.Empty;
            else if (!string.Equals(lastLevelSpoofInput, spoofLevelString ?? string.Empty, StringComparison.Ordinal))
                CommitIdentityLevel();

            GUILayout.BeginHorizontal(GUILayout.Height(22f));
            DrawIdentityLabel("Level Spoof", enableLevelSpoof, labelWidth);
            GUILayout.Space(4f);

            bool clickedInput = DrawPseudoInputButton(spoofLevelString, isEditingLevel, 22f, 32);
            bool submit = IsIdentitySubmitKeyPressed(isEditingLevel);
            if (clickedInput || submit)
            {
                bool wasEditing = isEditingLevel;
                isEditingLevel = !isEditingLevel;
                if (wasEditing || submit)
                {
                    isEditingLevel = false;
                    CommitIdentityLevel();
                }
                else
                {
                    ResetIdentityEditors();
                    isEditingLevel = true;
                }
                if (submit) Event.current.Use();
            }

            if (GUILayout.Button("-", btnStyle, GUILayout.Width(24f), GUILayout.Height(22f)))
            {
                uint level = uint.TryParse(spoofLevelString, out uint parsed) ? parsed : 0;
                spoofLevelString = level > 0 ? (level - 1).ToString() : "0";
                isEditingLevel = false;
                CommitIdentityLevel();
            }
            if (GUILayout.Button("+", btnStyle, GUILayout.Width(24f), GUILayout.Height(22f)))
            {
                uint level = uint.TryParse(spoofLevelString, out uint parsed) ? parsed : 0;
                spoofLevelString = Mathf.Min(level + 1u, 999999u).ToString();
                isEditingLevel = false;
                CommitIdentityLevel();
            }
            GUILayout.EndHorizontal();
        }

private void DrawSelfSpoof()
        {
            float contentWidth = GetMenuWorkWidth(220f, 610f);
            GUIStyle compactCard = CreateCompactMenuCardStyle();
            GUIStyle statusStyle = compactStatusStyle;
            float sideWidth = 150f;
            float mainWidth = contentWidth - sideWidth - 8f;
            float identityLabelWidth = Mathf.Clamp(mainWidth * 0.31f, 100f, 132f);
            const float platformCardHeight = 84f;
            const float taskCardHeight = 150f;
            const float sideCardGap = 6f;

            GUILayout.BeginHorizontal(GUILayout.Width(contentWidth));

            GUILayout.BeginVertical(compactCard, GUILayout.Width(mainWidth), GUILayout.Height(identityCardHeight));
            DrawMenuSectionHeader("IDENTITY");
            GUILayout.Space(2);
            DrawIdentityLevelRow(identityLabelWidth);
            GUILayout.Space(3);
            DrawIdentityTextRow(ref enableLocalNameSpoof, "Local Name", ref customNameInput, ref isEditingName,
                () => ApplyLocalNameSelf(customNameInput, true), RestoreLocalNameSelf, 54, identityLabelWidth);
            GUILayout.Space(3);
            DrawIdentityTextRow(ref enableLocalFriendCodeSpoof, "Local Friend Code", ref localFriendCodeInput, ref isEditingLocalFriendCode,
                () => ApplyLocalFriendCodeSelf(localFriendCodeInput, true), RestoreLocalFriendCodeSelf, 54, identityLabelWidth);
            GUILayout.Space(3);
            DrawIdentityTextRow(ref enableFriendCodeSpoof, "Spoof Friend Code", ref spoofFriendCodeInput, ref isEditingFriendCode,
                ApplyPublicFriendCode, null, 54, identityLabelWidth);
            GUILayout.Space(3);
            DrawIdentityTextRow(ref enableDeviceIdSpoof, "Device ID", ref spoofedDeviceId, ref isEditingDeviceId,
                () => spoofedDeviceId = (spoofedDeviceId ?? "").Trim(), null, 64, identityLabelWidth, true);
            GUILayout.EndVertical();
            Rect identityCardRect = GUILayoutUtility.GetLastRect();

            GUILayout.Space(8);

            GUILayout.BeginVertical(GUILayout.Width(sideWidth));
            GUILayout.BeginVertical(compactCard, GUILayout.Width(sideWidth), GUILayout.Height(platformCardHeight));
            DrawMenuSectionHeader("PLATFORM");
            if (GUILayout.Button(enablePlatformSpoof ? "SPOOF ON" : "SPOOF OFF", enablePlatformSpoof ? activeTabStyle : btnStyle, GUILayout.Height(22)))
            {
                enablePlatformSpoof = !enablePlatformSpoof;
                SaveConfig();
            }
            string hexColor = GetMenuAccentHex();
            GUILayout.Label($"<color=#{hexColor}>{platformNames[currentPlatformIndex]}</color>", statusStyle, GUILayout.Height(15));
            int newPlatIdx = (int)GUILayout.HorizontalSlider(currentPlatformIndex, 0, platformNames.Length - 1, sliderStyle, sliderThumbStyle, GUILayout.ExpandWidth(true));
            if (newPlatIdx != currentPlatformIndex)
            {
                currentPlatformIndex = newPlatIdx;
                SaveConfig();
            }
            GUILayout.EndVertical();

            GUILayout.Space(sideCardGap);

            GUILayout.BeginVertical(compactCard, GUILayout.Width(sideWidth), GUILayout.Height(taskCardHeight));
            DrawMenuSectionHeader("TASKS");
            autoTasksEnabled = DrawCompactToggle(autoTasksEnabled, "Auto Tasks", 126);
            GUILayout.Space(3);
            GUILayout.Label($"Delay: {autoTasksDelay:0.0}s", statusStyle, GUILayout.Height(15));
            autoTasksDelay = GUILayout.HorizontalSlider(autoTasksDelay, 0.8f, 6f,
                sliderStyle, sliderThumbStyle, GUILayout.ExpandWidth(true));
            GUILayout.Label($"Remaining: {AutoTasksFeature.RemainingTasks()}", statusStyle, GUILayout.Height(15));
            GUILayout.Space(3);
            if (GUILayout.Button("Complete", btnStyle, GUILayout.Height(24)))
                CompleteLocalTasks();
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
            Rect tasksCardRect = GUILayoutUtility.GetLastRect();
            GUILayout.EndVertical();

            // A fixed height can be smaller than the enlarged controls at some menu
            // scales. On repaint, match the visible lower edge to TASKS itself.
            if (Event.current != null && Event.current.type == EventType.Repaint)
            {
                float correction = tasksCardRect.yMax - identityCardRect.yMax;
                if (Mathf.Abs(correction) > 0.5f)
                    identityCardHeight = Mathf.Max(1f, identityCardHeight + correction);
            }

            GUILayout.EndHorizontal();
        }

private void CompleteLocalTasks()
        {
            try
            {
                PlayerControl local = PlayerControl.LocalPlayer;
                if (local == null || local.Data == null || local.myTasks == null || ShipStatus.Instance == null)
                    return;

                if (MeetingHud.Instance != null || ExileController.Instance != null ||
                    local.Data.IsDead || local.Data.Disconnected)
                    return;

                if (local.Data.Role != null && local.Data.Role.IsImpostor)
                {
                    ShowNotification("<color=#FF4444>[TASKS]</color> Available to crewmates only.");
                    return;
                }

                int completed = 0;
                for (int i = 0; i < local.myTasks.Count; i++)
                {
                    PlayerTask task = local.myTasks[i];
                    if (task == null || task.IsComplete) continue;
                    local.RpcCompleteTask((uint)task.Id);
                    completed++;
                }

                if (completed == 0)
                {
                    ShowNotification("<color=#AAAAAA>[TASKS]</color> No incomplete tasks.");
                    return;
                }

                ShowNotification($"<color=#00FFAA>[TASKS]</color> Completed {completed} task(s).");
            }
            catch { }
        }

private void DrawVisualsTab()
        {
            GUILayout.BeginHorizontal();
            for (int i = 0; i < visualsSubTabs.Length; i++)
            {
                if (GUILayout.Button(visualsSubTabs[i], currentVisualsSubTab == i ? activeSubTabStyle : subTabStyle, GUILayout.Height(18)))
                    SetMultiTab("visuals", ref currentVisualsSubTab, i, visualsSubTabs.Length);
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(8);
            BeginMultiTabContent("visuals", out Matrix4x4 oldMatrix, out Color oldColor);
            try
            {
                if (currentVisualsSubTab == 0) DrawVisualsInGame();
                else if (currentVisualsSubTab == 1) DrawOutfitsTab();
            }
            finally
            {
                EndMultiTabContent(oldMatrix, oldColor);
            }
        }

[HarmonyPatch(typeof(PlayerBanData), nameof(PlayerBanData.BanPoints), MethodType.Setter)]
        public static class RemoveDisconnectPenalty_Patch
        {
            public static bool Prefix(PlayerBanData __instance, ref float value)
            {
                if (!ElysiumModMenuGUI.removePenalty) return true;
                if (AmongUsClient.Instance == null || AmongUsClient.Instance.NetworkMode != NetworkModes.OnlineGame)
                    return true;

                value = 0f;
                return false;
            }
        }

[HarmonyPatch(typeof(DisconnectPopup), nameof(DisconnectPopup.DoShow))]
        public static class DisconnectPopup_CopyRoomCode_Patch
        {
            public static void Postfix(DisconnectPopup __instance)
            {
                try
                {
                    if (!ElysiumModMenuGUI.TryCopyRoomCodeToClipboard(false)) return;
                    if (__instance != null && __instance._textArea != null)
                        __instance.SetText(__instance._textArea.text + "\n\n<size=60%>Room code copied to clipboard</size>");
                }
                catch { }
            }
        }

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.ExitGame))]
        public static class AmongUsClient_ExitGame_CopyRoomCode_Patch
        {
            public static void Prefix()
            {
                ElysiumModMenuGUI.TryCopyRoomCodeToClipboard(true);
            }
        }

[HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Start))]
        public static class ShowLobbyTimer_Patch
        {
            public static void Prefix(GameStartManager __instance)
            {
                if (__instance == null) return;

                try
                {
                    if (__instance.StartButtonGlyph == null)
                        __instance.StartButtonGlyph = __instance.GetComponentInChildren<ActionMapGlyphDisplay>(true);

                    if (__instance.StartButtonGlyphContainer == null && __instance.StartButtonGlyph != null)
                        __instance.StartButtonGlyphContainer = __instance.StartButtonGlyph.gameObject;
                }
                catch { }
            }

            public static void Postfix(GameStartManager __instance)
            {
                if (!ElysiumModMenuGUI.alwaysShowLobbyTimer) return;

                if (__instance == null || GameData.Instance == null || AmongUsClient.Instance == null) return;
                if (AmongUsClient.Instance.NetworkMode == NetworkModes.LocalGame || !AmongUsClient.Instance.AmHost) return;

                RainbowLobbyCodeTimer_Patch.Reset(__instance);
                if (!ElysiumModMenuGUI.rainbowLobbyTimer && HudManager.Instance != null)
                {
                    HudManager.Instance.ShowLobbyTimer(600);
                }
            }
        }

[HarmonyPatch(typeof(TimerTextTMP), nameof(TimerTextTMP.Update))]
        public static class LobbyTimerColor_Patch
        {
            private static TimerTextTMP timer;
            private static Color normalColor;
            private static bool rainbowApplied;

            public static void Postfix(TimerTextTMP __instance)
            {
                try
                {
                    if (__instance == null || __instance.text == null || HudManager.Instance == null) return;
                    if (HudManager.Instance.LobbyTimerExtensionUI == null ||
                        HudManager.Instance.LobbyTimerExtensionUI.timerText != __instance) return;

                    if (timer != __instance)
                    {
                        if (timer != null && timer.text != null && rainbowApplied)
                            timer.text.color = normalColor;

                        timer = __instance;
                        normalColor = __instance.text.color;
                        rainbowApplied = false;
                    }

                    if (ElysiumModMenuGUI.alwaysShowLobbyTimer && ElysiumModMenuGUI.rainbowLobbyTimer)
                    {
                        if (!rainbowApplied)
                        {
                            normalColor = __instance.text.color;
                            rainbowApplied = true;
                        }

                        __instance.text.enabled = false;
                    }
                    else if (rainbowApplied)
                    {
                        __instance.text.enabled = true;
                        __instance.text.color = normalColor;
                        rainbowApplied = false;
                    }
                }
                catch { }
            }
        }

[HarmonyPatch(typeof(GameStartManager), "Update")]
        public static class RainbowLobbyCodeTimer_Patch
        {
            private static GameStartManager manager;
            private static string baseText = "";
            private static string renderedText = "";
            private static float timer = 600f;
            private static bool applied;

            public static void Reset(GameStartManager instance)
            {
                manager = instance;
                baseText = instance != null && instance.GameRoomNameCode != null
                    ? instance.GameRoomNameCode.text ?? ""
                    : "";
                renderedText = "";
                timer = 600f;
                applied = false;
            }

            public static void Postfix(GameStartManager __instance)
            {
                try
                {
                    if (__instance == null || __instance.GameRoomNameCode == null) return;
                    if (manager != __instance) Reset(__instance);

                    bool inLobby = LobbyBehaviour.Instance != null &&
                                   AmongUsClient.Instance != null &&
                                   AmongUsClient.Instance.NetworkMode != NetworkModes.LocalGame &&
                                   AmongUsClient.Instance.AmHost;
                    if (inLobby)
                        timer = Mathf.Max(0f, timer - Time.deltaTime);

                    bool rainbow = inLobby &&
                                   ElysiumModMenuGUI.alwaysShowLobbyTimer &&
                                   ElysiumModMenuGUI.rainbowLobbyTimer;
                    TimerTextTMP timerText = null;
                    if (HudManager.Instance != null && HudManager.Instance.LobbyTimerExtensionUI != null)
                        timerText = HudManager.Instance.LobbyTimerExtensionUI.timerText;

                    if (!rainbow)
                    {
                        if (applied && __instance.GameRoomNameCode.text == renderedText)
                            __instance.GameRoomNameCode.text = baseText;

                        if (timerText != null && timerText.text != null)
                            timerText.text.enabled = true;

                        if (applied && inLobby && ElysiumModMenuGUI.alwaysShowLobbyTimer &&
                            HudManager.Instance != null && HudManager.Instance.LobbyTimerExtensionUI == null)
                        {
                            HudManager.Instance.ShowLobbyTimer(Mathf.CeilToInt(timer));
                        }

                        applied = false;
                        renderedText = "";
                        return;
                    }

                    if (timerText != null && timerText.text != null)
                        timerText.text.enabled = false;

                    string current = __instance.GameRoomNameCode.text ?? "";
                    if (!applied || current != renderedText)
                        baseText = current;

                    int seconds = Mathf.Max(0, Mathf.FloorToInt(timer));
                    Color color = Color.HSVToRGB(Mathf.Repeat(Time.unscaledTime * 0.18f, 1f), 0.85f, 1f);
                    string hex = ColorUtility.ToHtmlStringRGB(color);
                    renderedText = $"{baseText} <color=#{hex}>({seconds / 60}:{seconds % 60:00})</color>";
                    __instance.GameRoomNameCode.text = renderedText;
                    applied = true;
                }
                catch { }
            }
        }

[HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.ToggleButtonGlyphs))]
        public static class GameStartManager_ToggleButtonGlyphs_Guard_Patch
        {
            public static bool Prefix(GameStartManager __instance)
            {
                try
                {
                    return __instance != null &&
                           __instance.gameObject != null &&
                           __instance.StartButtonGlyph != null &&
                           __instance.StartButtonGlyphContainer != null;
                }
                catch
                {
                    return false;
                }
            }
        }

public static bool IsCursorOverMenu()
        {
            try
            {
                if (!showMenu || !blockGameClicks) return false;
                ClampMenuWindowToScreen();
                Vector2 guiPos = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
                float scale = GetEffectiveMenuScale();
                return windowRect.Contains(guiPos / scale);
            }
            catch { return false; }
        }

public static bool IsCursorOverVisibleMenu()
        {
            try
            {
                if (!showMenu) return false;
                ClampMenuWindowToScreen();
                Vector2 guiPos = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
                float scale = GetEffectiveMenuScale();
                return windowRect.Contains(guiPos / scale);
            }
            catch { return false; }
        }

private static bool IsChatOpenForZoomBlock()
        {
            try
            {
                ChatController chat = HudManager.Instance?.Chat;
                return chat != null && chat.IsOpenOrOpening;
            }
            catch { return false; }
        }

private static bool IsCameraZoomScrollAllowed()
        {
            try
            {
                if (IsCursorOverVisibleMenu()) return false;
                if (IsChatOpenForZoomBlock()) return false;
                if (MeetingHud.Instance != null) return false;
                if (Minigame.Instance != null) return false;
                if (UnityEngine.EventSystems.EventSystem.current != null &&
                    UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                    return false;
                if (UnityEngine.Object.FindObjectOfType<FindAGameManager>() != null) return false;
                if (PlayerCustomizationMenu.Instance != null) return false;
                if (FriendsListUI.Instance != null && FriendsListUI.Instance.IsOpen) return false;
                if (LobbyBehaviour.Instance != null && GameStartManager.Instance != null)
                {
                    try
                    {
                        if (GameStartManager.Instance.LobbyInfoPane != null &&
                            GameStartManager.Instance.LobbyInfoPane.LobbyViewSettingsPane != null &&
                            GameStartManager.Instance.LobbyInfoPane.LobbyViewSettingsPane.gameObject.active)
                            return false;
                    }
                    catch { }

                    try
                    {
                        if (GameStartManager.Instance.RulesEditPanel != null)
                            return false;
                    }
                    catch { }
                }
            }
            catch { }

            return true;
        }

private static void RestoreFreecamCamera()
        {
            try
            {
                if (PlayerControl.LocalPlayer != null)
                    PlayerControl.LocalPlayer.moveable = true;

                if (Camera.main != null)
                {
                    var cam = Camera.main.gameObject.GetComponent<FollowerCamera>();
                    if (cam != null && PlayerControl.LocalPlayer != null)
                    {
                        cam.enabled = true;
                        cam.SetTarget(PlayerControl.LocalPlayer);
                    }
                }

                _freecamActive = false;
            }
            catch { }
        }
}
}
