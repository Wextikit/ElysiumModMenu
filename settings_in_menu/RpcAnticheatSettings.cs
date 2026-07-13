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

namespace ElysiumModMenu
{
    public partial class ElysiumModMenuGUI : MonoBehaviour
    {
        public static int punishmentMode = 1;

public static bool settingsDirty = false;

public static string[] punishmentNames = { "Null", "Warn", "Kick", "Ban" };

public static bool blockSpoofRPC = true;

public static bool blockSabotageRPC = true;

public static bool blockGameRpcInLobby = true;

public static bool blockChatFloodRpc = true;

public static bool blockMeetingFloodRpc = true;

public static bool enablePasosLimit = true;

public static bool enableLocalPasosBan = true;

public static bool enableHostPasosBan = true;

public static bool autoBanBrokenFriendCode = false;

public static int chatRpcLimit = 1;

public static float chatRpcWindow = 1f;

public static int meetingRpcLimit = 2;

public static float meetingRpcWindow = 9999f;
    }
}
