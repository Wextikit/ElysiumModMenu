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
    internal static class NetworkedClones
    {
        private const int Max = 100;
        private const int Owner = -2;
        private const byte SnapRpc = 21;
        private const ushort SeqStep = 5;
        private const float SettleDelay = 2.2f;

        private sealed class Clone { public PlayerControl Pc; public Vector2 At; public int Left; public float NextAt; public ushort Seq; }
        private struct CloneJob { public byte Src; public Vector2 At; }

        private static readonly List<Clone> live = new List<Clone>();
        private static readonly Queue<CloneJob> pend = new Queue<CloneJob>();
        private static float spawnGate;
        private static float tickAt;

        internal static bool ClickMode;
        internal static int Live => live.Count;
        internal static int Queued => pend.Count;

        internal static void Tick(bool menuOpen)
        {
            float now = Time.unscaledTime;

            if (ClickMode && !menuOpen && Ready() && Camera.main != null)
                Clicks();

            if (pend.Count > 0 && now >= spawnGate && Ready())
            {
                CloneJob j = pend.Dequeue();
                spawnGate = now + 0.14f;
                if (live.Count < Max)
                {
                    PlayerControl src = ById(j.Src);
                    if (src != null) { try { Make(src, j.At); } catch { } }
                }
            }

            if (live.Count > 0 && now >= tickAt)
            {
                tickAt = now + 0.3f;
                Settle(now);
            }
        }

        internal static string CloneOf(PlayerControl src)
        {
            if (!Ready()) return "Host only. Delete lobby through Maps first.";
            if (src == null || src.transform == null) return "No player.";
            if (live.Count + pend.Count >= Max) return "Clone limit.";
            Vector3 p = src.transform.position + Vector3.left * 0.6f;
            pend.Enqueue(new CloneJob { Src = src.PlayerId, At = new Vector2(p.x, p.y) });
            return "Clone queued";
        }

        internal static string Formation(int idx, int count)
        {
            if (!Ready()) return "Host only. Delete lobby through Maps first.";
            PlayerControl me = PlayerControl.LocalPlayer;
            if (me == null || me.transform == null) return "No player.";
            if (idx == 9 || idx == 10)
                return TextFormation(idx == 9 ? "NETWORK" : "ELYSIUM", me);

            int n = Mathf.Clamp(count, 1, Max - live.Count - pend.Count);
            if (n <= 0) return "Clone limit.";
            Vector3 c = me.transform.position;
            for (int i = 0; i < n; i++)
            {
                Vector2 p = FormationPos(idx, i, n, c);
                pend.Enqueue(new CloneJob { Src = me.PlayerId, At = p });
            }
            return "Clones queued: " + n;
        }

        internal static void Forget()
        {
            live.Clear();
            pend.Clear();
        }

        internal static void ClearAll()
        {
            pend.Clear();
            Clone[] arr = live.ToArray();
            live.Clear();
            foreach (Clone c in arr) Kill(c?.Pc);
        }

        internal static bool Ready()
        {
            try
            {
                return AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost
                    && PlayerControl.LocalPlayer != null && GameData.Instance != null;
            }
            catch { return false; }
        }

        private static void Clicks()
        {
            PlayerControl me = PlayerControl.LocalPlayer;
            if (me == null) return;

            Vector3 w = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 at = new Vector2(w.x, w.y);

            if (Input.GetMouseButtonDown(0))
            {
                if (live.Count + pend.Count < Max)
                    pend.Enqueue(new CloneJob { Src = me.PlayerId, At = at });
            }
            else if (Input.GetMouseButtonDown(1))
            {
                Nearest(at);
            }
        }

        private static Vector2 FormationPos(int idx, int i, int n, Vector3 c)
        {
            if (idx == 1)
            {
                float a = (Mathf.PI * 2f / Mathf.Max(1, n)) * i;
                float r = Mathf.Max(1.1f, n * 0.11f);
                return new Vector2(c.x + Mathf.Cos(a) * r, c.y + Mathf.Sin(a) * r);
            }

            if (idx == 2)
            {
                int cols = Mathf.CeilToInt(Mathf.Sqrt(n));
                int row = i / cols;
                int col = i % cols;
                return new Vector2(c.x + (col - (cols - 1) * 0.5f) * 0.62f, c.y - row * 0.62f);
            }

            if (idx == 3)
                return new Vector2(c.x + (i - (n - 1) * 0.5f) * 0.5f, c.y + Mathf.Sin(i * 0.8f) * 0.8f);

            if (idx == 4)
            {
                float t = Mathf.PI * 2f * i / Mathf.Max(1, n);
                float sx = 16f * Mathf.Pow(Mathf.Sin(t), 3f);
                float sy = 13f * Mathf.Cos(t) - 5f * Mathf.Cos(2f * t) - 2f * Mathf.Cos(3f * t) - Mathf.Cos(4f * t);
                return new Vector2(c.x + sx * 0.105f, c.y + sy * 0.105f - 0.45f);
            }

            if (idx == 5)
            {
                float a = Mathf.PI * 2f * i / Mathf.Max(1, n) - Mathf.PI * 0.5f;
                float r = 1.15f + 0.42f * Mathf.Cos(5f * a);
                return new Vector2(c.x + Mathf.Cos(a) * r, c.y + Mathf.Sin(a) * r);
            }

            if (idx == 6)
            {
                float a = i * 0.72f;
                float r = 0.18f + i * 0.045f;
                return new Vector2(c.x + Mathf.Cos(a) * r, c.y + Mathf.Sin(a) * r);
            }

            if (idx == 7)
            {
                int half = Mathf.Max(1, Mathf.CeilToInt(n * 0.5f));
                if (i < half)
                    return new Vector2(c.x + (i - (half - 1) * 0.5f) * 0.5f, c.y);
                int j = i - half;
                int rest = Mathf.Max(1, n - half);
                return new Vector2(c.x, c.y + (j - (rest - 1) * 0.5f) * 0.5f);
            }

            if (idx == 8)
            {
                float a = Mathf.PI * 2f * i / Mathf.Max(1, n);
                float x = Mathf.Cos(a);
                float y = Mathf.Sin(a);
                float d = Mathf.Max(0.001f, Mathf.Abs(x) + Mathf.Abs(y));
                return new Vector2(c.x + x / d * 1.55f, c.y + y / d * 1.55f);
            }

            return new Vector2(c.x + (i - (n - 1) * 0.5f) * 0.55f, c.y - 0.8f);
        }

        private static string TextFormation(string text, PlayerControl me)
        {
            int room = Max - live.Count - pend.Count;
            if (room <= 0) return "Clone limit.";
            List<Vector2> pts = TextPoints(text, me.transform.position);
            int made = 0;
            for (int i = 0; i < pts.Count && made < room; i++)
            {
                pend.Enqueue(new CloneJob { Src = me.PlayerId, At = pts[i] });
                made++;
            }
            return text + " queued: " + made;
        }

        private static List<Vector2> TextPoints(string text, Vector3 c)
        {
            List<Vector2> pts = new List<Vector2>();
            float scale = text.Length > 7 ? 0.28f : 0.32f;
            int width = 0;
            for (int i = 0; i < text.Length; i++) width += Letter(text[i])[0].Length + 1;
            float ox = c.x - width * scale * 0.5f;
            float oy = c.y + 1.05f;
            int x = 0;

            for (int a = 0; a < text.Length; a++)
            {
                string[] p = Letter(text[a]);
                for (int y = 0; y < p.Length; y++)
                    for (int xx = 0; xx < p[y].Length; xx++)
                        if (p[y][xx] == '1')
                            pts.Add(new Vector2(ox + (x + xx) * scale, oy - y * scale));
                x += p[0].Length + 1;
            }
            return pts;
        }

        private static string[] Letter(char ch)
        {
            switch (ch)
            {
                case 'E': return new[] { "111", "100", "111", "100", "111" };
                case 'I': return new[] { "111", "010", "010", "010", "111" };
                case 'K': return new[] { "101", "110", "100", "110", "101" };
                case 'L': return new[] { "100", "100", "100", "100", "111" };
                case 'M': return new[] { "10001", "11011", "10101", "10001", "10001" };
                case 'N': return new[] { "1001", "1101", "1011", "1001", "1001" };
                case 'O': return new[] { "111", "101", "101", "101", "111" };
                case 'R': return new[] { "111", "101", "111", "110", "101" };
                case 'S': return new[] { "111", "100", "111", "001", "111" };
                case 'T': return new[] { "111", "010", "010", "010", "010" };
                case 'U': return new[] { "101", "101", "101", "101", "111" };
                case 'W': return new[] { "10001", "10001", "10101", "10101", "01010" };
                case 'Y': return new[] { "101", "101", "010", "010", "010" };
            }
            return new[] { "111", "101", "101", "101", "111" };
        }

        private static void Nearest(Vector2 at)
        {
            int best = -1;
            float bd = 1.6f;
            for (int i = 0; i < live.Count; i++)
            {
                Clone c = live[i];
                if (c?.Pc == null) continue;
                float d = Vector2.Distance(c.At, at);
                if (d < bd) { bd = d; best = i; }
            }
            if (best < 0) return;
            PlayerControl pc = live[best].Pc;
            live.RemoveAt(best);
            Kill(pc);
        }

        private static PlayerControl ById(byte pid)
        {
            try { foreach (PlayerControl p in PlayerControl.AllPlayerControls) if (p != null && p.PlayerId == pid) return p; }
            catch { }
            return null;
        }

        private static void Make(PlayerControl src, Vector2 at)
        {
            if (src == null || src.Data == null) return;
            AmongUsClient net = AmongUsClient.Instance;
            PlayerControl prefab = net != null ? net.PlayerPrefab : null;
            if (prefab == null) return;

            Vector3 pos = new Vector3(at.x, at.y, src.transform.position.z);
            PlayerControl cl = Object.Instantiate(prefab);
            cl.PlayerId = src.PlayerId;
            cl.isNew = false;
            cl.notRealPlayer = true;
            net.Spawn(cl.Cast<InnerNetObject>(), Owner, SpawnFlags.None);
            cl.transform.position = pos;

            Clone c = new Clone { Pc = cl, At = at, Left = 6, NextAt = Time.unscaledTime + SettleDelay };
            if (cl.NetTransform != null)
            {
                ((Behaviour)cl.NetTransform).enabled = true;
                try { c.Seq = cl.NetTransform.lastSequenceId; } catch { }
                Place(c);
            }
            live.Add(c);
        }

        private static void Settle(float now)
        {
            for (int i = live.Count - 1; i >= 0; i--)
            {
                Clone c = live[i];
                if (c?.Pc == null) { live.RemoveAt(i); continue; }
                if (c.Left <= 0 || now < c.NextAt) continue;
                c.Left--;
                c.NextAt = now + 0.4f;
                Place(c);
            }
        }

        private static void Place(Clone c)
        {
            if (c?.Pc == null) return;
            try
            {
                CustomNetworkTransform nt = c.Pc.NetTransform;
                try { nt.Halt(); } catch { }
                c.Seq = (ushort)(c.Seq + SeqStep);
                nt.SnapTo(c.At, c.Seq);
                MessageWriter w = AmongUsClient.Instance.StartRpcImmediately(nt.Cast<InnerNetObject>().NetId, SnapRpc, SendOption.Reliable, -1);
                NetHelpers.WriteVector2(c.At, w);
                w.Write(c.Seq);
                AmongUsClient.Instance.FinishRpcImmediately(w);
            }
            catch { }
        }

        private static void Kill(PlayerControl pc)
        {
            if (pc == null) return;
            try { pc.PlayerId = 253; } catch { }
            try
            {
                AmongUsClient net = AmongUsClient.Instance;
                MessageWriter w = MessageWriter.Get(SendOption.Reliable);
                w.StartMessage(5);
                w.Write(((InnerNetClient)net).GameId);
                w.StartMessage(5);
                w.WritePacked(pc.Cast<InnerNetObject>().NetId);
                w.EndMessage();
                w.EndMessage();
                ((InnerNetClient)net).SendOrDisconnect(w);
                w.Recycle();
                ((InnerNetClient)net).RemoveNetObject(pc.Cast<InnerNetObject>());
            }
            catch { }
            try { Object.Destroy(pc.gameObject); } catch { }
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.GetTruePosition))]
    internal static class NetworkedClonesPosGuard
    {
        public static bool Prefix(PlayerControl __instance, ref Vector2 __result)
        {
            try
            {
                if (__instance != null && __instance.transform != null) return true;
            }
            catch { }
            __result = Vector2.zero;
            return false;
        }
    }

    [HarmonyPatch(typeof(LobbyBehaviour), "Start")]
    internal static class NetworkedClonesLobbyPatch
    {
        public static void Postfix() => NetworkedClones.Forget();
    }

    [HarmonyPatch(typeof(IntroCutscene), "CoBegin")]
    internal static class NetworkedClonesIntroPatch
    {
        public static void Prefix() => NetworkedClones.ClearAll();
    }
}
