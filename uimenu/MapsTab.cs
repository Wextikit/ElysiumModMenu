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
using Il2CppInterop.Runtime;
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

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.SetColor))]
        public static class AutoKickBugs_Patch
        {
            public static void Postfix(PlayerControl __instance, byte bodyColor)
            {
                if (!ElysiumModMenuGUI.autoKickBugs || AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return;

                try
                {
                    if (__instance != null && __instance != PlayerControl.LocalPlayer && __instance.Data != null && !__instance.Data.Disconnected)
                    {
                        byte pid = __instance.PlayerId;
                        string colorName = Palette.GetColorName((int)bodyColor);

                if (bodyColor == 18 || colorName == "???" || bodyColor >= Palette.PlayerColors.Length)
                        {
                            if (ElysiumModMenuGUI.IsProtectedFromAnticheat(__instance))
                            {
                                ElysiumModMenuGUI.fortegreenTimer.Remove(pid);
                                return;
                            }

                            if (!ElysiumModMenuGUI.fortegreenTimer.ContainsKey(pid))
                            {
                                ElysiumModMenuGUI.fortegreenTimer[pid] = Time.time + ElysiumModMenuGUI.autoKickTimer;
                            }
                        }
                        else
                        {
                            if (ElysiumModMenuGUI.fortegreenTimer.ContainsKey(pid))
                            {
                                ElysiumModMenuGUI.fortegreenTimer.Remove(pid);
                            }
                        }
                    }
                }
                catch { }
            }
        }

[HarmonyPatch(typeof(VoteBanSystem), nameof(VoteBanSystem.HandleRpc))]
        public static class VoteBanSystemPatch
        {
            public static bool Prefix(VoteBanSystem __instance, byte callId, Hazel.MessageReader reader)
            {
                if (callId != 26)
                    return true;

                bool shouldBlock = AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost && (ElysiumModMenuGUI.disableVoteKicks || ElysiumModMenuGUI.banVoteKickVoters);
                try
                {
                    Hazel.MessageReader copy = Hazel.MessageReader.Get(reader);
                    int firstClientId = copy.ReadInt32();
                    int targetClientId = copy.ReadInt32();
                    int voterClientId = ElysiumNetGuard.NetworkGuard.ResolveCurrentRpcSenderClientId(__instance, callId);
                    ElysiumModMenuGUI.RegisterVoteKickEsp(targetClientId);
                    if (voterClientId < 0 && firstClientId != targetClientId && FindVoteClientPlayer(firstClientId) != null)
                        voterClientId = firstClientId;
                    string targetName = ResolveVoteClientName(targetClientId);
                    string voterName = voterClientId >= 0 ? ResolveVoteClientName(voterClientId) : "unknown voter";

                    ShowVoteKickChatInfo(voterName, targetName);
                    if (ElysiumModMenuGUI.banVoteKickVoters && AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
                        BanVoteKickVoter(voterClientId, voterName, targetName);
                    if (shouldBlock)
                        ElysiumModMenuGUI.ShowNotification($"<color=#FFAC1C>[VOTEKICK BLOCK]</color> {voterName} tried to vote-kick {targetName}");
                }
                catch
                {
                    if (shouldBlock)
                        ElysiumModMenuGUI.ShowNotification("<color=#FFAC1C>[VOTEKICK BLOCK]</color> Vote-kick blocked, sender could not be resolved.");
                }

                return !shouldBlock;
            }

            internal static void BanVoteKickVoter(int voterClientId, string voterName, string targetName)
            {
                try
                {
                    if (AmongUsClient.Instance == null || voterClientId < 0 || voterClientId == AmongUsClient.Instance.ClientId)
                        return;

                    PlayerControl voter = FindVoteClientPlayer(voterClientId);
                    if (voter != null && ElysiumModMenuGUI.IsProtectedFromAnticheat(voter)) return;
                    if (voter == null && ElysiumModMenuGUI.IsProtectedFromAnticheat(voterClientId)) return;

                    ElysiumNetGuard.NetworkGuard.BanClient(voterClientId, "Vote-kick attempt", CleanVoteName(targetName));
                    ElysiumModMenuGUI.ShowNotification($"<color=#FF4444>[VOTE BAN]</color> {CleanVoteName(voterName)} banned for vote-kick.");
                }
                catch { }
            }

            private static PlayerControl FindVoteClientPlayer(int clientId)
            {
                try
                {
                    if (PlayerControl.AllPlayerControls == null) return null;
                    foreach (var pc in PlayerControl.AllPlayerControls)
                    {
                        if (pc == null || pc.Data == null || NetworkedClones.IsClone(pc)) continue;
                        if (pc.Data.ClientId == clientId || (int)pc.OwnerId == clientId)
                            return pc;
                    }
                }
                catch { }

                return null;
            }

            private static string ResolveVoteClientName(int clientId)
            {
                try
                {
                    if (PlayerControl.AllPlayerControls != null)
                    {
                        foreach (var pc in PlayerControl.AllPlayerControls)
                        {
                            if (pc == null || pc.Data == null || NetworkedClones.IsClone(pc)) continue;
                            if (pc.Data.ClientId == clientId || (int)pc.OwnerId == clientId)
                            {
                                string name = string.IsNullOrWhiteSpace(pc.Data.PlayerName) ? "Unknown" : pc.Data.PlayerName;
                                return $"{name} ({clientId})";
                            }
                        }
                    }
                }
                catch { }

                return $"client {clientId}";
            }

            private static void ShowVoteKickChatInfo(string voterName, string targetName)
            {
                string message = $"<color=#FFAC1C>[VOTEKICK]</color> <b>{CleanVoteName(voterName)}</b> vote-kicked <b>{CleanVoteName(targetName)}</b>";
                try
                {
                    if (HudManager.Instance != null && HudManager.Instance.Chat != null && PlayerControl.LocalPlayer != null)
                    {
                        HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, message);
                        return;
                    }
                }
                catch { }

                ElysiumModMenuGUI.ShowNotification(message);
            }

            private static string CleanVoteName(string value)
            {
                if (string.IsNullOrWhiteSpace(value)) return "Unknown";
                value = Regex.Replace(value, "<[^>]*>", string.Empty);
                return value.Replace("<", string.Empty).Replace(">", string.Empty).Trim();
            }
        }

public static bool disableVoteKicks = false;

public static bool banVoteKickVoters = false;

[HarmonyPatch(typeof(ShhhBehaviour), nameof(ShhhBehaviour.PlayAnimation))]
        public static class SkipShhh_Perfect_Patch
        {
            public static void Prefix()
            {
                ElysiumModMenuGUI.MarkCurrentGameIntroShhhSeen();
                ElysiumModMenuGUI.NotifyAutoChatEveryoneShhhSeen();
            }

            public static void Postfix(ShhhBehaviour __instance, ref Il2CppSystem.Collections.IEnumerator __result)
            {
                if (!ElysiumModMenuGUI.skipShhhAnim || __instance == null || __result == null) return;

                __result = FastSkip(__instance, __result).WrapToIl2Cpp();
            }

            private static System.Collections.IEnumerator FastSkip(ShhhBehaviour shhh, Il2CppSystem.Collections.IEnumerator anim)
            {
                float duration = shhh.Duration;
                float delay = shhh.Delay;
                float textDuration = shhh.TextDuration;
                float pulseDuration = shhh.PulseDuration;
                float holdDuration = shhh.HoldDuration;

                shhh.Duration = 0.01f;
                shhh.Delay = 0f;
                shhh.TextDuration = 0.01f;
                shhh.PulseDuration = 0.01f;
                shhh.HoldDuration = 0f;

                try
                {
                    while (anim.MoveNext())
                        yield return anim.Current;
                }
                finally
                {
                    try
                    {
                        if (shhh != null)
                        {
                            shhh.Duration = duration;
                            shhh.Delay = delay;
                            shhh.TextDuration = textDuration;
                            shhh.PulseDuration = pulseDuration;
                            shhh.HoldDuration = holdDuration;
                        }
                    }
                    catch { }
                }
            }
        }

private static NamePlateViewData meetingNameplate;

[HarmonyPatch(typeof(CosmeticsCache), nameof(CosmeticsCache.GetNameplate))]
        public static class CosmeticsCache_GetNameplate_RoleSkip_Patch
        {
            public static bool Prefix(ref NamePlateViewData __result)
            {
                if (!ElysiumModMenuGUI.skipRoleIntroAnim || MeetingHud.Instance == null)
                    return true;

                try
                {
                    if (ElysiumModMenuGUI.meetingNameplate == null)
                    {
                        ElysiumModMenuGUI.meetingNameplate = ScriptableObject.CreateInstance<NamePlateViewData>();
                        ElysiumModMenuGUI.meetingNameplate.Image = null;
                    }

                    __result = ElysiumModMenuGUI.meetingNameplate;
                    return false;
                }
                catch
                {
                    return true;
                }
            }
        }

[HarmonyPatch(typeof(CosmeticsCache), nameof(CosmeticsCache.Destroy))]
        public static class CosmeticsCache_Destroy_RoleSkip_Patch
        {
            public static void Postfix()
            {
                try
                {
                    if (ElysiumModMenuGUI.meetingNameplate != null)
                        Object.Destroy(ElysiumModMenuGUI.meetingNameplate);
                }
                catch { }

                ElysiumModMenuGUI.meetingNameplate = null;
            }
        }

[HarmonyPatch(typeof(IntroCutscene), "CoBegin")]
        public static class IntroCutscene_CoBegin_RoleSkip_Patch
        {
            private sealed class HnsIntroClips
            {
                public AnimationClip Empty;
                public AnimationClip Spawn;
                public AnimationClip HorseSpawn;
                public AnimationClip HorseInGame;
                public AnimationClip LongSpawn;
                public AnimationClip LongInGame;
            }

            private static void Prefix(IntroCutscene __instance, out HnsIntroClips __state)
            {
                __state = null;
                if (!ElysiumModMenuGUI.skipRoleIntroAnim || __instance == null || !ElysiumModMenuGUI.IsHideAndSeekMode())
                    return;

                HnsIntroClips clips = new HnsIntroClips
                {
                    Empty = new AnimationClip(),
                    Spawn = __instance.HnSSeekerSpawnAnim,
                    HorseSpawn = __instance.HnSSeekerSpawnHorseAnim,
                    HorseInGame = __instance.HnSSeekerSpawnHorseInGameAnim,
                    LongSpawn = __instance.HnSSeekerSpawnLongAnim,
                    LongInGame = __instance.HnSSeekerSpawnLongInGameAnim
                };

                try
                {
                    __instance.HnSSeekerSpawnAnim = clips.Empty;
                    __instance.HnSSeekerSpawnHorseAnim = clips.Empty;
                    __instance.HnSSeekerSpawnHorseInGameAnim = clips.Empty;
                    __instance.HnSSeekerSpawnLongAnim = clips.Empty;
                    __instance.HnSSeekerSpawnLongInGameAnim = clips.Empty;
                    __state = clips;
                }
                catch
                {
                    Restore(__instance, clips);
                }
            }

            private static void Postfix(IntroCutscene __instance, ref Il2CppSystem.Collections.IEnumerator __result, HnsIntroClips __state)
            {
                if (!ElysiumModMenuGUI.skipRoleIntroAnim)
                    return;

                if (__result == null)
                {
                    Restore(__instance, __state);
                    return;
                }

                try
                {
                    Plugin.Instance?.Log?.LogMessage((object)$"[ANIM] Role intro skip applied: {(ElysiumModMenuGUI.IsHideAndSeekMode() ? "HideAndSeek" : "Normal")}");
                }
                catch { }

                __result = Run(__instance, __result, __state).WrapToIl2Cpp();
            }

            private static System.Collections.IEnumerator Run(IntroCutscene intro, Il2CppSystem.Collections.IEnumerator anim, HnsIntroClips clips)
            {
                try
                {
                    bool running = true;
                    int steps = 0;
                    while (running && steps < 1024)
                    {
                        running = anim.MoveNext();
                        steps++;
                    }

                    if (running)
                    {
                        try
                        {
                            if (intro != null && intro.gameObject != null)
                                intro.gameObject.SetActive(false);
                        }
                        catch { }

                        while (anim.MoveNext())
                            yield return anim.Current;
                    }
                }
                finally
                {
                    Restore(intro, clips);
                }
            }

            private static void Restore(IntroCutscene intro, HnsIntroClips clips)
            {
                if (clips == null)
                    return;

                try
                {
                    if (intro != null)
                    {
                        intro.HnSSeekerSpawnAnim = clips.Spawn;
                        intro.HnSSeekerSpawnHorseAnim = clips.HorseSpawn;
                        intro.HnSSeekerSpawnHorseInGameAnim = clips.HorseInGame;
                        intro.HnSSeekerSpawnLongAnim = clips.LongSpawn;
                        intro.HnSSeekerSpawnLongInGameAnim = clips.LongInGame;
                    }
                }
                catch { }

                try
                {
                    if (clips.Empty != null)
                        Object.Destroy(clips.Empty);
                }
                catch { }
            }
        }

[HarmonyPatch(typeof(KillOverlay), nameof(KillOverlay.ShowKillAnimation), new Type[] { typeof(NetworkedPlayerInfo), typeof(NetworkedPlayerInfo) })]
        public static class KillOverlay_ShowKillAnimation_Info_Skip_Patch
        {
            public static bool Prefix()
            {
                return !ElysiumModMenuGUI.skipKillAnimation;
            }
        }

[HarmonyPatch(typeof(KillOverlay), nameof(KillOverlay.ShowKillAnimation), new Type[] { typeof(OverlayKillAnimation), typeof(NetworkedPlayerInfo), typeof(NetworkedPlayerInfo) })]
        public static class KillOverlay_ShowKillAnimation_AnimInfo_Skip_Patch
        {
            public static bool Prefix()
            {
                return !ElysiumModMenuGUI.skipKillAnimation;
            }
        }

[HarmonyPatch(typeof(KillOverlay), nameof(KillOverlay.ShowKillAnimation), new Type[] { typeof(OverlayKillAnimation), typeof(KillOverlayInitData) })]
        public static class KillOverlay_ShowKillAnimation_InitData_Skip_Patch
        {
            public static bool Prefix()
            {
                return !ElysiumModMenuGUI.skipKillAnimation;
            }
        }

private void SpawnMap(int mapId)
        {
            try
            {
                if (!CanMutateLobbyMap("Spawn Map", true, disableMapSafeMode)) return;
                if ((UnityEngine.Object)(object)AmongUsClient.Instance == (UnityEngine.Object)null || AmongUsClient.Instance.ShipPrefabs == null)
                    return;
                if (manualMapSpawnInProgress)
                {
                    ShowNotification("<color=#FFAA00>[MAP]</color> Map spawn is already running.");
                    return;
                }
                if (ShipStatus.Instance != null)
                {
                    ShowNotification("<color=#FFAA00>[MAP]</color> Despawn the current map first.");
                    return;
                }
                if (LobbyBehaviour.Instance != null)
                {
                    ShowNotification("<color=#FFAA00>[MAP]</color> Despawn the lobby before spawning a map.");
                    return;
                }

                int realMapId = mapId;
                if (mapId == 3) realMapId = 4;
                if (mapId == 4) realMapId = 5;

                if (realMapId >= AmongUsClient.Instance.ShipPrefabs.Count)
                    return;

                BepInEx.Unity.IL2CPP.Utils.MonoBehaviourExtensions.StartCoroutine(this, CoSpawnMap(realMapId));
            }
            catch { }
        }

[HideFromIl2Cpp]
        private System.Collections.IEnumerator CoSpawnMap(int mapId)
        {
            manualMapSpawnInProgress = true;
            try
            {
                AmongUsClient.Instance.ShipLoadingAsyncHandle = AmongUsClient.Instance.ShipPrefabs[mapId].InstantiateAsync((Transform)null, false);
                yield return AmongUsClient.Instance.ShipLoadingAsyncHandle;

                ShipStatus.Instance = AmongUsClient.Instance.ShipLoadingAsyncHandle.Result.GetComponent<ShipStatus>();
                ((InnerNetClient)AmongUsClient.Instance).Spawn(((Component)ShipStatus.Instance).GetComponent<InnerNetObject>(), -2, (SpawnFlags)0);
                manualSpawnedMapId = NormalizeRuntimeMapId(mapId);
                ResetLobbyMapTransientState();
            }
            finally
            {
                manualMapSpawnInProgress = false;
                try { AmongUsClient.Instance.ShipLoadingAsyncHandle = default; } catch { }
            }

        }

private void DespawnMap()
        {
            try
            {
                if (!CanMutateLobbyMap("Despawn Map", true, disableMapSafeMode)) return;
                if (ShipStatus.Instance != null)
                {
                    ShipStatus.Instance.Despawn();
                    ShipStatus.Instance = null;
                    manualSpawnedMapId = -1;
                    ResetLobbyMapTransientState();
                }
            }
            catch { }
        }

private void DespawnCurrentMap()
        {
            DespawnMap();
        }

[HideFromIl2Cpp]
        private System.Collections.IEnumerator CoSpawnOverlappedMap(int mapId)
        {
            yield return CoSpawnMap(mapId);
        }

public static Dictionary<string, Vector2> skeldTeleportLocations = new Dictionary<string, Vector2>()
{
    { "Cafeteria", new Vector2(-0.78f, 2.48f) },
    { "Weapons", new Vector2(8.04f, 1.24f) },
    { "Navigation", new Vector2(16.59f, -2.33f) },
    { "O2", new Vector2(5.15f, -3.12f) },
    { "Shields", new Vector2(10.15f, -7.64f) },
    { "Communications", new Vector2(3.87f, -11.08f) },
    { "Storage", new Vector2(-1.92f, -6.14f) },
    { "Admin", new Vector2(5.31f, -7.42f) },
    { "Electrical", new Vector2(-3.37f, -4.84f) },
    { "Security", new Vector2(-5.69f, -3.07f) },
    { "Medbay", new Vector2(-8.61f, -4.30f) },
    { "Reactor", new Vector2(-20.19f, -2.48f) },
    { "Upper Engine", new Vector2(-16.84f, 2.47f) },
    { "Lower Engine", new Vector2(-16.48f, -7.53f) }
};

public static Dictionary<string, Vector2> miraTeleportLocations = new Dictionary<string, Vector2>()
{
    { "Launchpad", new Vector2(0.12f, -1.5f) },
    { "Medbay", new Vector2(10.2f, 15.1f) },
    { "Locker Room", new Vector2(12.5f, 18.5f) },
    { "Decontamination", new Vector2(14.8f, 22.0f) },
    { "Reactor", new Vector2(20.5f, 25.0f) },
    { "Laboratory", new Vector2(26.2f, 22.1f) },
    { "Office", new Vector2(24.5f, 15.2f) },
    { "Greenhouse", new Vector2(22.1f, 8.5f) },
    { "Admin", new Vector2(18.2f, 3.1f) },
    { "Cafeteria", new Vector2(14.5f, -2.1f) },
    { "Storage", new Vector2(9.8f, -6.5f) }
};

public static Dictionary<string, Vector2> polusTeleportLocations = new Dictionary<string, Vector2>()
{
    { "Dropship", new Vector2(0f, 0f) },
    { "Electrical", new Vector2(5.2f, 12.1f) },
    { "O2", new Vector2(-12.4f, 8.5f) },
    { "Security", new Vector2(-18.5f, 2.2f) },
    { "Decontamination", new Vector2(-25.2f, 1.5f) },
    { "Specimen Room", new Vector2(-30.1f, -5.2f) },
    { "Laboratory", new Vector2(-20.5f, -12.1f) },
    { "Medbay", new Vector2(-8.2f, -15.4f) },
    { "Communications", new Vector2(8.5f, -12.1f) },
    { "Weapons", new Vector2(15.2f, -2.5f) }
};

public static Dictionary<string, Vector2> airshipTeleportLocations = new Dictionary<string, Vector2>()
{
    { "Cockpit", new Vector2(-30f, 15f) },
    { "Vault", new Vector2(-15f, 15f) },
    { "Brig", new Vector2(-5f, 10f) },
    { "Meeting Room", new Vector2(10f, 12f) },
    { "Records", new Vector2(25f, 12f) },
    { "Lounge", new Vector2(35f, 8f) },
    { "Kitchen", new Vector2(25f, -5f) }
};

public static Dictionary<string, Vector2> fungleTeleportLocations = new Dictionary<string, Vector2>()
{
    { "Beach", new Vector2(0f, -20f) },
    { "Jungle", new Vector2(15f, 10f) },
    { "Lookout", new Vector2(-10f, 25f) },
    { "Laboratory", new Vector2(-25f, 0f) },
    { "Storage", new Vector2(5f, -5f) }
};

public static int GetCurrentMapId()
        {
            if (TryGetLiveShipMapId(out int liveMapId))
                return liveMapId;

            if (manualSpawnedMapId >= 0)
                return manualSpawnedMapId;

            if (AmongUsClient.Instance == null) return 0;
            if (AmongUsClient.Instance.NetworkMode == NetworkModes.FreePlay)
            {
                return NormalizeRuntimeMapId(AmongUsClient.Instance.TutorialMapId);
            }
            else
            {
                if (GameOptionsManager.Instance == null || GameOptionsManager.Instance.CurrentGameOptions == null) return 0;
                return NormalizeRuntimeMapId(GameOptionsManager.Instance.CurrentGameOptions.MapId);
            }
        }

private static bool TryGetLiveShipMapId(out int mapId)
        {
            mapId = -1;

            try
            {
                ShipStatus ship = ShipStatus.Instance;
                if (ship == null) return false;

                string typeName = ship.GetType().Name ?? string.Empty;
                string objectName = string.Empty;
                try { objectName = ((Component)ship).gameObject.name ?? string.Empty; } catch { }
                string marker = (typeName + " " + objectName).ToLowerInvariant();

                if (marker.Contains("fungle")) { mapId = 5; return true; }
                if (marker.Contains("airship")) { mapId = 4; return true; }
                if (marker.Contains("polus")) { mapId = 2; return true; }
                if (marker.Contains("mira")) { mapId = 1; return true; }
                if (marker.Contains("skeld")) { mapId = 0; return true; }

                switch (ship.Type)
                {
                    case ShipStatus.MapType.Hq:
                        mapId = 1;
                        return true;
                    case ShipStatus.MapType.Pb:
                        mapId = 2;
                        return true;
                    case ShipStatus.MapType.Fungle:
                        mapId = 5;
                        return true;
                    case ShipStatus.MapType.Ship:
                        mapId = 0;
                        return true;
                }
            }
            catch { }

            return false;
        }

private static int NormalizeRuntimeMapId(int mapId)
        {
            if (mapId == 3) return 0;
            return mapId;
        }

private Vector2 mapsScrollPos = Vector2.zero;

public static Dictionary<string, Vector2> GetTeleportLocations()
        {
            if (TryBuildLiveRoomTeleportLocations(out Dictionary<string, Vector2> liveLocations))
                return liveLocations;

            switch (GetCurrentMapId())
            {
                case 0: return skeldTeleportLocations;
                case 1: return miraTeleportLocations;
                case 2: return polusTeleportLocations;
                case 3: return skeldTeleportLocations;
                case 4: return airshipTeleportLocations;
                case 5: return fungleTeleportLocations;
                default: return skeldTeleportLocations;
            }
        }

private static bool TryBuildLiveRoomTeleportLocations(out Dictionary<string, Vector2> locations)
        {
            locations = new Dictionary<string, Vector2>();

            try
            {
                ShipStatus ship = ShipStatus.Instance;
                if (ship == null || ship.AllRooms == null) return false;

                foreach (PlainShipRoom room in ship.AllRooms)
                {
                    if (room == null || room.roomArea == null) continue;

                    Bounds bounds = room.roomArea.bounds;
                    if (bounds.size.sqrMagnitude < 0.01f) continue;

                    string label = GetRoomTeleportLabel(room.RoomId);
                    if (string.IsNullOrWhiteSpace(label)) continue;

                    Vector2 position = ResolveRoomTeleportPosition(label, bounds);
                    AddUniqueTeleportLocation(locations, label, position);
                }
            }
            catch
            {
                locations.Clear();
                return false;
            }

            return locations.Count > 0;
        }

private static Vector2 ResolveRoomTeleportPosition(string label, Bounds bounds)
        {
            Vector2 center = new Vector2(bounds.center.x, bounds.center.y);

            try
            {
                if (TryGetStaticRoomTeleportPosition(label, out Vector2 staticPosition) && IsSafeRoomTeleportPoint(staticPosition))
                    return staticPosition;

                Vector2 safePosition = FindSafeRoomTeleportPoint(bounds, center);
                if (IsSafeRoomTeleportPoint(safePosition))
                    return safePosition;
            }
            catch { }

            return center;
        }

private static bool TryGetStaticRoomTeleportPosition(string label, out Vector2 position)
        {
            position = default;

            try
            {
                Dictionary<string, Vector2> staticLocations = null;
                switch (GetCurrentMapId())
                {
                    case 0:
                    case 3:
                        staticLocations = skeldTeleportLocations;
                        break;
                    case 1:
                        staticLocations = miraTeleportLocations;
                        break;
                    case 2:
                        staticLocations = polusTeleportLocations;
                        break;
                    case 4:
                        staticLocations = airshipTeleportLocations;
                        break;
                    case 5:
                        staticLocations = fungleTeleportLocations;
                        break;
                }

                return staticLocations != null && staticLocations.TryGetValue(label, out position);
            }
            catch
            {
                position = default;
                return false;
            }
        }

private static Vector2 FindSafeRoomTeleportPoint(Bounds bounds, Vector2 center)
        {
            const float margin = 0.45f;

            if (IsSafeRoomTeleportPoint(center))
                return center;

            float xLimit = Mathf.Max(0f, bounds.extents.x - margin);
            float yLimit = Mathf.Max(0f, bounds.extents.y - margin);
            float[] distances = { 0.65f, 1.1f, 1.65f, 2.25f, 3.0f };

            Vector2 bestFallback = center;
            float bestFallbackScore = float.MaxValue;

            foreach (float distance in distances)
            {
                Vector2[] offsets =
                {
                    new Vector2(0f, distance),
                    new Vector2(distance, 0f),
                    new Vector2(0f, -distance),
                    new Vector2(-distance, 0f),
                    new Vector2(distance * 0.7f, distance * 0.7f),
                    new Vector2(distance * 0.7f, -distance * 0.7f),
                    new Vector2(-distance * 0.7f, distance * 0.7f),
                    new Vector2(-distance * 0.7f, -distance * 0.7f)
                };

                foreach (Vector2 offset in offsets)
                {
                    Vector2 candidate = new Vector2(
                        center.x + Mathf.Clamp(offset.x, -xLimit, xLimit),
                        center.y + Mathf.Clamp(offset.y, -yLimit, yLimit));

                    float score = (candidate - center).sqrMagnitude;
                    if (score < bestFallbackScore)
                    {
                        bestFallback = candidate;
                        bestFallbackScore = score;
                    }

                    if (IsSafeRoomTeleportPoint(candidate))
                        return candidate;
                }
            }

            return bestFallback;
        }

private static bool IsSafeRoomTeleportPoint(Vector2 position)
        {
            try
            {
                Collider2D[] hits = Physics2D.OverlapCircleAll(position, 0.32f, Constants.ShipOnlyMask);
                if (hits == null) return true;

                Collider2D localCollider = null;
                try { localCollider = PlayerControl.LocalPlayer?.Collider; } catch { }

                foreach (Collider2D hit in hits)
                {
                    if (hit == null || hit.isTrigger || hit == localCollider) continue;
                    return false;
                }

                return true;
            }
            catch
            {
                return true;
            }
        }

private static void AddUniqueTeleportLocation(Dictionary<string, Vector2> locations, string label, Vector2 position)
        {
            if (!locations.ContainsKey(label))
            {
                locations[label] = position;
                return;
            }

            int index = 2;
            string key;
            do
            {
                key = $"{label} {index}";
                index++;
            }
            while (locations.ContainsKey(key));

            locations[key] = position;
        }

private static string GetRoomTeleportLabel(SystemTypes room)
        {
            switch (room)
            {
                case SystemTypes.LifeSupp: return "O2";
                case SystemTypes.Nav: return "Navigation";
                case SystemTypes.Comms: return "Communications";
                case SystemTypes.MedBay: return "Medbay";
                case SystemTypes.UpperEngine: return "Upper Engine";
                case SystemTypes.LowerEngine: return "Lower Engine";
                case SystemTypes.LockerRoom: return "Locker Room";
                case SystemTypes.Specimens: return "Specimen Room";
                case SystemTypes.VaultRoom: return "Vault";
                case SystemTypes.ViewingDeck: return "Viewing Deck";
                case SystemTypes.HallOfPortraits: return "Hall of Portraits";
                case SystemTypes.CargoBay: return "Cargo Bay";
                case SystemTypes.GapRoom: return "Gap Room";
                case SystemTypes.MainHall: return "Main Hall";
                case SystemTypes.MeetingRoom: return "Meeting Room";
                case SystemTypes.RecRoom: return "Rec Room";
                case SystemTypes.FishingDock: return "Fishing Dock";
                case SystemTypes.SleepingQuarters: return "Sleeping Quarters";
                case SystemTypes.Decontamination2: return "Decontamination 2";
                case SystemTypes.Decontamination3: return "Decontamination 3";
                default: return room.ToString();
            }
        }

public static void TeleportTo(Vector2 position)
        {
            if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.NetTransform == null) return;
            if (UseSnapToRPC)
            {
                PlayerControl.LocalPlayer.NetTransform.RpcSnapTo(position);
            }
            else
            {
                PlayerControl.LocalPlayer.NetTransform.SnapTo(position);
            }
        }

private static int GetVentCount()
        {
            try { return ShipStatus.Instance != null && ShipStatus.Instance.AllVents != null ? ShipStatus.Instance.AllVents.Count : 0; }
            catch { return 0; }
        }

private static string GetVentLabel(int idx)
        {
            try
            {
                if (ShipStatus.Instance == null || ShipStatus.Instance.AllVents == null || ShipStatus.Instance.AllVents.Count == 0) return "-";
                idx = Mathf.Clamp(idx, 0, ShipStatus.Instance.AllVents.Count - 1);
                Vent vent = ShipStatus.Instance.AllVents[idx];
                return vent != null ? GetVentRoomLabel((ushort)vent.Id) : idx.ToString();
            }
            catch { return idx.ToString(); }
        }

private static int GetVentIdByIndex(int idx)
        {
            try
            {
                if (ShipStatus.Instance == null || ShipStatus.Instance.AllVents == null || ShipStatus.Instance.AllVents.Count == 0) return 0;
                idx = Mathf.Clamp(idx, 0, ShipStatus.Instance.AllVents.Count - 1);
                Vent vent = ShipStatus.Instance.AllVents[idx];
                return vent != null ? vent.Id : idx;
            }
            catch { return idx; }
        }

private static void TeleportPlayerToVent(PlayerControl player, int ventIdx, bool notify = true)
        {
            if (player == null || player.Data == null || ShipStatus.Instance == null)
            {
                if (notify) ShowNotification("<color=#FF4444>[VENT]</color> No map.");
                return;
            }

            int ventId = GetVentIdByIndex(ventIdx);
            try
            {
                if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
                {
                    TryHostVentTp(player, ventId);
                }
                else
                {
                    SendVentTpUpdate(player, ventId);
                }

                if (notify) ShowNotification($"<color=#00FF00>[VENT]</color> {player.Data.PlayerName} -> {GetVentRoomLabel((ushort)ventId)}");
            }
            catch { }
        }

private static void TeleportAllPlayersToVent(int ventIdx)
        {
            if (PlayerControl.AllPlayerControls == null || ShipStatus.Instance == null) return;
            int cnt = 0;
            foreach (PlayerControl pc in PlayerControl.AllPlayerControls)
            {
                if (pc == null || pc.Data == null || pc.Data.Disconnected) continue;
                TeleportPlayerToVent(pc, ventIdx, false);
                cnt++;
            }
            int ventId = GetVentIdByIndex(ventIdx);
            ShowNotification($"<color=#00FF00>[VENT]</color> All -> {GetVentRoomLabel((ushort)ventId)} ({cnt})");
        }

private static void SendVentTpUpdate(PlayerControl player, int ventId)
        {
            if (AmongUsClient.Instance == null || PlayerControl.LocalPlayer == null || player == null) return;

            if (!TryGetVentSys(out SystemTypes sys)) return;

            if (!ventTpSeq.TryGetValue(player.PlayerId, out ushort seq))
                seq = 10000;

            MessageWriter batch = null;
            MessageWriter enter = null;
            MessageWriter boot = null;
            try
            {
                batch = MessageWriter.Get(SendOption.Reliable);
                batch.StartMessage(InnerNet.Tags.GameDataTo);
                batch.Write(AmongUsClient.Instance.GameId);
                batch.WritePacked(AmongUsClient.Instance.HostId);

                enter = MessageWriter.Get(SendOption.None);
                enter.Write(++seq);
                enter.Write((byte)VentilationSystem.Operation.Enter);
                enter.Write((byte)ventId);
                WriteVentTpUpdate(batch, player, sys, enter);

                boot = MessageWriter.Get(SendOption.None);
                boot.Write(++seq);
                boot.Write((byte)VentilationSystem.Operation.BootImpostors);
                boot.Write((byte)ventId);
                WriteVentTpUpdate(batch, player, sys, boot);

                ventTpSeq[player.PlayerId] = seq;
                batch.EndMessage();
                AmongUsClient.Instance.SendOrDisconnect(batch);
            }
            finally
            {
                try { enter?.Recycle(); } catch { }
                try { boot?.Recycle(); } catch { }
                try { batch?.Recycle(); } catch { }
            }
        }

private static bool TryHostVentTp(PlayerControl player, int ventId)
        {
            try
            {
                if (player == null || player.MyPhysics == null) return false;
                player.MyPhysics.RpcBootFromVent(ventId);
                return true;
            }
            catch { return false; }
        }

private static bool TryGetVentSys(out SystemTypes sys)
        {
            sys = SystemTypes.Ventilation;
            try
            {
                if (ShipStatus.Instance == null || ShipStatus.Instance.Systems == null) return false;
                if (ShipStatus.Instance.Systems.ContainsKey(SystemTypes.Ventilation)) return true;

                foreach (var pair in ShipStatus.Instance.Systems)
                {
                    try
                    {
                        if (pair.Value == null) continue;
                        VentilationSystem vent = pair.Value.TryCast<VentilationSystem>();
                        if (vent == null) continue;
                        sys = pair.Key;
                        return true;
                    }
                    catch { }
                }
            }
            catch { }

            return false;
        }

private static Vector2 GetVentPos(int ventId)
        {
            try
            {
                if (ShipStatus.Instance != null && ShipStatus.Instance.AllVents != null)
                {
                    foreach (Vent v in ShipStatus.Instance.AllVents)
                        if (v != null && v.Id == ventId)
                            return v.transform.position;
                }
            }
            catch { }

            return PlayerControl.LocalPlayer != null ? (Vector2)PlayerControl.LocalPlayer.transform.position : Vector2.zero;
        }

private static void WriteVentTpUpdate(MessageWriter batch, PlayerControl player, SystemTypes sys, MessageWriter msg)
        {
            batch.StartMessage((byte)GameDataTypes.RpcFlag);
            batch.WritePacked(ShipStatus.Instance.NetId);
            batch.Write((byte)RpcCalls.UpdateSystem);
            batch.Write((byte)sys);
            batch.WriteNetObject(player);
            batch.Write(msg, false);
            batch.EndMessage();
        }

private int currentTab = 0;

private int targetTabIndex = 0;

private float tabTransitionProgress = 1f;

private int tabTransitionDir = 1;

private Rect tabHighlightRect;

private bool tabHighlightReady = false;

private Vector2 scrollPosition = Vector2.zero;
}
}

