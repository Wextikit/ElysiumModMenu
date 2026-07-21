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
private static bool zoomOwnsCamera = false;
private static float zoomBaseMainSize = 3f;
private static float zoomBaseUiSize = 3f;
private static float zoomLastMainSize = 3f;
private static float zoomLastUiSize = 3f;

private static void ApplyCameraZoomTick()
        {
            try
            {
                Camera mainCamera = Camera.main;
                Camera uiCamera = HudManager.Instance?.UICamera;
                if (mainCamera == null) return;

                if (IsHudModalActive() || !cameraZoom)
                {
                    if (!zoomOwnsCamera) return;

                    if (Mathf.Abs(mainCamera.orthographicSize - zoomLastMainSize) <= 0.01f)
                        mainCamera.orthographicSize = zoomBaseMainSize;
                    if (uiCamera != null && Mathf.Abs(uiCamera.orthographicSize - zoomLastUiSize) <= 0.01f)
                        uiCamera.orthographicSize = zoomBaseUiSize;

                    zoomOwnsCamera = false;
                    if (zoomResolutionRefreshNeeded)
                        RefreshHudResolutionForZoom();
                    zoomResolutionRefreshNeeded = false;

                    return;
                }

                float scrollWheel = Input.GetAxis("Mouse ScrollWheel");
                if (Mathf.Abs(scrollWheel) <= 0.0001f)
                {
                    if (zoomOwnsCamera &&
                        (Mathf.Abs(mainCamera.orthographicSize - zoomLastMainSize) > 0.01f ||
                         (uiCamera != null && Mathf.Abs(uiCamera.orthographicSize - zoomLastUiSize) > 0.01f)))
                    {
                        zoomOwnsCamera = false;
                        if (zoomResolutionRefreshNeeded)
                            RefreshHudResolutionForZoom();
                        zoomResolutionRefreshNeeded = false;
                    }
                    return;
                }
                if (!IsCameraZoomScrollAllowed()) return;
                if (scrollWheel > 0f && mainCamera.orthographicSize <= 3f) return;

                if (!zoomOwnsCamera)
                {
                    zoomBaseMainSize = mainCamera.orthographicSize;
                    zoomBaseUiSize = uiCamera != null ? uiCamera.orthographicSize : 3f;
                    zoomOwnsCamera = true;
                }

                if (scrollWheel < 0f)
                {
                    mainCamera.orthographicSize += 1f;
                    if (uiCamera != null) uiCamera.orthographicSize += 1f;
                    zoomLastMainSize = mainCamera.orthographicSize;
                    zoomLastUiSize = uiCamera != null ? uiCamera.orthographicSize : zoomBaseUiSize;
                    zoomResolutionRefreshNeeded = true;
                    RefreshHudResolutionForZoom();
                }
                else if (scrollWheel > 0f && mainCamera.orthographicSize > 3f)
                {
                    mainCamera.orthographicSize -= 1f;
                    if (uiCamera != null) uiCamera.orthographicSize = Mathf.Max(3f, uiCamera.orthographicSize - 1f);
                    zoomLastMainSize = mainCamera.orthographicSize;
                    zoomLastUiSize = uiCamera != null ? uiCamera.orthographicSize : zoomBaseUiSize;
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
