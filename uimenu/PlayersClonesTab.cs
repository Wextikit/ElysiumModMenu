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
        private static readonly string[] cloneFormations = { "Line", "Circle", "Grid", "Wave", "Heart", "Star", "Spiral", "Cross", "Diamond", "Network", "Elysium" };

        private void DrawPlayersClonesTab()
        {
            float w = Mathf.Floor(Mathf.Max(220f, GetMenuWorkWidth(220f, 760f) - 36f));
            float innerW = Mathf.Max(160f, w - 28f);
            NetworkedClones.ClickMode = false;

            GUIStyle st = historyInfoStyle;
            string host = NetworkedClones.Ready() ? "<color=#55FF88>READY</color>" : "<color=#FF6666>HOST ONLY</color>";
            PlayerControl target = GetSelectedCloneTarget();

            GUILayout.BeginVertical(menuCardStyle, GUILayout.Width(w), GUILayout.Height(414f));
            try
            {
                DrawMenuSectionHeader("CLONES");
                GUILayout.Label($"Status: {host}   Live: <color=#{GetMenuAccentHex(false)}>{NetworkedClones.Live}</color>   Queue: <color=#{GetMenuAccentHex(false)}>{NetworkedClones.Queued}</color>", st, GUILayout.Width(innerW), GUILayout.Height(20));

                string autoClear = "Auto Clear Before Game: " + (NetworkedClones.AutoClearBeforeGame ? "ON" : "OFF");
                if (GUILayout.Button(autoClear, NetworkedClones.AutoClearBeforeGame ? activeTabStyle : btnStyle, GUILayout.Width(innerW), GUILayout.Height(25f)))
                {
                    NetworkedClones.AutoClearBeforeGame = !NetworkedClones.AutoClearBeforeGame;
                    SaveConfig();
                }

            if (GUILayout.Button(L("Delete Last Figure", "УДАЛИТЬ ПОСЛЕДНЮЮ ФИГУРУ"), btnStyle, GUILayout.Width(innerW), GUILayout.Height(25f)))
                {
                    int removed = NetworkedClones.ClearLastFigure();
                    ShowNotification(removed > 0
                        ? "<color=#FFAA55>[CLONES]</color> Removed last figure: " + removed
                        : "<color=#FFAA55>[CLONES]</color> No figure to remove.");
                }

            if (GUILayout.Button(L("Delete All Clones", "УДАЛИТЬ ВСЕХ КЛОНОВ"), btnStyle, GUILayout.Width(innerW), GUILayout.Height(25f)))
                {
                    if (!NetworkedClones.Ready())
                    {
                        ShowNotification("<color=#FF5555>[CLONES]</color> Host only.");
                        return;
                    }

                    int removed = NetworkedClones.ClearAll();
                    ShowNotification("<color=#FF5555>[CLONES]</color> Removed clones: " + removed);
                }

                GUILayout.Space(4);
                DrawMenuSectionHeader("TARGET SPAWN");
                cloneTargetScroll = GUILayout.BeginScrollView(cloneTargetScroll, false, true, GUIStyle.none, GUI.skin.verticalScrollbar, GUIStyle.none, GUILayout.Width(innerW), GUILayout.Height(112f));
                try
                {
                    if (lockedPlayersList != null)
                    {
                        foreach (PlayerControl pc in lockedPlayersList)
                        {
                            if (pc == null || pc.Data == null || pc.PlayerId >= 100 || NetworkedClones.IsClone(pc)) continue;
                            bool isSelected = selectedCloneTargetId == pc.PlayerId;
                            string pName = pc.Data.PlayerName ?? "Unknown";
                            Color oldContent = GUI.contentColor;
                            try { GUI.contentColor = Palette.PlayerColors[pc.Data.DefaultOutfit.ColorId]; } catch { GUI.contentColor = Color.white; }
                            if (GUILayout.Button(pName, isSelected ? activeTabStyle : btnStyle, GUILayout.Width(innerW - 18f), GUILayout.Height(24f)))
                            {
                                selectedCloneTargetId = pc.PlayerId;
                                target = pc;
                            }
                            GUI.contentColor = oldContent;
                        }
                    }
                }
                finally { GUILayout.EndScrollView(); }

                GUILayout.BeginHorizontal(GUILayout.Width(innerW));
                try
                {
                    if (GUILayout.Button(L("Target Spawn", "СПАВН ЦЕЛИ"), activeTabStyle, GUILayout.Width(130f), GUILayout.Height(25f)))
                    {
                        string msg = NetworkedClones.CloneOf(target);
                        ShowNotification("<color=#AA77FF>[CLONES]</color> " + msg);
                    }
                    if (GUILayout.Button(L("My Spawn", "МОЙ СПАВН"), activeTabStyle, GUILayout.Width(110f), GUILayout.Height(25f)))
                    {
                        string msg = NetworkedClones.CloneOf(PlayerControl.LocalPlayer);
                        ShowNotification("<color=#AA77FF>[CLONES]</color> " + msg);
                    }
                }
                finally { GUILayout.EndHorizontal(); }

                GUILayout.Space(6);
                DrawMenuSectionHeader("TARGET PATTERN");

                GUILayout.BeginHorizontal(GUILayout.Width(innerW));
                try
                {
                    if (GUILayout.Button("<", btnStyle, GUILayout.Width(28f), GUILayout.Height(24f)))
                    {
                        cloneFormationIdx--;
                        if (cloneFormationIdx < 0) cloneFormationIdx = cloneFormations.Length - 1;
                    }
                    GUILayout.Label(cloneFormations[cloneFormationIdx], centeredActiveTabStyle, GUILayout.Width(150f), GUILayout.Height(24f));
                    if (GUILayout.Button(">", btnStyle, GUILayout.Width(28f), GUILayout.Height(24f)))
                    {
                        cloneFormationIdx++;
                        if (cloneFormationIdx >= cloneFormations.Length) cloneFormationIdx = 0;
                    }
                }
                finally { GUILayout.EndHorizontal(); }

                GUILayout.BeginHorizontal(GUILayout.Width(innerW));
                try
                {
                    GUILayout.Label($"Count: {cloneFormationCount}", st, GUILayout.Width(72f), GUILayout.Height(22f));
                    cloneFormationCount = Mathf.Clamp(Mathf.RoundToInt(GUILayout.HorizontalSlider(cloneFormationCount, 1f, NetworkedClones.MaxCloneCount, sliderStyle, sliderThumbStyle, GUILayout.Width(Mathf.Max(90f, innerW - 86f)))), 1, NetworkedClones.MaxCloneCount);
                }
                finally { GUILayout.EndHorizontal(); }

                GUILayout.BeginHorizontal(GUILayout.Width(innerW));
                try
                {
                    GUILayout.Label($"Width: {cloneFormationWidth:0.00}x", st, GUILayout.Width(86f), GUILayout.Height(22f));
                    cloneFormationWidth = Mathf.Clamp(GUILayout.HorizontalSlider(cloneFormationWidth, 0.25f, 5f, sliderStyle, sliderThumbStyle, GUILayout.Width(Mathf.Max(90f, innerW - 100f))), 0.25f, 5f);
                }
                finally { GUILayout.EndHorizontal(); }

            if (GUILayout.Button(L("Target Pattern", "ПОСТРОЕНИЕ ЦЕЛИ"), activeTabStyle, GUILayout.Width(innerW), GUILayout.Height(25f)))
                {
                    string msg = NetworkedClones.FormationOf(target, cloneFormationIdx, cloneFormationCount, cloneFormationWidth);
                    ShowNotification("<color=#AA77FF>[CLONES]</color> " + msg);
                }
            }
            finally { GUILayout.EndVertical(); }
        }

        private PlayerControl GetSelectedCloneTarget()
        {
            try
            {
                PlayerControl target = lockedPlayersList.FirstOrDefault(p => p != null && p.PlayerId == selectedCloneTargetId && !NetworkedClones.IsClone(p));
                if (target != null) return target;

                target = lockedPlayersList.FirstOrDefault(p => p != null && p.Data != null && p.PlayerId < 100 && !NetworkedClones.IsClone(p));
                if (target != null) selectedCloneTargetId = target.PlayerId;
                return target;
            }
            catch
            {
                return null;
            }
        }
    }
}
