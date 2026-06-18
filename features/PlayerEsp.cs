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

[HarmonyPatch(typeof(RoleManager), nameof(RoleManager.SelectRoles))]
        public static class RoleManager_SelectRoles_Patch
        {
            public static bool Prefix(RoleManager __instance)
            {
                if (!ElysiumModMenuGUI.enablePreGameRoleForce || !AmongUsClient.Instance.AmHost) return true;
                try
                {
                    var allPlayers = PlayerControl.AllPlayerControls.ToArray().Where(p => p != null && p.Data != null && !p.Data.Disconnected && !p.Data.IsDead).ToList();
                    int numImps = 1;
                    try { numImps = GameOptionsManager.Instance.CurrentGameOptions.GetInt((Int32OptionNames)1); } catch { }
                    var impRoleTypes = new HashSet<int> { 1, 5, 9, 18 };
                    List<PlayerControl> impostors = new List<PlayerControl>();
                    foreach (var p in allPlayers)
                        if (ElysiumModMenuGUI.forcedImpostors.Contains(p.PlayerId) || (ElysiumModMenuGUI.forcedPreGameRoles.ContainsKey(p.PlayerId) && impRoleTypes.Contains((int)ElysiumModMenuGUI.forcedPreGameRoles[p.PlayerId])))
                            impostors.Add(p);
                    if (impostors.Count > 0) numImps = impostors.Count;
                    else { if (numImps >= allPlayers.Count) numImps = allPlayers.Count - 1; if (numImps < 1) numImps = 1; }
                    System.Random rand = new System.Random();
                    while (impostors.Count < numImps && allPlayers.Count > impostors.Count)
                    {
                        var available = allPlayers.Where(p => !impostors.Contains(p)).ToList();
                        impostors.Add(available[rand.Next(available.Count)]);
                    }
                    List<PlayerControl> crewmates = allPlayers.Where(p => !impostors.Contains(p)).ToList();
                    var impData = new Il2CppSystem.Collections.Generic.List<NetworkedPlayerInfo>();
                    foreach (var i in impostors) impData.Add(i.Data);
                    var crewData = new Il2CppSystem.Collections.Generic.List<NetworkedPlayerInfo>();
                    foreach (var c in crewmates) crewData.Add(c.Data);
                    IGameOptions opts = GameOptionsManager.Instance.CurrentGameOptions;
                    GameManager.Instance.LogicRoleSelection.AssignRolesForTeam(impData, opts, (RoleTeamTypes)1, int.MaxValue, new Il2CppSystem.Nullable<RoleTypes>());
                    GameManager.Instance.LogicRoleSelection.AssignRolesForTeam(crewData, opts, (RoleTeamTypes)0, int.MaxValue, new Il2CppSystem.Nullable<RoleTypes>((RoleTypes)0));
                    foreach (var kvp in ElysiumModMenuGUI.forcedPreGameRoles)
                    {
                        if (kvp.Value != RoleTypes.Crewmate && kvp.Value != RoleTypes.Impostor)
                        {
                            var pc = allPlayers.FirstOrDefault(p => p.PlayerId == kvp.Key);
                            if (pc != null) RoleManager.Instance.SetRole(pc, kvp.Value);
                        }
                    }
                    foreach (var pc in allPlayers) if (pc.Data.Role != null) pc.Data.Role.Initialize(pc);
                    return false;
                }
                catch { return true; }
            }
        }

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.TurnOnProtection))]
        public static class PlayerControl_TurnOnProtection_Patch
        {
            public static void Prefix(ref bool visible)
            {
                if (ElysiumModMenuGUI.seeGhosts || ElysiumModMenuGUI.seeProtections) visible = true;
            }
        }

[HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.LateUpdate))]
        public static class PlayerVisuals_LateUpdate_Patch
        {
            public static void Postfix(PlayerPhysics __instance)
            {
                if (__instance == null || __instance.myPlayer == null || __instance.myPlayer.Data == null) return;
                try
                {
                    if (ElysiumModMenuGUI.seeGhosts && __instance.myPlayer.Data.IsDead && PlayerControl.LocalPlayer != null && !PlayerControl.LocalPlayer.Data.IsDead)
                    {
                        __instance.myPlayer.Visible = true;
                        var rend = __instance.myPlayer.GetComponent<SpriteRenderer>();
                        if (rend != null) { Color c = rend.color; rend.color = new Color(c.r, c.g, c.b, 0.4f); }
                    }
                    var cosmetics = __instance.myPlayer.cosmetics;
                    var outfit = __instance.myPlayer.CurrentOutfit;
                    if (cosmetics != null && cosmetics.nameText != null && outfit != null)
                    {
                        cosmetics.SetName(ElysiumModMenuGUI.GetESPNameTag(__instance.myPlayer.Data, outfit.PlayerName));
                        if (ElysiumModMenuGUI.seeRoles && ElysiumModMenuGUI.showPlayerInfo) cosmetics.nameText.transform.localPosition = new Vector3(0f, 0.186f, 0f);
                        else if (ElysiumModMenuGUI.seeRoles || ElysiumModMenuGUI.showPlayerInfo) cosmetics.nameText.transform.localPosition = new Vector3(0f, 0.093f, 0f);
                        else cosmetics.nameText.transform.localPosition = new Vector3(0f, 0f, 0f);
                    }
                }
                catch { }
            }
        }

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Update))]
        public static class ESP_MeetingHud
        {
            public static void Postfix(MeetingHud __instance)
            {
                try
                {
                    if (__instance.playerStates == null) return;
                    foreach (var state in __instance.playerStates)
                    {
                        if (state == null) continue;
                        var data = GameData.Instance.GetPlayerById(state.TargetPlayerId);
                        if (data != null && !data.Disconnected && data.DefaultOutfit != null && state.NameText != null)
                        {
                            string espName = ElysiumModMenuGUI.GetESPNameTag(data, data.DefaultOutfit.PlayerName ?? "???");
                            if (!ElysiumModMenuGUI.seeRoles && ElysiumModMenuGUI.revealMeetingRoles && data.Role != null)
                            {
                                string roleName = data.Role.Role.ToString();
                                int roleId = (int)data.Role.Role;
                                if (roleId == 8) roleName = "Noisemaker";
                                else if (roleId == 9) roleName = "Phantom";
                                else if (roleId == 10) roleName = "Tracker";
                                else if (roleId == 12) roleName = "Detective";
                                else if (roleId == 18) roleName = "Viper";
                                else if (roleName == "GuardianAngel") roleName = "Guardian Angel";
                                Color customColor = ElysiumModMenuGUI.GetRoleColor(roleId, data.Role.TeamColor);
                                string roleColor = ColorUtility.ToHtmlStringRGB(customColor);
                                espName = $"<color=#{roleColor}>{roleName}</color>\n{espName}";
                            }
                            state.NameText.text = espName;
                            bool showingExtra = ElysiumModMenuGUI.seeRoles || ElysiumModMenuGUI.revealMeetingRoles;
                            if (showingExtra) { state.NameText.transform.localPosition = new Vector3(0.3384f, 0.1125f, -0.1f); state.NameText.transform.localScale = new Vector3(0.9f, 1f, 1f); }
                            else { state.NameText.transform.localPosition = new Vector3(0.3384f, 0.0311f, -0.1f); state.NameText.transform.localScale = new Vector3(0.9f, 1f, 1f); }
                        }
                    }
                }
                catch { }
            }
        }

[HarmonyPatch(typeof(ChatBubble), nameof(ChatBubble.SetName))]
        public static class ChatBubble_SetName_Patch
        {
            public static void Postfix(ChatBubble __instance)
            {
                if (!ElysiumModMenuGUI.showPlayerInfo || __instance.playerInfo == null) return;
                if (ElysiumModMenuGUI.IsMeetingVoteUiActive()) return;
                try
                {
                    string accentHex = ElysiumModMenuGUI.GetMenuAccentHex(false);
                    string espLine = ElysiumModMenuGUI.BuildESPInfoLine(__instance.playerInfo);
                    if (string.IsNullOrWhiteSpace(espLine)) return;
                    string extra = $" <color=#{accentHex}><size=80%>{espLine}</size></color>";

                    if (!__instance.NameText.text.Contains("Lv:")) __instance.NameText.text += extra;
                }
                catch { }
            }
        }

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcMurderPlayer))]
        public static class KillCooldownTrackerPatch
        {
            public static void Prefix(PlayerControl __instance, PlayerControl target, bool didSucceed)
            {
                try
                {
                    if (!didSucceed || __instance == null || __instance.Data == null) return;
                    ElysiumModMenuGUI.lastKillTimestamps[__instance.PlayerId] = Time.time;
                }
                catch { }
            }
        }

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
        public static class FullBright_Patch
        {
            public static void Postfix(HudManager __instance)
            {
                try
                {
                    if (__instance == null || __instance.ShadowQuad == null || __instance.ShadowQuad.gameObject == null) return;
                    __instance.ShadowQuad.gameObject.SetActive(!ElysiumModMenuGUI.fullBright);
                }
                catch { }
            }
        }

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
        public static class HudManager_Update_Patch
        {
            public static void Postfix(HudManager __instance)
            {
                try
                {
                    if (ElysiumModMenuGUI.alwaysChat && __instance.Chat != null)
                        __instance.Chat.gameObject.SetActive(true);

                    object aspectPosition = ElysiumModMenuGUI.GetHudAspectPosition(__instance);
                    if (aspectPosition != null)
                    {
                        float camSize = Camera.main != null ? Camera.main.orthographicSize : 3f;
                        if ((!ElysiumModMenuGUI.hudZoomBaseCaptured || (!ElysiumModMenuGUI.cameraZoom && camSize <= 3.05f)) &&
                            ElysiumModMenuGUI.TryGetAspectDistance(aspectPosition, out Vector3 currentDistance))
                        {
                            ElysiumModMenuGUI.hudZoomBaseDistance = currentDistance;
                            ElysiumModMenuGUI.hudZoomBaseCaptured = true;
                        }

                        if (ElysiumModMenuGUI.cameraZoom && ElysiumModMenuGUI.hudZoomBaseCaptured)
                        {
                            Vector3 distance = ElysiumModMenuGUI.hudZoomBaseDistance;
                            distance.y = ElysiumModMenuGUI.hudZoomBaseDistance.y + 3f * (camSize - 3f);
                            ElysiumModMenuGUI.TrySetAspectDistance(aspectPosition, distance);
                        }
                    }

                    if (ElysiumModMenuGUI.cameraZoom && __instance.TaskPanel != null && ShipStatus.Instance != null && MeetingHud.Instance == null)
                        __instance.TaskPanel.gameObject.SetActive(true);
                }
                catch { }
            }
        }

[HarmonyPatch(typeof(FullAccount), nameof(FullAccount.CanSetCustomName))]
        public static class FullAccount_CanSetCustomName_Patch { public static void Prefix(ref bool canSetName) { try { if (ElysiumModMenuGUI.unlockFeatures) canSetName = true; } catch { } } }

[HarmonyPatch(typeof(AccountManager), nameof(AccountManager.CanPlayOnline))]
        public static class AccountManager_CanPlayOnline_Patch { public static void Postfix(ref bool __result) { try { if (ElysiumModMenuGUI.unlockFeatures) __result = true; } catch { } } }

[HarmonyPatch(typeof(EngineerRole), "FixedUpdate")]
        public static class EngineerCheatsPatch
        {
            public static void Postfix(EngineerRole __instance)
            {
                if (__instance.Player != PlayerControl.LocalPlayer) return;
                if (ElysiumModMenuGUI.endlessVentTime) __instance.inVentTimeRemaining = float.MaxValue;
                if (ElysiumModMenuGUI.noVentCooldown && __instance.cooldownSecondsRemaining > 0f)
                {
                    __instance.cooldownSecondsRemaining = 0f;
                    var btn = DestroyableSingleton<HudManager>.Instance?.AbilityButton;
                    if (btn != null) { btn.ResetCoolDown(); btn.SetCooldownFill(0f); }
                }
            }
        }

[HarmonyPatch(typeof(Vent), nameof(Vent.CanUse))]
        public static class Vent_CanUse_UnlockVents_Patch
        {
            public static void Postfix(Vent __instance, NetworkedPlayerInfo pc, ref bool canUse, ref bool couldUse, ref float __result)
            {
                try
                {
                    if (!ElysiumModMenuGUI.unlockVents || __instance == null || pc == null || pc.Object == null)
                        return;

                    PlayerControl local = PlayerControl.LocalPlayer;
                    if (local == null || local.Data == null || local.Data.IsDead || (local.Data.Role != null && local.Data.Role.CanVent))
                        return;

                    PlayerControl target = pc.Object;
                    if (target != local || target.Collider == null)
                        return;

                    Vector2 center = target.Collider.bounds.center;
                    Vector2 position = __instance.transform.position;
                    float distance = Vector2.Distance(center, position);
                    canUse = distance <= __instance.UsableDistance && !PhysicsHelpers.AnythingBetween(target.Collider, center, position, Constants.ShipOnlyMask, false);
                    couldUse = true;
                    __result = distance;
                }
                catch { }
            }
        }

private static bool TrySetCooldownMember(object target, float value)
        {
            if (target == null) return false;

            string[] names = { "CoolDown", "_CoolDown_k__BackingField", "<CoolDown>k__BackingField", "coolDown", "cooldown" };
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            try
            {
                Type type = target.GetType();
                foreach (string name in names)
                {
                    PropertyInfo property = type.GetProperty(name, flags);
                    if (property != null && property.CanWrite)
                    {
                        property.SetValue(target, value, null);
                        return true;
                    }

                    FieldInfo field = type.GetField(name, flags);
                    if (field != null)
                    {
                        field.SetValue(target, value);
                        return true;
                    }
                }
            }
            catch { }

            return false;
        }

[HarmonyPatch(typeof(Ladder), "SetDestinationCooldown")]
        public static class Ladder_SetDestinationCooldown_Patch
        {
            public static bool Prefix(Ladder __instance)
            {
                try
                {
                    if (!ElysiumModMenuGUI.noMapCooldowns) return true;
                    TrySetCooldownMember(__instance, 0f);
                    return false;
                }
                catch { return true; }
            }
        }

[HarmonyPatch(typeof(ZiplineConsole), "Update")]
        public static class ZiplineConsole_Update_Patch
        {
            public static void Postfix(ZiplineConsole __instance)
            {
                try
                {
                    if (!ElysiumModMenuGUI.noMapCooldowns) return;
                    TrySetCooldownMember(__instance, 0f);
                }
                catch { }
            }
        }

[HarmonyPatch(typeof(PlayerControl), "MurderPlayer")]
        public static class KillCooldownTrackerPatch2
        {
            public static void Prefix(PlayerControl __instance, PlayerControl target)
            {
                try
                {
                    if (__instance == null || __instance.Data == null) return;
                    ElysiumModMenuGUI.lastKillTimestamps[__instance.PlayerId] = Time.time;

                    if (!ElysiumModMenuGUI.spamReportBodies) return;
                    if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data == null || PlayerControl.LocalPlayer.Data.IsDead) return;
                    if (target == null || target.Data == null || !target.Data.IsDead) return;

                    PlayerControl.LocalPlayer.CmdReportDeadBody(target.Data);
                }
                catch { }
            }
        }

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.SetKillTimer))]
        public static class KillAuraNoKillCooldownPatch
        {
            public static void Prefix(PlayerControl __instance, ref float time)
            {
                try
                {
                    if (!ElysiumModMenuGUI.noKillCooldownHostOnly) return;
                    if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return;
                    if (__instance != PlayerControl.LocalPlayer) return;
                    time = 0f;
                }
                catch { }
            }
        }

[HarmonyPatch(typeof(ScientistRole), "Update")]
        public static class ScientistCheatsPatch
        {
            public static void Postfix(ScientistRole __instance)
            {
                if (__instance.Player != PlayerControl.LocalPlayer) return;
                if (ElysiumModMenuGUI.noVitalsCooldown) __instance.currentCooldown = 0f;
                if (ElysiumModMenuGUI.endlessBattery) __instance.currentCharge = float.MaxValue;
            }
        }

[HarmonyPatch(typeof(ShapeshifterRole), "FixedUpdate")]
        public static class ShapeshifterDurationPatch
        {
            public static void Postfix(ShapeshifterRole __instance) { if (__instance.Player == PlayerControl.LocalPlayer && ElysiumModMenuGUI.endlessSsDuration) __instance.durationSecondsRemaining = float.MaxValue; }
        }

[HarmonyPatch(typeof(ImpostorRole), "FindClosestTarget")]
        public static class ImpostorRangePatch
        {
            public static bool Prefix(ImpostorRole __instance, ref PlayerControl __result)
            {
                if (!ElysiumModMenuGUI.killReach) return true;
                try
                {
                    var target = ElysiumModMenuGUI.FindClosestKillTarget(__instance, float.MaxValue);
                    if (target != null) __result = target;
                    return false;
                }
                catch { return true; }
            }
        }

[HarmonyPatch(typeof(ImpostorRole), "FindClosestTarget")]
        public static class ImpostorKillWhileVanishedPatch
        {
            public static void Postfix(ImpostorRole __instance, ref PlayerControl __result)
            {
                try
                {
                    if (__result != null) return;
                    if (!ElysiumModMenuGUI.killWhileVanishedHostOnly) return;
                    if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return;
                    if (!ElysiumModMenuGUI.IsLocalPhantomVanished()) return;

                    __result = ElysiumModMenuGUI.FindClosestKillTarget(__instance, ElysiumModMenuGUI.GetVanillaKillDistance());
                }
                catch { }
            }
        }

[HarmonyPatch(typeof(PhantomRole), nameof(PhantomRole.IsValidTarget))]
        public static class PhantomRole_IsValidTarget_KillWhileVanished_Patch
        {
            public static void Postfix(NetworkedPlayerInfo target, ref bool __result)
            {
                try
                {
                    if (!ElysiumModMenuGUI.killWhileVanishedHostOnly) return;
                    if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return;
                    if (!ElysiumModMenuGUI.IsLocalPhantomVanished()) return;

                    __result = ElysiumModMenuGUI.IsMalumValidKillTarget(target);
                }
                catch { }
            }
        }

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CmdCheckMurder))]
        public static class PlayerControl_CmdCheckMurder_KillWhileVanished_Patch
        {
            public static bool Prefix(PlayerControl __instance, PlayerControl target)
            {
                try
                {
                    if (!ElysiumModMenuGUI.killWhileVanishedHostOnly) return true;
                    if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return true;
                    if (__instance != PlayerControl.LocalPlayer) return true;
                    if (!ElysiumModMenuGUI.IsLocalPhantomVanished()) return true;
                    if (target == null || target.Data == null || !ElysiumModMenuGUI.IsMalumValidKillTarget(target.Data)) return true;

                    PlayerControl.LocalPlayer.RpcMurderPlayer(target, true);
                    return false;
                }
                catch { return true; }
            }
        }

[HarmonyPatch(typeof(ImpostorRole), "IsValidTarget")]
        public static class ImpostorKillAnyonePatch
        {
            public static void Postfix(NetworkedPlayerInfo target, ref bool __result) { try { if (ElysiumModMenuGUI.killAnyone) __result = ElysiumModMenuGUI.IsMalumValidKillTarget(target); } catch { } }
        }

private void teleportToPlayer(PlayerControl t)
        {
            if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.NetTransform == null || t == null) return;
            PlayerControl.LocalPlayer.NetTransform.RpcSnapTo(t.transform.position);
        }

[HarmonyPatch(typeof(DetectiveRole), "FindClosestTarget")]
        public static class DetectiveRangePatch
        {
            public static bool Prefix(DetectiveRole __instance, ref PlayerControl __result)
            {
                if (!ElysiumModMenuGUI.UnlimitedInterrogateRange) return true;
                try
                {
                    var target = PlayerControl.AllPlayerControls.ToArray()
                        .Where(p => p != null && __instance.IsValidTarget(p.Data) && !p.Data.IsDead && !p.Data.Disconnected)
                        .OrderBy(p => Vector2.Distance(p.transform.position, PlayerControl.LocalPlayer.transform.position))
                        .FirstOrDefault();
                    if (target != null) __result = target;
                    return false;
                }
                catch { return true; }
            }
        }

[HarmonyPatch(typeof(DoorBreakerGame), nameof(DoorBreakerGame.Start))]
        public static class DoorBreakerGame_Start_Patch
        {
            public static bool Prefix(DoorBreakerGame __instance)
            {
                if (!ElysiumModMenuGUI.autoOpenDoors) return true;
                try { ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Doors, (byte)(__instance.MyDoor.Id | 64)); } catch { }
                __instance.MyDoor.SetDoorway(true); __instance.Close();
                return false;
            }
        }

[HarmonyPatch(typeof(DoorCardSwipeGame), nameof(DoorCardSwipeGame.Begin))]
        public static class DoorCardSwipeGame_Begin_Patch
        {
            public static bool Prefix(DoorCardSwipeGame __instance)
            {
                if (!ElysiumModMenuGUI.autoOpenDoors) return true;
                try { ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Doors, (byte)(__instance.MyDoor.Id | 64)); } catch { }
                __instance.MyDoor.SetDoorway(true); __instance.Close();
                return false;
            }
        }

[HarmonyPatch(typeof(MushroomDoorSabotageMinigame), nameof(MushroomDoorSabotageMinigame.Begin))]
        public static class MushroomDoorSabotageMinigame_Begin_Patch
        {
            public static bool Prefix(MushroomDoorSabotageMinigame __instance) { if (ElysiumModMenuGUI.autoOpenDoors) { __instance.FixDoorAndCloseMinigame(); return false; } return true; }
        }

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.SetTasks))]
        public static class NoTaskMode_Patch { public static bool Prefix(PlayerControl __instance) { if (ElysiumModMenuGUI.noTaskMode) return false; return true; } }

[HarmonyPatch]
        public static class Console_CanUse_AllowTasksAsImpostor_Patch
        {
            public static MethodBase TargetMethod()
            {
                return AccessTools.Method(
                    typeof(global::Console),
                    nameof(global::Console.CanUse),
                    new[] { typeof(NetworkedPlayerInfo), typeof(bool).MakeByRefType(), typeof(bool).MakeByRefType() });
            }

            public static void Prefix(global::Console __instance, NetworkedPlayerInfo pc)
            {
                try
                {
                    if (__instance == null || pc == null) return;

                    bool isImpostor = ElysiumModMenuGUI.IsLocalImpostorRole(pc);
                    if (ElysiumModMenuGUI.allowTasksAsImpostor || !isImpostor)
                    {
                        __instance.AllowImpostor = true;
                        return;
                    }

                    int[] sabotageConsoleIds = { 0, 1, 2 };
                    if (!sabotageConsoleIds.Contains(__instance.ConsoleId))
                        __instance.AllowImpostor = false;
                }
                catch { }
            }

            public static void Postfix(global::Console __instance, NetworkedPlayerInfo pc, ref bool canUse, ref bool couldUse, ref float __result)
            {
                try
                {
                    if (!ElysiumModMenuGUI.allowTasksAsImpostor) return;
                    if (__instance == null || pc == null || pc.Object != PlayerControl.LocalPlayer) return;
                    if (!ElysiumModMenuGUI.IsLocalImpostorRole(pc)) return;

                    __instance.AllowImpostor = true;

                    if (!ElysiumModMenuGUI.LocalPlayerHasTaskForConsole(__instance))
                    {
                        canUse = false;
                        couldUse = false;
                        return;
                    }

                    if (couldUse)
                        __result = Mathf.Min(__result, ElysiumModMenuGUI.GetConsoleUsableDistance(__instance));
                }
                catch { }
            }
        }

[HarmonyPatch]
        public static class Console_Use_AllowTasksAsImpostor_Guard_Patch
        {
            public static IEnumerable<MethodBase> TargetMethods()
            {
                MethodInfo consoleUse = AccessTools.Method(typeof(global::Console), nameof(global::Console.Use), Type.EmptyTypes);
                if (consoleUse != null) yield return consoleUse;

                Type ventCleaningConsole = AccessTools.TypeByName("VentCleaningConsole");
                MethodInfo ventCleaningUse = ventCleaningConsole != null ? AccessTools.Method(ventCleaningConsole, nameof(global::Console.Use), Type.EmptyTypes) : null;
                if (ventCleaningUse != null) yield return ventCleaningUse;
            }

            public static bool Prefix(object __instance)
            {
                try
                {
                    if (__instance is global::Console console && ElysiumModMenuGUI.ShouldBlockUnsafeConsoleUse(console))
                        return false;
                }
                catch { }

                return true;
            }
        }

[HarmonyPatch(typeof(VentilationSystem), nameof(VentilationSystem.Update))]
        public static class VentilationSystem_Update_Immortality_Patch
        {
            public static bool Prefix(VentilationSystem.Operation op, int ventId)
            {
                try
                {
                    if (ventId != ElysiumModMenuGUI.ImmortalityCustomVentId &&
                        ElysiumModMenuGUI.roleBuffImmortality &&
                        ElysiumModMenuGUI.immortalityVentStateApplied &&
                        (op == VentilationSystem.Operation.Enter || op == VentilationSystem.Operation.Exit || op == VentilationSystem.Operation.Move))
                        return false;
                }
                catch { }

                return true;
            }
        }

[HarmonyPatch(typeof(GameManager), nameof(GameManager.StartGame))]
        public static class GameManager_StartGame_Immortality_Patch
        {
            public static void Postfix()
            {
                if (ElysiumModMenuGUI.roleBuffImmortality)
                    ElysiumModMenuGUI.SetImmortalityVentState(true);
            }
        }

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Close))]
        public static class MeetingHud_Close_Immortality_Patch
        {
            public static void Postfix()
            {
                try
                {
                    if (ElysiumModMenuGUI.roleBuffImmortality &&
                        PlayerControl.LocalPlayer != null &&
                        PlayerControl.LocalPlayer.Data != null &&
                        !PlayerControl.LocalPlayer.Data.IsDead)
                        ElysiumModMenuGUI.SetImmortalityVentState(true);
                }
                catch { }
            }
        }

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
        public static class PlayerControl_MurderPlayer_ImmortalityNotice_Patch
        {
            public static void Postfix(PlayerControl __instance, PlayerControl target)
            {
                try
                {
                    if (!ElysiumModMenuGUI.roleBuffImmortality || target != PlayerControl.LocalPlayer || __instance == null || __instance.Data == null) return;
                    ShowNotification($"<color=#66CCFF>[IMMORTALITY]</color> {__instance.Data.PlayerName} tried to kill you.");
                }
                catch { }
            }
        }
}
}
