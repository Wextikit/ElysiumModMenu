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
                catch { }

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
            catch { }

            try
            {
                if (menu.ControllerSelectable == null)
                    menu.ControllerSelectable = new Il2CppSystem.Collections.Generic.List<UiElement>();
            }
            catch { }

            try
            {
                if (menu.Tabs == null)
                {
                    TabGroup[] tabs = menu.GetComponentsInChildren<TabGroup>(true);
                    if (tabs != null && tabs.Length > 0)
                        menu.Tabs = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<TabGroup>(tabs);
                }
            }
            catch { }
        }

        private static void TryOpenOptionsMenuSoft(OptionsMenuBehaviour menu)
        {
            if (menu == null) return;

            try { if (menu.gameObject != null) menu.gameObject.SetActive(true); } catch { }
            try { if (menu.Background != null && menu.Background.gameObject != null) menu.Background.gameObject.SetActive(true); } catch { }
            try { menu.ResetText(); } catch { }
            try { menu.UpdateButtons(); } catch { }
            try { menu.OpenTabGroup(0); } catch { }
            try { if (menu.MenuButton != null) menu.MenuButton.SelectButton(true); } catch { }
            try { menu.GrabControllerButtons(); } catch { }
        }
    }
}
