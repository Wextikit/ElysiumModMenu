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
private static readonly string[] selfChatSubTabs = { "SETTINGS", "PORTABLE" };

private void DrawChatSettingsCompact(float columnWidth)
        {
            float contentWidth = Mathf.Min(Mathf.Max(120f, columnWidth - 8f), 600f);
            currentSelfChatSubTab = Mathf.Clamp(currentSelfChatSubTab, 0, selfChatSubTabs.Length - 1);

            GUILayout.BeginHorizontal(GUILayout.Width(contentWidth));
            for (int i = 0; i < selfChatSubTabs.Length; i++)
            {
                if (GUILayout.Button(selfChatSubTabs[i], currentSelfChatSubTab == i ? compactActiveSubTabStyle : compactSubTabStyle, GUILayout.Height(18)))
                    SetMultiTab("selfChat", ref currentSelfChatSubTab, i, selfChatSubTabs.Length, false);
                if (i < selfChatSubTabs.Length - 1) GUILayout.Space(4);
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(6);

            if (currentSelfChatSubTab == 1)
            {
                BeginMultiTabContent("selfChat", out Matrix4x4 portableMatrix, out Color portableColor);
                try
                {
                    DrawPortableChatTab(contentWidth);
                }
                finally
                {
                    EndMultiTabContent(portableMatrix, portableColor);
                }
                return;
            }

            BeginMultiTabContent("selfChat", out Matrix4x4 oldMatrix, out Color oldColor);
            try
            {
            float gap = 6f;
            int columns = contentWidth >= 560f ? 3 : 2;
            float blockWidth = Mathf.Floor((contentWidth - (gap * (columns - 1))) / columns);
            int toggleWidth = Mathf.RoundToInt(Mathf.Clamp(blockWidth - 16f, 150f, 210f));
            GUIStyle compactCard = CreateCompactMenuCardStyle();
            GUIStyle smallLabel = compactLabelStyle10;
            const float blockHeight = 172f;

            void DrawChatBlock(string title, System.Action content)
            {
                GUILayout.BeginVertical(compactCard, GUILayout.Width(blockWidth), GUILayout.Height(blockHeight));
                DrawMenuSectionHeader(title);
                content();
                GUILayout.FlexibleSpace();
                GUILayout.EndVertical();
            }

            void DrawHistorySizeRow()
            {
                GUILayout.BeginHorizontal(GUILayout.Width(toggleWidth), GUILayout.Height(20));
                GUILayout.Label("History Size", smallLabel, GUILayout.Width(76), GUILayout.Height(20));
                GUILayout.Label($"{chatHistoryLimit}", smallLabel, GUILayout.Width(22), GUILayout.Height(20));
                GUILayout.Space(5);
                int prevLimit = chatHistoryLimit;
                chatHistoryLimit = Mathf.Clamp((int)GUILayout.HorizontalSlider(chatHistoryLimit, 5f, 300f, sliderStyle, sliderThumbStyle, GUILayout.ExpandWidth(true)), 5, 300);
                if (prevLimit != chatHistoryLimit)
                {
                    TrimChatHistoryToLimit();
                    settingsDirty = true;
                }
                GUILayout.EndHorizontal();
            }

            void DrawSendBlock()
            {
                GUILayout.BeginVertical(compactCard, GUILayout.Width(blockWidth), GUILayout.Height(blockHeight));
                DrawMenuSectionHeader("SEND");
                GUILayout.Space(2);

                Rect chatInputRect = GUILayoutUtility.GetRect(10f, 24f, GUILayout.ExpandWidth(true), GUILayout.Height(24));
                GUI.Box(chatInputRect, string.Empty, compactChatFieldStyle);

                string drawText = string.IsNullOrEmpty(customChatMessage)
                    ? L("Type a message...", "Р’РІРµРґРёС‚Рµ СЃРѕРѕР±С‰РµРЅРёРµ...")
                    : customChatMessage;
                if (customChatInputFocused && (Time.unscaledTime % 1f) < 0.5f) drawText += "|";

                GUI.Label(new Rect(chatInputRect.x + 9f, chatInputRect.y + 3f, chatInputRect.width - 18f, chatInputRect.height - 6f), drawText, compactChatInputStyle);

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
                GUILayout.Label($"{L("Delay:", "Р—Р°РґРµСЂР¶РєР°:")} {Mathf.Round(customChatSpamDelay * 10f) / 10f}s", toggleLabelStyle11, GUILayout.Width(82), GUILayout.Height(17));
                customChatSpamDelay = GUILayout.HorizontalSlider(customChatSpamDelay, 0.5f, 10f, sliderStyle, sliderThumbStyle, GUILayout.ExpandWidth(true));
                GUILayout.EndHorizontal();

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("sendQUICK CHAT1", quickChatTileStyle, GUILayout.Width(toggleWidth), GUILayout.Height(24)))
                    SendQuickChatPhrase();

                GUILayout.EndVertical();
            }

            void SendQuickChatPhrase()
            {
                if (PlayerControl.LocalPlayer == null || AmongUsClient.Instance == null) return;
                try
                {
                    MessageWriter wr = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)RpcCalls.SendQuickChat, SendOption.Reliable, -1);
                    wr.Write((byte)3); wr.Write((ushort)78); wr.Write((byte)1); wr.Write((byte)2); wr.Write((ushort)1716);
                    AmongUsClient.Instance.FinishRpcImmediately(wr);

                    MessageWriter local = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)RpcCalls.SendQuickChat, SendOption.Reliable, PlayerControl.LocalPlayer.OwnerId);
                    local.Write((byte)3); local.Write((ushort)78); local.Write((byte)1); local.Write((byte)2); local.Write((ushort)1716);
                    AmongUsClient.Instance.FinishRpcImmediately(local);
                }
                catch { }
            }

            void DrawChatColorBlock()
            {
                GUILayout.BeginVertical(compactCard, GUILayout.Width(blockWidth), GUILayout.Height(blockHeight));
                DrawMenuSectionHeader("CHAT COLOR");
                GUILayout.Space(4);

                GUILayout.Label("Ghost Chat", smallLabel, GUILayout.Width(toggleWidth), GUILayout.Height(16));
                GUILayout.BeginHorizontal(GUILayout.Width(toggleWidth), GUILayout.Height(24));
                try
                {
                    if (DrawPseudoInputButton(ghostChatColorHex, isEditingGhostChatColor, 24f, 16))
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
                    GUILayout.Space(5);
                    if (GUILayout.Button("OK", btnStyle, GUILayout.Width(38), GUILayout.Height(24)))
                    {
                        isEditingGhostChatColor = false;
                        ghostChatColorHex = SanitizeGhostChatColorSetting(ghostChatColorHex);
                        SaveConfig();
                    }
                }
                finally { GUILayout.EndHorizontal(); }

                GUILayout.Space(5);
                GUILayout.Label(RenderGhostChatMessageText("Preview ghost chat"), compactPreviewStyle, GUILayout.Width(toggleWidth), GUILayout.Height(18));

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
                enableExtendedChat = DrawCompactToggle(enableExtendedChat, "Extended Chat", toggleWidth);
                GUILayout.Space(1);
                enableFastChat = DrawCompactToggle(enableFastChat, "Fast Chat", toggleWidth);
                GUILayout.Space(7);
                enableChatHistory = DrawCompactToggle(enableChatHistory, "Chat History", toggleWidth);
                GUILayout.Space(6);
                DrawHistorySizeRow();
            });

            GUILayout.Space(gap);
            DrawChatBlock("COPY", () =>
            {
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
                allowLinksAndSymbols = DrawCompactToggle(allowLinksAndSymbols, "Bypass URL Block", toggleWidth);
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
                DrawChatColorBlock();
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.Space(5);
                GUILayout.BeginHorizontal(GUILayout.Width(contentWidth));
                DrawChatColorBlock();
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
            }
            finally
            {
                EndMultiTabContent(oldMatrix, oldColor);
            }
        }

private void DrawGhostChatColorControl(float width)
        {
            GUILayout.BeginHorizontal(GUILayout.Width(width));
            GUILayout.Label(L("Ghost Chat:", "Чат призраков:"), toggleLabelStyle11, GUILayout.Width(74), GUILayout.Height(24));
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
            if (GUILayout.Button(L("Apply", "Применить"), btnStyle, GUILayout.Width(48), GUILayout.Height(24)))
            {
                isEditingGhostChatColor = false;
                ghostChatColorHex = SanitizeGhostChatColorSetting(ghostChatColorHex);
                SaveConfig();
            }
            GUILayout.EndHorizontal();

            GUILayout.Label(RenderGhostChatMessageText(L("Preview ghost chat color", "РџСЂРёРјРµСЂ С†РІРµС‚Р° С‡Р°С‚Р° РїСЂРёР·СЂР°РєРѕРІ")), richClipLabelStyle11, GUILayout.Width(width), GUILayout.Height(16f));
        }
    }
}
