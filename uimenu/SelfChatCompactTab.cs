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
private void DrawChatSettingsCompact(float columnWidth)
        {
            float contentWidth = Mathf.Clamp(columnWidth - 8f, 380f, 600f);
            string[] selfChatSubTabs = { "SETTINGS", "PORTABLE" };
            currentSelfChatSubTab = Mathf.Clamp(currentSelfChatSubTab, 0, selfChatSubTabs.Length - 1);

            GUIStyle compactSubTab = new GUIStyle(subTabStyle)
            {
                fontSize = 10,
                clipping = TextClipping.Clip,
                wordWrap = false,
                padding = CreateRectOffset(5, 5, 1, 1)
            };
            GUIStyle compactActiveSubTab = new GUIStyle(activeSubTabStyle)
            {
                fontSize = 10,
                clipping = TextClipping.Clip,
                wordWrap = false,
                padding = CreateRectOffset(5, 5, 1, 1)
            };

            GUILayout.BeginHorizontal(GUILayout.Width(contentWidth));
            for (int i = 0; i < selfChatSubTabs.Length; i++)
            {
                if (GUILayout.Button(selfChatSubTabs[i], currentSelfChatSubTab == i ? compactActiveSubTab : compactSubTab, GUILayout.Height(18)))
                    currentSelfChatSubTab = i;
                if (i < selfChatSubTabs.Length - 1) GUILayout.Space(4);
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(6);

            if (currentSelfChatSubTab == 1)
            {
                DrawPortableChatTab();
                return;
            }

            float gap = 6f;
            int columns = contentWidth >= 560f ? 3 : 2;
            float blockWidth = Mathf.Floor((contentWidth - (gap * (columns - 1))) / columns);
            int toggleWidth = Mathf.RoundToInt(Mathf.Clamp(blockWidth - 16f, 150f, 210f));
            GUIStyle compactCard = CreateCompactMenuCardStyle();
            GUIStyle smallLabel = new GUIStyle(toggleLabelStyle) { fontSize = 10, clipping = TextClipping.Clip };
            const float blockHeight = 148f;

            void DrawChatBlock(string title, System.Action drawContent)
            {
                GUILayout.BeginVertical(compactCard, GUILayout.Width(blockWidth), GUILayout.Height(blockHeight));
                DrawMenuSectionHeader(title);
                drawContent?.Invoke();
                GUILayout.FlexibleSpace();
                GUILayout.EndVertical();
            }

            void DrawSendBlock()
            {
                GUILayout.BeginVertical(compactCard, GUILayout.Width(blockWidth), GUILayout.Height(blockHeight));
                DrawMenuSectionHeader("SEND");
                GUILayout.Space(2);

                GUIStyle fieldStyle = new GUIStyle(GUI.skin.textField)
                {
                    fontSize = 12,
                    alignment = TextAnchor.MiddleLeft,
                    clipping = TextClipping.Clip
                };
                fieldStyle.normal.textColor = whiteMenuTheme ? new Color(0.12f, 0.12f, 0.12f, 1f) : new Color(0.9f, 0.9f, 0.9f, 1f);

                Rect chatInputRect = GUILayoutUtility.GetRect(10f, 24f, GUILayout.ExpandWidth(true), GUILayout.Height(24));
                GUI.Box(chatInputRect, string.Empty, fieldStyle);

                string drawText = string.IsNullOrEmpty(customChatMessage)
                    ? L("Type a message...", "Р’РІРµРґРёС‚Рµ СЃРѕРѕР±С‰РµРЅРёРµ...")
                    : customChatMessage;
                if (customChatInputFocused && (Time.unscaledTime % 1f) < 0.5f) drawText += "|";

                GUIStyle chatInputTextStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleLeft,
                    clipping = TextClipping.Clip,
                    richText = false,
                    fontSize = 11
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

                GUILayout.Space(3);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(L("Send", "РћС‚РїСЂР°РІРёС‚СЊ"), btnStyle, GUILayout.Height(22)))
                    TrySendCustomChatMessage(customChatMessage);
                GUILayout.Space(5);
                string spamBtnText = customChatSpamEnabled ? L("Spam ON", "РЎРїР°Рј Р’РљР›") : L("Spam OFF", "РЎРїР°Рј Р’Р«РљР›");
                if (GUILayout.Button(spamBtnText, customChatSpamEnabled ? activeTabStyle : btnStyle, GUILayout.Height(22)))
                    customChatSpamEnabled = !customChatSpamEnabled;
                GUILayout.EndHorizontal();

                GUILayout.Space(3);
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{L("Delay:", "Р—Р°РґРµСЂР¶РєР°:")} {Mathf.Round(customChatSpamDelay * 10f) / 10f}s", new GUIStyle(toggleLabelStyle) { fontSize = 11 }, GUILayout.Width(82), GUILayout.Height(17));
                customChatSpamDelay = GUILayout.HorizontalSlider(customChatSpamDelay, 0.5f, 10f, sliderStyle, sliderThumbStyle, GUILayout.ExpandWidth(true));
                GUILayout.EndHorizontal();

                GUILayout.EndVertical();
            }

            void SendQuickChatPhrase(string text)
            {
                if (PlayerControl.LocalPlayer == null || AmongUsClient.Instance == null) return;
                try
                {
                    string safe = (text ?? "").Replace("[", "").Replace("]", "");
                    MessageWriter wr = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)RpcCalls.SendQuickChat, SendOption.Reliable, -1);
                    wr.Write((byte)3); wr.Write((ushort)78); wr.Write((byte)2); wr.Write((byte)2); wr.Write((byte)15); wr.Write(safe);
                    AmongUsClient.Instance.FinishRpcImmediately(wr);
                }
                catch { }
            }

            void DrawQuickChatBlock()
            {
                GUILayout.BeginVertical(compactCard, GUILayout.Width(blockWidth), GUILayout.Height(blockHeight));
                DrawMenuSectionHeader("QUICK CHAT");
                GUILayout.Space(4);

                const string qcSend = "<color=#FF0000>Р±РµСЂРµРіРёС‚РµСЃСЊ РїСЂРѕС‚РёРІРѕРїСЂР°РІРЅС‹С… СЃРµСЃСѓР°Р»СЊРЅС‹С… РґРµР№СЃС‚РІРёР№ РїСЂРµРґР°С‚РµР»СЊР№ СЃСЂРµРґРё РЅР°СЃ</color>";
                GUIStyle tile = new GUIStyle(btnStyle)
                {
                    wordWrap = false,
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 11,
                    clipping = TextClipping.Clip
                };

                if (GUILayout.Button("sendQUICK CHAT1", tile, GUILayout.Width(toggleWidth), GUILayout.Height(28)))
                    SendQuickChatPhrase(qcSend);

                GUILayout.FlexibleSpace();
                GUILayout.EndVertical();
            }

            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal(GUILayout.Width(contentWidth));
            DrawChatBlock("CORE", () =>
            {
                alwaysChat = DrawCompactToggle(alwaysChat, "Always Show Chat", toggleWidth);
                GUILayout.Space(1);
                readGhostChat = DrawCompactToggle(readGhostChat, "Read Ghost Chat", toggleWidth);
                GUILayout.Space(1);
                GUILayout.BeginHorizontal(GUILayout.Width(toggleWidth), GUILayout.Height(20));
                GUILayout.Label("Ghost Chat Color", smallLabel, GUILayout.Width(94), GUILayout.Height(20));
                GUILayout.Space(3);
                if (DrawPseudoInputButton(ghostChatColorHex, isEditingGhostChatColor, 20f, 16))
                {
                    isEditingGhostChatColor = !isEditingGhostChatColor;
                    if (isEditingGhostChatColor) ghostChatColorHex = FilterGhostChatColorInput(ghostChatColorHex);
                    isEditingName = false;
                    isEditingLevel = false;
                    isEditingFriendCode = false;
                    isEditingLocalFriendCode = false;
                    isEditingBan = false;
                    ResetAllBindWaits();
                }
                if (GUILayout.Button("OK", btnStyle, GUILayout.Width(30), GUILayout.Height(20)))
                {
                    isEditingGhostChatColor = false;
                    ghostChatColorHex = SanitizeGhostChatColorSetting(ghostChatColorHex);
                    SaveConfig();
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(1);
                enableExtendedChat = DrawCompactToggle(enableExtendedChat, "Extended Chat", toggleWidth);
                GUILayout.Space(1);
                enableFastChat = DrawCompactToggle(enableFastChat, "Fast Chat", toggleWidth);
            });

            GUILayout.Space(gap);
            DrawChatBlock("COPY", () =>
            {
                enableChatHistory = DrawCompactToggle(enableChatHistory, "Chat History", toggleWidth);
                GUILayout.Space(1);
                GUILayout.BeginHorizontal(GUILayout.Width(toggleWidth), GUILayout.Height(20));
                GUILayout.Label("History Size", smallLabel, GUILayout.Width(76), GUILayout.Height(20));
                GUILayout.Label($"{chatHistoryLimit}", smallLabel, GUILayout.Width(22), GUILayout.Height(20));
                GUILayout.Space(3);
                chatHistoryLimit = Mathf.Clamp((int)GUILayout.HorizontalSlider(chatHistoryLimit, 5f, 80f, sliderStyle, sliderThumbStyle, GUILayout.ExpandWidth(true)), 5, 80);
                TrimChatHistoryToLimit();
                GUILayout.EndHorizontal();
                GUILayout.Space(1);
                enableClipboard = DrawCompactToggle(enableClipboard, "Chat Input Hotkeys", toggleWidth);
                GUILayout.Space(1);
                enableChatBubbleCopy = DrawCompactToggle(enableChatBubbleCopy, "Copy Message", toggleWidth);
                GUILayout.Space(1);
                enableChatNickCopy = DrawCompactToggle(enableChatNickCopy, "Copy Nickname", toggleWidth);
                GUILayout.Space(1);
                enableChatLog = DrawCompactToggle(enableChatLog, "Save Chat Log", toggleWidth);
            });

            if (columns == 3)
            {
                GUILayout.Space(gap);
            }
            else
            {
                GUILayout.EndHorizontal();
                GUILayout.Space(5);
                GUILayout.BeginHorizontal(GUILayout.Width(contentWidth));
            }

            DrawChatBlock("FILTER", () =>
            {
                allowLinksAndSymbols = DrawCompactToggle(allowLinksAndSymbols, "Extra Symbols", toggleWidth);
                GUILayout.Space(1);
                enableSpellCheck = DrawCompactToggle(enableSpellCheck, "Spell Check", toggleWidth);
                GUILayout.Space(1);
                enableChatDarkMode = DrawCompactToggle(enableChatDarkMode, "Dark Chat Theme", toggleWidth);
                GUILayout.Space(1);
                enableColorCommand = DrawCompactToggle(enableColorCommand, "Enable /color", toggleWidth);
                GUILayout.Space(1);
                blockFortegreenChat = DrawCompactToggle(blockFortegreenChat, "Block Fortegreen", toggleWidth);
                GUILayout.Space(1);
                blockRainbowChat = DrawCompactToggle(blockRainbowChat, "Block Rainbow", toggleWidth);
            });
            if (columns == 2)
            {
                GUILayout.Space(gap);
                DrawSendBlock();
            }
            GUILayout.EndHorizontal();

            if (columns == 3)
            {
                GUILayout.Space(6);
                GUILayout.BeginHorizontal(GUILayout.Width(contentWidth));
                DrawSendBlock();
                GUILayout.Space(gap);
                DrawQuickChatBlock();
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.Space(5);
                GUILayout.BeginHorizontal(GUILayout.Width(contentWidth));
                DrawQuickChatBlock();
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
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
                    ghostChatColorHex = FilterGhostChatColorInput(ghostChatColorHex);
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
                ghostChatColorHex = SanitizeGhostChatColorSetting(ghostChatColorHex);
                SaveConfig();
            }
            GUILayout.EndHorizontal();

            GUILayout.Label(RenderGhostChatMessageText(L("Preview ghost chat color", "РџСЂРёРјРµСЂ С†РІРµС‚Р° С‡Р°С‚Р° РїСЂРёР·СЂР°РєРѕРІ")), new GUIStyle(GUI.skin.label) { richText = true, fontSize = 11, wordWrap = false, clipping = TextClipping.Clip }, GUILayout.Width(width), GUILayout.Height(16f));
        }
    }
}
