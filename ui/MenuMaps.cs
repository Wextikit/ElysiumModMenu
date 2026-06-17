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

                bool shouldBlock = AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost && ElysiumModMenuGUI.disableVoteKicks;
                try
                {
                    Hazel.MessageReader copy = Hazel.MessageReader.Get(reader);
                    int targetClientId = copy.ReadInt32();
                    int voterClientId = copy.ReadInt32();
                    string targetName = ResolveVoteClientName(targetClientId);
                    string voterName = ResolveVoteClientName(voterClientId);

                    ShowVoteKickChatInfo(voterName, targetName);
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

            private static string ResolveVoteClientName(int clientId)
            {
                try
                {
                    if (PlayerControl.AllPlayerControls != null)
                    {
                        foreach (var pc in PlayerControl.AllPlayerControls)
                        {
                            if (pc == null || pc.Data == null) continue;
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

[HarmonyPatch(typeof(ShhhBehaviour), nameof(ShhhBehaviour.PlayAnimation))]
        public static class SkipShhh_Perfect_Patch
        {
            public static bool Prefix(ShhhBehaviour __instance, ref Il2CppSystem.Collections.IEnumerator __result)
            {
                if (!ElysiumModMenuGUI.skipShhhAnim || __instance == null) return true;

                __instance.gameObject.SetActive(false);

                __result = FastSkip().WrapToIl2Cpp();
                return false;
            }

            private static System.Collections.IEnumerator FastSkip() { yield break; }
        }

private void SpawnMap(int mapId)
        {
            try
            {
                if ((UnityEngine.Object)(object)AmongUsClient.Instance == (UnityEngine.Object)null || AmongUsClient.Instance.ShipPrefabs == null)
                    return;

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
            AmongUsClient.Instance.ShipLoadingAsyncHandle = AmongUsClient.Instance.ShipPrefabs[mapId].InstantiateAsync((Transform)null, false);
            yield return AmongUsClient.Instance.ShipLoadingAsyncHandle;

            ShipStatus.Instance = AmongUsClient.Instance.ShipLoadingAsyncHandle.Result.GetComponent<ShipStatus>();
            ((InnerNetClient)AmongUsClient.Instance).Spawn(((Component)ShipStatus.Instance).GetComponent<InnerNetObject>(), -2, (SpawnFlags)0);

        }

private void DespawnMap()
        {
            try
            {
                if (ShipStatus.Instance != null)
                {
                    ShipStatus.Instance.Despawn();
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
            if (AmongUsClient.Instance == null) return 0;
            if (AmongUsClient.Instance.NetworkMode == NetworkModes.FreePlay)
            {
                return AmongUsClient.Instance.TutorialMapId;
            }
            else
            {
                if (GameOptionsManager.Instance == null || GameOptionsManager.Instance.CurrentGameOptions == null) return 0;
                return GameOptionsManager.Instance.CurrentGameOptions.MapId;
            }
        }

private Vector2 mapsScrollPos = Vector2.zero;

public static Dictionary<string, Vector2> GetTeleportLocations()
        {
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

private int currentTab = 0;

private int targetTabIndex = 0;

private float tabTransitionProgress = 1f;

private Vector2 scrollPosition = Vector2.zero;
}
}
