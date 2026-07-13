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

public static bool PrepareLocalFriendCodeForSerialize(NetworkedPlayerInfo data, out string restoreValue)
        {
            restoreValue = null;
            return false;
        }

public static void RestoreLocalFriendCodeAfterSerialize(NetworkedPlayerInfo data, string restoreValue)
        {
            try
            {
                if (data == null || restoreValue == null) return;
                TrySetStringMember(data, "FriendCode", restoreValue);
            }
            catch { }
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
                    AmongUsClient.Instance.KickPlayer(owner, false);
                    ShowNotification($"<color=#FF4444>[LEVEL KICK]</color> {name} kicked: level {level} < {minLevel}");
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

private void DrawSelfSpoof()
        {
            float contentWidth = GetMenuWorkWidth(220f, 610f);
            GUIStyle compactCard = CreateCompactMenuCardStyle();
            GUIStyle statusStyle = new GUIStyle(toggleLabelStyle) { fontSize = 10, alignment = TextAnchor.MiddleCenter, richText = true, clipping = TextClipping.Clip };
            float sideWidth = 150f;
            float mainWidth = contentWidth - sideWidth - 8f;
            float identityLabelWidth = Mathf.Clamp(mainWidth * 0.38f, 118f, 140f);
            float okWidth = 34f;

            void ResetSelfEditors()
            {
                isEditingName = false;
                isEditingLevel = false;
                isEditingFriendCode = false;
                isEditingLocalFriendCode = false;
                isEditingDeviceId = false;
                isEditingGhostChatColor = false;

            }

            void DrawIdentityRow(ref bool enabled, string toggleText, ref string value, ref bool editing, System.Action apply, int maxChars)
            {
                GUILayout.BeginHorizontal(GUILayout.Height(22));

                Rect animSwitchRect = GUILayoutUtility.GetRect(28f, 14f, GUILayout.Width(28f), GUILayout.Height(14f));
                bool clickedBox = GUI.Button(animSwitchRect, "", enabled ? trackOnStyle : trackOffStyle);
                DrawAnimatedSwitch(animSwitchRect, enabled, "Identity:" + toggleText);
                GUILayout.Space(6);

                GUIStyle identityLabelStyle = new GUIStyle(toggleLabelStyle)
                {
                    fontSize = 11,
                    clipping = TextClipping.Clip,
                    wordWrap = false,
                    richText = true,
                    stretchWidth = false,
                    alignment = TextAnchor.MiddleLeft
                };
                Rect textRect = GUILayoutUtility.GetRect(identityLabelWidth, 16f, GUILayout.Width(identityLabelWidth), GUILayout.Height(16f));
                GUI.Label(textRect, toggleText, identityLabelStyle);
                bool clickedText = Event.current.type == EventType.MouseDown && textRect.Contains(Event.current.mousePosition);
                if (clickedText) Event.current.Use();

                if (clickedBox || clickedText)
                {
                    enabled = !enabled;
                    settingsDirty = true;
                    apply?.Invoke();
                    SaveConfig();
                }

                GUILayout.Space(4);
                if (DrawPseudoInputButton(value, editing, 22f, maxChars))
                {
                    bool newEditing = !editing;
                    ResetSelfEditors();
                    editing = newEditing;
                }
                if (GUILayout.Button("OK", btnStyle, GUILayout.Width(okWidth), GUILayout.Height(22)))
                {
                    editing = false;
                    apply?.Invoke();
                    SaveConfig();
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.BeginHorizontal(GUILayout.Width(contentWidth));

            GUILayout.BeginVertical(compactCard, GUILayout.Width(mainWidth), GUILayout.Height(175));
            DrawMenuSectionHeader("IDENTITY");
            GUILayout.Space(2);
            DrawIdentityRow(ref enableLevelSpoof, "Level Spoof", ref spoofLevelString, ref isEditingLevel, () =>
            {
                if (enableLevelSpoof && uint.TryParse(spoofLevelString, out uint parsedLvl)) ApplyLevelSpoofValue(parsedLvl);
                else if (!enableLevelSpoof) RestoreLevelSpoofDefault();
            }, 32);
            GUILayout.Space(3);
            DrawIdentityRow(ref enableLocalNameSpoof, "Local Name", ref customNameInput, ref isEditingName, () =>
            {
                if (enableLocalNameSpoof) ApplyLocalNameSelf(customNameInput, true);
                else RestoreLocalNameSelf();
            }, 54);
            GUILayout.Space(3);
            DrawIdentityRow(ref enableLocalFriendCodeSpoof, "Local Friend Code", ref localFriendCodeInput, ref isEditingLocalFriendCode, () =>
            {
                if (enableLocalFriendCodeSpoof) ApplyLocalFriendCodeSelf(localFriendCodeInput, true);
                else RestoreLocalFriendCodeSelf();
            }, 54);
            GUILayout.Space(3);
            DrawIdentityRow(ref enableFriendCodeSpoof, "Public Friend Code", ref spoofFriendCodeInput, ref isEditingFriendCode, () =>
            {
                spoofFriendCodeInput = SanitizeSpoofFriendCode(spoofFriendCodeInput);
            }, 54);
            GUILayout.Space(3);
            DrawIdentityRow(ref enableDeviceIdSpoof, "Device ID", ref spoofedDeviceId, ref isEditingDeviceId, () =>
            {
                spoofedDeviceId = (spoofedDeviceId ?? "").Trim();
            }, 64);
            GUILayout.EndVertical();

            GUILayout.Space(8);

            GUILayout.BeginVertical(GUILayout.Width(sideWidth));
            GUILayout.BeginVertical(compactCard, GUILayout.Width(sideWidth), GUILayout.Height(74));
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

            GUILayout.Space(6);

            GUILayout.BeginVertical(compactCard, GUILayout.Width(sideWidth), GUILayout.Height(70));
            DrawMenuSectionHeader("TASKS");
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Complete", btnStyle, GUILayout.Height(24)))
            {
                if (PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.myTasks != null)
                    foreach (var task in PlayerControl.LocalPlayer.myTasks)
                        if (task != null && !task.IsComplete) PlayerControl.LocalPlayer.RpcCompleteTask((uint)task.Id);
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

private void DrawVisualsTab()
        {
            GUILayout.BeginHorizontal();
            for (int i = 0; i < visualsSubTabs.Length; i++)
                if (GUILayout.Button(visualsSubTabs[i], currentVisualsSubTab == i ? activeSubTabStyle : subTabStyle, GUILayout.Height(18)))
                { currentVisualsSubTab = i; scrollPosition = Vector2.zero; }
            GUILayout.EndHorizontal();
            GUILayout.Space(8);
            if (currentVisualsSubTab == 0) DrawVisualsInGame();
            else if (currentVisualsSubTab == 1) DrawOutfitsTab();
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
            public static void Postfix(GameStartManager __instance)
            {
                if (!ElysiumModMenuGUI.alwaysShowLobbyTimer) return;

                if (__instance == null || GameData.Instance == null || AmongUsClient.Instance == null) return;
                if (AmongUsClient.Instance.NetworkMode == NetworkModes.LocalGame || !AmongUsClient.Instance.AmHost) return;

                if (HudManager.Instance != null)
                {
                    HudManager.Instance.ShowLobbyTimer(600);
                }
            }
        }

public static bool IsCursorOverMenu()
        {
            try
            {
                if (!showMenu || !hardMenu) return false;
                ClampMenuWindowToScreen();
                Vector2 guiPos = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
                return windowRect.Contains(guiPos);
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
                return windowRect.Contains(guiPos);
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

private static bool TryGetAspectDistance(object aspectPosition, out Vector3 distance)
        {
            distance = Vector3.zero;
            if (aspectPosition == null) return false;

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            try
            {
                Type type = aspectPosition.GetType();
                FieldInfo field = type.GetField("DistanceFromEdge", flags) ?? type.GetField("distanceFromEdge", flags);
                if (field != null && field.GetValue(aspectPosition) is Vector3 fieldValue)
                {
                    distance = fieldValue;
                    return true;
                }

                PropertyInfo property = type.GetProperty("DistanceFromEdge", flags) ?? type.GetProperty("distanceFromEdge", flags);
                if (property != null && property.GetValue(aspectPosition, null) is Vector3 propertyValue)
                {
                    distance = propertyValue;
                    return true;
                }
            }
            catch { }

            return false;
        }

private static bool TrySetAspectDistance(object aspectPosition, Vector3 distance)
        {
            if (aspectPosition == null) return false;

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            try
            {
                Type type = aspectPosition.GetType();
                FieldInfo field = type.GetField("DistanceFromEdge", flags) ?? type.GetField("distanceFromEdge", flags);
                if (field != null)
                {
                    field.SetValue(aspectPosition, distance);
                    return true;
                }

                PropertyInfo property = type.GetProperty("DistanceFromEdge", flags) ?? type.GetProperty("distanceFromEdge", flags);
                if (property != null && property.CanWrite)
                {
                    property.SetValue(aspectPosition, distance, null);
                    return true;
                }
            }
            catch { }

            return false;
        }

private static object GetHudAspectPosition(HudManager hud)
        {
            if (hud == null) return null;

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            try
            {
                Type type = hud.GetType();
                FieldInfo field = type.GetField("aspectPosition", flags) ?? type.GetField("AspectPosition", flags);
                if (field != null) return field.GetValue(hud);

                PropertyInfo property = type.GetProperty("aspectPosition", flags) ?? type.GetProperty("AspectPosition", flags);
                if (property != null) return property.GetValue(hud, null);
            }
            catch { }

            return null;
        }

private static void RefreshHudResolutionForZoom()
        {
            try
            {
                int w = Screen.width;
                int h = Screen.height;
                if (w < 320 || h < 240) return;

                ResolutionManager.ResolutionChanged.Invoke((float)w / h, w, h, Screen.fullScreen);
            }
            catch { }
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
