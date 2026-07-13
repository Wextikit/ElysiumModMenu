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
private void DrawPlayersRoles()
        {
            GUILayout.BeginVertical(menuCardStyle);
            DrawMenuSectionHeader("PRE-GAME ROLE MANAGER");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(enablePreGameRoleForce ? "Role Forcing: ON" : "Role Forcing: OFF", enablePreGameRoleForce ? activeTabStyle : btnStyle, GUILayout.Height(25))) enablePreGameRoleForce = !enablePreGameRoleForce;
            if (GUILayout.Button("Random 2 Imps", btnStyle, GUILayout.Width(110), GUILayout.Height(25)))
            {
                autoTwoImpostorPlayerIds.Clear();
                autoTwoImpostorsLastLobbyFingerprint = 0;
                RollAutoTwoImpostors(true);
            }
            if (GUILayout.Button(autoTwoImpostors ? "Auto 2 Imps: ON" : "Auto 2 Imps", autoTwoImpostors ? activeTabStyle : btnStyle, GUILayout.Width(120), GUILayout.Height(25)))
            {
                autoTwoImpostors = !autoTwoImpostors;
                if (autoTwoImpostors)
                {
                    autoTwoImpostorsNeedsGameStartRoll = true;
                    autoTwoImpostorsLastLobbyFingerprint = 0;
                    RollAutoTwoImpostors(true);
                }
                else
                {
                    ClearAutoTwoImpostorSelection();
                    autoTwoImpostorsLastLobbyFingerprint = 0;
                }
            }
            if (GUILayout.Button("Clear All Roles", btnStyle, GUILayout.Width(110), GUILayout.Height(25))) { autoTwoImpostors = false; autoTwoImpostorPlayerIds.Clear(); autoTwoImpostorsLastLobbyFingerprint = 0; forcedPreGameRoles.Clear(); forcedImpostors.Clear(); forcedPreGameRoleFcs.Clear(); forcedImpostorFcs.Clear(); }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.Space(8);
            GUILayout.BeginVertical(menuCardStyle);
            DrawMenuSectionHeader("LIVE ROLE DISTRIBUTOR (HOST)");
            GUILayout.BeginHorizontal();

            GUIStyle allRoleMidStyle = new GUIStyle(btnStyle)
            {
                fontStyle = FontStyle.Bold,
                normal = { background = null, textColor = GetMenuAccentColor() },
                alignment = TextAnchor.MiddleCenter
            };

            if (GUILayout.Button("<", btnStyle, GUILayout.Width(28), GUILayout.Height(25)))
            {
                allPlayersRoleAssignIdx--;
                if (allPlayersRoleAssignIdx < 0) allPlayersRoleAssignIdx = roleAssignOptions.Length - 1;
            }

            GUILayout.Label(roleAssignNames[allPlayersRoleAssignIdx], allRoleMidStyle, GUILayout.Height(25), GUILayout.ExpandWidth(true));

            if (GUILayout.Button(">", btnStyle, GUILayout.Width(28), GUILayout.Height(25)))
            {
                allPlayersRoleAssignIdx++;
                if (allPlayersRoleAssignIdx >= roleAssignOptions.Length) allPlayersRoleAssignIdx = 0;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
            if (GUILayout.Button("SET ALL PLAYERS ROLE", activeTabStyle, GUILayout.Height(28)))
            {
                if (IsGhostRoleSelection(allPlayersRoleAssignIdx))
                    SetAllPlayersGhost();
                else if (IsGhostImpostorRoleSelection(allPlayersRoleAssignIdx))
                    SetAllPlayersGhost(true);
                else
                    SetAllPlayersRole(roleAssignOptions[allPlayersRoleAssignIdx]);
            }
            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("ALL -> GHOST", btnStyle, GUILayout.Height(26)))
                SetAllPlayersGhost();
            if (GUILayout.Button("REVIVE ALL", activeTabStyle, GUILayout.Height(26)))
                ReviveAllPlayers();
            GUILayout.EndHorizontal();
            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("ALL -> GHOST IMP", btnStyle, GUILayout.Height(26)))
                SetAllPlayersGhost(true);
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(menuCardStyle, GUILayout.Width(150), GUILayout.Height(315));
            preRolesListScrollPos = GUILayout.BeginScrollView(preRolesListScrollPos, GUILayout.ExpandHeight(true));
            foreach (var pc in lockedPlayersList)
            {
                if (pc == null || pc.Data == null || pc.PlayerId >= 100) continue;
                string pName = pc.Data.PlayerName ?? "Unknown";
                string fc = GetRoleForceKey(pc);
                if (TryGetForcedRole(pc, out RoleTypes rowRole)) { string rShort = rowRole.ToString().Replace("9", "Pha").Replace("10", "Tra").Replace("8", "Noi").Replace("12", "Det").Replace("18", "Vip"); if (rShort.Length > 3) rShort = rShort.Substring(0, 3); pName += $" [{rShort}]"; }
                else if (IsForcedImp(pc)) pName += " [Imp]";
                bool isSelected = !string.IsNullOrEmpty(fc) ? selectedPreRoleFc == fc : selectedPreRoleId == pc.PlayerId;
                try { GUI.contentColor = Palette.PlayerColors[pc.Data.DefaultOutfit.ColorId]; } catch { }
                if (GUILayout.Button(pName, isSelected ? activeTabStyle : btnStyle, GUILayout.Height(30))) { selectedPreRoleId = pc.PlayerId; selectedPreRoleFc = fc; }
                GUI.contentColor = Color.white;
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.Space(8);
            GUILayout.BeginVertical(menuCardStyle, GUILayout.ExpandWidth(true), GUILayout.Height(315));
            preRolesActionScrollPos = GUILayout.BeginScrollView(preRolesActionScrollPos, GUILayout.ExpandHeight(true));
            PlayerControl target = !string.IsNullOrEmpty(selectedPreRoleFc)
                ? lockedPlayersList.FirstOrDefault(p => GetRoleForceKey(p) == selectedPreRoleFc)
                : lockedPlayersList.FirstOrDefault(p => p.PlayerId == selectedPreRoleId);
            if (target != null && target.Data != null)
            {
                GUIStyle infoStyle = new GUIStyle(GUI.skin.label) { richText = true, fontSize = 14 };
                GUILayout.Label($"<color=#aaaaaa>Selecting role for:</color> {target.Data.PlayerName}", infoStyle);
                RoleTypes currentForced = TryGetForcedRole(target, out RoleTypes targetRole) ? targetRole : RoleTypes.Crewmate;
                bool isForced = IsForced(target);
                string roleNameStr = currentForced.ToString().Replace("9", "Phantom").Replace("10", "Tracker").Replace("8", "Noisemaker").Replace("12", "Detective").Replace("18", "Viper");
                if (IsForcedImp(target)) roleNameStr = "Impostor";
                string targetFc = GetRoleForceKey(target);
                GUILayout.Label($"<color=#aaaaaa>Status:</color> {(isForced ? $"<color=#00FF00>Forced ({roleNameStr})</color>" : "<color=#FF0000>Not Forced (Random)</color>")}", infoStyle);
                GUILayout.Label($"<color=#aaaaaa>FC:</color> {(string.IsNullOrEmpty(targetFc) ? "none, fallback PlayerId" : targetFc)}", new GUIStyle(GUI.skin.label) { richText = true, fontSize = 11 });
                GUILayout.Space(15);
                DrawMenuSectionHeader("IMPOSTOR ROLES (Red Team)");
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Impostor", btnStyle, GUILayout.Height(24))) SetForcedImp(target);
                if (GUILayout.Button("Shapeshifter", btnStyle, GUILayout.Height(24))) SetForcedRole(target, RoleTypes.Shapeshifter);
                if (GUILayout.Button("Phantom", btnStyle, GUILayout.Height(24))) SetForcedRole(target, (RoleTypes)9);
                if (GUILayout.Button("Viper", btnStyle, GUILayout.Height(24))) SetForcedRole(target, (RoleTypes)18);
                GUILayout.EndHorizontal();
                GUILayout.Space(10);
                DrawMenuSectionHeader("CREWMATE ROLES (Blue Team)");
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Crewmate", btnStyle, GUILayout.Height(24))) SetForcedRole(target, RoleTypes.Crewmate);
                if (GUILayout.Button("Engineer", btnStyle, GUILayout.Height(24))) SetForcedRole(target, RoleTypes.Engineer);
                if (GUILayout.Button("Scientist", btnStyle, GUILayout.Height(24))) SetForcedRole(target, RoleTypes.Scientist);
                if (GUILayout.Button("Tracker", btnStyle, GUILayout.Height(24))) SetForcedRole(target, (RoleTypes)10);
                GUILayout.EndHorizontal();
                GUILayout.Space(5);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Noisemaker", btnStyle, GUILayout.Height(24))) SetForcedRole(target, (RoleTypes)8);
                if (GUILayout.Button("Guardian Angel", btnStyle, GUILayout.Height(24))) SetForcedRole(target, RoleTypes.GuardianAngel);
                if (GUILayout.Button("Detective", btnStyle, GUILayout.Height(24))) SetForcedRole(target, (RoleTypes)12);
                GUILayout.EndHorizontal();
                GUILayout.Space(15);
                if (GUILayout.Button("REMOVE FORCED ROLE", activeTabStyle, GUILayout.Height(35))) ClearForcedRole(target);
                GUILayout.Space(20);
                GUILayout.Label("<color=#777777><b>Hide & Seek Notice:</b>\nР’С‹Р±РѕСЂ Impostor/Shapeshifter/Phantom/Viper СЂР°СЃС€РёСЂРёС‚ Р»РёРјРёС‚ РјР°РЅСЊСЏРєРѕРІ (Seekers) РІ РџСЂСЏС‚РєР°С…!</color>", new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true });
            }
            else
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("<color=#777777>Select a player to set their role</color>", new GUIStyle(GUI.skin.label) { richText = true, alignment = TextAnchor.MiddleCenter });
                GUILayout.FlexibleSpace();
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }
    }
}
