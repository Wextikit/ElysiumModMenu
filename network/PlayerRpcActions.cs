#nullable disable
#pragma warning disable CS0162, CS0108, CS0219, CS0661, CS0660, CS8632, CS0168, CS0659
using AmongUs.GameOptions;
using AmongUs.InnerNet.GameDataMessages;
using Hazel;
using InnerNet;
using UnityEngine;

namespace ElysiumModMenu
{
    public partial class ElysiumModMenuGUI : MonoBehaviour
    {
private static bool SendPlayerSnapTo(PlayerControl player, Vector2 pos)
        {
            if (player == null || player.Data == null || player.Data.Disconnected || player.NetTransform == null || AmongUsClient.Instance == null)
                return false;
            if (HasServerAnticheat()) return false;

            MessageWriter batch = null;
            try
            {
                batch = MessageWriter.Get(SendOption.Reliable);
                batch.StartMessage(InnerNet.Tags.GameData);
                batch.Write(AmongUsClient.Instance.GameId);

                WriteSnapTo(batch, player, pos);

                batch.EndMessage();
                AmongUsClient.Instance.SendOrDisconnect(batch);
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                try { batch?.Recycle(); } catch { }
            }
        }

private static int SendAllPlayersSnapTo(Vector2 pos)
        {
            if (PlayerControl.AllPlayerControls == null || AmongUsClient.Instance == null || HasServerAnticheat())
                return 0;

            MessageWriter batch = null;
            int count = 0;
            try
            {
                batch = MessageWriter.Get(SendOption.Reliable);
                batch.StartMessage(InnerNet.Tags.GameData);
                batch.Write(AmongUsClient.Instance.GameId);

                foreach (PlayerControl player in PlayerControl.AllPlayerControls)
                {
                    if (player == null || player.Data == null || player.Data.Disconnected || player.NetTransform == null)
                        continue;

                    WriteSnapTo(batch, player, pos);
                    count++;
                }

                batch.EndMessage();
                AmongUsClient.Instance.SendOrDisconnect(batch);
                return count;
            }
            catch
            {
                return 0;
            }
            finally
            {
                try { batch?.Recycle(); } catch { }
            }
        }

private static void WriteSnapTo(MessageWriter batch, PlayerControl player, Vector2 pos)
        {
            player.NetTransform.SnapTo(pos, (ushort)(player.NetTransform.lastSequenceId + 1));
            ushort seq = (ushort)(player.NetTransform.lastSequenceId + 2);

            batch.StartMessage((byte)GameDataTypes.RpcFlag);
            batch.WritePacked(player.NetTransform.NetId);
            batch.Write((byte)RpcCalls.SnapTo);
            NetHelpers.WriteVector2(pos, batch);
            batch.Write(seq);
            batch.EndMessage();
        }

private static bool SendPlayerShapeshift(PlayerControl player, PlayerControl target, bool animate)
        {
            if (player == null || player.Data == null || player.Data.Disconnected || target == null || target.Data == null ||
                target.Data.Disconnected || AmongUsClient.Instance == null)
                return false;

            bool hasAnticheat = HasServerAnticheat();

            MessageWriter batch = null;
            try
            {
                batch = MessageWriter.Get(SendOption.Reliable);
                batch.StartMessage(InnerNet.Tags.GameData);
                batch.Write(AmongUsClient.Instance.GameId);

                if (hasAnticheat && player.Data.RoleType != RoleTypes.Shapeshifter)
                {
                    RoleTypes role = player.Data.RoleType;
                    WriteSetRole(batch, player, RoleTypes.Shapeshifter);
                    WriteShapeshift(batch, player, target, animate);
                    WriteSetRole(batch, player, role);
                }
                else
                {
                    WriteShapeshift(batch, player, target, animate);
                }

                batch.EndMessage();
                AmongUsClient.Instance.SendOrDisconnect(batch);
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                try { batch?.Recycle(); } catch { }
            }
        }

private static void PacketShapeshiftFrame(PlayerControl player, PlayerControl target)
        {
            if (SendPlayerShapeshift(player, target, true))
                ShowNotification($"<color=#ca08ff>[MORPH 2]</color> <b>{player.Data.PlayerName}</b> morphed into <b>{target.Data.PlayerName}</b>!");
            else
                ShowNotification("<color=#FF0000>[MORPH 2]</color> Failed.");
        }

private static void WriteSetRole(MessageWriter batch, PlayerControl player, RoleTypes role)
        {
            player.StartCoroutine(player.CoSetRole(role, true));

            batch.StartMessage((byte)GameDataTypes.RpcFlag);
            batch.WritePacked(player.NetId);
            batch.Write((byte)RpcCalls.SetRole);
            batch.Write((ushort)role);
            batch.Write(true);
            batch.EndMessage();
        }

private static void WriteShapeshift(MessageWriter batch, PlayerControl player, PlayerControl target, bool animate)
        {
            player.Shapeshift(target, animate);

            batch.StartMessage((byte)GameDataTypes.RpcFlag);
            batch.WritePacked(player.NetId);
            batch.Write((byte)RpcCalls.Shapeshift);
            batch.WriteNetObject(target);
            batch.Write(animate);
            batch.EndMessage();
        }
    }
}
