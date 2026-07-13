#nullable disable
#pragma warning disable CS0162, CS0108, CS0219, CS0661, CS0660, CS8632, CS0168, CS0659
using ElysiumModMenu;
using HarmonyLib;
using InnerNet;
using TMPro;
using UnityEngine;

[HarmonyPatch(typeof(GameContainer), nameof(GameContainer.SetupGameInfo))]
public static class MoreLobbyInfo_GameContainer_SetupGameInfo_Postfix
{
    private const string CenteredHostNameObject = "ElysiumCenteredLobbyHostName";
    private sealed class StyledLobbyName
    {
        public TMP_Text Text;
        public TMP_Text InfoText;
        public string HostName;
        public string Capacity;
        public string RoomCode;
        public string Platform;
        public string LobbyTime;
        public string OriginalInfoText;
    }

    private static readonly System.Collections.Generic.List<StyledLobbyName> StyledNames =
        new System.Collections.Generic.List<StyledLobbyName>();
    private static float NextStyleUpdateAt;

    public static void UpdateStyledNames(bool force = false)
    {
        if (!force && Time.unscaledTime < NextStyleUpdateAt) return;
        NextStyleUpdateAt = Time.unscaledTime + 0.05f;

        for (int i = StyledNames.Count - 1; i >= 0; i--)
        {
            try
            {
                StyledLobbyName entry = StyledNames[i];
                if (entry == null || entry.Text == null || entry.InfoText == null)
                {
                    StyledNames.RemoveAt(i);
                    continue;
                }

                entry.Text.gameObject.SetActive(ElysiumModMenuGUI.moreLobbyInfo);
                if (!ElysiumModMenuGUI.moreLobbyInfo)
                {
                    entry.InfoText.text = entry.OriginalInfoText;
                    continue;
                }

                string styledName = ElysiumModMenuGUI.rgbMenuMode
                    ? $"<color=#{ElysiumModMenuGUI.GetMenuControlAccentHex()}>{entry.HostName}</color>"
                    : ElysiumModMenuGUI.ApplyMenuShimmer(entry.HostName);
                entry.Text.text = $"<size=75%><b>{styledName}</b></size>";

            }
            catch
            {
                StyledNames.RemoveAt(i);
            }
        }
    }

    private static string StyleInfo(string value)
    {
        return ElysiumModMenuGUI.rgbMenuMode
            ? $"<color=#{ElysiumModMenuGUI.GetMenuControlAccentHex()}>{value}</color>"
            : ElysiumModMenuGUI.ApplyMenuShimmer(value);
    }

    private static void RegisterStyledName(TMP_Text text, TMP_Text infoText, string hostName,
        string capacity, string roomCode, string platform, string lobbyTime)
    {
        for (int i = StyledNames.Count - 1; i >= 0; i--)
        {
            try
            {
                if (StyledNames[i]?.Text == text)
                {
                    StyledNames[i].HostName = hostName;
                    StyledNames[i].InfoText = infoText;
                    StyledNames[i].Capacity = capacity;
                    StyledNames[i].RoomCode = roomCode;
                    StyledNames[i].Platform = platform;
                    StyledNames[i].LobbyTime = lobbyTime;
                    return;
                }
            }
            catch { StyledNames.RemoveAt(i); }
        }

        StyledNames.Add(new StyledLobbyName
        {
            Text = text,
            InfoText = infoText,
            HostName = hostName,
            Capacity = capacity,
            RoomCode = roomCode,
            Platform = platform,
            LobbyTime = lobbyTime,
            OriginalInfoText = infoText.text
        });
    }

    public static void Postfix(GameContainer __instance)
    {
        if (__instance == null || __instance.capacity == null) return;

        // Anchor the label to this card's background and derive the target from
        // its real bounds. This stays stable across resolution and list scaling.
        if (__instance.mapBackground == null) return;
        Transform nameParent = __instance.mapBackground.transform;
        if (nameParent == null) return;
        Transform existingName = nameParent.Find(CenteredHostNameObject);
        if (!ElysiumModMenuGUI.moreLobbyInfo)
        {
            if (existingName != null) existingName.gameObject.SetActive(false);
            return;
        }

        string trueHostName = EscapeTmpText(__instance.gameListing.TrueHostName);
        if (string.IsNullOrWhiteSpace(trueHostName)) trueHostName = "Unknown";

        string roomCode = GameCode.IntToGameName(__instance.gameListing.GameId);
        if (string.IsNullOrWhiteSpace(roomCode)) roomCode = "Unknown";

        int age = __instance.gameListing.Age;
        string lobbyTime = $"Age: {age / 60}:{(age % 60 < 10 ? "0" : string.Empty)}{age % 60}";
        string platform = GetPlatformName((int)__instance.gameListing.Platform);
        string capacity = $"{__instance.gameListing.PlayerCount}/{__instance.gameListing.MaxPlayers}";

        TMP_Text centeredName;
        if (existingName == null)
        {
            centeredName = UnityEngine.Object.Instantiate(__instance.capacity);
            centeredName.gameObject.name = CenteredHostNameObject;
            centeredName.transform.SetParent(nameParent, true);
        }
        else
        {
            centeredName = existingName.GetComponent<TMP_Text>();
        }

        if (centeredName != null)
        {
            centeredName.gameObject.SetActive(true);
            RegisterStyledName(centeredName, __instance.capacity, trueHostName,
                capacity, roomCode, platform, lobbyTime);
            centeredName.alignment = TextAlignmentOptions.Center;
            centeredName.enableAutoSizing = true;
            centeredName.fontSizeMin = 0.7f;
            centeredName.fontSizeMax = 2.4f;
            centeredName.overflowMode = TextOverflowModes.Ellipsis;

            RectTransform rect = centeredName.rectTransform;
            RectTransform capacityRect = __instance.capacity.rectTransform;
            Bounds cardBounds = __instance.mapBackground.bounds;
            float targetX = cardBounds.center.x - cardBounds.extents.x * 0.225f;
            rect.position = new Vector3(targetX, cardBounds.center.y, capacityRect.position.z - 1f);
            rect.sizeDelta = new Vector2(3.5f, 0.6f);
            centeredName.transform.SetAsLastSibling();
            centeredName.raycastTarget = false;
            Renderer nameRenderer = centeredName.GetComponent<Renderer>();
            Renderer capacityRenderer = __instance.capacity.GetComponent<Renderer>();
            if (nameRenderer != null && capacityRenderer != null)
            {
                nameRenderer.sortingLayerID = capacityRenderer.sortingLayerID;
                nameRenderer.sortingOrder = capacityRenderer.sortingOrder + 10;
            }
        }

        UpdateStyledNames(true);
    }

    private static string EscapeTmpText(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value.Replace("<", "\u2039").Replace(">", "\u203A");
    }

    private static string GetPlatformName(int platformId)
    {
        return platformId switch
        {
            1 => "Epic",
            2 => "Steam",
            3 => "Mac",
            4 => "Microsoft Store",
            5 => "Itch.io",
            6 => "iOS",
            7 => "Android",
            8 => "Nintendo Switch",
            9 => "Xbox",
            10 => "PlayStation",
            112 => "Starlight",
            _ => "Unknown"
        };
    }
}
