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

[HarmonyPatch(typeof(MeetingHud), "PopulateResults")]
public static class RevealVotesCleanupPatch
{
    public static void Prefix(MeetingHud __instance)
    {
        if (!ElysiumModMenuGUI.RevealVotesEnabled) return;
        try
        {
            RevealVotesPatch.ClearVoteIcons(__instance);
            RevealVotesPatch.ClearMeetingVotes(false);
        }
        catch { }
    }

    public static void Postfix(MeetingHud __instance, Il2CppStructArray<MeetingHud.VoterState> states)
    {
        if (!ElysiumModMenuGUI.RevealVotesEnabled) return;
        try
        {
            RevealVotesPatch.ClearVoteIcons(__instance);
            RevealVotesPatch.ClearMeetingVotes(false);

            if (states != null)
            {
                foreach (var state in states)
                {
                    byte votedForId = RevealVotesPatch._voteTargets.TryGetValue(state.VoterId, out byte rememberedVote)
                        ? rememberedVote
                        : state.VotedForId;
                    RevealVotesPatch.DrawVote(__instance, state.VoterId, votedForId);
                }
            }

            RevealVotesPatch.DrawRememberedVotes(__instance);
        }
        catch { }
    }
}
