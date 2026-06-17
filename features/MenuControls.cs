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

public static Color GetMenuAccentColor(bool allowRgbText = true)
        {
            return allowRgbText && RgbMenuTextActive()
                ? GetThemeAccentColor(currentAccentColor)
                : GetThemeAccentColor(GetStableMenuAccentSource());
        }

        public static string GetMenuAccentHex(bool allowRgbText = true)
        {
            return ColorUtility.ToHtmlStringRGB(GetMenuAccentColor(allowRgbText));
        }

        public static Color GetMenuControlAccentColor()
        {
            return rgbMenuMode
                ? GetThemeAccentColor(currentAccentColor)
                : GetThemeAccentColor(GetStableMenuAccentSource());
        }

        public static string GetMenuControlAccentHex()
        {
            return ColorUtility.ToHtmlStringRGB(GetMenuControlAccentColor());
        }

        private Color GetStableControlAccentColor(Color fallback)
        {
            Color source = fallback;
            try
            {
                if (rgbMenuMode)
                    source = GetStableMenuAccentSource();
            }
            catch { }

            return GetThemeAccentColor(source);
        }

        private void UpdateAccentColor(Color color)
        {
            currentAccentColor = color;
            Color effectiveColor = GetThemeAccentColor(color);
            Color controlColor = rgbMenuMode ? effectiveColor : GetStableControlAccentColor(color);
            if (texAccent != null)
            {
                int size = texAccent.width;
                Color[] pix = new Color[size * size];
                float center = size / 2f;
                float radius = 6f;
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dx = Mathf.Max(0, Mathf.Abs(x - center + 0.5f) - (center - radius));
                        float dy = Mathf.Max(0, Mathf.Abs(y - center + 0.5f) - (center - radius));
                        float dist = Mathf.Sqrt(dx * dx + dy * dy);
                        float alpha = Mathf.Clamp01(radius - dist + 0.5f);
                        Color c = controlColor; c.a = alpha;
                        pix[y * size + x] = c;
                    }
                }
                texAccent.SetPixels(pix); texAccent.Apply();
            }
            if (texSliderThumb != null)
            {
                int size = texSliderThumb.width;
                Color[] pix = new Color[size * size];
                float center = size / 2f;
                float radius = 10f;
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dx = Mathf.Max(0, Mathf.Abs(x - center + 0.5f) - (center - radius));
                        float dy = Mathf.Max(0, Mathf.Abs(y - center + 0.5f) - (center - radius));
                        float dist = Mathf.Sqrt(dx * dx + dy * dy);
                        float alpha = Mathf.Clamp01(radius - dist + 0.5f);
                        Color c = controlColor; c.a = alpha;
                        pix[y * size + x] = c;
                    }
                }
                texSliderThumb.SetPixels(pix); texSliderThumb.Apply();
            }
            if (texScrollThumb != null)
            {
                int size = texScrollThumb.width;
                Color[] pix = new Color[size * size];
                float center = size / 2f;
                float radius = 4f;
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dx = Mathf.Max(0, Mathf.Abs(x - center + 0.5f) - (center - radius));
                        float dy = Mathf.Max(0, Mathf.Abs(y - center + 0.5f) - (center - radius));
                        float dist = Mathf.Sqrt(dx * dx + dy * dy);
                        float alpha = Mathf.Clamp01(radius - dist + 0.5f);
                        Color c = controlColor; c.a = alpha;
                        pix[y * size + x] = c;
                    }
                }
                texScrollThumb.SetPixels(pix); texScrollThumb.Apply();
            }
            if (texToggleOn != null) UpdateSwitchTex(texToggleOn, true, controlColor);
            if (texTrackOn != null) UpdateTrackTex(texTrackOn, true, controlColor);
            bool rgbText = RgbMenuTextActive();
            Color menuHeadingText = rgbText ? effectiveColor : (whiteMenuTheme ? new Color(0.15f, 0.15f, 0.15f, 1f) : color);
            if (windowStyle != null) windowStyle.normal.textColor = rgbText ? effectiveColor : (whiteMenuTheme ? new Color(0.16f, 0.16f, 0.16f, 1f) : color);
            if (headerStyle != null) headerStyle.normal.textColor = menuHeadingText;
            if (menuSectionTitleStyle != null) menuSectionTitleStyle.normal.textColor = menuHeadingText;
            if (menuBadgeStyle != null) menuBadgeStyle.normal.textColor = menuHeadingText;
            if (activeSidebarBtnStyle != null) { activeSidebarBtnStyle.normal.textColor = effectiveColor; activeSidebarBtnStyle.hover.textColor = effectiveColor; }
            if (activeTabStyle != null) activeTabStyle.normal.background = texAccent;
            if (activeSubTabStyle != null) activeSubTabStyle.normal.background = texAccent;
            if (btnStyle != null) btnStyle.active.background = texAccent;
            if (inputBlockStyle != null) inputBlockStyle.normal.textColor = rgbText ? effectiveColor : (whiteMenuTheme ? new Color(0.15f, 0.15f, 0.15f, 1f) : color);
        }

        private void InitStyles()
        {
            bool isLightTheme = whiteMenuTheme;
            FontStyle menuTextStyle = boldMenuText ? FontStyle.Bold : FontStyle.Normal;
            Color accent = GetMenuAccentColor();
            Color darkBg = isLightTheme ? new Color(0.97f, 0.97f, 0.97f, 0.78f) : new Color(0.12f, 0.12f, 0.12f, 0.90f);
            Color sidebarBg = new Color(0.0f, 0.0f, 0.0f, 0.0f);
            Color boxBg = new Color(0f, 0f, 0f, 0f);
            Color btnCol = isLightTheme ? new Color32(252, 247, 240, 255) : new Color(0.23f, 0.23f, 0.23f, 1f);
            Color sliderBgCol = isLightTheme ? new Color(0.78f, 0.78f, 0.78f, 0.68f) : new Color(0.08f, 0.08f, 0.08f, 1f);
            Color textMain = isLightTheme ? new Color(0.18f, 0.18f, 0.18f, 1f) : new Color(0.78f, 0.78f, 0.78f, 1f);
            Color textMuted = isLightTheme ? new Color(0.33f, 0.33f, 0.33f, 1f) : new Color(0.6f, 0.6f, 0.6f, 1f);
            Color textHover = isLightTheme ? new Color(0.06f, 0.06f, 0.06f, 1f) : Color.white;
            Color headerText = RgbMenuTextActive() ? accent : (isLightTheme ? new Color(0.15f, 0.15f, 0.15f, 1f) : accent);
            Color inputBgCol = isLightTheme ? new Color(1f, 1f, 1f, 0.86f) : new Color(0.08f, 0.08f, 0.08f, 0.85f);

            texWindowBg = MakeRoundedTex(64, darkBg, 12f);
            texSidebarBg = MakeRoundedTex(64, sidebarBg, 0f);
            texBoxBg = MakeRoundedTex(64, boxBg, 0f);
            texBtnBg = MakeRoundedTex(64, btnCol, 6f);
            texAccent = MakeRoundedTex(64, accent, 6f);
            texSliderBg = MakeRoundedTex(64, sliderBgCol, 4f);
            texSliderThumb = MakeRoundedTex(20, accent, 10f);
            texInputBg = MakeRoundedTex(64, inputBgCol, 6f);
            texColorBtn = MakeRoundedTex(64, Color.white, 12f);

            texMenuCard = MakeRoundedTex(64, isLightTheme ? new Color(1f, 1f, 1f, 0.55f) : new Color(1f, 1f, 1f, 0.045f), 12f);

            menuCardStyle = new GUIStyle();
            menuCardStyle.normal.background = texMenuCard;
            menuCardStyle.border = CreateRectOffset(12, 12, 12, 12);
            menuCardStyle.padding = CreateRectOffset(14, 14, 12, 14);
            menuCardStyle.margin = CreateRectOffset(0, 0, 0, 10);

            menuAccentBarStyle = new GUIStyle();
            menuAccentBarStyle.normal.background = texAccent;

            menuSectionTitleStyle = new GUIStyle();
            menuSectionTitleStyle.normal.textColor = headerText;
            menuSectionTitleStyle.fontStyle = menuTextStyle;
            menuSectionTitleStyle.fontSize = 13;
            menuSectionTitleStyle.alignment = TextAnchor.MiddleLeft;
            menuSectionTitleStyle.richText = true;

            menuDescStyle = new GUIStyle();
            menuDescStyle.normal.textColor = textMuted;
            menuDescStyle.fontSize = 11;
            menuDescStyle.fontStyle = menuTextStyle;
            menuDescStyle.richText = true;
            menuDescStyle.wordWrap = true;
            menuDescStyle.padding = CreateRectOffset(2, 0, 2, 0);

            menuBadgeStyle = new GUIStyle();
            menuBadgeStyle.normal.background = texInputBg;
            menuBadgeStyle.normal.textColor = headerText;
            menuBadgeStyle.fontStyle = menuTextStyle;
            menuBadgeStyle.fontSize = 12;
            menuBadgeStyle.alignment = TextAnchor.MiddleCenter;
            menuBadgeStyle.border = CreateRectOffset(6, 6, 6, 6);
            menuBadgeStyle.padding = CreateRectOffset(8, 8, 3, 3);

            menuSwatchStyle = new GUIStyle();
            menuSwatchStyle.normal.background = texColorBtn;
            menuSwatchStyle.border = CreateRectOffset(8, 8, 8, 8);

            texSwatchSquare = MakeRoundedTex(32, Color.white, 6f);
            menuSwatchSquareStyle = new GUIStyle();
            menuSwatchSquareStyle.normal.background = texSwatchSquare;
            menuSwatchSquareStyle.border = CreateRectOffset(6, 6, 6, 6);

            texToggleOff = new Texture2D(30, 16, TextureFormat.RGBA32, false);
            texToggleOff.hideFlags = HideFlags.HideAndDontSave;
            texToggleOn = new Texture2D(30, 16, TextureFormat.RGBA32, false);
            texToggleOn.hideFlags = HideFlags.HideAndDontSave;
            UpdateSwitchTex(texToggleOff, false, Color.white);
            UpdateSwitchTex(texToggleOn, true, accent);
            texTrackOff = new Texture2D(30, 16, TextureFormat.RGBA32, false);
            texTrackOff.hideFlags = HideFlags.HideAndDontSave;
            texTrackOn = new Texture2D(30, 16, TextureFormat.RGBA32, false);
            texTrackOn.hideFlags = HideFlags.HideAndDontSave;
            UpdateTrackTex(texTrackOff, false, Color.white);
            UpdateTrackTex(texTrackOn, true, accent);
            texKnobWhite = MakeRoundedTex(16, Color.white, 8f);

            safeLineStyle = new GUIStyle();
            safeLineStyle.normal.background = MakeRoundedTex(2, isLightTheme ? new Color(0.75f, 0.75f, 0.75f, 1f) : Color.white, 0f);

            windowStyle = new GUIStyle();
            windowStyle.normal.background = texWindowBg;
            windowStyle.normal.textColor = accent;
            windowStyle.fontStyle = menuTextStyle;
            windowStyle.fontSize = 14;
            windowStyle.padding = CreateRectOffset(0, 0, 0, 0);
            windowStyle.border = CreateRectOffset(12, 12, 12, 12);

            boxStyle = new GUIStyle();
            boxStyle.normal.background = texBoxBg;
            boxStyle.padding = CreateRectOffset(0, 0, 0, 0);
            boxStyle.margin = CreateRectOffset(0, 0, 4, 8);

            btnStyle = new GUIStyle(GUI.skin.button);
            btnStyle.normal.background = texBtnBg;
            btnStyle.hover.background = texBtnBg;
            btnStyle.normal.textColor = textMain;
            btnStyle.hover.textColor = textHover;
            btnStyle.active.background = texAccent;
            btnStyle.active.textColor = Color.black;
            btnStyle.alignment = TextAnchor.MiddleCenter;
            btnStyle.border = CreateRectOffset(6, 6, 6, 6);
            btnStyle.fontSize = 12;
            btnStyle.fontStyle = menuTextStyle;
            btnStyle.clipping = TextClipping.Overflow;
            btnStyle.wordWrap = false;

            activeTabStyle = new GUIStyle(btnStyle);
            activeTabStyle.normal.background = texAccent;
            activeTabStyle.normal.textColor = Color.black;

            subTabStyle = new GUIStyle(btnStyle);
            subTabStyle.padding = CreateRectOffset(8, 8, 2, 2);
            subTabStyle.clipping = TextClipping.Overflow;
            subTabStyle.wordWrap = false;
            activeSubTabStyle = new GUIStyle(activeTabStyle);
            activeSubTabStyle.padding = CreateRectOffset(8, 8, 2, 2);
            activeSubTabStyle.clipping = TextClipping.Overflow;
            activeSubTabStyle.wordWrap = false;

            inputBlockStyle = new GUIStyle(btnStyle);
            inputBlockStyle.normal.background = texInputBg;
            inputBlockStyle.hover.background = texInputBg;
            inputBlockStyle.active.background = texAccent;
            inputBlockStyle.normal.textColor = isLightTheme ? new Color(0.15f, 0.15f, 0.15f, 1f) : accent;
            inputBlockStyle.alignment = TextAnchor.MiddleCenter;
            inputBlockStyle.fontStyle = menuTextStyle;

            headerStyle = new GUIStyle();
            headerStyle.normal.background = texBtnBg;
            headerStyle.normal.textColor = headerText;
            headerStyle.fontStyle = menuTextStyle;
            headerStyle.alignment = TextAnchor.MiddleLeft;
            headerStyle.padding = CreateRectOffset(6, 6, 4, 4);
            headerStyle.margin = CreateRectOffset(0, 0, 4, 4);
            headerStyle.fontSize = 13;
            headerStyle.clipping = TextClipping.Overflow;
            headerStyle.wordWrap = false;

            sidebarStyle = new GUIStyle();
            sidebarStyle.normal.background = texSidebarBg;
            sidebarStyle.padding = CreateRectOffset(0, 0, 5, 0);

            sidebarBtnStyle = new GUIStyle();
            sidebarBtnStyle.normal.textColor = textMuted;
            sidebarBtnStyle.hover.textColor = textHover;
            sidebarBtnStyle.padding = CreateRectOffset(12, 0, 6, 6);
            sidebarBtnStyle.alignment = TextAnchor.MiddleLeft;
            sidebarBtnStyle.fontSize = 13;
            sidebarBtnStyle.fontStyle = menuTextStyle;

            activeSidebarBtnStyle = new GUIStyle(sidebarBtnStyle);
            activeSidebarBtnStyle.normal.textColor = accent;
            activeSidebarBtnStyle.hover.textColor = accent;

            toggleOffStyle = new GUIStyle();
            toggleOffStyle.normal.background = texToggleOff;
            toggleOnStyle = new GUIStyle();
            toggleOnStyle.normal.background = texToggleOn;
            trackOffStyle = new GUIStyle();
            trackOffStyle.normal.background = texTrackOff;
            trackOnStyle = new GUIStyle();
            trackOnStyle.normal.background = texTrackOn;
            knobStyle = new GUIStyle();
            knobStyle.normal.background = texKnobWhite;

            toggleLabelStyle = new GUIStyle();
            toggleLabelStyle.normal.textColor = textMain;
            toggleLabelStyle.alignment = TextAnchor.MiddleLeft;
            toggleLabelStyle.padding = CreateRectOffset(4, 0, 0, 0);
            toggleLabelStyle.fontSize = 12;
            toggleLabelStyle.fontStyle = menuTextStyle;
            toggleLabelStyle.clipping = TextClipping.Overflow;
            toggleLabelStyle.wordWrap = false;
            toggleLabelStyle.richText = true;

            sliderStyle = new GUIStyle();
            sliderStyle.normal.background = texSliderBg;
            sliderStyle.border = CreateRectOffset(6, 6, 6, 6);
            sliderStyle.fixedHeight = 10f;
            sliderStyle.margin = CreateRectOffset(0, 0, 8, 8);

            sliderThumbStyle = new GUIStyle();
            sliderThumbStyle.normal.background = texSliderThumb;
            sliderThumbStyle.fixedWidth = 18f;
            sliderThumbStyle.fixedHeight = 18f;
            sliderThumbStyle.margin = CreateRectOffset(0, 0, -4, 0);

            titleStyle = new GUIStyle();
            titleStyle.normal.textColor = accent;
            titleStyle.fontStyle = menuTextStyle;
            titleStyle.fontSize = 14;
            titleStyle.richText = true;
            titleStyle.padding = CreateRectOffset(10, 0, 8, 0);

            Texture2D texScrollBg = MakeRoundedTex(8, new Color(0.1f, 0.1f, 0.1f, 0.2f), 4f);
            texScrollThumb = MakeRoundedTex(8, accent, 4f);

            GUIStyle scrollBarStyle = new GUIStyle(GUI.skin.verticalScrollbar);
            scrollBarStyle.normal.background = texScrollBg;
            scrollBarStyle.fixedWidth = 8f;
            scrollBarStyle.border = CreateRectOffset(0, 0, 4, 4);
            scrollBarStyle.margin = CreateRectOffset(2, 2, 2, 2);

            GUIStyle scrollBarThumbStyle = new GUIStyle(GUI.skin.verticalScrollbarThumb);
            scrollBarThumbStyle.normal.background = texScrollThumb;
            scrollBarThumbStyle.hover.background = texScrollThumb;
            scrollBarThumbStyle.active.background = texScrollThumb;
            scrollBarThumbStyle.fixedWidth = 8f;
            scrollBarThumbStyle.border = CreateRectOffset(0, 0, 4, 4);

            GUI.skin.verticalScrollbar = scrollBarStyle;
            GUI.skin.verticalScrollbarThumb = scrollBarThumbStyle;
            GUI.skin.horizontalScrollbar.normal.background = null;
            GUI.skin.horizontalScrollbarThumb.normal.background = null;
            GUI.skin.label.normal.textColor = textMain;
            GUI.skin.box.normal.textColor = textMain;

            stylesInited = true;
        }
        public static bool autoCopyCodeAndLeave = false;

public static HashSet<int> votedPlayerIds = new HashSet<int>();

private void LoadBackgroundImage()
        {
            try
            {
                string bgPath = System.IO.Path.Combine(Plugin.ElysiumFolder, "MenuBG.png");
                if (!System.IO.File.Exists(bgPath)) bgPath = System.IO.Path.Combine(Plugin.ElysiumFolder, "MenuBG.jpg");
                if (System.IO.File.Exists(bgPath))
                {
                    byte[] fileData = System.IO.File.ReadAllBytes(bgPath);
                    Texture2D tempTex = new Texture2D(2, 2);
                    ImageConversion.LoadImage(tempTex, fileData);
                    customMenuBg = new Texture2D(tempTex.width, tempTex.height, TextureFormat.RGBA32, false);
                    customMenuBg.hideFlags = HideFlags.HideAndDontSave;
                    Color[] pix = tempTex.GetPixels();
                    UnityEngine.Object.Destroy(tempTex);
                    int w = customMenuBg.width, h = customMenuBg.height;
                    float targetRadius = 12f, rx = targetRadius * (w / windowRect.width), ry = targetRadius * (h / windowRect.height);
                    for (int y = 0; y < h; y++)
                        for (int x = 0; x < w; x++)
                        {
                            float dx = 0f, dy = 0f;
                            if (x < rx) dx = rx - x;
                            else if (x > w - rx) dx = x - (w - rx);
                            if (y < ry) dy = ry - y;
                            else if (y > h - ry) dy = y - (h - ry);
                            if (dx > 0 && dy > 0)
                            {
                                float nx = dx / rx, ny = dy / ry;
                                float dist = Mathf.Sqrt(nx * nx + ny * ny);
                                if (dist > 1f) { Color c = pix[y * w + x]; c.a = 0f; pix[y * w + x] = c; }
                                else
                                {
                                    float alphaMult = Mathf.Clamp01((1f - dist) * Mathf.Max(rx, ry));
                                    Color c = pix[y * w + x]; c.a *= alphaMult; pix[y * w + x] = c;
                                }
                            }
                        }
                    customMenuBg.SetPixels(pix); customMenuBg.Apply();
                }
                else enableBackground = false;
            }
            catch { enableBackground = false; }
        }

public static string ApplyMenuShimmer(string text)
        {
            string result = "";
            Color baseColor = GetMenuControlAccentColor();
            Color glowColor = whiteMenuTheme ? Color.Lerp(baseColor, Color.white, 0.45f) : Color.white;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == ' ') { result += " "; continue; }
                float wave = Mathf.Sin(Time.unscaledTime * 6f - (i * 0.4f)) * 0.5f + 0.5f;
                Color c = Color.Lerp(baseColor, glowColor, wave);
                result += $"<color=#{ColorUtility.ToHtmlStringRGB(c)}>{text[i]}</color>";
            }
            return result;
        }

private static readonly Dictionary<string, float> toggleAnimStates = new Dictionary<string, float>();

private void DrawAnimatedSwitch(Rect boxRect, bool value, string animKey)
        {
            string key = animKey ?? "";
            float anim;
            if (!toggleAnimStates.TryGetValue(key, out anim)) { anim = value ? 1f : 0f; toggleAnimStates[key] = anim; }
            if (Event.current.type != EventType.Repaint) return;

            anim = Mathf.MoveTowards(anim, value ? 1f : 0f, Time.unscaledDeltaTime * 8f);
            toggleAnimStates[key] = anim;
            float eased = Mathf.SmoothStep(0f, 1f, anim);

            float knob = boxRect.height - 4f;
            float knobX = Mathf.Lerp(boxRect.x + 2f, boxRect.xMax - knob - 2f, eased);
            Rect knobRect = new Rect(knobX, boxRect.y + 2f, knob, knob);

            Color prevBg = GUI.backgroundColor;
            GUI.backgroundColor = Color.Lerp(new Color(0.80f, 0.80f, 0.84f, 1f), Color.white, eased);
            GUI.Box(knobRect, "", knobStyle);
            GUI.backgroundColor = prevBg;
        }

private bool DrawToggle(bool value, string text, int width = 0)
        {
            GUILayout.BeginHorizontal(GUILayout.MinWidth(width > 0 ? width : 200), GUILayout.Height(20));

            Rect animSwitchRect = GUILayoutUtility.GetRect(30f, 16f, GUILayout.Width(30f), GUILayout.Height(16f));
            bool clickedBox = GUI.Button(animSwitchRect, "", value ? trackOnStyle : trackOffStyle);
            DrawAnimatedSwitch(animSwitchRect, value, text);

            GUILayout.Space(6);

            GUIStyle toggleTextStyle = new GUIStyle(toggleLabelStyle)
            {
                clipping = TextClipping.Overflow,
                wordWrap = false,
                richText = true,
                stretchWidth = false,
                alignment = TextAnchor.MiddleLeft
            };

            GUIContent toggleContent = new GUIContent(text);
            float toggleTextWidth = Mathf.Ceil(toggleTextStyle.CalcSize(toggleContent).x) + 8f;
            Rect textRect = GUILayoutUtility.GetRect(toggleTextWidth, 18f, GUILayout.Width(toggleTextWidth), GUILayout.Height(18f));
            GUI.Label(textRect, toggleContent, toggleTextStyle);

            bool clickedText = Event.current.type == EventType.MouseDown && textRect.Contains(Event.current.mousePosition);
            if (clickedText) Event.current.Use();

            GUILayout.FlexibleSpace();

            GUILayout.EndHorizontal();

            if (clickedBox || clickedText) settingsDirty = true;
            return (clickedBox || clickedText) ? !value : value;
        }

        private bool DrawBindableButton(string label, string bindKey, float width)
        {
            bool clicked = false;
            GUILayout.BeginVertical(GUILayout.Width(width));
            if (GUILayout.Button(label, btnStyle, GUILayout.Height(25), GUILayout.Width(width))) clicked = true;
            string bindTxt = bindingAction == bindKey ? "Press Key..." : (keyBinds.ContainsKey(bindKey) ? $"[{keyBinds[bindKey]}]" : "[Bind Key]");
            GUIStyle bindStyle = new GUIStyle(btnStyle) { fontSize = 10, normal = { textColor = new Color(0.6f, 0.6f, 0.6f) } };
            if (bindingAction == bindKey) bindStyle.normal.textColor = GetMenuAccentColor();
            if (GUILayout.Button(bindTxt, bindStyle, GUILayout.Height(15), GUILayout.Width(width))) bindingAction = bindKey;
            GUILayout.EndVertical();
            return clicked;
        }

        private bool DrawHostToggle(bool value, string text, float totalWidth = 250f)
        {
            GUILayout.BeginHorizontal(GUILayout.MinWidth(totalWidth), GUILayout.Height(20));
            Rect animSwitchRect = GUILayoutUtility.GetRect(30f, 16f, GUILayout.Width(30f), GUILayout.Height(16f));
            bool clickedBox = GUI.Button(animSwitchRect, "", value ? trackOnStyle : trackOffStyle);
            DrawAnimatedSwitch(animSwitchRect, value, text);
            GUILayout.Space(6);

            GUIStyle hostToggleTextStyle = new GUIStyle(toggleLabelStyle)
            {
                clipping = TextClipping.Overflow,
                wordWrap = false,
                richText = true,
                stretchWidth = false,
                alignment = TextAnchor.MiddleLeft
            };

            GUIContent hostToggleContent = new GUIContent(text);
            float hostToggleTextWidth = Mathf.Ceil(hostToggleTextStyle.CalcSize(hostToggleContent).x) + 8f;
            Rect textRect = GUILayoutUtility.GetRect(hostToggleTextWidth, 16f, GUILayout.Width(hostToggleTextWidth), GUILayout.Height(16f));
            GUI.Label(textRect, hostToggleContent, hostToggleTextStyle);

            bool clickedText = Event.current.type == EventType.MouseDown && textRect.Contains(Event.current.mousePosition);
            if (clickedText) Event.current.Use();

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            if (clickedBox || clickedText) settingsDirty = true;
            return (clickedBox || clickedText) ? !value : value;
        }
        private void DrawBindsTab()
        {
            GUILayout.BeginVertical(menuCardStyle);
            try
            {
                DrawMenuSectionHeader("CUSTOM KEYBINDS");
                GUILayout.Label(L("Menu toggle is configurable. Right Shift stays disabled.", "Кнопку меню можно менять. Right Shift выключен."), menuDescStyle);
                GUILayout.Space(10);

                DrawKeybindRow("Menu Toggle:", ref menuToggleKey, ref isWaitingForBind);
                DrawKeybindRow("Magnet Cursor:", ref bindMagnetCursor, ref isWaitBindMagnetCursor);
                DrawKeybindRow("Mass Morph:", ref bindMassMorph, ref isWaitBindMassMorph);
                DrawKeybindRow("Spawn Lobby:", ref bindSpawnLobby, ref isWaitBindSpawnLobby);
                DrawKeybindRow("Despawn Lobby:", ref bindDespawnLobby, ref isWaitBindDespawnLobby);
                DrawKeybindRow("Close Meeting:", ref bindCloseMeeting, ref isWaitBindCloseMeeting);
                DrawKeybindRow("Insta Start:", ref bindInstaStart, ref isWaitBindInstaStart);
                DrawKeybindRow("End: Crewmate Win:", ref bindEndCrew, ref isWaitBindEndCrew);
                DrawKeybindRow("End: Impostor Win:", ref bindEndImp, ref isWaitBindEndImp);
                DrawKeybindRow("End: Imp Disconnect:", ref bindEndImpDC, ref isWaitBindEndImpDC);
                DrawKeybindRow("End: H&S Disconnect:", ref bindEndHnsDC, ref isWaitBindEndHnsDC);
                DrawKeybindRow("Toggle Tracers:", ref bindToggleTracers, ref isWaitBindToggleTracers);
                DrawKeybindRow("Toggle NoClip:", ref bindToggleNoClip, ref isWaitBindToggleNoClip);
                DrawKeybindRow("Toggle Freecam:", ref bindToggleFreecam, ref isWaitBindToggleFreecam);
                DrawKeybindRow("Toggle Camera Zoom:", ref bindToggleCameraZoom, ref isWaitBindToggleCameraZoom);
                DrawKeybindRow("Toggle Player Info:", ref bindTogglePlayerInfo, ref isWaitBindTogglePlayerInfo);
                DrawKeybindRow("Toggle See Roles:", ref bindToggleSeeRoles, ref isWaitBindToggleSeeRoles);
                DrawKeybindRow("Toggle See Ghosts:", ref bindToggleSeeGhosts, ref isWaitBindToggleSeeGhosts);
                DrawKeybindRow("Toggle Full Bright:", ref bindToggleFullBright, ref isWaitBindToggleFullBright);
                DrawKeybindRow("Kill All:", ref bindKillAll, ref isWaitBindKillAll);
                DrawKeybindRow("Call Meeting:", ref bindCallMeeting, ref isWaitBindCallMeeting);
                DrawKeybindRow("Kick All:", ref bindKickAll, ref isWaitBindKickAll);
                DrawKeybindRow("Fix Sabotages:", ref bindFixSabotages, ref isWaitBindFixSabotages);
                DrawKeybindRow("Ghost All:", ref bindSetAllGhost, ref isWaitBindSetAllGhost);
                DrawKeybindRow("Revive All:", ref bindReviveAll, ref isWaitBindReviveAll);
                DrawKeybindRow("All -> Ghost Imp:", ref bindSetAllGhostImp, ref isWaitBindSetAllGhostImp);
            }
            finally { GUILayout.EndVertical(); }
        }

        private void DrawKeybindRow(string label, ref KeyCode currentKey, ref bool isWaiting)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            GUIStyle alignedLabel = new GUIStyle(toggleLabelStyle) { alignment = TextAnchor.MiddleLeft, margin = CreateRectOffset(0, 0, 4, 0) };
            GUILayout.Label(label, alignedLabel, GUILayout.Width(220), GUILayout.Height(25));

            string bindText = isWaiting ? "Press any key..." : (currentKey == KeyCode.None ? "NONE" : currentKey.ToString());
            if (GUILayout.Button(bindText, isWaiting ? activeTabStyle : btnStyle, GUILayout.Width(120), GUILayout.Height(25)))
            {
                ResetAllBindWaits();
                isWaiting = true;
            }

            if (GUILayout.Button("Clear", btnStyle, GUILayout.Width(50), GUILayout.Height(25)))
            {
                currentKey = KeyCode.None;
                isWaiting = false;
                SaveConfig();
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(5);
        }
        public static bool AnimShieldsEnabled = false;

public static bool AnimAsteroidsEnabled = false;

public static bool AnimCamsInUseEnabled = false;

public static bool IsScanning = false;

private void ResetAllBindWaits()
        {
            isWaitingForBind = false;
            isWaitBindMassMorph = false;
            isWaitBindSpawnLobby = false;
            isWaitBindDespawnLobby = false;
            isWaitBindCloseMeeting = false;
            isWaitBindInstaStart = false;
            isWaitBindEndCrew = false;
            isWaitBindEndImp = false;
            isWaitBindEndImpDC = false;
            isWaitBindEndHnsDC = false;
            isWaitBindMagnetCursor = false;
            isWaitBindToggleTracers = false;
            isWaitBindToggleNoClip = false;
            isWaitBindToggleFreecam = false;
            isWaitBindToggleCameraZoom = false;
            isWaitBindKillAll = false;
            isWaitBindCallMeeting = false;
            isWaitBindTogglePlayerInfo = false;
            isWaitBindToggleSeeRoles = false;
            isWaitBindToggleSeeGhosts = false;
            isWaitBindToggleFullBright = false;
            isWaitBindKickAll = false;
            isWaitBindFixSabotages = false;
            isWaitBindSetAllGhost = false;
            isWaitBindSetAllGhostImp = false;
            isWaitBindReviveAll = false;
        }

private void DrawGeneralTab()
        {
            GUILayout.BeginVertical(menuCardStyle);
            DrawMenuSectionHeader(L("GENERAL", "ГЛАВНОЕ"));
            GUILayout.Space(4);

            GUILayout.BeginHorizontal();
            for (int i = 0; i < generalSubTabs.Length; i++)
            {
                if (GUILayout.Button(generalSubTabs[i], currentGeneralSubTab == i ? activeSubTabStyle : subTabStyle, GUILayout.Height(22)))
                {
                    currentGeneralSubTab = i;
                    scrollPosition = Vector2.zero;
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.Space(8);

            if (currentGeneralSubTab == 0) DrawGeneralInfoTab();
            else if (currentGeneralSubTab == 1) DrawBindsTab();
        }
}
}
