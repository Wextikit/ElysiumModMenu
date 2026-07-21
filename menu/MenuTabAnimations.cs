#nullable disable
using System.Collections.Generic;
using UnityEngine;

namespace ElysiumModMenu
{
    public partial class ElysiumModMenuGUI : MonoBehaviour
    {
        private sealed class MultiTabAnim
        {
            public float Progress = 1f;
            public int Dir = 1;
        }

        private readonly Dictionary<string, MultiTabAnim> multiTabAnims = new Dictionary<string, MultiTabAnim>();

        private MultiTabAnim GetMultiTabAnim(string key)
        {
            if (!multiTabAnims.TryGetValue(key, out MultiTabAnim anim))
            {
                anim = new MultiTabAnim();
                multiTabAnims[key] = anim;
            }

            return anim;
        }

        private bool SetMultiTab(string key, ref int current, int tab, int count, bool resetScroll = true)
        {
            tab = Mathf.Clamp(tab, 0, count - 1);
            if (resetScroll)
                scrollPosition = Vector2.zero;
            if (current == tab)
                return false;

            MultiTabAnim anim = GetMultiTabAnim(key);
            anim.Dir = tab > current ? 1 : -1;
            anim.Progress = 0f;
            current = tab;
            return true;
        }

        private void BeginMultiTabContent(string key, out Matrix4x4 oldMatrix, out Color oldColor)
        {
            MultiTabAnim anim = GetMultiTabAnim(key);
            if (Event.current != null && Event.current.type == EventType.Repaint && anim.Progress < 1f)
            {
                anim.Progress += Time.unscaledDeltaTime * 10f;
                if (anim.Progress > 1f)
                    anim.Progress = 1f;
            }

            float ease = SmoothMenuTab(anim.Progress);
            float slide = (1f - ease) * 18f * anim.Dir;
            oldMatrix = GUI.matrix;
            oldColor = GUI.color;
            GUI.matrix = Matrix4x4.Translate(new Vector3(slide, 0f, 0f)) * oldMatrix;
            GUI.color = new Color(oldColor.r, oldColor.g, oldColor.b, oldColor.a * Mathf.Clamp01(ease * 1.25f));
        }

        private static void EndMultiTabContent(Matrix4x4 oldMatrix, Color oldColor)
        {
            GUI.matrix = oldMatrix;
            GUI.color = oldColor;
        }
    }
}
