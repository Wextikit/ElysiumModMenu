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

[HarmonyPatch(typeof(PlatformSpecificData), nameof(PlatformSpecificData.Serialize))]
public static class PlatformSpooferPatch
{
    public static void Prefix(PlatformSpecificData __instance)
    {
        try
        {
            if (__instance != null)
            {
                if (!ElysiumModMenuGUI.enablePlatformSpoof) return;

                int platformIndex = Mathf.Clamp(ElysiumModMenuGUI.currentPlatformIndex, 0, ElysiumModMenuGUI.platformValues.Length - 1);
                __instance.Platform = ElysiumModMenuGUI.platformValues[platformIndex];
                __instance.PlatformName = "ElysiumModMenu by Meowchelo (and one <color=#FFA500>silly</color> guy :p) https://github.com/meowchelo/ElysiumModMenu";
            }
        }
        catch { }
    }
}
