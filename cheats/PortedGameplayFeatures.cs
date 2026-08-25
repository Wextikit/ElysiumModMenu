#nullable disable
using System;
using System.Collections.Generic;
using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;
using InnerNet;
using UnityEngine;

namespace ElysiumModMenu
{
    internal static class MeetingFreeRoamFeature
    {
        internal static string Activate()
        {
            try
            {
                if (MeetingHud.Instance != null)
                {
                    ((InnerNetObject)MeetingHud.Instance).DespawnOnDestroy = false;
                    UnityEngine.Object.Destroy(((Component)MeetingHud.Instance).gameObject);
                    RestoreGameplay();
                    return "Roaming locally; the meeting continues for everyone else.";
                }

                if (ExileController.Instance != null)
                {
                    ExileController.Instance.ReEnableGameplay();
                    ExileController.Instance.WrapUp();
                    RestoreGameplay();
                    return "Left the local ejection screen.";
                }
            }
            catch { }

            return "No meeting or ejection screen is active.";
        }

        private static void RestoreGameplay()
        {
            try
            {
                HudManager hud = HudManager.Instance;
                if (hud != null)
                {
                    hud.SetHudActive(true);
                    hud.SetMapAndInfoButtonsEnabled(true);
                    hud.StartCoroutine(hud.CoFadeFullScreen(Color.black, Color.clear, 0.2f, false));
                }
            }
            catch { }

            try { ControllerManager.Instance?.CloseAndResetAll(); } catch { }

            try
            {
                FollowerCamera follower = Camera.main?.GetComponent<FollowerCamera>();
                if (follower != null) follower.Locked = false;
            }
            catch { }
        }
    }

    internal static class TargetVisionFeature
    {
        private const float BlindVision = -1f;
        private const float FullBrightVision = 1000f;

        private static readonly Dictionary<byte, int> States = new Dictionary<byte, int>();
        private static int logicOptionsIndex = -1;

        internal static string StateName(byte playerId)
        {
            States.TryGetValue(playerId, out int state);
            return state == 1 ? "BLIND" : state == 2 ? "FULLBRIGHT" : "NORMAL";
        }

        internal static string Blind(PlayerControl target) => Apply(target, BlindVision, 1);
        internal static string FullBright(PlayerControl target) => Apply(target, FullBrightVision, 2);

        internal static string Restore(PlayerControl target)
        {
            string error = ValidateTarget(target);
            if (error != null) return error;

            try
            {
                IGameOptions options = GameManager.Instance.LogicOptions.currentGameOptions;
                if (options == null || !Push(options, target.OwnerId)) return "Failed to restore vision.";
                States.Remove(target.PlayerId);
                return $"Vision restored for {target.Data.PlayerName}.";
            }
            catch { return "Failed to restore vision."; }
        }

        internal static void Reset()
        {
            States.Clear();
            logicOptionsIndex = -1;
        }

        private static string Apply(PlayerControl target, float vision, int state)
        {
            string error = ValidateTarget(target);
            if (error != null) return error;

            try
            {
                IGameOptions clone = CloneCurrentOptions();
                if (clone == null) return "Failed to clone game options.";

                clone.SetFloat(FloatOptionNames.CrewLightMod, vision);
                clone.SetFloat(FloatOptionNames.ImpostorLightMod, vision);
                if (!Push(clone, target.OwnerId)) return "Failed to send targeted options.";

                States[target.PlayerId] = state;
                return $"{StateName(target.PlayerId)} applied to {target.Data.PlayerName}.";
            }
            catch { return "Failed to apply targeted vision."; }
        }

        private static string ValidateTarget(PlayerControl target)
        {
            if (target == null || target.Data == null) return "No target selected.";
            if (target == PlayerControl.LocalPlayer) return "Select another player.";
            if (target.Data.Disconnected) return "The target disconnected.";
            if (AmongUsClient.Instance == null || ShipStatus.Instance == null ||
                GameManager.Instance == null || GameManager.Instance.LogicOptions == null)
                return "This action is available in a match only.";

            try
            {
                InnerNetClient client = (InnerNetClient)AmongUsClient.Instance;
                if (!client.AmHost && client.NetworkMode == NetworkModes.OnlineGame)
                    return "Host permissions are required in online games.";
            }
            catch { return "Network state is unavailable."; }

            return null;
        }

        private static IGameOptions CloneCurrentOptions()
        {
            try
            {
                LogicOptions logic = GameManager.Instance.LogicOptions;
                byte[] bytes = logic.gameOptionsFactory.ToBytes(
                    logic.currentGameOptions,
                    AprilFoolsMode.IsAprilFoolsModeToggledOn);
                return logic.gameOptionsFactory.FromBytes(bytes);
            }
            catch { return null; }
        }

        private static int FindLogicOptionsIndex()
        {
            if (logicOptionsIndex >= 0) return logicOptionsIndex;

            try
            {
                var components = GameManager.Instance.LogicComponents;
                for (int i = 0; i < components.Count; i++)
                {
                    if (components[i].GetType() == typeof(LogicOptions))
                    {
                        logicOptionsIndex = i;
                        break;
                    }
                }
            }
            catch { }

            return logicOptionsIndex;
        }

        private static bool Push(IGameOptions options, int targetClientId)
        {
            int componentIndex = FindLogicOptionsIndex();
            if (componentIndex < 0) return false;

            MessageWriter body = null;
            MessageWriter packet = null;
            try
            {
                LogicOptions logic = GameManager.Instance.LogicOptions;
                byte[] bytes = logic.gameOptionsFactory.ToBytes(
                    options,
                    AprilFoolsMode.IsAprilFoolsModeToggledOn);

                body = MessageWriter.Get(SendOption.Reliable);
                body.StartMessage((byte)componentIndex);
                body.WriteBytesAndSize(bytes);
                body.EndMessage();

                InnerNetClient client = (InnerNetClient)AmongUsClient.Instance;
                packet = MessageWriter.Get(SendOption.Reliable);
                packet.StartMessage(6);
                packet.Write(client.GameId);
                packet.WritePacked(targetClientId);
                packet.StartMessage(1);
                packet.WritePacked(((InnerNetObject)GameManager.Instance).NetId);
                packet.Write(body, false);
                packet.EndMessage();
                packet.EndMessage();
                client.SendOrDisconnect(packet);
                return true;
            }
            catch { return false; }
            finally
            {
                try { body?.Recycle(); } catch { }
                try { packet?.Recycle(); } catch { }
            }
        }
    }

    [HarmonyPatch(typeof(LobbyBehaviour), nameof(LobbyBehaviour.Start))]
    internal static class TargetVisionLobbyResetPatch
    {
        public static void Postfix() => TargetVisionFeature.Reset();
    }

    internal static class AutoVentAfterKillFeature
    {
        private static float enterVentAt = -1f;

        internal static void Arm()
        {
            if (ElysiumModMenuGUI.autoVentAfterKill)
                enterVentAt = Time.time + 0.25f;
        }

        internal static void Tick()
        {
            if (!ElysiumModMenuGUI.autoVentAfterKill)
            {
                enterVentAt = -1f;
                return;
            }

            if (enterVentAt < 0f || Time.time < enterVentAt) return;
            enterVentAt = -1f;

            try
            {
                PlayerControl local = PlayerControl.LocalPlayer;
                if (local == null || local.Data == null || local.Data.IsDead || local.MyPhysics == null ||
                    local.inVent || ShipStatus.Instance == null || MeetingHud.Instance != null)
                    return;

                Vent nearest = FindNearestVent(local.GetTruePosition());
                if (nearest == null) return;

                try { local.NetTransform?.RpcSnapTo(nearest.transform.position); } catch { }
                try { local.MyPhysics.RpcEnterVent(nearest.Id); } catch { }
            }
            catch { }
        }

        private static Vent FindNearestVent(Vector2 position)
        {
            Vent nearest = null;
            float nearestDistance = float.MaxValue;

            try
            {
                var vents = ShipStatus.Instance?.AllVents;
                if (vents == null) return null;

                for (int i = 0; i < vents.Count; i++)
                {
                    Vent vent = vents[i];
                    if (vent == null) continue;
                    float distance = Vector2.Distance(position, vent.transform.position);
                    if (distance < nearestDistance)
                    {
                        nearest = vent;
                        nearestDistance = distance;
                    }
                }
            }
            catch { }

            return nearest;
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer),
        new Type[] { typeof(PlayerControl), typeof(MurderResultFlags) })]
    internal static class AutoVentAfterKillPatch
    {
        public static void Postfix(PlayerControl __instance)
        {
            if (__instance == PlayerControl.LocalPlayer)
                AutoVentAfterKillFeature.Arm();
        }
    }

    internal static class ImpTrapFeature
    {
        private const float TriggerGap = 1.5f;
        private static float lastTriggerAt = -99f;

        internal static void Fire(PlayerControl impostor)
        {
            if (!ElysiumModMenuGUI.impTrap || impostor == null || impostor.Data == null) return;
            if (ShipStatus.Instance == null || LobbyBehaviour.Instance != null) return;
            if (MeetingHud.Instance != null || ExileController.Instance != null) return;

            float now = Time.unscaledTime;
            if (now - lastTriggerAt < TriggerGap) return;

            int ventId = FindNearestVentId(impostor.GetTruePosition());
            if (ventId < 0) return;
            lastTriggerAt = now;

            int pulled = 0;
            try
            {
                var players = PlayerControl.AllPlayerControls;
                if (players == null) return;

                for (int i = 0; i < players.Count; i++)
                {
                    PlayerControl player = players[i];
                    if (player == null || player.Data == null || player.Data.Disconnected || player.Data.IsDead) continue;
                    if (player.PlayerId == impostor.PlayerId || player == PlayerControl.LocalPlayer) continue;
                    if (ElysiumModMenuGUI.SendImpTrapPlayerToVent(player, ventId)) pulled++;
                }
            }
            catch { }

            if (pulled > 0)
            {
                string name = impostor.Data.PlayerName ?? "?";
                ElysiumModMenuGUI.ShowNotification($"<color=#FFAA44>[IMP TRAP]</color> {name}: pulled {pulled} player(s).");
            }
        }

        private static int FindNearestVentId(Vector2 position)
        {
            try
            {
                var vents = ShipStatus.Instance?.AllVents;
                if (vents == null || vents.Count == 0) return -1;

                Vent nearest = null;
                float nearestDistance = float.MaxValue;
                for (int i = 0; i < vents.Count; i++)
                {
                    Vent vent = vents[i];
                    if (vent == null) continue;

                    float distance = Vector2.Distance(position, vent.transform.position);
                    if (distance >= nearestDistance) continue;
                    nearest = vent;
                    nearestDistance = distance;
                }

                return nearest != null ? nearest.Id : -1;
            }
            catch { return -1; }
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer),
        new Type[] { typeof(PlayerControl), typeof(MurderResultFlags) })]
    internal static class ImpTrapKillPatch
    {
        public static void Postfix(PlayerControl __instance) => ImpTrapFeature.Fire(__instance);
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Shapeshift))]
    internal static class ImpTrapShiftPatch
    {
        public static void Postfix(PlayerControl __instance) => ImpTrapFeature.Fire(__instance);
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcShapeshift))]
    internal static class ImpTrapRpcShiftPatch
    {
        public static void Postfix(PlayerControl __instance) => ImpTrapFeature.Fire(__instance);
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.HandleServerVanish))]
    internal static class ImpTrapVanishPatch
    {
        public static void Postfix(PlayerControl __instance) => ImpTrapFeature.Fire(__instance);
    }

    internal static class SpamMeetingsFeature
    {
        private static float nextMeetingAt;

        internal static void Tick()
        {
            if (!ElysiumModMenuGUI.spamMeetings) return;
            if (Time.unscaledTime < nextMeetingAt) return;

            nextMeetingAt = Time.unscaledTime + 0.15f;
            RequestMeeting();
        }

        private static void RequestMeeting()
        {
            try
            {
                PlayerControl reporter = PlayerControl.LocalPlayer;
                InnerNetClient network = (InnerNetClient)AmongUsClient.Instance;
                if (reporter == null || reporter.Data == null || network == null ||
                    ShipStatus.Instance == null || MeetingHud.Instance != null ||
                    ExileController.Instance != null || reporter.Data.IsDead)
                    return;

                bool anticheatLive = reporter.Data.OwnerId != -2;
                if (anticheatLive && network.GameState != InnerNetClient.GameStates.Started)
                    return;

                try { reporter.RemainingEmergencies = 999999; } catch { }

                if (!network.AmHost)
                {
                    reporter.CmdReportDeadBody(null);
                    return;
                }

                MeetingRoomManager.Instance?.AssignSelf(reporter, null);
                reporter.RpcStartMeeting(null);
                HudManager.Instance?.OpenMeetingRoom(reporter);
            }
            catch { }
        }
    }

    internal static class HnsTaskDrainFeature
    {
        private const float BurstSeconds = 2f;
        private const float RestSeconds = 1f;

        private static float nextSendAt;
        private static float phaseEndsAt;
        private static bool resting = true;

        internal static bool Running => ElysiumModMenuGUI.hnsTaskDrain && Ready();

        internal static void Tick()
        {
            if (!ElysiumModMenuGUI.hnsTaskDrain || !Ready())
            {
                resting = true;
                phaseEndsAt = 0f;
                return;
            }

            float now = Time.unscaledTime;
            if (now >= phaseEndsAt)
            {
                resting = !resting;
                phaseEndsAt = now + (resting ? RestSeconds : BurstSeconds);
            }

            if (resting || now < nextSendAt) return;
            nextSendAt = now + Mathf.Clamp(ElysiumModMenuGUI.hnsTaskDrainStep, 0.15f, 1.5f);
            SendTaskBurst();
        }

        private static bool Ready()
        {
            try
            {
                if (ShipStatus.Instance == null || GameManager.Instance == null ||
                    !GameManager.Instance.IsHideAndSeek() || MeetingHud.Instance != null)
                    return false;

                PlayerControl local = PlayerControl.LocalPlayer;
                return local != null && local.Data != null && !local.Data.Disconnected && local.myTasks != null;
            }
            catch { return false; }
        }

        private static void SendTaskBurst()
        {
            try
            {
                PlayerControl local = PlayerControl.LocalPlayer;
                if (local?.myTasks == null) return;

                for (int i = 0; i < local.myTasks.Count; i++)
                {
                    PlayerTask task = local.myTasks[i];
                    if (task != null) local.RpcCompleteTask(task.Id);
                }
            }
            catch { }
        }
    }

    internal static class AutoTasksFeature
    {
        private static float nextTaskAt;

        internal static int RemainingTasks()
        {
            int count = 0;
            try
            {
                PlayerControl local = PlayerControl.LocalPlayer;
                if (local?.myTasks == null) return 0;
                for (int i = 0; i < local.myTasks.Count; i++)
                {
                    PlayerTask task = local.myTasks[i];
                    if (task != null && !task.IsComplete) count++;
                }
            }
            catch { }
            return count;
        }

        internal static void Tick()
        {
            if (!ElysiumModMenuGUI.autoTasksEnabled || !Ready()) return;
            if (Time.time < nextTaskAt) return;

            float delay = Mathf.Max(0.8f, ElysiumModMenuGUI.autoTasksDelay);
            nextTaskAt = Time.time + delay + UnityEngine.Random.Range(0f, delay * 0.35f);
            CompleteNextTask();
        }

        private static bool Ready()
        {
            try
            {
                if (ShipStatus.Instance == null || MeetingHud.Instance != null || ExileController.Instance != null)
                    return false;

                PlayerControl local = PlayerControl.LocalPlayer;
                if (local == null || local.Data == null || local.Data.IsDead || local.Data.Disconnected || local.myTasks == null)
                    return false;

                return local.Data.Role == null || !local.Data.Role.IsImpostor;
            }
            catch { return false; }
        }

        private static void CompleteNextTask()
        {
            try
            {
                PlayerControl local = PlayerControl.LocalPlayer;
                for (int i = 0; i < local.myTasks.Count; i++)
                {
                    PlayerTask task = local.myTasks[i];
                    if (task == null || task.IsComplete) continue;

                    local.RpcCompleteTask((uint)task.Id);
                    return;
                }
            }
            catch { }
        }
    }
}
