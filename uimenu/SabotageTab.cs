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
private void DrawSabotageAnimationTab()
        {
            float tabWidth = GetMenuWorkWidth(180f, 760f);
            string[] tabs = new string[]
            {
                sabotageSubTabs.Length > 0 ? sabotageSubTabs[0] : "SABOTAGES",
                "LOBBY SETTINGS",
                "H&S",
                sabotageSubTabs.Length > 1 ? sabotageSubTabs[1] : "ANIMATIONS"
            };
            currentSabotageSubTab = Mathf.Clamp(currentSabotageSubTab, 0, tabs.Length - 1);
            GUILayout.BeginHorizontal(GUILayout.Width(tabWidth), GUILayout.Height(24));
            for (int i = 0; i < tabs.Length; i++)
            {
                if (GUILayout.Button(tabs[i], currentSabotageSubTab == i ? activeSubTabStyle : subTabStyle, GUILayout.Height(22)))
                {
                    currentSabotageSubTab = i;
                    scrollPosition = Vector2.zero;
                }
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(8);

            if (currentSabotageSubTab == 0) DrawSabotagesTab();
            else if (currentSabotageSubTab == 1) DrawLobbySettingsTab();
            else if (currentSabotageSubTab == 2) DrawHnsSettingsTab();
            else DrawAnimationsTab();
        }

private void DrawSabotagesTab()
        {
            GUIStyle miniLabelStyle = new GUIStyle(toggleLabelStyle) { fontSize = 11, richText = true, wordWrap = true };
            miniLabelStyle.normal.textColor = whiteMenuTheme ? new Color(0.25f, 0.25f, 0.25f, 1f) : new Color(0.72f, 0.72f, 0.72f, 1f);
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
            float criticalH = 110f;
            float systemsH = 142f;
            float doorActionsH = 102f;
            bool hasDoors = ShipStatus.Instance != null && ShipStatus.Instance.AllDoors != null;
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
                var rooms = ShipStatus.Instance.AllDoors
                    .Where(d => d != null)
                    .Select(d => d.Room)
                    .Distinct()
                    .OrderBy(r => r.ToString())
                    .ToList();

                doorsScrollPos = GUILayout.BeginScrollView(doorsScrollPos, false, true, GUIStyle.none, GUI.skin.verticalScrollbar, GUIStyle.none, GUILayout.Width(doorScrollWidth), GUILayout.Height(doorListHeight));
                GUILayout.BeginHorizontal(GUILayout.Width(doorScrollWidth - 8f));
                GUILayout.BeginVertical(GUILayout.Width(doorListWidth));
                foreach (var room in rooms)
                {
                    DrawDoorTargetRow(room, doorListWidth);
                    GUILayout.Space(3);
                }
                GUILayout.EndVertical();
                GUILayout.Space(24f);
                GUILayout.EndHorizontal();
                GUILayout.EndScrollView();
            }
            else
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("<color=#777777>Р’С‹ РЅРµ РІ РёРіСЂРµ РёР»Рё РЅР° РєР°СЂС‚Рµ РЅРµС‚ РґРІРµСЂРµР№.</color>", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, richText = true });
                GUILayout.FlexibleSpace();
            }
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
private static float roleEngineerCd = 10f;
private static float roleEngineerVent = 15f;
private static float roleScientistCd = 15f;
private static float roleScientistBattery = 5f;
    }
}
