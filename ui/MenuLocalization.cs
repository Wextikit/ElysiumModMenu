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

public static string[] spoofMenuNames = { "ElysiumModMenu", "HostGuard/TOH", "Polar", "BanMod", "Better Among Us", "Sicko Menu", "GNC", "KillNetwork (V1)", "KillNetwork (V2)", "KNM" };

public static byte[] spoofMenuRPCs = { 89, 176, 204, 212, 151, 164, 154, 85, 150, 162 };

public static float rpcSpoofDelay = 4f;

public static readonly string[] menuLanguageNames = { "Auto", "English", "Русский", "Deutsch", "Français", "Español", "Italiano", "Português", "Polski", "Nederlands", "Türkçe", "Čeština", "Română", "Magyar", "Svenska", "Dansk", "Suomi", "Norsk", "Українська", "Ελληνικά", "中文", "日本語", "한국어" };

public static readonly string[] menuLanguageCodes = { "auto", "en", "ru", "de", "fr", "es", "it", "pt", "pl", "nl", "tr", "cs", "ro", "hu", "sv", "da", "fi", "no", "uk", "el", "zh", "ja", "ko" };

public static int currentMenuLanguageIndex = 0;
}
}
