#nullable disable
#pragma warning disable CS0162, CS0108, CS0219, CS0661, CS0660, CS8632, CS0168, CS0659
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ElysiumModMenu
{
    public partial class ElysiumModMenuGUI : MonoBehaviour
    {
        public static bool isPanicked = false;
        public static bool rgbTaskBar = false;

        private static float rgbTaskBarScanAt = 0f;
        private static readonly List<SpriteRenderer> rgbTaskSprites = new List<SpriteRenderer>();
        private static readonly List<Image> rgbTaskImgs = new List<Image>();
        private static readonly List<TMP_Text> rgbTaskTexts = new List<TMP_Text>();
        private static readonly List<Renderer> rgbTaskRends = new List<Renderer>();
        private static readonly Dictionary<int, Color> rgbTaskSpriteCols = new Dictionary<int, Color>();
        private static readonly Dictionary<int, Color> rgbTaskImgCols = new Dictionary<int, Color>();
        private static readonly Dictionary<int, Color> rgbTaskTextCols = new Dictionary<int, Color>();
        private static readonly Dictionary<int, Color[]> rgbTaskRendCols = new Dictionary<int, Color[]>();
        private static readonly Dictionary<int, Texture[]> rgbTaskRendTex = new Dictionary<int, Texture[]>();
        private static Texture2D rgbTaskWhiteTex;

        private static void TickRgbTaskBar()
        {
            try
            {
                if (!rgbTaskBar)
                {
                    RestoreRgbTaskBar();
                    return;
                }

                if (HudManager.Instance == null) return;

                if (Time.unscaledTime >= rgbTaskBarScanAt || (rgbTaskSprites.Count == 0 && rgbTaskImgs.Count == 0 && rgbTaskTexts.Count == 0))
                    ScanRgbTaskBar();

                float h = Mathf.Repeat(Time.unscaledTime * 0.18f, 1f);
                Color col = Color.HSVToRGB(h, 0.95f, 1f);
                col.a = 1f;
                Color textCol = Color.HSVToRGB(Mathf.Repeat(h + 0.08f, 1f), 0.75f, 1f);
                textCol.a = 1f;

                for (int i = rgbTaskRends.Count - 1; i >= 0; i--)
                {
                    var rend = rgbTaskRends[i];
                    if (rend == null) { rgbTaskRends.RemoveAt(i); continue; }
                    ApplyRgbRenderer(rend, col);
                }

                for (int i = rgbTaskSprites.Count - 1; i >= 0; i--)
                {
                    var spr = rgbTaskSprites[i];
                    if (spr == null) { rgbTaskSprites.RemoveAt(i); continue; }
                    spr.color = col;
                }

                for (int i = rgbTaskImgs.Count - 1; i >= 0; i--)
                {
                    var img = rgbTaskImgs[i];
                    if (img == null) { rgbTaskImgs.RemoveAt(i); continue; }
                    img.color = col;
                }

                for (int i = rgbTaskTexts.Count - 1; i >= 0; i--)
                {
                    var txt = rgbTaskTexts[i];
                    if (txt == null) { rgbTaskTexts.RemoveAt(i); continue; }
                    txt.color = textCol;
                }

                ApplyRgbTaskBarLive(col, textCol);
            }
            catch
            {
                rgbTaskSprites.Clear();
                rgbTaskImgs.Clear();
                rgbTaskTexts.Clear();
                rgbTaskRends.Clear();
                rgbTaskBarScanAt = Time.unscaledTime + 1.5f;
            }
        }

        private static void ScanRgbTaskBar()
        {
            rgbTaskBarScanAt = Time.unscaledTime + 0.45f;
            rgbTaskSprites.Clear();
            rgbTaskImgs.Clear();
            rgbTaskTexts.Clear();

            List<Transform> roots = FindTaskBarRoots();

            HashSet<int> seenSprites = new HashSet<int>();
            HashSet<int> seenImgs = new HashSet<int>();
            HashSet<int> seenTexts = new HashSet<int>();

            for (int r = 0; r < roots.Count; r++)
            {
                Transform root = roots[r];
                if (root == null) continue;

                AddRgbTaskParts(root, seenSprites, seenImgs, seenTexts);
            }

            AddProgressTrackerParts(seenSprites, seenImgs, seenTexts);
            AddLooseTaskCompleteFill(seenSprites, seenImgs);
        }

        private static void ApplyRgbTaskBarLive(Color col, Color textCol)
        {
            try
            {
                var trackers = UnityEngine.Object.FindObjectsOfType<ProgressTracker>(true);
                for (int t = 0; t < trackers.Length; t++)
                {
                    var tracker = trackers[t];
                    if (tracker == null) continue;
                    Transform root = tracker.transform;
                    if (root == null || IsUnderTasksPanel(root)) continue;

                    ApplyRgbRenderer(tracker.TileParent, col);

                    var rends = root.GetComponentsInChildren<MeshRenderer>(true);
                    for (int i = 0; i < rends.Length; i++)
                    {
                        var rend = rends[i];
                        if (rend == null || IsTextOrShadowPart(rend.transform)) continue;
                        string n = FullPath(rend.transform).ToLowerInvariant();
                        if (!n.Contains("tile") && !n.Contains("fill") && !n.Contains("progress") && !n.Contains("bar")) continue;
                        ApplyRgbRenderer(rend, col);
                    }

                    var sprites = root.GetComponentsInChildren<SpriteRenderer>(true);
                    for (int i = 0; i < sprites.Length; i++)
                    {
                        var spr = sprites[i];
                        if (spr == null || IsTextOrShadowPart(spr.transform)) continue;
                        int id = spr.GetInstanceID();
                        if (!rgbTaskSpriteCols.ContainsKey(id)) rgbTaskSpriteCols[id] = spr.color;
                        if (!rgbTaskSprites.Contains(spr)) rgbTaskSprites.Add(spr);
                        spr.color = col;
                    }

                    var imgs = root.GetComponentsInChildren<Image>(true);
                    for (int i = 0; i < imgs.Length; i++)
                    {
                        var img = imgs[i];
                        if (img == null || IsTextOrShadowPart(img.transform)) continue;
                        int id = img.GetInstanceID();
                        if (!rgbTaskImgCols.ContainsKey(id)) rgbTaskImgCols[id] = img.color;
                        if (!rgbTaskImgs.Contains(img)) rgbTaskImgs.Add(img);
                        img.color = col;
                    }

                    var texts = root.GetComponentsInChildren<TMP_Text>(true);
                    for (int i = 0; i < texts.Length; i++)
                    {
                        var txt = texts[i];
                        if (txt == null || !LooksLikeTotalTasksText(txt)) continue;
                        int id = txt.GetInstanceID();
                        if (!rgbTaskTextCols.ContainsKey(id)) rgbTaskTextCols[id] = txt.color;
                        if (!rgbTaskTexts.Contains(txt)) rgbTaskTexts.Add(txt);
                        txt.color = textCol;
                    }
                }
            }
            catch { }
        }

        private static void ApplyRgbRenderer(Renderer rend, Color col)
        {
            if (rend == null) return;
            try
            {
                int id = rend.GetInstanceID();
                var mats = rend.materials;
                if (mats == null || mats.Length == 0) return;

                if (!rgbTaskRendCols.ContainsKey(id))
                {
                    Color[] cols = new Color[mats.Length];
                    Texture[] tex = new Texture[mats.Length];
                    for (int i = 0; i < mats.Length; i++)
                    {
                        cols[i] = mats[i] != null && mats[i].HasProperty("_Color") ? mats[i].color : Color.white;
                        tex[i] = mats[i] != null && mats[i].HasProperty("_MainTex") ? mats[i].GetTexture("_MainTex") : null;
                    }
                    rgbTaskRendCols[id] = cols;
                    rgbTaskRendTex[id] = tex;
                }

                if (!rgbTaskRends.Contains(rend)) rgbTaskRends.Add(rend);

                for (int i = 0; i < mats.Length; i++)
                {
                    var mat = mats[i];
                    if (mat == null) continue;
                    if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", GetRgbTaskWhiteTex());
                    if (mat.HasProperty("_Color")) mat.color = col;
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", col);
                    if (mat.HasProperty("_TintColor")) mat.SetColor("_TintColor", col);
                    if (mat.HasProperty("_EmissionColor"))
                    {
                        mat.EnableKeyword("_EMISSION");
                        mat.SetColor("_EmissionColor", col);
                    }
                }
            }
            catch { }
        }

        private static Texture2D GetRgbTaskWhiteTex()
        {
            if (rgbTaskWhiteTex != null) return rgbTaskWhiteTex;
            rgbTaskWhiteTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            rgbTaskWhiteTex.hideFlags = HideFlags.HideAndDontSave;
            rgbTaskWhiteTex.SetPixel(0, 0, Color.white);
            rgbTaskWhiteTex.Apply(false, true);
            return rgbTaskWhiteTex;
        }

        private static void AddProgressTrackerParts(HashSet<int> seenSprites, HashSet<int> seenImgs, HashSet<int> seenTexts)
        {
            try
            {
                var trackers = UnityEngine.Object.FindObjectsOfType<ProgressTracker>(true);
                for (int t = 0; t < trackers.Length; t++)
                {
                    var tracker = trackers[t];
                    if (tracker == null) continue;
                    Transform root = tracker.transform;
                    if (root == null || IsUnderTasksPanel(root)) continue;

                    var sprites = root.GetComponentsInChildren<SpriteRenderer>(true);
                    for (int i = 0; i < sprites.Length; i++)
                    {
                        var spr = sprites[i];
                        if (spr == null || IsTextOrShadowPart(spr.transform)) continue;
                        int id = spr.GetInstanceID();
                        if (!seenSprites.Add(id)) continue;
                        if (!rgbTaskSpriteCols.ContainsKey(id)) rgbTaskSpriteCols[id] = spr.color;
                        rgbTaskSprites.Add(spr);
                    }

                    var imgs = root.GetComponentsInChildren<Image>(true);
                    for (int i = 0; i < imgs.Length; i++)
                    {
                        var img = imgs[i];
                        if (img == null || IsTextOrShadowPart(img.transform)) continue;
                        int id = img.GetInstanceID();
                        if (!seenImgs.Add(id)) continue;
                        if (!rgbTaskImgCols.ContainsKey(id)) rgbTaskImgCols[id] = img.color;
                        rgbTaskImgs.Add(img);
                    }

                    var texts = root.GetComponentsInChildren<TMP_Text>(true);
                    for (int i = 0; i < texts.Length; i++)
                    {
                        var txt = texts[i];
                        if (txt == null || !LooksLikeTotalTasksText(txt)) continue;
                        int id = txt.GetInstanceID();
                        if (!seenTexts.Add(id)) continue;
                        if (!rgbTaskTextCols.ContainsKey(id)) rgbTaskTextCols[id] = txt.color;
                        rgbTaskTexts.Add(txt);
                    }
                }
            }
            catch { }
        }

        private static void AddRgbTaskParts(Transform root, HashSet<int> seenSprites, HashSet<int> seenImgs, HashSet<int> seenTexts)
        {
            if (root == null) return;

            var sprites = root.GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < sprites.Length; i++)
            {
                var spr = sprites[i];
                if (spr == null) continue;
                if (!LooksLikeTaskBarPart(spr) && !LooksLikeTaskBarPaint(spr.transform, spr.color)) continue;
                int id = spr.GetInstanceID();
                if (!seenSprites.Add(id)) continue;
                if (!rgbTaskSpriteCols.ContainsKey(id)) rgbTaskSpriteCols[id] = spr.color;
                rgbTaskSprites.Add(spr);
            }

            var imgs = root.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < imgs.Length; i++)
            {
                var img = imgs[i];
                if (img == null) continue;
                if (!LooksLikeTaskBarPart(img) && !LooksLikeTaskBarPaint(img.transform, img.color)) continue;
                int id = img.GetInstanceID();
                if (!seenImgs.Add(id)) continue;
                if (!rgbTaskImgCols.ContainsKey(id)) rgbTaskImgCols[id] = img.color;
                rgbTaskImgs.Add(img);
            }

            var texts = root.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                var txt = texts[i];
                if (txt == null || !LooksLikeTotalTasksText(txt)) continue;
                int id = txt.GetInstanceID();
                if (!seenTexts.Add(id)) continue;
                if (!rgbTaskTextCols.ContainsKey(id)) rgbTaskTextCols[id] = txt.color;
                rgbTaskTexts.Add(txt);
            }
        }

        private static void AddLooseTaskCompleteFill(HashSet<int> seenSprites, HashSet<int> seenImgs)
        {
            try
            {
                var hud = HudManager.Instance;
                if (hud == null) return;

                var sprites = hud.GetComponentsInChildren<SpriteRenderer>(true);
                for (int i = 0; i < sprites.Length; i++)
                {
                    var spr = sprites[i];
                    if (spr == null || IsUnderTasksPanel(spr.transform)) continue;
                    if (!LooksLikeLongGreenBar(spr.transform, spr.color)) continue;
                    int id = spr.GetInstanceID();
                    if (!seenSprites.Add(id)) continue;
                    if (!rgbTaskSpriteCols.ContainsKey(id)) rgbTaskSpriteCols[id] = spr.color;
                    rgbTaskSprites.Add(spr);
                }

                var imgs = hud.GetComponentsInChildren<Image>(true);
                for (int i = 0; i < imgs.Length; i++)
                {
                    var img = imgs[i];
                    if (img == null || IsUnderTasksPanel(img.transform)) continue;
                    if (!LooksLikeLongGreenBar(img.transform, img.color)) continue;
                    int id = img.GetInstanceID();
                    if (!seenImgs.Add(id)) continue;
                    if (!rgbTaskImgCols.ContainsKey(id)) rgbTaskImgCols[id] = img.color;
                    rgbTaskImgs.Add(img);
                }
            }
            catch { }
        }

        private static bool IsUnderTasksPanel(Transform tr)
        {
            for (int i = 0; i < 6 && tr != null; i++, tr = tr.parent)
            {
                string n = (tr.name ?? "").ToLowerInvariant();
                if (n == "tasks" || n.Contains("taskpanel") || n.Contains("task panel")) return true;
            }
            return false;
        }

        private static bool IsTextOrShadowPart(Transform tr)
        {
            for (int i = 0; i < 4 && tr != null; i++, tr = tr.parent)
            {
                string n = (tr.name ?? "").ToLowerInvariant();
                if (n.Contains("text") || n.Contains("shadow")) return true;
            }
            return false;
        }

        private static bool LooksLikeLongGreenBar(Transform tr, Color col)
        {
            if (tr == null) return false;
            if (!(col.g > 0.45f && col.g > col.r * 1.25f && col.g > col.b * 1.25f)) return false;

            try
            {
                Vector3 s = tr.lossyScale;
                if (Mathf.Abs(s.x) > Mathf.Abs(s.y) * 2.2f) return true;
            }
            catch { }

            string n = FullPath(tr).ToLowerInvariant();
            return n.Contains("total") || n.Contains("complete") || n.Contains("progress") || n.Contains("fill");
        }

        private static List<Transform> FindTaskBarRoots()
        {
            List<Transform> roots = new List<Transform>();
            try
            {
                var hud = HudManager.Instance;
                if (hud == null) return roots;

                var texts = UnityEngine.Object.FindObjectsOfType<TMP_Text>(true);
                for (int i = 0; i < texts.Length; i++)
                {
                    var txt = texts[i];
                    if (txt == null || !LooksLikeTotalTasksText(txt)) continue;

                    Transform tr = txt.transform;
                    for (int p = 0; p < 4 && tr != null; p++)
                    {
                        if (LooksLikeTaskRootName(tr.name) || HasTaskBarBits(tr))
                        {
                            AddTaskRoot(roots, tr);
                            break;
                        }
                        tr = tr.parent;
                    }

                    if (roots.Count == 0 && txt.transform.parent != null)
                        AddTaskRoot(roots, txt.transform.parent);
                }

                if (roots.Count > 0) return roots;

                var all = hud.GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < all.Length; i++)
                {
                    var tr = all[i];
                    if (tr == null) continue;
                    if (LooksLikeTotalTaskPath(tr) && HasTaskBarBits(tr)) AddTaskRoot(roots, tr);
                }
            }
            catch { }

            return roots;
        }

        private static void AddTaskRoot(List<Transform> roots, object obj)
        {
            try
            {
                Transform tr = null;
                if (obj is Transform t) tr = t;
                else if (obj is Component cmp) tr = cmp.transform;
                else if (obj is GameObject go) tr = go.transform;

                if (tr == null) return;
                for (int i = 0; i < roots.Count; i++)
                    if (roots[i] == tr) return;

                roots.Add(tr);
            }
            catch { }
        }

        private static bool LooksLikeTaskRootName(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            string n = name.ToLowerInvariant();
            return n.Contains("taskbar") ||
                   n.Contains("task bar") ||
                   n.Contains("taskcomplete") ||
                   n.Contains("task complete") ||
                   n.Contains("total task") ||
                   n.Contains("total_tasks") ||
                   n.Contains("tasks completed") ||
                   n.Contains("completedtasks") ||
                   n.Contains("completed tasks") ||
                   n.Contains("progressbar") ||
                   n.Contains("progress bar");
        }

        private static bool LooksLikeTotalTasksText(TMP_Text txt)
        {
            string s = ((txt.text ?? "") + " " + (txt.name ?? "") + " " + FullPath(txt.transform)).ToLowerInvariant();
            return (s.Contains("total") && s.Contains("task")) ||
                   s.Contains("tasks completed") ||
                   s.Contains("taskcomplete") ||
                   s.Contains("completedtasks");
        }

        private static bool LooksLikeTotalTaskPath(Transform tr)
        {
            string s = FullPath(tr).ToLowerInvariant();
            return (s.Contains("total") && s.Contains("task")) ||
                   s.Contains("taskcomplete") ||
                   s.Contains("completedtasks");
        }

        private static bool HasTaskBarBits(Transform root)
        {
            if (root == null) return false;

            var sprites = root.GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < sprites.Length; i++)
            {
                var spr = sprites[i];
                if (spr == null) continue;
                string n = FullPath(spr.transform).ToLowerInvariant();
                if (n.Contains("bar") || n.Contains("fill") || n.Contains("progress") || n.Contains("background")) return true;
            }

            var imgs = root.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < imgs.Length; i++)
            {
                var img = imgs[i];
                if (img == null) continue;
                string n = FullPath(img.transform).ToLowerInvariant();
                if (n.Contains("bar") || n.Contains("fill") || n.Contains("progress") || n.Contains("background")) return true;
            }

            return false;
        }

        private static bool LooksLikeTaskBarPart(Component cmp)
        {
            try
            {
                Transform tr = cmp.transform;
                for (int i = 0; i < 5 && tr != null; i++, tr = tr.parent)
                {
                    string n = (tr.name ?? "").ToLowerInvariant();
                    if (n.Contains("shadow") || n.Contains("taskpanel") || n == "tasks") return false;
                    if (LooksLikeTaskRootName(n) ||
                        n.Contains("progress") ||
                        n.Contains("fill") ||
                        n.Contains("bar") ||
                        n.Contains("background") ||
                        n.Contains("border") ||
                        n.Contains("outline") ||
                        n.Contains("frame") ||
                        n.Contains("back"))
                        return true;
                }
            }
            catch { }

            return false;
        }

        private static bool LooksLikeTaskBarPaint(Transform tr, Color col)
        {
            string n = FullPath(tr).ToLowerInvariant();
            if (n.Contains("shadow")) return false;
            if (n.Contains("fill") || n.Contains("progress") || n.Contains("green")) return true;
            if (n.Contains("bar") || n.Contains("background") || n.Contains("border") || n.Contains("outline") || n.Contains("frame") || n.Contains("back")) return true;
            return col.g > 0.35f && col.g > col.r * 1.12f && col.g > col.b * 1.12f;
        }

        private static string FullPath(Transform tr)
        {
            if (tr == null) return "";
            string s = tr.name ?? "";
            tr = tr.parent;
            for (int i = 0; i < 7 && tr != null; i++, tr = tr.parent)
                s = (tr.name ?? "") + "/" + s;
            return s;
        }

        private static void RestoreRgbTaskBar()
        {
            for (int i = rgbTaskSprites.Count - 1; i >= 0; i--)
            {
                var spr = rgbTaskSprites[i];
                if (spr == null) continue;
                int id = spr.GetInstanceID();
                if (rgbTaskSpriteCols.TryGetValue(id, out var col)) spr.color = col;
            }

            for (int i = rgbTaskImgs.Count - 1; i >= 0; i--)
            {
                var img = rgbTaskImgs[i];
                if (img == null) continue;
                int id = img.GetInstanceID();
                if (rgbTaskImgCols.TryGetValue(id, out var col)) img.color = col;
            }

            for (int i = rgbTaskTexts.Count - 1; i >= 0; i--)
            {
                var txt = rgbTaskTexts[i];
                if (txt == null) continue;
                int id = txt.GetInstanceID();
                if (rgbTaskTextCols.TryGetValue(id, out var col)) txt.color = col;
            }

            for (int i = rgbTaskRends.Count - 1; i >= 0; i--)
            {
                var rend = rgbTaskRends[i];
                if (rend == null) continue;
                int id = rend.GetInstanceID();
                if (!rgbTaskRendCols.TryGetValue(id, out var cols)) continue;
                try
                {
                    var mats = rend.materials;
                    if (mats == null) continue;
                    int count = Mathf.Min(mats.Length, cols.Length);
                    for (int m = 0; m < count; m++)
                    {
                        if (mats[m] == null) continue;
                        if (mats[m].HasProperty("_Color")) mats[m].color = cols[m];
                        if (mats[m].HasProperty("_BaseColor")) mats[m].SetColor("_BaseColor", cols[m]);
                        if (mats[m].HasProperty("_TintColor")) mats[m].SetColor("_TintColor", cols[m]);
                        if (mats[m].HasProperty("_EmissionColor")) mats[m].SetColor("_EmissionColor", Color.black);
                        if (rgbTaskRendTex.TryGetValue(id, out var tex) && m < tex.Length && mats[m].HasProperty("_MainTex"))
                            mats[m].SetTexture("_MainTex", tex[m]);
                    }
                }
                catch { }
            }

            rgbTaskSprites.Clear();
            rgbTaskImgs.Clear();
            rgbTaskTexts.Clear();
            rgbTaskRends.Clear();
            rgbTaskSpriteCols.Clear();
            rgbTaskImgCols.Clear();
            rgbTaskTextCols.Clear();
            rgbTaskRendCols.Clear();
            rgbTaskRendTex.Clear();
        }

        private static void ApplyPanicMode()
        {
            if (isPanicked) return;

            isPanicked = true;
            showMenu = false;
            showWatermark = false;
            rgbTaskBar = false;
            RestoreRgbTaskBar();
            ResetAllBindWaitsStatic();

            try
            {
                foreach (var fld in typeof(ElysiumModMenuGUI).GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (fld.FieldType != typeof(bool)) continue;
                    string n = fld.Name;
                    if (n == nameof(isPanicked) || n == nameof(whiteMenuTheme) || n == nameof(boldMenuText)) continue;
                    fld.SetValue(null, false);
                }
            }
            catch { }

            isPanicked = true;
            showMenu = false;
            showWatermark = false;

            try
            {
                var stamp = ModManager.Instance.ModStamp;
                if (stamp) stamp.enabled = false;
            }
            catch { }

            try
            {
                var scene = SceneManager.GetActiveScene();
                if (scene.name == "MainMenu" || scene.name == "MatchMaking")
                    SceneManager.LoadScene(scene.name);
            }
            catch { }

            try { Harmony.UnpatchID("com.elysiummodmenu.harmony"); } catch { }
            try { if (activeGui != null) activeGui.enabled = false; } catch { }
        }

        private static void ResetAllBindWaitsStatic()
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

        [HarmonyPatch]
        public static class ProgressTracker_RgbTaskBar_Patch
        {
            public static MethodBase TargetMethod()
            {
                return AccessTools.Method(typeof(ProgressTracker), "FixedUpdate");
            }

            public static bool Prepare()
            {
                return TargetMethod() != null;
            }

            public static void Postfix()
            {
                try { TickRgbTaskBar(); } catch { }
            }
        }
    }
}
