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

[HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.HandleAnimation))]
        public static class PlayerPhysics_HandleAnimation
        {
            public static bool Prefix(PlayerPhysics __instance)
            {
                if (ElysiumModMenuGUI.moonWalk && __instance.AmOwner)
                {
                    __instance.ResetAnimState();
                    return false;
                }
                return true;
            }
        }

[HarmonyPatch(typeof(FreeChatInputField), nameof(FreeChatInputField.UpdateCharCount))]
        public static class FreeChatInputField_UpdateCharCount_Patch
        {
            public static void Postfix(FreeChatInputField __instance)
            {
                if (__instance == null || __instance.textArea == null || __instance.charCountText == null) return;

                __instance.textArea.characterLimit = 120;

                int length = __instance.textArea.text.Length;

                __instance.charCountText.SetText($"{length}/{__instance.textArea.characterLimit}");

                if (length < 90)
                {
                    __instance.charCountText.color = Color.white;
                }
                else if (length < 115)
                {
                    __instance.charCountText.color = Color.yellow;
                }
                else
                {
                    __instance.charCountText.color = Color.red;
                }
            }
        }

public static class ChatHistory
        {
            public static List<string> sentMessages = new List<string>();
            public static int HistoryIndex = -1;
            public static string DraftBeforeHistory = "";
            public static bool BrowsingHistory = false;

            public static void Remember(string message)
            {
                if (string.IsNullOrWhiteSpace(message)) return;
                bool isNewEntry = sentMessages.Count == 0 || sentMessages[sentMessages.Count - 1] != message;
                if (isNewEntry)
                {
                    sentMessages.Add(message);
                    while (sentMessages.Count > ElysiumModMenuGUI.chatHistoryLimit)
                        sentMessages.RemoveAt(0);
                }
                HistoryIndex = sentMessages.Count;
            }

            public static void HandleNavigation(ChatController chat)
            {
                if (sentMessages.Count == 0 || chat.freeChatField == null || chat.freeChatField.textArea == null || !chat.freeChatField.textArea.hasFocus)
                    return;

                if (Input.GetKeyDown(KeyCode.UpArrow))
                {
                    if (!BrowsingHistory)
                    {
                        DraftBeforeHistory = chat.freeChatField.textArea.text;
                        BrowsingHistory = true;
                    }
                    if (HistoryIndex <= 0) return;

                    HistoryIndex = Mathf.Clamp(HistoryIndex - 1, 0, sentMessages.Count - 1);
                    chat.freeChatField.textArea.SetText(sentMessages[HistoryIndex], string.Empty);
                }
                else if (Input.GetKeyDown(KeyCode.DownArrow))
                {
                    if (!BrowsingHistory) return;

                    HistoryIndex += 1;
                    if (HistoryIndex < sentMessages.Count)
                    {
                        chat.freeChatField.textArea.SetText(sentMessages[HistoryIndex], string.Empty);
                    }
                    else
                    {
                        chat.freeChatField.textArea.SetText(DraftBeforeHistory, string.Empty);
                        BrowsingHistory = false;
                    }
                }
            }
        }

public static class ClipboardBridge
        {
            private static bool isPastingChatInput = false;
            private static int currentPasteCharPos = 0;
            private static int lastClipboardFrame = -1;

            public static void Run(TextBoxTMP box)
            {
                if (!enableClipboard) return;
                if (box == null || !box.hasFocus) return;

                bool controlHeld = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
                bool shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

                bool copyPressed = controlHeld && (Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.Insert));
                bool pastePressed = (controlHeld && Input.GetKeyDown(KeyCode.V)) || (shiftHeld && Input.GetKeyDown(KeyCode.Insert));
                bool cutPressed = controlHeld && Input.GetKeyDown(KeyCode.X);

                if (!copyPressed && !pastePressed && !cutPressed) return;
                if (lastClipboardFrame == Time.frameCount) return;
                lastClipboardFrame = Time.frameCount;

                if (copyPressed)
                {
                    GUIUtility.systemCopyBuffer = box.text ?? string.Empty;
                }
                else if (pastePressed)
                {
                    string paste = GUIUtility.systemCopyBuffer;
                    if (!string.IsNullOrEmpty(paste))
                    {
                        string currentText = box.text ?? string.Empty;
                        int caretPos = Mathf.Clamp(box.caretPos, 0, currentText.Length);
                        string nextText = currentText.Insert(caretPos, paste);

                        isPastingChatInput = true;
                        box.SetText(nextText, string.Empty);
                        isPastingChatInput = false;
                    }
                }
                else if (cutPressed)
                {
                    GUIUtility.systemCopyBuffer = box.text ?? string.Empty;
                    box.SetText(string.Empty, string.Empty);
                }
            }

            public static bool IsCharAllowed(TextBoxTMP box, ref bool result)
            {
                if (box == null) return true;

                string compositionString = Input.compositionString;
                if (!string.IsNullOrEmpty(compositionString))
                {
                    result = true;
                    return false;
                }

                string input = isPastingChatInput ? GUIUtility.systemCopyBuffer : Input.inputString;
                if (string.IsNullOrEmpty(input)) return true;

                string currentText = box.text ?? string.Empty;
                int caretPos = Mathf.Clamp(box.caretPos, 0, currentText.Length);
                string text = currentText.Insert(caretPos, input);

                currentPasteCharPos = Mathf.Clamp(currentPasteCharPos, 0, Mathf.Max(0, text.Length - 1));
                char currentChar = text[currentPasteCharPos];
                currentPasteCharPos = currentPasteCharPos >= text.Length - 1 ? 0 : currentPasteCharPos + 1;

                if (allowLinksAndSymbols)
                {
                    HashSet<char> blockedSymbols = new HashSet<char> { '\b', '\r', '\n', '>', '<', '[' };
                    result = !blockedSymbols.Contains(currentChar);
                    return false;
                }

                return true;
            }
        }

public static class ChatBubbleCopyInteraction
        {
            private const float DoubleClickSeconds = 0.35f;
            private static int lastMessageBubbleId = -1;
            private static float lastMessageClickAt = -10f;
            private static string lastMessageText = string.Empty;

            public static void HandleClick(ChatController chat)
            {
                if ((!enableChatMessageDoubleClickCopy && !enableChatNameColorCopy) || chat == null) return;
                if (!chat.IsOpenOrOpening || !Input.GetMouseButtonDown(0)) return;

                Camera camera = HudManager.Instance?.UICamera ?? Camera.main;
                if (camera == null) return;

                Vector2 screenPoint = new Vector2(Input.mousePosition.x, Input.mousePosition.y);

                if (enableChatNameColorCopy && TryFindNameBubble(chat, screenPoint, camera, out ChatBubble nameBubble))
                {
                    CopyPlayerName(nameBubble);
                    ResetMessageClick();
                    return;
                }

                if (!enableChatMessageDoubleClickCopy || !TryFindMessageBubble(chat, screenPoint, camera, out ChatBubble messageBubble))
                {
                    return;
                }

                int bubbleId = messageBubble.GetInstanceID();
                string clickedMessage = GetBubbleMessage(messageBubble);
                if (string.IsNullOrWhiteSpace(clickedMessage)) return;
                float now = Time.unscaledTime;
                if (lastMessageBubbleId == bubbleId && lastMessageText == clickedMessage &&
                    now - lastMessageClickAt <= DoubleClickSeconds)
                {
                    CopyMessage(clickedMessage);
                    ResetMessageClick();
                    return;
                }

                lastMessageBubbleId = bubbleId;
                lastMessageClickAt = now;
                lastMessageText = clickedMessage;
            }

            private static bool TryFindNameBubble(ChatController chat, Vector2 screenPoint, Camera camera, out ChatBubble bubble)
            {
                bubble = null;
                ChatBubble[] bubbles = UnityEngine.Object.FindObjectsOfType<ChatBubble>();
                if (bubbles == null) return false;

                float bestDistance = float.MaxValue;

                for (int i = 0; i < bubbles.Length; i++)
                {
                    ChatBubble candidate = bubbles[i];
                    if (!IsBubbleInCurrentChat(chat, candidate) || candidate.NameText == null) continue;
                    if (TryGetTextScreenRect(candidate.NameText, camera, out Rect rect) && rect.Contains(screenPoint))
                    {
                        float distance = (screenPoint - rect.center).sqrMagnitude;
                        if (distance < bestDistance)
                        {
                            bestDistance = distance;
                            bubble = candidate;
                        }
                    }
                }

                return bubble != null;
            }

            private static bool TryFindMessageBubble(ChatController chat, Vector2 screenPoint, Camera camera, out ChatBubble bubble)
            {
                bubble = null;
                ChatBubble[] bubbles = UnityEngine.Object.FindObjectsOfType<ChatBubble>();
                if (bubbles == null) return false;

                float bestDistance = float.MaxValue;

                for (int i = 0; i < bubbles.Length; i++)
                {
                    ChatBubble candidate = bubbles[i];
                    if (!IsBubbleInCurrentChat(chat, candidate) || candidate.TextArea == null) continue;
                    if (TryGetTextScreenRect(candidate.TextArea, camera, out Rect rect) && rect.Contains(screenPoint))
                    {
                        float distance = (screenPoint - rect.center).sqrMagnitude;
                        if (distance < bestDistance)
                        {
                            bestDistance = distance;
                            bubble = candidate;
                        }
                    }
                }

                return bubble != null;
            }

            private static bool IsBubbleInCurrentChat(ChatController chat, ChatBubble bubble)
            {
                if (chat == null || bubble == null || !bubble.gameObject.activeInHierarchy) return false;

                try
                {
                    Transform inner = chat.scroller?.Inner;
                    return inner == null || bubble.transform.IsChildOf(inner);
                }
                catch { return false; }
            }

            private static bool TryGetTextScreenRect(TMP_Text text, Camera camera, out Rect rect)
            {
                rect = default;
                if (text == null || camera == null || !text.gameObject.activeInHierarchy) return false;

                try
                {
                    text.ForceMeshUpdate();
                    Renderer renderer = text.GetComponent<Renderer>();
                    if (renderer == null) return false;

                    Bounds bounds = renderer.bounds;
                    if (bounds.size.sqrMagnitude <= 0.0001f) return false;

                    Vector3 min = bounds.min;
                    Vector3 max = bounds.max;
                    Vector3[] corners =
                    {
                        new Vector3(min.x, min.y, min.z), new Vector3(min.x, max.y, min.z),
                        new Vector3(max.x, min.y, min.z), new Vector3(max.x, max.y, min.z),
                        new Vector3(min.x, min.y, max.z), new Vector3(min.x, max.y, max.z),
                        new Vector3(max.x, min.y, max.z), new Vector3(max.x, max.y, max.z)
                    };

                    float minX = float.MaxValue;
                    float minY = float.MaxValue;
                    float maxX = float.MinValue;
                    float maxY = float.MinValue;
                    for (int i = 0; i < corners.Length; i++)
                    {
                        Vector3 screen = camera.WorldToScreenPoint(corners[i]);
                        if (screen.z <= 0f) continue;
                        minX = Mathf.Min(minX, screen.x);
                        minY = Mathf.Min(minY, screen.y);
                        maxX = Mathf.Max(maxX, screen.x);
                        maxY = Mathf.Max(maxY, screen.y);
                    }

                    if (minX == float.MaxValue || maxX <= minX || maxY <= minY) return false;
                    const float padding = 2f;
                    rect = Rect.MinMaxRect(minX - padding, minY - padding, maxX + padding, maxY + padding);
                    return true;
                }
                catch { }

                return false;
            }

            private static string GetBubbleMessage(ChatBubble bubble)
            {
                return StripRichText(bubble != null && bubble.TextArea != null
                    ? bubble.TextArea.text
                    : string.Empty).Trim();
            }

            private static void CopyMessage(string message)
            {
                if (string.IsNullOrWhiteSpace(message)) return;
                GUIUtility.systemCopyBuffer = message;
                ElysiumModMenuGUI.ShowNotification("<color=#00FFAA>[CHAT]</color> Message copied.");
            }

            private static void CopyPlayerName(ChatBubble bubble)
            {
                string playerName = GetBubblePlayerName(bubble);
                if (string.IsNullOrWhiteSpace(playerName)) return;

                GUIUtility.systemCopyBuffer = playerName;
                ElysiumModMenuGUI.ShowNotification("<color=#00FFAA>[CHAT]</color> Name copied.");
            }

            private static string GetBubblePlayerName(ChatBubble bubble)
            {
                try
                {
                    if (bubble != null && bubble.playerInfo != null && !string.IsNullOrWhiteSpace(bubble.playerInfo.PlayerName))
                    {
                        return StripRichText(bubble.playerInfo.PlayerName).Trim();
                    }
                }
                catch { }

                return StripRichText(bubble != null && bubble.NameText != null ? bubble.NameText.text : string.Empty).Trim();
            }

            private static string StripRichText(string value)
            {
                if (string.IsNullOrEmpty(value)) return string.Empty;
                return Regex.Replace(value, "<[^>]*>", string.Empty);
            }

            private static void ResetMessageClick()
            {
                lastMessageBubbleId = -1;
                lastMessageClickAt = -10f;
                lastMessageText = string.Empty;
            }
        }

[HarmonyPatch(typeof(TextBoxTMP), nameof(TextBoxTMP.Update))]
        public static class AllowSymbols_TextBoxTMP_Update_Patch
        {
            public static void Postfix(TextBoxTMP __instance)
            {
                if (__instance == null) return;
                __instance.allowAllCharacters = ElysiumModMenuGUI.allowLinksAndSymbols;
                __instance.AllowSymbols = ElysiumModMenuGUI.allowLinksAndSymbols;
                __instance.AllowEmail = ElysiumModMenuGUI.allowLinksAndSymbols;
            }
        }

[HarmonyPatch(typeof(TextBoxTMP), nameof(TextBoxTMP.Update))]
        public static class Clipboard_TextBoxTMP_Patch
        {
            public static void Postfix(TextBoxTMP __instance)
            {
                ClipboardBridge.Run(__instance);
            }
        }

[HarmonyPatch(typeof(TextBoxTMP), nameof(TextBoxTMP.IsCharAllowed))]
        public static class Clipboard_TextBoxTMP_IsCharAllowed_Patch
        {
            public static bool Prefix(TextBoxTMP __instance, ref bool __result)
            {
                return ClipboardBridge.IsCharAllowed(__instance, ref __result);
            }
        }

[HarmonyPatch(typeof(ChatController), nameof(ChatController.Update))]
        public static class ChatHistory_Update_Patch
        {
            public static void Postfix(ChatController __instance)
            {
                if (__instance != null && __instance.freeChatField != null && __instance.freeChatField.textArea != null)
                {
                    ClipboardBridge.Run(__instance.freeChatField.textArea);
                }
                ChatHistory.HandleNavigation(__instance);
                ChatBubbleCopyInteraction.HandleClick(__instance);
            }
        }

public static bool enableExtendedChat = true;

public static bool enableChatHistory = true;

public static bool enableClipboard = true;

public static bool enableChatMessageDoubleClickCopy = true;

public static bool enableChatNameColorCopy = true;

public static bool AnimEmptyGarbageEnabled = false;

public static bool skipShhhAnim = false;

public static bool isManualMapSpawn = false;

private void DrawAnimationsTab()
        {
            GUILayout.BeginVertical(menuCardStyle);

            DrawMenuSectionHeader(L("LOOPED PLAYER ANIMATIONS", "ЗАЦИКЛЕННЫЕ АНИМАЦИИ ИГРОКА"));

            string animInfo = L("<color=#777777>Animations are looped. They will run as long as the toggle is ON.</color>",
                                "<color=#777777>Анимации зациклены. Будут работать, пока включен тумблер.</color>");
            GUILayout.Label(animInfo, new GUIStyle(GUI.skin.label) { richText = true, fontSize = 11, wordWrap = true });

            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            AnimAsteroidsEnabled = DrawToggle(AnimAsteroidsEnabled, L("Weapons (Asteroids)", "Оружие (Астероиды)"), 250);
            IsScanning = DrawToggle(IsScanning, L("Medbay Scan", "Скан в медпункте"), 250);
            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            AnimShieldsEnabled = DrawToggle(AnimShieldsEnabled, L("Turn On Shields", "Включить щиты"), 250);
            AnimCamsInUseEnabled = DrawToggle(AnimCamsInUseEnabled, L("Use Cameras (Blink Red)", "Камеры (Красный индикатор)"), 250);
            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            AnimEmptyGarbageEnabled = DrawToggle(AnimEmptyGarbageEnabled, L("Empty Garbage", "Выкинуть мусор"), 250);
            skipShhhAnim = DrawToggle(skipShhhAnim, L("Skip 'Shhh!' Intro", "Пропустить 'Shhh!' интро"), 250);
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

        }

public static string GetPlatform(ClientData client)
        {
            if (client == null || client.PlatformData == null) return "Unknown";

            int platformId = (int)client.PlatformData.Platform;

            switch (platformId)
            {
                case 1: return "Epic";
                case 2: return "Steam";
                case 3: return "Mac";
                case 4: return "Microsoft";
                case 5: return "Itch";
                case 6: return "iOS";
                case 7: return "Android";
                case 8: return "Switch";
                case 9: return "Xbox";
                case 10: return "PlayStation";
                case 112: return "Starlight";
                default: return $"Unknown ({platformId})";
            }
        }
}
}
