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

public void OnGUI()
        {
            Event e = Event.current;

            HandleMessage.HandleTimer();

            bool isTyping = isEditingName || isEditingLevel || isEditingFriendCode || isEditingLocalFriendCode || isEditingGhostChatColor || isEditingBan;
            bool isBinding = isWaitingForBind || isWaitBindMassMorph || isWaitBindSpawnLobby || isWaitBindDespawnLobby ||
                  isWaitBindCloseMeeting || isWaitBindInstaStart || isWaitBindEndCrew || isWaitBindEndImp ||
                  isWaitBindEndImpDC || isWaitBindEndHnsDC || isWaitBindMagnetCursor || isWaitBindToggleTracers ||
                  isWaitBindToggleNoClip || isWaitBindToggleFreecam || isWaitBindToggleCameraZoom ||
                  isWaitBindKillAll || isWaitBindCallMeeting || isWaitBindTogglePlayerInfo ||
                  isWaitBindToggleSeeRoles || isWaitBindToggleSeeGhosts || isWaitBindToggleFullBright ||
                  isWaitBindKickAll || isWaitBindFixSabotages || isWaitBindSetAllGhost ||
                  isWaitBindSetAllGhostImp || isWaitBindReviveAll;

            if (e != null && e.isKey && e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.Escape)
                {
                    isEditingName = isEditingLevel = isEditingFriendCode = isEditingLocalFriendCode = isEditingGhostChatColor = isEditingBan = false;
                    ResetAllBindWaits();
                    e.Use();
                }
                else if (isBinding && e.keyCode != KeyCode.None)
                {
                    if (isWaitingForBind) { menuToggleKey = e.keyCode; }
                    else if (isWaitBindMassMorph) { bindMassMorph = e.keyCode; }
                    else if (isWaitBindSpawnLobby) { bindSpawnLobby = e.keyCode; }
                    else if (isWaitBindDespawnLobby) { bindDespawnLobby = e.keyCode; }
                    else if (isWaitBindCloseMeeting) { bindCloseMeeting = e.keyCode; }
                    else if (isWaitBindInstaStart) { bindInstaStart = e.keyCode; }
                    else if (isWaitBindEndCrew) { bindEndCrew = e.keyCode; }
                    else if (isWaitBindEndImp) { bindEndImp = e.keyCode; }
                    else if (isWaitBindEndImpDC) { bindEndImpDC = e.keyCode; }
                    else if (isWaitBindEndHnsDC) { bindEndHnsDC = e.keyCode; }
                    else if (isWaitBindMagnetCursor) { bindMagnetCursor = e.keyCode; }
                    else if (isWaitBindToggleTracers) { bindToggleTracers = e.keyCode; }
                    else if (isWaitBindToggleNoClip) { bindToggleNoClip = e.keyCode; }
                    else if (isWaitBindToggleFreecam) { bindToggleFreecam = e.keyCode; }
                    else if (isWaitBindToggleCameraZoom) { bindToggleCameraZoom = e.keyCode; }
                    else if (isWaitBindKillAll) { bindKillAll = e.keyCode; }
                    else if (isWaitBindCallMeeting) { bindCallMeeting = e.keyCode; }
                    else if (isWaitBindTogglePlayerInfo) { bindTogglePlayerInfo = e.keyCode; }
                    else if (isWaitBindToggleSeeRoles) { bindToggleSeeRoles = e.keyCode; }
                    else if (isWaitBindToggleSeeGhosts) { bindToggleSeeGhosts = e.keyCode; }
                    else if (isWaitBindToggleFullBright) { bindToggleFullBright = e.keyCode; }
                    else if (isWaitBindKickAll) { bindKickAll = e.keyCode; }
                    else if (isWaitBindFixSabotages) { bindFixSabotages = e.keyCode; }
                    else if (isWaitBindSetAllGhost) { bindSetAllGhost = e.keyCode; }
                    else if (isWaitBindSetAllGhostImp) { bindSetAllGhostImp = e.keyCode; }
                    else if (isWaitBindReviveAll) { bindReviveAll = e.keyCode; }

                    ResetAllBindWaits();
                    SaveConfig();
                    e.Use();
                }
                else if (isTyping)
                {
                    if (isEditingBan && HandleClipboardShortcut(e, ref banInput)) { }
                    else if (isEditingName && HandleClipboardShortcut(e, ref customNameInput)) { }
                    else if (isEditingLevel && HandleClipboardShortcut(e, ref spoofLevelString)) { }
                    else if (isEditingFriendCode && HandleClipboardShortcut(e, ref spoofFriendCodeInput)) { }
                    else if (isEditingLocalFriendCode && HandleClipboardShortcut(e, ref localFriendCodeInput)) { }
                    else if (isEditingGhostChatColor && HandleClipboardShortcut(e, ref ghostChatColorHex, 7)) { ghostChatColorHex = FilterHexInput(ghostChatColorHex, 7); }
                    else if (e.keyCode == KeyCode.Backspace)
                    {
                        if (isEditingBan && banInput.Length > 0) { banInput = banInput.Substring(0, banInput.Length - 1); }
                        if (isEditingName && customNameInput.Length > 0) { customNameInput = customNameInput.Substring(0, customNameInput.Length - 1); }
                        if (isEditingLevel && spoofLevelString.Length > 0) { spoofLevelString = spoofLevelString.Substring(0, spoofLevelString.Length - 1); }
                        if (isEditingFriendCode && spoofFriendCodeInput.Length > 0) { spoofFriendCodeInput = spoofFriendCodeInput.Substring(0, spoofFriendCodeInput.Length - 1); }
                        if (isEditingLocalFriendCode && localFriendCodeInput.Length > 0) { localFriendCodeInput = localFriendCodeInput.Substring(0, localFriendCodeInput.Length - 1); }
                        if (isEditingGhostChatColor && ghostChatColorHex.Length > 0) { ghostChatColorHex = ghostChatColorHex.Substring(0, ghostChatColorHex.Length - 1); }
                        e.Use();
                    }
                    else if (e.character != 0 && e.character != '\n' && e.character != '\r')
                    {
                        if (isEditingBan) { banInput += e.character; }
                        if (isEditingName) { customNameInput += e.character; }
                        if (isEditingLevel) { spoofLevelString += e.character; }
                        if (isEditingFriendCode) { spoofFriendCodeInput += e.character; }
                        if (isEditingLocalFriendCode) { localFriendCodeInput += e.character; }
                        if (isEditingGhostChatColor) { ghostChatColorHex = FilterHexInput((ghostChatColorHex ?? "") + e.character, 7); }
                        e.Use();
                    }
                }
            }

            if (Event.current.type == EventType.Layout)
            {
                lockedPlayersList.Clear();
                if (PlayerControl.AllPlayerControls != null)
                {
                    foreach (var p in PlayerControl.AllPlayerControls)
                    {
                        if (p != null && p.Data != null && !p.Data.Disconnected && p.PlayerId < 100)
                            lockedPlayersList.Add(p);
                    }
                }

                if (!stylesInited) InitStyles();

                if (showMenu)
                {
                    FontStyle oldLabelFont = GUI.skin.label.fontStyle;
                    FontStyle oldBoxFont = GUI.skin.box.fontStyle;
                    FontStyle oldButtonFont = GUI.skin.button.fontStyle;
                    FontStyle oldToggleFont = GUI.skin.toggle.fontStyle;
                    Color oldContentColor = GUI.contentColor;

                    try
                    {
                        FontStyle menuFontStyle = boldMenuText ? FontStyle.Bold : FontStyle.Normal;
                        GUI.skin.label.fontStyle = menuFontStyle;
                        GUI.skin.box.fontStyle = menuFontStyle;
                        GUI.skin.button.fontStyle = menuFontStyle;
                        GUI.skin.toggle.fontStyle = menuFontStyle;
                        if (RgbMenuTextActive())
                            GUI.contentColor = GetMenuAccentColor();

                        windowRect = GUI.Window(0, windowRect, (Action<int>)DrawElysiumModMenu, "", windowStyle);
                    }
                    finally
                    {
                        GUI.skin.label.fontStyle = oldLabelFont;
                        GUI.skin.box.fontStyle = oldBoxFont;
                        GUI.skin.button.fontStyle = oldButtonFont;
                        GUI.skin.toggle.fontStyle = oldToggleFont;
                        GUI.contentColor = oldContentColor;
                    }
                }

                for (int i = screenNotifications.Count - 1; i >= 0; i--)
                {
                    screenNotifications[i].lifetime += Time.deltaTime;
                    if (screenNotifications[i].HasExpired) screenNotifications.RemoveAt(i);
                }
            }

            try
            {
                if (AmongUsClient.Instance != null && (AmongUsClient.Instance.GameState == InnerNetClient.GameStates.Joined || AmongUsClient.Instance.GameState == InnerNetClient.GameStates.Started))
                {
                    if (PlayerControl.AllPlayerControls != null)
                    {
                        List<byte> currentIds = new List<byte>();
                        foreach (var pc in PlayerControl.AllPlayerControls)
                        {
                            if (pc != null && pc.Data != null && !pc.Data.Disconnected)
                            {
                                currentIds.Add(pc.PlayerId);
                                UpsertPlayerHistory(pc);
                            }
                        }

                        foreach (var id in currentIds)
                        {
                            if (!lastPlayerIds.Contains(id) && !pendingJoinTimers.ContainsKey(id))
                            {
                                pendingJoinTimers[id] = 1.5f;
                            }
                        }

                        var keysToProcess = pendingJoinTimers.Keys.ToList();
                        foreach (var id in keysToProcess)
                        {
                            pendingJoinTimers[id] -= Time.deltaTime;
                            if (pendingJoinTimers[id] <= 0f)
                            {
                                pendingJoinTimers.Remove(id);

                                var pc = PlayerControl.AllPlayerControls.ToArray().FirstOrDefault(p => p != null && p.PlayerId == id);
                                if (pc != null && pc.Data != null && !pc.Data.Disconnected)
                                {
                                    if (DetailedJoinInfo)
                                    {
                                        int level = 1;
                                        try
                                        {
                                            uint rawLevel = pc.Data.PlayerLevel;
                                            if (rawLevel != uint.MaxValue && rawLevel < 10000) level = (int)rawLevel + 1;
                                        }
                                        catch { }

                                        string platform = GetPlatform(AmongUsClient.Instance.GetClientFromCharacter(pc));
                                        string fc = GetDisplayedFriendCode(pc.Data);

                                        ShowNotification($"<color=#00FF00>[+]</color> {pc.Data.PlayerName} joined\n<color=#aaaaaa>Lvl: {level} | {platform} | FC: {fc}</color>");
                                    }
                                    else
                                    {
                                        ShowNotification($"<color=#00FF00>[+]</color> {pc.Data.PlayerName} присоединился");
                                    }
                                }
                            }
                        }

                        foreach (var id in lastPlayerIds)
                        {
                            if (!currentIds.Contains(id))
                            {
                                pendingJoinTimers.Remove(id);
                                MarkPlayerHistoryLeft(id);
                            }
                        }

                        lastPlayerIds = new List<byte>(currentIds);
                    }
                }
                else
                {
                    foreach (var id in lastPlayerIds)
                        MarkPlayerHistoryLeft(id);
                    lastPlayerIds.Clear();
                    pendingJoinTimers.Clear();
                }
            }
            catch { }
            if (screenNotifications.Count > 0)
            {
                int maxNotifs = 6;
                int startIdx = Mathf.Max(0, screenNotifications.Count - maxNotifs);
                for (int i = startIdx; i < screenNotifications.Count; i++)
                {
                    ElysiumNotification notif = screenNotifications[i];
                    int reverseIndex = screenNotifications.Count - 1 - i;

                    float slideOffset = 0f;
                    float animSpeed = 0.3f;
                    float currentAlpha = 0.95f;

                    if (notif.lifetime < animSpeed)
                    {
                        float t = Mathf.Clamp01(1f - (notif.lifetime / animSpeed));
                        slideOffset = t * t * 300f;
                    }
                    else if (notif.lifetime > notif.ttl - animSpeed)
                    {
                        float t = Mathf.Clamp01((notif.lifetime - (notif.ttl - animSpeed)) / animSpeed);
                        slideOffset = t * t * 300f;
                        currentAlpha = Mathf.Lerp(0.95f, 0f, t);
                    }

                    float xPos = (float)Screen.width - notificationBoxSize.x - 15f + slideOffset;
                    float yPos = Screen.height - 150f - (reverseIndex * (notificationBoxSize.y + 5f));

                    GUI.color = whiteMenuTheme
                        ? new Color(1f, 1f, 1f, currentAlpha)
                        : new Color(0.12f, 0.12f, 0.12f, currentAlpha);
                    GUI.Box(new Rect(xPos, yPos, notificationBoxSize.x, notificationBoxSize.y), "", windowStyle);

                    GUI.color = new Color(1f, 1f, 1f, currentAlpha > 0.5f ? 1f : currentAlpha * 2f);
                    string notificationTextHex = whiteMenuTheme ? "202020" : GetMenuAccentHex(false);
                    string notificationMessage = GetNotificationTextForTheme(notif.message);
                    GUIStyle notifTitleStyle = new GUIStyle(GUI.skin.label)
                    {
                        richText = true,
                        fontSize = 12,
                        clipping = TextClipping.Clip
                    };
                    notifTitleStyle.normal.textColor = whiteMenuTheme ? new Color(0.02f, 0.02f, 0.02f, 1f) : Color.white;
                    GUIStyle notifTimerStyle = new GUIStyle(notifTitleStyle)
                    {
                        alignment = TextAnchor.UpperRight
                    };
                    GUIStyle notifMessageStyle = new GUIStyle(GUI.skin.label)
                    {
                        richText = true,
                        wordWrap = true,
                        fontSize = 12,
                        clipping = TextClipping.Clip
                    };
                    notifMessageStyle.normal.textColor = whiteMenuTheme ? new Color(0.02f, 0.02f, 0.02f, 1f) : new Color(0.9f, 0.9f, 0.9f, 1f);

                    GUI.Label(new Rect(xPos + 10f, yPos + 5f, notificationBoxSize.x - 20f, 20f), $"<b><color=#{notificationTextHex}>{notif.title}</color></b>", notifTitleStyle);

                    float timeLeft = Mathf.Max(0, notif.ttl - notif.lifetime);
                    GUI.Label(new Rect(xPos + 10f, yPos + 5f, notificationBoxSize.x - 20f, 20f), $"<b><color=#{notificationTextHex}>{timeLeft:F1}s</color></b>", notifTimerStyle);
                    GUI.Label(new Rect(xPos + 10f, yPos + 25f, notificationBoxSize.x - 20f, notificationBoxSize.y - 30f), notificationMessage, notifMessageStyle);

                    float progress = 1f - (notif.lifetime / notif.ttl);
                    Color progressColor = GetMenuAccentColor(false);
                    GUI.color = new Color(progressColor.r, progressColor.g, progressColor.b, currentAlpha);
                    GUI.Box(new Rect(xPos + 8f, yPos + notificationBoxSize.y - 6f, (notificationBoxSize.x - 16f) * progress, 2f), "", safeLineStyle);
                    GUI.color = Color.white;
                }
            }
        }

public static bool votekickEveryone = false;

public static List<int> votekickedPlayerIds = new List<int>();

private static bool votekickExitQueued = false;

private static float votekickExitAt = 0f;

private const float VotekickExitDelay = 0.45f;

private Vector2 votekickScrollPosition = Vector2.zero;

private void StartVotekickEveryoneRun()
        {
            votekickEveryone = true;
            votekickedPlayerIds.Clear();
            votekickExitQueued = false;
            votekickExitAt = 0f;
            ShowNotification("<color=#ca08ff>[AUTO-VOTEKICK]</color> <b>Armed.</b> Join a room and votes will be sent automatically.");
        }

private void StopVotekickEveryoneRun(bool clearVotes = true)
        {
            votekickEveryone = false;
            votekickExitQueued = false;
            votekickExitAt = 0f;
            if (clearVotes) votekickedPlayerIds.Clear();
        }

        private void TickVotekickEveryoneRun()
        {
            if (!votekickEveryone) return;

            if (votekickExitQueued)
            {
                if (Time.unscaledTime >= votekickExitAt)
                    LeaveRoomAfterVotekick();
                return;
            }

            if (VoteBanSystem.Instance == null || PlayerControl.AllPlayerControls == null || AmongUsClient.Instance == null)
                return;

            int sent = ExecuteVotekickEveryone(true);
            if (sent <= 0) return;

            votekickExitQueued = true;
            votekickExitAt = Time.unscaledTime + VotekickExitDelay;
            ShowNotification($"<color=#ca08ff>[AUTO-VOTEKICK]</color> Votes sent: <b>{sent}</b>. <b>Leaving room...</b>");
        }

        private int ExecuteVotekickEveryone(bool rememberTargets)
        {
            if (VoteBanSystem.Instance == null || PlayerControl.AllPlayerControls == null) return 0;

            int votesSent = 0;
            try
            {
                foreach (PlayerControl pc in PlayerControl.AllPlayerControls)
                {
                    if (pc == null || pc.AmOwner || pc.Data == null || pc.Data.Disconnected) continue;

                    int clientId = pc.Data.ClientId;

                    if (!rememberTargets || !votekickedPlayerIds.Contains(clientId))
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            VoteBanSystem.Instance.CmdAddVote(clientId);
                            votesSent++;
                        }

                        if (rememberTargets)
                            votekickedPlayerIds.Add(clientId);

                        ShowNotification($"<color=#ca08ff>[VOTEKICK]</color> <b>3 votes</b> sent to <b>{pc.Data.PlayerName}</b>.");
                    }
                }
            }
            catch (Exception)
            {
            }

            return votesSent;
        }

        private void SendVotekickEveryoneStay()
        {
            int sent = ExecuteVotekickEveryone(false);
            if (sent > 0)
                ShowNotification($"<color=#ca08ff>[VOTEKICK]</color> Sent <b>{sent}</b> votes. <b>Staying in room.</b>");
            else
                ShowNotification("<color=#FF4444>[VOTEKICK]</color> No valid targets or VoteBanSystem is not ready.");
        }

        private void LeaveRoomAfterVotekick()
        {
            try
            {
                votekickExitQueued = false;
                votekickExitAt = 0f;
                votekickedPlayerIds.Clear();

                if (AmongUsClient.Instance != null)
                {
                    AmongUsClient.Instance.ExitGame(DisconnectReasons.ExitGame);
                    ShowNotification("<color=#ca08ff>[AUTO-VOTEKICK]</color> <b>Left room.</b> Auto mode is still armed.");
                }
            }
            catch (Exception)
            {
                votekickExitQueued = false;
                votekickExitAt = 0f;
            }
        }

        public static void ExecuteVotekickTarget(PlayerControl target)
        {
            if (target == null || target.Data == null || VoteBanSystem.Instance == null) return;

            try
            {
                int targetClientId = target.Data.ClientId;

                VoteBanSystem.Instance.CmdAddVote(targetClientId);


                if (DestroyableSingleton<HudManager>.Instance != null && DestroyableSingleton<HudManager>.Instance.Notifier != null)
                {
                    DestroyableSingleton<HudManager>.Instance.Notifier.AddDisconnectMessage("Votekick sent! Leave and rejoin 2 more times.");
                }

                ShowNotification($"<color=#ca08ff>[VOTEKICK]</color> Vote sent to <b>{target.Data.PlayerName}</b>!");
            }
            catch (Exception)
            {
            }
        }

        private void DrawVotekickTab()
        {
            GUILayout.BeginVertical(menuCardStyle);
            try
            {
                GUIStyle voteInfoStyle = new GUIStyle(toggleLabelStyle) { richText = true, wordWrap = true };
                DrawMenuSectionHeader("VOTEKICK MENU");
                GUILayout.Label("<color=#777777><b>Auto mode:</b> sends 3 votes to every valid player, leaves the room, and stays armed until you press it again.</color>", voteInfoStyle);
                GUILayout.Space(5);

                string autoButtonText = votekickEveryone ? "STOP AUTO VOTEKICK + LEAVE" : "AUTO VOTEKICK + LEAVE";
                if (GUILayout.Button(autoButtonText, votekickEveryone ? activeTabStyle : btnStyle, GUILayout.Height(35)))
                {
                    if (votekickEveryone) StopVotekickEveryoneRun();
                    else StartVotekickEveryoneRun();
                }

                GUILayout.Space(5);
                GUILayout.Label("<color=#777777><b>Manual mode:</b> sends 3 votes now and stays in the current room.</color>", voteInfoStyle);
                if (GUILayout.Button("SEND 3 VOTES + STAY", btnStyle, GUILayout.Height(32)))
                {
                    SendVotekickEveryoneStay();
                }
            }
            finally { GUILayout.EndVertical(); }

            GUILayout.Space(10);
            DrawMenuSectionHeader("TARGET VOTE");

            if (PlayerControl.AllPlayerControls != null)
            {
                var safePlayersList = new System.Collections.Generic.List<PlayerControl>();
                foreach (var p in PlayerControl.AllPlayerControls) safePlayersList.Add(p);

                votekickScrollPosition = GUILayout.BeginScrollView(votekickScrollPosition);
                try
                {
                    foreach (var pc in safePlayersList)
                    {
                        if (pc == null || pc.Data == null || pc.PlayerId >= 100 || pc == PlayerControl.LocalPlayer) continue;

                        GUILayout.BeginHorizontal(boxStyle);
                        try
                        {
                            string pName = pc.Data.PlayerName ?? "Unknown";
                            bool isHost = (AmongUsClient.Instance != null && AmongUsClient.Instance.GetHost()?.Character == pc);
                            string displayStr = isHost ? pName + " <color=#FF0000>[H]</color>" : pName;

                            GUILayout.Label(displayStr, GUILayout.Width(110));

                            GUILayout.FlexibleSpace();

                            if (GUILayout.Button("Vote", btnStyle, GUILayout.Width(60), GUILayout.Height(25)))
                            {
                                ExecuteVotekickTarget(pc);
                            }
                        }
                        finally
                        {
                            GUILayout.EndHorizontal();
                        }
                        GUILayout.Space(2);
                    }
                }
                finally
                {
                    GUILayout.EndScrollView();
                }
            }
        }

        private void DrawElysiumModMenu(int windowID)
        {
            if (Event.current.type == EventType.Repaint && tabTransitionProgress < 1f)
            {
                tabTransitionProgress += Time.unscaledDeltaTime * 8f;
                if (tabTransitionProgress >= 1f) { tabTransitionProgress = 1f; currentTab = targetTabIndex; }
            }

            if (enableBackground && customMenuBg != null)
            {
                GUI.color = new Color(0.6f, 0.6f, 0.6f, 0.8f);
                GUIStyle bgStyle = new GUIStyle() { normal = { background = customMenuBg } };
                GUI.Box(new Rect(0, 0, windowRect.width, windowRect.height), GUIContent.none, bgStyle);
                GUI.color = Color.white;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label(ApplyMenuShimmer("ElysiumModMenu Meowchelo & Carrot"), titleStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("-", new GUIStyle(btnStyle) { fixedWidth = 20, fixedHeight = 18, margin = CreateRectOffset(0, 8, 6, 0) })) showMenu = false;
            GUILayout.EndHorizontal();

            GUI.color = new Color(1f, 1f, 1f, 0.1f);
            GUI.Box(new Rect(0, 30, windowRect.width, 1), "", safeLineStyle);
            GUI.color = Color.white;

            GUILayout.BeginArea(new Rect(0f, 31f, 130f, windowRect.height - 31f));
            GUILayout.BeginVertical(sidebarStyle, GUILayout.ExpandHeight(true));
            GUILayout.Space(5);
            for (int i = 0; i < tabNames.Length; i++)
                if (GUILayout.Button(tabNames[i], i == targetTabIndex ? activeSidebarBtnStyle : sidebarBtnStyle, GUILayout.Height(24)))
                    if (targetTabIndex != i) { targetTabIndex = i; tabTransitionProgress = 0f; scrollPosition = Vector2.zero; }
            GUILayout.EndVertical();
            GUILayout.EndArea();

            GUI.color = new Color(1f, 1f, 1f, 0.1f);
            GUI.Box(new Rect(130, 31, 1, windowRect.height), "", safeLineStyle);
            GUI.color = new Color(1f, 1f, 1f, tabTransitionProgress);

            GUILayout.BeginArea(new Rect(140f, 36f + ((1f - tabTransitionProgress) * 10f), windowRect.width - 150f, windowRect.height - 46f));
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, false, GUIStyle.none, GUI.skin.verticalScrollbar);
            int tabToDraw = (tabTransitionProgress < 1f) ? targetTabIndex : currentTab;

            if (tabToDraw == 0) DrawGeneralTab();
            else if (tabToDraw == 1) DrawSelfTab();
            else if (tabToDraw == 2) DrawVisualsTab();
            else if (tabToDraw == 3) DrawPlayersTab();
            else if (tabToDraw == 4) DrawSabotagesTab();
            else if (tabToDraw == 5) DrawHostOnlyTab();
            else if (tabToDraw == 6) DrawVotekickTab();
            else if (tabToDraw == 7) DrawMenuTab();
            else if (tabToDraw == 8) DrawAnimationsTab();

            GUILayout.EndScrollView();
            GUILayout.EndArea();

            GUI.color = Color.white;
            GUI.DragWindow(new Rect(0, 0, 10000, 30));
        }
        public static int punishmentMode = 1;

public static bool settingsDirty = false;

public static string[] punishmentNames = { "Null", "Warn", "Kick", "Ban" };

public static bool blockSpoofRPC = true;

public static bool blockSabotageRPC = true;

public static bool blockGameRpcInLobby = true;

public static bool blockChatFloodRpc = true;

public static bool blockMeetingFloodRpc = true;

public static bool enablePasosLimit = true;

public static bool enableLocalPasosBan = true;

public static bool enableHostPasosBan = true;

public static bool autoBanBrokenFriendCode = false;

public static int chatRpcLimit = 1;

public static float chatRpcWindow = 1f;

public static int meetingRpcLimit = 2;

public static float meetingRpcWindow = 9999f;
}
}
