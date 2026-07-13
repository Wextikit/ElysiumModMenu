#nullable disable
using UnityEngine;

namespace ElysiumModMenu
{
    public partial class ElysiumModMenuGUI : MonoBehaviour
    {
        private static float alwaysChatBlockedUntil = -1f;
        private static float roundStartBlockedUntil = -1f;

public static void BlockAlwaysChatIntro()
        {
            alwaysChatBlockedUntil = Time.unscaledTime + 4f;
        }

public static void BlockRoundStartUi(float sec = 6f)
        {
            float until = Time.unscaledTime + sec;
            if (until > roundStartBlockedUntil) roundStartBlockedUntil = until;
        }

public static void ClearAlwaysChatIntroBlock()
        {
            alwaysChatBlockedUntil = -1f;
            roundStartBlockedUntil = -1f;
        }

public static bool IsRoundStartTransition()
        {
            if (IsIntroCutsceneActive()) return true;
            if (roundStartBlockedUntil > 0f && Time.unscaledTime < roundStartBlockedUntil) return true;
            return alwaysChatBlockedUntil > 0f && Time.unscaledTime < alwaysChatBlockedUntil;
        }

public static bool IsIntroCutsceneActive()
        {
            try
            {
                IntroCutscene intro = IntroCutscene.Instance;
                if (intro == null) return false;
                if (intro.gameObject == null) return true;
                return intro.gameObject.activeInHierarchy;
            }
            catch { return IntroCutscene.Instance != null; }
        }

public static bool CanForceAlwaysChat()
        {
            if (!alwaysChat) return false;
            if (MeetingHud.Instance != null || ExileController.Instance != null || IsIntroCutsceneActive()) return false;
            if (Minigame.Instance != null) return false;
            if (IsRoundStartTransition()) return false;

            try
            {
                if (AmongUsClient.Instance == null || !AmongUsClient.Instance.IsGameStarted) return true;
                if (!HasCurrentGameSeenShhh()) return false;
                if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data == null) return false;
            }
            catch { return false; }

            return true;
        }
    }
}
