# 🌙 ElysiumModMenu - Among us 

**ElysiumModMenu** is an advanced **BepInEx IL2CPP mod menu for Among Us** with host tools, ESP, anti-cheat utilities, spoofing, local identity tweaks, chat QoL, and lobby automation.

Not just another menu. It is built for people who want more lobby control, cleaner info, better protection, and less nonsense from crashers or spammy modders.

> [!CAUTION]
> This menu includes powerful host-side and network-related tools. Use it responsibly. Private lobbies with friends are the best place for the wild stuff.

---

## 📦 Download

| Version | Status | Link |
| :--- | :--- | :--- |
| **v1.3.2** | Latest | [⬇️ Download ElysiumModMenu.dll](https://github.com/meowchelo/ElysiumModMenu/releases/latest) |

---

## ✨ What's New / Main Stuff

- 🧊 New name: **ElysiumModMenu**
- 📝 Fixed custom menu text fields: long text no longer squeezes or breaks the menu
- 📋 Paste/copy support almost everywhere: `Ctrl+V`, `Shift+Insert`, `Ctrl+C`, `Ctrl+X`
- 🌈 Local name spoof with no RPC broadcast, visible only on your client
- ✨ Name effects: `shimmer:Name`, `#68B6E7Name`, and raw rich text
- 🪪 Local fake Friend Code: any text, any symbols, local client only
- 🛰 ESP now shows: `Host - Lv:X - Platform spf - FriendCode`
- 🕵️ `spf` near platform means platform spoof is active
- 🛡 Anti-cheat tools, RPC sniffer, pet spam protection, vote-kick protection, and more

---

## 👤 Account / Local Spoof

- Fake level spoof
- Platform spoof with selectable platform
- Local name spoof without sending RPC
- Local fake Friend Code with no symbol limit
- Network Friend Code spoof for serialized player info
- Unlock cosmetics
- Guest name unlock

Local name examples:

```text
shimmer:Elysium
#68B6E7BlueName
<color=#68B6E7><b>RichName</b></color>
```

---

## 👁 Visuals / ESP

- See ghosts
- See player roles
- Player info above nameplates
- Info format: `Host - Lv:X - Platform spf - FriendCode`
- Reveal roles in meetings
- Reveal votes
- See players inside vents
- Full Bright
- Tracers
- Freecam with WASD
- Camera zoom with mouse wheel
- Always show chat
- Read ghost chat

---

## 🛡 Anti-Cheat / Protection

ElysiumModMenu helps protect your lobby from crashers, packet spam, and suspicious mod traffic.

- RPC protection
- Block spoof RPC
- Block sabotage and meeting abuse
- Block game RPC in lobby
- Block chat flood
- Block meeting flood
- RPC sniffer with known menu IDs
- Pet Spam Shield
- Auto-ban pet spammers when you are host
- Anti vote-kick
- Auto-kick Fortegreen / bugged color players
- Broken Friend Code detector
- Local persistent ban list

Ban list file:

```text
ElysiumModMenuBanList.txt
```

---

## 💬 Chat System

- Extended chat length
- Fast chat
- Links, email, and symbols support
- Chat history with Up / Down arrows
- Clipboard support
- Local chat log
- Whisper / private messages
- `/color` command
- Host filters for rainbow and Fortegreen abuse

Chat log:

```text
ChatLog.txt
```

---

## 👑 Host / Lobby Tools

- Auto Host
- Auto return after matches
- Fast start
- Wait for loaded players
- Force start
- Pre-game role manager
- Force impostors
- Force roles
- Kill all
- Kick all
- Report / eject / revive tools
- Morph / mass morph
- Spawn / despawn lobby
- Insta start
- Smart end game
- No task mode
- No setting limits

---

## 🚪 Sabotage / Doors

- Trigger Reactor
- Trigger O2
- Trigger Comms
- Trigger Lights
- Trigger all sabotages at once
- Fix all sabotages
- Close all doors
- Open all doors
- Lock / unlock doors by room

---

## 🎮 Menu Controls

Default menu toggle:

```text
Insert
```

You can also set custom keybinds inside the menu.

---

## 📋 Text Fields

The menu uses custom input fields. Long values are visually clipped, so the UI stays clean while you keep editing normally.

Hotkeys:

```text
Ctrl+V        paste
Shift+Insert  paste
Ctrl+C        copy current field
Ctrl+X        cut current field
Backspace     delete last character
Esc           stop editing
```

---

## ⚙️ Installation

You need **BepInEx IL2CPP** first.

1. Download BepInEx IL2CPP.
2. Extract BepInEx into your Among Us folder, next to `Among Us.exe`.
3. Run the game once.
4. Close the game.
5. Download `ElysiumModMenu.dll`.
6. Put the DLL here:

```text
Among Us/BepInEx/plugins
```

7. Launch Among Us.

---

## 📁 Finding Your Among Us Folder

Steam:

```text
Library → Among Us → Manage → Browse local files
```

Epic Games:

```text
Library → Among Us → Manage → Open install folder
```

Xbox App:

```text
Manage → Files → Browse
```

Itch.io:

```text
Manage → Open folder in Explorer
```

---

## 🖼 Custom Background

You can use your own menu background:

```text
Among Us/BepInEx/config/ElysiumModMenu/MenuBG.png
```

or

```text
Among Us/BepInEx/config/ElysiumModMenu/MenuBG.jpg
```

---

## 🛠 Build

The project builds with .NET 6:

```powershell
dotnet build .\NjordMenu.csproj
```

Output:

```text
bin/Debug/net6.0/ElysiumModMenu.dll
```

---

## 📸 Screenshots

<img width="1884" height="1020" alt="Anti-cheat and protections" src="https://github.com/user-attachments/assets/c4a2a364-bd2f-44e4-a27c-8d299ddd8415" />

<img width="1919" height="1079" alt="Host and lobby controls" src="https://github.com/user-attachments/assets/8373b7c4-a0d2-4762-bf02-38263ad04636" />

<img width="1806" height="918" alt="Visuals and ESP" src="https://github.com/user-attachments/assets/69735f5d-31db-462e-abdb-de2dcce11f6a" />

---

## ⚠️ Disclaimer

ElysiumModMenu is not affiliated with Innersloth and is not officially supported by Among Us.

Use it at your own risk. Do not ruin public games, crash lobbies, or abuse normal players. If you use the menu maliciously, do not expect support.
