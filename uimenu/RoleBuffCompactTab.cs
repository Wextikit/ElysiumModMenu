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
private void DrawPlayerMovementCompact(float columnWidth)
        {
            GUILayout.BeginVertical();
            DrawMenuSectionHeader("MOVEMENT & TELEPORT");
            int controlWidth = Mathf.RoundToInt(Mathf.Clamp(columnWidth - 26f, 170f, 280f));

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Engine: {Mathf.Round(engineSpeed)}x", new GUIStyle(toggleLabelStyle) { fontSize = 11 }, GUILayout.Width(72), GUILayout.Height(18));
            engineSpeed = GUILayout.HorizontalSlider(engineSpeed, 1f, 555f, sliderStyle, sliderThumbStyle, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("R", btnStyle, GUILayout.Width(24), GUILayout.Height(18))) engineSpeed = 1f;
            GUILayout.EndHorizontal();

            GUILayout.Space(2);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Walk: {Mathf.Round(walkSpeed)}x", new GUIStyle(toggleLabelStyle) { fontSize = 11 }, GUILayout.Width(72), GUILayout.Height(18));
            walkSpeed = GUILayout.HorizontalSlider(walkSpeed, 1f, 30f, sliderStyle, sliderThumbStyle, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("R", btnStyle, GUILayout.Width(24), GUILayout.Height(18))) walkSpeed = 1f;
            GUILayout.EndHorizontal();

            GUILayout.Space(4);
            tpToCursor = DrawCompactToggle(tpToCursor, "TP To Cursor", controlWidth);
            GUILayout.Space(1);
            dragToCursor = DrawCompactToggle(dragToCursor, "Drag To Cursor", controlWidth);
            GUILayout.Space(1);
            autoFollowCursor = DrawCompactToggle(autoFollowCursor, $"Magnet Cursor ({bindMagnetCursor})", controlWidth);
            GUILayout.Space(1);
            noClip = DrawCompactToggle(noClip, "True NoClip", controlWidth);

            GUILayout.EndVertical();
        }

private void DrawRolesCompact(float columnWidth)
        {
            GUILayout.BeginVertical();
            DrawMenuSectionHeader("ROLE TOOLS");
            int roleToggleWidth = Mathf.RoundToInt(Mathf.Clamp(columnWidth - 26f, 170f, 280f));

            GUIStyle roleMidStyle = new GUIStyle(btnStyle)
            {
                fontStyle = FontStyle.Bold,
                normal = { background = null, textColor = GetMenuAccentColor() },
                alignment = TextAnchor.MiddleCenter
            };

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("<", btnStyle, GUILayout.Width(24), GUILayout.Height(20)))
            {
                fakeRoleIdx--;
                if (fakeRoleIdx < 0) fakeRoleIdx = forceRoleOptions.Length - 1;
            }
            GUILayout.Label(GetLocalRoleDisplayName(forceRoleOptions[fakeRoleIdx]), roleMidStyle, GUILayout.ExpandWidth(true), GUILayout.Height(20));
            if (GUILayout.Button(">", btnStyle, GUILayout.Width(24), GUILayout.Height(20)))
            {
                fakeRoleIdx++;
                if (fakeRoleIdx >= forceRoleOptions.Length) fakeRoleIdx = 0;
            }
            if (GUILayout.Button("Set", activeTabStyle, GUILayout.Width(38), GUILayout.Height(20)))
                RoleManager.Instance?.SetRole(PlayerControl.LocalPlayer, forceRoleOptions[fakeRoleIdx]);
            GUILayout.EndHorizontal();

            GUILayout.Space(4);
            DrawRoleBuffSubTabs();
            GUILayout.Space(4);

            if (currentRoleBuffSubTab == 0) DrawNonHostRoleBuffs(columnWidth, roleToggleWidth);
            else DrawHostRoleBuffs(roleToggleWidth);

            GUILayout.EndVertical();
        }

private static string GetLocalRoleDisplayName(RoleTypes role)
        {
            int roleId = (int)role;
            if (roleId == 9) return "Phantom";
            if (roleId == 18) return "Viper";
            if (roleId == 8) return "Noisemaker";
            if (roleId == 10) return "Tracker";
            if (roleId == 12) return "Detective";
            return role.ToString();
        }

private void DrawRoleBuffSubTabs()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("NON HOST", currentRoleBuffSubTab == 0 ? activeSubTabStyle : subTabStyle, GUILayout.Height(18)))
                currentRoleBuffSubTab = 0;
            if (GUILayout.Button("HOST", currentRoleBuffSubTab == 1 ? activeSubTabStyle : subTabStyle, GUILayout.Height(18)))
                currentRoleBuffSubTab = 1;
            GUILayout.EndHorizontal();
        }

private void DrawNonHostRoleBuffs(float availableWidth, int width)
        {
            bool twoColumns = availableWidth >= 560f;
            int colWidth = twoColumns
                ? Mathf.RoundToInt(Mathf.Clamp((availableWidth - 42f) * 0.5f, 170f, 235f))
                : width;

            if (twoColumns)
            {
                GUILayout.BeginHorizontal();
                GUILayout.BeginVertical(GUILayout.Width(colWidth));
                DrawMenuSectionHeader("IMPOSTOR");
                killReach = DrawCompactToggle(killReach, "Kill Reach", colWidth);
                GUILayout.Space(2);
                killAnyone = DrawCompactToggle(killAnyone, "Kill Anyone", colWidth);
                GUILayout.Space(2);
                killAuraHostOnly = DrawCompactToggle(killAuraHostOnly, "Kill Aura", colWidth);
                GUILayout.Space(2);
                allowTasksAsImpostor = DrawCompactToggle(allowTasksAsImpostor, "Allow Tasks (Imp)", colWidth);
                GUILayout.Space(2);
                spamReportBodies = DrawCompactToggle(spamReportBodies, "Spam Report Bodies", colWidth);

                GUILayout.Space(5);
                DrawMenuSectionHeader("SHAPESHIFTER");
                NoShapeshiftAnim = DrawCompactToggle(NoShapeshiftAnim, "No Ss Animation", colWidth);
                GUILayout.Space(2);
                endlessSsDuration = DrawCompactToggle(endlessSsDuration, "Endless Ss Duration", colWidth);

                GUILayout.Space(5);
                DrawMenuSectionHeader("TRACKER");
                EndlessTracking = DrawCompactToggle(EndlessTracking, "Endless Tracking", colWidth);
                GUILayout.Space(2);
                NoTrackingCooldown = DrawCompactToggle(NoTrackingCooldown, "No Track Cooldown", colWidth);
                GUILayout.EndVertical();

                GUILayout.Space(8);
                GUILayout.BeginVertical(GUILayout.Width(colWidth));
                DrawMenuSectionHeader("ENGINEER");
                endlessVentTime = DrawCompactToggle(endlessVentTime, "Endless Vent Time", colWidth);
                GUILayout.Space(2);
                noVentCooldown = DrawCompactToggle(noVentCooldown, "No Vent Cooldown", colWidth);
                GUILayout.Space(2);
                unlockVents = DrawCompactToggle(unlockVents, "Unlock Vents", colWidth);
                GUILayout.Space(2);
                walkInVents = DrawCompactToggle(walkInVents, "Walk In Vents", colWidth);
                GUILayout.Space(2);
                noMapCooldowns = DrawCompactToggle(noMapCooldowns, "No Map Cooldowns", colWidth);

                GUILayout.Space(5);
                DrawMenuSectionHeader("SCIENTIST");
                endlessBattery = DrawCompactToggle(endlessBattery, "Endless Battery", colWidth);
                GUILayout.Space(2);
                noVitalsCooldown = DrawCompactToggle(noVitalsCooldown, "No Vitals Cooldown", colWidth);

                GUILayout.Space(5);
                DrawMenuSectionHeader("DETECTIVE");
                UnlimitedInterrogateRange = DrawCompactToggle(UnlimitedInterrogateRange, "Interrogate Reach", colWidth);

                GUILayout.Space(5);
                DrawMenuSectionHeader("GLOBAL");
                roleBuffImmortality = DrawCompactToggle(roleBuffImmortality, "Immortality", colWidth);
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
                return;
            }

            DrawMenuSectionHeader("IMPOSTOR");
            killReach = DrawCompactToggle(killReach, "Kill Reach", width);
            GUILayout.Space(2);
            killAnyone = DrawCompactToggle(killAnyone, "Kill Anyone", width);
            GUILayout.Space(2);
            killAuraHostOnly = DrawCompactToggle(killAuraHostOnly, "Kill Aura", width);
            GUILayout.Space(2);
            allowTasksAsImpostor = DrawCompactToggle(allowTasksAsImpostor, "Allow Tasks (Imp)", width);
            GUILayout.Space(2);
            spamReportBodies = DrawCompactToggle(spamReportBodies, "Spam Report Bodies", width);

            GUILayout.Space(5);
            DrawMenuSectionHeader("SHAPESHIFTER");
            NoShapeshiftAnim = DrawCompactToggle(NoShapeshiftAnim, "No Ss Animation", width);
            GUILayout.Space(2);
            endlessSsDuration = DrawCompactToggle(endlessSsDuration, "Endless Ss Duration", width);

            GUILayout.Space(5);
            DrawMenuSectionHeader("TRACKER");
            EndlessTracking = DrawCompactToggle(EndlessTracking, "Endless Tracking", width);
            GUILayout.Space(2);
            NoTrackingCooldown = DrawCompactToggle(NoTrackingCooldown, "No Track Cooldown", width);

            GUILayout.Space(5);
            DrawMenuSectionHeader("ENGINEER");
            endlessVentTime = DrawCompactToggle(endlessVentTime, "Endless Vent Time", width);
            GUILayout.Space(2);
            noVentCooldown = DrawCompactToggle(noVentCooldown, "No Vent Cooldown", width);
            GUILayout.Space(2);
            unlockVents = DrawCompactToggle(unlockVents, "Unlock Vents", width);
            GUILayout.Space(2);
            walkInVents = DrawCompactToggle(walkInVents, "Walk In Vents", width);
            GUILayout.Space(2);
            noMapCooldowns = DrawCompactToggle(noMapCooldowns, "No Map Cooldowns", width);

            GUILayout.Space(5);
            DrawMenuSectionHeader("SCIENTIST");
            endlessBattery = DrawCompactToggle(endlessBattery, "Endless Battery", width);
            GUILayout.Space(2);
            noVitalsCooldown = DrawCompactToggle(noVitalsCooldown, "No Vitals Cooldown", width);

            GUILayout.Space(5);
            DrawMenuSectionHeader("DETECTIVE");
            UnlimitedInterrogateRange = DrawCompactToggle(UnlimitedInterrogateRange, "Interrogate Reach", width);

            GUILayout.Space(5);
            DrawMenuSectionHeader("GLOBAL");
            roleBuffImmortality = DrawCompactToggle(roleBuffImmortality, "Immortality", width);
        }

private void DrawHostRoleBuffs(int width)
        {
            DrawMenuSectionHeader("IMPOSTOR");
            noKillCooldownHostOnly = DrawCompactToggle(noKillCooldownHostOnly, "Kill Cooldown 0", width);
            GUILayout.Space(2);
            killWhileVanishedHostOnly = DrawCompactToggle(killWhileVanishedHostOnly, "Kill While Vanished", width);
        }
    }
}
