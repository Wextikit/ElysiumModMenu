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
using System.Globalization;
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
private void OpenSabotageMap()
        {
            try
            {
                if (DestroyableSingleton<HudManager>.Instance == null) return;
                DestroyableSingleton<HudManager>.Instance.ToggleMapVisible(new MapOptions
                {
                    Mode = MapOptions.Modes.Sabotage
                });
            }
            catch (global::System.Exception __elysiumCaught311) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught311); }
        }

private void DrawCustomRpcValidationInfo()
        {
            if (selectedSpoofMenuIndex != spoofMenuNames.Length - 1)
                return;

            GUIStyle statusStyle = new GUIStyle(toggleLabelStyle) { richText = true, fontSize = 11, wordWrap = true };
            string filtered = FilterSpoofRpcInput(customSpoofRpcInput);
            if (!int.TryParse(filtered, out int rpcId))
            {
                statusStyle.normal.textColor = new Color(1f, 0.35f, 0.35f, 1f);
                GUILayout.Label(L("Enter RPC ID.", "Введите ID RPC."), statusStyle, GUILayout.Height(18f));
                return;
            }

            if (VanillaRpcIds.Contains((byte)rpcId))
            {
                statusStyle.normal.textColor = new Color(1f, 0.35f, 0.35f, 1f);
                GUILayout.Label(L($"RPC {rpcId} is vanilla. It will not be sent.", $"RPC {rpcId} ванильный. Он не будет отправлен."), statusStyle, GUILayout.Height(18f));
                return;
            }

            statusStyle.normal.textColor = new Color(0.35f, 0.95f, 0.55f, 1f);
            GUILayout.Label(L($"RPC {rpcId} is custom. Sending is allowed.", $"RPC {rpcId} кастомный. Отправка разрешена."), statusStyle, GUILayout.Height(18f));
        }

private bool DrawCustomRpcInputButton(float width)
        {
            GUIStyle sourceStyle = customSpoofRpcInputFocused ? activeTabStyle : inputBlockStyle;
            GUIStyle style = new GUIStyle(btnStyle);
            style.normal.background = sourceStyle.normal.background;
            style.hover.background = sourceStyle.hover.background;
            style.active.background = sourceStyle.active.background;
            style.normal.textColor = sourceStyle.normal.textColor;
            style.hover.textColor = sourceStyle.hover.textColor;
            style.active.textColor = sourceStyle.active.textColor;
            style.alignment = TextAnchor.MiddleCenter;
            style.clipping = TextClipping.Clip;
            style.wordWrap = false;
            style.fontStyle = FontStyle.Bold;
            style.fontSize = 12;
            style.padding = CreateRectOffset(10, 10, 3, 3);
            style.fixedHeight = 25f;

            string preview = FormatInputPreview(customSpoofRpcInput, customSpoofRpcInputFocused, 12);
            bool clicked = GUILayout.Button(GUIContent.none, style, GUILayout.Width(width), GUILayout.Height(25f));
            Rect rect = GUILayoutUtility.GetLastRect();

            GUIStyle textStyle = new GUIStyle(style);
            textStyle.normal.background = null;
            textStyle.hover.background = null;
            textStyle.active.background = null;
            textStyle.padding = CreateRectOffset(0, 0, 0, 0);
            textStyle.margin = CreateRectOffset(0, 0, 0, 0);
            textStyle.contentOffset = new Vector2(0f, 1f);
            GUI.Label(rect, $"Custom RPC: {preview}", textStyle);
            return clicked;
        }

private void DrawSabotageButton(string label, ref bool state, Action<bool> toggleAction, Color accent, float width = 0f, float height = 30f)
        {
            GUIStyle style = CreateClippedButtonStyle(state ? activeTabStyle : btnStyle);
            Color oldBackground = GUI.backgroundColor;
            GUI.backgroundColor = state ? accent : Color.white;

            GUILayoutOption[] options = width > 0f
                ? new[] { GUILayout.Width(width), GUILayout.Height(height) }
                : new[] { GUILayout.Height(height) };
            if (GUILayout.Button(state ? label + "  ON" : label, style, options))
            {
                state = !state;
                toggleAction(state);
            }

            GUI.backgroundColor = oldBackground;
        }

private void DrawDoorTargetRow(SystemTypes room, float rowContentWidth)
        {
            GUIStyle rowStyle = new GUIStyle(boxStyle);
            rowStyle.padding.left = 3;
            rowStyle.padding.right = 3;
            rowStyle.padding.top = 1;
            rowStyle.padding.bottom = 1;
            GUILayout.BeginHorizontal(rowStyle, GUILayout.Width(rowContentWidth), GUILayout.Height(24));
            int cnt = 0;
            try
            {
                if (ShipStatus.Instance != null && ShipStatus.Instance.AllDoors != null)
                {
                    foreach (var door in ShipStatus.Instance.AllDoors)
                        if (door != null && door.Room == room) cnt++;
                }
            }
            catch (global::System.Exception __elysiumCaught312) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught312); }

            GUIStyle doorNameStyle = new GUIStyle(toggleLabelStyle)
            {
                clipping = TextClipping.Clip,
                wordWrap = false,
                fontSize = 11
            };
            float buttonGap = 2f;
            float buttonWidth = rowContentWidth < 130f ? 22f : (rowContentWidth < 150f ? 26f : 32f);
            float labelWidth = Mathf.Max(24f, rowContentWidth - (buttonWidth * 3f) - (buttonGap * 3f) - 12f);
            GUILayout.Label(cnt > 0 ? $"<b>{room}</b> <color=#888888>x{cnt}</color>" : $"<b>{room}</b>", doorNameStyle, GUILayout.Width(labelWidth), GUILayout.Height(22));

            if (GUILayout.Button("O", btnStyle, GUILayout.Width(buttonWidth), GUILayout.Height(22))) OpenDoorsOfType(room);
            GUILayout.Space(buttonGap);
            if (GUILayout.Button("L", activeSubTabStyle, GUILayout.Width(buttonWidth), GUILayout.Height(22))) LockDoorsOfType(room);
            GUILayout.Space(buttonGap);
            if (GUILayout.Button("C", btnStyle, GUILayout.Width(buttonWidth), GUILayout.Height(22))) CloseDoorsOfType(room);

            GUILayout.EndHorizontal();
        }

private void callMeetingPublic()
        {
            if (PlayerControl.LocalPlayer == null) return;
            try
            {
                if (AmongUsClient.Instance == null || !AmongUsClient.Instance.IsGameStarted || LobbyBehaviour.Instance != null || ShipStatus.Instance == null)
                {
                    ShowNotification("<color=#FF0000>[MEETING]</color> Match must be started.");
                    return;
                }

                if (MeetingHud.Instance != null || ExileController.Instance != null || IntroCutscene.Instance != null)
                {
                    ShowNotification("<color=#FFAA00>[MEETING]</color> Meeting/exile/intro is already active.");
                    return;
                }

                if (PlayerControl.LocalPlayer.Data != null && PlayerControl.LocalPlayer.Data.IsDead)
                {
                    ShowNotification("<color=#FF0000>[MEETING]</color> Local player is dead.");
                    return;
                }

                PlayerControl.LocalPlayer.CmdReportDeadBody(null);
                ShowNotification("<color=#00FF00>[MEETING]</color> Meeting called.");
            }
            catch (global::System.Exception __elysiumCaught313) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught313); }
        }

private void TriggerAllSabotages(bool notify = true)
        {
            if (ShipStatus.Instance == null) return;
            try
            {
                reactorSab = true;
                oxygenSab = true;
                commsSab = true;
                elecSab = true;

                ToggleReactor(true);
                ToggleO2(true);
                ToggleComms(true);
                ToggleLights(true);

                if (notify) ShowNotification("<color=#FF0000>[SABOTAGE]</color> All systems sabotaged!");
            }
            catch (global::System.Exception __elysiumCaught314) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught314); }
        }

private void FixAllSabotages(bool notify = true)
        {
            if (ShipStatus.Instance == null) return;
            try
            {
                reactorSab = false;
                oxygenSab = false;
                commsSab = false;
                elecSab = false;

                ToggleReactor(false);
                ToggleO2(false);
                ToggleComms(false);
                ToggleLights(false);

                if (ShipStatus.Instance.AllDoors != null)
                {
                    foreach (var door in ShipStatus.Instance.AllDoors)
                    {
                        if (door != null)
                        {
                            door.SetDoorway(true);
                            try { ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Doors, (byte)(door.Id | 64)); } catch (global::System.Exception __elysiumCaught315) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught315); }
                        }
                    }
                }
                try { ShipStatus.Instance.RpcUpdateSystem(SystemTypes.MushroomMixupSabotage, 0); } catch (global::System.Exception __elysiumCaught316) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught316); }
                if (notify) ShowNotification("<color=#00FF00>[SABOTAGE]</color> All sabotages and doors fixed!");
            }
            catch (global::System.Exception __elysiumCaught317) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught317); }
        }

private void TickAutoRepairSabotage()
        {
            if (!autoRepairSabotage) return;
            if (Time.unscaledTime < nextAutoRepairSabotageAt) return;
            nextAutoRepairSabotageAt = Time.unscaledTime + 1f;
            if (ShipStatus.Instance == null || AmongUsClient.Instance == null || !AmongUsClient.Instance.IsGameStarted) return;
            if (MeetingHud.Instance != null || ExileController.Instance != null || IntroCutscene.Instance != null) return;
            FixAllSabotages(false);
        }

private void TickAutoBreakSabotage()
        {
            if (!autoBreakSabotage) return;
            if (Time.unscaledTime < nextAutoBreakSabotageAt) return;
            nextAutoBreakSabotageAt = Time.unscaledTime + 3f;
            if (ShipStatus.Instance == null || AmongUsClient.Instance == null || !AmongUsClient.Instance.IsGameStarted) return;
            if (MeetingHud.Instance != null || ExileController.Instance != null || IntroCutscene.Instance != null) return;
            TriggerAllSabotages(false);
        }

private void SabotageDoors()
        {
            if (ShipStatus.Instance == null || ShipStatus.Instance.AllDoors == null) return;
            try
            {
                var rooms = new System.Collections.Generic.HashSet<SystemTypes>();
                foreach (var door in ShipStatus.Instance.AllDoors)
                {
                    if (door != null)
                    {
                        rooms.Add(door.Room);
                        try { ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Doors, (byte)door.Id); } catch (global::System.Exception __elysiumCaught318) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught318); }
                    }
                }
                foreach (var room in rooms)
                {
                    try { ShipStatus.Instance.RpcCloseDoorsOfType(room); } catch (global::System.Exception __elysiumCaught319) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught319); }
                }
                ShowNotification("<color=#FF0000>[DOORS]</color> Close signal sent!");
            }
            catch (global::System.Exception __elysiumCaught320) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught320); }
        }

private void CloseDoorsOfType(SystemTypes room)
        {
            if (ShipStatus.Instance == null || ShipStatus.Instance.AllDoors == null) return;
            try
            {
                ShipStatus.Instance.RpcCloseDoorsOfType(room);
                foreach (var door in ShipStatus.Instance.AllDoors)
                {
                    if (door != null && door.Room == room)
                        ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Doors, (byte)door.Id);
                }
                ShowNotification($"<color=#FF6A42>[DOORS]</color> {room}: close sent");
            }
            catch (global::System.Exception __elysiumCaught321) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught321); }
        }

private void LockDoorsOfType(SystemTypes room)
        {
            if (ShipStatus.Instance == null || ShipStatus.Instance.AllDoors == null) return;
            try
            {
                foreach (var door in ShipStatus.Instance.AllDoors)
                {
                    if (door != null && door.Room == room)
                    {
                        door.SetDoorway(false);
                        ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Doors, (byte)door.Id);
                    }
                }
                ShipStatus.Instance.RpcCloseDoorsOfType(room);
                ShowNotification($"<color=#FFB840>[DOORS]</color> {room}: locked");
            }
            catch (global::System.Exception __elysiumCaught322) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught322); }
        }

private void OpenDoorsOfType(SystemTypes room)
        {
            if (ShipStatus.Instance == null || ShipStatus.Instance.AllDoors == null) return;
            try
            {
                foreach (var door in ShipStatus.Instance.AllDoors)
                {
                    if (door != null && door.Room == room)
                    {
                        door.SetDoorway(true);
                        ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Doors, (byte)(door.Id | 64));
                    }
                }
                ShowNotification($"<color=#59DB92>[DOORS]</color> {room}: opened");
            }
            catch (global::System.Exception __elysiumCaught323) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught323); }
        }

private void LockAllDoors()
        {
            if (ShipStatus.Instance == null || ShipStatus.Instance.AllDoors == null) return;
            try
            {
                var rooms = new System.Collections.Generic.HashSet<SystemTypes>();
                foreach (var door in ShipStatus.Instance.AllDoors)
                {
                    if (door != null)
                    {
                        door.SetDoorway(false);
                        rooms.Add(door.Room);
                        ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Doors, (byte)door.Id);
                    }
                }
                foreach (var room in rooms)
                    ShipStatus.Instance.RpcCloseDoorsOfType(room);

                ShowNotification("<color=#FFB840>[DOORS]</color> All doors locked!");
            }
            catch (global::System.Exception __elysiumCaught324) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught324); }
        }

private void OpenAllDoors()
        {
            if (ShipStatus.Instance == null || ShipStatus.Instance.AllDoors == null) return;
            try
            {
                foreach (var door in ShipStatus.Instance.AllDoors)
                {
                    if (door != null)
                    {
                        door.SetDoorway(true);
                        try { ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Doors, (byte)(door.Id | 64)); } catch (global::System.Exception __elysiumCaught325) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught325); }
                    }
                }
                ShowNotification("<color=#00FF00>[DOORS]</color> All doors opened!");
            }
            catch (global::System.Exception __elysiumCaught326) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught326); }
        }

private void ToggleReactor(bool state) { if (ShipStatus.Instance == null) return; byte flag = (byte)(state ? 128 : 16); try { ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Reactor, flag); ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Laboratory, flag); if (state) ShipStatus.Instance.RpcUpdateSystem(SystemTypes.HeliSabotage, (byte)128); else { ShipStatus.Instance.RpcUpdateSystem(SystemTypes.HeliSabotage, (byte)16); ShipStatus.Instance.RpcUpdateSystem(SystemTypes.HeliSabotage, (byte)17); } } catch (global::System.Exception __elysiumCaught327) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught327); } }

private void ToggleO2(bool state) { if (ShipStatus.Instance == null) return; try { ShipStatus.Instance.RpcUpdateSystem(SystemTypes.LifeSupp, (byte)(state ? 128 : 16)); } catch (global::System.Exception __elysiumCaught328) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught328); } }

private void ToggleComms(bool state) { if (ShipStatus.Instance == null) return; try { if (state) ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Comms, (byte)128); else { ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Comms, (byte)16); ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Comms, (byte)17); } } catch (global::System.Exception __elysiumCaught329) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught329); } }

private void ToggleLights(bool state)
        {
            if (ShipStatus.Instance == null) return;
            try
            {
                if (state && unfixableLights)
                {
                    unfixableLights = false;
                    ToggleUnfixableLights(false);
                }
                if (state)
                {
                    byte b = 4;
                    for (int i = 0; i < 5; i++) if (UnityEngine.Random.value > 0.5f) b |= (byte)(1 << i);
                    ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Electrical, (byte)(b | 128));
                }
                else
                {
                    var sys = ShipStatus.Instance.Systems[SystemTypes.Electrical].Cast<SwitchSystem>();
                    if (sys != null)
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            bool expected = (sys.ExpectedSwitches & (1 << i)) != 0;
                            bool actual = (sys.ActualSwitches & (1 << i)) != 0;
                            if (expected != actual) ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Electrical, (byte)i);
                        }
                    }
                }
            }
            catch (global::System.Exception __elysiumCaught330) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught330); }
        }

private void ToggleUnfixableLights(bool state)
        {
            if (ShipStatus.Instance == null) return;
            try
            {
                if (!ShipStatus.Instance.Systems.ContainsKey(SystemTypes.Electrical))
                {
                    unfixableLights = false;
                    unfixableLightsApplied = false;
                    ShowNotification("<color=#FF4444>[LIGHTS]</color> Electrical system not present.");
                    return;
                }

                if (state)
                    elecSab = false;

                ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Electrical, 69);
                unfixableLightsApplied = state;
                ShowNotification(state ? "<color=#C080FF>[LIGHTS]</color> Unfixable lights ON" : "<color=#59DB92>[LIGHTS]</color> Unfixable lights OFF");
            }
            catch (global::System.Exception __elysiumCaught331) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught331); }
        }

private void UpdateUnfixableLightsState()
        {
            if (unfixableLights == unfixableLightsApplied) return;
            ToggleUnfixableLights(unfixableLights);
        }

private void ApplyVentCheatsTick()
        {
            try
            {
                PlayerControl local = PlayerControl.LocalPlayer;
                if (local == null || local.Data == null)
                    return;

                if (IsHudModalActive())
                {
                    if (HudManager.Instance != null && HudManager.Instance.ImpostorVentButton != null)
                        HudManager.Instance.ImpostorVentButton.gameObject.SetActive(false);
                    return;
                }

                // FreePlay can skip the HUD refresh after a role change.
                // Recheck the vent button for real vent roles and Unlock Vents.
                if (HudManager.Instance != null && HudManager.Instance.ImpostorVentButton != null)
                {
                    bool roleCanVent = local.Data.Role != null && local.Data.Role.CanVent;
                    bool shouldShowVent = !local.Data.IsDead && ShipStatus.Instance != null && (roleCanVent || unlockVents);
                    if (HudManager.Instance.ImpostorVentButton.gameObject.activeSelf != shouldShowVent)
                        HudManager.Instance.ImpostorVentButton.gameObject.SetActive(shouldShowVent);
                }

                if (walkInVents && local.inVent)
                {
                    local.inVent = false;
                    local.moveable = true;
                }
            }
            catch (global::System.Exception __elysiumCaught332) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught332); }
        }

private static void SetImmortalityVentState(bool enter)
        {
            try
            {
                PlayerControl local = PlayerControl.LocalPlayer;
                if (local == null || local.Data == null || ShipStatus.Instance == null) return;
                if (local.inVent) return;

                VentilationSystem.Update(enter ? VentilationSystem.Operation.Enter : VentilationSystem.Operation.Exit, ImmortalityCustomVentId);
                immortalityVentStateApplied = enter;
            }
            catch (global::System.Exception __elysiumCaught333) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught333); }
        }

private static void TickRoleBuffImmortality()
        {
            try
            {
                PlayerControl local = PlayerControl.LocalPlayer;
                if (local == null || local.Data == null || ShipStatus.Instance == null)
                {
                    immortalityVentStateApplied = false;
                    return;
                }

                if (!roleBuffImmortality || local.Data.IsDead)
                {
                    if (immortalityVentStateApplied)
                        SetImmortalityVentState(false);
                    return;
                }

                if (MeetingHud.Instance != null)
                    return;

                if (!immortalityVentStateApplied)
                    SetImmortalityVentState(true);
            }
            catch (global::System.Exception __elysiumCaught334) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught334); }
        }

private static void DisableRoleBuffImmortality()
        {
            try
            {
                if (immortalityVentStateApplied)
                    SetImmortalityVentState(false);
            }
            catch (global::System.Exception __elysiumCaught335) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught335); }
        }

private void SabotageMushroom() { if (ShipStatus.Instance == null) return; try { ShipStatus.Instance.RpcUpdateSystem(SystemTypes.MushroomMixupSabotage, (byte)1); } catch (global::System.Exception __elysiumCaught336) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught336); } }
    }
}
