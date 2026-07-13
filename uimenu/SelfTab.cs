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
private void DrawSelfTab()
        {
            float selfContentWidth = GetMenuWorkWidth(220f, 610f);
            currentSelfSubTab = Mathf.Clamp(currentSelfSubTab, 0, selfSubTabs.Length - 1);
            GUIStyle compactSubTab = new GUIStyle(subTabStyle) { fontSize = 10, padding = CreateRectOffset(5, 5, 1, 1) };
            GUIStyle compactActiveSubTab = new GUIStyle(activeSubTabStyle) { fontSize = 10, padding = CreateRectOffset(5, 5, 1, 1) };

            GUILayout.BeginVertical(GUILayout.Width(selfContentWidth));
            GUILayout.BeginHorizontal(GUILayout.Width(selfContentWidth));
            for (int i = 0; i < selfSubTabs.Length; i++)
            {
                if (GUILayout.Button(selfSubTabs[i], currentSelfSubTab == i ? compactActiveSubTab : compactSubTab, GUILayout.Height(18)))
                {
                    currentSelfSubTab = i;
                    scrollPosition = Vector2.zero;
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(3);

            if (currentSelfSubTab == 0)
            {
                DrawSelfSpoof();
            }
            else
            {
                GUILayout.BeginVertical(CreateCompactMenuCardStyle(), GUILayout.Width(selfContentWidth), GUILayout.ExpandHeight(false));
                if (currentSelfSubTab == 1) DrawRolesCompact(selfContentWidth);
                else if (currentSelfSubTab == 2) DrawPlayerMovementCompact(selfContentWidth);
                else if (currentSelfSubTab == 3) DrawChatSettingsCompact(selfContentWidth);
                GUILayout.EndVertical();
            }

            GUILayout.EndVertical();
        }
    }
}
