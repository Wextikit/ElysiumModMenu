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

public static void MakePlayerGhost(PlayerControl target, bool impostorGhost = false, bool notify = true)
        {
            if (target == null || target.Data == null)
            {
                if (notify) ShowNotification("<color=#FF0000>[ОШИБКА]</color> Цель не найдена!");
                return;
            }
            if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
            {
                if (notify) ShowNotification("<color=#FF0000>[ОШИБКА]</color> Требуются права хоста!");
                return;
            }
            if (target.Data.IsDead)
            {
                if (!TrySetGhostRole(target, impostorGhost, out _))
                    SetPlayerRole(target, impostorGhost ? RoleTypes.Impostor : (IsImpostorTeamRole(target.Data.RoleType) ? RoleTypes.Impostor : RoleTypes.Crewmate));
                if (notify) ShowNotification($"{target.Data.PlayerName} уже призрак!");
                return;
            }

            try
            {
                string methodUsed;
                if (!TrySetGhostRole(target, impostorGhost, out methodUsed))
                {
                    RoleTypes fallbackRole = impostorGhost ? RoleTypes.Impostor : (IsImpostorTeamRole(target.Data.RoleType) ? RoleTypes.Impostor : RoleTypes.Crewmate);
                    SetPlayerRole(target, fallbackRole);
                    TryActivateGhostState(target, out methodUsed);
                }

                var netObj = GameData.Instance?.GetComponent<InnerNetObject>();
                if (netObj != null) netObj.SetDirtyBit(uint.MaxValue);

                if (notify) ShowNotification($"<color=#00FF00>[GHOST]</color> {target.Data.PlayerName} стал призраком ({methodUsed})!");
            }
            catch (Exception)
            {
                if (notify) ShowNotification("<color=#FF0000>Ошибка перевода в призрака!</color>");
            }
        }

private static bool TrySetGhostRole(PlayerControl target, bool impostorGhost, out string methodUsed)
        {
            methodUsed = string.Empty;
            if (target == null || target.Data == null) return false;

            string[] roleNames = impostorGhost
                ? new[] { "ImpostorGhost", "GhostImpostor", "ImpGhost", "Ghost" }
                : new[] { "CrewmateGhost", "GhostCrewmate", "CrewGhost", "Ghost" };

            foreach (string roleName in roleNames)
            {
                if (!Enum.TryParse(roleName, true, out RoleTypes ghostRole)) continue;

                try { target.RpcSetRole(ghostRole, true); } catch { }
                try { RoleManager.Instance?.SetRole(target, ghostRole); } catch { }

                methodUsed = $"SetRole:{roleName}";
                return true;
            }

            return false;
        }

private static bool TryActivateGhostState(PlayerControl target, out string methodUsed)
        {
            methodUsed = string.Empty;
            if (target == null) return false;
            if (target.Data != null && target.Data.IsDead)
            {
                methodUsed = "already_dead";
                return true;
            }

            if (TryDie(target, DeathReason.Exile, true) ||
                TryDie(target, DeathReason.Exile, false) ||
                TryDie(target, DeathReason.Kill, true) ||
                TryDie(target, DeathReason.Kill, false))
            {
                methodUsed = "Die";
                return true;
            }

            if (TryInvokeNoArg(target, "Exiled") ||
                TryInvokeNoArg(target, "RpcExiled") ||
                TryInvokeNoArg(target, "RpcExiledV2") ||
                TryInvokeNoArg(target, "SetDead"))
            {
                methodUsed = "Exiled/SetDead";
                return true;
            }

            if (TrySetDeadFlag(target))
            {
                methodUsed = "Data.IsDead";
                return true;
            }

            methodUsed = "fallback";
            return false;
        }

private static bool TryDie(PlayerControl target, DeathReason reason, bool allowAnimation)
        {
            try { target.Die(reason, allowAnimation); }
            catch { }
            return target != null && target.Data != null && target.Data.IsDead;
        }

private static bool TryInvokeNoArg(object target, string methodName)
        {
            if (target == null || string.IsNullOrWhiteSpace(methodName)) return false;
            try
            {
                for (Type type = target.GetType(); type != null; type = type.BaseType)
                {
                    MethodInfo method = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
                    if (method == null) continue;
                    method.Invoke(target, null);
                    return target is PlayerControl player && player.Data != null && player.Data.IsDead;
                }
            }
            catch { }
            return false;
        }

private static bool TrySetDeadFlag(PlayerControl target)
        {
            if (target == null || target.Data == null) return false;
            try
            {
                target.Data.IsDead = true;
                if (target.Collider != null) target.Collider.enabled = false;
                if (target.MyPhysics != null) target.MyPhysics.gameObject.layer = LayerMask.NameToLayer("Ghost");
            }
            catch { }
            return target.Data.IsDead;
        }

public static void SetAllPlayersGhost()
        {
            SetAllPlayersGhost(false);
        }

public static void SetAllPlayersGhost(bool impostorGhost)
        {
            if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
            {
                ShowNotification("<color=#FF0000>[ОШИБКА]</color> Требуются права хоста!");
                return;
            }
            if (PlayerControl.AllPlayerControls == null) return;

            int count = 0;
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (pc != null && pc.Data != null && !pc.Data.Disconnected)
                {
                    MakePlayerGhost(pc, impostorGhost, false);
                    count++;
                }
            }

            ShowNotification($"<color=#00FF00>[GHOST]</color> {count} игрок(а/ов) стали {(impostorGhost ? "ghost impostor" : "призраками")}!");
        }

public static void ReviveAllPlayers()
        {
            if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
            {
                ShowNotification("<color=#FF0000>[ERROR]</color> Host required!");
                return;
            }
            if (PlayerControl.AllPlayerControls == null) return;

            int count = 0;
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (pc == null || pc.Data == null || pc.Data.Disconnected || !pc.Data.IsDead)
                    continue;

                try
                {
                    pc.Data.IsDead = false;

                    if (pc.Collider != null) pc.Collider.enabled = true;
                    if (pc.MyPhysics != null)
                        pc.MyPhysics.gameObject.layer = LayerMask.NameToLayer("Players");

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

                            if (parentId == pc.PlayerId)
                                mb.gameObject.SetActive(false);
                        }
                    }
                    catch { }

                    bool wasImpTeam = false;
                    try
                    {
                        if (pc.Data.Role != null)
                        {
                            int roleId = (int)pc.Data.Role.Role;
                            wasImpTeam = roleId == 1 || roleId == 5 || roleId == 7 || roleId == 9 || roleId == 18;
                        }
                        else
                        {
                            var rt = pc.Data.RoleType;
                            wasImpTeam = rt == RoleTypes.Impostor || rt == RoleTypes.Shapeshifter || (int)rt == 9 || (int)rt == 18;
                        }
                    }
                    catch { }

                    pc.RpcSetRole(wasImpTeam ? RoleTypes.Impostor : RoleTypes.Crewmate, true);
                    count++;
                }
                catch { }
            }

            var netObj = GameData.Instance?.GetComponent<InnerNetObject>();
            if (netObj != null) netObj.SetDirtyBit(uint.MaxValue);

            ShowNotification($"<color=#00FF00>[REVIVE]</color> {count} player(s) revived!");
        }

public static void SetAllPlayersRole(RoleTypes role)
        {
            if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
            {
                ShowNotification("<color=#FF0000>[ОШИБКА]</color> Требуются права хоста!");
                return;
            }
            if (PlayerControl.AllPlayerControls == null) return;

            int count = 0;
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (pc != null && pc.Data != null && !pc.Data.Disconnected)
                {
                    pc.RpcSetRole(role, true);
                    count++;
                }
            }

            ShowNotification($"<color=#00FF00>[РОЛИ]</color> {count} игрок(а/ов) получили роль {GetRoleDisplayName(role)}!");
        }

public static void SetPlayerRole(PlayerControl target, RoleTypes newRole)
        {
            if (target == null || target.Data == null) return;
            target.RpcSetRole(newRole, true);
        }

private void DrawRolesTab()
        {
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(GUILayout.Width(280));

            GUILayout.BeginVertical(boxStyle);
            GUILayout.Label("Roles", headerStyle);
            GUILayout.BeginHorizontal();
            GUIStyle middleLabelStyle = new GUIStyle(btnStyle) { fontStyle = FontStyle.Bold, normal = { background = null, textColor = GetMenuAccentColor() } };
            if (GUILayout.Button("<", btnStyle, GUILayout.Width(25), GUILayout.Height(22))) { fakeRoleIdx--; if (fakeRoleIdx < 0) fakeRoleIdx = forceRoleOptions.Length - 1; }
            GUILayout.Label(forceRoleOptions[fakeRoleIdx].ToString(), middleLabelStyle, GUILayout.Width(100), GUILayout.Height(22));
            if (GUILayout.Button(">", btnStyle, GUILayout.Width(25), GUILayout.Height(22))) { fakeRoleIdx++; if (fakeRoleIdx >= forceRoleOptions.Length) fakeRoleIdx = 0; }
            GUILayout.Space(15);
            if (GUILayout.Button("Set", activeTabStyle, GUILayout.Width(45), GUILayout.Height(22))) RoleManager.Instance?.SetRole(PlayerControl.LocalPlayer, forceRoleOptions[fakeRoleIdx]);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.Space(5);
            GUILayout.BeginVertical(boxStyle);
            GUILayout.Label("Impostor", headerStyle);
            killReach = DrawToggle(killReach, "Kill Reach", 160);
            GUILayout.Space(5);
            killAnyone = DrawToggle(killAnyone, "Kill Anyone", 160);
            GUILayout.Space(5);
            killAuraHostOnly = DrawToggle(killAuraHostOnly, "Kill Aura", 160);
            GUILayout.Space(5);
            noKillCooldownHostOnly = DrawToggle(noKillCooldownHostOnly, "Kill Cooldown 0", 160);
            GUILayout.Space(5);
            killWhileVanishedHostOnly = DrawToggle(killWhileVanishedHostOnly, "Kill While Vanished", 160);
            GUILayout.Space(5);
            allowTasksAsImpostor = DrawToggle(allowTasksAsImpostor, "Allow Tasks (Imp)", 160);
            GUILayout.Space(5);
            spamReportBodies = DrawToggle(spamReportBodies, "Spam Report Bodies", 160);
            GUILayout.EndVertical();

            GUILayout.Space(5);
            GUILayout.BeginVertical(boxStyle);
            GUILayout.Label("Shapeshifter", headerStyle);
            NoShapeshiftAnim = DrawToggle(NoShapeshiftAnim, "No Ss Animation", 160);
            GUILayout.Space(5);
            endlessSsDuration = DrawToggle(endlessSsDuration, "Endless Ss Duration", 160);
            GUILayout.EndVertical();

            GUILayout.Space(5);
            GUILayout.BeginVertical(boxStyle);
            GUILayout.Label("Tracker", headerStyle);
            EndlessTracking = DrawToggle(EndlessTracking, "Endless Tracking", 160);
            GUILayout.Space(5);
            NoTrackingCooldown = DrawToggle(NoTrackingCooldown, "No Track Cooldown", 160);
            GUILayout.EndVertical();

            GUILayout.EndVertical();

            GUILayout.Space(10);

            GUILayout.BeginVertical(GUILayout.Width(280));

            GUILayout.BeginVertical(boxStyle);
            GUILayout.Label("Engineer", headerStyle);
            endlessVentTime = DrawToggle(endlessVentTime, "Endless Vent Time", 160);
            GUILayout.Space(5);
            noVentCooldown = DrawToggle(noVentCooldown, "No Vent Cooldown", 160);
            GUILayout.Space(5);
            unlockVents = DrawToggle(unlockVents, "Unlock Vents", 160);
            GUILayout.Space(5);
            walkInVents = DrawToggle(walkInVents, "Walk In Vents", 160);
            GUILayout.Space(5);
            noMapCooldowns = DrawToggle(noMapCooldowns, "No Map Cooldowns", 160);
            GUILayout.EndVertical();

            GUILayout.Space(5);
            GUILayout.BeginVertical(boxStyle);
            GUILayout.Label("Scientist", headerStyle);
            endlessBattery = DrawToggle(endlessBattery, "Endless Battery", 160);
            GUILayout.Space(5);
            noVitalsCooldown = DrawToggle(noVitalsCooldown, "No Vitals Cooldown", 160);
            GUILayout.EndVertical();

            GUILayout.Space(5);
            GUILayout.BeginVertical(boxStyle);
            GUILayout.Label("Detective", headerStyle);
            UnlimitedInterrogateRange = DrawToggle(UnlimitedInterrogateRange, "Interrogate Reach", 160);
            GUILayout.EndVertical();

            GUILayout.Space(5);
            GUILayout.BeginVertical(boxStyle);
            GUILayout.Label("Global Buffs", headerStyle);
            roleBuffImmortality = DrawToggle(roleBuffImmortality, "Immortality", 160);
            GUILayout.EndVertical();

            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

private Vector2 doorsScrollPos = Vector2.zero;
}
}
