#nullable disable
#pragma warning disable CS0162, CS0108, CS0219, CS0661, CS0660, CS8632, CS0168, CS0659
using Hazel;
using InnerNet;
using System.Collections.Generic;
using UnityEngine;
using Color = UnityEngine.Color;
using Vector3 = UnityEngine.Vector3;

namespace ElysiumModMenu
{
    internal static class PetControl
    {
        private const float Speed = 5f;
        private const float RpcDelay = 0.20f;
        private const float AnimDelay = 0.55f;
        private const float PaintGap = 0.35f;
        private const int PaintMax = 400;
        private const byte PetRpc = 49;

        private static Vector2 hand;
        private static float elapsed;
        private static float anim;
        private static byte target = 255;
        private static bool drag;

        private static readonly List<Vector2> pts = new List<Vector2>();
        private static int pi;

        private static readonly List<Collider2D> roomAreas = new List<Collider2D>();
        private static readonly List<string> roomNames = new List<string>();
        private static float roomsAt;
        private static int room;

        private static Texture2D petTex;
        private static GUIStyle petStyle;
        private static Texture2D circleTex;
        private static GUIStyle circleStyle;

        internal static bool On;
        internal static bool Manual;
        internal static bool Paint;
        internal static Vector2 Joy;

        internal static int PaintCount => pts.Count;

        internal static bool IsTarget(byte pid) => On && !Manual && !Paint && target == pid;

        internal static string TogglePaint()
        {
            if (!HasPet(PlayerControl.LocalPlayer)) return "No pet.";
            if (On && Paint) { Stop(); return "Paint: off"; }
            Paint = true;
            Manual = false;
            On = true;
            pi = 0;
            anim = 0f;
            return "Paint: draw with mouse while menu is closed";
        }

        internal static string ClearPaint()
        {
            pts.Clear();
            pi = 0;
            return "Points cleared.";
        }

        internal static string Grab(PlayerControl pc)
        {
            if (pc == null || pc.Data == null) return "No target.";
            if (!HasPet(PlayerControl.LocalPlayer)) return "No pet.";
            target = pc.PlayerId;
            Manual = false;
            Paint = false;
            On = true;
            anim = 0f;
            return "Petting: " + pc.Data.PlayerName;
        }

        internal static string ToggleManual()
        {
            if (!HasPet(PlayerControl.LocalPlayer)) return "No pet.";
            if (On && Manual) { Stop(); return "Manual: off"; }
            Manual = true;
            Paint = false;
            On = true;
            hand = Vector2.zero;
            anim = 0f;
            return "Manual: on";
        }

        internal static string StopPet()
        {
            Stop();
            return "Stopped.";
        }

        internal static string RoomName()
        {
            EnsureRooms();
            if (roomNames.Count == 0) return "-";
            room = Mathf.Clamp(room, 0, roomNames.Count - 1);
            return roomNames[room];
        }

        internal static void RoomStep(int d)
        {
            EnsureRooms();
            int n = roomNames.Count;
            if (n == 0) return;
            room = ((room + d) % n + n) % n;
        }

        internal static string FillRoom()
        {
            if (!HasPet(PlayerControl.LocalPlayer)) return "No pet.";
            RefreshRooms();
            if (roomAreas.Count == 0) return "No rooms (not in match?).";
            room = Mathf.Clamp(room, 0, roomAreas.Count - 1);
            Collider2D area = roomAreas[room];
            if (area == null) return "No room.";

            pts.Clear();
            pi = 0;
            try
            {
                Bounds bb = area.bounds;
                for (float px = bb.min.x; px <= bb.max.x && pts.Count < PaintMax; px += 0.5f)
                    for (float py = bb.min.y; py <= bb.max.y && pts.Count < PaintMax; py += 0.5f)
                    {
                        Vector2 p = new Vector2(px, py);
                        bool inside;
                        try { inside = area.OverlapPoint(p); } catch { inside = true; }
                        if (inside) pts.Add(p);
                    }
            }
            catch { }

            if (pts.Count == 0) return "Empty.";
            Paint = true;
            Manual = false;
            On = true;
            anim = 0f;
            return "Filling: " + roomNames[room] + " (" + pts.Count + ")";
        }

        internal static void Stop()
        {
            On = false;
            Manual = false;
            Paint = false;
            hand = Vector2.zero;
            target = 255;
            Joy = Vector2.zero;
            anim = 0f;

            PlayerControl me = PlayerControl.LocalPlayer;
            if (me == null || me.cosmetics == null) return;
            me.moveable = true;
            if (me.MyPhysics != null && me.MyPhysics.body != null) me.MyPhysics.body.velocity = Vector2.zero;
            try { if (me.cosmetics.PettingHand != null) me.cosmetics.PettingHand.StopPetting(); } catch { }
            try { if (me.cosmetics.CurrentPet != null) me.cosmetics.CurrentPet.SetGettingPet(false, Vector2.zero); } catch { }
            try { if (me.NetTransform != null && MeetingHud.Instance == null) me.NetTransform.RpcSnapTo(me.GetTruePosition()); } catch { }
        }

        internal static void Tick(bool menuOpen)
        {
            if (!On) return;

            PlayerControl me = PlayerControl.LocalPlayer;
            if (!HasPet(me) || me.MyPhysics == null) { Stop(); return; }
            if (me.Data == null || me.Data.IsDead) { Stop(); return; }

            Vector2 petPos;
            if (Paint)
            {
                me.moveable = true;
                RecordStroke(menuOpen);
                if (pts.Count == 0) { try { me.cosmetics.CurrentPet.SetGettingPet(false, Vector2.zero); } catch { } return; }
                if (pi >= pts.Count) pi = 0;
                petPos = pts[pi];
            }
            else if (Manual)
            {
                me.moveable = true;
                hand += Joy * Speed * Time.deltaTime;
                petPos = (Vector2)me.transform.position + hand;
            }
            else
            {
                PlayerControl t = ById(target);
                if (t == null || t.Data == null || t.Data.Disconnected) { Stop(); return; }
                me.moveable = false;
                if (me.MyPhysics.body != null) me.MyPhysics.body.velocity = Vector2.zero;
                petPos = t.transform.position;
                try { petPos.y -= me.cosmetics.currentPet.yOffset * 2f; } catch { }
            }

            try { me.cosmetics.CurrentPet.SetGettingPet(true, petPos); } catch { }

            anim += Time.deltaTime;
            if (anim >= AnimDelay)
            {
                anim = 0f;
                try { if (me.cosmetics.PettingHand != null) me.cosmetics.PettingHand.StartPet(me.cosmetics.currentPet); } catch { }
            }

            elapsed += Time.deltaTime;
            if (elapsed < RpcDelay) return;
            elapsed = 0f;
            if (Paint) pi++;

            try
            {
                InnerNetClient net = (InnerNetClient)AmongUsClient.Instance;
                MessageWriter w = net.StartRpcImmediately(((InnerNetObject)me.MyPhysics).NetId, PetRpc, SendOption.Reliable, -1);
                NetHelpers.WriteVector2(me.GetTruePosition(), w);
                NetHelpers.WriteVector2(petPos, w);
                net.FinishRpcImmediately(w);
            }
            catch { }
        }

        internal static void DrawOverlay(bool menuOpen)
        {
            if (!On) return;
            Event e = Event.current;
            if (e == null) return;

            EnsureStyle();
            if (Manual) DrawJoystick(e);
            else if (Paint) DrawPaint(e, menuOpen);
        }

        private static void DrawJoystick(Event e)
        {
            const float r = 45f, kr = 16f;
            Vector2 center = new Vector2(80f, Screen.height - 120f);
            Vector2 mp = e.mousePosition;

            if (e.type == EventType.MouseDown && Vector2.Distance(mp, center) <= r + 15f) drag = true;
            else if (e.type == EventType.MouseUp) drag = false;

            Vector2 knob = center;
            if (drag)
            {
                Vector2 d = Vector2.ClampMagnitude(mp - center, r);
                knob = center + d;
                Joy = new Vector2(d.x / r, -d.y / r);
            }
            else Joy = Vector2.zero;

            Dot(new Rect(center.x - r, center.y - r, r * 2f, r * 2f), new Color(0.05f, 0.05f, 0.07f, 0.65f));
            Dot(new Rect(center.x - r, center.y - r, r * 2f, r * 2f), new Color(1f, 1f, 1f, 0.10f));
            Dot(new Rect(knob.x - kr, knob.y - kr, kr * 2f, kr * 2f), new Color(0.55f, 0.35f, 0.95f, 0.95f));

            if (GUI.Button(new Rect(center.x - 32f, center.y + r + 8f, 64f, 22f), "CENTER"))
                hand = Vector2.zero;
        }

        private static void DrawPaint(Event e, bool menuOpen)
        {
            if (e.type != EventType.Repaint || menuOpen) return;
            Camera cam = Camera.main;
            if (cam == null) return;

            Color acc = new Color(0.55f, 0.35f, 0.95f, 0.95f);
            for (int i = 0; i < pts.Count; i++)
            {
                Vector3 s = cam.WorldToScreenPoint(pts[i]);
                if (s.z <= 0f) continue;
                Dot(new Rect(s.x - 4f, Screen.height - s.y - 4f, 8f, 8f), acc);
            }

            Vector2 m = e.mousePosition;
            Rect box = new Rect(m.x - 18f, m.y - 18f, 36f, 36f);
            Fill(new Rect(box.x, box.y, box.width, 2f), acc);
            Fill(new Rect(box.x, box.yMax - 2f, box.width, 2f), acc);
            Fill(new Rect(box.x, box.y, 2f, box.height), acc);
            Fill(new Rect(box.xMax - 2f, box.y, 2f, box.height), acc);
        }

        private static void EnsureStyle()
        {
            if (petTex != null) return;

            petTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            petTex.SetPixel(0, 0, Color.white);
            petTex.Apply();
            petTex.hideFlags = HideFlags.HideAndDontSave;
            petStyle = new GUIStyle(GUIStyle.none);
            petStyle.normal.background = petTex;

            const int size = 64;
            circleTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float rad = size * 0.5f, mid = rad - 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = x - mid, dy = y - mid;
                    float a = Mathf.Clamp01(rad - Mathf.Sqrt(dx * dx + dy * dy));
                    circleTex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            circleTex.Apply();
            circleTex.hideFlags = HideFlags.HideAndDontSave;
            circleStyle = new GUIStyle(GUIStyle.none);
            circleStyle.normal.background = circleTex;
        }

        private static void Fill(Rect r, Color c)
        {
            Color prev = GUI.color;
            GUI.color = c;
            GUI.Box(r, GUIContent.none, petStyle);
            GUI.color = prev;
        }

        private static void Dot(Rect r, Color c)
        {
            Color prev = GUI.color;
            GUI.color = c;
            GUI.Box(r, GUIContent.none, circleStyle);
            GUI.color = prev;
        }

        private static void RecordStroke(bool menuOpen)
        {
            if (menuOpen || pts.Count >= PaintMax) return;
            if (!Input.GetMouseButton(0) || Camera.main == null) return;

            Vector3 w = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 p = new Vector2(w.x, w.y);
            if (pts.Count == 0 || Vector2.Distance(pts[pts.Count - 1], p) >= PaintGap)
                pts.Add(p);
        }

        private static void RefreshRooms()
        {
            roomAreas.Clear();
            roomNames.Clear();
            try
            {
                ShipStatus ss = ShipStatus.Instance;
                if (ss == null) return;
                foreach (var r in ss.AllRooms)
                {
                    if (r == null || r.roomArea == null) continue;
                    roomAreas.Add(r.roomArea);
                    string n;
                    try { n = TranslationController.Instance.GetString(r.RoomId); } catch { n = r.RoomId.ToString(); }
                    roomNames.Add(n);
                }
            }
            catch { roomAreas.Clear(); roomNames.Clear(); }
        }

        private static void EnsureRooms()
        {
            try
            {
                if (ShipStatus.Instance == null)
                {
                    if (roomNames.Count > 0) { roomAreas.Clear(); roomNames.Clear(); }
                    return;
                }
                if (roomNames.Count > 0 && Time.unscaledTime - roomsAt < 1f) return;
                roomsAt = Time.unscaledTime;
                RefreshRooms();
            }
            catch { }
        }

        private static bool HasPet(PlayerControl me)
        {
            try { return me != null && me.cosmetics != null && me.cosmetics.CurrentPet != null; }
            catch { return false; }
        }

        private static PlayerControl ById(byte id)
        {
            try
            {
                foreach (PlayerControl pc in PlayerControl.AllPlayerControls)
                    if (pc != null && pc.PlayerId == id) return pc;
            }
            catch { }
            return null;
        }
    }
}
