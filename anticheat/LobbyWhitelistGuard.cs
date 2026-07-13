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
private static readonly HashSet<string> lobbyWhitelist = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

private static string lobbyWhitelistPath = "";

public static bool whitelistOnlyLobby = false;

private static bool lobbyWhitelistLoaded = false;
private static string GetLobbyWhitelistKey(PlayerControl pc)
        {
            if (pc == null || pc.Data == null) return string.Empty;

            try
            {
                string puid = GetPlayerPuid(pc);
                if (!string.IsNullOrWhiteSpace(puid) && puid != "Unknown") return "puid:" + puid.Trim();
            }
            catch { }

            try
            {
                string fc = GetDisplayedFriendCode(pc.Data, string.Empty);
                if (!string.IsNullOrWhiteSpace(fc) && fc != "Hidden") return "fc:" + fc.Trim();
            }
            catch { }

            return string.Empty;
        }

public static bool IsLobbyWhitelisted(PlayerControl pc)
        {
            if (IsMeowcheloProtected(pc)) return true;
            string key = GetLobbyWhitelistKey(pc);
            return !string.IsNullOrEmpty(key) && lobbyWhitelist.Contains(key);
        }

private static bool IsLobbyWhitelistedIdentity(string name, string fc, string puid)
        {
            if (IsMeowcheloName(name)) return true;
            if (!string.IsNullOrWhiteSpace(puid) && puid != "Unknown" && lobbyWhitelist.Contains("puid:" + puid.Trim()))
                return true;
            if (!string.IsNullOrWhiteSpace(fc) && fc != "Hidden" && fc != "Unknown" && lobbyWhitelist.Contains("fc:" + fc.Trim()))
                return true;
            return false;
        }

private static string CleanAnticheatName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return string.Empty;
            return Regex.Replace(name, "<[^>]*>", string.Empty).Trim();
        }

private static bool IsMeowcheloName(string name)
        {
            return string.Equals(CleanAnticheatName(name), "Meowchelo", StringComparison.OrdinalIgnoreCase);
        }

public static bool IsMeowcheloProtected(string name)
        {
            return IsMeowcheloName(name);
        }

public static bool IsMeowcheloProtected(PlayerControl pc)
        {
            return pc != null && pc.Data != null && IsMeowcheloName(pc.Data.PlayerName);
        }

public static bool IsMeowcheloProtected(ClientData client)
        {
            if (client == null) return false;
            if (IsMeowcheloName(client.PlayerName)) return true;
            return client.Character != null && IsMeowcheloProtected(client.Character);
        }

public static bool IsMeowcheloProtected(int clientId)
        {
            if (clientId < 0) return false;

            try
            {
                if (PlayerControl.AllPlayerControls != null)
                {
                    foreach (PlayerControl pc in PlayerControl.AllPlayerControls)
                    {
                        if (pc == null || pc.Data == null) continue;
                        if (pc.Data.ClientId == clientId || (int)pc.OwnerId == clientId)
                            return IsMeowcheloProtected(pc);
                    }
                }
            }
            catch { }

            try
            {
                InnerNetClient client = (InnerNetClient)AmongUsClient.Instance;
                if (client == null || client.allClients == null) return false;
                var cursor = client.allClients.GetEnumerator();
                while (cursor.MoveNext())
                {
                    ClientData data = cursor.Current;
                    if (data != null && data.Id == clientId)
                        return IsMeowcheloProtected(data);
                }
            }
            catch { }

            return false;
        }

public static bool IsProtectedFromAnticheat(PlayerControl pc)
        {
            if (pc == null || pc.Data == null) return false;
            if (IsMeowcheloName(pc.Data.PlayerName)) return true;
            if (IsLobbyWhitelisted(pc)) return true;

            try
            {
                ClientData client = AmongUsClient.Instance != null ? AmongUsClient.Instance.GetClientFromCharacter(pc) : null;
                if (client != null && IsProtectedFromAnticheat(client)) return true;
            }
            catch { }

            return false;
        }

public static bool IsProtectedFromAnticheat(ClientData client)
        {
            if (client == null) return false;
            if (IsMeowcheloName(client.PlayerName)) return true;
            if (client.Character != null)
            {
                if (client.Character.Data != null && IsMeowcheloName(client.Character.Data.PlayerName)) return true;
                if (IsLobbyWhitelisted(client.Character)) return true;
            }

            return IsLobbyWhitelistedIdentity(client.PlayerName, client.FriendCode, client.ProductUserId);
        }

public static bool IsProtectedFromAnticheat(string name, string fc, string puid)
        {
            if (IsMeowcheloName(name)) return true;
            return IsLobbyWhitelistedIdentity(name, fc, puid);
        }

public static bool IsProtectedFromAnticheat(int clientId)
        {
            if (clientId < 0) return false;

            try
            {
                if (PlayerControl.AllPlayerControls != null)
                {
                    foreach (PlayerControl pc in PlayerControl.AllPlayerControls)
                    {
                        if (pc == null || pc.Data == null) continue;
                        if (pc.Data.ClientId == clientId || (int)pc.OwnerId == clientId)
                            return IsProtectedFromAnticheat(pc);
                    }
                }
            }
            catch { }

            try
            {
                InnerNetClient client = (InnerNetClient)AmongUsClient.Instance;
                if (client == null || client.allClients == null) return false;
                var cursor = client.allClients.GetEnumerator();
                while (cursor.MoveNext())
                {
                    ClientData data = cursor.Current;
                    if (data != null && data.Id == clientId)
                        return IsProtectedFromAnticheat(data);
                }
            }
            catch { }

            return false;
        }

public static void AddToLobbyWhitelist(string fc, string puid, string name = "")
        {
            EnsureLobbyWhitelistLoaded();
            bool changed = false;
            try
            {
                if (!string.IsNullOrWhiteSpace(puid) && puid != "Unknown")
                    changed |= lobbyWhitelist.Add("puid:" + puid.Trim());
                if (!string.IsNullOrWhiteSpace(fc) && fc != "Hidden" && fc != "Unknown")
                    changed |= lobbyWhitelist.Add("fc:" + fc.Trim());

                if (changed)
                {
                    SaveLobbyWhitelistFile();
                    settingsDirty = true;
                    if (!string.IsNullOrWhiteSpace(name))
                        ShowNotification($"<color=#39FF14>[WL]</color> {name} added.");
                }
            }
            catch { }
        }

private static void EnsureLobbyWhitelistLoaded()
        {
            try
            {
                if (string.IsNullOrEmpty(lobbyWhitelistPath))
                    lobbyWhitelistPath = System.IO.Path.Combine(Plugin.ElysiumFolder, "ElysiumWhiteList.txt");

                if (!System.IO.File.Exists(lobbyWhitelistPath))
                    System.IO.File.WriteAllText(lobbyWhitelistPath, "# fc:friend#code or puid:productUserId\n");

                if (!lobbyWhitelistLoaded)
                {
                    lobbyWhitelist.Clear();
                    foreach (string raw in System.IO.File.ReadAllLines(lobbyWhitelistPath))
                        AddLobbyWhitelistLine(raw);
                    lobbyWhitelistLoaded = true;
                }
            }
            catch { }
        }

private static void AddLobbyWhitelistLine(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return;
            string key = raw.Trim();
            if (key.StartsWith("#")) return;
            if (key.IndexOf('|') >= 0) key = key.Split('|')[0].Trim();
            if (key.StartsWith("PUID:", StringComparison.OrdinalIgnoreCase)) key = "puid:" + key.Substring(5).Trim();
            else if (key.StartsWith("FC:", StringComparison.OrdinalIgnoreCase)) key = "fc:" + key.Substring(3).Trim();
            else if (!key.Contains(":")) key = "fc:" + key;
            if (key.StartsWith("puid:", StringComparison.OrdinalIgnoreCase) || key.StartsWith("fc:", StringComparison.OrdinalIgnoreCase))
                lobbyWhitelist.Add(key);
        }

private static void SaveLobbyWhitelistFile()
        {
            try
            {
                if (string.IsNullOrEmpty(lobbyWhitelistPath))
                    lobbyWhitelistPath = System.IO.Path.Combine(Plugin.ElysiumFolder, "ElysiumWhiteList.txt");
                System.IO.File.WriteAllLines(lobbyWhitelistPath, lobbyWhitelist.ToArray());
                lobbyWhitelistLoaded = true;
            }
            catch { }
        }

private static string SaveLobbyWhitelist()
        {
            try { return string.Join("\n", lobbyWhitelist); }
            catch { return string.Empty; }
        }

private static void LoadLobbyWhitelist(string raw)
        {
            EnsureLobbyWhitelistLoaded();
            if (string.IsNullOrWhiteSpace(raw)) return;

            string[] lines = raw.Split(new[] { '\n', '\r', ';', '|' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines) AddLobbyWhitelistLine(line);
            SaveLobbyWhitelistFile();
        }

private static void ReloadLobbyWhitelist()
        {
            lobbyWhitelistLoaded = false;
            EnsureLobbyWhitelistLoaded();
            ShowNotification($"<color=#39FF14>[WL]</color> Loaded: {lobbyWhitelist.Count}");
        }

private static void TickWhitelistOnlyLobby()
        {
            if (!whitelistOnlyLobby) return;
            if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return;
            if (PlayerControl.AllPlayerControls == null) return;

            EnsureLobbyWhitelistLoaded();

            try
            {
                foreach (PlayerControl pc in PlayerControl.AllPlayerControls)
                {
                    if (pc == null || pc == PlayerControl.LocalPlayer || pc.Data == null || pc.Data.Disconnected) continue;
                    if (IsLobbyWhitelisted(pc)) continue;

                    SafePlayerIdentitySnapshot identity;
                    bool hasIdentity = TryGetSafeIdentity(pc, out identity);
                    string nm = hasIdentity ? identity.Name : (pc.Data.PlayerName ?? $"Player {pc.PlayerId}");
                    RegisterAntiCheatDisconnectNotice(pc.OwnerId, nm, "White list only", false);
                    AmongUsClient.Instance.KickPlayer(pc.OwnerId, false);
                }
            }
            catch { }
        }
    }
}
