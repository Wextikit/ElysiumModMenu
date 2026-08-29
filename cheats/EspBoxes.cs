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

            if (showTaskArrows) DrawEspTaskArrows(PlayerControl.LocalPlayer);
            GUI.color = Color.white;
        }

        private static void DrawEspTaskArrows(PlayerControl local)
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
            Color col = new Color(0.42f, 0.92f, 0.55f, 0.92f);

            for (int i = 0; i < espTaskTargets.Count; i++)
            {
                Vector2 target = espTaskTargets[i];
                Vector2 delta = EspScreenPoint(cam, target) - from;
                if (delta.sqrMagnitude < 4f) continue;
                delta.Normalize();

                Vector2 tip = from + delta * 92f;
                DrawEspArrow(tip, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg, 15f, col);

                string distance = Mathf.RoundToInt(Vector2.Distance(target, localPos)) + "m";
                Rect label = new Rect(tip.x + delta.x * 20f - 30f, tip.y + delta.y * 20f - 8f, 60f, 16f);
                Color old = GUI.color;
                GUI.color = new Color(0f, 0f, 0f, 0.82f);
                GUI.Label(new Rect(label.x + 1f, label.y + 1f, label.width, label.height), distance, espTaskLabelStyle);
                GUI.color = col;
                GUI.Label(label, distance, espTaskLabelStyle);
                GUI.color = old;
            }
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
                    if (!(task is NormalPlayerTask normal) || normal.Locations == null || normal.Locations.Count == 0) continue;

                    int step = Mathf.Clamp(normal.taskStep, 0, normal.Locations.Count - 1);
                    espTaskTargets.Add(normal.Locations[step]);
                }
            }
            catch { }
        }

        private static Vector2 EspScreenPoint(Camera cam, Vector2 world)
        {
            Vector3 point = cam.WorldToScreenPoint(new Vector3(world.x, world.y, 0f));
            return new Vector2(point.x, Screen.height - point.y);
        }

        private static void DrawEspArrow(Vector2 tip, float angle, float length, Color color)
        {
            float radians = angle * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
            DrawEspLine(tip - dir * length, tip, color, 3.5f);

            float left = (angle + 150f) * Mathf.Deg2Rad;
            float right = (angle - 150f) * Mathf.Deg2Rad;
            DrawEspLine(tip, tip + new Vector2(Mathf.Cos(left), Mathf.Sin(left)) * length * 0.7f, color, 3.5f);
            DrawEspLine(tip, tip + new Vector2(Mathf.Cos(right), Mathf.Sin(right)) * length * 0.7f, color, 3.5f);
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
            catch { }

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
