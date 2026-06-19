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

private struct FavoriteOutfitSnapshot
        {
            public int ColorId;
            public string HatId;
            public string SkinId;
            public string VisorId;
            public string NamePlateId;
            public string PetId;

            public FavoriteOutfitSnapshot(int colorId, string hatId, string skinId, string visorId, string namePlateId, string petId)
            {
                ColorId = colorId;
                HatId = hatId ?? string.Empty;
                SkinId = skinId ?? string.Empty;
                VisorId = visorId ?? string.Empty;
                NamePlateId = namePlateId ?? string.Empty;
                PetId = petId ?? string.Empty;
            }
        }

private void DrawOutfitsTab()
        {
            GUILayout.BeginVertical(menuCardStyle);
            DrawMenuSectionHeader(L("FAVORITE OUTFITS", "ИЗБРАННЫЕ ОБРАЗЫ"));

            PlayerControl selected = SelectedOutfitSourcePlayer();
            for (int i = 0; i < FavoriteOutfitSlotCount; i++)
            {
                bool hasOutfit = TryDeserializeFavoriteOutfit(favoriteOutfitSlots[i], out FavoriteOutfitSnapshot outfit);
                GUILayout.BeginVertical();

                GUILayout.BeginHorizontal();
                GUILayout.Label($"{L("Slot", "Слот")} {i + 1}", toggleLabelStyle, GUILayout.Width(52), GUILayout.Height(22));
                GUILayout.Label(hasOutfit ? FavoriteOutfitSummary(outfit) : L("Empty", "Пусто"), new GUIStyle(GUI.skin.label) { fontSize = 11, clipping = TextClipping.Clip, alignment = TextAnchor.MiddleLeft }, GUILayout.ExpandWidth(true), GUILayout.Height(22));
                GUI.enabled = hasOutfit;
                if (GUILayout.Button(L("Apply", "Надеть"), btnStyle, GUILayout.Width(58), GUILayout.Height(22)))
                    ApplyFavoriteOutfitSlot(i, outfit, hasOutfit);
                GUI.enabled = true;
                if (GUILayout.Button("X", btnStyle, GUILayout.Width(28), GUILayout.Height(22)))
                    ClearFavoriteOutfitSlot(i);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Space(52);
                if (GUILayout.Button(L("Save Mine", "Сохр. мой"), btnStyle, GUILayout.Width(100), GUILayout.Height(22)))
                    SaveFavoriteOutfitSlot(i, PlayerControl.LocalPlayer);
                if (GUILayout.Button(L("Save Selected", "Сохр. выбран"), btnStyle, GUILayout.Width(120), GUILayout.Height(22)))
                    SaveFavoriteOutfitSlot(i, selected);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.EndVertical();
                GUILayout.Space(4);
            }

            GUILayout.Space(12);
            DrawMenuSectionHeader("COPY OUTFIT FROM LOBBY");

            List<PlayerControl> outfitPlayers = GetLobbyOutfitPlayers();
            if (outfitPlayers.Count > 0)
            {
                foreach (PlayerControl pc in outfitPlayers)
                {
                    if (pc == null || pc.Data == null || pc.Data.DefaultOutfit == null) continue;

                    GUILayout.BeginHorizontal(boxStyle, GUILayout.Height(36));
                    try
                    {
                        string pName = CleanOutfitPlayerName(pc.Data.PlayerName ?? "Unknown");
                        string colorName = SafeColorName(pc.Data.DefaultOutfit.ColorId);
                        GUILayout.Label(pName, new GUIStyle(toggleLabelStyle) { alignment = TextAnchor.MiddleLeft, clipping = TextClipping.Clip }, GUILayout.ExpandWidth(true), GUILayout.Height(30));
                        GUILayout.Label(colorName, new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleRight, clipping = TextClipping.Clip, fontSize = 11 }, GUILayout.Width(105), GUILayout.Height(30));

                        if (GUILayout.Button("Copy Outfit", btnStyle, GUILayout.Width(120), GUILayout.Height(30)))
                            CopyOutfitFromPlayer(pc);
                    }
                    finally { GUILayout.EndHorizontal(); }
                    GUILayout.Space(4);
                }
            }
            else
            {
                GUILayout.Label("<color=#777777>Нет игроков для копирования.</color>");
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical(menuCardStyle);
            DrawMenuSectionHeader(L("RAINBOW \u2014 FREE COLORS (NO HOST)", "РАДУГА \u2014 СВОБОДНЫЕ ЦВЕТА (БЕЗ ХОСТА)"));

            bool prevFreeRainbow = localRainbowFreeOnly;
            localRainbowFreeOnly = DrawToggle(localRainbowFreeOnly, L("Rainbow (only free colors)", "Радуга (только свободные цвета)"), 260);
            if (localRainbowFreeOnly && !prevFreeRainbow) localRainbow = false;

            GUILayout.Space(4);
            GUILayout.Label(L("Cycles your color only through colors that are free in the room, so the host anti-cheat will not ban you for taking an occupied color.", "Переливает ваш цвет только по свободным цветам в комнате, чтобы анти-чит хоста не забанил за занятый цвет."), menuDescStyle);

            GUILayout.Space(12);

            var freeColors = GetFreeColorIds();
            int freeCount = freeColors.Count;
            int clampedFreeIdx = freeCount > 0 ? Mathf.Clamp(selectedFreeColorIndex, 0, freeCount - 1) : 0;
            string freeColorLabel = freeCount > 0 ? SafeColorName(freeColors[clampedFreeIdx]) : L("none", "нет");

            GUILayout.Label(L("Pick a free color:", "Выберите свободный цвет:"), new GUIStyle(toggleLabelStyle) { fontStyle = FontStyle.Bold });
            GUILayout.Space(6);

            GUILayout.BeginHorizontal();
            GUI.enabled = freeCount > 0;

            Color prevSwatchCol = GUI.color;
            if (freeCount > 0) { try { GUI.color = Palette.PlayerColors[freeColors[clampedFreeIdx]]; } catch { } }
            bool swatchClicked = GUILayout.Button(GUIContent.none, menuSwatchSquareStyle, GUILayout.Width(28), GUILayout.Height(28));
            GUI.color = prevSwatchCol;
            if (swatchClicked && freeCount > 0) selectedFreeColorIndex = (selectedFreeColorIndex + 1) % freeCount;

            GUILayout.Space(10);
            GUILayout.Label(freeColorLabel, new GUIStyle(toggleLabelStyle) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold, fontSize = 13 }, GUILayout.Height(28));

            GUILayout.FlexibleSpace();
            GUI.enabled = true;
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUI.enabled = freeCount > 0;
            if (GUILayout.Button(L("Apply free color", "Применить свободный цвет"), btnStyle, GUILayout.Height(32)))
            {
                if (freeCount > 0 && PlayerControl.LocalPlayer != null)
                {
                    int pick = freeColors[Mathf.Clamp(selectedFreeColorIndex, 0, freeCount - 1)];
                    PlayerControl.LocalPlayer.CmdCheckColor((byte)pick);
                    ShowNotification(L("Applied free color: ", "Применён свободный цвет: ") + SafeColorName(pick));
                }
            }
            GUI.enabled = true;

            GUILayout.Space(8);
            GUILayout.Label(L("Free colors right now: ", "Свободных цветов сейчас: ") + freeCount, menuDescStyle);

            GUILayout.EndVertical();
        }

private static PlayerControl SelectedOutfitSourcePlayer()
        {
            try
            {
                if (lockedPlayersList != null)
                {
                    foreach (PlayerControl pc in lockedPlayersList)
                    {
                        if (pc != null && pc != PlayerControl.LocalPlayer && pc.Data != null && !pc.Data.Disconnected)
                            return pc;
                    }
                }
            }
            catch { }

            return PlayerControl.LocalPlayer;
        }

private static List<PlayerControl> GetLobbyOutfitPlayers()
        {
            List<PlayerControl> players = new List<PlayerControl>();
            try
            {
                if (PlayerControl.AllPlayerControls == null)
                    return players;

                foreach (PlayerControl pc in PlayerControl.AllPlayerControls)
                {
                    if (pc == null || pc == PlayerControl.LocalPlayer || pc.Data == null || pc.Data.Disconnected || pc.Data.DefaultOutfit == null)
                        continue;

                    players.Add(pc);
                }
            }
            catch { }

            return players.OrderBy(p => p.PlayerId).ToList();
        }

private static void CopyOutfitFromPlayer(PlayerControl source)
        {
            try
            {
                if (PlayerControl.LocalPlayer == null || source == null || source.Data == null || source.Data.DefaultOutfit == null)
                    return;

                var outfit = source.Data.DefaultOutfit;
                PlayerControl.LocalPlayer.RpcSetSkin(outfit.SkinId);
                PlayerControl.LocalPlayer.RpcSetHat(outfit.HatId);
                PlayerControl.LocalPlayer.RpcSetVisor(outfit.VisorId);
                PlayerControl.LocalPlayer.RpcSetNamePlate(outfit.NamePlateId);
                PlayerControl.LocalPlayer.RpcSetPet(outfit.PetId);
                ShowNotification($"<color=#00FFAA>[OUTFIT]</color> Copied {CleanOutfitPlayerName(source.Data.PlayerName ?? "player")}");
            }
            catch { }
        }

private static string CleanOutfitPlayerName(string value)
        {
            try
            {
                string clean = Regex.Replace(value ?? string.Empty, "<.*?>", string.Empty).Trim();
                return string.IsNullOrWhiteSpace(clean) ? "Unknown" : clean;
            }
            catch { return "Unknown"; }
        }

public static void ApplyLevelSpoofValue(uint displayLevel, bool save = true)
        {
            CaptureLevelSpoofRestoreLevel(displayLevel);
            uint targetLevel = displayLevel > 0 ? displayLevel - 1 : 0;
            SetStoredLevelRaw(targetLevel, save);
        }

        private static void SetStoredLevelRaw(uint rawLevel, bool save = true)
        {
            try
            {
                AmongUs.Data.DataManager.Player.stats.level = rawLevel;
            }
            catch
            {
                try { AmongUs.Data.DataManager.Player.Stats.Level = rawLevel; }
                catch { }
            }

            if (save)
            {
                try { AmongUs.Data.DataManager.Player.Save(); } catch { }
            }
        }

        private static bool TryGetStoredLevelRaw(out uint rawLevel)
        {
            rawLevel = 0;
            try
            {
                rawLevel = AmongUs.Data.DataManager.Player.stats.level;
                return true;
            }
            catch
            {
                try
                {
                    rawLevel = AmongUs.Data.DataManager.Player.Stats.Level;
                    return true;
                }
                catch { return false; }
            }
        }

        private static void CaptureLevelSpoofRestoreLevel(uint displayLevel)
        {
            if (hasLevelSpoofRestoreLevel)
                return;

            if (PlayerPrefs.HasKey("M_LevelSpoofRestoreLevel"))
            {
                levelSpoofRestoreLevel = (uint)Mathf.Max(0, PlayerPrefs.GetInt("M_LevelSpoofRestoreLevel"));
                hasLevelSpoofRestoreLevel = true;
                return;
            }

            if (!TryGetStoredLevelRaw(out uint currentRaw))
                currentRaw = 0;

            uint spoofRaw = displayLevel > 0 ? displayLevel - 1 : 0;
            levelSpoofRestoreLevel = currentRaw == spoofRaw ? 0u : currentRaw;
            hasLevelSpoofRestoreLevel = true;
            PlayerPrefs.SetInt("M_LevelSpoofRestoreLevel", (int)Mathf.Min(levelSpoofRestoreLevel, int.MaxValue));
            PlayerPrefs.Save();
        }

        public static void RestoreLevelSpoofDefault()
        {
            uint restoreRaw = 0;
            if (hasLevelSpoofRestoreLevel)
                restoreRaw = levelSpoofRestoreLevel;
            else if (PlayerPrefs.HasKey("M_LevelSpoofRestoreLevel"))
                restoreRaw = (uint)Mathf.Max(0, PlayerPrefs.GetInt("M_LevelSpoofRestoreLevel"));

            SetStoredLevelRaw(restoreRaw);
            hasLevelSpoofRestoreLevel = false;
            levelSpoofRestoreLevel = 0;
            PlayerPrefs.DeleteKey("M_LevelSpoofRestoreLevel");
            PlayerPrefs.Save();
        }

        private static int MaxOutfitColorId()
        {
            try { return Palette.PlayerColors != null ? Mathf.Max(0, Palette.PlayerColors.Length - 1) : 18; }
            catch { return 18; }
        }

        private static bool TryCaptureFavoriteOutfit(PlayerControl source, out FavoriteOutfitSnapshot outfit)
        {
            outfit = default;
            try
            {
                if (source == null || source.Data == null || source.Data.DefaultOutfit == null) return false;
                var sourceOutfit = source.Data.DefaultOutfit;
                outfit = new FavoriteOutfitSnapshot(
                    Mathf.Clamp(sourceOutfit.ColorId, 0, MaxOutfitColorId()),
                    sourceOutfit.HatId,
                    sourceOutfit.SkinId,
                    sourceOutfit.VisorId,
                    sourceOutfit.NamePlateId,
                    sourceOutfit.PetId);
                return true;
            }
            catch { }

            return false;
        }

        private static void ApplyFavoriteOutfit(PlayerControl target, FavoriteOutfitSnapshot outfit)
        {
            if (target == null) return;
            target.RpcSetColor((byte)Mathf.Clamp(outfit.ColorId, 0, MaxOutfitColorId()));
            target.RpcSetSkin(outfit.SkinId ?? string.Empty);
            target.RpcSetHat(outfit.HatId ?? string.Empty);
            target.RpcSetVisor(outfit.VisorId ?? string.Empty);
            target.RpcSetNamePlate(outfit.NamePlateId ?? string.Empty);
            target.RpcSetPet(outfit.PetId ?? string.Empty);
        }

        private static string SerializeFavoriteOutfit(FavoriteOutfitSnapshot outfit)
        {
            return string.Join("\t", new[]
            {
                Mathf.Clamp(outfit.ColorId, 0, MaxOutfitColorId()).ToString(),
                CleanFavoriteOutfitPart(outfit.HatId),
                CleanFavoriteOutfitPart(outfit.SkinId),
                CleanFavoriteOutfitPart(outfit.VisorId),
                CleanFavoriteOutfitPart(outfit.NamePlateId),
                CleanFavoriteOutfitPart(outfit.PetId)
            });
        }

        private static bool TryDeserializeFavoriteOutfit(string value, out FavoriteOutfitSnapshot outfit)
        {
            outfit = default;
            if (string.IsNullOrWhiteSpace(value)) return false;

            string[] parts = value.Split('\t');
            if (parts.Length < 6 || !int.TryParse(parts[0], out int colorId)) return false;

            outfit = new FavoriteOutfitSnapshot(Mathf.Clamp(colorId, 0, MaxOutfitColorId()), parts[1], parts[2], parts[3], parts[4], parts[5]);
            return true;
        }

        private static string CleanFavoriteOutfitPart(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Replace("\t", " ").Replace("\r", " ").Replace("\n", " ").Trim();
        }

        private static string FavoriteOutfitSummary(FavoriteOutfitSnapshot outfit)
        {
            string color = "Color " + outfit.ColorId;
            try { color = Palette.GetColorName(outfit.ColorId); } catch { }
            return $"{color} | {ShortOutfitId(outfit.HatId)}";
        }

        private static string ShortOutfitId(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "-";
            string cleaned = value.Trim();
            return cleaned.Length <= 10 ? cleaned : cleaned.Substring(0, 10);
        }

        private void SaveFavoriteOutfitSlot(int index, PlayerControl source)
        {
            if (index < 0 || index >= favoriteOutfitSlots.Length) return;
            if (!TryCaptureFavoriteOutfit(source, out FavoriteOutfitSnapshot outfit))
            {
                ShowNotification($"<color=#FF4444>[OUTFIT]</color> {L("Player outfit is not ready.", "Образ игрока еще не готов.")}");
                return;
            }

            favoriteOutfitSlots[index] = SerializeFavoriteOutfit(outfit);
            SaveConfig();
            ShowNotification($"<color=#00FFAA>[OUTFIT]</color> {L("Saved slot", "Сохранен слот")} {index + 1}");
        }

        private void ApplyFavoriteOutfitSlot(int index, FavoriteOutfitSnapshot outfit, bool hasOutfit)
        {
            if (!hasOutfit)
            {
                ShowNotification($"<color=#FFAA00>[OUTFIT]</color> {L("Slot is empty.", "Слот пуст.")}");
                return;
            }

            try
            {
                ApplyFavoriteOutfit(PlayerControl.LocalPlayer, outfit);
                ShowNotification($"<color=#00FFAA>[OUTFIT]</color> {L("Applied slot", "Надет слот")} {index + 1}");
            }
            catch { }
        }

        private void ClearFavoriteOutfitSlot(int index)
        {
            if (index < 0 || index >= favoriteOutfitSlots.Length) return;
            favoriteOutfitSlots[index] = string.Empty;
            SaveConfig();
            ShowNotification($"<color=#AAAAAA>[OUTFIT]</color> {L("Cleared slot", "Очищен слот")} {index + 1}");
        }
        public static bool removePenalty = true;

public static bool alwaysShowLobbyTimer = false;

public static bool enableChatLog = true;

public static bool enableFastChat = true;

public static bool allowLinksAndSymbols = false;

private static readonly System.Collections.Generic.Dictionary<string, Sprite> CachedSprites = new();
}
}
