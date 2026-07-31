#nullable disable
using ElysiumModMenu;
using HarmonyLib;
using UnityEngine;

[HarmonyPatch(typeof(EOSManager), nameof(EOSManager.StartInitialLoginFlow))]
public static class GuestAccountSpoofPatch
{
    public static bool Prefix(EOSManager __instance)
    {
        bool guest = ElysiumModMenuGUI.spoofGuestAccount ||
            PlayerPrefs.GetInt("M_SpoofGuestAccount", 0) == 1;
        if (!guest || __instance == null) return true;

        try
        {
            __instance.StartTempAccountFlow();
            __instance.CloseStartupWaitScreen();
            return false;
        }
        catch
        {
            return true;
        }
    }
}
