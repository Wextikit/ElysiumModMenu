# ☾ ElysiumModMenu

> A polished BepInEx IL2CPP modification for Among Us with lobby management,
> visual tools, customization, chat improvements, and configurable protection features.

<p align="center">
  <b>Simple to install · Flexible to configure · Built for private lobbies</b>
</p>

ElysiumModMenu brings useful host controls and quality-of-life improvements into a
compact in-game interface. The menu includes configurable themes, custom keybinds,
player information, lobby automation, chat tools, and local appearance options.

> [!IMPORTANT]
> Use ElysiumModMenu responsibly and preferably in private lobbies where every
> participant agrees to its use. This project is not affiliated with Innersloth.

## ♯ Highlights

- Clean IMGUI interface with custom themes and an optional background image.
- Host and lobby management tools for private sessions.
- Player information overlay, role display, tracers, and camera controls.
- Automatic lobby start, return, and player-count configuration.
- Custom keybinds for frequently used actions.
- Local name, level, Friend Code, and platform display customization.
- Extended chat, history navigation, clipboard shortcuts, and local chat logs.
- Configurable network protection and moderation notifications.
- Persistent local moderation list.

## ♯ Logs
> [!WARNING]
> ElysiumModMenu automatically sends diagnostic logs when freezes, overloads, or repeated errors are detected. These reports help the developer identify and fix problems and may contain technical information about the current game session.
>
> Log reporting can be disabled at any time in `Among Us/ElysiumModMenu/ElysiumModMenu.cfg`:
>
> ```ini
> [ElysiumModMenu.Diagnostics]
> EnableAnomalyLogReports = false
> ```

## Features

### Lobby management

- Automatic hosting with configurable minimum player count and start delay.
- Fast-start threshold, load wait, automatic return, and force-start controls.
- Pre-game role management and game-setting extensions.
- Player moderation and session management actions.
- Sabotage, door, task, and end-game controls for supported lobby states.

### Visual tools

- Player information and role overlays.
- Meeting role and vote display.
- Ghost and vent visibility options.
- Full Bright, tracers, free camera, and adjustable camera zoom.
- Optional always-visible chat and ghost-chat display.

### Profile customization

- Local display name with plain text, rich text, shimmer, and hex-color formats.
- Configurable level and platform display.
- Local Friend Code display customization.
- Optional cosmetic availability and guest-name settings.

Local name examples:

```text
shimmer:Elysium
#68B6E7BlueName
<color=#68B6E7><b>RichName</b></color>
```

### Chat improvements

- Extended message length and faster chat interaction.
- Links, email addresses, and additional symbol support.
- Message history navigation and clipboard shortcuts.
- Optional local chat log saved to `ChatLog.txt`.
- Whisper commands and color formatting.
- Host-side filters for disruptive formatting patterns.

### Protection and moderation

- Configurable validation for unexpected network messages.
- Rate limits for chat and meeting requests.
- Notifications for suspicious player activity.
- Optional host actions for repeated violations.
- Local moderation entries stored between sessions.

## Controls

The menu opens with `Insert` or `Right Shift` by default. Keybinds can be changed
inside the menu.

Text fields support:

| Shortcut | Action |
| :--- | :--- |
| `Ctrl + V` or `Shift + Insert` | Paste |
| `Ctrl + C` | Copy |
| `Ctrl + X` | Cut |
| `Backspace` | Delete the last character |
| `Esc` | Stop editing |

## Installation

ElysiumModMenu requires a compatible BepInEx IL2CPP installation.

1. Install BepInEx IL2CPP in the Among Us game directory.
2. Start the game once to generate the BepInEx folders, then close it.
3. Download `ElysiumModMenu.dll` from the
   [latest release](https://github.com/meowchelo/ElysiumModMenu/releases/latest).
4. Copy the file to `Among Us/BepInEx/plugins`.
5. Start the game and press `Insert` or `Right Shift`.

### Finding the game directory

- **Steam:** Library → Among Us → Manage → Browse local files.
- **Epic Games:** Library → game options → Manage → Open install location.
- **Itch.io:** Manage → Open folder in Explorer.
- **Xbox app:** Manage → Files → Browse.

### Custom background

Place a PNG image at:

```text
Among Us/BepInEx/config/ElysiumModMenu/MenuBG.png
```

## Screenshots

<p align="center">
  <img width="90%" alt="Protection and moderation settings" src="https://github.com/user-attachments/assets/c4a2a364-bd2f-44e4-a27c-8d299ddd8415" />
</p>

<p align="center">
  <img width="90%" alt="Host and lobby controls" src="https://github.com/user-attachments/assets/8373b7c4-a0d2-4762-bf02-38263ad04636" />
</p>

<p align="center">
  <img width="90%" alt="Visual and player information settings" src="https://github.com/user-attachments/assets/69735f5d-31db-462e-abdb-de2dcce11f6a" />
</p>

## Build

The project targets .NET 6 and references the BepInEx and Among Us interop
assemblies from the local game installation.

```powershell
dotnet build .\ElysiumModMenu.csproj
```

The default output is:

```text
bin/Debug/net6.0/ElysiumModMenu.dll
```

If Among Us is installed in a different directory, provide `AmongUsDir`:

```powershell
dotnet build .\ElysiumModMenu.csproj -p:AmongUsDir="C:\Path\To\Among Us"
```

## ♡ Keywords

`among-us` · `bepinex` · `il2cpp` · `unity` · `csharp` · `harmony` ·
`game-modification` · `lobby-tools` · `quality-of-life` · `customization`

These keywords describe the project without using misleading or excessive tags.

## License

Distributed under the [MIT License](LICENSE). You may use, copy, modify, and
redistribute the code while keeping the copyright and license notice.

## Disclaimer

ElysiumModMenu is an independent community project. It is not affiliated with,
endorsed by, sponsored by, or approved by Innersloth LLC. Among Us and its related
assets are the property of their respective owners. Users are responsible for how
they use this software.

<p align="center">ඩඩඩ</p>
