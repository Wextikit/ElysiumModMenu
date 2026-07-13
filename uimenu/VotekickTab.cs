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
        private void DrawVotekickTab()
        {
            GUIStyle voteInfoStyle = new GUIStyle(toggleLabelStyle) { richText = true, wordWrap = false, clipping = TextClipping.Clip };
            float outerContentWidth = GetMenuWorkWidth(220f, 760f);
            float cardPaddingWidth = menuCardStyle != null && menuCardStyle.padding != null
                ? menuCardStyle.padding.left + menuCardStyle.padding.right
                : 28f;
            float innerWidth = Mathf.Max(260f, outerContentWidth - cardPaddingWidth - 4f);
            float gap = 6f;
            float statusW = 98f;
            int toggleW = Mathf.RoundToInt(Mathf.Max(92f, (innerWidth - statusW - gap * 3f) / 3f));
            float voteBtnW = Mathf.Floor((innerWidth - gap) * 0.5f);

            GUILayout.BeginVertical(menuCardStyle, GUILayout.Width(outerContentWidth), GUILayout.Height(104f));
            try
            {
                DrawMenuSectionHeader(L("VOTEKICK MENU", "РђР’РўРћ-Р“РћР›РћРЎРћР’РђРќРР•"));

                GUILayout.BeginHorizontal(GUILayout.Width(innerWidth), GUILayout.Height(18f));
                string statusText = VotekickStatusText();
                string statusColor = statusText == "OFF" ? "#999999" : "#39FF14";
                GUILayout.Label($"<b>Status: <color={statusColor}>{statusText}</color></b>", voteInfoStyle, GUILayout.Width(statusW), GUILayout.Height(18));
                GUILayout.Space(gap);
                string autoButtonText = L("AUTO CYCLE", "AUTO CYCLE");
                bool autoCycle = DrawCompactToggle(votekickEveryone, autoButtonText, toggleW);
                if (autoCycle != votekickEveryone)
                {
                    if (autoCycle) StartVotekickEveryoneRun();
                    else StopVotekickEveryoneRun();
                }
                GUILayout.Space(gap);
                votekickAutoRejoin = DrawCompactToggle(votekickAutoRejoin, L("AUTO REJOIN", "AUTO REJOIN"), toggleW);
                GUILayout.Space(gap);
                votekickCopyCode = DrawCompactToggle(votekickCopyCode, L("COPY CODE", "COPY CODE"), toggleW);
                GUILayout.EndHorizontal();
                GUILayout.Space(5);

                GUILayout.BeginHorizontal(GUILayout.Width(innerWidth), GUILayout.Height(22f));
                if (GUILayout.Button(L("SEND x3 + STAY", "SEND x3 + STAY"), btnStyle, GUILayout.Width(voteBtnW), GUILayout.Height(22)))
                    SendVotekickEveryoneStay();
                GUILayout.Space(gap);
                if (GUILayout.Button(L("SWEEP ALL x3", "SWEEP ALL x3"), btnStyle, GUILayout.Width(voteBtnW), GUILayout.Height(22)))
                    RunVotekickRapidAll();
                GUILayout.EndHorizontal();
                GUILayout.Space(3);
                voteInfoStyle.wordWrap = false;
                voteInfoStyle.clipping = TextClipping.Clip;
                GUILayout.Label("<color=#ca08ff><b>i</b></color> <color=#888888>Auto cycle: vote all, leave, rejoin, repeat twice, then sweep.</color>", voteInfoStyle, GUILayout.Width(innerWidth), GUILayout.Height(16f));
            }
            finally { GUILayout.EndVertical(); }

            int curPlayers = 0;
            if (PlayerControl.AllPlayerControls != null)
            {
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    if (pc != null && pc.Data != null && pc.PlayerId < 100 && pc != PlayerControl.LocalPlayer)
                        curPlayers++;
                }
            }

            GUILayout.Space(6);
            GUILayout.BeginVertical(menuCardStyle, GUILayout.Width(outerContentWidth));
            DrawMenuSectionHeader($"{L("TARGET VOTE", "Р’Р«Р‘РћР  Р¦Р•Р›Р")} ({curPlayers})");

            if (PlayerControl.AllPlayerControls != null)
            {
                var safePlayersList = new System.Collections.Generic.List<PlayerControl>();
                foreach (var p in PlayerControl.AllPlayerControls) safePlayersList.Add(p);

                float listH = 15f * 27f + 8f;
                votekickScrollPosition = GUILayout.BeginScrollView(votekickScrollPosition, false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUIStyle.none, GUILayout.Height(listH));
                try
                {
                    foreach (var pc in safePlayersList)
                    {
                        if (pc == null || pc.Data == null || pc.PlayerId >= 100 || pc == PlayerControl.LocalPlayer) continue;

                        GUILayout.BeginHorizontal(boxStyle, GUILayout.Width(innerWidth), GUILayout.Height(26));
                        try
                        {
                            string pName = pc.Data.PlayerName ?? "Unknown";
                            bool isHost = (AmongUsClient.Instance != null && AmongUsClient.Instance.GetHost()?.Character == pc);

                            string hexColor = "#FFFFFF";
                            try
                            {
                                var pColor = Palette.PlayerColors[pc.Data.DefaultOutfit.ColorId];
                                hexColor = $"#{(byte)(pColor.r * 255f):X2}{(byte)(pColor.g * 255f):X2}{(byte)(pColor.b * 255f):X2}";
                            }
                            catch { }

                            string displayStr = $"<color={hexColor}>{pName}</color>" + (isHost ? " <color=#FF3333>[Host]</color>" : "");

                            GUILayout.Space(4);
                            GUILayout.Label(displayStr, voteInfoStyle, GUILayout.Height(20));

                            GUILayout.FlexibleSpace();

                            if (GUILayout.Button(L("Vote", "Р“РѕР»РѕСЃ"), btnStyle, GUILayout.Width(58), GUILayout.Height(20)))
                                ExecuteVotekickTarget(pc);
                        }
                        finally
                        {
                            GUILayout.EndHorizontal();
                        }
                        GUILayout.Space(1);
                    }
                }
                finally
                {
                    GUILayout.EndScrollView();
                }
            }
            GUILayout.EndVertical();
        }
    }
}
