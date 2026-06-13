# ✦ ElysiumModMenu — Among Us

♡ **ElysiumModMenu** is a BepInEx IL2CPP mod menu for **Among Us**, designed as an all-in-one menu for private lobbies, testing, host-side controls, visual tools, chat improvements, cosmetics, and lobby management.

The menu includes account/local spoof options, ESP tools, anti-cheat protection, chat utilities, sabotage and door controls, outfit presets, host tools, keybinds, customization options, and multilingual UI support.

> ⚠️ **Warning**
> ElysiumModMenu includes powerful host-side and network-related features.
> Use it only in private lobbies, testing environments, or with friends who agreed to play with mods.
> Do not use it to ruin public games, crash lobbies, harass players, or abuse random users.

---

## 𖤐 Version

**Current version:** `v1.3.5`

♡ Download: [ElysiumModMenu.dll](#)

---

## ✦ Main Menu Sections

The menu is organized into multiple tabs for quick access:

* `GENERAL`
* `SELF`
* `VISUALS`
* `PLAYERS`
* `SABOTAGES`
* `HOST ONLY`
* `OUTFITS`
* `VOTEKICK`
* `MENU`
* `MAPS`
* `ANIMATIONS`
* `INFORMATION`
* `KEYBINDS`
* `WELCOME`
* `CREDITS`

---

## ♡ General

The **General** section contains useful global features and quality-of-life options.

Main features:

* FPS limiter with configurable cap
* Unlock cosmetics
* More lobby info in the public game list
* Auto Ghost After Start
* Allow duplicate player colors
* Menu watermark toggle
* RGB menu color mode
* Custom menu colors
* Multi-language menu support
* Basic spell check
* Anomaly log reports for diagnostic and anti-cheat fixes

---

## ✦ Self / Account

The **Self** section focuses on local player options, profile display, spoofing, and client-side changes.

Main features:

* Level spoof
* Platform spoof with selectable platform
* Local name spoof
* Local fake Friend Code
* Network Friend Code spoof
* Guest name unlock
* Cosmetic unlocks
* Client-side visual name effects

Name effect examples:

```text
shimmer:Elysium
#68B6E7BlueName
<color=#68B6E7><b>RichName</b></color>
```

---

## 𖤐 Visuals / ESP

The **Visuals** section adds extra information and visibility options during gameplay.

Main features:

* See ghosts
* See player roles
* Show player info above nameplates
* Show Host, level, platform, and Friend Code
* Hide Friend Code display if needed
* Friend ESP ignore list
* Reveal roles during meetings
* Reveal votes
* See players inside vents
* Full Bright
* Tracers
* Free camera with WASD movement and scroll zoom
* Always show chat
* Read ghost chat

The `spf` marker near a platform means that platform spoofing is active on that player.

---

## ♡ Players

The **Players** section is used for player-related actions and quick player management.

Main features:

* Select players from the lobby or match
* View player information
* Apply actions to selected players
* Save selected player outfit
* Use selected player data with other menu tools
* Manage player-related ESP visibility

---

## ✦ Anti-Cheat / Protection

The menu includes protection tools for hosts and private lobbies.

Main features:

* RPC protection with configurable rate limit
* Block spoof RPC
* Block sabotage abuse
* Block meeting abuse
* Block game RPC in lobby
* Block chat flood
* Block meeting flood
* RPC sniffer with known mod menu IDs
* Anti vote-kick
* Auto-kick Fortegreen and bugged color players
* Auto-ban platform spoofers as host
* Broken Friend Code detector
* Pet Spam Shield
* Local persistent ban list
* Bot ban list with persistent storage
* Friend ESP ignore list

Config files:

```text
ElysiumModMenuBanList.txt
ElysiumPlatformBanList.txt
ElysiumBotBanList.txt
ElysiumFriendEspIgnore.txt
```

---

## 𖤐 Chat

The **Chat** section improves the in-game chat and adds more flexible input options.

Main features:

* Extended chat length
* Fast chat
* Links, email, and symbol support
* Chat history with Up / Down arrows
* Clipboard support
* Local chat log
* Whisper / private messages
* `/color` command
* Dark chat theme
* Custom ghost chat color
* Basic spell check
* Host filters for rainbow names and Fortegreen abuse
* Allow duplicate player colors

Chat log file:

```text
ChatLog.txt
```

---

## ♡ Outfits

The **Outfits** section lets you save and restore cosmetic loadouts.

Main features:

* Favorite Outfits system
* 4 outfit slots
* Save your current outfit
* Save the selected player's outfit
* Restore saved outfit slots with one click
* Full cosmetic loadout support

---

## ✦ Host / Lobby Tools

The **Host Only** section contains tools designed for hosts and private lobby control.

Main features:

* Auto Host
* Auto return after matches
* Fast start
* Wait for loaded players
* Force start
* Pre-game role manager
* Force impostors
* Force roles
* Kick all
* Report / eject / revive
* Morph / mass morph
* Spawn / despawn in lobby
* Insta start
* Smart end game
* No task mode
* No setting limits
* Auto Ghost After Start

> ⚠️ Host tools should only be used in private lobbies with consent from other players.

---

## 𖤐 Sabotage / Doors

The **Sabotage** section provides quick sabotage and door controls.

Main features:

* Trigger Reactor
* Trigger O2
* Trigger Comms
* Trigger Lights
* Trigger all sabotages
* Fix all sabotages
* Close all doors
* Open all doors
* Lock doors by room
* Unlock doors by room

---

## ♡ VoteKick

The **VoteKick** section includes tools related to vote-kick behavior and lobby protection.

Main features:

* Anti vote-kick
* Disable vote kicks as host
* Protection against abusive vote-kick behavior
* Host-side control for private lobbies

---

## ✦ Menu Customization

The **Menu** section contains customization and interface options.

Main features:

* RGB menu color mode
* Custom menu color selection
* Menu watermark toggle
* Custom menu background
* Dark chat theme toggle
* Custom keybinds
* Multi-language interface
* Menu language auto-detection

Supported menu languages include:

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

You can rebind the menu key inside the menu under the **Keybinds** section.

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

![Anti-cheat and protections](https://github.com/user-attachments/assets/c4a2a364-bd2f-44e4-a27c-8d299ddd8415)

![Host and lobby controls](https://github.com/user-attachments/assets/8373b7c4-a0d2-4762-bf02-38263ad04636)

![Visuals and ESP](https://github.com/user-attachments/assets/69735f5d-31db-462e-abdb-de2dcce11f6a)

---

## ♡ Disclaimer

ElysiumModMenu is not affiliated with Innersloth and is not officially supported by Among Us.

Use it at your own risk.
Do not crash public lobbies, grief random players, abuse people, or ruin games for others.
Support will not be provided for abusive use.
