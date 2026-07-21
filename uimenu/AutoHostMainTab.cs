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
private void DrawAutoHostMainTab()
        {
            GUILayout.BeginHorizontal();
            for (int i = 0; i < autoHostSubTabs.Length; i++)
            {
                string subTabLabel = i < hostOnlySubTabs.Length ? hostOnlySubTabs[i] : autoHostSubTabs[i];
                if (GUILayout.Button(subTabLabel, currentAutoHostSubTab == i ? activeSubTabStyle : subTabStyle, GUILayout.Height(18)))
                    SetMultiTab("autoHost", ref currentAutoHostSubTab, i, autoHostSubTabs.Length);
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(8);

            BeginMultiTabContent("autoHost", out Matrix4x4 oldMatrix, out Color oldColor);
            try
            {
                if (currentAutoHostSubTab == 0) DrawLobbyControls();
                else if (currentAutoHostSubTab == 1) DrawPlayersRoles();
                else if (currentAutoHostSubTab == 2) DrawAntiCheatTab();
                else if (currentAutoHostSubTab == 3) DrawAutoHostTab();
            }
            finally
            {
                EndMultiTabContent(oldMatrix, oldColor);
            }
    }
}
}
