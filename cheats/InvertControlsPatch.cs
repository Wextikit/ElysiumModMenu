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



[HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.FixedUpdate))]
public static class InvertControls_Patch
{
    private static void SeePlayerVent(PlayerPhysics player)
    {
        if (player == null || AmongUsClient.Instance == null ||
            AmongUsClient.Instance.GameState != InnerNetClient.GameStates.Started) return;

        PlayerControl controlledPlayer = player.myPlayer;
        if (controlledPlayer == null || controlledPlayer.Data == null || controlledPlayer.cosmetics == null) return;

        if (GameManager.Instance != null && GameManager.Instance.IsHideAndSeek() &&
            controlledPlayer.Data.RoleType == RoleTypes.Impostor) return;

        if (!SeePlayersInVent)
        {
            if (controlledPlayer.invisibilityAlpha == 0.3f)
            {
                PhantomRole? role = controlledPlayer.Data.Role as PhantomRole;
                if (role != null)
                {
                    controlledPlayer.SetInvisibility(role.isInvisible);
                    return;
                }
                else
                {
                    controlledPlayer.cosmetics.SetPhantomRoleAlpha(1f);
                    controlledPlayer.invisibilityAlpha = 1;
                    if (controlledPlayer.inVent)
                    {
                        controlledPlayer.Visible = false;
                    }
                }
            }
            return;
        }

        PlayerControl localPlayer = PlayerControl.LocalPlayer;
        PlayerPhysics localPhysics = localPlayer?.MyPhysics;
        if (controlledPlayer.inVent && (localPhysics == null || player.NetId != localPhysics.NetId))
        {
            controlledPlayer.Visible = true;
            controlledPlayer.invisibilityAlpha = 0.3f;
            controlledPlayer.cosmetics.SetPhantomRoleAlpha(0.3f);
        }
        else
        {
            PhantomRole? role = controlledPlayer.Data.Role as PhantomRole;
            if (role != null)
            {
                controlledPlayer.SetInvisibility(role.isInvisible);
            }
            else
            {
                controlledPlayer.cosmetics.SetPhantomRoleAlpha(1f);
                controlledPlayer.invisibilityAlpha = 1;
            }
        }
    }

    public static void Postfix(PlayerPhysics __instance)
    {
        if (__instance == null) return;

        try
        {
            if (__instance.AmOwner && ElysiumModMenuGUI.invertControls && __instance.body != null)
                __instance.body.velocity = -__instance.body.velocity;

            SeePlayerVent(__instance);
            ElysiumModMenuGUI.PlayerControl_SetRoleInvisibility_SeePhantoms_Patch.ApplyVisiblePhantom(__instance.myPlayer);
        }
        catch { }
    }
}
