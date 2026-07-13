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


[HarmonyPatch(typeof(MeetingHud), "Update")]
public static class RevealVotesPatch
{
    internal static List<int> _votedPlayers = new List<int>();
    internal static Dictionary<byte, byte> _voteTargets = new Dictionary<byte, byte>();

    internal static void RememberVote(byte voterId, byte votedForId)
    {
        try
        {
            if (votedForId == PlayerVoteArea.HasNotVoted ||
                votedForId == PlayerVoteArea.MissedVote ||
                votedForId == PlayerVoteArea.DeadVote)
            {
                _voteTargets.Remove(voterId);
                return;
            }

            _voteTargets[voterId] = votedForId;
        }
        catch { }
    }

    internal static void ClearVoteIcons(MeetingHud meeting)
    {
        if (meeting == null || meeting.playerStates == null) return;
        foreach (var item in meeting.playerStates)
        {
            if (item == null) continue;
            var component = item.transform.GetComponent<VoteSpreader>();
            if (component != null && component.Votes.Count != 0)
            {
                foreach (var sprite in component.Votes) Object.DestroyImmediate(sprite.gameObject);
                component.Votes.Clear();
            }
        }
    }

    internal static void ClearMeetingVotes(bool clearRememberedVotes)
    {
        _votedPlayers.Clear();
        if (clearRememberedVotes) _voteTargets.Clear();
    }

    internal static void DrawRememberedVotes(MeetingHud meeting)
    {
        if (meeting == null) return;
        foreach (var vote in _voteTargets)
            DrawVote(meeting, vote.Key, vote.Value);
        ShowVoteSprites(meeting);
    }

    internal static void DrawVote(MeetingHud meeting, byte voterId, byte votedForId)
    {
        if (meeting == null || meeting.playerStates == null || GameData.Instance == null || _votedPlayers.Contains(voterId)) return;
        if (votedForId == PlayerVoteArea.HasNotVoted ||
            votedForId == PlayerVoteArea.MissedVote ||
            votedForId == PlayerVoteArea.DeadVote) return;

        var voter = GameData.Instance.GetPlayerById(voterId);
        if (voter == null || voter.Disconnected) return;

        _votedPlayers.Add(voterId);
        if (votedForId == PlayerVoteArea.SkippedVote)
        {
            if (meeting.SkippedVoting != null)
            {
                meeting.BloopAVoteIcon(voter, 0, meeting.SkippedVoting.transform);
                PaintVoteIcon(meeting.SkippedVoting.transform, voter);
            }
            return;
        }

        foreach (var state in meeting.playerStates)
        {
            if (state == null || state.TargetPlayerId != votedForId) continue;
            meeting.BloopAVoteIcon(voter, 0, state.transform);
            PaintVoteIcon(state.transform, voter);
            break;
        }
    }

    private static void PaintVoteIcon(Transform tr, NetworkedPlayerInfo voter)
    {
        try
        {
            if (tr == null || voter == null || voter.DefaultOutfit == null) return;
            int colorId = voter.DefaultOutfit.ColorId;
            if (colorId < 0 || colorId >= Palette.PlayerColors.Length) return;

            var spread = tr.GetComponent<VoteSpreader>();
            if (spread != null && spread.Votes != null && spread.Votes.Count != 0)
            {
                var spr = spread.Votes[spread.Votes.Count - 1];
                if (spr != null)
                {
                    PlayerMaterial.SetColors(colorId, spr);
                    spr.color = Color.white;
                }
            }
        }
        catch { }
    }

    internal static void ShowVoteSprites(MeetingHud meeting)
    {
        if (meeting == null || meeting.playerStates == null) return;
        foreach (var item in meeting.playerStates)
        {
            if (item == null) continue;
            var component = item.transform.GetComponent<VoteSpreader>();
            if (component != null) foreach (var sprite in component.Votes) sprite.gameObject.SetActive(true);
        }
        if (meeting.SkippedVoting != null) meeting.SkippedVoting.SetActive(true);
    }

    public static void Prefix(MeetingHud __instance)
    {
        if (!ElysiumModMenuGUI.RevealVotesEnabled) return;
        try
        {
            if ((int)__instance.state >= 4) return;
            foreach (var item in __instance.playerStates)
            {
                if (item == null) continue;
                var playerById = GameData.Instance.GetPlayerById(item.TargetPlayerId);
                if (playerById == null || playerById.Disconnected || _votedPlayers.Contains(item.TargetPlayerId)) continue;
                byte votedForId = _voteTargets.TryGetValue(item.TargetPlayerId, out byte rememberedVote) ? rememberedVote : item.VotedFor;
                DrawVote(__instance, item.TargetPlayerId, votedForId);
            }
            ShowVoteSprites(__instance);
        }
        catch { }
    }
}

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.CastVote))]
public static class RevealVotesCastVotePatch
{
    public static void Postfix(byte srcPlayerId, byte suspectPlayerId)
    {
        RevealVotesPatch.RememberVote(srcPlayerId, suspectPlayerId);
    }
}

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Close))]
public static class RevealVotesClosePatch
{
    public static void Prefix()
    {
        RevealVotesPatch.ClearMeetingVotes(true);
    }
}
