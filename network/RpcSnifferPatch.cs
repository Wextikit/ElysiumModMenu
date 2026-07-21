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


[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.HandleRpc))]
public static class RPCSniffer_Patch
{
    private static readonly HashSet<byte> VanillaRPCs = ElysiumModMenuGUI.VanillaRpcIds;

    private static readonly Dictionary<byte, (string Name, string Color)> KnownMods = BuildKnownMods();

    private static Dictionary<byte, (string Name, string Color)> BuildKnownMods()
        {
            Dictionary<byte, (string Name, string Color)> known = new Dictionary<byte, (string, string)>();
            int count = Mathf.Min(ElysiumModMenuGUI.spoofMenuRPCs.Length, ElysiumModMenuGUI.spoofMenuNames.Length);
            for (int i = 0; i < count; i++)
            {
                byte rpc = ElysiumModMenuGUI.spoofMenuRPCs[i];
                if (known.ContainsKey(rpc)) continue;
                known[rpc] = (CleanSpoofMenuName(ElysiumModMenuGUI.spoofMenuNames[i]), ColorForKnownRpc(rpc));
            }
            return known;
        }

    private static string CleanSpoofMenuName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "Known Mod";
            return Regex.Replace(name, @"\s*\(\d+\)\s*$", string.Empty).Trim();
        }

    private static string ColorForKnownRpc(byte rpc)
        {
            if (rpc == 89 || rpc == 133) return "#00FFFF";
            if (rpc == 176 || rpc == 151 || rpc == 152 || (rpc >= 212 && rpc <= 219)) return "#008000";
            if (rpc == 195 || rpc == 204 || rpc == 210 || rpc == 255) return "#FFFF00";
            if (rpc == 157 || rpc == 121) return "#800000";
            if (rpc == 202) return "#FF8C00";
            return "#FF0000";
        }

    public static bool Prefix(PlayerControl __instance, byte callId, MessageReader reader)
    {
        if (__instance == null) return true;


        if (PlayerControl.LocalPlayer != null && __instance == PlayerControl.LocalPlayer) return true;

        ElysiumModMenuGUI.RecordPlayerRpc(__instance, callId);

        if (ElysiumModMenuGUI.LogAllRPCs)
        {

            if (!VanillaRPCs.Contains(callId))
            {
                string pNameSniff = (__instance.Data != null && !string.IsNullOrEmpty(__instance.Data.PlayerName)) ? __instance.Data.PlayerName : $"Player_{__instance.PlayerId}";


                if (KnownMods.TryGetValue(callId, out var modInfo))
                {
                    ElysiumModMenuGUI.ShowNotification($"<color=#00FFFF>[RPC SPOOF]</color> <b>{pNameSniff}</b>: <b><color={modInfo.Color}>{modInfo.Name}</color></b> <color=#FFFF00>({callId})</color>");
                }
                else
                {
                    ElysiumModMenuGUI.ShowNotification($"<color=#00FFFF>[RPC SPOOF]</color> <b>{pNameSniff}</b> unknown RPC: <color=#FFFF00>{callId}</color>");
                }
            }
        }

        if (ElysiumModMenuGUI.blockSpoofRPC &&
            AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost &&
            !VanillaRPCs.Contains(callId))
        {
            int clientId = ElysiumNetGuard.NetworkGuard.ResolveCurrentRpcSenderClientId(__instance, callId);
            if (clientId < 0 && __instance.Data != null) clientId = __instance.Data.ClientId;
            if (clientId < 0) clientId = __instance.OwnerId;

            if (clientId >= 0 && !ElysiumModMenuGUI.IsProtectedFromAnticheat(clientId) &&
                !ElysiumModMenuGUI.IsProtectedFromAnticheat(__instance))
            {
                string rpcName = KnownMods.TryGetValue(callId, out var modInfo) ? modInfo.Name : "Unknown";
                ElysiumNetGuard.NetworkGuard.ApplyMenuProtection(clientId, "Custom RPC", $"{rpcName} ({callId})");
            }

            return false;
        }

        return true;
    }
}

[HarmonyPatch(typeof(InnerNetClient), "HandleGameData")]
public static class RPCSniffer_RawGameDataPatch
{
    private static readonly HashSet<byte> VanillaRPCs = ElysiumModMenuGUI.VanillaRpcIds;

    public static void Prefix(InnerNetClient __instance, MessageReader parentReader)
    {
        if (__instance == null || parentReader == null || !ElysiumModMenuGUI.LogAllRPCs)
            return;

        int originalPosition = parentReader.Position;
        MessageReader copy = null;
        try
        {
            copy = MessageReader.Get(parentReader);
            while (copy.Position < copy.Length)
            {
                MessageReader part = copy.ReadMessage();
                if (part == null)
                    break;

                try
                {
                    if ((GameDataTypes)part.Tag != GameDataTypes.RpcFlag)
                        continue;

                    uint netId = part.ReadPackedUInt32();
                    byte callId = part.ReadByte();
                    if (VanillaRPCs.Contains(callId))
                        continue;

                    string objectLabel = ResolveRpcObjectLabel(__instance, netId);
                    string rpcLabel = ResolveRpcLabel(callId);
                    string line = $"Raw RPC recv: {rpcLabel} ({callId}), netId={netId}, object={objectLabel}, localClient={__instance.ClientId}, gameId={__instance.GameId}";
                    try { Plugin.Instance?.Log?.LogInfo((object)line); } catch { }
                    ElysiumModMenuGUI.ShowNotification($"<color=#00FFFF>[RAW RPC]</color> {objectLabel}: <b>{rpcLabel}</b> <color=#FFFF00>({callId})</color>");
                }
                finally
                {
                    part.Recycle();
                }
            }
        }
        catch (Exception error)
        {
            try { Plugin.Instance?.Log?.LogWarning((object)$"Raw RPC sniffer failed: {error.Message}"); } catch { }
        }
        finally
        {
            copy?.Recycle();
            try { parentReader.Position = originalPosition; } catch { }
        }
    }

    private static string ResolveRpcObjectLabel(InnerNetClient client, uint netId)
    {
        try
        {
            if (client.allObjects != null && client.allObjects.AllObjectsFast.TryGetValue(netId, out InnerNetObject obj) && obj != null)
            {
                PlayerControl player = obj.TryCast<PlayerControl>();
                if (player != null)
                {
                    string playerName = player.Data != null && !string.IsNullOrEmpty(player.Data.PlayerName)
                        ? player.Data.PlayerName
                        : $"Player_{player.PlayerId}";
                    if (SameRpcName(playerName, obj.name)) return playerName;
                    return $"{playerName}/{obj.name}";
                }

                return obj.name;
            }
        }
        catch { }

        return $"unknown:{netId}";
    }

    private static bool SameRpcName(string a, string b)
    {
        string ca = CleanRpcName(a);
        string cb = CleanRpcName(b);
        return ca.Length > 0 && string.Equals(ca, cb, StringComparison.OrdinalIgnoreCase);
    }

    private static string CleanRpcName(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return string.Empty;
        return Regex.Replace(s, "<.*?>", string.Empty).Trim();
    }

    private static string ResolveRpcLabel(byte callId)
    {
        try
        {
            for (int i = 0; i < ElysiumModMenuGUI.spoofMenuRPCs.Length && i < ElysiumModMenuGUI.spoofMenuNames.Length; i++)
            {
                if (ElysiumModMenuGUI.spoofMenuRPCs[i] == callId)
                    return Regex.Replace(ElysiumModMenuGUI.spoofMenuNames[i], @"\s*\(\d+\)\s*$", string.Empty).Trim();
            }
        }
        catch { }

        return "Unknown RPC";
    }
}
