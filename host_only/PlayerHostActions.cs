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
private void ForceGlobalEject(PlayerControl target)
        {
            if (target == null || AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
            {
                ShowNotification("<color=#FF0000>[ERROR]</color> Host required!");
                return;
            }

            try
            {
                target.Data.IsDead = false;

                if (MeetingHud.Instance == null)
                {
                    MeetingHud.Instance = UnityEngine.Object.Instantiate<MeetingHud>(DestroyableSingleton<HudManager>.Instance.MeetingPrefab);
                    AmongUsClient.Instance.Spawn(MeetingHud.Instance.Cast<InnerNetObject>(), -2, SpawnFlags.None);
                }

                var emptyStates = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<MeetingHud.VoterState>(0);

                MeetingHud.Instance.RpcVotingComplete(emptyStates, target.Data, false);

                MeetingHud.Instance.RpcClose();

                ShowNotification($"<color=#00FF00>[EJECT]</color> Ejecting <b>{target.Data.PlayerName}</b>...");
            }
            catch (Exception)
            {
            }
        }

private void ForceAllVotesTo(PlayerControl target)
        {
            if (target == null || target.Data == null) return;
            if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
            {
                ShowNotification("<color=#FF0000>[VOTE]</color> Host required.");
                return;
            }

            if (MeetingHud.Instance == null || PlayerControl.AllPlayerControls == null)
            {
                ShowNotification("<color=#FF0000>[VOTE]</color> Meeting required.");
                return;
            }

            try
            {
                var list = PlayerControl.AllPlayerControls.ToArray()
                    .Where(p => p != null && p.Data != null && !p.Data.Disconnected && p.PlayerId < 100)
                    .ToArray();

                var states = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<MeetingHud.VoterState>(list.Length);
                for (int i = 0; i < list.Length; i++)
                {
                    MeetingHud.VoterState st = states[i];
                    st.VoterId = list[i].PlayerId;
                    st.VotedForId = target.PlayerId;
                    states[i] = st;
                }

                MeetingHud.Instance.RpcVotingComplete(states, target.Data, false);
                ShowNotification($"<color=#00FF00>[VOTE]</color> Votes -> <b>{target.Data.PlayerName}</b>");
            }
            catch { }
        }

private void DrawPlayerVentTpRow(PlayerControl target, float fullW, float btnW, float gap)
        {
            int max = GetVentCount() - 1;
            selectedPlayerVentIdx = Mathf.Clamp(selectedPlayerVentIdx, 0, Mathf.Max(0, max));
            float buttonW = Mathf.Clamp(btnW, 84f, 112f);
            float sliderW = Mathf.Max(80f, fullW - buttonW - gap);
            string room = GetVentLabel(selectedPlayerVentIdx);

            GUILayout.BeginVertical(GUILayout.Width(fullW));
            GUILayout.Label($"Room: <color=#{GetMenuAccentHex()}>{room}</color>", new GUIStyle(toggleLabelStyle) { richText = true, fontSize = 11, clipping = TextClipping.Clip }, GUILayout.Width(fullW), GUILayout.Height(18));
            GUILayout.BeginHorizontal(GUILayout.Width(fullW));
            if (max > 0)
                selectedPlayerVentIdx = Mathf.RoundToInt(GUILayout.HorizontalSlider(selectedPlayerVentIdx, 0, max, sliderStyle, sliderThumbStyle, GUILayout.Width(sliderW)));
            else
                GUILayout.Space(sliderW);
            GUILayout.Space(gap);
            if (DrawFixedMenuButton("TP VENT", btnStyle, buttonW, 22f))
            {
                TeleportPlayerToVent(target, selectedPlayerVentIdx);
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

private static bool TryForceGlobalEjectViaMeeting(PlayerControl target)
        {
            if (target == null || target.Data == null || AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
                return false;

            try
            {
                target.Data.IsDead = false;

                if (MeetingHud.Instance == null)
                {
                    MeetingHud.Instance = UnityEngine.Object.Instantiate<MeetingHud>(DestroyableSingleton<HudManager>.Instance.MeetingPrefab);
                    AmongUsClient.Instance.Spawn(MeetingHud.Instance.Cast<InnerNetObject>(), -2, SpawnFlags.None);
                }

                var emptyStates = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<MeetingHud.VoterState>(0);
                MeetingHud.Instance.RpcVotingComplete(emptyStates, target.Data, false);
                MeetingHud.Instance.RpcClose();
                return true;
            }
            catch
            {
                return false;
            }
        }

private static bool IsDeadBodyForPlayerPresent(byte playerId)
        {
            try
            {
                var allBehaviours = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>();
                foreach (var mb in allBehaviours)
                {
                    if (mb == null || mb.gameObject == null) continue;
                    Type t = mb.GetType();
                    if (t == null || t.Name != "DeadBody") continue;

                    byte parentId = byte.MaxValue;
                    var parentProp = t.GetProperty("ParentId", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (parentProp != null)
                    {
                        object val = parentProp.GetValue(mb, null);
                        if (val is byte b) parentId = b;
                        else if (val is int i) parentId = (byte)i;
                    }
                    else
                    {
                        var parentField = t.GetField("ParentId", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (parentField != null)
                        {
                            object val = parentField.GetValue(mb);
                            if (val is byte b) parentId = b;
                            else if (val is int i) parentId = (byte)i;
                        }
                    }

                    if (parentId == playerId) return true;
                }
            }
            catch { }

            return false;
        }

private static void AttemptReportBody(PlayerControl target)
        {
            if (target == null || target.Data == null || PlayerControl.LocalPlayer == null) return;

            try
            {
                if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
                {
                    ShowNotification("<color=#FF0000>[REPORT]</color> Modded report is host only.");
                    return;
                }

                if (LobbyBehaviour.Instance != null)
                {
                    ShowNotification("<color=#FF0000>[REPORT]</color> Match must be started.");
                    return;
                }

                if (!target.Data.IsDead)
                {
                    ShowNotification("<color=#FF0000>[REPORT]</color> Only dead players can be reported.");
                    return;
                }

                if (!IsDeadBodyForPlayerPresent(target.PlayerId))
                {
                    ShowNotification("<color=#FF0000>[REPORT]</color> Body not found or already gone.");
                    return;
                }

                    TryOpenModdedMeeting(PlayerControl.LocalPlayer, target.Data, $"<color=#00FF00>[REPORT]</color> Modded report: <b>{target.Data.PlayerName}</b>.");
            }
            catch (Exception)
            {
            }
        }
    }
}
