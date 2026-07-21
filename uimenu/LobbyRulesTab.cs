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
using System.Globalization;
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
private static float roleGuardianCd = 60f;
private static float roleGuardianTime = 10f;
private static bool roleGuardianImpSee;
private static float roleShifterCd = 15f;
private static float roleShifterTime = 30f;
private static bool roleShifterSkin = true;
private static float roleNoisemakerTime = 10f;
private static bool roleNoisemakerImpAlert = true;
private static float roleTrackerCd = 15f;
private static float roleTrackerTime = 5f;
private static float roleTrackerDelay = 1f;
private static float rolePhantomCd = 15f;
private static float rolePhantomTime = 10f;
private static float roleDetectiveLimit = 1f;
private static float roleViperDissolve = 10f;
private static float hnsSetHideTime = 60f;
private static float hnsSetFinalTime = 60f;
private static float hnsSetFinalSpeed = 1.5f;
private static float hnsSetVentCd = 10f;
private static int hnsSetVentUses = 3;
private static float hnsSetPingCd = 10f;
private static bool hnsSetFinalMap = true;
private static readonly Dictionary<string, string> lobbySetInputs = new Dictionary<string, string>();
private static string lobbySetEditKey = "";

private void DrawLobbySettingsTab()
        {
            float w = Mathf.Floor(Mathf.Max(240f, GetMenuWorkWidth(260f, 760f) - 44f));
            float gap = 10f;
            bool compact = w < 540f;
            float colW = compact ? w : Mathf.Floor((w - gap) * 0.5f);
            float rowW = Mathf.Max(180f, colW - 34f);
            LoadLobbySettingsFromGame(false);

            if (!HasGameOptions())
            {
                GUILayout.BeginVertical(menuCardStyle, GUILayout.Width(w), GUILayout.Height(86f));
                DrawMenuSectionHeader("LOBBY SETTINGS");
                GUILayout.FlexibleSpace();
                GUILayout.Label(L("Game options not ready.", "Настройки игры ещё не готовы."), centeredToggleLabelStyle, GUILayout.Height(24));
                GUILayout.FlexibleSpace();
                GUILayout.EndVertical();
                return;
            }

            if (compact) GUILayout.BeginVertical(GUILayout.Width(w));
            else GUILayout.BeginHorizontal(GUILayout.Width(w));

            GUILayout.BeginVertical(menuCardStyle, GUILayout.Width(colW), GUILayout.Height(250f));
            DrawMenuSectionHeader("GAME");
            DrawLocalIntRow("Map", rowW, ref lobbySetMap, 0, 5, 1);
            DrawLocalIntRow("Players", rowW, ref lobbySetPlayers, 4, 15, 1);
            DrawLocalIntRow("Imposters", rowW, ref lobbySetImps, 1, 3, 1);
            DrawLocalFloatRow("Speed", rowW, ref lobbySetSpeed, 0.1f);
            DrawLocalFloatRow("Crew Vision", rowW, ref lobbySetCrewVision, 0.1f);
            DrawLocalFloatRow("Imp Vision", rowW, ref lobbySetImpVision, 0.1f);
            DrawLocalFloatRow("Kill CD", rowW, ref lobbySetKillCd, 2.5f);
            DrawLocalIntRow("Kill Dist", rowW, ref lobbySetKillDist, 0, 2, 1);
            GUILayout.EndVertical();

            if (compact) GUILayout.Space(6); else GUILayout.Space(gap);

            GUILayout.BeginVertical(menuCardStyle, GUILayout.Width(colW), GUILayout.Height(250f));
            DrawMenuSectionHeader("MEETING / TASKS");
            DrawLocalIntRow("Meetings", rowW, ref lobbySetMeetings, 0, 15, 1);
            DrawLocalIntRow("Meet CD", rowW, ref lobbySetMeetingCd, 0, 60, 5);
            DrawLocalIntRow("Discuss", rowW, ref lobbySetDiscuss, 0, 120, 5);
            DrawLocalIntRow("Voting", rowW, ref lobbySetVoting, 0, 300, 5);
            DrawLocalIntRow("Common", rowW, ref lobbySetCommon, 0, 8, 1);
            DrawLocalIntRow("Long", rowW, ref lobbySetLong, 0, 8, 1);
            DrawLocalIntRow("Short", rowW, ref lobbySetShort, 0, 12, 1);
            GUILayout.EndVertical();

            if (compact) GUILayout.Space(6);
            else
            {
                GUILayout.EndHorizontal();
                GUILayout.Space(6);
            }

            if (compact) GUILayout.Space(6);

            GUILayout.BeginVertical(menuCardStyle, GUILayout.Width(compact ? colW : w), GUILayout.Height(compact ? 760f : 520f));
            DrawMenuSectionHeader("EXTRA");
            if (compact)
            {
                DrawLobbyToggle(ref lobbySetConfirm, "Confirm Ejects", Mathf.RoundToInt(rowW));
                GUILayout.Space(2);
                DrawLobbyToggle(ref lobbySetAnonymous, "Anonymous", Mathf.RoundToInt(rowW));
                GUILayout.Space(2);
                DrawLobbyToggle(ref lobbySetVisualTasks, "Visual Tasks", Mathf.RoundToInt(rowW));
            }
            else
            {
                GUILayout.BeginHorizontal(GUILayout.Width(Mathf.Max(360f, w - 34f)));
                DrawLobbyToggle(ref lobbySetConfirm, "Confirm Ejects", 170);
                GUILayout.Space(8);
                DrawLobbyToggle(ref lobbySetAnonymous, "Anonymous", 150);
                GUILayout.Space(8);
                DrawLobbyToggle(ref lobbySetVisualTasks, "Visual Tasks", 150);
                GUILayout.EndHorizontal();
            }
            DrawLocalIntRow("Task Bar", compact ? rowW : Mathf.Max(360f, w - 34f), ref lobbySetTaskBar, 0, 2, 1);
            GUILayout.Space(4);
            DrawClassicRolesSettings(compact ? rowW : Mathf.Max(360f, w - 34f), compact);
            GUILayout.Space(4);
            DrawLobbySettingsButtons(compact ? rowW : Mathf.Max(360f, w - 34f), false);
            GUILayout.EndVertical();

            if (compact) GUILayout.EndVertical();
        }

private void DrawHnsSettingsTab()
        {
            float w = Mathf.Floor(Mathf.Max(240f, GetMenuWorkWidth(260f, 760f) - 44f));
            float gap = 10f;
            bool compact = w < 540f;
            float colW = compact ? w : Mathf.Floor((w - gap) * 0.5f);
            float rowW = Mathf.Max(180f, colW - 34f);
            LoadLobbySettingsFromGame(false);

            if (compact) GUILayout.BeginVertical(GUILayout.Width(w));
            else GUILayout.BeginHorizontal(GUILayout.Width(w));

            GUILayout.BeginVertical(menuCardStyle, GUILayout.Width(colW), GUILayout.Height(312f));
            DrawMenuSectionHeader("H&S MAIN");
            DrawLocalIntRow("Map", rowW, ref lobbySetMap, 0, 5, 1);
            DrawLocalIntRow("Players", rowW, ref lobbySetPlayers, 4, 15, 1);
            DrawLocalIntRow("Imposters", rowW, ref lobbySetImps, 1, 3, 1);
            DrawLocalFloatRow("Player Speed", rowW, ref lobbySetSpeed, 0.1f);
            DrawLocalFloatRow("Crew Vision", rowW, ref lobbySetCrewVision, 0.1f);
            DrawLocalFloatRow("Seeker Vision", rowW, ref lobbySetImpVision, 0.1f);
            DrawLocalFloatRow("Kill CD", rowW, ref lobbySetKillCd, 2.5f);
            DrawLocalIntRow("Tasks Common", rowW, ref lobbySetCommon, 0, 8, 1);
            DrawLocalIntRow("Tasks Long", rowW, ref lobbySetLong, 0, 8, 1);
            DrawLocalIntRow("Tasks Short", rowW, ref lobbySetShort, 0, 12, 1);
            GUILayout.EndVertical();

            if (compact) GUILayout.Space(6); else GUILayout.Space(gap);

            GUILayout.BeginVertical(menuCardStyle, GUILayout.Width(colW), GUILayout.Height(238f));
            DrawMenuSectionHeader("H&S PORT");
            DrawLocalFloatRow("Hide Time", rowW, ref hnsSetHideTime, 5f);
            DrawLocalFloatRow("Final Time", rowW, ref hnsSetFinalTime, 5f);
            DrawLocalFloatRow("Final Speed", rowW, ref hnsSetFinalSpeed, 0.1f);
            DrawLocalFloatRow("Vent CD", rowW, ref hnsSetVentCd, 2.5f);
            DrawLocalIntRow("Vent Uses", rowW, ref hnsSetVentUses, 0, 20, 1);
            DrawLocalFloatRow("Ping CD", rowW, ref hnsSetPingCd, 1f);
            DrawLobbyToggle(ref hnsSetFinalMap, "Final Map", Mathf.RoundToInt(rowW));
            GUILayout.Space(4);
            DrawLobbySettingsButtons(rowW, true);
            GUILayout.EndVertical();

            if (compact) GUILayout.EndVertical();
            else GUILayout.EndHorizontal();
        }

private void LoadLobbySettingsFromGame(bool force)
        {
            int gid = 0;
            try { if (AmongUsClient.Instance != null) gid = AmongUsClient.Instance.GameId; } catch { }
            if (!force && lobbySettingsDirty) return;
            if (!force && !string.IsNullOrEmpty(lobbySetEditKey)) return;
            if (!force && lobbySettingsLoaded && lobbySettingsGameId == gid && Time.unscaledTime < nextLobbySettingsReadAt) return;
            if (force)
            {
                lobbySettingsDirty = false;
                lobbySetEditKey = "";
            }
            nextLobbySettingsReadAt = Time.unscaledTime + 0.2f;
            lobbySettingsLoaded = true;
            lobbySettingsGameId = gid;

            TryGetGameInt(out lobbySetMap, "MapId", "MapID", "mapId");
            if (TryGetGameInt(out int players, "MaxPlayers", "PlayerCount")) lobbySetPlayers = players;
            if (TryGetGameInt(out int imps, "NumImpostors", "Impostors")) lobbySetImps = imps;
            if (TryGetGameBool(out bool confirm, "ConfirmImpostor", "ConfirmEjects")) lobbySetConfirm = confirm;
            if (TryGetGameInt(out int meetings, "NumEmergencyMeetings", "EmergencyMeetings")) lobbySetMeetings = meetings;
            if (TryGetGameBool(out bool anon, "AnonymousVotes")) lobbySetAnonymous = anon;
            if (TryGetGameInt(out int meetingCd, "EmergencyCooldown")) lobbySetMeetingCd = meetingCd;
            if (TryGetGameInt(out int discuss, "DiscussionTime")) lobbySetDiscuss = discuss;
            if (TryGetGameInt(out int voting, "VotingTime")) lobbySetVoting = voting;
            if (TryGetGameFloat(out float speed, "PlayerSpeedMod", "PlayerSpeed"))
                lobbySetSpeed = noSettingLimit && Mathf.Abs(walkSpeed - 1f) > 0.0001f ? walkSpeed : speed;
            if (TryGetGameInt(out int taskBar, "TaskBarMode")) lobbySetTaskBar = taskBar;
            if (TryGetGameBool(out bool visual, "VisualTasks")) lobbySetVisualTasks = visual;
            if (TryGetGameFloat(out float crewVision, "CrewLightMod", "CrewmateVision")) lobbySetCrewVision = crewVision;
            if (TryGetGameFloat(out float impVision, "ImpostorLightMod", "ImpostorVision")) lobbySetImpVision = impVision;
            if (TryGetGameFloat(out float killCd, "KillCooldown")) lobbySetKillCd = killCd;
            if (TryGetGameInt(out int killDist, "KillDistance")) lobbySetKillDist = killDist;
            if (TryGetGameInt(out int common, "NumCommonTasks", "CommonTasks")) lobbySetCommon = common;
            if (TryGetGameInt(out int lng, "NumLongTasks", "LongTasks")) lobbySetLong = lng;
            if (TryGetGameInt(out int sh, "NumShortTasks", "ShortTasks")) lobbySetShort = sh;
            TryGetClassicRoleOpt(RoleTypes.Engineer, out roleEngineerCount, out roleEngineerChance, "Engineer");
            TryGetClassicRoleOpt(RoleTypes.Scientist, out roleScientistCount, out roleScientistChance, "Scientist");
            TryGetClassicRoleOpt(RoleTypes.GuardianAngel, out roleGuardianCount, out roleGuardianChance, "GuardianAngel", "Guardian");
            TryGetClassicRoleOpt(RoleTypes.Shapeshifter, out roleShifterCount, out roleShifterChance, "Shapeshifter", "Shifter");
            TryGetClassicRoleOpt((RoleTypes)8, out roleNoisemakerCount, out roleNoisemakerChance, "Noisemaker");
            TryGetClassicRoleOpt((RoleTypes)10, out roleTrackerCount, out roleTrackerChance, "Tracker");
            TryGetClassicRoleOpt((RoleTypes)9, out rolePhantomCount, out rolePhantomChance, "Phantom");
            TryGetClassicRoleOpt((RoleTypes)12, out roleDetectiveCount, out roleDetectiveChance, "Detective");
            TryGetClassicRoleOpt((RoleTypes)18, out roleViperCount, out roleViperChance, "Viper");
            LoadClassicRoleDetails();
            if (TryGetGameFloat(out float hideTime, "TotalHideTime", "totalHideTime", "CurrentHideTime", "currentHideTime", "HidingTime", "CategorizedHidingTime", "HideTime", "HnSHidingTime")) hnsSetHideTime = hideTime;
            if (TryGetGameFloat(out float finalTime, "TotalFinalHideTime", "totalFinalHideTime", "CurrentFinalHideTime", "currentFinalHideTime", "FinalHideTime", "CategorizedFinalHideTime", "FinaleTime", "FinalTime")) hnsSetFinalTime = finalTime;
            if (TryGetGameFloat(out float finalSpeed, "SeekerFinalSpeed", "FinalSeekerSpeed")) hnsSetFinalSpeed = finalSpeed;
            if (TryGetGameFloat(out float ventCd, "CrewmateVentCooldown")) hnsSetVentCd = ventCd;
            if (TryGetGameInt(out int ventUses, "CrewmateVentUses")) hnsSetVentUses = ventUses;
            if (TryGetGameFloat(out float pingCd, "SeekerPingCooldown", "PingCooldown", "MaxPingTime")) hnsSetPingCd = pingCd;
            if (TryGetGameBool(out bool finalMap, "SeekerFinalMap", "FinalMap")) hnsSetFinalMap = finalMap;
            lobbySetInputs.Clear();
        }

private void DrawLobbySettingsButtons(float width, bool hns)
        {
            float gap = 6f;
            float btnW = Mathf.Floor((width - gap * 2f) / 3f);
            GUILayout.BeginHorizontal(GUILayout.Width(width));
            if (GUILayout.Button(L("Apply", "ПРИМЕНИТЬ"), activeTabStyle, GUILayout.Width(btnW), GUILayout.Height(23f)))
            {
                if (hns) ApplyHnsSettings();
                else ApplyLobbySettings();
            }
            GUILayout.Space(gap);
            if (GUILayout.Button(L("Sync Settings", "СИНХРОНИЗИРОВАТЬ"), btnStyle, GUILayout.Width(btnW), GUILayout.Height(23f)))
            {
                LoadLobbySettingsFromGame(true);
                ShowNotification("<color=#00FFAA>[SETTINGS]</color> Synced from room.");
            }
            GUILayout.Space(gap);
            if (GUILayout.Button(L("Copy Lobby", "КОПИРОВАТЬ ЛОББИ"), btnStyle, GUILayout.Width(btnW), GUILayout.Height(23f)))
                CopyLobbyCode();
            GUILayout.EndHorizontal();
        }

private void DrawClassicRolesSettings(float width, bool compact)
        {
            DrawMenuSectionHeader("CLASSIC ROLES");
            if (compact)
            {
                DrawRoleSettingRow("Engineer", width, ref roleEngineerCount, ref roleEngineerChance);
                DrawRoleSettingRow("Scientist", width, ref roleScientistCount, ref roleScientistChance);
                DrawRoleSettingRow("Guardian", width, ref roleGuardianCount, ref roleGuardianChance);
                DrawRoleSettingRow("Shifter", width, ref roleShifterCount, ref roleShifterChance);
            DrawRoleSettingRow("Noisemaker", width, ref roleNoisemakerCount, ref roleNoisemakerChance);
            DrawRoleSettingRow("Tracker", width, ref roleTrackerCount, ref roleTrackerChance);
            DrawRoleSettingRow("Phantom", width, ref rolePhantomCount, ref rolePhantomChance);
                DrawRoleSettingRow("Detective", width, ref roleDetectiveCount, ref roleDetectiveChance);
                DrawRoleSettingRow("Viper", width, ref roleViperCount, ref roleViperChance);
                DrawClassicRoleDetails(width, true);
                return;
            }

            float gap = 8f;
            float col = Mathf.Floor((width - gap) * 0.5f);
            GUILayout.BeginHorizontal(GUILayout.Width(width));
            GUILayout.BeginVertical(GUILayout.Width(col));
            DrawRoleSettingRow("Engineer", col, ref roleEngineerCount, ref roleEngineerChance);
            DrawRoleSettingRow("Scientist", col, ref roleScientistCount, ref roleScientistChance);
            DrawRoleSettingRow("Guardian", col, ref roleGuardianCount, ref roleGuardianChance);
            DrawRoleSettingRow("Shifter", col, ref roleShifterCount, ref roleShifterChance);
            GUILayout.EndVertical();
            GUILayout.Space(gap);
            GUILayout.BeginVertical(GUILayout.Width(col));
            DrawRoleSettingRow("Noisemaker", col, ref roleNoisemakerCount, ref roleNoisemakerChance);
            DrawRoleSettingRow("Tracker", col, ref roleTrackerCount, ref roleTrackerChance);
            DrawRoleSettingRow("Phantom", col, ref rolePhantomCount, ref rolePhantomChance);
            DrawRoleSettingRow("Detective", col, ref roleDetectiveCount, ref roleDetectiveChance);
            DrawRoleSettingRow("Viper", col, ref roleViperCount, ref roleViperChance);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            DrawClassicRoleDetails(width, false);
        }

private void DrawRoleSettingRow(string label, float width, ref int count, ref int chance)
        {
            float labW = Mathf.Max(56f, width - 92f);
            GUIStyle labSt = lobbyLabelStyle11;
            GUILayout.BeginHorizontal(GUILayout.Width(width), GUILayout.Height(24f));
            GUILayout.Label(label, labSt, GUILayout.Width(labW), GUILayout.Height(22f));
            DrawTinyIntField("c_" + label, ref count, 1, 30f);
            GUILayout.Space(4f);
            DrawTinyIntField("p_" + label, ref chance, 10, 58f);
            GUILayout.EndHorizontal();
        }

private void DrawClassicRoleDetails(float width, bool compact)
        {
            GUILayout.Space(4f);
            DrawMenuSectionHeader("ROLE DETAILS");
            float gap = 8f;
            if (compact)
            {
                DrawLocalFloatRow("Eng Vent CD", width, ref roleEngineerCd, 1f);
                DrawLocalFloatRow("Eng Vent Time", width, ref roleEngineerVent, 1f);
                DrawLocalFloatRow("Sci CD", width, ref roleScientistCd, 1f);
                DrawLocalFloatRow("Sci Battery", width, ref roleScientistBattery, 1f);
                DrawLocalFloatRow("Angel Shield CD", width, ref roleGuardianCd, 1f);
                DrawLocalFloatRow("Angel Shield Time", width, ref roleGuardianTime, 1f);
                DrawLobbyToggle(ref roleGuardianImpSee, "Imp See Shield", Mathf.RoundToInt(width));
                DrawLocalFloatRow("Shifter CD", width, ref roleShifterCd, 1f);
                DrawLocalFloatRow("Shifter Time", width, ref roleShifterTime, 1f);
                DrawLobbyToggle(ref roleShifterSkin, "Leave Skin", Mathf.RoundToInt(width));
                DrawLocalFloatRow("Noise Alert Time", width, ref roleNoisemakerTime, 1f);
                DrawLobbyToggle(ref roleNoisemakerImpAlert, "Imp Alert", Mathf.RoundToInt(width));
                DrawLocalFloatRow("Tracker CD", width, ref roleTrackerCd, 1f);
                DrawLocalFloatRow("Tracker Time", width, ref roleTrackerTime, 1f);
                DrawLocalFloatRow("Tracker Delay", width, ref roleTrackerDelay, 1f);
                DrawLocalFloatRow("Phantom CD", width, ref rolePhantomCd, 1f);
                DrawLocalFloatRow("Phantom Time", width, ref rolePhantomTime, 1f);
                DrawLocalFloatRow("Detective Limit", width, ref roleDetectiveLimit, 1f);
                DrawLocalFloatRow("Viper Dissolve", width, ref roleViperDissolve, 1f);
                return;
            }

            float col = Mathf.Floor((width - gap) * 0.5f);
            GUILayout.BeginHorizontal(GUILayout.Width(width));
            GUILayout.BeginVertical(GUILayout.Width(col));
            DrawLocalFloatRow("Eng Vent CD", col, ref roleEngineerCd, 1f);
            DrawLocalFloatRow("Eng Vent Time", col, ref roleEngineerVent, 1f);
            DrawLocalFloatRow("Sci CD", col, ref roleScientistCd, 1f);
            DrawLocalFloatRow("Sci Battery", col, ref roleScientistBattery, 1f);
            DrawLocalFloatRow("Angel Shield CD", col, ref roleGuardianCd, 1f);
            DrawLocalFloatRow("Angel Shield Time", col, ref roleGuardianTime, 1f);
            DrawLobbyToggle(ref roleGuardianImpSee, "Imp See Shield", Mathf.RoundToInt(col));
            DrawLocalFloatRow("Shifter CD", col, ref roleShifterCd, 1f);
            DrawLocalFloatRow("Shifter Time", col, ref roleShifterTime, 1f);
            DrawLobbyToggle(ref roleShifterSkin, "Leave Skin", Mathf.RoundToInt(col));
            GUILayout.EndVertical();
            GUILayout.Space(gap);
            GUILayout.BeginVertical(GUILayout.Width(col));
            DrawLocalFloatRow("Noise Alert Time", col, ref roleNoisemakerTime, 1f);
            DrawLobbyToggle(ref roleNoisemakerImpAlert, "Imp Alert", Mathf.RoundToInt(col));
            DrawLocalFloatRow("Tracker CD", col, ref roleTrackerCd, 1f);
            DrawLocalFloatRow("Tracker Time", col, ref roleTrackerTime, 1f);
            DrawLocalFloatRow("Tracker Delay", col, ref roleTrackerDelay, 1f);
            DrawLocalFloatRow("Phantom CD", col, ref rolePhantomCd, 1f);
            DrawLocalFloatRow("Phantom Time", col, ref rolePhantomTime, 1f);
            DrawLocalFloatRow("Detective Limit", col, ref roleDetectiveLimit, 1f);
            DrawLocalFloatRow("Viper Dissolve", col, ref roleViperDissolve, 1f);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

private void DrawTinyIntField(string key, ref int val, int step, float width)
        {
            bool edit = lobbySetEditKey == key;
            if (!lobbySetInputs.ContainsKey(key) || !edit) lobbySetInputs[key] = val.ToString();
            GUIStyle st = MakeLobbyNumFieldStyle(edit);
            if (GUILayout.Button(lobbySetInputs[key], st, GUILayout.Width(width), GUILayout.Height(22f)))
            {
                if (!edit) lobbySetInputs[key] = "";
                lobbySetEditKey = key;
            }
            if (edit && ReadLobbyNumKeys(key, false) && int.TryParse(lobbySetInputs[key], out int n) && n != val)
            {
                val = n;
                lobbySettingsDirty = true;
            }
            if (Event.current != null && edit && Event.current.type == EventType.ScrollWheel)
            {
                val += Event.current.delta.y > 0f ? -step : step;
                lobbySetInputs[key] = val.ToString();
                lobbySettingsDirty = true;
                Event.current.Use();
            }
        }

private bool DrawLobbyToggle(ref bool val, string label, int width)
        {
            bool old = val;
            val = DrawCompactToggle(val, label, width);
            if (val != old) lobbySettingsDirty = true;
            return val;
        }

private void DrawLocalFloatRow(string label, float width, ref float val, float step)
        {
            float fieldW = Mathf.Clamp(width * 0.26f, 48f, 64f);
            float labelW = Mathf.Max(48f, width - 24f - 4f - fieldW - 4f - 24f);
            string key = "f_" + label;
            bool edit = lobbySetEditKey == key;
            if (!lobbySetInputs.ContainsKey(key) || !edit)
                lobbySetInputs[key] = FormatLobbyFloat(val);

            GUIStyle labSt = lobbyRichLabelStyle11;
            GUIStyle fldSt = MakeLobbyNumFieldStyle(edit);

            GUILayout.BeginHorizontal(GUILayout.Width(width), GUILayout.Height(24f));
            GUILayout.Label(label, labSt, GUILayout.Width(labelW), GUILayout.Height(22f));
            if (GUILayout.Button("-", btnStyle, GUILayout.Width(24f), GUILayout.Height(22f)))
            {
                val -= step;
                lobbySetInputs[key] = FormatLobbyFloat(val);
                lobbySetEditKey = "";
                lobbySettingsDirty = true;
            }
            GUILayout.Space(4f);
            if (GUILayout.Button(lobbySetInputs[key], fldSt, GUILayout.Width(fieldW), GUILayout.Height(22f)))
            {
                if (!edit) lobbySetInputs[key] = "";
                lobbySetEditKey = key;
            }
            if (edit && ReadLobbyNumKeys(key, true))
            {
                string raw = lobbySetInputs[key].Replace(',', '.');
                if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float n))
                {
                    if (Mathf.Abs(n - val) > 0.00001f) lobbySettingsDirty = true;
                    val = n;
                }
            }
            GUILayout.Space(4f);
            if (GUILayout.Button("+", btnStyle, GUILayout.Width(24f), GUILayout.Height(22f)))
            {
                val += step;
                lobbySetInputs[key] = FormatLobbyFloat(val);
                lobbySetEditKey = "";
                lobbySettingsDirty = true;
            }
            GUILayout.EndHorizontal();
        }

private void DrawLocalIntRow(string label, float width, ref int val, int min, int max, int step)
        {
            int stepI = Mathf.Max(1, step);
            float fieldW = Mathf.Clamp(width * 0.26f, 48f, 64f);
            float labelW = Mathf.Max(48f, width - 24f - 4f - fieldW - 4f - 24f);
            string key = "i_" + label;
            bool edit = lobbySetEditKey == key;
            if (!lobbySetInputs.ContainsKey(key) || !edit)
                lobbySetInputs[key] = val.ToString();

            GUIStyle labSt = lobbyLabelStyle11;
            GUIStyle fldSt = MakeLobbyNumFieldStyle(edit);

            GUILayout.BeginHorizontal(GUILayout.Width(width), GUILayout.Height(24f));
            GUILayout.Label(label, labSt, GUILayout.Width(labelW), GUILayout.Height(22f));
            if (GUILayout.Button("-", btnStyle, GUILayout.Width(24f), GUILayout.Height(22f)))
            {
                val -= stepI;
                lobbySetInputs[key] = val.ToString();
                lobbySetEditKey = "";
                lobbySettingsDirty = true;
            }
            GUILayout.Space(4f);
            if (GUILayout.Button(lobbySetInputs[key], fldSt, GUILayout.Width(fieldW), GUILayout.Height(22f)))
            {
                if (!edit) lobbySetInputs[key] = "";
                lobbySetEditKey = key;
            }
            if (edit && ReadLobbyNumKeys(key, false))
            {
                if (int.TryParse(lobbySetInputs[key], out int n) && n != val)
                {
                    val = n;
                    lobbySettingsDirty = true;
                }
            }
            GUILayout.Space(4f);
            if (GUILayout.Button("+", btnStyle, GUILayout.Width(24f), GUILayout.Height(22f)))
            {
                val += stepI;
                lobbySetInputs[key] = val.ToString();
                lobbySetEditKey = "";
                lobbySettingsDirty = true;
            }
            GUILayout.EndHorizontal();
        }

private bool ReadLobbyNumKeys(string key, bool flt)
        {
            Event e = Event.current;
            if (e == null || e.type != EventType.KeyDown) return false;
            if (!lobbySetInputs.ContainsKey(key)) lobbySetInputs[key] = "";

            if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter || e.keyCode == KeyCode.Escape)
            {
                lobbySetEditKey = "";
                e.Use();
                return false;
            }
            if (e.keyCode == KeyCode.Backspace)
            {
                string s = lobbySetInputs[key];
                if (s.Length > 0) lobbySetInputs[key] = s.Substring(0, s.Length - 1);
                e.Use();
                return true;
            }

            char c = e.character;
            if ((c >= '0' && c <= '9') || c == '-' || (flt && (c == '.' || c == ',')))
            {
                if (c == '-' && lobbySetInputs[key].Length > 0) return false;
                if ((c == '.' || c == ',') && (lobbySetInputs[key].Contains(".") || lobbySetInputs[key].Contains(","))) return false;
                lobbySetInputs[key] += c;
                e.Use();
                return true;
            }
            return false;
        }

private static string FormatLobbyFloat(float val)
        {
            return val.ToString("0.######", CultureInfo.InvariantCulture);
        }

private Color GetLobbyNumTextColor()
        {
            if (whiteMenuTheme)
                return new Color(0.12f, 0.12f, 0.12f, 1f);

            Color c = GetMenuAccentColor(false);
            if (GetLobbyColorLight(c) >= 0.38f)
                return c;

            c = GetMenuControlAccentColor();
            if (GetLobbyColorLight(c) >= 0.38f)
                return c;

            return new Color(0.92f, 0.92f, 0.92f, 1f);
        }

private static float GetLobbyColorLight(Color c)
        {
            return c.r * 0.299f + c.g * 0.587f + c.b * 0.114f;
        }

private GUIStyle MakeLobbyNumFieldStyle(bool edit)
        {
            GUIStyle st = edit ? lobbyNumEditStyle : lobbyNumFieldStyle;
            Color c = GetLobbyNumTextColor();
            st.alignment = TextAnchor.MiddleCenter;
            st.fontSize = 11;
            st.fontStyle = FontStyle.Bold;
            st.clipping = TextClipping.Clip;
            st.wordWrap = false;
            st.padding = CreateRectOffset(3, 3, 1, 1);
            if (btnStyle != null)
            {
                st.normal.background = inputBlockStyle != null ? inputBlockStyle.normal.background : btnStyle.normal.background;
                st.hover.background = btnStyle.hover.background ?? st.normal.background;
                st.active.background = activeTabStyle != null ? activeTabStyle.normal.background : btnStyle.active.background;
                st.focused.background = activeTabStyle != null ? activeTabStyle.normal.background : st.normal.background;
            }
            st.normal.textColor = c;
            st.hover.textColor = c;
            st.active.textColor = edit ? Color.black : c;
            st.focused.textColor = edit ? Color.black : c;
            if (edit && activeTabStyle != null)
            {
                st.normal.background = activeTabStyle.normal.background;
                st.hover.background = activeTabStyle.normal.background;
                st.normal.textColor = Color.black;
                st.hover.textColor = Color.black;
            }
            return st;
        }
    }
}
