# ElysiumModMenu

Advanced BepInEx IL2CPP mod menu for Among Us with host tools, anti-cheat utilities, visual ESP, account spoofing, local-only identity tweaks, chat quality-of-life features, and lobby automation.

> [!CAUTION]
> ElysiumModMenu contains powerful host and network tools. Use it responsibly, preferably in private lobbies with consenting players. This project is not affiliated with Innersloth.

## Release

| Version | Status | Download |
| :--- | :--- | :--- |
| v1.3.2 | Latest | [Download ElysiumModMenu.dll](https://github.com/meowchelo/ElysiumModMenu/releases/latest) |

## Highlights

- Clean IMGUI menu with custom themes, RGB accent mode, background image support, custom keybinds, and clipboard-friendly text fields.
- Local name spoofing with no RPC broadcast. Supports plain text, rich text, `shimmer:Name`, and quick hex color syntax like `#68B6E7Name`.
- Local fake Friend Code spoofing for your own client UI, with any symbols or text. The local fake FC is restored before network serialization unless real FC spoof is enabled.
- Real Friend Code spoof option for serialized player info, still sanitized to guest-style codes.
- ESP player info line: `Host - Lv:X - Platform spf - FriendCode`. The `spf` marker appears when platform spoofing is active for the local player.
- Menu text fields are clipped so long values do not squeeze or break the menu layout.
- Paste/copy support across menu input fields with `Ctrl+V`, `Shift+Insert`, `Ctrl+C`, and `Ctrl+X`.

## Features

### Account And Local Spoofing

- Fake level spoof.
- Platform spoof with selectable platform.
- Local name spoof that only changes your own client view.
- Local fake Friend Code for player info, history, join notifications, and local UI.
- Network Friend Code spoof for outgoing serialized data.
- Unlock cosmetics and guest account name features.

### Visuals And ESP

- Show ghosts.
- Reveal player roles above names.
- Show player info above names: host flag, level, platform, platform spoof marker, and Friend Code.
- Reveal meeting roles and votes.
- See players inside vents.
- Full Bright mode.
- Tracers.
- Freecam and camera zoom.
- Always show chat and read ghost chat.

### Anti-Cheat And Protections

- RPC protection toggles for spoof RPCs, sabotage/meeting abuse, game RPCs in lobby, chat floods, and meeting floods.
- Mod/RPC sniffer with known menu IDs.
- Pet spam local drop and optional host auto-ban.
- Anti vote-kick protection.
- Fortegreen and broken Friend Code checks.
- Persistent ban list stored locally.

### Host And Lobby Tools

- Auto-host system with minimum players, start delay, fast-start threshold, load wait, auto-return, and force-start controls.
- Pre-game role manager.
- Force impostors and roles before the game starts.
- Kill, kick, report, eject, revive, morph, mass morph, and task tools.
- Spawn/despawn lobby.
- Instant start and smart end-game actions.
- No task mode and no setting limits.

### Sabotage And Door Tools

- Trigger reactor, O2, comms, and lights sabotage.
- Fix all sabotages.
- Close, open, lock, or unlock all doors.
- Per-room door controls.

### Chat System

- Extended chat length.
- Fast chat.
- Links, email, and symbol support.
- Chat history navigation.
- Clipboard support in game chat text boxes.
- Local chat log saved to `ChatLog.txt`.
- Whisper/private message commands.
- Color command support.
- Host filters for rainbow/Fortegreen abuse.

## Text Input Notes

ElysiumModMenu has custom menu inputs instead of default text boxes. Long values are clipped visually and keep editing normally without resizing the menu.

Supported shortcuts:

- `Ctrl+V` or `Shift+Insert`: paste.
- `Ctrl+C`: copy current field text.
- `Ctrl+X`: cut current field text.
- `Backspace`: delete last character.
- `Esc`: stop editing.

Local name examples:

```text
shimmer:Elysium
#68B6E7BlueName
<color=#68B6E7><b>RichName</b></color>
```

## Installation

ElysiumModMenu is a BepInEx IL2CPP plugin. BepInEx must be installed first.

1. Download BepInEx IL2CPP from the official BepInEx releases.
2. Extract BepInEx into the Among Us game folder, next to `Among Us.exe`.
3. Run the game once so BepInEx can generate its folders.
4. Close the game.
5. Download `ElysiumModMenu.dll` from releases.
6. Put `ElysiumModMenu.dll` into `Among Us/BepInEx/plugins`.
7. Launch Among Us.

Default menu toggle: `Insert` or `Right Shift`.

Custom background:

```text
Among Us/BepInEx/config/ElysiumModMenu/MenuBG.png
```

## Game Folder Help

- Steam: right-click Among Us in Library, then `Manage`, then `Browse local files`.
- Epic Games: open game options, then manage/open install folder.
- Itch.io: right-click Among Us, then `Manage`, then `Open folder in Explorer`.
- Xbox app: game settings, then `Manage`, then `Files`, then browse.

## Build

The project targets `.NET 6` and uses the Among Us BepInEx/interop assemblies.

```powershell
dotnet build .\NjordMenu.csproj
```

Output:

```text
bin/Debug/net6.0/ElysiumModMenu.dll
```

## Screenshots

<img width="1884" height="1020" alt="Anti-cheat and protections" src="https://github.com/user-attachments/assets/c4a2a364-bd2f-44e4-a27c-8d299ddd8415" />

<img width="1919" height="1079" alt="Host and lobby controls" src="https://github.com/user-attachments/assets/8373b7c4-a0d2-4762-bf02-38263ad04636" />

<img width="1806" height="918" alt="Visuals and ESP" src="https://github.com/user-attachments/assets/69735f5d-31db-462e-abdb-de2dcce11f6a" />

## Disclaimer

This mod is not affiliated with, endorsed by, sponsored by, or approved by Innersloth LLC. Among Us and related assets belong to Innersloth LLC.

Use ElysiumModMenu at your own risk. Misuse may disrupt other players and may result in moderation action from game services. Support is not provided for malicious use.
