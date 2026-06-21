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

private struct RpcEnvelopeContext
	{
		internal uint NetId;
		internal byte CallId;
		internal int TargetClientId;
		internal int SenderClientId;
		internal int DataIndex;
		internal int DataCount;
		internal SendOption SendOption;
		internal long CreatedAtMs;
	}

private struct ClientSnapshot
	{
		internal int ClientId;
		internal byte PlayerId;
		internal int OwnerId;
		internal string PlayerName;
		internal string FriendCode;
		internal string ProductUserId;
		internal float JoinedAt;
		internal float LastSeenAt;
	}

private const int MaxMessagesPerFrame = 200;

private const int DefaultRpcLimitPerWindow = 10;

private const float DefaultRpcWindowSeconds = 1f;

private const int MaxChatMessagesPerWindow = 5;

private const float ChatWindowSeconds = 2f;

private const int MaxGameDataPartsPerPacket = 160;

private const int LobbyJoinGameDataPartsLimit = 320;

private const int MaxTrackedGameDataParts = LobbyJoinGameDataPartsLimit + 1;

private const int DataFloodCountPerSec = 60;

private const int DataFloodBytesPerSec = 65536;

private const int DataSinglePayloadMax = 4096;

private const float EarlyJoinVoteGuardSeconds = 5f;

private const float LobbyEnteredSyncGraceSeconds = 5f;

private const float LobbyRecentJoinSyncGraceSeconds = 4f;

private const float LobbyJoinWarmupSeconds = 12f;

private const float JoinIntegrityDelaySeconds = 25f;

private const float JoinIntegrityRetrySeconds = 10f;

private const float JoinIntegrityMaxWaitSeconds = 120f;

private const float MeetingTransitionGraceSeconds = 8f;

private const float ClientSnapshotTtlSeconds = 600f;

private const float RecentJoinSenderFallbackSeconds = 12f;

private const long ActiveInboundSenderTtlMs = 500;

private const float FloodConnectionDropSeconds = 14f;

private const float ProtectionNoticeRepeatSeconds = 1.25f;

private static readonly HashSet<byte> ValidMessageTags = new HashSet<byte>
	{
		0, 1, 2, 3, 4, 5, 6, 7, 8, 10,
		11, 12, 13, 14, 16, 17, 22, 24,
	};

private static readonly HashSet<int> LegacyExploitRpcIds = new HashSet<int>
	{
	};

private static readonly HashSet<int> SuspiciousRpcIds = new HashSet<int>
	{
		85, 101,
	};

private static readonly HashSet<int> AllowedCustomRpcIds = new HashSet<int>
	{
		212, 213, 214, 215, 216, 217, 218, 219,
		151, 152, 176, 210,
		70, 78, 80, 81,
	};

private static readonly HashSet<int> ImmediateRpcIds = new HashSet<int>
	{
		45, 46, 47, 48, 51, 52, 53, 54, 55, 56,
		62, 63, 64, 65,
		2, 5, 7, 12, 14, 21,
	};

private static readonly HashSet<int> PackedDataAllowedRpcIds = new HashSet<int>
	{
		38, 39, 40, 41, 42, 43,
		5, 6, 8,
	};

private static readonly HashSet<int> TargetedRpcAllowedIds = new HashSet<int>
	{
		13, 51, 55, 62, 64, 5, 7,
	};

private static readonly HashSet<int> LobbyAllowedRpcIds = new HashSet<int>
	{
		212, 213, 214, 215, 216, 217, 218, 219,
		151, 152, 176, 210, 70, 78, 80, 81,
		36, 37, 38, 39, 40, 41, 42, 43,
		2, 5, 7, 9, 10, 13, 17, 18, 21, 33, 49, 50, 60, 61,
	};

private static readonly HashSet<int> CosmeticMutationRpcIds = new HashSet<int>
	{
		5, 6, 8, 39, 40, 41, 42, 43,
	};

private static readonly HashSet<byte> NameStringRpcIds = new HashSet<byte> { 5, 6 };

private const int MaxRpcStringBytes = 512;

private const int MaxSpawnsPerScene = 300;

private const int MaxUnownedSpawnsPerScene = 100;

private static int spawnSceneId;

private static int spawnsThisScene;

private static int unownedSpawnsThisScene;

internal static void ResetSpawnFloodCounters()
	{
		spawnSceneId = 0;
		spawnsThisScene = 0;
		unownedSpawnsThisScene = 0;
	}

private static readonly Dictionary<int, (int Limit, float Window)> HighVolumeRpcLimits = new Dictionary<int, (int Limit, float Window)>
	{
		{ 49, (100, 0.1f) },
		{ 50, (100, 0.1f) },
		{ 18, (100, 0.1f) },
		{ 7, (100, 0.1f) },
		{ 44, (50, 0.1f) },
		{ 6, (30, 0.1f) },
		{ 8, (30, 0.1f) },
		{ 39, (30, 0.1f) },
		{ 40, (30, 0.1f) },
		{ 41, (30, 0.1f) },
		{ 42, (30, 0.1f) },
		{ 21, (15, 1f) },
		{ 54, (5, 1f) },
		{ 33, (1, 1f) },
	};

private static readonly Dictionary<int, Dictionary<int, Queue<float>>> SameRpcByClient = new Dictionary<int, Dictionary<int, Queue<float>>>();

private static readonly Dictionary<int, Queue<float>> ChatByClient = new Dictionary<int, Queue<float>>();

private static readonly Dictionary<int, float> LastTaskRpcAt = new Dictionary<int, float>();

private static readonly Dictionary<int, float> ClientJoinTimeAt = new Dictionary<int, float>();

private static readonly Dictionary<int, float> RecentJoinSenderCandidates = new Dictionary<int, float>();

private static readonly Dictionary<int, float> LastClientActionAt = new Dictionary<int, float>();

private static readonly Dictionary<int, float> PendingLocalCleanupAt = new Dictionary<int, float>();

private static readonly Dictionary<int, float> PendingJoinIntegrityAt = new Dictionary<int, float>();

private static readonly Dictionary<int, ClientSnapshot> ClientSnapshotsById = new Dictionary<int, ClientSnapshot>();

private static readonly Dictionary<byte, int> ClientIdByPlayerId = new Dictionary<byte, int>();

private static readonly Dictionary<int, int> ClientIdByOwnerId = new Dictionary<int, int>();

private static readonly Dictionary<int, int> MessagesThisFrameByClient = new Dictionary<int, int>();

private static readonly HashSet<int> WarnedFloodClientsThisFrame = new HashSet<int>();

private static readonly Dictionary<string, int> ClientIdByConnectionKey = new Dictionary<string, int>();

private static readonly Dictionary<string, float> ConnectionKeySeenAt = new Dictionary<string, float>();

private static readonly HashSet<string> AmbiguousConnectionKeys = new HashSet<string>();

private static readonly Dictionary<int, float> FloodDropClientUntil = new Dictionary<int, float>();

private static readonly Dictionary<string, float> FloodDropConnectionUntil = new Dictionary<string, float>();

private static readonly Dictionary<string, int> FloodDropClientByConnectionKey = new Dictionary<string, int>();

private static readonly Dictionary<string, float> HostRateDropByKey = new Dictionary<string, float>();

private static readonly Dictionary<string, int> HostDatagramsThisFrameByKey = new Dictionary<string, int>();

private static int hostDatagramFrame = -1;

private static readonly Dictionary<string, FloodAttributionState> FloodAttributionByKey = new Dictionary<string, FloodAttributionState>();

private const float FloodAttributionWindowSeconds = 5f;

private const float SpoofingSuppressionSeconds = 30f;

private sealed class FloodAttributionState
	{
		public readonly HashSet<int> DistinctClientIds = new HashSet<int>();
		public float WindowStart;
		public float SuppressUntil;
	}

private static readonly Dictionary<string, float> ProtectionNoticeSeenAt = new Dictionary<string, float>();

private static readonly HashSet<string> WarnedModRpcOnce = new HashSet<string>();

private static readonly List<int> ScratchClientSnapshotIds = new List<int>(16);

private static readonly List<string> ScratchConnectionKeys = new List<string>(16);

private static readonly List<RpcEnvelopeContext> PendingRpcContexts = new List<RpcEnvelopeContext>(32);

private static readonly HashSet<uint> ScratchSpawnNetIds = new HashSet<uint>();

private sealed class DataBurst { public readonly Queue<float> Ts = new Queue<float>(); public readonly Queue<int> By = new Queue<int>(); public int Total; }

private static readonly Dictionary<uint, DataBurst> DataByNetId = new Dictionary<uint, DataBurst>();

private static float nextDataFloodCleanupAt;

private static readonly Dictionary<long, Queue<float>> NonHostRpcByKey = new Dictionary<long, Queue<float>>();

private static float nextNonHostRpcCleanupAt;

private static readonly Queue<float> SnapToAggregate = new Queue<float>();

private const int MaxSnapToAggregatePerSec = 30;

private static int messageFrame = -1;

private static int messagesThisFrame;

private static float nonHostFloodDropUntil = 0f;

private static int nonHostDatagramFrame = -1;

private static int nonHostDatagramsThisFrame = 0;

private const int NonHostFloodDatagramThreshold = 50;

private static int lastRememberedClientsFrame = -1;

private static float lastScreenNoticeAt = -10f;

private static float lastLobbyJoinAt = -1000f;

private static float lastLobbyEnteredAt = -1000f;

private static int lastObservedLobbyGameState = -1;

private static byte trustedLocalCosmeticPlayerId = byte.MaxValue;

private static int trustedLocalCosmeticClientId = -1;

private static float trustedLocalCosmeticUntil = -1f;

private static int trustedLocalCosmeticBudget;

private static bool trustedLocalCosmeticPacket;

private static int activeInboundSenderClientId = -1;

private static string activeInboundConnectionKey;

private static int activeInboundTargetClientId = -1;

private static int activeInboundGameDataParts = 0;

private static bool activeInboundHasInvalidPartTag;

private static bool activeInboundHasSpawnExploit;

private static string activeInboundSpawnExploitDetail;

private static bool activeInboundHasDataFlood;

private static string activeInboundDataFloodDetail;

private static bool activeInboundPhantomFlood;

private static bool activeInboundHasPhantomNetId;

private static readonly Dictionary<uint, long> pendingUnknownNetId = new Dictionary<uint, long>();

private static long lastPendingUnknownCleanupMs;

private const int UnknownNetIdGraceMs = 2000;

private static long phantomDataWindowMs;

private static int phantomDataInWindow;

private const int MaxPhantomDataPerSec = 30;

private static long activeInboundSenderSetAtMs;

private static int activeInboundSenderFrame = -1;

private static float lastMeetingRpcAt = -1000f;

private static float lastInboundSenderErrorAt = -10f;

internal static void TrackInboundSender(InnerNetClient client, DataReceivedEventArgs eventArgs)
	{
		try
		{
			ClearActiveInboundSender();

			if (AcovNetPacketMonitor.Enabled && client != null && !client.AmHost && eventArgs != null)
			{
				AcovNetPacketMonitor.RecordInboundPacket(-1, eventArgs.Message);
			}

			if (!Enabled() || client == null || !client.AmHost || eventArgs == null)
			{
				if (EnabledNonHostFloodDrop() && client != null && !client.AmHost)
				{
					int frame = Time.frameCount;
					if (frame != nonHostDatagramFrame)
					{
						nonHostDatagramFrame = frame;
						nonHostDatagramsThisFrame = 0;
					}
					nonHostDatagramsThisFrame++;
					if (nonHostDatagramsThisFrame > NonHostFloodDatagramThreshold)
					{
						nonHostFloodDropUntil = Time.realtimeSinceStartup + FloodConnectionDropSeconds;
					}
				}
				return;
			}

			activeInboundConnectionKey = PacketConnectionKey(eventArgs) ?? string.Empty;

			if (!string.IsNullOrWhiteSpace(activeInboundConnectionKey))
			{
				float nowFast = Time.realtimeSinceStartup;

				if (FloodDropConnectionUntil.TryGetValue(activeInboundConnectionKey, out float dropUntil) && nowFast <= dropUntil)
				{
					if (FloodDropClientByConnectionKey.TryGetValue(activeInboundConnectionKey, out int droppedFast) && droppedFast >= 0)
					{
						SetActiveInboundSender(droppedFast);
						if (AcovNetPacketMonitor.Enabled)
						{
							AcovNetPacketMonitor.RecordInboundPacket(droppedFast, eventArgs.Message);
						}
					}
					return;
				}

				int dgFrame = Time.frameCount;
				if (dgFrame != hostDatagramFrame)
				{
					hostDatagramFrame = dgFrame;
					HostDatagramsThisFrameByKey.Clear();
				}
				HostDatagramsThisFrameByKey.TryGetValue(activeInboundConnectionKey, out int dgCount);
				dgCount++;
				HostDatagramsThisFrameByKey[activeInboundConnectionKey] = dgCount;
				if (dgCount > NonHostFloodDatagramThreshold)
				{
					HostRateDropByKey[activeInboundConnectionKey] = nowFast + FloodConnectionDropSeconds;
					return;
				}

				if (HostRateDropByKey.TryGetValue(activeInboundConnectionKey, out float rateDrop) && nowFast < rateDrop)
				{
					return;
				}
			}

			RememberAllClients();
			int senderClientId = ResolvePacketSenderClientId(client, eventArgs);
			if (senderClientId < 0)
			{
				senderClientId = ResolveConnectionClientId(activeInboundConnectionKey, client);
			}

			if (senderClientId < 0 && TryGetFloodDropClientForActiveConnection(out int droppedClientId))
			{
				senderClientId = droppedClientId;
			}

			if (senderClientId < 0)
			{
				return;
			}

			ClientData sender = GetClientById(senderClientId) ?? GetRecentClient(client, senderClientId);
			RememberClient(sender);
			RememberConnectionClient(activeInboundConnectionKey, senderClientId);
			SetActiveInboundSender(senderClientId);
			if (AcovNetPacketMonitor.Enabled)
			{
				AcovNetPacketMonitor.RecordInboundPacket(ResolveBestActiveClientId(senderClientId), eventArgs.Message);
			}
		}
		catch (Exception error)
		{
			ClearActiveInboundSender();
			float now = Time.realtimeSinceStartup;
			if (now - lastInboundSenderErrorAt > 2f)
			{
				lastInboundSenderErrorAt = now;
				AcovPlugin.Logger?.LogWarning((object)$"Network protection ignored sender tracking error: {error.Message}");
			}
		}
	}

internal static bool CheckMessage(InnerNetClient client, MessageReader reader, SendOption sendOption)
	{
		if (!Enabled())
		{
			if (client != null && reader != null && !ValidMessageTags.Contains(reader.Tag))
			{
				return HarmonyControl.SkipOriginal;
			}

			if (client != null)
			{
				int so = (int)sendOption;
				if (so != 0 && so != 1)
				{
					return HarmonyControl.SkipOriginal;
				}
			}

			if (client != null && reader != null && (reader.Tag == 5 || reader.Tag == 6))
			{
				if (reader.Length - reader.Position < 4)
				{
					return HarmonyControl.SkipOriginal;
				}

				if (!GameDataFramingValid(reader, reader.Tag))
				{
					AcovSecurityNotifications.Show("Warn", null, "Malformed GameData (non-host)", "Bad framing", -1);
					return HarmonyControl.SkipOriginal;
				}

				try
				{
					MessageReader copy = MessageReader.Get(reader);
					copy.ReadInt32();
					if (reader.Tag == 6)
					{
						copy.ReadPackedInt32();
					}

					int parts = 0;
					while (copy.Position < copy.Length && parts < MaxTrackedGameDataParts)
					{
						MessageReader part = copy.ReadMessage();
						if (part == null) break;
						parts++;
						byte partTag = part.Tag;
						if (partTag == 0 || partTag == 3 || partTag > 8)
						{
							return HarmonyControl.SkipOriginal;
						}

						if (partTag == 1 && client != null && !client.AmHost &&
							ModOptions.NonHostDataDrop != null && ModOptions.NonHostDataDrop.Value)
						{
							try
							{
								MessageReader idc = MessageReader.Get(part);
								uint nid = idc.ReadPackedUInt32();
								int by = idc.Length - idc.Position;

								if (AmongUsClient.Instance != null)
								{
									InnerNetClient innerData = (InnerNetClient)AmongUsClient.Instance;
									if (nid != 0 &&
										!innerData.allObjects.AllObjectsFast.ContainsKey(nid) &&
										!innerData.DestroyedObjects.Contains(nid))
									{
										bool flood = PhantomDataRateExceeded();
										bool phantom = IsPhantomNetId(nid);
										if (flood && phantom)
										{
											AcovSecurityNotifications.Show("Warn", null, "Phantom DATA flood (non-host)", $"netId {nid}", -1);
											return HarmonyControl.SkipOriginal;
										}
									}
								}

								if (ShouldDropDataObject(nid, by))
								{
									AcovSecurityNotifications.Show("Warn", null, "DATA flood (non-host)", $"netId {nid}", -1);
									return HarmonyControl.SkipOriginal;
								}
							}
							catch { }
						}

						if (partTag == 2 && client != null && !client.AmHost &&
							ModOptions.NonHostDataDrop != null && ModOptions.NonHostDataDrop.Value)
						{
							try
							{
								MessageReader rc = MessageReader.Get(part);
								uint rnid = rc.ReadPackedUInt32();
								byte rcall = rc.ReadByte();

								if (NameRpcStringTooLong(part, rcall))
								{
									AcovSecurityNotifications.Show("Warn", null, "Oversized name RPC (non-host)", $"#{rcall} netId {rnid}", -1);
									return HarmonyControl.SkipOriginal;
								}

								if (VentRpcOutOfRange(part, rcall))
								{
									AcovSecurityNotifications.Show("Warn", null, "Invalid vent id (non-host)", $"#{rcall} netId {rnid}", -1);
									return HarmonyControl.SkipOriginal;
								}

								bool rpcFlood = ShouldDropNonHostHighVolumeRpc(rnid, rcall);
								if (rcall == 21 && SnapToAggregateExceeded())
								{
									rpcFlood = true;
								}

								bool rpcPhantom = false;
								if (!rpcFlood && AmongUsClient.Instance != null)
								{
									InnerNetClient innerRpc = (InnerNetClient)AmongUsClient.Instance;
									if (rnid != 0 &&
										!innerRpc.allObjects.AllObjectsFast.ContainsKey(rnid) &&
										!innerRpc.DestroyedObjects.Contains(rnid))
									{
										bool rflood = PhantomDataRateExceeded();
										if (rflood && IsPhantomNetId(rnid))
										{
											rpcFlood = true;
											rpcPhantom = true;
										}
									}
								}

								if (rpcFlood)
								{
									string what = rpcPhantom ? $"Phantom RPC flood (non-host) #{rcall}"
										: rcall == 21 ? "SnapTo flood (non-host)" : $"RPC flood (non-host) #{rcall}";
									AcovSecurityNotifications.Show("Warn", null, what, $"netId {rnid}", -1);
									return HarmonyControl.SkipOriginal;
								}
							}
							catch { }
						}

					if (partTag == 4 && client != null && !client.AmHost && SpawnShouldDrop(part))
					{
						AcovSecurityNotifications.Show("Warn", null, "Spawn flood (non-host)", "fake-owner / over cap", -1);
						return HarmonyControl.SkipOriginal;
					}

					if (partTag == 4 && client != null && !client.AmHost && IdenticalNetIdProtectionEnabled() &&
						SpawnHasDuplicateNetId(part))
					{
						AcovSecurityNotifications.Show("Warn", null, "Duplicate spawn (non-host)", "netId already live", -1);
						return HarmonyControl.SkipOriginal;
					}
					}

					if (parts > LobbyJoinGameDataPartsLimit)
					{
						return HarmonyControl.SkipOriginal;
					}
				}
				catch { }
			}

			if (EnabledNonHostFloodDrop() && client != null && reader != null)
			{
				float now = Time.realtimeSinceStartup;
				if (now < nonHostFloodDropUntil)
					return HarmonyControl.SkipOriginal;
				if (ShouldBlockMessageFrameFlood(-1, out _))
				{
					nonHostFloodDropUntil = now + FloodConnectionDropSeconds;
					return HarmonyControl.SkipOriginal;
				}
			}
			return HarmonyControl.Continue;
		}

		if (!string.IsNullOrWhiteSpace(activeInboundConnectionKey) &&
			HostRateDropByKey.TryGetValue(activeInboundConnectionKey, out float hostRateUntil) &&
			Time.realtimeSinceStartup < hostRateUntil)
		{
			return HarmonyControl.SkipOriginal;
		}

		if (IsTrustedLocalCosmeticApplyActive())
		{
			return HarmonyControl.Continue;
		}

		PlatformSpoofGuard.InspectRawJoinMessage(client, reader);

		if (AcovAccessLists.TryHandleJoinMessage(client, reader))
		{
			return HarmonyControl.SkipOriginal;
		}

		if (client == null)
		{
			return HarmonyControl.Continue;
		}

		RememberAllClients();
		if (reader == null)
		{
			BlockMessage(GetActiveInboundSenderClientId(), "Сетевой пакет заблокирован", "Пустой MessageReader.");
			return HarmonyControl.SkipOriginal;
		}

		try
		{
			ResetInboundEnvelope(sendOption);

			int senderClientId = ResolveBestActiveClientId(GetActiveInboundSenderClientId());

			if (senderClientId < 0 && reader != null)
			{
				int contentId = TryResolveOwnerFromGameDataContent(client, reader);
				if (contentId >= 0)
				{
					SetActiveInboundSender(contentId);
					senderClientId = contentId;
				}
			}

			if (ShouldDropInboundFlood(senderClientId))
			{
				return HarmonyControl.SkipOriginal;
			}

			if (ShouldBlockMessageFrameFlood(senderClientId, out int frameCount))
			{
				RegisterInboundFloodDrop(senderClientId);
				string floodAction = IsFloodAttributionUnreliable(senderClientId) ? "Null" : null;
				BlockMessage(senderClientId, "Message flood", $"{frameCount}/{MaxMessagesPerFrame} messages in one frame.", floodAction);
				return HarmonyControl.SkipOriginal;
			}

			if (!ReaderLooksSane(reader))
			{
				BlockMessage(senderClientId, "Сетевой пакет заблокирован", $"Позиция {reader.Position}, длина {reader.Length}.");
				return HarmonyControl.SkipOriginal;
			}

			byte tag = reader.Tag;
			if (!ValidMessageTags.Contains(tag))
			{
				BlockMessage(senderClientId, "Сетевой пакет заблокирован", $"Неверный tag: {tag}.", "Null");
				return HarmonyControl.SkipOriginal;
			}

			if ((tag == 5 || tag == 6) && reader.Length - reader.Position < 4)
			{
				BlockMessage(senderClientId, "GameData заблокирован", "Пакет слишком короткий.", "Null");
				return HarmonyControl.SkipOriginal;
			}

			if (tag == 1)
			{
				CaptureJoinEnvelope(reader);
				senderClientId = GetActiveInboundSenderClientId();
			}

			if (tag == 5 || tag == 6)
			{
				if (!GameDataFramingValid(reader, tag))
				{
					if (senderClientId >= 0) RegisterInboundFloodDrop(senderClientId);
					string malformedAction = ModOptions.MalformedDataAction != null ? ModOptions.MalformedDataAction.Value : "Warn";
					BlockMessage(senderClientId, "Malformed GameData", "Bad framing — dropped before parse.", malformedAction);
					return HarmonyControl.SkipOriginal;
				}

				CaptureGameDataEnvelope(reader, tag, sendOption);
				int dataPartsLimit = IsLobbyJoinSyncGrace() ? LobbyJoinGameDataPartsLimit : MaxGameDataPartsPerPacket;
				if (activeInboundGameDataParts > dataPartsLimit)
				{
					RegisterInboundFloodDrop(senderClientId);
					BlockMessage(senderClientId, "GameData flood", $"{activeInboundGameDataParts}/{dataPartsLimit} parts.", "Null");
					return HarmonyControl.SkipOriginal;
				}

				if (activeInboundHasInvalidPartTag)
				{
					RegisterInboundFloodDrop(senderClientId);
					BlockMessage(senderClientId, "Invalid GameData tag", "Sub-message tag out of valid range.", "Null");
					return HarmonyControl.SkipOriginal;
				}

				if (activeInboundHasPhantomNetId && activeInboundPhantomFlood)
				{
					if (senderClientId >= 0) RegisterInboundFloodDrop(senderClientId);
					BlockMessage(senderClientId, "Phantom DATA flood", "Flood of DATA for non-existent netId(s).", "Null");
					return HarmonyControl.SkipOriginal;
				}

				if (activeInboundHasSpawnExploit)
				{
					if (senderClientId >= 0) RegisterInboundFloodDrop(senderClientId);
					BlockMessage(senderClientId, "Identical NetId attack", activeInboundSpawnExploitDetail ?? "Spawn payload would cause server desync.");
					return HarmonyControl.SkipOriginal;
				}

				if (activeInboundHasDataFlood)
				{
					if (senderClientId >= 0) RegisterInboundFloodDrop(senderClientId);
					BlockMessage(senderClientId, "DATA flood", activeInboundDataFloodDetail ?? "Too many DATA updates on one object.", "Warn");
					return HarmonyControl.SkipOriginal;
				}
			}

			return HarmonyControl.Continue;
		}
		catch (Exception error)
		{
			AcovPlugin.Logger?.LogWarning((object)$"Network protection blocked a malformed message: {error.Message}");
			BlockMessage(GetActiveInboundSenderClientId(), "Сетевой пакет заблокирован", "Ошибка проверки пакета.");
			return HarmonyControl.SkipOriginal;
		}
	}

private static bool ShouldBlockMessageFrameFlood(int senderClientId, out int frameCount)
	{
		if (Time.frameCount != messageFrame)
		{
			messageFrame = Time.frameCount;
			messagesThisFrame = 0;
			MessagesThisFrameByClient.Clear();
			WarnedFloodClientsThisFrame.Clear();
		}

		messagesThisFrame++;
		if (senderClientId < 0)
		{
			frameCount = messagesThisFrame;
			return frameCount > MaxMessagesPerFrame;
		}

		MessagesThisFrameByClient.TryGetValue(senderClientId, out frameCount);
		frameCount++;
		MessagesThisFrameByClient[senderClientId] = frameCount;
		return frameCount > MaxMessagesPerFrame;
	}

private static bool ShouldDropInboundFlood(int senderClientId)
	{
		PruneFloodDrops();
		float now = Time.realtimeSinceStartup;
		senderClientId = ResolveBestActiveClientId(senderClientId);
		if (senderClientId >= 0 && FloodDropClientUntil.TryGetValue(senderClientId, out float clientUntil))
		{
			if (now <= clientUntil)
			{
				return true;
			}

			FloodDropClientUntil.Remove(senderClientId);
		}

		if (!string.IsNullOrWhiteSpace(activeInboundConnectionKey) &&
			FloodDropConnectionUntil.TryGetValue(activeInboundConnectionKey, out float connectionUntil))
		{
			if (now <= connectionUntil)
			{
				if (activeInboundSenderClientId < 0 && FloodDropClientByConnectionKey.TryGetValue(activeInboundConnectionKey, out int droppedClientId))
				{
					SetActiveInboundSender(droppedClientId);
				}

				return true;
			}

			FloodDropConnectionUntil.Remove(activeInboundConnectionKey);
			FloodDropClientByConnectionKey.Remove(activeInboundConnectionKey);
		}

		return false;
	}

private static void RegisterInboundFloodDrop(int clientId)
	{
		PruneFloodDrops();
		clientId = ResolveBestActiveClientId(clientId);

		if (clientId < 0 && AmongUsClient.Instance != null)
		{
			clientId = ResolveSingleRemoteClientId((InnerNetClient)AmongUsClient.Instance);
		}

		if (clientId < 0)
		{
			clientId = TryResolveRecentSingleFloodCandidate();
		}

		float until = Time.realtimeSinceStartup + FloodConnectionDropSeconds;
		if (clientId >= 0)
		{
			FloodDropClientUntil[clientId] = until;
		}

		if (string.IsNullOrWhiteSpace(activeInboundConnectionKey))
		{
			return;
		}

		bool keyIsAmbiguous = AmbiguousConnectionKeys.Contains(activeInboundConnectionKey);
		if (!keyIsAmbiguous || clientId >= 0)
		{
			FloodDropConnectionUntil[activeInboundConnectionKey] = until;
		}
		if (clientId >= 0)
		{
			FloodDropClientByConnectionKey[activeInboundConnectionKey] = clientId;
			RememberConnectionClient(activeInboundConnectionKey, clientId);
		}
	}

private static bool IsFloodAttributionUnreliable(int clientId)
	{
		if (string.IsNullOrWhiteSpace(activeInboundConnectionKey))
		{
			return false;
		}

		bool keyAmbiguous = AmbiguousConnectionKeys.Contains(activeInboundConnectionKey);
		if (keyAmbiguous)
		{
			return true;
		}

		if (clientId < 0)
		{
			return false;
		}

		float now = Time.realtimeSinceStartup;
		if (!FloodAttributionByKey.TryGetValue(activeInboundConnectionKey, out FloodAttributionState state))
		{
			state = new FloodAttributionState { WindowStart = now };
			state.DistinctClientIds.Add(clientId);
			FloodAttributionByKey[activeInboundConnectionKey] = state;
			return false;
		}

		if (now < state.SuppressUntil)
		{
			return true;
		}

		if (now - state.WindowStart > FloodAttributionWindowSeconds)
		{
			state.DistinctClientIds.Clear();
			state.WindowStart = now;
		}

		state.DistinctClientIds.Add(clientId);
		if (state.DistinctClientIds.Count >= 2)
		{
			state.SuppressUntil = now + SpoofingSuppressionSeconds;
			return true;
		}

		return false;
	}

private static bool TryGetFloodDropClientForActiveConnection(out int clientId)
	{
		clientId = -1;
		if (string.IsNullOrWhiteSpace(activeInboundConnectionKey))
		{
			return false;
		}

		PruneFloodDrops();
		return FloodDropClientByConnectionKey.TryGetValue(activeInboundConnectionKey, out clientId) && clientId >= 0;
	}

private static void PruneFloodDrops()
	{
		float now = Time.realtimeSinceStartup;
		ScratchClientSnapshotIds.Clear();
		foreach (KeyValuePair<int, float> pair in FloodDropClientUntil)
		{
			if (now > pair.Value)
			{
				ScratchClientSnapshotIds.Add(pair.Key);
			}
		}

		for (int i = 0; i < ScratchClientSnapshotIds.Count; i++)
		{
			FloodDropClientUntil.Remove(ScratchClientSnapshotIds[i]);
		}

		ScratchClientSnapshotIds.Clear();
		ScratchConnectionKeys.Clear();
		foreach (KeyValuePair<string, float> pair in FloodDropConnectionUntil)
		{
			if (string.IsNullOrWhiteSpace(pair.Key) || now > pair.Value)
			{
				ScratchConnectionKeys.Add(pair.Key);
			}
		}

		for (int i = 0; i < ScratchConnectionKeys.Count; i++)
		{
			string key = ScratchConnectionKeys[i];
			if (string.IsNullOrWhiteSpace(key))
			{
				continue;
			}

			FloodDropConnectionUntil.Remove(key);
			FloodDropClientByConnectionKey.Remove(key);
		}

		ScratchConnectionKeys.Clear();
		foreach (KeyValuePair<string, float> pair in HostRateDropByKey)
		{
			if (string.IsNullOrWhiteSpace(pair.Key) || now > pair.Value)
				ScratchConnectionKeys.Add(pair.Key);
		}
		for (int i = 0; i < ScratchConnectionKeys.Count; i++)
			HostRateDropByKey.Remove(ScratchConnectionKeys[i]);

		ScratchConnectionKeys.Clear();
		foreach (KeyValuePair<string, FloodAttributionState> pair in FloodAttributionByKey)
		{
			if (string.IsNullOrWhiteSpace(pair.Key)) { ScratchConnectionKeys.Add(pair.Key); continue; }
			FloodAttributionState s = pair.Value;
			bool windowExpired = now - s.WindowStart > FloodAttributionWindowSeconds;
			bool suppressionExpired = s.SuppressUntil <= 0f || now > s.SuppressUntil;
			if (windowExpired && suppressionExpired) ScratchConnectionKeys.Add(pair.Key);
		}
		for (int i = 0; i < ScratchConnectionKeys.Count; i++)
		{
			FloodAttributionByKey.Remove(ScratchConnectionKeys[i]);
		}
		ScratchConnectionKeys.Clear();
	}

private static bool SpawnHasDuplicateNetId(MessageReader part)
	{
		if (part == null || AmongUsClient.Instance == null) return false;
		MessageReader copy = null;
		try
		{
			InnerNetClient inner = (InnerNetClient)AmongUsClient.Instance;
			copy = MessageReader.Get(part);
			copy.ReadPackedUInt32();
			copy.ReadPackedInt32();
			copy.ReadByte();
			int compCount = copy.ReadPackedInt32();
			if (compCount <= 0) return false;
			int scan = compCount > 32 ? 32 : compCount;
			for (int i = 0; i < scan; i++)
			{
				if (copy.Position >= copy.Length) break;
				uint netId = copy.ReadPackedUInt32();
				if (netId != 0 &&
					inner.allObjects.AllObjectsFast.ContainsKey(netId) &&
					!inner.DestroyedObjects.Contains(netId))
				{
					return true;
				}

				MessageReader init = copy.ReadMessage();
				if (init == null) break;
				init.Recycle();
			}

			return false;
		}
		catch
		{
			return false;
		}
		finally { copy?.Recycle(); }
	}

private static bool ShouldDropDataObject(uint netId, int payloadBytes)
	{
		if (payloadBytes >= DataSinglePayloadMax)
		{
			return true;
		}

		float now = Time.realtimeSinceStartup;
		if (!DataByNetId.TryGetValue(netId, out DataBurst b))
		{
			b = new DataBurst();
			DataByNetId[netId] = b;
		}

		while (b.Ts.Count > 0 && now - b.Ts.Peek() > 1f)
		{
			b.Ts.Dequeue();
			if (b.By.Count > 0) b.Total -= b.By.Dequeue();
		}

		b.Ts.Enqueue(now);
		b.By.Enqueue(payloadBytes);
		b.Total += payloadBytes;

		if (now >= nextDataFloodCleanupAt)
		{
			nextDataFloodCleanupAt = now + 5f;
			CleanupDataBursts(now);
		}

		return b.Ts.Count > DataFloodCountPerSec || b.Total > DataFloodBytesPerSec;
	}

private static void CleanupDataBursts(float now)
	{
		if (DataByNetId.Count == 0) return;
		List<uint> dead = null;
		foreach (KeyValuePair<uint, DataBurst> kv in DataByNetId)
		{
			DataBurst b = kv.Value;
			while (b.Ts.Count > 0 && now - b.Ts.Peek() > 1f)
			{
				b.Ts.Dequeue();
				if (b.By.Count > 0) b.Total -= b.By.Dequeue();
			}
			if (b.Ts.Count == 0)
			{
				(dead ??= new List<uint>()).Add(kv.Key);
			}
		}
		if (dead != null)
		{
			for (int i = 0; i < dead.Count; i++) DataByNetId.Remove(dead[i]);
		}
	}

private static bool ShouldDropNonHostHighVolumeRpc(uint netId, byte callId)
	{
		if (!HighVolumeRpcLimits.TryGetValue(callId, out (int Limit, float Window) rule))
		{
			return false;
		}

		float now = Time.realtimeSinceStartup;
		long key = ((long)netId << 8) | callId;
		if (!NonHostRpcByKey.TryGetValue(key, out Queue<float> q))
		{
			q = new Queue<float>();
			NonHostRpcByKey[key] = q;
		}

		while (q.Count > 0 && now - q.Peek() > rule.Window)
		{
			q.Dequeue();
		}

		q.Enqueue(now);

		if (now >= nextNonHostRpcCleanupAt)
		{
			nextNonHostRpcCleanupAt = now + 5f;
			CleanupNonHostRpc(now);
		}

		return q.Count > rule.Limit;
	}

private static bool SpawnShouldDrop(MessageReader part)
	{
		if (ModOptions.SpawnFloodGuard != null && !ModOptions.SpawnFloodGuard.Value)
		{
			return false;
		}

		try
		{
			if (AmongUsClient.Instance == null)
			{
				return false;
			}

			int sceneId = ((InnerNetClient)AmongUsClient.Instance).GameId;
			if (sceneId == 0)
			{
				return false;
			}

			if (sceneId != spawnSceneId)
			{
				spawnSceneId = sceneId;
				spawnsThisScene = 0;
				unownedSpawnsThisScene = 0;
			}

			bool fabricatedOwner = false;
			if (part != null)
			{
				MessageReader copy = null;
				try
				{
					InnerNetClient inner = (InnerNetClient)AmongUsClient.Instance;
					copy = MessageReader.Get(part);
					copy.ReadPackedUInt32();
					int ownerId = copy.ReadPackedInt32();
					fabricatedOwner = ownerId != -2 && ownerId != inner.ClientId && !IsKnownRemoteClient(ownerId);
				}
				catch
				{
				}
				finally { copy?.Recycle(); }
			}

			if (fabricatedOwner)
			{
				unownedSpawnsThisScene++;
				return unownedSpawnsThisScene > MaxUnownedSpawnsPerScene;
			}

			spawnsThisScene++;
			return spawnsThisScene > MaxSpawnsPerScene;
		}
		catch
		{
			return false;
		}
	}
}
}
