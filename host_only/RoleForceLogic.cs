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
using System.Globalization;
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
private static void ClearAutoTwoImpostorSelection()
        {
            try
            {
                foreach (byte playerId in autoTwoImpostorPlayerIds.ToArray())
                {
                    PlayerControl pc = FindPlayerById(playerId);
                    string fc = GetRoleForceKey(pc);
                    if (!string.IsNullOrEmpty(fc)) forcedImpostorFcs.Remove(fc);
                    forcedImpostors.Remove(playerId);
                }
                autoTwoImpostorPlayerIds.Clear();
            }
            catch { }
        }

public static string GetRoleForceKey(PlayerControl pc)
        {
            try
            {
                string fc = pc?.Data?.FriendCode;
                if (string.IsNullOrWhiteSpace(fc) && pc != null && AmongUsClient.Instance != null)
                {
                    ClientData cd = AmongUsClient.Instance.GetClient(pc.OwnerId);
                    if (cd != null) fc = cd.FriendCode;
                }

                if (string.IsNullOrWhiteSpace(fc)) return string.Empty;
                fc = fc.Trim();
                if (fc.Equals("unknown", System.StringComparison.OrdinalIgnoreCase) || fc == "----") return string.Empty;
                return fc;
            }
            catch { return string.Empty; }
        }

private static PlayerControl FindPlayerById(byte id)
        {
            try
            {
                if (PlayerControl.AllPlayerControls == null) return null;
                foreach (PlayerControl pc in PlayerControl.AllPlayerControls)
                    if (pc != null && pc.PlayerId == id)
                        return pc;
            }
            catch { }
            return null;
        }

public static bool IsForcedImp(PlayerControl pc)
        {
            if (pc == null) return false;
            string fc = GetRoleForceKey(pc);
            if (!string.IsNullOrEmpty(fc) && forcedImpostorFcs.Contains(fc)) return true;
            return forcedImpostors.Contains(pc.PlayerId);
        }

public static bool TryGetForcedRole(PlayerControl pc, out RoleTypes role)
        {
            role = RoleTypes.Crewmate;
            if (pc == null) return false;
            string fc = GetRoleForceKey(pc);
            if (!string.IsNullOrEmpty(fc) && forcedPreGameRoleFcs.TryGetValue(fc, out role)) return true;
            return forcedPreGameRoles.TryGetValue(pc.PlayerId, out role);
        }

private static void SetForcedImp(PlayerControl pc)
        {
            if (pc == null) return;
            string fc = GetRoleForceKey(pc);
            if (!string.IsNullOrEmpty(fc))
            {
                forcedPreGameRoleFcs.Remove(fc);
                forcedImpostorFcs.Add(fc);
            }
            else
            {
                forcedPreGameRoles.Remove(pc.PlayerId);
                forcedImpostors.Add(pc.PlayerId);
            }
        }

private static void SetForcedRole(PlayerControl pc, RoleTypes role)
        {
            if (pc == null) return;
            string fc = GetRoleForceKey(pc);
            if (!string.IsNullOrEmpty(fc))
            {
                forcedImpostorFcs.Remove(fc);
                forcedPreGameRoleFcs[fc] = role;
            }
            else
            {
                forcedImpostors.Remove(pc.PlayerId);
                forcedPreGameRoles[pc.PlayerId] = role;
            }
        }

private static void ClearForcedRole(PlayerControl pc)
        {
            if (pc == null) return;
            string fc = GetRoleForceKey(pc);
            if (!string.IsNullOrEmpty(fc))
            {
                forcedImpostorFcs.Remove(fc);
                forcedPreGameRoleFcs.Remove(fc);
            }

            forcedImpostors.Remove(pc.PlayerId);
            forcedPreGameRoles.Remove(pc.PlayerId);
        }

private static bool IsForced(PlayerControl pc)
        {
            if (pc == null) return false;
            return IsForcedImp(pc) || TryGetForcedRole(pc, out _);
        }

public static List<byte> GetForcedImpostorIdsByFc()
        {
            List<byte> result = new List<byte>();
            try
            {
                if (PlayerControl.AllPlayerControls == null) return result;
                foreach (PlayerControl pc in PlayerControl.AllPlayerControls)
                {
                    if (pc == null || pc.Data == null || pc.Data.Disconnected || pc.PlayerId >= 100) continue;
                    if (IsForcedImp(pc) || (TryGetForcedRole(pc, out RoleTypes role) && IsImpostorTeamRole(role)))
                    {
                        if (!result.Contains(pc.PlayerId)) result.Add(pc.PlayerId);
                    }
                }
            }
            catch { }
            return result;
        }

private static int GetAutoTwoImpostorLobbyFingerprint(List<PlayerControl> activePlayers)
        {
            if (activePlayers == null || activePlayers.Count == 0) return 0;

            int hash = 17;
            foreach (byte playerId in activePlayers.Select(p => p.PlayerId).OrderBy(id => id))
                hash = unchecked(hash * 31 + playerId);
            return unchecked(hash * 31 + activePlayers.Count);
        }

private static List<PlayerControl> GetAutoTwoImpostorCandidates()
        {
            try
            {
                if (PlayerControl.AllPlayerControls == null) return new List<PlayerControl>();
                return PlayerControl.AllPlayerControls.ToArray()
                    .Where(p => p != null && p.Data != null && !p.Data.Disconnected && p.PlayerId < 100)
                    .ToList();
            }
            catch
            {
                return new List<PlayerControl>();
            }
        }

private static bool RollAutoTwoImpostors(bool forceNewRoll)
        {
            try
            {
                List<PlayerControl> activePlayers = GetAutoTwoImpostorCandidates();
                int fingerprint = GetAutoTwoImpostorLobbyFingerprint(activePlayers);
                if (!forceNewRoll &&
                    !autoTwoImpostorsNeedsGameStartRoll &&
                    autoTwoImpostorPlayerIds.Count == 2 &&
                    autoTwoImpostorsLastLobbyFingerprint == fingerprint)
                    return true;

                forcedPreGameRoles.Clear();
                forcedImpostors.Clear();
                forcedPreGameRoleFcs.Clear();
                forcedImpostorFcs.Clear();
                autoTwoImpostorPlayerIds.Clear();
                autoTwoImpostorsLastLobbyFingerprint = fingerprint;

                if (activePlayers.Count < 2)
                {
                    enablePreGameRoleForce = false;
                    return false;
                }

                for (int i = activePlayers.Count - 1; i > 0; i--)
                {
                    int swapIndex = UnityEngine.Random.Range(0, i + 1);
                    PlayerControl temp = activePlayers[i];
                    activePlayers[i] = activePlayers[swapIndex];
                    activePlayers[swapIndex] = temp;
                }

                for (int i = 0; i < 2; i++)
                {
                    byte playerId = activePlayers[i].PlayerId;
                    SetForcedImp(activePlayers[i]);
                    autoTwoImpostorPlayerIds.Add(playerId);
                }

                enablePreGameRoleForce = true;
                return true;
            }
            catch
            {
                return false;
            }
        }

private static void TickAutoTwoImpostors()
        {
            try
            {
                if (!autoTwoImpostors)
                {
                    autoTwoImpostorsNeedsGameStartRoll = true;
                    autoTwoImpostorsWasGameStarted = false;
                    autoTwoImpostorsLastLobbyFingerprint = 0;
                    return;
                }

                if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
                    return;

                bool isGameStarted = AmongUsClient.Instance.IsGameStarted;
                if (isGameStarted)
                {
                    autoTwoImpostorsWasGameStarted = true;
                    return;
                }

                if (autoTwoImpostorsWasGameStarted)
                {
                    autoTwoImpostorsWasGameStarted = false;
                    autoTwoImpostorsNeedsGameStartRoll = true;
                    autoTwoImpostorsLastLobbyFingerprint = 0;
                    ClearAutoTwoImpostorSelection();
                }

                if (Time.unscaledTime < nextAutoTwoImpostorsLobbyCheckAt)
                    return;

                nextAutoTwoImpostorsLobbyCheckAt = Time.unscaledTime + 0.5f;
                List<PlayerControl> activePlayers = GetAutoTwoImpostorCandidates();
                int fingerprint = GetAutoTwoImpostorLobbyFingerprint(activePlayers);
                if (autoTwoImpostorPlayerIds.Count != 2 || autoTwoImpostorsLastLobbyFingerprint != fingerprint)
                    RollAutoTwoImpostors(true);
            }
            catch { }
        }

private static void EnsureAutoTwoImpostorsForRoleSelection()
        {
            if (!autoTwoImpostors) return;
            if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return;

            if (RollAutoTwoImpostors(true))
                autoTwoImpostorsNeedsGameStartRoll = false;
        }
    }
}
