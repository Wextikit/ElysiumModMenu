# ✦ ElysiumModMenu — Among Us

♡ **ElysiumModMenu** is a BepInEx IL2CPP mod menu for **Among Us**.  
It combines private lobby tools, visual options, chat improvements, cosmetics, host controls, and menu customization in one place.

> [!WARNING]
> This menu includes powerful host-side and network-related features.  
> Use it only in private lobbies, test rooms, or with friends who agreed to play with mods.  
> Do not use it to ruin public games, crash lobbies, harass players, or abuse random users.

---

## 𖤐 Version

**Current version:** `v1.3.5`

♡ Download: https://github.com/meowchelo/ElysiumModMenu/releases/tag/v1.3.5

---

## ✦ Main Menu Sections

The menu is organized into clean tabs for quick access:

* `GENERAL`
* `SELF`
* `VISUALS`
* `PLAYERS`
* `SABOTAGES`
* `HOST ONLY`
* `OUTFITS`
* `VOTEKICK`
* `MENU`
* `KEYBINDS`

---

## ♡ General

The **General** section contains global features and quality-of-life options.

* FPS limiter with configurable cap
* Unlock cosmetics
* More lobby info in the game list

---

## ✦ Self / Account

The **Self** section focuses on local player options and profile changes.

* Level spoof
* Platform spoof
* Local name and Friend Code spoof

Name effect examples:

```text
shimmer:Elysium
#68B6E7BlueName
<color=#68B6E7><b>RichName</b></color>
```

---

## 𖤐 Visuals / ESP

The **Visuals** section adds extra information and visibility options during gameplay.

* See ghosts and player roles
* Show player info above nameplates
* Free camera with zoom

---

## ♡ Players

The **Players** section is used for player-related actions and quick management.

* Select players from the lobby or match
* View player information
* Use selected player data with menu tools

---

## ✦ Anti-Cheat / Protection

The menu includes protection tools for hosts and private lobbies.

* RPC protection and rate limit
* Block chat, meeting, and sabotage abuse
* Local ban lists and bot protection

Config files:

```text
ElysiumModMenuBanList.txt
ElysiumPlatformBanList.txt
ElysiumBotBanList.txt
ElysiumFriendEspIgnore.txt
```

---

## 𖤐 Chat

The **Chat** section improves the in-game chat experience.

* Extended and fast chat
* Chat history and clipboard support
* Dark chat theme and custom ghost chat color

Chat log file:

```text
ChatLog.txt
```

---

## ♡ Outfits

The **Outfits** section lets you save and restore cosmetic loadouts.

* Favorite Outfits system
* 4 outfit slots
* Save your outfit or selected player outfit

---

## ✦ Host / Lobby Tools

The **Host Only** section contains tools for private lobby control.

* Auto Host and fast start
* Role manager and force roles
* Smart end game and lobby automation

> [!WARNING]
> Host tools should only be used in private lobbies with consent from other players.

---

## 𖤐 Sabotage / Doors

The **Sabotage** section provides quick sabotage and door controls.

* Trigger or fix sabotages
* Close or open all doors
* Lock or unlock doors by room

---

## ♡ VoteKick

The **VoteKick** section includes tools related to vote-kick behavior.

* Anti vote-kick
* Disable vote kicks as host
* Protection against vote-kick abuse

---

## ✦ Menu Customization

The **Menu** section contains interface and style options.

* RGB menu color mode
* Menu watermark toggle
* Custom menu background

Supported menu languages:

```text
English, Русский, Deutsch, Français, Español, Italiano, Português,
Polski, Nederlands, Türkçe, Čeština, Română, Magyar, Svenska,
Dansk, Suomi, Norsk, Українська, Ελληνικά, 中文, 日本語, 한국어
```

---

## 𖤐 Keybinds

Default toggle key:

```text
Insert
```

You can rebind the menu key inside the **Keybinds** section.

---

## ♡ Text Fields

The menu uses custom input fields. Long values may be visually clipped, but typing still works normally.

| Key                       | Action                |
| ------------------------- | --------------------- |
| `Ctrl+V` / `Shift+Insert` | Paste                 |
| `Ctrl+C`                  | Copy field            |
| `Ctrl+X`                  | Cut field             |
| `Backspace`               | Delete last character |
| `Esc`                     | Stop editing          |

---

## ✦ Custom Menu Background

Place your background image here:

```text
Among Us/BepInEx/config/ElysiumModMenu/MenuBG.png
```

`.jpg` is also supported.

---

## 𖤐 Installation

> [!WARNING]
> Before installing ElysiumModMenu, make sure **BepInEx IL2CPP** is installed correctly.  
> Use the version that matches your Among Us build and game architecture.

You need **BepInEx IL2CPP** first.

1. Download BepInEx IL2CPP.
2. Extract it into your Among Us folder, next to `Among Us.exe`.
3. Run the game once, then close it.
4. Download `ElysiumModMenu.dll`.
5. Drop it into:

```text
Among Us/BepInEx/plugins/
```

6. Launch Among Us.

---

## ♡ Finding Your Among Us Folder

**Steam:**  
Library → Among Us → Manage → Browse local files

**Epic Games:**  
Library → Among Us → Manage → Open install folder

**Xbox App:**  
Manage → Files → Browse

**Itch.io:**  
Manage → Open folder in Explorer

---

## ✦ Build

The project targets `.NET 6`.

```powershell
dotnet build .\NjordMenu.csproj
```

Output:

```text
bin/Debug/net6.0/ElysiumModMenu.dll
```

---

## 𖤐 Screenshots

<img width="1919" height="1079" alt="Снимок экрана 2026-06-14 045423" src="https://github.com/user-attachments/assets/7e64700e-4970-49a6-bedc-d20f6b64c1fb" />

<img width="1919" height="1079" alt="Снимок экрана 2026-06-14 045518" src="https://github.com/user-attachments/assets/61308a65-631f-498f-aa44-92a5dfe492c3" />

<img width="1919" height="1079" alt="Снимок экрана 2026-06-14 045615" src="https://github.com/user-attachments/assets/bc8705da-0efc-4eb9-ae57-f53730eee1bf" />

---

## ♡ Disclaimer

> [!CAUTION]
> ElysiumModMenu is not affiliated with Innersloth and is not officially supported by Among Us.  
> Use it at your own risk.  
> Do not crash public lobbies, grief random players, abuse people, or ruin games for others.  
> Support will not be provided for abusive use.
