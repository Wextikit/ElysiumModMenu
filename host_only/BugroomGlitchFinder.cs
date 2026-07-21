#nullable disable
using InnerNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace ElysiumModMenu
{
    public static class ElysiumBugroomGlitchFinder
    {
        public sealed class GlitchFinderStatusSnapshot
        {
            public bool Enabled;
            public string State = string.Empty;
            public string FilePath = string.Empty;
            public string CurrentCode = string.Empty;
            public string CurrentSuffix = string.Empty;
            public int VisitedCount;
            public int FoundCount;
            public int Level;
        }

        private enum FinderState
        {
            Idle,
            LobbyDelay,
            Starting,
            WaitShhh,
            WaitEnd,
            Leaving,
        }

        private const string FileName = "Bugroom Glitch Rooms.txt";
        private const float CreateInterval = 0.75f;
        private const float StartDelay = 0.75f;
        private const float StartRetry = 0.75f;
        private const float StartTimeout = 7f;
        private const float LevelDelay = 0.75f;
        private const float LevelTimeout = 5f;
        private const float LeaveDelay = 0.25f;

        private static readonly HashSet<string> visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static FinderState finderState;
        private static string state = "Off";
        private static string currentCode = string.Empty;
        private static string currentSuffix = string.Empty;
        private static float nextCreateAt;
        private static float actionAt = -1f;
        private static float levelTimeoutAt = -1f;
        private static int currentGameId;
        private static int currentLevel;
        private static bool fileLoaded;

        public static string FilePath => Path.Combine(Plugin.ElysiumFolder, FileName);

        public static void Tick()
        {
            if (!ElysiumModMenuGUI.BugroomGlitchFinderEnabled)
            {
                if (finderState != FinderState.Idle) ResetRoom();
                state = "Off";
                return;
            }

            EnsureFile();

            if (ElysiumBugroomScoutService.TryCloseDisconnectPopup())
            {
                ResetRoom();
                nextCreateAt = Time.unscaledTime + CreateInterval;
                state = "Create failed, retrying";
                return;
            }

            float now = Time.unscaledTime;
            InnerNetClient client = TryGetClient();

            if (finderState == FinderState.Leaving)
            {
                if (client == null || client.GameId == 0)
                {
                    ResetRoom();
                    nextCreateAt = now + CreateInterval;
                    state = "Creating room";
                    return;
                }

                if (now >= actionAt)
                {
                    Leave();
                    actionAt = now + 1f;
                }
                state = "Leaving room";
                return;
            }

            if (IsEndGameScreen())
            {
                if (finderState != FinderState.WaitEnd || actionAt < 0f)
                {
                    finderState = FinderState.WaitEnd;
                    actionAt = now + LevelDelay;
                    levelTimeoutAt = actionAt + LevelTimeout;
                }

                if (now < actionAt)
                {
                    state = "Waiting level update";
                    return;
                }

                currentLevel = GetLocalLevel();
                if (currentLevel == 0 && now < levelTimeoutAt)
                {
                    state = "Waiting local level";
                    return;
                }

                if (currentLevel == 1)
                {
                    if (SaveCode())
                    {
                        state = $"Found {currentSuffix}";
                        ElysiumModMenuGUI.ShowNotification($"<color=#00FFAA>[GLITCH FINDER]</color> Level reset in <b>{currentCode}</b>, code saved.");
                    }
                    else
                    {
                        state = $"Save failed {currentSuffix}";
                        ElysiumModMenuGUI.ShowNotification($"<color=#FF4444>[GLITCH FINDER]</color> Level reset in <b>{currentCode}</b>, TXT write failed.");
                    }
                }
                else
                {
                    state = currentLevel > 0 ? $"Level {currentLevel}, leaving" : "Level unavailable, leaving";
                }

                finderState = FinderState.Leaving;
                actionAt = now + LeaveDelay;
                return;
            }

            if (ShipStatus.Instance != null && LobbyBehaviour.Instance == null)
            {
                if (finderState == FinderState.WaitEnd)
                {
                    state = "Waiting end screen";
                    return;
                }

                finderState = FinderState.WaitShhh;
                if (!ElysiumModMenuGUI.HasCurrentGameSeenShhh())
                {
                    state = "Waiting Shhh";
                    return;
                }

                if (EndGame())
                {
                    finderState = FinderState.WaitEnd;
                    actionAt = -1f;
                    levelTimeoutAt = -1f;
                    state = "Ending game";
                }
                else
                {
                    state = "Retrying end game";
                }
                return;
            }

            if (client != null && client.GameId != 0 && LobbyBehaviour.Instance != null)
            {
                int gameId = client.GameId;
                string code = ElysiumModMenuGUI.GetCurrentRoomCodeForStatus();
                string suffix = LastFour(NormalizeCode(code));

                if (currentGameId != gameId || !string.Equals(currentSuffix, suffix, StringComparison.OrdinalIgnoreCase))
                {
                    currentGameId = gameId;
                    currentCode = code;
                    currentSuffix = suffix;
                    currentLevel = GetLocalLevel();

                    if (suffix.Length != 4)
                    {
                        state = "Waiting room code";
                        return;
                    }

                    if (visited.Contains(suffix))
                    {
                        finderState = FinderState.Leaving;
                        actionAt = now;
                        state = $"Repeat {suffix}, leaving";
                        return;
                    }

                    visited.Add(suffix);
                    finderState = FinderState.LobbyDelay;
                    actionAt = now + StartDelay;
                    state = $"Waiting 0.75s in {suffix}";
                }

                if (!client.AmHost)
                {
                    if (finderState == FinderState.LobbyDelay && now < actionAt)
                    {
                        state = "Waiting host";
                        return;
                    }

                    finderState = FinderState.Leaving;
                    actionAt = now;
                    state = "Not host, leaving";
                    return;
                }

                if (finderState == FinderState.Starting)
                {
                    if (now < actionAt)
                    {
                        state = "Starting game";
                        return;
                    }

                    finderState = FinderState.LobbyDelay;
                    actionAt = now;
                }

                if (finderState == FinderState.LobbyDelay && now >= actionAt)
                {
                    if (StartGame())
                    {
                        finderState = FinderState.Starting;
                        actionAt = now + StartTimeout;
                        state = "Start sent";
                    }
                    else
                    {
                        actionAt = now + StartRetry;
                        state = "Retrying start";
                    }
                }
                return;
            }

            if (client != null && client.GameId != 0)
            {
                finderState = FinderState.Leaving;
                actionAt = now;
                state = "Leaving stale room";
                return;
            }

            ResetRoom();
            state = "Creating room";
            if (now < nextCreateAt) return;
            nextCreateAt = now + CreateInterval;

            if (ElysiumBugroomScoutService.TryClickCreateConfirmButton())
            {
                state = "Confirming create";
                return;
            }

            if (ElysiumBugroomScoutService.TryClickUiButton(new[] { "create game" }, new[] { "enter code", "find game", "back", "cancel" }))
            {
                state = "Opening create game";
                return;
            }

            if (ElysiumBugroomScoutService.TryClickUiButton(new[] { "online" }, new[] { "local", "freeplay", "back", "cancel" }))
            {
                state = "Opening online";
                return;
            }

            if (ElysiumBugroomScoutService.TryClickUiButton(new[] { "play" }, new[] { "player", "display", "back", "cancel" }))
                state = "Opening play";
        }

        public static GlitchFinderStatusSnapshot GetStatusSnapshot()
        {
            EnsureFile();
            return new GlitchFinderStatusSnapshot
            {
                Enabled = ElysiumModMenuGUI.BugroomGlitchFinderEnabled,
                State = state ?? string.Empty,
                FilePath = FilePath,
                CurrentCode = currentCode ?? string.Empty,
                CurrentSuffix = currentSuffix ?? string.Empty,
                VisitedCount = visited.Count,
                FoundCount = found.Count,
                Level = currentLevel,
            };
        }

        public static void ResetFlow()
        {
            ResetRoom();
            nextCreateAt = 0f;
            state = ElysiumModMenuGUI.BugroomGlitchFinderEnabled ? "Creating room" : "Off";
        }

        public static void ClearVisited()
        {
            visited.Clear();
        }

        private static void ResetRoom()
        {
            finderState = FinderState.Idle;
            currentCode = string.Empty;
            currentSuffix = string.Empty;
            currentGameId = 0;
            currentLevel = 0;
            actionAt = -1f;
            levelTimeoutAt = -1f;
        }

        private static bool StartGame()
        {
            try
            {
                if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost || LobbyBehaviour.Instance == null)
                    return false;

                GameStartManager manager = UnityEngine.Object.FindObjectOfType<GameStartManager>();
                if (manager == null) return false;
                manager.MinPlayers = 1;
                manager.startState = GameStartManager.StartingStates.Countdown;
                manager.countDownTimer = 0f;
                return true;
            }
            catch { return false; }
        }

        private static bool EndGame()
        {
            bool oldBlock = false;
            bool changedBlock = false;
            try
            {
                if (!ElysiumModMenuGUI.CanRunHostEndGameAction(false) || GameManager.Instance == null)
                    return false;

                int reason = GameManager.Instance.IsHideAndSeek() ? 8 : 3;
                oldBlock = ElysiumModMenuGUI.neverEndGame;
                changedBlock = true;
                ElysiumModMenuGUI.neverEndGame = false;
                GameManager.Instance.RpcEndGame((GameOverReason)reason, false);
                return true;
            }
            catch { return false; }
            finally
            {
                if (changedBlock) ElysiumModMenuGUI.neverEndGame = oldBlock;
            }
        }

        private static int GetLocalLevel()
        {
            try
            {
                uint raw = AmongUs.Data.DataManager.Player.stats.level;
                if (raw != uint.MaxValue && raw < 10000) return (int)raw + 1;
            }
            catch
            {
                try
                {
                    uint raw = AmongUs.Data.DataManager.Player.Stats.Level;
                    if (raw != uint.MaxValue && raw < 10000) return (int)raw + 1;
                }
                catch { }
            }

            try
            {
                PlayerControl plr = PlayerControl.LocalPlayer;
                if (plr != null && plr.Data != null)
                {
                    uint raw = plr.Data.PlayerLevel;
                    if (raw != uint.MaxValue && raw < 10000) return (int)raw + 1;
                }
            }
            catch { }
            return 0;
        }

        private static bool SaveCode()
        {
            if (currentSuffix.Length != 4) return false;
            if (found.Contains(currentSuffix)) return true;
            try
            {
                Directory.CreateDirectory(Plugin.ElysiumFolder);
                File.AppendAllText(FilePath, currentCode + Environment.NewLine, Encoding.UTF8);
                found.Add(currentSuffix);
                return true;
            }
            catch { return false; }
        }

        private static void EnsureFile()
        {
            try
            {
                Directory.CreateDirectory(Plugin.ElysiumFolder);
                if (!File.Exists(FilePath))
                    File.WriteAllText(FilePath, string.Empty, Encoding.UTF8);

                if (fileLoaded) return;
                foreach (string line in File.ReadAllLines(FilePath))
                {
                    string suffix = LastFour(NormalizeCode(line));
                    if (suffix.Length == 4) found.Add(suffix);
                }
                fileLoaded = true;
            }
            catch { }
        }

        private static bool InRoom()
        {
            return LobbyBehaviour.Instance != null || ShipStatus.Instance != null || IsEndGameScreen();
        }

        private static bool IsEndGameScreen()
        {
            try { return UnityEngine.Object.FindObjectOfType<EndGameManager>() != null; }
            catch { return false; }
        }

        private static void Leave()
        {
            try
            {
                if (AmongUsClient.Instance != null)
                    AmongUsClient.Instance.ExitGame(DisconnectReasons.ExitGame);
            }
            catch { }
        }

        private static string NormalizeCode(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            StringBuilder sb = new StringBuilder(value.Length);
            foreach (char c in value)
                if (char.IsLetterOrDigit(c))
                    sb.Append(char.ToUpperInvariant(c));
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
}
