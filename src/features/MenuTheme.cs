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

private void LoadConfig()
        {
            try
            {
                spoofLevelString = Plugin.SpoofedLevel.Value;
                enableLevelSpoof = LoadBool("M_EnableLevelSpoof", Plugin.EnableLevelSpoofConfig.Value);
                enableFriendCodeSpoof = Plugin.EnableFriendCodeSpoofConfig.Value;
                spoofFriendCodeInput = Plugin.SpoofFriendCodeConfig.Value;
                enablePlatformSpoof = Plugin.EnablePlatformSpoof.Value;
                autoBanBrokenFriendCode = Plugin.AutoBanBrokenFriendCodeConfig.Value;
                currentPlatformIndex = Mathf.Clamp(Plugin.PlatformIndex.Value, 0, platformValues.Length - 1);
                showWatermark = Plugin.ShowWatermarkConfig.Value;
                unlockCosmetics = Plugin.UnlockCosmeticsConfig.Value;
                moreLobbyInfo = Plugin.MoreLobbyInfoConfig.Value;
                enableChatDarkMode = Plugin.EnableChatDarkModeConfig.Value;
                ghostChatColorHex = SanitizeHexColor(Plugin.GhostChatColorConfig.Value, "#D7B8FF");
                enableAnomalyLogReports = Plugin.EnableAnomalyLogReportsConfig.Value;
                throttleDefaultLogs = Plugin.ThrottleDefaultLogsConfig.Value;
                showEspFriendCode = Plugin.ShowEspFriendCodeConfig.Value;
                rpcSpoofDelay = Plugin.RpcSpoofDelayConfig.Value;
                currentMenuColorIndex = Plugin.MenuColorIndexConfig.Value;
                rgbMenuMode = Plugin.RgbMenuModeConfig.Value;
                rgbMenuText = LoadBool("M_RgbMenuText", Plugin.RgbMenuTextConfig.Value);
                boldMenuText = LoadBool("M_BoldMenuText", Plugin.BoldMenuTextConfig.Value);
                whiteMenuTheme = LoadBool("M_WhiteTheme", whiteMenuTheme);
                currentMenuLanguageIndex = Mathf.Clamp(LoadInt("M_MenuLanguageIndex", currentMenuLanguageIndex), 0, menuLanguageNames.Length - 1);
                fpsLimit = Mathf.Clamp(LoadInt("M_FpsLimit", fpsLimit), 60, 240);
                autoCopyCodeAndLeave = LoadBool("M_AutoCopyCodeAndLeave", autoCopyCodeAndLeave);
                ApplyFpsLimit();
                autoKickBugs = LoadBool("M_AutoKickBugs", autoKickBugs);
                if (PlayerPrefs.HasKey("M_AutoKickTimer")) autoKickTimer = PlayerPrefs.GetFloat("M_AutoKickTimer");
                disableVoteKicks = LoadBool("M_DisableVoteKicks", disableVoteKicks);
                enableLocalNameSpoof = LoadBool("M_LocalNameSpoof", enableLocalNameSpoof);
                enableLocalFriendCodeSpoof = LoadBool("M_LocalFakeFCEnabled", enableLocalFriendCodeSpoof);
                if (PlayerPrefs.HasKey("M_LocalFakeFC")) localFriendCodeInput = PlayerPrefs.GetString("M_LocalFakeFC");
                if (PlayerPrefs.HasKey("M_BndMagnet")) bindMagnetCursor = (KeyCode)PlayerPrefs.GetInt("M_BndMagnet");
                menuToggleKey = Plugin.MenuKeybind.Value == KeyCode.None ? KeyCode.Insert : Plugin.MenuKeybind.Value;
                if (PlayerPrefs.HasKey("M_MenuToggleKey")) menuToggleKey = (KeyCode)PlayerPrefs.GetInt("M_MenuToggleKey");
                if (menuToggleKey == KeyCode.None) menuToggleKey = KeyCode.Insert;
                if (PlayerPrefs.HasKey("M_BndMMorph")) bindMassMorph = (KeyCode)PlayerPrefs.GetInt("M_BndMMorph");
                if (PlayerPrefs.HasKey("M_BndSpawn")) bindSpawnLobby = (KeyCode)PlayerPrefs.GetInt("M_BndSpawn");
                if (PlayerPrefs.HasKey("M_BndDespawn")) bindDespawnLobby = (KeyCode)PlayerPrefs.GetInt("M_BndDespawn");
                if (PlayerPrefs.HasKey("M_BndCloseMtg")) bindCloseMeeting = (KeyCode)PlayerPrefs.GetInt("M_BndCloseMtg");
                if (PlayerPrefs.HasKey("M_BndInstaStart")) bindInstaStart = (KeyCode)PlayerPrefs.GetInt("M_BndInstaStart");
                if (PlayerPrefs.HasKey("M_BndEndCrew")) bindEndCrew = (KeyCode)PlayerPrefs.GetInt("M_BndEndCrew");
                if (PlayerPrefs.HasKey("M_BndEndImp")) bindEndImp = (KeyCode)PlayerPrefs.GetInt("M_BndEndImp");
                if (PlayerPrefs.HasKey("M_BndEndImpDC")) bindEndImpDC = (KeyCode)PlayerPrefs.GetInt("M_BndEndImpDC");
                if (PlayerPrefs.HasKey("M_BndEndHnsDC")) bindEndHnsDC = (KeyCode)PlayerPrefs.GetInt("M_BndEndHnsDC");
                if (PlayerPrefs.HasKey("M_BndToggleTracers")) bindToggleTracers = (KeyCode)PlayerPrefs.GetInt("M_BndToggleTracers");
                if (PlayerPrefs.HasKey("M_BndToggleNoClip")) bindToggleNoClip = (KeyCode)PlayerPrefs.GetInt("M_BndToggleNoClip");
                if (PlayerPrefs.HasKey("M_BndToggleFreecam")) bindToggleFreecam = (KeyCode)PlayerPrefs.GetInt("M_BndToggleFreecam");
                if (PlayerPrefs.HasKey("M_BndToggleCameraZoom")) bindToggleCameraZoom = (KeyCode)PlayerPrefs.GetInt("M_BndToggleCameraZoom");
                if (PlayerPrefs.HasKey("M_BndKillAll")) bindKillAll = (KeyCode)PlayerPrefs.GetInt("M_BndKillAll");
                if (PlayerPrefs.HasKey("M_BndCallMeeting")) bindCallMeeting = (KeyCode)PlayerPrefs.GetInt("M_BndCallMeeting");
                if (PlayerPrefs.HasKey("M_BndTogglePlayerInfo")) bindTogglePlayerInfo = (KeyCode)PlayerPrefs.GetInt("M_BndTogglePlayerInfo");
                if (PlayerPrefs.HasKey("M_BndToggleSeeRoles")) bindToggleSeeRoles = (KeyCode)PlayerPrefs.GetInt("M_BndToggleSeeRoles");
                if (PlayerPrefs.HasKey("M_BndToggleSeeGhosts")) bindToggleSeeGhosts = (KeyCode)PlayerPrefs.GetInt("M_BndToggleSeeGhosts");
                if (PlayerPrefs.HasKey("M_BndToggleFullBright")) bindToggleFullBright = (KeyCode)PlayerPrefs.GetInt("M_BndToggleFullBright");
                if (PlayerPrefs.HasKey("M_BndKickAll")) bindKickAll = (KeyCode)PlayerPrefs.GetInt("M_BndKickAll");
                if (PlayerPrefs.HasKey("M_BndFixSabotages")) bindFixSabotages = (KeyCode)PlayerPrefs.GetInt("M_BndFixSabotages");
                if (PlayerPrefs.HasKey("M_BndSetAllGhost")) bindSetAllGhost = (KeyCode)PlayerPrefs.GetInt("M_BndSetAllGhost");
                if (PlayerPrefs.HasKey("M_BndSetAllGhostImp")) bindSetAllGhostImp = (KeyCode)PlayerPrefs.GetInt("M_BndSetAllGhostImp");
                if (PlayerPrefs.HasKey("M_BndReviveAll")) bindReviveAll = (KeyCode)PlayerPrefs.GetInt("M_BndReviveAll");

                if (!rgbMenuMode && currentMenuColorIndex >= 0 && currentMenuColorIndex < menuColors.Length)
                {
                    currentAccentColor = menuColors[currentMenuColorIndex];
                }

                showPlayerInfo = LoadBool("M_ShowPlayerInfo", showPlayerInfo);
                seeGhosts = LoadBool("M_SeeGhosts", seeGhosts);
                seeRoles = LoadBool("M_SeeRoles", seeRoles);
                revealMeetingRoles = LoadBool("M_RevealMeetingRoles", revealMeetingRoles);
                showTracers = LoadBool("M_ShowTracers", showTracers);
                fullBright = LoadBool("M_FullBright", fullBright);
                seeProtections = LoadBool("M_SeeProtections", seeProtections);
                seeKillCooldown = LoadBool("M_SeeKillCooldown", seeKillCooldown);
                extendedLobby = LoadBool("M_ExtendedLobby", extendedLobby);
                moreLobbyInfo = LoadBool("M_MoreLobbyInfo", moreLobbyInfo);
                alwaysChat = LoadBool("M_AlwaysChat", alwaysChat);
                lobbyRainbowAll = LoadBool("M_LobbyRainbowAll", lobbyRainbowAll);
                lobbyAllColor = LoadBool("M_LobbyAllColor", lobbyAllColor);
                lobbyAllColorId = Mathf.Clamp(LoadInt("M_LobbyAllColorId", lobbyAllColorId), 0, MaxOutfitColorId());
                readGhostChat = LoadBool("M_ReadGhostChat", readGhostChat);
                enableExtendedChat = LoadBool("M_EnableExtendedChat", enableExtendedChat);
                enableFastChat = LoadBool("M_EnableFastChat", enableFastChat);
                allowLinksAndSymbols = LoadBool("M_AllowLinksAndSymbols", allowLinksAndSymbols);
                enableChatHistory = LoadBool("M_EnableChatHistory", enableChatHistory);
                chatHistoryLimit = Mathf.Clamp(LoadInt("M_ChatHistoryLimit", chatHistoryLimit), 5, 80);
                enableClipboard = LoadBool("M_EnableClipboard", enableClipboard);
                enableChatMessageDoubleClickCopy = LoadBool("M_EnableChatMessageDoubleClickCopy", enableChatMessageDoubleClickCopy);
                enableChatNameColorCopy = LoadBool("M_EnableChatNameColorCopy", enableChatNameColorCopy);
                enableChatLog = LoadBool("M_EnableChatLog", enableChatLog);
                enableColorCommand = LoadBool("M_EnableColorCommand", enableColorCommand);
                blockRainbowChat = LoadBool("M_BlockRainbowChat", blockRainbowChat);
                blockFortegreenChat = LoadBool("M_BlockFortegreenChat", blockFortegreenChat);
                SpoofMenuEnabled = LoadBool("M_SpoofMenuEnabled", SpoofMenuEnabled);
                noClip = LoadBool("M_NoClip", noClip);
                tpToCursor = LoadBool("M_TpToCursor", tpToCursor);
                dragToCursor = LoadBool("M_DragToCursor", dragToCursor);
                autoFollowCursor = LoadBool("M_AutoFollowCursor", autoFollowCursor);
                freecam = LoadBool("M_Freecam", freecam);
                cameraZoom = LoadBool("M_CameraZoom", cameraZoom);
                RevealVotesEnabled = LoadBool("M_RevealVotes", RevealVotesEnabled);
                noTaskMode = LoadBool("M_NoTaskMode", noTaskMode);
                noMapCooldowns = LoadBool("M_NoMapCooldowns", noMapCooldowns);
                unlockVents = LoadBool("M_UnlockVents", unlockVents);
                walkInVents = LoadBool("M_WalkInVents", walkInVents);
                allowTasksAsImpostor = LoadBool("M_AllowTasksAsImpostor", allowTasksAsImpostor);
                killWhileVanishedHostOnly = LoadBool("M_KillWhileVanishedHostOnly", killWhileVanishedHostOnly);
                roleBuffImmortality = LoadBool("M_RoleBuffImmortality", roleBuffImmortality);
                neverEndGame = LoadBool("M_NeverEndGame", neverEndGame);
                removePenalty = LoadBool("M_RemovePenalty", removePenalty);
                alwaysShowLobbyTimer = LoadBool("M_AlwaysShowLobbyTimer", alwaysShowLobbyTimer);
                autoBanEnabled = LoadBool("M_AutoBanEnabled", autoBanEnabled);
                allowDuplicateColors = LoadBool("M_AllowDuplicateColors", allowDuplicateColors);
                blockSpoofRPC = LoadBool("M_BlockSpoofRPC", blockSpoofRPC);
                autoBanPlatformSpoof = LoadBool("M_AutoBanPlatformSpoof", autoBanPlatformSpoof);
                banCustomPlatformsFromTxt = LoadBool("M_BanCustomPlatformsFromTxt", banCustomPlatformsFromTxt);
                autoKickLowLevelEnabled = LoadBool("M_AutoKickLowLevel", autoKickLowLevelEnabled);
                autoKickMinLevel = Mathf.Clamp(LoadInt("M_AutoKickMinLevel", autoKickMinLevel), 1, 300);
                blockSabotageRPC = LoadBool("M_BlockSabotageRPC", blockSabotageRPC);
                punishmentMode = Mathf.Clamp(LoadInt("M_PunishmentMode", punishmentMode), 0, punishmentNames.Length - 1);
                blockGameRpcInLobby = LoadBool("M_BlockGameRpcInLobby", blockGameRpcInLobby);
                blockChatFloodRpc = LoadBool("M_BlockChatFloodRpc", blockChatFloodRpc);
                blockMeetingFloodRpc = LoadBool("M_BlockMeetingFloodRpc", blockMeetingFloodRpc);
                unfixableLights = LoadBool("M_UnfixableLights", unfixableLights);
                enablePasosLimit = LoadBool("M_PasosLimit", enablePasosLimit);
                enableLocalPasosBan = LoadBool("M_AntiPasosLocalBan", enableLocalPasosBan);
                enableHostPasosBan = LoadBool("M_AntiPasosHostBan", enableHostPasosBan);
                enableMalformedPacketGuard = LoadBool("M_MalformedPacketGuard", enableMalformedPacketGuard);
                banMalformedPacketSender = LoadBool("M_BanMalformedPacketSender", banMalformedPacketSender);
                enableQuickChatEmptyGuard = LoadBool("M_QuickChatEmptyGuard", enableQuickChatEmptyGuard);
                banQuickChatEmptySpammer = LoadBool("M_BanQuickChatEmptySpammer", banQuickChatEmptySpammer);
                enableUnownedSpawnGuard = LoadBool("M_UnownedSpawnGuard", enableUnownedSpawnGuard);
                AutoHostEnabled = LoadBool("M_AutoHostEnabled", AutoHostEnabled);
                AutoReturnLobbyAfterMatch = LoadBool("M_AutoReturnLobbyAfterMatch", AutoReturnLobbyAfterMatch);
                AutoHostNotifications = LoadBool("M_AutoHostNotifications", AutoHostNotifications);
                AutoHostForceLastMinute = LoadBool("M_AutoHostForceLastMinute", AutoHostForceLastMinute);
                AutoHostWaitLoadedPlayers = LoadBool("M_AutoHostWaitLoadedPlayers", AutoHostWaitLoadedPlayers);
                AutoHostCancelBelowMin = LoadBool("M_AutoHostCancelBelowMin", AutoHostCancelBelowMin);
                AutoHostInstantStart = LoadBool("M_AutoHostInstantStart", AutoHostInstantStart);
                autoGhostAfterStart = LoadBool("M_AutoGhostAfterStart", autoGhostAfterStart);
                if (PlayerPrefs.HasKey("M_AutoHostMinPlayers")) AutoHostMinPlayers = PlayerPrefs.GetInt("M_AutoHostMinPlayers");
                if (PlayerPrefs.HasKey("M_AutoHostStartDelaySeconds")) AutoHostStartDelaySeconds = PlayerPrefs.GetFloat("M_AutoHostStartDelaySeconds");
                if (PlayerPrefs.HasKey("M_AutoHostFastStartPlayers")) AutoHostFastStartPlayers = PlayerPrefs.GetInt("M_AutoHostFastStartPlayers");
                if (PlayerPrefs.HasKey("M_AutoHostFastStartDelaySeconds")) AutoHostFastStartDelaySeconds = PlayerPrefs.GetFloat("M_AutoHostFastStartDelaySeconds");
                if (PlayerPrefs.HasKey("M_WalkSpeed")) walkSpeed = PlayerPrefs.GetFloat("M_WalkSpeed");
                if (PlayerPrefs.HasKey("M_EngineSpeed")) engineSpeed = PlayerPrefs.GetFloat("M_EngineSpeed");
                for (int i = 0; i < favoriteOutfitSlots.Length; i++)
                    favoriteOutfitSlots[i] = PlayerPrefs.GetString($"M_FavoriteOutfit_{i}", string.Empty);
                enableBackground = LoadBool("M_EnableBackground", enableBackground);
                hardMenu = LoadBool("M_HardMenu", hardMenu);
                EnableCustomNotifs = LoadBool("M_EnableCustomNotifs", EnableCustomNotifs);
                LogAllRPCs = LoadBool("M_LogAllRPCs", LogAllRPCs);
                selectedSpoofMenuIndex = Mathf.Clamp(LoadInt("M_SelectedSpoofMenuIndex", selectedSpoofMenuIndex), 0, spoofMenuNames.Length - 1);
                windowRect = new Rect(
                    LoadFloat("M_MenuWindowX", windowRect.x),
                    LoadFloat("M_MenuWindowY", windowRect.y),
                    Mathf.Clamp(LoadFloat("M_MenuWindowW", windowRect.width), 640f, 1400f),
                    Mathf.Clamp(LoadFloat("M_MenuWindowH", windowRect.height), 420f, 900f));
                currentTab = Mathf.Clamp(LoadInt("M_CurrentTab", currentTab), 0, tabNames.Length - 1);
                targetTabIndex = Mathf.Clamp(LoadInt("M_TargetTab", currentTab), 0, tabNames.Length - 1);
                currentGeneralSubTab = Mathf.Clamp(LoadInt("M_CurrentGeneralSubTab", currentGeneralSubTab), 0, generalSubTabs.Length - 1);
                currentGeneralInfoSubTab = Mathf.Clamp(LoadInt("M_CurrentGeneralInfoSubTab", currentGeneralInfoSubTab), 0, generalInfoSubTabs.Length - 1);
                currentSelfSubTab = Mathf.Clamp(LoadInt("M_CurrentSelfSubTab", currentSelfSubTab), 0, selfSubTabs.Length);
                currentVisualsSubTab = Mathf.Clamp(LoadInt("M_CurrentVisualsSubTab", currentVisualsSubTab), 0, visualsSubTabs.Length - 1);
                currentPlayersSubTab = Mathf.Clamp(LoadInt("M_CurrentPlayersSubTab", currentPlayersSubTab), 0, playersSubTabs.Length - 1);
                currentHostOnlySubTab = Mathf.Clamp(LoadInt("M_CurrentHostOnlySubTab", currentHostOnlySubTab), 0, hostOnlySubTabs.Length - 1);
                currentAutoHostSubTab = Mathf.Clamp(LoadInt("M_CurrentAutoHostSubTab", currentAutoHostSubTab), 0, autoHostSubTabs.Length - 1);
                tabTransitionProgress = 1f;
                SyncKeybindDictionary();
                if (PlayerPrefs.HasKey("M_SpoofName")) customNameInput = PlayerPrefs.GetString("M_SpoofName");
            }
            catch { }
        }

private static void ApplyFpsLimit()
        {
            try
            {
                fpsLimit = Mathf.Clamp(fpsLimit, 60, 240);
                if (lastAppliedFpsLimit == fpsLimit) return;
                Application.targetFrameRate = fpsLimit;
                QualitySettings.vSyncCount = 0;
                lastAppliedFpsLimit = fpsLimit;
            }
            catch { }
        }

private static void TrimChatHistoryToLimit()
        {
            try
            {
                chatHistoryLimit = Mathf.Clamp(chatHistoryLimit, 5, 80);
                while (ChatHistory.sentMessages.Count > chatHistoryLimit)
                    ChatHistory.sentMessages.RemoveAt(0);

                ChatHistory.HistoryIndex = Mathf.Clamp(ChatHistory.HistoryIndex, 0, ChatHistory.sentMessages.Count);
            }
            catch { }
        }

private static void SyncKeybindDictionary()
        {
            try
            {
                keyBinds["Toggle Menu"] = menuToggleKey;
                keyBinds["Magnet Cursor"] = bindMagnetCursor;
                keyBinds["Mass Morph"] = bindMassMorph;
                keyBinds["Spawn Lobby"] = bindSpawnLobby;
                keyBinds["Despawn Lobby"] = bindDespawnLobby;
                keyBinds["Close Meeting"] = bindCloseMeeting;
                keyBinds["Insta Start"] = bindInstaStart;
                keyBinds["End Crew"] = bindEndCrew;
                keyBinds["End Imp"] = bindEndImp;
                keyBinds["End Imp DC"] = bindEndImpDC;
                keyBinds["End H&S DC"] = bindEndHnsDC;
                keyBinds["Toggle Tracers"] = bindToggleTracers;
                keyBinds["Toggle NoClip"] = bindToggleNoClip;
                keyBinds["Toggle Freecam"] = bindToggleFreecam;
                keyBinds["Toggle Camera Zoom"] = bindToggleCameraZoom;
                keyBinds["Toggle Player Info"] = bindTogglePlayerInfo;
                keyBinds["Toggle See Roles"] = bindToggleSeeRoles;
                keyBinds["Toggle See Ghosts"] = bindToggleSeeGhosts;
                keyBinds["Toggle Full Bright"] = bindToggleFullBright;
                keyBinds["Kill All"] = bindKillAll;
                keyBinds["Call Meeting"] = bindCallMeeting;
                keyBinds["Kick All"] = bindKickAll;
                keyBinds["Fix Sabotages"] = bindFixSabotages;
                keyBinds["All Ghost"] = bindSetAllGhost;
                keyBinds["All Ghost Imp"] = bindSetAllGhostImp;
                keyBinds["Revive All"] = bindReviveAll;
            }
            catch { }
        }

private Texture2D MakeRoundedTex(int size, Color col, float radius)
        {
            Texture2D result = new Texture2D(size, size, TextureFormat.RGBA32, false);
            result.hideFlags = HideFlags.HideAndDontSave;
            Color[] pix = new Color[size * size];
            float center = size / 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(0, Mathf.Abs(x - center + 0.5f) - (center - radius));
                    float dy = Mathf.Max(0, Mathf.Abs(y - center + 0.5f) - (center - radius));
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(radius - dist + 0.5f);
                    Color c = col;
                    c.a = col.a * alpha;
                    pix[y * size + x] = c;
                }
            }
            result.SetPixels(pix); result.Apply();
            return result;
        }

private RectOffset CreateRectOffset(int left, int right, int top, int bottom)
        {
            return new RectOffset { left = left, right = right, top = top, bottom = bottom };
        }

private void UpdateSwitchTex(Texture2D tex, bool isOn, Color accentColor)
        {
            int width = tex.width; int height = tex.height;
            Color transparent = new Color(0, 0, 0, 0);
            Color offBg = new Color(0.23f, 0.23f, 0.23f, 1f);
            Color offKnob = new Color(0.6f, 0.6f, 0.6f, 1f);
            Color bgColor = isOn ? accentColor : offBg;
            Color knobColor = isOn ? Color.white : offKnob;
            float r = height / 2f;
            float cx1 = r; float cx2 = width - r; float cy = r;
            float knobRadius = r - 2f;
            float knobCx = isOn ? cx2 : cx1;
            Color[] pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dLeft = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx1, cy));
                    float dRight = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx2, cy));
                    float dRect = (x + 0.5f >= cx1 && x + 0.5f <= cx2) ? Mathf.Abs((y + 0.5f) - cy) : 9999f;
                    float distBg = Mathf.Min(dLeft, Mathf.Min(dRight, dRect));
                    float alphaBg = Mathf.Clamp01(r - distBg + 0.5f);
                    float distKnob = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(knobCx, cy));
                    float alphaKnob = Mathf.Clamp01(knobRadius - distKnob + 0.5f);
                    if (alphaBg > 0)
                    {
                        Color finalCol = Color.Lerp(bgColor, knobColor, alphaKnob);
                        finalCol.a = alphaBg;
                        pixels[y * width + x] = finalCol;
                    }
                    else pixels[y * width + x] = transparent;
                }
            }
            tex.SetPixels(pixels); tex.Apply();
        }

private void UpdateTrackTex(Texture2D tex, bool isOn, Color accentColor)
        {
            int width = tex.width; int height = tex.height;
            Color transparent = new Color(0, 0, 0, 0);
            Color offBg = new Color(0.23f, 0.23f, 0.23f, 1f);
            Color bgColor = isOn ? accentColor : offBg;
            float r = height / 2f;
            float cx1 = r; float cx2 = width - r; float cy = r;
            Color[] pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dLeft = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx1, cy));
                    float dRight = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx2, cy));
                    float dRect = (x + 0.5f >= cx1 && x + 0.5f <= cx2) ? Mathf.Abs((y + 0.5f) - cy) : 9999f;
                    float distBg = Mathf.Min(dLeft, Mathf.Min(dRight, dRect));
                    float alphaBg = Mathf.Clamp01(r - distBg + 0.5f);
                    if (alphaBg > 0)
                    {
                        Color finalCol = bgColor;
                        finalCol.a = alphaBg;
                        pixels[y * width + x] = finalCol;
                    }
                    else pixels[y * width + x] = transparent;
                }
            }
            tex.SetPixels(pixels); tex.Apply();
        }

private static Color GetThemeAccentColor(Color source)
        {
            if (!whiteMenuTheme) return source;

            Color.RGBToHSV(source, out float h, out float s, out float v);

            if (s < 0.08f)
                return new Color(0.34f, 0.34f, 0.34f, 1f);

            if (rgbMenuMode)
            {
                float rgbS = Mathf.Clamp(Mathf.Max(s, 0.62f), 0.62f, 0.9f);
                float rgbV = Mathf.Clamp(v * 0.58f, 0.34f, 0.68f);
                Color rgbMapped = Color.HSVToRGB(h, rgbS, rgbV);
                rgbMapped.a = 1f;
                return rgbMapped;
            }

            if (h <= 0.04f || h >= 0.96f)
                return new Color(0.50f, 0.14f, 0.18f, 1f);

            if (h >= 0.11f && h <= 0.19f)
                return new Color32(232, 194, 37, 255);

            float targetS = Mathf.Clamp(Mathf.Max(s, 0.55f), 0.55f, 0.95f);
            float targetV = Mathf.Clamp(v * 0.62f, 0.26f, 0.72f);
            Color mapped = Color.HSVToRGB(h, targetS, targetV);
            mapped.a = 1f;
            return mapped;
        }

private static bool RgbMenuTextActive()
        {
            return rgbMenuMode && rgbMenuText;
        }

private static Color GetStableMenuAccentSource()
        {
            try
            {
                if (activeGui != null && activeGui.menuColors != null && activeGui.menuColors.Length > 0)
                    return activeGui.menuColors[Mathf.Clamp(activeGui.currentMenuColorIndex, 0, activeGui.menuColors.Length - 1)];
            }
            catch { }

            return currentAccentColor;
        }
}
}
