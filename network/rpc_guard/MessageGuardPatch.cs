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


[HarmonyPatch(typeof(InnerNetClient), "HandleMessage")]
internal static class NetworkMessageProtectionPatch
{
	public static bool Prefix(InnerNetClient __instance, MessageReader reader, SendOption sendOption)
	{
		if (!ElysiumModMenu.ElysiumModMenuGUI.oldAntiCheatVersion &&
			!(ElysiumModMenu.ElysiumModMenuGUI.overflowProtection && __instance != null && __instance.AmHost))
			return true;
		using (ElysiumNetGuard.NetGuardProfiler.Sample("Net.CheckMessage"))
			return NetworkGuard.CheckMessage(__instance, reader, sendOption);
	}
}
}

