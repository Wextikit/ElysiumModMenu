#nullable disable
using Hazel;
using InnerNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ElysiumModMenu
{
    public static class ElysiumBugroomScoutService
    {
        public sealed class BugroomScoutStatusSnapshot
        {
            public bool Enabled;
            public string State = string.Empty;
            public string FilePath = string.Empty;
            public string CurrentCode = string.Empty;
            public string CurrentSuffix = string.Empty;
            public string FoundCode = string.Empty;
            public int TargetCount;
        }

        private const string FileName = "Bugroom Scout.txt";
        private const float ReloadIntervalSeconds = 2f;
        private const float ExitDelaySeconds = 0.25f;
        private const float AutoCreateClickIntervalSeconds = 0.75f;

        private static readonly HashSet<string> targetSuffixes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static string state = "Off";
        private static string currentCode = string.Empty;
        private static string currentSuffix = string.Empty;
        private static string foundCode = string.Empty;
        private static float nextReloadAt;
        private static float nextAutoCreateClickAt;
        private static float exitQueuedAt = -1f;
        private static int queuedExitGameId = int.MinValue;
        private static int checkedGameId = int.MinValue;

        public static string FilePath => Path.Combine(Plugin.ElysiumFolder, FileName);

        public static void Tick()
        {
            if (!ElysiumModMenuGUI.BugroomScoutEnabled)
            {
                ResetRoomState();
                if (string.IsNullOrEmpty(state) || !state.StartsWith("Found", StringComparison.Ordinal))
                    state = "Off";
                return;
            }

            EnsureFile();
            ReloadTargetsIfNeeded();

            if (targetSuffixes.Count == 0)
            {
                ResetRoomState();
                state = "Add codes to Bugroom Scout.txt";
                return;
            }

            InnerNetClient client = TryGetClient();
            if (client == null || client.GameId == 0 || LobbyBehaviour.Instance == null)
            {
                ResetRoomState();
                if (client != null && client.GameId != 0)
                {
                    state = $"Waiting for lobby ({targetSuffixes.Count} targets)";
                    return;
                }

                state = $"Creating room ({targetSuffixes.Count} targets)";
                TickAutoCreateRoom();
                return;
            }

            int gameId = client.GameId;
            currentCode = ElysiumModMenuGUI.GetCurrentRoomCodeForStatus();
            currentSuffix = LastFour(NormalizeCode(currentCode));

            if (string.IsNullOrEmpty(currentSuffix))
            {
                state = "Waiting for room code";
                return;
            }

            if (targetSuffixes.Contains(currentSuffix))
            {
                foundCode = currentCode;
                checkedGameId = gameId;
                queuedExitGameId = int.MinValue;
                exitQueuedAt = -1f;
                ElysiumModMenuGUI.BugroomScoutEnabled = false;
                ElysiumModMenuGUI.settingsDirty = true;
                state = $"Found {currentSuffix}";
                ElysiumModMenuGUI.ShowNotification($"<color=#00FFAA>[BUGROOM SCOUT]</color> Found code: <b>{currentCode}</b>");
                return;
            }

            if (checkedGameId != gameId)
            {
                checkedGameId = gameId;
                queuedExitGameId = gameId;
                exitQueuedAt = Time.unscaledTime + ExitDelaySeconds;
                state = $"Skip {currentSuffix}";
                ElysiumModMenuGUI.ShowNotification($"<color=#FFAA00>[BUGROOM SCOUT]</color> {currentCode} != target, leaving.");
            }

            if (queuedExitGameId == gameId && exitQueuedAt >= 0f && Time.unscaledTime >= exitQueuedAt)
            {
                string latestCode = ElysiumModMenuGUI.GetCurrentRoomCodeForStatus();
                string latestSuffix = LastFour(NormalizeCode(latestCode));
                if (!string.IsNullOrEmpty(latestSuffix) && targetSuffixes.Contains(latestSuffix))
                {
                    currentCode = latestCode;
                    currentSuffix = latestSuffix;
                    foundCode = latestCode;
                    queuedExitGameId = int.MinValue;
                    exitQueuedAt = -1f;
                    ElysiumModMenuGUI.BugroomScoutEnabled = false;
                    ElysiumModMenuGUI.settingsDirty = true;
                    state = $"Found {latestSuffix}";
                    ElysiumModMenuGUI.ShowNotification($"<color=#00FFAA>[BUGROOM SCOUT]</color> Found code: <b>{latestCode}</b>");
                    return;
                }

                queuedExitGameId = int.MinValue;
                exitQueuedAt = -1f;
                checkedGameId = int.MinValue;
                try { AmongUsClient.Instance.ExitGame(DisconnectReasons.ExitGame); }
                catch { }
            }
        }

        public static BugroomScoutStatusSnapshot GetStatusSnapshot()
        {
            return new BugroomScoutStatusSnapshot
            {
                Enabled = ElysiumModMenuGUI.BugroomScoutEnabled,
                State = state ?? string.Empty,
                FilePath = FilePath,
                CurrentCode = currentCode ?? string.Empty,
                CurrentSuffix = currentSuffix ?? string.Empty,
                FoundCode = foundCode ?? string.Empty,
                TargetCount = targetSuffixes.Count,
            };
        }

        public static void ForceReload()
        {
            nextReloadAt = 0f;
            foundCode = string.Empty;
            if (!ElysiumModMenuGUI.BugroomScoutEnabled) state = "Off";
            EnsureFile();
            ReloadTargetsIfNeeded();
        }

        private static void ResetRoomState()
        {
            currentCode = string.Empty;
            currentSuffix = string.Empty;
            checkedGameId = int.MinValue;
            queuedExitGameId = int.MinValue;
            exitQueuedAt = -1f;
        }

        private static void TickAutoCreateRoom()
        {
            float now = Time.unscaledTime;
            if (now < nextAutoCreateClickAt) return;
            nextAutoCreateClickAt = now + AutoCreateClickIntervalSeconds;

            if (TryClickCreateConfirmButton())
            {
                state = "Confirming create";
                return;
            }

            if (TryClickUiButton(new[] { "create game" }, new[] { "enter code", "find game", "back", "cancel" }))
            {
                state = "Opening create game";
                return;
            }

            if (TryClickUiButton(new[] { "online" }, new[] { "local", "freeplay", "back", "cancel" }))
            {
                state = "Opening online";
                return;
            }

            if (TryClickUiButton(new[] { "play" }, new[] { "player", "display", "back", "cancel" }))
            {
                state = "Opening play";
                return;
            }
        }

        private static bool TryClickUiButton(string[] includeTokens, string[] excludeTokens)
        {
            try
            {
                PassiveButton[] passiveButtons = UnityEngine.Object.FindObjectsOfType<PassiveButton>();
                foreach (PassiveButton button in passiveButtons)
                {
                    if (button == null || button.OnClick == null) continue;
                    if (!button.gameObject.activeInHierarchy || !button.isActiveAndEnabled) continue;
                    if (!IsButtonMatch(button.name, button.GetComponentsInChildren<TMP_Text>(true), includeTokens, excludeTokens)) continue;

                    button.OnClick.Invoke();
                    return true;
                }
            }
            catch { }

            if (TryClickConfirmByTextParent())
                return true;

            try
            {
                Button[] unityButtons = UnityEngine.Object.FindObjectsOfType<Button>();
                foreach (Button button in unityButtons)
                {
                    if (button == null || button.onClick == null) continue;
                    if (!button.gameObject.activeInHierarchy || !button.isActiveAndEnabled || !button.interactable) continue;
                    if (!IsButtonMatch(button.name, button.GetComponentsInChildren<TMP_Text>(true), includeTokens, excludeTokens)) continue;

                    button.onClick.Invoke();
                    return true;
                }
            }
            catch { }

            return false;
        }

        private static bool TryClickCreateConfirmButton()
        {
            if (TryInvokeCreateGameConfirm())
                return true;

            try
            {
                PassiveButton[] passiveButtons = UnityEngine.Object.FindObjectsOfType<PassiveButton>();
                foreach (PassiveButton button in passiveButtons)
                {
                    if (button == null) continue;
                    if (!button.gameObject.activeInHierarchy || !button.isActiveAndEnabled) continue;
                    if (!IsConfirmButton(button.name, button.GetComponentsInChildren<TMP_Text>(true))) continue;
                    if (ClickPassiveButton(button)) return true;
                }
            }
            catch { }

            try
            {
                Button[] unityButtons = UnityEngine.Object.FindObjectsOfType<Button>();
                foreach (Button button in unityButtons)
                {
                    if (button == null || button.onClick == null) continue;
                    if (!button.gameObject.activeInHierarchy || !button.isActiveAndEnabled || !button.interactable) continue;
                    if (!IsConfirmButton(button.name, button.GetComponentsInChildren<TMP_Text>(true))) continue;

                    button.onClick.Invoke();
                    return true;
                }
            }
            catch { }

            return false;
        }

        private static bool TryInvokeCreateGameConfirm()
        {
            try
            {
                ConfirmCreatePopUp popup = UnityEngine.Object.FindObjectOfType<ConfirmCreatePopUp>();
                if (popup == null || !popup.gameObject.activeInHierarchy) return false;

                CreateGameOptions createOptions = null;
                try { createOptions = popup.createGameOptions; } catch { }
                if (createOptions == null)
                    createOptions = UnityEngine.Object.FindObjectOfType<CreateGameOptions>();
                if (createOptions == null || !createOptions.gameObject.activeInHierarchy) return false;

                createOptions.Confirm();
                return true;
            }
            catch { return false; }
        }

        private static bool TryClickConfirmByTextParent()
        {
            try
            {
                TMP_Text[] texts = UnityEngine.Object.FindObjectsOfType<TMP_Text>();
                foreach (TMP_Text text in texts)
                {
                    if (text == null || !text.gameObject.activeInHierarchy) continue;
                    if (NormalizeUiText(text.text).Trim() != "confirm") continue;

                    Transform cursor = text.transform;
                    for (int depth = 0; cursor != null && depth < 8; depth++, cursor = cursor.parent)
                    {
                        try
                        {
                            PassiveButton passiveButton = cursor.GetComponent<PassiveButton>();
                            if (passiveButton != null && passiveButton.gameObject.activeInHierarchy && passiveButton.isActiveAndEnabled)
                            {
                                ClickPassiveButton(passiveButton);
                                return true;
                            }
                        }
                        catch { }

                        try
                        {
                            Button unityButton = cursor.GetComponent<Button>();
                            if (unityButton != null && unityButton.gameObject.activeInHierarchy && unityButton.isActiveAndEnabled && unityButton.interactable)
                            {
                                unityButton.onClick.Invoke();
                                return true;
                            }
                        }
                        catch { }

                        if (TryInvokeLikelyClickMethods(cursor.gameObject))
                            return true;
                    }
                }
            }
            catch { }

            return false;
        }

        private static bool ClickPassiveButton(PassiveButton button)
        {
            if (button == null) return false;
            bool invoked = false;

            try
            {
                if (button.OnClick != null)
                {
                    button.OnClick.Invoke();
                    invoked = true;
                }
            }
            catch { }

            try
            {
                button.ReceiveClickDown();
                button.ReceiveClickUp();
                invoked = true;
            }
            catch { }

            try
            {
                if (TryInvokeLikelyClickMethods(button.gameObject))
                    invoked = true;
            }
            catch { }

            return invoked;
        }

        private static bool TryInvokeLikelyClickMethods(GameObject gameObject)
        {
            if (gameObject == null) return false;
            bool invoked = false;
            string[] methodNames = new[]
            {
                "OnClick", "Click", "Confirm", "OnConfirm", "Submit", "OnSubmit",
                "Accept", "CreateGame", "ConfirmCreate", "Create", "DoCreate"
            };

            try
            {
                MonoBehaviour[] components = gameObject.GetComponents<MonoBehaviour>();
                foreach (MonoBehaviour component in components)
                {
                    if (component == null) continue;
                    Type type = component.GetType();
                    foreach (string methodName in methodNames)
                    {
                        MethodInfo method = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
                        if (method == null) continue;
                        try
                        {
                            method.Invoke(component, null);
                            invoked = true;
                        }
                        catch { }
                    }
                }
            }
            catch { }

            return invoked;
        }

        private static bool IsConfirmButton(string objectName, Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppArrayBase<TMP_Text> texts)
        {
            string objectText = NormalizeUiText(objectName);
            if (objectText.Contains("confirm") && !objectText.Contains("settings"))
                return true;

            if (texts == null) return false;
            foreach (TMP_Text text in texts)
            {
                if (text == null) continue;
                string value = NormalizeUiText(text.text);
                if (value == "confirm" || value.Trim() == "confirm")
                    return true;
            }

            return false;
        }

        private static bool IsButtonMatch(string objectName, Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppArrayBase<TMP_Text> texts, string[] includeTokens, string[] excludeTokens)
        {
            string combined = NormalizeUiText(objectName);
            if (texts != null)
            {
                foreach (TMP_Text text in texts)
                {
                    if (text == null) continue;
                    combined += " " + NormalizeUiText(text.text);
                }
            }

            if (ContainsAny(combined, excludeTokens)) return false;
            return ContainsAny(combined, includeTokens);
        }

        private static string NormalizeUiText(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;

            StringBuilder sb = new StringBuilder(value.Length);
            bool inTag = false;
            foreach (char c in value)
            {
                if (c == '<') { inTag = true; continue; }
                if (c == '>') { inTag = false; continue; }
                if (inTag) continue;

                if (char.IsLetterOrDigit(c))
                    sb.Append(char.ToLowerInvariant(c));
                else
                    sb.Append(' ');
            }

            return sb.ToString();
        }

        private static bool ContainsAny(string value, string[] tokens)
        {
            if (string.IsNullOrEmpty(value) || tokens == null) return false;
            foreach (string token in tokens)
            {
                if (string.IsNullOrWhiteSpace(token)) continue;
                if (value.Contains(token.ToLowerInvariant())) return true;
            }
            return false;
        }

        private static void EnsureFile()
        {
            try
            {
                Directory.CreateDirectory(Plugin.ElysiumFolder);
                if (!File.Exists(FilePath))
                {
                    File.WriteAllText(FilePath,
                        "# Put room codes here. Full codes are OK: VCTZTG becomes TZTG.\n" +
                        "# Multiple targets per line are OK: TZTG TZTF THAD\n",
                        Encoding.UTF8);
                }
            }
            catch { }
        }

        private static void ReloadTargetsIfNeeded()
        {
            if (Time.unscaledTime < nextReloadAt) return;
            nextReloadAt = Time.unscaledTime + ReloadIntervalSeconds;

            targetSuffixes.Clear();
            try
            {
                if (!File.Exists(FilePath)) return;
                string[] lines = File.ReadAllLines(FilePath);
                foreach (string line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    string trimmed = line.Trim();
                    if (trimmed.StartsWith("#")) continue;

                    string[] parts = trimmed.Split(new[] { ' ', '\t', ',', ';', '|', '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string part in parts)
                    {
                        string suffix = LastFour(NormalizeCode(part));
                        if (suffix.Length == 4) targetSuffixes.Add(suffix);
                    }
                }
            }
            catch { }
        }

        private static string NormalizeCode(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            StringBuilder sb = new StringBuilder(value.Length);
            foreach (char c in value)
            {
                if (char.IsLetterOrDigit(c))
                    sb.Append(char.ToUpperInvariant(c));
            }
            return sb.ToString();
        }

        private static string LastFour(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Length <= 4 ? value : value.Substring(value.Length - 4);
        }

        private static InnerNetClient TryGetClient()
        {
            try { return AmongUsClient.Instance == null ? null : (InnerNetClient)AmongUsClient.Instance; }
            catch { return null; }
        }
    }

    public static class ElysiumBugroomFarmService
    {
        public sealed class BugroomFarmStatusSnapshot
        {
            public bool MainEnabled;
            public bool PassEnabled;
            public string MainState = string.Empty;
            public string PassState = string.Empty;
            public int Players;
            public int Level;
            public float TimerLeft;
        }

        private const float FarmDelay = 240f;
        private const float PassDelay = 2f;
        private const float RejoinDelay = 2f;
        private const float JoinTimeout = 18f;
        private const float PassCooldown = 8f;

        private static string mainState = "Off";
        private static string passState = "Off";
        private static float mainAt = -1f;
        private static float passAt = -1f;
        private static float lastPassAt = -20f;
        private static int mainCode;
        private static int passCode;
        private static bool mainRun;
        private static bool mainSawGame;
        private static bool mainLeaving;
        private static bool mainJoining;
        private static bool passLeaving;
        private static bool passJoining;

        public static void Tick()
        {
            TickMain();
            TickPass();
        }

        public static BugroomFarmStatusSnapshot GetStatusSnapshot()
        {
            return new BugroomFarmStatusSnapshot
            {
                MainEnabled = ElysiumModMenuGUI.bugRoomLv35Rehost,
                PassEnabled = ElysiumModMenuGUI.bugRoomHostPassRejoin,
                MainState = mainState ?? string.Empty,
                PassState = passState ?? string.Empty,
                Players = CountPlayers(),
                Level = GetLocalLevel(),
                TimerLeft = mainAt > 0f && !mainRun && !mainLeaving && !mainJoining ? Mathf.Max(0f, mainAt - Time.unscaledTime) : 0f,
            };
        }

        public static void ResetMain()
        {
            mainAt = -1f;
            mainCode = 0;
            mainRun = false;
            mainSawGame = false;
            mainLeaving = false;
            mainJoining = false;
            mainState = ElysiumModMenuGUI.bugRoomLv35Rehost ? "Idle" : "Off";
        }

        public static void ResetPass()
        {
            passAt = -1f;
            passCode = 0;
            passLeaving = false;
            passJoining = false;
            passState = ElysiumModMenuGUI.bugRoomHostPassRejoin ? "Waiting host" : "Off";
        }

        private static void TickMain()
        {
            if (!ElysiumModMenuGUI.bugRoomLv35Rehost)
            {
                ResetMain();
                return;
            }

            float now = Time.unscaledTime;
            InnerNetClient client = TryGetClient();

            if (mainLeaving)
            {
                if (InRoom()) return;
                if (now < mainAt) return;

                Rejoin(mainCode);
                mainAt = now + JoinTimeout;
                mainLeaving = false;
                mainJoining = true;
                mainState = "Rejoining";
                return;
            }

            if (mainJoining)
            {
                if (InRoom())
                {
                    mainJoining = false;
                    mainRun = false;
                    mainSawGame = false;
                    mainAt = -1f;
                    ElysiumModMenuGUI.AutoHostAutoRunEnabled = false;
                    mainState = "Back, waiting host";
                    return;
                }

                if (now >= mainAt)
                {
                    GUIUtility.systemCopyBuffer = mainCode != 0 ? GameCode.IntToGameName(mainCode) : GUIUtility.systemCopyBuffer;
                    ElysiumModMenuGUI.bugRoomLv35Rehost = false;
                    ElysiumModMenuGUI.settingsDirty = true;
                    ResetMain();
                    ElysiumModMenuGUI.ShowNotification("<color=#FF4444>[BUG ROOM]</color> Main rejoin failed, code copied.");
                }
                return;
            }

            if (client == null || !InRoom())
            {
                mainAt = -1f;
                mainState = "Waiting room";
                return;
            }

            int cnt = CountPlayers();
            int lvl = GetLocalLevel();

            if (mainRun && mainSawGame && LobbyBehaviour.Instance != null)
            {
                ElysiumModMenuGUI.AutoHostAutoRunEnabled = false;
                ElysiumModMenuGUI.settingsDirty = true;

                if (lvl >= 35)
                {
                    if (cnt < 2)
                    {
                        mainState = $"Lv35 wait players {cnt}/2";
                        return;
                    }

                    mainCode = client.GameId;
                    Leave();
                    mainAt = now + RejoinDelay;
                    mainLeaving = true;
                    mainState = "Lv35 rejoin";
                    ElysiumModMenuGUI.ShowNotification("<color=#FF00FF>[BUG ROOM]</color> Lv35 reached, rejoining room.");
                    return;
                }

                mainRun = false;
                mainSawGame = false;
                mainAt = now + FarmDelay;
                mainState = "Next 4m timer";
                return;
            }

            if (ShipStatus.Instance != null && LobbyBehaviour.Instance == null)
            {
                if (mainRun) mainSawGame = true;
                mainState = mainRun ? "Auto win running" : "In game";
                return;
            }

            if (!client.AmHost)
            {
                mainAt = -1f;
                mainState = "Waiting host";
                return;
            }

            if (cnt < 2)
            {
                mainAt = -1f;
                mainState = $"Players {cnt}/2";
                return;
            }

            if (mainRun)
            {
                mainState = "Waiting result";
                return;
            }

            if (mainAt < 0f)
            {
                mainAt = now + FarmDelay;
                ElysiumModMenuGUI.AutoHostAutoRunEnabled = false;
                ElysiumModMenuGUI.settingsDirty = true;
                mainState = "Timer 4m";
                return;
            }

            if (now < mainAt)
            {
                mainState = "Timer 4m";
                return;
            }

            if (ElysiumModMenuGUI.AutoHostMinPlayers > 2)
                ElysiumModMenuGUI.AutoHostMinPlayers = 2;
            ElysiumModMenuGUI.AutoReturnLobbyAfterMatch = true;
            ElysiumModMenuGUI.AutoHostAutoRunEnabled = true;
            ElysiumModMenuGUI.settingsDirty = true;
            mainRun = true;
            mainSawGame = false;
            mainState = "Auto Run ON";
            ElysiumModMenuGUI.ShowNotification("<color=#FF00FF>[BUG ROOM]</color> Auto Run enabled after 4m.");
        }

        private static void TickPass()
        {
            if (!ElysiumModMenuGUI.bugRoomHostPassRejoin)
            {
                ResetPass();
                return;
            }

            float now = Time.unscaledTime;
            InnerNetClient client = TryGetClient();

            if (passLeaving)
            {
                if (InRoom()) return;
                if (now < passAt) return;

                Rejoin(passCode);
                passAt = now + JoinTimeout;
                passLeaving = false;
                passJoining = true;
                passState = "Rejoining";
                return;
            }

            if (passJoining)
            {
                if (InRoom())
                {
                    passJoining = false;
                    passAt = -1f;
                    lastPassAt = now;
                    passState = "Back";
                    return;
                }

                if (now >= passAt)
                {
                    GUIUtility.systemCopyBuffer = passCode != 0 ? GameCode.IntToGameName(passCode) : GUIUtility.systemCopyBuffer;
                    ResetPass();
                    ElysiumModMenuGUI.ShowNotification("<color=#FF4444>[BUG ROOM]</color> Pass rejoin failed, code copied.");
                }
                return;
            }

            if (client == null || LobbyBehaviour.Instance == null)
            {
                passAt = -1f;
                passState = "Waiting lobby";
                return;
            }

            if (!client.AmHost)
            {
                passAt = -1f;
                passState = "Waiting";
                return;
            }

            int cnt = CountPlayers();
            if (cnt < 2)
            {
                passAt = -1f;
                passState = $"Host, players {cnt}/2";
                return;
            }

            if (now - lastPassAt < PassCooldown)
            {
                passState = "Cooldown";
                return;
            }

            if (passAt < 0f)
            {
                passAt = now + PassDelay;
                passState = "Pass in 2s";
                return;
            }

            if (now < passAt)
            {
                passState = "Pass in 2s";
                return;
            }

            passCode = client.GameId;
            Leave();
            passAt = now + RejoinDelay;
            passLeaving = true;
            passState = "Leaving";
            ElysiumModMenuGUI.ShowNotification("<color=#FF00FF>[BUG ROOM]</color> Host pass rejoin.");
        }

        private static int CountPlayers()
        {
            int n = 0;
            try
            {
                if (AmongUsClient.Instance != null && AmongUsClient.Instance.allClients != null)
                {
                    var c = AmongUsClient.Instance.allClients.GetEnumerator();
                    while (c.MoveNext())
                    {
                        ClientData cd = c.Current;
                        if (cd == null || cd.Id < 0) continue;
                        if (cd.Character != null && cd.Character.Data != null && cd.Character.Data.Disconnected) continue;
                        n++;
                    }
                    if (n > 0) return n;
                }
            }
            catch { }

            try
            {
                if (PlayerControl.AllPlayerControls == null) return 0;
                foreach (PlayerControl pc in PlayerControl.AllPlayerControls)
                    if (pc != null && pc.Data != null && !pc.Data.Disconnected && pc.PlayerId < 100)
                        n++;
            }
            catch { }
            return n;
        }

        private static int GetLocalLevel()
        {
            try
            {
                PlayerControl pc = PlayerControl.LocalPlayer;
                if (pc != null && pc.Data != null)
                {
                    uint raw = pc.Data.PlayerLevel;
                    if (raw != uint.MaxValue && raw < 10000) return (int)raw + 1;
                }
            }
            catch { }
            return 0;
        }

        private static void Rejoin(int code)
        {
            try
            {
                AmongUsClient au = AmongUsClient.Instance;
                if (au == null || code == 0) return;
                au.GameId = code;
                var co = au.CoJoinOnlineGameFromCode(code);
                if (co != null) au.StartCoroutine(co);
            }
            catch { }
        }

        private static void Leave()
        {
            try { if (AmongUsClient.Instance != null) AmongUsClient.Instance.ExitGame(DisconnectReasons.ExitGame); }
            catch { }
        }

        private static bool InRoom()
        {
            return LobbyBehaviour.Instance != null || ShipStatus.Instance != null;
        }

        private static InnerNetClient TryGetClient()
        {
            try { return AmongUsClient.Instance == null ? null : (InnerNetClient)AmongUsClient.Instance; }
            catch { return null; }
        }
    }
}
