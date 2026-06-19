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

private static bool IsLobbyJoinSyncGrace(PlayerControl player = null, int clientId = -1)
	{
		UpdateLobbySyncState();
		if (LobbyBehaviour.Instance == null)
		{
			return false;
		}

		float now = Time.realtimeSinceStartup;
		if (now - lastLobbyEnteredAt < LobbyEnteredSyncGraceSeconds)
		{
			return true;
		}

		if (now - lastLobbyJoinAt < LobbyRecentJoinSyncGraceSeconds)
		{
			return true;
		}

		if (clientId >= 0 && ClientJoinTimeAt.TryGetValue(clientId, out float joinedAt) && now - joinedAt < LobbyJoinWarmupSeconds)
		{
			return true;
		}

		ClientData client = clientId >= 0 ? GetClientById(clientId) : GetClient(player);
		if (client == null || ClientIdentityReady(client))
		{
			return false;
		}

		if (ClientJoinTimeAt.TryGetValue(client.Id, out joinedAt))
		{
			return now - joinedAt < LobbyJoinWarmupSeconds;
		}

		return now - lastLobbyJoinAt < LobbyJoinWarmupSeconds;
	}

	private static void UpdateLobbySyncState()
	{
		InnerNetClient client = (InnerNetClient)AmongUsClient.Instance;
		if (client == null)
		{
			lastObservedLobbyGameState = -1;
			lastLobbyEnteredAt = -1000f;
			return;
		}

		int gameState = (int)client.GameState;
		if (gameState == lastObservedLobbyGameState)
		{
			return;
		}

		lastObservedLobbyGameState = gameState;
		if (gameState == 1)
		{
			lastLobbyEnteredAt = Time.realtimeSinceStartup;
		}
	}

	private static bool ShouldBlockPlayerSemanticRpc(PlayerControl player, byte rpcByte, int clientId, MessageReader reader)
	{
		if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost || player == null)
		{
			return false;
		}

		if (NameStringRpcIds.Contains(rpcByte))
		{
			try
			{
				int size = reader != null ? reader.Length : 0;
				if (size > MaxRpcStringBytes)
				{
					BlockRpc(player, clientId, "Oversized name RPC", $"{size} bytes (limit {MaxRpcStringBytes}).", "Null");
					return true;
				}
			}
			catch
			{
			}
		}

		if (rpcByte == 38 && TryReadLevel(reader, out uint level))
		{
			if (LevelRpcProtectionEnabled() && IsClearlyInvalidLevel(level))
			{
				BlockRpc(player, clientId, "Invalid level RPC", $"Level {level}, max {MaxAllowedLevelRpc()}.", LevelRpcProtectionAction());
				return true;
			}

			AcovPlayerLevels.Remember(player, level);
			MinLevelKickGuard.Check(player, level);
		}

		if (rpcByte == 1)
		{
			float now = Time.fixedTime;
			if (LastTaskRpcAt.TryGetValue(player.PlayerId, out float lastTask) && now - lastTask < 0.7f)
			{
				BlockRpc(player, clientId, "Task spam", $"{now - lastTask:0.00}s between tasks.");
				return true;
			}

			if (MeetingOrExileActive())
			{
				BlockRpc(player, clientId, "Task during meeting", "CompleteTask was blocked.");
				return true;
			}

			if (IsImpostor(player))
			{
				BlockRpc(player, clientId, "Impostor task RPC", "CompleteTask was blocked.");
				return true;
			}

			LastTaskRpcAt[player.PlayerId] = now;
			return false;
		}

		if (rpcByte == 11)
		{
			if (MeetingOrExileActive())
			{
				AcovPlugin.Logger?.LogWarning((object)$"Network protection ignored duplicate meeting/report RPC from {PlayerName(player)} (client {clientId}) while meeting UI is active.");
				return false;
			}

			if (TryReadReportedPlayerId(reader, out byte reportedPlayerId))
			{
				if (reportedPlayerId == byte.MaxValue)
				{
					try
					{
						if (player.RemainingEmergencies <= 0)
						{
							BlockRpc(player, clientId, "Invalid emergency RPC", $"Remaining emergencies: {player.RemainingEmergencies}.");
							return true;
						}
					}
					catch
					{
					}
				}
				else if (!PlayerExists(reportedPlayerId))
				{
					BlockRpc(player, clientId, "Invalid body report", $"PlayerId {reportedPlayerId} does not exist.");
					return true;
				}
			}
		}

		if (rpcByte == 12 && ShouldBlockMurderRpc(player, clientId, reader))
		{
			return true;
		}

		if (rpcByte == 13)
		{
			return ShouldBlockChatRpc(player, clientId, reader);
		}

		if (CosmeticSpoofProtectionEnabled() && !SenderAttributionAmbiguous() && CosmeticMutationRpcIds.Contains(rpcByte))
		{
			int cosmeticOwnerClientId = GetPlayerClientId(player);
			if (clientId >= 0 && cosmeticOwnerClientId >= 0 && clientId != cosmeticOwnerClientId && !IsLobbyJoinSyncGrace(player, clientId))
			{
				BlockRpc(player, clientId, "Cosmetic spoofing", $"client {clientId} set {RpcName(rpcByte)} on {PlayerName(player)} (owner {cosmeticOwnerClientId}).", CosmeticSpoofProtectionAction());
				return true;
			}
		}

		if (CosmeticMutationRpcIds.Contains(rpcByte) && InActiveMatch() && !AcovFakeMapLobby.Active && !MeetingOrExileActive() && IntroCutscene.Instance == null)
		{
			BlockRpc(player, clientId, "Match cosmetic RPC", $"{RpcName(rpcByte)} while match is running.");
			return true;
		}

		if (rpcByte == 45 && ShouldBlockProtectRpc(player, clientId, reader))
		{
			return true;
		}

		if ((rpcByte == 46 || rpcByte == 55) && ShouldBlockShapeshiftRpc(player, rpcByte, clientId, reader))
		{
			return true;
		}

		if ((rpcByte == 63 || rpcByte == 65) && MeetingOrExileActive())
		{
			BlockRpc(player, clientId, "Meeting invisibility RPC", $"{RpcName(rpcByte)} was blocked.");
			return true;
		}

		return false;
	}

	private static bool ShouldBlockMurderRpc(PlayerControl player, int clientId, MessageReader reader)
	{
		if (MeetingOrExileActive())
		{
			BlockRpc(player, clientId, "Kill during meeting", "MurderPlayer was blocked.");
			return true;
		}

		if (!TryReadPlayerObject(reader, out PlayerControl target) || target == null)
		{
			return false;
		}

		if (SamePlayer(player, target))
		{
			BlockRpc(player, clientId, "Self kill RPC", "MurderPlayer target equals sender.");
			return true;
		}

		try
		{
			if (player.Data?.Role is PhantomRole phantom && phantom.IsInvisible)
			{
				BlockRpc(player, clientId, "Invisible kill RPC", "MurderPlayer was sent while phantom is invisible.");
				return true;
			}
		}
		catch
		{
		}

		if (IsImpostor(target))
		{
			BlockRpc(player, clientId, "Kill impostor RPC", $"{PlayerName(target)} is impostor.");
			return true;
		}

		return false;
	}

	private static bool ShouldBlockProtectRpc(PlayerControl player, int clientId, MessageReader reader)
	{
		if (MeetingOrExileActive())
		{
			BlockRpc(player, clientId, "Protect during meeting", "ProtectPlayer was blocked.");
			return true;
		}

		if (TryReadPlayerObject(reader, out PlayerControl target) && target != null && SamePlayer(player, target))
		{
			BlockRpc(player, clientId, "Self protect RPC", "ProtectPlayer target equals sender.");
			return true;
		}

		try
		{
			if (player.Data?.Role == null || (int)player.Data.Role.Role != 4)
			{
				BlockRpc(player, clientId, "Invalid protect RPC", "ProtectPlayer was sent by a non-guardian role.");
				return true;
			}
		}
		catch
		{
		}

		return false;
	}

	private static bool ShouldBlockShapeshiftRpc(PlayerControl player, byte rpcByte, int clientId, MessageReader reader)
	{
		if (!TryReadPlayerObject(reader, out PlayerControl target) || target == null)
		{
			return false;
		}

		bool animated = true;
		try
		{
			MessageReader copy = MessageReader.Get(reader);
			MessageExtensions.ReadNetObject<PlayerControl>(copy);
			animated = copy.ReadBoolean();
		}
		catch
		{
		}

		if (MeetingOrExileActive() && !SamePlayer(player, target))
		{
			BlockRpc(player, clientId, "Shapeshift during meeting", "Targeted shapeshift was blocked.");
			return true;
		}

		if (!animated && (rpcByte == 46 || !SamePlayer(player, target)))
		{
			BlockRpc(player, clientId, "Silent shapeshift", $"{PlayerName(target)} without animation.");
			return true;
		}

		return false;
	}

	private static bool ShouldBlockCrashRpc(PlayerControl player, byte rpcByte, int clientId)
	{
		if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
		{
			return false;
		}

		if (rpcByte == 4 && MeetingHud.Instance == null && !((InnerNetObject)player).AmOwner)
		{
			BlockRpc(player, clientId, "Exiled вне встречи", "RPC заблокирован.");
			return true;
		}

		return false;
	}

	private static bool ShouldBlockChatRpc(PlayerControl player, int clientId, MessageReader reader)
	{
		if (ChatSpoofProtectionEnabled())
		{
			int ownerClientId = GetPlayerClientId(player);
			if (clientId >= 0 && ownerClientId >= 0 && clientId != ownerClientId && !IsLobbyJoinSyncGrace(player, clientId))
			{
				BlockRpc(player, clientId, "Chat spoofing", $"client {clientId} sent chat as {PlayerName(player)} (owner {ownerClientId}).", ChatSpoofProtectionAction());
				return true;
			}
		}

		if (clientId >= 0 && HitLimit(ChatByClient, clientId, Time.realtimeSinceStartup, ChatWindowSeconds, MaxChatMessagesPerWindow, out int chatCount))
		{
			BlockRpc(player, clientId, "Chat flood", $"{chatCount}/{MaxChatMessagesPerWindow} за {ChatWindowSeconds:0.0}с.");
			return true;
		}

		try
		{
			MessageReader copy = MessageReader.Get(reader);
			string text = copy.ReadString();
			if (IsDangerousChat(text))
			{
				BlockRpc(player, clientId, "Опасный чат", "Форматирование сообщения заблокировано.");
				return true;
			}
		}
		catch (Exception error)
		{
			BlockRpc(player, clientId, "Поврежденный чат", error.Message);
			return true;
		}

		return false;
	}

	private static bool TryReadLevel(MessageReader reader, out uint level)
	{
		level = 0;
		if (reader == null)
		{
			return false;
		}

		try
		{
			MessageReader copy = MessageReader.Get(reader);
			level = copy.ReadPackedUInt32();
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static string suspiciousLevelsRaw;

private static HashSet<uint> suspiciousLevels;

private static bool IsPopularSpoofLevel(uint level)
	{
		string raw = ModOptions.SuspiciousLevelList?.Value ?? string.Empty;
		if (suspiciousLevels == null || !string.Equals(raw, suspiciousLevelsRaw, StringComparison.Ordinal))
		{
			suspiciousLevelsRaw = raw;
			suspiciousLevels = ParseSuspiciousLevels(raw);
		}

		return suspiciousLevels.Contains(level);
	}

private static HashSet<uint> ParseSuspiciousLevels(string raw)
	{
		HashSet<uint> set = new HashSet<uint>();
		if (string.IsNullOrWhiteSpace(raw))
		{
			return set;
		}

		string[] parts = raw.Split(',');
		for (int i = 0; i < parts.Length; i++)
		{
			if (uint.TryParse(parts[i].Trim(), out uint value))
			{
				set.Add(value);
			}
		}

		return set;
	}

private static bool IsClearlyInvalidLevel(uint level)
	{
		return level > MaxAllowedLevelRpc() || IsPopularSpoofLevel(level);
	}

private static uint MaxAllowedLevelRpc()
	{
		try
		{
			return (uint)Mathf.Clamp(ModOptions.MaxAllowedLevelRpc?.Value ?? 10000, 100, 10000);
		}
		catch
		{
			return 10000u;
		}
	}

private static bool TryReadReportedPlayerId(MessageReader reader, out byte playerId)
	{
		playerId = byte.MaxValue;
		if (reader == null)
		{
			return false;
		}

		try
		{
			MessageReader copy = MessageReader.Get(reader);
			playerId = copy.ReadByte();
			return true;
		}
		catch
		{
			return false;
		}
	}

private static bool TryReadPlayerObject(MessageReader reader, out PlayerControl player)
	{
		player = null;
		if (reader == null)
		{
			return false;
		}

		try
		{
			MessageReader copy = MessageReader.Get(reader);
			player = MessageExtensions.ReadNetObject<PlayerControl>(copy);
			return player != null;
		}
		catch
		{
			return false;
		}
	}

private static bool SamePlayer(PlayerControl left, PlayerControl right)
	{
		return left != null && right != null && left.PlayerId == right.PlayerId;
	}

private static bool MeetingOrExileActive()
	{
		try
		{
			if (MeetingHud.Instance != null && (int)MeetingHud.Instance.state != 0)
			{
				return true;
			}
		}
		catch
		{
			if (MeetingHud.Instance != null)
			{
				return true;
			}
		}

		return ExileController.Instance != null;
	}

private static void NoteMeetingRpc()
	{
		lastMeetingRpcAt = Time.realtimeSinceStartup;
	}

internal static bool IsMeetingTransitionGrace()
	{
		return MeetingOrExileActive() || Time.realtimeSinceStartup - lastMeetingRpcAt < MeetingTransitionGraceSeconds;
	}

private static bool InActiveMatch()
	{
		return ShipStatus.Instance != null && LobbyBehaviour.Instance == null;
	}

private static bool IsImpostor(PlayerControl player)
	{
		try
		{
			return player != null && player.Data != null && player.Data.Role != null && player.Data.Role.IsImpostor;
		}
		catch
		{
			return false;
		}
	}

private static bool IsDead(PlayerControl player)
	{
		try
		{
			return player != null && player.Data != null && player.Data.IsDead;
		}
		catch
		{
			return false;
		}
	}

private static bool PlayerExists(byte playerId)
	{
		try
		{
			if (GameData.Instance != null && GameData.Instance.AllPlayers != null)
			{
				var infos = GameData.Instance.AllPlayers.GetEnumerator();
				while (infos.MoveNext())
				{
					NetworkedPlayerInfo info = infos.Current;
					if (info != null && info.PlayerId == playerId)
					{
						return true;
					}
				}
			}
		}
		catch
		{
		}

		if (PlayerControl.AllPlayerControls == null)
		{
			return false;
		}

		try
		{
			var players = PlayerControl.AllPlayerControls.GetEnumerator();
			while (players.MoveNext())
			{
				PlayerControl player = players.Current;
				if (player != null && player.PlayerId == playerId && player.Data != null && !player.Data.Disconnected)
				{
					return true;
				}
			}
		}
		catch
		{
		}

		return false;
	}

private static void BlockSystemRpc(PlayerControl actor, int clientId, string title, string detail)
	{
		if (actor != null)
		{
			BlockRpc(actor, clientId, title, detail);
		}
		else
		{
			BlockMessage(clientId, title, detail);
		}
	}

private static int CurrentMapId()
	{
		try
		{
			return GameOptionsManager.Instance.CurrentGameOptions.MapId;
		}
		catch
		{
			return -1;
		}
	}

private static bool SabotagePayloadFitsMap(int systemId, byte amount, int mapId)
	{
		switch (systemId)
		{
			case 3:
				return mapId != 2 && mapId != 4 && IsStandardSabotageAmount(amount);
			case 7:
				return mapId < 5 && amount < 5;
			case 8:
				return mapId != 2 && mapId < 4 && IsReducedSabotageAmount(amount);
			case 14:
				if (amount == 0)
				{
					return mapId != 1 && mapId < 5;
				}

				return (mapId == 1 || mapId >= 5) && IsExtendedSabotageAmount(amount);
			case 21:
				return mapId == 2 && IsStandardSabotageAmount(amount);
			case 57:
				return false;
			case 58:
				return mapId == 4 && IsExtendedSabotageAmount(amount);
			default:
				return true;
		}
	}

private static bool IsCrewSabotagePayload(int systemId, byte amount)
	{
		if (systemId != 17)
		{
			return false;
		}

		switch (amount)
		{
			case 14:
			case 21:
			case 57:
			case 58:
			case 3:
			case 7:
			case 8:
				return true;
			default:
				return false;
		}
	}

private static bool IsReducedSabotageAmount(byte amount)
	{
		return amount == 64 || amount == 65;
	}

private static bool IsStandardSabotageAmount(byte amount)
	{
		return amount == 64 || amount == 65 || amount == 32 || amount == 33;
	}

private static bool IsExtendedSabotageAmount(byte amount)
	{
		return IsStandardSabotageAmount(amount) || amount == 16 || amount == 17;
	}

private static bool HasBrokenFriendCode(ClientData client)
	{
		string friendCode = null;
		try
		{
			friendCode = client?.FriendCode;
		}
		catch
		{
		}

		if (string.IsNullOrWhiteSpace(friendCode) || friendCode.Length < 7 || friendCode.Length > 32)
		{
			return true;
		}

		int hashIndex = friendCode.IndexOf('#');
		if (hashIndex <= 0 || hashIndex != friendCode.LastIndexOf('#') || hashIndex >= friendCode.Length - 1)
		{
			return true;
		}

		string prefix = friendCode.Substring(0, hashIndex);
		for (int i = 0; i < prefix.Length; i++)
		{
			char ch = prefix[i];
			if (!char.IsLetter(ch) && ch != '_' && ch != '-')
			{
				return true;
			}
		}

		return false;
	}

private static bool FriendCodeReady(ClientData client)
	{
		try
		{
			return !string.IsNullOrWhiteSpace(client?.FriendCode);
		}
		catch
		{
			return false;
		}
	}

private static bool ClientIdentityReady(ClientData client)
	{
		try
		{
			AcovPlayerLoadInfo info = AcovPlayerLoadStates.Evaluate(client);
			return info.Connected && info.Ready;
		}
		catch
		{
			return true;
		}
	}

private static (int Limit, float Window) RpcSpamLimit(int callId)
	{
		if (HighVolumeRpcLimits.TryGetValue(callId, out (int Limit, float Window) rule))
		{
			return rule;
		}

		return (DefaultRpcLimitPerWindow, DefaultRpcWindowSeconds);
	}

private static bool HitLimit(Dictionary<int, Queue<float>> store, int key, float now, float windowSeconds, int limit, out int count)
	{
		if (!store.TryGetValue(key, out Queue<float> hits))
		{
			hits = new Queue<float>();
			store[key] = hits;
		}

		while (hits.Count > 0 && now - hits.Peek() > windowSeconds)
		{
			hits.Dequeue();
		}

		hits.Enqueue(now);
		count = hits.Count;
		return count > limit;
	}

private static bool HitSameRpcLimit(int clientId, int callId, float now, float windowSeconds, int limit, out int count)
	{
		if (!SameRpcByClient.TryGetValue(clientId, out Dictionary<int, Queue<float>> byRpc))
		{
			byRpc = new Dictionary<int, Queue<float>>();
			SameRpcByClient[clientId] = byRpc;
		}

		if (!byRpc.TryGetValue(callId, out Queue<float> hits))
		{
			hits = new Queue<float>();
			byRpc[callId] = hits;
		}

		while (hits.Count > 0 && now - hits.Peek() > windowSeconds)
		{
			hits.Dequeue();
		}

		hits.Enqueue(now);
		count = hits.Count;
		return count > limit;
	}

private static bool ReaderLooksSane(MessageReader reader)
	{
		return reader != null && reader.Position >= 0 && reader.Position <= reader.Length;
	}

private static bool Enabled()
	{
		return (ModOptions.NetworkProtection == null || ModOptions.NetworkProtection.Value)
			&& AmongUsClient.Instance != null
			&& AmongUsClient.Instance.AmHost;
	}

private static bool EnabledNonHostFloodDrop()
	{
		return ModOptions.FloodDropNonHost != null
			&& ModOptions.FloodDropNonHost.Value
			&& AmongUsClient.Instance != null
			&& !AmongUsClient.Instance.AmHost;
	}

private static bool BrokenFriendCodeBanEnabled()
	{
		return ModOptions.BanBrokenFriendCode == null || ModOptions.BanBrokenFriendCode.Value;
	}

private static bool LevelRpcProtectionEnabled()
	{
		return ModOptions.LevelRpcProtection == null || ModOptions.LevelRpcProtection.Value;
	}

private static bool IdenticalNetIdProtectionEnabled()
	{
		return ModOptions.IdenticalNetIdProtection == null || ModOptions.IdenticalNetIdProtection.Value;
	}

private static bool ChatSpoofProtectionEnabled()
	{
		return ModOptions.ChatSpoofProtection == null || ModOptions.ChatSpoofProtection.Value;
	}

private static string ChatSpoofProtectionAction()
	{
		return ModOptions.ChatSpoofAction == null ? "Kick" : ModOptions.NormalizeNetworkProtectionAction(ModOptions.ChatSpoofAction.Value);
	}

private static bool CosmeticSpoofProtectionEnabled()
	{
		return ModOptions.CosmeticSpoofProtection == null || ModOptions.CosmeticSpoofProtection.Value;
	}

private static string CosmeticSpoofProtectionAction()
	{
		return ModOptions.CosmeticSpoofAction == null ? "Warn" : ModOptions.NormalizeNetworkProtectionAction(ModOptions.CosmeticSpoofAction.Value);
	}

private static bool SenderAttributionAmbiguous()
	{
		return string.IsNullOrEmpty(activeInboundConnectionKey)
			|| AmbiguousConnectionKeys.Contains(activeInboundConnectionKey);
	}

private static bool TryHandleKnownModRpc(PlayerControl player, byte rpcByte, int clientId, out bool skipOriginal)
	{
		skipOriginal = false;

		if (!KnownModRpcCatalog.TryFind(rpcByte, out int ruleIndex, out KnownModRpcRule rule))
		{
			return false;
		}

		string action = KnownModRpcAction(ruleIndex);
		if (action == "Null")
		{
			return true;
		}

		string warnKey = clientId >= 0 ? $"{clientId}:{rule.DisplayName}" : null;
		bool firstTime = warnKey == null || WarnedModRpcOnce.Add(warnKey);
		if (!firstTime && action == "Warn")
		{
			skipOriginal = true;
			return true;
		}

		BlockRpc(player, clientId, "Mod CallRpc", $"{rule.DisplayName}: RPC {rpcByte}.", action);
		skipOriginal = true;
		return true;
	}

private static string KnownModRpcAction(int ruleIndex)
	{
		try
		{
			ConfigEntry<string>[] actions = ModOptions.KnownModRpcActions;
			if (actions != null && ruleIndex >= 0 && ruleIndex < actions.Length && actions[ruleIndex] != null)
			{
				return ModOptions.NormalizeNetworkProtectionAction(actions[ruleIndex].Value);
			}
		}
		catch
		{
		}

		return "Warn";
	}

private static string ProtectionAction()
	{
		return ModOptions.NetworkProtectionAction == null ? "Ban" : ModOptions.NormalizeNetworkProtectionAction(ModOptions.NetworkProtectionAction.Value);
	}

private static string LevelRpcProtectionAction()
	{
		return ModOptions.LevelRpcAction == null ? "Ban" : ModOptions.NormalizeNetworkProtectionAction(ModOptions.LevelRpcAction.Value);
	}

private static void BlockMessage(int clientId, string title, string detail)
	{
		BlockMessage(clientId, title, detail, null);
	}

private static void BlockMessage(int clientId, string title, string detail, string actionOverride)
	{
		clientId = ResolveBestActiveClientId(clientId);
		string action = string.IsNullOrWhiteSpace(actionOverride) ? ProtectionAction() : ModOptions.NormalizeNetworkProtectionAction(actionOverride);
		bool shouldNotify = ShouldShowProtectionNotice(clientId, title, detail);
		if (shouldNotify)
		{
			AcovPlugin.Logger?.LogWarning((object)$"Network protection blocked message from client {clientId}: {title}. {detail}");
		}

		if (shouldNotify && action != "Null")
		{
			AcovSecurityNotifications.Show(action, ClientName(clientId), title, detail, clientId);
		}

		ApplyProtectionAction(clientId, title, detail, action);
	}

private static bool ShouldShowProtectionNotice(int clientId, string title, string detail)
	{
		float now = Time.realtimeSinceStartup;
		PruneProtectionNotices(now);
		string key = ProtectionNoticeKey(clientId, title);
		if (ProtectionNoticeSeenAt.TryGetValue(key, out float lastSeen) && now - lastSeen < ProtectionNoticeRepeatSeconds)
		{
			return false;
		}

		ProtectionNoticeSeenAt[key] = now;
		return true;
	}

private static string ProtectionNoticeKey(int clientId, string title)
	{
		string source = clientId >= 0
			? "c:" + clientId.ToString()
			: "k:" + TrimConnectionKeyForLog(activeInboundConnectionKey);
		return source + ":" + (title ?? string.Empty);
	}

private static void PruneProtectionNotices(float now)
	{
		if (ProtectionNoticeSeenAt.Count == 0)
		{
			return;
		}

		ScratchConnectionKeys.Clear();
		foreach (KeyValuePair<string, float> pair in ProtectionNoticeSeenAt)
		{
			if (string.IsNullOrWhiteSpace(pair.Key) || now - pair.Value > ProtectionNoticeRepeatSeconds * 4f)
			{
				ScratchConnectionKeys.Add(pair.Key);
			}
		}

		for (int i = 0; i < ScratchConnectionKeys.Count; i++)
		{
			string key = ScratchConnectionKeys[i];
			if (!string.IsNullOrWhiteSpace(key))
			{
				ProtectionNoticeSeenAt.Remove(key);
			}
		}

		ScratchConnectionKeys.Clear();
	}

private static void BlockRpc(PlayerControl player, int clientId, string title, string detail)
	{
		BlockRpc(player, clientId, title, detail, null);
	}

private static void BlockRpc(PlayerControl player, int clientId, string title, string detail, string actionOverride)
	{
		int playerClientId = GetPlayerClientId(player);
		string name = clientId >= 0 && clientId != playerClientId ? ClientName(clientId) : PlayerName(player);
		AcovPlugin.Logger?.LogWarning((object)$"Network protection blocked RPC from {name} (client {clientId}): {title}. {detail}");
		string action = string.IsNullOrWhiteSpace(actionOverride) ? ProtectionAction() : ModOptions.NormalizeNetworkProtectionAction(actionOverride);
		if (action != "Null")
		{
			AcovSecurityNotifications.Show(action, name, title, detail, clientId);
		}

		ApplyProtectionAction(clientId, title, detail, action);
	}

internal static void ReportQuickChatFlood(PlayerControl sender, int ownerId, string detail)
	{
		try
		{
			string action = ModOptions.NormalizeNetworkProtectionAction(
				ModOptions.QuickChatAction != null ? ModOptions.QuickChatAction.Value : "Kick");

			if (action != "Null")
			{
				string name = ownerId >= 0 ? ClientName(ownerId) : PlayerName(sender);
				AcovSecurityNotifications.Show(action, name, "QuickChat flood", detail, ownerId);
			}

			ApplyProtectionAction(ownerId, "QuickChat flood", detail, action);
		}
		catch
		{
		}
	}

private static void ApplyProtectionAction(int clientId, string attackType, string detail)
	{
		ApplyProtectionAction(clientId, attackType, detail, ProtectionAction());
	}

private static void ApplyProtectionAction(int clientId, string attackType, string detail, string action)
	{
		switch (ModOptions.NormalizeNetworkProtectionAction(action))
		{
			case "Ban":
				DisconnectIfHost(clientId, true, attackType, detail);
				return;
			case "Kick":
				DisconnectIfHost(clientId, false, attackType, detail);
				return;
			default:
				return;
		}
	}

private static void DisconnectIfHost(int clientId, bool ban, string attackType, string detail)
	{
		if (clientId < 0 || AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
		{
			return;
		}

		InnerNetClient client = (InnerNetClient)AmongUsClient.Instance;
		if (clientId == client.ClientId || clientId == client.HostId)
		{
			AcovPlugin.Logger?.LogWarning((object)$"Network protection refused to {(ban ? "ban" : "kick")} local/host client id {clientId}.");
			return;
		}

		float now = Time.realtimeSinceStartup;
		if (LastClientActionAt.TryGetValue(clientId, out float lastAction) && now - lastAction < 5f)
		{
			return;
		}

		LastClientActionAt[clientId] = now;
		RecentJoinSenderCandidates.Remove(clientId);
		try
		{
			if (ban)
			{
				ClientData target = GetClientById(clientId) ?? GetRecentClient(client, clientId);
				if (!AcovAccessLists.AddBanClient(target, $"{attackType}: {detail}") && TryGetSnapshotIdentity(clientId, out AcovClientIdentity identity))
				{
					AcovAccessLists.AddBanIdentity(identity, $"{attackType}: {detail}");
				}
			}

			client.KickPlayer(clientId, ban);
			AcovPlugin.Logger?.LogWarning((object)$"Network protection sent {(ban ? "ban" : "kick")} for client {clientId}.");
			QueueLocalCleanup(clientId, 0f);
		}
		catch (Exception error)
		{
			AcovPlugin.Logger?.LogWarning((object)$"Network protection {(ban ? "ban" : "kick")} failed for client {clientId}: {error.Message}");
			QueueLocalCleanup(clientId, 0.15f);
		}
	}

internal static void UpdateLocalCleanup()
	{
		if (PendingLocalCleanupAt.Count == 0)
		{
			return;
		}

		float now = Time.realtimeSinceStartup;
		List<int> completed = null;
		List<int> ids = new List<int>(PendingLocalCleanupAt.Keys);
		for (int i = 0; i < ids.Count; i++)
		{
			int clientId = ids[i];
			if (!PendingLocalCleanupAt.TryGetValue(clientId, out float runAt) || now < runAt)
			{
				continue;
			}

			if (ForceLocalClientLeave(clientId))
			{
				if (completed == null)
				{
					completed = new List<int>();
				}

				completed.Add(clientId);
			}
			else
			{
				PendingLocalCleanupAt[clientId] = now + 0.35f;
			}
		}

		if (completed == null)
		{
			return;
		}

		for (int i = 0; i < completed.Count; i++)
		{
			PendingLocalCleanupAt.Remove(completed[i]);
		}
	}

private static void QueueLocalCleanup(int clientId, float delay)
	{
		if (clientId < 0)
		{
			return;
		}

		PendingLocalCleanupAt[clientId] = Time.realtimeSinceStartup + Mathf.Max(0f, delay);
	}

private static bool ForceLocalClientLeave(int clientId)
	{
		if (AmongUsClient.Instance == null)
		{
			return true;
		}

		InnerNetClient client = (InnerNetClient)AmongUsClient.Instance;
		if (!client.AmHost || clientId == client.ClientId || clientId == client.HostId)
		{
			return true;
		}

		ClientData target = GetClientById(clientId) ?? GetRecentClient(client, clientId);
		if (target == null)
		{
			CleanupDisconnectedGameData();
			return true;
		}

		PlayerControl character = null;
		try
		{
			character = target.Character;
		}
		catch
		{
		}

		MarkDisconnected(target, character);
		RunNormalPlayerLeft(target);
		RemoveClientFromActiveList(client, target, clientId);
		DestroyVisibleCharacter(character);
		CleanupDisconnectedGameData();

		bool stillListed = GetClientById(clientId) != null;
		if (!stillListed)
		{
			AcovPlugin.Logger?.LogWarning((object)$"Network protection cleaned removed client {clientId} from lobby.");
		}

		return !stillListed;
	}

private static void MarkDisconnected(ClientData client, PlayerControl character)
	{
		try
		{
			client.InScene = false;
		}
		catch
		{
		}

		try
		{
			if (character != null && character.Data != null)
			{
				character.Data.Disconnected = true;
			}
		}
		catch
		{
		}
	}

private static void RunNormalPlayerLeft(ClientData client)
	{
		if (client == null || AmongUsClient.Instance == null)
		{
			return;
		}

		try
		{
			AmongUsClient.Instance.OnPlayerLeft(client, (DisconnectReasons)0);
		}
		catch (Exception error)
		{
			AcovPlugin.Logger?.LogWarning((object)$"Network protection OnPlayerLeft cleanup failed for client {client.Id}: {error.Message}");
		}
	}

private static void RemoveClientFromActiveList(InnerNetClient client, ClientData target, int clientId)
	{
		if (client == null || target == null)
		{
			return;
		}

		try
		{
			lock (client.allClients)
			{
				for (int i = client.allClients.Count - 1; i >= 0; i--)
				{
					ClientData current = client.allClients[i];
					if (current != null && (current.Id == clientId || current == target))
					{
						client.allClients.RemoveAt(i);
					}
				}
			}
		}
		catch (Exception error)
		{
			AcovPlugin.Logger?.LogWarning((object)$"Network protection active client cleanup failed for {clientId}: {error.Message}");
		}
	}

private static void DestroyVisibleCharacter(PlayerControl character)
	{
		if (character == null)
		{
			return;
		}

		try
		{
			UnityEngine.Object.Destroy(((Component)character).gameObject);
		}
		catch
		{
		}
	}

private static void CleanupDisconnectedGameData()
	{
		try
		{
			GameData.Instance?.RemoveDisconnectedPlayers();
		}
		catch
		{
		}
	}

private static void RememberAllClients()
	{
		if (AmongUsClient.Instance == null)
		{
			return;
		}

		int frame = Time.frameCount;
		if (frame == lastRememberedClientsFrame)
		{
			return;
		}
		lastRememberedClientsFrame = frame;

		PruneClientSnapshots();
		PruneRecentJoinSenderCandidates();
		InnerNetClient inner = (InnerNetClient)AmongUsClient.Instance;
		try
		{
			var clients = inner.allClients.GetEnumerator();
			while (clients.MoveNext())
			{
				RememberClient(clients.Current);
			}
		}
		catch
		{
		}

		try
		{
			Il2CppSystem.Collections.Generic.List<ClientData> clients = new Il2CppSystem.Collections.Generic.List<ClientData>();
			inner.GetAllClients(clients);
			var cursor = clients.GetEnumerator();
			while (cursor.MoveNext())
			{
				RememberClient(cursor.Current);
			}
		}
		catch
		{
		}
	}

private static void RememberClient(ClientData client)
	{
		if (client == null || client.Id < 0)
		{
			return;
		}

		InnerNetClient inner = AmongUsClient.Instance == null ? null : (InnerNetClient)AmongUsClient.Instance;
		if (inner != null && (client.Id == inner.ClientId || client.Id == inner.HostId))
		{
			return;
		}

		float now = Time.realtimeSinceStartup;
		ClientSnapshot snapshot;
		if (!ClientSnapshotsById.TryGetValue(client.Id, out snapshot))
		{
			snapshot.PlayerId = byte.MaxValue;
			snapshot.OwnerId = -1;
		}
		snapshot.ClientId = client.Id;
		snapshot.PlayerName = ReadClientName(client, snapshot.PlayerName);
		snapshot.FriendCode = SafeString(client.FriendCode);
		snapshot.ProductUserId = SafeString(client.ProductUserId);
		snapshot.LastSeenAt = now;
		if (snapshot.JoinedAt <= 0f)
		{
			snapshot.JoinedAt = ClientJoinTimeAt.TryGetValue(client.Id, out float joinedAt) ? joinedAt : now;
		}

		PlayerControl character = null;
		try
		{
			character = client.Character;
		}
		catch
		{
		}

		if (character != null)
		{
			snapshot.PlayerId = character.PlayerId;
			try
			{
				int ownerId = ((InnerNetObject)character).OwnerId;
				if (ownerId >= 0)
				{
					snapshot.OwnerId = ownerId;
				}
			}
			catch
			{
			}
		}
		else if (snapshot.PlayerId == 0)
		{
			snapshot.PlayerId = byte.MaxValue;
		}

		ClientSnapshotsById[client.Id] = snapshot;
		if (snapshot.PlayerId < 100)
		{
			ClientIdByPlayerId[snapshot.PlayerId] = client.Id;
		}

		if (snapshot.OwnerId >= 0)
		{
			ClientIdByOwnerId[snapshot.OwnerId] = client.Id;
		}
	}

private static void PruneClientSnapshots()
	{
		if (ClientSnapshotsById.Count == 0)
		{
			return;
		}

		float now = Time.realtimeSinceStartup;
		ScratchClientSnapshotIds.Clear();
		foreach (KeyValuePair<int, ClientSnapshot> pair in ClientSnapshotsById)
		{
			if (now - pair.Value.LastSeenAt > ClientSnapshotTtlSeconds)
			{
				ScratchClientSnapshotIds.Add(pair.Key);
			}
		}

		for (int i = 0; i < ScratchClientSnapshotIds.Count; i++)
		{
			ForgetClientSnapshot(ScratchClientSnapshotIds[i]);
		}
	}
}
}
