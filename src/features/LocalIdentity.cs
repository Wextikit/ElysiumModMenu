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
            ShowNotification($"<color=#ca08ff>[MORPH]</color> <b>{target.Data.PlayerName}</b> превращен в <b>{morphInto.Data.PlayerName}</b>!");
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
            if (target == null || AmongUsClient.Instance == null) return;
            if (!AmongUsClient.Instance.AmHost) return;

            try
            {
                MeetingRoomManager.Instance.AssignSelf(target, null);
                target.RpcStartMeeting(null);
                DestroyableSingleton<HudManager>.Instance.OpenMeetingRoom(target);
            }
            catch { }
        }

private void KillAll()
        {
            if (PlayerControl.LocalPlayer == null || PlayerControl.AllPlayerControls == null) return;
            Vector3 op = PlayerControl.LocalPlayer.transform.position;
            foreach (var t in PlayerControl.AllPlayerControls)
            {
                if (t != null && t != PlayerControl.LocalPlayer && !t.Data.IsDead && !t.Data.Disconnected)
                {
                    PlayerControl.LocalPlayer.NetTransform.RpcSnapTo(t.transform.position);
                    PlayerControl.LocalPlayer.CmdCheckMurder(t);
                    PlayerControl.LocalPlayer.RpcMurderPlayer(t, true);
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
                if (LobbyBehaviour.Instance != null && AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
                {
                    LobbyBehaviour.Instance.Cast<InnerNetObject>().Despawn();
                }
            }
            catch { }
        }

private void SpawnLobby()
        {
            try
            {
                if (GameStartManager.Instance != null && AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
                {
                    LobbyBehaviour newLobby = UnityEngine.Object.Instantiate<LobbyBehaviour>(GameStartManager.Instance.LobbyPrefab);
                    AmongUsClient.Instance.Spawn(newLobby.Cast<InnerNetObject>(), -2, SpawnFlags.None);
                }
            }
            catch { }
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
                    ShowNotification($"<color=#00FFAA>[LOCAL NAME]</color> {L("Applied locally:", "Локально применен:")} <b>{newName}</b>");
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
                string current = local.Data.FriendCode ?? string.Empty;
                if (originalLocalFriendCode == null && current != fakeFriendCode)
                    originalLocalFriendCode = current;

                TrySetStringMember(local.Data, "FriendCode", fakeFriendCode);

                if (notify)
                    ShowNotification($"<color=#00FFAA>[LOCAL FC]</color> {L("Applied locally:", "Локально применен:")} <b>{fakeFriendCode}</b>");
            }
            catch { }
        }

        private static void RestoreLocalFriendCodeSelf()
        {
            try
            {
                if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data == null || originalLocalFriendCode == null) return;
                TrySetStringMember(PlayerControl.LocalPlayer.Data, "FriendCode", originalLocalFriendCode);
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
                Plugin.MoreLobbyInfoConfig.Value = moreLobbyInfo;
                Plugin.EnableChatDarkModeConfig.Value = enableChatDarkMode;
                Plugin.GhostChatColorConfig.Value = SanitizeHexColor(ghostChatColorHex, "#D7B8FF");
                Plugin.EnableAnomalyLogReportsConfig.Value = enableAnomalyLogReports;
                Plugin.ThrottleDefaultLogsConfig.Value = throttleDefaultLogs;
                Plugin.ShowEspFriendCodeConfig.Value = showEspFriendCode;
                Plugin.RpcSpoofDelayConfig.Value = rpcSpoofDelay;
                Plugin.MenuColorIndexConfig.Value = currentMenuColorIndex;
                Plugin.RgbMenuModeConfig.Value = rgbMenuMode;
                Plugin.RgbMenuTextConfig.Value = rgbMenuText;
                Plugin.BoldMenuTextConfig.Value = boldMenuText;
                if (menuToggleKey == KeyCode.None) menuToggleKey = KeyCode.Insert;
                Plugin.MenuKeybind.Value = menuToggleKey;
                PlayerPrefs.SetInt("M_MenuToggleKey", (int)menuToggleKey);
                SaveBool("M_WhiteTheme", whiteMenuTheme);
                SaveBool("M_RgbMenuText", rgbMenuText);
                SaveBool("M_BoldMenuText", boldMenuText);
                PlayerPrefs.SetInt("M_MenuLanguageIndex", currentMenuLanguageIndex);
                PlayerPrefs.SetInt("M_FpsLimit", fpsLimit);
                SaveBool("M_EnableBackground", enableBackground);
                SaveBool("M_HardMenu", hardMenu);
                SaveBool("M_AutoCopyCodeAndLeave", autoCopyCodeAndLeave);
                SaveBool("M_EnableCustomNotifs", EnableCustomNotifs);
                SaveBool("M_LogAllRPCs", LogAllRPCs);
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
                SaveBool("M_LocalNameSpoof", enableLocalNameSpoof);
                SaveBool("M_LocalFakeFCEnabled", enableLocalFriendCodeSpoof);
                PlayerPrefs.SetString("M_LocalFakeFC", localFriendCodeInput);

                SaveBool("M_ShowPlayerInfo", showPlayerInfo);
                SaveBool("M_SeeGhosts", seeGhosts);
                SaveBool("M_SeeRoles", seeRoles);
                SaveBool("M_RevealMeetingRoles", revealMeetingRoles);
                SaveBool("M_ShowTracers", showTracers);
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
                SaveBool("M_EnableChatMessageDoubleClickCopy", enableChatMessageDoubleClickCopy);
                SaveBool("M_EnableChatNameColorCopy", enableChatNameColorCopy);
                SaveBool("M_EnableChatLog", enableChatLog);
                SaveBool("M_EnableColorCommand", enableColorCommand);
                SaveBool("M_BlockRainbowChat", blockRainbowChat);
                SaveBool("M_BlockFortegreenChat", blockFortegreenChat);
                SaveBool("M_SpoofMenuEnabled", SpoofMenuEnabled);
                SaveBool("M_NoClip", noClip);
                SaveBool("M_TpToCursor", tpToCursor);
                SaveBool("M_DragToCursor", dragToCursor);
                SaveBool("M_AutoFollowCursor", autoFollowCursor);
                SaveBool("M_Freecam", freecam);
                SaveBool("M_CameraZoom", cameraZoom);
                SaveBool("M_RevealVotes", RevealVotesEnabled);
                SaveBool("M_NoTaskMode", noTaskMode);
                SaveBool("M_NoMapCooldowns", noMapCooldowns);
                SaveBool("M_UnlockVents", unlockVents);
                SaveBool("M_WalkInVents", walkInVents);
                SaveBool("M_AllowTasksAsImpostor", allowTasksAsImpostor);
                SaveBool("M_KillWhileVanishedHostOnly", killWhileVanishedHostOnly);
                SaveBool("M_RoleBuffImmortality", roleBuffImmortality);
                SaveBool("M_NeverEndGame", neverEndGame);
                SaveBool("M_RemovePenalty", removePenalty);
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
                SaveBool("M_AutoReturnLobbyAfterMatch", AutoReturnLobbyAfterMatch);
                SaveBool("M_AutoHostNotifications", AutoHostNotifications);
                SaveBool("M_AutoHostForceLastMinute", AutoHostForceLastMinute);
                SaveBool("M_AutoHostWaitLoadedPlayers", AutoHostWaitLoadedPlayers);
                SaveBool("M_AutoHostCancelBelowMin", AutoHostCancelBelowMin);
                SaveBool("M_AutoHostInstantStart", AutoHostInstantStart);
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
}
}
