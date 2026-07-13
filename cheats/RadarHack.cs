#nullable disable
#pragma warning disable CS0162, CS0108, CS0219, CS0661, CS0660, CS8632, CS0168, CS0659
using System;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ElysiumModMenu
{
    public partial class ElysiumModMenuGUI : MonoBehaviour
    {
        public static bool showRadar = false;
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
        private static GUIStyle radarWinStyle;
        private static GUIStyle radarDotLabelStyle;
        private static float radarNextTpAt;

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
                radarRect = GUI.Window(RadarWindowId, radarRect, (Action<int>)DrawRadarWindow, "", radarWinStyle);
            }
            catch { }
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

            if (PlayerControl.LocalPlayer == null || PlayerControl.AllPlayerControls == null)
                return false;

            try
            {
                return AmongUsClient.Instance != null &&
                       (AmongUsClient.Instance.GameState == InnerNet.InnerNetClient.GameStates.Joined ||
                        AmongUsClient.Instance.GameState == InnerNet.InnerNetClient.GameStates.Started ||
                        AmongUsClient.Instance.IsGameStarted);
            }
            catch { return true; }
        }

        private static RadarMap GetRadarMap()
        {
            int id = 0;
            try { id = Mathf.Clamp(GetCurrentMapId(), 0, 5); } catch { }
            for (int i = 0; i < radarMaps.Length; i++)
            {
                if (radarMaps[i].id != id) continue;
                if (radarMaps[i].tex == null)
                    radarMaps[i].tex = LoadRadarTex(radarMaps[i].res);
                if (radarMaps[i].tex != null && radarMaps[i].style == null)
                {
                    radarMaps[i].style = new GUIStyle(GUIStyle.none);
                    radarMaps[i].style.normal.background = radarMaps[i].tex;
                }
                return radarMaps[i].tex == null ? null : radarMaps[i];
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
            Color old = GUI.color;
            try
            {
                Rect img = new Rect(pad, pad, map.tex.width * 0.5f * radarScale, map.tex.height * 0.5f * radarScale);
                GUI.color = new Color(1f, 1f, 1f, radarAlpha);
                if (map.style != null) GUI.Box(img, GUIContent.none, map.style);

                DrawRadarPlayers(map, pad);
                if (showRadarDeadBodies) DrawRadarBodies(map, pad);
                if (radarRightClickTp) RadarClickTp(map, pad);
            }
            catch { }
            finally
            {
                GUI.color = old;
            }

            if (!lockRadar)
                GUI.DragWindow(new Rect(0f, 0f, radarRect.width, radarRect.height));
        }

        private static void DrawRadarPlayers(RadarMap map, float pad)
        {
            try
            {
                foreach (PlayerControl pc in PlayerControl.AllPlayerControls)
                {
                    try
                    {
                        if (pc == null || pc.Data == null || pc.Data.Disconnected) continue;
                        if (pc.Data.IsDead && !showRadarGhosts) continue;

                        Vector2 p = RadarPoint(map, pc.GetTruePosition(), pad);
                        if (!RadarInside(p)) continue;

                        Color c = GetRadarPlayerColor(pc);
                        bool sq = IsRadarImp(pc) && (seeRoles || IsRadarLocalImp());
                        DrawRadarGlyph(p, c, sq ? "■" : "●");
                    }
                    catch { }
                }
            }
            catch { }
        }

        private static void DrawRadarBodies(RadarMap map, float pad)
        {
            DeadBody[] bodies = null;
            try { bodies = Object.FindObjectsOfType<DeadBody>(); } catch { }
            if (bodies == null) return;

            foreach (DeadBody body in bodies)
            {
                try
                {
                    if (body == null) continue;
                    Vector2 p = RadarPoint(map, body.TruePosition, pad);
                    if (!RadarInside(p)) continue;
                    DrawRadarGlyph(p, GetRadarBodyColor(body), "✖");
                }
                catch { }
            }
        }

        private static Vector2 RadarPoint(RadarMap map, Vector2 pos, float pad)
        {
            float x = (map.x + pos.x * map.scale) * radarScale + pad;
            float y = (map.y - pos.y * map.scale) * radarScale + pad;
            return new Vector2(x, y);
        }

        private static bool RadarInside(Vector2 p)
        {
            return p.x >= 2f && p.y >= 2f && p.x <= radarRect.width - 2f && p.y <= radarRect.height - 2f;
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

        private static Color GetRadarPlayerColor(PlayerControl pc)
        {
            try
            {
                int cid = pc.Data.DefaultOutfit.ColorId;
                if (Palette.PlayerColors != null && cid >= 0 && cid < Palette.PlayerColors.Length)
                    return Palette.PlayerColors[cid];
            }
            catch { }
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
            catch { }
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
            catch { }

            try
            {
                PropertyInfo pi = body.GetType().GetProperty("ParentId", f);
                if (pi != null) return Convert.ToByte(pi.GetValue(body, null));
            }
            catch { }

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

            Vector2 m = e.mousePosition;
            Vector2 target = new Vector2(
                ((m.x - pad) / radarScale - map.x) / map.scale,
                (((m.y - pad) / radarScale - map.y) * -1f) / map.scale);

            try { PlayerControl.LocalPlayer.NetTransform.RpcSnapTo(target); }
            catch { try { PlayerControl.LocalPlayer.NetTransform.SnapTo(target); } catch { } }

            e.Use();
        }
    }
}
