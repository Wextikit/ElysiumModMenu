#nullable disable
#pragma warning disable CS0162, CS0108, CS0219, CS0661, CS0660, CS8632, CS0168, CS0659
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ElysiumModMenu
{
    public partial class ElysiumModMenuGUI : MonoBehaviour
    {
        public static bool showRadar = false;
        public static bool realisticRadar = false;
        public static bool showRadarDeadBodies = false;
        public static bool showRadarGhosts = true;
        public static bool radarRightClickTp = false;
        public static bool hideRadarInMeeting = true;
        public static bool radarDrawIcons = false;
        public static bool lockRadar = false;
        public static bool radarBorder = false;
        public static float radarScale = 1f;
        public static float radarAlpha = 0.78f;
        public static Rect radarRect = new Rect(15f, 90f, 220f, 180f);

        private const int RadarWindowId = 843207;
        // Calibrated on the original half-size maps; embedded PNGs are 2x.
        private const float RadarMapReferenceScale = 0.5f;
        private static GUIStyle radarWinStyle;
        private static GUIStyle radarDotLabelStyle;
        private static Texture2D radarPixelTex;
        private static GUIStyle radarPixelStyle;
        private static readonly Action<int> drawRadarWindow = DrawRadarWindow;
        private static float radarNextTpAt;
        private static readonly List<Bounds> radarWorldRooms = new List<Bounds>();
        private static Vector2 radarWorldMin;
        private static Vector2 radarWorldMax;
        private static int radarWorldMap = -1;

        private sealed class RadarMap
        {
            public int id;
            public string res;
            public float x;
            public float y;
            public float scale;
            public Texture2D tex;
            public GUIStyle style;
        }

        private static readonly RadarMap[] radarMaps =
        {
            new RadarMap { id = 0, res = "ElysiumModMenu.radar_skeld.png", x = 277f, y = 77f, scale = 11.5f },
            new RadarMap { id = 1, res = "ElysiumModMenu.radar_mira_hq.png", x = 115f, y = 240f, scale = 9.25f },
            new RadarMap { id = 2, res = "ElysiumModMenu.radar_polus.png", x = 8f, y = 21f, scale = 10f },
            new RadarMap { id = 3, res = "ElysiumModMenu.radar_skeld.png", x = 277f, y = 77f, scale = 11.5f },
            new RadarMap { id = 4, res = "ElysiumModMenu.radar_airship.png", x = 162f, y = 107f, scale = 6f },
            new RadarMap { id = 5, res = "ElysiumModMenu.radar_fungle.png", x = 237f, y = 140f, scale = 8.5f }
        };

        private static void DrawVisualRadar()
        {
            if (!showRadar) return;
            if (!RadarCanDraw()) return;

            RadarMap map = GetRadarMap();
            if (map == null) return;

            InitRadarGui();
            FitRadarRect(map);
            Vector2 oldPos = new Vector2(radarRect.x, radarRect.y);
            Color old = GUI.color;
            try
            {
                GUI.color = Color.white;
                radarRect = GUI.Window(RadarWindowId, radarRect, drawRadarWindow, "", radarWinStyle);
            }
            catch (global::System.Exception __elysiumCaught173) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught173); }
            finally
            {
                GUI.color = old;
            }
            ClampRadarRect();
            if (oldPos.x != radarRect.x || oldPos.y != radarRect.y)
                settingsDirty = true;
        }

        private static bool RadarCanDraw()
        {
            if (hideRadarInMeeting && (MeetingHud.Instance != null || ExileController.Instance != null || IntroCutscene.Instance != null))
                return false;

            if (MapBehaviour.Instance != null && MapBehaviour.Instance.IsOpen)
                return false;

            if (PlayerControl.LocalPlayer == null || PlayerControl.AllPlayerControls == null)
                return false;

            try
            {
                return AmongUsClient.Instance != null &&
                       (AmongUsClient.Instance.GameState == InnerNet.InnerNetClient.GameStates.Joined ||
                        AmongUsClient.Instance.GameState == InnerNet.InnerNetClient.GameStates.Started ||
                        AmongUsClient.Instance.IsGameStarted);
            }
            catch { return false; }
        }

        private static RadarMap GetRadarMap()
        {
            int id = 0;
            try { id = Mathf.Clamp(GetCurrentMapId(), 0, 5); } catch (global::System.Exception __elysiumCaught174) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught174); }
            RadarMap[] maps = radarMaps;
            for (int i = 0; i < maps.Length; i++)
            {
                if (maps[i].id != id) continue;
                if (maps[i].tex == null)
                    maps[i].tex = LoadRadarTex(maps[i].res);
                if (maps[i].tex != null && maps[i].style == null)
                {
                    maps[i].style = new GUIStyle(GUIStyle.none);
                    maps[i].style.normal.background = maps[i].tex;
                }
                return maps[i].tex == null ? null : maps[i];
            }
            return null;
        }

        private static Texture2D LoadRadarTex(string res)
        {
            try
            {
                Assembly asm = Assembly.GetExecutingAssembly();
                using (var s = asm.GetManifestResourceStream(res))
                {
                    if (s == null) return null;
                    byte[] buf = new byte[s.Length];
                    s.Read(buf, 0, buf.Length);
                    Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (!tex.LoadImage(buf)) return null;
                    tex.hideFlags = HideFlags.HideAndDontSave;
                    return tex;
                }
            }
            catch { return null; }
        }

        private static void InitRadarGui()
        {
            if (radarWinStyle == null)
                radarWinStyle = new GUIStyle(GUIStyle.none);

            if (radarDotLabelStyle == null)
                radarDotLabelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, richText = false };

            if (radarPixelTex == null)
            {
                radarPixelTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                radarPixelTex.SetPixel(0, 0, Color.white);
                radarPixelTex.Apply();
                radarPixelTex.hideFlags = HideFlags.HideAndDontSave;
                radarPixelStyle = new GUIStyle(GUIStyle.none);
                radarPixelStyle.normal.background = radarPixelTex;
            }

            if (radarDrawIcons)
                InitReplayIconTextures();

        }

        private static void FitRadarRect(RadarMap map)
        {
            radarScale = Mathf.Clamp(radarScale, 0.65f, 1.6f);
            radarAlpha = Mathf.Clamp(radarAlpha, 0.2f, 1f);

            float w = Mathf.Max(120f, map.tex.width * 0.5f * radarScale + 10f);
            float h = Mathf.Max(90f, map.tex.height * 0.5f * radarScale + 10f);
            radarRect.width = w;
            radarRect.height = h;
        }

        private static void ClampRadarRect()
        {
            radarRect.x = Mathf.Clamp(radarRect.x, 0f, Mathf.Max(0f, Screen.width - radarRect.width));
            radarRect.y = Mathf.Clamp(radarRect.y, 0f, Mathf.Max(0f, Screen.height - radarRect.height));
        }

        private static void DrawRadarWindow(int id)
        {
            RadarMap map = GetRadarMap();
            if (map == null) return;

            float pad = 5f;
            Rect img = GetRadarMapRect(map);
            Color old = GUI.color;
            try
            {
                if (realisticRadar)
                    DrawRadarWorldRooms(img);
                else
                {
                    GUI.color = new Color(1f, 1f, 1f, radarAlpha);
                    if (map.style != null) GUI.Box(img, GUIContent.none, map.style);
                }
                GUI.color = Color.white;

                if (radarBorder)
                {
                    Color accent = GetMenuAccentColor(false);
                    RadarStroke(img, new Color(accent.r, accent.g, accent.b, 0.92f));
                }

                // Radar is live-only; replay paths and icons stay in replay.
                DrawRadarPlayers(map, pad);
                if (showRadarDeadBodies) DrawRadarBodies(map, pad);
                if (radarRightClickTp) RadarClickTp(map, pad);
            }
            catch (global::System.Exception __elysiumCaught175) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught175); }
            finally
            {
                GUI.color = old;
            }

            if (!lockRadar)
                GUI.DragWindow(new Rect(0f, 0f, radarRect.width, radarRect.height));
        }

        private static Rect GetRadarMapRect(RadarMap map)
        {
            return new Rect(5f, 5f, map.tex.width * 0.5f * radarScale, map.tex.height * 0.5f * radarScale);
        }

        private static void DrawRadarPlayers(RadarMap map, float pad)
        {
            if (radarDrawIcons) InitReplayIconTextures();
            try
            {
                foreach (PlayerControl pc in PlayerControl.AllPlayerControls)
                {
                    try
                    {
                        if (pc == null || pc.Data == null || pc.Data.Disconnected) continue;
                        if (pc.Data.IsDead && !showRadarGhosts) continue;

                        Vector2 p = RadarPoint(map, pc.GetTruePosition(), pad);
                        if (!RadarInside(map, p)) continue;

                        Color c = GetRadarPlayerColor(pc);
                        if (radarDrawIcons && replayPlayerTex != null)
                            DrawRadarPlayerIcon(p, pc, c);
                        else
                        {
                            bool sq = IsRadarImp(pc) && (seeRoles || IsRadarLocalImp());
                            DrawRadarGlyph(p, c, sq ? "■" : "●");
                        }
                    }
                    catch (global::System.Exception __elysiumCaught176) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught176); }
                }
            }
            catch (global::System.Exception __elysiumCaught177) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught177); }
        }

        private static void DrawRadarBodies(RadarMap map, float pad)
        {
            DeadBody[] bodies = null;
            try { bodies = Object.FindObjectsOfType<DeadBody>(); } catch (global::System.Exception __elysiumCaught178) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught178); }
            if (bodies == null) return;

            foreach (DeadBody body in bodies)
            {
                try
                {
                    if (body == null) continue;
                    Vector2 p = RadarPoint(map, body.TruePosition, pad);
                    if (!RadarInside(map, p)) continue;
                    Color color = GetRadarBodyColor(body);
                    if (radarDrawIcons && replayBodyTex != null)
                    {
                        float size = 28f * radarScale;
                        Rect rect = new Rect(p.x - size * 0.5f, p.y - size * 0.5f, size, size);
                        DrawReplayIconLayer(new Rect(rect.x - 2f, rect.y - 2f, rect.width + 4f, rect.height + 4f), replayBodyTex, new Color(0f, 0f, 0f, 0.88f));
                        DrawReplayIconLayer(rect, replayBodyTex, color);
                    }
                    else
                        DrawRadarGlyph(p, color, "✖");
                }
                catch (global::System.Exception __elysiumCaught179) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught179); }
            }
        }

        private static Vector2 RadarPoint(RadarMap map, Vector2 pos, float pad)
        {
            Rect img = GetRadarMapRect(map);
            if (realisticRadar && EnsureRadarWorldRooms())
                return RadarWorldPoint(pos, img);

            float referenceWidth = Mathf.Max(1f, map.tex.width * RadarMapReferenceScale);
            float referenceHeight = Mathf.Max(1f, map.tex.height * RadarMapReferenceScale);
            float nativeX = map.x + pos.x * map.scale;
            if (map.id == 3 || (map.id == 0 && (flipSkeld || FlippedSkeld))) nativeX = referenceWidth - nativeX;
            float x = img.x + nativeX * (img.width / referenceWidth);
            float y = img.y + (map.y - pos.y * map.scale) * (img.height / referenceHeight);
            return new Vector2(x, y);
        }

        private static bool RadarInside(RadarMap map, Vector2 p)
        {
            return GetRadarMapRect(map).Contains(p);
        }

        private static bool EnsureRadarWorldRooms()
        {
            int map = GetCurrentMapId();
            if (radarWorldMap == map && radarWorldRooms.Count > 0)
                return true;

            radarWorldMap = map;
            radarWorldRooms.Clear();
            radarWorldMin = new Vector2(float.MaxValue, float.MaxValue);
            radarWorldMax = new Vector2(float.MinValue, float.MinValue);

            try
            {
                if (ShipStatus.Instance == null || ShipStatus.Instance.AllRooms == null)
                    return false;

                var rooms = ShipStatus.Instance.AllRooms;
                for (int i = 0; i < rooms.Length; i++)
                {
                    Collider2D col = rooms[i] != null ? rooms[i].roomArea : null;
                    if (col == null) continue;
                    Bounds bounds = col.bounds;
                    if (bounds.size.x <= 0.01f || bounds.size.y <= 0.01f) continue;
                    radarWorldRooms.Add(bounds);
                    GrowRadarWorldBounds(new Vector2(bounds.min.x, bounds.min.y));
                    GrowRadarWorldBounds(new Vector2(bounds.max.x, bounds.max.y));
                }
            }
            catch
            {
                radarWorldRooms.Clear();
                return false;
            }

            if (radarWorldRooms.Count == 0 || radarWorldMin.x > radarWorldMax.x)
                return false;

            Vector2 size = radarWorldMax - radarWorldMin;
            Vector2 margin = size * 0.035f + Vector2.one * 0.35f;
            radarWorldMin -= margin;
            radarWorldMax += margin;
            return true;
        }

        private static void GrowRadarWorldBounds(Vector2 point)
        {
            if (point.x < radarWorldMin.x) radarWorldMin.x = point.x;
            if (point.y < radarWorldMin.y) radarWorldMin.y = point.y;
            if (point.x > radarWorldMax.x) radarWorldMax.x = point.x;
            if (point.y > radarWorldMax.y) radarWorldMax.y = point.y;
        }

        private static Vector2 RadarWorldPoint(Vector2 world, Rect img)
        {
            float x = (world.x - radarWorldMin.x) / Mathf.Max(0.01f, radarWorldMax.x - radarWorldMin.x);
            float y = (world.y - radarWorldMin.y) / Mathf.Max(0.01f, radarWorldMax.y - radarWorldMin.y);
            return new Vector2(img.x + x * img.width, img.y + (1f - y) * img.height);
        }

        private static void DrawRadarWorldRooms(Rect img)
        {
            if (!EnsureRadarWorldRooms()) return;

            Color accent = GetMenuAccentColor(false);
            RadarFill(img, new Color(0.035f, 0.04f, 0.055f, 0.82f * radarAlpha));
            for (int i = 0; i < radarWorldRooms.Count; i++)
            {
                Bounds bounds = radarWorldRooms[i];
                Vector2 a = RadarWorldPoint(new Vector2(bounds.min.x, bounds.min.y), img);
                Vector2 b = RadarWorldPoint(new Vector2(bounds.max.x, bounds.max.y), img);
                Rect room = new Rect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Abs(b.x - a.x), Mathf.Abs(b.y - a.y));
                RadarFill(room, new Color(0.42f, 0.46f, 0.54f, 0.34f * radarAlpha));
                RadarStroke(room, new Color(accent.r, accent.g, accent.b, 0.72f * radarAlpha));
            }
        }

        private static void RadarFill(Rect rect, Color color)
        {
            Color old = GUI.color;
            GUI.color = color;
            GUI.Box(rect, GUIContent.none, radarPixelStyle);
            GUI.color = old;
        }

        private static void RadarStroke(Rect rect, Color color)
        {
            RadarFill(new Rect(rect.x, rect.y, rect.width, 1f), color);
            RadarFill(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), color);
            RadarFill(new Rect(rect.x, rect.y, 1f, rect.height), color);
            RadarFill(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), color);
        }

        private static void DrawRadarGlyph(Vector2 p, Color c, string glyph)
        {
            float sz = 20f * radarScale;
            radarDotLabelStyle.fontSize = Mathf.Max(10, Mathf.RoundToInt(18f * radarScale));
            Rect r = new Rect(p.x - sz * 0.5f, p.y - sz * 0.5f, sz, sz);

            Color old = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.9f);
            GUI.Label(new Rect(r.x + 1f, r.y + 1f, r.width, r.height), glyph, radarDotLabelStyle);
            GUI.color = new Color(c.r, c.g, c.b, 1f);
            GUI.Label(r, glyph, radarDotLabelStyle);
            GUI.color = old;
        }

        private static void DrawRadarPlayerIcon(Vector2 point, PlayerControl pc, Color color)
        {
            float size = 24f * radarScale;
            Rect rect = new Rect(point.x - size * 0.5f, point.y - size * 0.5f, size, size);
            DrawReplayIconLayer(rect, replayPlayerTex, color);
            if (replayVisorTex != null)
            {
                Color visor = new Color(0.72f, 0.86f, 0.96f, 1f);
                try
                {
                    if (seeRoles && pc.Data != null && pc.Data.Role != null)
                        visor = GetRoleColor((int)pc.Data.Role.Role, pc.Data.Role.TeamColor);
                }
                catch (global::System.Exception __elysiumCaught180) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught180); }
                DrawReplayIconLayer(rect, replayVisorTex, visor);
            }
            if (pc.Data != null && pc.Data.IsDead && replayCrossTex != null)
                DrawReplayIconLayer(rect, replayCrossTex, Color.white);
        }

        private static Color GetRadarPlayerColor(PlayerControl pc)
        {
            try
            {
                int cid = pc.Data.DefaultOutfit.ColorId;
                if (Palette.PlayerColors != null && cid >= 0 && cid < Palette.PlayerColors.Length)
                    return Palette.PlayerColors[cid];
            }
            catch (global::System.Exception __elysiumCaught181) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught181); }
            return Color.white;
        }

        private static Color GetRadarBodyColor(DeadBody body)
        {
            try
            {
                byte pid = GetBodyParentId(body);
                if (GameData.Instance != null)
                {
                    NetworkedPlayerInfo info = GameData.Instance.GetPlayerById(pid);
                    if (info != null)
                    {
                        int cid = info.DefaultOutfit.ColorId;
                        if (Palette.PlayerColors != null && cid >= 0 && cid < Palette.PlayerColors.Length)
                            return Palette.PlayerColors[cid];
                    }
                }
            }
            catch (global::System.Exception __elysiumCaught182) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught182); }
            return new Color(1f, 0.25f, 0.25f, 1f);
        }

        private static byte GetBodyParentId(DeadBody body)
        {
            if (body == null) return byte.MaxValue;
            const BindingFlags f = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            try
            {
                FieldInfo fi = body.GetType().GetField("ParentId", f);
                if (fi != null) return Convert.ToByte(fi.GetValue(body));
            }
            catch (global::System.Exception __elysiumCaught183) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught183); }

            try
            {
                PropertyInfo pi = body.GetType().GetProperty("ParentId", f);
                if (pi != null) return Convert.ToByte(pi.GetValue(body, null));
            }
            catch (global::System.Exception __elysiumCaught184) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught184); }

            return byte.MaxValue;
        }

        private static bool IsRadarLocalImp()
        {
            return IsRadarImp(PlayerControl.LocalPlayer);
        }

        private static bool IsRadarImp(PlayerControl pc)
        {
            try
            {
                if (pc == null || pc.Data == null) return false;
                if (pc.Data.Role != null && pc.Data.Role.IsImpostor) return true;
                return RoleManager.IsImpostorRole(pc.Data.RoleType);
            }
            catch { return false; }
        }

        private static void RadarClickTp(RadarMap map, float pad)
        {
            Event e = Event.current;
            if (e == null || e.button != 1) return;
            if (e.type != EventType.MouseDown && e.type != EventType.MouseDrag) return;
            if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.NetTransform == null) return;
            if (e.shift || e.control || e.alt) return;

            if (e.type == EventType.MouseDrag && Time.unscaledTime < radarNextTpAt) return;
            radarNextTpAt = Time.unscaledTime + 0.1f;

            Rect img = GetRadarMapRect(map);
            Vector2 m = e.mousePosition;
            if (!img.Contains(m)) return;

            if (realisticRadar && EnsureRadarWorldRooms())
            {
                float x = Mathf.Clamp01((m.x - img.x) / img.width);
                float y = 1f - Mathf.Clamp01((m.y - img.y) / img.height);
                Vector2 world = new Vector2(
                    Mathf.Lerp(radarWorldMin.x, radarWorldMax.x, x),
                    Mathf.Lerp(radarWorldMin.y, radarWorldMax.y, y));
                try { PlayerControl.LocalPlayer.NetTransform.RpcSnapTo(world); }
                catch { try { PlayerControl.LocalPlayer.NetTransform.SnapTo(world); } catch (global::System.Exception __elysiumCaught185) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught185); } }
                e.Use();
                return;
            }

            float referenceWidth = Mathf.Max(1f, map.tex.width * RadarMapReferenceScale);
            float referenceHeight = Mathf.Max(1f, map.tex.height * RadarMapReferenceScale);
            float nativeX = (m.x - img.x) * referenceWidth / Mathf.Max(1f, img.width);
            float nativeY = (m.y - img.y) * referenceHeight / Mathf.Max(1f, img.height);
            if (map.id == 3 || (map.id == 0 && (flipSkeld || FlippedSkeld))) nativeX = referenceWidth - nativeX;
            Vector2 target = new Vector2(
                (nativeX - map.x) / map.scale,
                (map.y - nativeY) / map.scale);

            try { PlayerControl.LocalPlayer.NetTransform.RpcSnapTo(target); }
            catch { try { PlayerControl.LocalPlayer.NetTransform.SnapTo(target); } catch (global::System.Exception __elysiumCaught186) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught186); } }

            e.Use();
        }
    }
}
