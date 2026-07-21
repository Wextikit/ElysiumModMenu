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
private void UpdateRoomPlayers()
        {
            int count = 0;
            try
            {
                foreach (PlayerControl player in lockedPlayersList)
                {
                    try
                    {
                        if (player == null || player == PlayerControl.LocalPlayer || player.Data == null || player.Data.Disconnected || NetworkedClones.IsClone(player))
                            continue;

                        SafePlayerIdentitySnapshot identity;
                        bool hasIdentity = TryGetSafeIdentity(player, out identity);
                        string playerName = Regex.Replace(hasIdentity ? identity.Name : $"Player {player.PlayerId}", "<.*?>", string.Empty);
                        if (playerName.Length > 18) playerName = playerName.Substring(0, 15) + "...";

                        int level = 1;
                        try { level = (int)player.Data.PlayerLevel + 1; } catch { }

                        RoomPlayerActionEntry entry;
                        if (count < roomPlayers.Count)
                        {
                            entry = roomPlayers[count];
                        }
                        else
                        {
                            entry = new RoomPlayerActionEntry();
                            roomPlayers.Add(entry);
                        }

                        entry.ownerId = (int)player.OwnerId;
                        entry.playerName = playerName;
                        entry.friendCode = hasIdentity ? identity.FriendCode : string.Empty;
                        entry.puid = hasIdentity ? identity.Puid : "Unknown";
                        entry.label = $"{playerName}  <color=#777777>Lv:{level}</color>";
                        count++;
                    }
                    catch { }
                }
            }
            catch { }

            if (roomPlayers.Count > count)
                roomPlayers.RemoveRange(count, roomPlayers.Count - count);
        }

private void DrawAntiCheatTab()
        {
            if (Event.current != null && Event.current.type == EventType.Layout)
                UpdateRoomPlayers();

            float outerContentWidth = GetMenuWorkWidth(220f, 760f);
            float cardPaddingWidth = menuCardStyle != null && menuCardStyle.padding != null
                ? menuCardStyle.padding.left + menuCardStyle.padding.right
                : 28f;
            float antiCheatGap = 8f;
            float antiCheatColumnWidth = Mathf.Floor(Mathf.Max(156f, (outerContentWidth - antiCheatGap) / 2f));
            int antiCheatToggleWidth = Mathf.RoundToInt(Mathf.Max(128f, antiCheatColumnWidth - cardPaddingWidth - 8f));
            float antiCheatAvailableHeight = Mathf.Max(330f, windowRect.height - 96f);
            float banListCardHeight = Mathf.Clamp(antiCheatAvailableHeight * 0.36f, 145f, 185f);
            float roomActionsCardHeight = Mathf.Max(132f, antiCheatAvailableHeight - banListCardHeight - 8f);
            float banListScrollHeight = Mathf.Max(82f, banListCardHeight - 100f);
            float roomActionsScrollHeight = Mathf.Max(120f, roomActionsCardHeight - 48f);
            float antiCheatInnerWidth = Mathf.Max(124f, antiCheatColumnWidth - cardPaddingWidth - 8f);

            GUILayout.BeginHorizontal(GUILayout.Width(outerContentWidth));

            GUILayout.BeginVertical(menuCardStyle, GUILayout.Width(antiCheatColumnWidth));

            DrawMenuSectionHeader(L("PUNISHMENT SYSTEM", "СИСТЕМА НАКАЗАНИЙ"));
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            GUILayout.Label(L("Mode:", "Режим:"), toggleLabelStyle, GUILayout.Width(60));

            if (GUILayout.Button("<", btnStyle, GUILayout.Width(25), GUILayout.Height(25)))
            {
                punishmentMode--;
                if (punishmentMode < 0) punishmentMode = punishmentNames.Length - 1;
                settingsDirty = true;
            }

            GUILayout.Label(punishmentNames[punishmentMode], accentValueStyle, GUILayout.ExpandWidth(true), GUILayout.Height(25));

            if (GUILayout.Button(">", btnStyle, GUILayout.Width(25), GUILayout.Height(25)))
            {
                punishmentMode++;
                if (punishmentMode >= punishmentNames.Length) punishmentMode = 0;
                settingsDirty = true;
            }
            GUILayout.EndHorizontal();

            string modeDesc = punishmentMode switch
            {
                0 => "<color=#777777>Null: Пакеты блокируются без действий.</color>",
                1 => "<color=#FFFF00>Warn: Блокировка + Уведомление на экран.</color>",
                2 => "<color=#FF8800>Kick: Игрок будет исключен из лобби.</color>",
                3 => "<color=#FF0000>Ban: Игрок будет забанен (Host Only).</color>",
                _ => ""
            };
            GUILayout.Label(modeDesc, richWrapLabelStyle11);

            GUILayout.Space(12);
            DrawMenuSectionHeader(L("RPC PROTECTIONS", "ЗАЩИТА RPC"));

            blockSpoofRPC = DrawToggle(blockSpoofRPC, L("Block Spoof RPC", "Блокировать spoof RPC"), antiCheatToggleWidth);
            GUILayout.Space(5);
            blockSabotageRPC = DrawToggle(blockSabotageRPC, L("Block Sabotage & Meetings", "Блокировать саботажи и митинги"), antiCheatToggleWidth);
            GUILayout.Space(5);
            blockGameRpcInLobby = DrawToggle(blockGameRpcInLobby, L("Block Game RPC in Lobby", "Блокировать игровые RPC в лобби"), antiCheatToggleWidth);
            GUILayout.Space(5);

            autoBanPlatformSpoof = DrawToggle(autoBanPlatformSpoof, L("Auto-Ban Platform Spoof (Host)", "Авто-бан Platform Spoof (Хост)"), antiCheatToggleWidth);
            GUILayout.Space(5);
            banCustomPlatformsFromTxt = DrawToggle(banCustomPlatformsFromTxt, L("Ban Custom Platforms From TXT", "Бан кастом платформ из TXT"), antiCheatToggleWidth);
            GUILayout.Space(5);

            blockMeetingFloodRpc = DrawToggle(blockMeetingFloodRpc, L("Block Meeting RPC Flood", "Блокировать флуд RPC митинга"), antiCheatToggleWidth);
            GUILayout.Space(5);
            blockChatFloodRpc = DrawToggle(blockChatFloodRpc, L("Block Chat RPC Flood", "Блокировать флуд RPC чата"), antiCheatToggleWidth);
            GUILayout.Space(5);
            overflowProtection = DrawToggle(overflowProtection, "Overflow Protection", antiCheatToggleWidth);
            GUILayout.Space(5);
            enablePasosLimit = DrawToggle(enablePasosLimit, L("RPC Anti-Cheat", "RPC Античит"), antiCheatToggleWidth);
            GUILayout.Space(5);
            oldAntiCheatVersion = DrawToggle(oldAntiCheatVersion, L("anti-cheat old version", "античит старой версии"), antiCheatToggleWidth);
            GUILayout.Space(5);
            banMalformedPacketSender = DrawToggle(banMalformedPacketSender, L("Ban Malformed Sender (Host)", "Бан за кривые пакеты (Хост)"), antiCheatToggleWidth);
            GUILayout.Space(5);
            enableQuickChatEmptyGuard = DrawToggle(enableQuickChatEmptyGuard, L("QuickChat Anti-Crash", "Анти-краш QuickChat"), antiCheatToggleWidth);
            GUILayout.Space(5);
            banQuickChatEmptySpammer = DrawToggle(banQuickChatEmptySpammer, L("Ban QuickChat Spammer (Host)", "Бан за QuickChat спам (Хост)"), antiCheatToggleWidth);
            GUILayout.Space(5);
            GUILayout.Space(15);
            DrawMenuSectionHeader(L("OTHER PROTECTIONS", "ПРОЧАЯ ЗАЩИТА"));

            disableVoteKicks = DrawToggle(disableVoteKicks, L("Disable Vote Kicks (Host)", "Запрет кика голосованием (Хост)"), antiCheatToggleWidth);
            GUILayout.Space(5);
            banVoteKickVoters = DrawToggle(banVoteKickVoters, L("Ban Vote-Kick Voters (Host)", "Бан за vote-kick (Хост)"), antiCheatToggleWidth);
            GUILayout.Space(5);
            blockVentKickExploit = DrawToggle(blockVentKickExploit, L("Block Vent-Kick Exploit", "Блокировать vent kick"), antiCheatToggleWidth);
            GUILayout.Space(5);
            blockServerTeleports = DrawToggle(blockServerTeleports, L("Block Server Teleports", "Блокировать server TP"), antiCheatToggleWidth);
            GUILayout.Space(5);

            autoKickBugs = DrawToggle(autoKickBugs, L("Auto-Kick Fortegreen", "Авто-кик багнутых игроков"), antiCheatToggleWidth);
            if (autoKickBugs)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(L("Timer:", "Таймер:"), toggleLabelStyle, GUILayout.Height(22), GUILayout.Width(62));
                autoKickTimer = GUILayout.HorizontalSlider(autoKickTimer, 1f, 15f, sliderStyle, sliderThumbStyle, GUILayout.Width(112));
                GUILayout.Space(8);
                GUILayout.Label(autoKickTimer.ToString("0.0") + "s", menuBadgeStyle, GUILayout.Width(46), GUILayout.Height(22));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            GUILayout.Space(5);
            autoBanBrokenFriendCode = DrawToggle(autoBanBrokenFriendCode, L("Auto-Ban Broken FriendCode (Host)", "Авто-бан сломанного FriendCode (Хост)"), antiCheatToggleWidth);
            GUILayout.Space(5);
            autoKickLowLevelEnabled = DrawToggle(autoKickLowLevelEnabled, L("Kick Low Level (Host)", "Кик по уровню (Хост)"), antiCheatToggleWidth);
            if (autoKickLowLevelEnabled)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(L("Min level:", "Мин. уровень:"), toggleLabelStyle, GUILayout.Height(22), GUILayout.Width(86));
                int oldMinLevel = autoKickMinLevel;
                autoKickMinLevel = Mathf.Clamp((int)GUILayout.HorizontalSlider(autoKickMinLevel, 1f, 300f, sliderStyle, sliderThumbStyle, GUILayout.Width(112)), 1, 300);
                if (oldMinLevel != autoKickMinLevel) settingsDirty = true;
                GUILayout.Space(8);
                GUILayout.Label(autoKickMinLevel.ToString(), menuBadgeStyle, GUILayout.Width(46), GUILayout.Height(22));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            GUILayout.Space(5);
            banBotsEnabled = DrawToggle(banBotsEnabled, L("Ban Bots (Host)", "Бан ботов (Хост)"), antiCheatToggleWidth);

            GUILayout.EndVertical();
            GUILayout.Space(antiCheatGap);

            GUILayout.BeginVertical(GUILayout.Width(antiCheatColumnWidth), GUILayout.Height(antiCheatAvailableHeight));
            GUILayout.BeginVertical(menuCardStyle, GUILayout.Width(antiCheatColumnWidth), GUILayout.Height(banListCardHeight));
            DrawMenuSectionHeader(L("BAN LIST", "БАН ЛИСТ"));
            autoBanEnabled = DrawToggle(autoBanEnabled, L("Auto-Ban Blacklisted Players", "Авто-бан игроков из списка"), antiCheatToggleWidth);
            GUILayout.Space(5);

            GUILayout.BeginHorizontal(GUILayout.Width(antiCheatInnerWidth));
            string defaultBanText = L("Enter Friend Code", "Введите Friend Code");
            string banValue = string.IsNullOrEmpty(banInput) && !isEditingBan ? defaultBanText : banInput;

            if (DrawPseudoInputButton(banValue, isEditingBan, 25f, 46))
            {
                isEditingBan = !isEditingBan;
                isEditingGhostChatColor = false;
                ResetAllBindWaits();
            }

            GUILayout.Space(6f);
            if (GUILayout.Button(L("ADD", "ДОБАВИТЬ"), btnStyle, GUILayout.Width(56f), GUILayout.Height(25f)))
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

            banListScroll = GUILayout.BeginScrollView(banListScroll, GUILayout.Height(banListScrollHeight));

            if (bannedEntries.Count == 0)
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label($"<color=#777777>{L("Ban list is empty.", "Бан лист пуст.")}</color>", centeredRichLabelStyle);
                GUILayout.FlexibleSpace();
            }
            else
            {
                for (int i = 0; i < bannedEntries.Count; i++)
                {
                    string entry = bannedEntries[i];
                    if (string.IsNullOrWhiteSpace(entry)) continue;

                    string disp = i < bannedEntryLabels.Count ? bannedEntryLabels[i] : entry;

                    GUILayout.BeginHorizontal(boxStyle);
                    GUILayout.Label(disp, labelStyle12, GUILayout.Width(185));
                    GUILayout.FlexibleSpace();

                    bool removedEntry = false;
                    if (GUILayout.Button("X", redCrossStyle, GUILayout.Width(25), GUILayout.Height(22)))
                    {
                        RemoveFromBanList(entry);
                        removedEntry = true;
                    }
                    GUILayout.EndHorizontal();
                    if (removedEntry) break;
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.Space(8f);
            GUILayout.BeginVertical(menuCardStyle, GUILayout.Width(antiCheatColumnWidth), GUILayout.Height(roomActionsCardHeight));
            DrawMenuSectionHeader("BAN / KICK PLAYER");
            GUILayout.Space(4f);

            roomPlayerActionsScroll = GUILayout.BeginScrollView(roomPlayerActionsScroll, false, true, GUIStyle.none, GUI.skin.verticalScrollbar, GUIStyle.none, GUILayout.Height(roomActionsScrollHeight));
            foreach (RoomPlayerActionEntry player in roomPlayers)
            {
                GUILayout.BeginHorizontal(boxStyle);
                GUILayout.Label(player.label, richLabelStyle11, GUILayout.ExpandWidth(true));

                bool previousEnabled = GUI.enabled;
                bool canHostAction = AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost;
                GUI.enabled = previousEnabled && canHostAction;

                if (GUILayout.Button("WL", btnStyle, GUILayout.Width(34f), GUILayout.Height(22f)))
                {
                    AddToLobbyWhitelist(player.friendCode, player.puid, player.playerName);
                }

                GUI.enabled = previousEnabled && AmongUsClient.Instance != null;
                if (GUILayout.Button("KICK", btnStyle, GUILayout.Width(48f), GUILayout.Height(22f)))
                {
                    try
                    {
                        KickRoomPlayer(player.ownerId, player.playerName);
                    }
                    catch { }
                }

                GUI.enabled = previousEnabled && canHostAction;
                if (GUILayout.Button("BAN", banButtonStyle, GUILayout.Width(45f), GUILayout.Height(22f)))
                {
                    try
                    {
                        string banKey = !string.IsNullOrWhiteSpace(player.friendCode)
                            ? player.friendCode
                            : (!string.IsNullOrWhiteSpace(player.puid) ? "PUID:" + player.puid : "Client:" + player.ownerId);

                        AddToBanList(banKey, string.IsNullOrWhiteSpace(player.puid) ? "Unknown" : player.puid,
                            player.playerName, "Manual room ban");
                        AmongUsClient.Instance.KickPlayer(player.ownerId, true);
                        ShowNotification($"<color=#FF4444>[BAN]</color> {player.playerName}");
                    }
                    catch { }
                }

                GUI.enabled = previousEnabled;
                GUILayout.EndHorizontal();
            }

            if (roomPlayers.Count == 0)
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("<color=#777777>No players in the room.</color>",
                    centeredRichLabelStyle);
                GUILayout.FlexibleSpace();
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

private static bool HasServerAnticheat()
        {
            try
            {
                if (AmongUsClient.Instance == null || PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data == null) return false;
                return AmongUsClient.Instance.NetworkMode == NetworkModes.OnlineGame && !Constants.IsVersionModded() && PlayerControl.LocalPlayer.Data.OwnerId != -2;
            }
            catch { return false; }
        }

private static void KickRoomPlayer(int ownerId, string name)
        {
            if (AmongUsClient.Instance == null) return;

            if (AmongUsClient.Instance.AmHost)
            {
                AmongUsClient.Instance.KickPlayer(ownerId, false);
                ShowNotification($"<color=#FFAA33>[KICK]</color> {name}");
                return;
            }

            if (ownerId == AmongUsClient.Instance.HostId)
            {
                ShowNotification("<color=#FF4444>[KICK]</color> Host can't be kicked.");
                return;
            }

            if (ShipStatus.Instance == null || PlayerControl.LocalPlayer == null)
            {
                ShowNotification("<color=#FF4444>[KICK]</color> Match must be started.");
                return;
            }

            if (!HasServerAnticheat())
            {
                ShowNotification("<color=#FF4444>[KICK]</color> Server anticheat is required.");
                return;
            }

            MessageWriter batch = null;
            MessageWriter msg = null;
            MessageWriter msg2 = null;
            try
            {
                batch = MessageWriter.Get(SendOption.Reliable);
                batch.StartMessage(InnerNet.Tags.GameDataTo);
                batch.Write(AmongUsClient.Instance.GameId);
                batch.WritePacked(ownerId);

                msg = MessageWriter.Get(SendOption.Reliable);
                msg.Write((ushort)0);
                msg.Write((byte)VentilationSystem.Operation.Enter);
                msg.Write((byte)0);
                WriteVentUpdate(batch, msg);

                msg2 = MessageWriter.Get(SendOption.Reliable);
                msg2.Write((ushort)1);
                msg2.Write((byte)VentilationSystem.Operation.BootImpostors);
                msg2.Write((byte)0);
                WriteVentUpdate(batch, msg2);

                batch.EndMessage();
                AmongUsClient.Instance.SendOrDisconnect(batch);
                ShowNotification($"<color=#FFAA33>[KICK]</color> {name}");
            }
            catch
            {
                ShowNotification("<color=#FF4444>[KICK]</color> Failed.");
            }
            finally
            {
                try { msg?.Recycle(); } catch { }
                try { msg2?.Recycle(); } catch { }
                try { batch?.Recycle(); } catch { }
            }
        }

private static void WriteVentUpdate(MessageWriter batch, MessageWriter msg)
        {
            batch.StartMessage((byte)GameDataTypes.RpcFlag);
            batch.WritePacked(ShipStatus.Instance.NetId);
            batch.Write((byte)RpcCalls.UpdateSystem);
            batch.Write((byte)SystemTypes.Ventilation);
            batch.WriteNetObject(PlayerControl.LocalPlayer);
            batch.Write(msg, false);
            batch.EndMessage();
        }
    }
}
