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

public static class AmongUsClientUtils
        {
            public static IEnumerator CreatePlayer(AmongUsClient __instance, ClientData clientData)
            {
                if (clientData.IsBeingCreated || clientData.Character)
                {
                    yield break;
                }
                if (!__instance.AmHost)
                {
                    __instance.logger.Debug("Waiting for host to make my player", null);
                    yield break;
                }
                clientData.IsBeingCreated = true;
                bool isOwnerOfPlayerData = (__instance.NetworkMode == NetworkModes.LocalGame || __instance.AmModdedHost || (__instance).NetworkMode == NetworkModes.FreePlay);
                sbyte b;
                if (isOwnerOfPlayerData)
                {
                    b = (GameData.Instance.HasPlayer(clientData) ? GameData.Instance.GetPlayerIdFromClient(clientData) : GameData.Instance.GetAvailableId());
                    if (b == -1)
                    {
                        (__instance).SendLateRejection(clientData.Id, DisconnectReasons.GameFull);
                        __instance.logger.Info("Overfilled room.", null);
                        clientData.IsBeingCreated = false;
                        yield break;
                    }
                }
                else
                {
                    yield return new WaitUntil((Func<bool>)(() => GameData.Instance.HasPlayer(clientData)));
                    b = GameData.Instance.GetPlayerIdFromClient(clientData);
                }
                Vector2 vector = Vector2.zero;
                if (DestroyableSingleton<TutorialManager>.InstanceExists)
                {
                    vector = new Vector2(-1.9f, 3.25f);
                }
                PlayerControl pc = Object.Instantiate(__instance.PlayerPrefab, vector, Quaternion.identity);
                pc.PlayerId = (byte)b;
                pc.FriendCode = clientData.FriendCode;
                pc.Puid = clientData.ProductUserId;
                clientData.Character = pc;
                (__instance).UpdateCachedClients(clientData, clientData.Character);
                if (ShipStatus.Instance)
                {
                    ShipStatus.Instance.SpawnPlayer(pc, Palette.PlayerColors.Length, initialSpawn: false);
                }
                if (isOwnerOfPlayerData)
                {
                    NetworkedPlayerInfo netObjParent = GameData.Instance.AddPlayer(pc, clientData);
                    __instance.Spawn(netObjParent);
                }
                else
                {
                    while (GameData.Instance.GetPlayerByClient(clientData) == null)
                    {
                        yield return null;
                    }
                }
                AmongUsClient.Instance.Spawn(pc, clientData.Id, SpawnFlags.IsClientCharacter);
                if (isOwnerOfPlayerData)
                {
                    GameData.Instance.DirtyAllData();
                }
                if (GameManager.Instance.LogicOptions.IsDefaults)
                {
                    GameManager.Instance.LogicOptions.SetRecommendations(GameData.Instance.PlayerCount, (AmongUsClient.Instance).NetworkMode);
                }
                clientData.IsBeingCreated = false;
            }

            public static SpawnGameDataMessage CreateSpawnMessage(InnerNetObject netObjParent, int ownerId, SpawnFlags flags)
            {
                InnerNetObject[] array = netObjParent.GetComponentsInChildren<InnerNetObject>();
                InnerNetObject[] array2 = array;
                foreach (InnerNetObject innerNetObject in array2)
                {
                    if (innerNetObject is CustomNetworkTransform)
                    {
                        innerNetObject.OwnerId = (AmongUsClient.Instance).ClientId;
                    }
                    else
                    {
                        innerNetObject.OwnerId = ownerId;
                    }
                    innerNetObject.SpawnFlags = flags;
                    if (innerNetObject.NetId == 0)
                    {
                        AmongUsClient instance = AmongUsClient.Instance;
                        uint netIdCnt = instance.NetIdCnt;
                        instance.NetIdCnt = netIdCnt + 1;
                        innerNetObject.NetId = netIdCnt;
                        lock (AmongUsClient.Instance.allObjects)
                        {
                            AmongUsClient.Instance.allObjects.TryAddNetObject(innerNetObject);
                        }
                    }
                }
                return new SpawnGameDataMessage(netObjParent, ownerId, flags, array);
            }

            public static SpawnGameDataMessage CreateSpawnMessage(AmongUsClient __instance, InnerNetObject netObjParent, int ownerId, SpawnFlags flags)
            {
                InnerNetObject[] array = netObjParent.GetComponentsInChildren<InnerNetObject>();
                InnerNetObject[] array2 = array;
                foreach (InnerNetObject innerNetObject in array2)
                {
                    innerNetObject.OwnerId = ownerId;
                    innerNetObject.SpawnFlags = flags;
                    if (innerNetObject.NetId == 0)
                    {
                        uint netIdCnt = (__instance).NetIdCnt;
                        (__instance).NetIdCnt = netIdCnt + 1;
                        innerNetObject.NetId = netIdCnt;
                        lock ((__instance).allObjects)
                        {
                            (__instance).allObjects.TryAddNetObject(innerNetObject);
                        }
                    }
                }
                return new SpawnGameDataMessage(netObjParent, ownerId, flags, array);
            }

            public static IEnumerator CoOnPlayerChangedScene(InnerNetClient __instance, ClientData client, string currentScene)
            {
                client.InScene = true;
                if (GameData.Instance == null)
                {
                    GameData.Instance = Object.Instantiate(AmongUsClient.Instance.GameDataPrefab);
                }
                GameData.Instance.RemoveDisconnectedPlayers();
                if (!__instance.AmHost)
                {
                    yield break;
                }
                if (VoteBanSystem.Instance == null)
                {
                    VoteBanSystem.Instance = Object.Instantiate(AmongUsClient.Instance.VoteBanPrefab);
                    __instance.Spawn(VoteBanSystem.Instance);
                }
                if (currentScene.Equals("Tutorial"))
                {
                    GameManager.DestroyInstance();
                    GameManager netObjParent = GameManagerCreator.CreateGameManager(GameOptionsManager.Instance.CurrentGameOptions.GameMode);
                    __instance.Spawn(netObjParent);
                    int index = ((AmongUsClient.Instance.TutorialMapId == 0 && AprilFoolsMode.ShouldFlipSkeld()) ? 3 : AmongUsClient.Instance.TutorialMapId);
                    AmongUsClient.Instance.ShipLoadingAsyncHandle = AmongUsClient.Instance.ShipPrefabs[index].InstantiateAsync(null, false);
                    yield return AmongUsClient.Instance.ShipLoadingAsyncHandle;
                    AsyncOperationHandle<GameObject> test = AmongUsClient.Instance.ShipLoadingAsyncHandle;
                    GameObject result = test.Result;
                    AmongUsClient.Instance.ShipLoadingAsyncHandle = null;
                    __instance.Spawn(result.GetComponent<ShipStatus>());
                    yield return AmongUsClient.Instance.CreatePlayer(client);
                }
                else
                {
                    if (!currentScene.Equals("OnlineGame"))
                    {
                        yield break;
                    }
                    if (client.Id != __instance.ClientId)
                    {
                        __instance.SendInitialData(client.Id);
                    }
                    else
                    {
                        if (__instance.NetworkMode == NetworkModes.LocalGame)
                        {
                            __instance.StartCoroutine(AmongUsClient.Instance.CoBroadcastManager());
                        }
                        GameManager.DestroyInstance();
                        GameManager netObjParent2 = GameManagerCreator.CreateGameManager(GameOptionsManager.Instance.CurrentGameOptions.GameMode);
                        __instance.Spawn(netObjParent2);
                    }
                    yield return CreatePlayer(AmongUsClient.Instance, client);
                }
            }
        }

[HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.HandleRpc))]
        public static class Shield_PetSpam_Patch
        {
            public static bool Prefix(PlayerPhysics __instance, byte callId, Hazel.MessageReader reader)
            {
                if (!ElysiumModMenuGUI.enablePasosLimit) return true;

                if (callId == 49 || callId == 50)
                {
                    try
                    {
                        if (__instance == null || __instance.myPlayer == null) return true;

                        if (__instance.myPlayer == PlayerControl.LocalPlayer) return true;

                        return false;

                        return false;
                    }
                    catch { }
                }

                return true;
            }
        }

public static int GetColorIdByName(string name)
        {
            string[] names = { "red", "blue", "green", "pink", "orange", "yellow", "black", "white", "purple", "brown", "cyan", "lime", "maroon", "rose", "banana", "gray", "tan", "coral", "fortegreen" };
            for (int i = 0; i < names.Length; i++)
                if (names[i] == name.ToLower().Trim()) return i;
            return -1;
        }

private IEnumerator AttemptShapeshiftFrame(PlayerControl target, PlayerControl morphInto)
        {
            if (target == null || morphInto == null || PlayerControl.LocalPlayer == null || AmongUsClient.Instance == null) yield break;

            bool hasAnticheat = AmongUsClient.Instance.NetworkMode == NetworkModes.OnlineGame && !Constants.IsVersionModded();

            if (target.Data.RoleType != RoleTypes.Shapeshifter && hasAnticheat)
            {
                RoleTypes currentRole = target.Data.RoleType;
                target.RpcSetRole(RoleTypes.Shapeshifter, true);

                yield return new WaitForSeconds(0.5f);

                target.RpcShapeshift(morphInto, true);

                yield return new WaitForSeconds(0.5f);

                target.RpcSetRole(currentRole, true);
            }
            else
            {
                target.RpcShapeshift(morphInto, true);
            }
            ShowNotification($"<color=#ca08ff>[MORPH]</color> <b>{target.Data.PlayerName}</b> morphed into <b>{morphInto.Data.PlayerName}</b>!");
        }

private IEnumerator MassMorphCoroutine()
        {
            if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost || PlayerControl.AllPlayerControls == null) yield break;

            bool hasAnticheat = AmongUsClient.Instance.NetworkMode == NetworkModes.OnlineGame && !Constants.IsVersionModded();

            Dictionary<byte, RoleTypes> originalRoles = new Dictionary<byte, RoleTypes>();

            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (pc != null && pc.Data != null && !pc.Data.Disconnected)
                {
                    originalRoles[pc.PlayerId] = pc.Data.RoleType;

                    if (hasAnticheat && pc.Data.RoleType != RoleTypes.Shapeshifter)
                    {
                        pc.RpcSetRole(RoleTypes.Shapeshifter, true);
                    }
                }
            }

            if (hasAnticheat) yield return new UnityEngine.WaitForSeconds(0.5f);

            PlayerControl targetToMorphInto = null;
            if (selectedMorphTargetId != 255)
            {
                targetToMorphInto = GameData.Instance.GetPlayerById(selectedMorphTargetId)?.Object;
            }

            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (pc != null && pc.Data != null && !pc.Data.Disconnected)
                {
                    PlayerControl morphTarget = targetToMorphInto != null ? targetToMorphInto : pc;
                    pc.RpcShapeshift(morphTarget, true);
                }
            }


            if (hasAnticheat) yield return new UnityEngine.WaitForSeconds(0.5f);

            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (pc != null && pc.Data != null && !pc.Data.Disconnected)
                {
                    if (hasAnticheat && originalRoles.ContainsKey(pc.PlayerId))
                    {
                        pc.RpcSetRole(originalRoles[pc.PlayerId], true);
                    }
                }
            }

            string notifText = targetToMorphInto != null ? targetToMorphInto.Data.PlayerName : "Egg";
            ShowNotification($"<color=#FF00FF>[MASS MORPH]</color> {notifText}");
        }

private void ForceMeetingAsPlayer(PlayerControl target)
        {
            if (target == null || target.Data == null) return;
            TryOpenModdedMeeting(target, null, $"<color=#00FF00>[MEETING]</color> Modded meeting from <b>{target.Data.PlayerName}</b>.");
        }

private void KillAll()
        {
            if (PlayerControl.LocalPlayer == null || PlayerControl.AllPlayerControls == null) return;
            if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
            {
                foreach (var player in PlayerControl.AllPlayerControls.ToArray())
                {
                    TryHostElysiumMurderPlayer(player);
                }
                return;
            }

            Vector3 op = PlayerControl.LocalPlayer.transform.position;
            var targets = PlayerControl.AllPlayerControls.ToArray();
            foreach (var t in targets)
            {
                if (t != null && t != PlayerControl.LocalPlayer && !t.Data.IsDead && !t.Data.Disconnected)
                {
                    PlayerControl.LocalPlayer.NetTransform.RpcSnapTo(t.transform.position);
                    PlayerControl.LocalPlayer.CmdCheckMurder(t);
                }
            }
            PlayerControl.LocalPlayer.NetTransform.RpcSnapTo(op);
        }

private void KickAll()
        {
            if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost && PlayerControl.AllPlayerControls != null)
            {
                foreach (var pc in PlayerControl.AllPlayerControls)
                    if (pc != null && pc != PlayerControl.LocalPlayer && !pc.Data.Disconnected)
                        AmongUsClient.Instance.KickPlayer((int)pc.OwnerId, false);
            }
        }

private void DespawnLobby()
        {
            try
            {
                if (!CanMutateLobbyMap("Despawn Lobby", false, disableMapSafeMode)) return;

                int despawned = 0;
                try
                {
                    LobbyBehaviour[] lobbies = UnityEngine.Object.FindObjectsOfType<LobbyBehaviour>();
                    foreach (LobbyBehaviour lobby in lobbies)
                    {
                        try
                        {
                            if (lobby == null) continue;
                            lobby.Cast<InnerNetObject>().Despawn();
                            despawned++;
                        }
                        catch { }
                    }
                }
                catch { }

                if (despawned == 0 && LobbyBehaviour.Instance != null)
                    LobbyBehaviour.Instance.Cast<InnerNetObject>().Despawn();

                ResetLobbyMapTransientState();
            }
            catch { }
        }

private void SpawnLobby()
        {
            try
            {
                if (!CanMutateLobbyMap("Spawn Lobby", false, disableMapSafeMode)) return;

                if (LobbyBehaviour.Instance != null)
                {
                    ShowNotification("<color=#FFAA00>[LOBBY]</color> Lobby is already spawned.");
                    return;
                }

                if (ShipStatus.Instance != null)
                {
                    ShowNotification("<color=#FFAA00>[LOBBY]</color> Despawn the map before spawning a lobby.");
                    return;
                }

                if (GameStartManager.Instance != null && AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
                {
                    LobbyBehaviour newLobby = UnityEngine.Object.Instantiate<LobbyBehaviour>(GameStartManager.Instance.LobbyPrefab);
                    AmongUsClient.Instance.Spawn(newLobby.Cast<InnerNetObject>(), -2, SpawnFlags.None);
                    ResetLobbyMapTransientState();
                }
            }
            catch { }
        }

private static void ResetLobbyMapTransientState()
        {
            try { fortegreenTimer.Clear(); } catch { }
            try { lastKillTimestamps.Clear(); } catch { }
        }

private static bool CanMutateLobbyMap(string actionName, bool allowActiveMatch = false, bool disableSafeMode = false)
        {
            try
            {
                if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
                {
                    ShowNotification($"<color=#FF0000>[{actionName}]</color> Host only.");
                    return false;
                }

                if (!disableSafeMode && (MeetingHud.Instance != null || ExileController.Instance != null || IntroCutscene.Instance != null))
                {
                    ShowNotification($"<color=#FFAA00>[{actionName}]</color> Blocked during meeting/exile/intro.");
                    return false;
                }

                if (!allowActiveMatch && AmongUsClient.Instance.IsGameStarted)
                {
                    ShowNotification($"<color=#FFAA00>[{actionName}]</color> Blocked during an active match.");
                    return false;
                }

                return true;
            }
            catch { return false; }
        }

public static void ChangeNameGlobalHost(PlayerControl target, string newName)
        {
            if (target == null) return;
            if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return;
            try
            {
                target.RpcSetName(newName);
                var netObj = GameData.Instance.GetComponent<InnerNetObject>();
                if (netObj != null) netObj.SetDirtyBit(1U << (int)target.PlayerId);
            }
            catch { }
        }

private static void ApplyLocalNameSelf(string newName, bool notify = true)
        {
            try
            {
                PlayerControl local = PlayerControl.LocalPlayer;
                if (local == null)
                {
                    if (notify) ShowNotification("<color=#FF4444>[LOCAL NAME]</color> Local player not found.");
                    return;
                }

                string renderName = BuildLocalNameRenderText(newName);
                if (originalLocalName == null)
                {
                    originalLocalName = local.CurrentOutfit != null && !string.IsNullOrWhiteSpace(local.CurrentOutfit.PlayerName)
                        ? local.CurrentOutfit.PlayerName
                        : local.Data?.PlayerName;
                }

                if (local.cosmetics != null)
                    local.cosmetics.SetName(renderName);

                TrySetPlayerNameObject(local.Data, renderName);
                if (local.Data != null)
                {
                    TrySetPlayerNameObject(local.Data.DefaultOutfit, renderName);
                    TrySetPlayerNameObject(local.CurrentOutfit, renderName);
                }

                if (notify)
                    ShowNotification($"<color=#00FFAA>[LOCAL NAME]</color> Applied locally: <b>{newName}</b>");
            }
            catch { }
        }

        private static void RestoreLocalNameSelf()
        {
            try
            {
                PlayerControl local = PlayerControl.LocalPlayer;
                if (local == null || local.cosmetics == null) return;

                string baseName = !string.IsNullOrWhiteSpace(originalLocalName)
                    ? originalLocalName
                    : (local.Data?.PlayerName ?? local.CurrentOutfit?.PlayerName);
                if (!string.IsNullOrWhiteSpace(baseName))
                {
                    local.cosmetics.SetName(baseName);
                    TrySetPlayerNameObject(local.Data, baseName);
                    if (local.Data != null)
                    {
                        TrySetPlayerNameObject(local.Data.DefaultOutfit, baseName);
                        TrySetPlayerNameObject(local.CurrentOutfit, baseName);
                    }
                }

                originalLocalName = null;
            }
            catch { }
        }

        private static void ApplyLocalFriendCodeSelf(string fakeFriendCode, bool notify = true)
        {
            try
            {
                PlayerControl local = PlayerControl.LocalPlayer;
                if (local == null || local.Data == null)
                {
                    if (notify) ShowNotification("<color=#FF4444>[LOCAL FC]</color> Local player data not found.");
                    return;
                }

                fakeFriendCode ??= string.Empty;
                if (originalLocalFriendCode == null)
                {
                    originalLocalFriendCode = GetCachedOriginalFriendCode(local.Data, string.Empty);
                }
                localFriendCodeInput = fakeFriendCode;

                if (notify)
                    ShowNotification($"<color=#00FFAA>[LOCAL FC]</color> Applied locally: <b>{fakeFriendCode}</b>");
            }
            catch { }
        }

        private static void RestoreLocalFriendCodeSelf()
        {
            try
            {
                originalLocalFriendCode = null;
            }
            catch { }
        }

        private static void TrySetPlayerNameObject(object target, string newName)
        {
            TrySetStringMember(target, "PlayerName", newName);
        }

        private static void TrySetStringMember(object target, string memberName, string value)
        {
            if (target == null || string.IsNullOrEmpty(memberName)) return;

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            Type type = target.GetType();

            try
            {
                PropertyInfo property = type.GetProperty(memberName, flags);
                if (property != null && property.CanWrite)
                {
                    property.SetValue(target, value, null);
                    return;
                }
            }
            catch { }

            try
            {
                FieldInfo field = type.GetField(memberName, flags);
                if (field != null) field.SetValue(target, value);
            }
            catch { }
        }

        private static void TryInvokeStringMethod(object target, string methodName, string value)
        {
            if (target == null) return;

            try
            {
                MethodInfo method = target.GetType().GetMethod(
                    methodName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    new[] { typeof(string) },
                    null);

                if (method != null)
                    method.Invoke(target, new object[] { value });
            }
            catch { }
        }

        public static bool showWatermark = true;

public static bool whiteMenuTheme = false;

private static void SaveBool(string key, bool value)
        {
            PlayerPrefs.SetInt(key, value ? 1 : 0);
        }

private static bool LoadBool(string key, bool defaultValue)
        {
            return PlayerPrefs.HasKey(key) ? PlayerPrefs.GetInt(key) == 1 : defaultValue;
        }

private static int LoadInt(string key, int defaultValue)
        {
            return PlayerPrefs.HasKey(key) ? PlayerPrefs.GetInt(key) : defaultValue;
        }

private static float LoadFloat(string key, float defaultValue)
        {
            return PlayerPrefs.HasKey(key) ? PlayerPrefs.GetFloat(key) : defaultValue;
        }

private void SaveConfig()
        {
            try
            {
                PlayerPrefs.SetInt("M_BndMagnet", (int)bindMagnetCursor);
                Plugin.SpoofedLevel.Value = spoofLevelString;
                Plugin.EnableLevelSpoofConfig.Value = enableLevelSpoof;
                SaveBool("M_EnableLevelSpoof", enableLevelSpoof);
                Plugin.EnableFriendCodeSpoofConfig.Value = enableFriendCodeSpoof;
                Plugin.SpoofFriendCodeConfig.Value = spoofFriendCodeInput;
                Plugin.EnablePlatformSpoof.Value = enablePlatformSpoof;
                Plugin.AutoBanBrokenFriendCodeConfig.Value = autoBanBrokenFriendCode;
                Plugin.PlatformIndex.Value = currentPlatformIndex;
                Plugin.ShowWatermarkConfig.Value = showWatermark;
                Plugin.UnlockCosmeticsConfig.Value = unlockCosmetics;
                SaveBool("M_UnlockCosmicubes", unlockCosmicubes);
                SaveBool("M_ActivateCompletedCosmicubes", activateCompletedCosmicubes);
                Plugin.MoreLobbyInfoConfig.Value = moreLobbyInfo;
                Plugin.EnableChatDarkModeConfig.Value = enableChatDarkMode;
                Plugin.GhostChatColorConfig.Value = SanitizeGhostChatColorSetting(ghostChatColorHex);
                Plugin.ThrottleDefaultLogsConfig.Value = throttleDefaultLogs;
                Plugin.DetailedLogsEnabledConfig.Value = detailedLogsEnabled;
                Plugin.ShowEspFriendCodeConfig.Value = showEspFriendCode;
                Plugin.RpcSpoofDelayConfig.Value = rpcSpoofDelay;
                Plugin.MenuColorIndexConfig.Value = currentMenuColorIndex;
                Plugin.RgbMenuModeConfig.Value = rgbMenuMode;
                Plugin.RgbMenuTextConfig.Value = rgbMenuText;
                Plugin.BoldMenuTextConfig.Value = boldMenuText;
                SaveBool("M_RgbTaskBar", rgbTaskBar);
                if (menuToggleKey == KeyCode.None) menuToggleKey = KeyCode.Insert;
                Plugin.MenuKeybind.Value = menuToggleKey;
                PlayerPrefs.SetInt("M_MenuToggleKey", (int)menuToggleKey);
                SaveBool("M_WhiteTheme", whiteMenuTheme);
                SaveBool("M_RgbMenuText", rgbMenuText);
                SaveBool("M_BoldMenuText", boldMenuText);
                PlayerPrefs.SetInt("M_MenuLanguageIndex", currentMenuLanguageIndex);
                SaveBool("M_LimitFps", limitFps);
                PlayerPrefs.SetInt("M_FpsLimit", fpsLimit);
                SaveBool("M_DetailedLogsEnabled", detailedLogsEnabled);
                SaveBool("M_EnableBackground", enableBackground);
                SaveBool("M_HardMenu", hardMenu);
                SaveBool("M_AutoCopyCodeAndLeave", autoCopyCodeAndLeave);
                SaveBool("M_BlockInnerslothTelemetry", blockInnerslothTelemetry);
                SaveBool("M_EnableCustomNotifs", EnableCustomNotifs);
                SaveBool("M_LogAllRPCs", LogAllRPCs);
                SaveBool("M_DiscordRpcEnabled", discordRpcEnabled);
                PlayerPrefs.SetInt("M_SelectedSpoofMenuIndex", selectedSpoofMenuIndex);
                PlayerPrefs.SetFloat("M_MenuWindowX", windowRect.x);
                PlayerPrefs.SetFloat("M_MenuWindowY", windowRect.y);
                PlayerPrefs.SetFloat("M_MenuWindowW", windowRect.width);
                PlayerPrefs.SetFloat("M_MenuWindowH", windowRect.height);
                PlayerPrefs.SetInt("M_CurrentTab", currentTab);
                PlayerPrefs.SetInt("M_TargetTab", targetTabIndex);
                PlayerPrefs.SetInt("M_CurrentGeneralSubTab", currentGeneralSubTab);
                PlayerPrefs.SetInt("M_CurrentGeneralInfoSubTab", currentGeneralInfoSubTab);
                PlayerPrefs.SetInt("M_CurrentSelfSubTab", currentSelfSubTab);
                PlayerPrefs.SetInt("M_CurrentVisualsSubTab", currentVisualsSubTab);
                PlayerPrefs.SetInt("M_CurrentPlayersSubTab", currentPlayersSubTab);
                PlayerPrefs.SetInt("M_CurrentSabotageSubTab", currentSabotageSubTab);
                PlayerPrefs.SetInt("M_CurrentHostOnlySubTab", currentHostOnlySubTab);
                PlayerPrefs.SetInt("M_CurrentAutoHostSubTab", currentAutoHostSubTab);
                PlayerPrefs.SetInt("M_BndMMorph", (int)bindMassMorph);
                PlayerPrefs.SetInt("M_BndSpawn", (int)bindSpawnLobby);
                PlayerPrefs.SetInt("M_BndDespawn", (int)bindDespawnLobby);
                PlayerPrefs.SetInt("M_BndCloseMtg", (int)bindCloseMeeting);
                PlayerPrefs.SetInt("M_BndInstaStart", (int)bindInstaStart);
                PlayerPrefs.SetInt("M_BndEndCrew", (int)bindEndCrew);
                PlayerPrefs.SetInt("M_BndEndImp", (int)bindEndImp);
                PlayerPrefs.SetInt("M_BndEndImpDC", (int)bindEndImpDC);
                PlayerPrefs.SetInt("M_BndEndHnsDC", (int)bindEndHnsDC);
                PlayerPrefs.SetInt("M_BndToggleTracers", (int)bindToggleTracers);
                PlayerPrefs.SetInt("M_BndToggleNoClip", (int)bindToggleNoClip);
                PlayerPrefs.SetInt("M_BndToggleFreecam", (int)bindToggleFreecam);
                PlayerPrefs.SetInt("M_BndToggleCameraZoom", (int)bindToggleCameraZoom);
                PlayerPrefs.SetInt("M_BndKillAll", (int)bindKillAll);
                PlayerPrefs.SetInt("M_BndCallMeeting", (int)bindCallMeeting);
                PlayerPrefs.SetInt("M_BndTogglePlayerInfo", (int)bindTogglePlayerInfo);
                PlayerPrefs.SetInt("M_BndToggleSeeRoles", (int)bindToggleSeeRoles);
                PlayerPrefs.SetInt("M_BndToggleSeeGhosts", (int)bindToggleSeeGhosts);
                PlayerPrefs.SetInt("M_BndToggleFullBright", (int)bindToggleFullBright);
                PlayerPrefs.SetInt("M_BndKickAll", (int)bindKickAll);
                PlayerPrefs.SetInt("M_BndFixSabotages", (int)bindFixSabotages);
                PlayerPrefs.SetInt("M_BndSetAllGhost", (int)bindSetAllGhost);
                PlayerPrefs.SetInt("M_BndSetAllGhostImp", (int)bindSetAllGhostImp);
                PlayerPrefs.SetInt("M_BndReviveAll", (int)bindReviveAll);
                SaveBool("M_AutoKickBugs", autoKickBugs);
                PlayerPrefs.SetFloat("M_AutoKickTimer", autoKickTimer);
                SaveBool("M_DisableVoteKicks", disableVoteKicks);
                SaveBool("M_BanVoteKickVoters", banVoteKickVoters);
                SaveBool("M_VotekickAutoRejoin", votekickAutoRejoin);
                SaveBool("M_VotekickCopyCode", votekickCopyCode);
                SaveBool("M_WhitelistOnlyLobby", whitelistOnlyLobby);
                PlayerPrefs.SetString("M_LobbyWhitelist", SaveLobbyWhitelist());
                SaveBool("M_LocalNameSpoof", enableLocalNameSpoof);
                SaveBool("M_LocalFakeFCEnabled", enableLocalFriendCodeSpoof);
                PlayerPrefs.SetString("M_LocalFakeFC", localFriendCodeInput);
                SaveBool("M_DeviceIdSpoof", enableDeviceIdSpoof);
                PlayerPrefs.SetString("M_DeviceId", spoofedDeviceId ?? "");
                SaveBool("M_SpoofAprilDate", spoofAprilFoolsDate);

                SaveBool("M_ShowPlayerInfo", showPlayerInfo);
                SaveBool("M_ShowEspBoxes", showEspBoxes);
                SaveBool("M_EspShimmerMode", espShimmerMode);
                SaveBool("M_ShowEspVoteKicks", showEspVoteKicks);
                SaveBool("M_SeeGhosts", seeGhosts);
                SaveBool("M_SeePhantoms", seePhantoms);
                SaveBool("M_SeeRoles", seeRoles);
                SaveBool("M_RevealMeetingRoles", revealMeetingRoles);
                SaveBool("M_ShowTracers", showTracers);
                SaveBool("M_ShowCrewmateTracers", showCrewmateTracers);
                SaveBool("M_ShowImpostorTracers", showImpostorTracers);
                SaveBool("M_ShowDeadTracers", showDeadTracers);
                SaveBool("M_ShowBodyTracers", showBodyTracers);
                SaveBool("M_FullBright", fullBright);
                SaveBool("M_SeeProtections", seeProtections);
                SaveBool("M_SeeKillCooldown", seeKillCooldown);
                SaveBool("M_ExtendedLobby", extendedLobby);
                SaveBool("M_MoreLobbyInfo", moreLobbyInfo);
                SaveBool("M_AlwaysChat", alwaysChat);
                SaveBool("M_LobbyRainbowAll", lobbyRainbowAll);
                SaveBool("M_LobbyAllColor", lobbyAllColor);
                PlayerPrefs.SetInt("M_LobbyAllColorId", lobbyAllColorId);
                SaveBool("M_ReadGhostChat", readGhostChat);
                SaveBool("M_EnableExtendedChat", enableExtendedChat);
                SaveBool("M_EnableFastChat", enableFastChat);
                SaveBool("M_AllowLinksAndSymbols", allowLinksAndSymbols);
                SaveBool("M_EnableChatHistory", enableChatHistory);
                PlayerPrefs.SetInt("M_ChatHistoryLimit", chatHistoryLimit);
                SaveBool("M_EnableClipboard", enableClipboard);
                SaveBool("M_EnableChatBubbleCopy", enableChatBubbleCopy);
                SaveBool("M_EnableChatNickCopy", enableChatNickCopy);
                SaveBool("M_EnableChatLog", enableChatLog);
                SaveBool("M_EnableColorCommand", enableColorCommand);
                SaveBool("M_BlockRainbowChat", blockRainbowChat);
                SaveBool("M_BlockFortegreenChat", blockFortegreenChat);
                SaveBool("M_SkipRoleIntroAnim", skipRoleIntroAnim);
                SaveBool("M_SkipKillAnimation", skipKillAnimation);
                SaveBool("M_SpoofMenuEnabled", SpoofMenuEnabled);
                PlayerPrefs.SetString("M_CustomSpoofRpcInput", customSpoofRpcInput ?? "89");
                SaveBool("M_NoClip", noClip);
                SaveBool("M_TpToCursor", tpToCursor);
                SaveBool("M_DragToCursor", dragToCursor);
                SaveBool("M_AutoFollowCursor", autoFollowCursor);
                SaveBool("M_Freecam", freecam);
                SaveBool("M_CameraZoom", cameraZoom);
                SaveBool("M_ShowRadar", showRadar);
                SaveBool("M_ShowRadarDeadBodies", showRadarDeadBodies);
                SaveBool("M_ShowRadarGhosts", showRadarGhosts);
                SaveBool("M_RadarRightClickTp", radarRightClickTp);
                SaveBool("M_HideRadarInMeeting", hideRadarInMeeting);
                SaveBool("M_RadarDrawIcons", radarDrawIcons);
                SaveBool("M_LockRadar", lockRadar);
                SaveBool("M_RadarBorder", radarBorder);
                PlayerPrefs.SetFloat("M_RadarScale", Mathf.Clamp(radarScale, 0.65f, 1.6f));
                PlayerPrefs.SetFloat("M_RadarAlpha", Mathf.Clamp(radarAlpha, 0.2f, 1f));
                PlayerPrefs.SetFloat("M_RadarX", radarRect.x);
                PlayerPrefs.SetFloat("M_RadarY", radarRect.y);
                SaveBool("M_ShowReplay", showReplay);
                SaveBool("M_ReplayOnlyLastSeconds", replayOnlyLastSeconds);
                SaveBool("M_ReplayDrawIcons", replayDrawIcons);
                PlayerPrefs.SetFloat("M_ReplaySeconds", Mathf.Clamp(replaySeconds, 5f, 180f));
                PlayerPrefs.SetFloat("M_ReplayX", replayRect.x);
                PlayerPrefs.SetFloat("M_ReplayY", replayRect.y);
                SaveBool("M_RevealVotes", RevealVotesEnabled);
                SaveBool("M_NoTaskMode", noTaskMode);
                SaveBool("M_NoMapCooldowns", noMapCooldowns);
                SaveBool("M_UnlockVents", unlockVents);
                SaveBool("M_WalkInVents", walkInVents);
                SaveBool("M_AutoRepairSabotage", autoRepairSabotage);
                SaveBool("M_AutoBreakSabotage", autoBreakSabotage);
                SaveBool("M_AllowTasksAsImpostor", allowTasksAsImpostor);
                SaveBool("M_HostAutoKillRandom", hostAutoKillRandom);
                SaveBool("M_HostAutoKillTarget", hostAutoKillTarget);
                PlayerPrefs.SetInt("M_HostAutoKillTargetId", hostAutoKillTargetId);
                SaveBool("M_BugRoomAutoAngel", bugRoomAutoAngel);
                SaveBool("M_BugRoomAutoKillShield", bugRoomAutoKillShield);
                SaveBool("M_BugRoomTimedAutoRun", bugRoomTimedAutoRun);
                PlayerPrefs.SetInt("M_BugRoomTimedAutoRunMinutes", Mathf.Clamp(bugRoomTimedAutoRunMinutes, 1, 60));
                SaveBool("M_KillWhileVanishedHostOnly", killWhileVanishedHostOnly);
                SaveBool("M_DisableEndGameSafeMode", disableEndGameSafeMode);
                SaveBool("M_DisableMapSafeMode", disableMapSafeMode);
                SaveBool("M_RoleBuffImmortality", roleBuffImmortality);
                SaveBool("M_NeverEndGame", neverEndGame);
                SaveBool("M_ChatAsEveryone", autoChatEveryone);
                SaveBool("M_RemovePenalty", removePenalty);
                SaveBool("M_GuestExtraFeatures", guestExtraFeatures);
                SaveBool("M_BypassAgeRestrictions", bypassAgeRestrictions);
                SaveBool("M_AlwaysShowLobbyTimer", alwaysShowLobbyTimer);
                SaveBool("M_AutoBanEnabled", autoBanEnabled);
                SaveBool("M_AllowDuplicateColors", allowDuplicateColors);
                SaveBool("M_BlockSpoofRPC", blockSpoofRPC);
                SaveBool("M_AutoBanPlatformSpoof", autoBanPlatformSpoof);
                SaveBool("M_BanCustomPlatformsFromTxt", banCustomPlatformsFromTxt);
                SaveBool("M_AutoKickLowLevel", autoKickLowLevelEnabled);
                PlayerPrefs.SetInt("M_AutoKickMinLevel", Mathf.Clamp(autoKickMinLevel, 1, 300));
                SaveBool("M_BlockSabotageRPC", blockSabotageRPC);
                PlayerPrefs.SetInt("M_PunishmentMode", punishmentMode);
                SaveBool("M_BlockGameRpcInLobby", blockGameRpcInLobby);
                SaveBool("M_BlockChatFloodRpc", blockChatFloodRpc);
                SaveBool("M_BlockMeetingFloodRpc", blockMeetingFloodRpc);
                SaveBool("M_OverflowProtection", overflowProtection);
                SaveBool("M_BlockVentKickExploit", blockVentKickExploit);
                SaveBool("M_BlockServerTeleports", blockServerTeleports);
                SaveBool("M_UnfixableLights", unfixableLights);
                SaveBool("M_PasosLimit", enablePasosLimit);
                SaveBool("M_AntiPasosLocalBan", enableLocalPasosBan);
                SaveBool("M_AntiPasosHostBan", enableHostPasosBan);
                SaveBool("M_MalformedPacketGuard", enableMalformedPacketGuard);
                SaveBool("M_BanMalformedPacketSender", banMalformedPacketSender);
                SaveBool("M_QuickChatEmptyGuard", enableQuickChatEmptyGuard);
                SaveBool("M_BanQuickChatEmptySpammer", banQuickChatEmptySpammer);
                SaveBool("M_UnownedSpawnGuard", enableUnownedSpawnGuard);
                SaveBool("M_AutoHostEnabled", AutoHostEnabled);
                SaveBool("M_AutoHostShieldBreakEnabled", AutoHostShieldBreakEnabled);
                SaveBool("M_AutoReturnLobbyAfterMatch", AutoReturnLobbyAfterMatch);
                SaveBool("M_AutoHostNotifications", AutoHostNotifications);
                SaveBool("M_AutoHostForceLastMinute", AutoHostForceLastMinute);
                SaveBool("M_AutoHostWaitLoadedPlayers", AutoHostWaitLoadedPlayers);
                SaveBool("M_AutoHostCancelBelowMin", AutoHostCancelBelowMin);
                SaveBool("M_AutoHostInstantStart", AutoHostInstantStart);
                SaveBool("M_AutoHostAutoRunEnabled", AutoHostAutoRunEnabled);
                SaveBool("M_BugroomScoutEnabled", BugroomScoutEnabled);
                SaveBool("M_AutoGhostAfterStart", autoGhostAfterStart);
                PlayerPrefs.SetInt("M_AutoHostMinPlayers", AutoHostMinPlayers);
                PlayerPrefs.SetFloat("M_AutoHostStartDelaySeconds", AutoHostStartDelaySeconds);
                PlayerPrefs.SetInt("M_AutoHostFastStartPlayers", AutoHostFastStartPlayers);
                PlayerPrefs.SetFloat("M_AutoHostFastStartDelaySeconds", AutoHostFastStartDelaySeconds);
                PlayerPrefs.SetFloat("M_WalkSpeed", walkSpeed);
                PlayerPrefs.SetFloat("M_EngineSpeed", engineSpeed);

                Plugin.MenuConfig.Save();

                PlayerPrefs.SetString("M_SpoofName", customNameInput);
                for (int i = 0; i < favoriteOutfitSlots.Length; i++)
                    PlayerPrefs.SetString($"M_FavoriteOutfit_{i}", favoriteOutfitSlots[i] ?? string.Empty);
                PlayerPrefs.Save();
            }
            catch { }
        }

private void DrawAutoHostTab()
        {
            GUILayout.BeginVertical(menuCardStyle);
            DrawMenuSectionHeader(L("AUTO HOST SYSTEM", "СИСТЕМА АВТО-ХОСТА"));

            var snapshot = ElysiumAutoHostService.GetStatusSnapshot();
            GUILayout.Label($"<color=#aaaaaa>{L("Status:", "Статус:")}</color> <color=#FFAC1C>{snapshot.State}</color>", new GUIStyle(GUI.skin.label) { richText = true, fontSize = 13 });
            GUILayout.Space(10);

            AutoHostEnabled = DrawToggle(AutoHostEnabled, L("Enable Auto Host", "Включить Авто-Хост"), 250);
            GUILayout.Space(5);
            AutoHostShieldBreakEnabled = DrawToggle(AutoHostShieldBreakEnabled, L("Auto Shield Break (Host)", "Авто-ломать щит (хост)"), 250);
            GUILayout.Space(5);
            AutoReturnLobbyAfterMatch = DrawToggle(AutoReturnLobbyAfterMatch, L("Auto Return To Lobby", "Авто-возврат в лобби"), 250);
            GUILayout.Space(5);
            AutoHostNotifications = DrawToggle(AutoHostNotifications, L("Show Notifications", "Показывать уведомления"), 250);
            GUILayout.Space(5);
            AutoHostWaitLoadedPlayers = DrawToggle(AutoHostWaitLoadedPlayers, L("Wait For Players To Load", "Ждать прогрузки игроков"), 250);
            GUILayout.Space(5);
            AutoHostCancelBelowMin = DrawToggle(AutoHostCancelBelowMin, L("Cancel Countdown If Player Leaves", "Отмена отсчета, если игрок вышел"), 250);
            GUILayout.Space(5);
            AutoHostInstantStart = DrawToggle(AutoHostInstantStart, L("Instant Start (No 5s Wait)", "Мгновенный старт (Без 5с)"), 250);
            GUILayout.Space(5);
            autoGhostAfterStart = DrawToggle(autoGhostAfterStart, L("Auto Ghost After Start", "Авто-призрак после старта"), 250);
            GUILayout.Space(5);
            AutoHostForceLastMinute = DrawToggle(AutoHostForceLastMinute, L("Force Start Last Minute", "Форс-старт на последней минуте"), 250);

            GUILayout.Space(15);

            string hexColor = GetMenuAccentHex();
            GUIStyle sliderLabelStyle = new GUIStyle(toggleLabelStyle) { richText = true };

            GUILayout.BeginHorizontal();
            GUILayout.Label($"{L("Min Players:", "Мин. игроков:")} <color=#{hexColor}>{AutoHostMinPlayers}</color>", sliderLabelStyle, GUILayout.Width(175));
            AutoHostMinPlayers = (int)GUILayout.HorizontalSlider(AutoHostMinPlayers, 1f, 15f, sliderStyle, sliderThumbStyle, GUILayout.Width(335));
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            GUILayout.Label($"{L("Start Delay:", "Задержка старта:")} <color=#{hexColor}>{Mathf.Round(AutoHostStartDelaySeconds)}s</color>", sliderLabelStyle, GUILayout.Width(175));
            AutoHostStartDelaySeconds = GUILayout.HorizontalSlider(AutoHostStartDelaySeconds, 0f, 180f, sliderStyle, sliderThumbStyle, GUILayout.Width(335));
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            GUILayout.Label($"{L("Fast Start Players:", "Игроков для фаст-старта:")} <color=#{hexColor}>{AutoHostFastStartPlayers}</color>", sliderLabelStyle, GUILayout.Width(175));
            AutoHostFastStartPlayers = (int)GUILayout.HorizontalSlider(AutoHostFastStartPlayers, 0f, 15f, sliderStyle, sliderThumbStyle, GUILayout.Width(335));
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            GUILayout.Label($"{L("Fast Start Delay:", "Задержка фаст-старта:")} <color=#{hexColor}>{Mathf.Round(AutoHostFastStartDelaySeconds)}s</color>", sliderLabelStyle, GUILayout.Width(175));
            AutoHostFastStartDelaySeconds = GUILayout.HorizontalSlider(AutoHostFastStartDelaySeconds, 0f, 60f, sliderStyle, sliderThumbStyle, GUILayout.Width(335));
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

private void DrawBugRoomTab()
        {
            GUILayout.BeginVertical(menuCardStyle);
            DrawMenuSectionHeader("BUG ROOM");

            neverEndGame = DrawToggle(neverEndGame, L("Unlimited Game", "Бесконечная игра"), 280);
            GUILayout.Space(5);
            AutoHostAutoRunEnabled = DrawToggle(AutoHostAutoRunEnabled, L("Auto Run 1.75s + Imp Win", "Авто-прогон 1.75с + победа предателей"), 280);
            GUILayout.Space(5);
            DrawBugRoomTimedAutoRun();
            GUILayout.Space(8);
            hostAutoKillRandom = DrawToggle(hostAutoKillRandom, "Kill Random Target", 280);
            GUILayout.Space(5);
            hostAutoKillTarget = DrawToggle(hostAutoKillTarget, "Auto Kill Target", 280);
            GUILayout.Space(5);
            DrawBugRoomKillTargetPicker();
            GUILayout.Space(8);
            bugRoomAutoAngel = DrawToggle(bugRoomAutoAngel, "Auto Angel 0.10", 280);
            GUILayout.Space(5);
            bugRoomAutoKillShield = DrawToggle(bugRoomAutoKillShield, "Auto Kill Angel Shield 0.13", 280);

            GUILayout.Space(12);
            DrawMenuSectionHeader("BUGROOM SCOUT");
            bool oldScout = BugroomScoutEnabled;
            BugroomScoutEnabled = DrawToggle(BugroomScoutEnabled, "Auto Create + Find TXT", 280);
            if (oldScout != BugroomScoutEnabled)
            {
                settingsDirty = true;
                ElysiumBugroomScoutService.ForceReload();
            }

            var scout = ElysiumBugroomScoutService.GetStatusSnapshot();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Create / Reload TXT", btnStyle, GUILayout.Width(170), GUILayout.Height(25)))
            {
                ElysiumBugroomScoutService.ForceReload();
                GUIUtility.systemCopyBuffer = scout.FilePath;
                ShowNotification("<color=#00FFAA>[BUGROOM SCOUT]</color> TXT path copied.");
            }
            GUILayout.Label($"Targets: <color=#{GetMenuAccentHex()}>{scout.TargetCount}</color> | {scout.State}", new GUIStyle(GUI.skin.label) { richText = true, fontSize = 12 }, GUILayout.Height(25));
            GUILayout.EndHorizontal();

            string code = string.IsNullOrWhiteSpace(scout.CurrentCode) ? "-" : scout.CurrentCode;
            string suffix = string.IsNullOrWhiteSpace(scout.CurrentSuffix) ? "-" : scout.CurrentSuffix;
            GUILayout.Label($"TXT: {scout.FilePath}", new GUIStyle(GUI.skin.label) { richText = true, fontSize = 11, wordWrap = true });
            GUILayout.Label($"Room: <color=#{GetMenuAccentHex()}>{code}</color> | suffix: <color=#{GetMenuAccentHex()}>{suffix}</color>", new GUIStyle(GUI.skin.label) { richText = true, fontSize = 12 });

            GUILayout.EndVertical();
        }

private void DrawBugRoomTimedAutoRun()
        {
            GUILayout.BeginHorizontal(GUILayout.Width(330), GUILayout.Height(24));
            bugRoomTimedAutoRun = DrawToggle(bugRoomTimedAutoRun, "Timed Auto Run", 150);
            GUILayout.Space(8);

            if (!isEditingBugRoomTimedAutoRun) bugRoomTimedAutoRunInput = bugRoomTimedAutoRunMinutes.ToString();
            if (DrawBugRoomMinuteInput())
            {
                isEditingBugRoomTimedAutoRun = true;
                bugRoomTimedAutoRunInput = string.Empty;
            }
            GUILayout.Label("min", new GUIStyle(toggleLabelStyle) { fontSize = 12 }, GUILayout.Width(32), GUILayout.Height(22));
            GUILayout.EndHorizontal();

            GUILayout.Label(GetBugRoomTimedAutoRunText(), new GUIStyle(GUI.skin.label) { richText = true, fontSize = 11 });
        }

private bool DrawBugRoomMinuteInput()
        {
            GUIStyle style = new GUIStyle(isEditingBugRoomTimedAutoRun ? activeTabStyle : inputBlockStyle);
            style.alignment = TextAnchor.MiddleCenter;
            style.clipping = TextClipping.Clip;
            style.wordWrap = false;
            style.padding = CreateRectOffset(4, 4, 0, 0);

            Rect rect = GUILayoutUtility.GetRect(45f, 22f, GUILayout.Width(45f), GUILayout.Height(22f));
            string text = string.IsNullOrEmpty(bugRoomTimedAutoRunInput) ? (isEditingBugRoomTimedAutoRun ? "|" : bugRoomTimedAutoRunMinutes.ToString()) : bugRoomTimedAutoRunInput;
            return GUI.Button(rect, text, style);
        }

private string GetBugRoomTimedAutoRunText()
        {
            if (!bugRoomTimedAutoRun) return "<color=#777777>Timer off</color>";
            if (AutoHostAutoRunEnabled) return "<color=#66FF99>Auto Run already ON</color>";
            if (!IsBugRoomTimedAutoRunInGame()) return "<color=#aaaaaa>Waiting game</color>";

            float left = Mathf.Max(0f, bugRoomTimedAutoRunMinutes * 60f - bugRoomTimedAutoRunTimer);
            return $"<color=#{GetMenuAccentHex()}>Timer:</color> {Mathf.FloorToInt(left / 60f):00}:{Mathf.FloorToInt(left % 60f):00}";
        }

private void DrawBugRoomKillTargetPicker()
        {
            List<PlayerControl> plrs = GetBugRoomKillTargets();
            if (plrs.Count == 0)
            {
                GUILayout.Label("<color=#aaaaaa>Target: none</color>", new GUIStyle(GUI.skin.label) { richText = true, fontSize = 12 });
                return;
            }

            int idx = plrs.FindIndex(p => p != null && p.PlayerId == hostAutoKillTargetId);
            if (idx < 0)
            {
                idx = 0;
                hostAutoKillTargetId = plrs[0].PlayerId;
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("<", btnStyle, GUILayout.Width(28), GUILayout.Height(24)))
            {
                idx--;
                if (idx < 0) idx = plrs.Count - 1;
                hostAutoKillTargetId = plrs[idx].PlayerId;
                settingsDirty = true;
            }

            PlayerControl target = plrs[idx];
            string nm = target.Data != null && !string.IsNullOrWhiteSpace(target.Data.PlayerName) ? target.Data.PlayerName : $"Player {target.PlayerId}";
            if (nm.Length > 18) nm = nm.Substring(0, 18) + "..";
            if (target.Data != null && target.Data.IsDead) nm += " [dead]";

            GUIStyle mid = new GUIStyle(btnStyle);
            mid.normal.background = null;
            mid.hover.background = null;
            mid.normal.textColor = GetMenuAccentColor();
            mid.fontStyle = FontStyle.Bold;
            mid.alignment = TextAnchor.MiddleCenter;
            GUILayout.Label(nm, mid, GUILayout.Height(24), GUILayout.ExpandWidth(true));

            if (GUILayout.Button(">", btnStyle, GUILayout.Width(28), GUILayout.Height(24)))
            {
                idx++;
                if (idx >= plrs.Count) idx = 0;
                hostAutoKillTargetId = plrs[idx].PlayerId;
                settingsDirty = true;
            }
            GUILayout.EndHorizontal();
        }

private static List<PlayerControl> GetBugRoomKillTargets()
        {
            List<PlayerControl> plrs = new List<PlayerControl>();
            try
            {
                if (PlayerControl.AllPlayerControls == null) return plrs;
                PlayerControl local = PlayerControl.LocalPlayer;
                foreach (PlayerControl pc in PlayerControl.AllPlayerControls)
                {
                    if (pc == null || pc == local || pc.Data == null) continue;
                    if (pc.Data.Disconnected || pc.PlayerId >= 100) continue;
                    plrs.Add(pc);
                }
                plrs.Sort((a, b) => a.PlayerId.CompareTo(b.PlayerId));
            }
            catch { }
            return plrs;
        }
}
}
