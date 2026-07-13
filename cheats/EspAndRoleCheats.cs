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
                ElysiumModMenuGUI.EnsureAutoTwoImpostorsForRoleSelection();
                if (!ElysiumModMenuGUI.enablePreGameRoleForce || !AmongUsClient.Instance.AmHost) return true;
                try
                {
                    var allPlayers = PlayerControl.AllPlayerControls.ToArray().Where(p => p != null && p.Data != null && !p.Data.Disconnected && !p.Data.IsDead).ToList();
                    int numImps = 1;
                    try { numImps = GameOptionsManager.Instance.CurrentGameOptions.GetInt((Int32OptionNames)1); } catch { }
                    var impRoleTypes = new HashSet<int> { 1, 5, 9, 18 };
                    List<PlayerControl> impostors = new List<PlayerControl>();
                    foreach (var p in allPlayers)
                        if (ElysiumModMenuGUI.IsForcedImp(p) || (ElysiumModMenuGUI.TryGetForcedRole(p, out RoleTypes forcedRole) && impRoleTypes.Contains((int)forcedRole)))
                            impostors.Add(p);

                    if (ElysiumModMenuGUI.IsHideAndSeekMode() && ElysiumModMenuGUI.TryGetForcedHideAndSeekSeekerId(out byte seekerId))
                    {
                        var seeker = allPlayers.FirstOrDefault(p => p.PlayerId == seekerId);
                        if (seeker != null)
                        {
                            if (!impostors.Contains(seeker))
                                impostors.Insert(0, seeker);
                            numImps = impostors.Count > 0 ? impostors.Count : 1;
                            ElysiumModMenuGUI.SetHideAndSeekSeekerOption(seekerId, numImps);
                        }
                    }
                    else if (impostors.Count > 0) numImps = impostors.Count;
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
                    foreach (var pc in allPlayers)
                    {
                        if (ElysiumModMenuGUI.TryGetForcedRole(pc, out RoleTypes role) && role != RoleTypes.Crewmate && role != RoleTypes.Impostor)
                        {
                            RoleManager.Instance.SetRole(pc, role);
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
            private struct ProtectionVisualState
            {
                public int ColorId;
                public int GuardianPlayerId;
            }

            private static readonly Dictionary<byte, ProtectionVisualState> protectionVisuals = new Dictionary<byte, ProtectionVisualState>();

            public static void Prefix(PlayerControl __instance, ref bool visible, int colorId, int guardianPlayerId)
            {
                if (__instance != null)
                {
                    protectionVisuals[__instance.PlayerId] = new ProtectionVisualState
                    {
                        ColorId = colorId,
                        GuardianPlayerId = guardianPlayerId
                    };
                }

                if (ElysiumModMenuGUI.seeGhosts || ElysiumModMenuGUI.seeProtections) visible = true;
            }

            public static void RefreshVisibleProtections()
            {
                if (!ElysiumModMenuGUI.seeProtections || PlayerControl.AllPlayerControls == null) return;

                foreach (PlayerControl player in PlayerControl.AllPlayerControls)
                {
                    if (player == null || player.Data == null || player.protectedByGuardianId < 0) continue;

                    try
                    {
                        bool alreadyVisible = false;
                        if (player.currentRoleAnimations != null)
                        {
                            foreach (RoleEffectAnimation animation in player.currentRoleAnimations)
                            {
                                if (animation != null && animation.effectType == RoleEffectAnimation.EffectType.ProtectLoop)
                                {
                                    alreadyVisible = true;
                                    break;
                                }
                            }
                        }
                        if (alreadyVisible) continue;

                        ProtectionVisualState state;
                        if (!protectionVisuals.TryGetValue(player.PlayerId, out state))
                        {
                            int fallbackColor = 0;
                            try { fallbackColor = player.Data.DefaultOutfit.ColorId; } catch { }
                            state = new ProtectionVisualState
                            {
                                ColorId = fallbackColor,
                                GuardianPlayerId = player.protectedByGuardianId
                            };
                        }

                        player.TurnOnProtection(true, state.ColorId, state.GuardianPlayerId);
                    }
                    catch { }
                }
            }
        }

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.SetRoleInvisibility))]
        public static class PlayerControl_SetRoleInvisibility_SeePhantoms_Patch
        {
            private static readonly HashSet<byte> vanishedPlayers = new HashSet<byte>();

            public static void Postfix(PlayerControl __instance, bool isActive)
            {
                if (__instance == null) return;
                if (isActive) vanishedPlayers.Add(__instance.PlayerId);
                else vanishedPlayers.Remove(__instance.PlayerId);
            }

            public static bool IsVanished(PlayerControl player)
            {
                if (player == null || player.Data == null) return false;

                bool isPhantom = (int)player.Data.RoleType == 9 || player.Data.Role is PhantomRole;
                if (!isPhantom) return false;

                if (vanishedPlayers.Contains(player.PlayerId) || player.shouldAppearInvisible) return true;
                if (player.Data.Role is PhantomRole phantom)
                    return phantom.fading || phantom.isInvisible || phantom.IsInvisible || phantom.IsFading;
                return false;
            }

            public static void ApplyVisiblePhantom(PlayerControl player)
            {
                if (player == null || player.Data == null || player.cosmetics == null) return;
                if (PlayerControl.LocalPlayer != null && player.PlayerId == PlayerControl.LocalPlayer.PlayerId) return;

                bool vanished = IsVanished(player);
                if (vanished && ElysiumModMenuGUI.seePhantoms)
                {
                    player.Visible = true;
                    player.invisibilityAlpha = 0.5f;
                    player.cosmetics.Visible = true;
                    player.cosmetics.SetPhantomRoleAlpha(0.5f);
                }
                else if (vanished && !ElysiumModMenuGUI.seePhantoms && player.invisibilityAlpha == 0.5f)
                {
                    player.SetInvisibility(true);
                }
            }
        }

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Visible), MethodType.Setter)]
        public static class PlayerControl_Visible_SeePhantoms_Patch
        {
            public static void Prefix(PlayerControl __instance, ref bool __0)
            {
                if (ElysiumModMenuGUI.seePhantoms &&
                    PlayerControl_SetRoleInvisibility_SeePhantoms_Patch.IsVanished(__instance) &&
                    (PlayerControl.LocalPlayer == null || __instance.PlayerId != PlayerControl.LocalPlayer.PlayerId))
                {
                    __0 = true;
                }
            }
        }

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.invisibilityAlpha), MethodType.Setter)]
        public static class PlayerControl_InvisibilityAlpha_SeePhantoms_Patch
        {
            public static void Prefix(PlayerControl __instance, ref float __0)
            {
                if (ElysiumModMenuGUI.seePhantoms &&
                    PlayerControl_SetRoleInvisibility_SeePhantoms_Patch.IsVanished(__instance) &&
                    (PlayerControl.LocalPlayer == null || __instance.PlayerId != PlayerControl.LocalPlayer.PlayerId))
                {
                    __0 = 0.5f;
                }
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
                    PlayerControl_SetRoleInvisibility_SeePhantoms_Patch.ApplyVisiblePhantom(__instance.myPlayer);

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
                        string espName = ElysiumModMenuGUI.GetESPNameTag(__instance.myPlayer.Data, outfit.PlayerName);
                        if (!string.Equals(cosmetics.nameText.text, espName, StringComparison.Ordinal))
                            cosmetics.SetName(espName);
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
                            if (!string.Equals(state.NameText.text, espName, StringComparison.Ordinal))
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
                AddInfo(__instance);
            }

            internal static void AddInfo(ChatBubble bubble)
            {
                if (!ElysiumModMenuGUI.showPlayerInfo || bubble == null || bubble.playerInfo == null || bubble.NameText == null) return;
                try
                {
                    string accentHex = ElysiumModMenuGUI.GetEspAccentHex(bubble.playerInfo.PlayerId);
                    string espLine = ElysiumModMenuGUI.BuildESPInfoLine(bubble.playerInfo, 9, false);
                    if (string.IsNullOrWhiteSpace(espLine)) return;
                    string extra = $" <color=#{accentHex}><size=80%>{espLine}</size></color>";

                    if (!bubble.NameText.text.Contains("Lv:")) bubble.NameText.text += extra;
                }
                catch { }
            }
        }

[HarmonyPatch(typeof(ChatBubble), nameof(ChatBubble.SetText))]
        public static class ChatBubble_SetText_Patch
        {
            public static void Postfix(ChatBubble __instance)
            {
                ChatBubble_SetName_Patch.AddInfo(__instance);
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
                    bool hudModalActive = ElysiumModMenuGUI.IsHudModalActive();

                    if (__instance == null || __instance.ShadowQuad == null || __instance.ShadowQuad.gameObject == null) return;
                    if (hudModalActive) return;

                    __instance.ShadowQuad.gameObject.SetActive(
                        !ElysiumModMenuGUI.fullBright && !ElysiumModMenuGUI.cameraZoom);
                }
                catch { }
            }
        }

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
        public static class HudManager_Update_Patch
        {
            public static bool Prefix(HudManager __instance)
            {
                try
                {
                    if (__instance == null) return false;
                    if (Screen.width < 640 || Screen.height < 360) return false;
                }
                catch { return false; }

                return true;
            }

            public static Exception Finalizer(Exception __exception)
            {
                if (__exception == null) return null;
                if (IsHudUpdateNullRef(__exception)) return null;
                if (Screen.width < 640 || Screen.height < 360) return null;
                return __exception;
            }

            private static bool IsHudUpdateNullRef(Exception ex)
            {
                try
                {
                    string s = (ex.GetType().Name + " " + ex.Message).ToLowerInvariant();
                    return s.Contains("nullreferenceexception") || s.Contains("object reference not set");
                }
                catch { return false; }
            }

            public static void Postfix(HudManager __instance)
            {
                try
                {
                    if (__instance == null) return;

                    bool hudModalActive = ElysiumModMenuGUI.IsHudModalActive();

                    if (!hudModalActive && ElysiumModMenuGUI.alwaysChat && __instance.Chat != null && __instance.Chat.gameObject != null)
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

                        if (!hudModalActive && ElysiumModMenuGUI.cameraZoom && ElysiumModMenuGUI.hudZoomBaseCaptured)
                        {
                            Vector3 distance = ElysiumModMenuGUI.hudZoomBaseDistance;
                            distance.y = ElysiumModMenuGUI.hudZoomBaseDistance.y + 3f * (camSize - 3f);
                            ElysiumModMenuGUI.TrySetAspectDistance(aspectPosition, distance);
                        }
                        else if (hudModalActive && ElysiumModMenuGUI.hudZoomBaseCaptured)
                        {
                            ElysiumModMenuGUI.TrySetAspectDistance(aspectPosition, ElysiumModMenuGUI.hudZoomBaseDistance);
                        }
                    }

                    if (!hudModalActive && ElysiumModMenuGUI.cameraZoom && __instance.TaskPanel != null && __instance.TaskPanel.gameObject != null && ShipStatus.Instance != null && MeetingHud.Instance == null)
                        __instance.TaskPanel.gameObject.SetActive(true);
                }
                catch { }
            }
        }

[HarmonyPatch(typeof(FullAccount), nameof(FullAccount.CanSetCustomName))]
        public static class FullAccount_CanSetCustomName_Patch { public static void Prefix(ref bool canSetName) { try { if (ElysiumModMenuGUI.unlockFeatures || ElysiumModMenuGUI.guestExtraFeatures) canSetName = true; } catch { } } }

[HarmonyPatch(typeof(AccountManager), nameof(AccountManager.CanPlayOnline))]
        public static class AccountManager_CanPlayOnline_Patch { public static void Postfix(ref bool __result) { try { if (ElysiumModMenuGUI.unlockFeatures || ElysiumModMenuGUI.bypassAgeRestrictions) __result = true; } catch { } } }

[HarmonyPatch(typeof(EOSManager), "IsFreechatAllowed")]
        public static class EOSManager_IsFreechatAllowed_Patch { public static void Postfix(ref bool __result) { try { if (ElysiumModMenuGUI.guestExtraFeatures) __result = true; } catch { } } }

[HarmonyPatch(typeof(EOSManager), "IsFriendsListAllowed")]
        public static class EOSManager_IsFriendsListAllowed_Patch { public static void Postfix(ref bool __result) { try { if (ElysiumModMenuGUI.guestExtraFeatures) __result = true; } catch { } } }

[HarmonyPatch(typeof(EOSManager), "IsMinorOrWaiting")]
        public static class EOSManager_IsMinorOrWaiting_Patch { public static void Postfix(ref bool __result) { try { if (ElysiumModMenuGUI.bypassAgeRestrictions) __result = false; } catch { } } }

[HarmonyPatch(typeof(EOSManager), "IsAllowedOnline")]
        public static class EOSManager_IsAllowedOnline_Patch { public static void Prefix(ref bool canOnline) { try { if (ElysiumModMenuGUI.bypassAgeRestrictions) canOnline = true; } catch { } } }

[HarmonyPatch(typeof(InnerNetClient), "JoinGame")]
        public static class InnerNetClient_JoinGame_AccountReady_Patch
        {
            public static void Prefix()
            {
                try
                {
                    if (!ElysiumModMenuGUI.bypassAgeRestrictions) return;
                    if (AmongUs.Data.DataManager.Player == null || AmongUs.Data.DataManager.Player.Account == null) return;
                    AmongUs.Data.DataManager.Player.Account.LoginStatus = (EOSManager.AccountLoginStatus)1;
                }
                catch { }
            }
        }

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
                    float cooldown = ElysiumModMenuGUI.GetConfiguredKillCooldown();
                    if (target != null && target.protectedByGuardianId >= 0) cooldown *= 0.5f;
                    __instance.killTimer = Mathf.Max(0f, cooldown);

                    if (!ElysiumModMenuGUI.spamReportBodies) return;
                    if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data == null || PlayerControl.LocalPlayer.Data.IsDead) return;
                    if (target == null || target.Data == null || !target.Data.IsDead) return;
                    if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return;

                    ElysiumModMenuGUI.TryOpenModdedMeeting(PlayerControl.LocalPlayer, target.Data);
                }
                catch { }
            }
        }

[HarmonyPatch(typeof(SabotageSystemType), nameof(SabotageSystemType.SetInitialSabotageCooldown))]
        public static class SabotageSystemType_InitialKillCooldown_Patch
        {
            public static void Postfix()
            {
                try
                {
                    if (PlayerControl.AllPlayerControls == null) return;
                    foreach (PlayerControl player in PlayerControl.AllPlayerControls)
                    {
                        if (player == null || player == PlayerControl.LocalPlayer || player.Data == null || player.Data.Disconnected || player.Data.IsDead) continue;
                        if (player.Data.Role == null || !player.Data.Role.CanUseKillButton) continue;
                        player.killTimer = 10f;
                    }
                }
                catch { }
            }
        }

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
        public static class PlayerControl_RemoteKillCooldown_Patch
        {
            public static void Postfix(PlayerControl __instance)
            {
                try
                {
                    if (__instance == null || __instance == PlayerControl.LocalPlayer || __instance.Data == null || __instance.Data.IsDead) return;
                    if (__instance.Data.Role == null || !__instance.Data.Role.CanUseKillButton) return;
                    if (__instance.ForceKillTimerContinue || __instance.IsKillTimerEnabled)
                        __instance.killTimer = Mathf.Max(0f, __instance.killTimer - Time.fixedDeltaTime);
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
        public static class ImpostorKillAnyoneTargetPatch
        {
            public static void Postfix(ImpostorRole __instance, ref PlayerControl __result)
            {
                try
                {
                    if (!ElysiumModMenuGUI.killAnyone || ElysiumModMenuGUI.killReach) return;
                    __result = ElysiumModMenuGUI.FindClosestKillTarget(__instance, ElysiumModMenuGUI.GetVanillaKillDistance());
                }
                catch { }
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

                    __result = ElysiumModMenuGUI.IsElysiumValidKillTarget(target);
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
                    if (target == null || target.Data == null || !ElysiumModMenuGUI.IsElysiumValidKillTarget(target.Data)) return true;

                    PlayerControl.LocalPlayer.RpcMurderPlayer(target, true);
                    return false;
                }
                catch { return true; }
            }
        }

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CmdCheckMurder))]
        public static class PlayerControl_CmdCheckMurder_KillAnyone_Patch
        {
            public static bool Prefix(PlayerControl __instance, PlayerControl target)
            {
                try
                {
                    if (!ElysiumModMenuGUI.killAnyone || __instance != PlayerControl.LocalPlayer) return true;
                    if (target == null || target.Data == null || !ElysiumModMenuGUI.IsElysiumValidKillTarget(target.Data)) return true;
                    if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return true;
                    if (ElysiumModMenuGUI.killWhileVanishedHostOnly && ElysiumModMenuGUI.IsLocalPhantomVanished()) return true;

                    __instance.RpcMurderPlayer(target, true);
                    return false;
                }
                catch { return true; }
            }
        }

[HarmonyPatch(typeof(ImpostorRole), "IsValidTarget")]
        public static class ImpostorKillAnyonePatch
        {
            public static void Postfix(NetworkedPlayerInfo target, ref bool __result) { try { if (ElysiumModMenuGUI.killAnyone) __result = ElysiumModMenuGUI.IsElysiumValidKillTarget(target); } catch { } }
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

        // No Task Mode is implemented by setting the host's task counts to zero.
        // Never suppress SetTasks itself: clients must accept the authoritative
        // server task list or the round-start synchronization remains incomplete.
        public static class NoTaskMode_Patch { public static bool Prefix(PlayerControl __instance) { return true; } }

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
