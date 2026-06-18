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
public class ModPlayer : MonoBehaviour

        {
            public PlayerControl player;

            public float LastTask;

            public float JoinTime;

            public bool NameApply = false;

            private Dictionary<byte, Queue<float>> rpcCallTimestamps = new Dictionary<byte, Queue<float>>();

            private const float DefaultTimeWindow = 1f;

            private const int DefaultRpcLimitPerWindow = 10;

            private bool isMarkedAsSpamRpc;

            private bool isMarkedAsModUser;

            public static ModPlayer LocalPlayer => PlayerControl.LocalPlayer.Mod();

            public static readonly HashSet<byte> normalCustomCallId = new HashSet<byte> { 6, 80, 78, 70, 210, 81, 176, 169 };

            public readonly HashSet<byte> SpammableCrashRpc = new HashSet<byte> { 49, 50, 7, 4, 18, 31 };

            public static readonly HashSet<int> excludedCallIdsForTargetClient = new HashSet<int> { 5, 6, 7, 13, 44, 51, 54, 62, 64, 55 };

            public static readonly HashSet<byte> ImmediatelyRPCs = new HashSet<byte>
    {
        51, 54, 5, 7, 14, 47, 48, 12, 52, 53, 54,
        45, 46, 62, 64, 55, 56, 2, 63, 65, 21, 49
    };

            public static readonly HashSet<int> excludedCallIdsForLobby = new HashSet<int>
    {
        5, 6, 7, 9, 10, 13, 17, 18, 21, 33,
        36, 37, 38, 39, 40, 41, 42, 43, 44, 49, 50,
        60, 61, 80, 78, 70, 210, 81, 176
    };

            public static readonly IReadOnlyDictionary<byte, (short, float)> StandartALotRPCs = new Dictionary<byte, (short, float)>
    {
        {
            31,
            (10, 1f)
        },
        {
            18,
            (25, 1f)
        },
        {
            49,
            (50, 0.1f)
        },
        {
            44,
            (50, 0.1f)
        },
        {
            50,
            (100, 0.1f)
        },
        {
            8,
            (30, 0.1f)
        },
        {
            6,
            (30, 0.1f)
        },
        {
        39,
        (30, 0.1f)
        },
        {
            40,
            (30, 0.1f)
        },
        {
            42,
            (30, 0.1f)
        },
        {
            41,
            (30, 0.1f)
        },
        {
            33,
            (1, 1f)
        },
        {
            54,
            (5, 1f)
        },
        {
            7,
            (100, 0.1f)
        }
    };

            static bool RpcCrash;

            public static readonly HashSet<byte> excludedNumMsgCallIds = new HashSet<byte> { 12, 41, 39, 40, 42, 43, 38, 49 };

            public static readonly HashSet<byte> SusRPCs = new HashSet<byte> { 101, 164, 154, 85, 219, 81, 176, 204, 216, 121, 119, 167 };


            public ModPlayer(IntPtr ptr)
                : base(ptr)
            {
            }

            public void Awake()
            {
                player = this.GetComponent<PlayerControl>();
            }

            public void FixedUpdate()
            {
                JoinTime += Time.fixedDeltaTime;
                LateTask.Update(Time.deltaTime);
            }

            public bool RpcCheck(byte callId, int targetClientId, SendOption sendOption, int numData)
            {
                if (PlayerControl.LocalPlayer == null) return true;
                RpcCalls b = (RpcCalls)callId;
                if (!CheckSpam(callId))
                {
                    return false;
                }

                if (AmongUsClient.Instance.AmHost)
                {
                    if (targetClientId >= 0 && !excludedCallIdsForTargetClient.Contains(callId) && !(player.Data.ClientId == AmongUsClient.Instance.ClientId))
                    {

                        if (!RpcCrash)
                        {
                            new LateTask(delegate
                            {
                               DestroyableSingleton<HudManager>.Instance.Notifier.AddDisconnectMessage($"<color=#FFFF00>Received Rpc from <b>{player.Data.PlayerName}</b>, {b}\nthat shouldn't be got like that way</color>");
                                if (enablePasosLimit)
                                {
                                    AmongUsClient.Instance.KickPlayer(player.OwnerId, true);
                                }
                            }, 2f);
                            _ = new LateTask(delegate
                            {
                                RpcCrash = false;
                            }, 15f);
                            RpcCrash = true;
                        }
                        return false;

                    }
                    if (AmongUsClient.Instance.GameState == InnerNetClient.GameStates.Joined && !excludedCallIdsForLobby.Contains(callId) && callId < 66 && player != PlayerControl.LocalPlayer)
                    {
                        if (!RpcCrash)
                        {
                            new LateTask(delegate
                            {
                                DestroyableSingleton<HudManager>.Instance.Notifier.AddDisconnectMessage($"<color=#FFFF00>Received Rpc in lobby from <b>{player.Data.PlayerName}</b>, <color=#00FFFF>{b}</color>\nthat shouldn't be used there</color>");

                                if (enablePasosLimit)
                                {
                                    AmongUsClient.Instance.KickPlayer(player.OwnerId, true);
                                }
                            }, 2f);
                            _ = new LateTask(delegate
                            {
                                RpcCrash = false;
                            }, 10f);
                            RpcCrash = true;
                        }
                        return false;
                    }
                }
                if (callId > 65)
                {
                    if (isMarkedAsModUser) return false;
                    if (!RpcCrash)
                    {
                        new LateTask(delegate
                        {    
                            isMarkedAsModUser = true;
                        }, 2f);
                        _ = new LateTask(delegate
                        {
                            RpcCrash = false;
                        }, 10f);
                        RpcCrash = true;
                    }
                    return true;
                }
                if (!excludedNumMsgCallIds.Contains(callId) && ((numData > 1 && ImmediatelyRPCs.Contains(callId)) && !AmongUsClient.Instance.AmHost || numData > 25) && player != PlayerControl.LocalPlayer)
                {
                    if (!RpcCrash)
                    {
                        new LateTask(delegate
                        {
                            DestroyableSingleton<HudManager>.Instance.Notifier.AddDisconnectMessage($"<color=#FFFF00>Received Too many Rpc from <b>{player.name}</b>\nfor this type of Rpc - <color=#00FFFF>{b}</color> </color>");
                        }, 2f);
                        _ = new LateTask(delegate
                        {
                            RpcCrash = false;
                        }, 10f);
                        RpcCrash = true;
                    }
                    return targetClientId <= 0;

                }
                if (sendOption == SendOption.None && callId != 0 && player != PlayerControl.LocalPlayer)
                {
                    if (!RpcCrash && !isMarkedAsModUser)
                    {
                        if (isMarkedAsModUser) return targetClientId <= 0;
                        isMarkedAsModUser = true;
                        _ = new LateTask(delegate
                        {
                           DestroyableSingleton<HudManager>.Instance.Notifier.AddDisconnectMessage($"<color=#FFFF00>Received SendOption None Rpc (Modded) from <b>{player.name}</b>, <color=#00FFFF>{b}</color></color>");
                        }, 2f);
                        _ = new LateTask(delegate
                        {
                            RpcCrash = false;
                        }, 10f);
                        RpcCrash = true;
                    }
                    return targetClientId <= 0;
                }
                return true;
            }

            public bool CheckSpam(byte callId)
            {

                if (!rpcCallTimestamps.ContainsKey(callId))
                {
                    rpcCallTimestamps[callId] = new Queue<float>();
                }
                Queue<float> queue = rpcCallTimestamps[callId];
                float fixedTime = Time.fixedTime;
                float num = 1f;
                int num2 = 10;
                if (StandartALotRPCs.TryGetValue(callId, out var value))
                {
                    num2 = value.Item1;
                    num = value.Item2;
                }
                while (queue.Count > 0 && queue.Peek() < fixedTime - num)
                {
                    queue.Dequeue();
                }
                queue.Enqueue(fixedTime);
                if ((queue.Count > num2) && (byte)RpcCalls.SnapTo != callId)
                {
                    if (!RpcCrash)
                    {
                        new LateTask(delegate
                        {
                            if (enablePasosLimit)
                            {
                                AmongUsClient.Instance.KickPlayer(player.OwnerId, true);
                            }
                            if (!isMarkedAsSpamRpc && player != PlayerControl.LocalPlayer)
                            {     
                                isMarkedAsSpamRpc = true;
                            }
                            DestroyableSingleton<HudManager>.Instance.Notifier.AddDisconnectMessage($"<color=#FFFF00>Rpc Spam from <b>{player.name}</b>\nwith <color=#00FFFF>{(RpcCalls)callId}</color></color>");
                        }, 2f);
                        new LateTask(delegate
                        {
                            RpcCrash = false;

                        }, 10f);
                        RpcCrash = true;
                    }
                    return false;
                }
                return true;
            }

            public static bool operator ==(ModPlayer a, PlayerControl b)
            {
                return a.player == b;
            }

            public static bool operator ==(PlayerControl a, ModPlayer b)
            {
                return a == b.player;
            }

            public static bool operator !=(ModPlayer a, PlayerControl b)
            {
                return a.player != b;
            }

            public static bool operator !=(PlayerControl a, ModPlayer b)
            {
                return a != b.player;
            }

            public override bool Equals(object o)
            {
                return base.Equals(o);
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }
        }

[HarmonyPatch(typeof(InnerNetClient), "HandleMessage")]
        internal class HandleMessage
        {
            public static List<uint> blockedSenders = new();
            public static Dictionary<uint, int> msgCount = new();
            public static float blockedtimer;
            public static float timer;

            public static void HandleTimer()
            {
                GoingTimer();
                resetingDataLimit += Time.deltaTime;
                timer -= Time.deltaTime;
                blockedtimer -= Time.deltaTime;

                if (resetingDataLimit >= 1)
                {
                    resetingDataLimit = 0;
                }

                if (timer <= 0)
                {
                    timer = 1;
                    msgCount.Clear();
                }
                if (blockedtimer <= 0)
                {
                    blockedtimer = 15;
                    blockedSenders.Clear();
                }
            }

            public static bool Crashed;
            public static Dictionary<int, float> LastJoin = new Dictionary<int, float>();

            public static void GoingTimer()
            {
                foreach (var item in LastJoin)
                {
                    LastJoin[item.Key] += Time.deltaTime;
                }
            }

            public static void Crash(string reason)
            {
                if (!Crashed)
                {
                    Debug.LogError("WARNING - " + reason);
                    _ = new LateTask(delegate
                    {
                        DestroyableSingleton<HudManager>.Instance.Notifier.AddDisconnectMessage("<#ffff00>" + L("Got crash attempt - "  + reason, "Произошла попытка краша - " + reason));
                        if (banMalformedPacketSender)
                        {
                            KeyValuePair<int, float> keyValuePair = HandleMessage.LastJoin.OrderBy((KeyValuePair<int, float> pair) => pair.Value).FirstOrDefault();
		                    AmongUsClient.Instance.KickPlayer(keyValuePair.Key, ban: true);
                        }
                    }, 0.1f);
                    _ = new LateTask(delegate
                    {
                        Crashed = false;
                    }, 10f);
                    Crashed = true;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool CheckDataMessage(MessageReader message, int TargetClientId, SendOption sendOption)
            {
                if (PlayerControl.LocalPlayer == null) return true;
                int num = 0;
                InnerNetObject innerNetObject = default;
                while (message.Position < message.Length)
                {

                    if (num > 75)
                    {
                        Crash("Spam of Data");
                        return false;
                    }

                    MessageReader messageReader = message.ReadMessage();
                    if (messageReader.Tag != 207 && messageReader.Tag > 8 || messageReader.Tag == 3 || messageReader.Tag == 0)
                    {
                        Crash("Bad Tag - " + messageReader.Tag);
                        return false;
                    }
                    if (messageReader.Tag == 1)
                    {
                        uint num2 = messageReader.ReadPackedUInt32();
                        try
                        {
                            if (!AmongUsClient.Instance.allObjects.allObjectsFast.ContainsKey(num2) && !AmongUsClient.Instance.DestroyedObjects.Contains(num2) && AmongUsClient.Instance.AmHost || num2 > AmongUsClient.Instance.NetIdCnt + 30 || num2 == 0)
                            {
                                Crash("Null Data - " + num2);
                                return false;
                            }
                        }
                        catch { }
                    }
                    else
                    {
                        if (messageReader.Tag != 2)
                        {
                            continue;
                        }
                        uint num3 = messageReader.ReadPackedUInt32();
                        byte callId = messageReader.ReadByte();
                        AmongUsClient instance = AmongUsClient.Instance;
                        lock (instance.allObjects)
                        {
                            if (instance.allObjects.allObjectsFast.TryGetValue(num3, out innerNetObject))
                            {
                                if (innerNetObject is PlayerControl pc)
                                {
                                    if (!pc.Mod().RpcCheck(callId, TargetClientId, sendOption, num))
                                    {
                                        return false;
                                    }
                                }
                                else if (innerNetObject is PlayerPhysics playerPhysics)
                                {
                                    if (!playerPhysics.myPlayer.Mod().RpcCheck(callId, TargetClientId, sendOption, num))
                                    {
                                        return false;
                                    }
                                }
                                else if (innerNetObject is CustomNetworkTransform customNetworkTransform && !customNetworkTransform.myPlayer.Mod().RpcCheck(callId, TargetClientId, sendOption, num))
                                {
                                    return false;
                                }
                            }
                        }
                    }
                }
                return true;
            }

            public static void Dispatcher(InnerNetClient __instance, System.Action action)
            {
                __instance.Dispatcher.Add(action);
            }

            public static void PreDispatcher(InnerNetClient __instance, System.Action action)
            {
                __instance.PreSpawnDispatcher.Add(action); 
            }

            public static bool Prefix(InnerNetClient __instance, MessageReader reader, SendOption sendOption)
            {
                if (enableMalformedPacketGuard == false) return true;

                if (reader == null)
                {
                    Crash("Empty network packet");
                    return false;
                }

                switch (reader.Tag)
                {
                    case 1:
                        {
                            int num2 = reader.ReadInt32();
                            int num3 = 0;
                            ClientData clientData = null;
                            bool flag = false;
                            if (__instance.GameId == num2)
                            {
                                num3 = reader.ReadInt32();
                                flag = __instance.AmHost;
                                __instance.HostId = reader.ReadInt32();
                                string playerName = reader.ReadString();
                                PlatformSpecificData platformSpecificData = new PlatformSpecificData();
                                MessageReader messageReader = reader.ReadMessage();
                                platformSpecificData.Platform = (Platforms)messageReader.Tag;
                                string platformName = messageReader.ReadString();
                                platformSpecificData.PlatformName = platformName;
                                switch (platformSpecificData.Platform)
                                {
                                    case Platforms.StandaloneWin10:
                                    case Platforms.Xbox:
                                        platformSpecificData.XboxPlatformId = messageReader.ReadUInt64();
                                        break;
                                    case Platforms.Playstation:
                                        platformSpecificData.PsnPlatformId = messageReader.ReadUInt64();
                                        break;
                                }
                                uint playerLevel = reader.ReadPackedUInt32();
                                string productUserId = reader.ReadString();
                                string friendCode = reader.ReadString();
                                clientData = new ClientData(num3, playerName, platformSpecificData, playerLevel, productUserId, friendCode);
                                LastJoin[num3] = 0f;
                                ClientData client = __instance.GetOrCreateClient(clientData);
                                Debug.Log($"Player {num3} joined. Name - {clientData.PlayerName} with FC {clientData.FriendCode}");
                                LastJoin[num3] = 0f;
                                lock (__instance.Dispatcher)
                                {
                                    Dispatcher(__instance, delegate
                                    {
                                        __instance.OnPlayerJoined(client);
                                    });
                                }
                                if (!__instance.AmHost || flag)
                                {
                                    break;
                                }
                                lock (__instance.Dispatcher)
                                {
                                    Dispatcher(__instance, delegate
                                    {
                                        __instance.OnBecomeHost();
                                    });
                                }
                            }
                            else
                            {
                                __instance.EnqueueDisconnect(DisconnectReasons.IncorrectGame);
                            }
                            break;
                        }
                    case 5:
                    case 6:
                        {
                            int num = reader.ReadInt32();
                            int TargetClientId = 0;
                            if (__instance.GameId != num)
                            {
                                break;
                            }
                            if (reader.Tag == 6)
                            {
                                try
                                {
                                    TargetClientId = reader.ReadPackedInt32();
                                }
                                catch
                                { 
                                    break;
                                }
                                if (__instance.ClientId != TargetClientId)
                                {
                                    Debug.Log($"Got data meant for {TargetClientId} for some unknown reason");
                                    break;
                                }
                            }
                            else
                            {
                                TargetClientId = -1;
                            }
                            if (__instance.InOnlineScene)
                            {
                                MessageReader subReader2 = MessageReader.Get(reader);
                                lock (__instance.Dispatcher)
                                {
                                    Dispatcher(__instance, delegate
                                    {

                                        int num4 = 0;
                                        num4 = subReader2.Position;
                                        if (!CheckDataMessage(subReader2, TargetClientId, sendOption))
                                        {
                                            subReader2.Recycle();
                                        }
                                        else
                                        {
                                            subReader2.Position = num4;
                                            HandleGameData.HandleData(__instance, subReader2, TargetClientId, sendOption);
                                        }
                                    });
                                }
                            }
                            else
                            {
                                if (sendOption == SendOption.None)
                                {
                                    break;
                                }
                                MessageReader subReader3 = MessageReader.Get(reader);
                                lock (__instance.PreSpawnDispatcher)
                                {
                                    PreDispatcher(__instance, delegate
                                    {
                                        int num4 = 0;
                                        num4 = subReader3.Position;
                                        if (CheckDataMessage(subReader3, TargetClientId, sendOption))
                                        {
                                            subReader3.Position = num4;
                                            HandleGameData.HandleData(__instance, subReader3, TargetClientId, sendOption);
                                        }
                                    });
                                }
                            }
                            break;
                        }
                    default:
                        return true;
                }
                return false;
            }

            // Catch malformed-reader failures at the packet boundary. Patching Hazel's
            // packed integer methods directly recurses on Epic's IL2CPP wrappers.
            public static Exception Finalizer(Exception __exception)
            {
                if (__exception == null || !enableMalformedPacketGuard)
                    return __exception;

                Crash("Malformed network packet (" + __exception.GetType().Name + ")");
                return null;
            }
        }

[HarmonyPatch(typeof(InnerNetClient), "HandleGameData")]
        internal class HandleGameData
        {

            public static bool Prefix(InnerNetClient __instance, MessageReader parentReader)
            {
                return !enableMalformedPacketGuard;
            }

            public static void HandleData(InnerNetClient __instance, MessageReader parentReader, int TargetClientId, SendOption sendOption)
            {
                try
                {
                    int num = 0;
                    while (parentReader.Position < parentReader.Length)
                    {
                        num++;
                        MessageReader reader = parentReader.ReadMessageAsNewBuffer();
                        int msgNum = __instance.msgNum;
                        __instance.msgNum = msgNum + 1;
                        __instance.StartCoroutine(BepInEx.Unity.IL2CPP.Utils.Collections.CollectionExtensions.WrapToIl2Cpp(Handle(__instance, reader, msgNum, TargetClientId, sendOption, num)));
                    }
                }
                catch (Exception error)
                {
                    HandleMessage.Crash("Malformed GameData frame (" + error.GetType().Name + ")");
                }
                finally
                {
                    parentReader.Recycle();
                }
            }

            public static IEnumerator Handle(InnerNetClient __instance, MessageReader reader, int msgNum, int TargetClientId, SendOption sendOption, int numData)
            {
                int cnt = 0;
                reader.Position = 0;

                switch ((GameDataTypes)reader.Tag)
                {
                    case GameDataTypes.SceneChangeFlag:
                        {
                            try
                            {
                                int num3 = reader.ReadPackedInt32();
                                ClientData clientData2 = __instance.FindClientById(num3);
                                string text = reader.ReadString();
                                if (clientData2 != null && !string.IsNullOrWhiteSpace(text))
                                {
                                    MonoBehaviourExtensions.StartCoroutine(__instance, AmongUsClientUtils.CoOnPlayerChangedScene(__instance, clientData2, text));
                                    Debug.Log($"SceneChangeFlag for {num3} to {text}");
                                    break;
                                }
                                Debug.Log($"(SceneChangeFlag) Couldn't find client {num3} to change scene to {text}");
                            }
                            catch (Exception error)
                            {
                                HandleMessage.Crash("Malformed scene packet (" + error.GetType().Name + ")");
                            }
                            reader.Recycle();
                            break;
                        }
                    case GameDataTypes.RpcFlag:
                        try
                        {
                            InnerNetObject value = default;
                            while (true)
                            {
                                uint num2;
                                try
                                {
                                    num2 = reader.ReadPackedUInt32();
                                }

                                catch (Exception)
                                {
                                    throw;
                                }
                                byte b = reader.ReadByte();
                                RpcCalls rpcCalls = (RpcCalls)b;
                                try
                                {
                                    if (PlayerControl.LocalPlayer) Debug.Log($"RpcFlag ({sendOption}) - ({__instance.FindObjectByNetId<InnerNetObject>(num2).name}({__instance.FindObjectByNetId<InnerNetObject>(num2).NetId})RPC:" + rpcCalls);
                                }
                                catch { if (PlayerControl.LocalPlayer) HandleMessage.Crash("Unknown object sent RPC - " + rpcCalls); }

                                lock (__instance.allObjects)
                                {
                                    if (__instance.allObjects.AllObjectsFast.TryGetValue(num2, out value))
                                    {
                                        value.HandleRpc(b, reader);
                                        goto IL_03b6;
                                    }
                                    if (num2 == uint.MaxValue || __instance.DestroyedObjects.Contains(num2))
                                    {
                                        goto IL_03b6;
                                    }
                                    if (cnt++ <= 10)
                                    {
                                        reader.Position = 0;
                                        yield return Effects.Wait(0.1f);
                                        continue;
                                    }
                                    break;
                                IL_03b6:
                                    value = null;
                                    break;
                                }
                            }
                            break;
                        }
                        finally
                        {
                            reader.Recycle();
                        }
                    default:
                        // Removed per-packet Debug.Log here. It fired on every unrecognized
                        // data flag and Unity's Debug.Log is expensive (synchronous write +
                        // stack trace capture), which caused log spam and micro-stutters.
                        yield return __instance.HandleGameDataInner(reader, __instance.msgNum);
                        break;
                }
            }
        }
    }
}
