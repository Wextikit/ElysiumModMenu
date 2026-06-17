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


internal static class LobbyTeleportPositionTracker
{
	private const float TeleportThreshold = 5f;
	private const float SpawnGraceSeconds = 3f;
	private static readonly Dictionary<byte, Vector2> lastPos = new Dictionary<byte, Vector2>();
	private static readonly Dictionary<byte, float> firstSeenAt = new Dictionary<byte, float>();
	private static int trackedGameId = -1;

	internal static void Reset()
	{
		lastPos.Clear();
		firstSeenAt.Clear();
		trackedGameId = -1;
	}

	internal static void ForgetPlayer(byte playerId)
	{
		lastPos.Remove(playerId);
		firstSeenAt.Remove(playerId);
	}

	internal static void CheckPlayer(PlayerControl player)
	{
		try
		{
			if (player == null || player == PlayerControl.LocalPlayer) return;
			if (LobbyBehaviour.Instance == null) return;
			if (ModOptions.LobbyTeleportDetection == null || !ModOptions.LobbyTeleportDetection.Value) return;

			int gameId = AmongUsClient.Instance != null ? ((InnerNetClient)AmongUsClient.Instance).GameId : -1;
			if (gameId != trackedGameId)
			{
				trackedGameId = gameId;
				lastPos.Clear();
				firstSeenAt.Clear();
			}

			float now = Time.realtimeSinceStartup;
			byte pid = player.PlayerId;
			Vector2 cur = ((Component)player).transform.position;

			if (!firstSeenAt.ContainsKey(pid))
			{
				firstSeenAt[pid] = now;
				lastPos[pid] = cur;
				return;
			}

			if (now - firstSeenAt[pid] < SpawnGraceSeconds)
			{
				lastPos[pid] = cur;
				return;
			}

			if (!lastPos.TryGetValue(pid, out Vector2 prev))
			{
				lastPos[pid] = cur;
				return;
			}

			lastPos[pid] = cur;

			float dist = Vector2.Distance(prev, cur);
			if (dist < TeleportThreshold) return;

			int clientId = NetworkProtectionGuard.ResolvePlayerClientId(player);
			NetworkProtectionGuard.FlagLobbyTeleport(player, clientId, dist);
		}
		catch { }
	}
}
}
