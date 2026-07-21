#nullable disable
using UnityEngine;

namespace ElysiumModMenu
{
    public partial class ElysiumModMenuGUI
    {
        public static bool localAlwaysRed = false;
        public static bool localFortegreen = false;
        public static bool localSnipeColor = false;
        public static int localSnipeColorId = 0;

        private static float nextColorSnipeAt;
        private static float nextFortegreenAt;

        private static void ApplyLocalColorOverride()
        {
            if (localAlwaysRed)
                SendAlwaysRed();
        }

        private static void SendAlwaysRed()
        {
            PlayerControl plr = PlayerControl.LocalPlayer;
            if (plr == null) return;

            try
            {
                plr.RpcSetColor(0);
                if (plr.Data != null && plr.Data.DefaultOutfit != null)
                    plr.Data.DefaultOutfit.ColorId = 0;
            }
            catch { }
        }

        private static void TickLocalColorOverride()
        {
            TickFortegreenColor();
        }

        private static void TickLocalColorSnipe()
        {
            if (!localSnipeColor || AmongUsClient.Instance == null || AmongUsClient.Instance.AmHost ||
                PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data == null ||
                PlayerControl.LocalPlayer.Data.DefaultOutfit == null ||
                (LobbyBehaviour.Instance == null && ShipStatus.Instance == null))
            {
                nextColorSnipeAt = 0f;
                return;
            }

            int colorId = Mathf.Clamp(localSnipeColorId, 0, 17);
            localSnipeColorId = colorId;
            if (PlayerControl.LocalPlayer.Data.DefaultOutfit.ColorId == colorId)
            {
                nextColorSnipeAt = 0f;
                return;
            }

            if (!IsLocalColorAvailable(colorId))
            {
                nextColorSnipeAt = 0f;
                return;
            }

            if (Time.unscaledTime < nextColorSnipeAt) return;
            nextColorSnipeAt = Time.unscaledTime + 1f;

            try { PlayerControl.LocalPlayer.CmdCheckColor((byte)colorId); }
            catch { }
        }

        private static void TickFortegreenColor()
        {
            if (!localFortegreen || AmongUsClient.Instance == null || AmongUsClient.Instance.AmHost ||
                PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data == null ||
                PlayerControl.LocalPlayer.Data.DefaultOutfit == null ||
                (LobbyBehaviour.Instance == null && ShipStatus.Instance == null))
            {
                nextFortegreenAt = 0f;
                return;
            }

            if (PlayerControl.LocalPlayer.Data.DefaultOutfit.ColorId == 18)
            {
                nextFortegreenAt = 0f;
                return;
            }

            if (Time.unscaledTime < nextFortegreenAt) return;
            nextFortegreenAt = Time.unscaledTime + 1f;

            try { PlayerControl.LocalPlayer.RpcSetColor(18); }
            catch { }
        }

        private static bool IsLocalColorAvailable(int colorId)
        {
            try
            {
                if (PlayerControl.AllPlayerControls == null) return false;

                foreach (PlayerControl plr in PlayerControl.AllPlayerControls)
                {
                    if (plr == null || plr == PlayerControl.LocalPlayer || plr.Data == null ||
                        plr.Data.Disconnected || plr.Data.DefaultOutfit == null)
                        continue;

                    if (plr.Data.DefaultOutfit.ColorId == colorId)
                        return false;
                }

                return true;
            }
            catch { return false; }
        }

        private static void ResetLocalColorSnipe()
        {
            nextColorSnipeAt = 0f;
        }

        private static void ResetFortegreenColor()
        {
            nextFortegreenAt = 0f;
        }

    }
}
