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
                if (playerById == null || playerById.Disconnected || item.VotedFor == PlayerVoteArea.HasNotVoted ||
                    item.VotedFor == PlayerVoteArea.MissedVote || item.VotedFor == PlayerVoteArea.DeadVote || _votedPlayers.Contains(item.TargetPlayerId)) continue;
                _votedPlayers.Add(item.TargetPlayerId);
                if (item.VotedFor != PlayerVoteArea.SkippedVote)
                {
                    foreach (var item2 in __instance.playerStates) if (item2.TargetPlayerId == item.VotedFor) { __instance.BloopAVoteIcon(playerById, 0, item2.transform); break; }
                }
                else if (__instance.SkippedVoting != null) __instance.BloopAVoteIcon(playerById, 0, __instance.SkippedVoting.transform);
            }
            foreach (var item3 in __instance.playerStates)
            {
                if (item3 == null) continue;
                var component = item3.transform.GetComponent<VoteSpreader>();
                if (component != null) foreach (var sprite in component.Votes) sprite.gameObject.SetActive(true);
            }
            if (__instance.SkippedVoting != null) __instance.SkippedVoting.SetActive(true);
        }
        catch { }
    }
}
