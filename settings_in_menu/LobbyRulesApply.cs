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
private void ApplyLobbySettings()
        {
            if (!CanApplyLobbySettings()) return;
            if (!lobbySettingsDirty)
            {
                TouchGameOptions();
                ShowNotification("<color=#00FFAA>[SETTINGS]</color> Synced.");
                return;
            }
            NormalizeLobbySettings(false);
            TrySetGameInt(lobbySetMap, "MapId", "MapID", "mapId");
            TrySetGameInt(lobbySetPlayers, "MaxPlayers", "PlayerCount", "maxPlayers");
            TrySetGameInt(lobbySetImps, "NumImpostors", "Impostors", "numImpostors");
            TrySetGameBool(lobbySetConfirm, "ConfirmImpostor", "ConfirmEjects", "confirmImpostor");
            TrySetGameInt(lobbySetMeetings, "NumEmergencyMeetings", "EmergencyMeetings", "numEmergencyMeetings");
            TrySetGameBool(lobbySetAnonymous, "AnonymousVotes", "anonymousVotes");
            TrySetGameInt(lobbySetMeetingCd, "EmergencyCooldown", "emergencyCooldown");
            TrySetGameInt(lobbySetDiscuss, "DiscussionTime", "discussionTime");
            TrySetGameInt(lobbySetVoting, "VotingTime", "votingTime");
            ApplyLobbyLocalSpeed();
            TrySetGameFloat(GetLobbyNetSpeed(), "PlayerSpeedMod", "PlayerSpeed", "playerSpeedMod");
            TrySetGameInt(lobbySetTaskBar, "TaskBarMode", "taskBarMode");
            TrySetGameBool(lobbySetVisualTasks, "VisualTasks", "visualTasks");
            TrySetGameFloat(lobbySetCrewVision, "CrewLightMod", "CrewmateVision", "crewLightMod");
            TrySetGameFloat(lobbySetImpVision, "ImpostorLightMod", "ImpostorVision", "impostorLightMod");
            TrySetGameFloat(lobbySetKillCd, "KillCooldown", "killCooldown");
            TrySetGameInt(lobbySetKillDist, "KillDistance", "killDistance");
            TrySetGameInt(lobbySetCommon, "NumCommonTasks", "CommonTasks", "numCommonTasks");
            TrySetGameInt(lobbySetLong, "NumLongTasks", "LongTasks", "numLongTasks");
            TrySetGameInt(lobbySetShort, "NumShortTasks", "ShortTasks", "numShortTasks");
            ApplyClassicRoles();
            TouchGameOptions();
            lobbySettingsDirty = false;
            lobbySetEditKey = "";
            lobbySettingsLoaded = false;
            ShowNotification("<color=#00FFAA>[SETTINGS]</color> Applied.");
        }

private void ApplyClassicRoles()
        {
            TrySetClassicRoleOpt(RoleTypes.Engineer, roleEngineerCount, roleEngineerChance, "Engineer");
            TrySetClassicRoleOpt(RoleTypes.Scientist, roleScientistCount, roleScientistChance, "Scientist");
            TrySetClassicRoleOpt(RoleTypes.GuardianAngel, roleGuardianCount, roleGuardianChance, "GuardianAngel", "Guardian");
            TrySetClassicRoleOpt(RoleTypes.Shapeshifter, roleShifterCount, roleShifterChance, "Shapeshifter", "Shifter");
            TrySetClassicRoleOpt((RoleTypes)8, roleNoisemakerCount, roleNoisemakerChance, "Noisemaker");
            TrySetClassicRoleOpt((RoleTypes)10, roleTrackerCount, roleTrackerChance, "Tracker");
            TrySetClassicRoleOpt((RoleTypes)9, rolePhantomCount, rolePhantomChance, "Phantom");
            TrySetClassicRoleOpt((RoleTypes)12, roleDetectiveCount, roleDetectiveChance, "Detective");
            TrySetClassicRoleOpt((RoleTypes)18, roleViperCount, roleViperChance, "Viper");
            ApplyClassicRoleDetails();
        }

private static void TryGetClassicRoleOpt(RoleTypes role, out int count, out int chance, params string[] names)
        {
            count = 0;
            chance = 0;
            object ro = GetRoleOptionsObj();
            if (ro != null)
            {
                try
                {
                    MethodInfo mc = ro.GetType().GetMethod("GetNumPerGame", BindingFlags.Public | BindingFlags.Instance);
                    MethodInfo mp = ro.GetType().GetMethod("GetChancePerGame", BindingFlags.Public | BindingFlags.Instance);
                    if (mc != null) count = Convert.ToInt32(mc.Invoke(ro, new object[] { role }));
                    if (mp != null) chance = Convert.ToInt32(mp.Invoke(ro, new object[] { role }));
                    return;
                }
                catch { }
            }

            string n = names != null && names.Length > 0 ? names[0] : role.ToString();
            foreach (string name in names ?? new[] { n })
            {
                if (TryGetGameInt(out int cnt, "Num" + name, name + "Count", name + "MaxCount", "Max" + name, "Max" + name + "Count", name + "RoleCount"))
                    count = cnt;
                if (TryGetGameInt(out int pct, name + "Chance", name + "SpawnChance", name + "Probability", name + "Rate", name + "Percentage"))
                    chance = pct;
            }

            object obj = FindRoleOptObj(role, names);
            if (obj == null) return;
            if (TryGetIntMember(obj, out int c, "MaxCount", "Count", "RoleCount", "NumRoles", "Amount")) count = c;
            if (TryGetIntMember(obj, out int p, "Chance", "SpawnChance", "Probability", "Rate", "Percentage")) chance = p;
        }

private static void TrySetClassicRoleOpt(RoleTypes role, int count, int chance, params string[] names)
        {
            bool ok = false;
            foreach (object opts in GetGameOptionsObjs())
            {
                object ro = GetRoleOptionsObj(opts);
                if (ro == null) continue;
                try
                {
                    MethodInfo m = ro.GetType().GetMethod("SetRoleRate", BindingFlags.Public | BindingFlags.Instance);
                    if (m != null)
                    {
                        m.Invoke(ro, new object[] { role, count, chance });
                        ok = true;
                    }
                }
                catch { }
            }

            string n = names != null && names.Length > 0 ? names[0] : role.ToString();
            foreach (string name in names ?? new[] { n })
            {
                TrySetGameInt(count, "Num" + name, name + "Count", name + "MaxCount", "Max" + name, "Max" + name + "Count", name + "RoleCount");
                TrySetGameInt(chance, name + "Chance", name + "SpawnChance", name + "Probability", name + "Rate", name + "Percentage");
            }

            if (ok) return;
            object obj = FindRoleOptObj(role, names);
            if (obj == null) return;
            TrySetMemberValue(obj, count, "MaxCount", "Count", "RoleCount", "NumRoles", "Amount");
            TrySetMemberValue(obj, chance, "Chance", "SpawnChance", "Probability", "Rate", "Percentage");
        }

private static void LoadClassicRoleDetails()
        {
            var eng = GetRoleOpt<EngineerRoleOptionsV10>(RoleTypes.Engineer);
            if (eng != null) { roleEngineerCd = eng.EngineerCooldown; roleEngineerVent = eng.EngineerInVentMaxTime; }
            if (TryGetGameFloat(out float engCd, "1300", "EngineerCooldown", "VentUseCooldown", "VentCooldown")) roleEngineerCd = engCd;
            if (TryGetGameFloat(out float engVent, "1301", "EngineerInVentMaxTime", "MaxTimeInVents", "VentDuration")) roleEngineerVent = engVent;
            var sci = GetRoleOpt<ScientistRoleOptionsV10>(RoleTypes.Scientist);
            if (sci != null) { roleScientistCd = sci.ScientistCooldown; roleScientistBattery = sci.ScientistBatteryCharge; }
            if (TryGetGameFloat(out float sciCd, "1200", "ScientistCooldown", "VitalsDisplayCooldown", "VitalsCooldown")) roleScientistCd = sciCd;
            if (TryGetGameFloat(out float sciBattery, "1201", "ScientistBatteryCharge", "BatteryDuration", "BatteryCharge")) roleScientistBattery = sciBattery;
            var ga = GetRoleOpt<GuardianAngelRoleOptionsV10>(RoleTypes.GuardianAngel);
            if (ga != null) { roleGuardianCd = ga.GuardianAngelCooldown; roleGuardianTime = ga.ProtectionDurationSeconds; roleGuardianImpSee = ga.ImpostorsCanSeeProtect; }
            if (TryGetGameFloat(out float gaCd, "1101", "GuardianAngelCooldown", "ProtectCooldown")) roleGuardianCd = gaCd;
            if (TryGetGameFloat(out float gaTime, "1100", "ProtectionDurationSeconds", "ProtectionDuration")) roleGuardianTime = gaTime;
            if (TryGetGameBool(out bool gaSee, "1100", "ImpostorsCanSeeProtect", "ProtectVisibleToImpostors")) roleGuardianImpSee = gaSee;
            var ss = GetRoleOpt<ShapeshifterRoleOptionsV10>(RoleTypes.Shapeshifter);
            if (ss != null) { roleShifterCd = ss.ShapeshifterCooldown; roleShifterTime = ss.ShapeshifterDuration; roleShifterSkin = ss.ShapeshifterLeaveSkin; }
            if (TryGetGameFloat(out float ssCd, "1000", "ShapeshifterCooldown", "ShapeshiftCooldown")) roleShifterCd = ssCd;
            if (TryGetGameFloat(out float ssTime, "1001", "ShapeshifterDuration", "ShapeshiftDuration")) roleShifterTime = ssTime;
            if (TryGetGameBool(out bool ssSkin, "1000", "ShapeshifterLeaveSkin", "LeaveShapeshiftingEvidence")) roleShifterSkin = ssSkin;
            var nm = GetRoleOpt<NoisemakerRoleOptionsV10>((RoleTypes)8);
            if (nm != null) { roleNoisemakerTime = nm.NoisemakerAlertDuration; roleNoisemakerImpAlert = nm.NoisemakerImpostorAlert; }
            if (TryGetGameFloat(out float nmTime, "1600", "NoisemakerAlertDuration", "AlertDuration")) roleNoisemakerTime = nmTime;
            if (TryGetGameBool(out bool nmAlert, "1600", "NoisemakerImpostorAlert", "ImpostorAlert")) roleNoisemakerImpAlert = nmAlert;
            var tr = GetRoleOpt<TrackerRoleOptionsV10>((RoleTypes)10);
            if (tr != null) { roleTrackerCd = tr.TrackerCooldown; roleTrackerTime = tr.TrackerDuration; roleTrackerDelay = tr.TrackerDelay; }
            if (TryGetGameFloat(out float trCd, "1550", "TrackerCooldown")) roleTrackerCd = trCd;
            if (TryGetGameFloat(out float trTime, "1551", "TrackerDuration")) roleTrackerTime = trTime;
            if (TryGetGameFloat(out float trDelay, "1552", "TrackerDelay")) roleTrackerDelay = trDelay;
            var ph = GetRoleOpt<PhantomRoleOptionsV10>((RoleTypes)9);
            if (ph != null) { rolePhantomCd = ph.PhantomCooldown; rolePhantomTime = ph.PhantomDuration; }
            if (TryGetGameFloat(out float phCd, "1500", "PhantomCooldown")) rolePhantomCd = phCd;
            if (TryGetGameFloat(out float phTime, "1501", "PhantomDuration")) rolePhantomTime = phTime;
            var dt = GetRoleOpt<DetectiveRoleOptionsV10>((RoleTypes)12);
            if (dt != null) roleDetectiveLimit = dt.DetectiveSuspectLimit;
            var vp = GetRoleOpt<ViperRoleOptionsV10>((RoleTypes)18);
            if (vp != null) roleViperDissolve = vp.viperDissolveTime;
        }

private static void ApplyClassicRoleDetails()
        {
            var eng = GetRoleOpt<EngineerRoleOptionsV10>(RoleTypes.Engineer);
            if (eng != null) { eng.EngineerCooldown = roleEngineerCd; eng.EngineerInVentMaxTime = roleEngineerVent; }
            TrySetGameFloat(roleEngineerCd, "1300", "EngineerCooldown", "VentUseCooldown", "VentCooldown");
            TrySetGameFloat(roleEngineerVent, "1301", "EngineerInVentMaxTime", "MaxTimeInVents", "VentDuration");
            var sci = GetRoleOpt<ScientistRoleOptionsV10>(RoleTypes.Scientist);
            if (sci != null) { sci.ScientistCooldown = roleScientistCd; sci.ScientistBatteryCharge = roleScientistBattery; }
            TrySetGameFloat(roleScientistCd, "1200", "ScientistCooldown", "VitalsDisplayCooldown", "VitalsCooldown");
            TrySetGameFloat(roleScientistBattery, "1201", "ScientistBatteryCharge", "BatteryDuration", "BatteryCharge");
            var ga = GetRoleOpt<GuardianAngelRoleOptionsV10>(RoleTypes.GuardianAngel);
            if (ga != null) { ga.GuardianAngelCooldown = roleGuardianCd; ga.ProtectionDurationSeconds = roleGuardianTime; ga.ImpostorsCanSeeProtect = roleGuardianImpSee; }
            TrySetGameFloat(roleGuardianCd, "1101", "GuardianAngelCooldown", "ProtectCooldown");
            TrySetGameFloat(roleGuardianTime, "1100", "ProtectionDurationSeconds", "ProtectionDuration");
            TrySetGameBool(roleGuardianImpSee, "1100", "ImpostorsCanSeeProtect", "ProtectVisibleToImpostors");
            var ss = GetRoleOpt<ShapeshifterRoleOptionsV10>(RoleTypes.Shapeshifter);
            if (ss != null) { ss.ShapeshifterCooldown = roleShifterCd; ss.ShapeshifterDuration = roleShifterTime; ss.ShapeshifterLeaveSkin = roleShifterSkin; }
            TrySetGameFloat(roleShifterCd, "1000", "ShapeshifterCooldown", "ShapeshiftCooldown");
            TrySetGameFloat(roleShifterTime, "1001", "ShapeshifterDuration", "ShapeshiftDuration");
            TrySetGameBool(roleShifterSkin, "1000", "ShapeshifterLeaveSkin", "LeaveShapeshiftingEvidence");
            var nm = GetRoleOpt<NoisemakerRoleOptionsV10>((RoleTypes)8);
            if (nm != null) { nm.NoisemakerAlertDuration = roleNoisemakerTime; nm.NoisemakerImpostorAlert = roleNoisemakerImpAlert; }
            TrySetGameFloat(roleNoisemakerTime, "1600", "NoisemakerAlertDuration", "AlertDuration");
            TrySetGameBool(roleNoisemakerImpAlert, "1600", "NoisemakerImpostorAlert", "ImpostorAlert");
            var tr = GetRoleOpt<TrackerRoleOptionsV10>((RoleTypes)10);
            if (tr != null) { tr.TrackerCooldown = roleTrackerCd; tr.TrackerDuration = roleTrackerTime; tr.TrackerDelay = roleTrackerDelay; }
            TrySetGameFloat(roleTrackerCd, "1550", "TrackerCooldown");
            TrySetGameFloat(roleTrackerTime, "1551", "TrackerDuration");
            TrySetGameFloat(roleTrackerDelay, "1552", "TrackerDelay");
            var ph = GetRoleOpt<PhantomRoleOptionsV10>((RoleTypes)9);
            if (ph != null) { ph.PhantomCooldown = rolePhantomCd; ph.PhantomDuration = rolePhantomTime; }
            TrySetGameFloat(rolePhantomCd, "1500", "PhantomCooldown");
            TrySetGameFloat(rolePhantomTime, "1501", "PhantomDuration");
            var dt = GetRoleOpt<DetectiveRoleOptionsV10>((RoleTypes)12);
            if (dt != null) dt.DetectiveSuspectLimit = roleDetectiveLimit;
            var vp = GetRoleOpt<ViperRoleOptionsV10>((RoleTypes)18);
            if (vp != null) vp.viperDissolveTime = roleViperDissolve;
        }

private static T GetRoleOpt<T>(RoleTypes role) where T : class
        {
            object ro = GetRoleOptionsObj();
            if (ro == null) return null;
            try
            {
                if (ro is RoleOptionsCollectionV10 col && col.TryGetRoleOptions<T>(role, out T opt))
                    return opt;
            }
            catch { }
            try
            {
                MethodInfo m = ro.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(x => x.Name == "TryGetRoleOptions" && x.IsGenericMethodDefinition);
                if (m == null) return null;
                MethodInfo g = m.MakeGenericMethod(typeof(T));
                object[] args = new object[] { role, null };
                if (Convert.ToBoolean(g.Invoke(ro, args))) return args[1] as T;
            }
            catch { }
            return null;
        }

private static object GetRoleOptionsObj()
        {
            return GetRoleOptionsObj(GetGameOptionsObj());
        }

private static object GetRoleOptionsObj(object opts)
        {
            if (opts == null) return null;
            object ro = GetMemberValue(opts, "RoleOptions") ?? GetMemberValue(opts, "roleOptions");
            if (ro != null) return ro;
            Type t = opts.GetType();
            foreach (string n in new[] { "get_RoleOptions", "GetRoleOptions", "RoleOptions" })
            {
                try
                {
                    MethodInfo m = t.GetMethod(n, BindingFlags.Public | BindingFlags.Instance);
                    if (m != null && m.GetParameters().Length == 0)
                    {
                        ro = m.Invoke(opts, null);
                        if (ro != null) return ro;
                    }
                }
                catch { }
            }
            return null;
        }

private static object FindRoleOptObj(RoleTypes role, params string[] names)
        {
            object opts = GetGameOptionsObj();
            if (opts == null) return null;
            Type t = opts.GetType();
            foreach (MethodInfo m in t.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                if (m.GetParameters().Length != 1) continue;
                string mn = m.Name;
                if (!mn.Contains("Role") || (!mn.Contains("Option") && !mn.Contains("Setting"))) continue;
                Type pt = m.GetParameters()[0].ParameterType;
                try
                {
                    object arg = pt.IsEnum ? Enum.ToObject(pt, (int)role) : CastGameValue((int)role, pt);
                    object got = m.Invoke(opts, new object[] { arg });
                    if (got != null) return got;
                }
                catch { }
            }

            foreach (string member in new[] { "RoleOptions", "roleOptions", "RoleSettings", "roleSettings", "Roles", "roles" })
            {
                object bag = GetMemberValue(opts, member);
                object got = FindRoleOptInBag(bag, role, names);
                if (got != null) return got;
            }
            return null;
        }

private static object FindRoleOptInBag(object bag, RoleTypes role, string[] names)
        {
            if (bag == null) return null;
            Type t = bag.GetType();
            foreach (PropertyInfo p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (p.GetIndexParameters().Length != 1) continue;
                Type pt = p.GetIndexParameters()[0].ParameterType;
                try
                {
                    object arg = pt.IsEnum ? Enum.ToObject(pt, (int)role) : CastGameValue((int)role, pt);
                    object got = p.GetValue(bag, new object[] { arg });
                    if (got != null) return got;
                }
                catch { }
            }

            if (bag is IEnumerable en)
            {
                foreach (object it in en)
                {
                    if (it == null) continue;
                    if (RoleOptMatches(it, role, names)) return it;
                }
            }
            return null;
        }

private static bool RoleOptMatches(object obj, RoleTypes role, string[] names)
        {
            object raw = GetMemberValue(obj, "Role") ?? GetMemberValue(obj, "RoleType") ?? GetMemberValue(obj, "RoleId");
            if (raw != null)
            {
                try { if (Convert.ToInt32(raw) == (int)role) return true; } catch { }
                string s = raw.ToString();
                foreach (string n in names ?? Array.Empty<string>())
                    if (!string.IsNullOrEmpty(n) && s.IndexOf(n, StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
            }
            string txt = obj.ToString();
            foreach (string n in names ?? Array.Empty<string>())
                if (!string.IsNullOrEmpty(n) && txt.IndexOf(n, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            return false;
        }

private static bool TryGetIntMember(object obj, out int val, params string[] names)
        {
            val = 0;
            foreach (string n in names)
            {
                object raw = GetMemberValue(obj, n);
                if (raw == null) continue;
                try { val = Convert.ToInt32(raw); return true; } catch { }
            }
            return false;
        }

private static bool TrySetMemberValue(object obj, object val, params string[] names)
        {
            if (obj == null) return false;
            Type t = obj.GetType();
            foreach (string n in names)
            {
                PropertyInfo p = FindGameProp(t, n);
                if (p != null && p.CanWrite)
                {
                    try { p.SetValue(obj, CastGameValue(val, p.PropertyType)); return true; } catch { }
                }
                FieldInfo f = t.GetField(n, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (f != null)
                {
                    try { f.SetValue(obj, CastGameValue(val, f.FieldType)); return true; } catch { }
                }
            }
            return false;
        }

private void ApplyHnsSettings()
        {
            if (!CanApplyLobbySettings()) return;
            if (!lobbySettingsDirty)
            {
                TouchHnsGameOptions();
                ShowNotification("<color=#00FFAA>[H&S]</color> Synced.");
                return;
            }
            NormalizeLobbySettings(true);
            ApplyLobbyLocalSpeed();
            TrySetGameFloat(GetLobbyNetSpeed(), "PlayerSpeedMod", "PlayerSpeed", "playerSpeedMod");
            TrySetGameInt(lobbySetMap, "MapId", "MapID", "mapId");
            TrySetGameInt(lobbySetPlayers, "MaxPlayers", "PlayerCount", "maxPlayers");
            TrySetGameInt(lobbySetImps, "NumImpostors", "Impostors", "numImpostors");
            TrySetGameFloat(lobbySetCrewVision, "CrewLightMod", "CrewmateVision", "CrewmateLightMod", "crewLightMod");
            TrySetGameFloat(lobbySetImpVision, "ImpostorLightMod", "ImpostorVision", "SeekerLightMod", "impostorLightMod");
            TrySetGameFloat(lobbySetKillCd, "KillCooldown", "SeekerKillCooldown", "killCooldown");
            TrySetGameInt(lobbySetCommon, "NumCommonTasks", "CommonTasks", "numCommonTasks");
            TrySetGameInt(lobbySetLong, "NumLongTasks", "LongTasks", "numLongTasks");
            TrySetGameInt(lobbySetShort, "NumShortTasks", "ShortTasks", "numShortTasks");
            TrySetGameFloatAll(hnsSetHideTime, "TotalHideTime", "totalHideTime", "CurrentHideTime", "currentHideTime", "HidingTime", "CategorizedHidingTime", "HideTime", "HnSHidingTime");
            TrySetGameFloatAll(hnsSetFinalTime, "TotalFinalHideTime", "totalFinalHideTime", "CurrentFinalHideTime", "currentFinalHideTime", "FinalHideTime", "CategorizedFinalHideTime", "FinaleTime", "FinalTime");
            TrySetGameFloat(hnsSetFinalSpeed, "SeekerFinalSpeed", "FinalSeekerSpeed");
            TrySetGameFloat(hnsSetVentCd, "CrewmateVentCooldown");
            TrySetGameInt(hnsSetVentUses, "CrewmateVentUses");
            TrySetGameFloat(hnsSetPingCd, "SeekerPingCooldown", "PingCooldown", "MaxPingTime");
            TrySetGameBool(hnsSetFinalMap, "SeekerFinalMap", "FinalMap");
            ApplyClassicRoles();
            TouchHnsGameOptions();
            lobbySettingsDirty = false;
            lobbySetEditKey = "";
            lobbySettingsLoaded = false;
            ShowNotification("<color=#00FFAA>[H&S]</color> Applied.");
        }

private static void NormalizeLobbySettings(bool hns)
        {
            if (noSettingLimit)
            {
                MinNoLimitFloat(ref lobbySetSpeed);
                MinNoLimitFloat(ref lobbySetCrewVision);
                MinNoLimitFloat(ref lobbySetImpVision);
                MinNoLimitFloat(ref lobbySetKillCd);
                MinNoLimitFloat(ref roleEngineerCd);
                MinNoLimitFloat(ref roleEngineerVent);
                MinNoLimitFloat(ref roleScientistCd);
                MinNoLimitFloat(ref roleScientistBattery);
                MinNoLimitFloat(ref roleGuardianCd);
                MinNoLimitFloat(ref roleGuardianTime);
                MinNoLimitFloat(ref roleShifterCd);
                MinNoLimitFloat(ref roleShifterTime);
                MinNoLimitFloat(ref roleNoisemakerTime);
                MinNoLimitFloat(ref roleTrackerCd);
                MinNoLimitFloat(ref roleTrackerTime);
                MinNoLimitFloat(ref roleTrackerDelay);
                MinNoLimitFloat(ref rolePhantomCd);
                MinNoLimitFloat(ref rolePhantomTime);
                MinNoLimitFloat(ref roleDetectiveLimit);
                MinNoLimitFloat(ref roleViperDissolve);
                MinNoLimitFloat(ref hnsSetHideTime);
                MinNoLimitFloat(ref hnsSetFinalTime);
                MinNoLimitFloat(ref hnsSetFinalSpeed);
                MinNoLimitFloat(ref hnsSetVentCd);
                MinNoLimitFloat(ref hnsSetPingCd);
                return;
            }

            lobbySetMap = Mathf.Clamp(lobbySetMap, 0, 5);
            lobbySetPlayers = Mathf.Clamp(lobbySetPlayers, 4, 15);
            lobbySetImps = Mathf.Clamp(lobbySetImps, 1, Mathf.Min(3, Mathf.Max(1, lobbySetPlayers - 1)));
            lobbySetMeetings = Mathf.Clamp(lobbySetMeetings, 0, 15);
            lobbySetMeetingCd = Mathf.Clamp(lobbySetMeetingCd, 0, 60);
            lobbySetDiscuss = Mathf.Clamp(lobbySetDiscuss, 0, 120);
            lobbySetVoting = Mathf.Clamp(lobbySetVoting, 0, 300);
            lobbySetSpeed = Mathf.Clamp(lobbySetSpeed, 0.1f, 3f);
            lobbySetTaskBar = Mathf.Clamp(lobbySetTaskBar, 0, 2);
            lobbySetCrewVision = Mathf.Clamp(lobbySetCrewVision, 0.25f, 5f);
            lobbySetImpVision = Mathf.Clamp(lobbySetImpVision, 0.25f, 5f);
            lobbySetKillCd = Mathf.Clamp(lobbySetKillCd, hns ? 2.5f : 10f, 60f);
            lobbySetKillDist = Mathf.Clamp(lobbySetKillDist, 0, 2);
            lobbySetCommon = Mathf.Clamp(lobbySetCommon, 0, 8);
            lobbySetLong = Mathf.Clamp(lobbySetLong, 0, 8);
            lobbySetShort = Mathf.Clamp(lobbySetShort, 0, 12);

            ClampRole(ref roleEngineerCount, ref roleEngineerChance);
            ClampRole(ref roleScientistCount, ref roleScientistChance);
            ClampRole(ref roleGuardianCount, ref roleGuardianChance);
            ClampRole(ref roleShifterCount, ref roleShifterChance);
            ClampRole(ref roleNoisemakerCount, ref roleNoisemakerChance);
            ClampRole(ref roleTrackerCount, ref roleTrackerChance);
            ClampRole(ref rolePhantomCount, ref rolePhantomChance);
            ClampRole(ref roleDetectiveCount, ref roleDetectiveChance);
            ClampRole(ref roleViperCount, ref roleViperChance);

            roleEngineerCd = Mathf.Clamp(roleEngineerCd, 0f, 120f);
            roleEngineerVent = Mathf.Clamp(roleEngineerVent, 0f, 120f);
            roleScientistCd = Mathf.Clamp(roleScientistCd, 0f, 120f);
            roleScientistBattery = Mathf.Clamp(roleScientistBattery, 0f, 120f);
            roleGuardianCd = Mathf.Clamp(roleGuardianCd, 0f, 120f);
            roleGuardianTime = Mathf.Clamp(roleGuardianTime, 0f, 120f);
            roleShifterCd = Mathf.Clamp(roleShifterCd, 0f, 120f);
            roleShifterTime = Mathf.Clamp(roleShifterTime, 0f, 120f);
            roleNoisemakerTime = Mathf.Clamp(roleNoisemakerTime, 0f, 120f);
            roleTrackerCd = Mathf.Clamp(roleTrackerCd, 0f, 120f);
            roleTrackerTime = Mathf.Clamp(roleTrackerTime, 0f, 120f);
            roleTrackerDelay = Mathf.Clamp(roleTrackerDelay, 0f, 120f);
            rolePhantomCd = Mathf.Clamp(rolePhantomCd, 0f, 120f);
            rolePhantomTime = Mathf.Clamp(rolePhantomTime, 0f, 120f);
            roleDetectiveLimit = Mathf.Clamp(roleDetectiveLimit, 0f, 10f);
            roleViperDissolve = Mathf.Clamp(roleViperDissolve, 0f, 120f);

            hnsSetHideTime = Mathf.Clamp(hnsSetHideTime, 10f, 180f);
            hnsSetFinalTime = Mathf.Clamp(hnsSetFinalTime, 10f, 180f);
            hnsSetFinalSpeed = Mathf.Clamp(hnsSetFinalSpeed, 0.1f, 3f);
            hnsSetVentCd = Mathf.Clamp(hnsSetVentCd, 0f, 60f);
            hnsSetVentUses = Mathf.Clamp(hnsSetVentUses, 0, 20);
            hnsSetPingCd = Mathf.Clamp(hnsSetPingCd, 0f, 60f);
        }

private static void ApplyLobbyLocalSpeed()
        {
            if (!noSettingLimit) return;
            walkSpeed = Mathf.Max(0.00001f, lobbySetSpeed);
        }

private static float GetLobbyNetSpeed()
        {
            if (!noSettingLimit) return lobbySetSpeed;
            return Mathf.Clamp(lobbySetSpeed, 0.00001f, 3f);
        }

private static void ClampRole(ref int count, ref int chance)
        {
            count = Mathf.Clamp(count, 0, 15);
            chance = Mathf.Clamp(chance, 0, 100);
        }

private static void MinNoLimitFloat(ref float val)
        {
            if (val < 0.00001f) val = 0.00001f;
        }

private static void ClampRoleWide(ref int count, ref int chance)
        {
            count = Mathf.Clamp(count, 0, 511);
            chance = Mathf.Clamp(chance, 0, 100);
        }

internal static void ApplyNoLimitRange(NumberOption opt)
        {
            if (!noSettingLimit || opt == null) return;
            try
            {
                opt.ValidRange = new FloatRange(0.00001f, 999999999f);
                if (opt.Value < 0.00001f)
                {
                    opt.Value = 0.00001f;
                    opt.UpdateValue();
                }
                opt.AdjustButtonsActiveState();
            }
            catch { }
        }

private static bool CanApplyLobbySettings()
        {
            if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
            {
                ShowNotification("<color=#FF4444>[SETTINGS]</color> Host required.");
                return false;
            }
            if (!HasGameOptions())
            {
                ShowNotification("<color=#FF4444>[SETTINGS]</color> Options not ready.");
                return false;
            }
            return true;
        }

private static void CopyLobbyCode()
        {
            string code = GetCurrentRoomCodeForStatus();
            if (string.IsNullOrWhiteSpace(code) || code == "No room")
            {
                ShowNotification("<color=#FF4444>[ROOM]</color> No code.");
                return;
            }
            GUIUtility.systemCopyBuffer = code;
            ShowNotification($"<color=#00FFAA>[ROOM]</color> Copied code: <b>{code}</b>");
        }

private static bool HasGameOptions()
        {
            try { return GameOptionsManager.Instance != null && GameOptionsManager.Instance.CurrentGameOptions != null; }
            catch { return false; }
        }

private float DrawGameFloatRow(string label, float width, float min, float max, float step, string suffix, params string[] names)
        {
            if (!TryGetGameFloat(out float val, names))
            {
                DrawMissingGameOption(label, width);
                return 0f;
            }

            val = Mathf.Clamp(val, min, max);
            float next = DrawSettingSlider(label, width, val, min, max, step, suffix);
            if (Mathf.Abs(next - val) > 0.0001f && TrySetGameFloat(next, names))
                TouchGameOptions();
            return next;
        }

private int DrawGameIntRow(string label, float width, int min, int max, int step, params string[] names)
        {
            if (!TryGetGameInt(out int val, names))
            {
                DrawMissingGameOption(label, width);
                return 0;
            }

            val = Mathf.Clamp(val, min, max);
            float nextF = DrawSettingSlider(label, width, val, min, max, Mathf.Max(1, step), "");
            int stepI = Mathf.Max(1, step);
            int next = Mathf.Clamp(Mathf.RoundToInt(nextF / stepI) * stepI, min, max);
            if (next != val && TrySetGameInt(next, names))
                TouchGameOptions();
            return next;
        }

private void DrawGameBoolRow(string label, int width, params string[] names)
        {
            if (!TryGetGameBool(out bool val, names))
            {
                DrawMissingGameOption(label, width);
                return;
            }

            bool next = DrawCompactToggle(val, label, width);
            if (next != val && TrySetGameBool(next, names))
                TouchGameOptions();
        }

private float DrawSettingSlider(string label, float width, float val, float min, float max, float step, string suffix)
        {
            float labelW = Mathf.Clamp(width * 0.38f, 84f, 130f);
            float sliderW = Mathf.Max(76f, width - labelW - 8f);
            float shown = step >= 1f ? Mathf.Round(val) : Mathf.Round(val / step) * step;
            string fmt = step >= 1f ? "0" : "0.##";
            GUILayout.BeginHorizontal(GUILayout.Width(width), GUILayout.Height(23f));
            GUILayout.Label($"{label}: <color=#{GetMenuAccentHex()}>{shown.ToString(fmt)}{suffix}</color>", lobbyRichLabelStyle11, GUILayout.Width(labelW), GUILayout.Height(22f));
            float next = GUILayout.HorizontalSlider(val, min, max, sliderStyle, sliderThumbStyle, GUILayout.Width(sliderW));
            GUILayout.EndHorizontal();
            return Mathf.Clamp(Mathf.Round(next / step) * step, min, max);
        }

private void DrawMissingGameOption(string label, float width)
        {
            GUIStyle st = lobbyLabelStyle11;
            st.normal.textColor = whiteMenuTheme ? new Color(0.42f, 0.42f, 0.42f, 1f) : new Color(0.42f, 0.42f, 0.42f, 1f);
            GUILayout.Label($"{label}: n/a", st, GUILayout.Width(width), GUILayout.Height(22f));
        }

private static object GetGameOptionsObj()
        {
            try { return GameOptionsManager.Instance?.CurrentGameOptions; }
            catch { return null; }
        }

private static IEnumerable<object> GetGameOptionsObjs()
        {
            object cur = null;
            try { cur = GameOptionsManager.Instance?.CurrentGameOptions; } catch { }
            if (cur != null) yield return cur;

            GameOptionsManager mgr = null;
            try { mgr = GameOptionsManager.Instance; } catch { }
            if (mgr == null) yield break;

            foreach (string n in new[] { "GameHostOptions", "currentNormalGameOptions", "currentGameOptions", "CurrentGameOptions", "currentHideNSeekGameOptions" })
            {
                object obj = GetMemberValue(mgr, n);
                if (obj == null || ReferenceEquals(obj, cur)) continue;
                yield return obj;
            }
        }

private static bool TryGetGameFloat(out float val, params string[] names)
        {
            val = 0f;
            if (!TryGetGameOption(out object raw, "GetFloat", names)) return false;
            try { val = Convert.ToSingle(raw); return true; } catch { return false; }
        }

private static bool TryGetGameInt(out int val, params string[] names)
        {
            val = 0;
            if (!TryGetGameOption(out object raw, "GetInt", names)) return false;
            try { val = Convert.ToInt32(raw); return true; } catch { return false; }
        }

private static bool TryGetGameBool(out bool val, params string[] names)
        {
            val = false;
            if (!TryGetGameOption(out object raw, "GetBool", names)) return false;
            try { val = Convert.ToBoolean(raw); return true; } catch { return false; }
        }

private static bool TryGetGameOption(out object val, string typedGetter, params string[] names)
        {
            val = null;
            foreach (object opts in GetGameOptionsObjs())
            {
                if (opts == null) continue;
                Type t = opts.GetType();

                foreach (string n in names)
                {
                    try
                    {
                        object member = GetMemberValue(opts, n);
                        if (member != null) { val = member; return true; }
                    }
                    catch { }

                    PropertyInfo p = FindGameProp(t, n);
                    if (p != null)
                    {
                        try { val = p.GetValue(opts); return true; } catch { }
                    }

                    MethodInfo m0 = t.GetMethod("Get" + n, BindingFlags.Public | BindingFlags.Instance);
                    if (m0 != null && m0.GetParameters().Length == 0)
                    {
                        try { val = m0.Invoke(opts, null); return true; } catch { }
                    }
                }

                MethodInfo m = t.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(x => x.Name == typedGetter && x.GetParameters().Length == 1 && x.GetParameters()[0].ParameterType.IsEnum);
                if (m == null) continue;

                object e = FindGameEnum(m.GetParameters()[0].ParameterType, names);
                if (e == null) continue;
                try { val = m.Invoke(opts, new object[] { e }); return true; } catch { }
            }
            return false;
        }

private static bool TrySetGameFloat(float val, params string[] names) => TrySetGameOption(val, "SetFloat", names);
private static bool TrySetGameInt(int val, params string[] names) => TrySetGameOption(val, "SetInt", names);
private static bool TrySetGameBool(bool val, params string[] names) => TrySetGameOption(val, "SetBool", names);

private static bool TrySetGameFloatAll(float val, params string[] names)
        {
            bool ok = false;
            foreach (string n in names)
                ok |= TrySetGameFloat(val, n);
            return ok;
        }

private static bool TrySetGameOption(object val, string typedSetter, params string[] names)
        {
            bool ok = false;
            ok |= TrySetGameOptionObj(GetGameOptionsObj(), val, typedSetter, names);
            try
            {
                object mgr = GameOptionsManager.Instance;
                ok |= TrySetGameOptionObj(GetMemberValue(mgr, "currentNormalGameOptions"), val, typedSetter, names);
                ok |= TrySetGameOptionObj(GetMemberValue(mgr, "currentGameOptions"), val, typedSetter, names);
                ok |= TrySetGameOptionObj(GetMemberValue(mgr, "CurrentGameOptions"), val, typedSetter, names);
                ok |= TrySetGameOptionObj(GetMemberValue(mgr, "currentHideNSeekGameOptions"), val, typedSetter, names);
                ok |= TrySetGameOptionObj(GetMemberValue(mgr, "GameHostOptions"), val, typedSetter, names);
            }
            catch { }
            return ok;
        }

private static bool TrySetGameOptionObj(object opts, object val, string typedSetter, params string[] names)
        {
            if (opts == null) return false;
            Type t = opts.GetType();

            foreach (string n in names)
            {
                if (TrySetMemberValue(opts, val, n))
                    return true;

                PropertyInfo p = FindGameProp(t, n);
                if (p != null && p.CanWrite)
                {
                    try { p.SetValue(opts, CastGameValue(val, p.PropertyType)); return true; } catch { }
                }

                MethodInfo m0 = t.GetMethod("Set" + n, BindingFlags.Public | BindingFlags.Instance);
                if (m0 != null && m0.GetParameters().Length == 1)
                {
                    try { m0.Invoke(opts, new object[] { CastGameValue(val, m0.GetParameters()[0].ParameterType) }); return true; } catch { }
                }
            }

            MethodInfo m = t.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(x => x.Name == typedSetter && x.GetParameters().Length == 2 && x.GetParameters()[0].ParameterType.IsEnum);
            if (m == null) return false;

            object e = FindGameEnum(m.GetParameters()[0].ParameterType, names);
            if (e == null) return false;
            try
            {
                m.Invoke(opts, new object[] { e, CastGameValue(val, m.GetParameters()[1].ParameterType) });
                return true;
            }
            catch { return false; }
        }

private static PropertyInfo FindGameProp(Type t, string name)
        {
            if (t == null || string.IsNullOrEmpty(name)) return null;
            return t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
        }

private static object FindGameEnum(Type enumType, string[] names)
        {
            if (enumType == null || !enumType.IsEnum || names == null) return null;
            foreach (string n in names)
            {
                if (int.TryParse(n, NumberStyles.Integer, CultureInfo.InvariantCulture, out int raw))
                {
                    try { return Enum.ToObject(enumType, raw); } catch { }
                }
            }

            Array vals = Enum.GetValues(enumType);
            foreach (object v in vals)
            {
                string vn = v.ToString();
                foreach (string n in names)
                    if (string.Equals(vn, n, StringComparison.OrdinalIgnoreCase))
                        return v;
            }
            foreach (object v in vals)
            {
                string vn = v.ToString().ToLowerInvariant();
                foreach (string n in names)
                {
                    string nn = (n ?? "").ToLowerInvariant();
                    if (nn.Length > 0 && (vn.Contains(nn) || nn.Contains(vn)))
                        return v;
                }
            }
            return null;
        }

private static object CastGameValue(object val, Type target)
        {
            try
            {
                if (target == typeof(float)) return Convert.ToSingle(val);
                if (target == typeof(double)) return Convert.ToDouble(val);
                if (target == typeof(int)) return Convert.ToInt32(val);
                if (target == typeof(byte)) return Convert.ToByte(val);
                if (target == typeof(bool)) return Convert.ToBoolean(val);
                if (target.IsEnum) return Enum.ToObject(target, Convert.ToInt32(val));
            }
            catch { }
            return val;
        }

private static object GetMemberValue(object obj, string name)
        {
            if (obj == null || string.IsNullOrEmpty(name)) return null;
            try
            {
                Type t = obj.GetType();
                var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
                PropertyInfo p = t.GetProperty(name, flags);
                if (p != null) return p.GetValue(obj);
                FieldInfo f = t.GetField(name, flags);
                if (f != null) return f.GetValue(obj);
            }
            catch { }
            return null;
        }

private static void TouchGameOptions()
        {
            settingsDirty = true;
            lobbySettingsSyncHns = false;
            RepairHostGameOptions();
            GameOptionsManager mgr = null;
            IGameOptions opts = null;
            try
            {
                mgr = GameOptionsManager.Instance;
                opts = mgr?.CurrentGameOptions;
                if (mgr != null && opts != null) mgr.GameHostOptions = opts;
            }
            catch { }

            QueueLobbySettingsSync(0.25f);
        }

private static void TouchHnsGameOptions()
        {
            settingsDirty = true;
            lobbySettingsSyncHns = true;
            RepairHostGameOptions();
            try
            {
                GameOptionsManager mgr = GameOptionsManager.Instance;
                IGameOptions opts = GetHnsGameOptionsObj(mgr);
                if (mgr != null && opts != null)
                    mgr.GameHostOptions = opts;
            }
            catch { }

            QueueLobbySettingsSync(0.25f);
        }

private static IGameOptions GetHnsGameOptionsObj(GameOptionsManager mgr = null)
        {
            try
            {
                if (mgr == null) mgr = GameOptionsManager.Instance;
                object raw = GetMemberValue(mgr, "currentHideNSeekGameOptions");
                if (raw is IGameOptions opts) return opts;
                if (raw is Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase obj)
                    return obj.Cast<IGameOptions>();
            }
            catch { }
            return null;
        }

internal static bool BlockDirectSettingsSync()
        {
            if (!noSettingLimit || lobbySettingsSyncRun) return false;
            QueueLobbySettingsSync(0.25f);
            return true;
        }

internal static void QueueLobbySettingsSync(float delay = 0.25f)
        {
            try
            {
                if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return;
                lobbySettingsSyncQueued = true;
                float at = Time.unscaledTime + Mathf.Max(0.05f, delay);
                if (lobbySettingsSyncAt <= 0f || at < lobbySettingsSyncAt)
                    lobbySettingsSyncAt = at;
            }
            catch { }
        }

internal static void TickLobbySettingsSync()
        {
            if (!lobbySettingsSyncQueued) return;
            if (Time.unscaledTime < lobbySettingsSyncAt) return;
            try
            {
                InnerNetClient client = AmongUsClient.Instance;
                if (client == null || !client.AmHost) { lobbySettingsSyncQueued = false; return; }
                if (client.GameState != InnerNetClient.GameStates.Joined || client.IsGameStarted)
                {
                    lobbySettingsSyncAt = Time.unscaledTime + 0.5f;
                    return;
                }

                lobbySettingsSyncQueued = false;
                SyncLobbySettingsNow();
            }
            catch
            {
                lobbySettingsSyncAt = Time.unscaledTime + 0.5f;
            }
        }

private static void SyncLobbySettingsNow()
        {
            GameOptionsManager mgr = null;
            IGameOptions opts = null;
            bool syncHns = lobbySettingsSyncHns;

            try
            {
                mgr = GameOptionsManager.Instance;
                opts = syncHns ? GetHnsGameOptionsObj(mgr) : mgr?.CurrentGameOptions;
                if (opts == null) opts = mgr?.CurrentGameOptions;
                if (mgr != null && opts != null)
                    mgr.GameHostOptions = opts;
            }
            catch { }

            lobbySettingsSyncRun = true;
            try
            {
                object logic = GameManager.Instance != null ? GameManager.Instance.LogicOptions : null;
                MethodInfo sync = logic?.GetType().GetMethod("SyncOptions", BindingFlags.Public | BindingFlags.Instance);
                if (sync != null)
                {
                    sync.Invoke(logic, null);
                    return;
                }
            }
            catch { }
            finally
            {
                lobbySettingsSyncRun = false;
                lobbySettingsSyncHns = false;
            }

            if (noSettingLimit && !syncHns) return;

            lobbySettingsSyncRun = true;
            try
            {
                PlayerControl lp = PlayerControl.LocalPlayer;
                Il2CppStructArray<byte> raw = mgr?.gameOptionsFactory?.ToBytes(opts, false);
                if (lp != null && raw != null && raw.Length > 0) lp.RpcSyncSettings(raw);
            }
            catch { }
            finally
            {
                lobbySettingsSyncRun = false;
            }
        }

internal static void RepairHostGameOptions()
        {
            try
            {
                GameOptionsManager mgr = GameOptionsManager.Instance;
                if (mgr == null) return;
                RepairGameOptions(mgr.CurrentGameOptions);
                RepairGameOptions(mgr.GameHostOptions);
                RepairGameOptions(mgr.currentNormalGameOptions?.Cast<IGameOptions>());
                RepairGameOptions(mgr.currentHideNSeekGameOptions?.Cast<IGameOptions>());
                try { mgr.SaveNormalHostOptions(); } catch { }
                try { mgr.SaveHideNSeekHostOptions(); } catch { }
            }
            catch { }
        }

private static void RepairGameOptions(IGameOptions opts)
        {
            if (opts == null) return;
            if (noSettingLimit)
            {
                try
                {
                    float spd = opts.GetFloat(FloatOptionNames.PlayerSpeedMod);
                    if (spd < 0.00001f) opts.SetFloat(FloatOptionNames.PlayerSpeedMod, 0.00001f);

                    float killCd = opts.GetFloat(FloatOptionNames.KillCooldown);
                    if (killCd < 0.00001f) opts.SetFloat(FloatOptionNames.KillCooldown, 0.00001f);
                }
                catch { }
                return;
            }
            try
            {
                int plrs = Mathf.Clamp(opts.MaxPlayers, 4, 15);
                if (opts.MaxPlayers != plrs)
                    opts.SetInt(Int32OptionNames.MaxPlayers, plrs);

                if (opts.MapId > 5)
                    opts.SetByte(ByteOptionNames.MapId, 0);

                float spd = opts.GetFloat(FloatOptionNames.PlayerSpeedMod);
                if (spd <= 0f) opts.SetFloat(FloatOptionNames.PlayerSpeedMod, 0.000001f);

                float killCd = opts.GetFloat(FloatOptionNames.KillCooldown);
                if (killCd <= 0f) opts.SetFloat(FloatOptionNames.KillCooldown, 0.000001f);
            }
            catch { }
        }
    }
}
