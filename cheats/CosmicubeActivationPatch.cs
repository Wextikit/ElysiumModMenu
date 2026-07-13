#nullable disable
using HarmonyLib;
using UnityEngine;

namespace ElysiumModMenu
{
    internal static class UnlockCosmeticsCosmicubeActivation
    {
        internal static void Refresh(CubesTab tab, CosmicubeData selectedCube = null)
        {
            if (!ElysiumModMenuGUI.activateCompletedCosmicubes || tab == null) return;

            try
            {
                CosmicubeData cube = selectedCube ?? tab.currentCube;
                PassiveButton activateButton = tab.activateButton;
                if (cube == null || activateButton == null) return;

                if (!DestroyableSingleton<CosmicubeManager>.InstanceExists) return;
                CosmicubeManager manager = DestroyableSingleton<CosmicubeManager>.Instance;
                if (manager == null || manager.GetCompletionProgress(cube) < 0.999f) return;

                // UI-only override. The game's original ActivateCube handler remains
                // responsible for changing the locally selected cube.
                if (activateButton.gameObject != null)
                    activateButton.gameObject.SetActive(true);
                activateButton.SetButtonEnableState(true);
            }
            catch { }
        }
    }

    [HarmonyPatch(typeof(CubesTab), "ActivateCube")]
    internal static class UnlockCosmetics_CubesTab_ActivateCube_Patch
    {
        public static bool Prefix(CubesTab __instance)
        {
            if (!ElysiumModMenuGUI.activateCompletedCosmicubes || __instance == null) return true;

            try
            {
                CosmicubeData cube = __instance.currentCube;
                if (cube == null || !DestroyableSingleton<CosmicubeManager>.InstanceExists) return true;

                CosmicubeManager manager = DestroyableSingleton<CosmicubeManager>.Instance;
                if (manager == null || manager.GetCompletionProgress(cube) < 0.999f) return true;

                string productId = cube.ProdId;
                if (string.IsNullOrWhiteSpace(productId)) productId = cube.productId;
                if (string.IsNullOrWhiteSpace(productId)) return true;

                AmongUs.Data.DataManager.Player.Store.ActiveCosmicube = productId;
                UnlockCosmeticsCosmicubeActivation.Refresh(__instance, cube);
                return false;
            }
            catch { return true; }
        }
    }

    [HarmonyPatch(typeof(CubesTab), "SelectCube")]
    internal static class UnlockCosmetics_CubesTab_SelectCube_Patch
    {
        public static void Postfix(CubesTab __instance, CosmicubeData cube)
        {
            UnlockCosmeticsCosmicubeActivation.Refresh(__instance, cube);
        }
    }

    [HarmonyPatch(typeof(CubesTab), nameof(CubesTab.OnEnable))]
    internal static class UnlockCosmetics_CubesTab_OnEnable_Patch
    {
        public static void Postfix(CubesTab __instance)
        {
            UnlockCosmeticsCosmicubeActivation.Refresh(__instance);
        }
    }
}
