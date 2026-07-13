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


[HarmonyPatch(typeof(ChatBubble), nameof(ChatBubble.SetText))]
public static class DarkMode_ChatBubblePatch
{
    public static void Postfix(ChatBubble __instance)
    {
        try
        {
            if (!ElysiumModMenuGUI.enableChatDarkMode) return;

            Transform bg = __instance.transform.Find("Background");
            if (bg != null)
            {
                var sr = bg.GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = new Color32(35, 35, 35, 255);
            }
            if (__instance.TextArea != null)
                __instance.TextArea.color = Color.white;
        }
        catch { }
    }
}
