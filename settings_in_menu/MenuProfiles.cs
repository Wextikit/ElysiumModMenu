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
using System.Globalization;
using System.IO;
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
private const int menuProfileCount = 5;
private int selectedMenuProfileIndex = 0;
private string menuProfileStatus = "";
private float menuProfileStatusUntil = 0f;

private static readonly string[] menuProfileBoolKeys = {
            "M_ActivateCompletedCosmicubes", "M_AllowDuplicateColors", "M_AllowLinksAndSymbols", "M_AllowTasksAsImpostor",
            "M_AlwaysChat", "M_AlwaysShowLobbyTimer", "M_AntiPasosHostBan", "M_AntiPasosLocalBan",
            "M_AutoBanEnabled", "M_AutoBanPlatformSpoof", "M_AutoBreakSabotage", "M_AutoClearClonesBeforeGame", "M_AutoCopyCodeAndLeave",
            "M_AutoFollowCursor", "M_AutoGhostAfterStart", "M_AutoHostAutoRunEnabled", "M_AutoHostCancelBelowMin",
            "M_AutoHostEnabled", "M_AutoHostForceLastMinute", "M_AutoHostInstantStart", "M_AutoHostNotifications",
            "M_AutoHostShieldBreakEnabled", "M_AutoHostWaitLoadedPlayers", "M_AutoKickBugs", "M_AutoKickLowLevel",
            "M_AutoRepairSabotage", "M_AutoReturnLobbyAfterMatch", "M_BanCustomPlatformsFromTxt", "M_BanMalformedPacketSender",
            "M_BanQuickChatEmptySpammer", "M_BanVoteKickVoters", "M_BlockChatFloodRpc", "M_BlockFortegreenChat",
            "M_BlockGameRpcInLobby", "M_BlockInnerslothTelemetry", "M_BlockMeetingFloodRpc", "M_BlockRainbowChat",
            "M_BlockSabotageRPC", "M_BlockServerTeleports", "M_BlockSpoofRPC", "M_BlockVentKickExploit",
            "M_BoldMenuText", "M_BugRoomAutoAngel", "M_BugRoomAutoKillShield", "M_BugroomGlitchFinderEnabled", "M_BugroomScoutEnabled",
            "M_BugRoomTimedAutoRun", "M_BypassAgeRestrictions", "M_CameraZoom", "M_ChatAsEveryone",
            "M_DetailedLogsEnabled", "M_DeviceIdSpoof", "M_DisableEndGameSafeMode", "M_DisableMapSafeMode",
            "M_DisableVoteKicks", "M_DiscordRpcEnabled", "M_DragToCursor", "M_EnableBackground", "M_EnableMenuCharacter",
            "M_EnableChatBubbleCopy", "M_EnableChatHistory", "M_EnableChatLog", "M_EnableChatNickCopy",
            "M_EnableClipboard", "M_EnableColorCommand", "M_EnableCustomNotifs", "M_EnableExtendedChat",
            "M_EnableFastChat", "M_EnableLevelSpoof", "M_EnableMenuScaleInput", "M_EspShimmerMode",
            "M_ExtendedLobby", "M_Freecam", "M_FullBright", "M_GuestExtraFeatures",
            "M_HardMenu", "M_HideRadarInMeeting", "M_HostAutoKillRandom", "M_HostAutoKillTarget",
            "M_KillWhileVanishedHostOnly", "M_LimitFps", "M_LobbyAllColor", "M_LobbyRainbowAll",
            "M_LocalAlwaysRed", "M_LocalFakeFCEnabled", "M_LocalFortegreen", "M_LocalNameSpoof", "M_LocalSnipeColor", "M_LockRadar", "M_LogAllRPCs",
            "M_MalformedPacketGuard", "M_MoreLobbyInfo", "M_NeverEndGame", "M_NoClip",
            "M_NoMapCooldowns", "M_NoTaskMode", "M_OverflowProtection", "M_PasosLimit",
            "M_QuickChatEmptyGuard", "M_RadarBorder", "M_RadarDrawIcons", "M_RadarRightClickTp",
            "M_ReadGhostChat", "M_RemovePenalty", "M_ReplayDrawIcons", "M_ReplayOnlyLastSeconds",
            "M_RevealMeetingRoles", "M_RevealVotes", "M_RgbMenuText", "M_RgbTaskBar",
            "M_RoleBuffImmortality", "M_SeeGhosts", "M_SeeKillCooldown", "M_SeePhantoms",
            "M_SeeProtections", "M_SeeRoles", "M_ShowBodyTracers", "M_ShowCrewmateTracers",
            "M_ShowDeadTracers", "M_ShowEspBoxes", "M_ShowEspVoteKicks", "M_ShowImpostorTracers",
            "M_ShowPlayerInfo", "M_ShowRadar", "M_ShowRadarDeadBodies", "M_ShowRadarGhosts",
            "M_ShowReplay", "M_ShowTracers", "M_ShowWatermarkInfo", "M_SkipKillAnimation",
            "M_SkipRoleIntroAnim", "M_SpoofAprilDate", "M_SpoofMenuEnabled", "M_TpToCursor",
            "M_UnfixableLights", "M_UnlockCosmicubes", "M_UnlockVents", "M_UnownedSpawnGuard",
            "M_VotekickAutoRejoin", "M_VotekickCopyCode", "M_WalkInVents", "M_WhitelistOnlyLobby", "M_WhiteTheme"
        };

private static readonly string[] menuProfileIntKeys = {
            "M_AutoHostFastStartPlayers", "M_AutoHostMinPlayers", "M_AutoKickMinLevel", "M_BndCallMeeting",
            "M_BndCloseMtg", "M_BndDespawn", "M_BndEndCrew", "M_BndEndHnsDC",
            "M_BndEndImp", "M_BndEndImpDC", "M_BndFixSabotages", "M_BndInstaStart",
            "M_BndKickAll", "M_BndKillAll", "M_BndMagnet", "M_BndMMorph",
            "M_BndReviveAll", "M_BndSetAllGhost", "M_BndSetAllGhostImp", "M_BndSpawn",
            "M_BndToggleCameraZoom", "M_BndToggleFreecam", "M_BndToggleFullBright", "M_BndToggleNoClip",
            "M_BndTogglePlayerInfo", "M_BndToggleSeeGhosts", "M_BndToggleSeeRoles", "M_BndToggleTracers",
            "M_BugRoomTimedAutoRunMinutes", "M_ChatHistoryLimit", "M_CurrentAutoHostSubTab", "M_CurrentGeneralInfoSubTab",
            "M_CurrentGeneralSubTab", "M_CurrentHostOnlySubTab", "M_CurrentPlayersSubTab", "M_CurrentSabotageSubTab",
            "M_CurrentSelfSubTab", "M_CurrentTab", "M_CurrentVisualsSubTab", "M_FpsLimit",
            "M_HostAutoKillTargetId", "M_LobbyAllColorId", "M_LocalSnipeColorId", "M_MenuToggleKey",
            "M_PunishmentMode", "M_SelectedSpoofMenuIndex", "M_TargetTab"
        };

private static readonly string[] menuProfileFloatKeys = {
            "M_AutoHostAutoRunDelaySeconds", "M_AutoHostFastStartDelaySeconds", "M_AutoHostStartDelaySeconds", "M_AutoKickTimer",
            "M_BugRoomAutoAngelIntervalSeconds", "M_EngineSpeed", "M_MenuScale", "M_MenuWindowH",
            "M_MenuWindowW", "M_MenuWindowX", "M_MenuWindowY", "M_RadarAlpha",
            "M_RadarScale", "M_RadarX", "M_RadarY", "M_ReplaySeconds",
            "M_ReplayX", "M_ReplayY", "M_WalkSpeed"
        };

private static readonly string[] menuProfileStringKeys = {
            "M_CustomSpoofRpcInput", "M_DeviceId", "M_LobbyWhitelist", "M_LocalFakeFC", "M_SpoofName"
        };

private string MenuProfileDir()
        {
            string dir = Path.Combine(Plugin.ElysiumFolder, "Profiles");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return dir;
        }

private string MenuProfilePath()
        {
            return Path.Combine(MenuProfileDir(), $"MenuProfile{selectedMenuProfileIndex + 1}.txt");
        }

private void SetMenuProfileStatus(string msg)
        {
            menuProfileStatus = msg;
            menuProfileStatusUntil = Time.unscaledTime + 3f;
        }

private static string PackProfileString(string text)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(text ?? ""));
        }

private static string UnpackProfileString(string text)
        {
            try { return Encoding.UTF8.GetString(Convert.FromBase64String(text ?? "")); }
            catch { return ""; }
        }

private void WriteMenuProfileConfig(StreamWriter writer)
        {
            writer.WriteLine("cfg.SpoofedLevel=" + PackProfileString(Plugin.SpoofedLevel.Value));
            writer.WriteLine("cfg.EnableLevelSpoofConfig=" + Plugin.EnableLevelSpoofConfig.Value);
            writer.WriteLine("cfg.EnableFriendCodeSpoofConfig=" + Plugin.EnableFriendCodeSpoofConfig.Value);
            writer.WriteLine("cfg.SpoofFriendCodeConfig=" + PackProfileString(Plugin.SpoofFriendCodeConfig.Value));
            writer.WriteLine("cfg.EnablePlatformSpoof=" + Plugin.EnablePlatformSpoof.Value);
            writer.WriteLine("cfg.AutoBanBrokenFriendCodeConfig=" + Plugin.AutoBanBrokenFriendCodeConfig.Value);
            writer.WriteLine("cfg.PlatformIndex=" + Plugin.PlatformIndex.Value);
            writer.WriteLine("cfg.ShowWatermarkConfig=" + Plugin.ShowWatermarkConfig.Value);
            writer.WriteLine("cfg.MenuColorIndexConfig=" + Plugin.MenuColorIndexConfig.Value);
            writer.WriteLine("cfg.RgbMenuModeConfig=" + Plugin.RgbMenuModeConfig.Value);
            writer.WriteLine("cfg.RgbMenuTextConfig=" + Plugin.RgbMenuTextConfig.Value);
            writer.WriteLine("cfg.BoldMenuTextConfig=" + Plugin.BoldMenuTextConfig.Value);
            writer.WriteLine("cfg.UnlockCosmeticsConfig=" + Plugin.UnlockCosmeticsConfig.Value);
            writer.WriteLine("cfg.MoreLobbyInfoConfig=" + Plugin.MoreLobbyInfoConfig.Value);
            writer.WriteLine("cfg.EnableChatDarkModeConfig=" + Plugin.EnableChatDarkModeConfig.Value);
            writer.WriteLine("cfg.GhostChatColorConfig=" + PackProfileString(Plugin.GhostChatColorConfig.Value));
            writer.WriteLine("cfg.ThrottleDefaultLogsConfig=" + Plugin.ThrottleDefaultLogsConfig.Value);
            writer.WriteLine("cfg.DetailedLogsEnabledConfig=" + Plugin.DetailedLogsEnabledConfig.Value);
            writer.WriteLine("cfg.ShowEspFriendCodeConfig=" + Plugin.ShowEspFriendCodeConfig.Value);
            writer.WriteLine("cfg.RpcSpoofDelayConfig=" + Plugin.RpcSpoofDelayConfig.Value.ToString(CultureInfo.InvariantCulture));
            writer.WriteLine("cfg.MenuKeybind=" + (int)Plugin.MenuKeybind.Value);
        }

private void SaveMenuProfile()
        {
            try
            {
                SaveConfig();

                using (var writer = new StreamWriter(MenuProfilePath(), false, Encoding.UTF8))
                {
                    writer.WriteLine("# ElysiumMenuProfile");
                    writer.WriteLine("# Format: type.key=value");
                    writer.WriteLine("# Strings are base64 encoded.");
                    writer.WriteLine();

                    WriteMenuProfileConfig(writer);

                    foreach (string key in menuProfileBoolKeys)
                        if (PlayerPrefs.HasKey(key)) writer.WriteLine("bool." + key + "=" + (PlayerPrefs.GetInt(key) == 1));

                    foreach (string key in menuProfileIntKeys)
                        if (PlayerPrefs.HasKey(key)) writer.WriteLine("int." + key + "=" + PlayerPrefs.GetInt(key));

                    foreach (string key in menuProfileFloatKeys)
                        if (PlayerPrefs.HasKey(key)) writer.WriteLine("float." + key + "=" + PlayerPrefs.GetFloat(key).ToString(CultureInfo.InvariantCulture));

                    foreach (string key in menuProfileStringKeys)
                        if (PlayerPrefs.HasKey(key)) writer.WriteLine("str." + key + "=" + PackProfileString(PlayerPrefs.GetString(key, "")));

                    for (int i = 0; i < favoriteOutfitSlots.Length; i++)
                    {
                        string key = $"M_FavoriteOutfit_{i}";
                        writer.WriteLine("str." + key + "=" + PackProfileString(PlayerPrefs.GetString(key, "")));
                    }
                }

                SetMenuProfileStatus($"Saved Profile {selectedMenuProfileIndex + 1}");
                ShowNotification($"<color=#00FFAA>[PROFILE]</color> Saved Profile {selectedMenuProfileIndex + 1}");
            }
            catch (Exception ex)
            {
                SetMenuProfileStatus("Save failed");
                ShowNotification($"<color=#FF4444>[PROFILE]</color> Save failed: {ex.GetType().Name}");
            }
        }

private void ApplyMenuProfileConfig(string key, string value)
        {
            if (key == "SpoofedLevel") Plugin.SpoofedLevel.Value = UnpackProfileString(value);
            else if (key == "EnableLevelSpoofConfig" && bool.TryParse(value, out bool enableLevel)) Plugin.EnableLevelSpoofConfig.Value = enableLevel;
            else if (key == "EnableFriendCodeSpoofConfig" && bool.TryParse(value, out bool enableFc)) Plugin.EnableFriendCodeSpoofConfig.Value = enableFc;
            else if (key == "SpoofFriendCodeConfig") Plugin.SpoofFriendCodeConfig.Value = UnpackProfileString(value);
            else if (key == "EnablePlatformSpoof" && bool.TryParse(value, out bool enablePlatform)) Plugin.EnablePlatformSpoof.Value = enablePlatform;
            else if (key == "AutoBanBrokenFriendCodeConfig" && bool.TryParse(value, out bool autoBanFc)) Plugin.AutoBanBrokenFriendCodeConfig.Value = autoBanFc;
            else if (key == "PlatformIndex" && int.TryParse(value, out int platformIndex)) Plugin.PlatformIndex.Value = platformIndex;
            else if (key == "ShowWatermarkConfig" && bool.TryParse(value, out bool watermark)) Plugin.ShowWatermarkConfig.Value = watermark;
            else if (key == "MenuColorIndexConfig" && int.TryParse(value, out int menuColor)) Plugin.MenuColorIndexConfig.Value = menuColor;
            else if (key == "RgbMenuModeConfig" && bool.TryParse(value, out bool rgbMode)) Plugin.RgbMenuModeConfig.Value = rgbMode;
            else if (key == "RgbMenuTextConfig" && bool.TryParse(value, out bool rgbText)) Plugin.RgbMenuTextConfig.Value = rgbText;
            else if (key == "BoldMenuTextConfig" && bool.TryParse(value, out bool boldText)) Plugin.BoldMenuTextConfig.Value = boldText;
            else if (key == "UnlockCosmeticsConfig" && bool.TryParse(value, out bool unlock)) Plugin.UnlockCosmeticsConfig.Value = unlock;
            else if (key == "MoreLobbyInfoConfig" && bool.TryParse(value, out bool lobbyInfo)) Plugin.MoreLobbyInfoConfig.Value = lobbyInfo;
            else if (key == "EnableChatDarkModeConfig" && bool.TryParse(value, out bool darkChat)) Plugin.EnableChatDarkModeConfig.Value = darkChat;
            else if (key == "GhostChatColorConfig") Plugin.GhostChatColorConfig.Value = UnpackProfileString(value);
            else if (key == "ThrottleDefaultLogsConfig" && bool.TryParse(value, out bool throttle)) Plugin.ThrottleDefaultLogsConfig.Value = throttle;
            else if (key == "DetailedLogsEnabledConfig" && bool.TryParse(value, out bool detailed)) Plugin.DetailedLogsEnabledConfig.Value = detailed;
            else if (key == "ShowEspFriendCodeConfig" && bool.TryParse(value, out bool espFc)) Plugin.ShowEspFriendCodeConfig.Value = espFc;
            else if (key == "RpcSpoofDelayConfig" && float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float rpcDelay)) Plugin.RpcSpoofDelayConfig.Value = rpcDelay;
            else if (key == "MenuKeybind" && int.TryParse(value, out int menuKey)) Plugin.MenuKeybind.Value = (KeyCode)menuKey;
        }

private bool LoadMenuProfile()
        {
            string path = MenuProfilePath();
            if (!File.Exists(path))
            {
                SetMenuProfileStatus("Profile file not found");
                ShowNotification($"<color=#FF4444>[PROFILE]</color> Profile {selectedMenuProfileIndex + 1} not found.");
                return false;
            }

            try
            {
                foreach (string raw in File.ReadAllLines(path, Encoding.UTF8))
                {
                    string line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith("#")) continue;

                    int idx = line.IndexOf('=');
                    if (idx <= 0) continue;

                    string key = line.Substring(0, idx).Trim();
                    string value = line.Substring(idx + 1).Trim();

                    if (key.StartsWith("bool."))
                    {
                        if (bool.TryParse(value, out bool v)) PlayerPrefs.SetInt(key.Substring(5), v ? 1 : 0);
                    }
                    else if (key.StartsWith("int."))
                    {
                        string prefKey = key.Substring(4);
                        if (prefKey != "M_MenuLanguageIndex" && int.TryParse(value, out int v)) PlayerPrefs.SetInt(prefKey, v);
                    }
                    else if (key.StartsWith("float."))
                    {
                        if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float v)) PlayerPrefs.SetFloat(key.Substring(6), v);
                    }
                    else if (key.StartsWith("str."))
                    {
                        PlayerPrefs.SetString(key.Substring(4), UnpackProfileString(value));
                    }
                    else if (key.StartsWith("cfg."))
                    {
                        ApplyMenuProfileConfig(key.Substring(4), value);
                    }
                }

                Plugin.MenuConfig.Save();
                PlayerPrefs.Save();
                LoadConfig();
                ApplyFpsLimit();
                if (!rgbMenuMode && currentMenuColorIndex >= 0 && currentMenuColorIndex < menuColors.Length)
                    UpdateAccentColor(menuColors[currentMenuColorIndex]);
                SaveConfig();
                SetMenuProfileStatus($"Loaded Profile {selectedMenuProfileIndex + 1}");
                ShowNotification($"<color=#00FFAA>[PROFILE]</color> Loaded Profile {selectedMenuProfileIndex + 1}");
                return true;
            }
            catch (Exception ex)
            {
                SetMenuProfileStatus("Load failed");
                ShowNotification($"<color=#FF4444>[PROFILE]</color> Load failed: {ex.GetType().Name}");
                return false;
            }
        }
    }
}
