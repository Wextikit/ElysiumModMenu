#nullable disable
#pragma warning disable CS0162, CS0108, CS0219, CS0661, CS0660, CS8632, CS0168, CS0659
using UnityEngine;

namespace ElysiumModMenu
{
    public partial class ElysiumModMenuGUI : MonoBehaviour
    {
        private static Texture2D espBoxTex;
        private static GUIStyle espBoxStyle;

        private static void DrawEspBoxes()
        {
            if (!showEspBoxes) return;
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

            GUI.color = Color.white;
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
    }
}
