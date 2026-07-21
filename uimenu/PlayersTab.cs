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
private void DrawPlayersTab()
        {
            if (lastPlayersSubTabForScroll != currentPlayersSubTab)
            {
                ResetPlayersTabScrolls();
                lastPlayersSubTabForScroll = currentPlayersSubTab;
            }

            GUILayout.BeginHorizontal();
            for (int i = 0; i < playersSubTabs.Length; i++)
            {
                if (GUILayout.Button(playersSubTabs[i], currentPlayersSubTab == i ? activeSubTabStyle : subTabStyle, GUILayout.Height(18)))
                {
                    if (SetMultiTab("players", ref currentPlayersSubTab, i, playersSubTabs.Length))
                    {
                        ResetPlayersTabScrolls();
                        lastPlayersSubTabForScroll = currentPlayersSubTab;
                    }
                    else
                    {
                        scrollPosition = Vector2.zero;
                    }
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(8);

            BeginMultiTabContent("players", out Matrix4x4 oldMatrix, out Color oldColor);
            try
            {
                if (currentPlayersSubTab == 1)
                {
                    DrawPlayersHistoryTab();
                    return;
                }

                if (currentPlayersSubTab == 2)
                {
                    DrawPlayersClonesTab();
                    return;
                }

            float playersTabWidth = GetMenuWorkWidth(220f, 760f);
            bool stackPlayers = playersTabWidth < 430f;
            float playerListWidth = stackPlayers ? playersTabWidth : (playersTabWidth < 520f ? 138f : Mathf.Clamp(windowRect.width * 0.26f, 165f, 210f));
            float playerActionGapMain = playersTabWidth < 520f ? 6f : 8f;
            float playerActionPanelWidth = stackPlayers ? playersTabWidth : Mathf.Max(260f, playersTabWidth - playerListWidth - playerActionGapMain - 18f);

            if (stackPlayers) GUILayout.BeginVertical(GUILayout.Width(playersTabWidth));
            else GUILayout.BeginHorizontal(GUILayout.Width(playersTabWidth));
            try
            {

                GUILayout.BeginVertical(menuCardStyle, GUILayout.Width(playerListWidth), stackPlayers ? GUILayout.Height(112f) : GUILayout.ExpandHeight(true));
                try
                {
                    playerListScrollPos = GUILayout.BeginScrollView(playerListScrollPos, false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUIStyle.none);
                    try
                    {
                        if (lockedPlayersList.Count > 0)
                        {
                            foreach (var pc in lockedPlayersList)
                            {
                                if (pc == null || pc.Data == null || pc.PlayerId >= 100) continue;
                                string pName = pc.Data.PlayerName ?? "Unknown";

                                if (TryGetForcedRole(pc, out _)) pName += " [*]";
                                else if (IsForcedImp(pc)) pName += " [Imp]";

                                bool isSelected = selectedAntiCheatPlayerId == pc.PlayerId;

                                GUI.contentColor = Color.white;
                                try { GUI.contentColor = Palette.PlayerColors[pc.Data.DefaultOutfit.ColorId]; } catch { }

                                if (GUILayout.Button(pName, isSelected ? activeTabStyle : btnStyle, GUILayout.Height(30)))
                                {
                                    selectedAntiCheatPlayerId = pc.PlayerId;
                                }
                                GUI.contentColor = Color.white;
                            }
                        }
                    }
                    finally { GUILayout.EndScrollView(); }
                }
                finally { GUILayout.EndVertical(); }

                GUILayout.Space(playerActionGapMain); GUILayout.BeginVertical(menuCardStyle, GUILayout.Width(playerActionPanelWidth), GUILayout.ExpandHeight(true));
                try
                {
                    playerActionScrollPos = GUILayout.BeginScrollView(playerActionScrollPos, false, false, GUIStyle.none, GUIStyle.none, GUIStyle.none, GUILayout.Width(playerActionPanelWidth - 8f));
                    try
                    {

            PlayerControl target = null;
            try { target = lockedPlayersList.FirstOrDefault(p => p != null && p.PlayerId == selectedAntiCheatPlayerId); }
            catch { }

            if (target != null && target.Data != null)
            {
                float playerActionContentWidth = Mathf.Max(150f, playerActionPanelWidth - 30f);
                float playerActionGap = 6f;
                float playerActionThirdWidth = Mathf.Floor((playerActionContentWidth - (playerActionGap * 2f)) / 3f);
                float playerActionHalfWidth = Mathf.Floor((playerActionContentWidth - playerActionGap) / 2f);
                float playerActionButtonHeight = 23f;

                GUILayout.Label($"<color=#aaaaaa>Selected:</color> {target.Data.PlayerName}", richLabelStyle12);
                GUILayout.Space(5);
                GUILayout.BeginHorizontal();

                GUI.backgroundColor = new Color(0.8f, 0.2f, 0.2f, 1f);
                if (DrawFixedMenuButton("KILL", btnStyle, playerActionThirdWidth, playerActionButtonHeight))
                {
                    PlayerControl local = PlayerControl.LocalPlayer;
                    if (local != null && local.NetTransform != null)
                    {
                        if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
                            TryHostElysiumMurderPlayer(target);
                        else
                        {
                            Vector3 op = local.transform.position;
                            local.NetTransform.RpcSnapTo(target.transform.position);
                            local.CmdCheckMurder(target);
                            local.NetTransform.RpcSnapTo(op);
                        }
                    }
                }
                GUI.backgroundColor = Color.white;

                GUILayout.Space(playerActionGap);

                if (DrawFixedMenuButton("TP TO", activeTabStyle, playerActionThirdWidth, playerActionButtonHeight))
                {
                    teleportToPlayer(target);
                    ShowNotification($"<color=#00FF00>[TELEPORT]</color> Teleported to <b>{target.Data.PlayerName}</b>!");
                }

                GUILayout.Space(playerActionGap);

                GUI.backgroundColor = new Color(1f, 0.5f, 0f, 1f);
                if (DrawFixedMenuButton("Force Eject", btnStyle, playerActionThirdWidth, playerActionButtonHeight)) ForceGlobalEject(target);
                GUI.backgroundColor = Color.white;

                GUILayout.EndHorizontal();

                GUILayout.Space(5);

                GUILayout.BeginHorizontal();

                GUI.backgroundColor = new Color(0.75f, 0.15f, 0.15f, 1f);
                if (DrawFixedMenuButton("TELEKILL", btnStyle, playerActionHalfWidth, playerActionButtonHeight))
                {
                    if (TelekillPlayerPPM(target))
                        ShowNotification($"<color=#FF5555>[TELEKILL]</color> {target.Data.PlayerName}");
                    else
                        ShowNotification("<color=#FF0000>[TELEKILL]</color> Failed");
                }
                GUI.backgroundColor = Color.white;

                GUILayout.Space(playerActionGap);
                if (DrawFixedMenuButton("FOLLOW", followPlayer && followPlayerId == target.PlayerId ? activeTabStyle : btnStyle, playerActionHalfWidth, playerActionButtonHeight))
                {
                    if (followPlayer && followPlayerId == target.PlayerId)
                    {
                        followPlayer = false;
                        followPlayerId = byte.MaxValue;
                    }
                    else
                    {
                        followPlayer = true;
                        followPlayerId = target.PlayerId;
                    }
                }

                GUILayout.EndHorizontal();

                GUILayout.Space(5);
                if (DrawFixedMenuButton("Force Meeting", btnStyle, playerActionContentWidth, playerActionButtonHeight)) ForceMeetingAsPlayer(target);

                GUILayout.Space(5);
                GUILayout.BeginHorizontal();

                if (DrawFixedMenuButton("Force Votes", btnStyle, playerActionHalfWidth, playerActionButtonHeight))
                    ForceAllVotesTo(target);

                bool hr = rainbowPlayers.Contains(target.PlayerId);
                GUILayout.Space(playerActionGap);
                if (DrawFixedMenuButton(hr ? "RGB: ON" : "RGB: OFF", hr ? activeTabStyle : btnStyle, playerActionHalfWidth, playerActionButtonHeight))
                {
                    if (!hr) rainbowPlayers.Add(target.PlayerId);
                    else rainbowPlayers.Remove(target.PlayerId);
                }

                GUILayout.EndHorizontal();

                GUILayout.Space(5);
                GUILayout.BeginHorizontal();

                if (DrawFixedMenuButton("Report Body", btnStyle, playerActionHalfWidth, playerActionButtonHeight))
                    AttemptReportBody(target);

                GUILayout.Space(playerActionGap);

                if (DrawFixedMenuButton("Flood Tasks", btnStyle, playerActionHalfWidth, playerActionButtonHeight))
                    FloodPlayerWithTasks(target);

                GUILayout.EndHorizontal();

                GUILayout.Space(5);
                GUILayout.BeginHorizontal();

                if (DrawFixedMenuButton("Change Tasks", btnStyle, playerActionHalfWidth, playerActionButtonHeight))
                    ChangePlayerTasks(target);

                GUILayout.Space(playerActionGap);

                if (DrawFixedMenuButton("Delete Tasks", btnStyle, playerActionHalfWidth, playerActionButtonHeight))
                    DeletePlayerTasks(target);

                GUILayout.EndHorizontal();

                GUILayout.Space(7);
                DrawMenuSectionHeader("TARGET ROLE CONTROL");

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("<", btnStyle, GUILayout.Width(28), GUILayout.Height(22)))
                {
                    targetRoleAssignIdx--;
                    if (targetRoleAssignIdx < 0) targetRoleAssignIdx = roleAssignOptions.Length - 1;
                }
                GUILayout.Label(roleAssignNames[targetRoleAssignIdx], accentValueStyle, GUILayout.Height(22), GUILayout.ExpandWidth(true));
                if (GUILayout.Button(">", btnStyle, GUILayout.Width(28), GUILayout.Height(22)))
                {
                    targetRoleAssignIdx++;
                    if (targetRoleAssignIdx >= roleAssignOptions.Length) targetRoleAssignIdx = 0;
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(4);
                GUILayout.BeginHorizontal();
                if (DrawFixedMenuButton("TARGET -> ROLE", btnStyle, playerActionHalfWidth, 24f))
                {
                    if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
                    {
                        ShowNotification("<color=#FF0000>[ERROR]</color> Host permissions required!");
                    }
                    else
                    {
                        if (IsGhostRoleSelection(targetRoleAssignIdx))
                        {
                            MakePlayerGhost(target);
                        }
                        else if (IsGhostImpostorRoleSelection(targetRoleAssignIdx))
                        {
                            MakePlayerGhost(target, true);
                        }
                        else
                        {
                            SetPlayerRole(target, roleAssignOptions[targetRoleAssignIdx]);
                            ShowNotification($"<color=#00FF00>[ROLE]</color> {target.Data.PlayerName} -> {roleAssignNames[targetRoleAssignIdx]}");
                        }
                    }
                }
                GUILayout.Space(playerActionGap);
                if (DrawFixedMenuButton("TARGET -> GHOST", btnStyle, playerActionHalfWidth, 24f))
                {
                    MakePlayerGhost(target);
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(4);
                GUILayout.BeginHorizontal();
                if (DrawFixedMenuButton("REVIVE TARGET", activeTabStyle, playerActionContentWidth, 24f))
                {
                    RevivePlayer(target);
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(8);
                GUILayout.Label($"<color=#aaaaaa>{L("Morph Target:", "Цель морфа:")}</color>", richLabelStyle11);
                GUILayout.BeginHorizontal();

                int mIdx = lockedPlayersList.FindIndex(p => p.PlayerId == selectedMorphTargetId);

                GUI.backgroundColor = GetMenuControlAccentColor();
                if (GUILayout.Button("<", btnStyle, GUILayout.Width(25), GUILayout.Height(25)))
                {
                    if (lockedPlayersList.Count > 0) { mIdx--; if (mIdx < 0) mIdx = lockedPlayersList.Count - 1; selectedMorphTargetId = lockedPlayersList[mIdx].PlayerId; }
                }
                GUI.backgroundColor = Color.white;

                string morphName = "Target";
                if (mIdx >= 0 && mIdx < lockedPlayersList.Count) morphName = lockedPlayersList[mIdx].Data.PlayerName;
                if (morphName.Length > 10) morphName = morphName.Substring(0, 10) + "..";

                GUILayout.Label(morphName, morphValueStyle, GUILayout.Height(25), GUILayout.ExpandWidth(true));

                GUI.backgroundColor = GetMenuControlAccentColor();
                if (GUILayout.Button(">", btnStyle, GUILayout.Width(25), GUILayout.Height(25)))
                {
                    if (lockedPlayersList.Count > 0) { mIdx++; if (mIdx >= lockedPlayersList.Count) mIdx = 0; selectedMorphTargetId = lockedPlayersList[mIdx].PlayerId; }
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(5);
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                GUI.backgroundColor = GetMenuControlAccentColor();
                if (GUILayout.Button(L("MORPH TARGET", "МОРФ В ЦЕЛЬ"), btnStyle, GUILayout.Width(160), GUILayout.Height(25)))
                {
                    var morphTarget = lockedPlayersList.FirstOrDefault(p => p.PlayerId == selectedMorphTargetId) ?? target;
                    this.StartCoroutine(AttemptShapeshiftFrame(target, morphTarget).WrapToIl2Cpp());
                }
                GUI.backgroundColor = Color.white;

                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.Space(15);
                DrawMenuSectionHeader("SET PLAYER COLOR");
                GUILayout.BeginVertical();

                int colorsPerRow = Mathf.Clamp(Mathf.FloorToInt(playerActionContentWidth / 36f), 4, 7);
                for (int i = 0; i < Palette.PlayerColors.Length; i++)
                {
                    if (i % colorsPerRow == 0) GUILayout.BeginHorizontal();

                    GUI.color = Palette.PlayerColors[i];

                    if (GUILayout.Button("", roundedColorButtonStyle, GUILayout.Width(32), GUILayout.Height(30)))
                        target.RpcSetColor((byte)i);

                    if (i % colorsPerRow == colorsPerRow - 1 || i == Palette.PlayerColors.Length - 1)
                        GUILayout.EndHorizontal();
                }
                GUI.color = Color.white;
                GUILayout.EndVertical();

                GUILayout.Space(15);
                DrawMenuSectionHeader("PLAYER INFO & REPORT");

                GUILayout.BeginHorizontal();
                if (GUILayout.Button(L("COPY PUID", "КОПИРОВАТЬ PUID"), btnStyle, GUILayout.Height(25)))
                {
                    string puid = GetPlayerPuid(target);
                    if (!string.IsNullOrWhiteSpace(puid) && puid != "Unknown")
                    {
                        GUIUtility.systemCopyBuffer = puid;
                        ShowNotification("<color=#00FF00>[COPY]</color> PUID copied.");
                    }
                    else ShowNotification("<color=#FF0000>[COPY]</color> PUID is unavailable.");
                }

                if (GUILayout.Button(L("COPY FRIEND CODE", "КОПИРОВАТЬ FRIEND CODE"), btnStyle, GUILayout.Height(25)))
                {
                    string friendCode = GetDisplayedFriendCode(target.Data, string.Empty);
                    if (!string.IsNullOrWhiteSpace(friendCode))
                    {
                        GUIUtility.systemCopyBuffer = friendCode;
                        ShowNotification("<color=#00FF00>[COPY]</color> Friend Code copied.");
                    }
                    else ShowNotification("<color=#FF0000>[COPY]</color> Friend Code is unavailable.");
                }
                GUILayout.EndHorizontal();

                if (GUILayout.Button(L("ADD TO BAN LIST", "ДОБАВИТЬ В БАН-ЛИСТ"), btnStyle, GUILayout.Height(25)))
                    AddSelectedPlayerToBanList(target);

                if (GUILayout.Button(L("ADD TO FRIENDS", "ДОБАВИТЬ В ДРУЗЬЯ"), btnStyle, GUILayout.Height(25)))
                    SendFriendInviteToPlayer(target);

                GUILayout.Space(8);
                GUILayout.Label(L("Report reason:", "Причина репорта:"), labelStyle11);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("<", btnStyle, GUILayout.Width(28), GUILayout.Height(24)))
                {
                    selectedPlayerReportReasonIdx--;
                    if (selectedPlayerReportReasonIdx < 0) selectedPlayerReportReasonIdx = selectedPlayerReportReasons.Length - 1;
                }
                GUILayout.Label(selectedPlayerReportReasonNames[selectedPlayerReportReasonIdx], accentValueStyle, GUILayout.Height(24), GUILayout.ExpandWidth(true));
                if (GUILayout.Button(">", btnStyle, GUILayout.Width(28), GUILayout.Height(24)))
                {
                    selectedPlayerReportReasonIdx++;
                    if (selectedPlayerReportReasonIdx >= selectedPlayerReportReasons.Length) selectedPlayerReportReasonIdx = 0;
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(5);
                GUI.backgroundColor = new Color(0.8f, 0.25f, 0.2f, 1f);
                if (GUILayout.Button(L("REPORT PLAYER", "ЗАРЕПОРТИТЬ ИГРОКА"), btnStyle, GUILayout.Height(27)))
                {
                    try
                    {
                        ClientData client = AmongUsClient.Instance?.GetClientFromCharacter(target);
                        if (client == null)
                        {
                            ShowNotification("<color=#FF0000>[REPORT]</color> Player client was not found.");
                        }
                        else
                        {
                            AmongUsClient.Instance.ReportPlayer(client.Id, selectedPlayerReportReasons[selectedPlayerReportReasonIdx]);
                            ShowNotification($"<color=#00FF00>[REPORT]</color> {target.Data.PlayerName}: {selectedPlayerReportReasonNames[selectedPlayerReportReasonIdx]}");
                        }
                    }
                    catch (Exception)
                    {
                        ShowNotification("<color=#FF0000>[REPORT]</color> Report failed.");
                    }
                }
                GUI.backgroundColor = Color.white;

                GUILayout.Space(12);
                DrawMenuSectionHeader("VENT TELEPORT");
                DrawPlayerVentTpRow(target, playerActionContentWidth, playerActionHalfWidth, playerActionGap);
            }
            else
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label($"<color=#777777>{L("Select a player...", "Выберите игрока...")}</color>", centeredRichLabelStyle);
                GUILayout.FlexibleSpace();
            }

                    }
                    finally { GUILayout.EndScrollView(); }
                }
                finally { GUILayout.EndVertical(); }

            }
            finally
            {
                if (stackPlayers) GUILayout.EndVertical();
                else GUILayout.EndHorizontal();
            }
            }
            finally
            {
                EndMultiTabContent(oldMatrix, oldColor);
            }
        }

private void ResetPlayersTabScrolls()
        {
            scrollPosition = Vector2.zero;
            playerListScrollPos = Vector2.zero;
            playerActionScrollPos = Vector2.zero;
            playersHistoryScroll = Vector2.zero;
            playersClonesScroll = Vector2.zero;
            cloneTargetScroll = Vector2.zero;
        }

    }
}

