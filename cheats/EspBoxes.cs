#nullable disable
#pragma warning disable CS0162, CS0108, CS0219, CS0661, CS0660, CS8632, CS0168, CS0659
using System.Collections.Generic;
using UnityEngine;

namespace ElysiumModMenu
{
    public partial class ElysiumModMenuGUI : MonoBehaviour
    {
        private static Texture2D espBoxTex;
        private static GUIStyle espBoxStyle;
        private static GUIStyle espTaskLabelStyle;
        private static readonly List<Vector2> espTaskTargets = new List<Vector2>(16);
        private static float espTaskTargetsAt = -1f;

        private static void DrawEspBoxes()
        {
            if (!showEspBoxes && !showTaskArrows) return;
            if (Event.current == null || Event.current.type != EventType.Repaint) return;
            if (MeetingHud.Instance != null || ExileController.Instance != null || IntroCutscene.Instance != null) return;
            if (PlayerControl.LocalPlayer == null || PlayerControl.AllPlayerControls == null || Camera.main == null) return;

            if (espBoxTex == null)
            {
                espBoxTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                espBoxTex.SetPixel(0, 0, Color.white);
                espBoxTex.Apply();
                espBoxTex.hideFlags = HideFlags.HideAndDontSave;
                espBoxStyle = new GUIStyle(GUIStyle.none);
                espBoxStyle.normal.background = espBoxTex;
            }

            if (showEspBoxes)
            {
                foreach (PlayerControl pc in PlayerControl.AllPlayerControls)
                {
                    if (pc == null || pc == PlayerControl.LocalPlayer || pc.Data == null || pc.Data.Disconnected) continue;
                    if (pc.Data.IsDead && !seeGhosts) continue;

                    Vector3 pos = pc.transform.position;
                    Vector3 foot = Camera.main.WorldToScreenPoint(pos + new Vector3(0f, -0.38f, 0f));
                    Vector3 head = Camera.main.WorldToScreenPoint(pos + new Vector3(0f, 0.82f, 0f));
                    if (foot.z < 0f || head.z < 0f) continue;

                    float y1 = Screen.height - head.y;
                    float y2 = Screen.height - foot.y;
                    float h = Mathf.Abs(y2 - y1);
                    if (h < 12f) continue;
                    float w = h * 0.48f;
                    float x = head.x - w * 0.5f;
                    Rect r = new Rect(x, y1, w, h);

                    Color c = GetEspBoxColor(pc);
                    GUI.color = new Color(c.r, c.g, c.b, 0.95f);
                    DrawEspLine(new Rect(r.x, r.y, r.width, 2f));
                    DrawEspLine(new Rect(r.x, r.yMax - 2f, r.width, 2f));
                    DrawEspLine(new Rect(r.x, r.y, 2f, r.height));
                    DrawEspLine(new Rect(r.xMax - 2f, r.y, 2f, r.height));
                }
            }

            if (showTaskArrows) DrawEspTaskTracers(PlayerControl.LocalPlayer);
            GUI.color = Color.white;
        }

        private static void DrawEspTaskTracers(PlayerControl local)
        {
            if (Time.unscaledTime - espTaskTargetsAt > 0.5f)
            {
                espTaskTargetsAt = Time.unscaledTime;
                RebuildEspTaskTargets(local);
            }

            if (espTaskTargets.Count == 0) return;
            if (espTaskLabelStyle == null)
            {
                espTaskLabelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 11,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    richText = false
                };
            }

            Camera cam = Camera.main;
            Vector2 from = EspScreenPoint(cam, local.GetTruePosition());
            Vector2 localPos = local.GetTruePosition();
            Color lineCol = new Color(0.58f, 0.88f, 0.66f, 0.58f);
            Color textCol = new Color(0.68f, 0.94f, 0.74f, 0.92f);

            for (int i = 0; i < espTaskTargets.Count; i++)
            {
                Vector2 target = espTaskTargets[i];
                Vector2 delta = EspScreenPoint(cam, target) - from;
                if (delta.sqrMagnitude < 4f) continue;
                Vector2 end = ClipEspTaskTracer(from, delta);
                DrawEspLine(from, end, lineCol, 2.2f);
                Color markerOld = GUI.color;
                GUI.color = textCol;
                DrawEspLine(new Rect(end.x - 2.5f, end.y - 2.5f, 5f, 5f));
                GUI.color = markerOld;

                string distance = Mathf.RoundToInt(Vector2.Distance(target, localPos)) + "m";
                Vector2 dir = delta.normalized;
                Rect label = new Rect(end.x - dir.x * 34f - 30f, end.y - dir.y * 18f - 8f, 60f, 16f);
                Color old = GUI.color;
                GUI.color = new Color(0f, 0f, 0f, 0.82f);
                GUI.Label(new Rect(label.x + 1f, label.y + 1f, label.width, label.height), distance, espTaskLabelStyle);
                GUI.color = textCol;
                GUI.Label(label, distance, espTaskLabelStyle);
                GUI.color = old;
            }
        }

        private static Vector2 ClipEspTaskTracer(Vector2 from, Vector2 delta)
        {
            float t = 1f;
            const float margin = 8f;

            if (delta.x > 0f) t = Mathf.Min(t, (Screen.width - margin - from.x) / delta.x);
            else if (delta.x < 0f) t = Mathf.Min(t, (margin - from.x) / delta.x);
            if (delta.y > 0f) t = Mathf.Min(t, (Screen.height - margin - from.y) / delta.y);
            else if (delta.y < 0f) t = Mathf.Min(t, (margin - from.y) / delta.y);

            return from + delta * Mathf.Clamp01(t);
        }

        private static void RebuildEspTaskTargets(PlayerControl local)
        {
            espTaskTargets.Clear();
            try
            {
                if (local == null || local.myTasks == null) return;

                for (int i = 0; i < local.myTasks.Count; i++)
                {
                    PlayerTask task = local.myTasks[i];
                    if (task == null || task.IsComplete) continue;

                    NormalPlayerTask normal = task as NormalPlayerTask;
                    if (normal != null && TryAddTaskArrowTarget(normal)) continue;
                    if (TryAddCurrentTaskLocation(task)) continue;

                    if (ShipStatus.Instance == null) continue;
                    var templates = ShipStatus.Instance.GetAllTasks();
                    if (templates == null) continue;
                    foreach (PlayerTask templateTask in templates)
                    {
                        if (templateTask == null || templateTask.TaskType != task.TaskType) continue;
                        if (TryAddCurrentTaskLocation(templateTask, task.TaskStep)) break;
                    }
                }
            }
            catch (global::System.Exception __elysiumCaught71) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught71); }
        }

        private static bool TryAddTaskArrowTarget(NormalPlayerTask task)
        {
            try
            {
                if (task == null || task.Arrow == null) return false;
                Vector3 target = task.Arrow.target;
                return AddEspTaskTarget(new Vector2(target.x, target.y));
            }
            catch { return false; }
        }

        private static bool TryAddCurrentTaskLocation(PlayerTask task, int requestedStep = -1)
        {
            try
            {
                if (task == null || task.Locations == null || task.Locations.Count == 0) return false;
                int step = requestedStep >= 0 ? requestedStep : task.TaskStep;
                step = Mathf.Clamp(step, 0, task.Locations.Count - 1);
                return AddEspTaskTarget(task.Locations[step]);
            }
            catch (global::System.Exception __elysiumCaught72) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught72); }
            return false;
        }

        private static bool AddEspTaskTarget(Vector2 target)
        {
            if (float.IsNaN(target.x) || float.IsNaN(target.y) || float.IsInfinity(target.x) || float.IsInfinity(target.y))
                return false;

            for (int i = 0; i < espTaskTargets.Count; i++)
                if ((espTaskTargets[i] - target).sqrMagnitude < 0.01f)
                    return true;

            espTaskTargets.Add(target);
            return true;
        }

        private static Vector2 EspScreenPoint(Camera cam, Vector2 world)
        {
            Vector3 point = cam.WorldToScreenPoint(new Vector3(world.x, world.y, 0f));
            return new Vector2(point.x, Screen.height - point.y);
        }

        private static Color GetEspBoxColor(PlayerControl pc)
        {
            try
            {
                Color rgb = GetEspColor(pc, Color.clear);
                if (rgb != Color.clear) return rgb;

                if (seeRoles && pc.Data.Role != null)
                    return GetRoleColor((int)pc.Data.Role.Role, pc.Data.Role.TeamColor);

                int cid = pc.Data.DefaultOutfit.ColorId;
                if (Palette.PlayerColors != null && cid >= 0 && cid < Palette.PlayerColors.Length)
                    return Palette.PlayerColors[cid];
            }
            catch (global::System.Exception __elysiumCaught73) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught73); }

            return Color.white;
        }

        private static void DrawEspLine(Rect r)
        {
            GUI.Box(r, GUIContent.none, espBoxStyle);
        }

        private static void DrawEspLine(Vector2 from, Vector2 to, Color color, float width)
        {
            float dx = to.x - from.x;
            float dy = to.y - from.y;
            float length = Mathf.Sqrt(dx * dx + dy * dy);
            if (length < 2f) return;

            Matrix4x4 oldMatrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(Mathf.Atan2(dy, dx) * Mathf.Rad2Deg, from);
            GUI.color = color;
            DrawEspLine(new Rect(from.x, from.y - width * 0.5f, length, width));
            GUI.matrix = oldMatrix;
        }
    }
}
