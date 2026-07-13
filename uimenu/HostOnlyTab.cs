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
private string[] visualsSubTabs => new string[] { L("IN-GAME", "В ИГРЕ"), L("OUTFITS", "ОДЕЖДА") };

private int currentSelfSubTab = 0;

private string[] selfSubTabs = { "SELF", "ROLES", "MOVEMENT", "CHAT" };

public static bool fakeStartCounterTroll = false;

public static bool fakeStartCounterCustom = false;

public static string fakeStartInput = "69";

public static bool isEditingFakeStart = false;

public static float customStartTimer = -1f;

public static bool localRainbow = false;

public static List<byte> rainbowPlayers = new List<byte>();

public static float colorTimer = 0f;

public static byte currentColorId = 0;

public static bool lobbyRainbowAll = false;

public static bool lobbyAllColor = false;

public static int lobbyAllColorId = 0;

public static bool localRainbowFreeOnly = false;

public static float freeColorTimer = 0f;

public static int freeRainbowIndex = 0;

public static int selectedFreeColorIndex = 0;

private Vector2 playerListScrollPos = Vector2.zero;

private Vector2 playerActionScrollPos = Vector2.zero;

private byte selectedAntiCheatPlayerId = 255;

public static string spoofLevelString = "100";

public static bool enableLevelSpoof = true;

private static bool hasLevelSpoofRestoreLevel = false;

private static uint levelSpoofRestoreLevel = 0;

private static uint lastAppliedLevelSpoofValue = uint.MaxValue;

private static int lastLevelSpoofGameId = int.MinValue;

public static string customNameInput = "хыхых";

public static string spoofFriendCodeInput = "----";

public static string localFriendCodeInput = "yourlocal#fc";

public static string spoofedDeviceId = "";

private const string GhostChatDefaultColor = "#AFAFAF";

public static string ghostChatColorHex = GhostChatDefaultColor;

public static bool isEditingLevel = false;

public static bool isEditingName = false;

public static bool isEditingFriendCode = false;

public static bool isEditingLocalFriendCode = false;

public static bool isEditingDeviceId = false;

public static bool isEditingGhostChatColor = false;

public static bool enableLocalNameSpoof = false;

public static bool enableLocalFriendCodeSpoof = false;

public static bool enableFriendCodeSpoof = false;

public static bool enableDeviceIdSpoof = false;

public static bool enablePlatformSpoof = true;

public static bool throttleDefaultLogs = true;

// Controls verbose Message/Info/Debug output. Warnings and errors are never hidden.
public static bool detailedLogsEnabled = false;

public static bool showEspFriendCode = true;

public static bool allowDuplicateColors = false;

public static bool autoGhostAfterStart = false;

public static bool bugRoomTimedAutoRun = false;

public static int bugRoomTimedAutoRunMinutes = 10;

public static string bugRoomTimedAutoRunInput = "10";

public static bool isEditingBugRoomTimedAutoRun = false;

public static bool bugRoomLv35Rehost = false;

public static bool bugRoomHostPassRejoin = false;

public static bool autoBanPlatformSpoof = false;

public static bool banCustomPlatformsFromTxt = false;

public static bool autoKickLowLevelEnabled = false;

public static int autoKickMinLevel = 200;

public static int fpsLimit = 60;

public static string fpsLimitInput = "60";

public static bool isEditingFpsLimit = false;

public static bool limitFps = true;

public static int chatHistoryLimit = 20;

public static int currentPlatformIndex = 1;

private static float localNameRefreshTimer = 0f;


private static float platformBanScanTimer = 0f;

private static int lastAppliedFpsLimit = -1;

private static bool autoGhostAppliedThisGame = false;

private static bool wasGameStartedForAutoGhost = false;

private static int autoChatEveryoneGameId = int.MinValue;

private static bool autoChatEveryoneSawShhh = false;

private static int gameIntroShhhSeenGameId = int.MinValue;

private static bool autoChatEveryoneNoEjectSent = false;

private static string originalLocalFriendCode = null;

private static string originalLocalName = null;

private static float friendEspIgnoreNextLoadAt = 0f;

private static readonly HashSet<string> friendEspIgnoreTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

private static string platformBanListPath = "";

private static float platformBanListNextLoadAt = 0f;

private static readonly HashSet<string> customPlatformBanTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

private static readonly HashSet<int> platformSpoofPunishedOwners = new HashSet<int>();

private float lowLevelKickScanTimer = 0f;

private static readonly HashSet<int> lowLevelKickPunishedOwners = new HashSet<int>();

private const int FavoriteOutfitSlotCount = 4;

private static readonly string[] favoriteOutfitSlots = new string[FavoriteOutfitSlotCount];

private float brokenFcScanTimer = 0f;

private static readonly HashSet<int> brokenFcPunishedOwners = new HashSet<int>();

public static string[] platformNames = {
            "Epic", "Steam", "Mac", "Microsoft", "Itch", "iOS",
            "Android", "Switch", "Xbox", "PlayStation", "Starlight"
        };

public static Platforms[] platformValues = {
            (Platforms)1,
            (Platforms)2,
            (Platforms)3,
            (Platforms)4,
            (Platforms)5,
            (Platforms)6,
            (Platforms)7,
            (Platforms)8,
            (Platforms)9,
            (Platforms)10,
            (Platforms)112
        };

public static bool unlockFeatures = true;

public static bool guestExtraFeatures = false;

public static bool bypassAgeRestrictions = false;

public class ElysiumNotification
        {
            public string title;
            public string message;
            public float ttl;
            public float lifetime;
            public bool HasExpired => lifetime > ttl;

            public ElysiumNotification(string title, string message, float ttl)
            {
                this.title = title;
                this.message = message;
                this.ttl = ttl;
                this.lifetime = 0f;
            }
        }

public static List<string> bannedEntries = new List<string>();

private static readonly HashSet<string> bannedFriendCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

private static bool noTaskOptionsApplied;

private static int noTaskOptionsGameId = int.MinValue;

private static float nextBotBanScanAt;

private static readonly List<PlayerControl> cachedTracerPlayers = new List<PlayerControl>();

private static readonly List<bool> cachedTracerVisibility = new List<bool>();

private static readonly List<DeadBody> cachedTracerBodies = new List<DeadBody>();

private static float nextTracerScanAt;

public static string banListPath = "";

private Vector2 banListScroll = Vector2.zero;

private Vector2 roomPlayerActionsScroll = Vector2.zero;

private sealed class RoomPlayerActionEntry
        {
            public int ownerId;
            public string playerName;
            public int level;
            public string friendCode;
            public string puid;
        }

public static bool autoBanEnabled = true;

public static string banInput = "";

public static bool isEditingBan = false;

public static List<string> botBannedEntries = new List<string>();

public static string botBanListPath = "";

public static bool banBotsEnabled = false;

public static bool oldAntiCheatVersion = false;

public static bool blockVentKickExploit = true;

public static bool blockServerTeleports = true;

public static bool overflowProtection = true;

public static readonly string[] botNameTokens = new string[]
        {
            "UCbot", "bot", "бот", "Ucбот", "sixseven", "лут", "67",
            "какойтобот", "бот67", "бот69"
        };

public static void LoadBanList()
        {
            try
            {
                banListPath = System.IO.Path.Combine(Plugin.ElysiumFolder, "ElysiumModMenuBanList.txt");
                if (!System.IO.File.Exists(banListPath))
                {
                    System.IO.File.Create(banListPath).Dispose();
                }
                bannedEntries = new List<string>(System.IO.File.ReadAllLines(banListPath));
                RebuildBannedFriendCodeIndex();
            }
            catch { }
        }

private static void RebuildBannedFriendCodeIndex()
        {
            bannedFriendCodes.Clear();
            foreach (string entry in bannedEntries)
            {
                if (string.IsNullOrWhiteSpace(entry)) continue;
                int separator = entry.IndexOf('|');
                string friendCode = (separator >= 0 ? entry.Substring(0, separator) : entry).Trim();
                if (!string.IsNullOrEmpty(friendCode)) bannedFriendCodes.Add(friendCode);
            }
        }

private static bool IsFriendCodeBanned(string friendCode)
        {
            return !string.IsNullOrWhiteSpace(friendCode) && bannedFriendCodes.Contains(friendCode.Trim());
        }

public static void AddToBanList(string friendCode, string puid, string name, string reason)
        {
            try
            {
                if (string.IsNullOrEmpty(friendCode)) return;
                if (IsMeowcheloProtected(name)) return;

                string normalizedFriendCode = friendCode.Trim();
                if (!bannedFriendCodes.Contains(normalizedFriendCode))
                {
                    string date = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
                    string entry = $"{friendCode}|{puid}|{name}|{date}|{reason}";
                    bannedEntries.Add(entry);
                    bannedFriendCodes.Add(normalizedFriendCode);
                    System.IO.File.AppendAllText(banListPath, entry + Environment.NewLine);
                }
            }
            catch { }
        }

public static void RemoveFromBanList(string entry)
        {
            try
            {
                bannedEntries.Remove(entry);
                System.IO.File.WriteAllLines(banListPath, bannedEntries.ToArray());
                RebuildBannedFriendCodeIndex();
            }
            catch { }
        }

public static void LoadBotBanList()
        {
            try
            {
                botBanListPath = System.IO.Path.Combine(Plugin.ElysiumFolder, "ElysiumBotBanList.txt");
                if (!System.IO.File.Exists(botBanListPath))
                {
                    System.IO.File.Create(botBanListPath).Dispose();
                }
                botBannedEntries = new List<string>(System.IO.File.ReadAllLines(botBanListPath));
            }
            catch { }
        }

public static void AddToBotBanList(string friendCode, string puid, string name, string reason)
        {
            try
            {
                string fc = string.IsNullOrWhiteSpace(friendCode) ? "Unknown" : friendCode.Trim();
                string nm = string.IsNullOrWhiteSpace(name) ? "Unknown" : name.Trim();
                string fcLower = fc.ToLower();
                string nameLower = nm.ToLower();

                bool already = false;
                foreach (var e in botBannedEntries)
                {
                    if (string.IsNullOrWhiteSpace(e) || e.TrimStart().StartsWith("#")) continue;
                    string[] parts = e.Split('|');
                    if (parts.Length > 0 && fcLower != "unknown" && parts[0].Trim().ToLower() == fcLower) { already = true; break; }
                    if (fcLower == "unknown" && parts.Length >= 3 && parts[2].Trim().ToLower() == nameLower) { already = true; break; }
                }

                if (!already)
                {
                    if (string.IsNullOrEmpty(botBanListPath)) LoadBotBanList();
                    string date = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
                    string entry = $"{fc}|{puid}|{nm}|{date}|{reason}";
                    botBannedEntries.Add(entry);
                    System.IO.File.AppendAllText(botBanListPath, entry + Environment.NewLine);
                }
            }
            catch { }
        }

public static bool IsBotName(string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name)) return false;
                string n = name.Trim().ToLowerInvariant();

                foreach (var token in botNameTokens)
                {
                    if (string.IsNullOrWhiteSpace(token)) continue;
                    if (n.Contains(token.Trim().ToLowerInvariant())) return true;
                }

                foreach (var e in botBannedEntries)
                {
                    if (string.IsNullOrWhiteSpace(e) || e.TrimStart().StartsWith("#")) continue;
                    string[] parts = e.Split('|');
                    string nick = parts.Length >= 3 ? parts[2].Trim().ToLowerInvariant() : e.Trim().ToLowerInvariant();
                    if (!string.IsNullOrWhiteSpace(nick) && nick != "unknown" && n.Contains(nick)) return true;
                }
            }
            catch { }
            return false;
        }

public static bool IsBotBannedFc(string fc)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fc)) return false;
                string f = fc.Trim().ToLowerInvariant();
                foreach (var e in botBannedEntries)
                {
                    if (string.IsNullOrWhiteSpace(e) || e.TrimStart().StartsWith("#")) continue;
                    string[] parts = e.Split('|');
                    if (parts.Length > 0 && parts[0].Trim().ToLowerInvariant() == f) return true;
                }
            }
            catch { }
            return false;
        }

public static bool killReach = false, killAnyone = false;

public static bool followPlayer = false;

public static byte followPlayerId = byte.MaxValue;

private static float nextFollowPlayerRpcAt = 0f;

private static int selectedPlayerVentIdx = 0;

private static int selectedLobbyVentIdx = 0;

private static readonly Dictionary<byte, ushort> ventTpSeq = new Dictionary<byte, ushort>();

public static bool endlessSsDuration = false, noVitalsCooldown = false;

public static bool endlessBattery = false, endlessVentTime = false, noVentCooldown = false, noMapCooldowns = false;

public static bool unlockVents = false, walkInVents = false;

public static bool reactorSab = false, oxygenSab = false, commsSab = false, elecSab = false, unfixableLights = false;

private static bool unfixableLightsApplied = false;

public static bool autoOpenDoors = false;

public static bool moonWalk = false;

public static bool SeePlayersInVent = false;

public static bool seeGhosts = false;

public static bool seePhantoms = false;

public static bool seeRoles = false;

public static bool showPlayerInfo = false;

public static bool showEspBoxes = false;

public static bool espShimmerMode = false;

public static bool showEspVoteKicks = false;

public static bool revealMeetingRoles = false;

public static bool showTracers = false;

public static bool showCrewmateTracers = false;

public static bool showImpostorTracers = false;

public static bool showDeadTracers = false;

public static bool showBodyTracers = false;

public static bool fullBright = false;

public static bool seeProtections = false;

public static bool seeKillCooldown = false;

public static bool extendedLobby = false;

public static bool DarkModeEnabled = true;

public static bool enableChatDarkMode = true;

public static float customLightRadius = 5f;

private static Dictionary<byte, float> lastKillTimestamps = new Dictionary<byte, float>();

public static bool alwaysChat = false;

public static bool readGhostChat = false;

public static bool enableSpellCheck = false;

public static bool neverEndGame = false;

public static bool autoRepairSabotage = false;

private static float nextAutoRepairSabotageAt = 0f;

public static bool autoBreakSabotage = false;

private static float nextAutoBreakSabotageAt = 0f;

public static bool showReplay = false;

public static bool replayOnlyLastSeconds = true;

public static float replaySeconds = 30f;

public static bool replayDrawIcons = false;

public static Rect replayRect = new Rect(250f, 90f, 330f, 240f);

public static void ShowNotification(string text)
        {
            string title = "ElysiumModMenu";
            string msg = text;

            if (text.Contains("[") && text.Contains("]"))
            {
                int start = text.IndexOf("[");
                int end = text.IndexOf("]");
                if (end > start)
                {
                    string rawTitle = text.Substring(start + 1, end - start - 1);
                    title = System.Text.RegularExpressions.Regex.Replace(rawTitle, "<.*?>", string.Empty);
                    msg = System.Text.RegularExpressions.Regex.Replace(msg, @"(<color=#[^>]+>)?\[.*?\](</color>)?\s*", "");
                }
            }
            SendNotification(title, msg.Trim(), 3.5f);
        }

public static void SendNotification(string title, string message, float ttl = 3.5f)
        {
            if (!EnableCustomNotifs) return;
            title = NormalizeNotificationText(title);
            message = NormalizeNotificationText(message);
            if (ShouldSuppressBanNotification(title, message)) return;
            lock (notificationQueueLock)
            {
                if (pendingScreenNotifications.Count >= 64)
                    pendingScreenNotifications.Dequeue();
                pendingScreenNotifications.Enqueue(new ElysiumNotification(title, message, ttl));
            }
        }

private static bool ShouldSuppressBanNotification(string title, string message)
        {
            if (!IsBanNotification(title, message)) return false;

            float now = Time.unscaledTime;
            if (now < nextBanNotificationAllowedAt)
            {
                return true;
            }

            nextBanNotificationAllowedAt = now + BanNotificationCooldownSeconds;
            return false;
        }

private static bool IsBanNotification(string title, string message)
        {
            return ContainsBanToken(title) || ContainsBanToken(message);
        }

private static bool ContainsBanToken(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            return text.IndexOf("ban", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   text.IndexOf("banned", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   text.IndexOf("бан", StringComparison.OrdinalIgnoreCase) >= 0;
        }

private static string NormalizeNotificationText(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return TryDecodeCp1251Mojibake(text, out string decoded) ? decoded : text;
        }

private static bool TryDecodeCp1251Mojibake(string text, out string decoded)
        {
            decoded = text;
            if (!MayContainCp1251Mojibake(text)) return false;

            try
            {
                byte[] bytes = new byte[text.Length];
                for (int i = 0; i < text.Length; i++)
                {
                    if (!TryGetCp1251Byte(text[i], out byte value))
                        return false;
                    bytes[i] = value;
                }

                string candidate = new UTF8Encoding(false, true).GetString(bytes);
                if (string.IsNullOrWhiteSpace(candidate) || !ContainsCyrillic(candidate)) return false;

                decoded = candidate;
                return true;
            }
            catch
            {
                decoded = text;
                return false;
            }
        }

private static bool MayContainCp1251Mojibake(string text)
        {
            bool hasLead = false;
            bool hasMojibakeMarker = false;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == 'Р' || c == 'С') hasLead = true;
                if ((c >= '\u0080' && c <= '\u009F') ||
                    (c >= '\u0402' && c <= '\u040F') ||
                    (c >= '\u0452' && c <= '\u045F') ||
                    c == '™' || c == 'њ' || c == 'ќ' || c == 'љ' || c == 'џ' ||
                    c == 'µ' || c == '±' || c == '»' || c == '«')
                    hasMojibakeMarker = true;
            }
            return hasLead && hasMojibakeMarker;
        }

private static bool ContainsCyrillic(string text)
        {
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if ((c >= '\u0400' && c <= '\u04FF') || c == 'Ё' || c == 'ё')
                    return true;
            }
            return false;
        }

private static bool TryGetCp1251Byte(char c, out byte value)
        {
            if (c <= '\u00FF')
            {
                value = (byte)c;
                return true;
            }

            if (c >= 'А' && c <= 'я')
            {
                value = (byte)(0xC0 + (c - 'А'));
                return true;
            }

            switch (c)
            {
                case 'Ђ': value = 0x80; return true;
                case 'Ѓ': value = 0x81; return true;
                case '‚': value = 0x82; return true;
                case 'ѓ': value = 0x83; return true;
                case '„': value = 0x84; return true;
                case '…': value = 0x85; return true;
                case '†': value = 0x86; return true;
                case '‡': value = 0x87; return true;
                case '€': value = 0x88; return true;
                case '‰': value = 0x89; return true;
                case 'Љ': value = 0x8A; return true;
                case '‹': value = 0x8B; return true;
                case 'Њ': value = 0x8C; return true;
                case 'Ќ': value = 0x8D; return true;
                case 'Ћ': value = 0x8E; return true;
                case 'Џ': value = 0x8F; return true;
                case 'ђ': value = 0x90; return true;
                case '‘': value = 0x91; return true;
                case '’': value = 0x92; return true;
                case '“': value = 0x93; return true;
                case '”': value = 0x94; return true;
                case '•': value = 0x95; return true;
                case '–': value = 0x96; return true;
                case '—': value = 0x97; return true;
                case '™': value = 0x99; return true;
                case 'љ': value = 0x9A; return true;
                case '›': value = 0x9B; return true;
                case 'њ': value = 0x9C; return true;
                case 'ќ': value = 0x9D; return true;
                case 'ћ': value = 0x9E; return true;
                case 'џ': value = 0x9F; return true;
                case 'Ў': value = 0xA1; return true;
                case 'ў': value = 0xA2; return true;
                case 'Ј': value = 0xA3; return true;
                case 'Ґ': value = 0xA5; return true;
                case 'Ё': value = 0xA8; return true;
                case 'Є': value = 0xAA; return true;
                case 'Ї': value = 0xAF; return true;
                case 'І': value = 0xB2; return true;
                case 'і': value = 0xB3; return true;
                case 'ґ': value = 0xB4; return true;
                case 'ё': value = 0xB8; return true;
                case '№': value = 0xB9; return true;
                case 'є': value = 0xBA; return true;
                case 'ј': value = 0xBC; return true;
                case 'Ѕ': value = 0xBD; return true;
                case 'ѕ': value = 0xBE; return true;
                case 'ї': value = 0xBF; return true;
                default:
                    value = 0;
                    return false;
            }
        }

public static void TickNotificationQueue()
        {
            if (!EnableCustomNotifs)
            {
                lock (notificationQueueLock) pendingScreenNotifications.Clear();
                return;
            }

            if (Time.unscaledTime < nextNotificationDisplayAt) return;

            ElysiumNotification next = null;
            lock (notificationQueueLock)
            {
                if (pendingScreenNotifications.Count > 0)
                    next = pendingScreenNotifications.Dequeue();
            }

            if (next == null) return;
            screenNotifications.Add(next);
            nextNotificationDisplayAt = Time.unscaledTime + 0.3f;
        }

        private static string GetNotificationTextForTheme(string message)
        {
            if (!whiteMenuTheme || string.IsNullOrEmpty(message))
                return message;

            return System.Text.RegularExpressions.Regex.Replace(message, @"</?color(=#[^>]+)?>", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }



        public static HashSet<byte> forcedImpostors = new HashSet<byte>();

public static Dictionary<byte, RoleTypes> forcedPreGameRoles = new Dictionary<byte, RoleTypes>();

public static HashSet<string> forcedImpostorFcs = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

public static Dictionary<string, RoleTypes> forcedPreGameRoleFcs = new Dictionary<string, RoleTypes>(System.StringComparer.OrdinalIgnoreCase);

public static bool enablePreGameRoleForce = false;

public static bool autoTwoImpostors = false;

private static readonly HashSet<byte> autoTwoImpostorPlayerIds = new HashSet<byte>();

private static bool autoTwoImpostorsNeedsGameStartRoll = true;

private static bool autoTwoImpostorsWasGameStarted = false;

private static int autoTwoImpostorsLastLobbyFingerprint = 0;

private static float nextAutoTwoImpostorsLobbyCheckAt = 0f;

private Vector2 preRolesListScrollPos = Vector2.zero;

private Vector2 preRolesActionScrollPos = Vector2.zero;

private byte selectedPreRoleId = 255;

private string selectedPreRoleFc = string.Empty;

public static List<PlayerControl> lockedPlayersList = new List<PlayerControl>();

public static bool LogAllRPCs = true;

public static bool blockRainbowChat = true;

public static bool blockFortegreenChat = true;

public static bool EnableCustomNotifs = true;

public static Vector2 notificationBoxSize = new Vector2(260f, 65f);

public static List<ElysiumNotification> screenNotifications = new List<ElysiumNotification>();

private static GUIStyle notificationTitleStyle;

private static GUIStyle notificationTimerStyle;

private static GUIStyle notificationMessageStyle;

private static readonly Queue<ElysiumNotification> pendingScreenNotifications = new Queue<ElysiumNotification>();

private static readonly object notificationQueueLock = new object();

private static float nextNotificationDisplayAt;

private const float BanNotificationCooldownSeconds = 2.5f;

private static float nextBanNotificationAllowedAt;

private bool stylesInited = false;

private GUIStyle windowStyle, btnStyle, activeTabStyle, headerStyle, boxStyle;

private GUIStyle sidebarStyle, sidebarBtnStyle, activeSidebarBtnStyle, titleStyle;

private GUIStyle toggleOnStyle, toggleOffStyle, toggleLabelStyle, safeLineStyle, trackOnStyle, trackOffStyle, knobStyle;

private GUIStyle sliderStyle, sliderThumbStyle, subTabStyle, activeSubTabStyle;

public GUIStyle inputBlockStyle;

private Texture2D texWindowBg, texBoxBg, texBtnBg, texAccent, texSidebarBg;

private Texture2D texToggleOff, texToggleOn, texSliderBg, texSliderThumb, texInputBg, texColorBtn, texScrollThumb, texTrackOff, texTrackOn, texKnobWhite, texSwatchSquare;

private Texture2D texMenuCard;

private GUIStyle menuCardStyle, menuSectionTitleStyle, menuDescStyle, menuBadgeStyle, menuAccentBarStyle, menuSwatchStyle, menuSwatchSquareStyle;

private void DrawHostOnlyTab()
        {
            float hostTabWidth = GetMenuWorkWidth(220f, 760f);
            float hostTabGap = 4f;
            float hostSubTabWidth = Mathf.Floor((hostTabWidth - (hostTabGap * (hostOnlySubTabs.Length - 1))) / hostOnlySubTabs.Length);
            int hostTabFontSize = hostSubTabWidth < 116f ? 10 : 11;
            GUIStyle compactSubTabStyle = new GUIStyle(subTabStyle)
            {
                fontSize = hostTabFontSize,
                clipping = TextClipping.Clip,
                wordWrap = false,
                padding = CreateRectOffset(2, 2, 2, 2)
            };
            GUIStyle compactActiveSubTabStyle = new GUIStyle(activeSubTabStyle)
            {
                fontSize = hostTabFontSize,
                clipping = TextClipping.Clip,
                wordWrap = false,
                padding = CreateRectOffset(2, 2, 2, 2)
            };
            float[] hostSubTabWidths = new float[hostOnlySubTabs.Length];
            float totalHostSubTabWidth = hostTabGap * (hostOnlySubTabs.Length - 1);
            for (int i = 0; i < hostOnlySubTabs.Length; i++)
            {
                float preferredWidth = Mathf.Ceil(compactSubTabStyle.CalcSize(new GUIContent(hostOnlySubTabs[i])).x) + 14f;
                hostSubTabWidths[i] = Mathf.Max(48f, preferredWidth);
                totalHostSubTabWidth += hostSubTabWidths[i];
            }

            if (totalHostSubTabWidth > hostTabWidth)
            {
                float tabWidthScale = (hostTabWidth - (hostTabGap * (hostOnlySubTabs.Length - 1))) / (totalHostSubTabWidth - (hostTabGap * (hostOnlySubTabs.Length - 1)));
                for (int i = 0; i < hostSubTabWidths.Length; i++)
                    hostSubTabWidths[i] = Mathf.Floor(hostSubTabWidths[i] * tabWidthScale);
            }

            GUILayout.BeginHorizontal(GUILayout.Width(hostTabWidth));
            for (int i = 0; i < hostOnlySubTabs.Length; i++)
            {
                if (GUILayout.Button(hostOnlySubTabs[i], currentHostOnlySubTab == i ? compactActiveSubTabStyle : compactSubTabStyle, GUILayout.Width(hostSubTabWidths[i]), GUILayout.Height(18)))
                {
                    currentHostOnlySubTab = i;
                    scrollPosition = Vector2.zero;
                }

                if (i < hostOnlySubTabs.Length - 1)
                    GUILayout.Space(hostTabGap);
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(8);

            if (currentHostOnlySubTab == 0) DrawLobbyControls();
            else if (currentHostOnlySubTab == 1) DrawPlayersRoles();
            else if (currentHostOnlySubTab == 2) DrawAntiCheatTab();
            else if (currentHostOnlySubTab == 3) DrawAutoHostTab();
            else if (currentHostOnlySubTab == 4) DrawBugRoomTab();
            else if (currentHostOnlySubTab == 5) DrawMapsTab();
        }

private void DrawVisualsInGame()
        {
            GUILayout.BeginVertical(menuCardStyle);
            DrawMenuSectionHeader(L("VISIBILITY", "ВИДИМОСТЬ"));
            GUILayout.BeginHorizontal();
            seeGhosts = DrawToggle(seeGhosts, L("See Ghosts", "Видеть призраков"), 210);
            seePhantoms = DrawToggle(seePhantoms, L("See Phantoms", "Видеть фантомов"), 210);
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            seeRoles = DrawToggle(seeRoles, L("See Roles", "Видеть роли"), 210);
            SeePlayersInVent = DrawToggle(SeePlayersInVent, L("See Players In Vents", "Видеть игроков в люках"), 210);
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            bool previousSeeProtections = seeProtections;
            seeProtections = DrawToggle(seeProtections, L("See Protections", "Видеть щиты"), 210);
            if (!previousSeeProtections && seeProtections)
                PlayerControl_TurnOnProtection_Patch.RefreshVisibleProtections();
            fullBright = DrawToggle(fullBright, L("Full Bright (No Shadows)", "Полная яркость (Нет теней)"), 210);
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            DrawMenuSectionHeader("ESP");
            GUILayout.BeginHorizontal();
            showPlayerInfo = DrawToggle(showPlayerInfo, L("Show Player Info", "Информация об игроках"), 210);
            showEspFriendCode = DrawToggle(showEspFriendCode, L("Show FC In ESP", "FriendCode в ESP"), 210);
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            showEspBoxes = DrawToggle(showEspBoxes, "ESP Boxes", 210);
            espShimmerMode = DrawToggle(espShimmerMode, "ESP Shimmer", 210);
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            showEspVoteKicks = DrawToggle(showEspVoteKicks, "Vote Kicks ESP", 210);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            revealMeetingRoles = DrawToggle(revealMeetingRoles, L("Reveal Roles (Meeting)", "Роли на собрании"), 210);
            RevealVotesEnabled = DrawToggle(RevealVotesEnabled, L("Reveal Votes (Meeting)", "Голоса на собрании"), 210);
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            seeKillCooldown = DrawToggle(seeKillCooldown, L("See Kill Cooldown", "Видеть килл-кд"), 210);
            moreLobbyInfo = DrawToggle(moreLobbyInfo, L("Lobby Host Info", "Информация о хосте лобби"), 210);
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            DrawMenuSectionHeader("RADAR");
            GUILayout.BeginHorizontal();
            showRadar = DrawToggle(showRadar, "Show Radar", 210);
            radarDrawIcons = DrawToggle(radarDrawIcons, "Draw Icons", 210, "radarDrawIcons");
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            showRadarDeadBodies = DrawToggle(showRadarDeadBodies, "Show Dead Bodies", 210);
            showRadarGhosts = DrawToggle(showRadarGhosts, "Show Ghosts", 210);
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            radarRightClickTp = DrawToggle(radarRightClickTp, "Right Click TP", 210);
            hideRadarInMeeting = DrawToggle(hideRadarInMeeting, "Hide In Meeting", 210);
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            lockRadar = DrawToggle(lockRadar, "Lock Position", 210);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(6);

            GUIStyle radarSliderLabel = new GUIStyle(toggleLabelStyle) { richText = true };
            float sliderRowW = GetMenuWorkWidth(180f, 390f);
            float sliderLabelW = Mathf.Min(96f, Mathf.Max(76f, sliderRowW * 0.32f));
            float sliderW = Mathf.Max(90f, sliderRowW - sliderLabelW - 10f);
            float oldRadarScale = radarScale;
            float oldRadarAlpha = radarAlpha;
            GUILayout.BeginHorizontal(GUILayout.Width(sliderRowW));
            GUILayout.Label($"Scale: <color=#{GetMenuAccentHex()}>{radarScale:F2}</color>", radarSliderLabel, GUILayout.Width(sliderLabelW), GUILayout.Height(22));
            radarScale = GUILayout.HorizontalSlider(radarScale, 0.65f, 1.6f, sliderStyle, sliderThumbStyle, GUILayout.Width(sliderW));
            GUILayout.EndHorizontal();
            GUILayout.Space(2);
            GUILayout.BeginHorizontal(GUILayout.Width(sliderRowW));
            GUILayout.Label($"Alpha: <color=#{GetMenuAccentHex()}>{radarAlpha:F2}</color>", radarSliderLabel, GUILayout.Width(sliderLabelW), GUILayout.Height(22));
            radarAlpha = GUILayout.HorizontalSlider(radarAlpha, 0.2f, 1f, sliderStyle, sliderThumbStyle, GUILayout.Width(sliderW));
            GUILayout.EndHorizontal();
            if (Mathf.Abs(oldRadarScale - radarScale) > 0.001f || Mathf.Abs(oldRadarAlpha - radarAlpha) > 0.001f)
                settingsDirty = true;
            GUILayout.Space(10);

            DrawMenuSectionHeader("REPLAY");
            GUILayout.BeginHorizontal();
            showReplay = DrawToggle(showReplay, "Show Replay", 210);
            replayDrawIcons = DrawToggle(replayDrawIcons, "Draw Icons", 210, "replayDrawIcons");
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            replayOnlyLastSeconds = DrawToggle(replayOnlyLastSeconds, "Only Last Seconds", 210);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(4);

            float oldReplaySeconds = replaySeconds;
            GUILayout.BeginHorizontal(GUILayout.Width(sliderRowW));
            GUILayout.Label($"Seconds: <color=#{GetMenuAccentHex()}>{Mathf.RoundToInt(replaySeconds)}</color>", radarSliderLabel, GUILayout.Width(sliderLabelW), GUILayout.Height(22));
            replaySeconds = GUILayout.HorizontalSlider(replaySeconds, 5f, 180f, sliderStyle, sliderThumbStyle, GUILayout.Width(sliderW));
            GUILayout.EndHorizontal();
            if (Mathf.Abs(oldReplaySeconds - replaySeconds) > 0.5f)
                settingsDirty = true;
            GUILayout.Space(10);

            DrawMenuSectionHeader(L("TRACERS", "ТРАССЕРЫ"));
            GUILayout.BeginHorizontal();
            showTracers = DrawToggle(showTracers, L("All Tracers", "Все трассеры"), 210);
            showCrewmateTracers = DrawToggle(showCrewmateTracers, L("Crewmate Tracers", "Трассеры экипажа"), 210);
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            showImpostorTracers = DrawToggle(showImpostorTracers, L("Impostor Tracers", "Трассеры предателей"), 210);
            showDeadTracers = DrawToggle(showDeadTracers, L("Dead / Ghost Tracers", "Трассеры мёртвых"), 210);
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            showBodyTracers = DrawToggle(showBodyTracers, L("Body Tracers", "Трассеры трупов"), 210);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            DrawMenuSectionHeader(L("OTHER", "ДРУГОЕ"));
            GUILayout.BeginHorizontal();
            alwaysShowLobbyTimer = DrawToggle(alwaysShowLobbyTimer, L("Always Show Lobby Timer", "Всегда показывать таймер лобби"), 210);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            alwaysChat = DrawToggle(alwaysChat, L("Always Show Chat", "Всегда показывать чат"), 210);
            readGhostChat = DrawToggle(readGhostChat, L("Read Ghost Chat", "Читать чат призраков"), 210);
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            freecam = DrawToggle(freecam, L("Freecam (WASD)", "Свободная камера (WASD)"), 210);
            cameraZoom = DrawToggle(cameraZoom, L("Camera Zoom (Scroll)", "Зум камеры (Колесико)"), 210);
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

public static bool enableLocalPetSpamDrop = true;

public static bool enableHostPetSpamBan = false;

public static bool enableMalformedPacketGuard = true;

public static bool banMalformedPacketSender = false;

public static bool enableQuickChatEmptyGuard = true;

public static bool banQuickChatEmptySpammer = true;

public static bool enableUnownedSpawnGuard = true;

internal class LateTask
        {
            public string name;

            public float timer;

            public System.Action action;

            public static List<LateTask> Tasks = new List<LateTask>();

            public bool Run(float deltaTime)
            {
                timer -= deltaTime;
                if (timer <= 0f)
                {
                    action();
                    return true;
                }
                return false;
            }

            public LateTask(System.Action action, float time, string name = "No Name Task")
            {
                this.action = action;
                timer = time;
                this.name = name;
                Tasks.Add(this);
            }

            public static void Stop(string name)
            {
                Tasks.RemoveAll((LateTask task) => task.name == name);
            }

            public static void Stop(LateTask task)
            {
                Tasks.Remove(task);
            }

            public static void Update(float deltaTime)
            {
                List<LateTask> list = new List<LateTask>();
                for (int i = 0; i < Tasks.Count; i++)
                {
                    LateTask lateTask = Tasks[i];
                    try
                    {
                        if (lateTask.Run(deltaTime))
                        {
                            list.Add(lateTask);
                        }
                    }
                    catch (Exception)
                    {
                        list.Add(lateTask);
                    }
                }
                list.ForEach(delegate (LateTask task)
                {
                    Tasks.Remove(task);
                });
            }
        }

        public static bool discordRpcEnabled = true;
    }
}
