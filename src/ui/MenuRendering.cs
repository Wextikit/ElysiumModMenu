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
private void DrawSabotagesTab()
        {
            GUIStyle miniLabelStyle = new GUIStyle(toggleLabelStyle) { fontSize = 11, richText = true, wordWrap = true };
            miniLabelStyle.normal.textColor = whiteMenuTheme ? new Color(0.25f, 0.25f, 0.25f, 1f) : new Color(0.72f, 0.72f, 0.72f, 1f);

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(menuCardStyle, GUILayout.Width(276), GUILayout.ExpandHeight(false));
            DrawMenuSectionHeader("CRITICAL SABOTAGES");
            GUILayout.Space(4);

            GUILayout.BeginHorizontal();
            if (DrawColoredActionButton("FIX ALL", new Color32(83, 231, 139, 255), 116f, 32f)) FixAllSabotages();
            GUILayout.Space(6);
            if (DrawColoredActionButton("TRIGGER ALL", new Color32(255, 74, 74, 255), 116f, 32f)) TriggerAllSabotages();
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            if (GUILayout.Button("CALL MEETING", btnStyle, GUILayout.Height(30))) callMeetingPublic();

            GUILayout.Space(8);
            GUILayout.BeginHorizontal();
            DrawSabotageButton("Reactor", ref reactorSab, ToggleReactor, new Color32(255, 84, 84, 255));
            GUILayout.Space(6);
            DrawSabotageButton("Oxygen", ref oxygenSab, ToggleO2, new Color32(255, 132, 54, 255));
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            DrawSabotageButton("Comms", ref commsSab, ToggleComms, new Color32(66, 205, 128, 255));
            GUILayout.Space(6);
            DrawSabotageButton("Lights", ref elecSab, ToggleLights, new Color32(255, 218, 77, 255));
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            DrawSabotageButton("Unfixable Lights", ref unfixableLights, ToggleUnfixableLights, new Color32(210, 128, 255, 255));

            GUILayout.Space(8);
            if (GUILayout.Button("MUSHROOM MIXUP", btnStyle, GUILayout.Height(28))) SabotageMushroom();

            GUILayout.Space(10);
            DrawMenuSectionHeader("VENTS");
            unlockVents = DrawToggle(unlockVents, "Unlock Vents", 230);
            GUILayout.Space(4);
            walkInVents = DrawToggle(walkInVents, "Walk In Vents", 230);
            GUILayout.EndVertical();

            GUILayout.Space(10);

            GUILayout.BeginVertical(menuCardStyle, GUILayout.ExpandWidth(true));
            DrawMenuSectionHeader("DOOR LOCKDOWN");
            GUILayout.Space(4);
            GUILayout.Label("<b>Global controls</b>", miniLabelStyle);

            GUILayout.BeginHorizontal();
            if (DrawColoredActionButton("CLOSE", new Color32(255, 106, 66, 255), 82f, 30f)) SabotageDoors();
            GUILayout.Space(6);
            if (DrawColoredActionButton("LOCK", new Color32(255, 184, 64, 255), 82f, 30f)) LockAllDoors();
            GUILayout.Space(6);
            if (DrawColoredActionButton("OPEN", new Color32(89, 219, 146, 255), 82f, 30f)) OpenAllDoors();
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            GUILayout.Label("<b>Target doors</b>", miniLabelStyle);

            if (ShipStatus.Instance != null && ShipStatus.Instance.AllDoors != null)
            {
                var rooms = ShipStatus.Instance.AllDoors
                    .Where(d => d != null)
                    .Select(d => d.Room)
                    .Distinct()
                    .OrderBy(r => r.ToString())
                    .ToList();

                doorsScrollPos = GUILayout.BeginScrollView(doorsScrollPos, false, true, GUILayout.Height(214));
                foreach (var room in rooms)
                {
                    DrawDoorTargetRow(room);
                    GUILayout.Space(3);
                }
                GUILayout.EndScrollView();
            }
            else
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("<color=#777777>Вы не в игре или на карте нет дверей.</color>", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, richText = true });
                GUILayout.FlexibleSpace();
            }
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

private void DrawSabotageButton(string label, ref bool state, Action<bool> toggleAction, Color accent)
        {
            GUIStyle style = state ? activeTabStyle : btnStyle;
            Color oldBackground = GUI.backgroundColor;
            GUI.backgroundColor = state ? accent : Color.white;

            if (GUILayout.Button(state ? label + "  ON" : label, style, GUILayout.Height(30)))
            {
                state = !state;
                toggleAction(state);
            }

            GUI.backgroundColor = oldBackground;
        }

private void DrawDoorTargetRow(SystemTypes room)
        {
            GUILayout.BeginHorizontal(boxStyle);
            GUILayout.Label($"<b>{room}</b>", toggleLabelStyle, GUILayout.Width(96));
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Close", btnStyle, GUILayout.Width(52), GUILayout.Height(24))) CloseDoorsOfType(room);
            GUILayout.Space(4);
            if (GUILayout.Button("Lock", activeSubTabStyle, GUILayout.Width(52), GUILayout.Height(24))) LockDoorsOfType(room);
            GUILayout.Space(4);
            if (GUILayout.Button("Open", btnStyle, GUILayout.Width(52), GUILayout.Height(24))) OpenDoorsOfType(room);

            GUILayout.EndHorizontal();
        }

private void callMeetingPublic()
        {
            if (PlayerControl.LocalPlayer == null || PlayerControl.AllPlayerControls == null) return;
            try
            {
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    if (pc != null && pc.Data != null && pc.Data.IsDead && !pc.Data.Disconnected)
                    {
                        PlayerControl.LocalPlayer.CmdReportDeadBody(pc.Data);
                        ShowNotification($"<color=#00FF00>[MEETING]</color> Найден и зарепорчен труп: <b>{pc.Data.PlayerName}</b>!");
                        return;
                    }
                }

                PlayerControl.LocalPlayer.CmdReportDeadBody(null);
                ShowNotification("<color=#00FF00>[MEETING]</color> Легально нажата кнопка собрания!");
            }
            catch { }
        }

private void TriggerAllSabotages()
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

                ShowNotification("<color=#FF0000>[SABOTAGE]</color> Все системы саботированы!");
            }
            catch { }
        }

private void FixAllSabotages()
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
                            try { ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Doors, (byte)(door.Id | 64)); } catch { }
                        }
                    }
                }
                try { ShipStatus.Instance.RpcUpdateSystem(SystemTypes.MushroomMixupSabotage, 0); } catch { }
                ShowNotification("<color=#00FF00>[SABOTAGE]</color> Все саботажи и двери починены!");
            }
            catch { }
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
                        try { ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Doors, (byte)door.Id); } catch { }
                    }
                }
                foreach (var room in rooms)
                {
                    try { ShipStatus.Instance.RpcCloseDoorsOfType(room); } catch { }
                }
                ShowNotification("<color=#FF0000>[DOORS]</color> Сигнал на закрытие отправлен!");
            }
            catch { }
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
            catch { }
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
            catch { }
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
            catch { }
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

                ShowNotification("<color=#FFB840>[DOORS]</color> Все двери залочены!");
            }
            catch { }
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
                        try { ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Doors, (byte)(door.Id | 64)); } catch { }
                    }
                }
                ShowNotification("<color=#00FF00>[DOORS]</color> Все двери открыты!");
            }
            catch { }
        }

private void ToggleReactor(bool state) { if (ShipStatus.Instance == null) return; byte flag = (byte)(state ? 128 : 16); try { ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Reactor, flag); ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Laboratory, flag); if (state) ShipStatus.Instance.RpcUpdateSystem(SystemTypes.HeliSabotage, (byte)128); else { ShipStatus.Instance.RpcUpdateSystem(SystemTypes.HeliSabotage, (byte)16); ShipStatus.Instance.RpcUpdateSystem(SystemTypes.HeliSabotage, (byte)17); } } catch { } }

private void ToggleO2(bool state) { if (ShipStatus.Instance == null) return; try { ShipStatus.Instance.RpcUpdateSystem(SystemTypes.LifeSupp, (byte)(state ? 128 : 16)); } catch { } }

private void ToggleComms(bool state) { if (ShipStatus.Instance == null) return; try { if (state) ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Comms, (byte)128); else { ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Comms, (byte)16); ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Comms, (byte)17); } } catch { } }

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
            catch { }
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
            catch { }
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

                if (unlockVents && !local.Data.IsDead && local.Data.Role != null && !local.Data.Role.CanVent && HudManager.Instance != null && HudManager.Instance.ImpostorVentButton != null)
                    HudManager.Instance.ImpostorVentButton.gameObject.SetActive(true);

                if (walkInVents && local.inVent)
                {
                    local.inVent = false;
                    local.moveable = true;
                }
            }
            catch { }
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
            catch { }
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
            catch { }
        }

private static void DisableRoleBuffImmortality()
        {
            try
            {
                if (immortalityVentStateApplied)
                    SetImmortalityVentState(false);
            }
            catch { }
        }

private void SabotageMushroom() { if (ShipStatus.Instance == null) return; try { ShipStatus.Instance.RpcUpdateSystem(SystemTypes.MushroomMixupSabotage, (byte)1); } catch { } }

private void DrawPlayersRoles()
        {
            GUILayout.BeginVertical(menuCardStyle);
            DrawMenuSectionHeader("PRE-GAME ROLE MANAGER");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(enablePreGameRoleForce ? "Role Forcing: ON" : "Role Forcing: OFF", enablePreGameRoleForce ? activeTabStyle : btnStyle, GUILayout.Height(25))) enablePreGameRoleForce = !enablePreGameRoleForce;
            if (GUILayout.Button("Random 2 Imps", btnStyle, GUILayout.Width(110), GUILayout.Height(25)))
            {
                forcedPreGameRoles.Clear(); forcedImpostors.Clear();
                var activePlayers = PlayerControl.AllPlayerControls.ToArray().Where(p => p != null && !p.Data.Disconnected).ToList();
                if (activePlayers.Count >= 2)
                {
                    for (int i = activePlayers.Count - 1; i > 0; i--) { int swapIndex = UnityEngine.Random.Range(0, i + 1); var temp = activePlayers[i]; activePlayers[i] = activePlayers[swapIndex]; activePlayers[swapIndex] = temp; }
                    forcedImpostors.Add(activePlayers[0].PlayerId); forcedImpostors.Add(activePlayers[1].PlayerId);
                    enablePreGameRoleForce = true;
                }
            }
            if (GUILayout.Button("Clear All Roles", btnStyle, GUILayout.Width(110), GUILayout.Height(25))) { forcedPreGameRoles.Clear(); forcedImpostors.Clear(); }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.Space(8);
            GUILayout.BeginVertical(menuCardStyle);
            DrawMenuSectionHeader("LIVE ROLE DISTRIBUTOR (HOST)");
            GUILayout.BeginHorizontal();

            GUIStyle allRoleMidStyle = new GUIStyle(btnStyle)
            {
                fontStyle = FontStyle.Bold,
                normal = { background = null, textColor = GetMenuAccentColor() },
                alignment = TextAnchor.MiddleCenter
            };

            if (GUILayout.Button("<", btnStyle, GUILayout.Width(28), GUILayout.Height(25)))
            {
                allPlayersRoleAssignIdx--;
                if (allPlayersRoleAssignIdx < 0) allPlayersRoleAssignIdx = roleAssignOptions.Length - 1;
            }

            GUILayout.Label(roleAssignNames[allPlayersRoleAssignIdx], allRoleMidStyle, GUILayout.Height(25), GUILayout.ExpandWidth(true));

            if (GUILayout.Button(">", btnStyle, GUILayout.Width(28), GUILayout.Height(25)))
            {
                allPlayersRoleAssignIdx++;
                if (allPlayersRoleAssignIdx >= roleAssignOptions.Length) allPlayersRoleAssignIdx = 0;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
            if (GUILayout.Button("SET ALL PLAYERS ROLE", activeTabStyle, GUILayout.Height(28)))
            {
                if (IsGhostRoleSelection(allPlayersRoleAssignIdx))
                    SetAllPlayersGhost();
                else if (IsGhostImpostorRoleSelection(allPlayersRoleAssignIdx))
                    SetAllPlayersGhost(true);
                else
                    SetAllPlayersRole(roleAssignOptions[allPlayersRoleAssignIdx]);
            }
            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("ALL -> GHOST", btnStyle, GUILayout.Height(26)))
                SetAllPlayersGhost();
            if (GUILayout.Button("REVIVE ALL", activeTabStyle, GUILayout.Height(26)))
                ReviveAllPlayers();
            GUILayout.EndHorizontal();
            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("ALL -> GHOST IMP", btnStyle, GUILayout.Height(26)))
                SetAllPlayersGhost(true);
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(menuCardStyle, GUILayout.Width(150), GUILayout.Height(315));
            preRolesListScrollPos = GUILayout.BeginScrollView(preRolesListScrollPos, GUILayout.ExpandHeight(true));
            foreach (var pc in lockedPlayersList)
            {
                if (pc == null || pc.Data == null || pc.PlayerId >= 100) continue;
                string pName = pc.Data.PlayerName ?? "Unknown";
                if (forcedPreGameRoles.ContainsKey(pc.PlayerId)) { string rShort = forcedPreGameRoles[pc.PlayerId].ToString().Replace("9", "Pha").Replace("10", "Tra").Replace("8", "Noi").Replace("12", "Det").Replace("18", "Vip"); if (rShort.Length > 3) rShort = rShort.Substring(0, 3); pName += $" [{rShort}]"; }
                else if (forcedImpostors.Contains(pc.PlayerId)) pName += " [Imp]";
                bool isSelected = selectedPreRoleId == pc.PlayerId;
                try { GUI.contentColor = Palette.PlayerColors[pc.Data.DefaultOutfit.ColorId]; } catch { }
                if (GUILayout.Button(pName, isSelected ? activeTabStyle : btnStyle, GUILayout.Height(30))) selectedPreRoleId = pc.PlayerId;
                GUI.contentColor = Color.white;
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.Space(8);
            GUILayout.BeginVertical(menuCardStyle, GUILayout.ExpandWidth(true), GUILayout.Height(315));
            preRolesActionScrollPos = GUILayout.BeginScrollView(preRolesActionScrollPos, GUILayout.ExpandHeight(true));
            PlayerControl target = lockedPlayersList.FirstOrDefault(p => p.PlayerId == selectedPreRoleId);
            if (target != null && target.Data != null)
            {
                GUIStyle infoStyle = new GUIStyle(GUI.skin.label) { richText = true, fontSize = 14 };
                GUILayout.Label($"<color=#aaaaaa>Selecting role for:</color> {target.Data.PlayerName}", infoStyle);
                RoleTypes currentForced = forcedPreGameRoles.ContainsKey(target.PlayerId) ? forcedPreGameRoles[target.PlayerId] : RoleTypes.Crewmate;
                bool isForced = forcedPreGameRoles.ContainsKey(target.PlayerId) || forcedImpostors.Contains(target.PlayerId);
                string roleNameStr = currentForced.ToString().Replace("9", "Phantom").Replace("10", "Tracker").Replace("8", "Noisemaker").Replace("12", "Detective").Replace("18", "Viper");
                if (forcedImpostors.Contains(target.PlayerId)) roleNameStr = "Impostor";
                GUILayout.Label($"<color=#aaaaaa>Status:</color> {(isForced ? $"<color=#00FF00>Forced ({roleNameStr})</color>" : "<color=#FF0000>Not Forced (Random)</color>")}", infoStyle);
                GUILayout.Space(15);
                DrawMenuSectionHeader("IMPOSTOR ROLES (Red Team)");
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Impostor", btnStyle, GUILayout.Height(24))) { forcedPreGameRoles.Remove(target.PlayerId); forcedImpostors.Add(target.PlayerId); }
                if (GUILayout.Button("Shapeshifter", btnStyle, GUILayout.Height(24))) { forcedImpostors.Remove(target.PlayerId); forcedPreGameRoles[target.PlayerId] = RoleTypes.Shapeshifter; }
                if (GUILayout.Button("Phantom", btnStyle, GUILayout.Height(24))) { forcedImpostors.Remove(target.PlayerId); forcedPreGameRoles[target.PlayerId] = (RoleTypes)9; }
                if (GUILayout.Button("Viper", btnStyle, GUILayout.Height(24))) { forcedImpostors.Remove(target.PlayerId); forcedPreGameRoles[target.PlayerId] = (RoleTypes)18; }
                GUILayout.EndHorizontal();
                GUILayout.Space(10);
                DrawMenuSectionHeader("CREWMATE ROLES (Blue Team)");
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Crewmate", btnStyle, GUILayout.Height(24))) { forcedImpostors.Remove(target.PlayerId); forcedPreGameRoles[target.PlayerId] = RoleTypes.Crewmate; }
                if (GUILayout.Button("Engineer", btnStyle, GUILayout.Height(24))) { forcedImpostors.Remove(target.PlayerId); forcedPreGameRoles[target.PlayerId] = RoleTypes.Engineer; }
                if (GUILayout.Button("Scientist", btnStyle, GUILayout.Height(24))) { forcedImpostors.Remove(target.PlayerId); forcedPreGameRoles[target.PlayerId] = RoleTypes.Scientist; }
                if (GUILayout.Button("Tracker", btnStyle, GUILayout.Height(24))) { forcedImpostors.Remove(target.PlayerId); forcedPreGameRoles[target.PlayerId] = (RoleTypes)10; }
                GUILayout.EndHorizontal();
                GUILayout.Space(5);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Noisemaker", btnStyle, GUILayout.Height(24))) { forcedImpostors.Remove(target.PlayerId); forcedPreGameRoles[target.PlayerId] = (RoleTypes)8; }
                if (GUILayout.Button("Guardian Angel", btnStyle, GUILayout.Height(24))) { forcedImpostors.Remove(target.PlayerId); forcedPreGameRoles[target.PlayerId] = RoleTypes.GuardianAngel; }
                if (GUILayout.Button("Detective", btnStyle, GUILayout.Height(24))) { forcedImpostors.Remove(target.PlayerId); forcedPreGameRoles[target.PlayerId] = (RoleTypes)12; }
                GUILayout.EndHorizontal();
                GUILayout.Space(15);
                if (GUILayout.Button("REMOVE FORCED ROLE", activeTabStyle, GUILayout.Height(35))) { forcedPreGameRoles.Remove(target.PlayerId); forcedImpostors.Remove(target.PlayerId); }
                GUILayout.Space(20);
                GUILayout.Label("<color=#777777><b>Hide & Seek Notice:</b>\nВыбор Impostor/Shapeshifter/Phantom/Viper расширит лимит маньяков (Seekers) в Прятках!</color>", new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true });
            }
            else
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("<color=#777777>Select a player to set their role</color>", new GUIStyle(GUI.skin.label) { richText = true, alignment = TextAnchor.MiddleCenter });
                GUILayout.FlexibleSpace();
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

private void DrawMenuSectionHeader(string title)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(GUIContent.none, menuAccentBarStyle, GUILayout.Width(3), GUILayout.Height(16));
            GUILayout.Space(8);
            GUILayout.Label(title, menuSectionTitleStyle, GUILayout.Height(16));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(8);
        }

private void DrawMenuTab()
        {
            bool menuPrefsChanged = false;

            GUILayout.BeginVertical(menuCardStyle);
            DrawMenuSectionHeader(L("MENU CUSTOMIZATION", "ОФОРМЛЕНИЕ МЕНЮ"));

            bool prevRgb = rgbMenuMode;
            rgbMenuMode = DrawToggle(rgbMenuMode, "RGB Menu Mode", 260);
            if (prevRgb && !rgbMenuMode) UpdateAccentColor(menuColors[currentMenuColorIndex]);
            if (prevRgb != rgbMenuMode) menuPrefsChanged = true;
            GUILayout.Label(L("Smoothly cycles the accent through the rainbow.", "Плавно переливает акцент по радуге."), menuDescStyle);
            GUILayout.Space(8);

            bool prevRgbText = rgbMenuText;
            rgbMenuText = DrawToggle(rgbMenuText, "RGB Text", 260);
            if (prevRgbText != rgbMenuText)
            {
                InitStyles();
                UpdateAccentColor(currentAccentColor);
                menuPrefsChanged = true;
            }
            GUILayout.Label("When off, RGB Menu Mode does not recolor menu text.", menuDescStyle);
            GUILayout.Space(8);

            bool prevBoldMenuText = boldMenuText;
            boldMenuText = DrawToggle(boldMenuText, "Bold Menu Text", 260);
            if (prevBoldMenuText != boldMenuText)
            {
                InitStyles();
                UpdateAccentColor(currentAccentColor);
                menuPrefsChanged = true;
            }
            GUILayout.Label("Switches menu text between bold and normal. Default: bold.", menuDescStyle);
            GUILayout.Space(8);

            bool prevWhiteTheme = whiteMenuTheme;
            whiteMenuTheme = DrawToggle(whiteMenuTheme, "White Theme", 260);
            if (prevWhiteTheme != whiteMenuTheme)
            {
                InitStyles();
                UpdateAccentColor(currentAccentColor);
                menuPrefsChanged = true;
            }
            GUILayout.Label(L("Switches between the dark and light interface.", "Переключает тёмный и светлый интерфейс."), menuDescStyle);
            GUILayout.Space(8);

            bool prevBg = enableBackground;
            enableBackground = DrawToggle(enableBackground, "Enable Image Background", 260);
            if (enableBackground && !prevBg) LoadBackgroundImage();
            if (prevBg != enableBackground) menuPrefsChanged = true;
            GUILayout.Label(L("Put 'MenuBG.png' or .jpg in BepInEx/config to add a background image.", "Положите 'MenuBG.png' или .jpg в BepInEx/config для фона."), menuDescStyle);
            GUILayout.Space(8);

            bool prevHardMenu = hardMenu;
            hardMenu = DrawToggle(hardMenu, L("Solid Menu (block game clicks)", "Твердое меню (блок кликов по игре)"), 260);
            if (prevHardMenu != hardMenu) menuPrefsChanged = true;
            GUILayout.Label(L("When on, clicks over the menu stay in the menu so you can't misclick the game behind it.", "Когда включено, клики по меню остаются в меню — вы не промахнёте��ь по игре за ним."), menuDescStyle);
            GUILayout.Space(8);

            bool prevAutoCopyCode = autoCopyCodeAndLeave;
            autoCopyCodeAndLeave = DrawToggle(autoCopyCodeAndLeave, "Copy Code On Disconnect", 260);
            if (prevAutoCopyCode != autoCopyCodeAndLeave) menuPrefsChanged = true;
            GUILayout.Label("Copies the room code when you leave, get kicked, banned, or disconnected.", menuDescStyle);
            GUILayout.EndVertical();

            GUILayout.BeginVertical(menuCardStyle);
            DrawMenuSectionHeader(L("ACCENT & PERFORMANCE", "АКЦЕНТ И ПРОИЗВОДИТЕЛЬНОСТЬ"));

            GUILayout.BeginHorizontal();
            GUILayout.Label(L("FPS Limit", "Лимит FPS"), new GUIStyle(toggleLabelStyle), GUILayout.Height(25), GUILayout.Width(110));
            int newFpsLimit = Mathf.Clamp((int)GUILayout.HorizontalSlider(fpsLimit, 60f, 240f, sliderStyle, sliderThumbStyle, GUILayout.Width(180)), 60, 240);
            GUILayout.Space(10);
            GUILayout.Label(fpsLimit.ToString(), menuBadgeStyle, GUILayout.Width(52), GUILayout.Height(22));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            if (newFpsLimit != fpsLimit)
            {
                fpsLimit = newFpsLimit;
                ApplyFpsLimit();
                menuPrefsChanged = true;
            }

            GUILayout.Space(12);

            GUILayout.BeginHorizontal();
            GUILayout.Label(L("Accent Color", "Цвет акцента"), new GUIStyle(toggleLabelStyle), GUILayout.Height(25), GUILayout.Width(110));
            Color prevGuiColor = GUI.color;
            GUI.color = GetMenuControlAccentColor();
            GUILayout.Label(GUIContent.none, menuSwatchStyle, GUILayout.Width(22), GUILayout.Height(22));
            GUI.color = prevGuiColor;
            GUILayout.Space(8);
            GUI.enabled = !rgbMenuMode;
            GUIStyle middleColorStyle = new GUIStyle(btnStyle) { normal = { background = null, textColor = GetMenuAccentColor() }, fontStyle = FontStyle.Bold };
            if (GUILayout.Button("<", btnStyle, GUILayout.Width(30), GUILayout.Height(25))) { currentMenuColorIndex--; if (currentMenuColorIndex < 0) currentMenuColorIndex = menuColors.Length - 1; if (!rgbMenuMode) UpdateAccentColor(menuColors[currentMenuColorIndex]); menuPrefsChanged = true; }
            GUILayout.Label(rgbMenuMode ? "RGB" : menuColorNames[currentMenuColorIndex], middleColorStyle, GUILayout.Width(120), GUILayout.Height(25));
            if (GUILayout.Button(">", btnStyle, GUILayout.Width(30), GUILayout.Height(25))) { currentMenuColorIndex++; if (currentMenuColorIndex >= menuColors.Length) currentMenuColorIndex = 0; if (!rgbMenuMode) UpdateAccentColor(menuColors[currentMenuColorIndex]); menuPrefsChanged = true; }
            GUI.enabled = true;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.BeginVertical(menuCardStyle);
            DrawMenuSectionHeader(L("SPOOF MENU IDENTITY", "ПОДМЕНА МЕНЮ"));
            bool prevSpoofMenuEnabled = SpoofMenuEnabled;
            SpoofMenuEnabled = DrawToggle(SpoofMenuEnabled, "Enable Fake RPC", 260);
            if (prevSpoofMenuEnabled != SpoofMenuEnabled) menuPrefsChanged = true;
            GUILayout.Label(L("Reports a fake mod menu name to other players.", "Показывает игрокам поддельное имя меню."), menuDescStyle);
            GUILayout.Space(8);
            GUILayout.BeginHorizontal();
            GUILayout.Label(L("Fake Name", "Поддельное имя"), new GUIStyle(toggleLabelStyle), GUILayout.Height(25), GUILayout.Width(110));
            GUI.enabled = SpoofMenuEnabled;
            GUIStyle middleLabelStyle = new GUIStyle(btnStyle) { fontStyle = FontStyle.Bold, normal = { background = null, textColor = GetMenuAccentColor() } };
            if (GUILayout.Button("<", btnStyle, GUILayout.Width(30), GUILayout.Height(25))) { selectedSpoofMenuIndex--; if (selectedSpoofMenuIndex < 0) selectedSpoofMenuIndex = spoofMenuNames.Length - 1; menuPrefsChanged = true; }
            GUILayout.Label(spoofMenuNames[selectedSpoofMenuIndex], middleLabelStyle, GUILayout.Width(150), GUILayout.Height(25));
            if (GUILayout.Button(">", btnStyle, GUILayout.Width(30), GUILayout.Height(25))) { selectedSpoofMenuIndex++; if (selectedSpoofMenuIndex >= spoofMenuNames.Length) selectedSpoofMenuIndex = 0; menuPrefsChanged = true; }
            GUI.enabled = true;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.BeginVertical(menuCardStyle);
            DrawMenuSectionHeader(L("NOTIFICATIONS & LOGGING", "УВЕДОМЛЕНИЯ И ЛОГИ"));
            bool prevCustomNotifs = EnableCustomNotifs;
            EnableCustomNotifs = DrawToggle(EnableCustomNotifs, "Enable Custom UI Notifications", 280);
            if (prevCustomNotifs != EnableCustomNotifs) menuPrefsChanged = true;
            GUILayout.Space(6);
            bool prevLogAllRpcs = LogAllRPCs;
            LogAllRPCs = DrawToggle(LogAllRPCs, "Sniff All RPCs (On-Screen)", 280);
            if (prevLogAllRpcs != LogAllRPCs) menuPrefsChanged = true;
            GUILayout.Space(6);
            bool previousLogThrottle = throttleDefaultLogs;
            throttleDefaultLogs = DrawToggle(throttleDefaultLogs, "Limit Console Logs (2/sec)", 280);
            if (previousLogThrottle != throttleDefaultLogs) menuPrefsChanged = true;
            GUILayout.EndVertical();

            GUILayout.BeginVertical(menuCardStyle);
            DrawMenuSectionHeader(L("RESET SETTINGS", "СБРОС НАСТРОЕК"));
            GUILayout.Label(L("Resets all sliders back to their default values.", "Сбрасывает все ползунки до значений по умолчанию."), menuDescStyle);
            GUILayout.Space(6);
            if (GUILayout.Button(L("Reset Sliders to Default", "Сбросить ползунки до дефолта"), activeTabStyle, GUILayout.Height(30)))
            {
                ResetSlidersToDefault();
                menuPrefsChanged = true;
            }
            GUILayout.EndVertical();

            if (menuPrefsChanged) SaveConfig();
        }

private void ResetSlidersToDefault()
        {
            selectedMapSpawnIdx = 0f;
            chatHistoryLimit = 20;
            customChatSpamDelay = 2.1f;
            autoChatEveryoneDelay = 2.5f;
            engineSpeed = 1f;
            walkSpeed = 1f;
            currentPlatformIndex = 1;
            autoKickTimer = 5f;
            autoKickMinLevel = 200;
            fpsLimit = 60;
            ApplyFpsLimit();
            AutoHostMinPlayers = 4;
            AutoHostStartDelaySeconds = 15f;
            AutoHostFastStartPlayers = 13;
            AutoHostFastStartDelaySeconds = 5f;
            punishmentMode = 0;

            showPlayerInfo = false;
            seeGhosts = false;
            seeRoles = false;
            revealMeetingRoles = false;
            showTracers = false;
            fullBright = false;
            seeProtections = false;
            seeKillCooldown = false;
            extendedLobby = false;
            moreLobbyInfo = true;
            alwaysShowLobbyTimer = false;
            noClip = false;
            tpToCursor = false;
            dragToCursor = false;
            autoFollowCursor = false;
            freecam = false;
            cameraZoom = false;
            alwaysChat = false;
            lobbyRainbowAll = false;
            lobbyAllColor = false;
            lobbyAllColorId = 0;
            readGhostChat = false;
            enableExtendedChat = true;
            enableFastChat = true;
            allowLinksAndSymbols = false;
            enableChatHistory = true;
            enableClipboard = true;
            enableChatMessageDoubleClickCopy = true;
            enableChatNameColorCopy = true;
            enableChatLog = true;
            enableColorCommand = false;
            blockRainbowChat = true;
            blockFortegreenChat = true;
            AnimAsteroidsEnabled = false;
            IsScanning = false;
            AnimShieldsEnabled = false;
            AnimCamsInUseEnabled = false;
            AnimEmptyGarbageEnabled = false;
            skipShhhAnim = false;
            localRainbow = false;
            localRainbowFreeOnly = false;
            RevealVotesEnabled = false;
            noTaskMode = false;
            noMapCooldowns = false;
            allowTasksAsImpostor = false;
            killWhileVanishedHostOnly = false;
            DisableRoleBuffImmortality();
            roleBuffImmortality = false;
            neverEndGame = false;
            removePenalty = true;
            autoGhostAfterStart = false;
            AutoHostEnabled = false;
            AutoReturnLobbyAfterMatch = true;
            AutoHostNotifications = true;
            AutoHostForceLastMinute = true;
            AutoHostWaitLoadedPlayers = true;
            AutoHostCancelBelowMin = true;
            AutoHostInstantStart = false;
            autoBanEnabled = true;
            allowDuplicateColors = false;
            blockSpoofRPC = true;
            autoBanPlatformSpoof = false;
            banCustomPlatformsFromTxt = false;
            autoKickLowLevelEnabled = false;
            autoKickBugs = false;
            disableVoteKicks = false;
            blockSabotageRPC = true;
            blockGameRpcInLobby = true;
            blockChatFloodRpc = true;
            blockMeetingFloodRpc = true;
            enablePasosLimit = true;
            enableLocalPasosBan = true;
            enableHostPasosBan = true;
            enableMalformedPacketGuard = true;
            banMalformedPacketSender = false;
            enableQuickChatEmptyGuard = true;
            banQuickChatEmptySpammer = true;
            enableUnownedSpawnGuard = true;
            enableLocalNameSpoof = false;
            enableLocalFriendCodeSpoof = false;
            SpoofMenuEnabled = false;
            enableBackground = false;
            hardMenu = false;
            rgbMenuText = false;
            boldMenuText = true;
            EnableCustomNotifs = true;
            LogAllRPCs = true;

            settingsDirty = true;
            InitStyles();
            UpdateAccentColor(currentAccentColor);

            ShowNotification(L("All sliders & toggles reset to default.", "Все ползунки и тумблеры сброшены до дефолта."));
        }

private Vector2 outfitsScrollPos = Vector2.zero;

public static bool AutoHostEnabled = false;

public static bool AutoReturnLobbyAfterMatch = true;

public static bool AutoHostNotifications = true;

public static bool AutoHostForceLastMinute = true;

public static bool AutoHostWaitLoadedPlayers = true;

public static bool AutoHostCancelBelowMin = true;

public static bool AutoHostInstantStart = false;

public static int AutoHostMinPlayers = 4;

public static int AutoHostForceMinPlayers = 2;

public static float AutoHostStartDelaySeconds = 15f;

public static float AutoHostBackoffSeconds = 8f;

public static float AutoHostWarmupSeconds = 5f;

public static float AutoHostLoadGraceSeconds = 20f;

public static int AutoHostForceAfterMinutes = 0;

public static int AutoHostFastStartPlayers = 13;

public static float AutoHostFastStartDelaySeconds = 5f;

private int currentAutoHostSubTab = 0;

private string[] autoHostSubTabs = { "LOBBY CONTROLS", "ROLE MANAGER", "ANTI CHEAT", "AUTO HOST" };
    }
}
