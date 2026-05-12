#nullable disable
#pragma warning disable CS0162, CS0108, CS0219

using AmongUs.Data.Player;
using AmongUs.GameOptions;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using HarmonyLib;
using Hazel;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using InnerNet;
using NjordMenu;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Playables;
using static NjordMenu.NjordMenuGUI;
using static Rewired.UI.ControlMapper.ControlMapper;
using Color = UnityEngine.Color;
using Object = UnityEngine.Object;
using Vector3 = UnityEngine.Vector3;
using System.Reflection;
using TMPro;
using UnityEngine.Events;
using UnityEngine.UI;
namespace NjordMenu
{
    [BepInPlugin("com.njord.menu", "NjordMenu", "1.2.3")]
    public class Plugin : BasePlugin
    {
        public static Plugin Instance { get; private set; } = null!;
        public static string NjordFolder = "";
        public static ConfigFile MenuConfig;
        public static ConfigEntry<float> RpcSpoofDelayConfig;
        public static ConfigEntry<KeyCode> MenuKeybind;
        public static ConfigEntry<string> SpoofedLevel;
        public static ConfigEntry<bool> EnablePlatformSpoof;
        public static ConfigEntry<int> PlatformIndex;
        public static ConfigEntry<bool> ShowWatermarkConfig;
        public static ConfigEntry<int> MenuColorIndexConfig;
        public static ConfigEntry<bool> RgbMenuModeConfig;
        public static ConfigEntry<bool> UnlockCosmeticsConfig;
        public static ConfigEntry<bool> MoreLobbyInfoConfig;

        public override void Load()
        {
            Instance = this;

            NjordFolder = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "NjordMenu");
            if (!System.IO.Directory.Exists(NjordFolder))
            {
                System.IO.Directory.CreateDirectory(NjordFolder);
            }

            string banFile = System.IO.Path.Combine(NjordFolder, "NjordMenuBanList.txt");
            if (!System.IO.File.Exists(banFile))
            {
                System.IO.File.Create(banFile).Dispose();
            }

            MenuConfig = new ConfigFile(System.IO.Path.Combine(NjordFolder, "NjordMenu.cfg"), true);
            RpcSpoofDelayConfig = MenuConfig.Bind("NjordMenu.Spoofing", "RpcDelay", 4f, "");
            MenuKeybind = MenuConfig.Bind("NjordMenu.GUI", "Keybind", KeyCode.Insert, "");
            SpoofedLevel = MenuConfig.Bind("NjordMenu.Spoofing", "Level", "100", "");
            EnablePlatformSpoof = MenuConfig.Bind("NjordMenu.Spoofing", "EnablePlatformSpoof", true, "");
            PlatformIndex = MenuConfig.Bind("NjordMenu.Spoofing", "PlatformIndex", 1, "");
            ShowWatermarkConfig = MenuConfig.Bind("NjordMenu.GUI", "ShowWatermark", true, "");
            MenuColorIndexConfig = MenuConfig.Bind("NjordMenu.GUI", "MenuColorIndex", 10, "");
            RgbMenuModeConfig = MenuConfig.Bind("NjordMenu.GUI", "RgbMenuMode", false, "");
            UnlockCosmeticsConfig = MenuConfig.Bind("NjordMenu.General", "UnlockCosmetics", true, "");
            MoreLobbyInfoConfig = MenuConfig.Bind("NjordMenu.Visuals", "MoreLobbyInfo", true, "");

            ClassInjector.RegisterTypeInIl2Cpp<NjordMenuGUI>();

            var guiObject = new GameObject("NjordMenu_Object");
            UnityEngine.Object.DontDestroyOnLoad(guiObject);
            guiObject.hideFlags = HideFlags.HideAndDontSave;
            guiObject.AddComponent<NjordMenuGUI>();

            var harmony = new Harmony("com.njord.harmony");
            harmony.PatchAll();
        }
    }
    public class NjordMenuGUI : MonoBehaviour
    {
        public static string[] spoofMenuNames = { "NjordMenu", "HostGuard/TOH", "Polar", "BanMod", "Better Among Us", "Sicko Menu", "GNC", "KillNetwork (V1)", "KillNetwork (V2)", "KNM" };
        public static byte[] spoofMenuRPCs = { 89, 176, 204, 212, 151, 164, 154, 85, 150, 162 };
        public static float rpcSpoofDelay = 4f;

        public static byte selectedMorphTargetId = 255;
        public static bool unlockCosmetics = true;
        public static bool moreLobbyInfo = true;
        public static Dictionary<string, KeyCode> keyBinds = new Dictionary<string, KeyCode>();
        public static string bindingAction = "";
        private int currentGeneralSubTab = 0;
        private string[] generalSubTabs = { "INFORMATION", "KEYBINDS" };
        public static KeyCode menuToggleKey = KeyCode.Insert;
        public static KeyCode bindMassMorph = KeyCode.None;
        public static KeyCode bindSpawnLobby = KeyCode.None;
        public static KeyCode bindDespawnLobby = KeyCode.None;
        public static KeyCode bindCloseMeeting = KeyCode.None;
        public static KeyCode bindInstaStart = KeyCode.None;
        public static KeyCode bindEndCrew = KeyCode.None;
        public static KeyCode bindEndImp = KeyCode.None;
        public static KeyCode bindEndImpDC = KeyCode.None;
        public static KeyCode bindEndHnsDC = KeyCode.None;

        public static bool isWaitingForBind = false;
        public static bool isWaitBindMassMorph = false;
        public static bool isWaitBindSpawnLobby = false;
        public static bool isWaitBindDespawnLobby = false;
        public static bool isWaitBindCloseMeeting = false;
        public static bool isWaitBindInstaStart = false;
        public static bool isWaitBindEndCrew = false;
        public static bool isWaitBindEndImp = false;
        public static bool isWaitBindEndImpDC = false;
        public static bool isWaitBindEndHnsDC = false;
        public static bool SpoofMenuEnabled = false;
        public static int selectedSpoofMenuIndex = 0;
        private float uiSpoofTimer = 0f;
        public static bool noClip = false;
        public static bool tpToCursor = false;
        public static bool dragToCursor = false;
        public static float walkSpeed = 1f;
        public static bool DetailedJoinInfo = true;
        private static List<byte> lastPlayerIds = new List<byte>();
        private static Dictionary<byte, float> pendingJoinTimers = new Dictionary<byte, float>();
       
        public static float engineSpeed = 1f;
        public static bool invertControls = false;
        public static bool autoFollowCursor = false;
        public static int fakeRoleIdx = 0;
        public static RoleTypes[] forceRoleOptions = { RoleTypes.Crewmate, RoleTypes.Impostor, RoleTypes.Engineer, RoleTypes.Scientist, RoleTypes.Shapeshifter, RoleTypes.GuardianAngel };
        public static bool NoShapeshiftAnim = false;
        public static bool EndlessTracking = false;
        public static bool NoTrackingCooldown = false;
        public static bool UnlimitedInterrogateRange = false;
        public static bool noTaskMode = false;

        public static bool enableColorCommand = false;
        public static bool hostChatColor = false;
        public static Color hostChatColorValue = new Color32(0, 128, 128, 255);
        public static bool showMenu = false;
        public static Rect windowRect = new Rect(100, 100, 750, 480);
        public static bool freecam = false;
        private static bool _freecamActive = false;
        public static bool cameraZoom = false;
        public static bool RevealVotesEnabled = false;

       
        public static Color currentAccentColor = new Color(1f, 0.549f, 0f, 1f);
        public static bool rgbMenuMode = false;
        private float rgbMenuHue = 0f;
        public static bool enableBackground = false;
        public static Texture2D customMenuBg = null;
        private bool wasShowMenu = false;
        private int currentMenuColorIndex = 10;
        private string[] menuColorNames = {
            "Njord Blue", "Dark Forest", "Green", "Sea Green", "Mint", "Chartreuse",
            "Sun Yellow", "Marigold", "Old Gold",
            "Bright Amber", "Vivid Orange", "Dark Orange",
            "Blood Red",
            "Hot Pink", "Pale Mauve", "Lilac",
            "Lavender", "Deep Indigo", "Indigo",
            "Med Slate Blue", "Slate Blue", "Navy", "Slate Grey"
        };
        private Color[] menuColors = {
            new Color32(51, 51, 255, 255), new Color(0.192f, 0.290f, 0.196f, 1f), new Color(0f, 0.502f, 0f, 1f), new Color(0.235f, 0.702f, 0.443f, 1f), new Color(0.243f, 0.706f, 0.537f, 1f), new Color(0.498f, 1f, 0f, 1f),
            new Color(0.996f, 0.718f, 0.082f, 1f), new Color(0.812f, 0.651f, 0.004f, 1f),
            new Color(0.996f, 0.612f, 0.063f, 1f), new Color(0.957f, 0.455f, 0.004f, 1f), new Color(1f, 0.549f, 0f, 1f),
            new Color(0.871f, 0.071f, 0.149f, 1f),
            new Color(0.992f, 0.529f, 0.859f, 1f), new Color(0.882f, 0.678f, 0.800f, 1f), new Color(0.784f, 0.635f, 0.784f, 1f),
            new Color(0.925f, 0.686f, 0.996f, 1f), new Color(0.314f, 0.267f, 0.675f, 1f), new Color(0.294f, 0f, 0.51f, 1f),
            new Color(0.482f, 0.408f, 0.933f, 1f), new Color(0.416f, 0.353f, 0.804f, 1f), new Color(0f, 0f, 0.502f, 1f), new Color(0.439f, 0.502f, 0.565f, 1f)
        };

        public static float speedMultiplier = 1f;
        public static bool noSettingLimit = false;
        public static float globalRoomColorId = 0f;
        private int currentHostOnlySubTab = 0;
        private string[] hostOnlySubTabs = { "LOBBY CONTROLS", "ROLE MANAGER", "ANTI CHEAT", "AUTO HOST" };
        private string[] tabNames = { "GENERAL", "SELF", "VISUALS", "PLAYERS", "ROLES", "SABOTAGES", "HOST ONLY", "OUTFITS", "MENU" };
        private int currentTab = 0;
        private int targetTabIndex = 0;
        private float tabTransitionProgress = 1f;
        private Vector2 scrollPosition = Vector2.zero;
        private void DrawAutoHostMainTab()
        {
            GUILayout.BeginHorizontal();
            for (int i = 0; i < autoHostSubTabs.Length; i++)
            {
                if (GUILayout.Button(autoHostSubTabs[i], currentAutoHostSubTab == i ? activeSubTabStyle : subTabStyle, GUILayout.Height(18)))
                {
                    currentAutoHostSubTab = i;
                    scrollPosition = Vector2.zero;
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(8);

            if (currentAutoHostSubTab == 0) DrawLobbyControls();
            else if (currentAutoHostSubTab == 1) DrawPlayersRoles();
            else if (currentAutoHostSubTab == 2) DrawAntiCheatTab();
            else if (currentAutoHostSubTab == 3) DrawAutoHostTab();
        }

        private void DrawAutoHostTab()
        {
            GUILayout.BeginVertical(boxStyle);
            GUILayout.Label("AUTO HOST SYSTEM", headerStyle);

            var snapshot = NjordAutoHostService.GetStatusSnapshot();
            GUILayout.Label($"<color=#aaaaaa>Status:</color> <color=#FFAC1C>{snapshot.State}</color>", new GUIStyle(GUI.skin.label) { richText = true, fontSize = 13 });
            GUILayout.Space(10);

            AutoHostEnabled = DrawToggle(AutoHostEnabled, "Enable Auto Host", 250);
            GUILayout.Space(5);
            AutoReturnLobbyAfterMatch = DrawToggle(AutoReturnLobbyAfterMatch, "Auto Return To Lobby", 250);
            GUILayout.Space(5);
            AutoHostNotifications = DrawToggle(AutoHostNotifications, "Show Notifications", 250);
            GUILayout.Space(5);
            AutoHostWaitLoadedPlayers = DrawToggle(AutoHostWaitLoadedPlayers, "Wait For Players To Load", 250);
            GUILayout.Space(5);
            AutoHostCancelBelowMin = DrawToggle(AutoHostCancelBelowMin, "Cancel Countdown If Player Leaves", 250);
            GUILayout.Space(5);
            AutoHostInstantStart = DrawToggle(AutoHostInstantStart, "Instant Start (No 5s Wait)", 250);
            GUILayout.Space(5);
            AutoHostForceLastMinute = DrawToggle(AutoHostForceLastMinute, "Force Start Last Minute", 250);

            GUILayout.Space(15);

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Min Players: {AutoHostMinPlayers}", GUILayout.Width(160));
            AutoHostMinPlayers = (int)GUILayout.HorizontalSlider(AutoHostMinPlayers, 1f, 15f, sliderStyle, sliderThumbStyle, GUILayout.Width(350));
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Start Delay: {AutoHostStartDelaySeconds}s", GUILayout.Width(160));
            AutoHostStartDelaySeconds = GUILayout.HorizontalSlider(AutoHostStartDelaySeconds, 0f, 180f, sliderStyle, sliderThumbStyle, GUILayout.Width(350));
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Fast Start Players: {AutoHostFastStartPlayers}", GUILayout.Width(160));
            AutoHostFastStartPlayers = (int)GUILayout.HorizontalSlider(AutoHostFastStartPlayers, 0f, 15f, sliderStyle, sliderThumbStyle, GUILayout.Width(350));
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Fast Start Delay: {AutoHostFastStartDelaySeconds}s", GUILayout.Width(160));
            AutoHostFastStartDelaySeconds = GUILayout.HorizontalSlider(AutoHostFastStartDelaySeconds, 0f, 60f, sliderStyle, sliderThumbStyle, GUILayout.Width(350));
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }
        public static class NjordAutoLobbyReturn
        {
            private const float AutoReturnDelaySeconds = 3f;
            private const float AutoReturnRetrySeconds = 0.4f;
            private const int AutoReturnMaxAttempts = 40;

            private static int trackedEndGameId;
            private static int exhaustedEndGameId;
            private static int attempt;
            private static float nextAttemptAt;
            private static bool pending;

            public static void UpdateLogic()
            {
                if (!ShouldAutoReturn())
                {
                    ResetState();
                    return;
                }
                if (LobbyBehaviour.Instance != null)
                {
                    ResetState();
                    return;
                }

                EndGameManager val = UnityEngine.Object.FindObjectOfType<EndGameManager>();
                if (val != null)
                {
                    int instanceID = val.gameObject.GetInstanceID();
                    if (trackedEndGameId != instanceID)
                    {
                        trackedEndGameId = instanceID;
                        exhaustedEndGameId = 0;
                        attempt = 0;
                        nextAttemptAt = Time.unscaledTime + AutoReturnDelaySeconds;
                        pending = true;
                    }
                }
                else if (trackedEndGameId == 0) return;

                if (!pending || exhaustedEndGameId == trackedEndGameId || Time.unscaledTime < nextAttemptAt)
                    return;

                bool flag = false;
                if (val != null)
                {
                    flag = TryInvokeEndGameAction(val);
                    flag = TryClickEndGameButtons(val) || flag;
                }
                flag = TryClickGlobalReturnButtons() || flag;

                if (LobbyBehaviour.Instance != null)
                {
                    ResetState();
                    return;
                }

                attempt++;
                if (attempt >= AutoReturnMaxAttempts)
                    pending = false;
                else
                    nextAttemptAt = Time.unscaledTime + AutoReturnRetrySeconds;
            }

            public static void ResetState()
            {
                trackedEndGameId = 0;
                exhaustedEndGameId = 0;
                attempt = 0;
                nextAttemptAt = 0f;
                pending = false;
            }

            private static bool ShouldAutoReturn()
            {
                return NjordMenuGUI.AutoReturnLobbyAfterMatch || NjordAutoHostService.ShouldReturnAfterMatch;
            }

            private static bool TryInvokeEndGameAction(EndGameManager manager)
            {
                if (manager == null) return false;
                string[] methods = new string[] { "Continue", "NextGame", "PlayAgain" };
                for (int i = 0; i < methods.Length; i++)
                {
                    System.Reflection.MethodInfo methodInfo = FindMethodNoWarn(manager.GetType(), methods[i], Type.EmptyTypes);
                    if (methodInfo != null)
                    {
                        try { methodInfo.Invoke(manager, null); return true; }
                        catch { }
                    }
                }
                return false;
            }

            private static System.Reflection.MethodInfo FindMethodNoWarn(Type type, string name, Type[] parameters)
            {
                if (type == null || string.IsNullOrWhiteSpace(name)) return null;
                Type[] types = parameters ?? Type.EmptyTypes;
                Type t = type;
                while (t != null)
                {
                    System.Reflection.MethodInfo method = t.GetMethod(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic, null, types, null);
                    if (method != null) return method;
                    t = t.BaseType;
                }
                return null;
            }

            private static bool TryClickEndGameButtons(EndGameManager manager)
            {
                if (manager == null) return false;
                if (TryClickPassiveButtons(manager.GetComponentsInChildren<PassiveButton>(true), true))
                    return true;
                return TryClickUnityButtons(manager.GetComponentsInChildren<UnityEngine.UI.Button>(true), true);
            }

            private static bool TryClickGlobalReturnButtons()
            {
                if (TryClickPassiveButtons(UnityEngine.Object.FindObjectsOfType<PassiveButton>(), true))
                    return true;
                return TryClickUnityButtons(UnityEngine.Object.FindObjectsOfType<UnityEngine.UI.Button>(), true);
            }

            private static bool TryClickPassiveButtons(Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppArrayBase<PassiveButton> buttons, bool onlyActive)
            {
                if (buttons == null) return false;
                foreach (PassiveButton btn in buttons)
                {
                    if (btn == null) continue;
                    if (onlyActive && (!btn.gameObject.activeInHierarchy || !btn.isActiveAndEnabled))
                        continue;
                    if (!IsLobbyReturnButton(btn.name, btn.GetComponentsInChildren<TMPro.TMP_Text>(true)))
                        continue;
                    try
                    {
                        if (btn.OnClick != null)
                        {
                            btn.OnClick.Invoke();
                            return true;
                        }
                    }
                    catch { }
                }
                return false;
            }

            private static bool TryClickUnityButtons(Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppArrayBase<UnityEngine.UI.Button> buttons, bool onlyActive)
            {
                if (buttons == null) return false;
                foreach (UnityEngine.UI.Button btn in buttons)
                {
                    if (btn == null) continue;
                    if (onlyActive && (!btn.gameObject.activeInHierarchy || !btn.isActiveAndEnabled || !btn.interactable))
                        continue;
                    if (!IsLobbyReturnButton(btn.name, btn.GetComponentsInChildren<TMPro.TMP_Text>(true)))
                        continue;
                    try
                    {
                        if (btn.onClick != null)
                        {
                            btn.onClick.Invoke();
                            return true;
                        }
                    }
                    catch { }
                }
                return false;
            }

            private static bool IsLobbyReturnButton(string objectName, Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppArrayBase<TMPro.TMP_Text> texts)
            {
                string input = (objectName ?? string.Empty).ToLowerInvariant();
                if (ContainsAny(input, "exit", "quit", "menu", "back", "leave", "вых", "выйт", "назад"))
                    return false;
                if (ContainsAny(input, "continue", "nextgame", "playagain", "returntolobby", "tolobby", "lobby", "again", "продолж", "занов", "снов", "лобби", "играть", "вернут"))
                    return true;
                if (texts == null) return false;
                foreach (TMPro.TMP_Text txt in texts)
                {
                    if (txt == null) continue;
                    string stripped = StripRichText(txt.text).ToLowerInvariant();
                    if (ContainsAny(stripped, "exit", "quit", "menu", "back", "leave", "вых", "выйт", "назад"))
                        return false;
                    if (ContainsAny(stripped, "continue", "next game", "play again", "return to lobby", "lobby", "again", "продолж", "занов", "снов", "лобби", "играть", "вернут"))
                        return true;
                }
                return false;
            }

            private static bool ContainsAny(string input, params string[] tokens)
            {
                if (string.IsNullOrEmpty(input)) return false;
                foreach (string token in tokens)
                    if (!string.IsNullOrWhiteSpace(token) && input.Contains(token))
                        return true;
                return false;
            }

            private static string StripRichText(string input)
            {
                if (string.IsNullOrEmpty(input)) return string.Empty;
                char[] chars = new char[input.Length];
                int length = 0;
                bool inTag = false;
                foreach (char c in input)
                {
                    switch (c)
                    {
                        case '<': inTag = true; continue;
                        case '>': inTag = false; continue;
                    }
                    if (!inTag) chars[length++] = c;
                }
                return new string(chars, 0, length);
            }
        }

        public static class NjordAutoHostService
        {
            public sealed class AutoHostStatusSnapshot
            {
                public bool Enabled;
                public bool IsHost;
                public bool IsLobby;
                public bool IsInGame;
                public string State = string.Empty;
                public string LastReason = string.Empty;
                public int ConnectedPlayers;
                public int ReadyPlayers;
                public int RequiredPlayers;
                public float CountdownRemainingSeconds;
                public float BackoffRemainingSeconds;
                public float LobbyAgeSeconds;
                public float LobbyLifeRemainingSeconds = -1f;
                public bool WaitingForLoadedPlayers;
                public bool AutoReturnAfterMatch;
                public bool ForceLastMinute;
                public string StartMode = string.Empty;
                public float EffectiveStartDelaySeconds;
                public float WarmupRemainingSeconds;
                public float LoadGraceRemainingSeconds;
                public bool FastStartActive;
                public bool ForceStartActive;
            }

            private enum AutoHostState
            {
                Disabled, Idle, Warmup, WaitingPlayers, WaitingLoad,
                Countdown, Starting, InGame, Returning, Backoff,
            }

            private const float TickIntervalSeconds = 0.2f;
            private const float StartRequestGraceSeconds = 7f;
            private const float LobbyLifetimeSeconds = 600f;
            private const float LastMinuteStartSeconds = 60f;
            private const float NotificationCooldownSeconds = 0.75f;

            private static AutoHostState state = AutoHostState.Disabled;
            private static string lastReason = "disabled";
            private static float nextTickAt;
            private static float countdownStartedAt = -1f;
            private static float activeCountdownDelay = -1f;
            private static float backoffUntil = -1f;
            private static float lastStartIssuedAt = -1f;
            private static float lobbyOpenedAt = -1f;
            private static float loadWaitStartedAt = -1f;
            private static float lastNotificationAt = -1f;
            private static int lobbyGameId = -1;
            private static int lastCountdownNotice = -1;

            public static void Tick()
            {
                float now = Time.unscaledTime;
                if (now < nextTickAt) return;
                nextTickAt = now + TickIntervalSeconds;

                if (!IsEnabled)
                {
                    ResetLobbyFlow(true);
                    SetState(AutoHostState.Disabled, "Выключен");
                    return;
                }

                InnerNetClient client = TryGetClient();
                if (client == null)
                {
                    ResetLobbyFlow(false);
                    SetState(AutoHostState.Idle, "Клиент недоступен");
                    return;
                }

                if (!client.AmHost)
                {
                    ResetLobbyFlow(false);
                    SetState(AutoHostState.Idle, "Ожидаю хост-контекст");
                    return;
                }

                if (IsEndGameScreen())
                {
                    ResetLobbyFlow(false);
                    SetState(ShouldReturnAfterMatch ? AutoHostState.Returning : AutoHostState.InGame,
                        ShouldReturnAfterMatch ? "Возврат в лобби" : "Матч завершен");
                    return;
                }

                if (IsInMatch())
                {
                    ResetLobbyFlow(true);
                    SetState(AutoHostState.InGame, "Матч идет");
                    return;
                }

                if (LobbyBehaviour.Instance == null)
                {
                    ResetLobbyFlow(false);
                    lobbyOpenedAt = -1f;
                    lobbyGameId = -1;
                    SetState(AutoHostState.Idle, "Вне лобби");
                    return;
                }

                TrackLobby(client, now);
                TickHostedLobby(client, now);
            }

            public static AutoHostStatusSnapshot GetStatusSnapshot()
            {
                AutoHostStatusSnapshot snapshot = new AutoHostStatusSnapshot
                {
                    Enabled = IsEnabled,
                    State = FormatState(state),
                    LastReason = lastReason ?? string.Empty,
                    RequiredPlayers = RequiredPlayers,
                    CountdownRemainingSeconds = CountdownRemaining,
                    BackoffRemainingSeconds = BackoffRemaining,
                    LobbyAgeSeconds = lobbyOpenedAt > 0f ? Mathf.Max(0f, Time.unscaledTime - lobbyOpenedAt) : 0f,
                    LobbyLifeRemainingSeconds = LobbyLifeRemaining,
                    AutoReturnAfterMatch = ShouldReturnAfterMatch,
                    ForceLastMinute = ForceLastMinuteEnabled,
                    StartMode = NjordMenuGUI.AutoHostInstantStart ? "Мгновенный" : "Обычный",
                    EffectiveStartDelaySeconds = EffectiveStartDelay(0),
                    WarmupRemainingSeconds = WarmupRemaining,
                    LoadGraceRemainingSeconds = LoadGraceRemaining,
                };
                InnerNetClient client = TryGetClient();
                if (client != null)
                {
                    snapshot.IsHost = client.AmHost;
                    snapshot.IsLobby = LobbyBehaviour.Instance != null;
                    snapshot.IsInGame = IsInMatch();
                    snapshot.ConnectedPlayers = CountLobbyPlayers(client, out int readyPlayers, out _);
                    snapshot.ReadyPlayers = readyPlayers;
                    snapshot.WaitingForLoadedPlayers = snapshot.ConnectedPlayers > snapshot.ReadyPlayers;
                    snapshot.FastStartActive = IsFastStartActive(snapshot.ConnectedPlayers);
                    snapshot.ForceStartActive = ShouldForceStart(snapshot.ConnectedPlayers, out _);
                    snapshot.EffectiveStartDelaySeconds = EffectiveStartDelay(snapshot.ConnectedPlayers);
                }
                return snapshot;
            }

            public static void ResetTransientState()
            {
                nextTickAt = 0f;
                ResetLobbyFlow(true);
                SetState(IsEnabled ? AutoHostState.Idle : AutoHostState.Disabled, IsEnabled ? "Сброшен" : "Выключен");
            }

            public static string TryStartNow()
            {
                if (!IsEnabled) return "Автохост выключен.";
                InnerNetClient client = TryGetClient();
                if (client == null || !client.AmHost) return "Ручной старт доступен только хосту.";
                if (LobbyBehaviour.Instance == null) return "Ручной старт доступен только в лобби.";
                GameStartManager manager = TryGetGameStartManager();
                if (manager == null) return "Кнопка старта не найдена.";

                if (!TryConfiguredStart(manager))
                {
                    EnterBackoff("Ручной старт отклонен");
                    return "Старт не сработал.";
                }
                lastStartIssuedAt = Time.unscaledTime;
                countdownStartedAt = -1f;
                activeCountdownDelay = -1f;
                backoffUntil = -1f;
                SetState(AutoHostState.Starting, "Ручной старт");
                Notify("Автохост", "Матч запускается вручную.");
                return "Старт отправлен.";
            }

            private static void TickHostedLobby(InnerNetClient client, float now)
            {
                int connectedPlayers = CountLobbyPlayers(client, out int readyPlayers, out string loadingName);
                bool forceStart = ShouldForceStart(connectedPlayers, out string forceReason);
                float warmupRemaining = WarmupRemaining;

                if (!forceStart && warmupRemaining > 0.05f)
                {
                    countdownStartedAt = -1f;
                    activeCountdownDelay = -1f;
                    lastStartIssuedAt = -1f;
                    lastCountdownNotice = -1;
                    SetState(AutoHostState.Warmup, $"Прогрев лобби {Mathf.CeilToInt(warmupRemaining)}с");
                    return;
                }

                bool waitingForLoad = NjordMenuGUI.AutoHostWaitLoadedPlayers && connectedPlayers > readyPlayers;
                if (waitingForLoad && !forceStart && !CanBypassLoadWait(now, readyPlayers, connectedPlayers, loadingName))
                {
                    countdownStartedAt = -1f;
                    activeCountdownDelay = -1f;
                    lastStartIssuedAt = -1f;
                    lastCountdownNotice = -1;
                    SetState(AutoHostState.WaitingLoad, $"Ожидаю прогрузку {readyPlayers}/{connectedPlayers}: {loadingName}");
                    return;
                }
                if (!waitingForLoad) loadWaitStartedAt = -1f;

                if (lastStartIssuedAt > 0f)
                {
                    if (now - lastStartIssuedAt < StartRequestGraceSeconds)
                    {
                        SetState(AutoHostState.Starting, "Старт отправлен");
                        return;
                    }
                    lastStartIssuedAt = -1f;
                    EnterBackoff("Старт не подтвердился");
                    return;
                }

                if (backoffUntil > now)
                {
                    SetState(AutoHostState.Backoff, "Пауза после попытки");
                    return;
                }

                int requiredPlayers = RequiredPlayers;
                bool enoughPlayers = NjordMenuGUI.AutoHostWaitLoadedPlayers ? readyPlayers >= requiredPlayers : connectedPlayers >= requiredPlayers;
                bool continueBelowMin = !NjordMenuGUI.AutoHostCancelBelowMin && countdownStartedAt >= 0f && connectedPlayers >= 2;

                if (!forceStart && !enoughPlayers && !continueBelowMin)
                {
                    if (countdownStartedAt >= 0f)
                        Notify("Автохост", "Отсчет отменен: игроков стало меньше минимума.");
                    countdownStartedAt = -1f;
                    activeCountdownDelay = -1f;
                    lastCountdownNotice = -1;
                    SetState(AutoHostState.WaitingPlayers, $"Игроки {connectedPlayers}/{requiredPlayers}");
                    return;
                }

                float delay = EffectiveStartDelay(connectedPlayers);
                if (!forceStart && countdownStartedAt < 0f)
                {
                    countdownStartedAt = now;
                    activeCountdownDelay = delay;
                    lastCountdownNotice = -1;
                    SetState(AutoHostState.Countdown, IsFastStartActive(connectedPlayers) ? "Быстрый старт" : "Минимум игроков набран");
                    Notify("Автохост", $"Старт через {Mathf.CeilToInt(delay)} с.");
                }

                if (!forceStart && now - countdownStartedAt < delay)
                {
                    AnnounceCountdown(delay - (now - countdownStartedAt));
                    SetState(AutoHostState.Countdown, "Отсчет");
                    return;
                }

                GameStartManager manager = TryGetGameStartManager();
                if (manager == null)
                {
                    EnterBackoff("Кнопка старта не найдена");
                    return;
                }
                if (!TryConfiguredStart(manager))
                {
                    EnterBackoff(forceStart ? "Форс-старт отклонен" : "Старт отклонен");
                    return;
                }

                countdownStartedAt = -1f;
                activeCountdownDelay = -1f;
                backoffUntil = -1f;
                lastStartIssuedAt = now;
                lastCountdownNotice = -1;
                SetState(AutoHostState.Starting, forceStart ? forceReason : "Старт матча");
                Notify("Автохост", forceStart ? forceReason : "Минимум набран, запускаю матч.");
            }

            private static void TrackLobby(InnerNetClient client, float now)
            {
                int gameId;
                try { gameId = client.GameId; } catch { gameId = 0; }
                if (lobbyOpenedAt >= 0f && lobbyGameId == gameId) return;
                lobbyOpenedAt = now;
                lobbyGameId = gameId;
                ResetLobbyFlow(true);
                SetState(AutoHostState.WaitingPlayers, "Новое лобби");
            }

            private static void AnnounceCountdown(float remaining)
            {
                int whole = Mathf.CeilToInt(Mathf.Max(0f, remaining));
                if (whole == lastCountdownNotice) return;
                if (whole == 60 || whole == 30 || whole == 15 || whole == 10 || whole == 5 || whole == 3 || whole == 2 || whole == 1)
                {
                    lastCountdownNotice = whole;
                    Notify("Автохост", $"Старт через {whole} с.");
                }
            }

            private static bool TryConfiguredStart(GameStartManager manager)
            {
                if (manager == null || AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost || LobbyBehaviour.Instance == null)
                    return false;
                try
                {
                    manager.MinPlayers = 1;
                    if (NjordMenuGUI.AutoHostInstantStart)
                    {
                        manager.startState = GameStartManager.StartingStates.Countdown;
                        manager.countDownTimer = 0f;
                        return true;
                    }
                    manager.BeginGame();
                    return true;
                }
                catch { return false; }
            }

            private static void EnterBackoff(string reason)
            {
                countdownStartedAt = -1f;
                activeCountdownDelay = -1f;
                lastStartIssuedAt = -1f;
                loadWaitStartedAt = -1f;
                lastCountdownNotice = -1;
                backoffUntil = Time.unscaledTime + BackoffSeconds;
                SetState(AutoHostState.Backoff, reason);
                Notify("Автохост: пауза", reason);
            }

            private static void ResetLobbyFlow(bool clearBackoff)
            {
                countdownStartedAt = -1f;
                lastStartIssuedAt = -1f;
                lastCountdownNotice = -1;
                if (clearBackoff) backoffUntil = -1f;
            }

            private static void SetState(AutoHostState nextState, string reason)
            {
                if (!string.IsNullOrWhiteSpace(reason)) lastReason = reason.Trim();
                state = nextState;
            }

            private static int CountLobbyPlayers(InnerNetClient client, out int readyPlayers, out string loadingName)
            {
                readyPlayers = 0;
                loadingName = "игрок";
                if (client == null || client.allClients == null) return 0;

                int connected = 0;
                try
                {
                    var cursor = client.allClients.GetEnumerator();
                    while (cursor.MoveNext())
                    {
                        ClientData data = cursor.Current;
                        if (data == null || data.Id < 0) continue;
                        if (IsDisconnected(data)) continue;
                        connected++;
                        if (IsReady(data)) readyPlayers++;
                        else loadingName = CleanName(data.PlayerName);
                    }
                }
                catch { return CountReadyPlayerControls(out readyPlayers); }
                return connected;
            }

            private static int CountReadyPlayerControls(out int readyPlayers)
            {
                readyPlayers = 0;
                try
                {
                    if (PlayerControl.AllPlayerControls == null) return 0;
                    int count = 0;
                    var cursor = PlayerControl.AllPlayerControls.GetEnumerator();
                    while (cursor.MoveNext())
                    {
                        PlayerControl player = cursor.Current;
                        if (player == null || player.Data == null || player.Data.Disconnected || player.PlayerId >= 100) continue;
                        count++;
                        readyPlayers++;
                    }
                    return count;
                }
                catch { return 0; }
            }

            private static bool IsReady(ClientData data)
            {
                try
                {
                    PlayerControl character = data.Character;
                    return character != null && character.Data != null && !character.Data.Disconnected && character.PlayerId < 100;
                }
                catch { return false; }
            }

            private static bool IsDisconnected(ClientData data)
            {
                try { return data.Character != null && data.Character.Data != null && data.Character.Data.Disconnected; }
                catch { return false; }
            }

            private static GameStartManager TryGetGameStartManager()
            {
                try { if (DestroyableSingleton<GameStartManager>.InstanceExists) return DestroyableSingleton<GameStartManager>.Instance; } catch { }
                try { return UnityEngine.Object.FindObjectOfType<GameStartManager>(); } catch { return null; }
            }

            private static InnerNetClient TryGetClient()
            {
                try { return AmongUsClient.Instance == null ? null : (InnerNetClient)AmongUsClient.Instance; } catch { return null; }
            }

            private static bool CanBypassLoadWait(float now, int readyPlayers, int connectedPlayers, string loadingName)
            {
                if (readyPlayers < RequiredPlayers) { loadWaitStartedAt = -1f; return false; }
                int grace = Mathf.Clamp((int)NjordMenuGUI.AutoHostLoadGraceSeconds, 0, 90);
                if (grace <= 0) { loadWaitStartedAt = -1f; return false; }
                if (loadWaitStartedAt < 0f) loadWaitStartedAt = now;
                if (now - loadWaitStartedAt < grace)
                {
                    SetState(AutoHostState.WaitingLoad, $"Жду прогрузку {readyPlayers}/{connectedPlayers}: {loadingName}");
                    return false;
                }
                SetState(AutoHostState.Countdown, "Прогрузка задержалась, старт по готовым");
                return true;
            }

            private static bool ShouldForceStart(int connectedPlayers, out string reason)
            {
                int minPlayers = ForceMinPlayers;
                if (ForceLastMinuteEnabled && connectedPlayers >= minPlayers && LobbyLifeRemaining >= 0f && LobbyLifeRemaining <= LastMinuteStartSeconds)
                {
                    reason = "Форс-старт: лобби скоро закроется";
                    return true;
                }
                int forceAfterMinutes = Mathf.Clamp(NjordMenuGUI.AutoHostForceAfterMinutes, 0, 10);
                if (forceAfterMinutes > 0 && connectedPlayers >= minPlayers && lobbyOpenedAt > 0f && Time.unscaledTime - lobbyOpenedAt >= forceAfterMinutes * 60f)
                {
                    reason = $"Форс-старт: ожидание {forceAfterMinutes} мин";
                    return true;
                }
                reason = string.Empty;
                return false;
            }

            private static bool IsFastStartActive(int connectedPlayers)
            {
                int threshold = Mathf.Clamp(NjordMenuGUI.AutoHostFastStartPlayers, 0, 15);
                return threshold > 0 && connectedPlayers >= threshold;
            }

            private static float EffectiveStartDelay(int connectedPlayers)
            {
                float delay = StartDelaySeconds;
                if (IsFastStartActive(connectedPlayers))
                    delay = Mathf.Min(delay, Mathf.Clamp(NjordMenuGUI.AutoHostFastStartDelaySeconds, 0, 60));
                return delay;
            }

            private static bool IsInMatch() => ShipStatus.Instance != null && LobbyBehaviour.Instance == null && !IsEndGameScreen();

            private static bool IsEndGameScreen()
            {
                try { return UnityEngine.Object.FindObjectOfType<EndGameManager>() != null; } catch { return false; }
            }

            private static void Notify(string title, string detail)
            {
                if (!NjordMenuGUI.AutoHostNotifications) return;
                float now = Time.unscaledTime;
                if (lastNotificationAt > 0f && now - lastNotificationAt < NotificationCooldownSeconds) return;
                lastNotificationAt = now;
                NjordMenuGUI.ShowNotification($"<color=#FF00FF>[{title}]</color> {detail}");
            }

            private static string FormatState(AutoHostState value)
            {
                return value switch
                {
                    AutoHostState.Disabled => "Выключен",
                    AutoHostState.Idle => "Ожидание",
                    AutoHostState.Warmup => "Прогрев",
                    AutoHostState.WaitingPlayers => "Ждет игроков",
                    AutoHostState.WaitingLoad => "Ждет прогрузку",
                    AutoHostState.Countdown => "Отсчет",
                    AutoHostState.Starting => "Запуск",
                    AutoHostState.InGame => "В игре",
                    AutoHostState.Returning => "Возврат",
                    AutoHostState.Backoff => "Пауза",
                    _ => value.ToString(),
                };
            }

            private static string CleanName(string value)
            {
                if (string.IsNullOrWhiteSpace(value)) return "игрок";
                string clean = value.Replace("\r", " ").Replace("\n", " ").Trim();
                return clean.Length <= 18 ? clean : clean.Substring(0, 17) + "...";
            }

            public static bool IsEnabled => NjordMenuGUI.AutoHostEnabled;
            public static bool ShouldReturnAfterMatch => IsEnabled && NjordMenuGUI.AutoReturnLobbyAfterMatch;
            private static bool ForceLastMinuteEnabled => NjordMenuGUI.AutoHostForceLastMinute;
            private static int RequiredPlayers => Mathf.Clamp(NjordMenuGUI.AutoHostMinPlayers, 1, 15);
            private static int ForceMinPlayers => Mathf.Clamp(NjordMenuGUI.AutoHostForceMinPlayers, 1, 15);
            private static float StartDelaySeconds => Mathf.Clamp(NjordMenuGUI.AutoHostStartDelaySeconds, 0f, 180f);
            private static float BackoffSeconds => Mathf.Clamp(NjordMenuGUI.AutoHostBackoffSeconds, 2f, 60f);
            private static float CountdownRemaining => countdownStartedAt < 0f ? 0f : Mathf.Clamp((activeCountdownDelay >= 0f ? activeCountdownDelay : StartDelaySeconds) - (Time.unscaledTime - countdownStartedAt), 0f, StartDelaySeconds);
            private static float BackoffRemaining => backoffUntil < 0f ? 0f : Mathf.Clamp(backoffUntil - Time.unscaledTime, 0f, BackoffSeconds);
            private static float LobbyLifeRemaining => lobbyOpenedAt < 0f ? -1f : Mathf.Clamp(LobbyLifetimeSeconds - (Time.unscaledTime - lobbyOpenedAt), 0f, LobbyLifetimeSeconds);
            private static float WarmupRemaining => lobbyOpenedAt < 0f ? 0f : Mathf.Clamp(NjordMenuGUI.AutoHostWarmupSeconds - (Time.unscaledTime - lobbyOpenedAt), 0f, 120f);
            private static float LoadGraceRemaining => loadWaitStartedAt < 0f || NjordMenuGUI.AutoHostLoadGraceSeconds <= 0 ? 0f : Mathf.Clamp(NjordMenuGUI.AutoHostLoadGraceSeconds - (Time.unscaledTime - loadWaitStartedAt), 0f, 90f);
        }
        private int currentVisualsSubTab = 0;
        private string[] visualsSubTabs = { "IN-GAME" };
        private int currentSelfSubTab = 0;
        private string[] selfSubTabs = { "SPOOF", "MOVEMENT" };
        public static bool fakeStartCounterTroll = false;
        public static bool fakeStartCounterCustom = false;
        public static string fakeStartInput = "69";
        public static bool isEditingFakeStart = false;
        public static float customStartTimer = -1f;

        public static bool localRainbow = false;
        public static List<byte> rainbowPlayers = new List<byte>();
        public static float colorTimer = 0f;
        public static byte currentColorId = 0;
        private Vector2 playerListScrollPos = Vector2.zero;
        private Vector2 playerActionScrollPos = Vector2.zero;
        private byte selectedHydraPlayerId = 255;

        public static string spoofLevelString = "100";
        public static string customNameInput = "хыхых";
        public static bool isEditingLevel = false;
        public static bool isEditingName = false;
        public static bool enablePlatformSpoof = true;
        public static int currentPlatformIndex = 1;

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



        public class NjordNotification
        {
            public string title;
            public string message;
            public float ttl;
            public float lifetime;
            public bool HasExpired => lifetime > ttl;

            public NjordNotification(string title, string message, float ttl)
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
        public static bool autoBanEnabled = true;
        public static string banInput = "";
        public static bool isEditingBan = false;

        public static void LoadBanList()
        {
            try
            {
                banListPath = System.IO.Path.Combine(Plugin.NjordFolder, "NjordMenuBanList.txt");
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

        public static bool killReach = false, killAnyone = false;
        public static bool endlessSsDuration = false, noVitalsCooldown = false;
        public static bool endlessBattery = false, endlessVentTime = false, noVentCooldown = false;
        public static bool reactorSab = false, oxygenSab = false, commsSab = false, elecSab = false;
        public static bool autoOpenDoors = false;

        public static bool SeePlayersInVent = false;
        public static bool seeGhosts = false;
        public static bool seeRoles = false;
        public static bool showPlayerInfo = false;
        public static bool revealMeetingRoles = false;
        public static bool showTracers = false;
        public static bool fullBright = false;
        public static bool extendedLobby = false;
        public static bool DarkModeEnabled = false;
        public static float customLightRadius = 5f;

        public static bool alwaysChat = false;
        public static bool readGhostChat = false;

        public static bool neverEndGame = false;
        public static void ShowNotification(string text)
        {
            string title = "NjordMenu";
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
            screenNotifications.Add(new NjordNotification(title, message, ttl));
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
        public static List<NjordNotification> screenNotifications = new List<NjordNotification>();

      
      
        private bool stylesInited = false;
        private GUIStyle windowStyle, btnStyle, activeTabStyle, headerStyle, boxStyle;
        private GUIStyle sidebarStyle, sidebarBtnStyle, activeSidebarBtnStyle, titleStyle;
        private GUIStyle toggleOnStyle, toggleOffStyle, toggleLabelStyle, safeLineStyle;
        private GUIStyle sliderStyle, sliderThumbStyle, subTabStyle, activeSubTabStyle;
        public GUIStyle inputBlockStyle;
        private Texture2D texWindowBg, texBoxBg, texBtnBg, texAccent, texSidebarBg;
        private Texture2D texToggleOff, texToggleOn, texSliderBg, texSliderThumb, texInputBg, texColorBtn, texScrollThumb;
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
        }
        private void DrawAntiCheatTab()
        {
            GUILayout.BeginVertical(boxStyle);
            GUILayout.Label("ANTI CHEAT", headerStyle);
            GUILayout.Space(5);

            enableLocalPetSpamDrop = DrawToggle(enableLocalPetSpamDrop, "Block Pet Spam (Local Drop)", 250);
            GUILayout.Space(5);
            enableHostPetSpamBan = DrawToggle(enableHostPetSpamBan, "Auto-Ban Pet Spammers (Host)", 250);
            GUILayout.Space(15);

            GUILayout.Label("BAN LIST", headerStyle);
            autoBanEnabled = DrawToggle(autoBanEnabled, "Auto-Ban Blacklisted Players", 250);
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            string banDisp = isEditingBan ? banInput + "_" : (string.IsNullOrEmpty(banInput) ? "Enter Friend Code" : banInput);
            if (GUILayout.Button(banDisp, isEditingBan ? activeTabStyle : inputBlockStyle, GUILayout.Width(200f), GUILayout.Height(25f)))
            {
                isEditingBan = !isEditingBan;
                ResetAllBindWaits();
            }
            if (GUILayout.Button("ADD", btnStyle, GUILayout.Width(50f), GUILayout.Height(25f)))
            {
                if (!string.IsNullOrWhiteSpace(banInput))
                {
                    AddToBanList(banInput.Trim(), "Manual", "Unknown", "Manual ban");
                    banInput = "";
                    isEditingBan = false;
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            banListScroll = GUILayout.BeginScrollView(banListScroll, GUILayout.Height(270));

            if (bannedEntries.Count == 0)
            {
                GUILayout.Label("<color=#777777>Бан лист пуст.</color>", new GUIStyle(GUI.skin.label) { richText = true, alignment = TextAnchor.MiddleCenter });
            }
            else
            {
                for (int i = 0; i < bannedEntries.Count; i++)
                {
                    string entry = bannedEntries[i];
                    if (string.IsNullOrWhiteSpace(entry)) continue;

                    string[] parts = entry.Split('|');
                    string disp = parts.Length >= 3 ? $"{parts[2]} ({parts[0]})" : entry;

                    GUILayout.BeginHorizontal(boxStyle);
                    GUILayout.Label(disp, new GUIStyle(GUI.skin.label) { fontSize = 12 }, GUILayout.Width(220));
                    GUILayout.FlexibleSpace();

                    GUIStyle redCrossStyle = new GUIStyle(btnStyle);
                    redCrossStyle.normal.textColor = new Color(1f, 0.3f, 0.3f);

                    if (GUILayout.Button("X", redCrossStyle, GUILayout.Width(25), GUILayout.Height(22)))
                    {
                        RemoveFromBanList(entry);
                        break;
                    }
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }
        public static bool enableLocalPetSpamDrop = true;
        public static bool enableHostPetSpamBan = false;
        [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.HandleRpc))]
        public static class Shield_PetSpam_Patch
        {
            public static System.Collections.Generic.HashSet<byte> petSpamBlockedPlayers = new System.Collections.Generic.HashSet<byte>();

            public static System.Collections.Generic.Dictionary<byte, System.Collections.Generic.Queue<float>> petSpamTrackers = new System.Collections.Generic.Dictionary<byte, System.Collections.Generic.Queue<float>>();

            public static bool Prefix(PlayerPhysics __instance, byte callId, Hazel.MessageReader reader)
            {
                if (!NjordMenuGUI.enableLocalPetSpamDrop && !NjordMenuGUI.enableHostPetSpamBan) return true;

                if (callId == 49 || callId == 50)
                {
                    try
                    {
                        if (__instance == null || __instance.myPlayer == null) return true;

                        if (__instance.myPlayer == PlayerControl.LocalPlayer) return true;

                        byte pId = __instance.myPlayer.PlayerId;

                        if (petSpamBlockedPlayers.Contains(pId))
                        {
                            if (NjordMenuGUI.enableLocalPetSpamDrop) return false;
                        }

                        float now = UnityEngine.Time.time;

                        if (!petSpamTrackers.ContainsKey(pId))
                            petSpamTrackers[pId] = new System.Collections.Generic.Queue<float>();

                        var q = petSpamTrackers[pId];

                        while (q.Count > 0 && q.Peek() < now - 0.75f)
                            q.Dequeue();

                        q.Enqueue(now);

                        if (q.Count > 160)
                        {
                            petSpamBlockedPlayers.Add(pId);

                            string pName = __instance.myPlayer.Data?.PlayerName ?? "Unknown";

                            if (NjordMenuGUI.enableLocalPetSpamDrop)
                            {
                                NjordMenuGUI.ShowNotification($"<color=#FF0000>[SHIELD]</color> Игрок <b>{pName}</b> заблокирован за Pet Spam (Локально)!");
                            }

                            if (NjordMenuGUI.enableHostPetSpamBan && AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
                            {
                                string fc = string.IsNullOrEmpty(__instance.myPlayer.Data?.FriendCode) ? "Unknown" : __instance.myPlayer.Data.FriendCode;
                                string puid = "Unknown";

                                try
                                {
                                    var client = AmongUsClient.Instance.GetClientFromCharacter(__instance.myPlayer);
                                    if (client != null) puid = client.Id.ToString();
                                }
                                catch { }

                                NjordMenuGUI.AddToBanList(fc, puid, pName, "Auto-banned for Pet Spam");

                                AmongUsClient.Instance.KickPlayer(__instance.myPlayer.OwnerId, true);

                                NjordMenuGUI.ShowNotification($"<color=#FF0000>[SHIELD]</color> Игрок <b>{pName}</b> АВТОМАТИЧЕСКИ ЗАБАНЕН за спам петом!");
                            }

                            return false;
                        }
                    }
                    catch { }
                }

                return true;
            }
        }
        public static int GetColorIdByName(string name)
        {
            string[] names = { "red", "blue", "green", "pink", "orange", "yellow", "black", "white", "purple", "brown", "cyan", "lime", "maroon", "rose", "banana", "gray", "tan", "coral", "fortegreen" };
            for (int i = 0; i < names.Length; i++)
                if (names[i] == name.ToLower().Trim()) return i;
            return -1;
        }
        private IEnumerator AttemptShapeshiftFrame(PlayerControl target, PlayerControl morphInto)
        {
            if (target == null || morphInto == null || PlayerControl.LocalPlayer == null || AmongUsClient.Instance == null) yield break;

            bool hasAnticheat = AmongUsClient.Instance.NetworkMode == NetworkModes.OnlineGame && !Constants.IsVersionModded();

            if (target.Data.RoleType != RoleTypes.Shapeshifter && hasAnticheat)
            {
                RoleTypes currentRole = target.Data.RoleType;
                target.RpcSetRole(RoleTypes.Shapeshifter, true);

                yield return new WaitForSeconds(0.5f);

                target.RpcShapeshift(morphInto, true);

                yield return new WaitForSeconds(0.5f);

                target.RpcSetRole(currentRole, true);
            }
            else
            {
                target.RpcShapeshift(morphInto, true);
            }
            ShowNotification($"<color=#ca08ff>[MORPH]</color> <b>{target.Data.PlayerName}</b> превращен в <b>{morphInto.Data.PlayerName}</b>!");
        }

        private IEnumerator MassMorphCoroutine()
        {
            if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost || PlayerControl.AllPlayerControls == null) yield break;

            bool hasAnticheat = AmongUsClient.Instance.NetworkMode == NetworkModes.OnlineGame && !Constants.IsVersionModded();

            Dictionary<byte, RoleTypes> originalRoles = new Dictionary<byte, RoleTypes>();

            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (pc != null && pc.Data != null && !pc.Data.Disconnected)
                {
                    originalRoles[pc.PlayerId] = pc.Data.RoleType;

                    if (hasAnticheat && pc.Data.RoleType != RoleTypes.Shapeshifter)
                    {
                        pc.RpcSetRole(RoleTypes.Shapeshifter, true);
                    }
                }
            }

            if (hasAnticheat) yield return new UnityEngine.WaitForSeconds(0.5f);

            PlayerControl targetToMorphInto = null;
            if (selectedMorphTargetId != 255)
            {
                targetToMorphInto = GameData.Instance.GetPlayerById(selectedMorphTargetId)?.Object;
            }

            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (pc != null && pc.Data != null && !pc.Data.Disconnected)
                {
                    PlayerControl morphTarget = targetToMorphInto != null ? targetToMorphInto : pc;
                    pc.RpcShapeshift(morphTarget, true);
                }
            }

           
            if (hasAnticheat) yield return new UnityEngine.WaitForSeconds(0.5f);
          
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (pc != null && pc.Data != null && !pc.Data.Disconnected)
                {
                    if (hasAnticheat && originalRoles.ContainsKey(pc.PlayerId))
                    {
                        pc.RpcSetRole(originalRoles[pc.PlayerId], true);
                    }
                }
            }

            string notifText = targetToMorphInto != null ? targetToMorphInto.Data.PlayerName : "Egg";
            ShowNotification($"<color=#FF00FF>[MASS MORPH]</color> {notifText}");
        }


        private void ForceMeetingAsPlayer(PlayerControl target)
        {
            if (target == null || AmongUsClient.Instance == null) return;
            if (!AmongUsClient.Instance.AmHost) return;

            try
            {
                MeetingRoomManager.Instance.AssignSelf(target, null);
                target.RpcStartMeeting(null);
                DestroyableSingleton<HudManager>.Instance.OpenMeetingRoom(target);
            }
            catch { }
        }

        private void KillAll()
        {
            if (PlayerControl.LocalPlayer == null || PlayerControl.AllPlayerControls == null) return;
            Vector3 op = PlayerControl.LocalPlayer.transform.position;
            foreach (var t in PlayerControl.AllPlayerControls)
            {
                if (t != null && t != PlayerControl.LocalPlayer && !t.Data.IsDead && !t.Data.Disconnected)
                {
                    PlayerControl.LocalPlayer.NetTransform.RpcSnapTo(t.transform.position);
                    PlayerControl.LocalPlayer.CmdCheckMurder(t);
                    PlayerControl.LocalPlayer.RpcMurderPlayer(t, true);
                }
            }
            PlayerControl.LocalPlayer.NetTransform.RpcSnapTo(op);
        }

        private void KickAll()
        {
            if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost && PlayerControl.AllPlayerControls != null)
            {
                foreach (var pc in PlayerControl.AllPlayerControls)
                    if (pc != null && pc != PlayerControl.LocalPlayer && !pc.Data.Disconnected)
                        AmongUsClient.Instance.KickPlayer((int)pc.OwnerId, false);
            }
        }

        private void DespawnLobby()
        {
            try
            {
                if (LobbyBehaviour.Instance != null && AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
                {
                    LobbyBehaviour.Instance.Cast<InnerNetObject>().Despawn();
                }
            }
            catch { }
        }

        private void SpawnLobby()
        {
            try
            {
                if (GameStartManager.Instance != null && AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
                {
                    LobbyBehaviour newLobby = UnityEngine.Object.Instantiate<LobbyBehaviour>(GameStartManager.Instance.LobbyPrefab);
                    AmongUsClient.Instance.Spawn(newLobby.Cast<InnerNetObject>(), -2, SpawnFlags.None);
                }
            }
            catch { }
        }



        public static void UnlockCosmetics()
        {
            if (HatManager.Instance == null) return;
            try
            {
                foreach (var h in HatManager.Instance.allHats) h.Free = true;
                foreach (var s in HatManager.Instance.allSkins) s.Free = true;
                foreach (var v in HatManager.Instance.allVisors) v.Free = true;
                foreach (var p in HatManager.Instance.allPets) p.Free = true;
                foreach (var n in HatManager.Instance.allNamePlates) n.Free = true;
            }
            catch { }
        }

        public static void ChangeNameGlobalHost(PlayerControl target, string newName)
        {
            if (target == null) return;
            if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return;
            try
            {
                target.RpcSetName(newName);
                var netObj = GameData.Instance.GetComponent<InnerNetObject>();
                if (netObj != null) netObj.SetDirtyBit(1U << (int)target.PlayerId);
            }
            catch { }
        }

        public static bool showWatermark = true;

        private void SaveConfig()
        {
            try
            {
                PlayerPrefs.SetInt("M_BndMagnet", (int)bindMagnetCursor);
                Plugin.SpoofedLevel.Value = spoofLevelString;
                Plugin.EnablePlatformSpoof.Value = enablePlatformSpoof;
                Plugin.PlatformIndex.Value = currentPlatformIndex;
                Plugin.ShowWatermarkConfig.Value = showWatermark;
                Plugin.UnlockCosmeticsConfig.Value = unlockCosmetics;
                Plugin.MoreLobbyInfoConfig.Value = moreLobbyInfo;
                Plugin.RpcSpoofDelayConfig.Value = rpcSpoofDelay;
                Plugin.MenuColorIndexConfig.Value = currentMenuColorIndex;
                Plugin.RgbMenuModeConfig.Value = rgbMenuMode;
                PlayerPrefs.SetInt("M_MenuToggleKey", (int)menuToggleKey);
                PlayerPrefs.SetInt("M_BndMMorph", (int)bindMassMorph);
                PlayerPrefs.SetInt("M_BndSpawn", (int)bindSpawnLobby);
                PlayerPrefs.SetInt("M_BndDespawn", (int)bindDespawnLobby);
                PlayerPrefs.SetInt("M_BndCloseMtg", (int)bindCloseMeeting);
                PlayerPrefs.SetInt("M_BndInstaStart", (int)bindInstaStart);
                PlayerPrefs.SetInt("M_BndEndCrew", (int)bindEndCrew);
                PlayerPrefs.SetInt("M_BndEndImp", (int)bindEndImp);
                PlayerPrefs.SetInt("M_BndEndImpDC", (int)bindEndImpDC);
                PlayerPrefs.SetInt("M_BndEndHnsDC", (int)bindEndHnsDC);

                if (keyBinds.ContainsKey("Toggle Menu"))
                    Plugin.MenuKeybind.Value = keyBinds["Toggle Menu"];

                Plugin.MenuConfig.Save();

                PlayerPrefs.SetString("M_SpoofName", customNameInput);
                PlayerPrefs.Save();
            }
            catch { }
        }
        private void LoadConfig()
        {
            try
            {
                spoofLevelString = Plugin.SpoofedLevel.Value;
                enablePlatformSpoof = Plugin.EnablePlatformSpoof.Value;
                currentPlatformIndex = Plugin.PlatformIndex.Value;
                showWatermark = Plugin.ShowWatermarkConfig.Value;
                unlockCosmetics = Plugin.UnlockCosmeticsConfig.Value;
                moreLobbyInfo = Plugin.MoreLobbyInfoConfig.Value;
                rpcSpoofDelay = Plugin.RpcSpoofDelayConfig.Value;
                currentMenuColorIndex = Plugin.MenuColorIndexConfig.Value;
                rgbMenuMode = Plugin.RgbMenuModeConfig.Value;
                if (PlayerPrefs.HasKey("M_BndMagnet")) bindMagnetCursor = (KeyCode)PlayerPrefs.GetInt("M_BndMagnet");
                if (PlayerPrefs.HasKey("M_MenuToggleKey")) menuToggleKey = (KeyCode)PlayerPrefs.GetInt("M_MenuToggleKey");
                if (PlayerPrefs.HasKey("M_BndMMorph")) bindMassMorph = (KeyCode)PlayerPrefs.GetInt("M_BndMMorph");
                if (PlayerPrefs.HasKey("M_BndSpawn")) bindSpawnLobby = (KeyCode)PlayerPrefs.GetInt("M_BndSpawn");
                if (PlayerPrefs.HasKey("M_BndDespawn")) bindDespawnLobby = (KeyCode)PlayerPrefs.GetInt("M_BndDespawn");
                if (PlayerPrefs.HasKey("M_BndCloseMtg")) bindCloseMeeting = (KeyCode)PlayerPrefs.GetInt("M_BndCloseMtg");
                if (PlayerPrefs.HasKey("M_BndInstaStart")) bindInstaStart = (KeyCode)PlayerPrefs.GetInt("M_BndInstaStart");
                if (PlayerPrefs.HasKey("M_BndEndCrew")) bindEndCrew = (KeyCode)PlayerPrefs.GetInt("M_BndEndCrew");
                if (PlayerPrefs.HasKey("M_BndEndImp")) bindEndImp = (KeyCode)PlayerPrefs.GetInt("M_BndEndImp");
                if (PlayerPrefs.HasKey("M_BndEndImpDC")) bindEndImpDC = (KeyCode)PlayerPrefs.GetInt("M_BndEndImpDC");
                if (PlayerPrefs.HasKey("M_BndEndHnsDC")) bindEndHnsDC = (KeyCode)PlayerPrefs.GetInt("M_BndEndHnsDC");

                if (!rgbMenuMode && currentMenuColorIndex >= 0 && currentMenuColorIndex < menuColors.Length)
                {
                    currentAccentColor = menuColors[currentMenuColorIndex];
                }

                keyBinds["Toggle Menu"] = Plugin.MenuKeybind.Value;
                if (PlayerPrefs.HasKey("M_SpoofName")) customNameInput = PlayerPrefs.GetString("M_SpoofName");
            }
            catch { }
        }
        private Texture2D MakeRoundedTex(int size, Color col, float radius)
        {
            Texture2D result = new Texture2D(size, size, TextureFormat.RGBA32, false);
            result.hideFlags = HideFlags.HideAndDontSave;
            Color[] pix = new Color[size * size];
            float center = size / 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(0, Mathf.Abs(x - center + 0.5f) - (center - radius));
                    float dy = Mathf.Max(0, Mathf.Abs(y - center + 0.5f) - (center - radius));
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(radius - dist + 0.5f);
                    Color c = col;
                    c.a = col.a * alpha;
                    pix[y * size + x] = c;
                }
            }
            result.SetPixels(pix); result.Apply();
            return result;
        }

        private RectOffset CreateRectOffset(int left, int right, int top, int bottom)
        {
            return new RectOffset { left = left, right = right, top = top, bottom = bottom };
        }

        private void UpdateSwitchTex(Texture2D tex, bool isOn, Color accentColor)
        {
            int width = tex.width; int height = tex.height;
            Color transparent = new Color(0, 0, 0, 0);
            Color offBg = new Color(0.23f, 0.23f, 0.23f, 1f);
            Color offKnob = new Color(0.6f, 0.6f, 0.6f, 1f);
            Color bgColor = isOn ? accentColor : offBg;
            Color knobColor = isOn ? Color.white : offKnob;
            float r = height / 2f;
            float cx1 = r; float cx2 = width - r; float cy = r;
            float knobRadius = r - 2f;
            float knobCx = isOn ? cx2 : cx1;
            Color[] pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dLeft = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx1, cy));
                    float dRight = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx2, cy));
                    float dRect = (x + 0.5f >= cx1 && x + 0.5f <= cx2) ? Mathf.Abs((y + 0.5f) - cy) : 9999f;
                    float distBg = Mathf.Min(dLeft, Mathf.Min(dRight, dRect));
                    float alphaBg = Mathf.Clamp01(r - distBg + 0.5f);
                    float distKnob = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(knobCx, cy));
                    float alphaKnob = Mathf.Clamp01(knobRadius - distKnob + 0.5f);
                    if (alphaBg > 0)
                    {
                        Color finalCol = Color.Lerp(bgColor, knobColor, alphaKnob);
                        finalCol.a = alphaBg;
                        pixels[y * width + x] = finalCol;
                    }
                    else pixels[y * width + x] = transparent;
                }
            }
            tex.SetPixels(pixels); tex.Apply();
        }

        private void UpdateAccentColor(Color color)
        {
            currentAccentColor = color;
            if (texAccent != null)
            {
                int size = texAccent.width;
                Color[] pix = new Color[size * size];
                float center = size / 2f;
                float radius = 6f;
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dx = Mathf.Max(0, Mathf.Abs(x - center + 0.5f) - (center - radius));
                        float dy = Mathf.Max(0, Mathf.Abs(y - center + 0.5f) - (center - radius));
                        float dist = Mathf.Sqrt(dx * dx + dy * dy);
                        float alpha = Mathf.Clamp01(radius - dist + 0.5f);
                        Color c = color; c.a = alpha;
                        pix[y * size + x] = c;
                    }
                }
                texAccent.SetPixels(pix); texAccent.Apply();
            }
            if (texSliderThumb != null)
            {
                int size = texSliderThumb.width;
                Color[] pix = new Color[size * size];
                float center = size / 2f;
                float radius = 10f;
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dx = Mathf.Max(0, Mathf.Abs(x - center + 0.5f) - (center - radius));
                        float dy = Mathf.Max(0, Mathf.Abs(y - center + 0.5f) - (center - radius));
                        float dist = Mathf.Sqrt(dx * dx + dy * dy);
                        float alpha = Mathf.Clamp01(radius - dist + 0.5f);
                        Color c = color; c.a = alpha;
                        pix[y * size + x] = c;
                    }
                }
                texSliderThumb.SetPixels(pix); texSliderThumb.Apply();
            }
            if (texScrollThumb != null)
            {
                int size = texScrollThumb.width;
                Color[] pix = new Color[size * size];
                float center = size / 2f;
                float radius = 4f;
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dx = Mathf.Max(0, Mathf.Abs(x - center + 0.5f) - (center - radius));
                        float dy = Mathf.Max(0, Mathf.Abs(y - center + 0.5f) - (center - radius));
                        float dist = Mathf.Sqrt(dx * dx + dy * dy);
                        float alpha = Mathf.Clamp01(radius - dist + 0.5f);
                        Color c = color; c.a = alpha;
                        pix[y * size + x] = c;
                    }
                }
                texScrollThumb.SetPixels(pix); texScrollThumb.Apply();
            }
            if (texToggleOn != null) UpdateSwitchTex(texToggleOn, true, color);
            if (windowStyle != null) windowStyle.normal.textColor = color;
            if (headerStyle != null) headerStyle.normal.textColor = color;
            if (activeSidebarBtnStyle != null) { activeSidebarBtnStyle.normal.textColor = color; activeSidebarBtnStyle.hover.textColor = color; }
            if (activeTabStyle != null) activeTabStyle.normal.background = texAccent;
            if (activeSubTabStyle != null) activeSubTabStyle.normal.background = texAccent;
            if (btnStyle != null) btnStyle.active.background = texAccent;
            if (inputBlockStyle != null) inputBlockStyle.normal.textColor = color;
        }

        private void InitStyles()
        {
            Color accent = currentAccentColor;
            Color darkBg = new Color(0.12f, 0.12f, 0.12f, 0.90f);
            Color sidebarBg = new Color(0.0f, 0.0f, 0.0f, 0.0f);
            Color boxBg = new Color(0f, 0f, 0f, 0f);
            Color btnCol = new Color(0.23f, 0.23f, 0.23f, 1f);
            Color sliderBgCol = new Color(0.08f, 0.08f, 0.08f, 1f);

            texWindowBg = MakeRoundedTex(64, darkBg, 12f);
            texSidebarBg = MakeRoundedTex(64, sidebarBg, 0f);
            texBoxBg = MakeRoundedTex(64, boxBg, 0f);
            texBtnBg = MakeRoundedTex(64, btnCol, 6f);
            texAccent = MakeRoundedTex(64, accent, 6f);
            texSliderBg = MakeRoundedTex(64, sliderBgCol, 4f);
            texSliderThumb = MakeRoundedTex(20, accent, 10f);
            texInputBg = MakeRoundedTex(64, new Color(0.08f, 0.08f, 0.08f, 0.85f), 6f);
            texColorBtn = MakeRoundedTex(64, Color.white, 12f);

            texToggleOff = new Texture2D(30, 16, TextureFormat.RGBA32, false);
            texToggleOff.hideFlags = HideFlags.HideAndDontSave;
            texToggleOn = new Texture2D(30, 16, TextureFormat.RGBA32, false);
            texToggleOn.hideFlags = HideFlags.HideAndDontSave;
            UpdateSwitchTex(texToggleOff, false, Color.white);
            UpdateSwitchTex(texToggleOn, true, accent);

            safeLineStyle = new GUIStyle();
            safeLineStyle.normal.background = Texture2D.whiteTexture;

            windowStyle = new GUIStyle();
            windowStyle.normal.background = texWindowBg;
            windowStyle.normal.textColor = accent;
            windowStyle.fontStyle = FontStyle.Bold;
            windowStyle.fontSize = 14;
            windowStyle.padding = CreateRectOffset(0, 0, 0, 0);
            windowStyle.border = CreateRectOffset(12, 12, 12, 12);

            boxStyle = new GUIStyle();
            boxStyle.normal.background = texBoxBg;
            boxStyle.padding = CreateRectOffset(0, 0, 0, 0);
            boxStyle.margin = CreateRectOffset(0, 0, 4, 8);

            btnStyle = new GUIStyle(GUI.skin.button);
            btnStyle.normal.background = texBtnBg;
            btnStyle.normal.textColor = new Color(0.78f, 0.78f, 0.78f);
            btnStyle.active.background = texAccent;
            btnStyle.active.textColor = Color.black;
            btnStyle.alignment = TextAnchor.MiddleCenter;
            btnStyle.border = CreateRectOffset(6, 6, 6, 6);
            btnStyle.fontSize = 12;
            btnStyle.fontStyle = FontStyle.Bold;

            activeTabStyle = new GUIStyle(btnStyle);
            activeTabStyle.normal.background = texAccent;
            activeTabStyle.normal.textColor = Color.black;

            subTabStyle = new GUIStyle(btnStyle);
            subTabStyle.padding = CreateRectOffset(6, 6, 2, 2);
            activeSubTabStyle = new GUIStyle(activeTabStyle);
            activeSubTabStyle.padding = CreateRectOffset(6, 6, 2, 2);

            inputBlockStyle = new GUIStyle(btnStyle);
            inputBlockStyle.normal.background = texInputBg;
            inputBlockStyle.hover.background = texInputBg;
            inputBlockStyle.active.background = texAccent;
            inputBlockStyle.normal.textColor = accent;
            inputBlockStyle.alignment = TextAnchor.MiddleCenter;
            inputBlockStyle.fontStyle = FontStyle.Bold;

            headerStyle = new GUIStyle();
            headerStyle.normal.background = texBtnBg;
            headerStyle.normal.textColor = accent;
            headerStyle.fontStyle = FontStyle.Bold;
            headerStyle.alignment = TextAnchor.MiddleLeft;
            headerStyle.padding = CreateRectOffset(6, 6, 4, 4);
            headerStyle.margin = CreateRectOffset(0, 0, 4, 4);
            headerStyle.fontSize = 13;

            sidebarStyle = new GUIStyle();
            sidebarStyle.normal.background = texSidebarBg;
            sidebarStyle.padding = CreateRectOffset(0, 0, 5, 0);

            sidebarBtnStyle = new GUIStyle();
            sidebarBtnStyle.normal.textColor = new Color(0.6f, 0.6f, 0.6f);
            sidebarBtnStyle.hover.textColor = Color.white;
            sidebarBtnStyle.padding = CreateRectOffset(12, 0, 6, 6);
            sidebarBtnStyle.alignment = TextAnchor.MiddleLeft;
            sidebarBtnStyle.fontSize = 13;
            sidebarBtnStyle.fontStyle = FontStyle.Bold;

            activeSidebarBtnStyle = new GUIStyle(sidebarBtnStyle);
            activeSidebarBtnStyle.normal.textColor = accent;
            activeSidebarBtnStyle.hover.textColor = accent;

            toggleOffStyle = new GUIStyle();
            toggleOffStyle.normal.background = texToggleOff;
            toggleOnStyle = new GUIStyle();
            toggleOnStyle.normal.background = texToggleOn;

            toggleLabelStyle = new GUIStyle();
            toggleLabelStyle.normal.textColor = new Color(0.78f, 0.78f, 0.78f);
            toggleLabelStyle.alignment = TextAnchor.MiddleLeft;
            toggleLabelStyle.padding = CreateRectOffset(4, 0, 0, 0);
            toggleLabelStyle.fontSize = 12;
            toggleLabelStyle.fontStyle = FontStyle.Bold;

            sliderStyle = new GUIStyle();
            sliderStyle.normal.background = texSliderBg;
            sliderStyle.border = CreateRectOffset(6, 6, 6, 6);
            sliderStyle.fixedHeight = 10f;
            sliderStyle.margin = CreateRectOffset(0, 0, 8, 8);

            sliderThumbStyle = new GUIStyle();
            sliderThumbStyle.normal.background = texSliderThumb;
            sliderThumbStyle.fixedWidth = 18f;
            sliderThumbStyle.fixedHeight = 18f;
            sliderThumbStyle.margin = CreateRectOffset(0, 0, -4, 0);

            titleStyle = new GUIStyle();
            titleStyle.normal.textColor = accent;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.fontSize = 14;
            titleStyle.padding = CreateRectOffset(10, 0, 8, 0);

            Texture2D texScrollBg = MakeRoundedTex(8, new Color(0.1f, 0.1f, 0.1f, 0.2f), 4f);
            texScrollThumb = MakeRoundedTex(8, accent, 4f); 

            GUIStyle scrollBarStyle = new GUIStyle(GUI.skin.verticalScrollbar);
            scrollBarStyle.normal.background = texScrollBg;
            scrollBarStyle.fixedWidth = 8f;
            scrollBarStyle.border = CreateRectOffset(0, 0, 4, 4);
            scrollBarStyle.margin = CreateRectOffset(2, 2, 2, 2);

            GUIStyle scrollBarThumbStyle = new GUIStyle(GUI.skin.verticalScrollbarThumb);
            scrollBarThumbStyle.normal.background = texScrollThumb;
            scrollBarThumbStyle.hover.background = texScrollThumb;
            scrollBarThumbStyle.active.background = texScrollThumb;
            scrollBarThumbStyle.fixedWidth = 8f;
            scrollBarThumbStyle.border = CreateRectOffset(0, 0, 4, 4);

            GUI.skin.verticalScrollbar = scrollBarStyle;
            GUI.skin.verticalScrollbarThumb = scrollBarThumbStyle;
            GUI.skin.horizontalScrollbar.normal.background = null;
            GUI.skin.horizontalScrollbarThumb.normal.background = null;

            stylesInited = true;
        }

        private void LoadBackgroundImage()
        {
            try
            {
                string bgPath = System.IO.Path.Combine(Plugin.NjordFolder, "MenuBG.png");
                if (!System.IO.File.Exists(bgPath)) bgPath = System.IO.Path.Combine(Plugin.NjordFolder, "MenuBG.jpg");
                if (System.IO.File.Exists(bgPath))
                {
                    byte[] fileData = System.IO.File.ReadAllBytes(bgPath);
                    Texture2D tempTex = new Texture2D(2, 2);
                    ImageConversion.LoadImage(tempTex, fileData);
                    customMenuBg = new Texture2D(tempTex.width, tempTex.height, TextureFormat.RGBA32, false);
                    customMenuBg.hideFlags = HideFlags.HideAndDontSave;
                    Color[] pix = tempTex.GetPixels();
                    UnityEngine.Object.Destroy(tempTex);
                    int w = customMenuBg.width, h = customMenuBg.height;
                    float targetRadius = 12f, rx = targetRadius * (w / windowRect.width), ry = targetRadius * (h / windowRect.height);
                    for (int y = 0; y < h; y++)
                        for (int x = 0; x < w; x++)
                        {
                            float dx = 0f, dy = 0f;
                            if (x < rx) dx = rx - x;
                            else if (x > w - rx) dx = x - (w - rx);
                            if (y < ry) dy = ry - y;
                            else if (y > h - ry) dy = y - (h - ry);
                            if (dx > 0 && dy > 0)
                            {
                                float nx = dx / rx, ny = dy / ry;
                                float dist = Mathf.Sqrt(nx * nx + ny * ny);
                                if (dist > 1f) { Color c = pix[y * w + x]; c.a = 0f; pix[y * w + x] = c; }
                                else
                                {
                                    float alphaMult = Mathf.Clamp01((1f - dist) * Mathf.Max(rx, ry));
                                    Color c = pix[y * w + x]; c.a *= alphaMult; pix[y * w + x] = c;
                                }
                            }
                        }
                    customMenuBg.SetPixels(pix); customMenuBg.Apply();
                }
                else enableBackground = false;
            }
            catch { enableBackground = false; }
        }

        public static string ApplyMenuShimmer(string text)
        {
            string result = "";
            Color baseColor = currentAccentColor, glowColor = Color.white;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == ' ') { result += " "; continue; }
                float wave = Mathf.Sin(Time.unscaledTime * 6f - (i * 0.4f)) * 0.5f + 0.5f;
                Color c = Color.Lerp(baseColor, glowColor, wave);
                result += $"<color=#{ColorUtility.ToHtmlStringRGB(c)}>{text[i]}</color>";
            }
            return result;
        }

        private bool DrawToggle(bool value, string text, int width = 0)
        {
            GUILayout.BeginHorizontal(GUILayout.Width(width > 0 ? width : 200));

            bool clickedBox = GUILayout.Button("", value ? toggleOnStyle : toggleOffStyle, GUILayout.Width(30), GUILayout.Height(16));

            GUILayout.Space(6);

            bool clickedText = GUILayout.Button(text, toggleLabelStyle);

            GUILayout.EndHorizontal();

            return (clickedBox || clickedText) ? !value : value;
        }

        private bool DrawBindableButton(string label, string bindKey, float width)
        {
            bool clicked = false;
            GUILayout.BeginVertical(GUILayout.Width(width));
            if (GUILayout.Button(label, btnStyle, GUILayout.Height(25), GUILayout.Width(width))) clicked = true;
            string bindTxt = bindingAction == bindKey ? "Press Key..." : (keyBinds.ContainsKey(bindKey) ? $"[{keyBinds[bindKey]}]" : "[Bind Key]");
            GUIStyle bindStyle = new GUIStyle(btnStyle) { fontSize = 10, normal = { textColor = new Color(0.6f, 0.6f, 0.6f) } };
            if (bindingAction == bindKey) bindStyle.normal.textColor = currentAccentColor;
            if (GUILayout.Button(bindTxt, bindStyle, GUILayout.Height(15), GUILayout.Width(width))) bindingAction = bindKey;
            GUILayout.EndVertical();
            return clicked;
        }

        private bool DrawHostToggle(bool value, string text, float totalWidth = 250f)
        {
            GUILayout.BeginHorizontal(GUILayout.Width(totalWidth), GUILayout.Height(20));
            bool clickedBox = GUILayout.Button("", value ? toggleOnStyle : toggleOffStyle, GUILayout.Width(30), GUILayout.Height(16));
            GUILayout.Space(6);
            bool clickedText = GUILayout.Button(text, toggleLabelStyle, GUILayout.Width(totalWidth - 36f), GUILayout.Height(16));
            GUILayout.EndHorizontal();
            return (clickedBox || clickedText) ? !value : value;
        }
        private void DrawBindsTab()
        {
            GUILayout.BeginVertical(boxStyle);
            try
            {
                GUILayout.Label("⌨️ CUSTOM KEYBINDS", headerStyle);
                GUILayout.Space(10);

                DrawKeybindRow("Magnet Cursor:", ref bindMagnetCursor, ref isWaitBindMagnetCursor);
                DrawKeybindRow("Menu Toggle Key:", ref menuToggleKey, ref isWaitingForBind);
                DrawKeybindRow("Mass Morph:", ref bindMassMorph, ref isWaitBindMassMorph);
                DrawKeybindRow("Spawn Lobby:", ref bindSpawnLobby, ref isWaitBindSpawnLobby);
                DrawKeybindRow("Despawn Lobby:", ref bindDespawnLobby, ref isWaitBindDespawnLobby);
                DrawKeybindRow("Close Meeting:", ref bindCloseMeeting, ref isWaitBindCloseMeeting);
                DrawKeybindRow("Insta Start:", ref bindInstaStart, ref isWaitBindInstaStart);
                DrawKeybindRow("End: Crewmate Win:", ref bindEndCrew, ref isWaitBindEndCrew);
                DrawKeybindRow("End: Impostor Win:", ref bindEndImp, ref isWaitBindEndImp);
                DrawKeybindRow("End: Imp Disconnect:", ref bindEndImpDC, ref isWaitBindEndImpDC);
                DrawKeybindRow("End: H&S Disconnect:", ref bindEndHnsDC, ref isWaitBindEndHnsDC);
            }
            finally { GUILayout.EndVertical(); }
        }

        private void DrawKeybindRow(string label, ref KeyCode currentKey, ref bool isWaiting)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            GUIStyle alignedLabel = new GUIStyle(toggleLabelStyle) { alignment = TextAnchor.MiddleLeft, margin = CreateRectOffset(0, 0, 4, 0) };
            GUILayout.Label(label, alignedLabel, GUILayout.Width(220), GUILayout.Height(25));

            string bindText = isWaiting ? "Press any key..." : (currentKey == KeyCode.None ? "NONE" : currentKey.ToString());
            if (GUILayout.Button(bindText, isWaiting ? activeTabStyle : btnStyle, GUILayout.Width(120), GUILayout.Height(25)))
            {
                ResetAllBindWaits();
                isWaiting = true;
            }

            if (GUILayout.Button("Clear", btnStyle, GUILayout.Width(50), GUILayout.Height(25)))
            {
                currentKey = KeyCode.None;
                isWaiting = false;
                SaveConfig();
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(5);
        }

        private void ResetAllBindWaits()
        {
            isWaitingForBind = false;
            isWaitBindMassMorph = false;
            isWaitBindSpawnLobby = false;
            isWaitBindDespawnLobby = false;
            isWaitBindCloseMeeting = false;
            isWaitBindInstaStart = false;
            isWaitBindEndCrew = false;
            isWaitBindEndImp = false;
            isWaitBindEndImpDC = false;
            isWaitBindEndHnsDC = false;
            isWaitBindMagnetCursor = false;
        }
        private void DrawGeneralTab()
        {
            GUILayout.BeginHorizontal();
            for (int i = 0; i < generalSubTabs.Length; i++)
            {
                if (GUILayout.Button(generalSubTabs[i], currentGeneralSubTab == i ? activeSubTabStyle : subTabStyle, GUILayout.Height(22)))
                {
                    currentGeneralSubTab = i;
                    scrollPosition = Vector2.zero;
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            if (currentGeneralSubTab == 0) DrawGeneralInfoTab();
            else if (currentGeneralSubTab == 1) DrawBindsTab();
        }

        private void DrawGeneralInfoTab()
        {
            GUILayout.BeginVertical(boxStyle);
            GUILayout.Label("INFORMATION & HOTKEYS", headerStyle);
            GUILayout.Space(10);

            GUIStyle textStyle = new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true, fontSize = 12 };

            string title = ApplyMenuShimmer("Welcome to NjordMenu v1.2.3!");
            string subtitle = "<color=#aaaaaa><b>(The start timer editing was patched by innersloth)</b></color>";

            string infoText = $"<size=16><b>{title}</b></size>\n{subtitle}\n\n" +
                 $"<b><color=#FFAC1C>📌 HOTKEYS:</color></b>\n" +
                 $"• <b>[Insert]</b> or <b>[Right Shift]</b> — Open / Close Menu\n" +
                 $"• <b>[ Right Click ]</b> — Teleport to Cursor\n" +
                 $"• <b>[ F9 ]</b> — Magnet Cursor (Auto Follow)\n\n" +
                 $"<b><color=#FF0000>⚠️ DISCLAIMER & CAUTION:</color></b>\n" +
                 $"<color=#dddddd>NjordMenu should NEVER, under any circumstances, be used to impair the experiences of other legitimate players. If you use some of the trolling, crashing, or forceful features, please make sure you are doing so in a private lobby with consenting friends. You are free to join public lobbies with NjordMenu enabled as long as you use it with the intention of improving your own game (e.g., using the Anticheat, ESP, or QoL features). With great power comes great responsibility!</color>\n\n" +
                 $"<color=#dddddd>I recognize that utility mods like NjordMenu open the door for malicious users to cause destruction. Even with safeguards, there is always a chance for abuse. All I can do is ask you, the person using this mod, to please do not use NjordMenu for malicious purposes and follow the Innersloth Code of Conduct.</color>\n\n" +
                 $"<color=#FF5555>If you fail to follow this suggestion, do not expect to receive any kind of support. Your account may be sanctioned or banned by Innersloth, resulting in the loss of your friends list, unlocked cosmetics, and purchases.</color>\n\n" +
                 $"<b><color=#00FF00>Have a great game and enjoy the mod♡! 🎮✨</color></b>\n\n" +
                 $"<size=10><color=#777777>This mod is not affiliated with Among Us or Innersloth LLC, and the content contained therein is not endorsed or otherwise sponsored by Innersloth LLC. Portions of the materials contained herein are property of Innersloth LLC. © Innersloth LLC.</color></size>";

            GUILayout.Label(infoText, textStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
        }

        private void DrawSelfTab()
        {
            GUILayout.BeginHorizontal();
            for (int i = 0; i < selfSubTabs.Length; i++)
                if (GUILayout.Button(selfSubTabs[i], currentSelfSubTab == i ? activeSubTabStyle : subTabStyle, GUILayout.Height(18)))
                { currentSelfSubTab = i; scrollPosition = Vector2.zero; }
            GUILayout.EndHorizontal();
            GUILayout.Space(8);
            if (currentSelfSubTab == 0) DrawSelfSpoof();
            else if (currentSelfSubTab == 1) DrawPlayerMovement();
        }

        private void DrawPlayerMovement()
        {
            GUILayout.BeginVertical(boxStyle);
            try
            {
                GUILayout.Label("MOVEMENT & TELEPORT", headerStyle);

                GUILayout.BeginHorizontal();
                try
                {
                    GUILayout.Label($"Engine Speed: {Mathf.Round(engineSpeed)}x", GUILayout.Width(130));
                    engineSpeed = GUILayout.HorizontalSlider(engineSpeed, 1f, 555f, sliderStyle, sliderThumbStyle, GUILayout.ExpandWidth(true));
                    GUILayout.Space(10);
                    if (GUILayout.Button("Reset", btnStyle, GUILayout.Width(50), GUILayout.Height(20))) engineSpeed = 1f;
                }
                finally { GUILayout.EndHorizontal(); }

                GUILayout.Space(5);

                GUILayout.BeginHorizontal();
                try
                {
                    GUILayout.Label($"Walk Speed: {Mathf.Round(walkSpeed)}x", GUILayout.Width(130));
                    walkSpeed = GUILayout.HorizontalSlider(walkSpeed, 1f, 30f, sliderStyle, sliderThumbStyle, GUILayout.ExpandWidth(true));
                    GUILayout.Space(10);
                    if (GUILayout.Button("Reset", btnStyle, GUILayout.Width(50), GUILayout.Height(20))) walkSpeed = 1f;
                }
                finally { GUILayout.EndHorizontal(); }

                GUILayout.Space(5);

                GUILayout.BeginHorizontal();
                try
                {
                    tpToCursor = DrawToggle(tpToCursor, "TP To Cursor", 160);
                    dragToCursor = DrawToggle(dragToCursor, "Drag To Cursor", 160);
                    GUILayout.FlexibleSpace();
                }
                finally { GUILayout.EndHorizontal(); }

                GUILayout.Space(5);

                GUILayout.BeginHorizontal();
                try
                {
                    autoFollowCursor = DrawToggle(autoFollowCursor, "Magnet Cursor (F9)", 160);
                    noClip = DrawToggle(noClip, "True NoClip", 160);
                    GUILayout.FlexibleSpace();
                }
                finally { GUILayout.EndHorizontal(); }
            }
            finally { GUILayout.EndVertical(); }
        }
        private void SmartEndGame(string outcome)
        {
            if (GameManager.Instance == null || AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return;

            bool isHns = GameManager.Instance.IsHideAndSeek();
            int reasonCode = 0;

            switch (outcome)
            {
                case "CrewWin": reasonCode = isHns ? 7 : 0; break;
                case "ImpWin": reasonCode = isHns ? 8 : 3; break;
                case "ImpDisconnect":
                case "HnsImpDisconnect": reasonCode = 5; break;
            }

            bool tempBlock = neverEndGame;
            neverEndGame = false;
            GameManager.Instance.RpcEndGame((GameOverReason)reasonCode, false);
            neverEndGame = tempBlock;
        }
        private void DrawSelfSpoof()
        {
            GUILayout.BeginVertical(boxStyle);
            GUIStyle greenHeader = new GUIStyle(headerStyle);
            greenHeader.normal.textColor = currentAccentColor;
            GUILayout.Label("ACCOUNT SPOOFER", greenHeader);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Fake Level", btnStyle, GUILayout.Width(120), GUILayout.Height(28));
            string lvlDisp = isEditingLevel ? spoofLevelString + "_" : spoofLevelString;
            if (GUILayout.Button(lvlDisp, isEditingLevel ? activeTabStyle : inputBlockStyle, GUILayout.Width(160), GUILayout.Height(28))) { isEditingLevel = !isEditingLevel; isEditingName = false; }
            if (GUILayout.Button("Apply", btnStyle, GUILayout.Width(75), GUILayout.Height(28)))
            {
                isEditingLevel = false;
                if (uint.TryParse(spoofLevelString, out uint parsedLvl))
                {
                    try { AmongUs.Data.DataManager.Player.stats.level = parsedLvl > 0 ? parsedLvl - 1 : 0; AmongUs.Data.DataManager.Player.Save(); }
                    catch { try { AmongUs.Data.DataManager.Player.Stats.Level = parsedLvl > 0 ? parsedLvl - 1 : 0; AmongUs.Data.DataManager.Player.Save(); } catch { } }
                }
                SaveConfig();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(2);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Name", btnStyle, GUILayout.Width(120), GUILayout.Height(28));
            string nmDisp = isEditingName ? customNameInput + "_" : customNameInput;
            if (GUILayout.Button(nmDisp, isEditingName ? activeTabStyle : inputBlockStyle, GUILayout.Width(160), GUILayout.Height(28))) { isEditingName = !isEditingName; isEditingLevel = false; }
            if (GUILayout.Button("Apply", btnStyle, GUILayout.Width(75), GUILayout.Height(28)))
            {
                isEditingName = false;
                try { AmongUs.Data.DataManager.Player.Customization.Name = customNameInput; AmongUs.Data.DataManager.Player.Save(); } catch { }
                ChangeNameGlobalHost(PlayerControl.LocalPlayer, customNameInput);
                SaveConfig();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Spoof Platform", enablePlatformSpoof ? activeTabStyle : btnStyle, GUILayout.Width(120), GUILayout.Height(28)))
            {
                enablePlatformSpoof = !enablePlatformSpoof;
                SaveConfig();
            }
            GUILayout.Space(10);
            string hexColor = ColorUtility.ToHtmlStringRGB(currentAccentColor);

            GUILayout.Label($"Platform: <color=#{hexColor}>{platformNames[currentPlatformIndex]}</color>", new GUIStyle(toggleLabelStyle) { fontSize = 13, richText = true }, GUILayout.Height(28));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            int newPlatIdx = (int)GUILayout.HorizontalSlider(currentPlatformIndex, 0, platformNames.Length - 1, sliderStyle, sliderThumbStyle, GUILayout.Width(290));
            if (newPlatIdx != currentPlatformIndex)
            {
                currentPlatformIndex = newPlatIdx;
                SaveConfig();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(15);
            GUILayout.Label("TASKS", headerStyle);
            if (GUILayout.Button("Complete My Tasks", btnStyle, GUILayout.Height(30)))
            {
                if (PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.myTasks != null)
                    foreach (var task in PlayerControl.LocalPlayer.myTasks)
                        if (task != null && !task.IsComplete) PlayerControl.LocalPlayer.RpcCompleteTask((uint)task.Id);
            }
            GUILayout.EndVertical();
        }

        private void DrawVisualsTab()
        {
            GUILayout.BeginHorizontal();
            for (int i = 0; i < visualsSubTabs.Length; i++)
                if (GUILayout.Button(visualsSubTabs[i], currentVisualsSubTab == i ? activeSubTabStyle : subTabStyle, GUILayout.Height(18)))
                { currentVisualsSubTab = i; scrollPosition = Vector2.zero; }
            GUILayout.EndHorizontal();
            GUILayout.Space(8);
            if (currentVisualsSubTab == 0) DrawVisualsInGame();
        }

        private void DrawVisualsInGame()
        {
            GUILayout.BeginVertical(boxStyle);
            GUILayout.Label("VISIBILITY", headerStyle);
            GUILayout.BeginHorizontal();
            seeGhosts = DrawToggle(seeGhosts, "See Ghosts", 210);
            seeRoles = DrawToggle(seeRoles, "See Roles", 210);
            GUILayout.EndHorizontal();
            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            showPlayerInfo = DrawToggle(showPlayerInfo, "Show Player Info (ESP)", 210);
            revealMeetingRoles = DrawToggle(revealMeetingRoles, "Reveal Roles (Meeting)", 210);
            GUILayout.EndHorizontal();
            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            showTracers = DrawToggle(showTracers, "Show Tracers", 210);
            fullBright = DrawToggle(fullBright, "Full Bright (No Shadows)", 210);
            GUILayout.EndHorizontal();
            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            alwaysChat = DrawToggle(alwaysChat, "Always Show Chat", 210);
            readGhostChat = DrawToggle(readGhostChat, "Read Ghost Chat", 210);
            GUILayout.EndHorizontal();
            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            freecam = DrawToggle(freecam, "Freecam (WASD)", 210);
            cameraZoom = DrawToggle(cameraZoom, "Camera Zoom (Scroll)", 210);
            GUILayout.EndHorizontal();
            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            RevealVotesEnabled = DrawToggle(RevealVotesEnabled, "Reveal Votes (Meeting)", 210);
            SeePlayersInVent = DrawToggle(SeePlayersInVent, "See Players In the Vents", 210);
            GUILayout.EndHorizontal();
            GUILayout.Space(5);
            GUILayout.Label("Light Radius", toggleLabelStyle);
            customLightRadius = GUILayout.HorizontalSlider(customLightRadius, 0.5f, 20f, sliderStyle, sliderThumbStyle, GUILayout.Width(200));
            GUILayout.EndVertical();
        }

        private void DrawPlayersTab()
        {
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(boxStyle, GUILayout.Width(200));
            playerListScrollPos = GUILayout.BeginScrollView(playerListScrollPos);
            if (lockedPlayersList.Count > 0)
            {
                foreach (var pc in lockedPlayersList)
                {
                    if (pc == null || pc.Data == null || pc.PlayerId >= 100) continue;
                    string pName = pc.Data.PlayerName ?? "Unknown";

                    if (forcedPreGameRoles.ContainsKey(pc.PlayerId)) pName += " [*]";
                    else if (forcedImpostors.Contains(pc.PlayerId)) pName += " [Imp]";

                    bool isSelected = selectedHydraPlayerId == pc.PlayerId;

                    GUI.contentColor = Color.white;
                    try { GUI.contentColor = Palette.PlayerColors[pc.Data.DefaultOutfit.ColorId]; } catch { }

                    if (GUILayout.Button(pName, isSelected ? activeTabStyle : btnStyle, GUILayout.Height(30)))
                    {
                        selectedHydraPlayerId = pc.PlayerId;
                    }
                    GUI.contentColor = Color.white;
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.BeginVertical(boxStyle, GUILayout.ExpandWidth(true));
            playerActionScrollPos = GUILayout.BeginScrollView(playerActionScrollPos);

            PlayerControl target = lockedPlayersList.FirstOrDefault(p => p.PlayerId == selectedHydraPlayerId);

            if (target != null && target.Data != null)
            {
                GUILayout.Label($"<color=#aaaaaa>Selected:</color> {target.Data.PlayerName}", new GUIStyle(GUI.skin.label) { richText = true, fontSize = 14 });
                GUILayout.Space(10);

                GUILayout.BeginHorizontal();
                GUI.backgroundColor = new Color(0.8f, 0.2f, 0.2f, 1f);
                if (GUILayout.Button("KILL", btnStyle, GUILayout.Height(30)))
                {
                    Vector3 op = PlayerControl.LocalPlayer.transform.position;
                    PlayerControl.LocalPlayer.NetTransform.RpcSnapTo(target.transform.position);
                    PlayerControl.LocalPlayer.CmdCheckMurder(target);
                    PlayerControl.LocalPlayer.RpcMurderPlayer(target, true);
                    PlayerControl.LocalPlayer.NetTransform.RpcSnapTo(op);
                }
                GUI.backgroundColor = Color.white;

                GUILayout.Space(5);
                if (GUILayout.Button("Force Meeting", btnStyle, GUILayout.Height(30))) ForceMeetingAsPlayer(target);

                GUILayout.Space(5);
                bool hr = rainbowPlayers.Contains(target.PlayerId);
                if (GUILayout.Button(hr ? "RGB: ON" : "RGB: OFF", hr ? activeTabStyle : btnStyle, GUILayout.Height(30)))
                {
                    if (!hr) rainbowPlayers.Add(target.PlayerId);
                    else rainbowPlayers.Remove(target.PlayerId);
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(15);
                GUILayout.Label("<color=#aaaaaa>Morph Target:</color>", new GUIStyle(GUI.skin.label) { richText = true, fontSize = 11 });
                GUILayout.BeginHorizontal();

                int mIdx = lockedPlayersList.FindIndex(p => p.PlayerId == selectedMorphTargetId);

                GUI.backgroundColor = currentAccentColor;
                if (GUILayout.Button("<", btnStyle, GUILayout.Width(25), GUILayout.Height(25)))
                {
                    if (lockedPlayersList.Count > 0) { mIdx--; if (mIdx < 0) mIdx = lockedPlayersList.Count - 1; selectedMorphTargetId = lockedPlayersList[mIdx].PlayerId; }
                }
                GUI.backgroundColor = Color.white;

                string morphName = "Target";
                if (mIdx >= 0 && mIdx < lockedPlayersList.Count) morphName = lockedPlayersList[mIdx].Data.PlayerName;
                if (morphName.Length > 10) morphName = morphName.Substring(0, 10) + "..";

                GUIStyle morphLabelStyle = new GUIStyle(btnStyle);
                morphLabelStyle.normal.background = null;
                morphLabelStyle.hover.background = null;
                morphLabelStyle.normal.textColor = currentAccentColor;
                morphLabelStyle.fontStyle = FontStyle.Bold;
                morphLabelStyle.alignment = TextAnchor.MiddleCenter;

                GUILayout.Label(morphName, morphLabelStyle, GUILayout.Height(25), GUILayout.ExpandWidth(true));

                GUI.backgroundColor = currentAccentColor;
                if (GUILayout.Button(">", btnStyle, GUILayout.Width(25), GUILayout.Height(25)))
                {
                    if (lockedPlayersList.Count > 0) { mIdx++; if (mIdx >= lockedPlayersList.Count) mIdx = 0; selectedMorphTargetId = lockedPlayersList[mIdx].PlayerId; }
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(5);
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                GUI.backgroundColor = currentAccentColor;
                if (GUILayout.Button("MORPH TARGET", btnStyle, GUILayout.Width(160), GUILayout.Height(25)))
                {
                    var morphTarget = lockedPlayersList.FirstOrDefault(p => p.PlayerId == selectedMorphTargetId) ?? target;
                    this.StartCoroutine(AttemptShapeshiftFrame(target, morphTarget).WrapToIl2Cpp());
                }
                GUI.backgroundColor = Color.white;

                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.Space(15);
                GUILayout.Label("SET PLAYER COLOR", headerStyle);
                GUILayout.BeginVertical(boxStyle);

                GUIStyle roundedColorBtnStyle = new GUIStyle();
                roundedColorBtnStyle.normal.background = texColorBtn;
                roundedColorBtnStyle.margin = CreateRectOffset(2, 2, 2, 2);

                int colorsPerRow = 7;
                for (int i = 0; i < Palette.PlayerColors.Length; i++)
                {
                    if (i % colorsPerRow == 0) GUILayout.BeginHorizontal();

                    GUI.color = Palette.PlayerColors[i];

                    if (GUILayout.Button("", roundedColorBtnStyle, GUILayout.Width(32), GUILayout.Height(30)))
                        target.RpcSetColor((byte)i);

                    if (i % colorsPerRow == colorsPerRow - 1 || i == Palette.PlayerColors.Length - 1)
                        GUILayout.EndHorizontal();
                }
                GUI.color = Color.white;
                GUILayout.EndVertical();

                GUILayout.Space(15);
                GUILayout.Label("PRE-GAME ROLE (HOST)", headerStyle);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Impostor", btnStyle, GUILayout.Height(25))) { forcedPreGameRoles.Remove(target.PlayerId); forcedImpostors.Add(target.PlayerId); enablePreGameRoleForce = true; }
                if (GUILayout.Button("Crewmate", btnStyle, GUILayout.Height(25))) { forcedImpostors.Remove(target.PlayerId); forcedPreGameRoles[target.PlayerId] = RoleTypes.Crewmate; enablePreGameRoleForce = true; }
                if (GUILayout.Button("Shapeshifter", btnStyle, GUILayout.Height(25))) { forcedImpostors.Remove(target.PlayerId); forcedPreGameRoles[target.PlayerId] = RoleTypes.Shapeshifter; enablePreGameRoleForce = true; }
                GUILayout.EndHorizontal();
                GUILayout.Space(5);
                if (GUILayout.Button("REMOVE FORCED ROLE", activeTabStyle, GUILayout.Height(25))) { forcedPreGameRoles.Remove(target.PlayerId); forcedImpostors.Remove(target.PlayerId); }
            }
            else
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("<color=#777777>Select a player...</color>", new GUIStyle(GUI.skin.label) { richText = true, alignment = TextAnchor.MiddleCenter });
                GUILayout.FlexibleSpace();
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

        private void DrawRolesTab()
        {
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(GUILayout.Width(280));

            GUILayout.BeginVertical(boxStyle);
            GUILayout.Label("Roles", headerStyle);
            GUILayout.BeginHorizontal();
            GUIStyle middleLabelStyle = new GUIStyle(btnStyle) { fontStyle = FontStyle.Bold, normal = { background = null, textColor = currentAccentColor } };
            if (GUILayout.Button("<", btnStyle, GUILayout.Width(25), GUILayout.Height(22))) { fakeRoleIdx--; if (fakeRoleIdx < 0) fakeRoleIdx = forceRoleOptions.Length - 1; }
            GUILayout.Label(forceRoleOptions[fakeRoleIdx].ToString(), middleLabelStyle, GUILayout.Width(100), GUILayout.Height(22));
            if (GUILayout.Button(">", btnStyle, GUILayout.Width(25), GUILayout.Height(22))) { fakeRoleIdx++; if (fakeRoleIdx >= forceRoleOptions.Length) fakeRoleIdx = 0; }
            GUILayout.Space(15);
            if (GUILayout.Button("Set", activeTabStyle, GUILayout.Width(45), GUILayout.Height(22))) RoleManager.Instance?.SetRole(PlayerControl.LocalPlayer, forceRoleOptions[fakeRoleIdx]);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.Space(5);
            GUILayout.BeginVertical(boxStyle);
            GUILayout.Label("Impostor", headerStyle);
            killReach = DrawToggle(killReach, "Kill Reach", 160);
            GUILayout.Space(5);
            killAnyone = DrawToggle(killAnyone, "Kill Anyone", 160);
            GUILayout.EndVertical();

            GUILayout.Space(5);
            GUILayout.BeginVertical(boxStyle);
            GUILayout.Label("Shapeshifter", headerStyle);
            NoShapeshiftAnim = DrawToggle(NoShapeshiftAnim, "No Ss Animation", 160);
            GUILayout.Space(5);
            endlessSsDuration = DrawToggle(endlessSsDuration, "Endless Ss Duration", 160);
            GUILayout.EndVertical();

            GUILayout.Space(5);
            GUILayout.BeginVertical(boxStyle);
            GUILayout.Label("Tracker", headerStyle);
            EndlessTracking = DrawToggle(EndlessTracking, "Endless Tracking", 160);
            GUILayout.Space(5);
            NoTrackingCooldown = DrawToggle(NoTrackingCooldown, "No Track Cooldown", 160);
            GUILayout.EndVertical();

            GUILayout.EndVertical();

            GUILayout.Space(10);

            GUILayout.BeginVertical(GUILayout.Width(280));

            GUILayout.BeginVertical(boxStyle);
            GUILayout.Label("Engineer", headerStyle);
            endlessVentTime = DrawToggle(endlessVentTime, "Endless Vent Time", 160);
            GUILayout.Space(5);
            noVentCooldown = DrawToggle(noVentCooldown, "No Vent Cooldown", 160);
            GUILayout.EndVertical();

            GUILayout.Space(5);
            GUILayout.BeginVertical(boxStyle);
            GUILayout.Label("Scientist", headerStyle);
            endlessBattery = DrawToggle(endlessBattery, "Endless Battery", 160);
            GUILayout.Space(5);
            noVitalsCooldown = DrawToggle(noVitalsCooldown, "No Vitals Cooldown", 160);
            GUILayout.EndVertical();

            GUILayout.Space(5);
            GUILayout.BeginVertical(boxStyle);
            GUILayout.Label("Detective", headerStyle);
            UnlimitedInterrogateRange = DrawToggle(UnlimitedInterrogateRange, "Interrogate Reach", 160);
            GUILayout.EndVertical();

            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private Vector2 doorsScrollPos = Vector2.zero;

        private void DrawSabotagesTab()
        {
            GUILayout.BeginVertical(boxStyle);
            GUILayout.Label("CRITICAL SABOTAGES", headerStyle);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("FIX ALL SABOTAGES", activeTabStyle, GUILayout.Height(35))) FixAllSabotages();
            GUI.backgroundColor = new Color(1f, 0.3f, 0.3f, 1f);
            if (GUILayout.Button("TRIGGER ALL", btnStyle, GUILayout.Height(35))) TriggerAllSabotages();
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            if (GUILayout.Button("CALL MEETING (LEGIT / PUBLIC)", btnStyle, GUILayout.Height(35))) callMeetingPublic();
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUI.backgroundColor = reactorSab ? new Color(1f, 0.3f, 0.3f, 1f) : Color.white;
            if (GUILayout.Button(reactorSab ? "Reactor: ON" : "Reactor", reactorSab ? activeTabStyle : btnStyle, GUILayout.Height(35))) { reactorSab = !reactorSab; ToggleReactor(reactorSab); }

            GUI.backgroundColor = oxygenSab ? new Color(1f, 0.3f, 0.3f, 1f) : Color.white;
            if (GUILayout.Button(oxygenSab ? "Oxygen: ON" : "Oxygen", oxygenSab ? activeTabStyle : btnStyle, GUILayout.Height(35))) { oxygenSab = !oxygenSab; ToggleO2(oxygenSab); }
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            GUI.backgroundColor = commsSab ? new Color(0.3f, 1f, 0.3f, 1f) : Color.white;
            if (GUILayout.Button(commsSab ? "Comms: ON" : "Comms", commsSab ? activeTabStyle : btnStyle, GUILayout.Height(35))) { commsSab = !commsSab; ToggleComms(commsSab); }

            GUI.backgroundColor = elecSab ? new Color(0.3f, 1f, 0.3f, 1f) : Color.white;
            if (GUILayout.Button(elecSab ? "Lights: ON" : "Lights", elecSab ? activeTabStyle : btnStyle, GUILayout.Height(35))) { elecSab = !elecSab; ToggleLights(elecSab); }
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            if (GUILayout.Button("Trigger Mushroom (Fungle)", btnStyle, GUILayout.Height(30))) SabotageMushroom();
            GUILayout.EndVertical();

            GUILayout.Space(10);

            GUILayout.BeginVertical(boxStyle);
            GUILayout.Label("DOORS CONTROL", headerStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Close All Doors", btnStyle, GUILayout.Height(30))) SabotageDoors();
            if (GUILayout.Button("Open All Doors", btnStyle, GUILayout.Height(30))) OpenAllDoors();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.Space(10);

            GUILayout.BeginVertical(boxStyle);
            GUILayout.Label("SPECIFIC DOORS", headerStyle);

            if (ShipStatus.Instance != null && ShipStatus.Instance.AllDoors != null)
            {
                var rooms = ShipStatus.Instance.AllDoors.Select(d => d.Room).Distinct().ToList();
                foreach (var room in rooms)
                {
                    GUILayout.BeginHorizontal(boxStyle);
                    GUILayout.Label($"<b>{room}</b>", toggleLabelStyle, GUILayout.Width(130));
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("Close", btnStyle, GUILayout.Width(70), GUILayout.Height(25)))
                    {
                        try
                        {
                            ShipStatus.Instance.RpcCloseDoorsOfType(room);
                            foreach (var d in ShipStatus.Instance.AllDoors)
                                if (d != null && d.Room == room)
                                    ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Doors, (byte)d.Id);
                        }
                        catch { }
                    }
                    GUILayout.Space(5);
                    if (GUILayout.Button("Open", btnStyle, GUILayout.Width(70), GUILayout.Height(25)))
                    {
                        foreach (var d in ShipStatus.Instance.AllDoors)
                        {
                            if (d != null && d.Room == room)
                            {
                                d.SetDoorway(true);
                                try { ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Doors, (byte)(d.Id | 64)); } catch { }
                            }
                        }
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.Space(2);
                }
            }
            else
            {
                GUILayout.Label("<color=#777777>Вы не в игре или на карте нет дверей.</color>", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, richText = true });
            }
            GUILayout.EndVertical();
        }
        private void callMeetingPublic()
        {
            if (PlayerControl.LocalPlayer == null || PlayerControl.AllPlayerControls == null) return;
            try
            {
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    if (pc != null && pc.Data != null && pc.Data.IsDead && !pc.Data.Disconnected)
                    {
                        PlayerControl.LocalPlayer.CmdReportDeadBody(pc.Data);
                        ShowNotification($"<color=#00FF00>[MEETING]</color> Найден и зарепорчен труп: <b>{pc.Data.PlayerName}</b>!");
                        return;
                    }
                }

                PlayerControl.LocalPlayer.CmdReportDeadBody(null);
                ShowNotification("<color=#00FF00>[MEETING]</color> Легально нажата кнопка собрания!");
            }
            catch (Exception ex)
            {
                Debug.Log("Public Meeting Error: " + ex.Message);
            }
        }
        private void TriggerAllSabotages()
        {
            if (ShipStatus.Instance == null) return;
            try
            {
                reactorSab = true;
                oxygenSab = true;
                commsSab = true;
                elecSab = true;

                ToggleReactor(true);
                ToggleO2(true);
                ToggleComms(true);
                ToggleLights(true);

                ShowNotification("<color=#FF0000>[SABOTAGE]</color> Все системы саботированы!");
            }
            catch (Exception ex) { Debug.Log("Trigger All Sabotages Error: " + ex.Message); }
        }
        private void FixAllSabotages()
        {
            if (ShipStatus.Instance == null) return;
            try
            {
                reactorSab = false;
                oxygenSab = false;
                commsSab = false;
                elecSab = false;

                ToggleReactor(false);
                ToggleO2(false);
                ToggleComms(false);
                ToggleLights(false);

                if (ShipStatus.Instance.AllDoors != null)
                {
                    foreach (var door in ShipStatus.Instance.AllDoors)
                    {
                        if (door != null)
                        {
                            door.SetDoorway(true);
                            try { ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Doors, (byte)(door.Id | 64)); } catch { }
                        }
                    }
                }
                try { ShipStatus.Instance.RpcUpdateSystem(SystemTypes.MushroomMixupSabotage, 0); } catch { }
                ShowNotification("<color=#00FF00>[SABOTAGE]</color> Все саботажи и двери починены!");
            }
            catch (Exception ex) { Debug.Log("Fix All Sabotages Error: " + ex.Message); }
        }

        private void SabotageDoors()
        {
            if (ShipStatus.Instance == null || ShipStatus.Instance.AllDoors == null) return;
            try
            {
                var rooms = new System.Collections.Generic.HashSet<SystemTypes>();
                foreach (var door in ShipStatus.Instance.AllDoors)
                {
                    if (door != null)
                    {
                        rooms.Add(door.Room);
                        try { ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Doors, (byte)door.Id); } catch { }
                    }
                }
                foreach (var room in rooms)
                {
                    try { ShipStatus.Instance.RpcCloseDoorsOfType(room); } catch { }
                }
                ShowNotification("<color=#FF0000>[DOORS]</color> Сигнал на закрытие отправлен!");
            }
            catch { }
        }

        private void OpenAllDoors()
        {
            if (ShipStatus.Instance == null || ShipStatus.Instance.AllDoors == null) return;
            try
            {
                foreach (var door in ShipStatus.Instance.AllDoors)
                {
                    if (door != null)
                    {
                        door.SetDoorway(true);
                        try { ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Doors, (byte)(door.Id | 64)); } catch { }
                    }
                }
                ShowNotification("<color=#00FF00>[DOORS]</color> Все двери открыты!");
            }
            catch { }
        }

        private void ToggleReactor(bool state) { if (ShipStatus.Instance == null) return; byte flag = (byte)(state ? 128 : 16); try { ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Reactor, flag); ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Laboratory, flag); if (state) ShipStatus.Instance.RpcUpdateSystem(SystemTypes.HeliSabotage, (byte)128); else { ShipStatus.Instance.RpcUpdateSystem(SystemTypes.HeliSabotage, (byte)16); ShipStatus.Instance.RpcUpdateSystem(SystemTypes.HeliSabotage, (byte)17); } } catch { } }
        private void ToggleO2(bool state) { if (ShipStatus.Instance == null) return; try { ShipStatus.Instance.RpcUpdateSystem(SystemTypes.LifeSupp, (byte)(state ? 128 : 16)); } catch { } }
        private void ToggleComms(bool state) { if (ShipStatus.Instance == null) return; try { if (state) ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Comms, (byte)128); else { ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Comms, (byte)16); ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Comms, (byte)17); } } catch { } }
        private void ToggleLights(bool state)
        {
            if (ShipStatus.Instance == null) return;
            try
            {
                if (state)
                {
                    byte b = 4;
                    for (int i = 0; i < 5; i++) if (UnityEngine.Random.value > 0.5f) b |= (byte)(1 << i);
                    ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Electrical, (byte)(b | 128));
                }
                else
                {
                    var sys = ShipStatus.Instance.Systems[SystemTypes.Electrical].Cast<SwitchSystem>();
                    if (sys != null)
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            bool expected = (sys.ExpectedSwitches & (1 << i)) != 0;
                            bool actual = (sys.ActualSwitches & (1 << i)) != 0;
                            if (expected != actual) ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Electrical, (byte)i);
                        }
                    }
                }
            }
            catch { }
        }
        private void SabotageMushroom() { if (ShipStatus.Instance == null) return; try { ShipStatus.Instance.RpcUpdateSystem(SystemTypes.MushroomMixupSabotage, (byte)1); } catch { } }

        private void DrawPlayersRoles()
        {
            GUILayout.BeginVertical(boxStyle);
            GUILayout.Label("PRE-GAME ROLE MANAGER", headerStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(enablePreGameRoleForce ? "Role Forcing: ON" : "Role Forcing: OFF", enablePreGameRoleForce ? activeTabStyle : btnStyle, GUILayout.Height(25))) enablePreGameRoleForce = !enablePreGameRoleForce;
            if (GUILayout.Button("Random 2 Imps", btnStyle, GUILayout.Width(110), GUILayout.Height(25)))
            {
                forcedPreGameRoles.Clear(); forcedImpostors.Clear();
                var activePlayers = PlayerControl.AllPlayerControls.ToArray().Where(p => p != null && !p.Data.Disconnected).ToList();
                if (activePlayers.Count >= 2)
                {
                    for (int i = activePlayers.Count - 1; i > 0; i--) { int swapIndex = UnityEngine.Random.Range(0, i + 1); var temp = activePlayers[i]; activePlayers[i] = activePlayers[swapIndex]; activePlayers[swapIndex] = temp; }
                    forcedImpostors.Add(activePlayers[0].PlayerId); forcedImpostors.Add(activePlayers[1].PlayerId);
                    enablePreGameRoleForce = true;
                }
            }
            if (GUILayout.Button("Clear All Roles", btnStyle, GUILayout.Width(110), GUILayout.Height(25))) { forcedPreGameRoles.Clear(); forcedImpostors.Clear(); }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(boxStyle, GUILayout.Width(200));
            preRolesListScrollPos = GUILayout.BeginScrollView(preRolesListScrollPos);
            foreach (var pc in lockedPlayersList)
            {
                if (pc == null || pc.Data == null || pc.PlayerId >= 100) continue;
                string pName = pc.Data.PlayerName ?? "Unknown";
                if (forcedPreGameRoles.ContainsKey(pc.PlayerId)) { string rShort = forcedPreGameRoles[pc.PlayerId].ToString().Replace("9", "Pha").Replace("10", "Tra").Replace("8", "Noi").Replace("12", "Det").Replace("18", "Vip"); if (rShort.Length > 3) rShort = rShort.Substring(0, 3); pName += $" [{rShort}]"; }
                else if (forcedImpostors.Contains(pc.PlayerId)) pName += " [Imp]";
                bool isSelected = selectedPreRoleId == pc.PlayerId;
                try { GUI.contentColor = Palette.PlayerColors[pc.Data.DefaultOutfit.ColorId]; } catch { }
                if (GUILayout.Button(pName, isSelected ? activeTabStyle : btnStyle, GUILayout.Height(30))) selectedPreRoleId = pc.PlayerId;
                GUI.contentColor = Color.white;
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.BeginVertical(boxStyle, GUILayout.ExpandWidth(true));
            preRolesActionScrollPos = GUILayout.BeginScrollView(preRolesActionScrollPos);
            PlayerControl target = lockedPlayersList.FirstOrDefault(p => p.PlayerId == selectedPreRoleId);
            if (target != null && target.Data != null)
            {
                GUIStyle infoStyle = new GUIStyle(GUI.skin.label) { richText = true, fontSize = 14 };
                GUILayout.Label($"<color=#aaaaaa>Selecting role for:</color> {target.Data.PlayerName}", infoStyle);
                RoleTypes currentForced = forcedPreGameRoles.ContainsKey(target.PlayerId) ? forcedPreGameRoles[target.PlayerId] : RoleTypes.Crewmate;
                bool isForced = forcedPreGameRoles.ContainsKey(target.PlayerId) || forcedImpostors.Contains(target.PlayerId);
                string roleNameStr = currentForced.ToString().Replace("9", "Phantom").Replace("10", "Tracker").Replace("8", "Noisemaker").Replace("12", "Detective").Replace("18", "Viper");
                if (forcedImpostors.Contains(target.PlayerId)) roleNameStr = "Impostor";
                GUILayout.Label($"<color=#aaaaaa>Status:</color> {(isForced ? $"<color=#00FF00>Forced ({roleNameStr})</color>" : "<color=#FF0000>Not Forced (Random)</color>")}", infoStyle);
                GUILayout.Space(15);
                GUILayout.Label("IMPOSTOR ROLES (Red Team)", headerStyle);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Impostor", btnStyle, GUILayout.Height(30))) { forcedPreGameRoles.Remove(target.PlayerId); forcedImpostors.Add(target.PlayerId); }
                if (GUILayout.Button("Shapeshifter", btnStyle, GUILayout.Height(30))) { forcedImpostors.Remove(target.PlayerId); forcedPreGameRoles[target.PlayerId] = RoleTypes.Shapeshifter; }
                if (GUILayout.Button("Phantom", btnStyle, GUILayout.Height(30))) { forcedImpostors.Remove(target.PlayerId); forcedPreGameRoles[target.PlayerId] = (RoleTypes)9; }
                if (GUILayout.Button("Viper", btnStyle, GUILayout.Height(30))) { forcedImpostors.Remove(target.PlayerId); forcedPreGameRoles[target.PlayerId] = (RoleTypes)18; }
                GUILayout.EndHorizontal();
                GUILayout.Space(10);
                GUILayout.Label("CREWMATE ROLES (Blue Team)", headerStyle);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Crewmate", btnStyle, GUILayout.Height(30))) { forcedImpostors.Remove(target.PlayerId); forcedPreGameRoles[target.PlayerId] = RoleTypes.Crewmate; }
                if (GUILayout.Button("Engineer", btnStyle, GUILayout.Height(30))) { forcedImpostors.Remove(target.PlayerId); forcedPreGameRoles[target.PlayerId] = RoleTypes.Engineer; }
                if (GUILayout.Button("Scientist", btnStyle, GUILayout.Height(30))) { forcedImpostors.Remove(target.PlayerId); forcedPreGameRoles[target.PlayerId] = RoleTypes.Scientist; }
                if (GUILayout.Button("Tracker", btnStyle, GUILayout.Height(30))) { forcedImpostors.Remove(target.PlayerId); forcedPreGameRoles[target.PlayerId] = (RoleTypes)10; }
                GUILayout.EndHorizontal();
                GUILayout.Space(5);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Noisemaker", btnStyle, GUILayout.Height(30))) { forcedImpostors.Remove(target.PlayerId); forcedPreGameRoles[target.PlayerId] = (RoleTypes)8; }
                if (GUILayout.Button("Guardian Angel", btnStyle, GUILayout.Height(30))) { forcedImpostors.Remove(target.PlayerId); forcedPreGameRoles[target.PlayerId] = RoleTypes.GuardianAngel; }
                if (GUILayout.Button("Detective", btnStyle, GUILayout.Height(30))) { forcedImpostors.Remove(target.PlayerId); forcedPreGameRoles[target.PlayerId] = (RoleTypes)12; }
                GUILayout.EndHorizontal();
                GUILayout.Space(15);
                if (GUILayout.Button("REMOVE FORCED ROLE", activeTabStyle, GUILayout.Height(35))) { forcedPreGameRoles.Remove(target.PlayerId); forcedImpostors.Remove(target.PlayerId); }
                GUILayout.Space(20);
                GUILayout.Label("<color=#777777><b>Hide & Seek Notice:</b>\nВыбор Impostor/Shapeshifter/Phantom/Viper расширит лимит маньяков (Seekers) в Прятках!</color>", new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true });
            }
            else
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("<color=#777777>Select a player to set their role</color>", new GUIStyle(GUI.skin.label) { richText = true, alignment = TextAnchor.MiddleCenter });
                GUILayout.FlexibleSpace();
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private void DrawMenuTab()
        {
            GUILayout.BeginVertical(boxStyle);
            GUILayout.Label("MENU CUSTOMIZATION", headerStyle);
            GUILayout.Space(5);

            bool prevRgb = rgbMenuMode;
            rgbMenuMode = DrawToggle(rgbMenuMode, "RGB Menu Mode");
            if (prevRgb && !rgbMenuMode) UpdateAccentColor(menuColors[currentMenuColorIndex]);

            GUILayout.Space(5);

            bool prevBg = enableBackground;
            enableBackground = DrawToggle(enableBackground, "Enable Image Background");
            if (enableBackground && !prevBg) LoadBackgroundImage();

            GUILayout.Space(5);
            GUILayout.Label("<color=#777777>Put 'MenuBG.png' or .jpg in BepInEx/config to add a background image.</color>", new GUIStyle(GUI.skin.label) { richText = true, fontSize = 11 });

            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUIStyle middleColorStyle = new GUIStyle(btnStyle) { normal = { background = null, textColor = currentAccentColor }, fontStyle = FontStyle.Bold };
            GUI.enabled = !rgbMenuMode;
            if (GUILayout.Button("<", btnStyle, GUILayout.Width(30), GUILayout.Height(25))) { currentMenuColorIndex--; if (currentMenuColorIndex < 0) currentMenuColorIndex = menuColors.Length - 1; if (!rgbMenuMode) UpdateAccentColor(menuColors[currentMenuColorIndex]); }
            GUILayout.Label(menuColorNames[currentMenuColorIndex], middleColorStyle, GUILayout.Width(110), GUILayout.Height(25));
            if (GUILayout.Button(">", btnStyle, GUILayout.Width(30), GUILayout.Height(25))) { currentMenuColorIndex++; if (currentMenuColorIndex >= menuColors.Length) currentMenuColorIndex = 0; if (!rgbMenuMode) UpdateAccentColor(menuColors[currentMenuColorIndex]); }
            GUI.enabled = true;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.Space(10);

            GUILayout.BeginVertical(boxStyle);
            GUILayout.Label("SPOOF MENU IDENTITY", headerStyle);
            SpoofMenuEnabled = DrawToggle(SpoofMenuEnabled, "Enable Fake RPC");
            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            GUIStyle middleLabelStyle = new GUIStyle(btnStyle) { fontStyle = FontStyle.Bold, normal = { background = null, textColor = currentAccentColor } };
            if (GUILayout.Button("<", btnStyle, GUILayout.Width(30), GUILayout.Height(25))) { selectedSpoofMenuIndex--; if (selectedSpoofMenuIndex < 0) selectedSpoofMenuIndex = spoofMenuNames.Length - 1; }
            GUILayout.Label($"{spoofMenuNames[selectedSpoofMenuIndex]}", middleLabelStyle, GUILayout.Width(110), GUILayout.Height(25));
            if (GUILayout.Button(">", btnStyle, GUILayout.Width(30), GUILayout.Height(25))) { selectedSpoofMenuIndex++; if (selectedSpoofMenuIndex >= spoofMenuNames.Length) selectedSpoofMenuIndex = 0; }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.Space(10);

            GUILayout.BeginVertical(boxStyle);
            GUILayout.Label("NOTIFICATIONS & LOGGING", headerStyle);
            GUILayout.Space(5);
            EnableCustomNotifs = DrawToggle(EnableCustomNotifs, "Enable Custom UI Notifications", 250);
            GUILayout.Space(5);
            LogAllRPCs = DrawToggle(LogAllRPCs, "Sniff All RPCs (On-Screen)", 250);
            GUILayout.EndVertical();
        }
        private Vector2 outfitsScrollPos = Vector2.zero;
        public static bool AutoHostEnabled = false;
        public static bool AutoReturnLobbyAfterMatch = true;
        public static bool AutoHostNotifications = true;
        public static bool AutoHostForceLastMinute = true;
        public static bool AutoHostWaitLoadedPlayers = true;
        public static bool AutoHostCancelBelowMin = true;
        public static bool AutoHostInstantStart = false;

        public static int AutoHostMinPlayers = 4;
        public static int AutoHostForceMinPlayers = 2;
        public static float AutoHostStartDelaySeconds = 15f;
        public static float AutoHostBackoffSeconds = 8f;
        public static float AutoHostWarmupSeconds = 5f;
        public static float AutoHostLoadGraceSeconds = 20f;

        public static int AutoHostForceAfterMinutes = 0;
        public static int AutoHostFastStartPlayers = 13;
        public static float AutoHostFastStartDelaySeconds = 5f;

        private int currentAutoHostSubTab = 0;
        private string[] autoHostSubTabs = { "LOBBY CONTROLS", "ROLE MANAGER", "ANTI CHEAT", "AUTO HOST" };
        private void DrawOutfitsTab()
        {
            GUILayout.BeginVertical(boxStyle);
            GUILayout.Label("COPY SPECIFIC PLAYER", headerStyle);

            outfitsScrollPos = GUILayout.BeginScrollView(outfitsScrollPos);
            if (lockedPlayersList.Count > 0)
            {
                foreach (var pc in lockedPlayersList)
                {
                    if (pc == null || pc == PlayerControl.LocalPlayer || pc.Data == null) continue;

                    GUILayout.BeginHorizontal(boxStyle);
                    try
                    {
                        string pName = pc.Data.PlayerName ?? "Unknown";
                        GUILayout.Label(pName, GUILayout.Width(150));

                        if (GUILayout.Button("Copy Outfit", btnStyle, GUILayout.Height(25)))
                        {
                            try
                            {
                                PlayerControl.LocalPlayer.RpcSetSkin(pc.Data.DefaultOutfit.SkinId);
                                PlayerControl.LocalPlayer.RpcSetHat(pc.Data.DefaultOutfit.HatId);
                                PlayerControl.LocalPlayer.RpcSetVisor(pc.Data.DefaultOutfit.VisorId);
                                PlayerControl.LocalPlayer.RpcSetNamePlate(pc.Data.DefaultOutfit.NamePlateId);
                                PlayerControl.LocalPlayer.RpcSetPet(pc.Data.DefaultOutfit.PetId);
                            }
                            catch { }
                        }
                    }
                    finally { GUILayout.EndHorizontal(); }
                    GUILayout.Space(2);
                }
            }
            else
            {
                GUILayout.Label("<color=#777777>Нет игроков для копирования.</color>");
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }
        public void Start()
        {
            if (enableBackground) LoadBackgroundImage();
            UnlockCosmetics();
            LoadConfig();

            LoadBanList();
        }
        public static KeyCode bindMagnetCursor = KeyCode.F9;
        public static bool isWaitBindMagnetCursor = false;
        public void Update()
        {
            KeyCode toggleKey = keyBinds.ContainsKey("Toggle Menu") ? keyBinds["Toggle Menu"] : KeyCode.Insert;
            if (Input.GetKeyDown(toggleKey) || Input.GetKeyDown(KeyCode.RightShift)) showMenu = !showMenu;

            bool isTypingOrBinding = isEditingName || isEditingLevel || isEditingBan ||
                                     isWaitingForBind || isWaitBindMassMorph || isWaitBindSpawnLobby ||
                                     isWaitBindDespawnLobby || isWaitBindCloseMeeting || isWaitBindInstaStart ||
                                     isWaitBindEndCrew || isWaitBindEndImp || isWaitBindEndImpDC || isWaitBindEndHnsDC;

            if (!isTypingOrBinding)
            {
                if (bindMassMorph != KeyCode.None && Input.GetKeyDown(bindMassMorph))
                    this.StartCoroutine(MassMorphCoroutine().WrapToIl2Cpp());

                if (bindSpawnLobby != KeyCode.None && Input.GetKeyDown(bindSpawnLobby))
                    SpawnLobby();

                if (bindDespawnLobby != KeyCode.None && Input.GetKeyDown(bindDespawnLobby))
                    DespawnLobby();

                if (bindCloseMeeting != KeyCode.None && Input.GetKeyDown(bindCloseMeeting) && MeetingHud.Instance != null)
                    MeetingHud.Instance.RpcClose();

                if (bindInstaStart != KeyCode.None && Input.GetKeyDown(bindInstaStart) && GameStartManager.Instance != null)
                {
                    GameStartManager.Instance.startState = GameStartManager.StartingStates.Countdown;
                    GameStartManager.Instance.countDownTimer = 0f;
                }
                if (bindMagnetCursor != KeyCode.None && Input.GetKeyDown(bindMagnetCursor))
                {
                    autoFollowCursor = !autoFollowCursor;
                    ShowNotification(autoFollowCursor ?
                        "<color=#00FF00>[MAGNET]</color> Magnet Cursor: ON" :
                        "<color=#FF0000>[MAGNET]</color> Magnet Cursor: OFF");
                }
                if (bindEndCrew != KeyCode.None && Input.GetKeyDown(bindEndCrew)) SmartEndGame("CrewWin");
                if (bindEndImp != KeyCode.None && Input.GetKeyDown(bindEndImp)) SmartEndGame("ImpWin");
                if (bindEndImpDC != KeyCode.None && Input.GetKeyDown(bindEndImpDC)) SmartEndGame("ImpDisconnect");
                if (bindEndHnsDC != KeyCode.None && Input.GetKeyDown(bindEndHnsDC)) SmartEndGame("HnsImpDisconnect");
            }

            NjordAutoHostService.Tick();
            NjordAutoLobbyReturn.UpdateLogic();

            if (stylesInited && rgbMenuMode)
            {
                rgbMenuHue += Time.deltaTime * 0.2f;
                if (rgbMenuHue > 1f) rgbMenuHue -= 1f;
                UpdateAccentColor(Color.HSVToRGB(rgbMenuHue, 1f, 1f));
            }
if (PlayerControl.LocalPlayer != null)
            {
                if (Input.GetKeyDown(KeyCode.F9))
                {
                    autoFollowCursor = !autoFollowCursor;
                    ShowNotification(autoFollowCursor ? 
                        "<color=#00FF00>[MAGNET]</color> Magnet Cursor: ON" : 
                        "<color=#FF0000>[MAGNET]</color> Magnet Cursor: OFF");
                }

                if ((tpToCursor && Input.GetMouseButtonDown(1)) || 
                    (dragToCursor && Input.GetMouseButton(2)) || 
                    autoFollowCursor)
                {
                    if (Camera.main != null)
                    {
                        Vector3 mPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                        mPos.z = PlayerControl.LocalPlayer.transform.position.z;
                        PlayerControl.LocalPlayer.NetTransform.RpcSnapTo(mPos);
                    }
                }

                try
                {
                    if (PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.MyPhysics != null && PlayerControl.LocalPlayer.Data != null)
                    {
                        if (PlayerControl.LocalPlayer.Collider != null)
                        {
                            PlayerControl.LocalPlayer.Collider.enabled = !(noClip || PlayerControl.LocalPlayer.onLadder);
                        }

                        float baseSpeed = 3f;
                        float targetSpeed = walkSpeed * baseSpeed;

                        if (PlayerControl.LocalPlayer.Data.IsDead)
                        {
                            PlayerControl.LocalPlayer.MyPhysics.GhostSpeed = targetSpeed;
                        }
                        else
                        {
                            PlayerControl.LocalPlayer.MyPhysics.Speed = targetSpeed;
                        }
                    }
                }
                catch { }

                if (wasShowMenu && !showMenu) SaveConfig();
                wasShowMenu = showMenu;


                if (SpoofMenuEnabled && PlayerControl.LocalPlayer != null)
                {
                    uiSpoofTimer += Time.deltaTime;
                    if (uiSpoofTimer >= rpcSpoofDelay)
                    {
                        uiSpoofTimer = 0f;
                        byte rpc = spoofMenuRPCs[selectedSpoofMenuIndex];
                        try
                        {
                            MessageWriter msg = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, rpc, SendOption.None, -1);
                            AmongUsClient.Instance.FinishRpcImmediately(msg);
                        }
                        catch { }
                    }
                }
                try
                {
                    if (autoBanEnabled && AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost && PlayerControl.AllPlayerControls != null)
                    {
                        foreach (var pc in PlayerControl.AllPlayerControls)
                        {
                            if (pc == null || pc.Data == null || pc.Data.Disconnected || pc == PlayerControl.LocalPlayer) continue;

                            string fc = pc.Data.FriendCode;
                            if (!string.IsNullOrEmpty(fc))
                            {
                                foreach (var entry in bannedEntries)
                                {
                                    string[] parts = entry.Split('|');
                                    if (parts.Length > 0 && parts[0].Trim().ToLower() == fc.Trim().ToLower())
                                    {
                                        AmongUsClient.Instance.KickPlayer(pc.OwnerId, true);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
                catch { }
                if (freecam)
                {
                    if (!_freecamActive && Camera.main != null)
                    {
                        var cam = Camera.main.gameObject.GetComponent<FollowerCamera>();
                        if (cam != null) { cam.enabled = false; cam.Target = null; }
                        _freecamActive = true;
                    }
                    if (PlayerControl.LocalPlayer != null) PlayerControl.LocalPlayer.moveable = false;
                    Vector3 movement = new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"), 0.0f);
                    if (Camera.main != null) Camera.main.transform.position += movement * 15f * Time.deltaTime;
                }
                else if (_freecamActive)
                {
                    if (PlayerControl.LocalPlayer != null) PlayerControl.LocalPlayer.moveable = true;
                    if (Camera.main != null)
                    {
                        var cam = Camera.main.gameObject.GetComponent<FollowerCamera>();
                        if (cam != null && PlayerControl.LocalPlayer != null) { cam.enabled = true; cam.SetTarget(PlayerControl.LocalPlayer); }
                    }
                    _freecamActive = false;
                }

                try
                {
                    if (cameraZoom && Camera.main != null && Input.GetAxis("Mouse ScrollWheel") != 0f)
                    {
                        if (Input.GetAxis("Mouse ScrollWheel") < 0f) Camera.main.orthographicSize += 0.5f;
                        else if (Input.GetAxis("Mouse ScrollWheel") > 0f && Camera.main.orthographicSize > 3f) Camera.main.orthographicSize -= 0.5f;
                    }
                }
                catch { }

                try
                {
                    if (rainbowPlayers.Count > 0 && AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost && PlayerControl.AllPlayerControls != null)
                    {
                        colorTimer += Time.deltaTime;
                        if (colorTimer > 0.15f)
                        {
                            colorTimer = 0f;
                            currentColorId++;
                            if (currentColorId > 17) currentColorId = 0;
                            foreach (var p in PlayerControl.AllPlayerControls)
                                if (p != null && p.Data != null && !p.Data.Disconnected && rainbowPlayers.Contains(p.PlayerId))
                                    p.RpcSetColor(currentColorId);
                        }
                    }
                }
                catch { }
                try
                {
                    if (PlayerControl.AllPlayerControls != null)
                    {
                        foreach (var pc in PlayerControl.AllPlayerControls)
                        {
                            if (pc != null) HandleTracer(pc, showTracers);
                        }
                    }
                }
                catch { }



                if (!isEditingLevel && uint.TryParse(spoofLevelString, out uint parsedLvl))
                {
                    uint targetLevel = parsedLvl > 0 ? parsedLvl - 1 : 0;
                    try
                    {
                        if (AmongUs.Data.DataManager.Player.stats.level != targetLevel)
                        {
                            AmongUs.Data.DataManager.Player.stats.level = targetLevel;
                        }
                    }
                    catch
                    {
                        try
                        {
                            if (AmongUs.Data.DataManager.Player.Stats.Level != targetLevel)
                            {
                                AmongUs.Data.DataManager.Player.Stats.Level = targetLevel;
                            }
                        }
                        catch { }
                    }
                }
                try
                {
                    if (localRainbow || rainbowPlayers.Count > 0)
                    {
                        colorTimer += Time.deltaTime;
                        if (colorTimer > 0.15f)
                        {
                            colorTimer = 0f;
                            currentColorId++;
                            if (currentColorId > 17) currentColorId = 0;

                            if (localRainbow && PlayerControl.LocalPlayer != null)
                                PlayerControl.LocalPlayer.CmdCheckColor(currentColorId);

                            if (rainbowPlayers.Count > 0 && AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost && PlayerControl.AllPlayerControls != null)
                            {
                                foreach (var p in PlayerControl.AllPlayerControls)
                                {
                                    if (p != null && p.Data != null && !p.Data.Disconnected && rainbowPlayers.Contains(p.PlayerId))
                                        p.RpcSetColor(currentColorId);
                                }
                            }
                        }
                    }
                }


                catch { }


            }
        }


        public void OnGUI()
        {
            Event e = Event.current;

            bool isTyping = isEditingName || isEditingLevel || isEditingBan;
            bool isBinding = isWaitingForBind || isWaitBindMassMorph || isWaitBindSpawnLobby || isWaitBindDespawnLobby ||
                  isWaitBindCloseMeeting || isWaitBindInstaStart || isWaitBindEndCrew || isWaitBindEndImp ||
                  isWaitBindEndImpDC || isWaitBindEndHnsDC || isWaitBindMagnetCursor;

            if (e != null && e.isKey && e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.Escape)
                {
                    isEditingName = isEditingLevel = isEditingBan = false;
                    ResetAllBindWaits();
                    e.Use();
                }
                else if (isBinding && e.keyCode != KeyCode.None)
                {
                    if (isWaitingForBind) { menuToggleKey = e.keyCode; }
                    else if (isWaitBindMassMorph) { bindMassMorph = e.keyCode; }
                    else if (isWaitBindSpawnLobby) { bindSpawnLobby = e.keyCode; }
                    else if (isWaitBindDespawnLobby) { bindDespawnLobby = e.keyCode; }
                    else if (isWaitBindCloseMeeting) { bindCloseMeeting = e.keyCode; }
                    else if (isWaitBindInstaStart) { bindInstaStart = e.keyCode; }
                    else if (isWaitBindEndCrew) { bindEndCrew = e.keyCode; }
                    else if (isWaitBindEndImp) { bindEndImp = e.keyCode; }
                    else if (isWaitBindEndImpDC) { bindEndImpDC = e.keyCode; }
                    else if (isWaitBindEndHnsDC) { bindEndHnsDC = e.keyCode; }

                    ResetAllBindWaits();
                    SaveConfig();
                    e.Use();
                }
                else if (isTyping)
                {
                    if (e.keyCode == KeyCode.Backspace)
                    {
                        if (isEditingBan && banInput.Length > 0) { banInput = banInput.Substring(0, banInput.Length - 1); }
                        if (isEditingName && customNameInput.Length > 0) { customNameInput = customNameInput.Substring(0, customNameInput.Length - 1); }
                        if (isEditingLevel && spoofLevelString.Length > 0) { spoofLevelString = spoofLevelString.Substring(0, spoofLevelString.Length - 1); }
                        e.Use();
                    }
                    else if (e.character != 0 && e.character != '\n' && e.character != '\r')
                    {
                        if (isEditingBan) { banInput += e.character; }
                        if (isEditingName) { customNameInput += e.character; }
                        if (isEditingLevel) { spoofLevelString += e.character; }
                        e.Use();
                    }
                }
                else if (e.keyCode == menuToggleKey && !isTyping && !isBinding)
                {
                    showMenu = !showMenu;
                    if (!showMenu) SaveConfig();
                    e.Use();
                }
            }

            if (Event.current.type == EventType.Layout)
            {
                lockedPlayersList.Clear();
                if (PlayerControl.AllPlayerControls != null)
                {
                    foreach (var p in PlayerControl.AllPlayerControls)
                    {
                        if (p != null && p.Data != null && !p.Data.Disconnected && p.PlayerId < 100)
                            lockedPlayersList.Add(p);
                    }
                }

                if (!stylesInited) InitStyles();

                if (showMenu)
                {
                    windowRect = GUI.Window(0, windowRect, (Action<int>)DrawNjordMenu, "", windowStyle);
                }

                for (int i = screenNotifications.Count - 1; i >= 0; i--)
                {
                    screenNotifications[i].lifetime += Time.deltaTime;
                    if (screenNotifications[i].HasExpired) screenNotifications.RemoveAt(i);
                }
            }
        
            try
            {
                if (AmongUsClient.Instance != null && (AmongUsClient.Instance.GameState == InnerNetClient.GameStates.Joined || AmongUsClient.Instance.GameState == InnerNetClient.GameStates.Started))
                {
                    if (PlayerControl.AllPlayerControls != null)
                    {
                        List<byte> currentIds = new List<byte>();
                        foreach (var pc in PlayerControl.AllPlayerControls)
                        {
                            if (pc != null && pc.Data != null) currentIds.Add(pc.PlayerId);
                        }

                        foreach (var id in currentIds)
                        {
                            if (!lastPlayerIds.Contains(id) && !pendingJoinTimers.ContainsKey(id))
                            {
                                pendingJoinTimers[id] = 1.5f;
                            }
                        }

                        var keysToProcess = pendingJoinTimers.Keys.ToList();
                        foreach (var id in keysToProcess)
                        {
                            pendingJoinTimers[id] -= Time.deltaTime;
                            if (pendingJoinTimers[id] <= 0f)
                            {
                                pendingJoinTimers.Remove(id);

                                var pc = PlayerControl.AllPlayerControls.ToArray().FirstOrDefault(p => p != null && p.PlayerId == id);
                                if (pc != null && pc.Data != null && !pc.Data.Disconnected)
                                {
                                    if (DetailedJoinInfo)
                                    {
                                        int level = 1;
                                        try
                                        {
                                            uint rawLevel = pc.Data.PlayerLevel;
                                            if (rawLevel != uint.MaxValue && rawLevel < 10000) level = (int)rawLevel + 1;
                                        }
                                        catch { }

                                        string platform = GetPlatform(AmongUsClient.Instance.GetClientFromCharacter(pc));
                                        string fc = string.IsNullOrEmpty(pc.Data.FriendCode) ? "Hidden" : pc.Data.FriendCode;

                                        ShowNotification($"<color=#00FF00>[+]</color> {pc.Data.PlayerName} joined\n<color=#aaaaaa>Lvl: {level} | {platform} | FC: {fc}</color>");
                                    }
                                    else
                                    {
                                        ShowNotification($"<color=#00FF00>[+]</color> {pc.Data.PlayerName} присоединился");
                                    }
                                }
                            }
                        }

                        foreach (var id in lastPlayerIds)
                        {
                            if (!currentIds.Contains(id)) pendingJoinTimers.Remove(id);
                        }

                        lastPlayerIds = new List<byte>(currentIds);
                    }
                }
                else
                {
                    lastPlayerIds.Clear();
                    pendingJoinTimers.Clear();
                }
            }
            catch { }
            if (screenNotifications.Count > 0)
            {
                int maxNotifs = 6;
                int startIdx = Mathf.Max(0, screenNotifications.Count - maxNotifs);
                for (int i = startIdx; i < screenNotifications.Count; i++)
                {
                    NjordNotification notif = screenNotifications[i];
                    int reverseIndex = screenNotifications.Count - 1 - i;

                    float slideOffset = 0f;
                    float animSpeed = 0.3f;
                    float currentAlpha = 0.95f;

                    if (notif.lifetime < animSpeed)
                    {
                        float t = Mathf.Clamp01(1f - (notif.lifetime / animSpeed));
                        slideOffset = t * t * 300f;
                    }
                    else if (notif.lifetime > notif.ttl - animSpeed)
                    {
                        float t = Mathf.Clamp01((notif.lifetime - (notif.ttl - animSpeed)) / animSpeed);
                        slideOffset = t * t * 300f;
                        currentAlpha = Mathf.Lerp(0.95f, 0f, t);
                    }

                    float xPos = (float)Screen.width - notificationBoxSize.x - 15f + slideOffset;
                    float yPos = Screen.height - 150f - (reverseIndex * (notificationBoxSize.y + 5f));

                    GUI.color = new Color(0.12f, 0.12f, 0.12f, currentAlpha);
                    GUI.Box(new Rect(xPos, yPos, notificationBoxSize.x, notificationBoxSize.y), "", windowStyle);

                    GUI.color = new Color(1f, 1f, 1f, currentAlpha > 0.5f ? 1f : currentAlpha * 2f);
                    string accentHex = ColorUtility.ToHtmlStringRGB(currentAccentColor);

                    GUI.Label(new Rect(xPos + 10f, yPos + 5f, notificationBoxSize.x - 20f, 20f), $"<b><color=#{accentHex}>{notif.title}</color></b>");

                    float timeLeft = Mathf.Max(0, notif.ttl - notif.lifetime);
                    GUI.Label(new Rect(xPos + 10f, yPos + 5f, notificationBoxSize.x - 20f, 20f), $"<b><color=#{accentHex}>{timeLeft:F1}s</color></b>", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperRight, fontSize = 12, richText = true });
                    GUI.Label(new Rect(xPos + 10f, yPos + 25f, notificationBoxSize.x - 20f, notificationBoxSize.y - 30f), notif.message, new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true, fontSize = 12 });

                    float progress = 1f - (notif.lifetime / notif.ttl);
                    GUI.color = new Color(currentAccentColor.r, currentAccentColor.g, currentAccentColor.b, currentAlpha);
                    GUI.Box(new Rect(xPos + 8f, yPos + notificationBoxSize.y - 6f, (notificationBoxSize.x - 16f) * progress, 2f), "", safeLineStyle);
                    GUI.color = Color.white;
                }
            }
        }



        private void DrawNjordMenu(int windowID)
        {
            if (Event.current.type == EventType.Repaint && tabTransitionProgress < 1f)
            {
                tabTransitionProgress += Time.unscaledDeltaTime * 8f;
                if (tabTransitionProgress >= 1f) { tabTransitionProgress = 1f; currentTab = targetTabIndex; }
            }

            if (enableBackground && customMenuBg != null)
            {
                GUI.color = new Color(0.6f, 0.6f, 0.6f, 0.8f);
                GUIStyle bgStyle = new GUIStyle() { normal = { background = customMenuBg } };
                GUI.Box(new Rect(0, 0, windowRect.width, windowRect.height), GUIContent.none, bgStyle);
                GUI.color = Color.white;
            }

          GUILayout.BeginHorizontal();
            GUILayout.Label(ApplyMenuShimmer("NjordMenu Meowchelo & Carrot"), titleStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("-", new GUIStyle(btnStyle) { fixedWidth = 20, fixedHeight = 18, margin = CreateRectOffset(0, 8, 6, 0) })) showMenu = false;
            GUILayout.EndHorizontal();

            GUI.color = new Color(1f, 1f, 1f, 0.1f);
            GUI.Box(new Rect(0, 30, windowRect.width, 1), "", safeLineStyle);
            GUI.color = Color.white;

            GUILayout.BeginArea(new Rect(0f, 31f, 130f, windowRect.height - 31f));
            GUILayout.BeginVertical(sidebarStyle, GUILayout.ExpandHeight(true));
            GUILayout.Space(5);
            for (int i = 0; i < tabNames.Length; i++)
                if (GUILayout.Button(tabNames[i], i == targetTabIndex ? activeSidebarBtnStyle : sidebarBtnStyle, GUILayout.Height(24)))
                    if (targetTabIndex != i) { targetTabIndex = i; tabTransitionProgress = 0f; scrollPosition = Vector2.zero; }
            GUILayout.EndVertical();
            GUILayout.EndArea();

            GUI.color = new Color(1f, 1f, 1f, 0.1f);
            GUI.Box(new Rect(130, 31, 1, windowRect.height), "", safeLineStyle);
            GUI.color = new Color(1f, 1f, 1f, tabTransitionProgress);

            GUILayout.BeginArea(new Rect(140f, 36f + ((1f - tabTransitionProgress) * 10f), windowRect.width - 150f, windowRect.height - 46f));
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, false, GUIStyle.none, GUI.skin.verticalScrollbar);
            int tabToDraw = (tabTransitionProgress < 1f) ? targetTabIndex : currentTab;
            if (tabToDraw == 0) DrawGeneralTab();
            else if (tabToDraw == 1) DrawSelfTab();
            else if (tabToDraw == 2) DrawVisualsTab();
            else if (tabToDraw == 3) DrawPlayersTab();
            else if (tabToDraw == 4) DrawRolesTab();
            else if (tabToDraw == 5) DrawSabotagesTab();
            else if (tabToDraw == 6) DrawHostOnlyTab();
            else if (tabToDraw == 7) DrawOutfitsTab();
            else if (tabToDraw == 8) DrawMenuTab();
            GUILayout.EndScrollView();
            GUILayout.EndArea();

            GUI.color = Color.white;
            GUI.DragWindow(new Rect(0, 0, 10000, 30));
        }

        public static string GetPlatform(ClientData client)
        {
            if (client == null || client.PlatformData == null) return "Unknown";

            int platformId = (int)client.PlatformData.Platform;

            switch (platformId)
            {
                case 1: return "Epic";
                case 2: return "Steam";
                case 3: return "Mac";
                case 4: return "Microsoft";
                case 5: return "Itch";
                case 6: return "iOS";
                case 7: return "Android";
                case 8: return "Switch";
                case 9: return "Xbox";
                case 10: return "PlayStation";
                case 112: return "Starlight";
                default: return $"Unknown ({platformId})";
            }
        }
        public static Color GetRoleColor(int roleId, Color fallbackColor)
        {
            switch (roleId)
            {
                case 1: return new Color32(255, 0, 0, 255);
                case 2: return new Color32(0, 0, 128, 255);
                case 3: return new Color32(127, 255, 212, 255);
                case 4: return new Color32(176, 196, 222, 255);
                case 5: return new Color32(255, 140, 0, 255);
                case 8: return new Color32(255, 105, 180, 255);
                case 9: return new Color32(139, 0, 0, 255);
                case 10: return new Color32(106, 90, 205, 255);
                case 12: return new Color32(189, 183, 107, 255);
                case 18: return new Color32(173, 255, 47, 255);
                default: return fallbackColor;
            }
        }

        public static void HandleTracer(PlayerControl target, bool enable)
        {
            try
            {
                if (target == null || target.gameObject == null) return;

                LineRenderer lr = target.GetComponent<LineRenderer>();

                if (!enable || PlayerControl.LocalPlayer == null || target == PlayerControl.LocalPlayer || target.Data == null || target.Data.Disconnected)
                {
                    if (lr != null) lr.enabled = false;
                    return;
                }

                if (target.Data.IsDead && !seeGhosts && !PlayerControl.LocalPlayer.Data.IsDead)
                {
                    if (lr != null) lr.enabled = false;
                    return;
                }

                if (lr == null)
                {
                    lr = target.gameObject.AddComponent<LineRenderer>();
                    lr.SetVertexCount(2);
                    lr.SetWidth(0.02f, 0.02f);
                    try { if (HatManager.Instance != null) lr.material = HatManager.Instance.PlayerMaterial; } catch { }
                }

                lr.enabled = true;

                Color tColor = Color.white;
                try
                {
                    if (target.Data.IsDead)
                    {
                        tColor = Color.gray;
                    }
                    else if (target.Data.Role != null)
                    {
                        tColor = GetRoleColor((int)target.Data.Role.Role, target.Data.Role.TeamColor);
                    }
                }
                catch { }

                lr.SetColors(tColor, tColor);

                lr.SetPosition(0, PlayerControl.LocalPlayer.transform.position);
                lr.SetPosition(1, target.transform.position);
            }
            catch { }
        }


        private void DrawLobbyControls()
        {
            GUILayout.BeginVertical(boxStyle);
            GUILayout.Label("LOBBY CONTROLS", headerStyle);

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(GUILayout.Width(280));
            neverEndGame = DrawToggle(neverEndGame, "Unlimited Game", 250);
            GUILayout.Space(5);
            noSettingLimit = DrawToggle(noSettingLimit, "No Setting Limit", 250);
            GUILayout.Space(5);
            noTaskMode = DrawToggle(noTaskMode, "No Task Mode", 250);
            GUILayout.Space(5);
            enableColorCommand = DrawToggle(enableColorCommand, "Enable /c command (Public)", 250);
            GUILayout.Space(5);
            blockFortegreenChat = DrawToggle(blockFortegreenChat, "Block Fortegreen Chat", 250);
            GUILayout.Space(5);
            blockRainbowChat = DrawToggle(blockRainbowChat, "Block Rainbow Chat", 250);
            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(15);
            GUILayout.Label("HOST ACTIONS", headerStyle);

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(GUILayout.Width(280));
            if (GUILayout.Button("Insta Start", btnStyle, GUILayout.Height(25)))
            { GameStartManager.Instance.startState = GameStartManager.StartingStates.Countdown; GameStartManager.Instance.countDownTimer = 0f; }
            GUILayout.Space(5);
            if (GUILayout.Button("Close Meeting", btnStyle, GUILayout.Height(25))) MeetingHud.Instance.RpcClose();
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Spawn Lobby", activeTabStyle, GUILayout.Height(25))) SpawnLobby();
            GUILayout.Space(5);
            if (GUILayout.Button("Despawn", btnStyle, GUILayout.Height(25))) DespawnLobby();
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Kill All", btnStyle, GUILayout.Height(25))) KillAll();
            GUILayout.Space(5);
            if (GUILayout.Button("Kick All", btnStyle, GUILayout.Height(25))) KickAll();
            GUILayout.Space(5);
            if (GUILayout.Button("Mass Morph", btnStyle, GUILayout.Height(25))) this.StartCoroutine(MassMorphCoroutine().WrapToIl2Cpp());
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.Space(10);

            GUILayout.BeginVertical(GUILayout.Width(280));
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Crewmate Win", btnStyle, GUILayout.Height(25))) SmartEndGame("CrewWin");
            GUILayout.Space(5);
            if (GUILayout.Button("Impostor Win", btnStyle, GUILayout.Height(25))) SmartEndGame("ImpWin");
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Imp Disconnect", btnStyle, GUILayout.Height(25))) SmartEndGame("ImpDisconnect");
            GUILayout.Space(5);
            if (GUILayout.Button("H&S Disconnect", activeTabStyle, GUILayout.Height(25))) SmartEndGame("HnsImpDisconnect");
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            if (GUILayout.Button("Force End (Impostor Disconnect)", btnStyle, GUILayout.Height(25)) && GameManager.Instance != null && AmongUsClient.Instance.AmHost)
            { bool tempNeverEnd = neverEndGame; neverEndGame = false; GameManager.Instance.RpcEndGame((GameOverReason)4, false); neverEndGame = tempNeverEnd; }
            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }
        public static string GetESPNameTag(NetworkedPlayerInfo info, string originalName)
        {
            if (info == null) return originalName;
            string newName = originalName;
            if (seeRoles && info.Role != null)
            {
                string roleName = info.Role.Role.ToString();
                int roleId = (int)info.Role.Role;
                if (roleId == 8) roleName = "Noisemaker";
                else if (roleId == 9) roleName = "Phantom";
                else if (roleId == 10) roleName = "Tracker";
                else if (roleId == 12) roleName = "Detective";
                else if (roleId == 18) roleName = "Viper";
                else if (roleName == "GuardianAngel") roleName = "Guardian Angel";
                Color customColor = GetRoleColor(roleId, info.Role.TeamColor);
                string roleColor = ColorUtility.ToHtmlStringRGB(customColor);
                newName = $"<color=#{roleColor}>{roleName}</color>\n{newName}";
            }
            if (showPlayerInfo)
            {
                int level = 0; string platform = "Unknown"; string hostStr = "";
                try { level = (int)info.PlayerLevel + 1; } catch { }
                try
                {
                    var client = AmongUsClient.Instance.GetClientFromPlayerInfo(info);
                    if (client != null) { platform = GetPlatform(client); if (AmongUsClient.Instance.GetHost() == client) hostStr = "Host - "; }
                }
                catch { }

                string accentHex = ColorUtility.ToHtmlStringRGB(currentAccentColor);
                newName = $"<size=80%><color=#{accentHex}>{hostStr}Lv:{level} - {platform}</color></size>\n{newName}";
            }
            return newName;
        }


        [HarmonyPatch(typeof(VersionShower), nameof(VersionShower.Start))]
    public static class VersionShower_Start_Patch
    {
        public static void Postfix(VersionShower __instance) { if (__instance != null && __instance.text != null) __instance.text.text = NjordMenuGUI.ApplyMenuShimmer("NjordMenu Meowchelo & Carrot"); }
    }

        [HarmonyPatch(typeof(PingTracker), nameof(PingTracker.Update))]
        public static class PingTracker_Watermark_Patch
        {
            private static float _smoothFps = 0f;
            private static int _smoothPing = 0;
            private static float _updateTimer = 0f;
            public static void Postfix(PingTracker __instance)
            {
                try
                {
                    _updateTimer += Time.deltaTime;
                    if (_updateTimer >= 0.5f) { _smoothFps = 1f / Time.deltaTime; if (AmongUsClient.Instance != null) _smoothPing = AmongUsClient.Instance.Ping; _updateTimer = 0f; }
                    int num = Mathf.RoundToInt(_smoothFps);
                    string pingColor = ((_smoothPing < 80) ? "#00FF00" : ((_smoothPing < 400) ? "#FFFF00" : "#FF0000"));

                    string finalString = $"<color=#FFFFFF>PING:</color> <color={pingColor}>{_smoothPing} ms</color> • <color=#FFFFFF>FPS:</color> <color=#FFFFFF>{num}</color>";

                    if (NjordMenuGUI.showWatermark)
                    {
                        string shimmerTitle = NjordMenuGUI.ApplyMenuShimmer("NjordMenu v1.2.3");
                        finalString = $"{shimmerTitle} • " + finalString;
                    }

                    if (AmongUsClient.Instance != null)
                    {
                        ClientData host = AmongUsClient.Instance.GetHost();
                        if (host != null && host.Character != null)
                        {
                            string hostName = host.Character.Data.PlayerName ?? "Unknown";
                            string shimmerHostName = NjordMenuGUI.ApplyMenuShimmer(hostName);
                            finalString += $" • <color=#FFFFFF>Host:</color> {shimmerHostName}";
                            if (AmongUsClient.Instance.AmHost) finalString += " <color=#00FF00>(You)</color>";
                        }
                    }
                    __instance.text.text = finalString;
                    __instance.text.alignment = TMPro.TextAlignmentOptions.Center;
                    __instance.aspectPosition.enabled = false;
                    float zPos = MeetingHud.Instance != null && MeetingHud.Instance.gameObject.activeInHierarchy ? -100f : -10f;
                    __instance.transform.localPosition = new Vector3(0f, -2.3f, zPos);
                }
                catch { }
            }
        }

        [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Update))]
    public static class GameStartManager_Update_Patch
    {
        public static void Postfix(GameStartManager __instance)
        {
            if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost || PlayerControl.LocalPlayer == null) return;
            if (NjordMenuGUI.fakeStartCounterTroll)
            {
                try { sbyte[] arr = { -123, -111, -100, -69, -67, -52, -42, 0, 42, 52, 67, 69, 100, 111, 123 }; sbyte b = arr[UnityEngine.Random.Range(0, arr.Length)]; PlayerControl.LocalPlayer.RpcSetStartCounter(b); __instance.SetStartCounter(b); } catch { }
            }
            else if (NjordMenuGUI.fakeStartCounterCustom && int.TryParse(NjordMenuGUI.fakeStartInput, out int custom))
            {
                try { PlayerControl.LocalPlayer.RpcSetStartCounter(custom); __instance.SetStartCounter((sbyte)Mathf.Clamp(custom, -128, 127)); } catch { }
            }
        }
    }

    [HarmonyPatch(typeof(GameManager), nameof(GameManager.RpcEndGame))]
    public static class InfiniteGamePatch { public static bool Prefix() { try { if (NjordMenuGUI.neverEndGame && AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost) return false; } catch { } return true; } }

    [HarmonyPatch(typeof(IntroCutscene), "CoBegin")]
    public static class IntroCutscene_CoBegin_Patch
    {
        public static void Prefix()
        {
            if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return;
            if (NjordMenuGUI.enablePreGameRoleForce)
            {
                foreach (var kvp in NjordMenuGUI.forcedPreGameRoles)
                { var target = GameData.Instance.GetPlayerById(kvp.Key)?.Object; if (target != null && target.Data.RoleType != kvp.Value) target.RpcSetRole(kvp.Value); }
                foreach (byte impId in NjordMenuGUI.forcedImpostors)
                { var target = GameData.Instance.GetPlayerById(impId)?.Object; if (target != null && target.Data.Role != null && !target.Data.Role.IsImpostor) target.RpcSetRole(RoleTypes.Impostor); }
            }
        }
    }

    [HarmonyPatch(typeof(LogicRoleSelectionNormal), "AssignRolesForTeam")]
    public static class RoleSelectionNormal_Patch
    {
        public static bool Prefix(Il2CppSystem.Collections.Generic.List<NetworkedPlayerInfo> players, IGameOptions opts, RoleTeamTypes team, ref int teamMax)
        {
            if (!NjordMenuGUI.enablePreGameRoleForce || !AmongUsClient.Instance.AmHost) return true;
            try
            {
                if ((int)team == 1)
                {
                    int numImps = opts.GetInt((Int32OptionNames)1);
                    var impRoleTypes = new HashSet<int> { 1, 5, 9, 18 };
                    List<byte> allForced = new List<byte>(NjordMenuGUI.forcedImpostors);
                    foreach (var kvp in NjordMenuGUI.forcedPreGameRoles) if (impRoleTypes.Contains((int)kvp.Value) && !allForced.Contains(kvp.Key)) allForced.Add(kvp.Key);
                    if (allForced.Count > 0) numImps = allForced.Count;
                    else { if (numImps >= players.Count) numImps = players.Count - 1; if (numImps < 1) numImps = 1; }
                    int assigned = 0;
                    foreach (byte impId in allForced)
                    {
                        if (players.Count == 0 || assigned >= numImps) break;
                        var targetInfo = players.ToArray().FirstOrDefault(p => p.PlayerId == impId);
                        if (targetInfo != null && targetInfo.Object != null)
                        {
                            RoleTypes role = NjordMenuGUI.forcedPreGameRoles.ContainsKey(impId) ? NjordMenuGUI.forcedPreGameRoles[impId] : RoleTypes.Impostor;
                            targetInfo.Object.RpcSetRole(role, false);
                            players.Remove(targetInfo);
                            assigned++;
                        }
                    }
                    while (assigned < numImps && players.Count > 0)
                    {
                        int idx = UnityEngine.Random.Range(0, players.Count);
                        players[idx].Object.RpcSetRole(RoleTypes.Impostor, false);
                        players.RemoveAt(idx);
                        assigned++;
                    }
                    return false;
                }
                else if ((int)team == 0)
                {
                    var crewRoleTypes = new HashSet<int> { 0, 2, 3, 4, 8, 10, 12 };
                    for (int i = players.Count - 1; i >= 0; i--)
                    {
                        var p = players[i];
                        if (p != null && p.Object != null)
                        {
                            RoleTypes role = RoleTypes.Crewmate;
                            if (NjordMenuGUI.forcedPreGameRoles.ContainsKey(p.PlayerId) && crewRoleTypes.Contains((int)NjordMenuGUI.forcedPreGameRoles[p.PlayerId]))
                                role = NjordMenuGUI.forcedPreGameRoles[p.PlayerId];
                            p.Object.RpcSetRole(role, false);
                            players.RemoveAt(i);
                        }
                    }
                    return false;
                }
                return true;
            }
            catch { return true; }
        }
    }

    [HarmonyPatch(typeof(LogicRoleSelectionHnS), "AssignRolesForTeam")]
    public static class RoleSelectionHnS_Patch
    {
        public static bool Prefix(Il2CppSystem.Collections.Generic.List<NetworkedPlayerInfo> players, IGameOptions opts, RoleTeamTypes team, ref int teamMax)
        {
            if (!NjordMenuGUI.enablePreGameRoleForce || !AmongUsClient.Instance.AmHost) return true;
            if ((int)team != 1) return true;
            try
            {
                int numImps = opts.GetInt((Int32OptionNames)1);
                var impRoleTypes = new HashSet<int> { 1, 5, 9, 18 };
                List<byte> allForced = new List<byte>(NjordMenuGUI.forcedImpostors);
                foreach (var kvp in NjordMenuGUI.forcedPreGameRoles) if (impRoleTypes.Contains((int)kvp.Value) && !allForced.Contains(kvp.Key)) allForced.Add(kvp.Key);
                if (allForced.Count > 0) numImps = allForced.Count;
                else { if (numImps >= players.Count) numImps = players.Count - 1; if (numImps < 1) numImps = 1; }
                int assigned = 0;
                foreach (byte impId in allForced)
                {
                    if (players.Count == 0 || assigned >= numImps) break;
                    var targetInfo = players.ToArray().FirstOrDefault(p => p.PlayerId == impId);
                    if (targetInfo != null) { targetInfo.Object.RpcSetRole((RoleTypes)1, false); players.Remove(targetInfo); assigned++; }
                }
                while (assigned < numImps && players.Count > 0)
                {
                    int idx = UnityEngine.Random.Range(0, players.Count);
                    players[idx].Object.RpcSetRole((RoleTypes)1, false);
                    players.RemoveAt(idx);
                    assigned++;
                }
                return false;
            }
            catch { return true; }
        }
    }

    [HarmonyPatch(typeof(RoleManager), nameof(RoleManager.SelectRoles))]
    public static class RoleManager_SelectRoles_Patch
    {
        public static bool Prefix(RoleManager __instance)
        {
            if (!NjordMenuGUI.enablePreGameRoleForce || !AmongUsClient.Instance.AmHost) return true;
            try
            {
                var allPlayers = PlayerControl.AllPlayerControls.ToArray().Where(p => p != null && p.Data != null && !p.Data.Disconnected && !p.Data.IsDead).ToList();
                int numImps = 1;
                try { numImps = GameOptionsManager.Instance.CurrentGameOptions.GetInt((Int32OptionNames)1); } catch { }
                var impRoleTypes = new HashSet<int> { 1, 5, 9, 18 };
                List<PlayerControl> impostors = new List<PlayerControl>();
                foreach (var p in allPlayers)
                    if (NjordMenuGUI.forcedImpostors.Contains(p.PlayerId) || (NjordMenuGUI.forcedPreGameRoles.ContainsKey(p.PlayerId) && impRoleTypes.Contains((int)NjordMenuGUI.forcedPreGameRoles[p.PlayerId])))
                        impostors.Add(p);
                if (impostors.Count > 0) numImps = impostors.Count;
                else { if (numImps >= allPlayers.Count) numImps = allPlayers.Count - 1; if (numImps < 1) numImps = 1; }
                System.Random rand = new System.Random();
                while (impostors.Count < numImps && allPlayers.Count > impostors.Count)
                {
                    var available = allPlayers.Where(p => !impostors.Contains(p)).ToList();
                    impostors.Add(available[rand.Next(available.Count)]);
                }
                List<PlayerControl> crewmates = allPlayers.Where(p => !impostors.Contains(p)).ToList();
                var impData = new Il2CppSystem.Collections.Generic.List<NetworkedPlayerInfo>();
                foreach (var i in impostors) impData.Add(i.Data);
                var crewData = new Il2CppSystem.Collections.Generic.List<NetworkedPlayerInfo>();
                foreach (var c in crewmates) crewData.Add(c.Data);
                IGameOptions opts = GameOptionsManager.Instance.CurrentGameOptions;
                GameManager.Instance.LogicRoleSelection.AssignRolesForTeam(impData, opts, (RoleTeamTypes)1, int.MaxValue, new Il2CppSystem.Nullable<RoleTypes>());
                GameManager.Instance.LogicRoleSelection.AssignRolesForTeam(crewData, opts, (RoleTeamTypes)0, int.MaxValue, new Il2CppSystem.Nullable<RoleTypes>((RoleTypes)0));
                foreach (var kvp in NjordMenuGUI.forcedPreGameRoles)
                {
                    if (kvp.Value != RoleTypes.Crewmate && kvp.Value != RoleTypes.Impostor)
                    {
                        var pc = allPlayers.FirstOrDefault(p => p.PlayerId == kvp.Key);
                        if (pc != null) RoleManager.Instance.SetRole(pc, kvp.Value);
                    }
                }
                foreach (var pc in allPlayers) if (pc.Data.Role != null) pc.Data.Role.Initialize(pc);
                return false;
            }
            catch { return true; }
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.TurnOnProtection))]
    public static class PlayerControl_TurnOnProtection_Patch { public static void Prefix(ref bool visible) { if (NjordMenuGUI.seeGhosts) visible = true; } }

    [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.LateUpdate))]
    public static class PlayerVisuals_LateUpdate_Patch
    {
        public static void Postfix(PlayerPhysics __instance)
        {
            if (__instance == null || __instance.myPlayer == null || __instance.myPlayer.Data == null) return;
            try
            {
                if (NjordMenuGUI.seeGhosts && __instance.myPlayer.Data.IsDead && PlayerControl.LocalPlayer != null && !PlayerControl.LocalPlayer.Data.IsDead)
                {
                    __instance.myPlayer.Visible = true;
                    var rend = __instance.myPlayer.GetComponent<SpriteRenderer>();
                    if (rend != null) { Color c = rend.color; rend.color = new Color(c.r, c.g, c.b, 0.4f); }
                }
                var cosmetics = __instance.myPlayer.cosmetics;
                var outfit = __instance.myPlayer.CurrentOutfit;
                if (cosmetics != null && cosmetics.nameText != null && outfit != null)
                {
                    cosmetics.SetName(NjordMenuGUI.GetESPNameTag(__instance.myPlayer.Data, outfit.PlayerName));
                    if (NjordMenuGUI.seeRoles && NjordMenuGUI.showPlayerInfo) cosmetics.nameText.transform.localPosition = new Vector3(0f, 0.186f, 0f);
                    else if (NjordMenuGUI.seeRoles || NjordMenuGUI.showPlayerInfo) cosmetics.nameText.transform.localPosition = new Vector3(0f, 0.093f, 0f);
                    else cosmetics.nameText.transform.localPosition = new Vector3(0f, 0f, 0f);
                }
            }
            catch { }
        }
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Update))]
    public static class ESP_MeetingHud
    {
        public static void Postfix(MeetingHud __instance)
        {
            try
            {
                if (__instance.playerStates == null) return;
                foreach (var state in __instance.playerStates)
                {
                    if (state == null) continue;
                    var data = GameData.Instance.GetPlayerById(state.TargetPlayerId);
                    if (data != null && !data.Disconnected && data.DefaultOutfit != null && state.NameText != null)
                    {
                        string espName = NjordMenuGUI.GetESPNameTag(data, data.DefaultOutfit.PlayerName ?? "???");
                        if (!NjordMenuGUI.seeRoles && NjordMenuGUI.revealMeetingRoles && data.Role != null)
                        {
                            string roleName = data.Role.Role.ToString();
                            int roleId = (int)data.Role.Role;
                            if (roleId == 8) roleName = "Noisemaker";
                            else if (roleId == 9) roleName = "Phantom";
                            else if (roleId == 10) roleName = "Tracker";
                            else if (roleId == 12) roleName = "Detective";
                            else if (roleId == 18) roleName = "Viper";
                            else if (roleName == "GuardianAngel") roleName = "Guardian Angel";
                            Color customColor = NjordMenuGUI.GetRoleColor(roleId, data.Role.TeamColor);
                            string roleColor = ColorUtility.ToHtmlStringRGB(customColor);
                            espName = $"<color=#{roleColor}>{roleName}</color>\n{espName}";
                        }
                        state.NameText.text = espName;
                        bool showingExtra = NjordMenuGUI.seeRoles || NjordMenuGUI.revealMeetingRoles;
                        if (showingExtra && NjordMenuGUI.showPlayerInfo) { state.NameText.transform.localPosition = new Vector3(0.33f, 0.08f, 0f); state.NameText.transform.localScale = new Vector3(0.75f, 0.75f, 0.75f); }
                        else if (showingExtra || NjordMenuGUI.showPlayerInfo) { state.NameText.transform.localPosition = new Vector3(0.3384f, 0.1125f, -0.1f); state.NameText.transform.localScale = new Vector3(0.9f, 1f, 1f); }
                        else { state.NameText.transform.localPosition = new Vector3(0.3384f, 0.0311f, -0.1f); state.NameText.transform.localScale = new Vector3(0.9f, 1f, 1f); }
                    }
                }
            }
            catch { }
        }
    }
        [HarmonyPatch(typeof(ChatBubble), nameof(ChatBubble.SetName))]
        public static class ChatBubble_SetName_Patch
        {
            public static void Postfix(ChatBubble __instance)
            {
                if (!NjordMenuGUI.showPlayerInfo || __instance.playerInfo == null) return;
                try
                {
                    int level = 0; string platform = "Unknown"; string hostStr = "";
                    try { level = (int)__instance.playerInfo.PlayerLevel + 1; } catch { }
                    try
                    {
                        var client = AmongUsClient.Instance.GetClientFromPlayerInfo(__instance.playerInfo);
                        if (client != null) { platform = NjordMenuGUI.GetPlatform(client); if (AmongUsClient.Instance.GetHost() == client) hostStr = "Host - "; }
                    }
                    catch { }

                    string accentHex = ColorUtility.ToHtmlStringRGB(NjordMenuGUI.currentAccentColor);
                    string extra = $" <color=#{accentHex}><size=80%>{hostStr}Lv:{level} - {platform}</size></color>";

                    if (!__instance.NameText.text.Contains("Lv:")) __instance.NameText.text += extra;
                }
                catch { }
            }
        }

   [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    public static class FullBright_Patch
    {
        public static void Postfix(HudManager __instance) { if (__instance.ShadowQuad != null) __instance.ShadowQuad.gameObject.SetActive(!NjordMenuGUI.fullBright); }
    }

    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    public static class HudManager_Update_Patch
    {
        public static void Postfix(HudManager __instance)
        {
            try
            {
                if (NjordMenuGUI.alwaysChat && __instance.Chat != null)
                    __instance.Chat.gameObject.SetActive(true);
            }
            catch { }
        }
    }

    [HarmonyPatch(typeof(PlatformSpecificData), nameof(PlatformSpecificData.Serialize))]
    public static class PlatformSpooferPatch { public static void Prefix(PlatformSpecificData __instance) { try { if (NjordMenuGUI.enablePlatformSpoof && __instance != null) __instance.Platform = NjordMenuGUI.platformValues[NjordMenuGUI.currentPlatformIndex]; } catch { } } }

    [HarmonyPatch(typeof(FullAccount), nameof(FullAccount.CanSetCustomName))]
    public static class FullAccount_CanSetCustomName_Patch { public static void Prefix(ref bool canSetName) { try { if (NjordMenuGUI.unlockFeatures) canSetName = true; } catch { } } }

    [HarmonyPatch(typeof(AccountManager), nameof(AccountManager.CanPlayOnline))]
    public static class AccountManager_CanPlayOnline_Patch { public static void Postfix(ref bool __result) { try { if (NjordMenuGUI.unlockFeatures) __result = true; } catch { } } }

    [HarmonyPatch(typeof(EngineerRole), "FixedUpdate")]
    public static class EngineerCheatsPatch
    {
        public static void Postfix(EngineerRole __instance)
        {
            if (__instance.Player != PlayerControl.LocalPlayer) return;
            if (NjordMenuGUI.endlessVentTime) __instance.inVentTimeRemaining = float.MaxValue;
            if (NjordMenuGUI.noVentCooldown && __instance.cooldownSecondsRemaining > 0f)
            {
                __instance.cooldownSecondsRemaining = 0f;
                var btn = DestroyableSingleton<HudManager>.Instance?.AbilityButton;
                if (btn != null) { btn.ResetCoolDown(); btn.SetCooldownFill(0f); }
            }
        }
    }

    [HarmonyPatch(typeof(ScientistRole), "Update")]
    public static class ScientistCheatsPatch
    {
        public static void Postfix(ScientistRole __instance)
        {
            if (__instance.Player != PlayerControl.LocalPlayer) return;
            if (NjordMenuGUI.noVitalsCooldown) __instance.currentCooldown = 0f;
            if (NjordMenuGUI.endlessBattery) __instance.currentCharge = float.MaxValue;
        }
    }

    [HarmonyPatch(typeof(ShapeshifterRole), "FixedUpdate")]
    public static class ShapeshifterDurationPatch
    {
        public static void Postfix(ShapeshifterRole __instance) { if (__instance.Player == PlayerControl.LocalPlayer && NjordMenuGUI.endlessSsDuration) __instance.durationSecondsRemaining = float.MaxValue; }
    }

    [HarmonyPatch(typeof(ImpostorRole), "FindClosestTarget")]
    public static class ImpostorRangePatch
    {
        public static bool Prefix(ImpostorRole __instance, ref PlayerControl __result)
        {
            if (!NjordMenuGUI.killReach) return true;
            try
            {
                var target = PlayerControl.AllPlayerControls.ToArray()
                    .Where(p => p != null && __instance.IsValidTarget(p.Data) && !p.Data.IsDead && !p.Data.Disconnected)
                    .OrderBy(p => Vector2.Distance(p.transform.position, PlayerControl.LocalPlayer.transform.position))
                    .FirstOrDefault();
                if (target != null) __result = target;
                return false;
            }
            catch { return true; }
        }
    }

    [HarmonyPatch(typeof(ImpostorRole), "IsValidTarget")]
    public static class ImpostorKillAnyonePatch
    {
        public static void Postfix(NetworkedPlayerInfo target, ref bool __result) { try { if (NjordMenuGUI.killAnyone && target != null && target.PlayerId != PlayerControl.LocalPlayer.PlayerId && !target.IsDead) __result = true; } catch { } }
    }



    [HarmonyPatch(typeof(DetectiveRole), "FindClosestTarget")]
    public static class DetectiveRangePatch
    {
        public static bool Prefix(DetectiveRole __instance, ref PlayerControl __result)
        {
            if (!NjordMenuGUI.UnlimitedInterrogateRange) return true;
            try
            {
                var target = PlayerControl.AllPlayerControls.ToArray()
                    .Where(p => p != null && __instance.IsValidTarget(p.Data) && !p.Data.IsDead && !p.Data.Disconnected)
                    .OrderBy(p => Vector2.Distance(p.transform.position, PlayerControl.LocalPlayer.transform.position))
                    .FirstOrDefault();
                if (target != null) __result = target;
                return false;
            }
            catch { return true; }
        }
    }

    [HarmonyPatch(typeof(DoorBreakerGame), nameof(DoorBreakerGame.Start))]
    public static class DoorBreakerGame_Start_Patch
    {
        public static bool Prefix(DoorBreakerGame __instance)
        {
            if (!NjordMenuGUI.autoOpenDoors) return true;
            try { ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Doors, (byte)(__instance.MyDoor.Id | 64)); } catch { }
            __instance.MyDoor.SetDoorway(true); __instance.Close();
            return false;
        }
    }
    [HarmonyPatch(typeof(DoorCardSwipeGame), nameof(DoorCardSwipeGame.Begin))]
    public static class DoorCardSwipeGame_Begin_Patch
    {
        public static bool Prefix(DoorCardSwipeGame __instance)
        {
            if (!NjordMenuGUI.autoOpenDoors) return true;
            try { ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Doors, (byte)(__instance.MyDoor.Id | 64)); } catch { }
            __instance.MyDoor.SetDoorway(true); __instance.Close();
            return false;
        }
    }
    [HarmonyPatch(typeof(MushroomDoorSabotageMinigame), nameof(MushroomDoorSabotageMinigame.Begin))]
    public static class MushroomDoorSabotageMinigame_Begin_Patch
    {
        public static bool Prefix(MushroomDoorSabotageMinigame __instance) { if (NjordMenuGUI.autoOpenDoors) { __instance.FixDoorAndCloseMinigame(); return false; } return true; }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.SetTasks))]
    public static class NoTaskMode_Patch { public static bool Prefix(PlayerControl __instance) { if (NjordMenuGUI.noTaskMode) return false; return true; } }
    [HarmonyPatch(typeof(ChatController), nameof(ChatController.SendChat))]
    public static class ChatController_SendChat_Patch
    {
        public static bool Prefix(ChatController __instance)
        {
            if (__instance.freeChatField == null || __instance.freeChatField.textArea == null) return true;
            string text = __instance.freeChatField.textArea.text;
            if (string.IsNullOrWhiteSpace(text)) return true;

            string lowerChat = text.ToLower().Trim();

            if (NjordMenuGUI.enableColorCommand)
            {
                if (lowerChat == "/rainbow" || lowerChat == "!rainbow" || lowerChat == "/lgbt" || lowerChat == "!lgbt")
                {
                    if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
                    {
                        if (NjordMenuGUI.rainbowPlayers.Contains(PlayerControl.LocalPlayer.PlayerId))
                        {
                            NjordMenuGUI.rainbowPlayers.Remove(PlayerControl.LocalPlayer.PlayerId);
                            NjordMenuGUI.ShowNotification("<color=#FF00FF>[SERVER]</color> Ваша радуга ВЫКЛ.");
                        }
                        else
                        {
                            NjordMenuGUI.rainbowPlayers.Add(PlayerControl.LocalPlayer.PlayerId);
                            NjordMenuGUI.ShowNotification("<color=#FF00FF>[SERVER]</color> Ваша радуга ВКЛ.");
                        }
                    }
                    else
                    {
                        if (HudManager.Instance?.Chat != null)
                            HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, "<color=#FF0000>[ОШИБКА]</color> Эта команда только для Хоста!");
                    }
                    __instance.freeChatField.textArea.SetText("", "");
                    return false;
                }

                if (lowerChat.StartsWith("/color ") || lowerChat.StartsWith("/c ") || lowerChat.StartsWith("/col ") ||
                    lowerChat.StartsWith("!color ") || lowerChat.StartsWith("!c ") || lowerChat.StartsWith("!col "))
                {
                    if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
                    {
                        string arg = lowerChat.Substring(lowerChat.IndexOf(' ') + 1).Trim();
                        int colorId = -1;

                        if (int.TryParse(arg, out int parsed)) colorId = parsed;
                        else colorId = NjordMenuGUI.GetColorIdByName(arg);

                        if (colorId >= 0 && colorId <= 18 && PlayerControl.LocalPlayer != null)
                        {
                            PlayerControl.LocalPlayer.RpcSetColor((byte)colorId);
                        }
                        else if (HudManager.Instance?.Chat != null)
                        {
                            HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, "<color=#FF0000>[ОШИБКА]</color> Используйте ID (0-18) или названия (красн, син, зел...)");
                        }
                    }
                    else
                    {
                        if (HudManager.Instance?.Chat != null)
                            HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, "<color=#FF0000>[ОШИБКА]</color> Смена цвета доступна только Хосту!");
                    }
                    __instance.freeChatField.textArea.SetText("", "");
                    return false;
                }
            }

            if (lowerChat.StartsWith("/w ") || lowerChat.StartsWith("/pm "))
            {
                string[] parts = text.Split(new char[] { ' ' }, 3);
                if (parts.Length >= 3)
                {
                    string targetInput = parts[1].ToLower().Trim();
                    string message = parts[2];
                    PlayerControl target = null;

                    if (byte.TryParse(targetInput, out byte pid))
                    {
                        target = PlayerControl.AllPlayerControls.ToArray().FirstOrDefault(p => p.PlayerId == pid);
                    }

                    if (target == null && PlayerControl.AllPlayerControls != null)
                    {
                        PlayerControl exactMatch = null;
                        PlayerControl partialMatch = null;

                        foreach (var pc in PlayerControl.AllPlayerControls)
                        {
                            if (pc == null || pc.Data == null || pc.Data.Disconnected || pc == PlayerControl.LocalPlayer) continue;

                            string rawName = Regex.Replace(pc.Data.PlayerName, "<.*?>", string.Empty).ToLower().Trim();
                            int cId = (int)pc.Data.DefaultOutfit.ColorId;
                            int targetColorId = NjordMenuGUI.GetColorIdByName(targetInput);

                            if (rawName == targetInput || (targetColorId != -1 && cId == targetColorId))
                            {
                                exactMatch = pc;
                                break;
                            }
                            if (rawName.StartsWith(targetInput))
                            {
                                if (partialMatch == null) partialMatch = pc;
                            }
                        }
                        target = exactMatch ?? partialMatch;
                    }

                    if (target != null && target != PlayerControl.LocalPlayer)
                    {
                        string safeMessage = Regex.Replace(message, "<.*?>", string.Empty).Replace("<", "").Replace(">", "");
                        string networkMsg = $"шепчет вам:\n{safeMessage}";

                        if (AmongUsClient.Instance != null && PlayerControl.LocalPlayer != null)
                        {
                            MessageWriter msgWriter = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, 13, Hazel.SendOption.Reliable, target.OwnerId);
                            msgWriter.Write(networkMsg);
                            AmongUsClient.Instance.FinishRpcImmediately(msgWriter);
                        }

                        string targetClean = Regex.Replace(target.Data.PlayerName, "<.*?>", string.Empty);
                        if (HudManager.Instance?.Chat != null)
                            HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, $"<color=#FFAC1C>Вы шепчете {targetClean}:\n{safeMessage}</color>");
                    }
                    else if (HudManager.Instance?.Chat != null)
                    {
                        HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, "<color=#FF0000>[ОШИБКА]</color> Игрок не найден! Введите ID, Цвет или Имя.");
                    }
                }
                __instance.freeChatField.textArea.SetText("", "");
                return false;
            }

            return true;
        }
    }
    [HarmonyPatch(typeof(ChatController), nameof(ChatController.AddChat))]
    public static class ChatController_AddChat_Patch
    {
        public static bool Prefix(PlayerControl sourcePlayer, ref string chatText, ChatController __instance)
        {
            if (string.IsNullOrEmpty(chatText)) return true;
            string lowerText = chatText.ToLower().Trim();

            if (NjordMenuGUI.enableColorCommand && sourcePlayer != null)
            {
                string[] colorCommands = { "/color ", "!color ", "/col ", "!col ", "/c ", "!c " };
                string usedCmd = colorCommands.FirstOrDefault(cmd => lowerText.StartsWith(cmd));

                if (usedCmd != null)
                {
                    if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
                    {
                        string colorInput = lowerText.Substring(usedCmd.Length).Trim();
                        int colorId = -1;

                        if (int.TryParse(colorInput, out int parsedId)) { if (parsedId >= 0 && parsedId <= 18) colorId = parsedId; }
                        else colorId = NjordMenuGUI.GetColorIdByName(colorInput);

                        if (colorId != -1)
                        {
                            if (colorId == 18 && NjordMenuGUI.blockFortegreenChat)
                            {
                                if (HudManager.Instance?.Chat != null)
                                    HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, "<color=#FF0000>[ОШИБКА]</color> Цвет Fortegreen запрещен хостом!");
                            }
                            else
                            {
                                sourcePlayer.RpcSetColor((byte)colorId);
                            }
                        }
                        else if (sourcePlayer == PlayerControl.LocalPlayer)
                        {
                            __instance.AddChat(PlayerControl.LocalPlayer, "<color=#FF0000>[ОШИБКА]</color> Неверный цвет.");
                        }
                    }
                    return false;
                }

                if (lowerText == "/rainbow" || lowerText == "!rainbow" || lowerText == "/lgbt" || lowerText == "!lgbt")
                {
                    if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
                    {
                        if (NjordMenuGUI.blockRainbowChat)
                        {
                            if (HudManager.Instance?.Chat != null)
                                HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, "<color=#FF0000>[ОШИБКА]</color> Радуга запрещена хостом!");
                        }
                        else
                        {
                            if (NjordMenuGUI.rainbowPlayers.Contains(sourcePlayer.PlayerId))
                            {
                                NjordMenuGUI.rainbowPlayers.Remove(sourcePlayer.PlayerId);
                                NjordMenuGUI.ShowNotification("<color=#FF00FF>[SERVER]</color> Радуга ВЫКЛ.");
                            }
                            else
                            {
                                NjordMenuGUI.rainbowPlayers.Add(sourcePlayer.PlayerId);
                                NjordMenuGUI.ShowNotification("<color=#FF00FF>[SERVER]</color> Радуга ВКЛ.");
                            }
                        }
                    }
                    return false;
                }
            }
            return true;
        }



        public static void Postfix(GameStartManager __instance)
        {
            if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost || PlayerControl.LocalPlayer == null) return;
            if (NjordMenuGUI.customStartTimer > 0f) return;

            if (NjordMenuGUI.fakeStartCounterTroll)
            {
                try
                {
                    sbyte[] arr = { -123, -100, -69, -42, 0, 42, 69, 100, 123 };
                    sbyte b = arr[UnityEngine.Random.Range(0, arr.Length)];
                    PlayerControl.LocalPlayer.RpcSetStartCounter((int)b);
                    __instance.SetStartCounter(b);
                }
                catch { }
            }
            else if (NjordMenuGUI.fakeStartCounterCustom && int.TryParse(NjordMenuGUI.fakeStartInput, out int custom))
            {
                try
                {
                    PlayerControl.LocalPlayer.RpcSetStartCounter(custom);
                    __instance.SetStartCounter((sbyte)Mathf.Clamp(custom, -128, 127));
                }
                catch { }
            }
        }
    }
}


[HarmonyPatch(typeof(ChatController), nameof(ChatController.Update))]
public static class ChatController_Update_Patch
{
    public static void Postfix(ChatController __instance)
    {
        try
        {
            if (__instance.freeChatField != null && __instance.freeChatField.background != null)
            {
                __instance.freeChatField.background.color = new Color32(40, 40, 40, byte.MaxValue);
                if (__instance.freeChatField.textArea != null && __instance.freeChatField.textArea.outputText != null)
                    __instance.freeChatField.textArea.outputText.color = Color.white;
            }
            if (__instance.quickChatField != null && __instance.quickChatField.background != null)
            {
                __instance.quickChatField.background.color = new Color32(40, 40, 40, byte.MaxValue);
                if (__instance.quickChatField.text != null)
                    __instance.quickChatField.text.color = Color.white;
            }
        }
        catch { }
    }
}

[HarmonyPatch(typeof(ChatBubble), nameof(ChatBubble.SetText))]
public static class DarkMode_ChatBubblePatch
{
    public static void Postfix(ChatBubble __instance)
    {
        try
        {
            Transform bg = __instance.transform.Find("Background");
            if (bg != null)
            {
                var sr = bg.GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = new Color32(0, 0, 0, 128);
            }
            if (__instance.TextArea != null)
                __instance.TextArea.color = Color.white;
        }
        catch { }
    }
}

[HarmonyPatch(typeof(GameManager), nameof(GameManager.CheckTaskCompletion))]
public static class GameManager_CheckTaskCompletion_Patch
{
    public static bool Prefix(ref bool __result)
    {
        try
        {
            if (!NjordMenuGUI.neverEndGame) return true;
            __result = false; return false;
        }
        catch { return true; }
    }
}

[HarmonyPatch(typeof(ChatController), nameof(ChatController.SetVisible))]
public static class ChatController_SetVisible_Patch
{
    public static void Prefix(ref bool visible)
    {
        if (NjordMenuGUI.alwaysChat) visible = true;
    }
}

[HarmonyPatch(typeof(MeetingHud), "Update")]
public static class RevealVotesPatch
{
    internal static List<int> _votedPlayers = new List<int>();
    public static void Prefix(MeetingHud __instance)
    {
        if (!NjordMenuGUI.RevealVotesEnabled) return;
        try
        {
            if ((int)__instance.state >= 4) return;
            foreach (var item in __instance.playerStates)
            {
                if (item == null) continue;
                var playerById = GameData.Instance.GetPlayerById(item.TargetPlayerId);
                if (playerById == null || playerById.Disconnected || item.VotedFor == PlayerVoteArea.HasNotVoted ||
                    item.VotedFor == PlayerVoteArea.MissedVote || item.VotedFor == PlayerVoteArea.DeadVote || _votedPlayers.Contains(item.TargetPlayerId)) continue;
                _votedPlayers.Add(item.TargetPlayerId);
                if (item.VotedFor != PlayerVoteArea.SkippedVote)
                {
                    foreach (var item2 in __instance.playerStates) if (item2.TargetPlayerId == item.VotedFor) { __instance.BloopAVoteIcon(playerById, 0, item2.transform); break; }
                }
                else if (__instance.SkippedVoting != null) __instance.BloopAVoteIcon(playerById, 0, __instance.SkippedVoting.transform);
            }
            foreach (var item3 in __instance.playerStates)
            {
                if (item3 == null) continue;
                var component = item3.transform.GetComponent<VoteSpreader>();
                if (component != null) foreach (var sprite in component.Votes) sprite.gameObject.SetActive(true);
            }
            if (__instance.SkippedVoting != null) __instance.SkippedVoting.SetActive(true);
        }
        catch { }
    }
}
[HarmonyPatch(typeof(MeetingHud), "PopulateResults")]
public static class RevealVotesCleanupPatch
{
    public static void Prefix(MeetingHud __instance)
    {
        if (!NjordMenuGUI.RevealVotesEnabled) return;
        try
        {
            foreach (var item in __instance.playerStates)
            {
                if (item == null) continue;
                var component = item.transform.GetComponent<VoteSpreader>();
                if (component != null && component.Votes.Count != 0)
                {
                    foreach (var sprite in component.Votes) Object.DestroyImmediate(sprite.gameObject);
                    component.Votes.Clear();
                }
            }
            RevealVotesPatch._votedPlayers.Clear();
        }
        catch { }
    }
}

[HarmonyPatch(typeof(NumberOption), nameof(NumberOption.Increase))]
public static class NumberOption_Increase_Patch
{
    public static bool Prefix(NumberOption __instance)
    {
        try
        {
            if (!NjordMenuGUI.noSettingLimit) return true;
            if (GameOptionsManager.Instance.CurrentGameOptions.GameMode != GameModes.HideNSeek &&
                (__instance.Title == StringNames.GameNumImpostors || __instance.Title == StringNames.GamePlayerSpeed))
                return true;
            __instance.Value += __instance.Increment;
            __instance.UpdateValue();
            __instance.OnValueChanged.Invoke(__instance);
            __instance.AdjustButtonsActiveState();
            return false;
        }
        catch { return true; }
    }
}

[HarmonyPatch(typeof(NumberOption), nameof(NumberOption.Decrease))]
public static class NumberOption_Decrease_Patch
{
    public static bool Prefix(NumberOption __instance)
    {
        try
        {
            if (!NjordMenuGUI.noSettingLimit) return true;
            if (GameOptionsManager.Instance.CurrentGameOptions.GameMode != GameModes.HideNSeek &&
                (__instance.Title == StringNames.GameNumImpostors || __instance.Title == StringNames.GamePlayerSpeed))
                return true;
            __instance.Value -= __instance.Increment;
            __instance.UpdateValue();
            __instance.OnValueChanged.Invoke(__instance);
            __instance.AdjustButtonsActiveState();
            return false;
        }
        catch { return true; }
    }
}

[HarmonyPatch(typeof(NumberOption), nameof(NumberOption.Initialize))]
public static class NumberOption_Initialize_Patch
{
    public static void Postfix(NumberOption __instance)
    {
        try
        {
            if (!NjordMenuGUI.noSettingLimit) return;
            if (GameOptionsManager.Instance.CurrentGameOptions.GameMode != GameModes.HideNSeek &&
                (__instance.Title == StringNames.GameNumImpostors || __instance.Title == StringNames.GamePlayerSpeed))
                return;
            __instance.ValidRange = new FloatRange(-999f, 999f);
        }
        catch { }
    }
}

[HarmonyPatch(typeof(IGameOptionsExtensions), nameof(IGameOptionsExtensions.GetAdjustedNumImpostors))]
public static class IGameOptionsExtensions_GetAdjustedNumImpostors_Patch
{
    public static bool Prefix(IGameOptions __instance, ref int __result)
    {
        try
        {
            if (!NjordMenuGUI.noSettingLimit) return true;
            __result = GameOptionsManager.Instance.CurrentGameOptions.NumImpostors;
            return false;
        }
        catch { return true; }
    }
}

[HarmonyPatch(typeof(FindAGameManager), nameof(FindAGameManager.Start))]
public static class ExtendedLobbyListPatch
{
    public static Scroller scroller;

    public static bool Prefix(FindAGameManager __instance)
    {
        if (!NjordMenuGUI.extendedLobby) return true;
        try
        {
            if (__instance.gameContainers == null || __instance.gameContainers.Count == 0) return true;
            if (__instance.gameContainers.Count > 10) return true;

            GameContainer prefab = __instance.gameContainers[0];
            GameObject holder = new GameObject("ExtendedLobbyScroller");
            holder.transform.SetParent(prefab.transform.parent);

            scroller = holder.AddComponent<Scroller>();
            scroller.Inner = holder.transform;
            scroller.MouseMustBeOverToScroll = true;
            scroller.allowY = true;
            scroller.ScrollWheelSpeed = 0.4f;
            scroller.SetYBoundsMin(0f);
            scroller.SetYBoundsMax(4f);

            BoxCollider2D collider = prefab.transform.parent.gameObject.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(100f, 100f);
            scroller.ClickMask = collider;

            var list = new System.Collections.Generic.List<GameContainer>();
            foreach (var gc in __instance.gameContainers)
            {
                gc.transform.SetParent(holder.transform);
                gc.transform.localPosition = new Vector3(gc.transform.localPosition.x, gc.transform.localPosition.y, 25f);
                list.Add(gc);
            }

            for (int i = 0; i < 15; i++)
            {
                GameContainer newGc = UnityEngine.Object.Instantiate<GameContainer>(prefab, holder.transform);
                newGc.transform.localPosition = new Vector3(newGc.transform.localPosition.x, newGc.transform.localPosition.y - 0.75f * list.Count, 25f);
                list.Add(newGc);
            }

            __instance.gameContainers = new Il2CppReferenceArray<GameContainer>(list.ToArray());
            return true;
        }
        catch { return true; }
    }
}

[HarmonyPatch(typeof(FindAGameManager), nameof(FindAGameManager.RefreshList))]
public static class ExtendedLobbyRefreshPatch
{
    public static void Postfix()
    {
        try { if (NjordMenuGUI.extendedLobby && ExtendedLobbyListPatch.scroller != null) ExtendedLobbyListPatch.scroller.ScrollRelative(new Vector2(0f, -100f)); } catch { }
    }
}


[HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.FixedUpdate))]
public static class InvertControls_Patch
{
    private static void SeePlayerVent(PlayerPhysics player)
    {
#pragma warning disable CS8632
        if (GameManager.Instance.IsHideAndSeek() && player.myPlayer.Data.RoleType == RoleTypes.Impostor || player == null ||
            AmongUsClient.Instance.GameState != InnerNetClient.GameStates.Started)
            return;
        if (!SeePlayersInVent)
        {
            if (player.myPlayer.invisibilityAlpha == 0.3f)
            {
                PhantomRole? role = player.myPlayer.Data.Role as PhantomRole;
                if (role != null)
                {
                    player.myPlayer.SetInvisibility(role.isInvisible);
                    return;
                }
                else
                {
                    player.myPlayer.cosmetics.SetPhantomRoleAlpha(1f);
                    player.myPlayer.invisibilityAlpha = 1;
                    if (player.myPlayer.inVent)
                    {
                        player.myPlayer.Visible = false;
                    }
                }
            }
            return;
        }

        if (player.myPlayer.inVent && player.NetId != PlayerControl.LocalPlayer.MyPhysics.NetId)
        {
            player.myPlayer.Visible = true;
            player.myPlayer.invisibilityAlpha = 0.3f;
            player.myPlayer.cosmetics.SetPhantomRoleAlpha(0.3f);
        }
        else
        {
            PhantomRole? role = player.myPlayer.Data.Role as PhantomRole;
            if (role != null)
            {
                player.myPlayer.SetInvisibility(role.isInvisible);
            }
            player.myPlayer.cosmetics.SetPhantomRoleAlpha(1f);
            player.myPlayer.invisibilityAlpha = 1;
        }
    }

    public static void Postfix(PlayerPhysics __instance)
    {
        if (__instance.AmOwner && NjordMenuGUI.invertControls && __instance.body != null)
        {
            __instance.body.velocity = -__instance.body.velocity;
        }

        SeePlayerVent(__instance);
        }
    }
    [HarmonyPatch(typeof(LobbyBehaviour), nameof(LobbyBehaviour.Start))]
    public static class LobbyStart_ApplyLevelSpoof
    {
        public static void Postfix()
        {
            if (!NjordMenuGUI.isEditingLevel && uint.TryParse(NjordMenuGUI.spoofLevelString, out uint parsedLvl))
            {
                uint targetLevel = parsedLvl > 0 ? parsedLvl - 1 : 0;
                try { AmongUs.Data.DataManager.Player.stats.level = targetLevel; }
                catch { try { AmongUs.Data.DataManager.Player.Stats.Level = targetLevel; } catch { } }
                AmongUs.Data.DataManager.Player.Save();
            }
        }
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.HandleRpc))]
public static class RPCSniffer_Patch
{
    private static readonly HashSet<byte> VanillaRPCs = new HashSet<byte>
        {
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21,
            22, 23, 24, 25, 26, 27, 29, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42,
            43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 60, 61, 62, 63, 64, 65
        };

    private static readonly Dictionary<byte, (string Name, string Color)> KnownMods = new Dictionary<byte, (string, string)>
        {
            { 157, ("RockStar", "#800000") },
            { 121, ("RockStar / Chocoo", "#800000") },
            { 167, ("TuffMenu", "#008000") },
            { 164, ("Hydra / Sicko", "#FF0000") },
            { 176, ("HostGuard / TOH", "#008000") },
            { 195, ("Polar Client", "#FFFF00") },
            { 204, ("Polar Client", "#FFFF00") },
            { 154, ("GNC", "#FF0000") },
            { 85,  ("KillNet (Base)", "#FF0000") },
            { 150, ("KillNet (V2)", "#FF0000") },
            { 162, ("KNM", "#FF0000") },
            { 250, ("KillNet (Alt)", "#FF0000") },
            { 212, ("BanMod", "#008000") },
            { 213, ("BanMod", "#008000") },
            { 214, ("BanMod", "#008000") },
            { 215, ("BanMod", "#008000") },
            { 216, ("BanMod", "#008000") },
            { 217, ("BanMod", "#008000") },
            { 218, ("BanMod", "#008000") },
            { 219, ("BanMod", "#008000") },
            { 144, ("Gaff Menu", "#FF0000") },
            { 145, ("Gaff Menu", "#FF0000") },
            { 188, ("GMM", "#FF0000") },
            { 189, ("GMM", "#FF0000") },
            { 169, ("Malum", "#FF0000") },
            { 210, ("Eclipse", "#FFFF00") },
            { 173, ("Private", "#FF0000") },
            { 151, ("Better Among Us", "#008000") },
            { 152, ("Better Among Us", "#008000") },
            { 255, ("CrewMod", "#FFFF00") },
            { 111, ("AUM (BitCrackers)", "#FF0000") },
            { 231, ("SentinelAU", "#FF0000") },
            { 133, ("Lunar / NjordMenu", "#00FFFF") },
            { 89,  ("Njord Menu Old", "#008000") }
        };

    public static bool Prefix(PlayerControl __instance, byte callId, MessageReader reader)
    {
        if (__instance == null) return true;

       
        if (PlayerControl.LocalPlayer != null && __instance == PlayerControl.LocalPlayer) return true;

        if (NjordMenuGUI.LogAllRPCs)
        {
            
            if (!VanillaRPCs.Contains(callId))
            {
                string pNameSniff = (__instance.Data != null && !string.IsNullOrEmpty(__instance.Data.PlayerName)) ? __instance.Data.PlayerName : $"Player_{__instance.PlayerId}";

                
                if (KnownMods.TryGetValue(callId, out var modInfo))
                {
                    NjordMenuGUI.ShowNotification($"<color=#00FFFF>[СНИФФЕР]</color> <b>{pNameSniff}</b>: <b><color={modInfo.Color}>{modInfo.Name}</color></b> <color=#FFFF00>({callId})</color>");
                }
                else
                {
                    NjordMenuGUI.ShowNotification($"<color=#00FFFF>[СНИФФЕР]</color> <b>{pNameSniff}</b> кинул неизвестный RPC: <color=#FFFF00>{callId}</color>");
                }
            }
        }
        return true;
    }
}


[HarmonyPatch(typeof(HatManager), nameof(HatManager.Initialize))]
public static class UnlockCosmetics_HatManager_Initialize_Postfix
{
    public static void Postfix(HatManager __instance)
    {
        if (!NjordMenuGUI.unlockCosmetics) return;

        foreach (var bundle in __instance.allBundles) bundle.Free = true;
        foreach (var hat in __instance.allHats) hat.Free = true;
        foreach (var nameplate in __instance.allNamePlates) nameplate.Free = true;
        foreach (var pet in __instance.allPets) pet.Free = true;
        foreach (var skin in __instance.allSkins) skin.Free = true;
        foreach (var visor in __instance.allVisors) visor.Free = true;
        foreach (var starBundle in __instance.allStarBundles) starBundle.price = 0;
    }
}

[HarmonyPatch(typeof(PlayerPurchasesData), nameof(PlayerPurchasesData.GetPurchase))]
public static class UnlockCosmetics_PlayerPurchasesData_GetPurchase_Prefix
{
    public static bool Prefix(ref bool __result)
    {
        if (!NjordMenuGUI.unlockCosmetics) return true;
        __result = true;
        return false;
    }
}

[HarmonyPatch(typeof(GameContainer), nameof(GameContainer.SetupGameInfo))]
public static class MoreLobbyInfo_GameContainer_SetupGameInfo_Postfix
{
    public static void Postfix(GameContainer __instance)
    {
        if (!NjordMenuGUI.moreLobbyInfo) return;

        var trueHostName = __instance.gameListing.TrueHostName;
        const string separator = "<#0000>000000000000000</color>";
        var age = __instance.gameListing.Age;
        var lobbyTime = $"Age: {age / 60}:{(age % 60 < 10 ? "0" : "")}{age % 60}";

        
        int platId = (int)__instance.gameListing.Platform;
        string platformStr = platId switch
        {
            1 => "Epic",
            2 => "Steam",
            3 => "Mac",
            4 => "Microsoft Store",
            5 => "Itch.io",
            6 => "iOS",
            7 => "Android",
            8 => "Nintendo Switch",
            9 => "Xbox",
            10 => "PlayStation",
            112 => "Starlight",
            _ => "Unknown"
        };

        string hexColor = ColorUtility.ToHtmlStringRGB(NjordMenuGUI.currentAccentColor);

        __instance.capacity.text = $"<size=40%>{separator}\n{trueHostName}\n{__instance.capacity.text}\n" +
                                   $"<color=#{hexColor}>{GameCode.IntToGameName(__instance.gameListing.GameId)}</color>\n" +
                                   $"<color=#{hexColor}>{platformStr}</color>\n{lobbyTime}\n{separator}</size>";
    }
}

[HarmonyPatch(typeof(FindAGameManager), nameof(FindAGameManager.HandleList))]
public static class MoreLobbyInfo_FindAGameManager_HandleList_Postfix
{
    public static void Postfix(HttpMatchmakerManager.FindGamesListFilteredResponse response, FindAGameManager __instance)
    {
        if (!NjordMenuGUI.moreLobbyInfo) return;

        __instance.TotalText.text = response.Metadata.AllGamesCount.ToString();
    }
}
[HarmonyPatch(typeof(PlatformSpecificData), nameof(PlatformSpecificData.Serialize))]
public static class PlatformSpooferPatch
{
    public static void Prefix(PlatformSpecificData __instance)
    {
        try
        {
            if (__instance != null)
            {
                if (NjordMenuGUI.enablePlatformSpoof)
                {
                    __instance.Platform = NjordMenuGUI.platformValues[NjordMenuGUI.currentPlatformIndex];
                }
                __instance.PlatformName = "NjordMenu by Meowchelo https://github.com/meowchelo/NjordMenu";
            }
        }
        catch { }
    }
}
[HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.KickPlayer))]
public static class AmongUsClient_KickPlayer_BanList_Patch
{
    public static void Prefix(InnerNetClient __instance, int clientId, bool ban)
    {
        if (ban && PlayerControl.AllPlayerControls != null && AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
        {
            try
            {
                var pc = PlayerControl.AllPlayerControls.ToArray().FirstOrDefault(p => p.OwnerId == clientId);
                if (pc != null && pc.Data != null)
                {
                    string fc = string.IsNullOrEmpty(pc.Data.FriendCode) ? "Unknown" : pc.Data.FriendCode;
                    string name = pc.Data.PlayerName ?? "Unknown";
                    string puid = "Unknown";

                    try
                    {
                        var client = AmongUsClient.Instance.GetClientFromCharacter(pc);
                        if (client != null) puid = client.Id.ToString();
                    }
                    catch { }

                    NjordMenuGUI.AddToBanList(fc, puid, name, "Host ban");
                    NjordMenuGUI.ShowNotification($"<color=#FF0000>[BAN]</color> {name} занесен в черный список!");
                }
            }
            catch { }
        }
    }
}
