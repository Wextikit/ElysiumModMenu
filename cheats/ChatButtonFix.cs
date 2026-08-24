#nullable disable
#pragma warning disable CS0162, CS0108, CS0219, CS0661, CS0660, CS8632, CS0168, CS0659
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;
using Vector3 = UnityEngine.Vector3;

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class ChatButtonFix
{
    private static float _placeAt;
    private static bool _placed;
    private static Vector3 _origEdge;

    public static void Postfix(HudManager __instance)
    {
        try
        {
            if (__instance == null) return;
            ChatController chat = __instance.Chat;
            if (chat == null) return;
            AspectPosition chatAp = chat.chatButtonAspectPosition;
            if (chatAp == null) return;

            bool inMatch = ShipStatus.Instance != null && LobbyBehaviour.Instance == null
                && MeetingHud.Instance == null;

            if (!inMatch)
            {
                if (_placed)
                {
                    _placed = false;
                    try { chatAp.DistanceFromEdge = _origEdge; chatAp.AdjustPosition(); } catch { }
                }
                return;
            }

            if (Time.unscaledTime < _placeAt) return;
            _placeAt = Time.unscaledTime + 0.5f;

            int chatId = ((Object)chatAp).GetInstanceID();

            var aps = ((Component)__instance).GetComponentsInChildren<AspectPosition>(true);
            float[] xs = new float[aps.Length];
            int n = 0;
            float maxX = float.NegativeInfinity;
            for (int i = 0; i < aps.Length; i++)
            {
                AspectPosition ap = aps[i];
                if (ap == null || ((Object)ap).GetInstanceID() == chatId) continue;
                if (ap.Alignment != chatAp.Alignment) continue;
                float x = ap.DistanceFromEdge.x;
                xs[n++] = x;
                if (x > maxX) maxX = x;
            }
            if (n == 0) return;

            float step = 0.8f;
            float minGap = float.PositiveInfinity;
            for (int a = 0; a < n; a++)
                for (int b = a + 1; b < n; b++)
                {
                    float g = Mathf.Abs(xs[a] - xs[b]);
                    if (g > 0.05f && g < minGap) minGap = g;
                }
            if (minGap < float.PositiveInfinity && minGap >= 0.25f) step = minGap;

            Vector3 want = chatAp.DistanceFromEdge;
            want.x = maxX + step * 1.2f;

            if (!_placed) { _origEdge = chatAp.DistanceFromEdge; _placed = true; }

            if (Mathf.Abs(chatAp.DistanceFromEdge.x - want.x) < 1e-4f) return;
            chatAp.updateAlways = false;
            chatAp.DistanceFromEdge = want;
            try { chatAp.AdjustPosition(); } catch { }
        }
        catch { }
    }
}
