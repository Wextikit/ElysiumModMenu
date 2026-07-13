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
private void DrawPlayersHistoryTab()
        {
            EnsurePlayerHistoryLoaded();

            GUILayout.BeginVertical(menuCardStyle);
            DrawMenuSectionHeader("PLAYER HISTORY");

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Entries: {playerHistoryEntries.Count}", new GUIStyle(toggleLabelStyle) { fontSize = 11, clipping = TextClipping.Overflow, wordWrap = false }, GUILayout.MinWidth(128), GUILayout.ExpandWidth(false), GUILayout.Height(24));
            GUILayout.Label("File: ElysiumPlayerHistory.txt", new GUIStyle(toggleLabelStyle) { fontSize = 11, clipping = TextClipping.Overflow, wordWrap = false }, GUILayout.MinWidth(220), GUILayout.ExpandWidth(false), GUILayout.Height(24));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Clear History", btnStyle, GUILayout.Width(136), GUILayout.Height(24)))
            {
                playerHistoryEntries.Clear();
                playerHistoryEntryLookup.Clear();
                playerHistoryViewRows.Clear();
                playerHistoryKeysById.Clear();
                playerHistoryKeysByClientId.Clear();
                playerHistoryLoaded = true;
                InvalidatePlayerHistoryViewCache();
                WritePlayerHistoryFile();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            playersHistoryScroll = GUILayout.BeginScrollView(playersHistoryScroll);
            if (playerHistoryEntries.Count == 0)
            {
                GUILayout.Label("<color=#777777>История пока пустая.</color>", new GUIStyle(GUI.skin.label) { richText = true, alignment = TextAnchor.MiddleCenter });
            }
            else
            {
                RebuildPlayerHistoryViewCache();

                GUIStyle historyHeaderStyle = new GUIStyle(GUI.skin.label) { richText = true, fontSize = 13, clipping = TextClipping.Clip };
                GUIStyle historyLineStyle = new GUIStyle(GUI.skin.label) { fontSize = 11, clipping = TextClipping.Clip };
                GUIStyle historyWrapStyle = new GUIStyle(GUI.skin.label) { fontSize = 11, wordWrap = false, clipping = TextClipping.Clip };

                int rowCount = playerHistoryViewRows.Count;
                int firstIndex = Mathf.Clamp(Mathf.FloorToInt(playersHistoryScroll.y / PlayerHistoryRowHeight), 0, Mathf.Max(0, rowCount - 1));
                int visibleRows = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(180f, windowRect.height - 170f) / PlayerHistoryRowHeight) + 3, 6, 30);
                int endIndex = Mathf.Min(rowCount, firstIndex + visibleRows);

                GUILayout.Space(firstIndex * PlayerHistoryRowHeight);
                for (int i = firstIndex; i < endIndex; i++)
                {
                    PlayerHistoryViewRow row = playerHistoryViewRows[i];
                    GUILayout.BeginVertical(GUILayout.Height(PlayerHistoryRowHeight - 2f));
                    GUILayout.Label(row.Header, historyHeaderStyle, GUILayout.Height(18));
                    GUILayout.Label(row.Identity, historyLineStyle, GUILayout.Height(16));
                    GUILayout.Label(row.Times, historyLineStyle, GUILayout.Height(16));
                    GUILayout.Label(row.Platform, historyWrapStyle, GUILayout.Height(16));
                    GUILayout.Label(row.Rpc, historyWrapStyle, GUILayout.Height(16));
                    GUILayout.EndVertical();
                    GUILayout.Space(2);
                }
                GUILayout.Space(Mathf.Max(0, rowCount - endIndex) * PlayerHistoryRowHeight);
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }
    }
}
