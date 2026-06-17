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
    [BepInPlugin("com.elysiummodmenu.menu", "ElysiumModMenu", "1.3.5.1")]
    public class Plugin : BasePlugin
    {
        public static ModPlayer modClass;

        public static Plugin Instance { get; private set; } = null!;
        public static string ElysiumFolder = "";
        public static ConfigFile MenuConfig;
        public static ConfigEntry<float> RpcSpoofDelayConfig;
        public static ConfigEntry<KeyCode> MenuKeybind;
        public static ConfigEntry<string> SpoofedLevel;
        public static ConfigEntry<bool> EnableLevelSpoofConfig;
        public static ConfigEntry<bool> EnableFriendCodeSpoofConfig;
        public static ConfigEntry<string> SpoofFriendCodeConfig;
        public static ConfigEntry<bool> EnablePlatformSpoof;
        public static ConfigEntry<bool> AutoBanBrokenFriendCodeConfig;
        public static ConfigEntry<int> PlatformIndex;
        public static ConfigEntry<bool> ShowWatermarkConfig;
        public static ConfigEntry<int> MenuColorIndexConfig;
        public static ConfigEntry<bool> RgbMenuModeConfig;
        public static ConfigEntry<bool> RgbMenuTextConfig;
        public static ConfigEntry<bool> BoldMenuTextConfig;
        public static ConfigEntry<bool> UnlockCosmeticsConfig;
        public static ConfigEntry<bool> MoreLobbyInfoConfig;
        public static ConfigEntry<bool> EnableChatDarkModeConfig;
        public static ConfigEntry<string> GhostChatColorConfig;
        public static ConfigEntry<bool> EnableAnomalyLogReportsConfig;
        public static ConfigEntry<bool> ShowEspFriendCodeConfig;

        public override void Load()
        {
            Instance = this;

            ElysiumFolder = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "ElysiumModMenu");
            if (!System.IO.Directory.Exists(ElysiumFolder))
            {
                System.IO.Directory.CreateDirectory(ElysiumFolder);
            }

            string banFile = System.IO.Path.Combine(ElysiumFolder, "ElysiumModMenuBanList.txt");
            if (!System.IO.File.Exists(banFile))
            {
                System.IO.File.Create(banFile).Dispose();
            }

            string platformBanFile = System.IO.Path.Combine(ElysiumFolder, "ElysiumPlatformBanList.txt");
            if (!System.IO.File.Exists(platformBanFile))
            {
                System.IO.File.WriteAllText(platformBanFile, "# One custom platform token per line. Matching PlatformName values are host-banned when enabled.\n# Example: github\n");
            }

            string friendEspFile = System.IO.Path.Combine(ElysiumFolder, "ElysiumFriendEspIgnore.txt");
            if (!System.IO.File.Exists(friendEspFile))
            {
                System.IO.File.WriteAllText(friendEspFile, "# One nickname, Friend Code, or PUID per line. Matching players will not show ESP info.\n");
            }

            string botBanFile = System.IO.Path.Combine(ElysiumFolder, "ElysiumBotBanList.txt");
            if (!System.IO.File.Exists(botBanFile))
            {
                System.IO.File.WriteAllText(botBanFile, "# Auto bot ban list. Format: FriendCode|PUID|Nickname|Date|Reason\n# You can also add one nickname, Friend Code, or PUID per line to always ban matching players.\n");
            }

            string configPath = System.IO.Path.Combine(ElysiumFolder, "ElysiumModMenu.cfg");
            RemoveLegacyPlaintextWebhookConfig(configPath);
            MenuConfig = new ConfigFile(configPath, true);
            RpcSpoofDelayConfig = MenuConfig.Bind("ElysiumModMenu.Spoofing", "RpcDelay", 4f, "");
            MenuKeybind = MenuConfig.Bind("ElysiumModMenu.GUI", "Keybind", KeyCode.Insert, "");
            SpoofedLevel = MenuConfig.Bind("ElysiumModMenu.Spoofing", "Level", "100", "");
            EnableLevelSpoofConfig = MenuConfig.Bind("ElysiumModMenu.Spoofing", "EnableLevelSpoof", true, "");
            EnableFriendCodeSpoofConfig = MenuConfig.Bind("ElysiumModMenu.Spoofing", "EnableFriendCodeSpoof", false, "");
            SpoofFriendCodeConfig = MenuConfig.Bind("ElysiumModMenu.Spoofing", "FriendCode", "crewmate01", "");
            EnablePlatformSpoof = MenuConfig.Bind("ElysiumModMenu.Spoofing", "EnablePlatformSpoof", true, "");
            AutoBanBrokenFriendCodeConfig = MenuConfig.Bind("ElysiumModMenu.Anticheat", "AutoBanBrokenFriendCode", false, "");
            PlatformIndex = MenuConfig.Bind("ElysiumModMenu.Spoofing", "PlatformIndex", 1, "");
            ShowWatermarkConfig = MenuConfig.Bind("ElysiumModMenu.GUI", "ShowWatermark", true, "");
            MenuColorIndexConfig = MenuConfig.Bind("ElysiumModMenu.GUI", "MenuColorIndex", 10, "");
            RgbMenuModeConfig = MenuConfig.Bind("ElysiumModMenu.GUI", "RgbMenuMode", false, "");
            RgbMenuTextConfig = MenuConfig.Bind("ElysiumModMenu.GUI", "RgbMenuText", false, "When true, RGB Menu Mode also recolors menu text.");
            BoldMenuTextConfig = MenuConfig.Bind("ElysiumModMenu.GUI", "BoldMenuText", true, "When true, menu text is drawn bold.");
            UnlockCosmeticsConfig = MenuConfig.Bind("ElysiumModMenu.General", "UnlockCosmetics", true, "");
            MoreLobbyInfoConfig = MenuConfig.Bind("ElysiumModMenu.Visuals", "MoreLobbyInfo", true, "");
            EnableChatDarkModeConfig = MenuConfig.Bind("ElysiumModMenu.Chat", "EnableChatDarkMode", true, "Turns the custom dark chat input and bubble colors on/off.");
            GhostChatColorConfig = MenuConfig.Bind("ElysiumModMenu.Chat", "GhostChatColor", "#D7B8FF", "Hex color for visible ghost chat messages.");
            EnableAnomalyLogReportsConfig = MenuConfig.Bind("ElysiumModMenu.Diagnostics", "EnableAnomalyLogReports", true, "Yes/No: sending freeze/overload logs to the mod author. Note: this does not affect your performance, nor does it steal your data or anything like that. It is strictly needed for quick anti-cheat fixes.");
            ShowEspFriendCodeConfig = MenuConfig.Bind("ElysiumModMenu.Visuals", "ShowEspFriendCode", true, "Show Friend Code in ESP player info.");
            ClassInjector.RegisterTypeInIl2Cpp<ElysiumModMenuGUI>();
            ClassInjector.RegisterTypeInIl2Cpp<ModPlayer>();
            ClassInjector.RegisterTypeInIl2Cpp<Acov.Patches.NetworkProtectionCleanupDriver>();

            var guiObject = new GameObject("ElysiumModMenu_Object");
            UnityEngine.Object.DontDestroyOnLoad(guiObject);
            guiObject.hideFlags = HideFlags.HideAndDontSave;
            guiObject.AddComponent<ElysiumModMenuGUI>();
            guiObject.AddComponent<Acov.Patches.NetworkProtectionCleanupDriver>();

            modClass = AddComponent<ModPlayer>();

            var harmony = new Harmony("com.elysiummodmenu.harmony");
            harmony.PatchAll();
        }

        private static void RemoveLegacyPlaintextWebhookConfig(string configPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(configPath) || !System.IO.File.Exists(configPath)) return;

                List<string> lines = System.IO.File.ReadAllLines(configPath).ToList();
                string[] legacyKeys = { "AnomalyLogWebhookUrl", "EnableDiagnostics", "EndpointUrl", "AuthKey", "IncludePuid" };
                bool changed = false;

                for (int i = lines.Count - 1; i >= 0; i--)
                {
                    string trimmed = lines[i].TrimStart();
                    if (!legacyKeys.Any(key => trimmed.StartsWith(key, StringComparison.OrdinalIgnoreCase))) continue;

                    int start = i;
                    while (start > 0)
                    {
                        string previous = lines[start - 1].TrimStart();
                        if (!previous.StartsWith("#") && previous.Length != 0) break;
                        start--;
                        if (previous.Length == 0) break;
                    }

                    lines.RemoveRange(start, i - start + 1);
                    changed = true;
                }

                if (!changed) return;
                System.IO.File.WriteAllLines(configPath, lines.ToArray());
            }
            catch { }
        }
    }
}
