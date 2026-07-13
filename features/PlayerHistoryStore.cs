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
private static void UpsertPlayerHistory(PlayerControl pc)
        {
            try
            {
                if (pc == null || pc.Data == null || pc.Data.Disconnected) return;
                EnsurePlayerHistoryLoaded();
                SafePlayerIdentitySnapshot snapshot;
                bool hasSnapshot = TryGetSafeIdentity(pc, out snapshot);
                string name = hasSnapshot ? snapshot.Name : $"Player {pc.PlayerId}";
                string fc = hasSnapshot ? snapshot.FriendCode : "Hidden";
                string puid = hasSnapshot ? snapshot.Puid : "Unknown";
                string platform = hasSnapshot ? snapshot.Platform : "Unknown";
                string customPlatform = hasSnapshot ? snapshot.CustomPlatform : "";
                int level;
                if (!TryGetPlayerDisplayLevel(pc, hasSnapshot ? snapshot : null, out level))
                    level = 1;

                string key = BuildPlayerHistoryKey(pc.Data.ClientId, fc, puid, name);
                var item = FindPlayerHistoryEntry(key, pc.Data.ClientId, fc, puid, name);
                bool changed = false;
                if (item == null)
                {
                    item = new PlayerHistoryEntry
                    {
                        Name = name,
                        FriendCode = fc,
                        Puid = puid,
                        Platform = platform,
                        CustomPlatform = customPlatform,
                        Level = level,
                        FirstSeenUtc = DateTime.UtcNow,
                        LastSeenUtc = DateTime.UtcNow,
                        IsOnline = true
                    };
                    playerHistoryEntries.Add(item);
                    changed = true;
                }
                else
                {
                    bool nameChanged = IsDifferentHistoryName(item.Name, name);
                    changed = item.Name != name ||
                              item.FriendCode != fc ||
                              item.Puid != puid ||
                              item.Platform != platform ||
                              item.CustomPlatform != customPlatform ||
                              item.Level != level ||
                              !item.IsOnline ||
                              item.LeftUtc.HasValue;
                    if (nameChanged)
                    {
                        string previousName = item.Name;
                        item.Name = name;
                        changed = AddPreviousPlayerHistoryName(item, previousName) || changed;
                    }
                    item.Name = name;
                    item.FriendCode = fc;
                    item.Puid = puid;
                    item.Platform = platform;
                    item.CustomPlatform = customPlatform;
                    item.Level = level;
                    item.LastSeenUtc = DateTime.UtcNow;
                    item.LeftUtc = null;
                    item.IsOnline = true;
                }
                playerHistoryKeysById[pc.PlayerId] = key;
                playerHistoryKeysByClientId[pc.Data.ClientId] = key;
                IndexPlayerHistoryEntry(item, pc.Data.ClientId);
                if (changed) WritePlayerHistoryFile();
            }
            catch { }
        }

private static bool TryGetSafeIdentity(PlayerControl player, out SafePlayerIdentitySnapshot snapshot)
        {
            snapshot = null;
            if (player == null) return false;

            try
            {
                NetworkedPlayerInfo data = player.Data;
                int clientId = data != null ? data.ClientId : -1;

                if (safeIdentityByPlayerId.TryGetValue(player.PlayerId, out snapshot))
                {
                    if (clientId >= 0 && snapshot.ClientId != clientId)
                    {
                        safeIdentityByPlayerId.Remove(player.PlayerId);
                        snapshot = null;
                    }
                    else
                    {
                    if (!IsSafeIdentityComplete(snapshot)) TryRefreshSafeIdentity(player, snapshot.ClientId);
                    safeIdentityByPlayerId.TryGetValue(player.PlayerId, out snapshot);
                    return snapshot != null;
                    }
                }
                if (data != null && safeIdentityByClientId.TryGetValue(data.ClientId, out snapshot))
                {
                    safeIdentityByPlayerId[player.PlayerId] = snapshot;
                    snapshot.PlayerId = player.PlayerId;
                    if (!IsSafeIdentityComplete(snapshot)) TryRefreshSafeIdentity(player, data.ClientId);
                    return true;
                }

                if (data != null)
                {
                    TryRefreshSafeIdentity(player, data.ClientId);
                    if (safeIdentityByClientId.TryGetValue(data.ClientId, out snapshot)) return true;
                }
            }
            catch { }

            snapshot = null;
            return false;
        }

private static bool IsSafeIdentityComplete(SafePlayerIdentitySnapshot snapshot)
        {
            return snapshot != null &&
                   !string.IsNullOrWhiteSpace(snapshot.Name) && snapshot.Name != "Unknown" &&
                   !string.IsNullOrWhiteSpace(snapshot.FriendCode) && snapshot.FriendCode != "Hidden" &&
                   !string.IsNullOrWhiteSpace(snapshot.Puid) && snapshot.Puid != "Unknown" &&
                   snapshot.Level > 0;
        }

private static bool TryGetPlayerDisplayLevel(PlayerControl player, SafePlayerIdentitySnapshot snapshot, out int level)
        {
            level = 0;

            try
            {
                if (player != null && player.Data != null)
                {
                    uint rawLevel = player.Data.PlayerLevel;
                    if (rawLevel != uint.MaxValue && rawLevel < 10000)
                    {
                        level = (int)rawLevel + 1;
                        return true;
                    }
                }
            }
            catch { }

            try
            {
                ClientData client = AmongUsClient.Instance?.GetClientFromCharacter(player);
                if (client != null)
                {
                    uint rawLevel = client.PlayerLevel;
                    if (rawLevel != uint.MaxValue && rawLevel < 10000)
                    {
                        level = (int)rawLevel + 1;
                        return true;
                    }
                }
            }
            catch { }

            if (snapshot != null && snapshot.Level > 0)
            {
                level = snapshot.Level;
                return true;
            }

            return false;
        }

private static string BuildPlayerHistoryKey(int clientId, string friendCode, string puid, string name)
        {
            string normalizedPuid = NormalizeHistoryIdentity(puid);
            if (!string.IsNullOrEmpty(normalizedPuid) && normalizedPuid != "unknown")
                return $"puid:{normalizedPuid}";

            string normalizedFriendCode = NormalizeHistoryIdentity(friendCode);
            if (!string.IsNullOrEmpty(normalizedFriendCode) && normalizedFriendCode != "hidden" && normalizedFriendCode != "unknown")
                return $"fc:{normalizedFriendCode}";

            string normalizedName = NormalizeHistoryIdentity(name);
            return $"client:{clientId}|name:{normalizedName}";
        }

private static string NormalizeHistoryIdentity(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            return Regex.Replace(value.Trim(), "<.*?>", string.Empty).Trim().ToLowerInvariant();
        }

private static bool IsDifferentHistoryName(string oldName, string newName)
        {
            string oldKey = NormalizeHistoryIdentity(oldName);
            string newKey = NormalizeHistoryIdentity(newName);
            return !string.IsNullOrEmpty(oldKey) && !string.IsNullOrEmpty(newKey) && oldKey != newKey;
        }

private static bool AddPreviousPlayerHistoryName(PlayerHistoryEntry entry, string previousName)
        {
            if (entry == null || string.IsNullOrWhiteSpace(previousName)) return false;

            string normalizedPrevious = NormalizeHistoryIdentity(previousName);
            if (string.IsNullOrEmpty(normalizedPrevious) || normalizedPrevious == NormalizeHistoryIdentity(entry.Name))
                return false;

            if (entry.PreviousNames == null)
                entry.PreviousNames = new List<string>();

            int existingIndex = entry.PreviousNames.FindIndex(x => NormalizeHistoryIdentity(x) == normalizedPrevious);
            if (existingIndex == 0)
                return false;
            if (existingIndex > 0)
                entry.PreviousNames.RemoveAt(existingIndex);

            entry.PreviousNames.Insert(0, previousName.Trim());
            while (entry.PreviousNames.Count > 6)
                entry.PreviousNames.RemoveAt(entry.PreviousNames.Count - 1);
            return true;
        }

private static void MergePreviousPlayerHistoryNames(PlayerHistoryEntry target, PlayerHistoryEntry source)
        {
            if (target == null || source == null || source.PreviousNames == null) return;

            for (int i = source.PreviousNames.Count - 1; i >= 0; i--)
                AddPreviousPlayerHistoryName(target, source.PreviousNames[i]);
        }

private static string FormatPlayerHistoryDisplayName(PlayerHistoryEntry entry)
        {
            if (entry == null) return string.Empty;
            if (entry.PreviousNames == null || entry.PreviousNames.Count == 0)
                return entry.Name;

            string previous = string.Join(", ", entry.PreviousNames.Where(x => IsDifferentHistoryName(entry.Name, x)).Take(3).ToArray());
            return string.IsNullOrWhiteSpace(previous) ? entry.Name : $"{entry.Name} ({previous})";
        }

private static string FormatPreviousPlayerHistoryNames(PlayerHistoryEntry entry)
        {
            if (entry == null || entry.PreviousNames == null || entry.PreviousNames.Count == 0)
                return "none";

            string[] names = entry.PreviousNames
                .Where(x => IsDifferentHistoryName(entry.Name, x))
                .Take(6)
                .Select(x => x.Replace("|", " ").Replace("\r", " ").Replace("\n", " ").Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToArray();
            return names.Length == 0 ? "none" : string.Join(" | ", names);
        }

private static PlayerHistoryEntry FindPlayerHistoryEntry(string key, int clientId, string friendCode, string puid, string name)
        {
            if (string.IsNullOrWhiteSpace(key)) return null;

            if (clientId >= 0 &&
                playerHistoryKeysByClientId.TryGetValue(clientId, out string clientKey) &&
                playerHistoryEntryLookup.TryGetValue(clientKey, out PlayerHistoryEntry clientEntry))
                return clientEntry;

            if (playerHistoryEntryLookup.TryGetValue(key, out PlayerHistoryEntry direct))
                return direct;

            PlayerHistoryEntry found = playerHistoryEntries.FirstOrDefault(x =>
                BuildPlayerHistoryKey(clientId, x.FriendCode, x.Puid, x.Name) == key ||
                (!string.IsNullOrWhiteSpace(puid) && puid != "Unknown" && x.Puid == puid) ||
                (!string.IsNullOrWhiteSpace(friendCode) && friendCode != "Hidden" && x.FriendCode == friendCode && x.Name == name));
            if (found != null)
                IndexPlayerHistoryEntry(found, clientId);
            return found;
        }

private static bool IsLocalClientId(int clientId)
        {
            try
            {
                return PlayerControl.LocalPlayer != null &&
                       PlayerControl.LocalPlayer.Data != null &&
                       PlayerControl.LocalPlayer.Data.ClientId == clientId;
            }
            catch { return false; }
        }

private static void TryRefreshSafeIdentity(PlayerControl player, int clientId)
        {
            if (player == null || clientId < 0) return;

            int attempts;
            safeIdentityCaptureAttempts.TryGetValue(clientId, out attempts);
            if (attempts >= 6) return;

            float now = Time.realtimeSinceStartup;
            float nextAt;
            if (safeIdentityNextCaptureAt.TryGetValue(clientId, out nextAt) && now < nextAt) return;

            safeIdentityCaptureAttempts[clientId] = attempts + 1;
            safeIdentityNextCaptureAt[clientId] = now + 0.75f;
            try
            {
                ClientData client = AmongUsClient.Instance?.GetClientFromCharacter(player);
                if (client == null) return;
                CaptureSafeIdentity(client);
                SafePlayerIdentitySnapshot refreshed;
                if (safeIdentityByClientId.TryGetValue(clientId, out refreshed) && IsSafeIdentityComplete(refreshed))
                    safeIdentityCaptureAttempts[clientId] = 6;
            }
            catch { }
        }

private static void CaptureSafeIdentity(ClientData client)
        {
            if (client == null) return;

            try
            {
                int clientId = client.Id;
                SafePlayerIdentitySnapshot snapshot;
                if (!safeIdentityByClientId.TryGetValue(clientId, out snapshot))
                    snapshot = new SafePlayerIdentitySnapshot();
                snapshot.ClientId = clientId;

                string name = client.PlayerName;
                string friendCode = client.FriendCode;
                string puid = client.ProductUserId;
                if (!string.IsNullOrWhiteSpace(name)) snapshot.Name = name;
                if (!string.IsNullOrWhiteSpace(friendCode)) snapshot.FriendCode = friendCode;
                if (!string.IsNullOrWhiteSpace(puid)) snapshot.Puid = puid;
                snapshot.Platform = GetPlatform(client);
                snapshot.CustomPlatform = GetCustomPlatformName(client);

                uint rawLevel = client.PlayerLevel;
                if (rawLevel != uint.MaxValue && rawLevel < 10000) snapshot.Level = (int)rawLevel + 1;

                PlayerControl character = client.Character;
                if (character != null)
                {
                    snapshot.PlayerId = character.PlayerId;
                    safeIdentityByPlayerId[snapshot.PlayerId] = snapshot;
                }

                safeIdentityByClientId[snapshot.ClientId] = snapshot;
            }
            catch { }
        }

[HarmonyPatch(typeof(AmongUsClient), "OnPlayerJoined")]
        public static class PlayerHistory_OnPlayerJoined_SafeSnapshot_Patch
        {
            public static void Postfix([HarmonyArgument(0)] ClientData client)
            {
                if (client != null)
                {
                    safeIdentityCaptureAttempts[client.Id] = 0;
                    safeIdentityNextCaptureAt[client.Id] = Time.realtimeSinceStartup + 0.25f;
                }
                CaptureSafeIdentity(client);
            }
        }

[HarmonyPatch(typeof(AmongUsClient), "OnPlayerLeft")]
        public static class PlayerHistory_OnPlayerLeft_SafeSnapshot_Patch
        {
            public static void Prefix([HarmonyArgument(0)] ClientData client)
            {
                if (client == null) return;
                try
                {
                    int clientId = client.Id;
                    SafePlayerIdentitySnapshot snapshot;
                    if (safeIdentityByClientId.TryGetValue(clientId, out snapshot))
                    {
                        safeIdentityByClientId.Remove(clientId);
                        if (snapshot.PlayerId != byte.MaxValue) safeIdentityByPlayerId.Remove(snapshot.PlayerId);
                    }
                    safeIdentityCaptureAttempts.Remove(clientId);
                    safeIdentityNextCaptureAt.Remove(clientId);
                }
                catch { }
            }
        }

private static string GetCustomPlatformName(ClientData client)
        {
            try
            {
                string value = client?.PlatformData?.PlatformName;
                if (string.IsNullOrWhiteSpace(value)) return "";
                value = Regex.Replace(value, "<.*?>", string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(value)) return "";
                if ((int)client.PlatformData.Platform == 112) return "";

                string platform = GetPlatform(client);
                if (value.Equals(platform, StringComparison.OrdinalIgnoreCase)) return "";
                if (value.Equals(client.PlatformData.Platform.ToString(), StringComparison.OrdinalIgnoreCase)) return "";
                return value;
            }
            catch { return ""; }
        }

public static string GetClientPuid(ClientData client)
        {
            if (client == null) return "Unknown";

            try
            {
                PlayerControl character = client.Character;
                return GetPlayerPuid(character);
            }
            catch { return "Unknown"; }
        }

public static string GetPlayerPuid(PlayerControl player)
        {
            if (player == null) return "Unknown";

            try
            {
                string puid = player.Puid;
                return string.IsNullOrWhiteSpace(puid) ? "Unknown" : puid.Trim();
            }
            catch { return "Unknown"; }
        }

private static string FormatPlatformHistory(PlayerHistoryEntry entry)
        {
            if (entry == null) return "Unknown";
            return string.IsNullOrWhiteSpace(entry.CustomPlatform)
                ? entry.Platform
                : $"{entry.Platform} + custom: {entry.CustomPlatform}";
        }

private static void InvalidatePlayerHistoryViewCache()
        {
            playerHistoryViewDirty = true;
        }

private static void IndexPlayerHistoryEntry(PlayerHistoryEntry entry, int clientId = -1)
        {
            if (entry == null) return;

            string key = BuildPlayerHistoryKey(clientId, entry.FriendCode, entry.Puid, entry.Name);
            if (!string.IsNullOrWhiteSpace(key))
                playerHistoryEntryLookup[key] = entry;

            string puid = NormalizeHistoryIdentity(entry.Puid);
            if (!string.IsNullOrEmpty(puid) && puid != "unknown")
                playerHistoryEntryLookup[$"puid:{puid}"] = entry;

            string friendCode = NormalizeHistoryIdentity(entry.FriendCode);
            if (!string.IsNullOrEmpty(friendCode) && friendCode != "hidden" && friendCode != "unknown")
                playerHistoryEntryLookup[$"fc:{friendCode}"] = entry;
        }

private static void RebuildPlayerHistoryViewCache()
        {
            if (!playerHistoryViewDirty) return;

            playerHistoryViewRows.Clear();
            foreach (var e in playerHistoryEntries.OrderByDescending(x => x.LastSeenUtc))
            {
                string status = e.IsOnline ? "<color=#55FF77>ONLINE</color>" : "<color=#aaaaaa>LEFT</color>";
                playerHistoryViewRows.Add(new PlayerHistoryViewRow
                {
                    Header = $"{FormatPlayerHistoryDisplayName(e)}  {status}",
                    Identity = $"Lv: {e.Level} | FC: {e.FriendCode} | PUID: {e.Puid}",
                    Times = $"Joined: {e.FirstSeenUtc:HH:mm:ss} | Left: {(e.LeftUtc.HasValue ? e.LeftUtc.Value.ToString("HH:mm:ss") : "online")}",
                    Platform = $"Platform: {FormatPlatformHistory(e)}",
                    Rpc = $"RPC: {FormatRpcHistory(e)}"
                });
            }

            playerHistoryViewDirty = false;
        }

private static string PlayerHistoryFilePath()
        {
            string folder = string.IsNullOrWhiteSpace(Plugin.ElysiumFolder)
                ? System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "ElysiumModMenu")
                : Plugin.ElysiumFolder;
            return System.IO.Path.Combine(folder, "ElysiumPlayerHistory.txt");
        }

private static void EnsurePlayerHistoryLoaded()
        {
            if (playerHistoryLoaded) return;
            playerHistoryLoaded = true;

            try
            {
                string path = PlayerHistoryFilePath();
                if (!System.IO.File.Exists(path)) return;

                PlayerHistoryEntry current = null;
                foreach (string rawLine in System.IO.File.ReadLines(path, Encoding.UTF8))
                {
                    string line = rawLine ?? string.Empty;
                    if (line.StartsWith("Nick: "))
                    {
                        AddLoadedPlayerHistoryEntry(current);
                        current = new PlayerHistoryEntry
                        {
                            Name = line.Substring("Nick: ".Length).Trim(),
                            FriendCode = "Hidden",
                            Puid = "Unknown",
                            Platform = "Unknown",
                            CustomPlatform = "",
                            Level = 1,
                            FirstSeenUtc = DateTime.UtcNow,
                            LastSeenUtc = DateTime.UtcNow,
                            IsOnline = false
                        };
                    }
                    else if (current != null && line.StartsWith("Level: "))
                    {
                        int level;
                        if (int.TryParse(line.Substring("Level: ".Length).Trim(), out level))
                            current.Level = level;
                    }
                    else if (current != null && line.StartsWith("FriendCode: "))
                    {
                        current.FriendCode = line.Substring("FriendCode: ".Length).Trim();
                    }
                    else if (current != null && line.StartsWith("Previous Nicks: "))
                    {
                        current.PreviousNames.Clear();
                        string value = line.Substring("Previous Nicks: ".Length).Trim();
                        if (!string.IsNullOrWhiteSpace(value) && !value.Equals("none", StringComparison.OrdinalIgnoreCase))
                        {
                            string[] parts = value.Split('|');
                            for (int i = parts.Length - 1; i >= 0; i--)
                                AddPreviousPlayerHistoryName(current, parts[i]);
                        }
                    }
                    else if (current != null && line.StartsWith("PUID: "))
                    {
                        current.Puid = line.Substring("PUID: ".Length).Trim();
                    }
                    else if (current != null && line.StartsWith("Joined UTC: "))
                    {
                        DateTime parsed;
                        if (DateTime.TryParse(line.Substring("Joined UTC: ".Length).Trim(), out parsed))
                            current.FirstSeenUtc = parsed;
                    }
                    else if (current != null && line.StartsWith("Left UTC: "))
                    {
                        string value = line.Substring("Left UTC: ".Length).Trim();
                        DateTime parsed;
                        if (!value.Equals("online", StringComparison.OrdinalIgnoreCase) && DateTime.TryParse(value, out parsed))
                        {
                            current.LeftUtc = parsed;
                            current.LastSeenUtc = parsed;
                        }
                        else
                        {
                            current.LastSeenUtc = current.FirstSeenUtc;
                        }
                    }
                    else if (current != null && line.StartsWith("Platform: "))
                    {
                        string platform = line.Substring("Platform: ".Length).Trim();
                        const string customPrefix = " + custom: ";
                        int customIndex = platform.IndexOf(customPrefix, StringComparison.Ordinal);
                        if (customIndex >= 0)
                        {
                            current.Platform = platform.Substring(0, customIndex);
                            current.CustomPlatform = platform.Substring(customIndex + customPrefix.Length);
                        }
                        else
                        {
                            current.Platform = platform;
                        }
                    }
                    else if (current != null && line.StartsWith("RPC calls: "))
                    {
                        string value = line.Substring("RPC calls: ".Length).Trim();
                        current.RpcCalls.Clear();
                        foreach (string part in value.Split(','))
                        {
                            byte rpc;
                            if (byte.TryParse(part.Trim(), out rpc) && !current.RpcCalls.Contains(rpc))
                                current.RpcCalls.Add(rpc);
                        }
                        current.RpcCalls.Sort();
                    }
                }

                AddLoadedPlayerHistoryEntry(current);
                InvalidatePlayerHistoryViewCache();
            }
            catch { }
        }

private static void AddLoadedPlayerHistoryEntry(PlayerHistoryEntry entry)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.Name)) return;

            string key = BuildPlayerHistoryKey(-1, entry.FriendCode, entry.Puid, entry.Name);
            var existing = FindPlayerHistoryEntry(key, -1, entry.FriendCode, entry.Puid, entry.Name);
            if (existing == null)
            {
                playerHistoryEntries.Add(entry);
                IndexPlayerHistoryEntry(entry);
                InvalidatePlayerHistoryViewCache();
                return;
            }

            if (entry.LastSeenUtc > existing.LastSeenUtc)
            {
                if (IsDifferentHistoryName(existing.Name, entry.Name))
                {
                    string previousName = existing.Name;
                    existing.Name = entry.Name;
                    AddPreviousPlayerHistoryName(existing, previousName);
                }
                MergePreviousPlayerHistoryNames(existing, entry);
                existing.Name = entry.Name;
                existing.FriendCode = entry.FriendCode;
                existing.Puid = entry.Puid;
                existing.Platform = entry.Platform;
                existing.CustomPlatform = entry.CustomPlatform;
                existing.Level = entry.Level;
                existing.FirstSeenUtc = existing.FirstSeenUtc < entry.FirstSeenUtc ? existing.FirstSeenUtc : entry.FirstSeenUtc;
                existing.LastSeenUtc = entry.LastSeenUtc;
                existing.LeftUtc = entry.LeftUtc;
                existing.IsOnline = false;
                InvalidatePlayerHistoryViewCache();
            }
            else
            {
                MergePreviousPlayerHistoryNames(existing, entry);
            }

            foreach (byte rpc in entry.RpcCalls)
            {
                if (!existing.RpcCalls.Contains(rpc))
                    existing.RpcCalls.Add(rpc);
            }
            existing.RpcCalls.Sort();
            IndexPlayerHistoryEntry(existing);
        }

private static void MarkPlayerHistoryLeft(byte playerId)
        {
            try
            {
                if (!playerHistoryKeysById.TryGetValue(playerId, out string key)) return;
                var item = FindPlayerHistoryEntry(key, -1, null, null, null);
                if (item == null || !item.IsOnline) return;

                item.IsOnline = false;
                item.LeftUtc = DateTime.UtcNow;
                item.LastSeenUtc = item.LeftUtc.Value;
                WritePlayerHistoryFile();
            }
            catch { }
        }

private static void MarkPlayerHistoryLeftByClientId(int clientId)
        {
            try
            {
                if (!playerHistoryKeysByClientId.TryGetValue(clientId, out string key)) return;
                var item = FindPlayerHistoryEntry(key, clientId, null, null, null);
                if (item == null || !item.IsOnline) return;

                item.IsOnline = false;
                item.LeftUtc = DateTime.UtcNow;
                item.LastSeenUtc = item.LeftUtc.Value;
                playerHistoryKeysByClientId.Remove(clientId);
                WritePlayerHistoryFile();
            }
            catch { }
        }

public static void RecordPlayerRpc(PlayerControl pc, byte callId)
        {
            try
            {
                if (VanillaRpcIds.Contains(callId)) return;
                if (pc == null || pc.Data == null) return;
                UpsertPlayerHistory(pc);

                if (!playerHistoryKeysById.TryGetValue(pc.PlayerId, out string key)) return;
                var item = FindPlayerHistoryEntry(key, pc.Data.ClientId, null, null, null);
                if (item == null) return;

                if (!item.RpcCalls.Contains(callId))
                {
                    item.RpcCalls.Add(callId);
                    item.RpcCalls.Sort();
                    WritePlayerHistoryFile();
                }
            }
            catch { }
        }

private static string FormatRpcHistory(PlayerHistoryEntry entry)
        {
            if (entry == null || entry.RpcCalls == null || entry.RpcCalls.Count == 0) return "нет";
            byte[] customRpcCalls = entry.RpcCalls.Where(x => !VanillaRpcIds.Contains(x)).Distinct().OrderBy(x => x).ToArray();
            if (customRpcCalls.Length == 0) return "нет";
            return string.Join(", ", customRpcCalls.Select(x => x.ToString()).ToArray());
        }

private static void WritePlayerHistoryFile()
        {
            InvalidatePlayerHistoryViewCache();

            try
            {
                string path = PlayerHistoryFilePath();
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));

                List<string> lines = new List<string>
                {
                    "ElysiumModMenu Player History",
                    $"Updated UTC: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}",
                    ""
                };

                foreach (var e in playerHistoryEntries.OrderByDescending(x => x.LastSeenUtc))
                {
                    string left = e.LeftUtc.HasValue ? e.LeftUtc.Value.ToString("yyyy-MM-dd HH:mm:ss") : "online";
                    lines.Add($"Nick: {e.Name}");
                    lines.Add($"Previous Nicks: {FormatPreviousPlayerHistoryNames(e)}");
                    lines.Add($"Level: {e.Level}");
                    lines.Add($"FriendCode: {e.FriendCode}");
                    lines.Add($"PUID: {e.Puid}");
                    lines.Add($"Joined UTC: {e.FirstSeenUtc:yyyy-MM-dd HH:mm:ss}");
                    lines.Add($"Left UTC: {left}");
                    lines.Add($"Platform: {FormatPlatformHistory(e)}");
                    lines.Add($"RPC calls: {FormatRpcHistory(e)}");
                    lines.Add(new string('-', 48));
                }

                System.IO.File.WriteAllLines(path, lines.ToArray(), Encoding.UTF8);
            }
            catch { }
        }
    }
}
