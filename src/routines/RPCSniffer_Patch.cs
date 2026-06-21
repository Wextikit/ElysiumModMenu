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

    private static readonly Dictionary<byte, (string Name, string Color)> KnownMods = new Dictionary<byte, (string, string)>
        {
            { 157, ("RockStar", "#800000") },
            { 121, ("RockStar / Chocoo", "#800000") },
            { 167, ("TuffMenu", "#008000") },
            { 164, ("Hydra / Sicko", "#FF0000") },
            { 176, ("HostGuard / TOH", "#008000") },
            { 195, ("Polar Client", "#FFFF00") },
            { 204, ("Polar Client", "#FFFF00") },
            { 154, ("GNC", "#FF0000") },
            { 85,  ("KillNet (Base)", "#FF0000") },
            { 150, ("KillNet (V2)", "#FF0000") },
            { 162, ("KNM", "#FF0000") },
            { 250, ("KillNet (Alt)", "#FF0000") },
            { 212, ("BanMod", "#008000") },
            { 213, ("BanMod", "#008000") },
            { 214, ("BanMod", "#008000") },
            { 215, ("BanMod", "#008000") },
            { 216, ("BanMod", "#008000") },
            { 217, ("BanMod", "#008000") },
            { 218, ("BanMod", "#008000") },
            { 219, ("BanMod", "#008000") },
            { 144, ("Gaff Menu", "#FF0000") },
            { 145, ("Gaff Menu", "#FF0000") },
            { 188, ("GMM", "#FF0000") },
            { 189, ("GMM", "#FF0000") },
            { 169, ("Malum", "#FF0000") },
            { 210, ("Eclipse", "#FFFF00") },
            { 173, ("Private Client", "#FF0000") },
            { 151, ("Better Among Us", "#008000") },
            { 152, ("Better Among Us", "#008000") },
            { 255, ("CrewMod", "#FFFF00") },
            { 111, ("AUM (BitCrackers)", "#FF0000") },
            { 231, ("SentinelAU", "#FF0000") },
            { 133, ("Lunar / ElysiumModMenu", "#00FFFF") },
            { 89,  ("ElysiumModMenu Old", "#008000") }
        };

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
                    ElysiumModMenuGUI.ShowNotification($"<color=#00FFFF>[СНИФФЕР]</color> <b>{pNameSniff}</b>: <b><color={modInfo.Color}>{modInfo.Name}</color></b> <color=#FFFF00>({callId})</color>");
                }
                else
                {
                    ElysiumModMenuGUI.ShowNotification($"<color=#00FFFF>[СНИФФЕР]</color> <b>{pNameSniff}</b> кинул неизвестный RPC: <color=#FFFF00>{callId}</color>");
                }
            }
        }
        return true;
    }
}
