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

private static string CompactEspValue(string value, int maxLength = 24)
        {
            value = Regex.Replace(value ?? string.Empty, "<.*?>", string.Empty)
                .Replace('\r', ' ')
                .Replace('\n', ' ')
                .Trim();

            if (string.IsNullOrEmpty(value)) return "Hidden";
            if (value.Length > maxLength) value = value.Substring(0, maxLength - 3) + "...";
            return value;
        }

private static string NormalizeEspToken(string value)
        {
            return Regex.Replace(value ?? string.Empty, "<.*?>", string.Empty)
                .Replace('\r', ' ')
                .Replace('\n', ' ')
                .Trim();
        }

private static string FriendEspIgnoreFilePath()
        {
            string folder = string.IsNullOrWhiteSpace(Plugin.ElysiumFolder)
                ? System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "ElysiumModMenu")
                : Plugin.ElysiumFolder;
            return System.IO.Path.Combine(folder, "ElysiumFriendEspIgnore.txt");
        }

private static void LoadFriendEspIgnoreTokensIfNeeded()
        {
            try
            {
                if (Time.unscaledTime < friendEspIgnoreNextLoadAt) return;
                friendEspIgnoreNextLoadAt = Time.unscaledTime + 3f;

                friendEspIgnoreTokens.Clear();
                string path = FriendEspIgnoreFilePath();
                if (!System.IO.File.Exists(path))
                {
                    System.IO.File.WriteAllText(path, "# One nickname, Friend Code, or PUID per line. Matching players will not show ESP info.\n");
                    return;
                }

                foreach (string line in System.IO.File.ReadAllLines(path))
                {
                    string token = NormalizeEspToken(line);
                    if (string.IsNullOrWhiteSpace(token) || token.StartsWith("#")) continue;
                    friendEspIgnoreTokens.Add(token);
                }
            }
            catch { }
        }

private static bool IsEspIgnored(NetworkedPlayerInfo info)
        {
            if (info == null) return false;

            LoadFriendEspIgnoreTokensIfNeeded();
            if (friendEspIgnoreTokens.Count == 0) return false;

            try
            {
                string name = NormalizeEspToken(info.PlayerName);
                if (!string.IsNullOrEmpty(name) && friendEspIgnoreTokens.Contains(name)) return true;

                string displayedFc = NormalizeEspToken(GetDisplayedFriendCode(info, string.Empty));
                if (!string.IsNullOrEmpty(displayedFc) && friendEspIgnoreTokens.Contains(displayedFc)) return true;

                string rawFc = NormalizeEspToken(info.FriendCode);
                if (!string.IsNullOrEmpty(rawFc) && friendEspIgnoreTokens.Contains(rawFc)) return true;

                ClientData client = AmongUsClient.Instance?.GetClientFromPlayerInfo(info);
                string puid = client == null ? string.Empty : NormalizeEspToken(GetClientPuid(client));
                return !string.IsNullOrEmpty(puid) && friendEspIgnoreTokens.Contains(puid);
            }
            catch { return false; }
        }

public static string BuildESPInfoLine(NetworkedPlayerInfo info, int customPlatformMaxLength = 13, bool includeFriendCode = true)
        {
            if (info == null) return string.Empty;

            int level = 0;
            string platform = "Unknown";
            bool isHost = false;
            bool hasCustomPlatformName = false;

            try { level = (int)info.PlayerLevel + 1; } catch { }

            try
            {
                var client = AmongUsClient.Instance.GetClientFromPlayerInfo(info);
                if (client != null)
                {
                    platform = GetPlatform(client);
                    if (IsCustomPlatformName(client, out string customPlatformName))
                    {
                        platform = CompactEspValue(customPlatformName, Mathf.Max(4, customPlatformMaxLength));
                        hasCustomPlatformName = true;
                    }
                    isHost = AmongUsClient.Instance.GetHost() == client;
                }
            }
            catch { }

            if (enablePlatformSpoof &&
                !hasCustomPlatformName &&
                PlayerControl.LocalPlayer != null &&
                info.PlayerId == PlayerControl.LocalPlayer.PlayerId)
            {
                platform = $"{platform} spf";
            }

            string fc = CompactEspValue(GetDisplayedFriendCode(info));
            List<string> parts = new List<string>();
            if (isHost) parts.Add("Host");
            parts.Add($"Lv:{level}");
            parts.Add(platform);
            if (includeFriendCode && showEspFriendCode) parts.Add(fc);
            return string.Join(" - ", parts);
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

                if (!enable || PlayerControl.LocalPlayer == null || target == PlayerControl.LocalPlayer || target.Data == null || target.Data.Disconnected || IsEspIgnored(target.Data))
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

private void DrawLobbyAllColorSlider()
        {
            int maxColor = MaxOutfitColorId();
            lobbyAllColorId = Mathf.Clamp(lobbyAllColorId, 0, maxColor);

            Rect row = GUILayoutUtility.GetRect(250f, 26f, GUILayout.Width(250f), GUILayout.Height(26f));
            string colorName = SafeColorName(lobbyAllColorId);

            GUIStyle rowLabelStyle = new GUIStyle(toggleLabelStyle)
            {
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip,
                fontSize = 11,
                padding = CreateRectOffset(0, 0, 0, 0)
            };

            Rect labelRect = new Rect(row.x, row.y + 2f, 88f, 22f);
            Rect sliderRect = new Rect(labelRect.xMax + 7f, row.y, 86f, row.height);
            Rect applyRect = new Rect(row.xMax - 58f, row.y + 2f, 58f, 22f);

            GUI.Label(labelRect, $"{L("Color:", "Цвет:")} {colorName}", rowLabelStyle);
            lobbyAllColorId = DrawCenteredColorSlider(sliderRect, lobbyAllColorId, maxColor);

            if (GUI.Button(applyRect, L("Apply", "Применить"), btnStyle))
            {
                ApplyColorToLobby(lobbyAllColorId);
                ShowNotification($"<color=#00FFAA>[LOBBY]</color> {L("Applied lobby color.", "Цвет лобби применен.")}");
            }
        }

private int DrawCenteredColorSlider(Rect rect, int value, int maxValue)
        {
            maxValue = Mathf.Max(0, maxValue);
            value = Mathf.Clamp(value, 0, maxValue);

            float centerY = rect.y + rect.height * 0.5f;
            Rect trackRect = new Rect(rect.x + 2f, centerY - 4f, rect.width - 4f, 8f);
            float thumbSize = 18f;
            float t = maxValue <= 0 ? 0f : value / (float)maxValue;
            float thumbCenterX = Mathf.Lerp(trackRect.xMin, trackRect.xMax, t);
            Rect thumbRect = new Rect(thumbCenterX - thumbSize * 0.5f, centerY - thumbSize * 0.5f, thumbSize, thumbSize);
            Rect hitRect = new Rect(rect.x, centerY - 12f, rect.width, 24f);

            int controlId = GUIUtility.GetControlID("LobbyAllColorSlider".GetHashCode(), FocusType.Passive, hitRect);
            Event e = Event.current;

            if (e != null)
            {
                EventType type = e.GetTypeForControl(controlId);
                if (type == EventType.MouseDown && e.button == 0 && hitRect.Contains(e.mousePosition))
                {
                    GUIUtility.hotControl = controlId;
                    value = SliderValueFromMouse(e.mousePosition.x, trackRect, maxValue);
                    e.Use();
                }
                else if (type == EventType.MouseDrag && GUIUtility.hotControl == controlId)
                {
                    value = SliderValueFromMouse(e.mousePosition.x, trackRect, maxValue);
                    e.Use();
                }
                else if (type == EventType.MouseUp && GUIUtility.hotControl == controlId)
                {
                    GUIUtility.hotControl = 0;
                    e.Use();
                }
            }

            GUI.Box(trackRect, string.Empty, sliderStyle);
            GUI.Box(thumbRect, string.Empty, sliderThumbStyle);
            return value;
        }

private static int SliderValueFromMouse(float mouseX, Rect trackRect, int maxValue)
        {
            if (maxValue <= 0) return 0;
            float t = Mathf.InverseLerp(trackRect.xMin, trackRect.xMax, mouseX);
            return Mathf.Clamp(Mathf.RoundToInt(t * maxValue), 0, maxValue);
        }

private static void ApplyColorToLobby(int colorId)
        {
            if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost || PlayerControl.AllPlayerControls == null) return;

            byte targetColor = (byte)Mathf.Clamp(colorId, 0, MaxOutfitColorId());
            try
            {
                foreach (var player in PlayerControl.AllPlayerControls)
                {
                    if (player != null && player.Data != null && !player.Data.Disconnected)
                        player.RpcSetColor(targetColor);
                }
            }
            catch { }
        }

private static string GetSafeColorName(int colorId)
        {
            try { return Palette.GetColorName(Mathf.Clamp(colorId, 0, MaxOutfitColorId())); }
            catch { return $"Color {colorId}"; }
        }

private void DrawLobbyControls()
        {
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(menuCardStyle, GUILayout.Width(282));
            DrawMenuSectionHeader(L("GAME RULES", "ПРАВИЛА ИГРЫ"));
            neverEndGame = DrawToggle(neverEndGame, L("Unlimited Game", "Бесконечная игра"), 250);
            GUILayout.Space(5);
            noSettingLimit = DrawToggle(noSettingLimit, L("No Setting Limit", "Без лимитов настроек"), 250);
            GUILayout.Space(5);
            noTaskMode = DrawToggle(noTaskMode, L("No Task Mode", "Без заданий"), 250);
            GUILayout.Space(5);
            allowDuplicateColors = DrawToggle(allowDuplicateColors, L("Allow Duplicate Colors", "Разрешить одинаковые цвета"), 250);
            GUILayout.Space(5);

            bool prevLobbyRainbowAll = lobbyRainbowAll;
            lobbyRainbowAll = DrawToggle(lobbyRainbowAll, L("Rainbow All", "Радуга всем"), 250);
            if (lobbyRainbowAll && !prevLobbyRainbowAll)
            {
                lobbyAllColor = false;
                colorTimer = 0f;
            }

            GUILayout.Space(5);
            bool prevLobbyAllColor = lobbyAllColor;
            lobbyAllColor = DrawToggle(lobbyAllColor, L("All Color", "Цвет всем"), 250);
            if (lobbyAllColor && !prevLobbyAllColor)
            {
                lobbyRainbowAll = false;
            }

            if (lobbyAllColor)
            {
                GUILayout.Space(3);
                DrawLobbyAllColorSlider();
            }
            GUILayout.EndVertical();

            GUILayout.Space(10);

            GUILayout.BeginVertical(menuCardStyle, GUILayout.Width(282));
            DrawMenuSectionHeader(L("CHAT MODERATION", "МОДЕРАЦИЯ ЧАТА"));
            enableColorCommand = DrawToggle(enableColorCommand, L("Enable /c command (Public)", "Разрешить команду /c"), 250);
            GUILayout.Space(5);
            blockFortegreenChat = DrawToggle(blockFortegreenChat, L("Block Fortegreen Chat", "Блокировать чат Fortegreen"), 250);
            GUILayout.Space(5);
            blockRainbowChat = DrawToggle(blockRainbowChat, L("Block Rainbow Chat", "Блокировать радужный чат"), 250);
            GUILayout.Space(5);
            autoChatEveryone = DrawToggle(autoChatEveryone, L("Chat Everyone (Auto-Meeting)", "Чат всем через авто-митинг"), 250);
            if (autoChatEveryone)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{L("Delay:", "Задержка:")} {autoChatEveryoneDelay:0.0}s", toggleLabelStyle, GUILayout.Width(92));
                autoChatEveryoneDelay = GUILayout.HorizontalSlider(autoChatEveryoneDelay, 0f, 10f, sliderStyle, sliderThumbStyle, GUILayout.Width(170));
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(menuCardStyle, GUILayout.Width(282));
            DrawMenuSectionHeader(L("LOBBY ACTIONS", "ДЕЙСТВИЯ ЛОББИ"));
            if (GUILayout.Button(L("Insta Start", "Мгновенный старт"), btnStyle, GUILayout.Height(26)))
            { GameStartManager.Instance.startState = GameStartManager.StartingStates.Countdown; GameStartManager.Instance.countDownTimer = 0f; }
            GUILayout.Space(5);
            if (GUILayout.Button(L("Close Meeting", "Закрыть собрание"), btnStyle, GUILayout.Height(26))) MeetingHud.Instance.RpcClose();
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(L("Spawn Lobby", "Создать лобби"), activeTabStyle, GUILayout.Height(26))) SpawnLobby();
            GUILayout.Space(5);
            if (GUILayout.Button(L("Despawn", "Удалить"), btnStyle, GUILayout.Height(26))) DespawnLobby();
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(L("Kill All", "Убить всех"), btnStyle, GUILayout.Height(26))) KillAll();
            GUILayout.Space(5);
            if (GUILayout.Button(L("Kick All", "Кикнуть всех"), btnStyle, GUILayout.Height(26))) KickAll();
            GUILayout.Space(5);
            if (GUILayout.Button(L("Mass Morph", "Масс-морф"), btnStyle, GUILayout.Height(26))) this.StartCoroutine(MassMorphCoroutine().WrapToIl2Cpp());
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.Space(10);

            GUILayout.BeginVertical(menuCardStyle, GUILayout.Width(282));
            DrawMenuSectionHeader(L("END GAME", "КОНЕЦ ИГРЫ"));
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(L("Crewmate Win", "Победа экипажа"), btnStyle, GUILayout.Height(26))) SmartEndGame("CrewWin");
            GUILayout.Space(5);
            if (GUILayout.Button(L("Impostor Win", "Победа предателей"), btnStyle, GUILayout.Height(26))) SmartEndGame("ImpWin");
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(L("Imp Disconnect", "Дисконнект предателя"), btnStyle, GUILayout.Height(26))) SmartEndGame("ImpDisconnect");
            GUILayout.Space(5);
            if (GUILayout.Button(L("H&S Disconnect", "H&S дисконнект"), activeTabStyle, GUILayout.Height(26))) SmartEndGame("HnsImpDisconnect");
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            if (GUILayout.Button(L("Force End (Impostor Disconnect)", "Завершить принудительно"), btnStyle, GUILayout.Height(26)) && GameManager.Instance != null && AmongUsClient.Instance.AmHost)
            { bool tempNeverEnd = neverEndGame; neverEndGame = false; GameManager.Instance.RpcEndGame((GameOverReason)4, false); neverEndGame = tempNeverEnd; }
            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

public static string GetESPNameTag(NetworkedPlayerInfo info, string originalName)
        {
            if (info == null) return originalName;
            string newName = originalName;
            bool suppressPlayerInfo = IsMeetingVoteUiActive();
            if (enableLocalNameSpoof &&
                PlayerControl.LocalPlayer != null &&
                info.PlayerId == PlayerControl.LocalPlayer.PlayerId &&
                !string.IsNullOrWhiteSpace(customNameInput))
            {
                newName = BuildLocalNameRenderText(customNameInput);
            }

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
            if (showPlayerInfo && !suppressPlayerInfo)
            {
                string accentHex = GetMenuAccentHex(false);
                string espLine = BuildESPInfoLine(info);
                if (!string.IsNullOrWhiteSpace(espLine))
                    newName = $"<size=80%><color=#{accentHex}>{espLine}</color></size>\n{newName}";
            }
            if (seeKillCooldown && !suppressPlayerInfo && info.Role != null && info.PlayerId != PlayerControl.LocalPlayer?.PlayerId)
            {
                int roleId = (int)info.Role.Role;
                bool isImpTeam = roleId == 1 || roleId == 5 || roleId == 9 || roleId == 18;
                if (isImpTeam)
                {
                    float rem = GetRemainingKillCooldown(info.PlayerId);
                    string kcdText = rem > 0.01f ? $"KCD: {rem:F1}s" : "KCD: READY";
                    string kcdColor = rem > 0.01f ? "FFAA33" : "55FF77";
                    newName = $"<size=78%><color=#{kcdColor}>{kcdText}</color></size>\n{newName}";
                }
            }
            return newName;
        }

public static bool IsMeetingVoteUiActive()
        {
            try
            {
                return MeetingHud.Instance != null && MeetingHud.Instance.gameObject != null && MeetingHud.Instance.gameObject.activeInHierarchy;
            }
            catch
            {
                return MeetingHud.Instance != null;
            }
        }

private static float GetConfiguredKillCooldown()
        {
            try
            {
                object opts = GameOptionsManager.Instance?.CurrentGameOptions;
                if (opts == null) return 25f;
                var m = opts.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(x => x.Name == "GetFloat" && x.GetParameters().Length == 1);
                if (m != null)
                {
                    Type enumType = m.GetParameters()[0].ParameterType;
                    if (enumType.IsEnum)
                    {
                        foreach (var val in Enum.GetValues(enumType))
                        {
                            string n = val.ToString().ToLower();
                            if (n.Contains("kill") && n.Contains("cool"))
                            {
                                object result = m.Invoke(opts, new object[] { val });
                                return Convert.ToSingle(result);
                            }
                        }
                    }
                }
            }
            catch { }
            return 25f;
        }

private static float GetRemainingKillCooldown(byte playerId)
        {
            if (!lastKillTimestamps.ContainsKey(playerId)) return 0f;
            float elapsed = Time.time - lastKillTimestamps[playerId];
            float rem = GetConfiguredKillCooldown() - elapsed;
            return Mathf.Max(0f, rem);
        }

private static bool IsImpostorTeamForCooldown(PlayerControl pc)
        {
            try
            {
                if (pc == null || pc.Data == null) return false;
                int roleId = pc.Data.Role != null ? (int)pc.Data.Role.Role : (int)pc.Data.RoleType;
                return roleId == 1 || roleId == 5 || roleId == 9 || roleId == 18;
            }
            catch { return false; }
        }

public static void InitializeKillCooldownOnRoundStart()
        {
            try
            {
                lastKillTimestamps.Clear();
                if (PlayerControl.AllPlayerControls == null) return;

                float now = Time.time;
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    if (pc == null || pc.Data == null || pc.Data.Disconnected) continue;
                    if (!IsImpostorTeamForCooldown(pc)) continue;
                    lastKillTimestamps[pc.PlayerId] = now;
                }
            }
            catch { }
        }

[HarmonyPatch(typeof(VersionShower), nameof(VersionShower.Start))]
        public static class VersionShower_Start_Patch
        {
            public static void Postfix(VersionShower __instance) { if (__instance != null && __instance.text != null) __instance.text.text = ElysiumModMenuGUI.ApplyMenuShimmer("ElysiumModMenu Meowchelo & Carrot"); }
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

                    if (ElysiumModMenuGUI.showWatermark)
                    {
                        string shimmerTitle = ElysiumModMenuGUI.ApplyMenuShimmer("ElysiumModMenu v1.3.8");
                        finalString = $"{shimmerTitle} • " + finalString;
                    }

                    if (AmongUsClient.Instance != null)
                    {
                        ClientData host = AmongUsClient.Instance.GetHost();
                        if (host != null && host.Character != null)
                        {
                            string hostName = host.Character.Data.PlayerName ?? "Unknown";
                            string shimmerHostName = ElysiumModMenuGUI.ApplyMenuShimmer(hostName);
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

[HarmonyPatch(typeof(GameManager), nameof(GameManager.RpcEndGame))]
        public static class InfiniteGamePatch { public static bool Prefix() { try { if (ElysiumModMenuGUI.neverEndGame && AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost) return false; } catch { } return true; } }

[HarmonyPatch(typeof(IntroCutscene), "CoBegin")]
        public static class IntroCutscene_CoBegin_Patch
        {
            public static void Prefix()
            {
                if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return;
                if (ElysiumModMenuGUI.enablePreGameRoleForce)
                {
                    foreach (var kvp in ElysiumModMenuGUI.forcedPreGameRoles)
                    { var target = GameData.Instance.GetPlayerById(kvp.Key)?.Object; if (target != null && target.Data.RoleType != kvp.Value) target.RpcSetRole(kvp.Value); }
                    foreach (byte impId in ElysiumModMenuGUI.forcedImpostors)
                    { var target = GameData.Instance.GetPlayerById(impId)?.Object; if (target != null && target.Data.Role != null && !target.Data.Role.IsImpostor) target.RpcSetRole(RoleTypes.Impostor); }
                }
            }
        }

        private static bool CanPatchAssignRolesForTeam()
        {
            try
            {
                // Epic currently uses Il2CppInterop 1.5.x. Its native-to-managed
                // wrapper crashes while boxing AssignRolesForTeam value arguments.
                // The SelectRoles patch remains active and calls the native method
                // directly, so role forcing still works without this wrapper.
                string gameRoot = System.IO.Directory.GetCurrentDirectory();
                return !System.IO.Directory.Exists(System.IO.Path.Combine(gameRoot, ".egstore"));
            }
            catch
            {
                // If the storefront cannot be detected, avoid installing the unsafe
                // wrapper. RoleManager_SelectRoles_Patch is the portable fallback.
                return false;
            }
        }

[HarmonyPatch(typeof(LogicRoleSelectionNormal), "AssignRolesForTeam")]
        public static class RoleSelectionNormal_Patch
        {
            public static bool Prepare() => ElysiumModMenuGUI.CanPatchAssignRolesForTeam();

            public static bool Prefix(Il2CppSystem.Collections.Generic.List<NetworkedPlayerInfo> players, IGameOptions opts, RoleTeamTypes team, ref int teamMax)
            {
                if (!ElysiumModMenuGUI.enablePreGameRoleForce || AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
                    return true;

                try
                {
                    if ((int)team == 1)
                    {
                        int numImps = opts.GetInt((Int32OptionNames)1);
                        var impRoleTypes = new HashSet<int> { 1, 5, 9, 18 };
                        List<byte> allForced = new List<byte>(ElysiumModMenuGUI.forcedImpostors);

                        foreach (var kvp in ElysiumModMenuGUI.forcedPreGameRoles)
                            if (impRoleTypes.Contains((int)kvp.Value) && !allForced.Contains(kvp.Key))
                                allForced.Add(kvp.Key);

                        if (allForced.Count > 0) numImps = allForced.Count;
                        else
                        {
                            if (numImps >= players.Count) numImps = players.Count - 1;
                            if (numImps < 1) numImps = 1;
                        }

                        int assigned = 0;
                        foreach (byte impId in allForced)
                        {
                            if (players.Count == 0 || assigned >= numImps) break;
                            var targetInfo = players.ToArray().FirstOrDefault(p => p.PlayerId == impId);
                            if (targetInfo == null || targetInfo.Object == null) continue;

                            RoleTypes role = ElysiumModMenuGUI.forcedPreGameRoles.TryGetValue(impId, out RoleTypes forcedRole)
                                ? forcedRole
                                : RoleTypes.Impostor;
                            targetInfo.Object.RpcSetRole(role, false);
                            players.Remove(targetInfo);
                            assigned++;
                        }

                        while (assigned < numImps && players.Count > 0)
                        {
                            int index = UnityEngine.Random.Range(0, players.Count);
                            players[index].Object.RpcSetRole(RoleTypes.Impostor, false);
                            players.RemoveAt(index);
                            assigned++;
                        }

                        return false;
                    }

                    if ((int)team == 0)
                    {
                        var crewRoleTypes = new HashSet<int> { 0, 2, 3, 4, 8, 10, 12 };
                        for (int index = players.Count - 1; index >= 0; index--)
                        {
                            NetworkedPlayerInfo player = players[index];
                            if (player == null || player.Object == null) continue;

                            RoleTypes role = RoleTypes.Crewmate;
                            if (ElysiumModMenuGUI.forcedPreGameRoles.TryGetValue(player.PlayerId, out RoleTypes forcedRole) &&
                                crewRoleTypes.Contains((int)forcedRole))
                                role = forcedRole;

                            player.Object.RpcSetRole(role, false);
                            players.RemoveAt(index);
                        }

                        return false;
                    }
                }
                catch { }

                return true;
            }
        }

[HarmonyPatch(typeof(LogicRoleSelectionHnS), "AssignRolesForTeam")]
        public static class RoleSelectionHnS_Patch
        {
            public static bool Prepare() => ElysiumModMenuGUI.CanPatchAssignRolesForTeam();

            public static bool Prefix(Il2CppSystem.Collections.Generic.List<NetworkedPlayerInfo> players, IGameOptions opts, RoleTeamTypes team, ref int teamMax)
            {
                if (!ElysiumModMenuGUI.enablePreGameRoleForce || AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost || (int)team != 1)
                    return true;

                try
                {
                    int numImps = opts.GetInt((Int32OptionNames)1);
                    var impRoleTypes = new HashSet<int> { 1, 5, 9, 18 };
                    List<byte> allForced = new List<byte>(ElysiumModMenuGUI.forcedImpostors);

                    foreach (var kvp in ElysiumModMenuGUI.forcedPreGameRoles)
                        if (impRoleTypes.Contains((int)kvp.Value) && !allForced.Contains(kvp.Key))
                            allForced.Add(kvp.Key);

                    if (allForced.Count > 0) numImps = allForced.Count;
                    else
                    {
                        if (numImps >= players.Count) numImps = players.Count - 1;
                        if (numImps < 1) numImps = 1;
                    }

                    int assigned = 0;
                    foreach (byte impId in allForced)
                    {
                        if (players.Count == 0 || assigned >= numImps) break;
                        var targetInfo = players.ToArray().FirstOrDefault(p => p.PlayerId == impId);
                        if (targetInfo == null || targetInfo.Object == null) continue;

                        targetInfo.Object.RpcSetRole(RoleTypes.Impostor, false);
                        players.Remove(targetInfo);
                        assigned++;
                    }

                    while (assigned < numImps && players.Count > 0)
                    {
                        int index = UnityEngine.Random.Range(0, players.Count);
                        players[index].Object.RpcSetRole(RoleTypes.Impostor, false);
                        players.RemoveAt(index);
                        assigned++;
                    }

                    return false;
                }
                catch
                {
                    return true;
                }
            }
        }

}
}
