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
private void DrawPlayerMovement()
        {
            GUILayout.BeginVertical(boxStyle);
            try
            {
                GUILayout.Label("MOVEMENT & TELEPORT", headerStyle);

                GUILayout.BeginHorizontal();
                try
                {
                    GUILayout.Label($"Engine Speed: {Mathf.Round(engineSpeed)}x", GUILayout.Width(130));
                    engineSpeed = GUILayout.HorizontalSlider(engineSpeed, 1f, 555f, sliderStyle, sliderThumbStyle, GUILayout.ExpandWidth(true));
                    GUILayout.Space(10);
                    if (GUILayout.Button("Reset", btnStyle, GUILayout.Width(50), GUILayout.Height(20))) engineSpeed = 1f;
                }
                finally { GUILayout.EndHorizontal(); }

                GUILayout.Space(5);

                GUILayout.BeginHorizontal();
                try
                {
                    GUILayout.Label($"Walk Speed: {Mathf.Round(walkSpeed)}x", GUILayout.Width(130));
                    walkSpeed = GUILayout.HorizontalSlider(walkSpeed, 1f, 30f, sliderStyle, sliderThumbStyle, GUILayout.ExpandWidth(true));
                    GUILayout.Space(10);
                    if (GUILayout.Button("Reset", btnStyle, GUILayout.Width(50), GUILayout.Height(20))) walkSpeed = 1f;
                }
                finally { GUILayout.EndHorizontal(); }

                GUILayout.Space(5);

                GUILayout.BeginHorizontal();
                try
                {
                    tpToCursor = DrawToggle(tpToCursor, "TP To Cursor", 160);
                    dragToCursor = DrawToggle(dragToCursor, "Drag To Cursor", 160);
                    GUILayout.FlexibleSpace();
                }
                finally { GUILayout.EndHorizontal(); }

                GUILayout.Space(5);

                GUILayout.BeginHorizontal();
                try
                {
                    autoFollowCursor = DrawToggle(autoFollowCursor, $"Magnet Cursor ({bindMagnetCursor})", 160);
                    noClip = DrawToggle(noClip, "True NoClip", 160);
                    GUILayout.FlexibleSpace();
                }
                finally { GUILayout.EndHorizontal(); }
            }
            finally { GUILayout.EndVertical(); }
        }

private void SmartEndGame(string outcome)
        {
            if (!CanRunHostEndGameAction(true)) return;

            bool isHns = GameManager.Instance.IsHideAndSeek();
            int reasonCode = 4;

            switch (outcome)
            {
                case "CrewWin": reasonCode = isHns ? 7 : 0; break;
                case "ImpWin": reasonCode = isHns ? 8 : 3; break;
                case "ImpDisconnect":
                case "HnsImpDisconnect": reasonCode = 5; break;
            }

            bool tempBlock = neverEndGame;
            neverEndGame = false;
            GameManager.Instance.RpcEndGame((GameOverReason)reasonCode, false);
            neverEndGame = tempBlock;
        }

private static string SanitizeSpoofFriendCode(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";

            string clean = "";
            foreach (char c in input.ToLowerInvariant())
            {
                if (char.IsWhiteSpace(c)) break;
                if (char.IsLetterOrDigit(c)) clean += c;
                if (clean.Length >= 10) break;
            }
            return clean;
        }

private static string SanitizeHexColor(string input, string fallback)
        {
            string value = (input ?? string.Empty).Trim();
            if (value.StartsWith("#")) value = value.Substring(1);

            string clean = "";
            foreach (char c in value)
            {
                if ((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'))
                {
                    clean += char.ToUpperInvariant(c);
                    if (clean.Length >= 6) break;
                }
            }

            return clean.Length == 6 ? "#" + clean : fallback;
        }
    }
}
