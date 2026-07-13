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
private static void FloodPlayerWithTasks(PlayerControl target)
        {
            if (target == null || target.Data == null)
            {
                ShowNotification("<color=#FF0000>[TASKS]</color> Target not found.");
                return;
            }

            if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
            {
                ShowNotification("<color=#FF0000>[TASKS]</color> Host required.");
                return;
            }

            try
            {
                byte[] taskIds = new byte[255];
                for (byte i = 0; i < 255; i++) taskIds[i] = i;
                target.Data.RpcSetTasks(taskIds);
                ShowNotification($"<color=#00FF00>[TASKS]</color> {target.Data.PlayerName} received flood tasks.");
            }
            catch (Exception)
            {
            }
        }

        private static void ChangePlayerTasks(PlayerControl target)
        {
            if (target == null || target.Data == null)
            {
                ShowNotification("<color=#FF0000>[TASKS]</color> Target not found.");
                return;
            }

            if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
            {
                ShowNotification("<color=#FF0000>[TASKS]</color> Host required.");
                return;
            }

            try
            {
                List<byte> taskIds = BuildRandomAssignableTaskIds(target);
                if (taskIds.Count == 0)
                {
                    ShowNotification("<color=#FF0000>[TASKS]</color> No assignable tasks found on this map.");
                    return;
                }

                ApplyTaskIdsToPlayer(target, taskIds.ToArray());
                ShowNotification($"<color=#00FF00>[TASKS]</color> {target.Data.PlayerName} tasks changed.");
            }
            catch (Exception)
            {
            }
        }

private static void AddSelectedPlayerToBanList(PlayerControl target)
        {
            if (target == null || target.Data == null)
            {
                ShowNotification("<color=#FF0000>[BAN]</color> Player not found.");
                return;
            }

            string friendCode = GetDisplayedFriendCode(target.Data, string.Empty);
            if (string.IsNullOrWhiteSpace(friendCode))
            {
                ShowNotification("<color=#FF0000>[BAN]</color> Friend Code is unavailable.");
                return;
            }

            if (IsFriendCodeBanned(friendCode))
            {
                ShowNotification($"<color=#FFD700>[BAN]</color> {friendCode} is already in ban list.");
                return;
            }

            string puid = GetPlayerPuid(target);
            if (string.IsNullOrWhiteSpace(puid) || puid == "Unknown") puid = "Unknown";

            string playerName = target.Data.PlayerName;
            if (string.IsNullOrWhiteSpace(playerName)) playerName = $"Player {target.PlayerId}";
            playerName = Regex.Replace(playerName, "<.*?>", string.Empty);

            if (string.IsNullOrEmpty(banListPath)) LoadBanList();
            AddToBanList(friendCode, puid, playerName, "Player info button");

            if (IsFriendCodeBanned(friendCode))
                ShowNotification($"<color=#00FF00>[BAN]</color> {playerName} added to ban list.");
            else
                ShowNotification("<color=#FF0000>[BAN]</color> Failed to add player.");
        }

private static void SendFriendInviteToPlayer(PlayerControl target)
        {
            if (target == null || target.Data == null)
            {
                ShowNotification("<color=#FF0000>[FRIENDS]</color> Player not found.");
                return;
            }

            string fc = GetDisplayedFriendCode(target.Data, string.Empty);
            string puid = GetPlayerPuid(target);
            bool hasPuid = !string.IsNullOrWhiteSpace(puid) && puid != "Unknown";
            bool hasFc = !string.IsNullOrWhiteSpace(fc) && fc != "Hidden" && fc != "Unknown";
            if (!hasPuid && !hasFc)
            {
                ShowNotification("<color=#FF0000>[FRIENDS]</color> Player identity is unavailable.");
                return;
            }

            try
            {
                FriendsListManager mgr = null;
                try { mgr = FriendsListManager.Instance; } catch { }
                if (mgr == null) mgr = UnityEngine.Object.FindObjectOfType<FriendsListManager>();

                if (mgr == null)
                {
                    ShowNotification("<color=#FF0000>[FRIENDS]</color> Friends manager not ready.");
                    return;
                }

                if (hasPuid && mgr.IsPlayerFriend(puid))
                {
                    ShowNotification("<color=#FFD700>[FRIENDS]</color> Already in friends.");
                    return;
                }

                if (hasPuid) mgr.SendFriendRequest(puid, null);
                else mgr.SendFriendRequestByUsername(fc, null);

                string nm = target.Data.PlayerName;
                if (string.IsNullOrWhiteSpace(nm)) nm = $"Player {target.PlayerId}";
                ShowNotification($"<color=#00FF00>[FRIENDS]</color> Request sent to <b>{Regex.Replace(nm, "<.*?>", string.Empty)}</b>.");
            }
            catch (Exception)
            {
                ShowNotification("<color=#FF0000>[FRIENDS]</color> Request failed.");
            }
        }

private static void DeletePlayerTasks(PlayerControl target)
        {
            if (target == null || target.Data == null)
            {
                ShowNotification("<color=#FF0000>[TASKS]</color> Target not found.");
                return;
            }

            if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
            {
                ShowNotification("<color=#FF0000>[TASKS]</color> Host required.");
                return;
            }

            try
            {
                int removed = CountPlayerTasks(target);
                ApplyTaskIdsToPlayer(target, Array.Empty<byte>());

                ShowNotification($"<color=#00FF00>[TASKS]</color> Deleted {removed} task(s) for {target.Data.PlayerName}.");
            }
            catch (Exception)
            {
            }
        }

private static void ApplyTaskIdsToPlayer(PlayerControl target, byte[] taskIds)
        {
            if (target == null || target.Data == null) return;

            byte[] safeTaskIds = taskIds ?? Array.Empty<byte>();
            target.Data.RpcSetTasks(safeTaskIds);

            if (safeTaskIds.Length == 0)
            {
                try { target.Data.Tasks?.Clear(); } catch { }
                try { target.ClearTasks(); } catch { }
            }

            try { GameData.Instance?.RecomputeTaskCounts(); } catch { }
            try { target.Data.SetDirtyBit(uint.MaxValue); } catch { }
            try
            {
                var netObj = GameData.Instance?.GetComponent<InnerNetObject>();
                if (netObj != null) netObj.SetDirtyBit(uint.MaxValue);
            }
            catch { }
        }

private static int CountPlayerTasks(PlayerControl target)
        {
            int count = 0;
            try
            {
                if (target?.Data?.Tasks != null)
                {
                    foreach (NetworkedPlayerInfo.TaskInfo task in target.Data.Tasks)
                    {
                        if (task != null) count++;
                    }
                }
            }
            catch { }

            return count;
        }

private static List<byte> BuildRandomAssignableTaskIds(PlayerControl target)
        {
            List<byte> result = new List<byte>();
            HashSet<byte> currentTaskIds = GetCurrentPlayerTaskIds(target);
            int commonCount = 0;
            int longCount = 0;
            int shortCount = 0;
            try
            {
                if (GameOptionsManager.Instance?.CurrentGameOptions != null)
                {
                    commonCount = Mathf.Clamp(GameOptionsManager.Instance.CurrentGameOptions.GetInt(Int32OptionNames.NumCommonTasks), 0, 8);
                    longCount = Mathf.Clamp(GameOptionsManager.Instance.CurrentGameOptions.GetInt(Int32OptionNames.NumLongTasks), 0, 8);
                    shortCount = Mathf.Clamp(GameOptionsManager.Instance.CurrentGameOptions.GetInt(Int32OptionNames.NumShortTasks), 0, 12);
                }
            }
            catch { }

            int currentCount = CountPlayerTasks(target);
            if (commonCount + longCount + shortCount <= 0)
            {
                shortCount = Mathf.Clamp(currentCount > 0 ? currentCount : 3, 1, 12);
            }

            try
            {
                if (ShipStatus.Instance != null)
                {
                    AddRandomTaskTemplates(result, ShipStatus.Instance.CommonTasks, commonCount, currentTaskIds);
                    AddRandomTaskTemplates(result, ShipStatus.Instance.LongTasks, longCount, currentTaskIds);
                    AddRandomTaskTemplates(result, ShipStatus.Instance.ShortTasks, shortCount, currentTaskIds);
                }
            }
            catch { }

            if (result.Count == 0)
            {
                List<byte> fallback = GetLiveTaskTypeIds();
                List<byte> preferred = new List<byte>();
                List<byte> reused = new List<byte>();
                foreach (byte taskId in fallback)
                {
                    if (currentTaskIds.Contains(taskId)) reused.Add(taskId);
                    else preferred.Add(taskId);
                }

                ShuffleByteList(preferred);
                ShuffleByteList(reused);
                int desiredCount = Mathf.Clamp(currentCount > 0 ? currentCount : commonCount + longCount + shortCount, 1, 12);
                List<byte> selected = new List<byte>();
                AddTaskIdsUntilCount(selected, preferred, desiredCount);
                AddTaskIdsUntilCount(selected, reused, desiredCount);
                return selected;
            }

            return result;
        }

private static void AddRandomTaskTemplates(List<byte> output, Il2CppReferenceArray<NormalPlayerTask> templates, int count, HashSet<byte> excludedTaskIds = null)
        {
            if (output == null || templates == null || count <= 0) return;

            List<byte> preferredPool = new List<byte>();
            List<byte> reusedPool = new List<byte>();
            try
            {
                foreach (NormalPlayerTask task in templates)
                {
                    if (task == null) continue;
                    byte taskId = (byte)task.TaskType;
                    if (!preferredPool.Contains(taskId) && !reusedPool.Contains(taskId) && !output.Contains(taskId))
                    {
                        List<byte> pool = excludedTaskIds != null && excludedTaskIds.Contains(taskId) ? reusedPool : preferredPool;
                        pool.Add(taskId);
                    }
                }
            }
            catch { }

            ShuffleByteList(preferredPool);
            ShuffleByteList(reusedPool);
            int startCount = output.Count;
            AddTaskIdsUntilCount(output, preferredPool, startCount + count);
            AddTaskIdsUntilCount(output, reusedPool, startCount + count);
        }

private static void AddTaskIdsUntilCount(List<byte> output, List<byte> pool, int desiredCount)
        {
            if (output == null || pool == null) return;
            for (int i = 0; i < pool.Count && output.Count < desiredCount; i++)
            {
                byte taskId = pool[i];
                if (!output.Contains(taskId))
                    output.Add(taskId);
            }
        }

private static HashSet<byte> GetCurrentPlayerTaskIds(PlayerControl target)
        {
            HashSet<byte> ids = new HashSet<byte>();
            try
            {
                if (target?.Data?.Tasks != null)
                {
                    foreach (NetworkedPlayerInfo.TaskInfo task in target.Data.Tasks)
                    {
                        if (TryReadTaskInfoId(task, out byte taskId))
                            ids.Add(taskId);
                    }
                }
            }
            catch { }

            return ids;
        }

private static bool TryReadTaskInfoId(object taskInfo, out byte taskId)
        {
            taskId = 0;
            if (taskInfo == null) return false;

            string[] memberNames = { "TypeId", "TaskType", "TaskId", "Id", "taskType", "taskId" };
            Type type = taskInfo.GetType();
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            foreach (string memberName in memberNames)
            {
                try
                {
                    PropertyInfo property = type.GetProperty(memberName, flags);
                    if (property != null && TryConvertTaskIdValue(property.GetValue(taskInfo, null), out taskId))
                        return true;
                }
                catch { }

                try
                {
                    FieldInfo field = type.GetField(memberName, flags);
                    if (field != null && TryConvertTaskIdValue(field.GetValue(taskInfo), out taskId))
                        return true;
                }
                catch { }
            }

            return false;
        }

private static bool TryConvertTaskIdValue(object value, out byte taskId)
        {
            taskId = 0;
            if (value == null) return false;

            try
            {
                if (value is byte byteValue)
                {
                    taskId = byteValue;
                    return true;
                }

                if (value is TaskTypes taskType)
                {
                    taskId = (byte)taskType;
                    return true;
                }

                if (value is Enum)
                {
                    taskId = Convert.ToByte(value);
                    return true;
                }

                taskId = Convert.ToByte(value);
                return true;
            }
            catch
            {
                return false;
            }
        }

private static List<byte> GetLiveTaskTypeIds()
        {
            List<byte> available = new List<byte>();
            try
            {
                if (ShipStatus.Instance != null)
                {
                    var allTasks = ShipStatus.Instance.GetAllTasks();
                    if (allTasks != null)
                    {
                        foreach (PlayerTask task in allTasks)
                        {
                            if (task is NormalPlayerTask normal)
                            {
                                byte taskId = (byte)normal.TaskType;
                                if (!available.Contains(taskId))
                                    available.Add(taskId);
                            }
                        }
                    }
                }
            }
            catch { }

            return available;
        }

private static void ShuffleByteList(List<byte> values)
        {
            if (values == null) return;
            for (int i = values.Count - 1; i > 0; i--)
            {
                int swapIndex = UnityEngine.Random.Range(0, i + 1);
                byte temp = values[i];
                values[i] = values[swapIndex];
                values[swapIndex] = temp;
            }
        }
    }
}
