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
private void DrawGeneralInfoTab()
        {
            GUILayout.BeginVertical(boxStyle);
            GUILayout.Label("ELYSIUM OVERVIEW", headerStyle);
            GUILayout.Space(6);

            if (!generalInfoSubTabWidthsReady)
            {
                for (int i = 0; i < generalInfoSubTabs.Length; i++)
                {
                    tabSizeContent.text = generalInfoSubTabs[i];
                    generalInfoSubTabWidths[i] = Mathf.Max(116f, Mathf.Ceil(subTabStyle.CalcSize(tabSizeContent).x) + 28f);
                }
                generalInfoSubTabWidthsReady = true;
            }

            GUILayout.BeginHorizontal();
            for (int i = 0; i < generalInfoSubTabs.Length; i++)
            {
                GUIStyle tabStyle = currentGeneralInfoSubTab == i ? activeSubTabStyle : subTabStyle;
                if (GUILayout.Button(generalInfoSubTabs[i], tabStyle, GUILayout.Width(generalInfoSubTabWidths[i]), GUILayout.Height(24)))
                    SetMultiTab("generalInfo", ref currentGeneralInfoSubTab, i, generalInfoSubTabs.Length, false);
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            BeginMultiTabContent("generalInfo", out Matrix4x4 oldMatrix, out Color oldColor);
            try
            {
            string accentHex = GetMenuAccentHex();
            bool rgbText = RgbMenuTextActive();
            string githubHex = rgbText ? accentHex : ColorUtility.ToHtmlStringRGB(whiteMenuTheme ? GetThemeAccentColor(new Color32(26, 188, 156, 255)) : new Color32(26, 188, 156, 255));
            string goldHex = rgbText ? accentHex : ColorUtility.ToHtmlStringRGB(whiteMenuTheme ? GetThemeAccentColor(new Color32(255, 187, 54, 255)) : new Color32(255, 187, 54, 255));
            string leadHex = rgbText ? accentHex : ColorUtility.ToHtmlStringRGB(whiteMenuTheme ? GetThemeAccentColor(new Color32(255, 92, 122, 255)) : new Color32(255, 92, 122, 255));
            string devHex = rgbText ? accentHex : ColorUtility.ToHtmlStringRGB(whiteMenuTheme ? GetThemeAccentColor(new Color32(38, 194, 129, 255)) : new Color32(38, 194, 129, 255));
            string contributorHex = rgbText ? accentHex : ColorUtility.ToHtmlStringRGB(whiteMenuTheme ? GetThemeAccentColor(new Color32(109, 138, 255, 255)) : new Color32(109, 138, 255, 255));
            string dangerHex = rgbText ? accentHex : ColorUtility.ToHtmlStringRGB(whiteMenuTheme ? GetThemeAccentColor(new Color32(231, 76, 60, 255)) : new Color32(231, 76, 60, 255));
            string safeHex = rgbText ? accentHex : ColorUtility.ToHtmlStringRGB(whiteMenuTheme ? GetThemeAccentColor(new Color32(57, 255, 20, 255)) : new Color32(57, 255, 20, 255));
            string versionText = Plugin.PluginVersion;

            GUIStyle textStyle = richWrapLabelStyle12;
            textStyle.normal.textColor = whiteMenuTheme ? new Color(0.16f, 0.16f, 0.16f, 1f) : new Color(0.85f, 0.85f, 0.85f, 1f);

            if (currentGeneralInfoSubTab == 0)
            {
                GUILayout.BeginVertical(boxStyle);
                GUILayout.Label(
                    $"{L("Welcome to", "Р”РѕР±СЂРѕ РїРѕР¶Р°Р»РѕРІР°С‚СЊ РІ")} <b><color=#{accentHex}>ElysiumModMenu</color></b> " +
                    $"<b><color=#{goldHex}>v{versionText}</color></b> {L("by", "РѕС‚")} <b><color=#{leadHex}>Meowchelo</color></b>!",
                    textStyle);
                GUILayout.Space(4);
                GUILayout.Label(L(
                    "ElysiumModMenu is a lightweight BepInEx IL2CPP utility for Among Us with lobby tools, visuals, spoofing and host-side controls.",
                    "ElysiumModMenu СЌС‚Рѕ Р»РµРіРєРёР№ BepInEx IL2CPP РјРѕРґ РґР»СЏ Among Us СЃ РёРЅСЃС‚СЂСѓРјРµРЅС‚Р°РјРё РґР»СЏ Р»РѕР±Р±Рё, РІРёР·СѓР°Р»РѕРј, СЃРїСѓС„РёРЅРіРѕРј Рё С…РѕСЃС‚-С„СѓРЅРєС†РёСЏРјРё."), textStyle);
                GUILayout.Label(L(
                    "Use the buttons below to open the GitHub repository or jump straight to the latest public release.",
                    "РљРЅРѕРїРєРё РЅРёР¶Рµ РѕС‚РєСЂС‹РІР°СЋС‚ GitHub СЂРµРїРѕР·РёС‚РѕСЂРёР№ Рё СЃС‚СЂР°РЅРёС†Сѓ СЃ РїРѕСЃР»РµРґРЅРёРј РїСѓР±Р»РёС‡РЅС‹Рј СЂРµР»РёР·РѕРј."), textStyle);
                GUILayout.Space(6);

                GUILayout.BeginHorizontal();
                if (DrawColoredActionButton("GitHub", new Color32(26, 188, 156, 255), 110f))
                    OpenExternalLink("https://github.com/Wextikit/ElysiumModMenu", "GitHub");
                GUILayout.Space(6);
                DrawUpdateActionButton();
                GUILayout.Space(6);
                if (DrawColoredActionButton("Discord", new Color32(88, 101, 242, 255), 110f))
                    OpenExternalLink("https://discord.gg/CdrpKJzFp", "Discord");
                GUILayout.EndHorizontal();

                GUILayout.Space(8);
                GUILayout.Label(BuildUpdateStatusText(), textStyle);
                GUILayout.Label($"{L("Project", "РџСЂРѕРµРєС‚")}: <b><color=#{githubHex}>Wextikit/ElysiumModMenu</color></b>", textStyle);
                GUILayout.Label($"{L("Main page", "Р“Р»Р°РІРЅР°СЏ СЃСЃС‹Р»РєР°")}: <color=#{githubHex}>https://github.com/Wextikit/ElysiumModMenu</color>", textStyle);
                GUILayout.Space(8);
                GUILayout.Label($"{L("ElysiumModMenu is free and open-source software.", "ElysiumModMenu СЌС‚Рѕ Р±РµСЃРїР»Р°С‚РЅС‹Р№ open-source РїСЂРѕРµРєС‚.")}", textStyle);
                GUILayout.Label($"<b><color=#{dangerHex}>{L("If you paid for this menu, demand a refund immediately.", "Р•СЃР»Рё РІС‹ Р·Р°РїР»Р°С‚РёР»Рё Р·Р° СЌС‚Рѕ РјРµРЅСЋ, С‚СЂРµР±СѓР№С‚Рµ РІРѕР·РІСЂР°С‚ РґРµРЅРµРі СЃСЂР°Р·Сѓ.")}</color></b>", textStyle);
                GUILayout.Label($"<b><color=#{safeHex}>{L("Make sure you are using the latest version from GitHub releases.", "РЈР±РµРґРёС‚РµСЃСЊ, С‡С‚Рѕ РёСЃРїРѕР»СЊР·СѓРµС‚Рµ РїРѕСЃР»РµРґРЅСЋСЋ РІРµСЂСЃРёСЋ РёР· GitHub releases.")}</color></b>", textStyle);
                GUILayout.Space(8);
                GUILayout.Label($"<b><color=#{accentHex}>{L("Quick Hotkeys", "Р‘С‹СЃС‚СЂС‹Рµ РєР»Р°РІРёС€Рё")}</color></b>", textStyle);
                string menuKeyText = (menuToggleKey == KeyCode.None ? KeyCode.Insert : menuToggleKey).ToString();
                GUILayout.Label($"{L("Menu key", "РљРЅРѕРїРєР° РјРµРЅСЋ")}: <b>{menuKeyText}</b>", textStyle);
                GUILayout.Label(L("Right Click: teleport to cursor", "РџРљРњ: С‚РµР»РµРїРѕСЂС‚ Рє РєСѓСЂСЃРѕСЂСѓ"), textStyle);
                GUILayout.Label(L("F9: magnet cursor", "F9: РјР°РіРЅРёС‚ РєСѓСЂСЃРѕСЂР°"), textStyle);
                GUILayout.EndVertical();
            }
            else
            {
                GUILayout.BeginVertical(boxStyle);
                GUILayout.Label(L(
                    "ElysiumModMenu is an open-source project. Meet the people behind this build:",
                    "ElysiumModMenu СЌС‚Рѕ open-source РїСЂРѕРµРєС‚. РќРёР¶Рµ Р»СЋРґРё, РєРѕС‚РѕСЂС‹Рµ СЃС‚РѕСЏС‚ Р·Р° СЌС‚РѕР№ СЃР±РѕСЂРєРѕР№:"), textStyle);
                GUILayout.Space(8);

                GUILayout.Label($"<b><color=#{goldHex}>LEAD DEVELOPER</color></b>", textStyle);
                GUILayout.Space(4);
                if (DrawColoredActionButton("Meowchelo", new Color32(255, 92, 122, 255), 150f))
                    OpenExternalLink("https://github.com/Meowchelo", "Meowchelo");

                GUILayout.Space(10);
                GUILayout.Label($"<b><color=#{devHex}>DEVELOPERS</color></b>", textStyle);
                GUILayout.Space(4);
                GUILayout.BeginHorizontal();
                if (DrawColoredActionButton("Carrot", new Color32(38, 194, 129, 255), 150f))
                    OpenExternalLink("https://github.com/abobanamne", "Carrot");
                GUILayout.Space(6);
                if (DrawColoredActionButton("Wextikit", new Color32(109, 138, 255, 255), 150f))
                    OpenExternalLink("https://github.com/Wextikit", "Wextikit");
                GUILayout.Space(6);
                if (DrawColoredActionButton("Darmioniks", new Color32(38, 194, 129, 255), 150f))
                    OpenExternalLink("https://github.com/Darmioniks", "Darmioniks");
                GUILayout.EndHorizontal();

                GUILayout.Space(10);
                GUILayout.Label($"<b><color=#{contributorHex}>TESTERS</color></b>", textStyle);
                GUILayout.Space(4);
                DrawColoredActionButton("Жена", new Color32(109, 138, 255, 255), 150f);

                GUILayout.Space(10);
                GUILayout.Label($"<b><color=#{accentHex}>{L("Repository", "Р РµРїРѕР·РёС‚РѕСЂРёР№")}</color></b>", textStyle);
                GUILayout.Label(L(
                    "The public source, releases and project updates are published on GitHub.",
                    "РџСѓР±Р»РёС‡РЅС‹Р№ РёСЃС…РѕРґРЅС‹Р№ РєРѕРґ, СЂРµР»РёР·С‹ Рё РѕР±РЅРѕРІР»РµРЅРёСЏ РїСЂРѕРµРєС‚Р° РїСѓР±Р»РёРєСѓСЋС‚СЃСЏ РЅР° GitHub."), textStyle);
                GUILayout.Space(4);
                if (DrawColoredActionButton("Open Elysium GitHub", new Color32(26, 188, 156, 255), 220f))
                    OpenExternalLink("https://github.com/Wextikit/ElysiumModMenu", "ElysiumModMenu GitHub");

                GUILayout.Space(10);
                GUILayout.Label($"<b><color=#{accentHex}>Found a bug or have a question?</color></b>", textStyle);
                GUILayout.Space(4);
                if (DrawColoredActionButton("Join Discord", new Color32(88, 101, 242, 255), 150f))
                    OpenExternalLink("https://discord.gg/CdrpKJzFp", "Discord");

                GUILayout.Space(10);
                GUILayout.Label($"<b><color=#{contributorHex}>{L("Notes", "РџСЂРёРјРµС‡Р°РЅРёРµ")}</color></b>", textStyle);
                GUILayout.Label(L(
                    "Thank you to everyone helping with ideas, testing and polishing the menu.",
                    "РЎРїР°СЃРёР±Рѕ РІСЃРµРј, РєС‚Рѕ РїРѕРјРѕРіР°РµС‚ РёРґРµСЏРјРё, С‚РµСЃС‚Р°РјРё Рё РїРѕР»РёСЂРѕРІРєРѕР№ РјРµРЅСЋ."), textStyle);
                GUILayout.EndVertical();
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
            }
            finally
            {
                EndMultiTabContent(oldMatrix, oldColor);
            }
        }

[HarmonyPatch(typeof(ChatController), nameof(ChatController.AddChat))]
        public static class ChatLogger_Patch
        {
            public static void Prefix(PlayerControl sourcePlayer, ref string chatText)
            {
                if (!ElysiumModMenuGUI.enableChatLog || string.IsNullOrWhiteSpace(chatText)) return;

                try
                {
                    string time = System.DateTime.Now.ToString("HH:mm:ss");

                    string name = "System/Unknown";
                    string levelStr = "?";
                    string fc = "Hidden";
                    string puid = "Unknown";
                    string platformStr = "Unknown";

                    if (sourcePlayer != null && sourcePlayer.Data != null)
                    {
                        name = sourcePlayer.Data.PlayerName;

                        uint rawLevel = sourcePlayer.Data.PlayerLevel;
                        if (rawLevel != uint.MaxValue && rawLevel < 10000) levelStr = (rawLevel + 1).ToString();

                        fc = GetDisplayedFriendCode(sourcePlayer.Data, "Hidden");

                        var client = AmongUsClient.Instance?.GetClientFromCharacter(sourcePlayer);
                        if (client != null)
                        {
                            puid = GetPlayerPuid(sourcePlayer);
                            platformStr = ElysiumModMenuGUI.GetPlatform(client);
                        }
                    }

                    string cleanText = System.Text.RegularExpressions.Regex.Replace(chatText, "<.*?>", string.Empty);

                    string logLine = $"[{time}] [{name}] [Lv:{levelStr}] [FC:{fc}] [ID:{puid}] [{platformStr}] : {cleanText}\n";

                    string chatLogPath = System.IO.Path.Combine(Plugin.ElysiumFolder, "ChatLog.txt");
                    System.IO.File.AppendAllText(chatLogPath, logLine);
                }
                catch { }
            }
        }

    }
}
