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


[HarmonyPatch(typeof(FriendsListManager), nameof(FriendsListManager.SetFriendCode))]
public static class PublicFriendCodeRegistrationPatch
{
    private static bool fallbackRequest;

    public static bool Prefix(
        [HarmonyArgument(0)] ref string username,
        [HarmonyArgument(1)] ref Il2CppSystem.Action<Assets.InnerNet.ResponseState,
            Assets.InnerNet.Response<Assets.InnerNet.ResponseFriendCode>> resultCallback)
    {
        if (fallbackRequest || !ElysiumModMenuGUI.enableFriendCodeSpoof) return true;

        string value = (ElysiumModMenuGUI.spoofFriendCodeInput ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(value)) return true;

        int hash = value.LastIndexOf('#');
        if (hash <= 0)
        {
            username = value;
            return true;
        }

        string fallback = value.Substring(0, hash);
        string suffix = value.Substring(hash + 1);
        if (string.IsNullOrEmpty(suffix))
        {
            username = fallback;
            return true;
        }

        for (int i = 0; i < suffix.Length; i++)
        {
            if (suffix[i] >= '0' && suffix[i] <= '9') continue;
            username = fallback;
            return true;
        }

        username = value;
        var original = resultCallback;
        resultCallback = Il2CppInterop.Runtime.DelegateSupport.ConvertDelegate<Il2CppSystem.Action<
            Assets.InnerNet.ResponseState, Assets.InnerNet.Response<Assets.InnerNet.ResponseFriendCode>>>(
            new System.Action<Assets.InnerNet.ResponseState, Assets.InnerNet.Response<Assets.InnerNet.ResponseFriendCode>>(
                (state, response) =>
                {
                    if (state == Assets.InnerNet.ResponseState.Failed)
                    {
                        try
                        {
                            FriendsListManager mgr = FriendsListManager.Instance;
                            if (mgr != null)
                            {
                                fallbackRequest = true;
                                mgr.SetFriendCode(fallback, original);
                                return;
                            }
                        }
                        catch { }
                        finally
                        {
                            fallbackRequest = false;
                        }
                    }

                    if (original != null)
                        original.Invoke(state, response);
                }));
        return true;
    }
}

[HarmonyPatch(typeof(EditAccountUsername), nameof(EditAccountUsername.OnEnable))]
public static class PublicFriendCodeScreenPatch
{
    public static void Postfix(EditAccountUsername __instance)
    {
        if (!ElysiumModMenuGUI.enableFriendCodeSpoof || __instance == null) return;

        string value = (ElysiumModMenuGUI.spoofFriendCodeInput ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(value)) return;

        if (__instance.UsernameText != null)
            __instance.UsernameText.text = value;

        try
        {
            var buttons = __instance.GetComponentsInChildren<PassiveButton>(true);
            for (int i = 0; i < buttons.Count; i++)
                buttons[i]?.SetButtonEnableState(true);
        }
        catch { }
    }
}

[HarmonyPatch(typeof(EditAccountUsername), nameof(EditAccountUsername.RandomizeName))]
public static class PublicFriendCodeRandomPatch
{
    public static void Postfix(EditAccountUsername __instance)
    {
        if (!ElysiumModMenuGUI.enableFriendCodeSpoof || __instance == null || __instance.UsernameText == null) return;

        string value = (ElysiumModMenuGUI.spoofFriendCodeInput ?? string.Empty).Trim();
        if (!string.IsNullOrEmpty(value))
            __instance.UsernameText.text = value;
    }
}

[HarmonyPatch(typeof(EditAccountUsername), nameof(EditAccountUsername.SaveUsername))]
public static class PublicFriendCodeConfirmPatch
{
    public static bool Prefix(EditAccountUsername __instance)
    {
        if (!ElysiumModMenuGUI.enableFriendCodeSpoof || __instance == null) return true;

        string value = (ElysiumModMenuGUI.spoofFriendCodeInput ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(value)) return true;

        try
        {
            FriendsListManager mgr = FriendsListManager.Instance;
            if (mgr == null) return true;

            if (__instance.UsernameText != null)
                __instance.UsernameText.text = value;

            var callback = Il2CppInterop.Runtime.DelegateSupport.ConvertDelegate<Il2CppSystem.Action<
                Assets.InnerNet.ResponseState, Assets.InnerNet.Response<Assets.InnerNet.ResponseFriendCode>>>(
                new System.Action<Assets.InnerNet.ResponseState, Assets.InnerNet.Response<Assets.InnerNet.ResponseFriendCode>>(
                    __instance._SaveUsername_b__8_0));
            mgr.SetFriendCode(value, callback);
            return false;
        }
        catch
        {
            return true;
        }
    }
}

[HarmonyPatch(typeof(PassiveButton), nameof(PassiveButton.SetButtonEnableState))]
public static class PublicFriendCodeButtonPatch
{
    public static void Prefix(PassiveButton __instance, [HarmonyArgument(0)] ref bool enable)
    {
        if (!ElysiumModMenuGUI.enableFriendCodeSpoof || __instance == null || enable) return;

        try
        {
            if (__instance.GetComponentInParent<EditAccountUsername>() != null)
                enable = true;
        }
        catch { }
    }
}
