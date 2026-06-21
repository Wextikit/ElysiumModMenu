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
public static class ElysiumAnticheat
        {
            public static void Flag(PlayerControl player, string reason)
            {
                if (player == null || player.Data == null || player == PlayerControl.LocalPlayer) return;

                string pName = player.Data.PlayerName ?? "Unknown";

                int mode = ElysiumModMenuGUI.punishmentMode;

                if (mode >= 1)
                {
                    ElysiumModMenuGUI.ShowNotification($"<color=#FF0000>[ANTICHEAT]</color> <b>{pName}</b>: {reason}");
                }

                if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
                {
                    if (mode == 2)
                    {
                        AmongUsClient.Instance.KickPlayer(player.OwnerId, false);
                    }
                    else if (mode == 3)
                    {
                        string fc = string.IsNullOrEmpty(player.Data.FriendCode) ? "Unknown" : player.Data.FriendCode;
                        string puid = "Unknown";
                        try
                        {
                            var client = AmongUsClient.Instance.GetClientFromCharacter(player);
                            if (client != null) puid = GetClientPuid(client);
                        }
                        catch { }

                        ElysiumModMenuGUI.AddToBanList(fc, puid, pName, $"Anticheat: {reason}");

                        AmongUsClient.Instance.KickPlayer(player.OwnerId, true);
                    }
                }
            }
        }

[HarmonyPatch(typeof(PlayerPhysics), "RpcBootFromVent")]
        public static class Anticheat_RpcBootFromVent_Patch
        {
            public static bool Prefix(PlayerPhysics __instance)
            {
                if (!ElysiumModMenuGUI.blockSpoofRPC) return true;

                try
                {
                    if (__instance == null || __instance.myPlayer == null) return true;
                    if (!__instance.myPlayer.inVent) return false;
                }
                catch { }

                return true;
            }
        }

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.HandleRpc))]
        public static class Anticheat_PlayerControl_RPC
        {
            private static readonly Dictionary<byte, Queue<float>> chatRpcTimes = new Dictionary<byte, Queue<float>>();
            private static readonly Dictionary<byte, Queue<float>> meetingRpcTimes = new Dictionary<byte, Queue<float>>();
            private static readonly HashSet<byte> lobbyGameRpcs = new HashSet<byte>
            {
                (byte)RpcCalls.MurderPlayer,
                (byte)RpcCalls.ReportDeadBody,
                (byte)RpcCalls.StartMeeting,
                (byte)RpcCalls.EnterVent,
                (byte)RpcCalls.ExitVent,
                (byte)RpcCalls.Shapeshift,
                (byte)RpcCalls.ProtectPlayer
            };

            private static bool IsFlooded(Dictionary<byte, Queue<float>> map, byte playerId, int maxCalls, float windowSeconds)
            {
                float now = Time.unscaledTime;
                if (!map.TryGetValue(playerId, out Queue<float> times))
                {
                    times = new Queue<float>();
                    map[playerId] = times;
                }

                times.Enqueue(now);
                while (times.Count > 0 && now - times.Peek() > windowSeconds)
                    times.Dequeue();

                return times.Count > maxCalls;
            }

            public static bool Prefix(PlayerControl __instance, byte callId, Hazel.MessageReader reader)
            {
                if (__instance != null && __instance != PlayerControl.LocalPlayer && __instance.Data != null && ElysiumModMenuGUI.enablePasosLimit)
                {
                }

                if (!ElysiumModMenuGUI.blockSpoofRPC &&
                    !ElysiumModMenuGUI.blockSabotageRPC &&
                    !ElysiumModMenuGUI.blockGameRpcInLobby &&
                    !ElysiumModMenuGUI.blockChatFloodRpc &&
                    !ElysiumModMenuGUI.blockMeetingFloodRpc) return true;
                if (__instance == null || __instance == PlayerControl.LocalPlayer || __instance.Data == null) return true;

                int oldPos = reader.Position;
                bool isCheat = false;
                string cheatReason = "";

                try
                {
                    if (ElysiumModMenuGUI.blockGameRpcInLobby &&
                        AmongUsClient.Instance != null &&
                        !AmongUsClient.Instance.IsGameStarted &&
                        lobbyGameRpcs.Contains(callId))
                    {
                        isCheat = true;
                        cheatReason = $"Game RPC in lobby ({((RpcCalls)callId)})";
                    }

                    if (!isCheat && ElysiumModMenuGUI.blockChatFloodRpc &&
                        (callId == (byte)RpcCalls.SendChat || callId == (byte)RpcCalls.SendQuickChat))
                    {
                        if (IsFlooded(chatRpcTimes, __instance.PlayerId, ElysiumModMenuGUI.chatRpcLimit, ElysiumModMenuGUI.chatRpcWindow))
                        {
                            isCheat = true;
                            cheatReason = "Chat RPC flood";
                        }
                    }

       
                    if (!isCheat && ElysiumModMenuGUI.enableQuickChatEmptyGuard &&
                        callId == (byte)RpcCalls.SendQuickChat)
                    {
                        int qcPos = reader.Position;
                        int zeroRun = 0, zeroMax = 0, scanned = 0;
                        while (reader.Position < reader.Length && scanned < 4096)
                        {
                            scanned++;
                            if (reader.ReadByte() == 0) { zeroRun++; if (zeroRun > zeroMax) zeroMax = zeroRun; }
                            else zeroRun = 0;
                        }
                        reader.Position = qcPos;

                        if (zeroMax >= 8)
                        {
                            if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost &&
                                __instance != null && __instance != PlayerControl.LocalPlayer &&
                                __instance.OwnerId != AmongUsClient.Instance.HostId)
                            {
                                try
                                {
                                    bool qcBan = ElysiumModMenuGUI.banQuickChatEmptySpammer;
                                    string qcName = (__instance.Data != null && !string.IsNullOrEmpty(__instance.Data.PlayerName))
                                        ? __instance.Data.PlayerName : $"Client {__instance.OwnerId}";
                                    if (qcBan)
                                    {
                                        string qcFc = (__instance.Data != null && !string.IsNullOrEmpty(__instance.Data.FriendCode))
                                            ? __instance.Data.FriendCode : "Unknown";
                                        string qcPuid = "Unknown";
                                        try
                                        {
                                            var qcClient = AmongUsClient.Instance.GetClientFromCharacter(__instance);
                                            if (qcClient != null) qcPuid = ElysiumModMenuGUI.GetClientPuid(qcClient);
                                        }
                                        catch { }
                                        ElysiumModMenuGUI.AddToBanList(qcFc, qcPuid, qcName, "QuickChat Empty spam (anti-crash)");
                                    }
                                    AmongUsClient.Instance.KickPlayer(__instance.OwnerId, qcBan);
                                    ElysiumModMenuGUI.ShowNotification($"<color=#FF4444>[ANTI-CRASH]</color> {qcName} {(qcBan ? "banned" : "kicked")}: QuickChat spam");
                                }
                                catch { }
                            }
                            return false; 
                        }
                    }

                    if (!isCheat && ElysiumModMenuGUI.blockMeetingFloodRpc &&
                        (callId == (byte)RpcCalls.StartMeeting || callId == (byte)RpcCalls.ReportDeadBody))
                    {
                        if (IsFlooded(meetingRpcTimes, __instance.PlayerId, ElysiumModMenuGUI.meetingRpcLimit, ElysiumModMenuGUI.meetingRpcWindow))
                        {
                            isCheat = true;
                            cheatReason = "Meeting RPC flood";
                        }
                    }

                    if (!isCheat && ElysiumModMenuGUI.blockSpoofRPC)
                    {
                        if (callId == (byte)RpcCalls.SetColor)
                        {
                            uint netId = reader.ReadUInt32();
                            byte color = reader.ReadByte();
                            if (color >= Palette.PlayerColors.Length) { isCheat = true; cheatReason = $"Invalid Color ID ({color})"; }
                        }
                        else if (callId == (byte)RpcCalls.SetName || callId == (byte)RpcCalls.CheckName)
                        {
                            uint netId = callId == (byte)RpcCalls.SetName ? reader.ReadUInt32() : 0;
                            string reqName = reader.ReadString();
                            if (reqName.Length > 12) { isCheat = true; cheatReason = "Name length too long"; }
                            if (reqName.Contains("<")) { isCheat = true; cheatReason = "HTML Tags in name"; }
                        }
                        else if (callId == (byte)RpcCalls.SetScanner)
                        {
                            bool scanning = reader.ReadBoolean();
                            if (scanning && RoleManager.IsImpostorRole(__instance.Data.RoleType))
                            { isCheat = true; cheatReason = "Scanner activated as Impostor"; }
                        }
                        else if (callId == (byte)RpcCalls.PlayAnimation)
                        {
                            byte anim = reader.ReadByte();
                            if (RoleManager.IsImpostorRole(__instance.Data.RoleType))
                            { isCheat = true; cheatReason = "Task Animation as Impostor"; }
                        }
                        else if (callId == (byte)RpcCalls.EnterVent || callId == (byte)RpcCalls.ExitVent)
                        {
                            if (!__instance.Data.IsDead && __instance.Data.Role != null && !__instance.Data.Role.CanVent)
                            { isCheat = true; cheatReason = "Vent without vent ability"; }

                            if (GameManager.Instance != null && GameManager.Instance.IsHideAndSeek() && RoleManager.IsImpostorRole(__instance.Data.RoleType))
                            { isCheat = true; cheatReason = "Venting as Seeker in H&S"; }
                        }
                    }

                    if (!isCheat && ElysiumModMenuGUI.blockSabotageRPC)
                    {
                        if (callId == (byte)RpcCalls.ReportDeadBody)
                        {
                            if (GameManager.Instance != null && GameManager.Instance.IsHideAndSeek())
                            { isCheat = true; cheatReason = "Reported body in H&S"; }
                        }
                        else if (callId == (byte)RpcCalls.SetStartCounter)
                        {
                            reader.ReadPackedInt32();
                            sbyte counter = reader.ReadSByte();

                            if (__instance.OwnerId != AmongUsClient.Instance.HostId && counter != -1)
                            { isCheat = true; cheatReason = "Start counter changed by non-host"; }
                        }
                    }
                }
                catch { }

                reader.Position = oldPos;

                if (isCheat)
                {
                    ElysiumAnticheat.Flag(__instance, cheatReason);
                    return false;
                }

                return true;
            }
        }

[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.HandleRpc))]
        public static class Anticheat_ShipStatus_RPC
        {
            public static bool Prefix(ShipStatus __instance, byte callId, Hazel.MessageReader reader)
            {
                if (!ElysiumModMenuGUI.blockSabotageRPC) return true;

                int oldPos = reader.Position;
                bool isCheat = false;
                string cheatReason = "";
                PlayerControl sender = null;

                try
                {
                    if (callId == (byte)RpcCalls.UpdateSystem)
                    {
                        SystemTypes system = (SystemTypes)reader.ReadByte();
                        sender = reader.ReadNetObject<PlayerControl>();

                        if (sender != null && !sender.AmOwner)
                        {
                            if (system == SystemTypes.Sabotage)
                            {
                                SystemTypes sabSystem = (SystemTypes)reader.ReadByte();
                                if (sender.Data != null && !RoleManager.IsImpostorRole(sender.Data.RoleType))
                                { isCheat = true; cheatReason = "Triggered Sabotage as Crewmate"; }
                            }
                        }
                    }
                    else if (callId == (byte)RpcCalls.CloseDoorsOfType)
                    {
                        if (GameManager.Instance != null && GameManager.Instance.IsHideAndSeek())
                        { isCheat = true; cheatReason = "Closed doors in H&S"; }
                    }
                }
                catch { }

                reader.Position = oldPos;

                if (isCheat && sender != null && sender != PlayerControl.LocalPlayer)
                {
                    ElysiumAnticheat.Flag(sender, cheatReason);
                    return false;
                }

                return true;
            }
        }

public static bool autoChatEveryone = false;

public static bool pendingAutoMeeting = false;

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckColor))]
        public static class AllowDuplicateColors_CheckColor_Patch
        {
            private static bool applyingDuplicateColor;

            public static bool Prefix(PlayerControl __instance, byte bodyColor)
            {
                if (applyingDuplicateColor || !ElysiumModMenuGUI.allowDuplicateColors ||
                    __instance == null || AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost ||
                    bodyColor == byte.MaxValue)
                    return true;

                try
                {
                    applyingDuplicateColor = true;
                    __instance.RpcSetColor(bodyColor);
                    return false;
                }
                catch { return true; }
                finally { applyingDuplicateColor = false; }
            }
        }

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Start))]
        public static class Anticheat_Platform_Check
        {
            public static void Postfix(PlayerControl __instance)
            {
                if ((!ElysiumModMenuGUI.blockSpoofRPC && !ElysiumModMenuGUI.autoBanPlatformSpoof && !ElysiumModMenuGUI.banCustomPlatformsFromTxt) ||
                    __instance == null || __instance == PlayerControl.LocalPlayer) return;

                try
                {
                    var clientData = AmongUsClient.Instance.GetClientFromCharacter(__instance);
                    if (clientData == null || clientData.PlatformData == null) return;

                    if (ElysiumModMenuGUI.banCustomPlatformsFromTxt &&
                        MatchesPlatformBanTxt(clientData, out string customPlatformName, out string token))
                    {
                        HostBanForPlatform(__instance, $"Custom platform TXT match '{token}' ({customPlatformName})");
                        return;
                    }

                    var platform = clientData.PlatformData;
                    string pName = platform.PlatformName;
                    ulong xuid = platform.XboxPlatformId;
                    ulong psid = platform.PsnPlatformId;

                    bool isValid = true;

                    switch (platform.Platform)
                    {
                        case Platforms.StandaloneEpicPC:
                        case Platforms.StandaloneSteamPC:
                        case Platforms.StandaloneMac:
                        case Platforms.StandaloneItch:
                        case Platforms.IPhone:
                        case Platforms.Android:
                            isValid = (pName == "TESTNAME" && xuid == 0 && psid == 0);
                            break;
                        case Platforms.StandaloneWin10:
                            isValid = (pName == "TESTNAME" && xuid != 0 && psid == 0);
                            break;
                        case Platforms.Xbox:
                            isValid = (pName != "TESTNAME" && pName.Length >= 3 && xuid != 0 && psid == 0);
                            break;
                        case Platforms.Playstation:
                            isValid = (pName != "TESTNAME" && xuid == 0 && psid != 0);
                            break;
                        case Platforms.Switch:
                            isValid = (pName != "TESTNAME" && xuid == 0 && psid == 0);
                            break;
                    }

                    if (!isValid)
                    {
                        string reason = $"Platform Spoof detected ({platform.Platform})";
                        if (ElysiumModMenuGUI.autoBanPlatformSpoof)
                            HostBanForPlatform(__instance, reason);
                        else if (ElysiumModMenuGUI.blockSpoofRPC)
                            ElysiumAnticheat.Flag(__instance, reason);
                    }
                }
                catch { }
            }
        }
    }
}
