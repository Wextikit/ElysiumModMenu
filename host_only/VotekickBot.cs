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
private enum VoteKickPhase { Off, Room, Voted, Left, Rejoin, Final }

public static bool votekickEveryone = false;

public static bool votekickAutoRejoin = false;

public static bool votekickCopyCode = true;
private static VoteKickPhase votekickPhase = VoteKickPhase.Off;

private static int votekickCode = 0;

private static int votekickCyclesDone = 0;

private static float votekickAt = 0f;

private static float votekickPulseAt = 0f;

private static float votekickVotedStart = 0f;

private static int votekickVotedCount = -1;

private static float votekickVotedStableAt = 0f;

private static bool votekickSwept = false;

private static readonly List<byte> votekickRapidQueue = new List<byte>();

private static float votekickRapidAt = 0f;

private static int votekickPassesLeft = 0;

private const float VotekickSettleDelay = 0.4f;

private const float VotekickLeaveMinDelay = 1.1f;

private const float VotekickLeaveMaxDelay = 1.5f;

private const float VotekickStableHold = 0.5f;

private const float VotekickRejoinDelay = 1.5f;

private const float VotekickRejoinTimeout = 22f;

private const float VotekickManualTimeout = 180f;

private const float VotekickFinalDelay = 1.5f;

private const float VotekickRapidStep = 0.12f;

private const float VotekickPulseStep = 0.3f;

private const int VotekickSweepPasses = 3;

private const int VotekickCycles = 2;

private Vector2 votekickScrollPosition = Vector2.zero;

private void StartVotekickEveryoneRun()
        {
            votekickEveryone = true;
            votekickCyclesDone = 0;
            votekickPhase = VoteKickPhase.Room;
            votekickAt = Time.unscaledTime + VotekickSettleDelay;
            votekickPulseAt = 0f;
            votekickVotedCount = -1;
            votekickSwept = false;
            votekickPassesLeft = 0;
            votekickRapidQueue.Clear();
            ShowNotification("<color=#ca08ff>[AUTO-VOTEKICK]</color> Armed. Votes will be sent after joining a room.");
        }

private static void StopVotekickEveryoneRun(bool clearVotes = true)
        {
            votekickEveryone = false;
            votekickPhase = VoteKickPhase.Off;
            votekickSwept = false;
            votekickPassesLeft = 0;
            if (clearVotes) votekickRapidQueue.Clear();
            ShowNotification("<color=#ca08ff>[AUTO-VOTEKICK]</color> Stopped.");
        }

private void TickVotekickEveryoneRun()
        {
            TickVotekickRapid();
            if (!votekickEveryone || votekickPhase == VoteKickPhase.Off) return;

            try
            {
                if (votekickPhase == VoteKickPhase.Room) TickVotekickRoom();
                else if (votekickPhase == VoteKickPhase.Voted) TickVotekickVoted();
                else if (votekickPhase == VoteKickPhase.Left) TickVotekickLeft();
                else if (votekickPhase == VoteKickPhase.Rejoin) TickVotekickRejoin();
                else if (votekickPhase == VoteKickPhase.Final) TickVotekickFinal();
            }
            catch { }
        }

private static void TickVotekickRoom()
        {
            if (!VotekickInRoom()) return;
            if (Time.unscaledTime < votekickAt) return;

            SaveVotekickCode(!votekickAutoRejoin);
            if (votekickCyclesDone >= VotekickCycles)
            {
                votekickSwept = false;
                votekickPhase = VoteKickPhase.Final;
                votekickAt = Time.unscaledTime + VotekickFinalDelay;
                ShowNotification("<color=#ca08ff>[AUTO-VOTEKICK]</color> Final sweep soon.");
                return;
            }

            int sent = ExecuteVotekickEveryone(false);
            string tail = votekickAutoRejoin ? " Leaving..." : " Leaving, code copied.";
            ShowNotification($"<color=#ca08ff>[AUTO-VOTEKICK]</color> Round {votekickCyclesDone + 1}: votes sent <b>{sent}</b>.{tail}");

            float now = Time.unscaledTime;
            votekickPhase = VoteKickPhase.Voted;
            votekickVotedStart = now;
            votekickPulseAt = now + VotekickPulseStep;
            votekickVotedCount = -1;
            votekickVotedStableAt = now + VotekickStableHold;
        }

private static void TickVotekickVoted()
        {
            float now = Time.unscaledTime;
            if (now >= votekickPulseAt)
            {
                votekickPulseAt = now + VotekickPulseStep;
                ExecuteVotekickEveryone(true);
            }

            int cnt = CountVotekickTargets();
            if (cnt != votekickVotedCount)
            {
                votekickVotedCount = cnt;
                votekickVotedStableAt = now + VotekickStableHold;
            }

            float since = now - votekickVotedStart;
            bool ready = since >= VotekickLeaveMinDelay && now >= votekickVotedStableAt;
            if (!ready && since < VotekickLeaveMaxDelay) return;

            LeaveVotekickRoom();
            votekickPhase = VoteKickPhase.Left;
            votekickAt = now + VotekickRejoinDelay;
        }

private static void TickVotekickLeft()
        {
            if (VotekickInRoom()) return;
            if (Time.unscaledTime < votekickAt) return;

            if (votekickAutoRejoin)
            {
                RejoinVotekickRoom(votekickCode);
                votekickAt = Time.unscaledTime + VotekickRejoinTimeout;
            }
            else
            {
                SaveVotekickCode(true);
                votekickAt = Time.unscaledTime + VotekickManualTimeout;
                ShowNotification("<color=#ca08ff>[AUTO-VOTEKICK]</color> Code copied. Rejoin manually to continue.");
            }
            votekickPhase = VoteKickPhase.Rejoin;
        }

private static void TickVotekickRejoin()
        {
            if (VotekickInRoom())
            {
                votekickCyclesDone++;
                votekickPhase = VoteKickPhase.Room;
                votekickAt = Time.unscaledTime + VotekickSettleDelay;
                ShowNotification($"<color=#ca08ff>[AUTO-VOTEKICK]</color> Joined. Round {votekickCyclesDone + 1}.");
                return;
            }

            if (Time.unscaledTime >= votekickAt)
            {
                SaveVotekickCode(true);
                StopVotekickEveryoneRun(false);
                ShowNotification(votekickAutoRejoin
                    ? "<color=#FF4444>[AUTO-VOTEKICK]</color> Auto rejoin failed. Code copied."
                    : "<color=#FF4444>[AUTO-VOTEKICK]</color> Rejoin timeout.");
            }
        }

private static void TickVotekickFinal()
        {
            if (votekickSwept) return;
            if (Time.unscaledTime < votekickAt) return;
            StartVotekickRapid();
            votekickSwept = true;
        }

private static void TickVotekickRapid()
        {
            if (votekickRapidQueue.Count == 0)
            {
                if (votekickPassesLeft > 0)
                {
                    votekickPassesLeft--;
                    FillVotekickQueue();
                    return;
                }

                if (votekickPhase == VoteKickPhase.Final && votekickSwept)
                    StopVotekickEveryoneRun(false);
                return;
            }

            if (Time.unscaledTime < votekickRapidAt) return;
            votekickRapidAt = Time.unscaledTime + VotekickRapidStep;

            byte id = votekickRapidQueue[0];
            votekickRapidQueue.RemoveAt(0);
            PlayerControl pc = FindVotekickPlayer(id);
            if (pc != null && pc.Data != null) TryVotekickVote(pc.Data.ClientId);
        }

private static void StartVotekickRapid()
        {
            votekickPassesLeft = VotekickSweepPasses - 1;
            FillVotekickQueue();
            votekickRapidAt = Time.unscaledTime;
        }

private static void FillVotekickQueue()
        {
            votekickRapidQueue.Clear();
            try
            {
                foreach (PlayerControl pc in PlayerControl.AllPlayerControls)
                    if (pc != null && !pc.AmOwner && pc.Data != null && !pc.Data.Disconnected)
                        votekickRapidQueue.Add(pc.PlayerId);
            }
            catch { }
        }

private static void RunVotekickRapidAll()
        {
            StartVotekickRapid();
            int targets = votekickRapidQueue.Count;
            if (targets > 0) ShowNotification($"<color=#ca08ff>[VOTEKICK]</color> Sweep x{VotekickSweepPasses}: <b>{targets}</b> targets.");
            else ShowNotification("<color=#FF4444>[VOTEKICK]</color> No targets.");
        }

private static int ExecuteVotekickEveryone(bool once)
        {
            if (VoteBanSystem.Instance == null || PlayerControl.AllPlayerControls == null) return 0;
            int n = 0;
            try
            {
                foreach (PlayerControl pc in PlayerControl.AllPlayerControls)
                {
                    if (pc == null || pc.AmOwner || pc.Data == null || pc.Data.Disconnected) continue;
                    int reps = once ? 1 : 3;
                    for (int i = 0; i < reps; i++)
                    {
                        if (TryVotekickVote(pc.Data.ClientId)) n++;
                    }
                }
            }
            catch { }
            return n;
        }

private static int CountVotekickTargets()
        {
            int n = 0;
            try
            {
                foreach (PlayerControl pc in PlayerControl.AllPlayerControls)
                    if (pc != null && !pc.AmOwner && pc.Data != null && !pc.Data.Disconnected)
                        n++;
            }
            catch { }
            return n;
        }

private void SendVotekickEveryoneStay()
        {
            int sent = ExecuteVotekickEveryone(false);
            if (sent > 0)
                ShowNotification($"<color=#ca08ff>[VOTEKICK]</color> Sent <b>{sent}</b> votes. Staying.");
            else
                ShowNotification("<color=#FF4444>[VOTEKICK]</color> No valid targets or VoteBanSystem is not ready.");
        }

public static void ExecuteVotekickTarget(PlayerControl target)
        {
            if (target == null || target.Data == null) return;

            if (TryVotekickVote(target.Data.ClientId))
            {
                string nm = string.IsNullOrEmpty(target.Data.PlayerName) ? "?" : target.Data.PlayerName;
                ShowNotification($"<color=#ca08ff>[VOTEKICK]</color> Vote sent to <b>{nm}</b>. Needs 3 unique clients.");
            }
        }

private static bool TryVotekickVote(int clientId)
        {
            if (clientId < 0 || VoteBanSystem.Instance == null) return false;
            try
            {
                VoteBanSystem.Instance.CmdAddVote(clientId);
                return true;
            }
            catch { return false; }
        }

private static void SaveVotekickCode(bool copyAlways = false)
        {
            try
            {
                if (AmongUsClient.Instance == null) return;
                int code = ((InnerNetClient)AmongUsClient.Instance).GameId;
                if (code != 0) votekickCode = code;
                if ((copyAlways || votekickCopyCode) && votekickCode != 0)
                    GUIUtility.systemCopyBuffer = GameCode.IntToGameName(votekickCode);
            }
            catch { }
        }

private static void RejoinVotekickRoom(int code)
        {
            try
            {
                AmongUsClient au = AmongUsClient.Instance;
                if (au == null || code == 0) return;
                au.GameId = code;
                var co = au.CoJoinOnlineGameFromCode(code);
                if (co != null) au.StartCoroutine(co);
            }
            catch { }
        }

private static void LeaveVotekickRoom()
        {
            try { if (AmongUsClient.Instance != null) AmongUsClient.Instance.ExitGame(DisconnectReasons.ExitGame); }
            catch { }
        }

private static bool VotekickInRoom()
        {
            return LobbyBehaviour.Instance != null || ShipStatus.Instance != null;
        }

private static PlayerControl FindVotekickPlayer(byte id)
        {
            try
            {
                foreach (PlayerControl pc in PlayerControl.AllPlayerControls)
                    if (pc != null && pc.PlayerId == id) return pc;
            }
            catch { }
            return null;
        }

private static string VotekickStatusText()
        {
            if (!votekickEveryone || votekickPhase == VoteKickPhase.Off)
                return "OFF";
            return $"{votekickPhase} | round {Mathf.Min(votekickCyclesDone + 1, VotekickCycles + 1)}";
        }

    }
}
