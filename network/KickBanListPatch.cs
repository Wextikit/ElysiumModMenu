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

[HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.KickPlayer))]
public static class AmongUsClient_KickPlayer_BanList_Patch
{
    public static bool Prefix(InnerNetClient __instance, ref int clientId, bool ban)
    {
        PlayerControl pc = null;
        if (PlayerControl.AllPlayerControls != null)
        {
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player == null || player.Data == null) continue;
                if (player.Data.ClientId != clientId && (int)player.OwnerId != clientId) continue;
                pc = player;
                break;
            }
        }

        if (pc != null)
        {
            try
            {
                var client = __instance.GetClientFromCharacter(pc);
                if (client != null)
                    clientId = client.Id;
                else if (pc.Data.ClientId >= 0)
                    clientId = pc.Data.ClientId;
            }
            catch
            {
                if (pc.Data.ClientId >= 0)
                    clientId = pc.Data.ClientId;
            }
        }

        if (ElysiumModMenuGUI.IsMeowcheloProtected(clientId))
            return false;

        if (ban && pc != null && AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
        {
            try
            {
                if (pc.Data != null)
                {
                    string fc = string.IsNullOrEmpty(pc.Data.FriendCode) ? "Unknown" : pc.Data.FriendCode;
                    string name = pc.Data.PlayerName ?? "Unknown";
                    string puid = "Unknown";

                    try
                    {
                        var client = AmongUsClient.Instance.GetClientFromCharacter(pc);
                        if (client != null && !string.IsNullOrWhiteSpace(client.ProductUserId))
                            puid = client.ProductUserId.Trim();
                        else
                            puid = ElysiumModMenuGUI.GetPlayerPuid(pc);
                    }
                    catch { }

                    ElysiumModMenuGUI.AddToBanList(fc, puid, name, "Host ban");
                    ElysiumModMenuGUI.ShowNotification($"<color=#FF0000>[BAN]</color> {name} added to ban list!");
                }
            }
            catch { }
        }

        return true;
    }
}
