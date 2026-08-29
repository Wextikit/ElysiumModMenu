#nullable disable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ElysiumModMenu
{
    public partial class ElysiumModMenuGUI : MonoBehaviour
    {
        private sealed class MenuSearchEntry
        {
            public readonly string Name;
            public readonly string Keywords;
            public readonly int Tab;
            public readonly int SubTab;

            public MenuSearchEntry(string name, string keywords, int tab, int subTab = 0)
            {
                Name = name;
                Keywords = keywords;
                Tab = tab;
                SubTab = subTab;
            }
        }

        private static string menuSearchQuery = string.Empty;
        private static bool menuSearchInputFocused;
        private GUIStyle menuSearchHintStyle;
        private GUIStyle menuSearchTextStyle;
        private GUIStyle menuSearchHintSource;
        private GUIStyle menuSearchTextSource;
        private static readonly List<MenuSearchEntry> menuSearchHits = new List<MenuSearchEntry>();
        private static readonly MenuSearchEntry[] menuSearchEntries =
        {
            new MenuSearchEntry("General", "general common kill all meeting teleport noclip freecam camera", 0),
            new MenuSearchEntry("Self", "self player speed outfits spoof name friend code", 1),
            new MenuSearchEntry("Visuals", "visual esp radar replay tracers roles ghosts", 2, 0),
            new MenuSearchEntry("ESP Boxes", "esp boxes player boxes", 2, 0),
            new MenuSearchEntry("Task Tracers", "task tasks tracers arrows трассеры стрелки таски", 2, 0),
            new MenuSearchEntry("Radar", "radar map bodies ghosts", 2, 0),
            new MenuSearchEntry("Players", "players target player kill telekill report", 3, 0),
            new MenuSearchEntry("Player Tasks", "flood change delete clear tasks", 3, 0),
            new MenuSearchEntry("Player History", "history players history", 3, 1),
            new MenuSearchEntry("Clones", "clone clones spawn target", 3, 2),
            new MenuSearchEntry("Sabotages", "sabotage reactor oxygen comms lights doors vents", 4, 0),
            new MenuSearchEntry("Task Tools", "tasks task drain auto tasks fake tasks host tasks", 4, 1),
            new MenuSearchEntry("Animations", "animations scanner medbay shields asteroids garbage", 4, 4),
            new MenuSearchEntry("Maps", "maps map teleport rooms", 4, 5),
            new MenuSearchEntry("Lobby Settings", "lobby settings meetings rules roles cooldown tasks", 4, 2),
            new MenuSearchEntry("H&S", "hns hide seek task drain", 4, 3),
            new MenuSearchEntry("Host Only", "host lobby role manager anti cheat auto host glitch room", 5),
            new MenuSearchEntry("Votekick", "votekick kick vote", 6),
            new MenuSearchEntry("Menu Settings", "menu profiles theme language keybinds settings", 7),
            new MenuSearchEntry("Pet Hand", "pet hand arrows joystick", 8),
            new MenuSearchEntry("Anti Cheat", "anti cheat protection rpc guard", 9)
        };

        private void DrawMenuSearchResults(float width)
        {
            string query = (menuSearchQuery ?? string.Empty).Trim();
            if (query.Length == 0) return;

            menuSearchHits.Clear();
            for (int i = 0; i < menuSearchEntries.Length; i++)
            {
                MenuSearchEntry entry = menuSearchEntries[i];
                if (entry.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    entry.Keywords.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                    menuSearchHits.Add(entry);
            }

            float w = Mathf.Max(120f, width - 4f);
            GUILayout.BeginVertical(menuCardStyle, GUILayout.Width(w));
            try
            {
                DrawMenuSectionHeader($"SEARCH ({menuSearchHits.Count})");
                if (menuSearchHits.Count == 0)
                {
                    GUILayout.Label("Nothing found.", toggleLabelStyle11);
                    return;
                }

                for (int i = 0; i < menuSearchHits.Count; i++)
                {
                    MenuSearchEntry entry = menuSearchHits[i];
                    if (GUILayout.Button(entry.Name, btnStyle, GUILayout.Height(25f)))
                    {
                        OpenMenuSearchEntry(entry);
                        return;
                    }
                    if (i < menuSearchHits.Count - 1) GUILayout.Space(3f);
                }
            }
            finally { GUILayout.EndVertical(); }
        }

        private void DrawMenuSearchInput(float width)
        {
            const float searchHeight = 26f;
            Rect rect = GUILayoutUtility.GetRect(width, searchHeight, GUILayout.Width(width), GUILayout.Height(searchHeight));
            Event e = Event.current;

            if (e != null && e.type == EventType.MouseDown)
            {
                if (rect.Contains(e.mousePosition))
                {
                    menuSearchInputFocused = true;
                    e.Use();
                }
                else if (menuSearchInputFocused)
                {
                    menuSearchInputFocused = false;
                }
            }

            if (menuSearchInputFocused && e != null && e.type == EventType.KeyDown)
            {
                string previous = menuSearchQuery ?? string.Empty;
                if (HandleClipboardShortcut(e, ref menuSearchQuery, 64)) { }
                else if (e.keyCode == KeyCode.Backspace)
                {
                    if (menuSearchQuery.Length > 0)
                        menuSearchQuery = menuSearchQuery.Substring(0, menuSearchQuery.Length - 1);
                    e.Use();
                }
                else if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
                {
                    menuSearchInputFocused = false;
                    e.Use();
                }
                else if (!char.IsControl(e.character) && e.character != '\n' && e.character != '\r' && menuSearchQuery.Length < 64)
                {
                    menuSearchQuery += e.character;
                    e.Use();
                }

                if (menuSearchQuery != previous)
                    scrollPosition = Vector2.zero;
            }

            EnsureMenuSearchStyles();
            GUI.Box(rect, GUIContent.none, inputBlockStyle);
            Rect textRect = new Rect(rect.x + 8f, rect.y, Mathf.Max(0f, rect.width - 16f), rect.height);
            string text = menuSearchQuery ?? string.Empty;
            if (text.Length == 0 && !menuSearchInputFocused)
                GUI.Label(textRect, "Search menu...", menuSearchHintStyle);
            else
                GUI.Label(textRect, menuSearchInputFocused && Time.unscaledTime % 1f < 0.5f ? text + "|" : text, menuSearchTextStyle);
        }

        private void EnsureMenuSearchStyles()
        {
            if (menuSearchHintStyle != null && menuSearchTextStyle != null && menuSearchHintSource == menuDescStyle && menuSearchTextSource == toggleLabelStyle)
                return;

            menuSearchHintSource = menuDescStyle;
            menuSearchTextSource = toggleLabelStyle;
            menuSearchHintStyle = new GUIStyle(menuDescStyle) { alignment = TextAnchor.MiddleLeft, clipping = TextClipping.Clip, wordWrap = false, padding = CreateRectOffset(0, 0, 0, 0), fontSize = 11 };
            menuSearchTextStyle = new GUIStyle(toggleLabelStyle) { alignment = TextAnchor.MiddleLeft, clipping = TextClipping.Clip, wordWrap = false, padding = CreateRectOffset(0, 0, 0, 0), fontSize = 11 };
        }

        private void DrawMenuSearchBar(float width)
        {
            bool hasQuery = !string.IsNullOrEmpty(menuSearchQuery);
            float inputWidth = Mathf.Max(0f, width - (hasQuery ? 28f : 0f));

            GUILayout.BeginHorizontal(GUILayout.Width(width), GUILayout.Height(26f));
            try
            {
                DrawMenuSearchInput(inputWidth);
                if (hasQuery)
                {
                    GUILayout.Space(2f);
                    if (GUILayout.Button("×", btnStyle, GUILayout.Width(26f), GUILayout.Height(26f)))
                    {
                        menuSearchQuery = string.Empty;
                        menuSearchInputFocused = false;
                        scrollPosition = Vector2.zero;
                    }
                }
            }
            finally { GUILayout.EndHorizontal(); }
        }

        private void OpenMenuSearchEntry(MenuSearchEntry entry)
        {
            if (entry.Tab == 2) currentVisualsSubTab = entry.SubTab;
            else if (entry.Tab == 3) currentPlayersSubTab = entry.SubTab;
            else if (entry.Tab == 4) currentSabotageSubTab = entry.SubTab;
            else if (entry.Tab == 5) currentHostOnlySubTab = entry.SubTab;

            menuSearchQuery = string.Empty;
            menuSearchInputFocused = false;
            SetMenuTab(entry.Tab);
        }
    }
}
