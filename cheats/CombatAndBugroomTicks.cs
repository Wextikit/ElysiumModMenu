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
private void TryKillAuraTick()
        {
            if (!killAuraHostOnly)
            {
                killAuraTimer = 0f;
                return;
            }

            if (AmongUsClient.Instance == null || AmongUsClient.Instance.GameState != InnerNetClient.GameStates.Started) return;
            PlayerControl localPlayer = PlayerControl.LocalPlayer;
            if (localPlayer == null || localPlayer.Data == null || localPlayer.Data.Role == null) return;
            if (localPlayer.Data.IsDead) return;
            if (!RoleManager.IsImpostorRole(localPlayer.Data.RoleType) && !localPlayer.Data.Role.IsImpostor) return;
            if (MeetingHud.Instance != null) return;
            if (localPlayer.inVent || localPlayer.onLadder || localPlayer.inMovingPlat) return;

            bool hostCooldownBypass = AmongUsClient.Instance.AmHost && noKillCooldownHostOnly;
            if (!hostCooldownBypass && GetRemainingKillCooldown(localPlayer.PlayerId) > 0.05f) return;

            killAuraTimer += Time.deltaTime;
            if (killAuraTimer < 0.10f) return;

            if (PlayerControl.AllPlayerControls == null) return;

            ImpostorRole impostorRole = localPlayer.Data.Role as ImpostorRole;
            PlayerControl nearestTarget = FindClosestKillTarget(impostorRole, GetVanillaKillDistance());

            if (nearestTarget == null) return;

            try
            {
                killAuraTimer = 0f;
                localPlayer.CmdCheckMurder(nearestTarget);
            }
            catch { }
        }

private void TryHostAutoKillRandomTick()
        {
            if (!hostAutoKillRandom)
            {
                hostAutoKillTimer = 0f;
                return;
            }

            if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost || AmongUsClient.Instance.GameState != InnerNetClient.GameStates.Started) return;
            if (ShipStatus.Instance == null || LobbyBehaviour.Instance != null) return;
            if (IsMeetingOrExileActive() || IntroCutscene.Instance != null) return;

            PlayerControl localPlayer = PlayerControl.LocalPlayer;
            if (localPlayer == null || localPlayer.Data == null) return;
            if (PlayerControl.AllPlayerControls == null) return;

            hostAutoKillTimer += Time.deltaTime;
            if (hostAutoKillTimer < 0.125f) return;

            PlayerControl target = FindRandomHostAutoKillTarget(localPlayer);
            if (target == null) return;

            hostAutoKillTimer = 0f;
            TryHostElysiumMurderPlayer(target);
        }

private static PlayerControl FindRandomHostAutoKillTarget(PlayerControl localPlayer)
        {
            try
            {
                if (localPlayer == null || PlayerControl.AllPlayerControls == null) return null;

                List<PlayerControl> targets = new List<PlayerControl>();
                foreach (PlayerControl pc in PlayerControl.AllPlayerControls)
                {
                    if (pc == null || pc == localPlayer || pc.Data == null) continue;
                    if (pc.Data.Disconnected) continue;
                    targets.Add(pc);
                }

                if (targets.Count == 0) return null;
                return targets[UnityEngine.Random.Range(0, targets.Count)];
            }
            catch { return null; }
        }

private void TryHostAutoKillTargetTick()
        {
            if (!hostAutoKillTarget)
            {
                hostAutoKillTargetTimer = 0f;
                return;
            }

            if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost || AmongUsClient.Instance.GameState != InnerNetClient.GameStates.Started) return;
            if (ShipStatus.Instance == null || LobbyBehaviour.Instance != null) return;
            if (IsMeetingOrExileActive() || IntroCutscene.Instance != null) return;

            PlayerControl localPlayer = PlayerControl.LocalPlayer;
            if (localPlayer == null || localPlayer.Data == null) return;
            if (PlayerControl.AllPlayerControls == null) return;

            hostAutoKillTargetTimer += Time.deltaTime;
            if (hostAutoKillTargetTimer < 0.125f) return;

            PlayerControl target = FindHostAutoKillTarget(localPlayer);
            if (target == null) return;

            hostAutoKillTargetTimer = 0f;
            TryHostElysiumMurderPlayer(target);
        }

private static PlayerControl FindHostAutoKillTarget(PlayerControl localPlayer)
        {
            try
            {
                if (localPlayer == null || PlayerControl.AllPlayerControls == null) return null;
                foreach (PlayerControl pc in PlayerControl.AllPlayerControls)
                {
                    if (pc == null || pc == localPlayer || pc.Data == null) continue;
                    if (pc.Data.Disconnected || pc.Data.IsDead) continue;
                    if (pc.PlayerId == hostAutoKillTargetId) return pc;
                }
            }
            catch { }
            return null;
        }

private void TryBugRoomAutoAngelTick()
        {
            if (!bugRoomAutoAngel)
            {
                bugRoomAngelTimer = 0f;
                return;
            }

            if (!CanRunBugRoomNonHostTick()) return;

            PlayerControl local = PlayerControl.LocalPlayer;
            if (local == null || local.Data == null) return;

            GuardianAngelRole angel = local.Data.Role as GuardianAngelRole;
            if (angel == null) return;

            bugRoomAngelTimer += Time.deltaTime;
            if (bugRoomAngelTimer < 0.10f) return;

            PlayerControl target = null;
            try { target = angel.FindClosestTarget(); } catch { }
            if (!IsBugRoomAngelTarget(target, local))
                target = FindBugRoomAngelTarget(local);
            if (target == null) return;

            try
            {
                bugRoomAngelTimer = 0f;
                angel.cooldownSecondsRemaining = 0f;
                angel.SetPlayerTarget(target);

                AbilityButton btn = HudManager.Instance != null ? HudManager.Instance.AbilityButton : null;
                if (btn != null)
                {
                    btn.SetEnabled();
                    btn.SetCooldownFill(0f);
                    btn.DoClick();
                }
                else angel.UseAbility();
            }
            catch { }
        }

private void TryBugRoomAutoKillShieldTick()
        {
            if (!bugRoomAutoKillShield)
            {
                bugRoomShieldKillTimer = 0f;
                return;
            }

            if (!CanRunBugRoomNonHostTick()) return;

            PlayerControl local = PlayerControl.LocalPlayer;
            if (local == null || local.Data == null || local.Data.IsDead || local.Data.Role == null) return;
            if (!local.Data.Role.CanUseKillButton) return;

            PlayerControl target = FindBugRoomShieldKillTarget(local);
            if (target == null) return;

            bugRoomShieldKillTimer += Time.deltaTime;
            if (bugRoomShieldKillTimer < 0.13f) return;

            try
            {
                KillButton btn = HudManager.Instance != null ? HudManager.Instance.KillButton : null;
                if (btn == null) return;

                bugRoomShieldKillTimer = 0f;
                local.SetKillTimer(0f);
                btn.SetTarget(target);
                btn.SetCooldownFill(0f);
                btn.SetEnabled();
                btn.DoClick();
            }
            catch { }
        }

private void TryBugRoomTimedAutoRunTick()
        {
            if (!bugRoomTimedAutoRun)
            {
                bugRoomTimedAutoRunTimer = 0f;
                bugRoomTimedAutoRunDone = false;
                return;
            }

            if (AutoHostAutoRunEnabled)
            {
                bugRoomTimedAutoRunTimer = 0f;
                bugRoomTimedAutoRunDone = true;
                return;
            }

            if (!IsBugRoomTimedAutoRunInGame())
            {
                bugRoomTimedAutoRunTimer = 0f;
                bugRoomTimedAutoRunDone = false;
                return;
            }

            if (bugRoomTimedAutoRunDone) return;

            bugRoomTimedAutoRunTimer += Time.deltaTime;
            if (bugRoomTimedAutoRunTimer < Mathf.Clamp(bugRoomTimedAutoRunMinutes, 1, 60) * 60f) return;

            AutoHostAutoRunEnabled = true;
            bugRoomTimedAutoRunDone = true;
            bugRoomTimedAutoRunTimer = 0f;
            settingsDirty = true;
            ShowNotification("<color=#FF00FF>[BUG ROOM]</color> Auto Run 1.75 enabled.");
        }

private static bool IsBugRoomTimedAutoRunInGame()
        {
            try
            {
                if (AmongUsClient.Instance == null || AmongUsClient.Instance.GameState != InnerNetClient.GameStates.Started) return false;
                if (ShipStatus.Instance == null || LobbyBehaviour.Instance != null) return false;
                if (UnityEngine.Object.FindObjectOfType<EndGameManager>() != null) return false;
                return true;
            }
            catch { return false; }
        }

private static bool CanRunBugRoomNonHostTick()
        {
            try
            {
                if (AmongUsClient.Instance == null || AmongUsClient.Instance.AmHost) return false;
                if (AmongUsClient.Instance.GameState != InnerNetClient.GameStates.Started) return false;
                if (ShipStatus.Instance == null || LobbyBehaviour.Instance != null) return false;
                if (IsMeetingOrExileActive() || IntroCutscene.Instance != null) return false;
                return PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.Data != null;
            }
            catch { return false; }
        }

private static bool IsBugRoomAngelTarget(PlayerControl pc, PlayerControl local)
        {
            try
            {
                if (pc == null || pc == local || pc.Data == null) return false;
                if (pc.Data.Disconnected || pc.Data.IsDead) return false;
                if (pc.inVent || pc.onLadder || pc.inMovingPlat) return false;
                return pc.Visible;
            }
            catch { return false; }
        }

private static PlayerControl FindBugRoomAngelTarget(PlayerControl local)
        {
            try
            {
                if (local == null || PlayerControl.AllPlayerControls == null) return null;
                Vector3 lp = local.transform.position;
                PlayerControl best = null;
                float dist = float.MaxValue;
                foreach (PlayerControl pc in PlayerControl.AllPlayerControls)
                {
                    if (!IsBugRoomAngelTarget(pc, local)) continue;
                    if (pc.protectedByGuardianId >= 0) continue;

                    float d = Vector2.Distance(new Vector2(lp.x, lp.y), new Vector2(pc.transform.position.x, pc.transform.position.y));
                    if (d < dist)
                    {
                        dist = d;
                        best = pc;
                    }
                }
                return best;
            }
            catch { return null; }
        }

private static PlayerControl FindBugRoomShieldKillTarget(PlayerControl local)
        {
            try
            {
                if (local == null || PlayerControl.AllPlayerControls == null) return null;
                ImpostorRole role = local.Data.Role as ImpostorRole;
                Vector3 lp = local.transform.position;
                PlayerControl best = null;
                float dist = GetVanillaKillDistance();

                foreach (PlayerControl pc in PlayerControl.AllPlayerControls)
                {
                    if (pc == null || pc == local || pc.Data == null) continue;
                    if (pc.Data.Disconnected || pc.Data.IsDead) continue;
                    if (pc.protectedByGuardianId < 0) continue;
                    if (pc.inVent || pc.onLadder || pc.inMovingPlat || !pc.Visible) continue;
                    if (!killAnyone && IsImpostorTeamRole(pc.Data.RoleType)) continue;
                    if (!killAnyone && role != null && !role.IsValidTarget(pc.Data)) continue;

                    float d = Vector2.Distance(new Vector2(lp.x, lp.y), new Vector2(pc.transform.position.x, pc.transform.position.y));
                    if (d <= dist)
                    {
                        dist = d;
                        best = pc;
                    }
                }
                return best;
            }
            catch { return null; }
        }
    }
}
