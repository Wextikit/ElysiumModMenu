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


internal static class AcovAccessLists
{
	internal static bool AddBanClient(ClientData target, string reason)
	{
		try
		{
			if (target == null) return false;
			string fc = target.FriendCode;
			if (string.IsNullOrEmpty(fc)) return false;
			ElysiumModMenu.ElysiumModMenuGUI.AddToBanList(fc, string.IsNullOrEmpty(target.ProductUserId) ? "Unknown" : target.ProductUserId, string.IsNullOrEmpty(target.PlayerName) ? "Unknown" : target.PlayerName, reason);
			return true;
		}
		catch { return false; }
	}
	internal static void AddBanIdentity(AcovClientIdentity identity, string reason)
	{
		try
		{
			string fc = identity.FriendCode;
			if (string.IsNullOrEmpty(fc)) fc = identity.ProductUserId;
			if (string.IsNullOrEmpty(fc)) return;
			ElysiumModMenu.ElysiumModMenuGUI.AddToBanList(fc, string.IsNullOrEmpty(identity.ProductUserId) ? "Unknown" : identity.ProductUserId, string.IsNullOrEmpty(identity.PlayerName) ? "Unknown" : identity.PlayerName, reason);
		}
		catch { }
	}
	internal static string ClientDisplayName(ClientData data, int clientId)
	{
		try { return data != null && !string.IsNullOrEmpty(data.PlayerName) ? data.PlayerName : ("Client " + clientId); }
		catch { return "Client " + clientId; }
	}
	internal static bool TryHandleJoinMessage(InnerNetClient client, MessageReader reader) { return false; }
	internal static void UpdateNickBanChecks() { }
}
}
