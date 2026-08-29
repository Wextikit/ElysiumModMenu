#nullable disable
#pragma warning disable CS0162, CS0108, CS0219, CS0661, CS0660, CS8632, CS0168, CS0659
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;

namespace ElysiumModMenu
{
    public partial class ElysiumModMenuGUI : MonoBehaviour
    {
        private const int ReplayWindowId = 843208;
        private const int ReplayLogWindowId = 843209;
        private const int ReplayMaxEvents = 1200;
        private const int ReplayMaxPointsPerPlayer = 18000;
        private const int ReplayPathFilterBit = 1 << 9;

        public static bool replayRecordEnabled = true;
        public static bool showReplayLog = false;
        public static bool replayOverlayOnRadar = false;
        public static bool replayHideRadarLive = false;
        public static int replayFilterMask = 0x3FF;
        public static Rect replayLogRect = new Rect(700f, 90f, 540f, 390f);

        internal enum ReplayEventType
        {
            Kill = 0,
            Protect = 1,
            Task = 2,
            Vent = 3,
            Report = 4,
            Shapeshift = 5,
            Sabotage = 6,
            Meeting = 7,
            Other = 8
        }

        private struct ReplayPoint
        {
            public Vector2 pos;
            public float t;
        }

        private sealed class ReplayPlayerState
        {
            public Vector2 pos;
            public Color color;
            public Color roleColor;
            public string name;
            public bool dead;
            public float deadAt;
            public bool imp;
            public float lastAt;
        }

        private sealed class ReplayEvent
        {
            public ReplayEventType type;
            public byte playerId;
            public Vector2 pos;
            public Vector2 targetPos;
            public bool hasPosition;
            public bool hasTargetPosition;
            public float t;
            public string text;
        }

        private sealed class ReplayMap
        {
            public int id;
            public string res;
            public float x;
            public float y;
            public float scale;
            public Texture2D tex;
            public GUIStyle style;
        }

        private static readonly Dictionary<byte, List<ReplayPoint>> replayPaths = new Dictionary<byte, List<ReplayPoint>>();
        private static readonly Dictionary<byte, ReplayPlayerState> replayPlayers = new Dictionary<byte, ReplayPlayerState>();
        private static readonly List<ReplayEvent> replayEvents = new List<ReplayEvent>(ReplayMaxEvents);
        private static readonly int[] replaySabotageSystems = { 3, 8, 7, 14, 15 };
        private static readonly bool[] replaySabotagePrevious = new bool[5];
        private static readonly Action<int> drawReplayWindow = DrawReplayWindow;
        private static readonly Action<int> drawReplayConsoleWindow = DrawReplayConsoleWindow;

        private static Texture2D replayPxTex;
        private static Texture2D replayPlayerTex;
        private static Texture2D replayVisorTex;
        private static Texture2D replayCrossTex;
        private static Texture2D replayBodyTex;
        private static Texture2D replayKillTex;
        private static Texture2D replayReportTex;
        private static Texture2D replayTaskTex;
        private static Texture2D replayVentInTex;
        private static Texture2D replayVentOutTex;
        private static GUIStyle replayPxStyle;
        private static GUIStyle replayWinStyle;
        private static GUIStyle replayHeaderStyle;
        private static GUIStyle replaySmallStyle;
        private static GUIStyle replayCenterStyle;
        private static GUIStyle replayLogStyle;
        private static GUIStyle replayButtonStyle;
        private static GUIStyle replayPanelStyle;
        private static GUIStyle replayCardStyle;
        private static Texture2D replayPanelTex;
        private static Texture2D replayCardTex;
        private static Texture2D replayButtonTex;
        private static Texture2D replayButtonActiveTex;
        private static Color replayThemeAccent = new Color(-1f, -1f, -1f, -1f);
        private static bool replayThemeLight;
        // Cache one style per icon.
        private static readonly Dictionary<Texture2D, GUIStyle> replayIconStyles = new Dictionary<Texture2D, GUIStyle>();

        private static float replayNextSampleAt;
        private static bool replayWasInGame;
        private static bool replayWasMeeting;
        private static int replayMapId;
        private static float replayStartTime;
        private static float replayEndTime;
        private static bool replayHasTime;
        private static float replayScrub = 1f;
        private static bool replayPlaying;
        private static bool replayLive = true;
        private static byte replayFocusedPlayer = byte.MaxValue;
        private static Vector2 replayLogScroll;
        private static bool replayResizeActive;
        private static bool replayLogResizeActive;
        private static Vector2 replayResizeStartMouse;
        private static Vector2 replayResizeStartSize;
        private static Vector2 replayLogResizeStartMouse;
        private static Vector2 replayLogResizeStartSize;
        private static float replayPendingWidth;
        private static float replayPendingHeight;
        private static float replayLogPendingWidth;
        private static float replayLogPendingHeight;

        private static readonly ReplayMap[] replayMaps =
        {
            new ReplayMap { id = 0, res = "ElysiumModMenu.radar_skeld.png", x = 277f, y = 77f, scale = 11.5f },
            new ReplayMap { id = 1, res = "ElysiumModMenu.radar_mira_hq.png", x = 115f, y = 240f, scale = 9.25f },
            new ReplayMap { id = 2, res = "ElysiumModMenu.radar_polus.png", x = 8f, y = 21f, scale = 10f },
            new ReplayMap { id = 3, res = "ElysiumModMenu.radar_skeld.png", x = 277f, y = 77f, scale = 11.5f },
            new ReplayMap { id = 4, res = "ElysiumModMenu.radar_airship.png", x = 162f, y = 107f, scale = 6f },
            new ReplayMap { id = 5, res = "ElysiumModMenu.radar_fungle.png", x = 237f, y = 140f, scale = 8.5f }
        };

        private static void TickVisualReplay()
        {
            bool inGame = IsVisualReplaySessionActive();
            if (!inGame)
            {
                bool clientStillInGame = false;
                try { clientStillInGame = AmongUsClient.Instance != null && AmongUsClient.Instance.IsGameStarted; } catch (global::System.Exception __elysiumCaught187) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught187); }
                if (!clientStillInGame) replayWasInGame = false;
                replayPlaying = false;
                replayWasMeeting = false;
                ResetReplaySabotageState();
                return;
            }

            if (!replayWasInGame)
            {
                ClearVisualReplay();
                replayMapId = GetCurrentMapId();
                replayWasInGame = true;
            }

            TickReplayPlayback();

            if (MeetingHud.Instance != null || ExileController.Instance != null)
            {
                replayWasMeeting = true;
                ResetReplaySabotageState();
                return;
            }
            if (replayWasMeeting)
            {
                replayWasMeeting = false;
                replayPaths.Clear();
            }

            if (!replayRecordEnabled) return;
            if (Time.unscaledTime < replayNextSampleAt) return;
            replayNextSampleAt = Time.unscaledTime + 0.12f;
            if (PlayerControl.AllPlayerControls == null) return;

            float now = Time.unscaledTime;
            StampReplayTime(now);
            try
            {
                foreach (PlayerControl pc in PlayerControl.AllPlayerControls)
                {
                    if (pc == null || pc.Data == null || pc.Data.Disconnected) continue;
                    byte id = pc.PlayerId;
                    Vector2 pos = pc.GetTruePosition();
                    if (!replayPaths.TryGetValue(id, out List<ReplayPoint> points))
                    {
                        points = new List<ReplayPoint>(1024);
                        replayPaths[id] = points;
                    }

                    if (points.Count == 0 || (points[points.Count - 1].pos - pos).sqrMagnitude > 0.0016f)
                    {
                        points.Add(new ReplayPoint { pos = pos, t = now });
                        if (points.Count > ReplayMaxPointsPerPlayer)
                            points.RemoveRange(0, points.Count - ReplayMaxPointsPerPlayer);
                    }

                    if (!replayPlayers.TryGetValue(id, out ReplayPlayerState state))
                    {
                        state = new ReplayPlayerState { deadAt = float.MaxValue };
                        replayPlayers[id] = state;
                    }
                    state.pos = pos;
                    state.color = GetReplayPlayerColor(pc);
                    state.roleColor = GetReplayRoleColor(pc);
                    state.name = ReplayPlayerName(pc);
                    if (!state.dead && pc.Data.IsDead) state.deadAt = now;
                    state.dead = pc.Data.IsDead;
                    state.imp = IsReplayImp(pc);
                    state.lastAt = now;
                }
                TickReplaySabotages();
            }
            catch (global::System.Exception __elysiumCaught188) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught188); }

        }

        private static void TickReplayPlayback()
        {
            if (replayPlaying && replayHasTime && replayEndTime > replayStartTime)
            {
                replayLive = false;
                replayScrub += Time.unscaledDeltaTime / Mathf.Max(0.5f, replayEndTime - replayStartTime);
                if (replayScrub >= 1f)
                {
                    replayScrub = 1f;
                    replayPlaying = false;
                    replayLive = true;
                }
            }
        }

        private static bool IsVisualReplaySessionActive()
        {
            try
            {
                return AmongUsClient.Instance != null && AmongUsClient.Instance.IsGameStarted &&
                       ShipStatus.Instance != null && GameData.Instance != null && PlayerControl.LocalPlayer != null;
            }
            catch { return false; }
        }

        private static void StampReplayTime(float time)
        {
            float heldTime = !replayLive && !replayPlaying && replayHasTime ? ReplayViewTime() : 0f;
            if (!replayHasTime)
            {
                replayStartTime = time;
                replayHasTime = true;
            }
            replayEndTime = Mathf.Max(replayEndTime, time);
            if (heldTime > 0f && replayEndTime > replayStartTime)
                replayScrub = Mathf.Clamp01((heldTime - replayStartTime) / (replayEndTime - replayStartTime));
        }

        internal static void RecordReplayEvent(ReplayEventType type, PlayerControl player, PlayerControl target, string text)
        {
            if (!replayRecordEnabled || (!replayWasInGame && !IsVisualReplaySessionActive())) return;
            try
            {
                float now = Time.unscaledTime;
                byte playerId = player != null ? player.PlayerId : byte.MaxValue;
                if (replayEvents.Count > 0)
                {
                    ReplayEvent last = replayEvents[replayEvents.Count - 1];
                    if (last.type == type && last.playerId == playerId && now - last.t < 0.15f && last.text == text)
                        return;
                }

                while (replayEvents.Count >= ReplayMaxEvents) replayEvents.RemoveAt(0);
                ReplayEvent ev = new ReplayEvent
                {
                    type = type,
                    playerId = playerId,
                    t = now,
                    text = string.IsNullOrWhiteSpace(text) ? ReplayEventLabel(type) : text
                };
                if (player != null)
                {
                    ev.pos = player.GetTruePosition();
                    ev.hasPosition = true;
                }
                if (target != null)
                {
                    ev.targetPos = target.GetTruePosition();
                    ev.hasTargetPosition = true;
                }
                replayEvents.Add(ev);
                StampReplayTime(now);
            }
            catch (global::System.Exception __elysiumCaught189) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught189); }
        }

        private static void TickReplaySabotages()
        {
            if (ShipStatus.Instance == null || MeetingHud.Instance != null)
            {
                ResetReplaySabotageState();
                return;
            }
            for (int i = 0; i < replaySabotageSystems.Length; i++)
            {
                bool active = IsReplaySabotageActive(replaySabotageSystems[i]);
                if (active && !replaySabotagePrevious[i])
                    RecordReplayEvent(ReplayEventType.Sabotage, null, null, "Sabotage: " + ReplaySabotageName(replaySabotageSystems[i]));
                replaySabotagePrevious[i] = active;
            }
        }

        private static bool IsReplaySabotageActive(int systemId)
        {
            try
            {
                var system = ShipStatus.Instance.Systems[(SystemTypes)systemId];
                if (system == null) return false;
                var active = ((Il2CppObjectBase)system).TryCast<IActivatable>();
                return active != null && active.IsActive;
            }
            catch { return false; }
        }

        private static string ReplaySabotageName(int id)
        {
            if (id == 3 || id == 15) return "Reactor";
            if (id == 8) return "Oxygen";
            if (id == 7) return "Lights";
            if (id == 14) return "Comms";
            return "System";
        }

        private static void ResetReplaySabotageState()
        {
            for (int i = 0; i < replaySabotagePrevious.Length; i++) replaySabotagePrevious[i] = false;
        }

        public static void ClearVisualReplay()
        {
            replayPaths.Clear();
            replayPlayers.Clear();
            replayEvents.Clear();
            replayNextSampleAt = 0f;
            replayStartTime = 0f;
            replayEndTime = 0f;
            replayHasTime = false;
            replayScrub = 1f;
            replayPlaying = false;
            replayLive = true;
            replayFocusedPlayer = byte.MaxValue;
            replayLogScroll = Vector2.zero;
            replayWasMeeting = false;
            ResetReplaySabotageState();
        }

        private static void DrawVisualReplay()
        {
            if (!showReplay && !showReplayLog) return;
            InitReplayGui();

            if (showReplay)
            {
                ReplayMap map = GetReplayMap();
                if (map != null)
                {
                    replayRect.width = Mathf.Clamp(replayRect.width, 460f, Mathf.Max(460f, Screen.width));
                    replayRect.height = Mathf.Clamp(replayRect.height, 320f, Mathf.Max(320f, Screen.height));
                    Rect input = replayRect;
                    Rect result = GUI.Window(ReplayWindowId, input, drawReplayWindow, string.Empty, replayWinStyle);
                    if (replayResizeActive)
                    {
                        result.width = replayPendingWidth;
                        result.height = replayPendingHeight;
                    }
                    replayRect = ClampReplayRect(result, 460f, 320f);
                    if (Mathf.Abs(input.x - replayRect.x) > 0.1f || Mathf.Abs(input.y - replayRect.y) > 0.1f ||
                        Mathf.Abs(input.width - replayRect.width) > 0.1f || Mathf.Abs(input.height - replayRect.height) > 0.1f)
                        settingsDirty = true;
                }
            }

            if (showReplayLog)
            {
                replayLogRect.width = Mathf.Clamp(replayLogRect.width, 520f, Mathf.Max(520f, Screen.width));
                replayLogRect.height = Mathf.Clamp(replayLogRect.height, 300f, Mathf.Max(300f, Screen.height));
                Rect logInput = replayLogRect;
                Rect logResult = GUI.Window(ReplayLogWindowId, logInput, drawReplayConsoleWindow, string.Empty, replayWinStyle);
                if (replayLogResizeActive)
                {
                    logResult.width = replayLogPendingWidth;
                    logResult.height = replayLogPendingHeight;
                }
                replayLogRect = ClampReplayRect(logResult, 520f, 300f);
                if (Mathf.Abs(logInput.x - replayLogRect.x) > 0.1f || Mathf.Abs(logInput.y - replayLogRect.y) > 0.1f ||
                    Mathf.Abs(logInput.width - replayLogRect.width) > 0.1f || Mathf.Abs(logInput.height - replayLogRect.height) > 0.1f)
                    settingsDirty = true;
            }
        }

        private static Rect ClampReplayRect(Rect rect, float minWidth, float minHeight)
        {
            rect.width = Mathf.Clamp(rect.width, minWidth, Mathf.Max(minWidth, Screen.width));
            rect.height = Mathf.Clamp(rect.height, minHeight, Mathf.Max(minHeight, Screen.height));
            rect.x = Mathf.Clamp(rect.x, 0f, Mathf.Max(0f, Screen.width - rect.width));
            rect.y = Mathf.Clamp(rect.y, 0f, Mathf.Max(0f, Screen.height - rect.height));
            return rect;
        }

        private static void DrawReplayWindow(int id)
        {
            ReplayMap map = GetReplayMap();
            if (map == null) return;
            Event e = Event.current;
            float w = replayRect.width;
            float h = replayRect.height;
            Color accent = GetMenuAccentColor(false);

            ReplayFill(new Rect(0f, 0f, w, h), new Color(0.035f, 0.04f, 0.055f, 0.97f));
            ReplayFill(new Rect(0f, 0f, w, 30f), new Color(accent.r, accent.g, accent.b, 0.18f));
            ReplayStroke(new Rect(0f, 0f, w, h), new Color(accent.r, accent.g, accent.b, 0.82f));
            GUI.Label(new Rect(12f, 5f, 160f, 20f), "REPLAY", replayHeaderStyle);

            if (GUI.Button(new Rect(w - 264f, 5f, 104f, 20f), replayFocusedPlayer == byte.MaxValue ? "Player: All" : ReplayFocusedPlayerLabel(), replayButtonStyle))
                CycleReplayFocusedPlayer();
            if (GUI.Button(new Rect(w - 156f, 5f, 72f, 20f), "CONSOLE", replayButtonStyle)) { showReplayLog = !showReplayLog; settingsDirty = true; }
            if (GUI.Button(new Rect(w - 80f, 5f, 48f, 20f), "CLEAR", replayButtonStyle)) ClearVisualReplay();
            if (GUI.Button(new Rect(w - 28f, 5f, 20f, 20f), "X", replayButtonStyle)) { showReplay = false; settingsDirty = true; }

            DrawReplayFilters(new Rect(8f, 34f, w - 16f, 22f));

            Rect timeline = new Rect(42f, h - 28f, w - 132f, 18f);
            if (GUI.Button(new Rect(8f, h - 29f, 30f, 20f), replayPlaying ? "II" : ">", replayButtonStyle))
            {
                if (replayLive || replayScrub >= 0.999f) replayScrub = 0f;
                replayLive = false;
                replayPlaying = !replayPlaying;
            }
            float oldScrub = replayScrub;
            replayScrub = GUI.HorizontalSlider(timeline, replayScrub, 0f, 1f);
            DrawReplayTimelineTicks(timeline);
            if (Mathf.Abs(oldScrub - replayScrub) > 0.0001f)
            {
                replayLive = replayScrub >= 0.995f;
                replayPlaying = false;
            }
            if (GUI.Button(new Rect(w - 84f, h - 29f, 76f, 20f), replayLive ? "LIVE" : ReplayTimelineLabel(), replayButtonStyle))
            {
                replayScrub = 1f;
                replayLive = true;
                replayPlaying = false;
            }

            Rect body = new Rect(8f, 60f, w - 16f, h - 94f);
            ReplayFill(body, new Color(0.10f, 0.115f, 0.145f, 0.92f));
            Rect mapRect = FitTextureRect(body, map.tex.width, map.tex.height);
            GUI.BeginGroup(mapRect);
            try
            {
                Rect clippedMap = new Rect(0f, 0f, mapRect.width, mapRect.height);
                GUI.color = new Color(1f, 1f, 1f, 0.80f);
                GUI.Box(clippedMap, GUIContent.none, map.style);
                GUI.color = Color.white;

                float viewTime = ReplayViewTime();
                DrawReplayPaths(map, clippedMap, viewTime);
                DrawReplayEvents(map, clippedMap, viewTime);
                DrawReplayPlayersAt(map, clippedMap, viewTime);
            }
            finally
            {
                GUI.color = Color.white;
                GUI.EndGroup();
            }
            ReplayStroke(mapRect, new Color(accent.r, accent.g, accent.b, 0.55f));

            if (!replayHasTime && replayPaths.Count == 0)
            {
                replayCenterStyle.fontSize = 12;
                GUI.color = new Color(0.78f, 0.80f, 0.86f, 1f);
                GUI.Label(new Rect(body.x, body.center.y - 12f, body.width, 24f), replayRecordEnabled ? "WAITING FOR GAME DATA" : "RECORDING DISABLED", replayCenterStyle);
                GUI.color = Color.white;
                replayCenterStyle.fontSize = 10;
            }

            HandleReplayResize(e, false, w, h);
            GUI.DragWindow(new Rect(0f, 0f, Mathf.Max(0f, w - 276f), 30f));
        }

        private static void DrawReplayConsoleWindow(int id)
        {
            Event e = Event.current;
            float w = replayLogRect.width;
            float h = replayLogRect.height;
            Color accent = GetMenuAccentColor(false);
            RefreshReplayThemeStyles();

            GUI.Box(new Rect(0f, 0f, w, h), GUIContent.none, replayPanelStyle);
            ReplayFill(new Rect(12f, 10f, 3f, 20f), accent);
            GUI.Label(new Rect(22f, 8f, 190f, 24f), "REPLAY CONSOLE", replayHeaderStyle);
            GUI.Label(new Rect(w - 108f, 10f, 64f, 20f), replayEvents.Count + " / " + ReplayMaxEvents, replaySmallStyle);
            if (GUI.Button(new Rect(w - 36f, 8f, 28f, 24f), "X", replayButtonStyle)) { showReplayLog = false; settingsDirty = true; }

            GUI.Box(new Rect(8f, 40f, w - 16f, 36f), GUIContent.none, replayCardStyle);
            GUI.Label(new Rect(18f, 48f, 56f, 20f), "PLAYER", replaySmallStyle);
            if (GUI.Button(new Rect(76f, 46f, 126f, 24f), replayFocusedPlayer == byte.MaxValue ? "All Players" : ReplayFocusedPlayerLabel(), replayButtonStyle))
                CycleReplayFocusedPlayer();
            if (GUI.Button(new Rect(w - 122f, 46f, 104f, 24f), "CLEAR", replayButtonStyle))
            {
                replayEvents.Clear();
                replayLogScroll = Vector2.zero;
            }
            DrawReplayFilters(new Rect(8f, 84f, w - 16f, 26f), false);
            Rect body = new Rect(8f, 118f, w - 16f, Mathf.Max(44f, h - 126f));
            GUI.Box(body, GUIContent.none, replayCardStyle);
            int visible = 0;
            for (int i = 0; i < replayEvents.Count; i++)
            {
                ReplayEvent ev = replayEvents[i];
                if (ReplayEventVisible(ev) && (replayFocusedPlayer == byte.MaxValue || ev.playerId == replayFocusedPlayer)) visible++;
            }
            Rect scrollBody = new Rect(body.x + 6f, body.y + 6f, body.width - 12f, body.height - 12f);
            Rect view = new Rect(0f, 0f, Mathf.Max(100f, scrollBody.width - 18f), Mathf.Max(scrollBody.height, visible * 24f + 4f));
            replayLogScroll = GUI.BeginScrollView(scrollBody, replayLogScroll, view);
            try
            {
                if (visible == 0)
                {
                    GUI.Label(new Rect(8f, Mathf.Max(4f, scrollBody.height * 0.5f - 12f), view.width - 16f, 24f), "NO EVENTS FOR SELECTED FILTERS", replayCenterStyle);
                }
                float y = 2f;
                for (int i = replayEvents.Count - 1; i >= 0; i--)
                {
                    ReplayEvent ev = replayEvents[i];
                    if (!ReplayEventVisible(ev)) continue;
                    if (replayFocusedPlayer != byte.MaxValue && ev.playerId != replayFocusedPlayer) continue;
                    if (((int)(y / 24f) & 1) == 0) ReplayFill(new Rect(0f, y, view.width, 23f), whiteMenuTheme ? new Color(0f, 0f, 0f, 0.035f) : new Color(1f, 1f, 1f, 0.035f));
                    GUI.color = ReplayEventColor(ev.type);
                    GUI.Label(new Rect(4f, y + 2f, 24f, 20f), ReplayEventShort(ev.type), replayCenterStyle);
                    GUI.color = Color.white;
                    GUI.Label(new Rect(32f, y + 2f, 58f, 20f), ReplayFormatTime(ev.t - replayStartTime), replaySmallStyle);
                    GUI.Label(new Rect(94f, y + 2f, view.width - 98f, 20f), ev.text ?? string.Empty, replayLogStyle);
                    y += 24f;
                }
            }
            finally
            {
                GUI.color = Color.white;
                GUI.EndScrollView();
            }

            HandleReplayResize(e, true, w, h);
            GUI.DragWindow(new Rect(0f, 0f, Mathf.Max(0f, w - 118f), 38f));
        }

        private static void DrawReplayTimelineTicks(Rect timeline)
        {
            if (!replayHasTime || replayEndTime <= replayStartTime) return;
            float width = Mathf.Max(1f, timeline.width - 8f);
            for (int i = 0; i < replayEvents.Count; i++)
            {
                ReplayEvent ev = replayEvents[i];
                if (!ReplayEventVisible(ev)) continue;
                float normalized = Mathf.Clamp01((ev.t - replayStartTime) / (replayEndTime - replayStartTime));
                float x = timeline.x + 4f + normalized * width;
                ReplayFill(new Rect(x - 1f, timeline.y + timeline.height - 7f, 2f, 7f), ReplayEventColor(ev.type));
            }
        }

        private static void DrawReplayFilters(Rect row, bool includePath = true)
        {
            string[] labels = includePath
                ? new[] { "KILL", "GA", "TASK", "VENT", "REP", "SHIFT", "SAB", "MEET", "OTHER", "PATH" }
                : new[] { "KILL", "GA", "TASK", "VENT", "REP", "SHIFT", "SAB", "MEET", "OTHER" };
            float gap = 3f;
            float bw = (row.width - gap * (labels.Length - 1)) / labels.Length;
            for (int i = 0; i < labels.Length; i++)
            {
                int bit = 1 << i;
                bool on = (replayFilterMask & bit) != 0;
                Color old = GUI.color;
                GUI.color = on ? GetMenuAccentColor(false) : new Color(0.55f, 0.58f, 0.65f, 1f);
                if (GUI.Button(new Rect(row.x + i * (bw + gap), row.y, bw, row.height), labels[i], replayButtonStyle))
                {
                    replayFilterMask ^= bit;
                    settingsDirty = true;
                }
                GUI.color = old;
            }
        }

        private static Rect FitTextureRect(Rect body, float texWidth, float texHeight)
        {
            float scale = Mathf.Min(body.width / Mathf.Max(1f, texWidth), body.height / Mathf.Max(1f, texHeight));
            float width = texWidth * scale;
            float height = texHeight * scale;
            return new Rect(body.x + (body.width - width) * 0.5f, body.y + (body.height - height) * 0.5f, width, height);
        }

        private static float ReplayViewTime()
        {
            if (!replayHasTime) return 0f;
            if (replayLive) return replayEndTime;
            return Mathf.Lerp(replayStartTime, replayEndTime, Mathf.Clamp01(replayScrub));
        }

        private static void DrawReplayPaths(ReplayMap map, Rect mapRect, float viewTime)
        {
            if ((replayFilterMask & ReplayPathFilterBit) == 0) return;
            float fromTime = replayOnlyLastSeconds ? viewTime - Mathf.Clamp(replaySeconds, 5f, 900f) : replayStartTime;
            foreach (var pair in replayPaths)
            {
                if (replayFocusedPlayer != byte.MaxValue && pair.Key != replayFocusedPlayer) continue;
                List<ReplayPoint> points = pair.Value;
                if (points == null || points.Count < 2) continue;
                int start = FindReplayStartIndex(points, fromTime);
                int end = FindReplayEndIndex(points, viewTime);
                if (end - start < 1) continue;
                int stride = Mathf.Max(1, (end - start) / 500);
                Color color = replayPlayers.TryGetValue(pair.Key, out ReplayPlayerState state) ? state.color : Color.white;
                color.a = 0.74f;
                int previous = start;
                for (int i = start + stride; i <= end; i += stride)
                {
                    Vector2 a = points[previous].pos;
                    Vector2 b = points[i].pos;
                    if ((b - a).sqrMagnitude <= 9f)
                        DrawReplayLine(ReplayPointOnMap(map, a, mapRect), ReplayPointOnMap(map, b, mapRect), color, 2f);
                    previous = i;
                }
                if (previous != end && (points[end].pos - points[previous].pos).sqrMagnitude <= 9f)
                    DrawReplayLine(ReplayPointOnMap(map, points[previous].pos, mapRect), ReplayPointOnMap(map, points[end].pos, mapRect), color, 2f);
            }
        }

        private static void DrawReplayEvents(ReplayMap map, Rect mapRect, float viewTime)
        {
            if (replayDrawIcons) InitReplayIconTextures();
            float fromTime = replayOnlyLastSeconds ? viewTime - Mathf.Clamp(replaySeconds, 5f, 900f) : replayStartTime;
            for (int i = 0; i < replayEvents.Count; i++)
            {
                ReplayEvent ev = replayEvents[i];
                if (ev.t < fromTime || ev.t > viewTime || !ReplayEventVisible(ev) || !ev.hasPosition) continue;
                if (replayFocusedPlayer != byte.MaxValue && ev.playerId != byte.MaxValue && ev.playerId != replayFocusedPlayer) continue;
                Vector2 p = ReplayPointOnMap(map, ev.pos, mapRect);
                if (replayDrawIcons && TryGetReplayEventIcon(ev, out Texture2D icon, out Vector2 iconPos))
                {
                    if (ev.type == ReplayEventType.Kill && ev.hasTargetPosition)
                        DrawReplayLine(p, ReplayPointOnMap(map, ev.targetPos, mapRect), ReplayEventColor(ev.type), 2f);
                    DrawReplayEventIcon(ReplayPointOnMap(map, iconPos, mapRect), icon);
                    continue;
                }
                if (ev.type == ReplayEventType.Kill && ev.hasTargetPosition)
                {
                    Vector2 target = ReplayPointOnMap(map, ev.targetPos, mapRect);
                    DrawReplayLine(p, target, ReplayEventColor(ev.type), 2f);
                    DrawReplayMarker(target, "X", ReplayEventColor(ev.type));
                }
                DrawReplayMarker(p, ReplayEventShort(ev.type), ReplayEventColor(ev.type));
            }
        }

        private static void DrawReplayPlayersAt(ReplayMap map, Rect mapRect, float viewTime)
        {
            if (replayDrawIcons) InitReplayIconTextures();
            foreach (var pair in replayPaths)
            {
                if (replayFocusedPlayer != byte.MaxValue && pair.Key != replayFocusedPlayer) continue;
                if (!TryReplayPositionAt(pair.Value, viewTime, out Vector2 pos)) continue;
                ReplayPlayerState state = replayPlayers.TryGetValue(pair.Key, out ReplayPlayerState found) ? found : null;
                Color color = state != null ? state.color : Color.white;
                Vector2 point = ReplayPointOnMap(map, pos, mapRect);
                if (replayDrawIcons && replayPlayerTex != null && state != null)
                    DrawReplayPlayerIcon(point, state, viewTime, 28f);
                else
                    DrawReplayDot(point, 9f, color);
            }
        }

        private static int FindReplayStartIndex(List<ReplayPoint> points, float time)
        {
            int low = 0, high = points.Count;
            while (low < high)
            {
                int mid = low + ((high - low) >> 1);
                if (points[mid].t < time) low = mid + 1; else high = mid;
            }
            return Mathf.Max(0, low - 1);
        }

        private static int FindReplayEndIndex(List<ReplayPoint> points, float time)
        {
            int low = 0, high = points.Count;
            while (low < high)
            {
                int mid = low + ((high - low) >> 1);
                if (points[mid].t <= time) low = mid + 1; else high = mid;
            }
            return Mathf.Clamp(low - 1, 0, points.Count - 1);
        }

        private static bool TryReplayPositionAt(List<ReplayPoint> points, float time, out Vector2 position)
        {
            position = default;
            if (points == null || points.Count == 0) return false;
            int end = FindReplayEndIndex(points, time);
            if (end >= points.Count - 1 || time <= points[end].t)
            {
                position = points[end].pos;
                return true;
            }
            ReplayPoint a = points[end];
            ReplayPoint b = points[end + 1];
            if ((b.pos - a.pos).sqrMagnitude > 9f || b.t <= a.t)
            {
                position = a.pos;
                return true;
            }
            position = Vector2.Lerp(a.pos, b.pos, Mathf.Clamp01((time - a.t) / (b.t - a.t)));
            return true;
        }

        private static Vector2 ReplayPointOnMap(ReplayMap map, Vector2 pos, Rect mapRect)
        {
            float referenceWidth = Mathf.Max(1f, map.tex.width * RadarMapReferenceScale);
            float referenceHeight = Mathf.Max(1f, map.tex.height * RadarMapReferenceScale);
            float nativeX = map.x + pos.x * map.scale;
            float nativeY = map.y - pos.y * map.scale;
            if (ShouldFlipReplaySkeld(map.id)) nativeX = referenceWidth - nativeX;
            return new Vector2(
                mapRect.x + nativeX * (mapRect.width / referenceWidth),
                mapRect.y + nativeY * (mapRect.height / referenceHeight));
        }

        private static bool ShouldFlipReplaySkeld(int mapId)
        {
            return mapId == 3 || (mapId == 0 && (flipSkeld || FlippedSkeld));
        }

        private static void DrawReplayPlayerIcon(Vector2 point, ReplayPlayerState state, float viewTime, float size)
        {
            Rect rect = new Rect(point.x - size * 0.5f, point.y - size * 0.5f, size, size);
            Rect shadow = new Rect(rect.x - 2f, rect.y - 2f, rect.width + 4f, rect.height + 4f);
            DrawReplayIconLayer(shadow, replayPlayerTex, new Color(0f, 0f, 0f, 0.88f));
            DrawReplayIconLayer(rect, replayPlayerTex, state.color);
            if (replayVisorTex != null)
                DrawReplayIconLayer(rect, replayVisorTex, seeRoles ? state.roleColor : new Color(0.72f, 0.86f, 0.96f, 1f));
            if (state.dead && viewTime >= state.deadAt && replayCrossTex != null)
                DrawReplayIconLayer(rect, replayCrossTex, Color.white);
        }

        private static bool TryGetReplayEventIcon(ReplayEvent ev, out Texture2D tex, out Vector2 pos)
        {
            tex = null;
            pos = ev.pos;
            switch (ev.type)
            {
                case ReplayEventType.Kill:
                    tex = replayKillTex;
                    if (ev.hasTargetPosition) pos = ev.targetPos;
                    break;
                case ReplayEventType.Vent:
                    tex = ev.text != null && ev.text.StartsWith("Exited", StringComparison.OrdinalIgnoreCase) ? replayVentOutTex : replayVentInTex;
                    break;
                case ReplayEventType.Task:
                    tex = replayTaskTex;
                    break;
                case ReplayEventType.Report:
                    tex = replayReportTex;
                    break;
            }
            return tex != null;
        }

        private static void DrawReplayEventIcon(Vector2 point, Texture2D tex)
        {
            const float size = 30f;
            Rect rect = new Rect(point.x - size * 0.5f, point.y - size * 0.5f, size, size);
            DrawReplayIconLayer(new Rect(rect.x - 2f, rect.y - 2f, rect.width + 4f, rect.height + 4f), tex, new Color(0f, 0f, 0f, 0.9f));
            DrawReplayIconLayer(rect, tex, Color.white);
        }

        private static void DrawReplayIconLayer(Rect rect, Texture2D tex, Color color)
        {
            if (tex == null) return;
            Color old = GUI.color;
            try
            {
                GUI.color = color;
                // This IL2CPP build has no GUI.DrawTexture overload.
                if (!replayIconStyles.TryGetValue(tex, out GUIStyle style) || style == null)
                {
                    style = new GUIStyle(GUIStyle.none);
                    style.normal.background = tex;
                    replayIconStyles[tex] = style;
                }
                GUI.Box(rect, GUIContent.none, style);
            }
            finally
            {
                GUI.color = old;
            }
        }

        private static void DrawReplayDot(Vector2 point, float size, Color color)
        {
            ReplayFill(new Rect(point.x - size * 0.5f - 1f, point.y - size * 0.5f - 1f, size + 2f, size + 2f), new Color(0f, 0f, 0f, 0.65f));
            ReplayFill(new Rect(point.x - size * 0.5f, point.y - size * 0.5f, size, size), color);
        }

        private static void DrawReplayMarker(Vector2 point, string label, Color color)
        {
            DrawReplayDot(point, 13f, color);
            Color old = GUI.color;
            GUI.color = Color.white;
            GUI.Label(new Rect(point.x - 8f, point.y - 8f, 16f, 16f), label, replayCenterStyle);
            GUI.color = old;
        }

        private static void DrawReplayLine(Vector2 a, Vector2 b, Color color, float width)
        {
            Vector2 delta = b - a;
            float length = delta.magnitude;
            if (length < 0.25f) return;
            Matrix4x4 oldMatrix = GUI.matrix;
            Color oldColor = GUI.color;
            GUIUtility.RotateAroundPivot(Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg, a);
            GUI.color = color;
            GUI.Box(new Rect(a.x, a.y - width * 0.5f, length, width), GUIContent.none, replayPxStyle);
            GUI.matrix = oldMatrix;
            GUI.color = oldColor;
        }

        private static void ReplayFill(Rect rect, Color color)
        {
            Color old = GUI.color;
            GUI.color = color;
            GUI.Box(rect, GUIContent.none, replayPxStyle);
            GUI.color = old;
        }

        private static void ReplayStroke(Rect rect, Color color)
        {
            ReplayFill(new Rect(rect.x, rect.y, rect.width, 1f), color);
            ReplayFill(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), color);
            ReplayFill(new Rect(rect.x, rect.y, 1f, rect.height), color);
            ReplayFill(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), color);
        }

        private static void HandleReplayResize(Event e, bool logWindow, float width, float height)
        {
            Rect handle = new Rect(width - 18f, height - 18f, 18f, 18f);
            GUI.Label(handle, "↘", replayCenterStyle);
            if (e == null) return;
            bool active = logWindow ? replayLogResizeActive : replayResizeActive;
            if (e.type == EventType.MouseDown && e.button == 0 && handle.Contains(e.mousePosition))
            {
                Vector2 screen = GUIUtility.GUIToScreenPoint(e.mousePosition);
                if (logWindow)
                {
                    replayLogResizeActive = true;
                    replayLogResizeStartMouse = screen;
                    replayLogResizeStartSize = new Vector2(width, height);
                    replayLogPendingWidth = width;
                    replayLogPendingHeight = height;
                }
                else
                {
                    replayResizeActive = true;
                    replayResizeStartMouse = screen;
                    replayResizeStartSize = new Vector2(width, height);
                    replayPendingWidth = width;
                    replayPendingHeight = height;
                }
                e.Use();
            }
            else if (active && e.type == EventType.MouseDrag)
            {
                Vector2 screen = GUIUtility.GUIToScreenPoint(e.mousePosition);
                if (logWindow)
                {
                    Vector2 delta = screen - replayLogResizeStartMouse;
                    replayLogPendingWidth = Mathf.Clamp(replayLogResizeStartSize.x + delta.x, 360f, Screen.width);
                    replayLogPendingHeight = Mathf.Clamp(replayLogResizeStartSize.y + delta.y, 220f, Screen.height);
                }
                else
                {
                    Vector2 delta = screen - replayResizeStartMouse;
                    replayPendingWidth = Mathf.Clamp(replayResizeStartSize.x + delta.x, 460f, Screen.width);
                    replayPendingHeight = Mathf.Clamp(replayResizeStartSize.y + delta.y, 320f, Screen.height);
                }
                settingsDirty = true;
                e.Use();
            }
            else if (active && e.type == EventType.MouseUp)
            {
                if (logWindow) replayLogResizeActive = false; else replayResizeActive = false;
                settingsDirty = true;
                e.Use();
            }
        }

        private static bool ReplayEventVisible(ReplayEvent ev)
        {
            return ev != null && (replayFilterMask & (1 << (int)ev.type)) != 0;
        }

        private static string ReplayEventLabel(ReplayEventType type)
        {
            switch (type)
            {
                case ReplayEventType.Kill: return "Kill";
                case ReplayEventType.Protect: return "Guardian protection";
                case ReplayEventType.Task: return "Task completed";
                case ReplayEventType.Vent: return "Vent";
                case ReplayEventType.Report: return "Report";
                case ReplayEventType.Shapeshift: return "Shapeshift";
                case ReplayEventType.Sabotage: return "Sabotage";
                case ReplayEventType.Meeting: return "Meeting";
                default: return "Event";
            }
        }

        private static string ReplayEventShort(ReplayEventType type)
        {
            switch (type)
            {
                case ReplayEventType.Kill: return "K";
                case ReplayEventType.Protect: return "P";
                case ReplayEventType.Task: return "T";
                case ReplayEventType.Vent: return "V";
                case ReplayEventType.Report: return "R";
                case ReplayEventType.Shapeshift: return "S";
                case ReplayEventType.Sabotage: return "!";
                case ReplayEventType.Meeting: return "M";
                default: return "•";
            }
        }

        private static Color ReplayEventColor(ReplayEventType type)
        {
            switch (type)
            {
                case ReplayEventType.Kill: return new Color(0.95f, 0.24f, 0.24f, 1f);
                case ReplayEventType.Protect: return new Color(0.25f, 0.90f, 0.50f, 1f);
                case ReplayEventType.Task: return new Color(0.25f, 0.80f, 1f, 1f);
                case ReplayEventType.Vent: return new Color(0.70f, 0.70f, 0.76f, 1f);
                case ReplayEventType.Report: return new Color(0.34f, 0.56f, 1f, 1f);
                case ReplayEventType.Shapeshift: return new Color(0.77f, 0.39f, 1f, 1f);
                case ReplayEventType.Sabotage: return new Color(1f, 0.49f, 0.12f, 1f);
                case ReplayEventType.Meeting: return new Color(1f, 0.84f, 0.24f, 1f);
                default: return Color.white;
            }
        }

        private static string ReplayFormatTime(float seconds)
        {
            seconds = Mathf.Max(0f, seconds);
            int minutes = (int)(seconds / 60f);
            int secs = (int)(seconds % 60f);
            return minutes + ":" + secs.ToString("00");
        }

        private static string ReplayTimelineLabel()
        {
            float total = Mathf.Max(0f, replayEndTime - replayStartTime);
            float current = Mathf.Clamp(ReplayViewTime() - replayStartTime, 0f, total);
            return ReplayFormatTime(current) + "/" + ReplayFormatTime(total);
        }

        private static void CycleReplayFocusedPlayer()
        {
            List<byte> ids = new List<byte>(replayPaths.Keys);
            ids.Sort();
            if (ids.Count == 0) { replayFocusedPlayer = byte.MaxValue; return; }
            if (replayFocusedPlayer == byte.MaxValue) { replayFocusedPlayer = ids[0]; return; }
            int index = ids.IndexOf(replayFocusedPlayer);
            replayFocusedPlayer = index < 0 || index >= ids.Count - 1 ? byte.MaxValue : ids[index + 1];
        }

        private static string ReplayFocusedPlayerLabel()
        {
            if (replayPlayers.TryGetValue(replayFocusedPlayer, out ReplayPlayerState state) && !string.IsNullOrWhiteSpace(state.name))
                return state.name.Length > 11 ? state.name.Substring(0, 11) : state.name;
            return "#" + replayFocusedPlayer;
        }

        private static ReplayMap GetReplayMap()
        {
            int mapId = replayPaths.Count > 0 || replayEvents.Count > 0 ? replayMapId : GetCurrentMapId();
            mapId = Mathf.Clamp(mapId, 0, 5);
            for (int i = 0; i < replayMaps.Length; i++)
            {
                ReplayMap map = replayMaps[i];
                if (map.id != mapId) continue;
                if (map.tex == null) map.tex = LoadReplayTex(map.res);
                if (map.tex != null && map.style == null)
                {
                    map.style = new GUIStyle(GUIStyle.none);
                    map.style.normal.background = map.tex;
                }
                return map.tex == null ? null : map;
            }
            return null;
        }

        private static Texture2D LoadReplayTex(string resource)
        {
            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                using (var stream = assembly.GetManifestResourceStream(resource))
                {
                    if (stream == null) return null;
                    byte[] data = new byte[stream.Length];
                    stream.Read(data, 0, data.Length);
                    Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (!texture.LoadImage(data)) return null;
                    texture.filterMode = FilterMode.Bilinear;
                    texture.wrapMode = TextureWrapMode.Clamp;
                    texture.hideFlags = HideFlags.HideAndDontSave;
                    return texture;
                }
            }
            catch { return null; }
        }

        private static void InitReplayGui()
        {
            if (replayWinStyle == null) replayWinStyle = new GUIStyle(GUIStyle.none);
            if (replayPxTex == null)
            {
                replayPxTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                replayPxTex.SetPixel(0, 0, Color.white);
                replayPxTex.Apply();
                replayPxTex.hideFlags = HideFlags.HideAndDontSave;
                replayPxStyle = new GUIStyle(GUIStyle.none);
                replayPxStyle.normal.background = replayPxTex;
            }
            if (replayHeaderStyle == null)
            {
                replayHeaderStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
                replaySmallStyle = new GUIStyle(GUI.skin.label) { fontSize = 11, alignment = TextAnchor.MiddleLeft, clipping = TextClipping.Clip };
                replayCenterStyle = new GUIStyle(GUI.skin.label) { fontSize = 10, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, clipping = TextClipping.Clip };
                replayLogStyle = new GUIStyle(GUI.skin.label) { fontSize = 11, alignment = TextAnchor.MiddleLeft, clipping = TextClipping.Clip };
                replayButtonStyle = new GUIStyle(GUI.skin.button) { fontSize = 10, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            }
            RefreshReplayThemeStyles();
            InitReplayIconTextures();
        }

        private static void RefreshReplayThemeStyles()
        {
            Color accent = GetMenuAccentColor(false);
            if (replayPanelTex != null && replayThemeLight == whiteMenuTheme &&
                Mathf.Abs(replayThemeAccent.r - accent.r) < 0.001f && Mathf.Abs(replayThemeAccent.g - accent.g) < 0.001f && Mathf.Abs(replayThemeAccent.b - accent.b) < 0.001f) return;

            DestroyMenuTexture(ref replayPanelTex);
            DestroyMenuTexture(ref replayCardTex);
            DestroyMenuTexture(ref replayButtonTex);
            DestroyMenuTexture(ref replayButtonActiveTex);
            replayThemeAccent = accent;
            replayThemeLight = whiteMenuTheme;

            Color panel = whiteMenuTheme ? new Color(0.97f, 0.97f, 0.97f, 0.98f) : new Color(0.12f, 0.12f, 0.12f, 0.98f);
            Color card = whiteMenuTheme ? new Color(1f, 1f, 1f, 0.90f) : new Color(1f, 1f, 1f, 0.055f);
            Color button = whiteMenuTheme ? new Color32(252, 247, 240, 255) : new Color(0.23f, 0.23f, 0.23f, 1f);
            Color text = whiteMenuTheme ? new Color(0.18f, 0.18f, 0.18f, 1f) : new Color(0.82f, 0.82f, 0.82f, 1f);

            replayPanelTex = MakeRoundedTex(64, panel, 12f);
            replayCardTex = MakeRoundedTex(64, card, 10f);
            replayButtonTex = MakeRoundedTex(64, button, 6f);
            replayButtonActiveTex = MakeRoundedTex(64, accent, 6f);
            replayPanelStyle = new GUIStyle(GUIStyle.none) { border = CreateRectOffset(12, 12, 12, 12) };
            replayPanelStyle.normal.background = replayPanelTex;
            replayCardStyle = new GUIStyle(GUIStyle.none) { border = CreateRectOffset(10, 10, 10, 10) };
            replayCardStyle.normal.background = replayCardTex;
            replayButtonStyle.normal.background = replayButtonTex;
            replayButtonStyle.hover.background = replayButtonTex;
            replayButtonStyle.active.background = replayButtonActiveTex;
            replayButtonStyle.border = CreateRectOffset(6, 6, 6, 6);
            replayButtonStyle.normal.textColor = text;
            replayButtonStyle.hover.textColor = whiteMenuTheme ? Color.black : Color.white;
            replayButtonStyle.active.textColor = Color.black;
            replayHeaderStyle.normal.textColor = accent;
            replaySmallStyle.normal.textColor = whiteMenuTheme ? new Color(0.35f, 0.35f, 0.35f, 1f) : new Color(0.66f, 0.66f, 0.66f, 1f);
            replayLogStyle.normal.textColor = text;
            replayCenterStyle.normal.textColor = text;
        }

        private static void InitReplayIconTextures()
        {
            if (replayPlayerTex == null) replayPlayerTex = LoadReplayTex("ElysiumModMenu.sicko_player.png");
            if (replayVisorTex == null) replayVisorTex = LoadReplayTex("ElysiumModMenu.sicko_player_visor.png");
            if (replayCrossTex == null) replayCrossTex = LoadReplayTex("ElysiumModMenu.sicko_cross.png");
            if (replayBodyTex == null) replayBodyTex = LoadReplayTex("ElysiumModMenu.sicko_dead_body.png");
            if (replayKillTex == null) replayKillTex = LoadReplayTex("ElysiumModMenu.sicko_kill.png");
            if (replayReportTex == null) replayReportTex = LoadReplayTex("ElysiumModMenu.sicko_report.png");
            if (replayTaskTex == null) replayTaskTex = LoadReplayTex("ElysiumModMenu.sicko_task.png");
            if (replayVentInTex == null) replayVentInTex = LoadReplayTex("ElysiumModMenu.sicko_vent_in.png");
            if (replayVentOutTex == null) replayVentOutTex = LoadReplayTex("ElysiumModMenu.sicko_vent_out.png");
        }

        private static Color GetReplayPlayerColor(PlayerControl player)
        {
            try
            {
                int colorId = player.Data.DefaultOutfit.ColorId;
                if (Palette.PlayerColors != null && colorId >= 0 && colorId < Palette.PlayerColors.Length)
                    return Palette.PlayerColors[colorId];
            }
            catch (global::System.Exception __elysiumCaught190) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught190); }
            return Color.white;
        }

        private static Color GetReplayRoleColor(PlayerControl player)
        {
            try
            {
                if (player.Data.Role != null) return GetRoleColor((int)player.Data.Role.Role, player.Data.Role.TeamColor);
            }
            catch (global::System.Exception __elysiumCaught191) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught191); }
            return Color.black;
        }

        private static string ReplayPlayerName(PlayerControl player)
        {
            try
            {
                if (player != null && player.Data != null && !string.IsNullOrWhiteSpace(player.Data.PlayerName)) return player.Data.PlayerName;
            }
            catch (global::System.Exception __elysiumCaught192) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught192); }
            return player != null ? "Player " + player.PlayerId : "System";
        }

        private static bool IsReplayImp(PlayerControl player)
        {
            try
            {
                if (player == null || player.Data == null) return false;
                if (player.Data.Role != null && player.Data.Role.IsImpostor) return true;
                return RoleManager.IsImpostorRole(player.Data.RoleType);
            }
            catch { return false; }
        }

        private static void DrawReplayOverlayOnRadar(RadarMap map, float pad)
        {
            if (!replayOverlayOnRadar || map == null || !replayHasTime) return;
            InitReplayGui();
            float viewTime = ReplayViewTime();
            float fromTime = replayOnlyLastSeconds ? viewTime - Mathf.Clamp(replaySeconds, 5f, 900f) : replayStartTime;
            if ((replayFilterMask & ReplayPathFilterBit) != 0)
            {
                foreach (var pair in replayPaths)
                {
                    if (replayFocusedPlayer != byte.MaxValue && pair.Key != replayFocusedPlayer) continue;
                    List<ReplayPoint> points = pair.Value;
                    if (points == null || points.Count < 2) continue;
                    int start = FindReplayStartIndex(points, fromTime);
                    int end = FindReplayEndIndex(points, viewTime);
                    int stride = Mathf.Max(1, (end - start) / 300);
                    Color color = replayPlayers.TryGetValue(pair.Key, out ReplayPlayerState state) ? state.color : Color.white;
                    color.a = 0.72f;
                    int previous = start;
                    for (int i = start + stride; i <= end; i += stride)
                    {
                        if ((points[i].pos - points[previous].pos).sqrMagnitude <= 9f)
                            DrawReplayLine(RadarPoint(map, points[previous].pos, pad), RadarPoint(map, points[i].pos, pad), color, 2f);
                        previous = i;
                    }
                }
            }
            for (int i = 0; i < replayEvents.Count; i++)
            {
                ReplayEvent ev = replayEvents[i];
                if (ev.t < fromTime || ev.t > viewTime || !ReplayEventVisible(ev) || !ev.hasPosition) continue;
                DrawRadarGlyph(RadarPoint(map, ev.pos, pad), ReplayEventColor(ev.type), ReplayEventShort(ev.type));
            }
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer), new Type[] { typeof(PlayerControl), typeof(MurderResultFlags) })]
    internal static class ReplayKillEventPatch
    {
        public static void Postfix(PlayerControl __instance, PlayerControl target)
        {
            if (__instance == null || target == null || target.Data == null || !target.Data.IsDead) return;
            ElysiumModMenuGUI.RecordReplayEvent(ElysiumModMenuGUI.ReplayEventType.Kill, __instance, target,
                "Kill: " + ReplayPatchName(__instance) + " -> " + ReplayPatchName(target));
        }
        private static string ReplayPatchName(PlayerControl pc) => pc != null && pc.Data != null ? pc.Data.PlayerName : "Unknown";
    }

    [HarmonyPatch(typeof(Vent), nameof(Vent.EnterVent))]
    internal static class ReplayVentEnterEventPatch
    {
        public static void Postfix([HarmonyArgument(0)] PlayerControl player)
        {
            if (player == null) return;
            ElysiumModMenuGUI.RecordReplayEvent(ElysiumModMenuGUI.ReplayEventType.Vent, player, null,
                "Entered vent: " + (player.Data != null ? player.Data.PlayerName : "Unknown"));
        }
    }

    [HarmonyPatch(typeof(Vent), nameof(Vent.ExitVent))]
    internal static class ReplayVentExitEventPatch
    {
        public static void Postfix([HarmonyArgument(0)] PlayerControl player)
        {
            if (player == null) return;
            ElysiumModMenuGUI.RecordReplayEvent(ElysiumModMenuGUI.ReplayEventType.Vent, player, null,
                "Exited vent: " + (player.Data != null ? player.Data.PlayerName : "Unknown"));
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Shapeshift))]
    internal static class ReplayShapeshiftEventPatch
    {
        public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
        {
            if (__instance == null || target == null) return;
            string a = __instance.Data != null ? __instance.Data.PlayerName : "Unknown";
            string b = target.Data != null ? target.Data.PlayerName : "Unknown";
            ElysiumModMenuGUI.RecordReplayEvent(ElysiumModMenuGUI.ReplayEventType.Shapeshift, __instance, target,
                __instance == target ? "Unshifted: " + a : "Shapeshift: " + a + " -> " + b);
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.ProtectPlayer))]
    internal static class ReplayProtectEventPatch
    {
        public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
        {
            if (__instance == null || target == null) return;
            string a = __instance.Data != null ? __instance.Data.PlayerName : "Unknown";
            string b = target.Data != null ? target.Data.PlayerName : "Unknown";
            ElysiumModMenuGUI.RecordReplayEvent(ElysiumModMenuGUI.ReplayEventType.Protect, __instance, target,
                "Guardian protection: " + a + " -> " + b);
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CompleteTask))]
    internal static class ReplayTaskEventPatch
    {
        public static void Postfix(PlayerControl __instance)
        {
            if (__instance == null) return;
            ElysiumModMenuGUI.RecordReplayEvent(ElysiumModMenuGUI.ReplayEventType.Task, __instance, null,
                "Task completed: " + (__instance.Data != null ? __instance.Data.PlayerName : "Unknown"));
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CmdReportDeadBody))]
    internal static class ReplayReportEventPatch
    {
        public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] NetworkedPlayerInfo target)
        {
            if (__instance == null) return;
            string reporter = __instance.Data != null ? __instance.Data.PlayerName : "Unknown";
            string reported = target != null ? target.PlayerName : "Emergency";
            ElysiumModMenuGUI.RecordReplayEvent(ElysiumModMenuGUI.ReplayEventType.Report, __instance, null,
                "Report: " + reporter + " (" + reported + ")");
        }
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    internal static class ReplayMeetingEventPatch
    {
        public static void Postfix()
        {
            ElysiumModMenuGUI.RecordReplayEvent(ElysiumModMenuGUI.ReplayEventType.Meeting, null, null, "Meeting started");
        }
    }

    [HarmonyPatch(typeof(ExileController), nameof(ExileController.Begin))]
    internal static class ReplayExileEventPatch
    {
        public static void Postfix(ExileController __instance)
        {
            if (__instance == null) return;
            try
            {
                NetworkedPlayerInfo exiled = __instance.initData.networkedPlayer;
                ElysiumModMenuGUI.RecordReplayEvent(ElysiumModMenuGUI.ReplayEventType.Meeting, null, null,
                    "Ejected: " + (exiled != null ? exiled.PlayerName : "No one"));
            }
            catch (global::System.Exception __elysiumCaught193) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught193); }
        }
    }
}
