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


internal static class ModOptions
{
	internal static readonly AcovOption<bool> NetworkProtection = new AcovOption<bool>(() => ElysiumModMenu.ElysiumModMenuGUI.oldAntiCheatVersion);
	internal static readonly AcovOption<bool> FloodDropNonHost = new AcovOption<bool>(() => true);
	internal static readonly AcovOption<bool> NonHostDataDrop = new AcovOption<bool>(() => true);
	internal static readonly AcovOption<bool> LobbyTeleportDetection = new AcovOption<bool>(() => true);
	internal static readonly AcovOption<string> NetworkProtectionAction = null;
	internal static readonly AcovOption<bool> BanBrokenFriendCode = null;
	internal static readonly AcovOption<bool> LevelRpcProtection = null;
	internal static readonly AcovOption<string> LevelRpcAction = null;
	internal static readonly AcovOption<bool> IdenticalNetIdProtection = null;
	internal static readonly AcovOption<bool> ChatSpoofProtection = null;
	internal static readonly AcovOption<string> ChatSpoofAction = null;
	internal static readonly AcovOption<bool> CosmeticSpoofProtection = null;
	internal static readonly AcovOption<string> CosmeticSpoofAction = null;
	internal static readonly AcovOption<string> MalformedDataAction = null;
	internal static readonly AcovOption<string> QuickChatAction = null;
	internal static readonly AcovOption<string> LobbyTeleportAction = null;
	internal static readonly AcovOption<string> SnapToAction = null;
	internal static readonly AcovOption<bool> VentGuard = null;
	internal static readonly AcovOption<bool> SpawnFloodGuard = null;
	internal static readonly AcovOption<int> MaxAllowedLevelRpc = null;
	internal static readonly AcovOption<string> SuspiciousLevelList = null;
	internal static ConfigEntry<string>[] KnownModRpcActions { get { return null; } }
	internal static string NormalizeNetworkProtectionAction(string action)
	{
		if (string.IsNullOrWhiteSpace(action)) return "Warn";
		switch (action.Trim().ToLowerInvariant())
		{
			case "null": case "none": case "off": case "ignore": return "Null";
			case "warn": case "notify": return "Warn";
			case "kick": return "Kick";
			case "ban": return "Ban";
			default: return "Warn";
		}
	}
}
}
