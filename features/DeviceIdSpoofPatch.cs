#nullable disable
#pragma warning disable CS0162, CS0108, CS0219, CS0661, CS0660, CS8632, CS0168, CS0659
using ElysiumModMenu;
using HarmonyLib;
using System;
using System.Security.Cryptography;
using UnityEngine;

[HarmonyPatch(typeof(SystemInfo), nameof(SystemInfo.deviceUniqueIdentifier), MethodType.Getter)]
public static class DeviceIdSpoofPatch
{
    public static void Postfix(ref string __result)
    {
        if (!ElysiumModMenuGUI.enableDeviceIdSpoof) return;

        if (string.IsNullOrEmpty(ElysiumModMenuGUI.spoofedDeviceId))
        {
            byte[] bytes = new byte[16];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
                rng.GetBytes(bytes);

            ElysiumModMenuGUI.spoofedDeviceId = BitConverter.ToString(bytes).Replace("-", "").ToLower();
            PlayerPrefs.SetString("M_DeviceId", ElysiumModMenuGUI.spoofedDeviceId);
            PlayerPrefs.Save();
        }

        __result = ElysiumModMenuGUI.spoofedDeviceId;
    }
}
