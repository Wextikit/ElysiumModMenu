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
                if (!enableChatHistory) return;
                if (string.IsNullOrWhiteSpace(message)) return;
                ElysiumModMenuGUI.chatHistoryLimit = Mathf.Clamp(ElysiumModMenuGUI.chatHistoryLimit, 5, 80);
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
                if (!enableChatHistory) return;
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

public static class ChatBubbleCopyHandler
        {
            private const float DoubleClickWindow = 0.38f;
            private const float CopyDedupWindow = 2f;

            private static float lastClickAt = -10f;
            private static string lastClickKey = string.Empty;
            private static float lastCopyAt = -10f;
            private static string lastCopiedKey = string.Empty;

            public static void Check(ChatController chat)
            {
                if ((!enableChatBubbleCopy && !enableChatNickCopy) || chat == null) return;
                if (!Input.GetMouseButtonDown(0)) return;

                try
                {
                    Camera cam = Camera.main;
                    if (cam == null) return;

                    Vector2 mouseScreen = Input.mousePosition;
                    ChatBubble[] bubbles = ((Component)chat).GetComponentsInChildren<ChatBubble>(false);
                    for (int i = bubbles.Length - 1; i >= 0; i--)
                    {
                        ChatBubble bubble = bubbles[i];
                        if (bubble == null) continue;

                        if (enableChatNickCopy && HitsNickArea(bubble, cam, mouseScreen))
                        {
                            string sender = ReadBubbleSenderName(bubble);
                            if (TryCopy(sender, "nick")) return;
                        }

                        if (enableChatBubbleCopy && HitsBubble(bubble, cam, mouseScreen))
                        {
                            string message = ReadBubbleText(bubble);
                            if (TryCopy(message, "message")) return;
                        }
                    }
                }
                catch { }
            }

            private static bool TryCopy(string copyText, string kind)
            {
                if (string.IsNullOrWhiteSpace(copyText)) return false;

                float now = Time.unscaledTime;
                string copyKey = $"{kind}:{copyText}";
                bool isDoubleClick = copyKey == lastClickKey && now - lastClickAt <= DoubleClickWindow;
                lastClickAt = now;
                lastClickKey = copyKey;

                if (!isDoubleClick) return true;
                if (copyKey == lastCopiedKey && now - lastCopyAt < CopyDedupWindow) return true;

                GUIUtility.systemCopyBuffer = copyText;
                lastCopyAt = now;
                lastCopiedKey = copyKey;
                lastClickAt = -10f;
                ShowNotification("<color=#66CCFF>[CHAT]</color> Copied");
                return true;
            }

            private static bool HitsBubble(ChatBubble bubble, Camera cam, Vector2 mouseScreen)
            {
                try
                {
                    Transform bg = ((Component)bubble).transform.Find("Background");
                    if (bg == null) return false;
                    SpriteRenderer sr = ((Component)bg).GetComponent<SpriteRenderer>();
                    if (sr == null) return false;

                    Bounds bounds = sr.bounds;
                    if (bounds.size.sqrMagnitude < 0.001f) return false;

                    Vector3 smin = cam.WorldToScreenPoint(bounds.min);
                    Vector3 smax = cam.WorldToScreenPoint(bounds.max);
                    float x0 = Mathf.Min(smin.x, smax.x);
                    float x1 = Mathf.Max(smin.x, smax.x);
                    float y0 = Mathf.Min(smin.y, smax.y);
                    float y1 = Mathf.Max(smin.y, smax.y);
                    return mouseScreen.x >= x0 && mouseScreen.x <= x1
                        && mouseScreen.y >= y0 && mouseScreen.y <= y1;
                }
                catch { return false; }
            }

            private static bool TryGetBubbleScreenRect(ChatBubble bubble, Camera cam, out Rect rect)
            {
                rect = default;
                try
                {
                    Transform bg = ((Component)bubble).transform.Find("Background");
                    if (bg == null) return false;
                    SpriteRenderer sr = ((Component)bg).GetComponent<SpriteRenderer>();
                    if (sr == null) return false;

                    Bounds bounds = sr.bounds;
                    if (bounds.size.sqrMagnitude < 0.001f) return false;

                    Vector3 smin = cam.WorldToScreenPoint(bounds.min);
                    Vector3 smax = cam.WorldToScreenPoint(bounds.max);
                    float x0 = Mathf.Min(smin.x, smax.x);
                    float x1 = Mathf.Max(smin.x, smax.x);
                    float y0 = Mathf.Min(smin.y, smax.y);
                    float y1 = Mathf.Max(smin.y, smax.y);
                    rect = Rect.MinMaxRect(x0, y0, x1, y1);
                    return rect.width > 1f && rect.height > 1f;
                }
                catch { return false; }
            }

            private static bool HitsNickArea(ChatBubble bubble, Camera cam, Vector2 mouseScreen)
            {
                if (HitsText(bubble.NameText, cam, mouseScreen)) return true;

                if (!TryGetBubbleScreenRect(bubble, cam, out Rect rect)) return false;
                if (!rect.Contains(mouseScreen)) return false;

                float nameBandHeight = Mathf.Clamp(rect.height * 0.34f, 16f, 34f);
                return mouseScreen.y >= rect.yMax - nameBandHeight;
            }

            private static bool HitsText(TMP_Text text, Camera cam, Vector2 mouseScreen)
            {
                try
                {
                    if (text == null) return false;
                    Renderer renderer = ((Component)text).GetComponent<Renderer>();
                    if (renderer == null) return false;

                    Bounds bounds = renderer.bounds;
                    if (bounds.size.sqrMagnitude < 0.001f) return false;

                    Vector3 smin = cam.WorldToScreenPoint(bounds.min);
                    Vector3 smax = cam.WorldToScreenPoint(bounds.max);
                    float x0 = Mathf.Min(smin.x, smax.x);
                    float x1 = Mathf.Max(smin.x, smax.x);
                    float y0 = Mathf.Min(smin.y, smax.y);
                    float y1 = Mathf.Max(smin.y, smax.y);
                    x0 -= 8f;
                    x1 += 8f;
                    y0 -= 5f;
                    y1 += 5f;
                    return mouseScreen.x >= x0 && mouseScreen.x <= x1
                        && mouseScreen.y >= y0 && mouseScreen.y <= y1;
                }
                catch { return false; }
            }

            private static string ReadBubbleText(ChatBubble bubble)
            {
                try
                {
                    if (bubble.TextArea == null) return string.Empty;
                    return (((TMP_Text)bubble.TextArea).text ?? string.Empty).Trim();
                }
                catch { return string.Empty; }
            }

            private static string ReadBubbleSenderName(ChatBubble bubble)
            {
                try
                {
                    if (bubble.playerInfo != null && !string.IsNullOrWhiteSpace(bubble.playerInfo.PlayerName))
                        return CleanCopiedChatText(bubble.playerInfo.PlayerName);
                }
                catch { }

                try
                {
                    if (bubble.NameText == null) return string.Empty;
                    string text = CleanCopiedChatText(((TMP_Text)bubble.NameText).text);
                    int newline = text.IndexOf('\n');
                    return newline >= 0 ? text.Substring(0, newline).Trim() : text;
                }
                catch { return string.Empty; }
            }

            private static string CleanCopiedChatText(string text)
            {
                if (string.IsNullOrEmpty(text)) return string.Empty;
                string cleaned = Regex.Replace(text, "<.*?>", string.Empty);
                cleaned = cleaned.Replace("\r", " ").Replace("\n", " ");
                return Regex.Replace(cleaned, @"\s+", " ").Trim();
            }
        }

public static class ClipboardBridge
        {
            private static bool isPastingChatInput = false;
            private static bool allTextSelected = false;
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
                bool selectAllPressed = controlHeld && Input.GetKeyDown(KeyCode.A);

                if (!copyPressed && !pastePressed && !cutPressed && !selectAllPressed) return;
                if (lastClipboardFrame == Time.frameCount) return;
                lastClipboardFrame = Time.frameCount;

                if (selectAllPressed)
                {
                    allTextSelected = true;
                }
                else if (copyPressed)
                {
                    GUIUtility.systemCopyBuffer = box.text ?? string.Empty;
                }
                else if (pastePressed)
                {
                    string paste = GUIUtility.systemCopyBuffer;
                    if (!string.IsNullOrEmpty(paste))
                    {
                        string currentText = box.text ?? string.Empty;
                        string nextText;
                        if (allTextSelected)
                        {
                            nextText = paste;
                            allTextSelected = false;
                        }
                        else
                        {
                            int caretPos = Mathf.Clamp(box.caretPos, 0, currentText.Length);
                            nextText = currentText.Insert(caretPos, paste);
                        }

                        isPastingChatInput = true;
                        box.SetText(nextText, string.Empty);
                        isPastingChatInput = false;
                    }
                }
                else if (cutPressed)
                {
                    GUIUtility.systemCopyBuffer = box.text ?? string.Empty;
                    box.SetText(string.Empty, string.Empty);
                    allTextSelected = false;
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
                string text = allTextSelected ? input : currentText.Insert(caretPos, input);
                if (allTextSelected)
                {
                    box.SetText(string.Empty, string.Empty);
                    allTextSelected = false;
                }

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
                ChatBubbleCopyHandler.Check(__instance);
            }
        }

public static bool enableExtendedChat = true;

public static bool enableChatHistory = true;

public static bool enableClipboard = true;

public static bool enableChatBubbleCopy = true;

public static bool enableChatNickCopy = false;

public static bool AnimEmptyGarbageEnabled = false;

public static bool skipShhhAnim = false;

public static bool skipRoleIntroAnim = false;

public static bool skipKillAnimation = false;

public static bool isManualMapSpawn = false;

private static bool manualMapSpawnInProgress = false;

private static int manualSpawnedMapId = -1;

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

            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            skipRoleIntroAnim = DrawToggle(skipRoleIntroAnim, L("Skip Role Intro", "Пропустить выдачу ролей"), 250);
            skipKillAnimation = DrawToggle(skipKillAnimation, L("Skip Kill Animation", "Пропустить анимацию килла"), 250);
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
