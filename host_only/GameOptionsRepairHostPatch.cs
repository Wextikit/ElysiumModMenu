#nullable disable
#pragma warning disable CS0162, CS0108, CS0219, CS0661, CS0660, CS8632, CS0168, CS0659
using ElysiumModMenu;
using HarmonyLib;

[HarmonyPatch(typeof(CreateGameOptions), nameof(CreateGameOptions.Confirm))]
public static class CreateGameOptions_RepairHost_Patch
{
    public static void Prefix()
    {
        ElysiumModMenuGUI.RepairHostGameOptions();
    }
}
