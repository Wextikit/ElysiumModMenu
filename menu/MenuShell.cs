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
            if (isPanicked) return;

            Event e = Event.current;

            HandleMessage.HandleTimer();

            bool isTyping = isEditingName || isEditingLevel || isEditingFriendCode || isEditingLocalFriendCode || isEditingDeviceId || isEditingGhostChatColor || isEditingBan || isEditingFpsLimit || isEditingBugRoomTimedAutoRun;
            bool isCustomSpoofRpcEditing = customSpoofRpcInputFocused && selectedSpoofMenuIndex == spoofMenuNames.Length - 1;
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
                    if (isEditingFpsLimit)
                    {
                        ApplyFpsLimitInput();
                    }
                    if (isEditingBugRoomTimedAutoRun)
                    {
                        ApplyBugRoomTimedAutoRunInput();
                    }
                    isEditingName = isEditingLevel = isEditingFriendCode = isEditingLocalFriendCode = isEditingDeviceId = isEditingGhostChatColor = isEditingBan = isEditingBugRoomTimedAutoRun = false;
                    customSpoofRpcInputFocused = false;
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
                else if (isCustomSpoofRpcEditing)
                {
                    if (HandleClipboardShortcut(e, ref customSpoofRpcInput, 3))
                    {
                        customSpoofRpcInput = FilterSpoofRpcInput(customSpoofRpcInput);
                    }
                    else if (e.keyCode == KeyCode.Backspace)
                    {
                        if (!string.IsNullOrEmpty(customSpoofRpcInput))
                            customSpoofRpcInput = customSpoofRpcInput.Substring(0, customSpoofRpcInput.Length - 1);
                        e.Use();
                    }
                    else if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
                    {
                        customSpoofRpcInputFocused = false;
                        SaveConfig();
                        e.Use();
                    }
                    else if (e.character >= '0' && e.character <= '9')
                    {
                        customSpoofRpcInput = FilterSpoofRpcInput((customSpoofRpcInput ?? string.Empty) + e.character);
                        e.Use();
                    }
                }
                else if (isTyping)
                {
                    if (isEditingBan && HandleClipboardShortcut(e, ref banInput)) { }
                    else if (isEditingName && HandleClipboardShortcut(e, ref customNameInput)) { }
                    else if (isEditingLevel && HandleClipboardShortcut(e, ref spoofLevelString)) { }
                    else if (isEditingFriendCode && HandleClipboardShortcut(e, ref spoofFriendCodeInput)) { }
                    else if (isEditingLocalFriendCode && HandleClipboardShortcut(e, ref localFriendCodeInput)) { }
                    else if (isEditingDeviceId && HandleClipboardShortcut(e, ref spoofedDeviceId, 64)) { }
                    else if (isEditingGhostChatColor && HandleClipboardShortcut(e, ref ghostChatColorHex, 10)) { ghostChatColorHex = FilterGhostChatColorInput(ghostChatColorHex); }
                    else if (isEditingFpsLimit && HandleClipboardShortcut(e, ref fpsLimitInput, 3)) { fpsLimitInput = FilterFpsLimitInput(fpsLimitInput); }
                    else if (isEditingBugRoomTimedAutoRun && HandleClipboardShortcut(e, ref bugRoomTimedAutoRunInput, 2)) { bugRoomTimedAutoRunInput = FilterMinuteInput(bugRoomTimedAutoRunInput); }
                    else if (e.keyCode == KeyCode.Backspace)
                    {
                        if (isEditingBan && banInput.Length > 0) { banInput = banInput.Substring(0, banInput.Length - 1); }
                        if (isEditingName && customNameInput.Length > 0) { customNameInput = customNameInput.Substring(0, customNameInput.Length - 1); }
                        if (isEditingLevel && spoofLevelString.Length > 0) { spoofLevelString = spoofLevelString.Substring(0, spoofLevelString.Length - 1); }
                        if (isEditingFriendCode && spoofFriendCodeInput.Length > 0) { spoofFriendCodeInput = spoofFriendCodeInput.Substring(0, spoofFriendCodeInput.Length - 1); }
                        if (isEditingLocalFriendCode && localFriendCodeInput.Length > 0) { localFriendCodeInput = localFriendCodeInput.Substring(0, localFriendCodeInput.Length - 1); }
                        if (isEditingDeviceId && spoofedDeviceId.Length > 0) { spoofedDeviceId = spoofedDeviceId.Substring(0, spoofedDeviceId.Length - 1); }
                        if (isEditingGhostChatColor && ghostChatColorHex.Length > 0) { ghostChatColorHex = ghostChatColorHex.Substring(0, ghostChatColorHex.Length - 1); }
                        if (isEditingFpsLimit && fpsLimitInput.Length > 0) { fpsLimitInput = fpsLimitInput.Substring(0, fpsLimitInput.Length - 1); }
                        if (isEditingBugRoomTimedAutoRun && bugRoomTimedAutoRunInput.Length > 0) { bugRoomTimedAutoRunInput = bugRoomTimedAutoRunInput.Substring(0, bugRoomTimedAutoRunInput.Length - 1); }
                        e.Use();
                    }
                    else if ((e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter) && isEditingFpsLimit)
                    {
                        ApplyFpsLimitInput();
                        e.Use();
                    }
                    else if ((e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter) && isEditingBugRoomTimedAutoRun)
                    {
                        ApplyBugRoomTimedAutoRunInput();
                        e.Use();
                    }
                    else if (e.character != 0 && e.character != '\n' && e.character != '\r')
                    {
                        if (isEditingBan) { banInput += e.character; }
                        if (isEditingName) { customNameInput += e.character; }
                        if (isEditingLevel) { spoofLevelString += e.character; }
                        if (isEditingFriendCode) { spoofFriendCodeInput += e.character; }
                        if (isEditingLocalFriendCode) { localFriendCodeInput += e.character; }
                        if (isEditingDeviceId && spoofedDeviceId.Length < 64) { spoofedDeviceId += e.character; }
                        if (isEditingGhostChatColor) { ghostChatColorHex = FilterGhostChatColorInput((ghostChatColorHex ?? "") + e.character); }
                        if (isEditingFpsLimit && e.character >= '0' && e.character <= '9') { fpsLimitInput = FilterFpsLimitInput((fpsLimitInput ?? "") + e.character); }
                        if (isEditingBugRoomTimedAutoRun && e.character >= '0' && e.character <= '9') { bugRoomTimedAutoRunInput = FilterMinuteInput((bugRoomTimedAutoRunInput ?? "") + e.character); }
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

                for (int i = screenNotifications.Count - 1; i >= 0; i--)
                {
                    screenNotifications[i].lifetime += Time.deltaTime;
                    if (screenNotifications[i].HasExpired) screenNotifications.RemoveAt(i);
                }
            }

            DrawMenuWindowIfVisible();
            TickVisualReplay();
            DrawVisualRadar();
            DrawVisualReplay();
            DrawEspBoxes();

            if (Time.unscaledTime >= nextPlayerHistoryUpdateAt)
            {
                float historyDelta = lastPlayerHistoryUpdateAt <= 0f ? 0.25f : Mathf.Min(1f, Time.unscaledTime - lastPlayerHistoryUpdateAt);
                lastPlayerHistoryUpdateAt = Time.unscaledTime;
                nextPlayerHistoryUpdateAt = Time.unscaledTime + 0.25f;
                try
                {
                    if (AmongUsClient.Instance != null && (AmongUsClient.Instance.GameState == InnerNetClient.GameStates.Joined || AmongUsClient.Instance.GameState == InnerNetClient.GameStates.Started))
                    {
                        if (PlayerControl.AllPlayerControls != null)
                        {
                        List<int> currentClientIds = new List<int>();
                        foreach (var pc in PlayerControl.AllPlayerControls)
                        {
                            if (pc != null && pc.Data != null && !pc.Data.Disconnected)
                            {
                                currentClientIds.Add(pc.Data.ClientId);
                                UpsertPlayerHistory(pc);
                            }
                        }

                        bool isInitialPresenceSnapshot = lastPlayerClientIds.Count == 0 && pendingJoinTimers.Count == 0;
                        foreach (var id in currentClientIds)
                        {
                            if (!lastPlayerClientIds.Contains(id) && !pendingJoinTimers.ContainsKey(id))
                            {
                                if (!isInitialPresenceSnapshot && !IsLocalClientId(id))
                                {
                                    pendingJoinTimers[id] = 1.5f;
                                    pendingJoinWaitTimes[id] = 0f;
                                }
                            }
                        }

                        var keysToProcess = pendingJoinTimers.Keys.ToList();
                        foreach (var id in keysToProcess)
                        {
                            pendingJoinTimers[id] -= historyDelta;
                            pendingJoinWaitTimes[id] = pendingJoinWaitTimes.TryGetValue(id, out float waited) ? waited + historyDelta : historyDelta;
                            if (pendingJoinTimers[id] <= 0f)
                            {
                                var pc = PlayerControl.AllPlayerControls.ToArray().FirstOrDefault(p => p != null && p.Data != null && p.Data.ClientId == id);
                                if (pc == null || pc.Data == null || pc.Data.Disconnected)
                                {
                                    if (pendingJoinWaitTimes[id] < JoinLevelMaxWaitSeconds)
                                    {
                                        pendingJoinTimers[id] = 0.5f;
                                        continue;
                                    }

                                    pendingJoinTimers.Remove(id);
                                    pendingJoinWaitTimes.Remove(id);
                                    continue;
                                }

                                if (pc == PlayerControl.LocalPlayer || pc.AmOwner)
                                {
                                    pendingJoinTimers.Remove(id);
                                    pendingJoinWaitTimes.Remove(id);
                                    continue;
                                }

                                SafePlayerIdentitySnapshot identity;
                                bool hasIdentity = TryGetSafeIdentity(pc, out identity);
                                bool hasLevel = TryGetPlayerDisplayLevel(pc, hasIdentity ? identity : null, out int level);
                                if (DetailedJoinInfo && !hasLevel && pendingJoinWaitTimes[id] < JoinLevelMaxWaitSeconds)
                                {
                                    pendingJoinTimers[id] = 0.5f;
                                    continue;
                                }

                                string safeName = hasIdentity ? identity.Name : $"Player {pc.PlayerId}";
                                if (DetailedJoinInfo)
                                {
                                    string levelText = hasLevel ? level.ToString() : "?";
                                    string platform = hasIdentity ? identity.Platform : "Unknown";
                                    string fc = hasIdentity ? identity.FriendCode : "Hidden";

                                    ShowNotification($"<color=#00FF00>[+]</color> {safeName} joined\n<color=#aaaaaa>Lvl: {levelText} | {platform} | FC: {fc}</color>");
                                }
                                else
                                {
                                    ShowNotification($"<color=#00FF00>[+]</color> {safeName} joined");
                                }

                                pendingJoinTimers.Remove(id);
                                pendingJoinWaitTimes.Remove(id);
                            }
                        }

                        foreach (var id in lastPlayerClientIds)
                        {
                            if (!currentClientIds.Contains(id))
                            {
                                pendingJoinTimers.Remove(id);
                                pendingJoinWaitTimes.Remove(id);
                                MarkPlayerHistoryLeftByClientId(id);
                            }
                        }

                        lastPlayerClientIds = new List<int>(currentClientIds);
                        }
                    }
                    else
                    {
                        foreach (var id in lastPlayerClientIds)
                            MarkPlayerHistoryLeftByClientId(id);
                        lastPlayerClientIds.Clear();
                        pendingJoinTimers.Clear();
                        pendingJoinWaitTimes.Clear();
                    }
                }
                catch { }
            }
            if (screenNotifications.Count > 0)
            {
                if (notificationTitleStyle == null)
                {
                    notificationTitleStyle = new GUIStyle(GUI.skin.label)
                    {
                        richText = true,
                        fontSize = 12,
                        clipping = TextClipping.Clip
                    };
                    notificationTimerStyle = new GUIStyle(notificationTitleStyle)
                    {
                        alignment = TextAnchor.UpperRight
                    };
                    notificationMessageStyle = new GUIStyle(GUI.skin.label)
                    {
                        richText = true,
                        wordWrap = true,
                        fontSize = 12,
                        clipping = TextClipping.Clip
                    };
                }

                Color notificationTextColor = whiteMenuTheme ? new Color(0.02f, 0.02f, 0.02f, 1f) : Color.white;
                notificationTitleStyle.normal.textColor = notificationTextColor;
                notificationTimerStyle.normal.textColor = notificationTextColor;
                notificationMessageStyle.normal.textColor = whiteMenuTheme ? new Color(0.02f, 0.02f, 0.02f, 1f) : new Color(0.9f, 0.9f, 0.9f, 1f);

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
                    GUI.Label(new Rect(xPos + 10f, yPos + 5f, notificationBoxSize.x - 20f, 20f), $"<b><color=#{notificationTextHex}>{notif.title}</color></b>", notificationTitleStyle);

                    float timeLeft = Mathf.Max(0, notif.ttl - notif.lifetime);
                    GUI.Label(new Rect(xPos + 10f, yPos + 5f, notificationBoxSize.x - 20f, 20f), $"<b><color=#{notificationTextHex}>{timeLeft:F1}s</color></b>", notificationTimerStyle);
                    GUI.Label(new Rect(xPos + 10f, yPos + 25f, notificationBoxSize.x - 20f, notificationBoxSize.y - 30f), notificationMessage, notificationMessageStyle);

                    float progress = 1f - (notif.lifetime / notif.ttl);
                    Color progressColor = GetMenuAccentColor(false);
                    GUI.color = new Color(progressColor.r, progressColor.g, progressColor.b, currentAlpha);
                    GUI.Box(new Rect(xPos + 8f, yPos + notificationBoxSize.y - 6f, (notificationBoxSize.x - 16f) * progress, 2f), "", safeLineStyle);
                    GUI.color = Color.white;
                }
            }
        }

private void DrawMenuWindowIfVisible()
        {
            if (!showMenu) return;
            if (!stylesInited) InitStyles();
            ClampMenuWindowToScreen();

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
                GUI.color = Color.white;
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
                GUI.color = Color.white;
            }

            ClampMenuWindowToScreen();
        }

private static void ClampMenuWindowToScreen()
        {
            float screenWidth = Mathf.Max(1f, Screen.width);
            float screenHeight = Mathf.Max(1f, Screen.height);
            float maxWidth = Mathf.Max(320f, screenWidth - 20f);
            float maxHeight = Mathf.Max(260f, screenHeight - 20f);

            windowRect.width = Mathf.Clamp(windowRect.width, Mathf.Min(640f, maxWidth), maxWidth);
            windowRect.height = Mathf.Clamp(windowRect.height, Mathf.Min(420f, maxHeight), maxHeight);
            windowRect.x = Mathf.Clamp(windowRect.x, 0f, Mathf.Max(0f, screenWidth - windowRect.width));
            windowRect.y = Mathf.Clamp(windowRect.y, 0f, Mathf.Max(0f, screenHeight - windowRect.height));
        }

private static float GetMenuSidebarWidth()
        {
            float w = GetMenuVisibleWidth();
            if (w < 220f) return 0f;
            if (w < 340f) return 58f;
            if (w < 430f) return 72f;
            if (w < 560f) return 96f;
            return 130f;
        }

private static float GetMenuVisibleWidth()
        {
            try { return Mathf.Max(80f, Mathf.Min(windowRect.width, Screen.width - windowRect.x)); }
            catch { return windowRect.width; }
        }

private static float GetMenuBodyX()
        {
            float side = GetMenuSidebarWidth();
            if (side <= 0f) return 4f;
            return side + (side < 90f ? 6f : 10f);
        }

private static float GetMenuBodyWidth()
        {
            return Mathf.Max(64f, GetMenuVisibleWidth() - GetMenuBodyX() - 14f);
        }

private static float GetMenuWorkWidth(float min = 120f, float max = 620f)
        {
            float w = GetMenuBodyWidth() - 12f;
            if (w < min) return Mathf.Max(96f, w);
            return Mathf.Min(w, max);
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

            float visibleW = GetMenuVisibleWidth();
            bool microMenu = visibleW < 150f;

            GUILayout.BeginHorizontal();
            GUILayout.Label(microMenu ? "Elysium" : ApplyMenuShimmer("ElysiumModMenu Meowchelo & Carrot"), titleStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("-", new GUIStyle(btnStyle) { fixedWidth = 20, fixedHeight = 18, margin = CreateRectOffset(0, 8, 6, 0) })) showMenu = false;
            GUILayout.EndHorizontal();

            GUI.color = new Color(1f, 1f, 1f, 0.1f);
            GUI.Box(new Rect(0, 30, windowRect.width, 1), "", safeLineStyle);
            GUI.color = Color.white;

            if (microMenu)
            {
                DrawMicroMenu(visibleW);
                GUI.color = Color.white;
                GUI.DragWindow(new Rect(0, 0, 10000, 30));
                return;
            }

            float sideW = GetMenuSidebarWidth();
            float bodyX = GetMenuBodyX();
            float bodyW = GetMenuBodyWidth();
            float bodyY = 36f + ((1f - tabTransitionProgress) * 10f);
            float bodyH = windowRect.height - 46f;

            GUIStyle sideBtn = sidebarBtnStyle;
            GUIStyle sideBtnOn = activeSidebarBtnStyle;
            if (sideW < 110f)
            {
                sideBtn = new GUIStyle(sidebarBtnStyle) { fontSize = sideW < 70f ? 8 : 10, alignment = TextAnchor.MiddleCenter, clipping = TextClipping.Clip, padding = CreateRectOffset(2, 2, 6, 6) };
                sideBtnOn = new GUIStyle(activeSidebarBtnStyle) { fontSize = sideW < 70f ? 8 : 10, alignment = TextAnchor.MiddleCenter, clipping = TextClipping.Clip, padding = CreateRectOffset(2, 2, 6, 6) };
            }

            if (sideW > 0f)
            {
                GUILayout.BeginArea(new Rect(0f, 31f, sideW, windowRect.height - 31f));
                GUILayout.BeginVertical(sidebarStyle, GUILayout.ExpandHeight(true));
                GUILayout.Space(5);
                for (int i = 0; i < tabNames.Length; i++)
                    if (GUILayout.Button(tabNames[i], i == targetTabIndex ? sideBtnOn : sideBtn, GUILayout.Height(24)))
                        if (targetTabIndex != i) { targetTabIndex = i; tabTransitionProgress = 0f; scrollPosition = Vector2.zero; }
                GUILayout.EndVertical();
                GUILayout.EndArea();

                GUI.color = new Color(1f, 1f, 1f, 0.1f);
                GUI.Box(new Rect(sideW, 31, 1, windowRect.height), "", safeLineStyle);
            }
            else
            {
                GUIStyle topBtn = new GUIStyle(sidebarBtnStyle) { fontSize = 7, alignment = TextAnchor.MiddleCenter, clipping = TextClipping.Clip, padding = CreateRectOffset(1, 1, 2, 2) };
                GUIStyle topBtnOn = new GUIStyle(activeSidebarBtnStyle) { fontSize = 7, alignment = TextAnchor.MiddleCenter, clipping = TextClipping.Clip, padding = CreateRectOffset(1, 1, 2, 2) };
                float topW = GetMenuVisibleWidth() - 8f;
                float btnW = Mathf.Max(18f, Mathf.Floor((topW - 6f) / 4f));

                GUILayout.BeginArea(new Rect(4f, 32f, topW, 45f));
                for (int row = 0; row < 2; row++)
                {
                    GUILayout.BeginHorizontal(GUILayout.Height(20f));
                    for (int col = 0; col < 4; col++)
                    {
                        int i = row * 4 + col;
                        if (i >= tabNames.Length) break;
                        string nm = tabNames[i];
                        if (nm.Length > 4) nm = nm.Substring(0, 4);
                        if (GUILayout.Button(nm, i == targetTabIndex ? topBtnOn : topBtn, GUILayout.Width(btnW), GUILayout.Height(19f)))
                            if (targetTabIndex != i) { targetTabIndex = i; tabTransitionProgress = 0f; scrollPosition = Vector2.zero; }
                        if (col < 3) GUILayout.Space(2f);
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndArea();

                bodyY = 80f + ((1f - tabTransitionProgress) * 6f);
                bodyH = windowRect.height - 88f;
            }
            GUI.color = new Color(1f, 1f, 1f, tabTransitionProgress);

            GUILayout.BeginArea(new Rect(bodyX, bodyY, bodyW, bodyH));
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, false, GUIStyle.none, GUI.skin.verticalScrollbar);

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();

            int tabToDraw = (tabTransitionProgress < 1f) ? targetTabIndex : currentTab;

            if (tabToDraw == 0) DrawGeneralTab();
            else if (tabToDraw == 1) DrawSelfTab();
            else if (tabToDraw == 2) DrawVisualsTab();
            else if (tabToDraw == 3) { try { DrawPlayersTab(); } catch { } }
            else if (tabToDraw == 4) { try { DrawSabotageAnimationTab(); } catch { } }
            else if (tabToDraw == 5) DrawHostOnlyTab();
            else if (tabToDraw == 6) DrawVotekickTab();
            else if (tabToDraw == 7) DrawMenuTab();

            GUILayout.EndVertical();
            GUILayout.Space(10); // РћС‚СЃС‚СѓРї СЃРїСЂР°РІР° РґР»СЏ РІРµСЂС‚РёРєР°Р»СЊРЅРѕРіРѕ СЃРєСЂРѕР»Р»Р±Р°СЂР°
            GUILayout.EndHorizontal();

            GUILayout.EndScrollView();
            GUILayout.EndArea();

            GUI.color = Color.white;
            GUI.DragWindow(new Rect(0, 0, 10000, 30));
        }

private void DrawMicroMenu(float visibleW)
        {
            float w = Mathf.Max(54f, visibleW - 8f);
            GUIStyle st = new GUIStyle(sidebarBtnStyle) { fontSize = 7, alignment = TextAnchor.MiddleCenter, clipping = TextClipping.Clip, padding = CreateRectOffset(1, 1, 2, 2) };
            GUIStyle on = new GUIStyle(activeSidebarBtnStyle) { fontSize = 7, alignment = TextAnchor.MiddleCenter, clipping = TextClipping.Clip, padding = CreateRectOffset(1, 1, 2, 2) };

            GUILayout.BeginArea(new Rect(4f, 34f, w, Mathf.Max(60f, windowRect.height - 40f)));
            for (int i = 0; i < tabNames.Length; i++)
            {
                string nm = tabNames[i];
                if (nm.Length > 5) nm = nm.Substring(0, 5);
                if (GUILayout.Button(nm, i == targetTabIndex ? on : st, GUILayout.Width(w), GUILayout.Height(18f)))
                {
                    if (targetTabIndex != i)
                    {
                        targetTabIndex = i;
                        tabTransitionProgress = 0f;
                        scrollPosition = Vector2.zero;
                    }
                }
            }

            GUILayout.Space(4f);
            GUIStyle hint = new GUIStyle(menuDescStyle) { fontSize = 8, alignment = TextAnchor.MiddleCenter, clipping = TextClipping.Clip, wordWrap = false };
            GUILayout.Label("WIDEN", hint, GUILayout.Width(w), GUILayout.Height(14f));
            GUILayout.EndArea();
        }
    }
}
