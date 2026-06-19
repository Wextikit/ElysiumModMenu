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

public void Update()
        {
            TickFakeStartCounter();

            bool isTypingOrBinding = isEditingName || isEditingLevel || isEditingFriendCode || isEditingLocalFriendCode || isEditingGhostChatColor || isEditingBan || customChatInputFocused ||
                                     isWaitingForBind || isWaitBindMassMorph || isWaitBindSpawnLobby ||
                                     isWaitBindDespawnLobby || isWaitBindCloseMeeting || isWaitBindInstaStart ||
                                     isWaitBindEndCrew || isWaitBindEndImp || isWaitBindEndImpDC || isWaitBindEndHnsDC ||
                                     isWaitBindMagnetCursor || isWaitBindToggleTracers || isWaitBindToggleNoClip ||
                                     isWaitBindToggleFreecam || isWaitBindToggleCameraZoom || isWaitBindKillAll ||
                                     isWaitBindCallMeeting || isWaitBindTogglePlayerInfo || isWaitBindToggleSeeRoles ||
                                     isWaitBindToggleSeeGhosts || isWaitBindToggleFullBright || isWaitBindKickAll ||
                                     isWaitBindFixSabotages || isWaitBindSetAllGhost || isWaitBindSetAllGhostImp ||
                                     isWaitBindReviveAll;

            KeyCode activeMenuKey = menuToggleKey == KeyCode.None ? KeyCode.Insert : menuToggleKey;
            if (!isTypingOrBinding && Input.GetKeyDown(activeMenuKey))
            {
                showMenu = !showMenu;
                if (!showMenu) SaveConfig();
            }

            if (!isTypingOrBinding)
            {
                if (bindMassMorph != KeyCode.None && Input.GetKeyDown(bindMassMorph))
                {
                    if (CanRunHostBind("Mass Morph"))
                        this.StartCoroutine(MassMorphCoroutine().WrapToIl2Cpp());
                }

                if (bindSpawnLobby != KeyCode.None && Input.GetKeyDown(bindSpawnLobby))
                {
                    if (CanRunHostBind("Spawn Lobby")) SpawnLobby();
                }

                if (bindDespawnLobby != KeyCode.None && Input.GetKeyDown(bindDespawnLobby))
                {
                    if (CanRunHostBind("Despawn Lobby")) DespawnLobby();
                }

                if (bindCloseMeeting != KeyCode.None && Input.GetKeyDown(bindCloseMeeting))
                {
                    if (CanRunHostBind("Close Meeting") && MeetingHud.Instance != null)
                        MeetingHud.Instance.RpcClose();
                }

                if (bindInstaStart != KeyCode.None && Input.GetKeyDown(bindInstaStart) && CanRunHostBind("Insta Start") && GameStartManager.Instance != null)
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
                if (bindEndCrew != KeyCode.None && Input.GetKeyDown(bindEndCrew) && CanRunHostBind("End: Crewmate Win")) SmartEndGame("CrewWin");
                if (bindEndImp != KeyCode.None && Input.GetKeyDown(bindEndImp) && CanRunHostBind("End: Impostor Win")) SmartEndGame("ImpWin");
                if (bindEndImpDC != KeyCode.None && Input.GetKeyDown(bindEndImpDC) && CanRunHostBind("End: Imp Disconnect")) SmartEndGame("ImpDisconnect");
                if (bindEndHnsDC != KeyCode.None && Input.GetKeyDown(bindEndHnsDC) && CanRunHostBind("End: H&S Disconnect")) SmartEndGame("HnsImpDisconnect");
                if (bindToggleTracers != KeyCode.None && Input.GetKeyDown(bindToggleTracers))
                {
                    showTracers = !showTracers;
                    ShowNotification(showTracers ? "<color=#00FF00>[TRACERS]</color> ON" : "<color=#FF0000>[TRACERS]</color> OFF");
                }
                if (bindToggleNoClip != KeyCode.None && Input.GetKeyDown(bindToggleNoClip))
                {
                    noClip = !noClip;
                    ShowNotification(noClip ? "<color=#00FF00>[NOCLIP]</color> ON" : "<color=#FF0000>[NOCLIP]</color> OFF");
                }
                if (bindToggleFreecam != KeyCode.None && Input.GetKeyDown(bindToggleFreecam))
                {
                    freecam = !freecam;
                    ShowNotification(freecam ? "<color=#00FF00>[FREECAM]</color> ON" : "<color=#FF0000>[FREECAM]</color> OFF");
                }
                if (bindToggleCameraZoom != KeyCode.None && Input.GetKeyDown(bindToggleCameraZoom))
                {
                    cameraZoom = !cameraZoom;
                    ShowNotification(cameraZoom ? "<color=#00FF00>[ZOOM]</color> ON" : "<color=#FF0000>[ZOOM]</color> OFF");
                }
                if (bindTogglePlayerInfo != KeyCode.None && Input.GetKeyDown(bindTogglePlayerInfo))
                {
                    showPlayerInfo = !showPlayerInfo;
                    ShowNotification(showPlayerInfo ? "<color=#00FF00>[PLAYER INFO]</color> ON" : "<color=#FF0000>[PLAYER INFO]</color> OFF");
                }
                if (bindToggleSeeRoles != KeyCode.None && Input.GetKeyDown(bindToggleSeeRoles))
                {
                    seeRoles = !seeRoles;
                    ShowNotification(seeRoles ? "<color=#00FF00>[ROLES]</color> ON" : "<color=#FF0000>[ROLES]</color> OFF");
                }
                if (bindToggleSeeGhosts != KeyCode.None && Input.GetKeyDown(bindToggleSeeGhosts))
                {
                    seeGhosts = !seeGhosts;
                    ShowNotification(seeGhosts ? "<color=#00FF00>[GHOSTS]</color> ON" : "<color=#FF0000>[GHOSTS]</color> OFF");
                }
                if (bindToggleFullBright != KeyCode.None && Input.GetKeyDown(bindToggleFullBright))
                {
                    fullBright = !fullBright;
                    ShowNotification(fullBright ? "<color=#00FF00>[FULL BRIGHT]</color> ON" : "<color=#FF0000>[FULL BRIGHT]</color> OFF");
                }
                if (bindKillAll != KeyCode.None && Input.GetKeyDown(bindKillAll) && CanRunHostBind("Kill All")) KillAll();
                if (bindCallMeeting != KeyCode.None && Input.GetKeyDown(bindCallMeeting) && CanRunHostBind("Call Meeting")) callMeetingPublic();
                if (bindKickAll != KeyCode.None && Input.GetKeyDown(bindKickAll) && CanRunHostBind("Kick All")) KickAll();
                if (bindFixSabotages != KeyCode.None && Input.GetKeyDown(bindFixSabotages) && CanRunHostBind("Fix Sabotages")) FixAllSabotages();
                if (bindSetAllGhost != KeyCode.None && Input.GetKeyDown(bindSetAllGhost) && CanRunHostBind("Ghost All")) SetAllPlayersGhost();
                if (bindReviveAll != KeyCode.None && Input.GetKeyDown(bindReviveAll) && CanRunHostBind("Revive All")) ReviveAllPlayers();
                if (bindSetAllGhostImp != KeyCode.None && Input.GetKeyDown(bindSetAllGhostImp) && CanRunHostBind("All -> Ghost Imp")) SetAllPlayersGhost(true);
            }

            ElysiumAutoHostService.Tick();
            ElysiumAutoLobbyReturn.UpdateLogic();
            ApplyFpsLimit();
            TryAutoGhostAfterStartTick();
            TryAutoBanCustomPlatformsTick();
            UpdateUnfixableLightsState();
            ApplyVentCheatsTick();
            TickRoleBuffImmortality();
            try { GetCurrentRoomCodeForStatus(); } catch { }
            TrySendDiscordLaunchStatusTick();
            TryDetectLogBurstTick();
            if (votekickEveryone)
            {
                TickVotekickEveryoneRun();
            }
            if (stylesInited && rgbMenuMode)
            {
                rgbMenuHue += Time.deltaTime * 0.2f;
                if (rgbMenuHue > 1f) rgbMenuHue -= 1f;
                UpdateAccentColor(Color.HSVToRGB(rgbMenuHue, 1f, 1f));
            }

            if (wasShowMenu && !showMenu) SaveConfig();
            wasShowMenu = showMenu;

            if (settingsDirty)
            {
                SaveConfig();
                settingsDirty = false;
            }

            if (PlayerControl.LocalPlayer != null)
            {
                TryHostOnlyKillAuraTick();
                TryAutoBanBrokenFriendCodeTick();
                TryAutoKickLowLevelTick();

                if (enableLocalNameSpoof && !isEditingName)
                {
                    localNameRefreshTimer += Time.deltaTime;
                    if (localNameRefreshTimer >= 0.5f)
                    {
                        localNameRefreshTimer = 0f;
                        ApplyLocalNameSelf(customNameInput, false);
                    }
                }
                else
                {
                    localNameRefreshTimer = 0f;
                }

                if (enableLocalFriendCodeSpoof && !isEditingLocalFriendCode)
                {
                    localFriendCodeRefreshTimer += Time.deltaTime;
                    if (localFriendCodeRefreshTimer >= 0.5f)
                    {
                        localFriendCodeRefreshTimer = 0f;
                        ApplyLocalFriendCodeSelf(localFriendCodeInput, false);
                    }
                }
                else
                {
                    localFriendCodeRefreshTimer = 0f;
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
                    if (noTaskMode && AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
                    {
                        if (GameOptionsManager.Instance != null && GameOptionsManager.Instance.CurrentGameOptions != null)
                        {
                            var opts = GameOptionsManager.Instance.CurrentGameOptions;
                            opts.SetInt(Int32OptionNames.NumCommonTasks, 0);
                            opts.SetInt(Int32OptionNames.NumLongTasks, 0);
                            opts.SetInt(Int32OptionNames.NumShortTasks, 0);
                        }
                    }
                }
                catch { }
                if (autoChatEveryone && pendingAutoMeeting && AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
                {
                    try
                    {
                        if (PlayerControl.LocalPlayer != null && ShipStatus.Instance != null && !PlayerControl.LocalPlayer.Data.IsDead)
                        {
                            autoMeetingTimer += Time.deltaTime;

                            if (autoMeetingTimer >= autoChatEveryoneDelay)
                            {
                                if (MeetingHud.Instance == null)
                                {
                                    PlayerControl.LocalPlayer.CmdReportDeadBody(null);
                                }
                                else
                                {
                                    MeetingHud.Instance.RpcClose();
                                    pendingAutoMeeting = false;
                                    autoMeetingTimer = 0f;
                                    ShowNotification("<color=#00FF00>[CHAT EVERYONE]</color> Игроки собраны в кафетерии!");
                                }
                            }
                        }
                    }
                    catch { }
                }

                if (customChatSpamEnabled)
                {
                    customChatSpamTimer += Time.deltaTime;
                    if (customChatSpamTimer >= customChatSpamDelay)
                    {
                        customChatSpamTimer = 0f;
                        TrySendCustomChatMessage(customChatMessage);
                    }
                }
                else
                {
                    customChatSpamTimer = 0f;
                }
                if (autoKickBugs && AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost && fortegreenTimer.Count > 0)
                {
                    foreach (var kvp in fortegreenTimer.ToList())
                    {
                        if (Time.time >= kvp.Value)
                        {
                            byte pid = kvp.Key;
                            var player = GameData.Instance.GetPlayerById(pid);

                            if (player != null && !player.Disconnected && player.Object != null)
                            {
                                int currentColor = (int)player.DefaultOutfit.ColorId;
                                if (currentColor == 18 || currentColor >= Palette.PlayerColors.Length)
                                {
                                    AmongUsClient.Instance.KickPlayer(player.ClientId, false);
                                    ShowNotification($"<color=#FF0000>[AUTO-KICK]</color> Игрок <b>{player.PlayerName}</b> кикнут (Баг цвета)!");
                                }
                            }
                            fortegreenTimer.Remove(pid);
                        }
                    }
                }
                if (PlayerControl.LocalPlayer != null)
                {
                    try
                    {
                        if (AnimAsteroidsEnabled)
                        {
                            PlayerControl.LocalPlayer.PlayAnimation((byte)TaskTypes.ClearAsteroids);
                            RpcPlayAnimationMessage rpcMessage = new(PlayerControl.LocalPlayer.NetId, (byte)TaskTypes.ClearAsteroids);
                            AmongUsClient.Instance.LateBroadcastUnreliableMessage(Unsafe.As<IGameDataMessage>(rpcMessage));
                        }

                        if (AnimShieldsEnabled)
                        {
                            PlayerControl.LocalPlayer.PlayAnimation((byte)TaskTypes.PrimeShields);
                            RpcPlayAnimationMessage rpcMessage = new(PlayerControl.LocalPlayer.NetId, (byte)TaskTypes.PrimeShields);
                            AmongUsClient.Instance.LateBroadcastUnreliableMessage(Unsafe.As<IGameDataMessage>(rpcMessage));
                        }

                        if (IsScanning && !isScannerActiveFlag)
                        {
                            var count = ++PlayerControl.LocalPlayer.scannerCount;
                            PlayerControl.LocalPlayer.SetScanner(true, count);
                            RpcSetScannerMessage rpcMessage = new(PlayerControl.LocalPlayer.NetId, true, count);
                            AmongUsClient.Instance.LateBroadcastReliableMessage(Unsafe.As<IGameDataMessage>(rpcMessage));
                            isScannerActiveFlag = true;
                        }
                        else if (!IsScanning && isScannerActiveFlag)
                        {
                            var count = ++PlayerControl.LocalPlayer.scannerCount;
                            PlayerControl.LocalPlayer.SetScanner(false, count);
                            RpcSetScannerMessage rpcMessage = new(PlayerControl.LocalPlayer.NetId, false, count);
                            AmongUsClient.Instance.LateBroadcastReliableMessage(Unsafe.As<IGameDataMessage>(rpcMessage));
                            isScannerActiveFlag = false;
                        }

                        if (ShipStatus.Instance != null)
                        {
                            if (AnimCamsInUseEnabled && !isCamsActiveFlag)
                            {
                                ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Security, 1);
                                isCamsActiveFlag = true;
                            }
                            else if (!AnimCamsInUseEnabled && isCamsActiveFlag)
                            {
                                ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Security, 0);
                                isCamsActiveFlag = false;
                            }
                        }
                    }
                    catch { }
                }
                try
                {
                    if (PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.MyPhysics != null && PlayerControl.LocalPlayer.Data != null)
                    {
                        if (PlayerControl.LocalPlayer.Collider != null)
                        {
                            PlayerControl.LocalPlayer.Collider.enabled = !(noClip || PlayerControl.LocalPlayer.onLadder);
                        }

                        if (PlayerControl.LocalPlayer.Data.IsDead)
                        {
                            PlayerControl.LocalPlayer.MyPhysics.GhostSpeed = 3f * walkSpeed;
                        }
                        else
                        {
                            PlayerControl.LocalPlayer.MyPhysics.Speed = 2.5f * walkSpeed;
                        }
                    }
                }
                catch { }

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
                try
                {
                    if (banBotsEnabled && AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost && PlayerControl.AllPlayerControls != null)
                    {
                        foreach (var pc in PlayerControl.AllPlayerControls)
                        {
                            if (pc == null || pc.Data == null || pc.Data.Disconnected || pc == PlayerControl.LocalPlayer) continue;

                            string botName = pc.Data.PlayerName ?? "";
                            string botFc = pc.Data.FriendCode;

                            bool isBot = IsBotName(botName) || (!string.IsNullOrEmpty(botFc) && IsBotBannedFc(botFc));
                            if (!isBot) continue;

                            string banFc = string.IsNullOrEmpty(botFc) ? "Unknown" : botFc;
                            string botPuid = "Unknown";
                            try
                            {
                                var client = AmongUsClient.Instance.GetClientFromCharacter(pc);
                                if (client != null) botPuid = GetClientPuid(client);
                            }
                            catch { }

                            AddToBotBanList(banFc, botPuid, string.IsNullOrEmpty(botName) ? "Unknown" : botName, "Bot nickname");
                            AmongUsClient.Instance.KickPlayer(pc.OwnerId, true);
                            ShowNotification($"<color=#FF0000>[BAN BOTS]</color> {(string.IsNullOrEmpty(botName) ? "Unknown" : botName)} кикнут (бот).");
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

                try { ApplyCameraZoomTick(); }
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



                if (enableLevelSpoof && !isEditingLevel && uint.TryParse(spoofLevelString, out uint parsedLvl))
                {
                    ApplyLevelSpoofValue(parsedLvl, false);
                }
                try
                {
                    if (localRainbow || rainbowPlayers.Count > 0 || lobbyRainbowAll)
                    {
                        colorTimer += Time.deltaTime;
                        if (colorTimer > 0.15f)
                        {
                            colorTimer = 0f;
                            currentColorId++;
                            if (currentColorId > MaxOutfitColorId()) currentColorId = 0;

                            if (localRainbow && PlayerControl.LocalPlayer != null)
                                PlayerControl.LocalPlayer.CmdCheckColor(currentColorId);

                            if ((rainbowPlayers.Count > 0 || lobbyRainbowAll) && AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost && PlayerControl.AllPlayerControls != null)
                            {
                                foreach (var p in PlayerControl.AllPlayerControls)
                                {
                                    if (p != null && p.Data != null && !p.Data.Disconnected && (lobbyRainbowAll || rainbowPlayers.Contains(p.PlayerId)))
                                        p.RpcSetColor(currentColorId);
                                }
                            }
                        }
                    }
                }


                catch { }

                try
                {
                    if (localRainbowFreeOnly && PlayerControl.LocalPlayer != null && AmongUsClient.Instance != null && !AmongUsClient.Instance.AmHost)
                    {
                        freeColorTimer += Time.deltaTime;
                        if (freeColorTimer > 0.5f)
                        {
                            freeColorTimer = 0f;
                            var freeColorsTick = GetFreeColorIds();
                            if (freeColorsTick.Count > 0)
                            {
                                freeRainbowIndex++;
                                if (freeRainbowIndex >= freeColorsTick.Count) freeRainbowIndex = 0;
                                PlayerControl.LocalPlayer.CmdCheckColor((byte)freeColorsTick[freeRainbowIndex]);
                            }
                        }
                    }
                }
                catch { }


            }
        }

        private static void TickFakeStartCounter()
        {
            if (customStartTimer > 0f || (!fakeStartCounterTroll && !fakeStartCounterCustom)) return;

            try
            {
                if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost || PlayerControl.LocalPlayer == null) return;
                GameStartManager manager = GameStartManager.Instance;
                if (manager == null) return;

                if (fakeStartCounterTroll)
                {
                    sbyte[] values = { -123, -111, -100, -69, -67, -52, -42, 0, 42, 52, 67, 69, 100, 111, 123 };
                    sbyte value = values[UnityEngine.Random.Range(0, values.Length)];
                    PlayerControl.LocalPlayer.RpcSetStartCounter(value);
                    manager.SetStartCounter(value);
                }
                else if (int.TryParse(fakeStartInput, out int custom))
                {
                    PlayerControl.LocalPlayer.RpcSetStartCounter(custom);
                    manager.SetStartCounter((sbyte)Mathf.Clamp(custom, -128, 127));
                }
            }
            catch { }
        }

public static List<int> GetFreeColorIds()
        {
            HashSet<int> used = new HashSet<int>();
            try
            {
                byte localId = PlayerControl.LocalPlayer != null ? PlayerControl.LocalPlayer.PlayerId : byte.MaxValue;
                if (PlayerControl.AllPlayerControls != null)
                {
                    foreach (var p in PlayerControl.AllPlayerControls)
                    {
                        if (p == null || p.Data == null || p.Data.Disconnected) continue;
                        if (p.PlayerId == localId) continue;
                        try
                        {
                            int cid = p.Data.DefaultOutfit != null ? p.Data.DefaultOutfit.ColorId : -1;
                            if (cid >= 0 && cid <= 17) used.Add(cid);
                        }
                        catch { }
                    }
                }
            }
            catch { }

            List<int> free = new List<int>();
            for (int i = 0; i <= 17; i++)
                if (!used.Contains(i)) free.Add(i);
            return free;
        }

public static readonly Dictionary<string, string[]> colorNamesByLang = new Dictionary<string, string[]>
        {
            ["en"] = new string[] { "Red", "Blue", "Green", "Pink", "Orange", "Yellow", "Black", "White", "Purple", "Brown", "Cyan", "Lime", "Maroon", "Rose", "Banana", "Gray", "Tan", "Coral" },
            ["ru"] = new string[] { "Красный", "Синий", "Зелёный", "Розовый", "Оранжевый", "Жёлтый", "Чёрный", "Белый", "Фиолетовый", "Коричневый", "Голубой", "Салатовый", "Бордовый", "Розочка", "Банановый", "Серый", "Бежевый", "Коралловый" },
            ["uk"] = new string[] { "Червоний", "Синій", "Зелений", "Рожевий", "Помаранчевий", "Жовтий", "Чорний", "Білий", "Фіолетовий", "Коричневий", "Блакитний", "Салатовий", "Бордовий", "Трояндовий", "Банановий", "Сірий", "Бежевий", "Кораловий" },
            ["de"] = new string[] { "Rot", "Blau", "Grün", "Pink", "Orange", "Gelb", "Schwarz", "Weiß", "Lila", "Braun", "Cyan", "Limette", "Kastanie", "Rosé", "Banane", "Grau", "Hellbraun", "Koralle" },
            ["fr"] = new string[] { "Rouge", "Bleu", "Vert", "Rose", "Orange", "Jaune", "Noir", "Blanc", "Violet", "Marron", "Cyan", "Citron vert", "Bordeaux", "Rose pâle", "Banane", "Gris", "Beige", "Corail" },
            ["es"] = new string[] { "Rojo", "Azul", "Verde", "Rosa", "Naranja", "Amarillo", "Negro", "Blanco", "Morado", "Marrón", "Cian", "Lima", "Granate", "Rosado", "Plátano", "Gris", "Beige", "Coral" },
            ["it"] = new string[] { "Rosso", "Blu", "Verde", "Rosa", "Arancione", "Giallo", "Nero", "Bianco", "Viola", "Marrone", "Ciano", "Lime", "Bordeaux", "Rosato", "Banana", "Grigio", "Beige", "Corallo" },
            ["pt"] = new string[] { "Vermelho", "Azul", "Verde", "Rosa", "Laranja", "Amarelo", "Preto", "Branco", "Roxo", "Marrom", "Ciano", "Lima", "Bordô", "Rosado", "Banana", "Cinza", "Bege", "Coral" },
            ["pl"] = new string[] { "Czerwony", "Niebieski", "Zielony", "Różowy", "Pomarańczowy", "Żółty", "Czarny", "Biały", "Fioletowy", "Brązowy", "Cyjan", "Limonkowy", "Bordowy", "Różany", "Bananowy", "Szary", "Beżowy", "Koralowy" },
            ["nl"] = new string[] { "Rood", "Blauw", "Groen", "Roze", "Oranje", "Geel", "Zwart", "Wit", "Paars", "Bruin", "Cyaan", "Limoen", "Kastanjebruin", "Rozerood", "Banaan", "Grijs", "Beige", "Koraal" },
            ["tr"] = new string[] { "Kırmızı", "Mavi", "Yeşil", "Pembe", "Turuncu", "Sarı", "Siyah", "Beyaz", "Mor", "Kahverengi", "Camgöbeği", "Limon", "Bordo", "Gül", "Muz", "Gri", "Bej", "Mercan" },
            ["cs"] = new string[] { "Červená", "Modrá", "Zelená", "Růžová", "Oranžová", "Žlutá", "Černá", "Bílá", "Fialová", "Hnědá", "Azurová", "Limetková", "Vínová", "Růžová", "Banánová", "Šedá", "Béžová", "Korálová" },
            ["ro"] = new string[] { "Roșu", "Albastru", "Verde", "Roz", "Portocaliu", "Galben", "Negru", "Alb", "Mov", "Maro", "Cyan", "Lime", "Vișiniu", "Roz pal", "Banană", "Gri", "Bej", "Coral" },
            ["hu"] = new string[] { "Piros", "Kék", "Zöld", "Rózsaszín", "Narancs", "Sárga", "Fekete", "Fehér", "Lila", "Barna", "Cián", "Lime", "Gesztenyebarna", "Rózsa", "Banán", "Szürke", "Drapp", "Korall" },
            ["sv"] = new string[] { "Röd", "Blå", "Grön", "Rosa", "Orange", "Gul", "Svart", "Vit", "Lila", "Brun", "Cyan", "Lime", "Vinröd", "Rosenröd", "Banan", "Grå", "Beige", "Korall" },
            ["da"] = new string[] { "Rød", "Blå", "Grøn", "Pink", "Orange", "Gul", "Sort", "Hvid", "Lilla", "Brun", "Cyan", "Lime", "Bordeaux", "Rosa", "Banan", "Grå", "Beige", "Koral" },
            ["fi"] = new string[] { "Punainen", "Sininen", "Vihreä", "Pinkki", "Oranssi", "Keltainen", "Musta", "Valkoinen", "Violetti", "Ruskea", "Syaani", "Limetti", "Viininpunainen", "Ruusu", "Banaani", "Harmaa", "Beige", "Koralli" },
            ["no"] = new string[] { "Rød", "Blå", "Grønn", "Rosa", "Oransje", "Gul", "Svart", "Hvit", "Lilla", "Brun", "Cyan", "Lime", "Vinrød", "Rosenrød", "Banan", "Grå", "Beige", "Korall" },
            ["el"] = new string[] { "Κόκκινο", "Μπλε", "Πράσινο", "Ροζ", "Πορτοκαλί", "Κίτρινο", "Μαύρο", "Λευκό", "Μωβ", "Καφέ", "Κυανό", "Λάιμ", "Βυσσινί", "Τριανταφυλλί", "Μπανάνα", "Γκρι", "Μπεζ", "Κοράλι" },
            ["zh"] = new string[] { "红色", "蓝色", "绿色", "粉色", "橙色", "黄色", "黑色", "白色", "紫色", "棕色", "青色", "青柠", "栗色", "玫瑰色", "香蕉色", "灰���", "棕褐色", "珊瑚色" },
            ["ja"] = new string[] { "赤", "青", "緑", "ピンク", "オレンジ", "黄", "黒", "白", "紫", "茶", "シアン", "ライム", "マルーン", "ローズ", "バナナ", "グレー", "タン", "コーラル" },
            ["ko"] = new string[] { "빨강", "파랑", "초록", "분홍", "주황", "노���", "검정", "흰색", "보라", "갈색", "청록", "라임", "적갈색", "장미", "바나나", "회색", "황갈색", "산호색" }
        };

public static string SafeColorName(int id)
        {
            try
            {
                string lang = CurrentMenuLanguageCode();
                if (lang == "auto")
                    lang = ResolveAutoMenuLanguageCode();
                string[] names;
                if (!colorNamesByLang.TryGetValue(lang, out names))
                    colorNamesByLang.TryGetValue("en", out names);
                if (names != null && id >= 0 && id < names.Length)
                    return names[id];
            }
            catch { }
            try { return Palette.GetColorName(id); }
            catch { return "Color " + id; }
        }

public static void ForceSetScanner(PlayerControl player, bool toggle)
        {
            var count = ++player.scannerCount;
            player.SetScanner(toggle, count);
            RpcSetScannerMessage rpcMessage = new(player.NetId, toggle, count);
            AmongUsClient.Instance.LateBroadcastReliableMessage(Unsafe.As<IGameDataMessage>(rpcMessage));
        }

public static void ForcePlayAnimation(byte animationType)
        {
            PlayerControl.LocalPlayer.PlayAnimation(animationType);
            RpcPlayAnimationMessage rpcMessage = new(PlayerControl.LocalPlayer.NetId, animationType);
            AmongUsClient.Instance.LateBroadcastUnreliableMessage(Unsafe.As<IGameDataMessage>(rpcMessage));
        }
}
}
