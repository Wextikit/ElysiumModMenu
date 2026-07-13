#nullable disable
#pragma warning disable CS0162, CS0108, CS0219, CS0661, CS0660, CS8632, CS0168, CS0659
using ElysiumModMenu;
using HarmonyLib;

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSyncSettings))]
public static class PlayerControl_RpcSyncSettings_Patch
{
    public static bool Prefix(PlayerControl __instance)
    {
        try
        {
            if (__instance == PlayerControl.LocalPlayer && ElysiumModMenuGUI.BlockDirectSettingsSync())
                return false;
        }
        catch { }
        return true;
    }
}
