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
            float w = GetMenuWorkWidth(220f, 760f);
            playersClonesScroll = GUILayout.BeginScrollView(playersClonesScroll, false, true, GUIStyle.none, GUI.skin.verticalScrollbar, GUIStyle.none);
            GUILayout.BeginVertical(menuCardStyle, GUILayout.Width(w));
            DrawMenuSectionHeader("NETWORKED CLONES");

            GUILayout.Label("For clones to accept positions, delete the lobby through Maps first.", menuDescStyle);
            GUILayout.Space(6);

            GUIStyle st = new GUIStyle(toggleLabelStyle) { richText = true, fontSize = 11, clipping = TextClipping.Clip };
            string host = NetworkedClones.Ready() ? "<color=#55FF88>READY</color>" : "<color=#FF6666>HOST ONLY</color>";
            GUILayout.Label($"Status: {host}   Live: <color=#{GetMenuAccentHex(false)}>{NetworkedClones.Live}</color>   Queue: <color=#{GetMenuAccentHex(false)}>{NetworkedClones.Queued}</color>", st, GUILayout.Height(20));

            GUILayout.Space(4);
            NetworkedClones.ClickMode = DrawToggle(NetworkedClones.ClickMode, "Click Clone Mode", 220);
            GUILayout.Label("Left click creates a clone at cursor. Right click removes the nearest clone.", menuDescStyle);

            GUILayout.Space(8);
            PlayerControl target = null;
            try { target = lockedPlayersList.FirstOrDefault(p => p != null && p.PlayerId == selectedAntiCheatPlayerId); } catch { }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Clone Selected", btnStyle, GUILayout.Width(145), GUILayout.Height(24)))
            {
                string msg = NetworkedClones.CloneOf(target);
                ShowNotification("<color=#AA77FF>[CLONES]</color> " + msg);
            }
            GUILayout.Label(target != null && target.Data != null ? target.Data.PlayerName : "No selected player", st, GUILayout.Height(24));
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            DrawMenuSectionHeader("FORMATION");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("<", btnStyle, GUILayout.Width(28), GUILayout.Height(22)))
            {
                cloneFormationIdx--;
                if (cloneFormationIdx < 0) cloneFormationIdx = cloneFormations.Length - 1;
            }
            GUILayout.Label(cloneFormations[cloneFormationIdx], new GUIStyle(activeTabStyle) { alignment = TextAnchor.MiddleCenter }, GUILayout.Width(120), GUILayout.Height(22));
            if (GUILayout.Button(">", btnStyle, GUILayout.Width(28), GUILayout.Height(22)))
            {
                cloneFormationIdx++;
                if (cloneFormationIdx >= cloneFormations.Length) cloneFormationIdx = 0;
            }
            GUILayout.Space(12);
            GUILayout.Label($"Count: {cloneFormationCount}", st, GUILayout.Width(70), GUILayout.Height(22));
            cloneFormationCount = Mathf.Clamp((int)GUILayout.HorizontalSlider(cloneFormationCount, 1f, 60f, sliderStyle, sliderThumbStyle, GUILayout.Width(Mathf.Max(120f, w - 420f))), 1, 60);
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Spawn Formation", activeTabStyle, GUILayout.Width(160), GUILayout.Height(24)))
            {
                string msg = NetworkedClones.Formation(cloneFormationIdx, cloneFormationCount);
                ShowNotification("<color=#AA77FF>[CLONES]</color> " + msg);
            }

            GUI.backgroundColor = new Color(0.8f, 0.18f, 0.18f, 1f);
            if (GUILayout.Button("Clear Clones", btnStyle, GUILayout.Width(130), GUILayout.Height(24)))
            {
                NetworkedClones.ClearAll();
                ShowNotification("<color=#AA77FF>[CLONES]</color> Cleared");
            }
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            GUILayout.Label("Clones are networked fake player objects. They are cleared on round intro and when you press Clear Clones.", menuDescStyle);
            GUILayout.EndVertical();
            GUILayout.EndScrollView();
        }
    }
}
