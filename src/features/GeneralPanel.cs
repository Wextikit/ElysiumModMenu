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

private bool DrawColoredActionButton(string text, Color color, float width, float height = 24f)
        {
            GUIStyle style = new GUIStyle(btnStyle);
            Color themedColor = RgbMenuTextActive() ? GetMenuAccentColor() : (whiteMenuTheme ? GetThemeAccentColor(color) : color);
            Color hoverColor = whiteMenuTheme
                ? Color.Lerp(themedColor, Color.black, 0.18f)
                : Color.Lerp(themedColor, Color.white, 0.22f);

            style.normal.textColor = themedColor;
            style.hover.textColor = hoverColor;
            style.focused.textColor = themedColor;
            style.active.textColor = whiteMenuTheme ? Color.white : Color.black;
            style.clipping = TextClipping.Overflow;
            style.wordWrap = false;

            float minContentWidth = Mathf.Ceil(style.CalcSize(new GUIContent(text)).x) + 32f;
            float finalButtonWidth = Mathf.Max(width, minContentWidth);
            return GUILayout.Button(text, style, GUILayout.Width(finalButtonWidth), GUILayout.Height(height));
        }

private bool DrawPseudoInputButton(string value, bool editing, float height = 28f, int maxChars = 52)
        {
            GUIStyle style = new GUIStyle(editing ? activeTabStyle : inputBlockStyle);
            style.alignment = TextAnchor.MiddleLeft;
            style.clipping = TextClipping.Clip;
            style.wordWrap = false;
            style.padding = CreateRectOffset(10, 10, 0, 0);

            Rect rect = GUILayoutUtility.GetRect(GUIContent.none, style, GUILayout.ExpandWidth(true), GUILayout.Height(height));
            return GUI.Button(rect, FormatInputPreview(value, editing, maxChars), style);
        }

private void DrawClippedHint(string text, float height = 13f)
        {
            GUIStyle style = new GUIStyle(toggleLabelStyle)
            {
                fontSize = 10,
                clipping = TextClipping.Clip,
                wordWrap = false,
                alignment = TextAnchor.MiddleLeft
            };

            Rect rect = GUILayoutUtility.GetRect(GUIContent.none, style, GUILayout.ExpandWidth(true), GUILayout.Height(height));
            GUI.Label(rect, text, style);
        }

private void OpenExternalLink(string url, string label)
        {
            try
            {
                Application.OpenURL(url);
                ShowNotification($"<color=#00FFAA>[LINK]</color> {L("Opening", "Открываю")} <b>{label}</b>");
            }
            catch
            {
                ShowNotification($"<color=#FF4444>[LINK]</color> {L("Failed to open link.", "Не удалось открыть ссылку.")}");
            }
        }

private void DrawGeneralInfoTab()
        {
            GUILayout.BeginVertical(boxStyle);
            GUILayout.Label("ELYSIUM OVERVIEW", headerStyle);
            GUILayout.Space(6);

            GUILayout.BeginHorizontal();
            for (int i = 0; i < generalInfoSubTabs.Length; i++)
            {
                GUIStyle tabStyle = currentGeneralInfoSubTab == i ? activeSubTabStyle : subTabStyle;
                float tabWidth = Mathf.Max(116f, Mathf.Ceil(tabStyle.CalcSize(new GUIContent(generalInfoSubTabs[i])).x) + 28f);
                if (GUILayout.Button(generalInfoSubTabs[i], tabStyle, GUILayout.Width(tabWidth), GUILayout.Height(24)))
                {
                    currentGeneralInfoSubTab = i;
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.Label(L("Menu language:", "Язык меню:"), toggleLabelStyle, GUILayout.MinWidth(128), GUILayout.ExpandWidth(false), GUILayout.Height(24));
            if (GUILayout.Button("<", btnStyle, GUILayout.Width(26), GUILayout.Height(24)))
            {
                currentMenuLanguageIndex--;
                if (currentMenuLanguageIndex < 0) currentMenuLanguageIndex = menuLanguageNames.Length - 1;
                SaveConfig();
            }
            GUIStyle languageValueStyle = new GUIStyle(btnStyle) { normal = { background = null, textColor = GetMenuAccentColor() }, fontStyle = FontStyle.Bold, clipping = TextClipping.Overflow, wordWrap = false };
            string languageValue = menuLanguageNames[Mathf.Clamp(currentMenuLanguageIndex, 0, menuLanguageNames.Length - 1)];
            float languageValueWidth = Mathf.Max(132f, Mathf.Ceil(languageValueStyle.CalcSize(new GUIContent(languageValue)).x) + 24f);
            GUILayout.Label(languageValue, languageValueStyle, GUILayout.Width(languageValueWidth), GUILayout.Height(24));
            if (GUILayout.Button(">", btnStyle, GUILayout.Width(26), GUILayout.Height(24)))
            {
                currentMenuLanguageIndex++;
                if (currentMenuLanguageIndex >= menuLanguageNames.Length) currentMenuLanguageIndex = 0;
                SaveConfig();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(8);

            string accentHex = GetMenuAccentHex();
            bool rgbText = RgbMenuTextActive();
            string githubHex = rgbText ? accentHex : ColorUtility.ToHtmlStringRGB(whiteMenuTheme ? GetThemeAccentColor(new Color32(26, 188, 156, 255)) : new Color32(26, 188, 156, 255));
            string goldHex = rgbText ? accentHex : ColorUtility.ToHtmlStringRGB(whiteMenuTheme ? GetThemeAccentColor(new Color32(255, 187, 54, 255)) : new Color32(255, 187, 54, 255));
            string leadHex = rgbText ? accentHex : ColorUtility.ToHtmlStringRGB(whiteMenuTheme ? GetThemeAccentColor(new Color32(255, 92, 122, 255)) : new Color32(255, 92, 122, 255));
            string devHex = rgbText ? accentHex : ColorUtility.ToHtmlStringRGB(whiteMenuTheme ? GetThemeAccentColor(new Color32(38, 194, 129, 255)) : new Color32(38, 194, 129, 255));
            string contributorHex = rgbText ? accentHex : ColorUtility.ToHtmlStringRGB(whiteMenuTheme ? GetThemeAccentColor(new Color32(109, 138, 255, 255)) : new Color32(109, 138, 255, 255));
            string dangerHex = rgbText ? accentHex : ColorUtility.ToHtmlStringRGB(whiteMenuTheme ? GetThemeAccentColor(new Color32(231, 76, 60, 255)) : new Color32(231, 76, 60, 255));
            string safeHex = rgbText ? accentHex : ColorUtility.ToHtmlStringRGB(whiteMenuTheme ? GetThemeAccentColor(new Color32(57, 255, 20, 255)) : new Color32(57, 255, 20, 255));
            string versionText = "1.3.8";

            GUIStyle textStyle = new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true, fontSize = 12 };
            textStyle.normal.textColor = whiteMenuTheme ? new Color(0.16f, 0.16f, 0.16f, 1f) : new Color(0.85f, 0.85f, 0.85f, 1f);

            if (currentGeneralInfoSubTab == 0)
            {
                GUILayout.BeginVertical(boxStyle);
                GUILayout.Label(
                    $"{L("Welcome to", "Добро пожаловать в")} <b><color=#{accentHex}>ElysiumModMenu</color></b> " +
                    $"<b><color=#{goldHex}>v{versionText}</color></b> {L("by", "от")} <b><color=#{leadHex}>meowchelo</color></b>!",
                    textStyle);
                GUILayout.Space(4);
                GUILayout.Label(L(
                    "ElysiumModMenu is a lightweight BepInEx IL2CPP utility for Among Us with lobby tools, visuals, spoofing and host-side controls.",
                    "ElysiumModMenu это легкий BepInEx IL2CPP мод для Among Us с инструментами для лобби, визуалом, спуфингом и хост-функциями."), textStyle);
                GUILayout.Label(L(
                    "Use the buttons below to open the GitHub repository or jump straight to the latest public release.",
                    "Кнопки ниже открывают GitHub репозиторий и страницу с последним публичным релизом."), textStyle);
                GUILayout.Space(6);

                GUILayout.BeginHorizontal();
                if (DrawColoredActionButton("GitHub", new Color32(26, 188, 156, 255), 110f))
                    OpenExternalLink("https://github.com/meowchelo/ElysiumModMenu", "GitHub");
                GUILayout.Space(6);
                if (DrawColoredActionButton("Check for Updates", new Color32(255, 187, 54, 255), 165f))
                    OpenExternalLink("https://github.com/meowchelo/ElysiumModMenu/releases/latest", "Latest Release");
                GUILayout.Space(6);
                if (DrawColoredActionButton("Discord", new Color32(88, 101, 242, 255), 110f))
                    OpenExternalLink("https://discord.gg/bvwYBZZwUX", "Discord");
                GUILayout.EndHorizontal();

                GUILayout.Space(8);
                GUILayout.Label($"{L("Project", "Проект")}: <b><color=#{githubHex}>meowchelo/ElysiumModMenu</color></b>", textStyle);
                GUILayout.Label($"{L("Main page", "Главная ссылка")}: <color=#{githubHex}>https://github.com/meowchelo/ElysiumModMenu</color>", textStyle);
                GUILayout.Space(8);
                GUILayout.Label($"{L("ElysiumModMenu is free and open-source software.", "ElysiumModMenu это бесплатный open-source проект.")}", textStyle);
                GUILayout.Label($"<b><color=#{dangerHex}>{L("If you paid for this menu, demand a refund immediately.", "Если вы заплатили за это меню, требуйте возврат денег сразу.")}</color></b>", textStyle);
                GUILayout.Label($"<b><color=#{safeHex}>{L("Make sure you are using the latest version from GitHub releases.", "Убедитесь, что используете последнюю версию из GitHub releases.")}</color></b>", textStyle);
                GUILayout.Space(8);
                GUILayout.Label($"<b><color=#{accentHex}>{L("Quick Hotkeys", "Быстрые клави��и")}</color></b>", textStyle);
                string menuKeyText = (menuToggleKey == KeyCode.None ? KeyCode.Insert : menuToggleKey).ToString();
                GUILayout.Label($"{L("Menu key", "Кнопка меню")}: <b>{menuKeyText}</b>", textStyle);
                GUILayout.Label(L("Right Click: teleport to cursor", "ПКМ: телепорт к курсору"), textStyle);
                GUILayout.Label(L("F9: magnet cursor", "F9: магнит курсора"), textStyle);
                GUILayout.EndVertical();
            }
            else
            {
                GUILayout.BeginVertical(boxStyle);
                GUILayout.Label(L(
                    "ElysiumModMenu is an open-source project. Meet the people behind this build:",
                    "ElysiumModMenu это open-source проект. Ниже люди, которые стоят за этой сборкой:"), textStyle);
                GUILayout.Space(8);

                GUILayout.Label($"<b><color=#{goldHex}>LEAD DEVELOPER</color></b>", textStyle);
                GUILayout.Space(4);
                if (DrawColoredActionButton("meowchelo", new Color32(255, 92, 122, 255), 150f))
                    OpenExternalLink("https://github.com/meowchelo", "meowchelo");

                GUILayout.Space(10);
                GUILayout.Label($"<b><color=#{devHex}>DEVELOPERS</color></b>", textStyle);
                GUILayout.Space(4);
                GUILayout.BeginHorizontal();
                if (DrawColoredActionButton("Carrot", new Color32(38, 194, 129, 255), 150f))
                    OpenExternalLink("https://github.com/abobanamne", "Carrot");
                GUILayout.Space(6);
                if (DrawColoredActionButton("wextikit", new Color32(109, 138, 255, 255), 150f))
                    OpenExternalLink("https://github.com/wextikit", "wextikit");
                GUILayout.EndHorizontal();

                GUILayout.Space(10);
                GUILayout.Label($"<b><color=#{contributorHex}>TESTERS</color></b>", textStyle);
                GUILayout.Space(4);
                DrawColoredActionButton("Жена", new Color32(109, 138, 255, 255), 150f);

                GUILayout.Space(10);
                GUILayout.Label($"<b><color=#{accentHex}>{L("Repository", "Репозиторий")}</color></b>", textStyle);
                GUILayout.Label(L(
                    "The public source, releases and project updates are published on GitHub.",
                    "Публичный исходный код, релизы и обновления проекта публикуются на GitHub."), textStyle);
                GUILayout.Space(4);
                if (DrawColoredActionButton("Open ElysiumModMenu Repository", new Color32(26, 188, 156, 255), 220f))
                    OpenExternalLink("https://github.com/meowchelo/ElysiumModMenu", "ElysiumModMenu Repository");

                GUILayout.Space(10);
                GUILayout.Label($"<b><color=#{accentHex}>Found a bug or have a question?</color></b>", textStyle);
                GUILayout.Space(4);
                if (DrawColoredActionButton("Join Discord", new Color32(88, 101, 242, 255), 150f))
                    OpenExternalLink("https://discord.gg/bvwYBZZwUX", "Discord");

                GUILayout.Space(10);
                GUILayout.Label($"<b><color=#{contributorHex}>{L("Notes", "Примечание")}</color></b>", textStyle);
                GUILayout.Label(L(
                    "Thank you to everyone helping with ideas, testing and polishing the menu.",
                    "Спасибо всем, кто помогает идеями, тестами и полировкой меню."), textStyle);
                GUILayout.EndVertical();
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
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
                            puid = GetClientPuid(client);
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

private void DrawSelfTab()
        {
            if (currentSelfSubTab == 0) currentSelfSubTab = 1;

            float selfColumnWidth = Mathf.Max(270f, (windowRect.width - 186f) * 0.5f);

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(selfColumnWidth));
            DrawSelfSpoof();
            GUILayout.EndVertical();

            GUILayout.Space(8);

            GUILayout.BeginVertical(menuCardStyle, GUILayout.Width(selfColumnWidth), GUILayout.ExpandHeight(false));
            DrawMenuSectionHeader("SELF TOOLS");
            GUILayout.Space(4);

            GUILayout.BeginHorizontal();
            for (int i = 0; i < selfOtherTabs.Length; i++)
            {
                int tabIndex = i + 1;
                if (GUILayout.Button(selfOtherTabs[i], currentSelfSubTab == tabIndex ? activeSubTabStyle : subTabStyle, GUILayout.Height(22)))
                {
                    currentSelfSubTab = tabIndex;
                    scrollPosition = Vector2.zero;
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(8);

            if (currentSelfSubTab == 1) DrawPlayerMovementCompact();
            else if (currentSelfSubTab == 2) DrawRolesCompact();
            else if (currentSelfSubTab == 3) DrawChatSettingsCompact();

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

private void DrawPlayerMovementCompact()
        {
            GUILayout.BeginVertical();
            DrawMenuSectionHeader("MOVEMENT & TELEPORT");

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Engine: {Mathf.Round(engineSpeed)}x", toggleLabelStyle, GUILayout.Width(86));
            engineSpeed = GUILayout.HorizontalSlider(engineSpeed, 1f, 555f, sliderStyle, sliderThumbStyle, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("R", btnStyle, GUILayout.Width(28), GUILayout.Height(22))) engineSpeed = 1f;
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Walk: {Mathf.Round(walkSpeed)}x", toggleLabelStyle, GUILayout.Width(86));
            walkSpeed = GUILayout.HorizontalSlider(walkSpeed, 1f, 30f, sliderStyle, sliderThumbStyle, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("R", btnStyle, GUILayout.Width(28), GUILayout.Height(22))) walkSpeed = 1f;
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            tpToCursor = DrawToggle(tpToCursor, "TP To Cursor", 230);
            GUILayout.Space(3);
            dragToCursor = DrawToggle(dragToCursor, "Drag To Cursor", 230);
            GUILayout.Space(3);
            autoFollowCursor = DrawToggle(autoFollowCursor, $"Magnet Cursor ({bindMagnetCursor})", 230);
            GUILayout.Space(3);
            noClip = DrawToggle(noClip, "True NoClip", 230);

            GUILayout.EndVertical();
        }

private void DrawRolesCompact()
        {
            GUILayout.BeginVertical();
            DrawMenuSectionHeader("ROLE TOOLS");

            GUIStyle roleMidStyle = new GUIStyle(btnStyle)
            {
                fontStyle = FontStyle.Bold,
                normal = { background = null, textColor = GetMenuAccentColor() },
                alignment = TextAnchor.MiddleCenter
            };

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("<", btnStyle, GUILayout.Width(28), GUILayout.Height(24)))
            {
                fakeRoleIdx--;
                if (fakeRoleIdx < 0) fakeRoleIdx = forceRoleOptions.Length - 1;
            }
            GUILayout.Label(forceRoleOptions[fakeRoleIdx].ToString(), roleMidStyle, GUILayout.ExpandWidth(true), GUILayout.Height(24));
            if (GUILayout.Button(">", btnStyle, GUILayout.Width(28), GUILayout.Height(24)))
            {
                fakeRoleIdx++;
                if (fakeRoleIdx >= forceRoleOptions.Length) fakeRoleIdx = 0;
            }
            if (GUILayout.Button("Set", activeTabStyle, GUILayout.Width(42), GUILayout.Height(24)))
                RoleManager.Instance?.SetRole(PlayerControl.LocalPlayer, forceRoleOptions[fakeRoleIdx]);
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            DrawRoleBuffSubTabs();
            GUILayout.Space(8);

            if (currentRoleBuffSubTab == 0) DrawNonHostRoleBuffs(230);
            else DrawHostRoleBuffs(230);

            GUILayout.EndVertical();
        }

private void DrawRoleBuffSubTabs()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("NON HOST", currentRoleBuffSubTab == 0 ? activeSubTabStyle : subTabStyle, GUILayout.Height(22)))
                currentRoleBuffSubTab = 0;
            if (GUILayout.Button("HOST", currentRoleBuffSubTab == 1 ? activeSubTabStyle : subTabStyle, GUILayout.Height(22)))
                currentRoleBuffSubTab = 1;
            GUILayout.EndHorizontal();
        }

private void DrawNonHostRoleBuffs(int width)
        {
            DrawMenuSectionHeader("IMPOSTOR");
            killReach = DrawToggle(killReach, "Kill Reach", width);
            GUILayout.Space(3);
            killAnyone = DrawToggle(killAnyone, "Kill Anyone", width);
            GUILayout.Space(3);
            allowTasksAsImpostor = DrawToggle(allowTasksAsImpostor, "Allow Tasks (Imp)", width);
            GUILayout.Space(3);
            spamReportBodies = DrawToggle(spamReportBodies, "Spam Report Bodies", width);

            GUILayout.Space(8);
            DrawMenuSectionHeader("SHAPESHIFTER");
            NoShapeshiftAnim = DrawToggle(NoShapeshiftAnim, "No Ss Animation", width);
            GUILayout.Space(3);
            endlessSsDuration = DrawToggle(endlessSsDuration, "Endless Ss Duration", width);

            GUILayout.Space(8);
            DrawMenuSectionHeader("TRACKER");
            EndlessTracking = DrawToggle(EndlessTracking, "Endless Tracking", width);
            GUILayout.Space(3);
            NoTrackingCooldown = DrawToggle(NoTrackingCooldown, "No Track Cooldown", width);

            GUILayout.Space(8);
            DrawMenuSectionHeader("ENGINEER");
            endlessVentTime = DrawToggle(endlessVentTime, "Endless Vent Time", width);
            GUILayout.Space(3);
            noVentCooldown = DrawToggle(noVentCooldown, "No Vent Cooldown", width);
            GUILayout.Space(3);
            unlockVents = DrawToggle(unlockVents, "Unlock Vents", width);
            GUILayout.Space(3);
            walkInVents = DrawToggle(walkInVents, "Walk In Vents", width);
            GUILayout.Space(3);
            noMapCooldowns = DrawToggle(noMapCooldowns, "No Map Cooldowns", width);

            GUILayout.Space(8);
            DrawMenuSectionHeader("SCIENTIST");
            endlessBattery = DrawToggle(endlessBattery, "Endless Battery", width);
            GUILayout.Space(3);
            noVitalsCooldown = DrawToggle(noVitalsCooldown, "No Vitals Cooldown", width);

            GUILayout.Space(8);
            DrawMenuSectionHeader("DETECTIVE");
            UnlimitedInterrogateRange = DrawToggle(UnlimitedInterrogateRange, "Interrogate Reach", width);

            GUILayout.Space(8);
            DrawMenuSectionHeader("GLOBAL");
            roleBuffImmortality = DrawToggle(roleBuffImmortality, "Immortality", width);
        }

private void DrawHostRoleBuffs(int width)
        {
            DrawMenuSectionHeader("IMPOSTOR HOST");
            killAuraHostOnly = DrawToggle(killAuraHostOnly, "Kill Aura", width);
            GUILayout.Space(3);
            noKillCooldownHostOnly = DrawToggle(noKillCooldownHostOnly, "Kill Cooldown 0", width);
            GUILayout.Space(3);
            killWhileVanishedHostOnly = DrawToggle(killWhileVanishedHostOnly, "Kill While Vanished", width);
        }

private void DrawChatSettingsCompact()
        {
            GUILayout.BeginVertical();
            DrawMenuSectionHeader(L("CHAT SETTINGS", "НАСТРОЙКИ ЧАТА"));

            alwaysChat = DrawToggle(alwaysChat, L("Always Show Chat", "Всегда показывать чат"), 230);
            GUILayout.Space(3);
            readGhostChat = DrawToggle(readGhostChat, L("Read Ghost Chat", "Читать чат призраков"), 230);
            GUILayout.Space(4);
            DrawGhostChatColorControl(230f);
            GUILayout.Space(3);
            enableExtendedChat = DrawToggle(enableExtendedChat, L("Extended Chat", "Длинный чат"), 230);
            GUILayout.Space(3);
            enableFastChat = DrawToggle(enableFastChat, L("Fast Chat", "Быстрый чат"), 230);
            GUILayout.Space(3);
            allowLinksAndSymbols = DrawToggle(allowLinksAndSymbols, L("Unlock Extra Characters", "Все символы"), 230);
            GUILayout.Space(3);
            enableSpellCheck = DrawToggle(enableSpellCheck, L("Spell Check", "Проверка орфографии"), 230);

            GUILayout.Space(8);
            DrawMenuSectionHeader(L("CHAT UTILITY", "УТИЛИТЫ ЧАТА"));
            enableChatHistory = DrawToggle(enableChatHistory, L("Chat History", "История чата"), 230);
            GUILayout.Space(3);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{L("History:", "История:")} {chatHistoryLimit}", toggleLabelStyle, GUILayout.MinWidth(106), GUILayout.ExpandWidth(false));
            chatHistoryLimit = Mathf.Clamp((int)GUILayout.HorizontalSlider(chatHistoryLimit, 5f, 80f, sliderStyle, sliderThumbStyle, GUILayout.ExpandWidth(true)), 5, 80);
            TrimChatHistoryToLimit();
            GUILayout.EndHorizontal();
            GUILayout.Space(3);
            enableClipboard = DrawToggle(enableClipboard, L("Clipboard", "Буфер обмена"), 230);
            GUILayout.Space(3);
            enableChatMessageDoubleClickCopy = DrawToggle(enableChatMessageDoubleClickCopy, L("Double-click Copy", "Дабл-клик копирует"), 230);
            GUILayout.Space(3);
            enableChatNameColorCopy = DrawToggle(enableChatNameColorCopy, L("Copy Name", "Копировать ник"), 230);
            GUILayout.Space(3);
            enableChatLog = DrawToggle(enableChatLog, L("Save Chat Log", "Сохранять лог чата"), 230);
            GUILayout.Space(3);
            enableChatDarkMode = DrawToggle(enableChatDarkMode, L("Dark Chat Theme", "Темная тема чата"), 230);
            GUILayout.Space(3);
            if (enableChatDarkMode && GUILayout.Button(L("Turn Off Dark Chat", "Выключить темный чат"), btnStyle, GUILayout.Height(24)))
            {
                enableChatDarkMode = false;
                SaveConfig();
            }
            GUILayout.Space(3);
            enableColorCommand = DrawToggle(enableColorCommand, L("Enable /color", "Разрешить /color"), 230);
            GUILayout.Space(3);
            blockFortegreenChat = DrawToggle(blockFortegreenChat, L("Block Fortegreen", "Блок Fortegreen"), 230);
            GUILayout.Space(3);
            blockRainbowChat = DrawToggle(blockRainbowChat, L("Block Rainbow", "Блок Rainbow"), 230);

            GUILayout.Space(8);
            DrawMenuSectionHeader(L("CHAT SENDER", "ОТПРАВКА ЧАТА"));
            GUILayout.Space(4);

            GUIStyle fieldStyle = new GUIStyle(GUI.skin.textField)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip
            };
            fieldStyle.normal.textColor = whiteMenuTheme ? new Color(0.12f, 0.12f, 0.12f, 1f) : new Color(0.9f, 0.9f, 0.9f, 1f);

            Rect chatInputRect = GUILayoutUtility.GetRect(10f, 32f, GUILayout.ExpandWidth(true), GUILayout.Height(32));
            GUI.Box(chatInputRect, string.Empty, fieldStyle);

            string drawText = string.IsNullOrEmpty(customChatMessage)
                ? L("Type a message...", "Введите сообщение...")
                : customChatMessage;
            if (customChatInputFocused && (Time.unscaledTime % 1f) < 0.5f) drawText += "|";

            GUIStyle chatInputTextStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip,
                richText = false,
                fontSize = 12
            };
            chatInputTextStyle.normal.textColor = whiteMenuTheme ? new Color(0.12f, 0.12f, 0.12f, 1f) : new Color(0.9f, 0.9f, 0.9f, 1f);
            GUI.Label(new Rect(chatInputRect.x + 9f, chatInputRect.y + 3f, chatInputRect.width - 18f, chatInputRect.height - 6f), drawText, chatInputTextStyle);

            Event e = Event.current;
            if (e != null)
            {
                if (e.type == EventType.MouseDown)
                {
                    customChatInputFocused = chatInputRect.Contains(e.mousePosition);
                    if (customChatInputFocused) e.Use();
                }
                else if (customChatInputFocused && e.type == EventType.KeyDown)
                {
                    if (HandleClipboardShortcut(e, ref customChatMessage, 120))
                    {
                    }
                    else if (e.keyCode == KeyCode.Backspace)
                    {
                        if (!string.IsNullOrEmpty(customChatMessage))
                            customChatMessage = customChatMessage.Substring(0, customChatMessage.Length - 1);
                        e.Use();
                    }
                    else if (e.keyCode == KeyCode.Escape)
                    {
                        customChatInputFocused = false;
                        e.Use();
                    }
                    else if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
                    {
                        TrySendCustomChatMessage(customChatMessage);
                        e.Use();
                    }
                    else if (!char.IsControl(e.character))
                    {
                        if (customChatMessage == null) customChatMessage = string.Empty;
                        if (customChatMessage.Length < 120) customChatMessage += e.character;
                        e.Use();
                    }
                }
            }

            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(L("Send", "Отправить"), btnStyle, GUILayout.Height(28)))
                TrySendCustomChatMessage(customChatMessage);
            GUILayout.Space(6);
            string spamBtnText = customChatSpamEnabled ? L("Spam ON", "Спам ВКЛ") : L("Spam OFF", "Спам ВЫКЛ");
            if (GUILayout.Button(spamBtnText, customChatSpamEnabled ? activeTabStyle : btnStyle, GUILayout.Height(28)))
                customChatSpamEnabled = !customChatSpamEnabled;
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{L("Delay:", "Задержка:")} {Mathf.Round(customChatSpamDelay * 10f) / 10f}s", toggleLabelStyle, GUILayout.Width(92));
            customChatSpamDelay = GUILayout.HorizontalSlider(customChatSpamDelay, 0.5f, 10f, sliderStyle, sliderThumbStyle, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

private void DrawGhostChatColorControl(float width)
        {
            GUILayout.BeginHorizontal(GUILayout.Width(width));
            GUILayout.Label(L("Ghost Chat:", "Ghost Chat:"), new GUIStyle(toggleLabelStyle) { fontSize = 11 }, GUILayout.Width(74), GUILayout.Height(24));
            if (DrawPseudoInputButton(ghostChatColorHex, isEditingGhostChatColor, 24f, 16))
            {
                isEditingGhostChatColor = !isEditingGhostChatColor;
                if (isEditingGhostChatColor)
                {
                    ghostChatColorHex = FilterHexInput(ghostChatColorHex, 7);
                }
                isEditingName = false;
                isEditingLevel = false;
                isEditingFriendCode = false;
                isEditingLocalFriendCode = false;
                isEditingBan = false;
                ResetAllBindWaits();
            }
            if (GUILayout.Button(L("Apply", "OK"), btnStyle, GUILayout.Width(48), GUILayout.Height(24)))
            {
                isEditingGhostChatColor = false;
                ghostChatColorHex = SanitizeHexColor(ghostChatColorHex, "#D7B8FF");
                SaveConfig();
            }
            GUILayout.EndHorizontal();

            string previewHex = GetGhostChatColorHex();
            GUILayout.Label($"<color={previewHex}>{L("Preview ghost chat color", "Пример цвета чата призраков")}</color>", new GUIStyle(GUI.skin.label) { richText = true, fontSize = 11, wordWrap = false, clipping = TextClipping.Clip }, GUILayout.Width(width), GUILayout.Height(16f));
        }

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
            if (GameManager.Instance == null || AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return;

            bool isHns = GameManager.Instance.IsHideAndSeek();
            int reasonCode = 0;

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
