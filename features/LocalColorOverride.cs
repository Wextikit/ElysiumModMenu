#nullable disable
using HarmonyLib;
using UnityEngine;

namespace ElysiumModMenu
{
    public partial class ElysiumModMenuGUI
    {
        public static bool localAlwaysRed = false;
        public static bool localFortegreen = false;
        public static bool localSnipeColor = false;
        public static int localSnipeColorId = 0;

        private static PlayerControl localColorPlayer;
        private static byte localColorRestoreId;
        private static bool localColorRestoreReady;
        private static bool restoringLocalColor;
        private static float nextColorSnipeAt;
        private static float nextFortegreenAt;

        private static bool TryGetLocalColorOverride(out byte colorId)
        {
            colorId = 0;
            if (!localAlwaysRed) return false;
            if (AmongUsClient.Instance == null || AmongUsClient.Instance.AmHost || PlayerControl.LocalPlayer == null) return false;

            colorId = 0;
            return true;
        }

        private static void CaptureLocalColor(PlayerControl plr, byte colorId)
        {
            localColorPlayer = plr;
            localColorRestoreId = colorId;
            localColorRestoreReady = true;
        }

        private static void ApplyLocalColorOverride()
        {
            if (!TryGetLocalColorOverride(out byte colorId))
            {
                RestoreLocalColorOverride();
                return;
            }

            PlayerControl plr = PlayerControl.LocalPlayer;
            if (!localColorRestoreReady || localColorPlayer != plr)
            {
                localColorRestoreReady = false;
                if (plr.Data != null && plr.Data.DefaultOutfit != null)
                    CaptureLocalColor(plr, (byte)plr.Data.DefaultOutfit.ColorId);
            }

            if (plr.Data == null || plr.Data.DefaultOutfit == null || plr.Data.DefaultOutfit.ColorId != colorId)
                plr.SetColor(colorId);

            if (plr.Data != null && plr.Data.DefaultOutfit != null)
                plr.Data.DefaultOutfit.ColorId = colorId;
        }

        private static void RestoreLocalColorOverride()
        {
            if (!localColorRestoreReady) return;

            PlayerControl plr = PlayerControl.LocalPlayer;
            if (plr != null && plr == localColorPlayer)
            {
                restoringLocalColor = true;
                try
                {
                    plr.SetColor(localColorRestoreId);
                    if (plr.Data != null && plr.Data.DefaultOutfit != null)
                        plr.Data.DefaultOutfit.ColorId = localColorRestoreId;
                }
                finally
                {
                    restoringLocalColor = false;
                }
            }

            localColorPlayer = null;
            localColorRestoreReady = false;
        }

        private static void TickLocalColorOverride()
        {
            if (PlayerControl.LocalPlayer == null)
            {
                localColorPlayer = null;
                localColorRestoreReady = false;
                return;
            }

            ApplyLocalColorOverride();
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

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.SetColor))]
        public static class LocalColor_SetColor_Patch
        {
            public static void Prefix(PlayerControl __instance, ref byte bodyColor)
            {
                if (restoringLocalColor || __instance == null || __instance != PlayerControl.LocalPlayer) return;
                if (!TryGetLocalColorOverride(out byte colorId)) return;

                if (!localColorRestoreReady || localColorPlayer != __instance || bodyColor != colorId)
                    CaptureLocalColor(__instance, bodyColor);

                bodyColor = colorId;
            }
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CmdCheckColor))]
        public static class LocalColor_CmdCheckColor_Patch
        {
            public static bool Prefix(PlayerControl __instance)
            {
                if (__instance == null || __instance != PlayerControl.LocalPlayer || !TryGetLocalColorOverride(out _))
                    return true;

                ApplyLocalColorOverride();
                return false;
            }
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSetColor))]
        public static class LocalColor_RpcSetColor_Patch
        {
            public static bool Prefix(PlayerControl __instance)
            {
                if (__instance == null || __instance != PlayerControl.LocalPlayer || !TryGetLocalColorOverride(out _))
                    return true;

                ApplyLocalColorOverride();
                return false;
            }
        }
    }
}
