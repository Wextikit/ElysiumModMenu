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

[HarmonyPatch(typeof(ChatController), nameof(ChatController.AddChat))]
public static class ChatController_AddChat_Patch
{
    [HarmonyPriority(Priority.First)]
    public static bool Prefix(PlayerControl sourcePlayer, ref string chatText, bool censor, ChatController __instance)
    {
        if (string.IsNullOrEmpty(chatText)) return true;
        ElysiumModMenuGUI.AddPortableChatLog(sourcePlayer, chatText);
        string lowerText = chatText.ToLower().Trim();

        if (ElysiumModMenuGUI.enableColorCommand && sourcePlayer != null)
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
                    else colorId = ElysiumModMenuGUI.GetColorIdByName(colorInput);

                    if (colorId != -1)
                    {
                        if (colorId == 18 && ElysiumModMenuGUI.blockFortegreenChat)
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
                    if (ElysiumModMenuGUI.blockRainbowChat)
                    {
                        if (HudManager.Instance?.Chat != null)
                            HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, "<color=#FF0000>[ОШИБКА]</color> Радуга запрещена хостом!");
                    }
                    else
                    {
                        if (ElysiumModMenuGUI.rainbowPlayers.Contains(sourcePlayer.PlayerId))
                        {
                            ElysiumModMenuGUI.rainbowPlayers.Remove(sourcePlayer.PlayerId);
                            ElysiumModMenuGUI.ShowNotification("<color=#FF00FF>[SERVER]</color> Rainbow OFF.");
                        }
                        else
                        {
                            ElysiumModMenuGUI.rainbowPlayers.Add(sourcePlayer.PlayerId);
                            ElysiumModMenuGUI.ShowNotification("<color=#FF00FF>[SERVER]</color> Rainbow ON.");
                        }
                    }
                }
                return false;
            }
        }

        if (ShouldShowGhostMessage(sourcePlayer))
        {
            return ShowGhostMessage(sourcePlayer, chatText, censor, __instance);
        }

        return true;
    }

    private static bool ShouldShowGhostMessage(PlayerControl sourcePlayer)
    {
        try
        {
            if (!ElysiumModMenuGUI.readGhostChat && !ElysiumModMenuGUI.seeGhosts) return false;
            if (sourcePlayer == null || sourcePlayer.Data == null) return false;
            if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data == null) return false;
            if (PlayerControl.LocalPlayer.Data.IsDead) return false;

            return sourcePlayer.Data.IsDead;
        }
        catch { return false; }
    }

    private static bool ShowGhostMessage(PlayerControl sourcePlayer, string chatText, bool censor, ChatController chat)
    {
        if (chat == null) return true;

        ChatBubble pooledBubble = null;
        try
        {
            NetworkedPlayerInfo sourceData = sourcePlayer.Data;
            if (sourceData == null) return true;

            pooledBubble = chat.GetPooledBubble();
            pooledBubble.transform.SetParent(chat.scroller.Inner);
            pooledBubble.transform.localScale = Vector3.one;

            bool isLocal = sourcePlayer == PlayerControl.LocalPlayer;
            if (isLocal) pooledBubble.SetRight();
            else pooledBubble.SetLeft();

            bool didVote = MeetingHud.Instance != null && MeetingHud.Instance.DidVote(sourcePlayer.PlayerId);
            pooledBubble.SetCosmetics(sourceData);
            chat.SetChatBubbleName(pooledBubble, sourceData, sourceData.IsDead, didVote, PlayerNameColor.Get(sourceData), null);

            if (censor && AmongUs.Data.DataManager.Settings.Multiplayer.CensorChat)
            {
                chatText = BlockedWords.CensorWords(chatText, false);
            }

            pooledBubble.SetText(ElysiumModMenuGUI.RenderGhostChatMessageText(chatText));
            pooledBubble.AlignChildren();
            chat.AlignAllBubbles();

            if (!chat.IsOpenOrOpening && chat.notificationRoutine == null)
            {
                chat.notificationRoutine = chat.StartCoroutine(chat.BounceDot());
            }

            if (!isLocal && !chat.IsOpenOrOpening)
            {
                SoundManager.Instance.PlaySound(chat.messageSound, false).pitch = 0.5f + sourcePlayer.PlayerId / 15f;
                chat.chatNotification.SetUp(sourcePlayer, chatText);
            }

            return false;
        }
        catch
        {
            try
            {
                if (pooledBubble != null) chat.chatBubblePool.Reclaim(pooledBubble);
            }
            catch { }
            return true;
        }
    }
}
