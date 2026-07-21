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
                    if (pc.Data.Disconnected) continue;
                    if (pc.PlayerId == hostAutoKillTargetId) return pc;
                }
            }
            catch { }
            return null;
        }

    }
}
