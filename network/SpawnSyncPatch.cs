#nullable disable
using HarmonyLib;
using Hazel;
using InnerNet;

namespace ElysiumModMenu
{
    [HarmonyPatch(typeof(InnerNetObjectCollection), nameof(InnerNetObjectCollection.TryAddNetObject))]
    internal static class SpawnSyncPatch
    {
        public static bool Prefix(InnerNetObjectCollection __instance, InnerNetObject obj, ref bool __result)
        {
            if (obj == null) return true;

            int sendMode = (int)obj.sendMode;
            if (sendMode != (int)SendOption.None && sendMode != (int)SendOption.Reliable)
                obj.sendMode = SendOption.Reliable;

            if (obj.NetId == 0 || __instance == null || __instance.allObjectsFast == null)
                return true;

            if (__instance.allObjectsFast.TryGetValue(obj.NetId, out InnerNetObject current) && current == obj)
            {
                __result = false;
                return false;
            }

            return true;
        }
    }
}
