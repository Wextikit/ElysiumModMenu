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
        private static readonly string[] cloneFormations = BuildCloneFormations();

        private static string[] BuildCloneFormations()
        {
            string[] baseNames = { "Line", "Circle", "Grid", "Wave", "Heart", "Star", "Spiral", "Cross", "Diamond", "Network", "Elysium" };
            string[] all = new string[baseNames.Length + CloneGlyphs.Names.Length];
            baseNames.CopyTo(all, 0);
            CloneGlyphs.Names.CopyTo(all, baseNames.Length);
            return all;
        }

        private void DrawPlayersClonesTab()
        {
            float w = Mathf.Floor(Mathf.Max(220f, GetMenuWorkWidth(220f, 760f) - 36f));
            float innerW = Mathf.Max(160f, w - 28f);
            float halfW = Mathf.Floor((innerW - 6f) * 0.5f);
            NetworkedClones.ClickMode = false;

            GUIStyle st = historyInfoStyle;
            string hex = GetMenuAccentHex(false);
            string host = NetworkedClones.Ready() ? "<color=#55FF88>READY</color>" : "<color=#FF6666>HOST ONLY</color>";
            PlayerControl target = GetSelectedCloneTarget();

            GUILayout.BeginVertical(menuCardStyle, GUILayout.Width(w));
            try
            {
                DrawMenuSectionHeader("CLONES");
                GUILayout.Label($"{host}   <color=#{hex}>{NetworkedClones.Live}</color> live   <color=#{hex}>{NetworkedClones.Queued}</color> queue", st, GUILayout.Width(innerW), GUILayout.Height(18f));
                GUILayout.Space(6);

                cloneTargetScroll = GUILayout.BeginScrollView(cloneTargetScroll, false, true, GUIStyle.none, GUI.skin.verticalScrollbar, GUIStyle.none, GUILayout.Width(innerW), GUILayout.Height(96f));
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
                            if (GUILayout.Button(pName, isSelected ? activeTabStyle : btnStyle, GUILayout.Width(innerW - 18f), GUILayout.Height(22f)))
                            {
                                selectedCloneTargetId = pc.PlayerId;
                                target = pc;
                            }
                            GUI.contentColor = oldContent;
                        }
                    }
                }
                finally { GUILayout.EndScrollView(); }

                GUILayout.Space(6);

                GUILayout.BeginHorizontal(GUILayout.Width(innerW));
                try
                {
                    if (GUILayout.Button(L("Target Spawn", "СПАВН ЦЕЛИ"), btnStyle, GUILayout.Width(halfW), GUILayout.Height(22f)))
                    {
                        string msg = NetworkedClones.CloneOf(target);
                        ShowNotification("<color=#AA77FF>[CLONES]</color> " + msg);
                    }
                    GUILayout.Space(6);
                    if (GUILayout.Button(L("My Spawn", "МОЙ СПАВН"), btnStyle, GUILayout.Width(halfW), GUILayout.Height(22f)))
                    {
                        string msg = NetworkedClones.CloneOf(PlayerControl.LocalPlayer);
                        ShowNotification("<color=#AA77FF>[CLONES]</color> " + msg);
                    }
                }
                finally { GUILayout.EndHorizontal(); }

                GUILayout.Space(6);

                GUILayout.BeginHorizontal(GUILayout.Width(innerW));
                try
                {
                    if (GUILayout.Button("<", btnStyle, GUILayout.Width(24f), GUILayout.Height(22f)))
                    {
                        cloneFormationIdx--;
                        if (cloneFormationIdx < 0) cloneFormationIdx = cloneFormations.Length - 1;
                        settingsDirty = true;
                    }
                    GUILayout.Label(cloneFormations[cloneFormationIdx], centeredActiveTabStyle, GUILayout.Width(Mathf.Max(90f, innerW - 60f)), GUILayout.Height(22f));
                    if (GUILayout.Button(">", btnStyle, GUILayout.Width(24f), GUILayout.Height(22f)))
                    {
                        cloneFormationIdx++;
                        if (cloneFormationIdx >= cloneFormations.Length) cloneFormationIdx = 0;
                        settingsDirty = true;
                    }
                }
                finally { GUILayout.EndHorizontal(); }

                GUILayout.Space(6);

                GUILayout.BeginHorizontal(GUILayout.Width(innerW));
                try
                {
                    GUILayout.Label($"Count <color=#{hex}>{cloneFormationCount}</color>", st, GUILayout.Width(78f), GUILayout.Height(20f));
                    int prevCount = cloneFormationCount;
                    cloneFormationCount = Mathf.Clamp(Mathf.RoundToInt(GUILayout.HorizontalSlider(cloneFormationCount, 1f, NetworkedClones.MaxCloneCount, sliderStyle, sliderThumbStyle, GUILayout.Width(Mathf.Max(90f, innerW - 84f)))), 1, NetworkedClones.MaxCloneCount);
                    if (prevCount != cloneFormationCount) settingsDirty = true;
                }
                finally { GUILayout.EndHorizontal(); }

                GUILayout.Space(4);

                GUILayout.BeginHorizontal(GUILayout.Width(innerW));
                try
                {
                    GUILayout.Label($"Width <color=#{hex}>{cloneFormationWidth:0.00}x</color>", st, GUILayout.Width(78f), GUILayout.Height(20f));
                    float prevWidth = cloneFormationWidth;
                    cloneFormationWidth = Mathf.Clamp(GUILayout.HorizontalSlider(cloneFormationWidth, 0.25f, 5f, sliderStyle, sliderThumbStyle, GUILayout.Width(Mathf.Max(90f, innerW - 84f))), 0.25f, 5f);
                    if (Mathf.Abs(prevWidth - cloneFormationWidth) > 0.001f) settingsDirty = true;
                }
                finally { GUILayout.EndHorizontal(); }

                GUILayout.Space(6);

                if (GUILayout.Button(L("Target Pattern", "ПОСТРОЕНИЕ ЦЕЛИ"), activeTabStyle, GUILayout.Width(innerW), GUILayout.Height(24f)))
                {
                    string msg = NetworkedClones.FormationOf(target, cloneFormationIdx, cloneFormationCount, cloneFormationWidth);
                    ShowNotification("<color=#AA77FF>[CLONES]</color> " + msg);
                }

                GUILayout.Space(6);

                if (GUILayout.Button(L("Delete Last Figure", "УДАЛИТЬ ПОСЛЕДНЮЮ ФИГУРУ"), btnStyle, GUILayout.Width(innerW), GUILayout.Height(22f)))
                {
                    int removed = NetworkedClones.ClearLastFigure();
                    ShowNotification(removed > 0
                        ? "<color=#FFAA55>[CLONES]</color> Removed last figure: " + removed
                        : "<color=#FFAA55>[CLONES]</color> No figure to remove.");
                }

                GUILayout.Space(4);

                if (GUILayout.Button(L("Delete All Clones", "УДАЛИТЬ ВСЕХ КЛОНОВ"), btnStyle, GUILayout.Width(innerW), GUILayout.Height(22f)))
                {
                    if (NetworkedClones.Ready())
                    {
                        int removed = NetworkedClones.ClearAll();
                        ShowNotification("<color=#FF5555>[CLONES]</color> Removed clones: " + removed);
                    }
                    else ShowNotification("<color=#FF5555>[CLONES]</color> Host only.");
                }

                GUILayout.Space(4);

                string autoClear = "Auto Clear Before Game: " + (NetworkedClones.AutoClearBeforeGame ? "ON" : "OFF");
                if (GUILayout.Button(autoClear, NetworkedClones.AutoClearBeforeGame ? activeTabStyle : btnStyle, GUILayout.Width(innerW), GUILayout.Height(22f)))
                {
                    NetworkedClones.AutoClearBeforeGame = !NetworkedClones.AutoClearBeforeGame;
                    settingsDirty = true;
                }
            }
            finally { GUILayout.EndVertical(); }
        }

        private PlayerControl GetSelectedCloneTarget()
        {
            try
            {
                PlayerControl first = null;
                foreach (PlayerControl p in lockedPlayersList)
                {
                    if (p == null || NetworkedClones.IsClone(p)) continue;
                    if (p.PlayerId == selectedCloneTargetId) return p;
                    if (first == null && p.Data != null && p.PlayerId < 100) first = p;
                }

                if (first != null) selectedCloneTargetId = first.PlayerId;
                return first;
            }
            catch
            {
                return null;
            }
        }
    }
}
