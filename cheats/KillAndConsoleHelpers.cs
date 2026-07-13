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

private static bool IsElysiumValidKillTarget(NetworkedPlayerInfo target)
        {
            try
            {
                if (target == null || target.Object == null || target.Role == null) return false;
                if (target.Disconnected || target.PlayerId == PlayerControl.LocalPlayer.PlayerId) return false;

                bool baseRequirements = target.Object.Visible &&
                                        !target.IsDead &&
                                        !target.Object.inVent &&
                                        !target.Object.onLadder &&
                                        !target.Object.inMovingPlat;
                if (!baseRequirements) return false;
                if (killAnyone) return true;

                return target.Role.CanBeKilled;
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

public static bool TryHostElysiumMurderPlayer(PlayerControl target)
        {
            try
            {
                if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return false;
                PlayerControl local = PlayerControl.LocalPlayer;
                if (local == null || target == null) return false;

                if (AmongUsClient.Instance.NetworkMode == NetworkModes.FreePlay)
                {
                    local.MurderPlayer(target, MurderResultFlags.Succeeded);
                    return true;
                }

                if (PlayerControl.AllPlayerControls == null) return false;
                foreach (PlayerControl receiver in PlayerControl.AllPlayerControls)
                {
                    if (receiver == null) continue;
                    MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(
                        local.NetId,
                        (byte)RpcCalls.MurderPlayer,
                        SendOption.Reliable,
                        AmongUsClient.Instance.GetClientIdFromCharacter(receiver));
                    writer.WriteNetObject(target);
                    writer.Write((int)MurderResultFlags.Succeeded);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);
                }

                return true;
            }
            catch { return false; }
        }

private static bool TelekillPlayerPPM(PlayerControl target)
        {
            try
            {
                PlayerControl local = PlayerControl.LocalPlayer;
                if (local == null || target == null || local.NetTransform == null) return false;

                Vector3 old = local.transform.position;
                local.NetTransform.RpcSnapTo(target.transform.position);

                bool ok;
                if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
                    ok = TryHostElysiumMurderPlayer(target);
                else
                {
                    local.CmdCheckMurder(target);
                    ok = true;
                }

                local.NetTransform.RpcSnapTo(old);
                return ok;
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
                    if (pc.inVent || pc.onLadder || pc.inMovingPlat) continue;
                    if (!killAnyone && IsImpostorTeamRole(pc.Data.RoleType)) continue;
                    if (!killAnyone && role != null && !role.IsValidTarget(pc.Data)) continue;

                    Vector3 targetWorld = pc.transform.position;
                    Vector2 targetPos = new Vector2(targetWorld.x, targetWorld.y);
                    float distance = Vector2.Distance(localPos, targetPos);
                    if (local.Collider != null && PhysicsHelpers.AnythingBetween(local.Collider, localPos, targetPos, Constants.ShipOnlyMask, false)) continue;
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
    }
}
