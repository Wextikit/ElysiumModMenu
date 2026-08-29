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
                    catch (global::System.Exception __elysiumCaught440) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught440); }
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
            KillAll(0);
        }

private void KillAll(int mode)
        {
            if (PlayerControl.LocalPlayer == null || PlayerControl.AllPlayerControls == null) return;
            if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
            {
                foreach (var player in PlayerControl.AllPlayerControls.ToArray())
                {
                    if (player == null || player == PlayerControl.LocalPlayer || player.Data == null || player.Data.IsDead || player.Data.Disconnected) continue;

                    bool isImpostor = RoleManager.IsImpostorRole(player.Data.RoleType) ||
                                      (player.Data.Role != null && player.Data.Role.IsImpostor);
                    if (mode == 1 && isImpostor) continue;
                    if (mode == 2 && !isImpostor) continue;

                    TryHostElysiumMurderPlayer(player);
                }
                return;
            }

            Vector3 op = PlayerControl.LocalPlayer.transform.position;
            var targets = PlayerControl.AllPlayerControls.ToArray();
            foreach (var t in targets)
            {
                if (t != null && t != PlayerControl.LocalPlayer && t.Data != null && !t.Data.IsDead && !t.Data.Disconnected)
                {
                    bool isImpostor = RoleManager.IsImpostorRole(t.Data.RoleType) ||
                                      (t.Data.Role != null && t.Data.Role.IsImpostor);
                    if (mode == 1 && isImpostor) continue;
                    if (mode == 2 && !isImpostor) continue;

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
                        catch (global::System.Exception __elysiumCaught441) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught441); }
                    }
                }
                catch (global::System.Exception __elysiumCaught442) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught442); }

                if (despawned == 0 && LobbyBehaviour.Instance != null)
                    LobbyBehaviour.Instance.Cast<InnerNetObject>().Despawn();

                ResetLobbyMapTransientState();
            }
            catch (global::System.Exception __elysiumCaught443) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught443); }
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
            catch (global::System.Exception __elysiumCaught444) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught444); }
        }

private static void ResetLobbyMapTransientState()
        {
            try { fortegreenTimer.Clear(); } catch (global::System.Exception __elysiumCaught445) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught445); }
            try { lastKillTimestamps.Clear(); } catch (global::System.Exception __elysiumCaught446) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught446); }
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
            catch (global::System.Exception __elysiumCaught447) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught447); }
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
            catch (global::System.Exception __elysiumCaught448) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught448); }
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
            catch (global::System.Exception __elysiumCaught449) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught449); }
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
            catch (global::System.Exception __elysiumCaught450) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught450); }
        }

        private static void RestoreLocalFriendCodeSelf()
        {
            try
            {
                originalLocalFriendCode = null;
            }
            catch (global::System.Exception __elysiumCaught451) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught451); }
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
            catch (global::System.Exception __elysiumCaught452) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught452); }

            try
            {
                FieldInfo field = type.GetField(memberName, flags);
                if (field != null) field.SetValue(target, value);
            }
            catch (global::System.Exception __elysiumCaught453) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught453); }
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
            catch (global::System.Exception __elysiumCaught454) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught454); }
        }

        public static bool showWatermark = true;
        public static bool showWatermarkInfo = true;

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

private static void SaveKeybinds()
        {
            try
            {
                if (menuToggleKey == KeyCode.None) menuToggleKey = KeyCode.Insert;

                PlayerPrefs.SetInt("M_MenuToggleKey", (int)menuToggleKey);
                PlayerPrefs.SetInt("M_BndMagnet", (int)bindMagnetCursor);
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
                PlayerPrefs.SetInt("M_BndToggleRadar", (int)bindToggleRadar);
                PlayerPrefs.SetInt("M_BndToggleReplay", (int)bindToggleReplay);
                PlayerPrefs.SetInt("M_BndToggleReplayConsole", (int)bindToggleReplayConsole);
                PlayerPrefs.SetInt("M_BndToggleRadarIcons", (int)bindToggleRadarIcons);
                PlayerPrefs.SetInt("M_BndToggleReplayIcons", (int)bindToggleReplayIcons);
                PlayerPrefs.SetInt("M_BndToggleAlwaysChat", (int)bindToggleAlwaysChat);
                SyncKeybindDictionary();
                PlayerPrefs.Save();
            }
            catch (global::System.Exception __elysiumCaught455) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught455); }

            try
            {
                Plugin.MenuKeybind.Value = menuToggleKey;
                Plugin.MenuConfig.Save();
            }
            catch (global::System.Exception __elysiumCaught456) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught456); }
        }

private void SaveConfig()
        {
            try
            {
                Plugin.SpoofedLevel.Value = spoofLevelString;
                Plugin.EnableLevelSpoofConfig.Value = enableLevelSpoof;
                SaveBool("M_EnableLevelSpoof", enableLevelSpoof);
                Plugin.EnableFriendCodeSpoofConfig.Value = enableFriendCodeSpoof;
                Plugin.SpoofFriendCodeConfig.Value = spoofFriendCodeInput;
                Plugin.EnablePlatformSpoof.Value = enablePlatformSpoof;
                Plugin.AutoBanBrokenFriendCodeConfig.Value = autoBanBrokenFriendCode;
                Plugin.PlatformIndex.Value = currentPlatformIndex;
                Plugin.ShowWatermarkConfig.Value = showWatermark;
                SaveBool("M_ShowWatermarkInfo", showWatermarkInfo);
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
                SaveBool("M_WhiteTheme", whiteMenuTheme);
                SaveBool("M_RgbMenuText", rgbMenuText);
                SaveBool("M_BoldMenuText", boldMenuText);
                PlayerPrefs.SetInt("M_MenuLanguageIndex", currentMenuLanguageIndex);
                SaveBool("M_MenuLanguageV2", true);
                PlayerPrefs.SetInt("M_MenuProfileSlot", Mathf.Clamp(selectedMenuProfileIndex, 0, menuProfileCount - 1));
                SaveBool("M_LimitFps", limitFps);
                PlayerPrefs.SetInt("M_FpsLimit", fpsLimit);
                SaveBool("M_DetailedLogsEnabled", detailedLogsEnabled);
                SaveBool("M_EnableBackground", enableBackground);
                SaveBool("M_EnableMenuCharacter", enableMenuCharacter);
                SaveBool("M_BlockGameClicks", blockGameClicks);
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
                PlayerPrefs.SetFloat("M_MenuScale", Mathf.Clamp(menuScale, minMenuScale, maxMenuScale));
                SaveBool("M_EnableMenuScaleInput", enableMenuScaleInput);
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
                SaveBool("M_LocalAlwaysRed", localAlwaysRed);
                SaveBool("M_LocalFortegreen", localFortegreen);
                SaveBool("M_LocalSnipeColor", localSnipeColor);
                PlayerPrefs.SetInt("M_LocalSnipeColorId", Mathf.Clamp(localSnipeColorId, 0, 17));
                PlayerPrefs.SetString("M_LocalFakeFC", localFriendCodeInput);
                SaveBool("M_DeviceIdSpoof", enableDeviceIdSpoof);
                PlayerPrefs.SetString("M_DeviceId", spoofedDeviceId ?? "");
                SaveBool("M_SpoofAprilDate", spoofAprilFoolsDate);

                SaveBool("M_ShowPlayerInfo", showPlayerInfo);
                SaveBool("M_ShowEspBoxes", showEspBoxes);
                SaveBool("M_EspShimmerMode", espShimmerMode);
                SaveBool("M_ShowTaskArrows", showTaskArrows);
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
                SaveBool("M_ChatNoCooldown", chatNoCooldown);
                SaveBool("M_AllowLinksAndSymbols", allowLinksAndSymbols);
                SaveBool("M_EnableChatHistory", enableChatHistory);
                PlayerPrefs.SetInt("M_ChatHistoryLimit", Mathf.Clamp(chatHistoryLimit, 5, 200));
                SaveBool("M_EnableClipboard", enableClipboard);
                SaveBool("M_EnableChatBubbleCopy", enableChatBubbleCopy);
                SaveBool("M_EnableChatNickCopy", enableChatNickCopy);
                SaveBool("M_EnableChatLog", enableChatLog);
                SaveBool("M_EnableColorCommand", enableColorCommand);
                SaveBool("M_BlockRainbowChat", blockRainbowChat);
                PlayerPrefs.SetFloat("M_AutoChatEveryoneDelay", Mathf.Clamp(autoChatEveryoneDelay, 0f, 10f));
                SaveBool("M_BlockFortegreenChat", blockFortegreenChat);
                SaveBool("M_SkipRoleIntroAnim", skipRoleIntroAnim);
                SaveBool("M_SkipKillAnimation", skipKillAnimation);
                SaveBool("M_SpoofMenuEnabled", SpoofMenuEnabled);
                PlayerPrefs.SetString("M_CustomSpoofRpcInput", customSpoofRpcInput ?? "89");
                SaveBool("M_NoClip", noClip);
                SaveBool("M_TpToCursor", tpToCursor);
                SaveBool("M_DragToCursor", dragToCursor);
                SaveBool("M_AutoVentAfterKill", autoVentAfterKill);
                SaveBool("M_ImpTrap", impTrap);
                SaveBool("M_HnsTaskDrain", hnsTaskDrain);
                PlayerPrefs.SetFloat("M_HnsTaskDrainStep", Mathf.Clamp(hnsTaskDrainStep, 0.15f, 1.5f));
                SaveBool("M_AutoTasksEnabled", autoTasksEnabled);
                PlayerPrefs.SetFloat("M_AutoTasksDelay", Mathf.Clamp(autoTasksDelay, 0.8f, 6f));
                SaveBool("M_AutoFollowCursor", autoFollowCursor);
                SaveBool("M_Freecam", freecam);
                SaveBool("M_CameraZoom", cameraZoom);
                SaveBool("M_ShowRadar", showRadar);
                SaveBool("M_RealisticRadar", realisticRadar);
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
                SaveBool("M_ShowReplayLog", showReplayLog);
                SaveBool("M_ReplayRecordEnabled", replayRecordEnabled);
                SaveBool("M_ReplayOverlayOnRadar", replayOverlayOnRadar);
                SaveBool("M_ReplayHideRadarLive", replayHideRadarLive);
                SaveBool("M_ReplayOnlyLastSeconds", replayOnlyLastSeconds);
                SaveBool("M_ReplayDrawIcons", replayDrawIcons);
                PlayerPrefs.SetInt("M_ReplayFilterMask", replayFilterMask & 0x3FF);
                PlayerPrefs.SetFloat("M_ReplaySeconds", Mathf.Clamp(replaySeconds, 5f, 900f));
                PlayerPrefs.SetFloat("M_ReplayX", replayRect.x);
                PlayerPrefs.SetFloat("M_ReplayY", replayRect.y);
                PlayerPrefs.SetFloat("M_ReplayW", replayRect.width);
                PlayerPrefs.SetFloat("M_ReplayH", replayRect.height);
                PlayerPrefs.SetFloat("M_ReplayLogX", replayLogRect.x);
                PlayerPrefs.SetFloat("M_ReplayLogY", replayLogRect.y);
                PlayerPrefs.SetFloat("M_ReplayLogW", replayLogRect.width);
                PlayerPrefs.SetFloat("M_ReplayLogH", replayLogRect.height);
                SaveBool("M_RevealVotes", RevealVotesEnabled);
                SaveBool("M_NoTaskMode", noTaskMode);
                SaveBool("M_NoMapCooldowns", noMapCooldowns);
                SaveBool("M_UnlockVents", unlockVents);
                SaveBool("M_WalkInVents", walkInVents);
                SaveBool("M_AutoRepairSabotage", autoRepairSabotage);
                SaveBool("M_SpamMeetings", spamMeetings);
                SaveBool("M_AutoBreakSabotage", autoBreakSabotage);
                SaveBool("M_AllowTasksAsImpostor", allowTasksAsImpostor);
                SaveBool("M_HostAutoKillRandom", hostAutoKillRandom);
                SaveBool("M_HostAutoKillTarget", hostAutoKillTarget);
                PlayerPrefs.SetInt("M_HostAutoKillTargetId", hostAutoKillTargetId);
                PlayerPrefs.SetInt("M_HostAutoKillRate", Mathf.Clamp(hostAutoKillRate, 1, 35));
                SaveBool("M_BugRoomAutoAngel", bugRoomAutoAngel);
                PlayerPrefs.SetFloat("M_BugRoomAutoAngelIntervalSeconds", Mathf.Clamp(bugRoomAutoAngelIntervalSeconds, 0.001f, 0.50f));
                SaveBool("M_BugRoomAutoKillShield", bugRoomAutoKillShield);
                SaveBool("M_BugRoomImpMeeting", bugRoomImpMeeting);
                SaveBool("M_GlitchRoomBypassShield", glitchRoomBypassShield);
                SaveBool("M_GlitchRoomGodMode", glitchRoomGodMode);
                SaveBool("M_GlitchRoomGodModeAll", glitchRoomGodModeAll);
                SaveBool("M_BugRoomTimedAutoRun", bugRoomTimedAutoRun);
                PlayerPrefs.SetInt("M_BugRoomTimedAutoRunMinutes", Mathf.Clamp(bugRoomTimedAutoRunMinutes, 1, 60));
                SaveBool("M_BugRoomAutoRejoin", bugRoomAutoRejoin);
                SaveBool("M_KillWhileVanishedHostOnly", killWhileVanishedHostOnly);
                SaveBool("M_DisableEndGameSafeMode", disableEndGameSafeMode);
                SaveBool("M_DisableMapSafeMode", disableMapSafeMode);
                SaveBool("M_NoAbilityCooldown", noAbilityCooldown);
                SaveBool("M_RoleBuffImmortality", roleBuffImmortality);
                SaveBool("M_NeverEndGame", neverEndGame);
                SaveBool("M_ChatAsEveryone", autoChatEveryone);
                SaveBool("M_RemovePenalty", removePenalty);
                SaveBool("M_SpoofGuestAccount", spoofGuestAccount);
                SaveBool("M_GuestExtraFeatures", guestExtraFeatures);
                SaveBool("M_BypassAgeRestrictions", bypassAgeRestrictions);
                SaveBool("M_AlwaysShowLobbyTimer", alwaysShowLobbyTimer);
                SaveBool("M_RainbowLobbyTimer", rainbowLobbyTimer);
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
                SaveBool("M_AutoClearClonesBeforeGame", NetworkedClones.AutoClearBeforeGame);
                PlayerPrefs.SetInt("M_CloneFormationIdx", cloneFormationIdx);
                PlayerPrefs.SetInt("M_CloneFormationCount", cloneFormationCount);
                PlayerPrefs.SetFloat("M_CloneFormationWidth", cloneFormationWidth);
                SaveBool("M_AnimShields", AnimShieldsEnabled);
                SaveBool("M_AnimAsteroids", AnimAsteroidsEnabled);
                SaveBool("M_AnimCamsInUse", AnimCamsInUseEnabled);
                SaveBool("M_AnimEmptyGarbage", AnimEmptyGarbageEnabled);
                SaveBool("M_SkipShhhAnim", skipShhhAnim);
                SaveBool("M_ManualMapSpawn", isManualMapSpawn);
                SaveBool("M_FlipSkeld", flipSkeld);
                SaveBool("M_SeePlayersInVent", SeePlayersInVent);
                SaveBool("M_BanBotsEnabled", banBotsEnabled);
                SaveBool("M_OldAntiCheatVersion", oldAntiCheatVersion);
                SaveBool("M_EnableSpellCheck", enableSpellCheck);
                SaveBool("M_PreGameRoleForce", enablePreGameRoleForce);
                SaveBool("M_AutoTwoImpostors", autoTwoImpostors);
                SaveBool("M_ForceFourImpostors", forceFourImpostors);
                SaveBool("M_CustomChatSpam", customChatSpamEnabled);
                PlayerPrefs.SetFloat("M_AutoHostAutoRunDelaySeconds", Mathf.Clamp(AutoHostAutoRunDelaySeconds, 0.25f, 10f));
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
            catch (global::System.Exception __elysiumCaught457) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught457); }
        }

private void DrawAutoHostTab()
        {
            float contentWidth = Mathf.Max(96f, GetMenuWorkWidth(120f, 760f) - 8f);
            float cardPaddingWidth = menuCardStyle != null && menuCardStyle.padding != null
                ? menuCardStyle.padding.left + menuCardStyle.padding.right
                : 28f;
            float innerWidth = Mathf.Max(68f, contentWidth - cardPaddingWidth);
            int toggleWidth = Mathf.RoundToInt(Mathf.Min(250f, innerWidth));

            GUILayout.BeginVertical(menuCardStyle, GUILayout.Width(contentWidth));
            DrawMenuSectionHeader(L("AUTO HOST SYSTEM", "СИСТЕМА АВТО-ХОСТА"));

            var snapshot = ElysiumAutoHostService.GetStatusSnapshot();
            GUILayout.Label($"<color=#aaaaaa>{L("Status:", "Статус:")}</color> <color=#FFAC1C>{snapshot.State}</color>", historyHeaderStyle);
            GUILayout.Space(10);

            AutoHostEnabled = DrawToggle(AutoHostEnabled, L("Enable Auto Host", "Включить Авто-Хост"), toggleWidth);
            GUILayout.Space(5);
            AutoHostShieldBreakEnabled = DrawToggle(AutoHostShieldBreakEnabled, L("Auto Shield Break (Host)", "Авто-ломать щит (хост)"), toggleWidth);
            GUILayout.Space(5);
            AutoReturnLobbyAfterMatch = DrawToggle(AutoReturnLobbyAfterMatch, L("Auto Return To Lobby", "Авто-возврат в лобби"), toggleWidth);
            GUILayout.Space(5);
            AutoHostNotifications = DrawToggle(AutoHostNotifications, L("Show Notifications", "Показывать уведомления"), toggleWidth);
            GUILayout.Space(5);
            AutoHostWaitLoadedPlayers = DrawToggle(AutoHostWaitLoadedPlayers, L("Wait For Players To Load", "Ждать прогрузки игроков"), toggleWidth);
            GUILayout.Space(5);
            AutoHostCancelBelowMin = DrawToggle(AutoHostCancelBelowMin, L("Cancel Countdown If Player Leaves", "Отмена отсчета, если игрок вышел"), toggleWidth);
            GUILayout.Space(5);
            AutoHostInstantStart = DrawToggle(AutoHostInstantStart, L("Instant Start (No 5s Wait)", "Мгновенный старт (Без 5с)"), toggleWidth);
            GUILayout.Space(5);
            autoGhostAfterStart = DrawToggle(autoGhostAfterStart, L("Auto Ghost After Start", "Авто-призрак после старта"), toggleWidth);
            GUILayout.Space(5);
            AutoHostForceLastMinute = DrawToggle(AutoHostForceLastMinute, L("Force Start Last Minute", "Форс-старт на последней минуте"), toggleWidth);

            GUILayout.Space(15);

            string hexColor = GetMenuAccentHex();
            GUIStyle sliderLabelStyle = toggleLabelStyle;
            float sliderLabelWidth = Mathf.Min(175f, Mathf.Max(60f, innerWidth * 0.34f));
            sliderLabelWidth = Mathf.Min(sliderLabelWidth, innerWidth * 0.48f);
            float sliderWidth = Mathf.Max(24f, innerWidth - sliderLabelWidth - 8f);

            GUILayout.BeginHorizontal(GUILayout.Width(innerWidth));
            GUILayout.Label($"{L("Min Players:", "Мин. игроков:")} <color=#{hexColor}>{AutoHostMinPlayers}</color>", sliderLabelStyle, GUILayout.Width(sliderLabelWidth));
            AutoHostMinPlayers = (int)GUILayout.HorizontalSlider(AutoHostMinPlayers, 1f, 15f, sliderStyle, sliderThumbStyle, GUILayout.Width(sliderWidth));
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            GUILayout.BeginHorizontal(GUILayout.Width(innerWidth));
            GUILayout.Label($"{L("Start Delay:", "Задержка старта:")} <color=#{hexColor}>{Mathf.Round(AutoHostStartDelaySeconds)}s</color>", sliderLabelStyle, GUILayout.Width(sliderLabelWidth));
            AutoHostStartDelaySeconds = GUILayout.HorizontalSlider(AutoHostStartDelaySeconds, 0f, 180f, sliderStyle, sliderThumbStyle, GUILayout.Width(sliderWidth));
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            GUILayout.BeginHorizontal(GUILayout.Width(innerWidth));
            GUILayout.Label($"{L("Fast Start Players:", "Игроков для фаст-старта:")} <color=#{hexColor}>{AutoHostFastStartPlayers}</color>", sliderLabelStyle, GUILayout.Width(sliderLabelWidth));
            AutoHostFastStartPlayers = (int)GUILayout.HorizontalSlider(AutoHostFastStartPlayers, 0f, 15f, sliderStyle, sliderThumbStyle, GUILayout.Width(sliderWidth));
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            GUILayout.BeginHorizontal(GUILayout.Width(innerWidth));
            GUILayout.Label($"{L("Fast Start Delay:", "Задержка фаст-старта:")} <color=#{hexColor}>{Mathf.Round(AutoHostFastStartDelaySeconds)}s</color>", sliderLabelStyle, GUILayout.Width(sliderLabelWidth));
            AutoHostFastStartDelaySeconds = GUILayout.HorizontalSlider(AutoHostFastStartDelaySeconds, 0f, 60f, sliderStyle, sliderThumbStyle, GUILayout.Width(sliderWidth));
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

private void DrawBugRoomTab()
        {
            float contentWidth = Mathf.Max(96f, GetMenuWorkWidth(120f, 760f) - 8f);
            float cardPaddingWidth = menuCardStyle != null && menuCardStyle.padding != null
                ? menuCardStyle.padding.left + menuCardStyle.padding.right
                : 28f;
            float innerWidth = Mathf.Floor(Mathf.Max(68f, contentWidth - cardPaddingWidth));
            int toggleWidth = Mathf.RoundToInt(Mathf.Min(300f, innerWidth));
            string accent = GetMenuAccentHex();

            GUILayout.BeginVertical(menuCardStyle, GUILayout.Width(contentWidth));

            DrawMenuSectionHeader(L("GAME FLOW", "ХОД ИГРЫ"));
            neverEndGame = DrawToggle(neverEndGame, L("Unlimited Game", "Бесконечная игра"), toggleWidth);
            GUILayout.Space(6);
            AutoHostAutoRunEnabled = DrawToggle(AutoHostAutoRunEnabled, L("Auto Run + Imp Win", "Авто-прогон + победа предателей"), toggleWidth);
            GUILayout.Space(6);
            DrawBugRoomAutoRunDelay(innerWidth);
            GUILayout.Space(6);
            DrawBugRoomTimedAutoRun(innerWidth);
            GUILayout.Space(6);
            bugRoomAutoRejoin = DrawToggle(bugRoomAutoRejoin, L("Rejoin Once Per Game", "Перезаход один раз за игру"), toggleWidth);
            GUILayout.Space(6);
            bugRoomImpMeeting = DrawToggle(bugRoomImpMeeting, L("Imp Meeting After 20s (Client)", "Митинг через 20с за импа (клиент)"), toggleWidth);

            GUILayout.Space(14);
            DrawMenuSectionHeader(L("TARGET & KILLS", "ЦЕЛЬ И КИЛЛЫ"));
            DrawBugRoomKillTargetPicker(innerWidth);
            GUILayout.Space(6);
            hostAutoKillRandom = DrawToggle(hostAutoKillRandom, L("Kill Random Target", "Килл случайной цели"), toggleWidth);
            GUILayout.Space(6);
            hostAutoKillTarget = DrawToggle(hostAutoKillTarget, L("Auto Kill Target", "Авто-килл цели"), toggleWidth);
            GUILayout.Space(6);
            DrawBugRoomAutoKillRate(innerWidth);

            GUILayout.Space(14);
            DrawMenuSectionHeader(L("PROTECTION", "ЗАЩИТА"));
            glitchRoomBypassShield = DrawToggle(glitchRoomBypassShield, L("Bypass Angel Shield", "Игнорировать щит ангела"), toggleWidth);
            GUILayout.Space(6);
            glitchRoomGodMode = DrawToggle(glitchRoomGodMode, L("God Mode", "Режим бога"), toggleWidth);
            GUILayout.Space(6);
            glitchRoomGodModeAll = DrawToggle(glitchRoomGodModeAll, L("God Mode: Everyone", "Режим бога: все"), toggleWidth);
            GUILayout.Space(6);
            bugRoomAutoAngel = DrawToggle(bugRoomAutoAngel, L("Auto Angel", "Авто-ангел"), toggleWidth);
            GUILayout.Space(6);
            DrawBugRoomAngelInterval(innerWidth);
            GUILayout.Space(6);
            bugRoomAutoKillShield = DrawToggle(bugRoomAutoKillShield, L("Auto Kill Angel Shield 0.13", "Авто-снять щит ангела 0.13"), toggleWidth);
            GUILayout.Space(8);

            if (DrawBugRoomWideButton(innerWidth, L("Protect Everyone (Network)", "Защитить всех (сеть)")))
                ProtectGlitchRoomEveryone(true);
            GUILayout.Space(6);

            bool forceProtectOn = glitchRoomProtectedPlayers.Contains(hostAutoKillTargetId);
            if (DrawBugRoomRowButton(innerWidth, 0, L("Protect as Angel", "Защитить ангелом")))
                ProtectGlitchRoomTarget(false);
            if (DrawBugRoomRowButton(innerWidth, 1, forceProtectOn
                ? L("Force Protect: ON", "Защита: ВКЛ")
                : L("Force Protect: OFF", "Защита: ВЫКЛ")))
                ProtectGlitchRoomTarget(true);
            DrawBugRoomRowButtonEnd();
            GUILayout.Space(6);
            if (DrawBugRoomWideButton(innerWidth, L("Reset State", "Сбросить состояние")))
                ResetGlitchRoomState();

            GUILayout.Space(14);
            DrawMenuSectionHeader(L("SCOUT", "СКАУТ"));
            bool oldScout = BugroomScoutEnabled;
            BugroomScoutEnabled = DrawToggle(BugroomScoutEnabled, L("Auto Create + Find TXT", "Авто создать + найти TXT"), toggleWidth);
            if (oldScout != BugroomScoutEnabled)
            {
                settingsDirty = true;
                ElysiumBugroomScoutService.ForceReload();
            }
            GUILayout.Space(6);

            var scout = ElysiumBugroomScoutService.GetStatusSnapshot();
            DrawBugRoomStatRow(innerWidth, L("Status", "Статус"), BugRoomStatusValue(scout.State));
            DrawBugRoomStatRow(innerWidth, L("Targets", "Цели"), $"<color=#{accent}>{scout.TargetCount}</color>");
            DrawBugRoomStatRow(innerWidth, L("Room", "Комната"), BugRoomCodeValue(scout.CurrentCode, scout.CurrentSuffix, accent));
            DrawBugRoomFileRow(innerWidth, L("File", "Файл"), scout.FilePath);
            GUILayout.Space(6);
            if (DrawBugRoomRowButton(innerWidth, 0, L("Reload TXT", "Перезагрузить TXT")))
            {
                ElysiumBugroomScoutService.ForceReload();
                ShowNotification("<color=#00FFAA>[SCOUT]</color> TXT reloaded.");
            }
            if (DrawBugRoomRowButton(innerWidth, 1, L("Copy Path", "Копировать путь")))
            {
                GUIUtility.systemCopyBuffer = scout.FilePath;
                ShowNotification("<color=#00FFAA>[SCOUT]</color> TXT path copied.");
            }
            DrawBugRoomRowButtonEnd();

            GUILayout.EndVertical();
        }

private static readonly char[] bugRoomPathSeparators = { '/', '\\' };

private static float GetBugRoomLabelWidth(float innerWidth)
        {
            float labelWidth = Mathf.Floor(Mathf.Clamp(innerWidth * 0.38f, 54f, 150f));
            if (labelWidth > innerWidth - 40f)
                labelWidth = Mathf.Floor(Mathf.Max(24f, innerWidth * 0.45f));
            return labelWidth;
        }

private void DrawBugRoomStatRow(float innerWidth, string label, string value)
        {
            float labelWidth = GetBugRoomLabelWidth(innerWidth);
            float valueWidth = Mathf.Max(24f, innerWidth - labelWidth - 6f);

            GUILayout.BeginHorizontal(GUILayout.Width(innerWidth), GUILayout.Height(20));
            GUILayout.Label(label, lobbyRichLabelStyle11, GUILayout.Width(labelWidth), GUILayout.Height(20));
            GUILayout.Space(6);
            GUILayout.Label(value, richClipLabelStyle11, GUILayout.Width(valueWidth), GUILayout.Height(20));
            GUILayout.EndHorizontal();
        }

private void DrawBugRoomFileRow(float innerWidth, string label, string path)
        {
            float labelWidth = GetBugRoomLabelWidth(innerWidth);
            float valueWidth = Mathf.Max(24f, innerWidth - labelWidth - 6f);
            DrawBugRoomStatRow(innerWidth, label, ShortenBugRoomPath(path, valueWidth));
        }

private static string ShortenBugRoomPath(string path, float valueWidth)
        {
            if (string.IsNullOrWhiteSpace(path)) return "<color=#777777>-</color>";

            string name = path;
            int separator = name.LastIndexOfAny(bugRoomPathSeparators);
            if (separator >= 0 && separator < name.Length - 1)
                name = name.Substring(separator + 1);

            int budget = Mathf.Max(6, Mathf.FloorToInt(valueWidth / 6.4f));
            if (name.Length > budget)
                name = name.Substring(0, Mathf.Max(3, budget - 2)) + "..";
            return name;
        }

private bool DrawBugRoomRowButton(float innerWidth, int slot, string text)
        {
            float half = Mathf.Floor((innerWidth - 8f) * 0.5f);
            float width = slot == 0 ? half : Mathf.Max(24f, innerWidth - half - 8f);

            if (slot == 0)
                GUILayout.BeginHorizontal(GUILayout.Width(innerWidth), GUILayout.Height(25));
            else
                GUILayout.Space(8);

            return GUILayout.Button(text, btnStyle, GUILayout.Width(width), GUILayout.Height(25));
        }

private void DrawBugRoomRowButtonEnd()
        {
            GUILayout.EndHorizontal();
        }

private bool DrawBugRoomWideButton(float innerWidth, string text)
        {
            GUILayout.BeginHorizontal(GUILayout.Width(innerWidth), GUILayout.Height(25));
            bool clicked = GUILayout.Button(text, btnStyle, GUILayout.Width(innerWidth), GUILayout.Height(25));
            GUILayout.EndHorizontal();
            return clicked;
        }

private static string BugRoomStatusValue(string state)
        {
            if (string.IsNullOrWhiteSpace(state)) return "<color=#777777>-</color>";
            string trimmed = state.Trim();
            bool off = trimmed.Equals("Off", StringComparison.OrdinalIgnoreCase)
                || trimmed.Equals("Disabled", StringComparison.OrdinalIgnoreCase);
            return off ? $"<color=#777777>{trimmed}</color>" : $"<color=#66FF99>{trimmed}</color>";
        }

private static string BugRoomCodeValue(string code, string suffix, string accent)
        {
            bool hasCode = !string.IsNullOrWhiteSpace(code);
            bool hasSuffix = !string.IsNullOrWhiteSpace(suffix);
            if (!hasCode && !hasSuffix) return "<color=#777777>-</color>";

            string codeText = hasCode ? $"<color=#{accent}>{code.Trim()}</color>" : "<color=#777777>-</color>";
            if (!hasSuffix) return codeText;
            return $"{codeText} <color=#777777>·</color> <color=#{accent}>{suffix.Trim()}</color>";
        }

private void DrawBugRoomAngelInterval(float innerWidth)
        {
            float labelWidth = Mathf.Min(155f, Mathf.Max(66f, innerWidth * 0.38f));
            labelWidth = Mathf.Min(labelWidth, innerWidth * 0.55f);
            float sliderWidth = Mathf.Max(24f, innerWidth - labelWidth - 8f);

            GUILayout.BeginHorizontal(GUILayout.Width(innerWidth), GUILayout.Height(22));
            GUILayout.Label($"Angel Delay: <color=#{GetMenuAccentHex()}>{bugRoomAutoAngelIntervalSeconds:0.000}s</color>", lobbyRichLabelStyle11, GUILayout.Width(labelWidth), GUILayout.Height(22));

            float old = bugRoomAutoAngelIntervalSeconds;
            float val = GUILayout.HorizontalSlider(bugRoomAutoAngelIntervalSeconds, 0.001f, 0.50f, sliderStyle, sliderThumbStyle, GUILayout.Width(sliderWidth));
            bugRoomAutoAngelIntervalSeconds = Mathf.Clamp(Mathf.Round(val * 1000f) / 1000f, 0.001f, 0.50f);
            if (Mathf.Abs(old - bugRoomAutoAngelIntervalSeconds) > 0.0001f) settingsDirty = true;
            GUILayout.EndHorizontal();
        }

private void DrawBugRoomAutoRunDelay(float innerWidth)
        {
            float labelWidth = Mathf.Min(155f, Mathf.Max(66f, innerWidth * 0.38f));
            labelWidth = Mathf.Min(labelWidth, innerWidth * 0.55f);
            float sliderWidth = Mathf.Max(24f, innerWidth - labelWidth - 8f);

            GUILayout.BeginHorizontal(GUILayout.Width(innerWidth), GUILayout.Height(22));
            GUILayout.Label($"Auto Run Delay: <color=#{GetMenuAccentHex()}>{AutoHostAutoRunDelaySeconds:0.00}s</color>", lobbyRichLabelStyle11, GUILayout.Width(labelWidth), GUILayout.Height(22));

            float old = AutoHostAutoRunDelaySeconds;
            float val = GUILayout.HorizontalSlider(AutoHostAutoRunDelaySeconds, 0.25f, 10f, sliderStyle, sliderThumbStyle, GUILayout.Width(sliderWidth));
            AutoHostAutoRunDelaySeconds = Mathf.Clamp(Mathf.Round(val * 100f) / 100f, 0.25f, 10f);
            if (Mathf.Abs(old - AutoHostAutoRunDelaySeconds) > 0.001f) settingsDirty = true;
            GUILayout.EndHorizontal();
        }

private void DrawBugRoomTimedAutoRun(float innerWidth)
        {
            float rowWidth = Mathf.Max(68f, innerWidth);
            int timedToggleWidth = Mathf.RoundToInt(Mathf.Min(150f, Mathf.Max(82f, rowWidth - 85f)));

            GUILayout.BeginHorizontal(GUILayout.Width(rowWidth), GUILayout.Height(24));
            bugRoomTimedAutoRun = DrawToggle(bugRoomTimedAutoRun, "Timed Auto Run", timedToggleWidth);
            GUILayout.Space(8);

            if (!isEditingBugRoomTimedAutoRun) bugRoomTimedAutoRunInput = bugRoomTimedAutoRunMinutes.ToString();
            if (DrawBugRoomMinuteInput())
            {
                isEditingBugRoomTimedAutoRun = true;
                bugRoomTimedAutoRunInput = string.Empty;
            }
            GUILayout.Label("min", toggleLabelStyle, GUILayout.Width(32), GUILayout.Height(22));
            GUILayout.EndHorizontal();

            GUILayout.Label(GetBugRoomTimedAutoRunText(), richClipLabelStyle11, GUILayout.Width(rowWidth));
        }

private bool DrawBugRoomMinuteInput()
        {
            GUIStyle style = isEditingBugRoomTimedAutoRun ? activeSmallInputStyle : smallInputStyle;

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

private void DrawBugRoomKillTargetPicker(float innerWidth)
        {
            List<PlayerControl> plrs = GetBugRoomKillTargets();
            if (plrs.Count == 0)
            {
                GUILayout.Label("<color=#aaaaaa>Target: none</color>", richClipLabelStyle12, GUILayout.Width(innerWidth));
                return;
            }

            int idx = plrs.FindIndex(p => p != null && p.PlayerId == hostAutoKillTargetId);
            if (idx < 0)
            {
                idx = 0;
                hostAutoKillTargetId = plrs[0].PlayerId;
            }

            GUILayout.BeginHorizontal(GUILayout.Width(innerWidth));
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
            if (target == PlayerControl.LocalPlayer) nm += " [you]";
            if (target.Data != null && target.Data.IsDead) nm += " [dead]";

            GUILayout.Label(nm, morphValueStyle, GUILayout.Height(24), GUILayout.ExpandWidth(true));

            if (GUILayout.Button(">", btnStyle, GUILayout.Width(28), GUILayout.Height(24)))
            {
                idx++;
                if (idx >= plrs.Count) idx = 0;
                hostAutoKillTargetId = plrs[idx].PlayerId;
                settingsDirty = true;
            }
            GUILayout.EndHorizontal();
        }

private void DrawBugRoomAutoKillRate(float innerWidth)
        {
            float labelWidth = Mathf.Min(155f, Mathf.Max(66f, innerWidth * 0.38f));
            labelWidth = Mathf.Min(labelWidth, innerWidth * 0.55f);
            float sliderWidth = Mathf.Max(24f, innerWidth - labelWidth - 8f);
            int old = hostAutoKillRate;
            float delay = 1f / Mathf.Clamp(hostAutoKillRate, 1, 35);

            GUILayout.BeginHorizontal(GUILayout.Width(innerWidth), GUILayout.Height(22));
            GUILayout.Label($"Kill Rate: <color=#{GetMenuAccentHex()}>{hostAutoKillRate}/s ({delay:0.###}s)</color>", lobbyRichLabelStyle11, GUILayout.Width(labelWidth), GUILayout.Height(22));
            float val = GUILayout.HorizontalSlider(hostAutoKillRate, 1f, 35f, sliderStyle, sliderThumbStyle, GUILayout.Width(sliderWidth));
            hostAutoKillRate = Mathf.Clamp(Mathf.RoundToInt(val), 1, 35);
            if (old != hostAutoKillRate) settingsDirty = true;
            GUILayout.EndHorizontal();
        }

}
}
