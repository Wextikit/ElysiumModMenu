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

public static string[] spoofMenuNames = {
            "RockStar (157)", "RockStarLite / Chocoo (121)", "TuffMenu (167)", "SickoMenu (164)",
            "HostGuard / TOH (176)", "Polar Client (195)", "Polar Client (204)", "GNC (154)",
            "KillNet old (85)", "KillNet Base (150)", "KNM (162)", "KillNet Alt (250)",
            "BanMod (212)", "BanMod (213)", "BanMod (214)", "BanMod (215)", "BanMod (216)",
            "BanMod (217)", "BanMod (218)", "BanMod (219)", "Gaff Menu (144)", "Gaff Menu (145)",
            "GMM (188)", "GMM (189)", "ABOBIKImenu (169)", "Eclipse (210)",
            "Private Client (173)", "Better Among Us (151)", "Better Among Us (152)", "CrewMod (255)",
            "AUM BitCrackers (111)", "SentinelAU (231)", "Lunar / ElysiumModMenu (133)",
            "ElysiumModMenu (89)", "Banmod or Tuff maybe (202)", "Custom RPC"
        };

public static byte[] spoofMenuRPCs = {
            157, 121, 167, 164, 176, 195, 204, 154, 85, 150, 162, 250, 212, 213, 214, 215, 216,
            217, 218, 219, 144, 145, 188, 189, 169, 210, 173, 151, 152, 255, 111, 231, 133, 89, 202
        };

public static float rpcSpoofDelay = 4f;

public static string customSpoofRpcInput = "89";

public static bool customSpoofRpcInputFocused = false;

public static readonly string[] menuLanguageNames = { "Auto", "English", "Русский", "Deutsch", "Français", "Español", "Italiano", "Português", "Polski", "Nederlands", "Türkçe", "Čeština", "Română", "Magyar", "Svenska", "Dansk", "Suomi", "Norsk", "Українська", "Ελληνικά", "中文", "日本語", "한국어" };

public static readonly string[] menuLanguageCodes = { "auto", "en", "ru", "de", "fr", "es", "it", "pt", "pl", "nl", "tr", "cs", "ro", "hu", "sv", "da", "fi", "no", "uk", "el", "zh", "ja", "ko" };

public static int currentMenuLanguageIndex = 0;
}
}
