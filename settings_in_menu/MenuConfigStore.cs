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

private static void LoadKeybinds()
        {
            try
            {
                menuToggleKey = Plugin.MenuKeybind.Value == KeyCode.None ? KeyCode.Insert : Plugin.MenuKeybind.Value;
            }
            catch
            {
                menuToggleKey = KeyCode.Insert;
            }

            try
            {
                if (PlayerPrefs.HasKey("M_MenuToggleKey")) menuToggleKey = (KeyCode)PlayerPrefs.GetInt("M_MenuToggleKey");
                if (menuToggleKey == KeyCode.None) menuToggleKey = KeyCode.Insert;
                if (PlayerPrefs.HasKey("M_BndMagnet")) bindMagnetCursor = (KeyCode)PlayerPrefs.GetInt("M_BndMagnet");
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
                SyncKeybindDictionary();
            }
            catch { }
        }

private void LoadConfig()
        {
            LoadKeybinds();
            try
            {
                spoofLevelString = Plugin.SpoofedLevel.Value;
                enableLevelSpoof = uint.TryParse(spoofLevelString, out uint spoofLevel) && spoofLevel > 0;
                enableFriendCodeSpoof = Plugin.EnableFriendCodeSpoofConfig.Value;
                spoofFriendCodeInput = Plugin.SpoofFriendCodeConfig.Value;
                enablePlatformSpoof = Plugin.EnablePlatformSpoof.Value;
                autoBanBrokenFriendCode = Plugin.AutoBanBrokenFriendCodeConfig.Value;
                currentPlatformIndex = Mathf.Clamp(Plugin.PlatformIndex.Value, 0, platformValues.Length - 1);
                showWatermark = Plugin.ShowWatermarkConfig.Value;
                showWatermarkInfo = LoadBool("M_ShowWatermarkInfo", showWatermarkInfo);
                unlockCosmetics = Plugin.UnlockCosmeticsConfig.Value;
                unlockCosmicubes = LoadBool("M_UnlockCosmicubes", true);
                activateCompletedCosmicubes = LoadBool("M_ActivateCompletedCosmicubes", false);
                moreLobbyInfo = Plugin.MoreLobbyInfoConfig.Value;
                enableChatDarkMode = Plugin.EnableChatDarkModeConfig.Value;
                ghostChatColorHex = SanitizeGhostChatColorSetting(Plugin.GhostChatColorConfig.Value);
                throttleDefaultLogs = Plugin.ThrottleDefaultLogsConfig.Value;
                detailedLogsEnabled = LoadBool("M_DetailedLogsEnabled", Plugin.DetailedLogsEnabledConfig.Value);
                throttleDefaultLogs = !detailedLogsEnabled;
                showEspFriendCode = Plugin.ShowEspFriendCodeConfig.Value;
                rpcSpoofDelay = Plugin.RpcSpoofDelayConfig.Value;
                currentMenuColorIndex = Mathf.Clamp(Plugin.MenuColorIndexConfig.Value, 0, menuColors.Length - 1);
                rgbMenuMode = Plugin.RgbMenuModeConfig.Value;
                rgbTaskBar = LoadBool("M_RgbTaskBar", rgbTaskBar);
                rgbMenuText = LoadBool("M_RgbMenuText", Plugin.RgbMenuTextConfig.Value);
                boldMenuText = LoadBool("M_BoldMenuText", Plugin.BoldMenuTextConfig.Value);
                whiteMenuTheme = LoadBool("M_WhiteTheme", whiteMenuTheme);
                currentMenuLanguageIndex = 0;
                selectedMenuProfileIndex = Mathf.Clamp(LoadInt("M_MenuProfileSlot", selectedMenuProfileIndex), 0, menuProfileCount - 1);
                limitFps = LoadBool("M_LimitFps", limitFps);
                fpsLimit = Mathf.Clamp(LoadInt("M_FpsLimit", fpsLimit), 1, 560);
                autoCopyCodeAndLeave = LoadBool("M_AutoCopyCodeAndLeave", autoCopyCodeAndLeave);
                blockInnerslothTelemetry = LoadBool("M_BlockInnerslothTelemetry", blockInnerslothTelemetry);
                ApplyTelemetryPreference();
                ApplyFpsLimit();
                autoKickBugs = LoadBool("M_AutoKickBugs", autoKickBugs);
                if (PlayerPrefs.HasKey("M_AutoKickTimer")) autoKickTimer = PlayerPrefs.GetFloat("M_AutoKickTimer");
                disableVoteKicks = LoadBool("M_DisableVoteKicks", disableVoteKicks);
                banVoteKickVoters = LoadBool("M_BanVoteKickVoters", banVoteKickVoters);
                votekickAutoRejoin = LoadBool("M_VotekickAutoRejoin", votekickAutoRejoin);
                votekickCopyCode = LoadBool("M_VotekickCopyCode", votekickCopyCode);
                whitelistOnlyLobby = LoadBool("M_WhitelistOnlyLobby", whitelistOnlyLobby);
                LoadLobbyWhitelist(PlayerPrefs.GetString("M_LobbyWhitelist", PlayerPrefs.GetString("M_VotekickWhitelist", string.Empty)));
                enableLocalNameSpoof = LoadBool("M_LocalNameSpoof", enableLocalNameSpoof);
                enableLocalFriendCodeSpoof = LoadBool("M_LocalFakeFCEnabled", enableLocalFriendCodeSpoof);
                localAlwaysRed = LoadBool("M_LocalAlwaysRed", localAlwaysRed);
                localFortegreen = LoadBool("M_LocalFortegreen", localFortegreen);
                localSnipeColor = LoadBool("M_LocalSnipeColor", localSnipeColor);
                localSnipeColorId = Mathf.Clamp(LoadInt("M_LocalSnipeColorId", localSnipeColorId), 0, 17);
                if (localSnipeColor)
                {
                    localAlwaysRed = false;
                    localFortegreen = false;
                }
                else if (localFortegreen) localAlwaysRed = false;
                if (PlayerPrefs.HasKey("M_LocalFakeFC")) localFriendCodeInput = PlayerPrefs.GetString("M_LocalFakeFC");
                enableDeviceIdSpoof = LoadBool("M_DeviceIdSpoof", enableDeviceIdSpoof);
                if (PlayerPrefs.HasKey("M_DeviceId")) spoofedDeviceId = PlayerPrefs.GetString("M_DeviceId");
                spoofAprilFoolsDate = LoadBool("M_SpoofAprilDate", spoofAprilFoolsDate);
                if (!rgbMenuMode && currentMenuColorIndex >= 0 && currentMenuColorIndex < menuColors.Length)
                {
                    currentAccentColor = menuColors[currentMenuColorIndex];
                }

                showPlayerInfo = LoadBool("M_ShowPlayerInfo", showPlayerInfo);
                showEspBoxes = LoadBool("M_ShowEspBoxes", showEspBoxes);
                espShimmerMode = LoadBool("M_EspShimmerMode", espShimmerMode);
                showEspVoteKicks = LoadBool("M_ShowEspVoteKicks", showEspVoteKicks);
                seeGhosts = LoadBool("M_SeeGhosts", seeGhosts);
                seePhantoms = LoadBool("M_SeePhantoms", seePhantoms);
                seeRoles = LoadBool("M_SeeRoles", seeRoles);
                revealMeetingRoles = LoadBool("M_RevealMeetingRoles", revealMeetingRoles);
                showTracers = LoadBool("M_ShowTracers", showTracers);
                showCrewmateTracers = LoadBool("M_ShowCrewmateTracers", showCrewmateTracers);
                showImpostorTracers = LoadBool("M_ShowImpostorTracers", showImpostorTracers);
                showDeadTracers = LoadBool("M_ShowDeadTracers", showDeadTracers);
                showBodyTracers = LoadBool("M_ShowBodyTracers", showBodyTracers);
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
                chatHistoryLimit = Mathf.Clamp(LoadInt("M_ChatHistoryLimit", chatHistoryLimit), 5, 300);
                if (chatHistoryLimit <= 20)
                    chatHistoryLimit = 80;
                enableClipboard = LoadBool("M_EnableClipboard", enableClipboard);
                enableChatBubbleCopy = LoadBool("M_EnableChatBubbleCopy", enableChatBubbleCopy);
                enableChatNickCopy = LoadBool("M_EnableChatNickCopy", enableChatNickCopy);
                enableChatLog = LoadBool("M_EnableChatLog", enableChatLog);
                enableColorCommand = LoadBool("M_EnableColorCommand", enableColorCommand);
                blockRainbowChat = LoadBool("M_BlockRainbowChat", blockRainbowChat);
                blockFortegreenChat = LoadBool("M_BlockFortegreenChat", blockFortegreenChat);
                skipRoleIntroAnim = LoadBool("M_SkipRoleIntroAnim", skipRoleIntroAnim);
                skipKillAnimation = LoadBool("M_SkipKillAnimation", skipKillAnimation);
                SpoofMenuEnabled = LoadBool("M_SpoofMenuEnabled", SpoofMenuEnabled);
                if (PlayerPrefs.HasKey("M_CustomSpoofRpcInput"))
                    customSpoofRpcInput = FilterSpoofRpcInput(PlayerPrefs.GetString("M_CustomSpoofRpcInput", customSpoofRpcInput));
                noClip = LoadBool("M_NoClip", noClip);
                tpToCursor = LoadBool("M_TpToCursor", tpToCursor);
                dragToCursor = LoadBool("M_DragToCursor", dragToCursor);
                autoFollowCursor = LoadBool("M_AutoFollowCursor", autoFollowCursor);
                freecam = LoadBool("M_Freecam", freecam);
                cameraZoom = LoadBool("M_CameraZoom", cameraZoom);
                showRadar = LoadBool("M_ShowRadar", showRadar);
                showRadarDeadBodies = LoadBool("M_ShowRadarDeadBodies", showRadarDeadBodies);
                showRadarGhosts = LoadBool("M_ShowRadarGhosts", showRadarGhosts);
                radarRightClickTp = LoadBool("M_RadarRightClickTp", radarRightClickTp);
                hideRadarInMeeting = LoadBool("M_HideRadarInMeeting", hideRadarInMeeting);
                radarDrawIcons = LoadBool("M_RadarDrawIcons", radarDrawIcons);
                lockRadar = LoadBool("M_LockRadar", lockRadar);
                radarBorder = LoadBool("M_RadarBorder", radarBorder);
                radarScale = Mathf.Clamp(LoadFloat("M_RadarScale", radarScale), 0.65f, 1.6f);
                radarAlpha = Mathf.Clamp(LoadFloat("M_RadarAlpha", radarAlpha), 0.2f, 1f);
                radarRect = new Rect(
                    LoadFloat("M_RadarX", radarRect.x),
                    LoadFloat("M_RadarY", radarRect.y),
                    radarRect.width,
                    radarRect.height);
                showReplay = LoadBool("M_ShowReplay", showReplay);
                replayOnlyLastSeconds = LoadBool("M_ReplayOnlyLastSeconds", replayOnlyLastSeconds);
                replaySeconds = Mathf.Clamp(LoadFloat("M_ReplaySeconds", replaySeconds), 5f, 180f);
                replayDrawIcons = LoadBool("M_ReplayDrawIcons", replayDrawIcons);
                replayRect = new Rect(
                    LoadFloat("M_ReplayX", replayRect.x),
                    LoadFloat("M_ReplayY", replayRect.y),
                    replayRect.width,
                    replayRect.height);
                RevealVotesEnabled = LoadBool("M_RevealVotes", RevealVotesEnabled);
                noTaskMode = LoadBool("M_NoTaskMode", noTaskMode);
                noMapCooldowns = LoadBool("M_NoMapCooldowns", noMapCooldowns);
                unlockVents = LoadBool("M_UnlockVents", unlockVents);
                walkInVents = LoadBool("M_WalkInVents", walkInVents);
                autoRepairSabotage = LoadBool("M_AutoRepairSabotage", autoRepairSabotage);
                autoBreakSabotage = LoadBool("M_AutoBreakSabotage", autoBreakSabotage);
                allowTasksAsImpostor = LoadBool("M_AllowTasksAsImpostor", allowTasksAsImpostor);
                hostAutoKillRandom = LoadBool("M_HostAutoKillRandom", hostAutoKillRandom);
                hostAutoKillTarget = LoadBool("M_HostAutoKillTarget", hostAutoKillTarget);
                hostAutoKillTargetId = (byte)Mathf.Clamp(LoadInt("M_HostAutoKillTargetId", hostAutoKillTargetId), 0, 255);
                bugRoomAutoAngel = LoadBool("M_BugRoomAutoAngel", bugRoomAutoAngel);
                bugRoomAutoAngelIntervalSeconds = Mathf.Clamp(LoadFloat("M_BugRoomAutoAngelIntervalSeconds", bugRoomAutoAngelIntervalSeconds), 0.001f, 0.50f);
                bugRoomAutoKillShield = LoadBool("M_BugRoomAutoKillShield", bugRoomAutoKillShield);
                bugRoomTimedAutoRun = LoadBool("M_BugRoomTimedAutoRun", bugRoomTimedAutoRun);
                if (PlayerPrefs.HasKey("M_BugRoomTimedAutoRunMinutes"))
                    bugRoomTimedAutoRunMinutes = Mathf.Clamp(PlayerPrefs.GetInt("M_BugRoomTimedAutoRunMinutes", bugRoomTimedAutoRunMinutes), 1, 60);
                bugRoomTimedAutoRunInput = bugRoomTimedAutoRunMinutes.ToString();
                bugRoomLv35Rehost = false;
                bugRoomHostPassRejoin = false;
                killWhileVanishedHostOnly = LoadBool("M_KillWhileVanishedHostOnly", killWhileVanishedHostOnly);
                disableEndGameSafeMode = LoadBool("M_DisableEndGameSafeMode", disableEndGameSafeMode);
                disableMapSafeMode = LoadBool("M_DisableMapSafeMode", disableMapSafeMode);
                roleBuffImmortality = LoadBool("M_RoleBuffImmortality", roleBuffImmortality);
                neverEndGame = LoadBool("M_NeverEndGame", neverEndGame);
                autoChatEveryone = LoadBool("M_ChatAsEveryone", autoChatEveryone);
                removePenalty = LoadBool("M_RemovePenalty", removePenalty);
                guestExtraFeatures = LoadBool("M_GuestExtraFeatures", guestExtraFeatures);
                bypassAgeRestrictions = LoadBool("M_BypassAgeRestrictions", bypassAgeRestrictions);
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
                overflowProtection = LoadBool("M_OverflowProtection", LoadBool("M_OverflowDef", overflowProtection));
                blockVentKickExploit = LoadBool("M_BlockVentKickExploit", blockVentKickExploit);
                blockServerTeleports = LoadBool("M_BlockServerTeleports", blockServerTeleports);
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
                AutoHostShieldBreakEnabled = LoadBool("M_AutoHostShieldBreakEnabled", AutoHostShieldBreakEnabled);
                AutoReturnLobbyAfterMatch = LoadBool("M_AutoReturnLobbyAfterMatch", AutoReturnLobbyAfterMatch);
                AutoHostNotifications = LoadBool("M_AutoHostNotifications", AutoHostNotifications);
                AutoHostForceLastMinute = LoadBool("M_AutoHostForceLastMinute", AutoHostForceLastMinute);
                AutoHostWaitLoadedPlayers = LoadBool("M_AutoHostWaitLoadedPlayers", AutoHostWaitLoadedPlayers);
                AutoHostCancelBelowMin = LoadBool("M_AutoHostCancelBelowMin", AutoHostCancelBelowMin);
                AutoHostInstantStart = LoadBool("M_AutoHostInstantStart", AutoHostInstantStart);
                AutoHostAutoRunEnabled = LoadBool("M_AutoHostAutoRunEnabled", AutoHostAutoRunEnabled);
                AutoHostAutoRunDelaySeconds = Mathf.Clamp(LoadFloat("M_AutoHostAutoRunDelaySeconds", AutoHostAutoRunDelaySeconds), 0.25f, 10f);
                BugroomScoutEnabled = LoadBool("M_BugroomScoutEnabled", BugroomScoutEnabled);
                BugroomGlitchFinderEnabled = LoadBool("M_BugroomGlitchFinderEnabled", BugroomGlitchFinderEnabled);
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
                enableMenuCharacter = LoadBool("M_EnableMenuCharacter", enableMenuCharacter);
                hardMenu = LoadBool("M_HardMenu", hardMenu);
                EnableCustomNotifs = LoadBool("M_EnableCustomNotifs", EnableCustomNotifs);
                LogAllRPCs = LoadBool("M_LogAllRPCs", LogAllRPCs);
                discordRpcEnabled = LoadBool("M_DiscordRpcEnabled", discordRpcEnabled);
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
                currentSelfSubTab = Mathf.Clamp(LoadInt("M_CurrentSelfSubTab", currentSelfSubTab), 0, selfSubTabs.Length - 1);
                currentVisualsSubTab = Mathf.Clamp(LoadInt("M_CurrentVisualsSubTab", currentVisualsSubTab), 0, visualsSubTabs.Length - 1);
                currentPlayersSubTab = Mathf.Clamp(LoadInt("M_CurrentPlayersSubTab", currentPlayersSubTab), 0, playersSubTabs.Length - 1);
                NetworkedClones.AutoClearBeforeGame = LoadBool("M_AutoClearClonesBeforeGame", NetworkedClones.AutoClearBeforeGame);
                currentSabotageSubTab = Mathf.Clamp(LoadInt("M_CurrentSabotageSubTab", currentSabotageSubTab), 0, 3);
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
                if (!limitFps)
                {
                    Application.targetFrameRate = -1;
                    lastAppliedFpsLimit = -1;
                    return;
                }

                fpsLimit = Mathf.Clamp(fpsLimit, 1, 560);
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
                chatHistoryLimit = Mathf.Clamp(chatHistoryLimit, 5, 300);
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

private void UpdateRoundedGradientTex(Texture2D tex, Color left, Color right, float radius)
        {
            int width = tex.width;
            int height = tex.height;
            Color[] pix = new Color[width * height];
            float cx = width / 2f;
            float cy = height / 2f;
            float innerX = Mathf.Max(0f, cx - radius);
            float innerY = Mathf.Max(0f, cy - radius);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dx = Mathf.Max(0f, Mathf.Abs(x - cx + 0.5f) - innerX);
                    float dy = Mathf.Max(0f, Mathf.Abs(y - cy + 0.5f) - innerY);
                    float alpha = Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
                    Color col = Color.Lerp(left, right, width > 1 ? (float)x / (width - 1) : 0f);
                    col.a *= alpha;
                    pix[y * width + x] = col;
                }
            }

            tex.SetPixels(pix);
            tex.Apply();
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

private bool HasMenuAccentPair()
        {
            return currentMenuColorIndex >= menuColors.Length - menuPairColors.Length;
        }

private static bool MenuAccentPairActive()
        {
            try
            {
                return activeGui != null && activeGui.HasMenuAccentPair();
            }
            catch { }

            return false;
        }

private Color GetSelectedMenuControlColor()
        {
            int index = Mathf.Clamp(currentMenuColorIndex, 0, menuColors.Length - 1);
            int pairIndex = index - (menuColors.Length - menuPairColors.Length);
            return pairIndex >= 0 && pairIndex < menuPairColors.Length ? menuPairColors[pairIndex] : menuColors[index];
        }

private static Color GetStableMenuControlAccentSource()
        {
            try
            {
                if (activeGui != null)
                    return activeGui.GetSelectedMenuControlColor();
            }
            catch { }

            return currentAccentColor;
        }
}
}
