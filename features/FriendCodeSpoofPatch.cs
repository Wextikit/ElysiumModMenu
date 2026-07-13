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


[HarmonyPatch(typeof(NetworkedPlayerInfo), nameof(NetworkedPlayerInfo.Serialize))]
public static class FriendCodeSpooferPatch
{
    private static string serializeRestoreValue = null;

    public static void Prefix(NetworkedPlayerInfo __instance)
    {
        try
        {
            serializeRestoreValue = null;
            if (ElysiumModMenuGUI.PrepareLocalFriendCodeForSerialize(__instance, out serializeRestoreValue)) return;
            if (!ElysiumModMenuGUI.enableFriendCodeSpoof) return;
            if (__instance == null || PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data == null) return;
            if (__instance.PlayerId != PlayerControl.LocalPlayer.PlayerId) return;

            string input = ElysiumModMenuGUI.spoofFriendCodeInput ?? "";
            string clean = "";
            foreach (char c in input.ToLowerInvariant())
            {
                if (char.IsWhiteSpace(c)) break;
                if (char.IsLetterOrDigit(c)) clean += c;
                if (clean.Length >= 10) break;
            }

            if (string.IsNullOrWhiteSpace(clean)) return;
            serializeRestoreValue = ElysiumModMenuGUI.GetCachedOriginalFriendCode(__instance, string.Empty);
            __instance.FriendCode = clean;
        }
        catch { }
    }

    public static void Postfix(NetworkedPlayerInfo __instance)
    {
        ElysiumModMenuGUI.RestoreLocalFriendCodeAfterSerialize(__instance, serializeRestoreValue);
        serializeRestoreValue = null;
    }
}
