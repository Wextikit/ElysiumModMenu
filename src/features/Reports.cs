#nullable disable
#pragma warning disable CS0162, CS0108, CS0219, CS0661, CS0660, CS8632, CS0168, CS0659
using AmongUs.Data.Player;
using AmongUs.GameOptions;
using AmongUs.InnerNet.GameDataMessages;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
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

        private sealed class RawSevereLogEntry
        {
            public string Title;
            public string Source;
            public string Level;
            public string Text;
            public DateTime LastSeenUtc;
            public DateTime LastSummaryUtc;
            public int RepeatedCount;
        }

        private static readonly Dictionary<string, RawSevereLogEntry> rawSevereLogEntries = new Dictionary<string, RawSevereLogEntry>();

public static Sprite LoadEmbeddedSprite(string fileName, float pixelsPerUnit = 1f)
        {
            string path = $"ElysiumModMenu.{fileName}";

            try
            {
                if (CachedSprites.TryGetValue(path + pixelsPerUnit, out var cachedSprite))
                    return cachedSprite;

                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                using var stream = assembly.GetManifestResourceStream(path);

                if (stream == null)
                {
                    return null;
                }

                var texture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
                using System.IO.MemoryStream ms = new System.IO.MemoryStream();
                stream.CopyTo(ms);

                ImageConversion.LoadImage(texture, ms.ToArray(), false);

                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);

                sprite.hideFlags |= HideFlags.HideAndDontSave | HideFlags.DontSaveInEditor;

                return CachedSprites[path + pixelsPerUnit] = sprite;
            }
            catch (System.Exception)
            {
                return null;
            }
        }
        public void Start()
        {
            activeGui = this;
            if (enableBackground) LoadBackgroundImage();
            LoadConfig();
            LoadBanList();
            LoadBotBanList();
            ClearSpamErrorLogOnStartup();
            StartBackgroundAnomalyLogMonitor();


            try
            {
                int starts = UnityEngine.PlayerPrefs.GetInt("Elysium_GameStarts", 0);
                starts++;

                string chatLogPath = System.IO.Path.Combine(Plugin.ElysiumFolder, "ChatLog.txt");

                if (starts >= 3)
                {
                    if (System.IO.File.Exists(chatLogPath))
                    {
                        System.IO.File.WriteAllText(chatLogPath, string.Empty);
                    }
                    starts = 0;
                }

                UnityEngine.PlayerPrefs.SetInt("Elysium_GameStarts", starts);
                UnityEngine.PlayerPrefs.Save();
            }
            catch { }
        }

        public void OnApplicationQuit()
        {
            StopBackgroundAnomalyLogMonitor();
            SaveConfig();
        }

        public void OnDisable()
        {
            SaveConfig();
        }

        private static void ClearSpamErrorLogOnStartup()
        {
            try
            {
                watchedLogLineCounts.Clear();
                logBurstWindowStartedAt = -1f;
                logBurstCooldownUntil = 0f;
                logBurstLineCount = 0;
                anomalyLogWatchNotified = false;
                logMonitorNextScanAt = 0f;
                rawLogWindowStartedUtc = DateTime.MinValue;
                rawLogSpamNextAllowedUtc = DateTime.MinValue;
                rawLogWindowCount = 0;
                lock (rawLogDiagnosticLock)
                    rawSevereLogEntries.Clear();

                string root = string.IsNullOrWhiteSpace(Plugin.ElysiumFolder)
                    ? System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "ElysiumModMenu")
                    : Plugin.ElysiumFolder;

                if (!System.IO.Directory.Exists(root)) return;

                foreach (string file in System.IO.Directory.GetFiles(root, "SpamErrorLog*.txt", System.IO.SearchOption.AllDirectories))
                {
                    try { System.IO.File.Delete(file); }
                    catch { }
                }

                System.Console.WriteLine("[ElysiumModMenu] Cleared previous SpamErrorLog files and reset log monitor state.");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[ElysiumModMenu] Failed to clear SpamErrorLog files: {ex.GetType().Name}: {ex.Message}");
            }
        }

        public static void SendAnomalyAlert(string title, string message, string dedupeKey = null, bool waitForCompletion = false, IEnumerable<string> attachmentPaths = null)
        {
            if (!enableAnomalyLogReports) return;
            if (!DiscordStatusReporter.IsEnabled) return;
            string webhookUrl = DiscordStatusReporter.ConfiguredWebhookUrl;
            if (!DiscordStatusReporter.IsValidWebhookUrl(webhookUrl)) return;

            int attachmentCount = attachmentPaths == null ? 0 : attachmentPaths.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().Count();
            DiscordStatusReporter.WriteDiagnosticConsoleStatus($"[ElysiumModMenu] Sending freeze/overload logs to configured webhook. Type={title}. Attachments={attachmentCount}.");
            DiscordStatusReporter.SendDiagnosticAlert(webhookUrl, title, message, waitForCompletion, attachmentPaths);
        }

        public static void ObserveRawDiagnosticLog(LogEventArgs eventArgs)
        {
            if (!enableAnomalyLogReports || eventArgs == null) return;

            try
            {
                string source = eventArgs.Source?.SourceName ?? "Unknown";
                if (source.Equals("ElysiumModMenu", StringComparison.OrdinalIgnoreCase) ||
                    source.Equals("Console", StringComparison.OrdinalIgnoreCase))
                    return;

                string text = eventArgs.Data?.ToString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(text)) return;
                if (text.Length > 3000) text = text.Substring(0, 3000);

                bool isError = (eventArgs.Level & (LogLevel.Error | LogLevel.Fatal)) != 0;
                bool isWarning = (eventArgs.Level & LogLevel.Warning) != 0;
                if (isError || isWarning)
                {
                    string title = isError ? "Immediate error/crash log" : "Immediate warning log";
                    string key = $"{source}\n{eventArgs.Level}\n{text}";
                    bool sendImmediately = false;

                    lock (rawLogDiagnosticLock)
                    {
                        DateTime now = DateTime.UtcNow;
                        if (!rawSevereLogEntries.TryGetValue(key, out RawSevereLogEntry entry))
                        {
                            rawSevereLogEntries[key] = new RawSevereLogEntry
                            {
                                Title = title,
                                Source = source,
                                Level = eventArgs.Level.ToString(),
                                Text = text,
                                LastSeenUtc = now,
                                LastSummaryUtc = now
                            };
                            sendImmediately = true;
                        }
                        else
                        {
                            entry.LastSeenUtc = now;
                            entry.RepeatedCount++;
                        }
                    }

                    if (sendImmediately)
                    {
                        string message = $"{BuildAnomalyReportDetails(false)}\nsource={source}\nlevel={eventArgs.Level}\nlog={text}";
                        SendAnomalyAlert(title, message, isError ? "raw-error" : "raw-warning");
                    }
                    return;
                }

                bool reportSpam = false;
                int observedCount = 0;
                lock (rawLogDiagnosticLock)
                {
                    DateTime now = DateTime.UtcNow;
                    if (rawLogWindowStartedUtc == DateTime.MinValue ||
                        now - rawLogWindowStartedUtc > TimeSpan.FromSeconds(LogBurstWindowSeconds))
                    {
                        rawLogWindowStartedUtc = now;
                        rawLogWindowCount = 0;
                    }

                    rawLogWindowCount++;
                    if (rawLogWindowCount >= LogBurstLineThreshold && now >= rawLogSpamNextAllowedUtc)
                    {
                        observedCount = rawLogWindowCount;
                        rawLogWindowCount = 0;
                        rawLogWindowStartedUtc = now;
                        rawLogSpamNextAllowedUtc = now.AddSeconds(LogBurstAlertCooldownSeconds);
                        reportSpam = true;
                    }
                }

                if (reportSpam)
                {
                    string message = $"{BuildAnomalyReportDetails(false)}\nrawLogLines={observedCount}\nwindowSeconds={LogBurstWindowSeconds}\nthreshold={LogBurstLineThreshold}\nreason=raw log spam before console throttling";
                    SendAnomalyAlert("Abnormal raw log spam", message, "raw-log-spam");
                }
            }
            catch { }
        }

        private static void FlushRawSevereLogRepeats()
        {
            List<RawSevereLogEntry> summaries = new List<RawSevereLogEntry>();

            lock (rawLogDiagnosticLock)
            {
                DateTime now = DateTime.UtcNow;
                foreach (RawSevereLogEntry entry in rawSevereLogEntries.Values)
                {
                    if (entry.RepeatedCount <= 0 || now - entry.LastSummaryUtc < TimeSpan.FromSeconds(5))
                        continue;

                    summaries.Add(new RawSevereLogEntry
                    {
                        Title = entry.Title,
                        Source = entry.Source,
                        Level = entry.Level,
                        Text = entry.Text,
                        RepeatedCount = entry.RepeatedCount
                    });
                    entry.RepeatedCount = 0;
                    entry.LastSummaryUtc = now;
                }

                string[] expiredKeys = rawSevereLogEntries
                    .Where(pair => pair.Value.RepeatedCount == 0 && now - pair.Value.LastSeenUtc > TimeSpan.FromMinutes(1))
                    .Select(pair => pair.Key)
                    .ToArray();
                foreach (string key in expiredKeys)
                    rawSevereLogEntries.Remove(key);
            }

            foreach (RawSevereLogEntry entry in summaries)
            {
                string message = $"{BuildAnomalyReportDetails(false)}\nsource={entry.Source}\nlevel={entry.Level}\nrepeatedCount={entry.RepeatedCount}\nlog={entry.Text}";
                SendAnomalyAlert(entry.Title + " (repeated)", message, "raw-severe-repeat");
            }
        }

        private static void StartBackgroundAnomalyLogMonitor()
        {
            try
            {
                if (anomalyLogMonitorTimer != null) return;
                anomalyLogMonitorTimer = new System.Threading.Timer(_ => TryDetectLogBurstTick(false), null, 1000, 1000);
                System.Console.WriteLine("[ElysiumModMenu] Background freeze/overload log monitor started.");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[ElysiumModMenu] Failed to start background log monitor: {ex.GetType().Name}: {ex.Message}");
            }
        }

        private static void StopBackgroundAnomalyLogMonitor()
        {
            try
            {
                anomalyLogMonitorTimer?.Dispose();
                anomalyLogMonitorTimer = null;
            }
            catch { }
        }

        private static float GetLogMonitorSeconds()
        {
            try { return (float)(DateTime.UtcNow - logMonitorStartedUtc).TotalSeconds; }
            catch { return 0f; }
        }

        private static string BuildAnomalyReportDetails(bool allowUnityAccess = true)
        {
            if (!allowUnityAccess)
                return anomalyReportDetailsCache + "\nmonitor=background";

            string clientId = "Unknown";
            string networkMode = "Unknown";
            string inGame = "no";
            string host = "no";
            string platform = "Unknown";

            try
            {
                if (AmongUsClient.Instance != null)
                {
                    clientId = AmongUsClient.Instance.ClientId.ToString();
                    networkMode = AmongUsClient.Instance.NetworkMode.ToString();
                    host = AmongUsClient.Instance.AmHost ? "yes" : "no";

                    ClientData client = AmongUsClient.Instance.GetClientFromCharacter(PlayerControl.LocalPlayer);
                    if (client != null)
                    {
                        platform = GetPlatform(client);
                    }
                }
            }
            catch { }

            try { inGame = ShipStatus.Instance != null && LobbyBehaviour.Instance == null ? "yes" : "no"; } catch { }

            string details = $"sessionId={relaySessionId}\nclientId={clientId}\nnetworkMode={networkMode}\nhost={host}\nplatform={platform}\ninGame={inGame}\nmonitor=unity";
            anomalyReportDetailsCache = details;
            return details;
        }

        private static void TryDetectLogBurstTick(bool allowUnityAccess = true)
        {
            if (!enableAnomalyLogReports) return;
            FlushRawSevereLogRepeats();
            lock (anomalyLogMonitorLock)
            {
                try
                {
                    float now = GetLogMonitorSeconds();
                    if (now < logMonitorNextScanAt) return;
                    logMonitorNextScanAt = now + LogBurstScanIntervalSeconds;

                    List<string> watchedFiles = GetWatchedLogFiles().ToList();
                    if (!anomalyLogWatchNotified)
                    {
                        anomalyLogWatchNotified = true;
                        System.Console.WriteLine($"[ElysiumModMenu] Freeze/overload log reporting is enabled. Watching: {string.Join(", ", watchedFiles.Select(System.IO.Path.GetFileName).ToArray())}. Sends only summary counters when error/red logs appear or {LogBurstLineThreshold}+ new lines arrive within {LogBurstWindowSeconds:0}s.");
                    }

                    int newLines = 0;
                    int errorLines = 0;
                    int storedMsgLines = 0;
                    List<string> touchedLogs = new List<string>();
                    List<string> touchedLogFiles = new List<string>();
                    foreach (string file in watchedFiles)
                    {
                        List<string> addedLines = ReadNewLogLines(file, out int currentLines);
                        if (!watchedLogLineCounts.TryGetValue(file, out int previousLines))
                        {
                            watchedLogLineCounts[file] = currentLines;
                            addedLines = ReadInitialRecentLogTail(file);
                            if (addedLines.Count <= 0) continue;
                        }
                        else
                        {
                            watchedLogLineCounts[file] = currentLines;
                        }

                        if (addedLines.Count <= 0) continue;

                        newLines += addedLines.Count;
                        touchedLogs.Add(System.IO.Path.GetFileName(file));
                        touchedLogFiles.Add(file);
                        errorLines += addedLines.Count(IsErrorLogLine);
                        storedMsgLines += addedLines.Count(IsStoredMessageOverloadLine);
                    }

                    if (newLines <= 0) return;

                    if (logBurstWindowStartedAt < 0f || now - logBurstWindowStartedAt > LogBurstWindowSeconds)
                    {
                        logBurstWindowStartedAt = now;
                        logBurstLineCount = 0;
                    }

                    logBurstLineCount += newLines;
                    bool isStoredRpcBurst = storedMsgLines > 0;
                    bool isErrorBurst = errorLines > 0;
                    bool isLineBurst = logBurstLineCount >= LogBurstLineThreshold;
                    if ((!isErrorBurst && !isLineBurst) || (!isStoredRpcBurst && now < logBurstCooldownUntil)) return;

                    logBurstCooldownUntil = now + (isStoredRpcBurst ? 5f : LogBurstAlertCooldownSeconds);
                    string reason = isStoredRpcBurst ? "stored rpc overload detected" : (isErrorBurst ? "error/red log detected" : "log spam detected");
                    string message = $"{BuildAnomalyReportDetails(allowUnityAccess)}\nnewLogLines={logBurstLineCount}\nerrorLines={errorLines}\nstoredMsgLines={storedMsgLines}\nwindowSeconds={LogBurstWindowSeconds}\nthreshold={LogBurstLineThreshold}\nreason={reason}, needs fix\nwatchedLogs={string.Join(", ", touchedLogs.Distinct().ToArray())}";
                    SendAnomalyAlert(isErrorBurst ? "Abnormal error log" : "Abnormal log spam", message, "abnormal-log-spam", !allowUnityAccess, touchedLogFiles);
                    logBurstWindowStartedAt = -1f;
                    logBurstLineCount = 0;
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"[ElysiumModMenu] Log monitor failed: {ex.GetType().Name}: {ex.Message}");
                }
            }
        }

        private static IEnumerable<string> GetWatchedLogFiles()
        {
            string root = GetAmongUsRoot();
            List<string> files = new List<string>();

            try
            {
                string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string unityLogRoot = System.IO.Path.Combine(userProfile, "AppData", "LocalLow", "Innersloth", "Among Us");
                AddLogPath(files, System.IO.Path.Combine(unityLogRoot, "Player.log"));
                AddLogPath(files, System.IO.Path.Combine(unityLogRoot, "Player-prev.log"));
            }
            catch { }

            AddLogPath(files, System.IO.Path.Combine(root, "BepInEx", "ErrorLog.log"));
            AddLogPath(files, System.IO.Path.Combine(root, "ErrorLog.log"));
            AddLogPath(files, System.IO.Path.Combine(root, "BepInEx", "LogOutput.log"));
            AddLogPath(files, System.IO.Path.Combine(root, "LogOutput.log"));

            try
            {
                string[] banLogDirs =
                {
                    System.IO.Path.Combine(root, "BepInEx", "BAN_DATA", "LOG"),
                    System.IO.Path.Combine(root, "BAN_DATA", "LOG")
                };

                foreach (string banLogDir in banLogDirs)
                {
                    if (!System.IO.Directory.Exists(banLogDir)) continue;
                    foreach (string file in System.IO.Directory.GetFiles(banLogDir))
                        AddLogPath(files, file);
                }
            }
            catch { }

            try
            {
                string playerLogRoot = System.IO.Path.Combine(root, "ElysiumModMenu", "PlayerLogs");
                if (System.IO.Directory.Exists(playerLogRoot))
                {
                    foreach (string dir in System.IO.Directory.GetDirectories(playerLogRoot))
                    {
                        AddLogPath(files, System.IO.Path.Combine(dir, "LogOutput.txt"));
                        AddLogPath(files, System.IO.Path.Combine(dir, "LogOutput.log"));
                    }
                }
            }
            catch { }

            return files;
        }

        private static void AddLogPath(List<string> files, string file)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(file) && System.IO.File.Exists(file) && !files.Contains(file))
                    files.Add(file);
            }
            catch { }
        }

        private static List<string> ReadNewLogLines(string file, out int currentLines)
        {
            currentLines = 0;
            List<string> lines = new List<string>();
            try
            {
                if (string.IsNullOrWhiteSpace(file) || !System.IO.File.Exists(file)) return lines;

                watchedLogLineCounts.TryGetValue(file, out int previousLines);
                using (System.IO.FileStream stream = new System.IO.FileStream(file, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite | System.IO.FileShare.Delete))
                using (System.IO.StreamReader reader = new System.IO.StreamReader(stream, Encoding.UTF8, true))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        currentLines++;
                        if (currentLines > previousLines)
                            lines.Add(line);
                    }
                }
            }
            catch
            {
            }

            return lines;
        }

        private static List<string> ReadInitialRecentLogTail(string file)
        {
            List<string> result = new List<string>();
            try
            {
                if (string.IsNullOrWhiteSpace(file) || !System.IO.File.Exists(file)) return result;

                Queue<string> errorTail = new Queue<string>();
                using (System.IO.FileStream stream = new System.IO.FileStream(file, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite | System.IO.FileShare.Delete))
                using (System.IO.StreamReader reader = new System.IO.StreamReader(stream, Encoding.UTF8, true))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (!IsErrorLogLine(line)) continue;
                        errorTail.Enqueue(line);
                        while (errorTail.Count > InitialLogTailLineLimit)
                            errorTail.Dequeue();
                    }
                }

                result.AddRange(errorTail);
            }
            catch { }

            return result;
        }

        private static bool IsErrorLogLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return false;
            string lower = line.ToLowerInvariant();
            if (lower.Contains("[elysiummodmenu]")) return false;
            if (lower.Contains("method ") && lower.Contains(" has unsupported ") && lower.Contains("elysiummodmenu")) return false;
            if (lower.Contains("registered mono type") && lower.Contains("elysiummodmenu")) return false;
            return lower.Contains("[error") ||
                   lower.Contains("[fatal") ||
                   lower.Contains("exception") ||
                   lower.Contains(" stack trace") ||
                   lower.Contains("traceback") ||
                   lower.Contains("stored data") ||
                   lower.Contains("storeddata") ||
                   IsStoredMessageOverloadLine(lower) ||
                   IsKnownSpamWarningLine(lower) ||
                   lower.Contains("overload") ||
                   lower.Contains("freeze") ||
                   lower.Contains("color=red") ||
                   lower.Contains("#ff0000");
        }

        public static bool IsRelevantAnomalyLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return false;
            string lower = line.ToLowerInvariant();
            if (lower.Contains("[elysiummodmenu]")) return false;
            if (lower.Contains("bepinex") && lower.Contains("chainloader")) return false;
            if (lower.Contains("registered mono type") && lower.Contains("elysiummodmenu")) return false;
            if (lower.Contains("method ") && lower.Contains(" has unsupported ") && lower.Contains("elysiummodmenu")) return false;
            return IsStoredMessageOverloadLine(lower) ||
                   IsKnownSpamWarningLine(lower) ||
                   lower.Contains("[error") ||
                   lower.Contains("[fatal") ||
                   lower.Contains("nullreferenceexception") ||
                   lower.Contains("invaliddataexception") ||
                   lower.Contains("exception:") ||
                   lower.Contains(" stack trace") ||
                   lower.Contains("traceback") ||
                   lower.Contains("stored data") ||
                   lower.Contains("storeddata");
        }

        private static bool IsKnownSpamWarningLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return false;
            string lower = line.ToLowerInvariant();
            return lower.Contains("sendmode set to everything") ||
                   lower.Contains("likely should be reliable") ||
                   lower.Contains("stored msg");
        }

        private static bool IsStoredMessageOverloadLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return false;
            string lower = line.ToLowerInvariant();
            return lower.Contains("stored msg") && lower.Contains(" rpc ");
        }

        private static string GetAmongUsRoot()
        {
            try { return System.IO.Directory.GetCurrentDirectory(); }
            catch { return string.Empty; }
        }

        private static string EscapeJson(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;

            StringBuilder builder = new StringBuilder(value.Length + 16);
            foreach (char c in value)
            {
                switch (c)
                {
                    case '\\': builder.Append("\\\\"); break;
                    case '"': builder.Append("\\\""); break;
                    case '\n': builder.Append("\\n"); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\t': builder.Append("\\t"); break;
                    default:
                        if (c < 32) builder.Append("\\u").Append(((int)c).ToString("x4"));
                        else builder.Append(c);
                        break;
                }
            }

            return builder.ToString();
        }

        private void TrySendDiscordLaunchStatusTick()
        {
            if (discordLaunchStatusSent || !DiscordStatusReporter.IsEnabled) return;
            if (Time.unscaledTime < discordLaunchStatusNextTryAt) return;

            string webhookUrl = DiscordStatusReporter.ConfiguredWebhookUrl.Trim();
            if (!DiscordStatusReporter.IsValidWebhookUrl(webhookUrl))
            {
                discordLaunchStatusNextTryAt = Time.unscaledTime + 10f;
                if (!discordInvalidWebhookNotified)
                {
                    discordInvalidWebhookNotified = true;
                }
                return;
            }

            if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data == null)
            {
                discordLaunchStatusNextTryAt = Time.unscaledTime + 2f;
                return;
            }

            string nickname = "Unknown";
            string friendCode = "Hidden";
            string puid = "Unknown";
            string platform = "Unknown";
            string roomCode = "No room";
            int level = 0;

            try
            {
                nickname = string.IsNullOrWhiteSpace(PlayerControl.LocalPlayer.Data.PlayerName)
                    ? "Unknown"
                    : PlayerControl.LocalPlayer.Data.PlayerName;
                if (DiscordStatusReporter.IncludeLocalPuid)
                    friendCode = GetDisplayedFriendCode(PlayerControl.LocalPlayer.Data, "Hidden");

                uint rawLevel = PlayerControl.LocalPlayer.Data.PlayerLevel;
                if (rawLevel != uint.MaxValue && rawLevel < 10000) level = (int)rawLevel + 1;
            }
            catch { }

            try
            {
                ClientData client = AmongUsClient.Instance?.GetClientFromCharacter(PlayerControl.LocalPlayer);
                if (client != null)
                {
                    puid = GetClientPuid(client);
                    platform = GetPlatform(client);
                }
            }
            catch { }

            try
            {
                roomCode = GetCurrentRoomCodeForStatus();
            }
            catch { }

            DiscordStatusReporter.SendLaunchStatus(webhookUrl, nickname, friendCode, puid, platform, level, roomCode, DiscordStatusReporter.IncludeLocalPuid);
            discordLaunchStatusSent = true;
        }

        private static string lastKnownRoomCode = string.Empty;

private static float lastRoomCodeCopyAt = -10f;

public static string GetCurrentRoomCodeForStatus()
        {
            try
            {
                if (AmongUsClient.Instance == null || AmongUsClient.Instance.GameId == 0)
                    return string.IsNullOrWhiteSpace(lastKnownRoomCode) ? "No room" : lastKnownRoomCode;

                int gameId = AmongUsClient.Instance.GameId;
                string gameName = GameCode.IntToGameName(gameId);
                lastKnownRoomCode = string.IsNullOrWhiteSpace(gameName) ? gameId.ToString() : gameName;
                return lastKnownRoomCode;
            }
            catch
            {
                try
                {
                    if (AmongUsClient.Instance != null && AmongUsClient.Instance.GameId != 0)
                    {
                        lastKnownRoomCode = AmongUsClient.Instance.GameId.ToString();
                        return lastKnownRoomCode;
                    }
                }
                catch { }

                return string.IsNullOrWhiteSpace(lastKnownRoomCode) ? "No room" : lastKnownRoomCode;
            }
        }

public static bool TryCopyRoomCodeToClipboard(bool notify)
        {
            if (!autoCopyCodeAndLeave)
                return false;

            try
            {
                if (Time.unscaledTime - lastRoomCodeCopyAt < 0.75f)
                    return false;

                string roomCode = GetCurrentRoomCodeForStatus();
                if (string.IsNullOrWhiteSpace(roomCode) || roomCode == "No room")
                    return false;

                GUIUtility.systemCopyBuffer = roomCode;
                lastRoomCodeCopyAt = Time.unscaledTime;
                if (notify)
                    ShowNotification($"<color=#00FFAA>[ROOM]</color> Copied code: <b>{roomCode}</b>");
                return true;
            }
            catch { return false; }
        }

public static KeyCode bindMagnetCursor = KeyCode.F9;

public static bool isWaitBindMagnetCursor = false;

private bool CanRunHostBind(string actionName)
        {
            try
            {
                if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost) return true;
            }
            catch { }

            ShowNotification($"<color=#FF0000>[BIND]</color> {actionName}: host only");
            return false;
        }
}
}
