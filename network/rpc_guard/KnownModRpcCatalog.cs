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

namespace ElysiumNetGuard
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


internal static class KnownModRpcCatalog
{
	private static readonly KnownModRpcRule[] Rules = BuildRules();
	private static readonly byte[] RpcIds = BuildRpcIds();

	internal static bool TryFind(byte rpcByte, out int ruleIndex, out KnownModRpcRule rule)
	{
		ruleIndex = -1;
		rule = default(KnownModRpcRule);
		for (int i = 0; i < RpcIds.Length && i < Rules.Length; i++)
		{
			if (RpcIds[i] != rpcByte) continue;
			ruleIndex = i;
			rule = Rules[i];
			return true;
		}

		return false;
	}

	private static byte[] BuildRpcIds()
	{
		try
		{
			if (ElysiumModMenu.ElysiumModMenuGUI.spoofMenuRPCs != null)
				return ElysiumModMenu.ElysiumModMenuGUI.spoofMenuRPCs.ToArray();
		}
		catch { }

		return new byte[0];
	}

	private static KnownModRpcRule[] BuildRules()
	{
		try
		{
			string[] names = ElysiumModMenu.ElysiumModMenuGUI.spoofMenuNames;
			byte[] rpcs = ElysiumModMenu.ElysiumModMenuGUI.spoofMenuRPCs;
			if (names == null || rpcs == null) return new KnownModRpcRule[0];

			int count = Math.Min(names.Length, rpcs.Length);
			KnownModRpcRule[] rules = new KnownModRpcRule[count];
			for (int i = 0; i < count; i++)
			{
				rules[i] = new KnownModRpcRule(CleanName(names[i]));
			}
			return rules;
		}
		catch
		{
			return new KnownModRpcRule[0];
		}
	}

	private static string CleanName(string name)
	{
		if (string.IsNullOrWhiteSpace(name)) return "Known Mod RPC";
		return Regex.Replace(name, @"\s*\(\d+\)\s*$", string.Empty).Trim();
	}
}
}

