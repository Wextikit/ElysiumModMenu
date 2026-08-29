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
private static readonly string[] sabotageMenuTabs = { L("SABOTAGES", "САБОТАЖИ"), L("TASKS", "ТАСКИ"), L("ANIMATIONS", "АНИМАЦИИ"), L("MAPS", "КАРТЫ") };

private readonly List<SystemTypes> doorRooms = new List<SystemTypes>();
private Vector2 taskPlayersScrollPos;

private void UpdateDoorRooms()
        {
            doorRooms.Clear();
            if (ShipStatus.Instance == null || ShipStatus.Instance.AllDoors == null) return;

            foreach (var door in ShipStatus.Instance.AllDoors)
            {
                if (door != null && !doorRooms.Contains(door.Room))
                    doorRooms.Add(door.Room);
            }

            doorRooms.Sort((a, b) => string.Compare(a.ToString(), b.ToString(), StringComparison.CurrentCulture));
        }

private void DrawSabotageAnimationTab()
        {
            float tabWidth = GetMenuWorkWidth(180f, 760f);
            string[] tabs = sabotageMenuTabs;
            currentSabotageSubTab = Mathf.Clamp(currentSabotageSubTab, 0, tabs.Length - 1);

            GUILayout.BeginHorizontal(GUILayout.Width(tabWidth), GUILayout.Height(24));
            for (int i = 0; i < tabs.Length; i++)
            {
                if (GUILayout.Button(tabs[i], currentSabotageSubTab == i ? activeSubTabStyle : subTabStyle, GUILayout.Height(22)))
                    SetMultiTab("sabotage", ref currentSabotageSubTab, i, tabs.Length);
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(8);

            BeginMultiTabContent("sabotage", out Matrix4x4 oldMatrix, out Color oldColor);
            try
            {
                if (currentSabotageSubTab == 0) DrawSabotagesTab();
                else if (currentSabotageSubTab == 1) DrawTasksTab();
                else if (currentSabotageSubTab == 2) DrawAnimationsTab();
                else DrawMapsTab();
            }
            finally
            {
                EndMultiTabContent(oldMatrix, oldColor);
            }
        }

private void DrawTasksTab()
        {
            float w = Mathf.Floor(Mathf.Max(240f, GetMenuWorkWidth(260f, 760f) - 44f));
            float gap = 10f;
            bool compact = w < 540f;
            float colW = compact ? w : Mathf.Floor((w - gap) * 0.5f);
            float rowW = Mathf.Max(180f, colW - 34f);

            if (compact) GUILayout.BeginVertical(GUILayout.Width(w));
            else GUILayout.BeginHorizontal(GUILayout.Width(w));

            GUILayout.BeginVertical(GUILayout.Width(colW));
            GUILayout.BeginVertical(menuCardStyle, GUILayout.Width(colW), GUILayout.Height(122f));
            DrawMenuSectionHeader("H&S TASK DRAIN");
            hnsTaskDrain = DrawCompactToggle(hnsTaskDrain, "Drain Crew Timer", Mathf.RoundToInt(rowW));
            GUILayout.Space(3);
            GUILayout.Label($"Burst delay: {hnsTaskDrainStep:0.00}s | {(HnsTaskDrainFeature.Running ? "RUNNING" : "IDLE")}", toggleLabelStyle11);
            hnsTaskDrainStep = GUILayout.HorizontalSlider(hnsTaskDrainStep, 0.15f, 1.5f,
                sliderStyle, sliderThumbStyle, GUILayout.Width(rowW));
            GUILayout.EndVertical();

            GUILayout.Space(6);
            GUILayout.BeginVertical(menuCardStyle, GUILayout.Width(colW), GUILayout.Height(132f));
            DrawMenuSectionHeader("AUTO TASKS");
            autoTasksEnabled = DrawCompactToggle(autoTasksEnabled, "Complete Tasks Automatically", Mathf.RoundToInt(rowW));
            GUILayout.Space(3);
            GUILayout.Label($"Remaining: {AutoTasksFeature.RemainingTasks()}", toggleLabelStyle11);
            GUILayout.Label($"Delay: {autoTasksDelay:0.0}s", toggleLabelStyle11);
            autoTasksDelay = GUILayout.HorizontalSlider(autoTasksDelay, 0.8f, 6f,
                sliderStyle, sliderThumbStyle, GUILayout.Width(rowW));
            GUILayout.EndVertical();

            GUILayout.EndVertical();

            if (compact) GUILayout.Space(6); else GUILayout.Space(gap);

            GUILayout.BeginVertical(menuCardStyle, GUILayout.Width(colW), GUILayout.Height(360f));
            DrawMenuSectionHeader("HOST TASKS");
            GUILayout.Label("Clear, restore or flood tasks for players.", toggleLabelStyle11);
            GUILayout.Space(4);
            GUILayout.BeginHorizontal(GUILayout.Width(rowW));
            if (GUILayout.Button("FLOOD ALL", btnStyle, GUILayout.Width(Mathf.Floor((rowW - 4f) * 0.5f)), GUILayout.Height(24f))) FloodAllPlayersWithTasks();
            GUILayout.Space(4);
            if (GUILayout.Button("CLEAR ALL", btnStyle, GUILayout.Width(Mathf.Floor((rowW - 4f) * 0.5f)), GUILayout.Height(24f))) DeleteAllPlayerTasks();
            GUILayout.EndHorizontal();
            GUILayout.Space(6);

            taskPlayersScrollPos = GUILayout.BeginScrollView(taskPlayersScrollPos, false, true, GUIStyle.none, GUI.skin.verticalScrollbar, GUIStyle.none,
                GUILayout.Width(rowW + 8f), GUILayout.Height(292f));
            try
            {
                if (PlayerControl.AllPlayerControls == null)
                {
                    GUILayout.Label("No players.", toggleLabelStyle11);
                }
                else
                {
                    foreach (PlayerControl player in PlayerControl.AllPlayerControls)
                    {
                        if (player == null || player == PlayerControl.LocalPlayer || player.Data == null || player.Data.Disconnected) continue;

                        GUILayout.BeginHorizontal(boxStyle, GUILayout.Width(rowW), GUILayout.Height(28f));
                        GUILayout.Label(player.Data.PlayerName, toggleLabelStyle11, GUILayout.Width(Mathf.Max(70f, rowW - 184f)), GUILayout.Height(24f));
                        if (GUILayout.Button("CLEAR", btnStyle, GUILayout.Width(54f), GUILayout.Height(22f))) DeletePlayerTasks(player);
                        GUILayout.Space(2);
                        if (GUILayout.Button("NORMAL", btnStyle, GUILayout.Width(60f), GUILayout.Height(22f))) ChangePlayerTasks(player);
                        GUILayout.Space(2);
                        if (GUILayout.Button("FLOOD", btnStyle, GUILayout.Width(54f), GUILayout.Height(22f))) FloodPlayerWithTasks(player);
                        GUILayout.EndHorizontal();
                        GUILayout.Space(3);
                    }
                }
            }
            finally { GUILayout.EndScrollView(); }
            GUILayout.EndVertical();

            if (compact) GUILayout.EndVertical();
            else GUILayout.EndHorizontal();
        }

private void DrawSabotagesTab()
        {
            float outerContentWidth = Mathf.Floor(Mathf.Max(130f, GetMenuWorkWidth(150f, 760f) - 44f));
            float cardPaddingWidth = menuCardStyle != null && menuCardStyle.padding != null
                ? menuCardStyle.padding.left + menuCardStyle.padding.right
                : 28f;
            bool compactLayout = outerContentWidth < 340f;
            float columnGap = 10f;
            float sabotageColumnWidth = compactLayout ? outerContentWidth : Mathf.Floor((outerContentWidth - columnGap) * 0.5f);
            float doorColumnWidth = compactLayout ? outerContentWidth : outerContentWidth - columnGap - sabotageColumnWidth;

            float sabotageInnerWidth = Mathf.Max(compactLayout ? 84f : 118f, sabotageColumnWidth - cardPaddingWidth - 4f);
            float doorInnerWidth = Mathf.Max(compactLayout ? 84f : 118f, doorColumnWidth - cardPaddingWidth - 10f);
            float doorScrollWidth = Mathf.Max(86f, doorInnerWidth + 8f);
            float doorListWidth = Mathf.Max(72f, doorInnerWidth - 48f);
            float sabotagePairGap = 4f;
            float sabotageHalfWidth = Mathf.Floor((sabotageInnerWidth - sabotagePairGap) * 0.5f);
            float doorPairWidth = Mathf.Floor((doorInnerWidth - 6f) * 0.5f);
            int ventToggleWidth = Mathf.RoundToInt(Mathf.Max(compactLayout ? 48f : 70f, (sabotageInnerWidth - 6f) * 0.5f));
            float actionH = 24f;
            float criticalH = 138f;
            float systemsH = 142f;
            float doorActionsH = 102f;
            bool hasDoors = ShipStatus.Instance != null && ShipStatus.Instance.AllDoors != null;
            if (Event.current != null && Event.current.type == EventType.Layout)
                UpdateDoorRooms();
            float doorListHeight = hasDoors
                ? Mathf.Clamp(windowRect.height - 330f, 72f, 150f)
                : 86f;

            if (compactLayout) GUILayout.BeginVertical(GUILayout.Width(outerContentWidth));
            else GUILayout.BeginHorizontal(GUILayout.Width(outerContentWidth));

            GUILayout.BeginVertical(GUILayout.Width(sabotageColumnWidth));
            GUILayout.BeginVertical(menuCardStyle, GUILayout.Width(sabotageColumnWidth), GUILayout.Height(criticalH));
            DrawMenuSectionHeader("CRITICAL SABOTAGES");
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal(GUILayout.Width(sabotageInnerWidth));
            if (DrawColoredActionButton("FIX ALL", new Color32(83, 231, 139, 255), sabotageHalfWidth, actionH, true)) FixAllSabotages();
            GUILayout.Space(sabotagePairGap);
            if (DrawColoredActionButton("TRIGGER ALL", new Color32(255, 74, 74, 255), sabotageHalfWidth, actionH, true)) TriggerAllSabotages();
            GUILayout.EndHorizontal();
            GUILayout.Space(sabotagePairGap);

            GUILayout.BeginHorizontal(GUILayout.Width(sabotageInnerWidth));
            if (GUILayout.Button("MEETING", btnStyle, GUILayout.Width(sabotageHalfWidth), GUILayout.Height(actionH))) callMeetingPublic();
            GUILayout.Space(sabotagePairGap);
            if (GUILayout.Button("MAP", btnStyle, GUILayout.Width(sabotageHalfWidth), GUILayout.Height(actionH))) OpenSabotageMap();
            GUILayout.EndHorizontal();
            GUILayout.Space(sabotagePairGap);
            GUILayout.BeginHorizontal(GUILayout.Width(sabotageInnerWidth));
            if (GUILayout.Button(autoRepairSabotage ? "AUTO FIX ON" : "AUTO FIX", autoRepairSabotage ? activeTabStyle : btnStyle, GUILayout.Width(sabotageHalfWidth), GUILayout.Height(actionH)))
            {
                autoRepairSabotage = !autoRepairSabotage;
                if (autoRepairSabotage) autoBreakSabotage = false;
                settingsDirty = true;
            }
            GUILayout.Space(sabotagePairGap);
            if (GUILayout.Button(autoBreakSabotage ? "AUTO BREAK ON" : "AUTO BREAK", autoBreakSabotage ? activeTabStyle : btnStyle, GUILayout.Width(sabotageHalfWidth), GUILayout.Height(actionH)))
            {
                autoBreakSabotage = !autoBreakSabotage;
                if (autoBreakSabotage) autoRepairSabotage = false;
                settingsDirty = true;
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(3);
            spamMeetings = DrawCompactToggle(spamMeetings, "Spam Meetings", Mathf.RoundToInt(sabotageInnerWidth));
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();

            GUILayout.Space(6);
            GUILayout.BeginVertical(menuCardStyle, GUILayout.Width(sabotageColumnWidth), GUILayout.Height(systemsH));
            DrawMenuSectionHeader("SYSTEMS");
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal(GUILayout.Width(sabotageInnerWidth));
            DrawSabotageButton("Reactor", ref reactorSab, ToggleReactor, new Color32(255, 84, 84, 255), sabotageHalfWidth, actionH);
            GUILayout.Space(sabotagePairGap);
            DrawSabotageButton("Oxygen", ref oxygenSab, ToggleO2, new Color32(255, 132, 54, 255), sabotageHalfWidth, actionH);
            GUILayout.EndHorizontal();
            GUILayout.Space(4);

            GUILayout.BeginHorizontal(GUILayout.Width(sabotageInnerWidth));
            DrawSabotageButton("Comms", ref commsSab, ToggleComms, new Color32(66, 205, 128, 255), sabotageHalfWidth, actionH);
            GUILayout.Space(sabotagePairGap);
            DrawSabotageButton("Lights", ref elecSab, ToggleLights, new Color32(255, 218, 77, 255), sabotageHalfWidth, actionH);
            GUILayout.EndHorizontal();
            GUILayout.Space(4);

            GUILayout.BeginHorizontal(GUILayout.Width(sabotageInnerWidth));
            DrawSabotageButton("Bad Lights", ref unfixableLights, ToggleUnfixableLights, new Color32(210, 128, 255, 255), sabotageHalfWidth, actionH);
            GUILayout.Space(sabotagePairGap);
            if (GUILayout.Button("MUSHROOM", btnStyle, GUILayout.Width(sabotageHalfWidth), GUILayout.Height(actionH))) SabotageMushroom();
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();

            GUILayout.Space(6);
            GUILayout.BeginVertical(menuCardStyle, GUILayout.Width(sabotageColumnWidth), GUILayout.Height(62f));
            DrawMenuSectionHeader("VENTS");
            GUILayout.FlexibleSpace();
            unlockVents = DrawCompactToggle(unlockVents, "Unlock Vents", Mathf.RoundToInt(sabotageInnerWidth));
            GUILayout.Space(2);
            walkInVents = DrawCompactToggle(walkInVents, "Walk In Vents", Mathf.RoundToInt(sabotageInnerWidth));
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();

            GUILayout.EndVertical();

            GUILayout.Space(columnGap);

            GUILayout.BeginVertical(GUILayout.Width(doorColumnWidth));
            GUILayout.BeginVertical(menuCardStyle, GUILayout.Width(doorColumnWidth), GUILayout.Height(doorActionsH));
            DrawMenuSectionHeader("DOOR LOCKDOWN");
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal(GUILayout.Width(doorInnerWidth));
            if (DrawColoredActionButton("OPEN", new Color32(89, 219, 146, 255), doorPairWidth, actionH, true)) OpenAllDoors();
            GUILayout.Space(6);
            if (DrawColoredActionButton("CLOSE", new Color32(255, 106, 66, 255), doorPairWidth, actionH, true)) SabotageDoors();
            GUILayout.EndHorizontal();
            GUILayout.Space(4);
            if (DrawColoredActionButton("LOCK ALL", new Color32(255, 184, 64, 255), doorInnerWidth, actionH, true)) LockAllDoors();
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();

            GUILayout.Space(6);
            GUILayout.BeginVertical(menuCardStyle, GUILayout.Width(doorColumnWidth), GUILayout.Height(doorListHeight + 34f));
            DrawMenuSectionHeader("DOOR TARGETS");
            GUILayout.Space(2);

            if (hasDoors)
            {
                doorsScrollPos = GUILayout.BeginScrollView(doorsScrollPos, false, true, GUIStyle.none, GUI.skin.verticalScrollbar, GUIStyle.none, GUILayout.Width(doorScrollWidth), GUILayout.Height(doorListHeight));
                try
                {
                    GUILayout.BeginHorizontal(GUILayout.Width(doorScrollWidth - 8f));
                    try
                    {
                        GUILayout.BeginVertical(GUILayout.Width(doorListWidth));
                        try
                        {
                            foreach (var room in doorRooms)
                            {
                                DrawDoorTargetRow(room, doorListWidth);
                                GUILayout.Space(3);
                            }
                        }
                        finally { GUILayout.EndVertical(); }

                        GUILayout.Space(24f);
                    }
                    finally { GUILayout.EndHorizontal(); }
                }
                finally { GUILayout.EndScrollView(); }
            }
            else
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("<color=#777777>You are not in a game or this map has no doors.</color>", centeredRichLabelStyle);
                GUILayout.FlexibleSpace();
            }
            GUILayout.EndVertical();

            GUILayout.Space(6);
            GUILayout.BeginVertical(menuCardStyle, GUILayout.Width(doorColumnWidth), GUILayout.Height(268f));
            DrawMenuSectionHeader("ZIPLINE");
            GUILayout.Label(ZiplineControl.OnMap ? $"Selected: {ZiplineControl.TargetCount}   Loops: {ZiplineControl.LoopCount}" : "Fungle map only", toggleLabelStyle11);
            GUILayout.Space(3);
            PlayerControl zipTarget = ZiplineControl.CurrentTarget;
            string zipTargetName = zipTarget != null && zipTarget.Data != null ? zipTarget.Data.PlayerName : "No target";
            GUILayout.BeginHorizontal(GUILayout.Width(doorInnerWidth));
            if (GUILayout.Button("<", btnStyle, GUILayout.Width(24f), GUILayout.Height(actionH))) ZiplineControl.CycleTarget(-1);
            GUILayout.Label(zipTargetName, accentValueStyle, GUILayout.Height(actionH), GUILayout.ExpandWidth(true));
            if (GUILayout.Button(">", btnStyle, GUILayout.Width(24f), GUILayout.Height(actionH))) ZiplineControl.CycleTarget(1);
            GUILayout.EndHorizontal();
            GUILayout.Space(4);
            GUILayout.BeginHorizontal(GUILayout.Width(doorInnerWidth));
            bool markedZipTarget = zipTarget != null && ZiplineControl.IsTarget(zipTarget.PlayerId);
            if (GUILayout.Button(markedZipTarget ? "REMOVE" : "ADD", markedZipTarget ? activeTabStyle : btnStyle, GUILayout.Width(doorPairWidth), GUILayout.Height(actionH)) && zipTarget != null)
                ZiplineControl.ToggleTarget(zipTarget.PlayerId);
            GUILayout.Space(6);
            if (GUILayout.Button("SELECT ALL", btnStyle, GUILayout.Width(doorPairWidth), GUILayout.Height(actionH))) ZiplineControl.SelectAll();
            GUILayout.EndHorizontal();
            GUILayout.Space(4);
            if (GUILayout.Button("CLEAR TARGETS", btnStyle, GUILayout.Width(doorInnerWidth), GUILayout.Height(actionH))) ZiplineControl.ClearTargets();
            GUILayout.Space(4);
            GUILayout.BeginHorizontal(GUILayout.Width(doorInnerWidth));
            if (GUILayout.Button("SEND MARKED DOWN", btnStyle, GUILayout.Width(doorPairWidth), GUILayout.Height(actionH)))
            {
                int count = ZiplineControl.RideSelected(true);
                ShowNotification(count < 0 ? "<color=#FF0000>[ZIPLINE]</color> Fungle map required" : $"<color=#00FF00>[ZIPLINE]</color> Down: {count}");
            }
            GUILayout.Space(6);
            if (GUILayout.Button("SEND MARKED UP", btnStyle, GUILayout.Width(doorPairWidth), GUILayout.Height(actionH)))
            {
                int count = ZiplineControl.RideSelected(false);
                ShowNotification(count < 0 ? "<color=#FF0000>[ZIPLINE]</color> Fungle map required" : $"<color=#00FF00>[ZIPLINE]</color> Up: {count}");
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(4);
            GUILayout.BeginHorizontal(GUILayout.Width(doorInnerWidth));
            if (GUILayout.Button("START ALL DOWN", ZiplineControl.LoopCount > 0 ? activeTabStyle : btnStyle, GUILayout.Width(doorPairWidth), GUILayout.Height(actionH)))
            {
                int count = ZiplineControl.StartAll(true);
                ShowNotification(count < 0 ? "<color=#FF0000>[ZIPLINE]</color> Fungle map required" : $"<color=#00FF00>[ZIPLINE]</color> Loop all: {count}");
            }
            GUILayout.Space(6);
            if (GUILayout.Button("START ALL UP", ZiplineControl.LoopCount > 0 ? activeTabStyle : btnStyle, GUILayout.Width(doorPairWidth), GUILayout.Height(actionH)))
            {
                int count = ZiplineControl.StartAll(false);
                ShowNotification(count < 0 ? "<color=#FF0000>[ZIPLINE]</color> Fungle map required" : $"<color=#00FF00>[ZIPLINE]</color> Loop all: {count}");
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(4);
            if (GUILayout.Button($"STOP ALL LOOPS ({ZiplineControl.LoopCount})", btnStyle, GUILayout.Width(doorInnerWidth), GUILayout.Height(actionH)))
                ZiplineControl.StopAllLoops();
            GUILayout.EndVertical();
            GUILayout.EndVertical();

            if (compactLayout) GUILayout.EndVertical();
            else GUILayout.EndHorizontal();
        }

private static bool lobbySettingsLoaded;
private static int lobbySettingsGameId = int.MinValue;
private static bool lobbySettingsDirty;
private static float nextLobbySettingsReadAt;
private static bool lobbySettingsSyncQueued;
private static float lobbySettingsSyncAt;
private static bool lobbySettingsSyncRun;
private static bool lobbySettingsSyncHns;
private static int lobbySetMap;
private static int lobbySetPlayers = 10;
private static int lobbySetImps = 2;
private static bool lobbySetConfirm = true;
private static int lobbySetMeetings = 1;
private static bool lobbySetAnonymous;
private static int lobbySetMeetingCd = 15;
private static int lobbySetDiscuss = 15;
private static int lobbySetVoting = 120;
private static float lobbySetSpeed = 1f;
private static int lobbySetTaskBar;
private static bool lobbySetVisualTasks = true;
private static float lobbySetCrewVision = 1f;
private static float lobbySetImpVision = 1.5f;
private static float lobbySetKillCd = 22.5f;
private static int lobbySetKillDist = 1;
private static int lobbySetCommon = 1;
private static int lobbySetLong = 1;
private static int lobbySetShort = 2;
private static int roleEngineerCount;
private static int roleEngineerChance;
private static int roleScientistCount;
private static int roleScientistChance;
private static int roleGuardianCount;
private static int roleGuardianChance;
private static int roleShifterCount;
private static int roleShifterChance;
private static int roleNoisemakerCount;
private static int roleNoisemakerChance;
private static int roleTrackerCount;
private static int roleTrackerChance;
private static int rolePhantomCount;
private static int rolePhantomChance;
private static int roleDetectiveCount;
private static int roleDetectiveChance;
private static int roleViperCount;
private static int roleViperChance;
private static int roleJudgeCount;
private static int roleJudgeChance;
private static float roleEngineerCd = 10f;
private static float roleEngineerVent = 15f;
private static float roleScientistCd = 15f;
private static float roleScientistBattery = 5f;
    }
}
