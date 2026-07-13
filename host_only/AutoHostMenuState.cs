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
using System.Globalization;
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

namespace ElysiumModMenu
{
    public partial class ElysiumModMenuGUI : MonoBehaviour
    {
private Vector2 outfitsScrollPos = Vector2.zero;

public static bool AutoHostEnabled = false;

public static bool AutoHostShieldBreakEnabled = false;

public static bool AutoReturnLobbyAfterMatch = true;

public static bool AutoHostNotifications = true;

public static bool AutoHostForceLastMinute = true;

public static bool AutoHostWaitLoadedPlayers = true;

public static bool AutoHostCancelBelowMin = true;

public static bool AutoHostInstantStart = false;

public static bool AutoHostAutoRunEnabled = false;

public static bool BugroomScoutEnabled = false;

public static int AutoHostMinPlayers = 4;

public static int AutoHostForceMinPlayers = 2;

public static float AutoHostStartDelaySeconds = 15f;

public static float AutoHostBackoffSeconds = 8f;

public static float AutoHostWarmupSeconds = 5f;

public static float AutoHostLoadGraceSeconds = 20f;

public static int AutoHostForceAfterMinutes = 0;

public static int AutoHostFastStartPlayers = 13;

public static float AutoHostFastStartDelaySeconds = 5f;

private int currentAutoHostSubTab = 0;

private string[] autoHostSubTabs = { "LOBBY CONTROLS", "ROLE MANAGER", "ANTI CHEAT", "AUTO HOST" };
    }
}
