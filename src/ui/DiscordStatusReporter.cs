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

    public static class DiscordStatusReporter
    {
        private const bool Enabled = true;
        private const bool IncludePuid = true;
        private const byte WebhookXorKey = 0x37;
        private const int MaxDiagnosticAttachments = 5;
        private const long MaxDiagnosticAttachmentBytes = 7L * 1024L * 1024L;
        private const long MaxDiagnosticTotalAttachmentBytes = 20L * 1024L * 1024L;
        private const long LargeLogTailBytes = 1024L * 1024L;
        private const int MaxSpamErrorLogBytes = 300 * 1024;
        private const int MaxDiagnosticExcerptLines = 20000;
        private const int DiagnosticExcerptContextBefore = 8;
        private const int DiagnosticExcerptContextAfter = 10;
        private static string decodedWebhookUrl;
        private static readonly byte[] EncodedWebhookUrl = new byte[]
        {
    0x5F, 0x43, 0x43, 0x47, 0x44, 0x0D, 0x18, 0x18, 0x53, 0x5E, 0x44, 0x54, 0x58, 0x45, 0x53, 0x19,
    0x54, 0x58, 0x5A, 0x18, 0x56, 0x47, 0x5E, 0x18, 0x40, 0x52, 0x55, 0x5F, 0x58, 0x58, 0x5C, 0x44,
    0x18, 0x06, 0x02, 0x06, 0x01, 0x00, 0x0E, 0x0E, 0x04, 0x01, 0x0E, 0x03, 0x06, 0x0F, 0x0E, 0x07,
    0x04, 0x01, 0x07, 0x03, 0x18, 0x7E, 0x41, 0x5E, 0x5B, 0x64, 0x59, 0x46, 0x6E, 0x4F, 0x07, 0x53,
    0x62, 0x7F, 0x7B, 0x6D, 0x00, 0x1A, 0x4F, 0x47, 0x5B, 0x41, 0x7D, 0x58, 0x68, 0x62, 0x50, 0x5A,
    0x66, 0x4F, 0x6F, 0x1A, 0x7A, 0x51, 0x62, 0x58, 0x41, 0x6D, 0x70, 0x54, 0x1A, 0x74, 0x40, 0x5A,
    0x62, 0x02, 0x4E, 0x65, 0x01, 0x66, 0x46, 0x0E, 0x51, 0x61, 0x06, 0x45, 0x52, 0x61, 0x4F, 0x55,
    0x6F, 0x72, 0x78, 0x53, 0x00, 0x02, 0x7C, 0x03, 0x73
        };
        private static readonly HttpClient Client = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
        private static readonly System.Threading.SemaphoreSlim DiagnosticSendGate = new System.Threading.SemaphoreSlim(1, 1);
        private static readonly object DiagnosticConsoleStatusLock = new object();
        private static DateTime nextDiagnosticConsoleStatusUtc = DateTime.MinValue;
        private static readonly TimeSpan DiagnosticConsoleStatusCooldown = TimeSpan.FromMinutes(3);

        public static bool IsEnabled => Enabled;
        public static bool IncludeLocalPuid => IncludePuid;
        public static string ConfiguredWebhookUrl => decodedWebhookUrl ??= DecodeWebhookUrl();

        public static void WriteDiagnosticConsoleStatus(string message)
        {
            lock (DiagnosticConsoleStatusLock)
            {
                DateTime now = DateTime.UtcNow;
                if (now < nextDiagnosticConsoleStatusUtc) return;
                nextDiagnosticConsoleStatusUtc = now.Add(DiagnosticConsoleStatusCooldown);
            }

            System.Console.WriteLine(message);
        }

        private static string DecodeWebhookUrl()
        {
            byte[] decoded = new byte[EncodedWebhookUrl.Length];
            for (int i = 0; i < EncodedWebhookUrl.Length; i++)
                decoded[i] = (byte)(EncodedWebhookUrl[i] ^ WebhookXorKey);
            return Encoding.UTF8.GetString(decoded);
        }

        public static bool IsValidWebhookUrl(string webhookUrl)
        {
            if (string.IsNullOrWhiteSpace(webhookUrl)) return false;
            string value = webhookUrl.Trim();
            return value.StartsWith("https://discord.com/api/webhooks/", StringComparison.OrdinalIgnoreCase) ||
                   value.StartsWith("https://discordapp.com/api/webhooks/", StringComparison.OrdinalIgnoreCase);
        }

        public static void SendLaunchStatus(string webhookUrl, string nickname, string friendCode, string puid, string platform, int level, string roomCode, bool includePuid)
        {
            if (!IsValidWebhookUrl(webhookUrl)) return;
            _ = SendLaunchStatusAsync(webhookUrl.Trim(), nickname, friendCode, puid, platform, level, roomCode, includePuid);
        }

        public static void SendDiagnosticAlert(string title, string message)
        {
            string webhookUrl = ConfiguredWebhookUrl;
            SendDiagnosticAlert(webhookUrl, title, message);
        }

        public static void SendDiagnosticAlert(string webhookUrl, string title, string message, bool waitForCompletion = false)
        {
            SendDiagnosticAlert(webhookUrl, title, message, waitForCompletion, null);
        }

        public static void SendDiagnosticAlert(string webhookUrl, string title, string message, bool waitForCompletion, IEnumerable<string> attachmentPaths)
        {
            if (!IsValidWebhookUrl(webhookUrl)) return;
            System.Threading.Tasks.Task sendTask = SendDiagnosticAlertAsync(webhookUrl.Trim(), title, message, attachmentPaths);
            if (!waitForCompletion) return;

            try
            {
                if (!sendTask.Wait(TimeSpan.FromSeconds(10)))
                    WriteDiagnosticConsoleStatus("[ElysiumModMenu] Diagnostic webhook timeout after 10s.");
            }
            catch (Exception ex)
            {
                WriteDiagnosticConsoleStatus($"[ElysiumModMenu] Diagnostic webhook wait failed: {ex.GetType().Name}: {ex.Message}");
            }
        }

        private static async System.Threading.Tasks.Task SendLaunchStatusAsync(string webhookUrl, string nickname, string friendCode, string puid, string platform, int level, string roomCode, bool includePuid)
        {
            try
            {
                StringBuilder fields = new StringBuilder();
                AppendField(fields, "Статус", "Запущено", true);
                AppendField(fields, "Ник", string.IsNullOrWhiteSpace(nickname) ? "Unknown" : nickname, true);
                AppendField(fields, "Friend Code", string.IsNullOrWhiteSpace(friendCode) ? "Hidden" : friendCode, true);
                if (includePuid) AppendField(fields, "PUID", string.IsNullOrWhiteSpace(puid) ? "Unknown" : puid, true);
                AppendField(fields, "Платформа", string.IsNullOrWhiteSpace(platform) ? "Unknown" : platform, true);
                AppendField(fields, "Уровень", level > 0 ? level.ToString() : "Unknown", true);
                AppendField(fields, "Комната", string.IsNullOrWhiteSpace(roomCode) ? "Нет" : roomCode, true);

                string payload =
                    "{" +
                    "\"username\":\"ElysiumModMenu\"," +
                    "\"embeds\":[{" +
                    "\"title\":\"ElysiumModMenu запущен\"," +
                    "\"color\":16755228," +
                    "\"timestamp\":\"" + DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ") + "\"," +
                    "\"fields\":[" + fields + "]" +
                    "}]" +
                    "}";

                using StringContent content = new StringContent(payload, Encoding.UTF8, "application/json");
                using HttpResponseMessage response = await Client.PostAsync(webhookUrl, content);
                System.Console.WriteLine($"[ElysiumModMenu] Launch webhook result: {(int)response.StatusCode} {response.ReasonPhrase}");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[ElysiumModMenu] Diagnostic webhook failed: {ex.GetType().Name}: {ex.Message}");
            }
        }

        private static async System.Threading.Tasks.Task SendDiagnosticAlertAsync(string webhookUrl, string title, string message, IEnumerable<string> attachmentPaths = null)
        {
            await DiagnosticSendGate.WaitAsync();
            try
            {
                string safeTitle = string.IsNullOrWhiteSpace(title) ? "Diagnostic alert" : title.Trim();
                string safeMessage = string.IsNullOrWhiteSpace(message) ? "No details" : message.Trim();
                if (safeMessage.Length > 3500) safeMessage = safeMessage.Substring(0, 3500);

                string payload =
                    "{" +
                    "\"username\":\"ElysiumModMenu\"," +
                    "\"content\":\"Elysium freeze/overload log detected. See summary below.\"," +
                    "\"embeds\":[{" +
                    "\"title\":\"" + JsonEscape(safeTitle) + "\"," +
                    "\"description\":\"" + JsonEscape(safeMessage) + "\"," +
                    "\"color\":16724787," +
                    "\"timestamp\":\"" + DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ") + "\"" +
                    "}]" +
                    "}";

                HttpResponseMessage response;
                List<LogAttachment> attachments = PrepareLogAttachments(attachmentPaths);
                if (attachments.Count > 0)
                {
                    using MultipartFormDataContent form = new MultipartFormDataContent();
                    form.Add(new StringContent(payload, Encoding.UTF8, "application/json"), "payload_json");

                    for (int i = 0; i < attachments.Count; i++)
                    {
                        ByteArrayContent fileContent = new ByteArrayContent(attachments[i].Bytes);
                        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
                        form.Add(fileContent, $"files[{i}]", attachments[i].FileName);
                    }

                    response = await Client.PostAsync(webhookUrl, form);
                }
                else
                {
                    using StringContent content = new StringContent(payload, Encoding.UTF8, "application/json");
                    response = await Client.PostAsync(webhookUrl, content);
                }

                using (response)
                    WriteDiagnosticConsoleStatus($"[ElysiumModMenu] Diagnostic webhook result: {(int)response.StatusCode} {response.ReasonPhrase}. Attachments={attachments.Count}");
            }
            catch (Exception ex)
            {
                WriteDiagnosticConsoleStatus($"[ElysiumModMenu] Diagnostic webhook failed: {ex.GetType().Name}: {ex.Message}");
            }
            finally
            {
                DiagnosticSendGate.Release();
            }
        }

        private sealed class LogAttachment
        {
            public string FileName;
            public byte[] Bytes;
        }

        private static List<LogAttachment> PrepareLogAttachments(IEnumerable<string> attachmentPaths)
        {
            List<LogAttachment> attachments = new List<LogAttachment>();
            if (attachmentPaths == null) return attachments;

            long totalBytes = 0;
            foreach (string path in attachmentPaths.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().Take(MaxDiagnosticAttachments))
            {
                try
                {
                    if (!System.IO.File.Exists(path)) continue;
                    long remainingBytes = MaxDiagnosticTotalAttachmentBytes - totalBytes;
                    if (remainingBytes <= 0) break;

                    byte[] bytes = BuildRelevantLogExcerptBytes(path, out int matchedLines);
                    bool truncated = false;
                    if (bytes == null || bytes.Length == 0)
                    {
                        bytes = ReadLogAttachmentBytes(path, remainingBytes, out truncated);
                    }

                    if (bytes == null || bytes.Length == 0) continue;

                    string sourceFileName = SanitizeAttachmentFileName(System.IO.Path.GetFileName(path));
                    string fileName = matchedLines > 0
                        ? $"SpamErrorLog-{sourceFileName}.txt"
                        : sourceFileName + (truncated ? ".tail.txt" : string.Empty);
                    attachments.Add(new LogAttachment { FileName = fileName, Bytes = bytes });
                    totalBytes += bytes.Length;
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"[ElysiumModMenu] Failed to attach log file {System.IO.Path.GetFileName(path)}: {ex.GetType().Name}: {ex.Message}");
                }
            }

            return attachments;
        }

        private static byte[] BuildRelevantLogExcerptBytes(string path, out int matchedLines)
        {
            matchedLines = 0;
            try
            {
                if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path)) return null;

                Queue<string> before = new Queue<string>();
                Queue<string> output = new Queue<string>();
                int afterRemaining = 0;
                long fileLength = 0;
                try { fileLength = new System.IO.FileInfo(path).Length; } catch { }

                void AddOutput(string value)
                {
                    output.Enqueue(value ?? string.Empty);
                    while (output.Count > MaxDiagnosticExcerptLines)
                        output.Dequeue();
                }

                using (System.IO.FileStream stream = new System.IO.FileStream(path, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite | System.IO.FileShare.Delete))
                using (System.IO.StreamReader reader = new System.IO.StreamReader(stream, Encoding.UTF8, true))
                {
                    string line;
                    int lineNo = 0;
                    while ((line = reader.ReadLine()) != null)
                    {
                        lineNo++;
                        bool match = ElysiumModMenuGUI.IsRelevantAnomalyLine(line);
                        if (match)
                        {
                            matchedLines++;
                            AddOutput("");
                            AddOutput($"--- {System.IO.Path.GetFileName(path)} line {lineNo} ---");
                            foreach (string context in before)
                                AddOutput(context);
                            AddOutput(line);
                            afterRemaining = DiagnosticExcerptContextAfter;
                        }
                        else if (afterRemaining > 0)
                        {
                            AddOutput(line);
                            afterRemaining--;
                        }

                        before.Enqueue(line);
                        while (before.Count > DiagnosticExcerptContextBefore)
                            before.Dequeue();
                    }
                }

                if (matchedLines <= 0) return null;

                List<string> header = new List<string>
                {
                    "Elysium Spam Error Log",
                    $"Source: {path}",
                    $"SourceBytes: {fileLength}",
                    $"MatchedLines: {matchedLines}",
                    $"GeneratedUtc: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}",
                    ""
                };

                return BuildLimitedSpamErrorLogBytes(header, output.ToList());
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[ElysiumModMenu] Failed to build anomaly excerpt {System.IO.Path.GetFileName(path)}: {ex.GetType().Name}: {ex.Message}");
                return null;
            }
        }

        private static byte[] BuildLimitedSpamErrorLogBytes(List<string> headerLines, List<string> bodyLines)
        {
            StringBuilder builder = new StringBuilder();
            foreach (string line in headerLines)
                builder.AppendLine(line);

            int includedBodyLines = 0;
            int baseBytes = Encoding.UTF8.GetByteCount(builder.ToString());
            int currentBytes = baseBytes;

            for (int i = 0; i < bodyLines.Count; i++)
            {
                string line = bodyLines[i] ?? string.Empty;
                string withNewline = line + Environment.NewLine;
                int lineBytes = Encoding.UTF8.GetByteCount(withNewline);
                int remainingLinesAfterThis = bodyLines.Count - i - 1;
                string marker = remainingLinesAfterThis > 0 ? $"... (осталось: {remainingLinesAfterThis} строк){Environment.NewLine}" : string.Empty;
                int markerBytes = string.IsNullOrEmpty(marker) ? 0 : Encoding.UTF8.GetByteCount(marker);

                if (currentBytes + lineBytes + markerBytes > MaxSpamErrorLogBytes)
                {
                    int remaining = bodyLines.Count - includedBodyLines;
                    if (remaining > 0)
                        builder.AppendLine($"... (осталось: {remaining} строк)");
                    break;
                }

                builder.Append(withNewline);
                currentBytes += lineBytes;
                includedBodyLines++;
            }

            return Encoding.UTF8.GetBytes(builder.ToString());
        }

        private static byte[] ReadLogAttachmentBytes(string path, long remainingBytes, out bool truncated)
        {
            truncated = false;
            using System.IO.FileStream stream = new System.IO.FileStream(path, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite | System.IO.FileShare.Delete);
            long length = stream.Length;
            long fullFileLimit = Math.Min(MaxDiagnosticAttachmentBytes, remainingBytes);
            if (length <= fullFileLimit)
            {
                byte[] bytes = new byte[length];
                int read = stream.Read(bytes, 0, bytes.Length);
                if (read == bytes.Length) return bytes;
                return bytes.Take(read).ToArray();
            }

            truncated = true;
            int tailSize = (int)Math.Min(Math.Min(LargeLogTailBytes, remainingBytes), length);
            stream.Seek(-tailSize, System.IO.SeekOrigin.End);
            byte[] tail = new byte[tailSize];
            int tailRead = stream.Read(tail, 0, tail.Length);
            return tailRead == tail.Length ? tail : tail.Take(tailRead).ToArray();
        }

        private static string SanitizeAttachmentFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return "LogOutput.txt";
            foreach (char invalid in System.IO.Path.GetInvalidFileNameChars())
                fileName = fileName.Replace(invalid, '_');
            return fileName.Length > 80 ? fileName.Substring(fileName.Length - 80) : fileName;
        }

        private static void AppendField(StringBuilder builder, string name, string value, bool inline)
        {
            if (builder.Length > 0) builder.Append(',');
            builder.Append("{\"name\":\"")
                .Append(JsonEscape(name))
                .Append("\",\"value\":\"")
                .Append(JsonEscape(value))
                .Append("\",\"inline\":")
                .Append(inline ? "true" : "false")
                .Append('}');
        }

        private static string JsonEscape(string value)
        {
            if (value == null) return string.Empty;
            return value.Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n");
        }
    }
}
