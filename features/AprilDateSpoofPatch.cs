#nullable disable
#pragma warning disable CS0162, CS0108, CS0219, CS0661, CS0660, CS8632, CS0168, CS0659
using HarmonyLib;

namespace ElysiumModMenu
{
    [HarmonyPatch(typeof(EOSManager), nameof(EOSManager.HasServerTimestamp), MethodType.Getter)]
    public static class AprilDateSpoof_HasServerTimestamp_Patch
    {
        public static void Postfix(ref bool __result)
        {
            if (ElysiumModMenuGUI.spoofAprilFoolsDate)
                __result = true;
        }
    }

    [HarmonyPatch(typeof(EOSManager), nameof(EOSManager.ApproximateServerTime), MethodType.Getter)]
    public static class AprilDateSpoof_ApproximateServerTime_Patch
    {
        public static void Postfix(ref Il2CppSystem.DateTime __result)
        {
            if (!ElysiumModMenuGUI.spoofAprilFoolsDate) return;

            var dt = new System.DateTime(System.DateTime.UtcNow.Year, 4, 1, 7, 1, 0, System.DateTimeKind.Utc);
            __result = new Il2CppSystem.DateTime(dt.Ticks);
        }
    }
}
