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

private static void ForgetClientSnapshot(int clientId)
	{
		if (!ClientSnapshotsById.TryGetValue(clientId, out ClientSnapshot snapshot))
		{
			return;
		}

		ClientSnapshotsById.Remove(clientId);
		RecentJoinSenderCandidates.Remove(clientId);
		if (snapshot.PlayerId < 100 && ClientIdByPlayerId.TryGetValue(snapshot.PlayerId, out int mappedClientId) && mappedClientId == clientId)
		{
			ClientIdByPlayerId.Remove(snapshot.PlayerId);
		}

		if (snapshot.OwnerId >= 0 && ClientIdByOwnerId.TryGetValue(snapshot.OwnerId, out mappedClientId) && mappedClientId == clientId)
		{
			ClientIdByOwnerId.Remove(snapshot.OwnerId);
		}
	}

private static bool TryGetClientSnapshot(int clientId, out ClientSnapshot snapshot)
	{
		snapshot = default;
		if (clientId < 0 || !ClientSnapshotsById.TryGetValue(clientId, out snapshot))
		{
			return false;
		}

		if (Time.realtimeSinceStartup - snapshot.LastSeenAt > ClientSnapshotTtlSeconds)
		{
			ForgetClientSnapshot(clientId);
			snapshot = default;
			return false;
		}

		return true;
	}

private static bool TryResolveCachedClientId(PlayerControl player, out int clientId)
	{
		clientId = -1;
		if (player == null)
		{
			return false;
		}

		byte playerId = player.PlayerId;
		if (playerId < 100 && ClientIdByPlayerId.TryGetValue(playerId, out clientId) && IsKnownRemoteClient(clientId))
		{
			return true;
		}

		try
		{
			int ownerId = ((InnerNetObject)player).OwnerId;
			if (ownerId >= 0 && ClientIdByOwnerId.TryGetValue(ownerId, out clientId) && IsKnownRemoteClient(clientId))
			{
				return true;
			}
		}
		catch
		{
		}

		clientId = -1;
		return false;
	}

private static bool TryGetSnapshotIdentity(int clientId, out AcovClientIdentity identity)
	{
		identity = default;
		if (!TryGetClientSnapshot(clientId, out ClientSnapshot snapshot))
		{
			return false;
		}

		identity = new AcovClientIdentity(clientId, snapshot.PlayerName, snapshot.FriendCode, snapshot.ProductUserId);
		return true;
	}

private static string SnapshotDisplayName(int clientId)
	{
		if (TryGetClientSnapshot(clientId, out ClientSnapshot snapshot) && !string.IsNullOrWhiteSpace(snapshot.PlayerName))
		{
			return StripRichText(snapshot.PlayerName);
		}

		return clientId >= 0 ? $"Client {clientId}" : "Unknown";
	}

private static string ReadClientName(ClientData client, string fallback)
	{
		string name = fallback ?? string.Empty;
		try
		{
			if (!string.IsNullOrWhiteSpace(client.PlayerName))
			{
				name = client.PlayerName;
			}
		}
		catch
		{
		}

		try
		{
			if (string.IsNullOrWhiteSpace(name) && client.Character != null && client.Character.Data != null)
			{
				name = client.Character.Data.PlayerName;
			}
		}
		catch
		{
		}

		return SafeString(name);
	}

private static string SafeString(string value)
	{
		return value ?? string.Empty;
	}

private static string TrimJoinLog(string value, int maxLength)
	{
		string text = SafeString(value).Replace("\r", " ").Replace("\n", " ").Trim();
		if (maxLength <= 0 || text.Length <= maxLength)
		{
			return text;
		}

		return text.Substring(0, maxLength) + "...";
	}

private static string StripRichText(string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return string.Empty;
		}

		string text = value;
		int guard = 0;
		while (guard++ < 24)
		{
			int start = text.IndexOf('<');
			if (start < 0)
			{
				break;
			}

			int end = text.IndexOf('>', start);
			if (end < start)
			{
				break;
			}

			text = text.Remove(start, end - start + 1);
		}

		return text;
	}

private static int GetResponsibleClientId(PlayerControl player)
	{
		int inboundSender = GetActiveInboundSenderClientId();
		if (IsKnownRemoteClient(inboundSender))
		{
			return inboundSender;
		}

		return -1;
	}

private static int GetPlayerClientId(PlayerControl player)
	{
		ClientData client = GetClient(player);
		if (client != null)
		{
			return client.Id;
		}

		try
		{
			int ownerId = ((InnerNetObject)player).OwnerId;
			if (ownerId >= 0)
			{
				return ownerId;
			}
		}
		catch
		{
		}

		return -1;
	}

private static int GetVerifiedPlayerClientId(PlayerControl player)
	{
		if (player == null || AmongUsClient.Instance == null)
		{
			return -1;
		}

		try
		{
			ClientData client = GetClient(player);
			if (client == null || client.Character == null || client.Character.PlayerId != player.PlayerId)
			{
				return -1;
			}

			InnerNetClient inner = (InnerNetClient)AmongUsClient.Instance;
			if (client.Id == inner.ClientId || client.Id == inner.HostId)
			{
				return -1;
			}

			return client.Id;
		}
		catch
		{
			return -1;
		}
	}

private static ClientData GetClient(PlayerControl player)
	{
		if (player == null || AmongUsClient.Instance == null)
		{
			return null;
		}

		try
		{
			ClientData direct = ((InnerNetClient)AmongUsClient.Instance).GetClientFromCharacter(player);
			if (direct != null)
			{
				RememberClient(direct);
				return direct;
			}
		}
		catch
		{
		}

		try
		{
			var clients = ((InnerNetClient)AmongUsClient.Instance).allClients.GetEnumerator();
			while (clients.MoveNext())
			{
				ClientData client = clients.Current;
				if (client != null && client.Character != null && client.Character.PlayerId == player.PlayerId)
				{
					RememberClient(client);
					return client;
				}
			}
		}
		catch
		{
		}

		try
		{
			Il2CppSystem.Collections.Generic.List<ClientData> clients = new Il2CppSystem.Collections.Generic.List<ClientData>();
			((InnerNetClient)AmongUsClient.Instance).GetAllClients(clients);
			var cursor = clients.GetEnumerator();
			while (cursor.MoveNext())
			{
				ClientData client = cursor.Current;
				if (client != null && client.Character != null && client.Character.PlayerId == player.PlayerId)
				{
					RememberClient(client);
					return client;
				}
			}
		}
		catch
		{
		}

		return null;
	}

private static ClientData GetClientById(int clientId)
	{
		if (clientId < 0 || AmongUsClient.Instance == null)
		{
			return null;
		}

		try
		{
			var clients = ((InnerNetClient)AmongUsClient.Instance).allClients.GetEnumerator();
			while (clients.MoveNext())
			{
				ClientData client = clients.Current;
				if (client != null && client.Id == clientId)
				{
					RememberClient(client);
					return client;
				}
			}
		}
		catch
		{
		}

		try
		{
			Il2CppSystem.Collections.Generic.List<ClientData> clients = new Il2CppSystem.Collections.Generic.List<ClientData>();
			((InnerNetClient)AmongUsClient.Instance).GetAllClients(clients);
			var cursor = clients.GetEnumerator();
			while (cursor.MoveNext())
			{
				ClientData client = cursor.Current;
				if (client != null && client.Id == clientId)
				{
					RememberClient(client);
					return client;
				}
			}
		}
		catch
		{
		}

		return null;
	}

private static ClientData GetRecentClient(InnerNetClient client, int clientId)
	{
		if (client == null || clientId < 0)
		{
			return null;
		}

		try
		{
			ClientData data = client.GetRecentClient(clientId);
			RememberClient(data);
			return data;
		}
		catch
		{
			return null;
		}
	}

private static int GetActiveInboundSenderClientId()
	{
		long ageMs = Environment.TickCount64 - activeInboundSenderSetAtMs;
		int frameAge = activeInboundSenderFrame < 0 ? int.MaxValue : Time.frameCount - activeInboundSenderFrame;
		if (activeInboundSenderClientId < 0 || ageMs > ActiveInboundSenderTtlMs || (frameAge > 2 && ageMs > 80))
		{
			ClearActiveInboundSender();
			return -1;
		}

		return activeInboundSenderClientId;
	}

private static int ResolveBestActiveClientId(int clientId)
	{
		InnerNetClient inner = AmongUsClient.Instance == null ? null : (InnerNetClient)AmongUsClient.Instance;
		if (IsRemoteClientIdValue(inner, clientId))
		{
			return clientId;
		}

		if (!string.IsNullOrWhiteSpace(activeInboundConnectionKey))
		{
			if (FloodDropClientByConnectionKey.TryGetValue(activeInboundConnectionKey, out int droppedClientId) && IsRemoteClientIdValue(inner, droppedClientId))
			{
				return droppedClientId;
			}

			if (ClientIdByConnectionKey.TryGetValue(activeInboundConnectionKey, out int mappedClientId) && IsRemoteClientIdValue(inner, mappedClientId))
			{
				return mappedClientId;
			}
		}

		if (IsRemoteClientIdValue(inner, activeInboundSenderClientId))
		{
			return activeInboundSenderClientId;
		}

		if (inner != null)
		{
			int singleClientId = ResolveSingleRemoteClientId(inner);
			if (singleClientId >= 0)
				return singleClientId;
		}

		return clientId;
	}

private static void SetActiveInboundSender(int clientId)
	{
		if (clientId < 0 || AmongUsClient.Instance == null)
		{
			return;
		}

		try
		{
			InnerNetClient inner = (InnerNetClient)AmongUsClient.Instance;
			if (clientId == inner.ClientId || clientId == inner.HostId)
			{
				return;
			}
		}
		catch
		{
		}

		activeInboundSenderClientId = clientId;
		activeInboundSenderSetAtMs = Environment.TickCount64;
		activeInboundSenderFrame = Time.frameCount;
	}

private static void ClearActiveInboundSender()
	{
		activeInboundSenderClientId = -1;
		activeInboundConnectionKey = string.Empty;
		activeInboundSenderSetAtMs = 0;
		activeInboundSenderFrame = -1;
	}

private static void RememberRecentJoinCandidate(int clientId)
	{
		if (clientId < 0 || AmongUsClient.Instance == null)
		{
			return;
		}

		try
		{
			InnerNetClient inner = (InnerNetClient)AmongUsClient.Instance;
			if (clientId == inner.ClientId || clientId == inner.HostId)
			{
				return;
			}
		}
		catch
		{
		}

		RecentJoinSenderCandidates[clientId] = Time.realtimeSinceStartup;
		PruneRecentJoinSenderCandidates();
	}

private static int ResolveSenderClientIdFromRecentJoin(InnerNetClient client)
	{
		PruneRecentJoinSenderCandidates();
		if (client == null || RecentJoinSenderCandidates.Count == 0)
		{
			return -1;
		}

		int found = -1;
		int count = 0;
		float now = Time.realtimeSinceStartup;
		foreach (KeyValuePair<int, float> pair in RecentJoinSenderCandidates)
		{
			if (now - pair.Value > RecentJoinSenderFallbackSeconds)
			{
				continue;
			}

			if (pair.Key == client.ClientId || pair.Key == client.HostId)
			{
				continue;
			}

			found = pair.Key;
			count++;
			if (count > 1)
			{
				return -1;
			}
		}

		if (count != 1)
		{
			return -1;
		}

		if (CountRemoteClients() != 1)
		{
			return -1;
		}

		return found;
	}

private static int TryResolveRecentSingleFloodCandidate()
	{
		if (AmongUsClient.Instance == null) return -1;
		InnerNetClient inner = (InnerNetClient)AmongUsClient.Instance;
		PruneRecentJoinSenderCandidates();
		if (RecentJoinSenderCandidates.Count == 0) return -1;
		const float recentJoinWindow = 3f;
		float now = Time.realtimeSinceStartup;
		int found = -1;
		int count = 0;
		foreach (KeyValuePair<int, float> pair in RecentJoinSenderCandidates)
		{
			if (now - pair.Value > recentJoinWindow) continue;
			if (pair.Key == inner.ClientId || pair.Key == inner.HostId) continue;
			if (!IsKnownRemoteClient(pair.Key)) continue;
			found = pair.Key;
			count++;
			if (count > 1) return -1;
		}
		return count == 1 ? found : -1;
	}

private static void PruneRecentJoinSenderCandidates()
	{
		if (RecentJoinSenderCandidates.Count == 0)
		{
			return;
		}

		float now = Time.realtimeSinceStartup;
		ScratchClientSnapshotIds.Clear();
		foreach (KeyValuePair<int, float> pair in RecentJoinSenderCandidates)
		{
			if (now - pair.Value > RecentJoinSenderFallbackSeconds)
			{
				ScratchClientSnapshotIds.Add(pair.Key);
			}
		}

		for (int i = 0; i < ScratchClientSnapshotIds.Count; i++)
		{
			RecentJoinSenderCandidates.Remove(ScratchClientSnapshotIds[i]);
		}

		ScratchClientSnapshotIds.Clear();
	}

private static void RememberConnectionClient(string connectionKey, int clientId)
	{
		if (string.IsNullOrWhiteSpace(connectionKey) || clientId < 0 || AmongUsClient.Instance == null)
		{
			return;
		}

		if (AmbiguousConnectionKeys.Contains(connectionKey))
		{
			ConnectionKeySeenAt[connectionKey] = Time.realtimeSinceStartup;
			return;
		}

		try
		{
			InnerNetClient inner = (InnerNetClient)AmongUsClient.Instance;
			if (clientId == inner.ClientId || clientId == inner.HostId)
			{
				return;
			}
		}
		catch
		{
		}

		if (ClientIdByConnectionKey.TryGetValue(connectionKey, out int existingClientId) && existingClientId != clientId)
		{
			ClientIdByConnectionKey.Remove(connectionKey);
			AmbiguousConnectionKeys.Add(connectionKey);
			ConnectionKeySeenAt[connectionKey] = Time.realtimeSinceStartup;
			AcovPlugin.Logger?.LogInfo((object)$"Network protection marked connection key as ambiguous: {TrimConnectionKeyForLog(connectionKey)} ({existingClientId} -> {clientId}).");
			return;
		}

		ClientIdByConnectionKey[connectionKey] = clientId;
		ConnectionKeySeenAt[connectionKey] = Time.realtimeSinceStartup;
		PruneConnectionKeys();
	}

private static int ResolveConnectionClientId(string connectionKey, InnerNetClient client)
	{
		if (string.IsNullOrWhiteSpace(connectionKey) || AmbiguousConnectionKeys.Contains(connectionKey) || !ClientIdByConnectionKey.TryGetValue(connectionKey, out int clientId))
		{
			return -1;
		}

		if (!ConnectionKeySeenAt.TryGetValue(connectionKey, out float seenAt) || Time.realtimeSinceStartup - seenAt > ClientSnapshotTtlSeconds)
		{
			ClientIdByConnectionKey.Remove(connectionKey);
			ConnectionKeySeenAt.Remove(connectionKey);
			return -1;
		}

		return IsRemoteClientIdValue(client, clientId) ? clientId : -1;
	}

private static void PruneConnectionKeys()
	{
		if (ConnectionKeySeenAt.Count == 0)
		{
			return;
		}

		float now = Time.realtimeSinceStartup;
		List<string> expired = null;
		foreach (KeyValuePair<string, float> pair in ConnectionKeySeenAt)
		{
			if (!string.IsNullOrWhiteSpace(pair.Key) && now - pair.Value <= ClientSnapshotTtlSeconds)
			{
				continue;
			}

			if (expired == null)
			{
				expired = new List<string>();
			}

			expired.Add(pair.Key);
		}

		if (expired == null)
		{
			return;
		}

		for (int i = 0; i < expired.Count; i++)
		{
			string key = expired[i];
			if (string.IsNullOrWhiteSpace(key))
			{
				continue;
			}

			ClientIdByConnectionKey.Remove(key);
			ConnectionKeySeenAt.Remove(key);
			AmbiguousConnectionKeys.Remove(key);
		}
	}

private static string TrimConnectionKeyForLog(string connectionKey)
	{
		if (string.IsNullOrWhiteSpace(connectionKey))
		{
			return "-";
		}

		string clean = connectionKey.Trim();
		if (clean.Length <= 96)
		{
			return clean;
		}

		return clean.Substring(0, 96) + "...";
	}

private static string PacketConnectionKey(DataReceivedEventArgs eventArgs)
	{
		if (eventArgs == null)
		{
			return null;
		}

		object connection = TryReadMember(eventArgs, "Connection", "connection", "Sender", "sender", "Peer", "peer", "Client", "client", "Conn", "conn", "Remote", "remote", "Socket", "socket");
		return StableObjectKey(connection) ?? StableObjectKey(eventArgs);
	}

private static string StableObjectKey(object source)
	{
		if (source == null)
		{
			return null;
		}

		try
		{
			string endpoint = ReadFirstNonEmptyString(source, "EndPoint", "endpoint", "RemoteEndPoint", "remoteEndPoint", "Address", "address", "Host", "host", "Ip", "IP", "ip");
			if (!string.IsNullOrWhiteSpace(endpoint))
			{
				return source.GetType().FullName + ":" + endpoint.Trim();
			}
		}
		catch
		{
		}

		try
		{
			return source.GetType().FullName + "#" + RuntimeHelpers.GetHashCode(source).ToString();
		}
		catch
		{
			return null;
		}
	}

private static string ReadFirstNonEmptyString(object source, params string[] memberNames)
	{
		if (source == null || memberNames == null)
		{
			return null;
		}

		for (int i = 0; i < memberNames.Length; i++)
		{
			object value = TryReadMember(source, memberNames[i]);
			if (value == null)
			{
				continue;
			}

			try
			{
				string text = value.ToString();
				if (!string.IsNullOrWhiteSpace(text))
				{
					return text;
				}
			}
			catch
			{
			}
		}

		return null;
	}

private static int ResolvePacketSenderClientId(InnerNetClient client, DataReceivedEventArgs eventArgs)
	{
		int clientId = ResolveConnectionClientId(activeInboundConnectionKey, client);
		if (clientId >= 0)
		{
			return clientId;
		}

		clientId = TryReadClientId(eventArgs);
		if (IsKnownRemoteClient(client, clientId))
		{
			RememberConnectionClient(activeInboundConnectionKey, clientId);
			return clientId;
		}

		object connection = TryReadMember(eventArgs, "Connection", "connection", "Sender", "sender", "Peer", "peer", "Client", "client", "Conn", "conn", "Remote", "remote", "Socket", "socket");
		if (TryResolveClientIdFromObject(client, connection, out clientId))
		{
			RememberConnectionClient(activeInboundConnectionKey, clientId);
			return clientId;
		}

		object nestedConnection = TryReadMember(connection, "Connection", "connection", "Sender", "sender", "Client", "client", "Peer", "peer", "Owner", "owner", "Conn", "conn", "Remote", "remote", "Socket", "socket");
		if (TryResolveClientIdFromObject(client, nestedConnection, out clientId))
		{
			RememberConnectionClient(activeInboundConnectionKey, clientId);
			return clientId;
		}

		if (TryResolveClientIdFromObject(client, eventArgs, out clientId))
		{
			RememberConnectionClient(activeInboundConnectionKey, clientId);
			return clientId;
		}

		if (TryResolveClientIdFromMemberValues(
			client,
			eventArgs,
			out clientId,
			"Connection", "connection", "Sender", "sender", "Peer", "peer", "Client", "client", "Owner", "owner",
			"Data", "data", "Packet", "packet", "Remote", "remote", "Socket", "socket", "Conn", "conn"))
		{
			RememberConnectionClient(activeInboundConnectionKey, clientId);
			return clientId;
		}

		return ResolveSenderClientIdFromRecentJoin(client);
	}

private static bool TryResolveClientIdFromObject(InnerNetClient client, object source, out int clientId)
	{
		clientId = TryReadClientId(source);
		if (IsKnownRemoteClient(client, clientId))
		{
			return true;
		}

		return TryResolveClientIdFromLikelyMembers(client, source, out clientId);
	}

private static bool TryResolveClientIdFromMemberValues(InnerNetClient client, object source, out int clientId, params string[] memberNames)
	{
		clientId = -1;
		if (source == null || memberNames == null)
		{
			return false;
		}

		for (int i = 0; i < memberNames.Length; i++)
		{
			object member = TryReadMember(source, memberNames[i]);
			if (member == null)
			{
				continue;
			}

			if (TryResolveClientIdFromObject(client, member, out clientId))
			{
				return true;
			}

			object nested = TryReadMember(member, "Connection", "connection", "Sender", "sender", "Client", "client", "Peer", "peer", "Owner", "owner", "Conn", "conn", "Remote", "remote", "Socket", "socket");
			if (TryResolveClientIdFromObject(client, nested, out clientId))
			{
				return true;
			}
		}

		return false;
	}

private sealed class TypeReflectionPlan
	{
		internal readonly MemberAccess[] IdCarriers;
		internal readonly Dictionary<string, MemberAccess> ByName;

		internal TypeReflectionPlan(MemberAccess[] idCarriers, Dictionary<string, MemberAccess> byName)
		{
			IdCarriers = idCarriers;
			ByName = byName;
		}
	}

private readonly struct MemberAccess
	{
		private readonly PropertyInfo property;
		private readonly FieldInfo field;

		internal MemberAccess(PropertyInfo property)
		{
			this.property = property;
			field = null;
		}

		internal MemberAccess(FieldInfo field)
		{
			property = null;
			this.field = field;
		}

		internal object Read(object target)
		{
			try
			{
				return property != null ? property.GetValue(target, null) : field?.GetValue(target);
			}
			catch
			{
				return null;
			}
		}
	}

private static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, TypeReflectionPlan> reflectionPlans =
		new System.Collections.Concurrent.ConcurrentDictionary<Type, TypeReflectionPlan>();

private static readonly string[] ClientIdMemberNames =
	{
		"ClientId", "clientId", "SenderId", "senderId", "OwnerId", "ownerId",
		"PlayerId", "playerId", "Id", "id",
		"ConnectionId", "connectionId", "ConnId", "connId",
		"RemoteId", "remoteId", "SourceId", "sourceId", "PeerId", "peerId"
	};

private static readonly string[] ConnectionTokens = { "client", "sender", "owner", "peer", "conn" };

private static TypeReflectionPlan PlanFor(object source)
	{
		if (source == null)
		{
			return null;
		}

		Type type;
		try
		{
			type = source.GetType();
		}
		catch
		{
			return null;
		}

		return reflectionPlans.GetOrAdd(type, BuildReflectionPlan);
	}

private static TypeReflectionPlan BuildReflectionPlan(Type type)
	{
		Dictionary<string, MemberAccess> byName = new Dictionary<string, MemberAccess>(StringComparer.Ordinal);
		List<MemberAccess> idCarriers = new List<MemberAccess>();
		const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

		try
		{
			for (Type level = type; level != null && level != typeof(object); level = level.BaseType)
			{
				PropertyInfo[] properties;
				try { properties = level.GetProperties(flags); }
				catch { properties = Array.Empty<PropertyInfo>(); }

				foreach (PropertyInfo property in properties)
				{
					if (property == null || property.GetIndexParameters().Length != 0)
					{
						continue;
					}

					MemberAccess access = new MemberAccess(property);
					if (!byName.ContainsKey(property.Name))
					{
						byName[property.Name] = access;
					}

					if (LooksLikeClientIdMember(property.Name))
					{
						idCarriers.Add(access);
					}
				}

				FieldInfo[] fields;
				try { fields = level.GetFields(flags); }
				catch { fields = Array.Empty<FieldInfo>(); }

				foreach (FieldInfo field in fields)
				{
					if (field == null)
					{
						continue;
					}

					MemberAccess access = new MemberAccess(field);
					if (!byName.ContainsKey(field.Name))
					{
						byName[field.Name] = access;
					}

					if (LooksLikeClientIdMember(field.Name))
					{
						idCarriers.Add(access);
					}
				}
			}
		}
		catch
		{
		}

		return new TypeReflectionPlan(idCarriers.ToArray(), byName);
	}

private static bool TryResolveClientIdFromLikelyMembers(InnerNetClient client, object source, out int clientId)
	{
		clientId = -1;
		if (client == null)
		{
			return false;
		}

		TypeReflectionPlan plan = PlanFor(source);
		if (plan == null)
		{
			return false;
		}

		MemberAccess[] carriers = plan.IdCarriers;
		for (int i = 0; i < carriers.Length; i++)
		{
			int candidate = TryReadClientId(carriers[i].Read(source));
			if (IsKnownRemoteClient(client, candidate))
			{
				clientId = candidate;
				return true;
			}
		}

		return false;
	}

private static bool LooksLikeClientIdMember(string memberName)
	{
		if (string.IsNullOrWhiteSpace(memberName))
		{
			return false;
		}

		string lower = memberName.Trim().ToLowerInvariant();
		if (lower.Length == 0)
		{
			return false;
		}

		if (lower.EndsWith("id", StringComparison.Ordinal))
		{
			return true;
		}

		for (int i = 0; i < ConnectionTokens.Length; i++)
		{
			if (lower.IndexOf(ConnectionTokens[i], StringComparison.Ordinal) >= 0)
			{
				return true;
			}
		}

		return false;
	}

private static int ResolveSingleRemoteClientId(InnerNetClient client)
	{
		if (client == null || AmongUsClient.Instance == null)
		{
			return -1;
		}

		try
		{
			int found = -1;
			int count = 0;
			var clients = ((InnerNetClient)AmongUsClient.Instance).allClients.GetEnumerator();
			while (clients.MoveNext())
			{
				ClientData data = clients.Current;
				if (data == null || data.Id < 0 || data.Id == client.ClientId || data.Id == client.HostId)
				{
					continue;
				}

				found = data.Id;
				count++;
				if (count > 1)
				{
					return -1;
				}
			}

			return count == 1 ? found : -1;
		}
		catch
		{
			return -1;
		}
	}

private static int CountRemoteClients()
	{
		if (AmongUsClient.Instance == null)
		{
			return 0;
		}

		try
		{
			InnerNetClient inner = (InnerNetClient)AmongUsClient.Instance;
			int count = 0;
			var clients = inner.allClients.GetEnumerator();
			while (clients.MoveNext())
			{
				ClientData data = clients.Current;
				if (data != null && data.Id >= 0 && data.Id != inner.ClientId && data.Id != inner.HostId)
				{
					count++;
				}
			}

			return count;
		}
		catch
		{
			return 0;
		}
	}

private static bool IsKnownRemoteClient(int clientId)
	{
		if (AmongUsClient.Instance == null)
		{
			return false;
		}

		return IsKnownRemoteClient((InnerNetClient)AmongUsClient.Instance, clientId);
	}

private static bool IsKnownRemoteClient(InnerNetClient client, int clientId)
	{
		if (client == null || clientId < 0 || clientId == client.ClientId || clientId == client.HostId)
		{
			return false;
		}

		return GetClientById(clientId) != null
			|| GetRecentClient(client, clientId) != null
			|| TryGetClientSnapshot(clientId, out _);
	}

private static bool IsRemoteClientIdValue(InnerNetClient client, int clientId)
	{
		if (clientId < 0)
		{
			return false;
		}

		if (client == null)
		{
			return AmongUsClient.Instance == null || IsRemoteClientIdValue((InnerNetClient)AmongUsClient.Instance, clientId);
		}

		return clientId != client.ClientId && clientId != client.HostId;
	}

private static int TryReadClientId(object source)
	{
		if (source == null)
		{
			return -1;
		}

		if (TryConvertToInt(source, out int direct))
		{
			return direct;
		}

		object member = TryReadMember(source, ClientIdMemberNames);
		return TryConvertToInt(member, out int nested) ? nested : -1;
	}

private static bool TryConvertToInt(object value, out int result)
	{
		result = -1;
		if (value == null)
		{
			return false;
		}

		switch (value)
		{
			case int i: result = i; return true;
			case uint u: result = unchecked((int)u); return true;
			case byte b: result = b; return true;
			case sbyte sb: result = sb; return true;
			case short s: result = s; return true;
			case ushort us: result = us; return true;
			case long l: result = unchecked((int)l); return true;
			case ulong ul: result = unchecked((int)ul); return true;
			case Enum e:
				try { result = Convert.ToInt32(e); return true; }
				catch { return false; }
		}

		try
		{
			string text = value.ToString();
			return !string.IsNullOrWhiteSpace(text) && int.TryParse(text, out result);
		}
		catch
		{
			result = -1;
			return false;
		}
	}

private static object TryReadMember(object source, params string[] memberNames)
	{
		if (memberNames == null || memberNames.Length == 0)
		{
			return null;
		}

		TypeReflectionPlan plan = PlanFor(source);
		if (plan == null)
		{
			return null;
		}

		for (int i = 0; i < memberNames.Length; i++)
		{
			string name = memberNames[i];
			if (name != null && plan.ByName.TryGetValue(name, out MemberAccess access))
			{
				object value = access.Read(source);
				if (value != null)
				{
					return value;
				}
			}
		}

		return null;
	}

private static string PlayerName(PlayerControl player)
	{
		try
		{
			string name = player?.Data?.PlayerName;
			if (!string.IsNullOrWhiteSpace(name))
			{
				return StripTags(name);
			}
		}
		catch
		{
		}

		return player == null ? "Unknown" : $"Player {player.PlayerId}";
	}

private static string ClientName(int clientId)
	{
		try
		{
			if (AmongUsClient.Instance != null)
			{
				InnerNetClient client = (InnerNetClient)AmongUsClient.Instance;
				ClientData data = GetClientById(clientId) ?? GetRecentClient(client, clientId);
				if (data != null)
				{
					return AcovAccessLists.ClientDisplayName(data, clientId);
				}
			}
		}
		catch
		{
		}

		return SnapshotDisplayName(clientId);
	}

private static string RpcName(byte rpcByte)
	{
		try
		{
			if (Enum.IsDefined(typeof(RpcCalls), rpcByte))
			{
				return ((RpcCalls)rpcByte).ToString();
			}
		}
		catch
		{
		}

		return $"RPC {rpcByte}";
	}

private static bool IsDangerousChat(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}

		if (text.Length > 200)
		{
			return true;
		}

		string lower = text.ToLowerInvariant();
		if (lower.Contains("<size") || lower.Contains("voffset") || lower.Contains("<mark") || lower.Contains("<material") || lower.Contains("<quad"))
		{
			return true;
		}

		int spriteCount = 0;
		int index = 0;
		while ((index = lower.IndexOf("<sprite", index, StringComparison.Ordinal)) >= 0)
		{
			spriteCount++;
			if (spriteCount > 3)
			{
				return true;
			}

			index += 7;
		}

		return false;
	}

private static string StripTags(string value)
	{
		if (string.IsNullOrEmpty(value) || value.IndexOf('<') < 0)
		{
			return value ?? string.Empty;
		}

		char[] buffer = new char[value.Length];
		int count = 0;
		bool insideTag = false;
		for (int i = 0; i < value.Length; i++)
		{
			char ch = value[i];
			if (ch == '<')
			{
				insideTag = true;
				continue;
			}

			if (ch == '>')
			{
				insideTag = false;
				continue;
			}

			if (!insideTag)
			{
				buffer[count++] = ch;
			}
		}

		return new string(buffer, 0, count);
	}

private static void Notice(string title, string detail)
	{
		float now = Time.realtimeSinceStartup;
		if (now - lastScreenNoticeAt < 0.75f)
		{
			return;
		}

		lastScreenNoticeAt = now;
		AcovNotifications.Show(title, detail, 3.2f);
	}

internal static int ResolvePlayerClientId(PlayerControl player)
	{
		return GetPlayerClientId(player);
	}

internal static void FlagLobbyTeleport(PlayerControl player, int clientId, float dist)
	{
		string action = ModOptions.LobbyTeleportAction == null
			? "Warn"
			: ModOptions.NormalizeNetworkProtectionAction(ModOptions.LobbyTeleportAction.Value);
		string name = PlayerName(player);
		string title = AcovText.T("Телепорт в лобби", "Lobby teleport");
		string detail = AcovText.T($"Снап на {dist:F1} ед.", $"Snapped {dist:F1} units.");
		AcovPlugin.Logger?.LogWarning((object)$"Lobby teleport: {name} (client {clientId}) snapped {dist:F1} units.");
		bool notify = ShouldShowProtectionNotice(clientId, title, detail);
		if (notify && action != "Null")
		{
			AcovSecurityNotifications.Show(action, name, title, detail, clientId);
		}
		ApplyProtectionAction(clientId, title, detail, action);
	}
}
}
