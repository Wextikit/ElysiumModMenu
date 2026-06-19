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
        if (GameManager.Instance.IsHideAndSeek() && player.myPlayer.Data.RoleType == RoleTypes.Impostor || player == null ||
            AmongUsClient.Instance.GameState != InnerNetClient.GameStates.Started)
            return;
        if (!SeePlayersInVent)
        {
            if (player.myPlayer.invisibilityAlpha == 0.3f)
            {
                PhantomRole? role = player.myPlayer.Data.Role as PhantomRole;
                if (role != null)
                {
                    player.myPlayer.SetInvisibility(role.isInvisible);
                    return;
                }
                else
                {
                    player.myPlayer.cosmetics.SetPhantomRoleAlpha(1f);
                    player.myPlayer.invisibilityAlpha = 1;
                    if (player.myPlayer.inVent)
                    {
                        player.myPlayer.Visible = false;
                    }
                }
            }
            return;
        }

        if (player.myPlayer.inVent && player.NetId != PlayerControl.LocalPlayer.MyPhysics.NetId)
        {
            player.myPlayer.Visible = true;
            player.myPlayer.invisibilityAlpha = 0.3f;
            player.myPlayer.cosmetics.SetPhantomRoleAlpha(0.3f);
        }
        else
        {
            PhantomRole? role = player.myPlayer.Data.Role as PhantomRole;
            if (role != null)
            {
                player.myPlayer.SetInvisibility(role.isInvisible);
            }
            else
            {
                player.myPlayer.cosmetics.SetPhantomRoleAlpha(1f);
                player.myPlayer.invisibilityAlpha = 1;
            }
        }
    }

    public static void Postfix(PlayerPhysics __instance)
    {
        if (__instance.AmOwner && ElysiumModMenuGUI.invertControls && __instance.body != null)
        {
            __instance.body.velocity = -__instance.body.velocity;
        }

        SeePlayerVent(__instance);
    }
}
