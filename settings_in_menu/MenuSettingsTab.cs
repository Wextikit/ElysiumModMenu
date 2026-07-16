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
private void DrawMenuSectionHeader(string title)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(GUIContent.none, menuAccentBarStyle, GUILayout.Width(3), GUILayout.Height(16));
            GUILayout.Space(8);
            GUILayout.Label(title, menuSectionTitleStyle, GUILayout.Height(16));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(8);
        }

private void DrawMenuTab()
        {
            bool menuPrefsChanged = false;

            GUILayout.BeginVertical(menuCardStyle);
            DrawMenuSectionHeader(L("MENU CUSTOMIZATION", "РћР¤РћР РњР›Р•РќРР• РњР•РќР®"));

            bool prevRgb = rgbMenuMode;
            rgbMenuMode = DrawToggle(rgbMenuMode, "RGB Menu Mode", 260);
            if (prevRgb && !rgbMenuMode) UpdateAccentColor(menuColors[currentMenuColorIndex]);
            if (prevRgb != rgbMenuMode) menuPrefsChanged = true;
            GUILayout.Label(L("Smoothly cycles the accent through the rainbow.", "РџР»Р°РІРЅРѕ РїРµСЂРµР»РёРІР°РµС‚ Р°РєС†РµРЅС‚ РїРѕ СЂР°РґСѓРіРµ."), menuDescStyle);
            GUILayout.Space(8);

            bool prevRgbTaskBar = rgbTaskBar;
            rgbTaskBar = DrawToggle(rgbTaskBar, "RGB Task Bar", 260);
            if (prevRgbTaskBar != rgbTaskBar)
            {
                if (!rgbTaskBar) RestoreRgbTaskBar();
                menuPrefsChanged = true;
            }
            GUILayout.Label("Recolors the in-game task progress bar.", menuDescStyle);
            GUILayout.Space(8);

            bool prevRgbText = rgbMenuText;
            rgbMenuText = DrawToggle(rgbMenuText, "RGB Text", 260);
            if (prevRgbText != rgbMenuText)
            {
                InitStyles();
                UpdateAccentColor(currentAccentColor);
                menuPrefsChanged = true;
            }
            GUILayout.Label("When off, RGB Menu Mode does not recolor menu text.", menuDescStyle);
            GUILayout.Space(8);

            bool prevBoldMenuText = boldMenuText;
            boldMenuText = DrawToggle(boldMenuText, "Bold Menu Text", 260);
            if (prevBoldMenuText != boldMenuText)
            {
                InitStyles();
                UpdateAccentColor(currentAccentColor);
                menuPrefsChanged = true;
            }
            GUILayout.Label("Switches menu text between bold and normal. Default: bold.", menuDescStyle);
            GUILayout.Space(8);

            bool prevMenuScaleInput = enableMenuScaleInput;
            enableMenuScaleInput = DrawToggle(enableMenuScaleInput, L("Enable Ctrl + Wheel Scale", "Масштабирование Ctrl + колёсиком"), 260);
            if (prevMenuScaleInput != enableMenuScaleInput) menuPrefsChanged = true;
            GUILayout.Label(L("Hold Ctrl and scroll anywhere over the menu to change its size, including scrollable settings. Turning this off keeps the current size.", "Зажмите Ctrl и крутите колёсико в любом месте меню, включая прокручиваемые настройки, чтобы изменить размер. Выключение сохраняет текущий размер."), menuDescStyle);
            GUILayout.Space(8);

            bool prevWhiteTheme = whiteMenuTheme;
            whiteMenuTheme = DrawToggle(whiteMenuTheme, "White Theme", 260);
            if (prevWhiteTheme != whiteMenuTheme)
            {
                InitStyles();
                UpdateAccentColor(currentAccentColor);
                menuPrefsChanged = true;
            }
            GUILayout.Label(L("Switches between the dark and light interface.", "РџРµСЂРµРєР»СЋС‡Р°РµС‚ С‚С‘РјРЅС‹Р№ Рё СЃРІРµС‚Р»С‹Р№ РёРЅС‚РµСЂС„РµР№СЃ."), menuDescStyle);
            GUILayout.Space(8);

            bool prevBg = enableBackground;
            enableBackground = DrawToggle(enableBackground, "Enable Image Background", 260);
            if (enableBackground && !prevBg) LoadBackgroundImage();
            if (prevBg != enableBackground) menuPrefsChanged = true;
            GUILayout.Label(L("Put 'MenuBG.png' or .jpg in BepInEx/config to add a background image.", "РџРѕР»РѕР¶РёС‚Рµ 'MenuBG.png' РёР»Рё .jpg РІ BepInEx/config РґР»СЏ С„РѕРЅР°."), menuDescStyle);
            GUILayout.Space(8);

            bool prevWatermark = showWatermark;
            showWatermark = DrawToggle(showWatermark, L("Show Watermark", "РџРѕРєР°Р·С‹РІР°С‚СЊ РІРѕС‚РµСЂРјР°СЂРє"), 260);
            if (prevWatermark != showWatermark) menuPrefsChanged = true;
            GUILayout.Label(L("Shows the ElysiumModMenu watermark near ping and FPS.", "РџРѕРєР°Р·С‹РІР°РµС‚ РІРѕС‚РµСЂРјР°СЂРє ElysiumModMenu СЂСЏРґРѕРј СЃ ping Рё FPS."), menuDescStyle);
            GUILayout.Space(8);

            bool prevHardMenu = hardMenu;
            hardMenu = DrawToggle(hardMenu, L("Solid Menu (block game clicks)", "РўРІРµСЂРґРѕРµ РјРµРЅСЋ (Р±Р»РѕРє РєР»РёРєРѕРІ РїРѕ РёРіСЂРµ)"), 260);
            if (prevHardMenu != hardMenu) menuPrefsChanged = true;
            GUILayout.Label(L("When on, clicks over the menu stay in the menu so you can't misclick the game behind it.", "РљРѕРіРґР° РІРєР»СЋС‡РµРЅРѕ, РєР»РёРєРё РїРѕ РјРµРЅСЋ РѕСЃС‚Р°СЋС‚СЃСЏ РІ РјРµРЅСЋ вЂ” РІС‹ РЅРµ РїСЂРѕРјР°С…РЅС‘С‚РµСЃСЊ РїРѕ РёРіСЂРµ Р·Р° РЅРёРј."), menuDescStyle);
            GUILayout.Space(8);

            bool prevAutoCopyCode = autoCopyCodeAndLeave;
            autoCopyCodeAndLeave = DrawToggle(autoCopyCodeAndLeave, "Copy Code On Disconnect", 260);
            if (prevAutoCopyCode != autoCopyCodeAndLeave) menuPrefsChanged = true;
            GUILayout.Label("Copies the room code when you leave, get kicked, banned, or disconnected.", menuDescStyle);
            GUILayout.Space(8);

            bool previousBlockTelemetry = blockInnerslothTelemetry;
            blockInnerslothTelemetry = DrawToggle(blockInnerslothTelemetry, "Block Innersloth Telemetry", 260);
            if (previousBlockTelemetry != blockInnerslothTelemetry)
            {
                ApplyTelemetryPreference();
                menuPrefsChanged = true;
            }
            GUILayout.Label("Disables Unity Analytics, device statistics, and performance reporting.", menuDescStyle);
            GUILayout.Space(8);

            bool previousRemovePenalty = removePenalty;
            removePenalty = DrawToggle(removePenalty, "No Disconnect Penalty", 260);
            if (previousRemovePenalty != removePenalty) menuPrefsChanged = true;
            GUILayout.Label("Prevents matchmaking cooldown when leaving or disconnecting.", menuDescStyle);
            GUILayout.Space(8);

            bool prevAprilDate = spoofAprilFoolsDate;
            spoofAprilFoolsDate = DrawToggle(spoofAprilFoolsDate, "Spoof April Date", 260);
            if (prevAprilDate != spoofAprilFoolsDate) menuPrefsChanged = true;
            GUILayout.Label("Makes the client use April Fools date locally.", menuDescStyle);
            GUILayout.Space(8);

            bool prevGuestExtra = guestExtraFeatures;
            guestExtraFeatures = DrawToggle(guestExtraFeatures, L("Guest Extra Features", "Р”РѕРї. С„СѓРЅРєС†РёРё РіРѕСЃС‚СЋ"), 260);
            if (prevGuestExtra != guestExtraFeatures) menuPrefsChanged = true;
            GUILayout.Label(L("Opens client-side free chat, friend list and custom name checks for guest accounts.", "РћС‚РєСЂС‹РІР°РµС‚ Р»РѕРєР°Р»СЊРЅС‹Рµ РїСЂРѕРІРµСЂРєРё free chat, СЃРїРёСЃРєР° РґСЂСѓР·РµР№ Рё СЃРІРѕРµРіРѕ РЅРёРєР° РґР»СЏ guest."), menuDescStyle);
            GUILayout.Space(8);

            bool prevAgeBypass = bypassAgeRestrictions;
            bypassAgeRestrictions = DrawToggle(bypassAgeRestrictions, L("Bypass Age Restrictions", "РћР±С…РѕРґ РІРѕР·СЂР°СЃС‚РЅС‹С… РѕРіСЂР°РЅРёС‡РµРЅРёР№"), 280);
            if (prevAgeBypass != bypassAgeRestrictions) menuPrefsChanged = true;
            GUILayout.Label(L("Ignores client-side minor/waiting checks and online lock where the game asks locally.", "РРіРЅРѕСЂРёСЂСѓРµС‚ Р»РѕРєР°Р»СЊРЅС‹Рµ РїСЂРѕРІРµСЂРєРё minor/waiting Рё Р»РѕРєР°Р»СЊРЅС‹Р№ Р·Р°РїСЂРµС‚ РѕРЅР»Р°Р№РЅР°."), menuDescStyle);
            GUILayout.Space(8);

            bool previousUnlockAll = unlockCosmetics;
            unlockCosmetics = DrawToggle(unlockCosmetics, "Unlock All (except Cosmicubes)", 280);
            if (previousUnlockAll != unlockCosmetics) menuPrefsChanged = true;
            GUILayout.Label("Locally unlocks all cosmetics except Cosmicubes.", menuDescStyle);
            GUILayout.Space(8);

            bool previousUnlockCosmicubes = unlockCosmicubes;
            unlockCosmicubes = DrawToggle(unlockCosmicubes, "Unlock Cosmicubes", 280);
            if (previousUnlockCosmicubes != unlockCosmicubes) menuPrefsChanged = true;
            GUILayout.Label("Locally unlocks all Cosmicubes without changing their progress.", menuDescStyle);
            GUILayout.Space(8);

            bool previousActivateCompleted = activateCompletedCosmicubes;
            activateCompletedCosmicubes = DrawToggle(activateCompletedCosmicubes, "Activate 100% Cosmicubes", 280);
            if (previousActivateCompleted != activateCompletedCosmicubes) menuPrefsChanged = true;
            GUILayout.Label("Allows a 100% completed Cosmicube to be activated locally; no data is sent to the server.", menuDescStyle);
            GUILayout.EndVertical();

            GUILayout.BeginVertical(menuCardStyle);
            DrawMenuSectionHeader("PANIC");
            GUILayout.Label("Turns off menu flags, hides the watermark and unpatches Harmony until restart.", menuDescStyle);
            GUILayout.Space(6);
            GUI.backgroundColor = new Color(0.85f, 0.12f, 0.10f, 1f);
            if (GUILayout.Button("PANIC MODE", btnStyle, GUILayout.Height(30), GUILayout.Width(180)))
                ApplyPanicMode();
            GUI.backgroundColor = Color.white;
            GUILayout.EndVertical();

            GUILayout.BeginVertical(menuCardStyle);
            DrawMenuSectionHeader(L("ACCENT & PERFORMANCE", "РђРљР¦Р•РќРў Р РџР РћРР—Р’РћР”РРўР•Р›Р¬РќРћРЎРўР¬"));

            bool prevLimitFps = limitFps;
            limitFps = DrawToggle(limitFps, L("Limit FPS", "РћРіСЂР°РЅРёС‡РёРІР°С‚СЊ FPS"), 260);
            if (prevLimitFps != limitFps)
            {
                ApplyFpsLimit();
                menuPrefsChanged = true;
            }
            GUILayout.Space(6);

            GUILayout.BeginHorizontal();
            GUI.enabled = limitFps;
            GUILayout.Label(L("FPS Limit", "Р›РёРјРёС‚ FPS"), new GUIStyle(toggleLabelStyle), GUILayout.Height(25), GUILayout.Width(110));
            int newFpsLimit = Mathf.Clamp((int)GUILayout.HorizontalSlider(fpsLimit, 1f, 560f, sliderStyle, sliderThumbStyle, GUILayout.Width(180)), 1, 560);
            GUILayout.Space(10);
            if (!isEditingFpsLimit) fpsLimitInput = fpsLimit.ToString();
            if (DrawFpsLimitInput())
            {
                isEditingFpsLimit = true;
                fpsLimitInput = string.Empty;
                isEditingName = false;
                isEditingLevel = false;
                isEditingFriendCode = false;
                isEditingLocalFriendCode = false;
                isEditingGhostChatColor = false;
                isEditingBan = false;
                ResetAllBindWaits();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUI.enabled = true;
            if (newFpsLimit != fpsLimit)
            {
                fpsLimit = newFpsLimit;
                fpsLimitInput = fpsLimit.ToString();
                ApplyFpsLimit();
                menuPrefsChanged = true;
            }

            GUILayout.Space(12);

            GUILayout.BeginHorizontal();
            GUILayout.Label(L("Accent Color", "Р¦РІРµС‚ Р°РєС†РµРЅС‚Р°"), new GUIStyle(toggleLabelStyle), GUILayout.Height(25), GUILayout.Width(110));
            Color prevGuiColor = GUI.color;
            GUI.color = GetMenuControlAccentColor();
            GUILayout.Label(GUIContent.none, menuSwatchStyle, GUILayout.Width(22), GUILayout.Height(22));
            GUI.color = prevGuiColor;
            GUILayout.Space(8);
            GUI.enabled = !rgbMenuMode;
            GUIStyle middleColorStyle = new GUIStyle(btnStyle) { normal = { background = null, textColor = GetMenuAccentColor() }, fontStyle = FontStyle.Bold };
            if (GUILayout.Button("<", btnStyle, GUILayout.Width(30), GUILayout.Height(25))) { currentMenuColorIndex--; if (currentMenuColorIndex < 0) currentMenuColorIndex = menuColors.Length - 1; if (!rgbMenuMode) UpdateAccentColor(menuColors[currentMenuColorIndex]); menuPrefsChanged = true; }
            GUILayout.Label(rgbMenuMode ? "RGB" : menuColorNames[currentMenuColorIndex], middleColorStyle, GUILayout.Width(120), GUILayout.Height(25));
            if (GUILayout.Button(">", btnStyle, GUILayout.Width(30), GUILayout.Height(25))) { currentMenuColorIndex++; if (currentMenuColorIndex >= menuColors.Length) currentMenuColorIndex = 0; if (!rgbMenuMode) UpdateAccentColor(menuColors[currentMenuColorIndex]); menuPrefsChanged = true; }
            GUI.enabled = true;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.BeginVertical(menuCardStyle);
            DrawMenuSectionHeader(L("SPOOF MENU IDENTITY", "РџРћР”РњР•РќРђ РњР•РќР®"));
            bool prevSpoofMenuEnabled = SpoofMenuEnabled;
            SpoofMenuEnabled = DrawToggle(SpoofMenuEnabled, "Enable Fake RPC", 260);
            if (prevSpoofMenuEnabled != SpoofMenuEnabled) menuPrefsChanged = true;
            GUILayout.Label(L("Reports a fake mod menu name to other players.", "РџРѕРєР°Р·С‹РІР°РµС‚ РёРіСЂРѕРєР°Рј РїРѕРґРґРµР»СЊРЅРѕРµ РёРјСЏ РјРµРЅСЋ."), menuDescStyle);
            GUILayout.Space(8);
            float spoofRowWidth = GetMenuWorkWidth(160f, 360f);
            float spoofNameWidth = Mathf.Clamp(spoofRowWidth - 76f, 150f, 260f);
            GUILayout.Label(L("Fake Name", "РџРѕРґРґРµР»СЊРЅРѕРµ РёРјСЏ"), new GUIStyle(toggleLabelStyle), GUILayout.Height(16), GUILayout.ExpandWidth(false));
            GUILayout.BeginHorizontal(GUILayout.Width(spoofNameWidth + 68f));
            GUI.enabled = SpoofMenuEnabled;
            GUIStyle middleLabelStyle = new GUIStyle(btnStyle) { fontStyle = FontStyle.Bold, normal = { background = null, textColor = GetMenuAccentColor() } };
            if (GUILayout.Button("<", btnStyle, GUILayout.Width(28), GUILayout.Height(25))) { selectedSpoofMenuIndex--; if (selectedSpoofMenuIndex < 0) selectedSpoofMenuIndex = spoofMenuNames.Length - 1; customSpoofRpcInputFocused = selectedSpoofMenuIndex == spoofMenuNames.Length - 1 && customSpoofRpcInputFocused; menuPrefsChanged = true; }
            GUILayout.Space(6);
            if (selectedSpoofMenuIndex == spoofMenuNames.Length - 1)
            {
                if (DrawCustomRpcInputButton(spoofNameWidth))
                {
                    customSpoofRpcInputFocused = true;
                    isEditingName = isEditingLevel = isEditingFriendCode = isEditingLocalFriendCode = isEditingGhostChatColor = isEditingBan = false;
                    ResetAllBindWaits();
                }
            }
            else
            {
                GUILayout.Label(spoofMenuNames[selectedSpoofMenuIndex], middleLabelStyle, GUILayout.Width(spoofNameWidth), GUILayout.Height(25));
            }
            GUILayout.Space(6);
            if (GUILayout.Button(">", btnStyle, GUILayout.Width(28), GUILayout.Height(25))) { selectedSpoofMenuIndex++; if (selectedSpoofMenuIndex >= spoofMenuNames.Length) selectedSpoofMenuIndex = 0; customSpoofRpcInputFocused = selectedSpoofMenuIndex == spoofMenuNames.Length - 1 && customSpoofRpcInputFocused; menuPrefsChanged = true; }
            GUI.enabled = true;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            DrawCustomRpcValidationInfo();
            GUILayout.Space(6);
            GUIStyle spoofDescStyle = new GUIStyle(menuDescStyle) { fontSize = 11, wordWrap = true, clipping = TextClipping.Clip };
            GUILayout.Label(L("Fake RPC sends the selected non-vanilla CallRpc as your local player. Custom RPC accepts only IDs outside the vanilla RPC list.",
                "Fake RPC РѕС‚РїСЂР°РІР»СЏРµС‚ РІС‹Р±СЂР°РЅРЅС‹Р№ РЅРµ-РІР°РЅРёР»СЊРЅС‹Р№ CallRpc РѕС‚ РІР°С€РµРіРѕ РёРіСЂРѕРєР°. Custom RPC РїСЂРёРЅРёРјР°РµС‚ С‚РѕР»СЊРєРѕ ID РІРЅРµ СЃРїРёСЃРєР° РІР°РЅРёР»СЊРЅС‹С… RPC."), spoofDescStyle, GUILayout.Height(36f));
            GUILayout.EndVertical();

            GUILayout.BeginVertical(menuCardStyle);
            DrawMenuSectionHeader(L("NOTIFICATIONS & LOGGING", "РЈР’Р•Р”РћРњР›Р•РќРРЇ Р Р›РћР“Р"));
            bool prevCustomNotifs = EnableCustomNotifs;
            EnableCustomNotifs = DrawToggle(EnableCustomNotifs, "Enable Custom UI Notifications", 280);
            if (prevCustomNotifs != EnableCustomNotifs) menuPrefsChanged = true;
            GUILayout.Space(6);
            bool prevLogAllRpcs = LogAllRPCs;
            LogAllRPCs = DrawToggle(LogAllRPCs, "Sniff All RPCs (On-Screen)", 280);
            if (prevLogAllRpcs != LogAllRPCs) menuPrefsChanged = true;
            GUILayout.Space(6);
            bool previousDetailedLogsEnabled = detailedLogsEnabled;
            detailedLogsEnabled = DrawToggle(detailedLogsEnabled, L("Detailed Unity/RPC Logs", "РџРѕРґСЂРѕР±РЅС‹Рµ Unity/RPC Р»РѕРіРё"), 280);
            if (previousDetailedLogsEnabled != detailedLogsEnabled)
            {
                throttleDefaultLogs = !detailedLogsEnabled;
                menuPrefsChanged = true;
            }
            GUILayout.Label(L("Turn off to stop routine RPC, Message, Info and Debug output. Warnings and errors remain enabled.", "Р’С‹РєР»СЋС‡РёС‚Рµ, С‡С‚РѕР±С‹ СѓР±СЂР°С‚СЊ РѕР±С‹С‡РЅС‹Рµ RPC, Message, Info Рё Debug Р»РѕРіРё. РћС€РёР±РєРё Рё РїСЂРµРґСѓРїСЂРµР¶РґРµРЅРёСЏ РѕСЃС‚Р°РЅСѓС‚СЃСЏ РІРєР»СЋС‡РµРЅС‹."), menuDescStyle);
            GUILayout.EndVertical();

            GUILayout.BeginVertical(menuCardStyle);
            DrawMenuSectionHeader(L("DISCORD RICH PRESENCE", "DISCORD РЎРўРђРўРЈРЎ"));
            bool prevDiscordRpc = discordRpcEnabled;
            discordRpcEnabled = DrawToggle(discordRpcEnabled, L("Enable Discord RPC", "Р’РєР»СЋС‡РёС‚СЊ Discord RPC"), 280);
            if (prevDiscordRpc != discordRpcEnabled)
            {
                menuPrefsChanged = true;
            }
            GUILayout.EndVertical();



            GUILayout.BeginVertical(menuCardStyle);
            DrawMenuSectionHeader(L("RESET SETTINGS", "РЎР‘Р РћРЎ РќРђРЎРўР РћР•Рљ"));
            GUILayout.Label(L("Resets all sliders back to their default values.", "РЎР±СЂР°СЃС‹РІР°РµС‚ РІСЃРµ РїРѕР»Р·СѓРЅРєРё РґРѕ Р·РЅР°С‡РµРЅРёР№ РїРѕ СѓРјРѕР»С‡Р°РЅРёСЋ."), menuDescStyle);
            GUILayout.Space(6);
            if (GUILayout.Button(L("Reset Sliders to Default", "РЎР±СЂРѕСЃРёС‚СЊ РїРѕР»Р·СѓРЅРєРё РґРѕ РґРµС„РѕР»С‚Р°"), activeTabStyle, GUILayout.Height(30)))
            {
                ResetSlidersToDefault();
                menuPrefsChanged = true;
            }
            GUILayout.EndVertical();

            if (menuPrefsChanged) SaveConfig();
        }

private static string FilterFpsLimitInput(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            StringBuilder sb = new StringBuilder(3);
            for (int i = 0; i < input.Length && sb.Length < 3; i++)
            {
                char c = input[i];
                if (c >= '0' && c <= '9') sb.Append(c);
            }
            return sb.ToString();
        }

private static string FilterMinuteInput(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            StringBuilder sb = new StringBuilder(2);
            for (int i = 0; i < input.Length && sb.Length < 2; i++)
            {
                char c = input[i];
                if (c >= '0' && c <= '9') sb.Append(c);
            }
            return sb.ToString();
        }

private void ApplyBugRoomTimedAutoRunInput()
        {
            int min;
            if (!int.TryParse(bugRoomTimedAutoRunInput, out min))
                min = bugRoomTimedAutoRunMinutes;

            bugRoomTimedAutoRunMinutes = Mathf.Clamp(min, 1, 60);
            bugRoomTimedAutoRunInput = bugRoomTimedAutoRunMinutes.ToString();
            isEditingBugRoomTimedAutoRun = false;
            settingsDirty = true;
        }

private void ApplyFpsLimitInput()
        {
            int val;
            if (!int.TryParse(fpsLimitInput, out val))
                val = fpsLimit;

            fpsLimit = Mathf.Clamp(val, 1, 560);
            fpsLimitInput = fpsLimit.ToString();
            isEditingFpsLimit = false;
            ApplyFpsLimit();
            SaveConfig();
        }

private bool DrawFpsLimitInput()
        {
            GUIStyle style = new GUIStyle(isEditingFpsLimit ? activeTabStyle : inputBlockStyle);
            style.alignment = TextAnchor.MiddleCenter;
            style.clipping = TextClipping.Clip;
            style.wordWrap = false;
            style.padding = CreateRectOffset(4, 4, 0, 0);

            Rect rect = GUILayoutUtility.GetRect(52f, 22f, GUILayout.Width(52f), GUILayout.Height(22f));
            return GUI.Button(rect, string.IsNullOrEmpty(fpsLimitInput) ? (isEditingFpsLimit ? "|" : fpsLimit.ToString()) : fpsLimitInput, style);
        }

private void ResetSlidersToDefault()
        {
            selectedMapSpawnIdx = 0f;
            chatHistoryLimit = 20;
            customChatSpamDelay = 2.1f;
            autoChatEveryoneDelay = 2.5f;
            engineSpeed = 1f;
            walkSpeed = 1f;
            currentPlatformIndex = 1;
            autoKickTimer = 5f;
            autoKickMinLevel = 200;
            fpsLimit = 60;
            fpsLimitInput = "60";
            isEditingFpsLimit = false;
            bugRoomTimedAutoRun = false;
            bugRoomTimedAutoRunMinutes = 10;
            bugRoomTimedAutoRunInput = "10";
            isEditingBugRoomTimedAutoRun = false;
            bugRoomLv35Rehost = false;
            bugRoomHostPassRejoin = false;
            limitFps = true;
            detailedLogsEnabled = false;
            throttleDefaultLogs = true;
            ApplyFpsLimit();
            AutoHostMinPlayers = 4;
            AutoHostStartDelaySeconds = 15f;
            AutoHostFastStartPlayers = 13;
            AutoHostFastStartDelaySeconds = 5f;
            punishmentMode = 0;

            showPlayerInfo = false;
            showEspBoxes = false;
            espShimmerMode = false;
            showEspVoteKicks = false;
            seeGhosts = false;
            seePhantoms = false;
            seeRoles = false;
            revealMeetingRoles = false;
            showTracers = false;
            showCrewmateTracers = false;
            showImpostorTracers = false;
            showDeadTracers = false;
            showBodyTracers = false;
            fullBright = false;
            seeProtections = false;
            seeKillCooldown = false;
            extendedLobby = false;
            moreLobbyInfo = true;
            alwaysShowLobbyTimer = false;
            noClip = false;
            tpToCursor = false;
            dragToCursor = false;
            autoFollowCursor = false;
            freecam = false;
            cameraZoom = false;
            showRadar = false;
            showRadarDeadBodies = false;
            showRadarGhosts = true;
            radarRightClickTp = false;
            hideRadarInMeeting = true;
            radarDrawIcons = false;
            lockRadar = false;
            radarBorder = false;
            radarScale = 1f;
            radarAlpha = 0.78f;
            radarRect = new Rect(15f, 90f, 220f, 180f);
            showReplay = false;
            replayOnlyLastSeconds = true;
            replaySeconds = 30f;
            replayDrawIcons = false;
            replayRect = new Rect(250f, 90f, 330f, 240f);
            blockInnerslothTelemetry = false;
            ApplyTelemetryPreference();
            unlockCosmetics = true;
            unlockCosmicubes = true;
            activateCompletedCosmicubes = false;
            alwaysChat = false;
            lobbyRainbowAll = false;
            lobbyAllColor = false;
            lobbyAllColorId = 0;
            readGhostChat = false;
            enableExtendedChat = true;
            enableFastChat = true;
            allowLinksAndSymbols = false;
            enableChatHistory = true;
            enableClipboard = true;
            enableChatBubbleCopy = true;
            enableChatNickCopy = false;
            enableChatLog = true;
            enableColorCommand = false;
            blockRainbowChat = true;
            blockFortegreenChat = true;
            AnimAsteroidsEnabled = false;
            IsScanning = false;
            AnimShieldsEnabled = false;
            AnimCamsInUseEnabled = false;
            AnimEmptyGarbageEnabled = false;
            skipShhhAnim = false;
            skipRoleIntroAnim = false;
            skipKillAnimation = false;
            localRainbow = false;
            localRainbowFreeOnly = false;
            RevealVotesEnabled = false;
            noTaskMode = false;
            noMapCooldowns = false;
            autoRepairSabotage = false;
            autoBreakSabotage = false;
            allowTasksAsImpostor = false;
            hostAutoKillRandom = false;
            hostAutoKillTarget = false;
            hostAutoKillTargetId = byte.MaxValue;
            bugRoomAutoAngel = false;
            bugRoomAutoAngelIntervalSeconds = 0.15f;
            bugRoomAutoKillShield = false;
            killWhileVanishedHostOnly = false;
            disableEndGameSafeMode = false;
            disableMapSafeMode = false;
            DisableRoleBuffImmortality();
            roleBuffImmortality = false;
            neverEndGame = false;
            autoChatEveryone = false;
            removePenalty = true;
            guestExtraFeatures = false;
            bypassAgeRestrictions = false;
            autoGhostAfterStart = false;
            AutoHostEnabled = false;
            AutoHostShieldBreakEnabled = false;
            AutoReturnLobbyAfterMatch = true;
            AutoHostNotifications = true;
            AutoHostForceLastMinute = true;
            AutoHostWaitLoadedPlayers = true;
            AutoHostCancelBelowMin = true;
            AutoHostInstantStart = false;
            AutoHostAutoRunEnabled = false;
            BugroomScoutEnabled = false;
            autoBanEnabled = true;
            allowDuplicateColors = false;
            blockSpoofRPC = true;
            autoBanPlatformSpoof = false;
            banCustomPlatformsFromTxt = false;
            autoKickLowLevelEnabled = false;
            autoKickBugs = false;
            disableVoteKicks = false;
            banVoteKickVoters = false;
            votekickAutoRejoin = false;
            votekickCopyCode = true;
            blockSabotageRPC = true;
            blockGameRpcInLobby = true;
            blockChatFloodRpc = true;
            blockMeetingFloodRpc = true;
            overflowProtection = true;
            blockVentKickExploit = true;
            blockServerTeleports = true;
            enablePasosLimit = true;
            enableLocalPasosBan = true;
            enableHostPasosBan = true;
            enableMalformedPacketGuard = true;
            banMalformedPacketSender = false;
            enableQuickChatEmptyGuard = true;
            banQuickChatEmptySpammer = true;
            enableUnownedSpawnGuard = true;
            enableLocalNameSpoof = false;
            enableLocalFriendCodeSpoof = false;
            SpoofMenuEnabled = false;
            enableBackground = false;
            showWatermark = true;
            hardMenu = false;
            rgbMenuText = false;
            boldMenuText = true;
            EnableCustomNotifs = true;
            LogAllRPCs = true;
            discordRpcEnabled = true;

            settingsDirty = true;
            InitStyles();
            UpdateAccentColor(currentAccentColor);

            ShowNotification("All sliders & toggles reset to default.");
        }
    }
}
