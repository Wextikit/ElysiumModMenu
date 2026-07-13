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
