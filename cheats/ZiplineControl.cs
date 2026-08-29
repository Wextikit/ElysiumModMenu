#nullable disable
using System.Collections.Generic;
using Il2CppInterop.Runtime.InteropTypes;
using Object = UnityEngine.Object;

namespace ElysiumModMenu
{
    internal static class ZiplineControl
    {
        private static ShipStatus cachedShip;
        private static ZiplineBehaviour line;
        private static readonly HashSet<byte> targetIds = new HashSet<byte>();
        private static readonly HashSet<byte> loopIds = new HashSet<byte>();
        private static readonly HashSet<byte> movingIds = new HashSet<byte>();
        private static readonly HashSet<byte> seenIds = new HashSet<byte>();
        private static readonly Dictionary<byte, bool> loopDown = new Dictionary<byte, bool>();
        private static readonly Dictionary<byte, float> loopAt = new Dictionary<byte, float>();
        private static readonly List<byte> removeIds = new List<byte>();
        private static readonly List<PlayerControl> cycleTargets = new List<PlayerControl>();
        private static byte currentTargetId = byte.MaxValue;
        private static float nextTickAt;

        internal static int TargetCount => targetIds.Count;
        internal static int LoopCount => loopIds.Count;
        internal static bool OnMap => GetLine() != null;
        internal static PlayerControl CurrentTarget => GetCurrentTarget();

        internal static bool IsTarget(byte playerId) => targetIds.Contains(playerId);
        internal static bool IsLooping(byte playerId) => loopIds.Contains(playerId);

        internal static void ToggleTarget(byte playerId)
        {
            if (!targetIds.Remove(playerId)) targetIds.Add(playerId);
        }

        internal static void SelectAll()
        {
            targetIds.Clear();
            if (PlayerControl.AllPlayerControls == null) return;

            foreach (PlayerControl player in PlayerControl.AllPlayerControls)
            {
                if (player == null || player == PlayerControl.LocalPlayer || player.Data == null || player.Data.Disconnected) continue;
                targetIds.Add(player.PlayerId);
            }
        }

        internal static void ClearTargets() => targetIds.Clear();

        internal static PlayerControl CycleTarget(int direction)
        {
            if (PlayerControl.AllPlayerControls == null) return null;

            cycleTargets.Clear();
            foreach (PlayerControl player in PlayerControl.AllPlayerControls)
            {
                if (IsValidTarget(player)) cycleTargets.Add(player);
            }

            if (cycleTargets.Count == 0)
            {
                currentTargetId = byte.MaxValue;
                return null;
            }

            int index = -1;
            for (int i = 0; i < cycleTargets.Count; i++)
            {
                if (cycleTargets[i].PlayerId != currentTargetId) continue;
                index = i;
                break;
            }
            if (index < 0) index = direction < 0 ? 0 : -1;
            index = (index + direction + cycleTargets.Count) % cycleTargets.Count;
            PlayerControl target = cycleTargets[index];
            currentTargetId = target.PlayerId;
            cycleTargets.Clear();
            return target;
        }

        internal static bool Ride(PlayerControl target, bool fromTop)
        {
            if (target == null || target.Data == null || target.Data.Disconnected) return false;

            ZiplineBehaviour zipline = GetLine();
            if (zipline == null) return false;

            return Send(target, zipline, fromTop);
        }

        internal static int RideSelected(bool fromTop)
        {
            ZiplineBehaviour zipline = GetLine();
            if (zipline == null || PlayerControl.AllPlayerControls == null) return -1;

            int ridden = 0;
            foreach (PlayerControl player in PlayerControl.AllPlayerControls)
            {
                if (player == null || player == PlayerControl.LocalPlayer || player.Data == null || player.Data.Disconnected || !targetIds.Contains(player.PlayerId)) continue;
                if (Send(player, zipline, fromTop)) ridden++;
            }

            return ridden;
        }

        internal static bool StartLoop(PlayerControl target, bool fromTop)
        {
            ZiplineBehaviour zipline = GetLine();
            return zipline != null && StartLoop(target, zipline, fromTop);
        }

        internal static int StartAll(bool fromTop)
        {
            ZiplineBehaviour zipline = GetLine();
            if (zipline == null || PlayerControl.AllPlayerControls == null) return -1;

            int count = 0;
            foreach (PlayerControl player in PlayerControl.AllPlayerControls)
            {
                if (!IsValidTarget(player)) continue;
                if (StartLoop(player, zipline, fromTop)) count++;
            }
            return count;
        }

        internal static void StopLoop(PlayerControl target)
        {
            if (target != null) StopLoop(target.PlayerId);
        }

        internal static void StopAllLoops()
        {
            loopIds.Clear();
            movingIds.Clear();
            seenIds.Clear();
            loopDown.Clear();
            loopAt.Clear();
            removeIds.Clear();
            nextTickAt = 0f;
        }

        internal static void Tick()
        {
            if (loopIds.Count == 0) return;

            float now = UnityEngine.Time.unscaledTime;
            if (now < nextTickAt) return;
            nextTickAt = now + 0.04f;

            ZiplineBehaviour zipline = GetLine();
            if (zipline == null || PlayerControl.AllPlayerControls == null)
            {
                StopAllLoops();
                return;
            }

            seenIds.Clear();
            foreach (PlayerControl player in PlayerControl.AllPlayerControls)
            {
                if (player == null || player.Data == null || player.Data.Disconnected || !loopIds.Contains(player.PlayerId)) continue;

                byte id = player.PlayerId;
                seenIds.Add(id);

                if (player.inMovingPlat)
                {
                    movingIds.Add(id);
                    continue;
                }

                if (movingIds.Remove(id))
                {
                    bool down = loopDown.TryGetValue(id, out bool lastDown) && lastDown;
                    loopDown[id] = !down;
                    loopAt[id] = now + 0.12f;
                    continue;
                }

                if (loopAt.TryGetValue(id, out float at) && now < at) continue;

                bool fromTop = !loopDown.TryGetValue(id, out bool nextDown) || nextDown;
                if (Send(player, zipline, fromTop)) loopAt[id] = now + 2f;
                else StopLoop(id);
            }

            removeIds.Clear();
            foreach (byte id in loopIds)
            {
                if (!seenIds.Contains(id)) removeIds.Add(id);
            }
            foreach (byte id in removeIds) StopLoop(id);
            removeIds.Clear();
        }

        private static bool StartLoop(PlayerControl target, ZiplineBehaviour zipline, bool fromTop)
        {
            if (target == null || target.Data == null || target.Data.Disconnected || !Send(target, zipline, fromTop)) return false;

            byte id = target.PlayerId;
            loopIds.Add(id);
            movingIds.Remove(id);
            loopDown[id] = fromTop;
            loopAt[id] = UnityEngine.Time.unscaledTime + 2f;
            return true;
        }

        private static void StopLoop(byte id)
        {
            loopIds.Remove(id);
            movingIds.Remove(id);
            loopDown.Remove(id);
            loopAt.Remove(id);
        }

        private static ZiplineBehaviour GetLine()
        {
            ShipStatus ship = ShipStatus.Instance;
            if (ship == null)
            {
                cachedShip = null;
                line = null;
                targetIds.Clear();
                StopAllLoops();
                currentTargetId = byte.MaxValue;
                return null;
            }

            if (cachedShip == ship) return line;

            cachedShip = ship;
            targetIds.Clear();
            StopAllLoops();
            currentTargetId = byte.MaxValue;
            line = null;

            try
            {
                FungleShipStatus fungle = ((Il2CppObjectBase)ship).TryCast<FungleShipStatus>();
                if (fungle != null && fungle.Zipline != null) line = fungle.Zipline;
                else line = Object.FindObjectOfType<ZiplineBehaviour>();
            }
            catch
            {
                line = null;
            }

            return line;
        }

        private static PlayerControl GetCurrentTarget()
        {
            if (PlayerControl.AllPlayerControls == null) return null;

            PlayerControl first = null;
            foreach (PlayerControl player in PlayerControl.AllPlayerControls)
            {
                if (!IsValidTarget(player)) continue;
                if (first == null) first = player;
                if (player.PlayerId == currentTargetId) return player;
            }

            if (first != null) currentTargetId = first.PlayerId;
            return first;
        }

        private static bool IsValidTarget(PlayerControl player)
        {
            return player != null && player != PlayerControl.LocalPlayer && player.Data != null && !player.Data.Disconnected;
        }

        private static bool Send(PlayerControl target, ZiplineBehaviour zipline, bool fromTop)
        {
            try
            {
                target.RpcUseZipline(target, zipline, fromTop);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
