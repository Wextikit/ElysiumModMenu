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
            float outerContentWidth = GetMenuWorkWidth(220f, 760f);
            float cardPaddingWidth = menuCardStyle != null && menuCardStyle.padding != null
                ? menuCardStyle.padding.left + menuCardStyle.padding.right
                : 28f;
            float innerWidth = Mathf.Max(260f, outerContentWidth - cardPaddingWidth - 4f);
            float gap = 6f;
            float statusW = 98f;
            int toggleW = Mathf.RoundToInt(Mathf.Max(92f, (innerWidth - statusW - gap * 3f) / 3f));
            float voteBtnW = Mathf.Floor((innerWidth - gap) * 0.5f);

            GUILayout.BeginVertical(menuCardStyle, GUILayout.Width(outerContentWidth), GUILayout.Height(154f));
            try
            {
                DrawMenuSectionHeader(L("VOTEKICK MENU", "РђР’РўРћ-Р“РћР›РћРЎРћР’РђРќРР•"));

                GUILayout.BeginHorizontal(GUILayout.Width(innerWidth), GUILayout.Height(18f));
                string statusText = VotekickStatusText();
                string statusColor = statusText == "OFF" ? "#999999" : "#39FF14";
                GUILayout.Label($"<b>Status: <color={statusColor}>{statusText}</color></b>", voteInfoStyle, GUILayout.Width(statusW), GUILayout.Height(18));
                GUILayout.Space(gap);
                string autoButtonText = L("AUTO CYCLE", "АВТО ЦИКЛ");
                bool autoCycle = DrawCompactToggle(votekickEveryone, autoButtonText, toggleW);
                if (autoCycle != votekickEveryone)
                {
                    if (autoCycle) StartVotekickEveryoneRun();
                    else StopVotekickEveryoneRun();
                }
                GUILayout.Space(gap);
                votekickAutoRejoin = DrawCompactToggle(votekickAutoRejoin, L("AUTO REJOIN", "АВТО ВОЗВРАТ"), toggleW);
                GUILayout.Space(gap);
                votekickCopyCode = DrawCompactToggle(votekickCopyCode, L("COPY CODE", "КОПИРОВАТЬ КОД"), toggleW);
                GUILayout.EndHorizontal();
                GUILayout.Space(5);

                GUILayout.BeginHorizontal(GUILayout.Width(innerWidth), GUILayout.Height(22f));
                if (GUILayout.Button(L("SEND x3 + STAY", "ОТПРАВИТЬ x3 + ОСТАТЬСЯ"), btnStyle, GUILayout.Width(voteBtnW), GUILayout.Height(22)))
                    SendVotekickEveryoneStay();
                GUILayout.Space(gap);
                if (GUILayout.Button(L("SWEEP ALL x3", "ПРОЙТИ ВСЕХ x3"), btnStyle, GUILayout.Width(voteBtnW), GUILayout.Height(22)))
                    RunVotekickRapidAll();
                GUILayout.EndHorizontal();

                GUILayout.Space(4);
                GUILayout.BeginHorizontal(GUILayout.Width(innerWidth), GUILayout.Height(22f));
                string autoTargetsText = votekickTargetAuto
                    ? L("AUTO TARGETS: STOP", "АВТО ЦЕЛИ: СТОП")
                    : $"{L("AUTO TARGETS", "АВТО ЦЕЛИ")} ({VotekickTargetCount()})";
                if (GUILayout.Button(autoTargetsText, votekickTargetAuto ? activeTabStyle : btnStyle, GUILayout.Width(voteBtnW), GUILayout.Height(22)))
                    ToggleVotekickTargetAuto();
                GUILayout.Space(gap);
                if (GUILayout.Button(L("CLEAR TARGETS", "ОЧИСТИТЬ ЦЕЛИ"), btnStyle, GUILayout.Width(voteBtnW), GUILayout.Height(22)))
                    ClearVotekickTargets();
                GUILayout.EndHorizontal();

                GUILayout.Space(4);
                bool hostTarget = VotekickHostIsTarget();
                if (GUILayout.Button(hostTarget ? L("HOST TARGET: ON", "ХОСТ — ЦЕЛЬ: ВКЛ") : L("HOST TARGET", "ХОСТ — ЦЕЛЬ"), hostTarget ? activeTabStyle : btnStyle, GUILayout.Width(innerWidth), GUILayout.Height(22)))
                    ToggleVotekickHostTarget();

                GUILayout.Space(3);
                voteInfoStyle.wordWrap = false;
                voteInfoStyle.clipping = TextClipping.Clip;
                GUILayout.Label("<color=#ca08ff><b>i</b></color> <color=#888888>If targets are marked, auto cycle and sweep vote only them.</color>", voteInfoStyle, GUILayout.Width(innerWidth), GUILayout.Height(16f));
            }
            finally { GUILayout.EndVertical(); }

            int curPlayers = 0;
            if (PlayerControl.AllPlayerControls != null)
            {
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    if (pc != null && pc.Data != null && pc.PlayerId < 100 && pc != PlayerControl.LocalPlayer && !NetworkedClones.IsClone(pc))
                        curPlayers++;
                }
            }

            GUILayout.Space(6);
            GUILayout.BeginVertical(menuCardStyle, GUILayout.Width(outerContentWidth));
            DrawMenuSectionHeader($"{L("TARGET VOTE", "Р’Р«Р‘РћР  Р¦Р•Р›Р")} ({curPlayers})");

            if (PlayerControl.AllPlayerControls != null)
            {
                float listH = 15f * 27f + 8f;
                votekickScrollPosition = GUILayout.BeginScrollView(votekickScrollPosition, false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUIStyle.none, GUILayout.Height(listH));
                try
                {
                    foreach (var pc in PlayerControl.AllPlayerControls)
                    {
                        if (NetworkedClones.IsClone(pc)) continue;
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

                            bool selected = IsVotekickTarget(pc.PlayerId);
                            if (GUILayout.Button(selected ? L("AUTO ON", "АВТО ВКЛ") : L("AUTO", "АВТО"), selected ? activeTabStyle : btnStyle, GUILayout.Width(72), GUILayout.Height(20)))
                                ToggleVotekickTarget(pc.PlayerId);

                            GUILayout.Space(4);

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
