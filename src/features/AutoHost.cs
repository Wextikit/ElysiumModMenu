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
public static class ElysiumAutoLobbyReturn
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
                return ElysiumModMenuGUI.AutoReturnLobbyAfterMatch || ElysiumAutoHostService.ShouldReturnAfterMatch;
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

public static class ElysiumAutoHostService
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
                    StartMode = ElysiumModMenuGUI.AutoHostInstantStart ? "Мгновенный" : "Обычный",
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

                bool waitingForLoad = ElysiumModMenuGUI.AutoHostWaitLoadedPlayers && connectedPlayers > readyPlayers;
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
                bool enoughPlayers = ElysiumModMenuGUI.AutoHostWaitLoadedPlayers ? readyPlayers >= requiredPlayers : connectedPlayers >= requiredPlayers;
                bool continueBelowMin = !ElysiumModMenuGUI.AutoHostCancelBelowMin && countdownStartedAt >= 0f && connectedPlayers >= 2;

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
                    if (ElysiumModMenuGUI.AutoHostInstantStart)
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
                int grace = Mathf.Clamp((int)ElysiumModMenuGUI.AutoHostLoadGraceSeconds, 0, 90);
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
                int forceAfterMinutes = Mathf.Clamp(ElysiumModMenuGUI.AutoHostForceAfterMinutes, 0, 10);
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
                int threshold = Mathf.Clamp(ElysiumModMenuGUI.AutoHostFastStartPlayers, 0, 15);
                return threshold > 0 && connectedPlayers >= threshold;
            }

            private static float EffectiveStartDelay(int connectedPlayers)
            {
                float delay = StartDelaySeconds;
                if (IsFastStartActive(connectedPlayers))
                    delay = Mathf.Min(delay, Mathf.Clamp(ElysiumModMenuGUI.AutoHostFastStartDelaySeconds, 0, 60));
                return delay;
            }

            private static bool IsInMatch() => ShipStatus.Instance != null && LobbyBehaviour.Instance == null && !IsEndGameScreen();

            private static bool IsEndGameScreen()
            {
                try { return UnityEngine.Object.FindObjectOfType<EndGameManager>() != null; } catch { return false; }
            }

            private static void Notify(string title, string detail)
            {
                if (!ElysiumModMenuGUI.AutoHostNotifications) return;
                float now = Time.unscaledTime;
                if (lastNotificationAt > 0f && now - lastNotificationAt < NotificationCooldownSeconds) return;
                lastNotificationAt = now;
                ElysiumModMenuGUI.ShowNotification($"<color=#FF00FF>[{title}]</color> {detail}");
            }

            private static string FormatState(AutoHostState value)
            {
                return value switch
                {
                    AutoHostState.Disabled => L("Disabled", "Выключен"),
                    AutoHostState.Idle => L("Idle", "Ожидание"),
                    AutoHostState.Warmup => L("Warmup", "Прогрев"),
                    AutoHostState.WaitingPlayers => L("Waiting for players", "Ждет игроков"),
                    AutoHostState.WaitingLoad => L("Waiting for load", "Ждет прогрузку"),
                    AutoHostState.Countdown => L("Countdown", "Отсчет"),
                    AutoHostState.Starting => L("Starting", "Запуск"),
                    AutoHostState.InGame => L("In Game", "В игре"),
                    AutoHostState.Returning => L("Returning", "Возврат"),
                    AutoHostState.Backoff => L("Backoff", "Пауза"),
                    _ => value.ToString(),
                };
            }

            private static string CleanName(string value)
            {
                if (string.IsNullOrWhiteSpace(value)) return "игрок";
                string clean = value.Replace("\r", " ").Replace("\n", " ").Trim();
                return clean.Length <= 18 ? clean : clean.Substring(0, 17) + "...";
            }

            public static bool IsEnabled => ElysiumModMenuGUI.AutoHostEnabled;
            public static bool ShouldReturnAfterMatch => IsEnabled && ElysiumModMenuGUI.AutoReturnLobbyAfterMatch;
            private static bool ForceLastMinuteEnabled => ElysiumModMenuGUI.AutoHostForceLastMinute;
            private static int RequiredPlayers => Mathf.Clamp(ElysiumModMenuGUI.AutoHostMinPlayers, 1, 15);
            private static int ForceMinPlayers => Mathf.Clamp(ElysiumModMenuGUI.AutoHostForceMinPlayers, 1, 15);
            private static float StartDelaySeconds => Mathf.Clamp(ElysiumModMenuGUI.AutoHostStartDelaySeconds, 0f, 180f);
            private static float BackoffSeconds => Mathf.Clamp(ElysiumModMenuGUI.AutoHostBackoffSeconds, 2f, 60f);
            private static float CountdownRemaining => countdownStartedAt < 0f ? 0f : Mathf.Clamp((activeCountdownDelay >= 0f ? activeCountdownDelay : StartDelaySeconds) - (Time.unscaledTime - countdownStartedAt), 0f, StartDelaySeconds);
            private static float BackoffRemaining => backoffUntil < 0f ? 0f : Mathf.Clamp(backoffUntil - Time.unscaledTime, 0f, BackoffSeconds);
            private static float LobbyLifeRemaining => lobbyOpenedAt < 0f ? -1f : Mathf.Clamp(LobbyLifetimeSeconds - (Time.unscaledTime - lobbyOpenedAt), 0f, LobbyLifetimeSeconds);
            private static float WarmupRemaining => lobbyOpenedAt < 0f ? 0f : Mathf.Clamp(ElysiumModMenuGUI.AutoHostWarmupSeconds - (Time.unscaledTime - lobbyOpenedAt), 0f, 120f);
            private static float LoadGraceRemaining => loadWaitStartedAt < 0f || ElysiumModMenuGUI.AutoHostLoadGraceSeconds <= 0 ? 0f : Mathf.Clamp(ElysiumModMenuGUI.AutoHostLoadGraceSeconds - (Time.unscaledTime - loadWaitStartedAt), 0f, 90f);
        }

private int currentVisualsSubTab = 0;
    }
}
