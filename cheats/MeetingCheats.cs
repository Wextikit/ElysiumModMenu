#nullable disable
#pragma warning disable CS0162, CS0108, CS0219, CS0661, CS0660, CS8632, CS0168, CS0659
using AmongUs.Data.Player;
using Il2CppInterop.Runtime;
using InnerNet;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ElysiumModMenu
{
    public partial class ElysiumModMenuGUI : MonoBehaviour
    {
        private static bool TryOpenModdedMeeting(PlayerControl caller, NetworkedPlayerInfo reportedBody, string successMessage = null)
        {
            if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
            {
                ShowNotification("<color=#FF0000>[MEETING]</color> Modded meeting call is host only.");
                return false;
            }

            if (PlayerControl.LocalPlayer == null || ShipStatus.Instance == null || LobbyBehaviour.Instance != null || !AmongUsClient.Instance.IsGameStarted)
            {
                ShowNotification("<color=#FF0000>[MEETING]</color> Match must be started.");
                return false;
            }

            if (IsMeetingOrExileActive() || IntroCutscene.Instance != null)
            {
                ShowNotification("<color=#FFAA00>[MEETING]</color> Meeting/exile/intro is already active.");
                return false;
            }

            PlayerControl meetingCaller = caller ?? PlayerControl.LocalPlayer;
            if (meetingCaller == null || meetingCaller.Data == null || meetingCaller.Data.Disconnected)
            {
                ShowNotification("<color=#FF0000>[MEETING]</color> Meeting caller was not found.");
                return false;
            }

            try
            {
                if (meetingCaller == PlayerControl.LocalPlayer)
                {
                    meetingCaller.CmdReportDeadBody(reportedBody);

                    if (!string.IsNullOrWhiteSpace(successMessage))
                        ShowNotification(successMessage);

                    return true;
                }

                if (MeetingRoomManager.Instance != null)
                    MeetingRoomManager.Instance.AssignSelf(meetingCaller, reportedBody);
            }
            catch { }

            try
            {
                HudManager hud = DestroyableSingleton<HudManager>.Instance;
                if (hud == null || hud.MeetingPrefab == null)
                {
                    ShowNotification("<color=#FF0000>[MEETING]</color> Meeting prefab is unavailable.");
                    return false;
                }

                if (MeetingHud.Instance == null)
                {
                    MeetingHud.Instance = Object.Instantiate<MeetingHud>(hud.MeetingPrefab);
                    AmongUsClient.Instance.Spawn(MeetingHud.Instance.Cast<InnerNetObject>(), -2, SpawnFlags.None);
                }
                else if (MeetingHud.Instance.gameObject != null && !MeetingHud.Instance.gameObject.activeSelf)
                {
                    MeetingHud.Instance.gameObject.SetActive(true);
                }

                try { hud.OpenMeetingRoom(meetingCaller); } catch { }

                if (!string.IsNullOrWhiteSpace(successMessage))
                    ShowNotification(successMessage);

                return true;
            }
            catch
            {
                ShowNotification("<color=#FF0000>[MEETING]</color> Failed to open modded meeting.");
                return false;
            }
        }
    }
}
