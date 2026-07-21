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



internal static class DarkChatTheme
{
    private struct SpriteState
    {
        public SpriteRenderer sr;
        public Color color;
        public Color darkColor;
        public Sprite sprite;
        public Sprite darkSprite;
    }

    private struct TextState
    {
        public TMP_Text txt;
        public Color color;
        public Color darkColor;
    }

    private static readonly List<SpriteState> spriteStates = new List<SpriteState>(24);
    private static readonly List<TextState> textStates = new List<TextState>(12);

    private static ChatController chat;
    private static bool applied;
    private static bool iconSpritesLoaded;

    private static Sprite quickIconSprite;
    private static Sprite reportIconSprite;
    private static Sprite keyboardIconSprite;

    private static readonly Color panelColor = new Color32(18, 17, 23, 248);
    private static readonly Color fieldColor = new Color32(32, 30, 39, 255);
    private static readonly Color buttonColor = new Color32(48, 43, 58, 255);
    private static readonly Color iconColor = new Color32(225, 216, 238, 255);
    private static readonly Color textColor = new Color32(241, 237, 246, 255);
    private static readonly Color mutedTextColor = new Color32(178, 170, 190, 255);

    public static void Tick(ChatController instance)
    {
        if (instance == null) return;

        if (chat != instance)
        {
            Restore();
            Cache(instance);
        }

        if (!ElysiumModMenuGUI.enableChatDarkMode)
        {
            if (applied) RestoreColors();
            return;
        }

        if (!applied)
        {
            ApplyColors();
            return;
        }

        PaintColors();
    }

    private static void Cache(ChatController instance)
    {
        chat = instance;

        AddSprite(instance.backgroundImage, panelColor);

        if (instance.freeChatField != null)
        {
            AddSprite(instance.freeChatField.background, fieldColor);

            if (instance.freeChatField.textArea != null)
            {
                AddSprite(instance.freeChatField.textArea.Background, fieldColor);
                AddText(instance.freeChatField.textArea.outputText, textColor);
                AddText(instance.freeChatField.textArea.placeholderText, mutedTextColor);
            }

            AddText(instance.freeChatField.charCountText, mutedTextColor);
            AddInputButton(instance.freeChatField.submitButton);
        }

        if (instance.quickChatField != null)
        {
            AddSprite(instance.quickChatField.background, fieldColor);
            AddText(instance.quickChatField.text, textColor);
            AddText(instance.quickChatField.placeholderText, mutedTextColor);
            AddText(instance.quickChatField.warningText, mutedTextColor);
            AddInputButton(instance.quickChatField.submitButton);
            AddInputButton(instance.quickChatField.clearButton);
            AddInputButton(instance.quickChatField.undoButton);
        }

        CacheIcons(instance);
    }

    private static void AddInputButton(ChatInputFieldButton btn)
    {
        if (btn == null) return;

        if (btn.backgroundSprites != null)
        {
            foreach (SpriteRenderer sr in btn.backgroundSprites)
            {
                AddSprite(sr, buttonColor);
            }
        }

        if (btn.iconSprites != null)
        {
            foreach (SpriteRenderer sr in btn.iconSprites)
            {
                AddSprite(sr, iconColor);
            }
        }

        AddText(btn.text, textColor);
    }

    private static void AddSprite(SpriteRenderer sr, Color darkColor, Sprite darkSprite = null)
    {
        if (sr == null) return;

        for (int i = 0; i < spriteStates.Count; i++)
        {
            if (spriteStates[i].sr == sr) return;
        }

        spriteStates.Add(new SpriteState
        {
            sr = sr,
            color = sr.color,
            darkColor = darkColor,
            sprite = sr.sprite,
            darkSprite = darkSprite
        });
    }

    private static void AddText(TMP_Text txt, Color darkColor)
    {
        if (txt == null) return;

        for (int i = 0; i < textStates.Count; i++)
        {
            if (textStates[i].txt == txt) return;
        }

        textStates.Add(new TextState
        {
            txt = txt,
            color = txt.color,
            darkColor = darkColor
        });
    }

    private static void CacheIcons(ChatController instance)
    {
        SpriteRenderer quick = FindIcon(instance.quickChatButton != null ? instance.quickChatButton.gameObject : null, "QuickChatIcon");
        SpriteRenderer report = FindIcon(instance.banButton != null && instance.banButton.MenuButton != null ? instance.banButton.MenuButton.gameObject : null, "OpenBanMenuIcon");
        SpriteRenderer keyboard = FindIcon(instance.openKeyboardButton, "OpenKeyboardIcon");

        if (quick == null && report == null && keyboard == null) return;

        LoadIconSprites();
        AddSprite(quick, Color.white, quickIconSprite);
        AddSprite(report, Color.white, reportIconSprite);
        AddSprite(keyboard, Color.white, keyboardIconSprite);
    }

    private static SpriteRenderer FindIcon(GameObject root, string name)
    {
        if (root == null) return null;

        SpriteRenderer own = root.GetComponent<SpriteRenderer>();
        if (own != null && root.name == name) return own;

        SpriteRenderer[] sprites = root.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < sprites.Length; i++)
        {
            SpriteRenderer sr = sprites[i];
            if (sr != null && sr.gameObject.name == name) return sr;
        }

        return null;
    }

    private static void LoadIconSprites()
    {
        if (iconSpritesLoaded) return;
        iconSpritesLoaded = true;

        quickIconSprite = LoadSprite("ElysiumModMenu.dark_chat_quick.png");
        reportIconSprite = LoadSprite("ElysiumModMenu.dark_chat_report.png");
        keyboardIconSprite = LoadSprite("ElysiumModMenu.dark_chat_keyboard.png");
    }

    private static Sprite LoadSprite(string path)
    {
        try
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            using System.IO.Stream stream = asm.GetManifestResourceStream(path);
            if (stream == null || stream.Length <= 0 || stream.Length > int.MaxValue) return null;

            byte[] bytes = new byte[(int)stream.Length];
            int offset = 0;
            while (offset < bytes.Length)
            {
                int read = stream.Read(bytes, offset, bytes.Length - offset);
                if (read <= 0) break;
                offset += read;
            }
            if (offset != bytes.Length) return null;

            Texture2D tex = new Texture2D(2, 2, TextureFormat.ARGB32, false);
            if (!ImageConversion.LoadImage(tex, bytes, true))
            {
                Object.Destroy(tex);
                return null;
            }

            tex.hideFlags |= HideFlags.HideAndDontSave | HideFlags.DontSaveInEditor;
            Sprite spr = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            spr.hideFlags |= HideFlags.HideAndDontSave | HideFlags.DontSaveInEditor;
            return spr;
        }
        catch
        {
            return null;
        }
    }

    private static void ApplyColors()
    {
        PaintColors();
        UpdateBubbles(true);
        applied = true;
    }

    private static void PaintColors()
    {
        for (int i = 0; i < spriteStates.Count; i++)
        {
            SpriteState state = spriteStates[i];
            if (state.sr == null) continue;
            state.sr.color = state.darkColor;
            if (state.darkSprite != null) state.sr.sprite = state.darkSprite;
        }

        for (int i = 0; i < textStates.Count; i++)
        {
            TextState state = textStates[i];
            if (state.txt != null) state.txt.color = state.darkColor;
        }
    }

    private static void RestoreColors()
    {
        for (int i = 0; i < spriteStates.Count; i++)
        {
            SpriteState state = spriteStates[i];
            if (state.sr == null) continue;
            state.sr.color = state.color;
            state.sr.sprite = state.sprite;
        }

        for (int i = 0; i < textStates.Count; i++)
        {
            TextState state = textStates[i];
            if (state.txt != null) state.txt.color = state.color;
        }

        UpdateBubbles(false);
        applied = false;
    }

    private static void UpdateBubbles(bool dark)
    {
        if (chat == null) return;

        ChatBubble[] bubbles = chat.GetComponentsInChildren<ChatBubble>(false);
        for (int i = 0; i < bubbles.Length; i++)
        {
            ApplyBubble(bubbles[i], dark);
        }
    }

    public static void ApplyBubble(ChatBubble bubble, bool dark)
    {
        if (bubble == null) return;
        if (bubble.Background != null)
            bubble.Background.color = dark ? new Color32(27, 25, 33, 250) : Color.white;
        if (bubble.TextArea != null)
            bubble.TextArea.color = dark ? textColor : Color.black;
    }

    private static void Restore()
    {
        if (applied) RestoreColors();

        spriteStates.Clear();
        textStates.Clear();
        chat = null;
    }
}

[HarmonyPatch(typeof(ChatController), nameof(ChatController.Update))]
public static class ChatController_Update_Patch
{
    public static void Postfix(ChatController __instance)
    {
        try
        {
            DarkChatTheme.Tick(__instance);
        }
        catch { }
    }
}
