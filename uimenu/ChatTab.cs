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
private int currentChatSubTab = 0;

private int currentSelfChatSubTab = 0;

private string[] chatSubTabs => new string[] { L("SETTINGS", "НАСТРОЙКИ"), L("PORTABLE", "ПОРТАТИВНЫЙ"), L("SYMBOLS", "СИМВОЛЫ") };

public static List<string> portableChatLogs = new List<string>();

public static string portableChatInput = string.Empty;

public static bool isEditingPortableChat = false;

private static string lastPortableChatLogKey = string.Empty;

private static float lastPortableChatLogAt = -10f;

private Vector2 portableChatScrollPos = Vector2.zero;

private static int portableChatLogVersion = 0;

private int seenPortableChatLogVersion = -1;

private Vector2 symbolScrollPos = Vector2.zero;

private static readonly string[] chatSymbolRows = new string[]
{
    "★ ☆ ✦ ✧ ✪ ✿ ♥ ♦ ♣ ♠",
    "← → ↑ ↓ ↔ ↕ ✓ ✕ ! ?",
    "α β γ δ λ π Ω ∞ ≠ ≈ ±",
    "０ １ ２ ３ ４ ５ ６ ７ ８ ９"
};

private void DrawChatSettingsTab()
        {
            currentChatSubTab = Mathf.Clamp(currentChatSubTab, 0, chatSubTabs.Length - 1);

            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            for (int i = 0; i < chatSubTabs.Length; i++)
            {
                if (GUILayout.Button(chatSubTabs[i], currentChatSubTab == i ? activeSubTabStyle : subTabStyle, GUILayout.Height(22), GUILayout.ExpandWidth(true)))
                {
                    currentChatSubTab = i;
                    scrollPosition = Vector2.zero;
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            if (currentChatSubTab == 0) DrawChatSettingsContent();
            else if (currentChatSubTab == 1) DrawPortableChatTab();
            else if (currentChatSubTab == 2) DrawChatSymbolsTab();

            GUILayout.EndVertical();
        }

private void DrawChatSettingsContent()
        {
            GUILayout.BeginVertical(boxStyle);
            GUILayout.Label(L("CHAT SETTINGS & LOGS", "НАСТРОЙКИ ЧАТА И ЛОГИ"), headerStyle);
            GUILayout.Space(10);

            string hexColor = GetMenuAccentHex();

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(GUILayout.Width(300));
            GUILayout.Label($"<b><color=#{hexColor}>{L("LOCAL FEATURES", "ЛОКАЛЬНЫЕ ФУНКЦИИ")}</color></b>", toggleLabelStyle);
            GUILayout.Space(6);
            alwaysChat = DrawToggle(alwaysChat, L("Always Show Chat", "Всегда показывать чат"), 280);
            GUILayout.Space(2);
            readGhostChat = DrawToggle(readGhostChat, L("Read Ghost Chat", "Читать чат призраков"), 280);
            GUILayout.Space(4);
            DrawGhostChatColorControl(280f);
            GUILayout.Space(2);
            enableExtendedChat = DrawToggle(enableExtendedChat, L("Extended Chat (120 chars)", "Длинный чат (120 симв.)"), 280);
            GUILayout.Space(2);
            enableFastChat = DrawToggle(enableFastChat, L("Fast Chat (3.1 to 2.1", "Быстрый чат (c 3.1 до 2.1)"), 280);
            GUILayout.Space(2);
            allowLinksAndSymbols = DrawToggle(allowLinksAndSymbols, L("Unlock Extra Characters", "Экстра символы"), 280);
            GUILayout.Space(2);
            enableSpellCheck = DrawToggle(enableSpellCheck, L("Spell Check (Basic)", "Проверка орфографии (Базовая)"), 280);
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            GUILayout.Label($"<b><color=#{hexColor}>{L("UTILITY OPTIONS", "УТИЛИТЫ")}</color></b>", toggleLabelStyle);
            GUILayout.Space(6);
            enableChatHistory = DrawToggle(enableChatHistory, L("Chat History (Up/Down)", "История чата (Стрелочки)"), 280);
            GUILayout.Space(2);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{L("History size:", "Размер истории:")} <color=#{hexColor}>{chatHistoryLimit}</color>", new GUIStyle(toggleLabelStyle) { richText = true }, GUILayout.Height(22), GUILayout.Width(130));
            chatHistoryLimit = Mathf.Clamp((int)GUILayout.HorizontalSlider(chatHistoryLimit, 5f, 80f, sliderStyle, sliderThumbStyle, GUILayout.Width(145)), 5, 80);
            TrimChatHistoryToLimit();
            GUILayout.EndHorizontal();
            GUILayout.Space(2);
            enableClipboard = DrawToggle(enableClipboard, L("Chat Input Hotkeys (Ctrl+C/X/A/V)", "Горячие клавиши ввода (Ctrl+C/X/A/V)"), 280);
            GUILayout.Space(2);
            enableChatBubbleCopy = DrawToggle(enableChatBubbleCopy, L("Copy Message by Double Click", "Копировать сообщение двойным кликом"), 280);
            GUILayout.Space(2);
            enableChatNickCopy = DrawToggle(enableChatNickCopy, L("Copy Nick by Double Click", "Копировать ник двойным кликом"), 280);
            GUILayout.Space(2);
            enableChatLog = DrawToggle(enableChatLog, L("Save Chat Log to File", "Сохранять лог чата в файл"), 280);
            GUILayout.Space(2);
            enableChatDarkMode = DrawToggle(enableChatDarkMode, L("Dark Chat Theme", "Темная тема чата"), 280);
            if (enableChatDarkMode && GUILayout.Button(L("Turn Off Dark Chat", "Выключить темный чат"), btnStyle, GUILayout.Width(180), GUILayout.Height(24)))
            {
                enableChatDarkMode = false;
                SaveConfig();
            }

            GUILayout.Space(8);

            GUILayout.Label($"<b><color=#{hexColor}>{L("HOST LOBBY OPTIONS", "НАСТРОЙКИ ХОСТА")}</color></b>", toggleLabelStyle);
            GUILayout.Space(6);
            enableColorCommand = DrawToggle(enableColorCommand, L("Enable /color command", "Разрешить команду /color"), 280);
            GUILayout.Space(2);
            blockFortegreenChat = DrawToggle(blockFortegreenChat, L("Block Fortegreen Chat", "Запрет чата Fortegreen"), 280);
            GUILayout.Space(2);
            blockRainbowChat = DrawToggle(blockRainbowChat, L("Block Rainbow Chat", "Запрет радужного чата"), 280);
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            GUILayout.Space(12);

            GUILayout.Label($"<b><color=#{hexColor}>{L("CHAT SENDER", "ОТПРАВКА ЧАТА")}</color></b>", toggleLabelStyle);
            GUILayout.Space(6);

            GUILayout.BeginVertical(boxStyle);
            GUILayout.Space(6);

            GUIStyle macFieldStyle = new GUIStyle(GUI.skin.textField)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleLeft
            };
            macFieldStyle.normal.textColor = whiteMenuTheme ? new Color(0.12f, 0.12f, 0.12f, 1f) : new Color(0.9f, 0.9f, 0.9f, 1f);
            macFieldStyle.padding = new RectOffset();
            macFieldStyle.padding.left = 12;
            macFieldStyle.padding.right = 12;
            macFieldStyle.padding.top = 8;
            macFieldStyle.padding.bottom = 8;
            macFieldStyle.margin = new RectOffset();
            macFieldStyle.margin.left = 4;
            macFieldStyle.margin.right = 4;
            macFieldStyle.margin.top = 4;
            macFieldStyle.margin.bottom = 4;

            Rect chatInputRect = GUILayoutUtility.GetRect(10f, 34f, GUILayout.ExpandWidth(true), GUILayout.Height(34));
            GUI.Box(chatInputRect, string.Empty, macFieldStyle);

            string drawText = string.IsNullOrEmpty(customChatMessage)
                ? L("Type a message...", "Введите сообщение...")
                : customChatMessage;

            if (customChatInputFocused && (Time.unscaledTime % 1f) < 0.5f)
                drawText += "|";

            GUIStyle chatInputTextStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip,
                richText = false,
                fontSize = 12
            };
            chatInputTextStyle.normal.textColor = whiteMenuTheme ? new Color(0.12f, 0.12f, 0.12f, 1f) : new Color(0.9f, 0.9f, 0.9f, 1f);

            Rect textRect = new Rect(chatInputRect.x + 12f, chatInputRect.y + 4f, chatInputRect.width - 24f, chatInputRect.height - 8f);
            GUI.Label(textRect, drawText, chatInputTextStyle);

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
                        if (customChatMessage.Length < 120)
                            customChatMessage += e.character;
                        e.Use();
                    }
                }
            }

            GUILayout.Space(10);

            GUILayout.BeginHorizontal(GUILayout.Height(30));
            if (GUILayout.Button(L("Send Chat", "Отправить"), btnStyle, GUILayout.Width(150), GUILayout.Height(30)))
                TrySendCustomChatMessage(customChatMessage);

            GUILayout.Space(10);
            string spamBtnText = customChatSpamEnabled ? L("Spam: ON", "Спам: ВКЛ") : L("Spam: OFF", "Спам: ВЫКЛ");
            if (GUILayout.Button(spamBtnText, customChatSpamEnabled ? activeTabStyle : btnStyle, GUILayout.Width(150), GUILayout.Height(30)))
                customChatSpamEnabled = !customChatSpamEnabled;

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(12);

            GUILayout.BeginHorizontal(GUILayout.Height(24));
            GUILayout.Label($"{L("Delay:", "Задержка:")} {Mathf.Round(customChatSpamDelay * 10f) / 10f}s", new GUIStyle(toggleLabelStyle) { fontSize = 11 }, GUILayout.Height(22), GUILayout.Width(122));
            customChatSpamDelay = GUILayout.HorizontalSlider(customChatSpamDelay, 0.5f, 10f, sliderStyle, sliderThumbStyle, GUILayout.Width(300));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.EndVertical();

            GUILayout.Space(10);

            GUILayout.Label($"<b><color=#{hexColor}>{L("COMMANDS & INFO", "КОМАНДЫ И ИНФОРМАЦИЯ")}</color></b>", toggleLabelStyle);
            GUILayout.Space(4);

            GUILayout.Label($"<color=#FFAC1C><b>{L("Whisper:", "Шепот:")}</b></color> /w, /pm, /msg [Name/ID/Color] [Text]", new GUIStyle(GUI.skin.label) { richText = true, fontSize = 12 });
            GUILayout.Label($"<color=#777777>{L("Sends a private message to a player on your screen only.", "Отправляет личное сообщение выбранному игроку (видит только он и вы).")}</color>", new GUIStyle(GUI.skin.label) { richText = true, fontSize = 11, wordWrap = true });

            GUILayout.Space(6);

            GUILayout.Label($"<color=#777777><b>Log Info:</b> {L("ChatLog.txt clears every 3 game restarts.", "Файл ChatLog.txt очищается каждые 3 запуска игры.")}</color>", new GUIStyle(GUI.skin.label) { richText = true, fontSize = 11, wordWrap = true });

            GUILayout.EndVertical();
        }

private void DrawPortableChatTab()
        {
            GUILayout.BeginVertical(menuCardStyle, GUILayout.ExpandHeight(false));
            DrawMenuSectionHeader(L("PORTABLE CHAT", "ПОРТАТИВНЫЙ ЧАТ"));
            GUILayout.Label(L("Read recent messages and send chat without opening the game chat panel.", "Читайте последние сообщения и отправляйте чат без открытия игровой панели."), menuDescStyle);
            GUILayout.Space(8);

            GUIStyle logBoxStyle = new GUIStyle(boxStyle);
            logBoxStyle.padding = CreateRectOffset(8, 8, 6, 6);
            logBoxStyle.margin = CreateRectOffset(0, 0, 0, 0);

            float logHeight = Mathf.Clamp(windowRect.height - 235f, 120f, 285f);
            if (seenPortableChatLogVersion != portableChatLogVersion)
            {
                portableChatScrollPos.y = float.MaxValue;
                seenPortableChatLogVersion = portableChatLogVersion;
            }

            GUILayout.BeginVertical(logBoxStyle, GUILayout.ExpandWidth(true), GUILayout.Height(logHeight));
            portableChatScrollPos = GUILayout.BeginScrollView(portableChatScrollPos, false, true, GUIStyle.none, GUI.skin.verticalScrollbar, GUIStyle.none, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            if (portableChatLogs.Count == 0)
            {
                GUIStyle emptyStyle = new GUIStyle(menuDescStyle)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 12,
                    wordWrap = true
                };
                GUILayout.FlexibleSpace();
                GUILayout.Label(L("No messages yet.", "Сообщений пока нет."), emptyStyle, GUILayout.ExpandWidth(true));
                GUILayout.FlexibleSpace();
            }
            else
            {
                GUIStyle rowStyle = new GUIStyle(GUI.skin.label)
                {
                    richText = true,
                    wordWrap = true,
                    fontSize = 12,
                    normal = { textColor = whiteMenuTheme ? new Color(0.12f, 0.12f, 0.12f, 1f) : new Color(0.9f, 0.9f, 0.9f, 1f) }
                };

                foreach (string log in portableChatLogs)
                    GUILayout.Label(log, rowStyle);
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.Space(8);
            DrawChatTextInput(ref portableChatInput, ref isEditingPortableChat, L("Type a message...", "Введите сообщение..."), 120);
            GUILayout.Space(8);

            GUILayout.BeginHorizontal(GUILayout.Height(28));
            if (GUILayout.Button(L("Send", "Отправить"), btnStyle, GUILayout.Width(120), GUILayout.Height(28)))
                SendPortableChatMessage();

            if (GUILayout.Button(L("Clear Log", "Очистить лог"), btnStyle, GUILayout.Width(120), GUILayout.Height(28)))
            {
                portableChatLogs.Clear();
                lastPortableChatLogKey = string.Empty;
                lastPortableChatLogAt = -10f;
                portableChatLogVersion++;
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

private void DrawChatSymbolsTab()
        {
            GUILayout.BeginVertical(menuCardStyle);
            DrawMenuSectionHeader(L("SYMBOL KEYBOARD", "КЛАВИАТУРА СИМВОЛОВ"));
            GUILayout.Label(L("Click a symbol to append it to the portable chat and the in-game chat input.", "Нажмите символ, чтобы добавить его в портативный и игровой ввод чата."), menuDescStyle);
            GUILayout.Space(8);

            symbolScrollPos = GUILayout.BeginScrollView(symbolScrollPos, false, true, GUIStyle.none, GUI.skin.verticalScrollbar, GUIStyle.none);
            foreach (string row in chatSymbolRows)
            {
                GUILayout.BeginHorizontal();
                string[] symbols = row.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string symbol in symbols)
                {
                    if (GUILayout.Button(symbol, btnStyle, GUILayout.Width(42), GUILayout.Height(34)))
                        InsertSymbolIntoChatInputs(symbol);
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.Space(4);
            }
            GUILayout.EndScrollView();

            GUILayout.Space(8);
            DrawChatTextInput(ref portableChatInput, ref isEditingPortableChat, L("Symbol output...", "Вывод символов..."), 120);
            GUILayout.Space(8);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(L("Send", "Отправить"), btnStyle, GUILayout.Width(120), GUILayout.Height(28)))
                SendPortableChatMessage();
            if (GUILayout.Button(L("Backspace", "Удалить"), btnStyle, GUILayout.Width(120), GUILayout.Height(28)) && !string.IsNullOrEmpty(portableChatInput))
                portableChatInput = portableChatInput.Substring(0, portableChatInput.Length - 1);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

private void DrawChatTextInput(ref string input, ref bool focused, string placeholder, int maxLength)
        {
            GUIStyle fieldStyle = new GUIStyle(GUI.skin.textField)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip,
                padding = CreateRectOffset(12, 12, 8, 8)
            };
            fieldStyle.normal.textColor = whiteMenuTheme ? new Color(0.12f, 0.12f, 0.12f, 1f) : new Color(0.9f, 0.9f, 0.9f, 1f);

            Rect inputRect = GUILayoutUtility.GetRect(10f, 34f, GUILayout.ExpandWidth(true), GUILayout.Height(34));
            GUI.Box(inputRect, string.Empty, fieldStyle);

            string drawText = string.IsNullOrEmpty(input) ? placeholder : input;
            if (focused && (Time.unscaledTime % 1f) < 0.5f) drawText += "|";

            GUIStyle textStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip,
                richText = false,
                fontSize = 12
            };
            textStyle.normal.textColor = whiteMenuTheme ? new Color(0.12f, 0.12f, 0.12f, 1f) : new Color(0.9f, 0.9f, 0.9f, 1f);
            GUI.Label(new Rect(inputRect.x + 12f, inputRect.y + 4f, inputRect.width - 24f, inputRect.height - 8f), drawText, textStyle);

            Event e = Event.current;
            if (e == null) return;

            if (e.type == EventType.MouseDown)
            {
                focused = inputRect.Contains(e.mousePosition);
                if (focused) e.Use();
            }
            else if (focused && e.type == EventType.KeyDown)
            {
                if (HandleClipboardShortcut(e, ref input, maxLength))
                {
                }
                else if (e.keyCode == KeyCode.Backspace)
                {
                    if (!string.IsNullOrEmpty(input))
                        input = input.Substring(0, input.Length - 1);
                    e.Use();
                }
                else if (e.keyCode == KeyCode.Escape)
                {
                    focused = false;
                    e.Use();
                }
                else if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
                {
                    SendPortableChatMessage();
                    e.Use();
                }
                else if (!char.IsControl(e.character))
                {
                    if (input == null) input = string.Empty;
                    if (input.Length < maxLength)
                        input += e.character;
                    e.Use();
                }
            }
        }

private void SendPortableChatMessage()
        {
            if (TrySendCustomChatMessage(portableChatInput))
            {
                portableChatInput = string.Empty;
                isEditingPortableChat = false;
                portableChatScrollPos.y = float.MaxValue;
            }
        }

private void InsertSymbolIntoChatInputs(string symbol)
        {
            if (string.IsNullOrEmpty(symbol)) return;

            if ((portableChatInput?.Length ?? 0) + symbol.Length <= 120)
                portableChatInput = (portableChatInput ?? string.Empty) + symbol;

            try
            {
                TextBoxTMP textArea = HudManager.Instance?.Chat?.freeChatField?.textArea;
                if (textArea != null && (textArea.text?.Length ?? 0) + symbol.Length <= 120)
                    textArea.text = (textArea.text ?? string.Empty) + symbol;
            }
            catch { }
        }

public static void AddPortableChatLog(PlayerControl sourcePlayer, string chatText)
        {
            if (string.IsNullOrWhiteSpace(chatText)) return;

            try
            {
                string time = DateTime.Now.ToString("HH:mm:ss");
                string name = "System";
                bool isLocal = false;
                bool isDead = false;
                int sourceId = -1;

                if (sourcePlayer != null && sourcePlayer.Data != null)
                {
                    name = sourcePlayer.Data.PlayerName ?? "Player";
                    isLocal = sourcePlayer == PlayerControl.LocalPlayer;
                    isDead = sourcePlayer.Data.IsDead;
                    sourceId = sourcePlayer.PlayerId;
                }

                string safeName = CleanPortableChatText(name, 24);
                string safeText = CleanPortableChatText(chatText, 220);
                if (string.IsNullOrEmpty(safeText)) return;

                float now = Time.unscaledTime;
                string logKey = sourceId + "|" + safeText;
                if (logKey == lastPortableChatLogKey && now - lastPortableChatLogAt < 0.75f)
                    return;
                lastPortableChatLogKey = logKey;
                lastPortableChatLogAt = now;

                string timeColor = whiteMenuTheme ? "666666" : "888888";
                string nameColor = isLocal
                    ? (whiteMenuTheme ? "007CA6" : "59D8FF")
                    : (isDead ? (whiteMenuTheme ? "7A4DCF" : "D7B8FF") : (whiteMenuTheme ? "222222" : "EAEAEA"));
                string textColor = whiteMenuTheme ? "222222" : "EAEAEA";
                portableChatLogs.Add($"<color=#{timeColor}>[{time}]</color> <color=#{nameColor}>{safeName}</color>: <color=#{textColor}>{safeText}</color>");

                while (portableChatLogs.Count > 80)
                    portableChatLogs.RemoveAt(0);

                portableChatLogVersion++;
            }
            catch { }
        }

private static string CleanPortableChatText(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            string clean = Regex.Replace(value, "<.*?>", string.Empty)
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Replace("<", "(")
                .Replace(">", ")");
            if (clean.Length > maxLength)
                clean = clean.Substring(0, maxLength - 3) + "...";
            return clean;
        }

private bool TrySendCustomChatMessage(string rawText)
        {
            if (string.IsNullOrWhiteSpace(rawText)) return false;
            if (PlayerControl.LocalPlayer == null)
            {
                AddPortableChatLog(null, L("Cannot send: local player is not ready.", "Нельзя отправить: локальный игрок не готов."));
                return false;
            }

            try
            {
                string message = rawText.Trim();
                if (enableChatHistory) ChatHistory.Remember(message);
                PlayerControl.LocalPlayer.RpcSendChat(message);
                AddPortableChatLog(PlayerControl.LocalPlayer, message);
                return true;
            }
            catch
            {
                AddPortableChatLog(null, L("Failed to send message.", "Не удалось отправить сообщение."));
                return false;
            }
        }

private static readonly HashSet<string> BasicSpellDictionary = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "hello","hi","gg","wp","yes","no","ok","pls","please","thanks","thx","go","come","start","skip","vote","report","body","kill","who","where","why",
            "привет","да","нет","ок","пж","пожалуйста","спасибо","го","старт","скип","голос","репорт","труп","килл","кто","где","почему","лол"
        };

private static void TrySpellCheckNotify(string text)
        {
            if (!enableSpellCheck || string.IsNullOrWhiteSpace(text)) return;
            if (text.StartsWith("/") || text.StartsWith("!")) return;

            try
            {
                var words = Regex.Matches(text.ToLower(), @"[a-zа-яё]{3,}");
                List<string> suspicious = new List<string>();
                foreach (Match m in words)
                {
                    string w = m.Value;
                    if (w.Length < 3) continue;
                    if (BasicSpellDictionary.Contains(w)) continue;
                    if (suspicious.Contains(w)) continue;
                    suspicious.Add(w);
                    if (suspicious.Count >= 4) break;
                }

                if (suspicious.Count > 0)
                {
                    string joined = string.Join(", ", suspicious);
                    ShowNotification($"<color=#FFCC66>[SPELL]</color> Check words: {joined}");
                }
            }
            catch { }
        }
    }
}
