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

private static byte runtimeHideAndSeekSeekerId = byte.MaxValue;

private static bool IsHideAndSeekMode()
        {
            try
            {
                if (GameManager.Instance != null && GameManager.Instance.IsHideAndSeek())
                    return true;
            }
            catch { }

            try
            {
                return GameOptionsManager.Instance != null &&
                       GameOptionsManager.Instance.CurrentGameOptions != null &&
                       GameOptionsManager.Instance.CurrentGameOptions.GameMode == GameModes.HideNSeek;
            }
            catch { return false; }
        }

private static List<byte> GetForcedImpostorPlayerIds()
        {
            List<byte> result = new List<byte>();

            try
            {
                if (PlayerControl.AllPlayerControls != null)
                {
                    foreach (PlayerControl player in PlayerControl.AllPlayerControls)
                    {
                        if (player == null || player.Data == null || player.Data.Disconnected) continue;
                        byte playerId = player.PlayerId;
                        if (IsForcedImp(player) ||
                            (TryGetForcedRole(player, out RoleTypes role) && IsImpostorTeamRole(role)))
                        {
                            if (!result.Contains(playerId))
                                result.Add(playerId);
                        }
                    }
                }
            }
            catch { }

            return result;
        }

private static bool TryGetForcedHideAndSeekSeekerId(out byte seekerId)
        {
            seekerId = byte.MaxValue;
            bool isHideAndSeek = IsHideAndSeekMode();
            if (!enablePreGameRoleForce && !isHideAndSeek)
                return false;

            List<byte> forcedIds = GetForcedImpostorPlayerIds();
            if (forcedIds.Count > 0)
            {
                if (runtimeHideAndSeekSeekerId != byte.MaxValue &&
                    forcedIds.Contains(runtimeHideAndSeekSeekerId) &&
                    IsPlayerIdActive(runtimeHideAndSeekSeekerId))
                {
                    seekerId = runtimeHideAndSeekSeekerId;
                    return true;
                }

                seekerId = forcedIds[0];
                return true;
            }

            if (isHideAndSeek && runtimeHideAndSeekSeekerId != byte.MaxValue && IsPlayerIdActive(runtimeHideAndSeekSeekerId))
            {
                seekerId = runtimeHideAndSeekSeekerId;
                return true;
            }

            return false;
        }

private static void SetHideAndSeekSeekerOption(byte seekerId, int impostorCount = 1)
        {
            try
            {
                runtimeHideAndSeekSeekerId = seekerId;
                impostorCount = Math.Max(1, impostorCount);
                object options = GameOptionsManager.Instance?.CurrentGameOptions;
                if (options == null) return;

                const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                Type type = options.GetType();

                PropertyInfo impostorIdProperty = type.GetProperty("ImpostorPlayerID", flags);
                if (impostorIdProperty != null && impostorIdProperty.CanWrite)
                    impostorIdProperty.SetValue(options, (int)seekerId, null);

                FieldInfo impostorIdField = type.GetField("ImpostorPlayerID", flags);
                if (impostorIdField != null)
                    impostorIdField.SetValue(options, (int)seekerId);

                PropertyInfo numImpostorsProperty = type.GetProperty("NumImpostors", flags);
                if (numImpostorsProperty != null && numImpostorsProperty.CanWrite)
                    numImpostorsProperty.SetValue(options, impostorCount, null);

                FieldInfo numImpostorsField = type.GetField("_NumImpostors_k__BackingField", flags);
                if (numImpostorsField != null)
                    numImpostorsField.SetValue(options, impostorCount);
            }
            catch { }
        }

private static bool IsPlayerIdActive(byte playerId)
        {
            try
            {
                if (PlayerControl.AllPlayerControls == null) return false;
                foreach (PlayerControl player in PlayerControl.AllPlayerControls)
                {
                    if (player != null && player.PlayerId == playerId && player.Data != null && !player.Data.Disconnected)
                        return true;
                }
            }
            catch { }

            return false;
        }

private static void RefreshRoleBehaviour(PlayerControl target)
        {
            try
            {
                if (target == null || target.Data == null) return;
                target.Data.Role?.Initialize(target);
                if (IsImpostorTeamRole(target.Data.RoleType))
                    target.SetKillTimer(0f);
            }
            catch { }
        }
    }
}
