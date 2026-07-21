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

private sealed class EspStringCacheEntry
        {
            public string Value;
            public string Original;
            public string LocalName;
            public string Accent;
            public float ExpiresAt;
            public int RoleId;
            public bool SuppressInfo;
            public bool Roles;
            public bool PlayerInfo;
            public bool KillCooldown;
            public bool FriendCode;
            public bool LocalSpoof;
            public bool VoteKicks;
            public bool RgbEsp;
        }

private static readonly Dictionary<int, EspStringCacheEntry> espInfoLineCache = new Dictionary<int, EspStringCacheEntry>();

private static readonly Dictionary<byte, EspStringCacheEntry> espNameTagCache = new Dictionary<byte, EspStringCacheEntry>();

private sealed class VoteKickEspEntry
        {
            public int Count;
            public float LastAt;
        }

private static readonly Dictionary<byte, VoteKickEspEntry> voteKickEsp = new Dictionary<byte, VoteKickEspEntry>();

private static bool IsRainbowEsp(byte id)
        {
            if (lobbyRainbowAll) return true;
            if (rainbowPlayers != null && rainbowPlayers.Contains(id)) return true;
            try
            {
                if (PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.PlayerId == id)
                    return localRainbow || localRainbowFreeOnly;
            }
            catch { }
            return false;
        }

private static Color GetRainbowEspColor(byte id)
        {
            float h = Mathf.Repeat(Time.unscaledTime * 0.42f + id * 0.071f, 1f);
            Color c = Color.HSVToRGB(h, 0.92f, 1f);
            c.a = 1f;
            return c;
        }

private static Color GetEspBaseColor(byte id, Color fallback)
        {
            if (rgbMenuMode) return GetMenuControlAccentColor();
            if (IsRainbowEsp(id)) return GetRainbowEspColor(id);
            return fallback;
        }

private static Color GetEspShimmerColor(byte id, Color fallback)
        {
            Color baseCol = GetEspBaseColor(id, fallback);
            float wave = 0.5f + Mathf.Sin(Time.unscaledTime * 4.6f + id * 0.63f) * 0.5f;
            Color c = Color.Lerp(baseCol, Color.white, 0.18f + wave * 0.42f);
            c.a = 1f;
            return c;
        }

public static string GetEspAccentHex(byte id)
        {
            if (espShimmerMode)
                return ColorUtility.ToHtmlStringRGB(GetEspShimmerColor(id, GetMenuAccentColor(false)));
            if (rgbMenuMode) return GetMenuControlAccentHex();
            return IsRainbowEsp(id) ? ColorUtility.ToHtmlStringRGB(GetRainbowEspColor(id)) : GetMenuAccentHex(false);
        }

public static Color GetEspColor(PlayerControl pc, Color fallback)
        {
            try
            {
                if (pc != null && pc.Data != null && espShimmerMode)
                    return GetEspShimmerColor(pc.PlayerId, fallback);
                if (pc != null && pc.Data != null && IsRainbowEsp(pc.PlayerId))
                    return GetRainbowEspColor(pc.PlayerId);
                if (rgbMenuMode)
                    return GetMenuControlAccentColor();
            }
            catch { }
            return fallback;
        }

public static void RegisterVoteKickEsp(int targetClientId)
        {
            try
            {
                if (targetClientId < 0 || PlayerControl.AllPlayerControls == null) return;
                foreach (PlayerControl pc in PlayerControl.AllPlayerControls)
                {
                    if (pc == null || pc.Data == null) continue;
                    if (pc.Data.ClientId != targetClientId && (int)pc.OwnerId != targetClientId) continue;

                    if (!voteKickEsp.TryGetValue(pc.PlayerId, out VoteKickEspEntry vk))
                    {
                        vk = new VoteKickEspEntry();
                        voteKickEsp[pc.PlayerId] = vk;
                    }

                    if (Time.unscaledTime - vk.LastAt > 12f)
                        vk.Count = 0;

                    vk.Count++;
                    vk.LastAt = Time.unscaledTime;
                    espNameTagCache.Remove(pc.PlayerId);
                    break;
                }
            }
            catch { }
        }

private static int GetVoteKickEspCount(byte playerId)
        {
            try
            {
                if (!voteKickEsp.TryGetValue(playerId, out VoteKickEspEntry vk)) return 0;
                if (Time.unscaledTime - vk.LastAt > 12f)
                {
                    voteKickEsp.Remove(playerId);
                    return 0;
                }
                return vk.Count;
            }
            catch { return 0; }
        }

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

                ClientData client = AmongUsClient.Instance?.GetClientFromPlayerInfo(info);
                string puid = client == null ? string.Empty : NormalizeEspToken(GetClientPuid(client));
                return !string.IsNullOrEmpty(puid) && friendEspIgnoreTokens.Contains(puid);
            }
            catch { return false; }
        }

public static string BuildESPInfoLine(NetworkedPlayerInfo info, int customPlatformMaxLength = 13, bool includeFriendCode = true)
        {
            if (info == null) return string.Empty;

            int cacheKey = info.PlayerId |
                (Mathf.Clamp(customPlatformMaxLength, 0, 255) << 8) |
                (includeFriendCode ? 1 << 16 : 0) |
                (showEspFriendCode ? 1 << 17 : 0) |
                (enablePlatformSpoof ? 1 << 18 : 0);
            if (espInfoLineCache.TryGetValue(cacheKey, out EspStringCacheEntry cached) &&
                Time.unscaledTime < cached.ExpiresAt)
                return cached.Value;

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
            string result = string.Join(" - ", parts);
            espInfoLineCache[cacheKey] = new EspStringCacheEntry
            {
                Value = result,
                ExpiresAt = Time.unscaledTime + 1f
            };
            return result;
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

                tColor = GetEspColor(target, tColor);
                lr.SetColors(tColor, tColor);

                lr.SetPosition(0, PlayerControl.LocalPlayer.transform.position);
                lr.SetPosition(1, target.transform.position);
            }
            catch { }
        }

public static bool ShouldShowPlayerTracer(PlayerControl target)
        {
            if (target == null || target.Data == null || target.Data.Disconnected) return false;
            if (showTracers) return true;
            if (target.Data.IsDead) return showDeadTracers;

            bool isImpostor = false;
            try
            {
                isImpostor = target.Data.Role != null && target.Data.Role.IsImpostor;
                if (!isImpostor)
                {
                    int roleId = (int)target.Data.RoleType;
                    isImpostor = roleId == 1 || roleId == 5 || roleId == 9 || roleId == 18;
                }
            }
            catch { }

            return isImpostor ? showImpostorTracers : showCrewmateTracers;
        }

public static void HandleBodyTracer(DeadBody body, bool enable)
        {
            try
            {
                if (body == null || body.gameObject == null) return;

                LineRenderer line = body.GetComponent<LineRenderer>();
                if (!enable || PlayerControl.LocalPlayer == null)
                {
                    if (line != null) line.enabled = false;
                    return;
                }

                if (line == null)
                {
                    line = body.gameObject.AddComponent<LineRenderer>();
                    line.SetVertexCount(2);
                    line.SetWidth(0.025f, 0.025f);
                    try { if (HatManager.Instance != null) line.material = HatManager.Instance.PlayerMaterial; } catch { }
                }

                line.enabled = true;
                Color bodyColor = Color.yellow;
                line.SetColors(bodyColor, bodyColor);
                line.SetPosition(0, PlayerControl.LocalPlayer.transform.position);
                line.SetPosition(1, body.transform.position);
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
                ShowNotification("<color=#00FFAA>[LOBBY]</color> Applied lobby color.");
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
            // Keep room for the menu's inner margins and the scroll-view gutter.
            float outerContentWidth = Mathf.Floor(Mathf.Max(220f, GetMenuWorkWidth(220f, 760f) - 44f));
            float columnGap = 10f;
            const float hostActionButtonHeight = 24f;
            const float hostActionGap = 4f;
            const float topTileHeight = 220f;
            const float bottomTileHeight = 220f;
            const float lobbyActionsWidthOffset = 20f;
            float lobbyColumnWidth = Mathf.Floor(Mathf.Max(190f, (outerContentWidth - columnGap) / 2f));
            float cardPaddingWidth = menuCardStyle != null && menuCardStyle.padding != null
                ? menuCardStyle.padding.left + menuCardStyle.padding.right
                : 28f;
            float cardInnerWidth = Mathf.Max(120f, lobbyColumnWidth - cardPaddingWidth);
            float twoButtonWidth = Mathf.Max(52f, (cardInnerWidth - hostActionGap) / 2f);
            float threeButtonWidth = Mathf.Max(40f, (cardInnerWidth - (hostActionGap * 2f)) / 3f);
            int lobbyControlWidth = Mathf.RoundToInt(Mathf.Max(150f, lobbyColumnWidth - 32f));

            GUILayout.BeginVertical(GUILayout.Width(outerContentWidth));

            GUILayout.BeginHorizontal(GUILayout.Width(outerContentWidth));
            GUILayout.BeginVertical(menuCardStyle, GUILayout.Width(lobbyColumnWidth + lobbyActionsWidthOffset), GUILayout.Height(topTileHeight));
            DrawMenuSectionHeader(L("GAME RULES", "ПРАВИЛА ИГРЫ"));
            GUILayout.FlexibleSpace();
            neverEndGame = DrawToggle(neverEndGame, L("Unlimited Game", "Бесконечная игра"), lobbyControlWidth);
            GUILayout.Space(5);
            noSettingLimit = DrawToggle(noSettingLimit, L("No Setting Limit", "Без лимитов настроек"), lobbyControlWidth);
            GUILayout.Space(5);
            noTaskMode = DrawToggle(noTaskMode, L("No Task Mode", "Без заданий"), lobbyControlWidth);
            GUILayout.Space(5);
            allowDuplicateColors = DrawToggle(allowDuplicateColors, L("Allow Duplicate Colors", "Разрешить одинаковые цвета"), lobbyControlWidth);
            GUILayout.Space(5);

            bool prevLobbyRainbowAll = lobbyRainbowAll;
            lobbyRainbowAll = DrawToggle(lobbyRainbowAll, L("Rainbow All", "Радуга всем"), lobbyControlWidth);
            if (lobbyRainbowAll && !prevLobbyRainbowAll)
            {
                lobbyAllColor = false;
                colorTimer = 0f;
            }

            GUILayout.Space(5);
            bool prevLobbyAllColor = lobbyAllColor;
            lobbyAllColor = DrawToggle(lobbyAllColor, L("All Color", "Цвет всем"), lobbyControlWidth);
            if (lobbyAllColor && !prevLobbyAllColor)
            {
                lobbyRainbowAll = false;
            }

            GUILayout.Space(5);
            bool oldWlOnly = whitelistOnlyLobby;
            whitelistOnlyLobby = DrawToggle(whitelistOnlyLobby, "White List Only", lobbyControlWidth);
            if (oldWlOnly != whitelistOnlyLobby) settingsDirty = true;

            if (lobbyAllColor)
            {
                GUILayout.Space(3);
                DrawLobbyAllColorSlider();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();

            GUILayout.Space(10);

            GUILayout.BeginVertical(menuCardStyle, GUILayout.Width(lobbyColumnWidth), GUILayout.Height(topTileHeight));
            DrawMenuSectionHeader(L("CHAT MODERATION", "МОДЕРАЦИЯ ЧАТА"));
            GUILayout.Space(4f);
            enableColorCommand = DrawToggle(enableColorCommand, L("Enable /c command (Public)", "Разрешить команду /c"), lobbyControlWidth);
            GUILayout.Space(5);
            blockFortegreenChat = DrawToggle(blockFortegreenChat, L("Block Fortegreen Chat", "Блокировать чат Fortegreen"), lobbyControlWidth);
            GUILayout.Space(5);
            blockRainbowChat = DrawToggle(blockRainbowChat, L("Block Rainbow Chat", "Блокировать радужный чат"), lobbyControlWidth);
            GUILayout.Space(5);
            autoChatEveryone = DrawToggle(autoChatEveryone, L("Chat as Everyone", "Чат от всех"), lobbyControlWidth);
            if (false && autoChatEveryone)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{L("Delay:", "Задержка:")} {autoChatEveryoneDelay:0.0}s", toggleLabelStyle, GUILayout.Width(92));
                autoChatEveryoneDelay = GUILayout.HorizontalSlider(autoChatEveryoneDelay, 0f, 10f, sliderStyle, sliderThumbStyle, GUILayout.Width(170));
                GUILayout.EndHorizontal();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            GUILayout.BeginHorizontal(GUILayout.Width(outerContentWidth));
            GUILayout.BeginVertical(menuCardStyle, GUILayout.Width(lobbyColumnWidth), GUILayout.Height(bottomTileHeight));
            DrawMenuSectionHeader(L("LOBBY ACTIONS", "ДЕЙСТВИЯ ЛОББИ"));
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(L("Spawn Lobby", "Создать лобби"), activeTabStyle, GUILayout.Width(twoButtonWidth), GUILayout.Height(hostActionButtonHeight))) SpawnLobby();
            GUILayout.Space(hostActionGap);
            if (GUILayout.Button(L("Despawn", "Удалить"), btnStyle, GUILayout.Width(twoButtonWidth), GUILayout.Height(hostActionButtonHeight))) DespawnLobby();
            GUILayout.EndHorizontal();
            GUILayout.Space(hostActionGap);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Flood Task All", btnStyle, GUILayout.Width(twoButtonWidth), GUILayout.Height(hostActionButtonHeight))) FloodAllPlayersWithTasks();
            GUILayout.Space(hostActionGap);
            if (GUILayout.Button("Delete Task All", btnStyle, GUILayout.Width(twoButtonWidth), GUILayout.Height(hostActionButtonHeight))) DeleteAllPlayerTasks();
            GUILayout.EndHorizontal();
            GUILayout.Space(hostActionGap);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(L("Kill All", "Убить всех"), btnStyle, GUILayout.Width(threeButtonWidth), GUILayout.Height(hostActionButtonHeight))) KillAll();
            GUILayout.Space(hostActionGap);
            if (GUILayout.Button(L("Kick All", "Кикнуть всех"), btnStyle, GUILayout.Width(threeButtonWidth), GUILayout.Height(hostActionButtonHeight))) KickAll();
            GUILayout.Space(hostActionGap);
            if (GUILayout.Button(L("Mass Morph", "Масс-морф"), btnStyle, GUILayout.Width(threeButtonWidth), GUILayout.Height(hostActionButtonHeight))) this.StartCoroutine(MassMorphCoroutine().WrapToIl2Cpp());
            GUILayout.EndHorizontal();
            GUILayout.Space(hostActionGap);

            selectedLobbyVentIdx = Mathf.Clamp(selectedLobbyVentIdx, 0, Mathf.Max(0, GetVentCount() - 1));
            float ventButtonWidth = Mathf.Min(102f, Mathf.Max(60f, cardInnerWidth * 0.3f));
            string ventText = $"Vent: {GetVentLabel(selectedLobbyVentIdx)}";
            GUIStyle ventLabelStyle = new GUIStyle(toggleLabelStyle) { fontSize = 11, clipping = TextClipping.Clip, alignment = TextAnchor.MiddleLeft };
            float ventSliderWidth = Mathf.Max(70f, cardInnerWidth - ventButtonWidth - hostActionGap);

            GUILayout.BeginHorizontal();
            if (GetVentCount() > 1)
                selectedLobbyVentIdx = Mathf.RoundToInt(GUILayout.HorizontalSlider(selectedLobbyVentIdx, 0, GetVentCount() - 1, sliderStyle, sliderThumbStyle, GUILayout.Width(ventSliderWidth)));
            else
                GUILayout.Space(ventSliderWidth);
            GUILayout.Space(hostActionGap);
            if (GUILayout.Button("Vent TP All", btnStyle, GUILayout.Width(ventButtonWidth), GUILayout.Height(hostActionButtonHeight))) TeleportAllPlayersToVent(selectedLobbyVentIdx);
            GUILayout.EndHorizontal();

            GUILayout.Label(ventText, ventLabelStyle, GUILayout.Width(ventSliderWidth), GUILayout.Height(16f));
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();

            GUILayout.Space(10);

            GUILayout.BeginVertical(menuCardStyle, GUILayout.Width(lobbyColumnWidth), GUILayout.Height(bottomTileHeight));
            DrawMenuSectionHeader(L("END GAME", "КОНЕЦ ИГРЫ"));
            disableEndGameSafeMode = DrawToggle(disableEndGameSafeMode, L("Disable Safe End", "Отключить safe end"), Mathf.RoundToInt(cardInnerWidth));
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(L("Crewmate Win", "Победа экипажа"), btnStyle, GUILayout.Width(twoButtonWidth), GUILayout.Height(hostActionButtonHeight))) SmartEndGame("CrewWin");
            GUILayout.Space(hostActionGap);
            if (GUILayout.Button(L("Impostor Win", "Победа предателей"), btnStyle, GUILayout.Width(twoButtonWidth), GUILayout.Height(hostActionButtonHeight))) SmartEndGame("ImpWin");
            GUILayout.EndHorizontal();
            GUILayout.Space(hostActionGap);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(L("Imp Disconnect", "Дисконнект предателя"), btnStyle, GUILayout.Width(twoButtonWidth), GUILayout.Height(hostActionButtonHeight))) SmartEndGame("ImpDisconnect");
            GUILayout.Space(hostActionGap);
            if (GUILayout.Button(L("H&S Disconnect", "H&S дисконнект"), activeTabStyle, GUILayout.Width(twoButtonWidth), GUILayout.Height(hostActionButtonHeight))) SmartEndGame("HnsImpDisconnect");
            GUILayout.EndHorizontal();
            GUILayout.Space(hostActionGap);

            if (GUILayout.Button(L("Force End", "Завершить"), btnStyle, GUILayout.Width(cardInnerWidth), GUILayout.Height(hostActionButtonHeight))) SmartEndGame("ForceEnd");
            GUILayout.Space(hostActionGap);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(L("Insta Start", "Мгновенный старт"), btnStyle, GUILayout.Width(twoButtonWidth), GUILayout.Height(hostActionButtonHeight))) TryInstaStartAfterEveryoneLoaded(true);
            GUILayout.Space(hostActionGap);
            if (GUILayout.Button(L("Close Meeting", "Закрыть собрание"), btnStyle, GUILayout.Width(twoButtonWidth), GUILayout.Height(hostActionButtonHeight)) && MeetingHud.Instance != null) MeetingHud.Instance.RpcClose();
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

private static void FloodAllPlayersWithTasks()
        {
            if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
            {
                ShowNotification("<color=#FF0000>[TASKS]</color> Host required.");
                return;
            }

            try
            {
                byte[] taskIds = new byte[255];
                for (byte i = 0; i < 255; i++) taskIds[i] = i;

                int changed = 0;
                foreach (PlayerControl player in PlayerControl.AllPlayerControls)
                {
                    if (player == null || player.Data == null || player.Data.Disconnected) continue;
                    ApplyTaskIdsToPlayer(player, taskIds);
                    changed++;
                }

                ShowNotification($"<color=#00FF00>[TASKS]</color> Flood tasks applied to {changed} player(s).");
            }
            catch (Exception)
            {
            }
        }

private static void DeleteAllPlayerTasks()
        {
            if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
            {
                ShowNotification("<color=#FF0000>[TASKS]</color> Host required.");
                return;
            }

            try
            {
                int changed = 0;
                int removed = 0;
                foreach (PlayerControl player in PlayerControl.AllPlayerControls)
                {
                    if (player == null || player.Data == null || player.Data.Disconnected) continue;

                    removed += CountPlayerTasks(player);
                    ApplyTaskIdsToPlayer(player, Array.Empty<byte>());
                    changed++;
                }

                ShowNotification($"<color=#00FF00>[TASKS]</color> Deleted {removed} task(s) for {changed} player(s).");
            }
            catch (Exception)
            {
            }
        }

public static string GetESPNameTag(NetworkedPlayerInfo info, string originalName)
        {
            if (info == null) return originalName;
            bool suppressPlayerInfo = IsMeetingVoteUiActive();
            int currentRoleId = -1;
            try { if (info.Role != null) currentRoleId = (int)info.Role.Role; } catch { }
            string activeLocalName = enableLocalNameSpoof ? customNameInput ?? string.Empty : string.Empty;
            bool rgbEsp = rgbMenuMode || espShimmerMode || IsRainbowEsp(info.PlayerId);
            string activeAccent = (showPlayerInfo || seeKillCooldown || showEspVoteKicks) ? GetEspAccentHex(info.PlayerId) : string.Empty;
            string safeOriginalName = originalName ?? string.Empty;

            if (espNameTagCache.TryGetValue(info.PlayerId, out EspStringCacheEntry nameCache) &&
                Time.unscaledTime < nameCache.ExpiresAt &&
                nameCache.Original == safeOriginalName &&
                nameCache.LocalName == activeLocalName &&
                nameCache.Accent == activeAccent &&
                nameCache.RoleId == currentRoleId &&
                nameCache.SuppressInfo == suppressPlayerInfo &&
                nameCache.Roles == seeRoles &&
                nameCache.PlayerInfo == showPlayerInfo &&
                nameCache.KillCooldown == seeKillCooldown &&
                nameCache.FriendCode == showEspFriendCode &&
                nameCache.VoteKicks == showEspVoteKicks &&
                nameCache.RgbEsp == rgbEsp &&
                nameCache.LocalSpoof == enableLocalNameSpoof)
                return nameCache.Value;

            string newName = safeOriginalName;
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
                string accentHex = GetEspAccentHex(info.PlayerId);
                string espLine = BuildESPInfoLine(info);
                if (!string.IsNullOrWhiteSpace(espLine))
                    newName = $"<size=80%><color=#{accentHex}>{espLine}</color></size>\n{newName}";
            }
            if (showEspVoteKicks && !suppressPlayerInfo)
            {
                int vk = GetVoteKickEspCount(info.PlayerId);
                if (vk > 0)
                {
                    string accentHex = GetEspAccentHex(info.PlayerId);
                    newName = $"<size=82%><color=#{accentHex}>VK:</color> <color=#FFAC1C>{vk}</color></size>\n{newName}";
                }
            }
            if (seeKillCooldown && !suppressPlayerInfo && info.Role != null && info.PlayerId != PlayerControl.LocalPlayer?.PlayerId)
            {
                int roleId = (int)info.Role.Role;
                bool isImpTeam = roleId == 1 || roleId == 5 || roleId == 9 || roleId == 18;
                if (isImpTeam)
                {
                    float rem = GetRemainingKillCooldown(info.PlayerId);
                    string accentHex = GetEspAccentHex(info.PlayerId);
                    string timerText;
                    string timerColor;
                    if (rem <= 0.01f)
                    {
                        timerText = "READY";
                        timerColor = "800020";
                    }
                    else
                    {
                        timerText = $"{rem:F1}s";
                        if (rem >= 35f) timerColor = "006400";
                        else if (rem >= 25f) timerColor = "7CFC00";
                        else if (rem >= 15f) timerColor = "FFD700";
                        else timerColor = "FF3030";
                    }
                    newName = $"<size=78%><color=#{accentHex}>KCD: </color><color=#{timerColor}>{timerText}</color></size>\n{newName}";
                }
            }
            espNameTagCache[info.PlayerId] = new EspStringCacheEntry
            {
                Value = newName,
                Original = safeOriginalName,
                LocalName = activeLocalName,
                Accent = activeAccent,
                RoleId = currentRoleId,
                SuppressInfo = suppressPlayerInfo,
                Roles = seeRoles,
                PlayerInfo = showPlayerInfo,
                KillCooldown = seeKillCooldown,
                FriendCode = showEspFriendCode,
                VoteKicks = showEspVoteKicks,
                RgbEsp = rgbEsp,
                LocalSpoof = enableLocalNameSpoof,
                ExpiresAt = Time.unscaledTime + (rgbEsp ? 0.05f : ((seeKillCooldown || showEspVoteKicks) ? 0.1f : 0.75f))
            };
            return newName;
        }

public static bool IsMeetingVoteUiActive()
        {
            try
            {
                return IsMeetingOrExileActive();
            }
            catch
            {
                return MeetingHud.Instance != null;
            }
        }

public static bool IsMeetingOrExileActive()
        {
            try
            {
                if (MeetingHud.Instance != null)
                {
                    if (MeetingHud.Instance.gameObject == null || MeetingHud.Instance.gameObject.activeInHierarchy)
                        return true;
                }
            }
            catch
            {
                if (MeetingHud.Instance != null)
                    return true;
            }

            try
            {
                if (ExileController.Instance != null)
                    return true;
            }
            catch { }

            return false;
        }

private static bool IsHudModalActive()
        {
            try
            {
                return IsMeetingOrExileActive() || Minigame.Instance != null || IntroCutscene.Instance != null;
            }
            catch
            {
                return IsMeetingOrExileActive();
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
            try
            {
                if (PlayerControl.AllPlayerControls != null)
                {
                    foreach (PlayerControl player in PlayerControl.AllPlayerControls)
                    {
                        if (player != null && player.PlayerId == playerId)
                            return Mathf.Max(0f, player.killTimer);
                    }
                }
            }
            catch { }

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
            }
            catch { }
        }

[HarmonyPatch(typeof(VersionShower), nameof(VersionShower.Start))]
        public static class VersionShower_Start_Patch
        {
            private static string gameVersion = "";

            public static void Postfix(VersionShower __instance)
            {
                if (__instance != null && __instance.text != null)
                    gameVersion = __instance.text.text;
                Paint(__instance);
            }

            public static void Paint(VersionShower ver)
            {
                if (ver == null || ver.text == null) return;
                if (!ElysiumModMenuGUI.showWatermark)
                {
                    ver.text.text = "";
                    return;
                }

                ver.text.fontStyle |= FontStyles.Bold;
                string title = "<b>" + ElysiumModMenuGUI.ApplyMenuShimmer("ElysiumModMenu " + Plugin.PluginVersion) + "</b>";
                ver.text.text = string.IsNullOrWhiteSpace(gameVersion)
                    ? title
                    : title + " <color=#FFFFFF>|</color> " + gameVersion;
            }
        }

[HarmonyPatch(typeof(PingTracker), nameof(PingTracker.Update))]
        public static class PingTracker_Watermark_Patch
        {
            private static float _smoothFps = 0f;
            private static int _smoothPing = 0;
            private static float _updateTimer = 0f;
            private static VersionShower _ver;
            public static void Postfix(PingTracker __instance)
            {
                try
                {
                    if (_ver == null) _ver = UnityEngine.Object.FindObjectOfType<VersionShower>();
                    VersionShower_Start_Patch.Paint(_ver);

                    if (!ElysiumModMenuGUI.showWatermark && !ElysiumModMenuGUI.showWatermarkInfo)
                    {
                        __instance.text.text = "";
                        return;
                    }

                    _updateTimer += Time.deltaTime;
                    if (_updateTimer >= 0.5f) { _smoothFps = 1f / Time.deltaTime; if (AmongUsClient.Instance != null) _smoothPing = AmongUsClient.Instance.Ping; _updateTimer = 0f; }
                    int num = Mathf.RoundToInt(_smoothFps);
                    string pingColor = ((_smoothPing < 80) ? "#00FF00" : ((_smoothPing < 400) ? "#FFFF00" : "#FF0000"));

                    string separator = " <color=#FFFFFF>|</color> ";
                    string finalString = $"<color=#FFFFFF>PING:</color> <color={pingColor}>{_smoothPing} ms</color>{separator}<color=#FFFFFF>FPS:</color> <color=#FFFFFF>{num}</color>";
                    if (ElysiumModMenuGUI.showWatermark)
                    {
                        string shimmerTitle = "<b>" + ElysiumModMenuGUI.ApplyMenuShimmer("ElysiumModMenu " + Plugin.PluginVersion) + "</b>";
                        finalString = $"{shimmerTitle}{separator}" + finalString;
                    }

                    if (AmongUsClient.Instance != null)
                    {
                        ClientData host = AmongUsClient.Instance.GetHost();
                        if (host != null && host.Character != null)
                        {
                            string hostName = host.Character.Data.PlayerName ?? "Unknown";
                            string shimmerHostName = ElysiumModMenuGUI.ApplyMenuShimmer(hostName);
                            finalString += $"{separator}<color=#FFFFFF>Host:</color> {shimmerHostName}";
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
                if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost && ElysiumModMenuGUI.enablePreGameRoleForce)
                {
                    foreach (PlayerControl target in PlayerControl.AllPlayerControls)
                    {
                        if (target == null || target.Data == null || target.Data.Disconnected) continue;
                        if (ElysiumModMenuGUI.TryGetForcedRole(target, out RoleTypes role))
                        {
                            if (target.Data.RoleType != role) target.RpcSetRole(role);
                        }
                        else if (ElysiumModMenuGUI.IsForcedImp(target) && target.Data.Role != null && !target.Data.Role.IsImpostor)
                            target.RpcSetRole(RoleTypes.Impostor);
                    }
                }
            }
        }

        private static bool CanPatchAssignRolesForTeam()
        {
            return false;
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
                        List<byte> allForced = ElysiumModMenuGUI.GetForcedImpostorPlayerIds();

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

                            RoleTypes role = ElysiumModMenuGUI.TryGetForcedRole(targetInfo.Object, out RoleTypes forcedRole)
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
                            if (ElysiumModMenuGUI.TryGetForcedRole(player.Object, out RoleTypes forcedRole) &&
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
                    List<byte> allForced = ElysiumModMenuGUI.GetForcedImpostorPlayerIds();
                    if (ElysiumModMenuGUI.TryGetForcedHideAndSeekSeekerId(out byte seekerId))
                    {
                        if (!allForced.Contains(seekerId))
                            allForced.Insert(0, seekerId);
                    }

                    if (allForced.Count > 0) numImps = allForced.Count;
                    else
                    {
                        if (numImps >= players.Count) numImps = players.Count - 1;
                        if (numImps < 1) numImps = 1;
                    }

                    if (ElysiumModMenuGUI.TryGetForcedHideAndSeekSeekerId(out byte forcedSeekerId))
                        ElysiumModMenuGUI.SetHideAndSeekSeekerOption(forcedSeekerId, numImps);

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

[HarmonyPatch(typeof(LogicOptionsHnS), nameof(LogicOptionsHnS.ImpostorPlayerID))]
        public static class LogicOptionsHnS_ImpostorPlayerID_Patch
        {
            public static void Postfix(ref int __result)
            {
                try
                {
                    if (ElysiumModMenuGUI.TryGetForcedHideAndSeekSeekerId(out byte seekerId))
                        __result = seekerId;
                }
                catch { }
            }
        }

[HarmonyPatch(typeof(LogicOptionsHnS), nameof(LogicOptionsHnS.HasImpostorPlayerID))]
        public static class LogicOptionsHnS_HasImpostorPlayerID_Patch
        {
            public static void Postfix(ref bool __result)
            {
                try
                {
                    if (ElysiumModMenuGUI.TryGetForcedHideAndSeekSeekerId(out _))
                        __result = true;
                }
                catch { }
            }
        }

[HarmonyPatch(typeof(LogicOptionsHnS), nameof(LogicOptionsHnS.ValidateImpostorPlayerID))]
        public static class LogicOptionsHnS_ValidateImpostorPlayerID_Patch
        {
            public static void Postfix(Il2CppSystem.Collections.Generic.List<NetworkedPlayerInfo> players, ref bool __result)
            {
                try
                {
                    if (!ElysiumModMenuGUI.TryGetForcedHideAndSeekSeekerId(out byte seekerId))
                        return;

                    if (players == null)
                    {
                        __result = true;
                        return;
                    }

                    foreach (NetworkedPlayerInfo player in players)
                    {
                        if (player != null && player.PlayerId == seekerId && !player.Disconnected)
                        {
                            __result = true;
                            return;
                        }
                    }
                }
                catch { }
            }
        }

}
}
