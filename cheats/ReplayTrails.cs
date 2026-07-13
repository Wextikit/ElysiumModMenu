#nullable disable
#pragma warning disable CS0162, CS0108, CS0219, CS0661, CS0660, CS8632, CS0168, CS0659
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace ElysiumModMenu
{
    public partial class ElysiumModMenuGUI : MonoBehaviour
    {
        private const int ReplayWindowId = 843208;
        private static readonly Dictionary<byte, List<ReplayPoint>> replayPaths = new Dictionary<byte, List<ReplayPoint>>();
        private static readonly Dictionary<byte, ReplayPlayerState> replayPlayers = new Dictionary<byte, ReplayPlayerState>();
        private static Texture2D replayPxTex;
        private static Texture2D replayPlayerTex;
        private static Texture2D replayVisorTex;
        private static Texture2D replayCrossTex;
        private static GUIStyle replayPxStyle;
        private static GUIStyle replayWinStyle;
        private static float replayNextSampleAt;

        private sealed class ReplayPoint
        {
            public Vector2 pos;
            public float t;
        }

        private sealed class ReplayPlayerState
        {
            public Vector2 pos;
            public Color color;
            public Color roleColor;
            public bool dead;
            public bool imp;
            public float lastAt;
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
            if (!showReplay)
            {
                if (replayPaths.Count > 0 && AmongUsClient.Instance == null) ClearVisualReplay();
                return;
            }
            if (Time.unscaledTime < replayNextSampleAt) return;
            replayNextSampleAt = Time.unscaledTime + 0.12f;
            if (PlayerControl.AllPlayerControls == null || AmongUsClient.Instance == null || !AmongUsClient.Instance.IsGameStarted) return;

            float now = Time.unscaledTime;
            foreach (PlayerControl pc in PlayerControl.AllPlayerControls)
            {
                if (pc == null || pc.Data == null || pc.Data.Disconnected) continue;

                byte id = pc.PlayerId;
                if (!replayPaths.TryGetValue(id, out List<ReplayPoint> pts))
                {
                    pts = new List<ReplayPoint>(512);
                    replayPaths[id] = pts;
                }

                Vector2 pos = pc.transform.position;
                if (pts.Count == 0 || Vector2.Distance(pts[pts.Count - 1].pos, pos) > 0.04f)
                    pts.Add(new ReplayPoint { pos = pos, t = now });

                while (pts.Count > 0 && now - pts[0].t > 900f)
                    pts.RemoveAt(0);

                replayPlayers[id] = new ReplayPlayerState
                {
                    pos = pos,
                    color = GetReplayPlayerColor(pc),
                    roleColor = GetReplayRoleColor(pc),
                    dead = pc.Data.IsDead,
                    imp = IsReplayImp(pc),
                    lastAt = now
                };
            }
        }

        private static void DrawVisualReplay()
        {
            if (!showReplay) return;
            ReplayMap map = GetReplayMap();
            if (map == null) return;

            InitReplayGui();
            float w = Mathf.Max(180f, map.tex.width * 0.5f + 14f);
            float h = Mathf.Max(120f, map.tex.height * 0.5f + 30f);
            replayRect.width = w;
            replayRect.height = h;

            Vector2 oldPos = new Vector2(replayRect.x, replayRect.y);
            Color old = GUI.color;
            try
            {
                GUI.color = Color.white;
                replayRect = GUI.Window(ReplayWindowId, replayRect, (Action<int>)DrawReplayWindow, "", replayWinStyle);
            }
            finally
            {
                GUI.color = old;
            }
            replayRect.x = Mathf.Clamp(replayRect.x, 0f, Mathf.Max(0f, Screen.width - replayRect.width));
            replayRect.y = Mathf.Clamp(replayRect.y, 0f, Mathf.Max(0f, Screen.height - replayRect.height));
            if (oldPos.x != replayRect.x || oldPos.y != replayRect.y)
                settingsDirty = true;
        }

        public static void ClearVisualReplay()
        {
            replayPaths.Clear();
            replayPlayers.Clear();
        }

        private static void DrawReplayWindow(int id)
        {
            ReplayMap map = GetReplayMap();
            if (map == null) return;

            float pad = 7f;
            Rect img = new Rect(pad, 22f, map.tex.width * 0.5f, map.tex.height * 0.5f);
            Color old = GUI.color;

            GUI.color = new Color(1f, 1f, 1f, 0.78f);
            GUI.Box(img, GUIContent.none, map.style);

            GUI.color = GetMenuAccentColor(false);
            GUI.Label(new Rect(8f, 2f, 140f, 18f), "Replay");

            DrawReplayPaths(map, img.x, img.y);
            DrawReplayPlayers(map, img.x, img.y);

            GUI.color = old;
            GUI.DragWindow(new Rect(0f, 0f, replayRect.width, 22f));
        }

        private static void DrawReplayPaths(ReplayMap map, float ox, float oy)
        {
            float now = Time.unscaledTime;
            float min = replayOnlyLastSeconds ? now - Mathf.Clamp(replaySeconds, 5f, 180f) : -9999f;

            foreach (var pair in replayPaths)
            {
                if (!replayPlayers.TryGetValue(pair.Key, out ReplayPlayerState st)) continue;
                List<ReplayPoint> pts = pair.Value;
                if (pts == null || pts.Count < 2) continue;

                GUI.color = new Color(st.color.r, st.color.g, st.color.b, 0.78f);
                Vector2 prev = Vector2.zero;
                bool hasPrev = false;

                for (int i = 0; i < pts.Count; i++)
                {
                    ReplayPoint rp = pts[i];
                    if (rp.t < min) { hasPrev = false; continue; }

                    Vector2 p = ReplayPointOnMap(map, rp.pos, ox, oy);
                    if (hasPrev) DrawReplayLine(prev, p, 2f);
                    prev = p;
                    hasPrev = true;
                }
            }
        }

        private static void DrawReplayPlayers(ReplayMap map, float ox, float oy)
        {
            float now = Time.unscaledTime;
            foreach (var pair in replayPlayers)
            {
                ReplayPlayerState st = pair.Value;
                if (now - st.lastAt > 2f) continue;

                Vector2 p = ReplayPointOnMap(map, st.pos, ox, oy);
                if (replayDrawIcons && replayPlayerTex != null)
                    DrawReplayPlayerIcon(p, st);
                else
                {
                    float s = 8f;
                    GUI.color = st.color;
                    DrawReplayLine(new Rect(p.x - s * 0.5f, p.y - s * 0.5f, s, s));
                    if (st.imp && (seeRoles || IsReplayLocalImp()))
                    {
                        GUI.color = st.roleColor;
                        DrawReplayLine(new Rect(p.x - s * 0.5f, p.y - s * 0.5f, s, 2f));
                        DrawReplayLine(new Rect(p.x - s * 0.5f, p.y + s * 0.5f, s, 2f));
                        DrawReplayLine(new Rect(p.x - s * 0.5f, p.y - s * 0.5f, 2f, s));
                        DrawReplayLine(new Rect(p.x + s * 0.5f, p.y - s * 0.5f, 2f, s));
                    }
                }
            }
        }

        private static Vector2 ReplayPointOnMap(ReplayMap map, Vector2 pos, float ox, float oy)
        {
            return new Vector2(ox + map.x * 0.5f + pos.x * map.scale * 0.5f, oy + map.y * 0.5f - pos.y * map.scale * 0.5f);
        }

        private static void DrawReplayPlayerIcon(Vector2 p, ReplayPlayerState st)
        {
            float s = 17f;
            Rect r = new Rect(p.x - s * 0.5f, p.y - s * 0.5f, s, s);
            GUI.color = st.color;
            GUI.DrawTexture(r, replayPlayerTex, ScaleMode.StretchToFill, true);
            if (replayVisorTex != null)
            {
                GUI.color = seeRoles ? st.roleColor : new Color(0.72f, 0.86f, 0.96f, 1f);
                GUI.DrawTexture(r, replayVisorTex, ScaleMode.StretchToFill, true);
            }
            if (st.dead && replayCrossTex != null)
            {
                GUI.color = Color.white;
                GUI.DrawTexture(r, replayCrossTex, ScaleMode.StretchToFill, true);
            }
        }

        private static void DrawReplayLine(Vector2 a, Vector2 b, float width)
        {
            Vector2 d = b - a;
            float len = d.magnitude;
            if (len < 0.1f) return;

            Matrix4x4 old = GUI.matrix;
            float ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            GUIUtility.RotateAroundPivot(ang, a);
            DrawReplayLine(new Rect(a.x, a.y - width * 0.5f, len, width));
            GUI.matrix = old;
        }

        private static void DrawReplayLine(Rect r)
        {
            GUI.Box(r, GUIContent.none, replayPxStyle);
        }

        private static void InitReplayGui()
        {
            if (replayWinStyle == null)
                replayWinStyle = new GUIStyle(GUIStyle.none);

            if (replayPxTex == null)
            {
                replayPxTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                replayPxTex.SetPixel(0, 0, Color.white);
                replayPxTex.Apply();
                replayPxTex.hideFlags = HideFlags.HideAndDontSave;
                replayPxStyle = new GUIStyle(GUIStyle.none);
                replayPxStyle.normal.background = replayPxTex;
            }

            if (replayPlayerTex == null) replayPlayerTex = LoadReplayTex("ElysiumModMenu.radar_player.png");
            if (replayVisorTex == null) replayVisorTex = LoadReplayTex("ElysiumModMenu.radar_player_visor.png");
            if (replayCrossTex == null) replayCrossTex = LoadReplayTex("ElysiumModMenu.radar_cross.png");
        }

        private static ReplayMap GetReplayMap()
        {
            int mid = 0;
            try { mid = Mathf.Clamp(GetCurrentMapId(), 0, 5); } catch { }
            for (int i = 0; i < replayMaps.Length; i++)
            {
                ReplayMap m = replayMaps[i];
                if (m.id != mid) continue;
                if (m.tex == null) m.tex = LoadReplayTex(m.res);
                if (m.tex != null && m.style == null)
                {
                    m.style = new GUIStyle(GUIStyle.none);
                    m.style.normal.background = m.tex;
                }
                return m.tex == null ? null : m;
            }
            return null;
        }

        private static Texture2D LoadReplayTex(string res)
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

        private static Color GetReplayPlayerColor(PlayerControl pc)
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

        private static Color GetReplayRoleColor(PlayerControl pc)
        {
            try
            {
                if (pc.Data.Role != null)
                    return GetRoleColor((int)pc.Data.Role.Role, pc.Data.Role.TeamColor);
            }
            catch { }
            return Color.black;
        }

        private static bool IsReplayLocalImp() => IsReplayImp(PlayerControl.LocalPlayer);

        private static bool IsReplayImp(PlayerControl pc)
        {
            try
            {
                if (pc == null || pc.Data == null) return false;
                if (pc.Data.Role != null && pc.Data.Role.IsImpostor) return true;
                return RoleManager.IsImpostorRole(pc.Data.RoleType);
            }
            catch { return false; }
        }
    }
}
