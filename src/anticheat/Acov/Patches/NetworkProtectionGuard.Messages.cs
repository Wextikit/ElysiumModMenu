#nullable disable
#pragma warning disable CS0162, CS0108, CS0219, CS0661, CS0660, CS8632, CS0168, CS0659
using AmongUs.Data.Player;
using AmongUs.GameOptions;
using AmongUs.InnerNet.GameDataMessages;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using BepInEx.Unity.IL2CPP.Utils;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using ElysiumModMenu;
using HarmonyLib;
using Hazel;
using Il2CppInterop.Runtime.Attributes;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using InnerNet;
using RewiredConsts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.Playables;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using static ElysiumModMenu.ElysiumModMenuGUI;
using static Rewired.UI.ControlMapper.ControlMapper;
using Color = UnityEngine.Color;
using Object = UnityEngine.Object;
using Vector3 = UnityEngine.Vector3;

namespace Acov.Patches
{
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using BepInEx.Configuration;
using HarmonyLib;
using Hazel;
using InnerNet;
using UnityEngine;


internal static partial class NetworkProtectionGuard
{

private static bool NameRpcStringTooLong(MessageReader part, byte callId)
	{
		if (!NameStringRpcIds.Contains(callId))
		{
			return false;
		}

		try
		{
			return part != null && part.Length > MaxRpcStringBytes;
		}
		catch
		{
			return false;
		}
	}

private static bool VentRpcOutOfRange(MessageReader part, byte callId)
	{
		if (callId != 19 && callId != 20)
		{
			return false;
		}

		if (ModOptions.VentGuard != null && !ModOptions.VentGuard.Value)
		{
			return false;
		}

		MessageReader copy = null;
		try
		{
			copy = MessageReader.Get(part);
			copy.ReadPackedUInt32();
			copy.ReadByte();
			int ventId = copy.ReadPackedInt32();
			return !IsRealVentId(ventId);
		}
		catch
		{
			return false;
		}
		finally { copy?.Recycle(); }
	}

internal static bool IsRealVentId(int ventId)
	{
		try
		{
			ShipStatus ship = ShipStatus.Instance;
			if (ship == null || ship.AllVents == null)
			{
				return true;
			}

			var vents = ship.AllVents;
			int count = vents.Count;
			if (count <= 0)
			{
				return true;
			}

			for (int i = 0; i < count; i++)
			{
				var vent = vents[i];
				if (vent != null && vent.Id == ventId)
				{
					return true;
				}
			}

			return false;
		}
		catch
		{
			return true;
		}
	}

private static bool SnapToAggregateExceeded()
	{
		float now = Time.realtimeSinceStartup;
		while (SnapToAggregate.Count > 0 && now - SnapToAggregate.Peek() > 1f)
		{
			SnapToAggregate.Dequeue();
		}
		SnapToAggregate.Enqueue(now);
		return SnapToAggregate.Count > MaxSnapToAggregatePerSec;
	}

private static void CleanupNonHostRpc(float now)
	{
		if (NonHostRpcByKey.Count == 0) return;
		List<long> dead = null;
		foreach (KeyValuePair<long, Queue<float>> kv in NonHostRpcByKey)
		{
			Queue<float> q = kv.Value;
			while (q.Count > 0 && now - q.Peek() > 6f)
			{
				q.Dequeue();
			}
			if (q.Count == 0)
			{
				(dead ??= new List<long>()).Add(kv.Key);
			}
		}
		if (dead != null)
		{
			for (int i = 0; i < dead.Count; i++) NonHostRpcByKey.Remove(dead[i]);
		}
	}

private static void ResetInboundEnvelope(SendOption sendOption)
	{
		activeInboundTargetClientId = -1;
		activeInboundGameDataParts = 0;
		activeInboundHasInvalidPartTag = false;
		activeInboundHasSpawnExploit = false;
		activeInboundSpawnExploitDetail = null;
		activeInboundHasDataFlood = false;
		activeInboundDataFloodDetail = null;
		activeInboundPhantomFlood = false;
		activeInboundHasPhantomNetId = false;
	}

private static bool PhantomDataRateExceeded()
	{
		long now = Environment.TickCount64;
		if (now - phantomDataWindowMs >= 1000)
		{
			phantomDataWindowMs = now;
			phantomDataInWindow = 0;
		}
		phantomDataInWindow++;
		return phantomDataInWindow > MaxPhantomDataPerSec;
	}

private static bool IsPhantomNetId(uint nid)
	{
		long now = Environment.TickCount64;
		if (now - lastPendingUnknownCleanupMs > 4000)
		{
			lastPendingUnknownCleanupMs = now;
			CleanupPendingUnknownNetIds(now);
		}

		if (!pendingUnknownNetId.TryGetValue(nid, out long firstSeen))
		{
			pendingUnknownNetId[nid] = now;
			return false;
		}

		return now - firstSeen > UnknownNetIdGraceMs;
	}

private static void CleanupPendingUnknownNetIds(long now)
	{
		if (pendingUnknownNetId.Count == 0) return;
		InnerNetClient inner = AmongUsClient.Instance != null ? (InnerNetClient)AmongUsClient.Instance : null;
		List<uint> dead = null;
		foreach (KeyValuePair<uint, long> kv in pendingUnknownNetId)
		{
			bool live = inner != null && inner.allObjects.AllObjectsFast.ContainsKey(kv.Key);
			if (live || now - kv.Value > 10000)
			{
				(dead ??= new List<uint>()).Add(kv.Key);
			}
		}
		if (dead != null)
		{
			for (int i = 0; i < dead.Count; i++) pendingUnknownNetId.Remove(dead[i]);
		}
	}

private static bool GameDataFramingValid(MessageReader reader, byte tag)
	{
		MessageReader copy = null;
		try
		{
			copy = MessageReader.Get(reader);
			copy.ReadInt32();
			if (tag == 6)
			{
				copy.ReadPackedInt32();
			}

			int parts = 0;
			while (copy.Position < copy.Length && parts < MaxTrackedGameDataParts)
			{
				MessageReader part = copy.ReadMessage();
				if (part == null)
				{
					break;
				}

				try
				{
					parts++;
					byte pt = part.Tag;
					if (pt == 0 || pt == 3 || pt > 8)
					{
						return false;
					}

					MessageReader idc = null;
					try
					{
						idc = MessageReader.Get(part);
						idc.ReadPackedUInt32();
						if (pt == 2) idc.ReadByte();
						else if (pt == 6) idc.ReadString();
					}
					finally { idc?.Recycle(); }
				}
				finally { part.Recycle(); }
			}

			return true;
		}
		catch
		{
			return false;
		}
		finally
		{
			copy?.Recycle();
		}
	}

private static void CaptureGameDataEnvelope(MessageReader reader, byte tag, SendOption sendOption)
	{
		activeInboundTargetClientId = -1;
		activeInboundGameDataParts = 0;
		activeInboundHasInvalidPartTag = false;
		activeInboundHasSpawnExploit = false;
		activeInboundSpawnExploitDetail = null;
		activeInboundHasDataFlood = false;
		activeInboundDataFloodDetail = null;
		ScratchSpawnNetIds.Clear();
		if (reader == null)
		{
			return;
		}

		int originalPosition = reader.Position;
		MessageReader copy = null;
		try
		{
			int senderClientId = GetActiveInboundSenderClientId();
			copy = MessageReader.Get(reader);
			copy.ReadInt32();
			if (tag == 6)
			{
				activeInboundTargetClientId = copy.ReadPackedInt32();
			}

			int safety = 0;
			List<RpcEnvelopeContext> rpcContexts = null;
			while (copy.Position < copy.Length && safety++ < MaxTrackedGameDataParts)
			{
				MessageReader part = copy.ReadMessage();
				if (part == null)
				{
					break;
				}

				try
				{
				activeInboundGameDataParts++;

				byte partTag = part.Tag;
				if (partTag == 0 || partTag == 3 || partTag > 8)
				{
					activeInboundHasInvalidPartTag = true;
					break;
				}

				if (partTag == 1 && AmongUsClient.Instance != null)
				{
					try
					{
						MessageReader dataCopy = null;
						try
						{
						dataCopy = MessageReader.Get(part);
						uint dataNetId = dataCopy.ReadPackedUInt32();
						InnerNetClient innerCheck = (InnerNetClient)AmongUsClient.Instance;
						if (!innerCheck.allObjects.AllObjectsFast.ContainsKey(dataNetId) &&
							!innerCheck.DestroyedObjects.Contains(dataNetId))
						{
							if (PhantomDataRateExceeded())
							{
								activeInboundPhantomFlood = true;
							}
							if (IsPhantomNetId(dataNetId))
							{
								activeInboundHasPhantomNetId = true;
							}
						}

						int dataBytes = dataCopy.Length - dataCopy.Position;
						if (!IsLobbyJoinSyncGrace() && ShouldDropDataObject(dataNetId, dataBytes))
						{
							activeInboundHasDataFlood = true;
							activeInboundDataFloodDetail = $"netId {dataNetId}, {dataBytes}B — DATA flood.";
							break;
						}
						}
						finally { dataCopy?.Recycle(); }
					}
					catch { }
				}

				if (partTag == 4 && SpawnShouldDrop(part))
				{
					activeInboundHasSpawnExploit = true;
					activeInboundSpawnExploitDetail = "Spawn flood (fake-owner / over per-scene cap).";
					break;
				}

				if (partTag == 4 && IdenticalNetIdProtectionEnabled())
				{
					try
					{
						MessageReader spawnCopy = null;
						try
						{
						spawnCopy = MessageReader.Get(part);
						spawnCopy.ReadPackedUInt32();
						int spawnOwnerId = spawnCopy.ReadPackedInt32();
						spawnCopy.ReadByte();
						int compCount = spawnCopy.ReadPackedInt32();
						if (compCount <= 0) break;
						int spawnScan = compCount > 32 ? 32 : compCount;

						InnerNetClient innerForOwnerCheck = AmongUsClient.Instance != null
							? (InnerNetClient)AmongUsClient.Instance
							: null;

						for (int ci = 0; ci < spawnScan; ci++)
						{
							if (spawnCopy.Position >= spawnCopy.Length) break;
							uint newNetId = spawnCopy.ReadPackedUInt32();
							if (newNetId != 0)
							{
								if (ScratchSpawnNetIds.Contains(newNetId))
								{
									activeInboundHasSpawnExploit = true;
									activeInboundSpawnExploitDetail = $"Duplicate Spawn netId {newNetId} within packet.";
									break;
								}

								if (innerForOwnerCheck != null &&
									innerForOwnerCheck.allObjects.AllObjectsFast.ContainsKey(newNetId) &&
									!innerForOwnerCheck.DestroyedObjects.Contains(newNetId))
								{
									activeInboundHasSpawnExploit = true;
									try
									{
										InnerNetObject existingObj = innerForOwnerCheck.allObjects.AllObjectsFast[newNetId];
										activeInboundSpawnExploitDetail = (existingObj != null && existingObj.OwnerId != spawnOwnerId)
											? $"Spawn netId {newNetId} hijack: existing owner {existingObj.OwnerId} != new owner {spawnOwnerId}."
											: $"Spawn netId {newNetId} duplicates a live object.";
									}
									catch { activeInboundSpawnExploitDetail = $"Spawn netId {newNetId} duplicates a live object."; }
									break;
								}

								ScratchSpawnNetIds.Add(newNetId);
							}
							MessageReader compInit = spawnCopy.ReadMessage();
							if (compInit == null) break;
							compInit.Recycle();
						}

						if (activeInboundHasSpawnExploit) break;
						}
						finally { spawnCopy?.Recycle(); }
					}
					catch { }
				}

				if (part.Tag == 2 && TryReadRpcEnvelope(part, out uint netId, out byte callId))
				{
					if (rpcContexts == null)
					{
						rpcContexts = new List<RpcEnvelopeContext>();
					}

					rpcContexts.Add(new RpcEnvelopeContext
					{
						NetId = netId,
						CallId = callId,
						TargetClientId = activeInboundTargetClientId,
						SenderClientId = senderClientId,
						DataIndex = activeInboundGameDataParts,
						SendOption = sendOption,
						CreatedAtMs = Environment.TickCount64,
					});
				}
				}
				finally { part.Recycle(); }
			}

			if (rpcContexts != null)
			{
				for (int i = 0; i < rpcContexts.Count; i++)
				{
					RpcEnvelopeContext context = rpcContexts[i];
					context.DataCount = activeInboundGameDataParts;
					QueueRpcEnvelopeContext(context);
				}
			}
		}
		catch
		{
			activeInboundGameDataParts = 0;
			activeInboundTargetClientId = -1;
		}
		finally
		{
			copy?.Recycle();
			try
			{
				reader.Position = originalPosition;
			}
			catch
			{
			}
		}
	}

private static void CaptureJoinEnvelope(MessageReader reader)
	{
		if (reader == null)
		{
			return;
		}

		int originalPosition = reader.Position;
		MessageReader copy = null;
		try
		{
			copy = MessageReader.Get(reader);
			int gameId = copy.ReadInt32();
			if (AmongUsClient.Instance != null && ((InnerNetClient)AmongUsClient.Instance).GameId != gameId)
			{
				return;
			}

			int joinedClientId = copy.ReadInt32();
			if (joinedClientId < 0 || AmongUsClient.Instance == null)
			{
				return;
			}

			InnerNetClient inner = (InnerNetClient)AmongUsClient.Instance;
			if (joinedClientId == inner.ClientId || joinedClientId == inner.HostId)
			{
				return;
			}

			RememberConnectionClient(activeInboundConnectionKey, joinedClientId);
			ClientJoinTimeAt[joinedClientId] = Time.realtimeSinceStartup;
			RememberRecentJoinCandidate(joinedClientId);
			if (GetActiveInboundSenderClientId() < 0)
			{
				SetActiveInboundSender(joinedClientId);
			}
		}
		catch
		{
		}
		finally
		{
			copy?.Recycle();
			try
			{
				reader.Position = originalPosition;
			}
			catch
			{
			}
		}
	}

private static bool TryReadRpcEnvelope(MessageReader part, out uint netId, out byte callId)
	{
		netId = 0;
		callId = 0;
		if (part == null)
		{
			return false;
		}

		MessageReader copy = null;
		try
		{
			copy = MessageReader.Get(part);
			netId = copy.ReadPackedUInt32();
			callId = copy.ReadByte();
			return true;
		}
		catch
		{
			return false;
		}
		finally { copy?.Recycle(); }
	}

private static int TryResolveOwnerFromGameDataContent(InnerNetClient client, MessageReader reader)
	{
		if (client == null || reader == null) return -1;
		byte tag = reader.Tag;
		if (tag != 1 && tag != 5 && tag != 6) return -1;
		MessageReader copy = null;
		MessageReader subMsg = null;
		try
		{
			copy = MessageReader.Get(reader);
			if (copy.Length - copy.Position < 4) return -1;
			int gameId = copy.ReadInt32();
			if (gameId != client.GameId) return -1;

			if (tag == 1)
			{
				if (copy.Length - copy.Position < 4) return -1;
				int joinClientId = copy.ReadInt32();
				return IsKnownRemoteClient(client, joinClientId) ? joinClientId : -1;
			}

			if (tag == 6)
			{
				if (copy.Position >= copy.Length) return -1;
				copy.ReadPackedInt32();
			}

			if (copy.Position >= copy.Length) return -1;
			subMsg = copy.ReadMessage();
			if (subMsg == null) return -1;
			byte subTag = subMsg.Tag;
			if (subTag != 1 && subTag != 2) return -1;
			if (subMsg.Position >= subMsg.Length) return -1;
			uint netId = subMsg.ReadPackedUInt32();
			if (netId == 0) return -1;
			return FindClientIdByNetId(netId);
		}
		catch
		{
			return -1;
		}
		finally
		{
			subMsg?.Recycle();
			copy?.Recycle();
		}
	}

private static int FindClientIdByNetId(uint netId)
	{
		if (netId == 0 || PlayerControl.AllPlayerControls == null) return -1;
		try
		{
			var players = PlayerControl.AllPlayerControls.GetEnumerator();
			while (players.MoveNext())
			{
				PlayerControl player = players.Current;
				if (player == null) continue;
				try
				{
					bool matched = ((InnerNetObject)player).NetId == netId;
					if (!matched && player.MyPhysics != null)
						matched = ((InnerNetObject)player.MyPhysics).NetId == netId;
					if (!matched && player.NetTransform != null)
						matched = ((InnerNetObject)player.NetTransform).NetId == netId;
					if (!matched) continue;

					int ownerId = ((InnerNetObject)player).OwnerId;
					if (ClientIdByOwnerId.TryGetValue(ownerId, out int clientId) && IsKnownRemoteClient(clientId))
						return clientId;
					if (IsKnownRemoteClient(ownerId))
						return ownerId;
				}
				catch { }
			}
			return -1;
		}
		catch
		{
			return -1;
		}
	}

private static void QueueRpcEnvelopeContext(RpcEnvelopeContext context)
	{
		CleanupRpcEnvelopeContexts();
		PendingRpcContexts.Add(context);
		if (PendingRpcContexts.Count <= 96)
		{
			return;
		}

		int removeCount = PendingRpcContexts.Count - 96;
		PendingRpcContexts.RemoveRange(0, removeCount);
	}

private static bool TryTakeRpcEnvelopeContext(PlayerControl player, byte callId, out RpcEnvelopeContext context)
	{
		context = default;
		if (player == null)
		{
			return false;
		}

		return TryTakeRpcEnvelopeContext((InnerNetObject)player, callId, out context);
	}

private static bool TryTakeRpcEnvelopeContext(InnerNetObject netObject, byte callId, out RpcEnvelopeContext context)
	{
		context = default;
		CleanupRpcEnvelopeContexts();
		if (netObject == null || PendingRpcContexts.Count == 0)
		{
			return false;
		}

		uint netId = netObject.NetId;
		for (int i = 0; i < PendingRpcContexts.Count; i++)
		{
			RpcEnvelopeContext candidate = PendingRpcContexts[i];
			if (candidate.NetId != netId || candidate.CallId != callId)
			{
				continue;
			}

			context = candidate;
			PendingRpcContexts.RemoveAt(i);
			return true;
		}

		return false;
	}

private static int ResolveRpcSenderClientId(int currentClientId, bool hasRpcContext, RpcEnvelopeContext rpcContext)
	{
		if (hasRpcContext && IsKnownRemoteClient(rpcContext.SenderClientId))
		{
			return rpcContext.SenderClientId;
		}

		return IsKnownRemoteClient(currentClientId) ? currentClientId : -1;
	}

private static void CleanupRpcEnvelopeContexts()
	{
		if (PendingRpcContexts.Count == 0)
		{
			return;
		}

		long now = Environment.TickCount64;
		for (int i = PendingRpcContexts.Count - 1; i >= 0; i--)
		{
			if (now - PendingRpcContexts[i].CreatedAtMs > 3000)
			{
				PendingRpcContexts.RemoveAt(i);
			}
		}
	}

internal static bool CheckRpc(PlayerControl player, int callId, MessageReader reader)
	{
		if (!Enabled())
		{
			return HarmonyControl.Continue;
		}

		if (player == null)
		{
			return HarmonyControl.Continue;
		}

		if (PlayerControl.LocalPlayer != null && player == PlayerControl.LocalPlayer)
		{
			return HarmonyControl.Continue;
		}

		int originalPosition = reader == null ? 0 : reader.Position;
		int clientId = GetResponsibleClientId(player);
		try
		{
			if (reader == null)
			{
				return HarmonyControl.Continue;
			}

			if (!ReaderLooksSane(reader))
			{
				return HarmonyControl.Continue;
			}

			if (callId < 0 || callId > byte.MaxValue)
			{
				BlockRpc(player, clientId, "RPC вне диапазона", $"CallId: {callId}.");
				return HarmonyControl.SkipOriginal;
			}

			byte rpcByte = (byte)callId;
			bool hasRpcContext = TryTakeRpcEnvelopeContext(player, rpcByte, out RpcEnvelopeContext rpcContext);
			clientId = ResolveRpcSenderClientId(clientId, hasRpcContext, rpcContext);
			if (clientId < 0 && rpcByte == 13)
			{
				clientId = GetVerifiedPlayerClientId(player);
			}

			if (ShouldTrustLocalOutfitRpc(player, callId, clientId))
			{
				return HarmonyControl.Continue;
			}

			if (TryHandleKnownModRpc(player, rpcByte, clientId, out bool skipKnownModRpc))
			{
				return skipKnownModRpc ? HarmonyControl.SkipOriginal : HarmonyControl.Continue;
			}

			if (LegacyExploitRpcIds.Contains(callId))
			{
				BlockRpc(player, clientId, "Подозрительный RPC", $"CallId: {callId}.");
				return HarmonyControl.SkipOriginal;
			}

			if (SuspiciousRpcIds.Contains(callId))
			{
				BlockRpc(player, clientId, "Suspicious RPC", $"CallId: {callId}.");
				return HarmonyControl.SkipOriginal;
			}

			if (rpcByte == 11)
			{
				NoteMeetingRpc();
			}

			bool lobbyJoinSyncGrace = IsLobbyJoinSyncGrace(player, clientId);
			bool isKnownVanillaRpc = Enum.IsDefined(typeof(RpcCalls), rpcByte);
			if (ShouldBlockRpcEnvelope(player, rpcByte, clientId, hasRpcContext, rpcContext))
			{
				return HarmonyControl.SkipOriginal;
			}

			float now = Time.realtimeSinceStartup;
			if (!lobbyJoinSyncGrace && clientId >= 0)
			{
				(int sameLimit, float sameWindow) = RpcSpamLimit(callId);
				if (HitSameRpcLimit(clientId, callId, now, sameWindow, sameLimit, out int sameCount))
				{
					string rpcAction = callId == 21 && ModOptions.SnapToAction != null ? ModOptions.SnapToAction.Value : null;
					BlockRpc(player, clientId, "RPC flood", $"{RpcName(rpcByte)}: {sameCount}/{sameLimit} за {sameWindow:0.00}с.", rpcAction);
					return HarmonyControl.SkipOriginal;
				}
			}

			if (ShouldBlockCrashRpc(player, rpcByte, clientId))
			{
				return HarmonyControl.SkipOriginal;
			}

			if (ShouldBlockPlayerSemanticRpc(player, rpcByte, clientId, reader))
			{
				return HarmonyControl.SkipOriginal;
			}

			return HarmonyControl.Continue;
		}
		catch (Exception error)
		{
			AcovPlugin.Logger?.LogWarning((object)$"Network protection ignored RPC check error for {PlayerName(player)} (client {clientId}, call {callId}): {error.Message}");
			return HarmonyControl.Continue;
		}
		finally
		{
			if (reader != null)
			{
				try
				{
					reader.Position = originalPosition;
				}
				catch
				{
				}
			}
		}
	}

internal static void TrustLocalCosmeticApply(PlayerControl target)
	{
		if (target == null)
		{
			return;
		}

		trustedLocalCosmeticPlayerId = target.PlayerId;
		trustedLocalCosmeticClientId = GetPlayerClientId(target);
		trustedLocalCosmeticUntil = Time.realtimeSinceStartup + 4.00f;
		trustedLocalCosmeticBudget = 64;
		trustedLocalCosmeticPacket = true;
	}

private static bool ShouldTrustLocalOutfitRpc(PlayerControl player, int callId, int clientId)
	{
		if (player == null)
		{
			return false;
		}

		if (!IsTrustedLocalCosmeticApplyActive() || player.PlayerId != trustedLocalCosmeticPlayerId)
		{
			ClearTrustedLocalCosmeticApply();
			return false;
		}

		if (trustedLocalCosmeticClientId >= 0)
		{
			int playerClientId = GetPlayerClientId(player);
			if (playerClientId >= 0 && playerClientId != trustedLocalCosmeticClientId)
			{
				ClearTrustedLocalCosmeticApply();
				return false;
			}
		}

		trustedLocalCosmeticBudget--;
		if (trustedLocalCosmeticBudget <= 0)
		{
			ClearTrustedLocalCosmeticApply();
		}

		return true;
	}

private static bool IsTrustedLocalCosmeticApplyActive()
	{
		if (!trustedLocalCosmeticPacket)
		{
			return false;
		}

		if (trustedLocalCosmeticBudget <= 0 || Time.realtimeSinceStartup > trustedLocalCosmeticUntil)
		{
			ClearTrustedLocalCosmeticApply();
			return false;
		}

		return true;
	}

private static void ClearTrustedLocalCosmeticApply()
	{
		trustedLocalCosmeticPlayerId = byte.MaxValue;
		trustedLocalCosmeticClientId = -1;
		trustedLocalCosmeticUntil = -1f;
		trustedLocalCosmeticBudget = 0;
		trustedLocalCosmeticPacket = false;
	}

internal static bool CheckShipStatusRpc(ShipStatus ship, int callId, MessageReader reader)
	{
		if (!Enabled() || AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost || ship == null)
		{
			return HarmonyControl.Continue;
		}

		if (callId != 35 || reader == null)
		{
			return HarmonyControl.Continue;
		}

		int clientId = GetActiveInboundSenderClientId();
		MessageReader copy = null;
		try
		{
			if (callId >= 0 && callId <= byte.MaxValue && TryTakeRpcEnvelopeContext((InnerNetObject)ship, (byte)callId, out RpcEnvelopeContext rpcContext))
			{
				clientId = ResolveRpcSenderClientId(clientId, true, rpcContext);
			}

			copy = MessageReader.Get(reader);
			int systemId = copy.ReadByte();
			PlayerControl actor = MessageExtensions.ReadNetObject<PlayerControl>(copy);
			byte amount = copy.ReadByte();
			if (actor != null && clientId < 0)
			{
				clientId = GetResponsibleClientId(actor);
			}

			int mapId = CurrentMapId();
			if (!SabotagePayloadFitsMap(systemId, amount, mapId))
			{
				BlockSystemRpc(actor, clientId, "Invalid sabotage payload", $"Map {mapId}, system {systemId}, amount {amount}.");
				return HarmonyControl.SkipOriginal;
			}

			if (IsCrewSabotagePayload(systemId, amount) && actor != null && !IsImpostor(actor) && !IsDead(actor))
			{
				BlockSystemRpc(actor, clientId, "Crew sabotage RPC", $"System {systemId}, amount {amount}.");
				return HarmonyControl.SkipOriginal;
			}
		}
		catch (Exception error)
		{
			BlockMessage(clientId, "Malformed ShipStatus RPC", error.Message);
			return HarmonyControl.SkipOriginal;
		}
		finally { copy?.Recycle(); }

		return HarmonyControl.Continue;
	}

internal static bool CheckVoteKickRpc(VoteBanSystem system, int callId, MessageReader reader)
	{
		if (!Enabled() || AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost || callId != 26 || reader == null)
		{
			return HarmonyControl.Continue;
		}

		int voterClientId = GetActiveInboundSenderClientId();
		MessageReader copy = null;
		try
		{
			if (TryTakeRpcEnvelopeContext((InnerNetObject)system, (byte)callId, out RpcEnvelopeContext rpcContext))
			{
				voterClientId = ResolveRpcSenderClientId(voterClientId, true, rpcContext);
			}

			copy = MessageReader.Get(reader);
			copy.ReadInt32();
			int target = copy.ReadInt32();

			InnerNetClient inner = (InnerNetClient)AmongUsClient.Instance;
			if (target == inner.ClientId || target == inner.HostId)
			{
				BlockMessage(voterClientId, "Vote-kick host", $"Target client {target}.");
				return HarmonyControl.SkipOriginal;
			}

			float now = Time.realtimeSinceStartup;
			if (ClientJoinTimeAt.TryGetValue(voterClientId, out float joinedAt) && now - joinedAt < EarlyJoinVoteGuardSeconds)
			{
				BlockMessage(voterClientId, "Early vote-kick", $"{now - joinedAt:0.0}s after join.");
				return HarmonyControl.SkipOriginal;
			}

			ClientData targetClient = GetClientById(target);
			PlayerControl targetPlayer = targetClient?.Character;
			if (targetPlayer != null && InActiveMatch() && !MeetingOrExileActive() && !IsDead(targetPlayer))
			{
				BlockMessage(voterClientId, "In-match vote-kick", $"{ClientName(target)} is alive.");
				return HarmonyControl.SkipOriginal;
			}
		}
		catch (Exception error)
		{
			BlockMessage(voterClientId, "Malformed vote-kick", error.Message);
			return HarmonyControl.SkipOriginal;
		}
		finally { copy?.Recycle(); }

		return HarmonyControl.Continue;
	}

internal static void TrackClientJoined(ClientData client)
	{
		if (!Enabled() || client == null || AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
		{
			return;
		}

		InnerNetClient inner = (InnerNetClient)AmongUsClient.Instance;
		if (client.Id == inner.ClientId || client.Id == inner.HostId)
		{
			return;
		}

		RememberClient(client);
		float now = Time.realtimeSinceStartup;
		lastLobbyJoinAt = now;
		ClientJoinTimeAt[client.Id] = now;
		RememberRecentJoinCandidate(client.Id);
		PendingJoinIntegrityAt[client.Id] = now + JoinIntegrityDelaySeconds;
		AcovPlugin.Logger?.LogInfo((object)$"Acov.Net client {client.Id} passed entry checks");
		LogClientSuccessDetails(client);
		AcovPlugin.Logger?.LogInfo((object)$"Acov.Net client {client.Id} entered lobby");
	}

private static void LogClientSuccessDetails(ClientData client)
	{
		if (client == null)
		{
			return;
		}

		int platformTag = 0;
		string platform = "-";
		string rawPlatformName = string.Empty;
		try
		{
			if (client.PlatformData != null)
			{
				platformTag = (int)client.PlatformData.Platform;
				platform = client.PlatformData.Platform.ToString();
				rawPlatformName = SafeString(client.PlatformData.PlatformName);
			}
		}
		catch
		{
		}

		string levelText = "-";
		try
		{
			if (AcovPlayerLevels.TryGetDisplayLevel(client.Id, out uint cachedLevel))
			{
				levelText = cachedLevel.ToString();
			}
			else if (client.Character != null && AcovPlayerLevels.TryGetDisplayLevel(client.Character, out uint characterLevel))
			{
				levelText = characterLevel.ToString();
			}
		}
		catch
		{
		}

		AcovPlugin.Logger?.LogInfo((object)$"Acov.Net client info: id={client.Id}, player='{TrimJoinLog(ReadClientName(client, client.PlayerName), 64)}', platformTag={platformTag}, platform='{TrimJoinLog(platform, 48)}', rawPlatformName='{TrimJoinLog(rawPlatformName, 64)}', level={levelText}, friendCode='{TrimJoinLog(SafeString(client.FriendCode), 64)}', productUserId='{TrimJoinLog(SafeString(client.ProductUserId), 128)}'");
	}

internal static void UpdateJoinIntegrityChecks()
	{
		PruneRecentJoinSenderCandidates();
		if (PendingJoinIntegrityAt.Count == 0)
		{
			return;
		}

		float now = Time.realtimeSinceStartup;
		List<int> done = null;
		List<int> ids = new List<int>(PendingJoinIntegrityAt.Keys);
		for (int i = 0; i < ids.Count; i++)
		{
			int clientId = ids[i];
			if (!PendingJoinIntegrityAt.TryGetValue(clientId, out float dueAt) || now < dueAt)
			{
				continue;
			}

			if (done == null)
			{
				done = new List<int>();
			}

			done.Add(clientId);
			ClientData client = GetClientById(clientId);
			if (client == null)
			{
				continue;
			}

			float joinedAt = ClientJoinTimeAt.TryGetValue(clientId, out float savedJoinAt) ? savedJoinAt : now;
			if (!ClientIdentityReady(client))
			{
				if (now - joinedAt < JoinIntegrityMaxWaitSeconds)
				{
					PendingJoinIntegrityAt[clientId] = now + JoinIntegrityRetrySeconds;
					done.RemoveAt(done.Count - 1);
					continue;
				}

				AcovPlugin.Logger?.LogWarning((object)$"Network protection stopped waiting for client {clientId}: player data is still incomplete.");
				continue;
			}

			if (BrokenFriendCodeBanEnabled() && !FriendCodeReady(client) && now - joinedAt < JoinIntegrityMaxWaitSeconds)
			{
				PendingJoinIntegrityAt[clientId] = now + JoinIntegrityRetrySeconds;
				done.RemoveAt(done.Count - 1);
				continue;
			}

			if (BrokenFriendCodeBanEnabled() && FriendCodeReady(client) && HasBrokenFriendCode(client))
			{
				BlockMessage(clientId, "Broken friend code", "Client identity failed join hygiene check.", "Ban");
			}
		}

		if (done == null)
		{
			return;
		}

		for (int i = 0; i < done.Count; i++)
		{
			PendingJoinIntegrityAt.Remove(done[i]);
		}
	}

private static bool ShouldBlockRpcEnvelope(PlayerControl player, byte rpcByte, int clientId, bool hasRpcContext, RpcEnvelopeContext rpcContext)
	{
		if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
		{
			return false;
		}

		if (IsLobbyJoinSyncGrace(player, clientId))
		{
			return false;
		}

		if (IsMeetingTransitionGrace())
		{
			return false;
		}

		int targetClientId = hasRpcContext ? rpcContext.TargetClientId : activeInboundTargetClientId;
		if (targetClientId >= 0 && !TargetedRpcAllowedIds.Contains(rpcByte))
		{
			BlockRpc(player, clientId, "Target-only RPC", $"{RpcName(rpcByte)} -> client {targetClientId}.");
			return true;
		}

		int dataIndex = hasRpcContext ? rpcContext.DataIndex : 1;
		if (!PackedDataAllowedRpcIds.Contains(rpcByte) && ((dataIndex > 1 && ImmediateRpcIds.Contains(rpcByte)) || dataIndex > 10))
		{
			if (targetClientId > 0)
			{
				int dataCount = hasRpcContext ? rpcContext.DataCount : activeInboundGameDataParts;
				BlockRpc(player, clientId, "Packed instant RPC", $"{RpcName(rpcByte)} at data #{dataIndex}/{dataCount}.");
				return true;
			}

			return false;
		}

		SendOption sendOption = hasRpcContext ? rpcContext.SendOption : SendOption.Reliable;
		if ((int)sendOption == 0 && rpcByte != 0)
		{
			if (targetClientId > 0 && rpcByte != 13)
			{
				BlockRpc(player, clientId, "Invalid RPC delivery", $"{RpcName(rpcByte)} was sent with {sendOption}.");
				return true;
			}

			return false;
		}

		if (LobbyBehaviour.Instance != null && !LobbyAllowedRpcIds.Contains(rpcByte))
		{
			BlockRpc(player, clientId, "Lobby RPC mismatch", $"{RpcName(rpcByte)} is not expected in lobby.");
			return true;
		}

		return false;
	}
}
}
