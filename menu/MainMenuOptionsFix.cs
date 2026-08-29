#nullable disable
#pragma warning disable CS0162, CS0108, CS0219, CS0661, CS0660, CS8632, CS0168, CS0659
using HarmonyLib;
using UnityEngine;

namespace ElysiumModMenu
{
    public partial class ElysiumModMenuGUI : MonoBehaviour
    {
        [HarmonyPatch(typeof(OptionsMenuBehaviour), nameof(OptionsMenuBehaviour.Open))]
        public static class OptionsMenuBehaviour_Open_Fix
        {
            public static void Prefix(OptionsMenuBehaviour __instance)
            {
                TryRepairOptionsMenu(__instance);
            }

            public static System.Exception Finalizer(OptionsMenuBehaviour __instance, System.Exception __exception)
            {
                if (__exception == null) return null;

                try
                {
                    TryRepairOptionsMenu(__instance);
                    TryOpenOptionsMenuSoft(__instance);
                }
                catch (global::System.Exception __elysiumCaught347) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught347); }

                return null;
            }
        }

        private static void TryRepairOptionsMenu(OptionsMenuBehaviour menu)
        {
            if (menu == null) return;

            try
            {
                if (menu.DefaultButtonSelected == null && menu.BackButton != null)
                    menu.DefaultButtonSelected = menu.BackButton;
            }
            catch (global::System.Exception __elysiumCaught348) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught348); }

            try
            {
                if (menu.ControllerSelectable == null)
                    menu.ControllerSelectable = new Il2CppSystem.Collections.Generic.List<UiElement>();
            }
            catch (global::System.Exception __elysiumCaught349) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught349); }

            try
            {
                if (menu.Tabs == null)
                {
                    TabGroup[] tabs = menu.GetComponentsInChildren<TabGroup>(true);
                    if (tabs != null && tabs.Length > 0)
                        menu.Tabs = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<TabGroup>(tabs);
                }
            }
            catch (global::System.Exception __elysiumCaught350) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught350); }
        }

        private static void TryOpenOptionsMenuSoft(OptionsMenuBehaviour menu)
        {
            if (menu == null) return;

            try { if (menu.gameObject != null) menu.gameObject.SetActive(true); } catch (global::System.Exception __elysiumCaught351) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught351); }
            try { if (menu.Background != null && menu.Background.gameObject != null) menu.Background.gameObject.SetActive(true); } catch (global::System.Exception __elysiumCaught352) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught352); }
            try { menu.ResetText(); } catch (global::System.Exception __elysiumCaught353) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught353); }
            try { menu.UpdateButtons(); } catch (global::System.Exception __elysiumCaught354) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught354); }
            try { menu.OpenTabGroup(0); } catch (global::System.Exception __elysiumCaught355) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught355); }
            try { if (menu.MenuButton != null) menu.MenuButton.SelectButton(true); } catch (global::System.Exception __elysiumCaught356) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught356); }
            try { menu.GrabControllerButtons(); } catch (global::System.Exception __elysiumCaught357) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught357); }
        }
    }
}
