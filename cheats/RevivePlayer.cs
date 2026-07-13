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
public static void RevivePlayer(PlayerControl target)
        {
            if (target == null || target.Data == null)
            {
                ShowNotification("<color=#FF0000>[ERROR]</color> Target not found!");
                return;
            }
            if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
            {
                ShowNotification("<color=#FF0000>[ERROR]</color> Host required!");
                return;
            }
            if (!target.Data.IsDead)
            {
                ShowNotification($"{target.Data.PlayerName} is already alive!");
                return;
            }

            try
            {
                target.Data.IsDead = false;

                if (target.Collider != null) target.Collider.enabled = true;

                if (target.MyPhysics != null)
                    target.MyPhysics.gameObject.layer = LayerMask.NameToLayer("Players");

                try
                {
                    var allBehaviours = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>();
                    foreach (var mb in allBehaviours)
                    {
                        if (mb == null || mb.gameObject == null) continue;
                        Type t = mb.GetType();
                        if (t == null || t.Name != "DeadBody") continue;

                        byte parentId = byte.MaxValue;

                        var parentProp = t.GetProperty("ParentId", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (parentProp != null)
                        {
                            object val = parentProp.GetValue(mb, null);
                            if (val is byte b) parentId = b;
                            else if (val is int i) parentId = (byte)i;
                        }
                        else
                        {
                            var parentField = t.GetField("ParentId", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            if (parentField != null)
                            {
                                object val = parentField.GetValue(mb);
                                if (val is byte b) parentId = b;
                                else if (val is int i) parentId = (byte)i;
                            }
                        }

                        if (parentId == target.PlayerId)
                            mb.gameObject.SetActive(false);
                    }
                }
                catch { }

                bool wasImpTeam = false;
                try
                {
                    if (target.Data.Role != null)
                    {
                        int roleId = (int)target.Data.Role.Role;
                        wasImpTeam = roleId == 1 || roleId == 5 || roleId == 7 || roleId == 9 || roleId == 18;
                    }
                    else
                    {
                        var rt = target.Data.RoleType;
                        wasImpTeam = rt == RoleTypes.Impostor || rt == RoleTypes.Shapeshifter || (int)rt == 9 || (int)rt == 18;
                    }
                }
                catch { }

                target.RpcSetRole(wasImpTeam ? RoleTypes.Impostor : RoleTypes.Crewmate, true);

                var netObj = GameData.Instance?.GetComponent<InnerNetObject>();
                if (netObj != null) netObj.SetDirtyBit(uint.MaxValue);

                ShowNotification($"<color=#00FF00>[REVIVE]</color> {target.Data.PlayerName} revived!");
            }
            catch (Exception)
            {
                ShowNotification("<color=#FF0000>Revive failed!</color>");
            }
    }
}
}
