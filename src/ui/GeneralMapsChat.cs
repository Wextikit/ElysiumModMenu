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
                {
                    currentAutoHostSubTab = i;
                    scrollPosition = Vector2.zero;
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(8);

            if (currentAutoHostSubTab == 0) DrawLobbyControls();
            else if (currentAutoHostSubTab == 1) DrawPlayersRoles();
            else if (currentAutoHostSubTab == 2) DrawAntiCheatTab();
            else if (currentAutoHostSubTab == 3) DrawAutoHostTab();
        }

private void DrawMapsTab()
        {
            GUILayout.BeginVertical(menuCardStyle);

            DrawMenuSectionHeader(L("LOBBY CONTROL", "КОНТРОЛЬ ЛОББИ"));
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(L("Spawn Lobby", "Создать лобби"), btnStyle, GUILayout.Height(30))) SpawnLobby();
            if (GUILayout.Button(L("Despawn Lobby", "Удалить лобби"), btnStyle, GUILayout.Height(30))) DespawnLobby();
            GUILayout.EndHorizontal();

            GUILayout.Space(15);

            DrawMenuSectionHeader(L("MAP CONTROL", "КОНТРОЛЬ КАРТЫ"));
            isManualMapSpawn = DrawToggle(isManualMapSpawn, L("Manual Map Spawn Mode", "Ручной спавн карты"), 250);
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.Label(L("Select Map:", "Выбор карты:"), GUILayout.Width(100));
            selectedMapSpawnIdx = (int)GUILayout.HorizontalSlider((int)selectedMapSpawnIdx, 0, mapSpawnNames.Length - 1, sliderStyle, sliderThumbStyle, GUILayout.Width(200));
            GUILayout.Label($"<color=#{GetMenuAccentHex()}>{mapSpawnNames[(int)selectedMapSpawnIdx]}</color>", new GUIStyle(GUI.skin.label) { richText = true });
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(L("Spawn Map", "Создать карту"), activeTabStyle, GUILayout.Height(30))) SpawnMap((int)selectedMapSpawnIdx);
            if (GUILayout.Button(L("Despawn Map", "Удалить карту"), btnStyle, GUILayout.Height(30))) DespawnCurrentMap();
            GUILayout.EndHorizontal();

            GUILayout.Space(15);

            DrawMenuSectionHeader(L("ROOM TELEPORTS (IN-GAME)", "ТЕЛЕПОРТЫ ПО КОМНАТАМ (В ИГРЕ)"));
            if (ShipStatus.Instance != null && PlayerControl.LocalPlayer != null)
            {
                mapsScrollPos = GUILayout.BeginScrollView(mapsScrollPos, GUILayout.Height(160));
                var locations = GetTeleportLocations();
                int columns = 3;
                int count = 0;

                GUILayout.BeginHorizontal();
                foreach (var loc in locations)
                {
                    if (GUILayout.Button(loc.Key, btnStyle, GUILayout.Width(135), GUILayout.Height(30)))
                    {
                        TeleportTo(loc.Value);
                        ShowNotification($"<color=#00FF00>[TELEPORT]</color> {L("Moved to:", "Перемещен в:")} <b>{loc.Key}</b>");
                    }

                    count++;
                    if (count % columns == 0)
                    {
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                    }
                }
                GUILayout.EndHorizontal();
                GUILayout.EndScrollView();
            }
            else
            {
                GUILayout.Label($"<color=#777777>{L("Teleports are only available when you are on a map.", "Телепорты доступны только когда вы находитесь на карте.")}</color>", new GUIStyle(GUI.skin.label) { richText = true, alignment = TextAnchor.MiddleCenter });
            }

            GUILayout.EndVertical();
        }

private void DrawChatSettingsTab()
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
            allowLinksAndSymbols = DrawToggle(allowLinksAndSymbols, L("Unlock Extra Characters", "Разрешить все символы"), 280);
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
            enableClipboard = DrawToggle(enableClipboard, L("Clipboard (Ctrl+C/V)", "Буфер обмена (Ctrl+C/V)"), 280);
            GUILayout.Space(2);
            enableChatMessageDoubleClickCopy = DrawToggle(enableChatMessageDoubleClickCopy, L("Double-click Copy Message", "Дабл-клик копирует сообщение"), 280);
            GUILayout.Space(2);
            enableChatNameColorCopy = DrawToggle(enableChatNameColorCopy, L("Click Copy Name", "Клик по нику копирует ник"), 280);
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

private void TrySendCustomChatMessage(string rawText)
        {
            if (string.IsNullOrWhiteSpace(rawText)) return;
            if (PlayerControl.LocalPlayer == null) return;

            try
            {
                PlayerControl.LocalPlayer.RpcSendChat(rawText.Trim());
            }
            catch { }
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
                    ShowNotification($"<color=#FFCC66>[SPELL]</color> Проверь слова: {joined}");
                }
            }
            catch { }
        }

private static void UpsertPlayerHistory(PlayerControl pc)
        {
            try
            {
                if (pc == null || pc.Data == null || pc.Data.Disconnected) return;
                string name = string.IsNullOrEmpty(pc.Data.PlayerName) ? "Unknown" : pc.Data.PlayerName;
                string fc = GetDisplayedFriendCode(pc.Data);
                string puid = "Unknown";
                string platform = "Unknown";
                string customPlatform = "";
                int level = 1;

                try
                {
                    uint rawLevel = pc.Data.PlayerLevel;
                    if (rawLevel != uint.MaxValue && rawLevel < 10000) level = (int)rawLevel + 1;
                }
                catch { }

                try
                {
                    var client = AmongUsClient.Instance?.GetClientFromCharacter(pc);
                    if (client != null)
                    {
                        platform = GetPlatform(client);
                        customPlatform = GetCustomPlatformName(client);
                        puid = GetClientPuid(client);
                    }
                }
                catch { }

                string key = $"{fc}|{puid}|{name}";
                var item = playerHistoryEntries.FirstOrDefault(x => $"{x.FriendCode}|{x.Puid}|{x.Name}" == key);
                bool changed = false;
                if (item == null)
                {
                    item = new PlayerHistoryEntry
                    {
                        Name = name,
                        FriendCode = fc,
                        Puid = puid,
                        Platform = platform,
                        CustomPlatform = customPlatform,
                        Level = level,
                        FirstSeenUtc = DateTime.UtcNow,
                        LastSeenUtc = DateTime.UtcNow,
                        IsOnline = true
                    };
                    playerHistoryEntries.Add(item);
                    changed = true;
                }
                else
                {
                    changed = item.Name != name ||
                              item.FriendCode != fc ||
                              item.Puid != puid ||
                              item.Platform != platform ||
                              item.CustomPlatform != customPlatform ||
                              item.Level != level ||
                              !item.IsOnline ||
                              item.LeftUtc.HasValue;
                    item.Name = name;
                    item.FriendCode = fc;
                    item.Puid = puid;
                    item.Platform = platform;
                    item.CustomPlatform = customPlatform;
                    item.Level = level;
                    item.LastSeenUtc = DateTime.UtcNow;
                    item.LeftUtc = null;
                    item.IsOnline = true;
                }
                playerHistoryKeysById[pc.PlayerId] = key;
                if (changed) WritePlayerHistoryFile();
            }
            catch { }
        }

private static string GetCustomPlatformName(ClientData client)
        {
            try
            {
                string value = client?.PlatformData?.PlatformName;
                if (string.IsNullOrWhiteSpace(value)) return "";
                value = Regex.Replace(value, "<.*?>", string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(value)) return "";

                string platform = GetPlatform(client);
                if (value.Equals(platform, StringComparison.OrdinalIgnoreCase)) return "";
                if (value.Equals(client.PlatformData.Platform.ToString(), StringComparison.OrdinalIgnoreCase)) return "";
                return value;
            }
            catch { return ""; }
        }

public static string GetClientPuid(ClientData client)
        {
            if (client == null) return "Unknown";

            try
            {
                string direct = client.ProductUserId;
                if (!string.IsNullOrWhiteSpace(direct)) return direct.Trim();
            }
            catch { }

            string[] memberNames = { "ProductUserId", "productUserId", "Puid", "PUID", "puid", "EosId", "EOSId", "ProductId", "PlayerId" };
            foreach (string memberName in memberNames)
            {
                try
                {
                    PropertyInfo prop = client.GetType().GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    object value = prop?.GetValue(client, null);
                    if (value != null && !string.IsNullOrWhiteSpace(value.ToString())) return value.ToString().Trim();
                }
                catch { }

                try
                {
                    FieldInfo field = client.GetType().GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    object value = field?.GetValue(client);
                    if (value != null && !string.IsNullOrWhiteSpace(value.ToString())) return value.ToString().Trim();
                }
                catch { }
            }

            return "Unknown";
        }

private static string FormatPlatformHistory(PlayerHistoryEntry entry)
        {
            if (entry == null) return "Unknown";
            return string.IsNullOrWhiteSpace(entry.CustomPlatform)
                ? entry.Platform
                : $"{entry.Platform} + custom: {entry.CustomPlatform}";
        }

private static string PlayerHistoryFilePath()
        {
            string folder = string.IsNullOrWhiteSpace(Plugin.ElysiumFolder)
                ? System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "ElysiumModMenu")
                : Plugin.ElysiumFolder;
            return System.IO.Path.Combine(folder, "ElysiumPlayerHistory.txt");
        }

private static void MarkPlayerHistoryLeft(byte playerId)
        {
            try
            {
                if (!playerHistoryKeysById.TryGetValue(playerId, out string key)) return;
                var item = playerHistoryEntries.FirstOrDefault(x => $"{x.FriendCode}|{x.Puid}|{x.Name}" == key);
                if (item == null || !item.IsOnline) return;

                item.IsOnline = false;
                item.LeftUtc = DateTime.UtcNow;
                item.LastSeenUtc = item.LeftUtc.Value;
                WritePlayerHistoryFile();
            }
            catch { }
        }

public static void RecordPlayerRpc(PlayerControl pc, byte callId)
        {
            try
            {
                if (VanillaRpcIds.Contains(callId)) return;
                if (pc == null || pc.Data == null) return;
                UpsertPlayerHistory(pc);

                if (!playerHistoryKeysById.TryGetValue(pc.PlayerId, out string key)) return;
                var item = playerHistoryEntries.FirstOrDefault(x => $"{x.FriendCode}|{x.Puid}|{x.Name}" == key);
                if (item == null) return;

                if (!item.RpcCalls.Contains(callId))
                {
                    item.RpcCalls.Add(callId);
                    item.RpcCalls.Sort();
                    WritePlayerHistoryFile();
                }
            }
            catch { }
        }

private static string FormatRpcHistory(PlayerHistoryEntry entry)
        {
            if (entry == null || entry.RpcCalls == null || entry.RpcCalls.Count == 0) return "нет";
            byte[] customRpcCalls = entry.RpcCalls.Where(x => !VanillaRpcIds.Contains(x)).Distinct().OrderBy(x => x).ToArray();
            if (customRpcCalls.Length == 0) return "нет";
            return string.Join(", ", customRpcCalls.Select(x => x.ToString()).ToArray());
        }

private static void WritePlayerHistoryFile()
        {
            try
            {
                string path = PlayerHistoryFilePath();
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));

                List<string> lines = new List<string>
                {
                    "ElysiumModMenu Player History",
                    $"Updated UTC: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}",
                    ""
                };

                foreach (var e in playerHistoryEntries.OrderByDescending(x => x.LastSeenUtc))
                {
                    string left = e.LeftUtc.HasValue ? e.LeftUtc.Value.ToString("yyyy-MM-dd HH:mm:ss") : "online";
                    lines.Add($"Nick: {e.Name}");
                    lines.Add($"Level: {e.Level}");
                    lines.Add($"FriendCode: {e.FriendCode}");
                    lines.Add($"PUID: {e.Puid}");
                    lines.Add($"Joined UTC: {e.FirstSeenUtc:yyyy-MM-dd HH:mm:ss}");
                    lines.Add($"Left UTC: {left}");
                    lines.Add($"Platform: {FormatPlatformHistory(e)}");
                    lines.Add($"RPC calls: {FormatRpcHistory(e)}");
                    lines.Add(new string('-', 48));
                }

                System.IO.File.WriteAllLines(path, lines.ToArray(), Encoding.UTF8);
            }
            catch { }
        }

private void TryHostOnlyKillAuraTick()
        {
            if (!killAuraHostOnly)
            {
                killAuraTimer = 0f;
                return;
            }

            if (AmongUsClient.Instance == null) return;
            if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data == null) return;
            if (PlayerControl.LocalPlayer.Data.IsDead) return;
            if (!RoleManager.IsImpostorRole(PlayerControl.LocalPlayer.Data.RoleType)) return;
            if (MeetingHud.Instance != null) return;
            if (PlayerControl.LocalPlayer.inVent || PlayerControl.LocalPlayer.onLadder) return;
            if (!noKillCooldownHostOnly && GetRemainingKillCooldown(PlayerControl.LocalPlayer.PlayerId) > 0.05f) return;

            killAuraTimer += Time.deltaTime;
            if (killAuraTimer < 0.05f) return;

            if (PlayerControl.AllPlayerControls == null) return;

            PlayerControl nearestTarget = null;
            float nearestDistance = float.MaxValue;
            Vector3 localPos = PlayerControl.LocalPlayer.transform.position;
            Vector2 localPos2D = new Vector2(localPos.x, localPos.y);

            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (pc == null || pc == PlayerControl.LocalPlayer || pc.Data == null) continue;
                if (pc.Data.Disconnected || pc.Data.IsDead) continue;
                if (pc.inVent || pc.onLadder) continue;

                Vector3 targetPos = pc.transform.position;
                float dist = Vector2.Distance(localPos2D, new Vector2(targetPos.x, targetPos.y));
                if (dist <= 2.2f && dist < nearestDistance)
                {
                    nearestDistance = dist;
                    nearestTarget = pc;
                }
            }

            if (nearestTarget == null) return;

            try
            {
                PlayerControl.LocalPlayer.CmdCheckMurder(nearestTarget);
                PlayerControl.LocalPlayer.RpcMurderPlayer(nearestTarget, true);

                if (AmongUsClient.Instance.AmHost)
                    PlayerControl.LocalPlayer.SetKillTimer(noKillCooldownHostOnly ? 0f : GetConfiguredKillCooldown());

                killAuraTimer = 0f;
            }
            catch { }
        }

private void DrawAntiCheatTab()
        {
            Event wheelEvent = Event.current;
            if (wheelEvent != null && wheelEvent.type == EventType.ScrollWheel)
            {
                scrollPosition.y = Mathf.Max(0f, scrollPosition.y + wheelEvent.delta.y * 32f);
                wheelEvent.Use();
            }

            float antiCheatColumnWidth = (windowRect.width - 186f) / 2f;
            if (antiCheatColumnWidth < 282f) antiCheatColumnWidth = 282f;

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(menuCardStyle, GUILayout.Width(antiCheatColumnWidth));

            DrawMenuSectionHeader(L("PUNISHMENT SYSTEM", "СИСТЕМА НАКАЗАНИЙ"));
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            GUILayout.Label(L("Mode:", "Режим:"), toggleLabelStyle, GUILayout.Width(60));

            GUIStyle middleLabelStyle = new GUIStyle(btnStyle) { fontStyle = FontStyle.Bold, normal = { background = null, textColor = GetMenuAccentColor() } };

            if (GUILayout.Button("<", btnStyle, GUILayout.Width(25), GUILayout.Height(25)))
            {
                punishmentMode--;
                if (punishmentMode < 0) punishmentMode = punishmentNames.Length - 1;
                settingsDirty = true;
            }

            GUILayout.Label(punishmentNames[punishmentMode], middleLabelStyle, GUILayout.ExpandWidth(true), GUILayout.Height(25));

            if (GUILayout.Button(">", btnStyle, GUILayout.Width(25), GUILayout.Height(25)))
            {
                punishmentMode++;
                if (punishmentMode >= punishmentNames.Length) punishmentMode = 0;
                settingsDirty = true;
            }
            GUILayout.EndHorizontal();

            string modeDesc = punishmentMode switch
            {
                0 => "<color=#777777>Null: Пакеты блокируются без действий.</color>",
                1 => "<color=#FFFF00>Warn: Блокировка + Уведомление на экран.</color>",
                2 => "<color=#FF8800>Kick: Игрок будет исключен из лобби.</color>",
                3 => "<color=#FF0000>Ban: Игрок будет забанен (Host Only).</color>",
                _ => ""
            };
            GUILayout.Label(modeDesc, new GUIStyle(GUI.skin.label) { richText = true, fontSize = 11, wordWrap = true });

            GUILayout.Space(12);
            DrawMenuSectionHeader(L("RPC PROTECTIONS", "ЗАЩИТА RPC"));

            blockSpoofRPC = DrawToggle(blockSpoofRPC, L("Block Spoof RPC", "Блокировать spoof RPC"), 250);
            GUILayout.Space(5);
            blockSabotageRPC = DrawToggle(blockSabotageRPC, L("Block Sabotage & Meetings", "Блокировать саботажи и митинги"), 250);
            GUILayout.Space(5);
            blockGameRpcInLobby = DrawToggle(blockGameRpcInLobby, L("Block Game RPC in Lobby", "Блокировать игровые RPC в лобби"), 250);
            GUILayout.Space(5);

            autoBanPlatformSpoof = DrawToggle(autoBanPlatformSpoof, L("Auto-Ban Platform Spoof (Host)", "Авто-бан Platform Spoof (Хост)"), 250);
            GUILayout.Space(5);
            banCustomPlatformsFromTxt = DrawToggle(banCustomPlatformsFromTxt, L("Ban Custom Platforms From TXT", "Бан кастом платформ из TXT"), 250);
            GUILayout.Space(5);

            blockMeetingFloodRpc = DrawToggle(blockMeetingFloodRpc, L("Block Meeting RPC Flood", "Блокировать флуд RPC митинга"), 250);
            GUILayout.Space(5);
            blockChatFloodRpc = DrawToggle(blockChatFloodRpc, L("Block Chat RPC Flood", "Блокировать флуд RPC чата"), 250);
            GUILayout.Space(5);
            enablePasosLimit = DrawToggle(enablePasosLimit, L("RPC Anti-Cheat", "RPC Античит"), 250);
            GUILayout.Space(5);
            oldAntiCheatVersion = DrawToggle(oldAntiCheatVersion, L("anti-cheat old version", "anti-cheat old version"), 250);
            GUILayout.Space(5);
            banMalformedPacketSender = DrawToggle(banMalformedPacketSender, L("Ban Malformed Sender (Host)", "Бан за кривые пакеты (Хост)"), 250);
            GUILayout.Space(5);
            enableQuickChatEmptyGuard = DrawToggle(enableQuickChatEmptyGuard, L("QuickChat Anti-Crash", "Анти-краш QuickChat"), 250);
            GUILayout.Space(5);
            banQuickChatEmptySpammer = DrawToggle(banQuickChatEmptySpammer, L("Ban QuickChat Spammer (Host)", "Бан за QuickChat спам (Хост)"), 250);
            GUILayout.Space(5);
            GUILayout.Space(15);
            DrawMenuSectionHeader(L("OTHER PROTECTIONS", "ПРОЧАЯ ЗАЩИТА"));

            disableVoteKicks = DrawToggle(disableVoteKicks, L("Disable Vote Kicks (Host)", "Запрет кика голосованием (Хост)"), 250);
            GUILayout.Space(5);

            autoKickBugs = DrawToggle(autoKickBugs, L("Auto-Kick Fortegreen", "Авто-кик багнутых игроков"), 250);
            if (autoKickBugs)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(L("Timer:", "Таймер:"), new GUIStyle(toggleLabelStyle), GUILayout.Height(22), GUILayout.Width(62));
                autoKickTimer = GUILayout.HorizontalSlider(autoKickTimer, 1f, 15f, sliderStyle, sliderThumbStyle, GUILayout.Width(112));
                GUILayout.Space(8);
                GUILayout.Label(autoKickTimer.ToString("0.0") + "s", menuBadgeStyle, GUILayout.Width(46), GUILayout.Height(22));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            GUILayout.Space(5);
            autoBanBrokenFriendCode = DrawToggle(autoBanBrokenFriendCode, L("Auto-Ban Broken FriendCode (Host)", "Авто-бан сломанного FriendCode (Хост)"), 250);
            GUILayout.Space(5);
            autoKickLowLevelEnabled = DrawToggle(autoKickLowLevelEnabled, L("Kick Low Level (Host)", "Кик по уровню (Хост)"), 250);
            if (autoKickLowLevelEnabled)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(L("Min level:", "Мин. уровень:"), new GUIStyle(toggleLabelStyle), GUILayout.Height(22), GUILayout.Width(86));
                int oldMinLevel = autoKickMinLevel;
                autoKickMinLevel = Mathf.Clamp((int)GUILayout.HorizontalSlider(autoKickMinLevel, 1f, 300f, sliderStyle, sliderThumbStyle, GUILayout.Width(112)), 1, 300);
                if (oldMinLevel != autoKickMinLevel) settingsDirty = true;
                GUILayout.Space(8);
                GUILayout.Label(autoKickMinLevel.ToString(), menuBadgeStyle, GUILayout.Width(46), GUILayout.Height(22));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            GUILayout.Space(5);
            banBotsEnabled = DrawToggle(banBotsEnabled, L("Ban Bots (Host)", "Бан ботов (Хост)"), 250);

            GUILayout.EndVertical();
            GUILayout.Space(10);

            GUILayout.BeginVertical(GUILayout.Width(antiCheatColumnWidth), GUILayout.ExpandHeight(true));
            GUILayout.BeginVertical(menuCardStyle, GUILayout.Height(285f));
            DrawMenuSectionHeader(L("BAN LIST", "БАН ЛИСТ"));
            autoBanEnabled = DrawToggle(autoBanEnabled, L("Auto-Ban Blacklisted Players", "Авто-бан игроков из списка"), 250);
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            string defaultBanText = L("Enter Friend Code", "Введите Friend Code");
            string banValue = string.IsNullOrEmpty(banInput) && !isEditingBan ? defaultBanText : banInput;

            if (DrawPseudoInputButton(banValue, isEditingBan, 25f, 46))
            {
                isEditingBan = !isEditingBan;
                isEditingGhostChatColor = false;
                ResetAllBindWaits();
            }

            if (GUILayout.Button(L("ADD", "ДОБАВИТЬ"), btnStyle, GUILayout.Width(75f), GUILayout.Height(25f)))
            {
                if (!string.IsNullOrWhiteSpace(banInput))
                {
                    AddToBanList(banInput.Trim(), "Manual", "Unknown", "Manual ban");
                    banInput = "";
                    isEditingBan = false;
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            banListScroll = GUILayout.BeginScrollView(banListScroll, GUILayout.Height(185f));

            if (bannedEntries.Count == 0)
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label($"<color=#777777>{L("Ban list is empty.", "Бан лист пуст.")}</color>", new GUIStyle(GUI.skin.label) { richText = true, alignment = TextAnchor.MiddleCenter });
                GUILayout.FlexibleSpace();
            }
            else
            {
                for (int i = 0; i < bannedEntries.Count; i++)
                {
                    string entry = bannedEntries[i];
                    if (string.IsNullOrWhiteSpace(entry)) continue;

                    string[] parts = entry.Split('|');
                    string disp = parts.Length >= 3 ? $"{parts[2]} ({parts[0]})" : entry;

                    GUILayout.BeginHorizontal(boxStyle);
                    GUILayout.Label(disp, new GUIStyle(GUI.skin.label) { fontSize = 12 }, GUILayout.Width(185));
                    GUILayout.FlexibleSpace();

                    GUIStyle redCrossStyle = new GUIStyle(btnStyle);
                    redCrossStyle.normal.textColor = new Color(1f, 0.3f, 0.3f);

                    bool removedEntry = false;
                    if (GUILayout.Button("X", redCrossStyle, GUILayout.Width(25), GUILayout.Height(22)))
                    {
                        RemoveFromBanList(entry);
                        removedEntry = true;
                    }
                    GUILayout.EndHorizontal();
                    if (removedEntry) break;
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.Space(8f);
            GUILayout.BeginVertical(menuCardStyle, GUILayout.ExpandHeight(true));
            DrawMenuSectionHeader("BAN / KICK PLAYER");
            GUILayout.Space(4f);

            List<RoomPlayerActionEntry> roomPlayers = new List<RoomPlayerActionEntry>();
            try
            {
                if (PlayerControl.AllPlayerControls != null)
                {
                    foreach (PlayerControl player in PlayerControl.AllPlayerControls)
                    {
                        try
                        {
                            if (player == null || player == PlayerControl.LocalPlayer || player.Data == null || player.Data.Disconnected)
                                continue;

                            string playerName = Regex.Replace(player.Data.PlayerName ?? "Unknown", "<.*?>", string.Empty);
                            if (playerName.Length > 18) playerName = playerName.Substring(0, 15) + "...";

                            int level = 1;
                            try { level = (int)player.Data.PlayerLevel + 1; } catch { }

                            ClientData client = AmongUsClient.Instance.GetClientFromCharacter(player);
                            roomPlayers.Add(new RoomPlayerActionEntry
                            {
                                ownerId = (int)player.OwnerId,
                                playerName = playerName,
                                level = level,
                                friendCode = player.Data.FriendCode ?? string.Empty,
                                puid = client == null ? string.Empty : GetClientPuid(client)
                            });
                        }
                        catch { }
                    }
                }
            }
            catch { }

            roomPlayerActionsScroll = GUILayout.BeginScrollView(roomPlayerActionsScroll, GUILayout.Height(220f));
            foreach (RoomPlayerActionEntry player in roomPlayers)
            {
                GUILayout.BeginHorizontal(boxStyle);
                GUILayout.Label($"{player.playerName}  <color=#777777>Lv:{player.level}</color>",
                    new GUIStyle(GUI.skin.label) { fontSize = 11, richText = true }, GUILayout.ExpandWidth(true));

                bool previousEnabled = GUI.enabled;
                GUI.enabled = previousEnabled && AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost;

                if (GUILayout.Button("KICK", btnStyle, GUILayout.Width(48f), GUILayout.Height(22f)))
                {
                    try
                    {
                        AmongUsClient.Instance.KickPlayer(player.ownerId, false);
                        ShowNotification($"<color=#FFAA33>[KICK]</color> {player.playerName}");
                    }
                    catch { }
                }

                GUIStyle banButtonStyle = new GUIStyle(btnStyle);
                banButtonStyle.normal.textColor = new Color(1f, 0.35f, 0.35f);
                if (GUILayout.Button("BAN", banButtonStyle, GUILayout.Width(45f), GUILayout.Height(22f)))
                {
                    try
                    {
                        string banKey = !string.IsNullOrWhiteSpace(player.friendCode)
                            ? player.friendCode
                            : (!string.IsNullOrWhiteSpace(player.puid) ? "PUID:" + player.puid : "Client:" + player.ownerId);

                        AddToBanList(banKey, string.IsNullOrWhiteSpace(player.puid) ? "Unknown" : player.puid,
                            player.playerName, "Manual room ban");
                        AmongUsClient.Instance.KickPlayer(player.ownerId, true);
                        ShowNotification($"<color=#FF4444>[BAN]</color> {player.playerName}");
                    }
                    catch { }
                }

                GUI.enabled = previousEnabled;
                GUILayout.EndHorizontal();
            }

            if (roomPlayers.Count == 0)
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("<color=#777777>No players in the room.</color>",
                    new GUIStyle(GUI.skin.label) { richText = true, alignment = TextAnchor.MiddleCenter });
                GUILayout.FlexibleSpace();
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }
    }
}
