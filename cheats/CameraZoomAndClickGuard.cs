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
private static void ApplyCameraZoomTick()
        {
            try
            {
                Camera mainCamera = Camera.main;
                Camera uiCamera = HudManager.Instance?.UICamera;
                if (mainCamera == null) return;

                if (IsHudModalActive())
                {
                    bool changed = Mathf.Abs(mainCamera.orthographicSize - 3f) > 0.001f ||
                                   (uiCamera != null && Mathf.Abs(uiCamera.orthographicSize - 3f) > 0.001f);

                    mainCamera.orthographicSize = 3f;
                    if (uiCamera != null) uiCamera.orthographicSize = 3f;

                    if (changed || zoomResolutionRefreshNeeded)
                    {
                        RefreshHudResolutionForZoom();
                        zoomResolutionRefreshNeeded = false;
                    }

                    return;
                }

                if (!cameraZoom)
                {
                    bool changed = Mathf.Abs(mainCamera.orthographicSize - 3f) > 0.001f ||
                                   (uiCamera != null && Mathf.Abs(uiCamera.orthographicSize - 3f) > 0.001f);

                    mainCamera.orthographicSize = 3f;
                    if (uiCamera != null) uiCamera.orthographicSize = 3f;

                    if (zoomResolutionRefreshNeeded || changed)
                    {
                        RefreshHudResolutionForZoom();
                        zoomResolutionRefreshNeeded = false;
                    }

                    return;
                }

                float scrollWheel = Input.GetAxis("Mouse ScrollWheel");
                if (Mathf.Abs(scrollWheel) <= 0.0001f || !IsCameraZoomScrollAllowed()) return;

                if (scrollWheel < 0f)
                {
                    mainCamera.orthographicSize += 1f;
                    if (uiCamera != null) uiCamera.orthographicSize += 1f;
                    zoomResolutionRefreshNeeded = true;
                    RefreshHudResolutionForZoom();
                }
                else if (scrollWheel > 0f && mainCamera.orthographicSize > 3f)
                {
                    mainCamera.orthographicSize -= 1f;
                    if (uiCamera != null) uiCamera.orthographicSize = Mathf.Max(3f, uiCamera.orthographicSize - 1f);
                    zoomResolutionRefreshNeeded = true;
                    RefreshHudResolutionForZoom();
                }
            }
            catch { }
        }

[HarmonyPatch(typeof(PassiveButton), nameof(PassiveButton.ReceiveClickDown))]
        public static class HardMenu_BlockClickDown_Patch
        {
            public static bool Prefix() { return !ElysiumModMenuGUI.IsCursorOverMenu(); }
        }

[HarmonyPatch(typeof(PassiveButton), nameof(PassiveButton.ReceiveClickUp))]
        public static class HardMenu_BlockClickUp_Patch
        {
            public static bool Prefix() { return !ElysiumModMenuGUI.IsCursorOverMenu(); }
        }
    }
}
