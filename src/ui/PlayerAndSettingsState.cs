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

private string[] selfSubTabs = { "SPOOF", "MOVEMENT", "ROLES", "CHAT" };

private string[] selfOtherTabs = { "MOVEMENT", "ROLES", "CHAT" };

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

public static string customNameInput = "хыхых";

public static string spoofFriendCodeInput = "----";

public static string localFriendCodeInput = "yourlocal#fc";

public static string ghostChatColorHex = "#D7B8FF";

public static bool isEditingLevel = false;

public static bool isEditingName = false;

public static bool isEditingFriendCode = false;

public static bool isEditingLocalFriendCode = false;

public static bool isEditingGhostChatColor = false;

private static bool discordLaunchStatusSent = false;

private static bool discordInvalidWebhookNotified = false;

private static float discordLaunchStatusNextTryAt = 0f;

private static readonly string relaySessionId = Guid.NewGuid().ToString("N").Substring(0, 12);

private static readonly Dictionary<string, int> watchedLogLineCounts = new Dictionary<string, int>();

private static readonly DateTime logMonitorStartedUtc = DateTime.UtcNow;

private static readonly object anomalyLogMonitorLock = new object();

private static System.Threading.Timer anomalyLogMonitorTimer;

private static string anomalyReportDetailsCache = $"sessionId={relaySessionId}\nclientId=Unknown\nnetworkMode=Unknown\nhost=Unknown\nplatform=Unknown\ninGame=Unknown";

private static float logMonitorNextScanAt = 0f;

private static float logBurstWindowStartedAt = -1f;

private static float logBurstCooldownUntil = 0f;

private static int logBurstLineCount = 0;

private static bool anomalyLogWatchNotified = false;

private const int LogBurstLineThreshold = 15;

private const int InitialLogTailLineLimit = 120;

private const float LogBurstWindowSeconds = 5f;

private const float LogBurstScanIntervalSeconds = 1f;

private const float LogBurstAlertCooldownSeconds = 60f;

private static readonly object rawLogDiagnosticLock = new object();

private static DateTime rawLogWindowStartedUtc = DateTime.MinValue;

private static DateTime rawLogSpamNextAllowedUtc = DateTime.MinValue;

private static int rawLogWindowCount = 0;

public static bool enableLocalNameSpoof = false;

public static bool enableLocalFriendCodeSpoof = false;

public static bool enableFriendCodeSpoof = false;

public static bool enablePlatformSpoof = true;

public static bool enableAnomalyLogReports = true;

public static bool throttleDefaultLogs = true;

public static bool showEspFriendCode = true;

public static bool allowDuplicateColors = false;

public static bool autoGhostAfterStart = false;

public static bool autoBanPlatformSpoof = false;

public static bool banCustomPlatformsFromTxt = false;

public static bool autoKickLowLevelEnabled = false;

public static int autoKickMinLevel = 200;

public static int fpsLimit = 60;

public static int chatHistoryLimit = 20;

public static int currentPlatformIndex = 1;

private static float localNameRefreshTimer = 0f;

private static float localFriendCodeRefreshTimer = 0f;

private static float platformBanScanTimer = 0f;

private static int lastAppliedFpsLimit = -1;

private static bool autoGhostAppliedThisGame = false;

private static bool wasGameStartedForAutoGhost = false;

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
            }
            catch { }
        }

public static void AddToBanList(string friendCode, string puid, string name, string reason)
        {
            try
            {
                if (string.IsNullOrEmpty(friendCode)) return;

                bool alreadyBanned = false;
                string fcLower = friendCode.Trim().ToLower();

                foreach (var e in bannedEntries)
                {
                    string[] parts = e.Split('|');
                    if (parts.Length > 0 && parts[0].Trim().ToLower() == fcLower)
                    {
                        alreadyBanned = true;
                        break;
                    }
                }

                if (!alreadyBanned)
                {
                    string date = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
                    string entry = $"{friendCode}|{puid}|{name}|{date}|{reason}";
                    bannedEntries.Add(entry);
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

public static bool endlessSsDuration = false, noVitalsCooldown = false;

public static bool endlessBattery = false, endlessVentTime = false, noVentCooldown = false, noMapCooldowns = false;

public static bool unlockVents = false, walkInVents = false;

public static bool reactorSab = false, oxygenSab = false, commsSab = false, elecSab = false, unfixableLights = false;

private static bool unfixableLightsApplied = false;

public static bool autoOpenDoors = false;

public static bool moonWalk = false;

public static bool SeePlayersInVent = false;

public static bool seeGhosts = false;

public static bool seeRoles = false;

public static bool showPlayerInfo = false;

public static bool revealMeetingRoles = false;

public static bool showTracers = false;

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
            screenNotifications.Add(new ElysiumNotification(title, message, ttl));
        }

        private static string GetNotificationTextForTheme(string message)
        {
            if (!whiteMenuTheme || string.IsNullOrEmpty(message))
                return message;

            return System.Text.RegularExpressions.Regex.Replace(message, @"</?color(=#[^>]+)?>", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }



        public static HashSet<byte> forcedImpostors = new HashSet<byte>();

public static Dictionary<byte, RoleTypes> forcedPreGameRoles = new Dictionary<byte, RoleTypes>();

public static bool enablePreGameRoleForce = false;

private Vector2 preRolesListScrollPos = Vector2.zero;

private Vector2 preRolesActionScrollPos = Vector2.zero;

private byte selectedPreRoleId = 255;

public static List<PlayerControl> lockedPlayersList = new List<PlayerControl>();

public static bool LogAllRPCs = true;

public static bool blockRainbowChat = true;

public static bool blockFortegreenChat = true;

public static bool EnableCustomNotifs = true;

public static Vector2 notificationBoxSize = new Vector2(260f, 65f);

public static List<ElysiumNotification> screenNotifications = new List<ElysiumNotification>();

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
            GUILayout.BeginHorizontal();
            for (int i = 0; i < hostOnlySubTabs.Length; i++)
            {
                if (GUILayout.Button(hostOnlySubTabs[i], currentHostOnlySubTab == i ? activeSubTabStyle : subTabStyle, GUILayout.Height(18)))
                {
                    currentHostOnlySubTab = i;
                    scrollPosition = Vector2.zero;
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(8);

            if (currentHostOnlySubTab == 0) DrawLobbyControls();
            else if (currentHostOnlySubTab == 1) DrawPlayersRoles();
            else if (currentHostOnlySubTab == 2) DrawAntiCheatTab();
            else if (currentHostOnlySubTab == 3) DrawAutoHostTab();
            else if (currentHostOnlySubTab == 4) DrawMapsTab();
        }

private void DrawVisualsInGame()
        {
            GUILayout.BeginVertical(menuCardStyle);
            DrawMenuSectionHeader(L("VISIBILITY", "ВИДИМОСТЬ"));

            GUILayout.BeginHorizontal();
            seeGhosts = DrawToggle(seeGhosts, L("See Ghosts", "Видеть призраков"), 210);
            seeRoles = DrawToggle(seeRoles, L("See Roles", "Видеть роли"), 210);
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            showPlayerInfo = DrawToggle(showPlayerInfo, L("Show Player Info (ESP)", "Инфо об игроке (ESP)"), 210);
            revealMeetingRoles = DrawToggle(revealMeetingRoles, L("Reveal Roles (Meeting)", "Показывать роли на собрании"), 210);
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            showEspFriendCode = DrawToggle(showEspFriendCode, L("Show FC In ESP", "FriendCode в ESP"), 210);
            moreLobbyInfo = DrawToggle(moreLobbyInfo, L("More Lobby Info", "Больше инфо о лобби"), 210);
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            removePenalty = DrawToggle(removePenalty, L("No Disconnect Penalty", "Нет штрафа за выход"), 210);
            alwaysShowLobbyTimer = DrawToggle(alwaysShowLobbyTimer, L("Always Show Lobby Timer", "Всегда показывать таймер лобби"), 210);
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            showTracers = DrawToggle(showTracers, L("Show Tracers", "Показывать линии (Tracer)"), 210);
            fullBright = DrawToggle(fullBright, L("Full Bright (No Shadows)", "Полная яркость (Нет теней)"), 210);
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
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            RevealVotesEnabled = DrawToggle(RevealVotesEnabled, L("Reveal Votes (Meeting)", "Показывать голоса (Собрание)"), 210);
            SeePlayersInVent = DrawToggle(SeePlayersInVent, L("See Players In Vents", "Видеть игроков в люках"), 210);
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            seeProtections = DrawToggle(seeProtections, L("See Protections", "Видеть щиты"), 210);
            seeKillCooldown = DrawToggle(seeKillCooldown, L("See Kill Cooldown", "Видеть килл-кд"), 210);
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
    }
}
