#nullable disable
using Il2CppInterop.Runtime;
using Hazel;
using HarmonyLib;
using InnerNet;
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ElysiumModMenu
{
    public partial class ElysiumModMenuGUI : MonoBehaviour
    {
private static MeetingHud SpawnMeetingHudSafe()
        {
            HudManager hud = DestroyableSingleton<HudManager>.Instance;
            if (hud == null || hud.MeetingPrefab == null || AmongUsClient.Instance == null) return null;

            MeetingHud mtg = Object.Instantiate<MeetingHud>(hud.MeetingPrefab);
            InnerNetObject obj = mtg.Cast<InnerNetObject>();
            FixSendMode(obj, mtg.gameObject);
            AmongUsClient.Instance.Spawn(obj, -2, SpawnFlags.None);
            return mtg;
        }

private static void FixSendMode(InnerNetObject obj, GameObject root)
        {
            try { if (obj != null) obj.sendMode = SendOption.Reliable; } catch { }
            if (root == null) return;

            try
            {
                InnerNetObject[] nets = root.GetComponentsInChildren<InnerNetObject>(true);
                foreach (InnerNetObject net in nets)
                {
                    try { if (net != null) net.sendMode = SendOption.Reliable; } catch { }
                }
            }
            catch { }

            try
            {
                MonoBehaviour[] comps = root.GetComponentsInChildren<MonoBehaviour>(true);
                foreach (MonoBehaviour comp in comps)
                {
                    try
                    {
                        InnerNetObject net = comp.TryCast<InnerNetObject>();
                        if (net != null) net.sendMode = SendOption.Reliable;
                    }
                    catch { }
                    FixSendModeObj(comp);
                }
            }
            catch { }
        }

private static void FixSendModeObj(object obj)
        {
            if (obj == null) return;
            try
            {
                Type type = obj.GetType();
                var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                var prop = type.GetProperty("sendMode", flags) ?? type.GetProperty("SendMode", flags);
                if (prop != null && prop.CanWrite)
                {
                    object val = Enum.Parse(prop.PropertyType, "Reliable");
                    prop.SetValue(obj, val, null);
                    return;
                }

                var field = type.GetField("sendMode", flags) ?? type.GetField("SendMode", flags);
                if (field != null)
                {
                    object val = Enum.Parse(field.FieldType, "Reliable");
                    field.SetValue(obj, val);
                }
            }
            catch { }
        }

        private static bool IsChatUiBusy()
        {
            try
            {
                ChatController chat = HudManager.Instance?.Chat;
                if (chat == null || chat.gameObject == null) return false;
                if (!chat.gameObject.activeInHierarchy) return false;

                if (chat.freeChatField != null && chat.freeChatField.textArea != null && chat.freeChatField.textArea.hasFocus)
                    return true;

                return true;
            }
            catch { return false; }
        }

private static void HideMenuForMeeting()
        {
            try
            {
                if (showMenu)
                {
                    showMenu = false;
                    settingsDirty = true;
                }

                isEditingName = false;
                isEditingLevel = false;
                isEditingFriendCode = false;
                isEditingLocalFriendCode = false;
                isEditingGhostChatColor = false;
                isEditingBan = false;
                isEditingFpsLimit = false;
                isEditingBugRoomTimedAutoRun = false;
                customSpoofRpcInputFocused = false;
            }
            catch { }
        }

private static void KeepMeetingHudVisible()
        {
            try
            {
                HideMenuForMeeting();

                if (MeetingHud.Instance != null && MeetingHud.Instance.gameObject != null)
                    MeetingHud.Instance.gameObject.SetActive(true);

                if (HudManager.Instance != null && HudManager.Instance.Chat != null && HudManager.Instance.Chat.gameObject != null)
                    HudManager.Instance.Chat.gameObject.SetActive(false);
            }
            catch { }
        }

private static bool BlockEarlyEmergencyCall(string src)
        {
            try
            {
                bool block = IsRoundStartTransition() ||
                             IsIntroCutsceneActive() ||
                             ExileController.Instance != null ||
                             AmongUsClient.Instance == null ||
                             !AmongUsClient.Instance.IsGameStarted ||
                             ShipStatus.Instance == null ||
                             PlayerControl.LocalPlayer == null ||
                             PlayerControl.LocalPlayer.Data == null ||
                             !HasCurrentGameSeenShhh();

                if (!block) return false;

                try
                {
                    Plugin.Instance?.Log?.LogWarning((object)$"[MEETING] blocked early {src}: intro={IsIntroCutsceneActive()}, mtg={MeetingHud.Instance != null}, mini={Minigame.Instance != null}, shhh={HasCurrentGameSeenShhh()}");
                }
                catch { }
                return true;
            }
            catch
            {
                return true;
            }
        }

[HarmonyPatch(typeof(IntroCutscene), "CoBegin")]
        public static class IntroCutscene_CoBegin_MeetingGuard_Patch
        {
            public static void Prefix()
            {
                BlockRoundStartUi(10f);
            }
        }

[HarmonyPatch(typeof(HudManager), nameof(HudManager.OpenMeetingRoom))]
        public static class HudManager_OpenMeetingRoom_MenuFix_Patch
        {
            public static void Prefix()
            {
                HideMenuForMeeting();
            }

            public static void Postfix()
            {
                KeepMeetingHudVisible();
            }
        }

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.StartMeeting))]
        public static class PlayerControl_StartMeeting_EarlyBlock_Patch
        {
            public static bool Prefix()
            {
                if (BlockEarlyEmergencyCall("start"))
                    return false;

                HideMenuForMeeting();
                return true;
            }
        }

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcStartMeeting))]
        public static class PlayerControl_RpcStartMeeting_EarlyBlock_Patch
        {
            public static bool Prefix()
            {
                if (BlockEarlyEmergencyCall("rpc-start"))
                    return false;

                HideMenuForMeeting();
                return true;
            }
        }

[HarmonyPatch(typeof(MeetingRoomManager), nameof(MeetingRoomManager.AssignSelf))]
        public static class MeetingRoomManager_AssignSelf_EarlyBlock_Patch
        {
            public static bool Prefix()
            {
                if (BlockEarlyEmergencyCall("assign"))
                    return false;

                HideMenuForMeeting();
                return true;
            }
        }

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CmdReportDeadBody))]
        public static class PlayerControl_CmdReportDeadBody_MenuFix_Patch
        {
            public static bool Prefix(NetworkedPlayerInfo target)
            {
                if (target == null && BlockEarlyEmergencyCall("report"))
                    return false;

                HideMenuForMeeting();
                return true;
            }
        }

[HarmonyPatch(typeof(EmergencyMinigame), nameof(EmergencyMinigame.CallMeeting))]
        public static class EmergencyMinigame_CallMeeting_EarlyBlock_Patch
        {
            public static bool Prefix(EmergencyMinigame __instance)
            {
                if (!BlockEarlyEmergencyCall("button-call"))
                    return true;

                try { __instance?.Close(); } catch { }
                return false;
            }
        }

[HarmonyPatch(typeof(EmergencyMinigame), nameof(EmergencyMinigame.Begin))]
        public static class EmergencyMinigame_Begin_EarlyBlock_Patch
        {
            public static bool Prefix(EmergencyMinigame __instance)
            {
                if (!BlockEarlyEmergencyCall("button"))
                    return true;

                try
                {
                    if (__instance != null && __instance.gameObject != null)
                        __instance.gameObject.SetActive(false);
                }
                catch { }

                return false;
            }
        }

[HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.Spawn))]
        public static class InnerNetClient_Spawn_MeetingSendMode_Patch
        {
            public static void Prefix(InnerNetObject netObjParent)
            {
                if (netObjParent == null) return;

                try
                {
                    GameObject obj = netObjParent.gameObject;
                    if (obj == null) return;

                    bool mtg = false;
                    try { mtg = obj.GetComponent<MeetingHud>() != null; } catch { }
                    if (!mtg)
                    {
                        string nm = obj.name ?? string.Empty;
                        mtg = nm.IndexOf("Meeting", StringComparison.OrdinalIgnoreCase) >= 0 ||
                              nm.IndexOf("MeetingHub", StringComparison.OrdinalIgnoreCase) >= 0;
                    }

                    if (mtg) FixSendMode(netObjParent, obj);
                }
                catch { }
            }
        }
    }
}
