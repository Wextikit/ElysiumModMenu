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

private static void ApplyCameraZoomTick()
        {
            try
            {
                Camera mainCamera = Camera.main;
                Camera uiCamera = HudManager.Instance?.UICamera;
                if (mainCamera == null) return;

                if (!cameraZoom)
                {
                    bool changed = Mathf.Abs(mainCamera.orthographicSize - 3f) > 0.001f ||
                                   (uiCamera != null && Mathf.Abs(uiCamera.orthographicSize - 3f) > 0.001f);

                    mainCamera.orthographicSize = 3f;
                    if (uiCamera != null) uiCamera.orthographicSize = 3f;

                    if (zoomResolutionRefreshNeeded || changed)
                    {
                        RefreshHudResolutionForZoom();
                        zoomResolutionRefreshNeeded = false;
                    }

                    return;
                }

                float scrollWheel = Input.GetAxis("Mouse ScrollWheel");
                if (Mathf.Abs(scrollWheel) <= 0.0001f || !IsCameraZoomScrollAllowed()) return;

                if (scrollWheel < 0f)
                {
                    mainCamera.orthographicSize += 1f;
                    if (uiCamera != null) uiCamera.orthographicSize += 1f;
                    zoomResolutionRefreshNeeded = true;
                    RefreshHudResolutionForZoom();
                }
                else if (scrollWheel > 0f && mainCamera.orthographicSize > 3f)
                {
                    mainCamera.orthographicSize -= 1f;
                    if (uiCamera != null) uiCamera.orthographicSize = Mathf.Max(3f, uiCamera.orthographicSize - 1f);
                    zoomResolutionRefreshNeeded = true;
                    RefreshHudResolutionForZoom();
                }
            }
            catch { }
        }

[HarmonyPatch(typeof(PassiveButton), nameof(PassiveButton.ReceiveClickDown))]
        public static class HardMenu_BlockClickDown_Patch
        {
            public static bool Prefix() { return !ElysiumModMenuGUI.IsCursorOverMenu(); }
        }

[HarmonyPatch(typeof(PassiveButton), nameof(PassiveButton.ReceiveClickUp))]
        public static class HardMenu_BlockClickUp_Patch
        {
            public static bool Prefix() { return !ElysiumModMenuGUI.IsCursorOverMenu(); }
        }

private void DrawPlayersTab()
        {
            GUILayout.BeginHorizontal();
            for (int i = 0; i < playersSubTabs.Length; i++)
                if (GUILayout.Button(playersSubTabs[i], currentPlayersSubTab == i ? activeSubTabStyle : subTabStyle, GUILayout.Height(18)))
                { currentPlayersSubTab = i; scrollPosition = Vector2.zero; }
            GUILayout.EndHorizontal();
            GUILayout.Space(8);

            if (currentPlayersSubTab == 1)
            {
                DrawPlayersHistoryTab();
                return;
            }

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(menuCardStyle, GUILayout.Width(200));
            playerListScrollPos = GUILayout.BeginScrollView(playerListScrollPos);
            if (lockedPlayersList.Count > 0)
            {
                foreach (var pc in lockedPlayersList)
                {
                    if (pc == null || pc.Data == null || pc.PlayerId >= 100) continue;
                    string pName = pc.Data.PlayerName ?? "Unknown";

                    if (forcedPreGameRoles.ContainsKey(pc.PlayerId)) pName += " [*]";
                    else if (forcedImpostors.Contains(pc.PlayerId)) pName += " [Imp]";

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
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.Space(8); GUILayout.BeginVertical(menuCardStyle, GUILayout.ExpandWidth(true));
            playerActionScrollPos = GUILayout.BeginScrollView(playerActionScrollPos);

            PlayerControl target = lockedPlayersList.FirstOrDefault(p => p.PlayerId == selectedAntiCheatPlayerId);

            if (target != null && target.Data != null)
            {
                GUILayout.Label($"<color=#aaaaaa>Selected:</color> {target.Data.PlayerName}", new GUIStyle(GUI.skin.label) { richText = true, fontSize = 14 });
                GUILayout.Space(10);
                GUILayout.BeginHorizontal();

                GUI.backgroundColor = new Color(0.8f, 0.2f, 0.2f, 1f);
                if (GUILayout.Button("KILL", btnStyle, GUILayout.Height(25)))
                {
                    Vector3 op = PlayerControl.LocalPlayer.transform.position;
                    PlayerControl.LocalPlayer.NetTransform.RpcSnapTo(target.transform.position);
                    PlayerControl.LocalPlayer.CmdCheckMurder(target);
                    PlayerControl.LocalPlayer.RpcMurderPlayer(target, true);
                    PlayerControl.LocalPlayer.NetTransform.RpcSnapTo(op);
                }
                GUI.backgroundColor = Color.white;

                if (GUILayout.Button("TP TO", activeTabStyle, GUILayout.Height(25)))
                {
                    teleportToPlayer(target);
                    ShowNotification($"<color=#00FF00>[TELEPORT]</color> Телепортирован к <b>{target.Data.PlayerName}</b>!");
                }

                GUI.backgroundColor = new Color(1f, 0.5f, 0f, 1f);
                if (GUILayout.Button("Force Eject", btnStyle, GUILayout.Height(25))) ForceGlobalEject(target);
                GUI.backgroundColor = Color.white;

                GUILayout.EndHorizontal();

                GUILayout.Space(5);

                GUILayout.BeginHorizontal();

                if (GUILayout.Button("Force Meeting", btnStyle, GUILayout.Height(25))) ForceMeetingAsPlayer(target);

                bool hr = rainbowPlayers.Contains(target.PlayerId);
                if (GUILayout.Button(hr ? "RGB: ON" : "RGB: OFF", hr ? activeTabStyle : btnStyle, GUILayout.Height(25)))
                {
                    if (!hr) rainbowPlayers.Add(target.PlayerId);
                    else rainbowPlayers.Remove(target.PlayerId);
                }

                GUILayout.EndHorizontal();

                GUILayout.Space(5);
                GUILayout.BeginHorizontal();

                if (GUILayout.Button("Report Body", btnStyle, GUILayout.Height(25)))
                    AttemptReportBody(target);

                if (GUILayout.Button("Flood Tasks", btnStyle, GUILayout.Height(25)))
                    FloodPlayerWithTasks(target);

                if (GUILayout.Button("Clear Tasks", btnStyle, GUILayout.Height(25)))
                    ClearPlayerTasks(target);

                GUILayout.EndHorizontal();

                GUILayout.Space(10);
                DrawMenuSectionHeader("TARGET ROLE CONTROL");

                GUILayout.BeginHorizontal();
                GUIStyle roleMidStyle = new GUIStyle(btnStyle) { fontStyle = FontStyle.Bold, normal = { background = null, textColor = GetMenuAccentColor() }, alignment = TextAnchor.MiddleCenter };
                if (GUILayout.Button("<", btnStyle, GUILayout.Width(28), GUILayout.Height(24)))
                {
                    targetRoleAssignIdx--;
                    if (targetRoleAssignIdx < 0) targetRoleAssignIdx = roleAssignOptions.Length - 1;
                }
                GUILayout.Label(roleAssignNames[targetRoleAssignIdx], roleMidStyle, GUILayout.Height(24), GUILayout.ExpandWidth(true));
                if (GUILayout.Button(">", btnStyle, GUILayout.Width(28), GUILayout.Height(24)))
                {
                    targetRoleAssignIdx++;
                    if (targetRoleAssignIdx >= roleAssignOptions.Length) targetRoleAssignIdx = 0;
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(4);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("TARGET -> ROLE", btnStyle, GUILayout.Height(26)))
                {
                    if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
                    {
                        ShowNotification("<color=#FF0000>[ОШИБКА]</color> Требуются права хоста!");
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
                if (GUILayout.Button("TARGET -> GHOST", btnStyle, GUILayout.Height(26)))
                {
                    MakePlayerGhost(target);
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(4);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("REVIVE TARGET", activeTabStyle, GUILayout.Height(26)))
                {
                    RevivePlayer(target);
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(15);
                GUILayout.Label("<color=#aaaaaa>Morph Target:</color>", new GUIStyle(GUI.skin.label) { richText = true, fontSize = 11 });
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

                GUIStyle morphLabelStyle = new GUIStyle(btnStyle);
                morphLabelStyle.normal.background = null;
                morphLabelStyle.hover.background = null;
                morphLabelStyle.normal.textColor = GetMenuAccentColor();
                morphLabelStyle.fontStyle = FontStyle.Bold;
                morphLabelStyle.alignment = TextAnchor.MiddleCenter;

                GUILayout.Label(morphName, morphLabelStyle, GUILayout.Height(25), GUILayout.ExpandWidth(true));

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
                if (GUILayout.Button("MORPH TARGET", btnStyle, GUILayout.Width(160), GUILayout.Height(25)))
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

                GUIStyle roundedColorBtnStyle = new GUIStyle();
                roundedColorBtnStyle.normal.background = texColorBtn;
                roundedColorBtnStyle.margin = CreateRectOffset(2, 2, 2, 2);

                int colorsPerRow = 7;
                for (int i = 0; i < Palette.PlayerColors.Length; i++)
                {
                    if (i % colorsPerRow == 0) GUILayout.BeginHorizontal();

                    GUI.color = Palette.PlayerColors[i];

                    if (GUILayout.Button("", roundedColorBtnStyle, GUILayout.Width(32), GUILayout.Height(30)))
                        target.RpcSetColor((byte)i);

                    if (i % colorsPerRow == colorsPerRow - 1 || i == Palette.PlayerColors.Length - 1)
                        GUILayout.EndHorizontal();
                }
                GUI.color = Color.white;
                GUILayout.EndVertical();

                GUILayout.Space(15);
                DrawMenuSectionHeader("PRE-GAME ROLE (HOST)");
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Impostor", btnStyle, GUILayout.Height(25))) { forcedPreGameRoles.Remove(target.PlayerId); forcedImpostors.Add(target.PlayerId); enablePreGameRoleForce = true; }
                if (GUILayout.Button("Crewmate", btnStyle, GUILayout.Height(25))) { forcedImpostors.Remove(target.PlayerId); forcedPreGameRoles[target.PlayerId] = RoleTypes.Crewmate; enablePreGameRoleForce = true; }
                if (GUILayout.Button("Shapeshifter", btnStyle, GUILayout.Height(25))) { forcedImpostors.Remove(target.PlayerId); forcedPreGameRoles[target.PlayerId] = RoleTypes.Shapeshifter; enablePreGameRoleForce = true; }
                GUILayout.EndHorizontal();
                GUILayout.Space(5);
                if (GUILayout.Button("REMOVE FORCED ROLE", activeTabStyle, GUILayout.Height(25))) { forcedPreGameRoles.Remove(target.PlayerId); forcedImpostors.Remove(target.PlayerId); }
            }
            else
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("<color=#777777>Select a player...</color>", new GUIStyle(GUI.skin.label) { richText = true, alignment = TextAnchor.MiddleCenter });
                GUILayout.FlexibleSpace();
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

private void DrawPlayersHistoryTab()
        {
            GUILayout.BeginVertical(menuCardStyle);
            DrawMenuSectionHeader("PLAYER HISTORY");

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Entries: {playerHistoryEntries.Count}", new GUIStyle(toggleLabelStyle) { fontSize = 11, clipping = TextClipping.Overflow, wordWrap = false }, GUILayout.MinWidth(128), GUILayout.ExpandWidth(false), GUILayout.Height(24));
            GUILayout.Label("File: ElysiumPlayerHistory.txt", new GUIStyle(toggleLabelStyle) { fontSize = 11, clipping = TextClipping.Overflow, wordWrap = false }, GUILayout.MinWidth(220), GUILayout.ExpandWidth(false), GUILayout.Height(24));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Clear History", btnStyle, GUILayout.Width(136), GUILayout.Height(24)))
            {
                playerHistoryEntries.Clear();
                playerHistoryKeysById.Clear();
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
                foreach (var e in playerHistoryEntries.OrderByDescending(x => x.LastSeenUtc))
                {
                    GUILayout.BeginVertical();
                    string status = e.IsOnline ? "<color=#55FF77>ONLINE</color>" : "<color=#aaaaaa>LEFT</color>";
                    GUILayout.Label($"{e.Name}  {status}", new GUIStyle(GUI.skin.label) { richText = true, fontSize = 13 });
                    GUILayout.Label($"Lv: {e.Level} | FC: {e.FriendCode} | PUID: {e.Puid}", new GUIStyle(GUI.skin.label) { fontSize = 11 });
                    GUILayout.Label($"Joined: {e.FirstSeenUtc:HH:mm:ss} | Left: {(e.LeftUtc.HasValue ? e.LeftUtc.Value.ToString("HH:mm:ss") : "online")}", new GUIStyle(GUI.skin.label) { fontSize = 11 });
                    GUILayout.Label($"Platform: {FormatPlatformHistory(e)}", new GUIStyle(GUI.skin.label) { fontSize = 11, wordWrap = true });
                    GUILayout.Label($"RPC: {FormatRpcHistory(e)}", new GUIStyle(GUI.skin.label) { fontSize = 11, wordWrap = true });
                    GUILayout.EndVertical();
                    GUILayout.Space(2);
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

private void ForceGlobalEject(PlayerControl target)
        {
            if (target == null || AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
            {
                ShowNotification("<color=#FF0000>[ERROR]</color> Нужны права Хоста!");
                return;
            }

            try
            {
                target.Data.IsDead = false;

                if (MeetingHud.Instance == null)
                {
                    MeetingHud.Instance = UnityEngine.Object.Instantiate<MeetingHud>(DestroyableSingleton<HudManager>.Instance.MeetingPrefab);
                    AmongUsClient.Instance.Spawn(MeetingHud.Instance.Cast<InnerNetObject>(), -2, SpawnFlags.None);
                }

                var emptyStates = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<MeetingHud.VoterState>(0);

                MeetingHud.Instance.RpcVotingComplete(emptyStates, target.Data, false);

                MeetingHud.Instance.RpcClose();

                ShowNotification($"<color=#00FF00>[EJECT]</color> Изгоняем <b>{target.Data.PlayerName}</b>...");
            }
            catch (Exception)
            {
            }
        }

private static bool IsDeadBodyForPlayerPresent(byte playerId)
        {
            try
            {
                var allBehaviours = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>();
                foreach (var mb in allBehaviours)
                {
                    if (mb == null || mb.gameObject == null) continue;
                    Type t = mb.GetType();
                    if (t == null || t.Name != "DeadBody") continue;

                    byte parentId = byte.MaxValue;
                    var parentProp = t.GetProperty("ParentId", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (parentProp != null)
                    {
                        object val = parentProp.GetValue(mb, null);
                        if (val is byte b) parentId = b;
                        else if (val is int i) parentId = (byte)i;
                    }
                    else
                    {
                        var parentField = t.GetField("ParentId", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (parentField != null)
                        {
                            object val = parentField.GetValue(mb);
                            if (val is byte b) parentId = b;
                            else if (val is int i) parentId = (byte)i;
                        }
                    }

                    if (parentId == playerId) return true;
                }
            }
            catch { }

            return false;
        }

private static void AttemptReportBody(PlayerControl target)
        {
            if (target == null || target.Data == null || PlayerControl.LocalPlayer == null) return;

            try
            {
                if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
                {
                    PlayerControl.LocalPlayer.CmdReportDeadBody(target.Data);
                    ShowNotification($"<color=#00FF00>[REPORT]</color> Репорт {target.Data.PlayerName}");
                    return;
                }

                if (LobbyBehaviour.Instance != null)
                {
                    ShowNotification("<color=#FF0000>[REPORT]</color> Игра должна начаться.");
                    return;
                }

                if (!target.Data.IsDead)
                {
                    ShowNotification("<color=#FF0000>[REPORT]</color> Можно репортить только мертвых игроков.");
                    return;
                }

                if (!IsDeadBodyForPlayerPresent(target.PlayerId))
                {
                    ShowNotification("<color=#FF0000>[REPORT]</color> Труп не найден или уже исчез.");
                    return;
                }

                PlayerControl.LocalPlayer.CmdReportDeadBody(target.Data);
                ShowNotification($"<color=#00FF00>[REPORT]</color> Репорт {target.Data.PlayerName}");
            }
            catch (Exception)
            {
            }
        }

private static void FloodPlayerWithTasks(PlayerControl target)
        {
            if (target == null || target.Data == null)
            {
                ShowNotification("<color=#FF0000>[TASKS]</color> Цель не найдена.");
                return;
            }

            if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
            {
                ShowNotification("<color=#FF0000>[TASKS]</color> Нужны права хоста.");
                return;
            }

            try
            {
                byte[] taskIds = new byte[255];
                for (byte i = 0; i < 255; i++) taskIds[i] = i;
                target.Data.RpcSetTasks(taskIds);
                ShowNotification($"<color=#00FF00>[TASKS]</color> {target.Data.PlayerName} получил flood tasks.");
            }
            catch (Exception)
            {
            }
        }

private static void ClearPlayerTasks(PlayerControl target)
        {
            if (target == null || target.Data == null)
            {
                ShowNotification("<color=#FF0000>[TASKS]</color> Цель не найдена.");
                return;
            }

            if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
            {
                ShowNotification("<color=#FF0000>[TASKS]</color> Нужны права хоста.");
                return;
            }

            try
            {
                target.Data.RpcSetTasks(Array.Empty<byte>());
                ShowNotification($"<color=#00FF00>[TASKS]</color> Задачи {target.Data.PlayerName} очищены.");
            }
            catch (Exception)
            {
            }
        }

private static string GetRoleDisplayName(RoleTypes role)
        {
            for (int i = 0; i < roleAssignOptions.Length; i++)
                if (roleAssignOptions[i].Equals(role))
                    return roleAssignNames[i];
            return role.ToString();
        }

private static bool IsGhostRoleSelection(int roleIndex)
        {
            return roleIndex >= 0 &&
                   roleIndex < roleAssignNames.Length &&
                   string.Equals(roleAssignNames[roleIndex], "Ghost", StringComparison.OrdinalIgnoreCase);
        }

private static bool IsGhostImpostorRoleSelection(int roleIndex)
        {
            return roleIndex >= 0 &&
                   roleIndex < roleAssignNames.Length &&
                   string.Equals(roleAssignNames[roleIndex], "Ghost Imp", StringComparison.OrdinalIgnoreCase);
        }

private static bool IsImpostorTeamRole(RoleTypes role)
        {
            int roleId = (int)role;
            return role == RoleTypes.Impostor || role == RoleTypes.Shapeshifter || roleId == 9 || roleId == 18;
        }

private static bool IsLocalPhantomVanished()
        {
            try
            {
                PlayerControl local = PlayerControl.LocalPlayer;
                if (local == null || local.Data == null || local.Data.Role == null) return false;
                return local.Data.Role is PhantomRole phantom && (phantom.fading || phantom.isInvisible || phantom.IsInvisible);
            }
            catch { return false; }
        }

private static bool IsMalumValidKillTarget(NetworkedPlayerInfo target)
        {
            try
            {
                if (target == null || target.Object == null || target.Role == null) return false;
                if (target.Disconnected || target.PlayerId == PlayerControl.LocalPlayer.PlayerId) return false;

                bool baseRequirements = target.Object.Visible;
                if (!baseRequirements) return false;
                if (killAnyone) return true;

                return !target.IsDead &&
                       !target.Object.inVent &&
                       !target.Object.inMovingPlat &&
                       target.Role.CanBeKilled;
            }
            catch { return false; }
        }

private static bool IsLocalImpostorRole(NetworkedPlayerInfo info = null)
        {
            try
            {
                NetworkedPlayerInfo playerInfo = info ?? PlayerControl.LocalPlayer?.Data;
                if (playerInfo == null) return false;
                return RoleManager.IsImpostorRole(playerInfo.RoleType) ||
                       (playerInfo.Role != null && playerInfo.Role.IsImpostor);
            }
            catch { return false; }
        }

private static float GetConsoleUsableDistance(global::Console console)
        {
            if (console == null) return 1f;

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            try
            {
                FieldInfo field = console.GetType().GetField("usableDistance", flags) ?? console.GetType().GetField("UsableDistance", flags);
                if (field != null && field.GetValue(console) is float fieldValue) return fieldValue;

                PropertyInfo property = console.GetType().GetProperty("usableDistance", flags) ?? console.GetType().GetProperty("UsableDistance", flags);
                if (property != null && property.GetValue(console, null) is float propertyValue) return propertyValue;
            }
            catch { }

            return 1f;
        }

private static bool LocalPlayerHasTaskForConsole(global::Console console)
        {
            try
            {
                if (console == null || PlayerControl.LocalPlayer?.myTasks == null) return false;

                foreach (var task in PlayerControl.LocalPlayer.myTasks)
                {
                    if (task == null) continue;
                    try { if (task.IsComplete) continue; } catch { }
                    if (TaskAcceptsConsole(task, console)) return true;
                }
            }
            catch { }

            return false;
        }

private static bool TaskAcceptsConsole(object task, global::Console console)
        {
            if (task == null || console == null) return false;

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            try
            {
                MethodInfo validConsole = task.GetType().GetMethod("ValidConsole", flags, null, new[] { typeof(global::Console) }, null);
                if (validConsole != null && validConsole.Invoke(task, new object[] { console }) is bool valid)
                    return valid;
            }
            catch { }

            return false;
        }

private static bool ShouldBlockUnsafeConsoleUse(global::Console console)
        {
            try
            {
                if (!allowTasksAsImpostor || console == null) return false;
                if (!IsLocalImpostorRole()) return false;
                return !LocalPlayerHasTaskForConsole(console);
            }
            catch { return false; }
        }

private static float GetVanillaKillDistance()
        {
            try
            {
                int killDistance = GameOptionsManager.Instance.CurrentGameOptions.GetInt(Int32OptionNames.KillDistance);
                if (killDistance <= 0) return 1f;
                if (killDistance == 1) return 1.8f;
                return 2.5f;
            }
            catch { return 2.5f; }
        }

private static PlayerControl FindClosestKillTarget(ImpostorRole role, float maxDistance)
        {
            try
            {
                PlayerControl local = PlayerControl.LocalPlayer;
                if (local == null || local.Data == null || PlayerControl.AllPlayerControls == null) return null;

                Vector3 localWorld = local.transform.position;
                Vector2 localPos = new Vector2(localWorld.x, localWorld.y);
                PlayerControl result = null;
                float bestDistance = maxDistance;

                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    if (pc == null || pc == local || pc.Data == null) continue;
                    if (pc.Data.Disconnected || pc.Data.IsDead) continue;
                    if (pc.inVent || pc.onLadder) continue;
                    if (!killAnyone && IsImpostorTeamRole(pc.Data.RoleType)) continue;
                    if (!killAnyone && role != null && !role.IsValidTarget(pc.Data)) continue;

                    Vector3 targetWorld = pc.transform.position;
                    float distance = Vector2.Distance(localPos, new Vector2(targetWorld.x, targetWorld.y));
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        result = pc;
                    }
                }

                return result;
            }
            catch { return null; }
        }

public static void RevivePlayer(PlayerControl target)
        {
            if (target == null || target.Data == null)
            {
                ShowNotification("<color=#FF0000>[ОШИБКА]</color> Цель не найдена!");
                return;
            }
            if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
            {
                ShowNotification("<color=#FF0000>[ОШИБКА]</color> Требуются права хоста!");
                return;
            }
            if (!target.Data.IsDead)
            {
                ShowNotification($"{target.Data.PlayerName} уже жив!");
                return;
            }

            try
            {
                target.Data.IsDead = false;

                if (target.Collider != null) target.Collider.enabled = true;

                if (target.MyPhysics != null)
                    target.MyPhysics.gameObject.layer = LayerMask.NameToLayer("Players");

                try
                {
                    var allBehaviours = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>();
                    foreach (var mb in allBehaviours)
                    {
                        if (mb == null || mb.gameObject == null) continue;
                        Type t = mb.GetType();
                        if (t == null || t.Name != "DeadBody") continue;

                        byte parentId = byte.MaxValue;

                        var parentProp = t.GetProperty("ParentId", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (parentProp != null)
                        {
                            object val = parentProp.GetValue(mb, null);
                            if (val is byte b) parentId = b;
                            else if (val is int i) parentId = (byte)i;
                        }
                        else
                        {
                            var parentField = t.GetField("ParentId", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            if (parentField != null)
                            {
                                object val = parentField.GetValue(mb);
                                if (val is byte b) parentId = b;
                                else if (val is int i) parentId = (byte)i;
                            }
                        }

                        if (parentId == target.PlayerId)
                            mb.gameObject.SetActive(false);
                    }
                }
                catch { }

                bool wasImpTeam = false;
                try
                {
                    if (target.Data.Role != null)
                    {
                        int roleId = (int)target.Data.Role.Role;
                        wasImpTeam = roleId == 1 || roleId == 5 || roleId == 7 || roleId == 9 || roleId == 18;
                    }
                    else
                    {
                        var rt = target.Data.RoleType;
                        wasImpTeam = rt == RoleTypes.Impostor || rt == RoleTypes.Shapeshifter || (int)rt == 9 || (int)rt == 18;
                    }
                }
                catch { }

                target.RpcSetRole(wasImpTeam ? RoleTypes.Impostor : RoleTypes.Crewmate, true);

                var netObj = GameData.Instance?.GetComponent<InnerNetObject>();
                if (netObj != null) netObj.SetDirtyBit(uint.MaxValue);

                ShowNotification($"<color=#00FF00>[ВОСКРЕШЕНИЕ]</color> {target.Data.PlayerName} воскрешён!");
            }
            catch (Exception)
            {
                ShowNotification("<color=#FF0000>Ошибка воскрешения!</color>");
            }
        }
}
}
