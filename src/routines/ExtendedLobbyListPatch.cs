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


[HarmonyPatch(typeof(FindAGameManager), nameof(FindAGameManager.Start))]
public static class ExtendedLobbyListPatch
{
    public static Scroller scroller;

    public static bool Prefix(FindAGameManager __instance)
    {
        if (!ElysiumModMenuGUI.extendedLobby) return true;
        try
        {
            if (__instance.gameContainers == null || __instance.gameContainers.Count == 0) return true;
            if (__instance.gameContainers.Count > 10) return true;

            GameContainer prefab = __instance.gameContainers[0];
            GameObject holder = new GameObject("ExtendedLobbyScroller");
            holder.transform.SetParent(prefab.transform.parent);

            scroller = holder.AddComponent<Scroller>();
            scroller.Inner = holder.transform;
            scroller.MouseMustBeOverToScroll = true;
            scroller.allowY = true;
            scroller.ScrollWheelSpeed = 0.4f;
            scroller.SetYBoundsMin(0f);
            scroller.SetYBoundsMax(4f);

            BoxCollider2D collider = prefab.transform.parent.gameObject.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(100f, 100f);
            scroller.ClickMask = collider;

            var list = new System.Collections.Generic.List<GameContainer>();
            foreach (var gc in __instance.gameContainers)
            {
                gc.transform.SetParent(holder.transform);
                gc.transform.localPosition = new Vector3(gc.transform.localPosition.x, gc.transform.localPosition.y, 25f);
                list.Add(gc);
            }

            for (int i = 0; i < 15; i++)
            {
                GameContainer newGc = UnityEngine.Object.Instantiate<GameContainer>(prefab, holder.transform);
                newGc.transform.localPosition = new Vector3(newGc.transform.localPosition.x, newGc.transform.localPosition.y - 0.75f * list.Count, 25f);
                list.Add(newGc);
            }

            __instance.gameContainers = new Il2CppReferenceArray<GameContainer>(list.ToArray());
            return true;
        }
        catch { return true; }
    }
}
