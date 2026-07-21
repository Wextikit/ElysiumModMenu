<div align="center">

# Elysium Mod Menu (emm) - Among Us 

**Dark IMGUI overlay for Among Us with local visual, host, lobby and moderation tools.**

<p>
  <img src="https://img.shields.io/badge/Among%20Us-Mod%20Menu-4b5563?style=flat-square" alt="Among Us Mod Menu">
  <img src="https://img.shields.io/badge/Runtime-IL2CPP-111827?style=flat-square" alt="IL2CPP">
  <img src="https://img.shields.io/badge/Loader-BepInEx-374151?style=flat-square" alt="BepInEx">
  <img src="https://img.shields.io/badge/Language-C%23-512BD4?style=flat-square&logo=csharp&logoColor=white" alt="C#">
  <img src="https://img.shields.io/badge/Version-1.4.4-a855f7?style=flat-square" alt="Version 1.4.4">
  <img src="https://img.shields.io/github/downloads/Wextikit/ElysiumModMenu/total?style=flat-square&label=Downloads&color=2563eb" alt="Downloads">
</p>

<p>
  <a href="https://github.com/Wextikit/ElysiumModMenu/releases/latest">
    <img src="https://img.shields.io/badge/Download-Latest%20Release-2ea44f?style=for-the-badge&logo=github&logoColor=white" alt="Download latest release">
  </a>
  <a href="docs/CHANGELOG.md">
    <img src="https://img.shields.io/badge/Changelog-View-0969da?style=for-the-badge" alt="View changelog">
  </a>
  <a href="https://github.com/Wextikit/ElysiumModMenu/issues">
    <img src="https://img.shields.io/badge/Report-Issue-da3633?style=for-the-badge&logo=github&logoColor=white" alt="Report an issue">
  </a>
</p>

</div>

<table align="center">
  <tr>
    <td align="center" width="52%">
      <strong>Need help or want to suggest a feature?</strong><br>
      The Discord is the quickest place to reach the project and see release notes as they land.
    </td>
    <td align="center" width="48%">
      <a href="https://discord.gg/ZP8MgMcB8C">
        <img src="https://img.shields.io/badge/Open%20Elysium%20Discord-5865F2?style=for-the-badge&logo=discord&logoColor=white" alt="Open Elysium Discord">
      </a><br>
      <sub>Support · previews · bug reports · ideas</sub>
    </td>
  </tr>
</table>

<p align="center">
  <a href="#main-features">Features</a> ·
  <a href="#installation">Install</a> ·
  <a href="#menu-guide">Menu guide</a> ·
  <a href="#community">Community</a>
</p>

> [!CAUTION]
> Elysium Mod Menu includes host, network, spoofing and moderation tools. Use them only in private, testing or consenting lobbies. Misuse can disrupt games and may result in account or server moderation. The project is not affiliated with Innersloth.

## About the menu

Elysium Mod Menu is a configurable BepInEx IL2CPP plugin for Among Us. It adds an in-game IMGUI menu without replacing the original game UI. The menu combines local visual options, player information, outfit controls, host-only lobby administration, anti-cheat checks, chat tools and configurable keybinds.

The project separates local and network behavior:

| Action type | Who is affected |
| :---------- | :-------------- |
| **Local visual** | Only your client sees the result. Examples include ESP, Full Bright, freecam and camera zoom. |
| **Local profile** | Changes your local saved data or menu configuration. Examples include menu profiles, saved outfits and selected UI settings. |
| **RPC action** | The local player sends a normal game RPC, so players in the current room can see the result. |
| **Host-only** | Requires you to be the current host and can affect selected players or the entire lobby. |

Availability depends on the current game state. Some actions only make sense in a lobby, during a match, while a meeting is open or when the local player is alive.

## Main features

| Area | Included tools |
| :--- | :------------- |
| **Visuals & ESP** | Player roles and information, ghosts, vents, protection effects, filtered tracers, ESP boxes, Full Bright, freecam, camera zoom, meeting roles, revealed votes and Phantom visibility. |
| **Self & movement** | Walk-speed controls, no-clip, cursor teleport, player following, local identity options, level spoofing and animation controls. |
| **Outfits** | Four persistent favorite slots, copying an outfit from another player, Random Outfit from the full cosmetic catalog, profile saving and free-color tools. |
| **Host & lobby** | Auto Host, role manager, lobby controls, task settings, smart start/end actions, forced impostors, No Task Mode and unrestricted game settings. |
| **Anti-Cheat** | RPC validation, flood protections, malformed identity checks, bot detection, configurable kick/ban actions, vote-kick protection and custom platform bans. |
| **Players** | Select and inspect players, view player history, copy identifiers, teleport, morph, kill, revive, kick, ban and report. |
| **Chat & QoL** | Extended and fast chat, history, clipboard tools, whispers, filters, notifications, custom colors, keybinds and local chat logging. |
| **Maps & sabotage** | Sabotage controls, instant repairs, vent tools, global doors, per-room door controls, Mushroom Mixup and unfixable lights. |
| **Customization** | Menu themes, background image, RGB accents, watermark settings, notification styles, FPS limit and configurable menu scale. |

### Random Outfit

The `Outfits` page contains a one-click Random Outfit card:

- selects a random color from `Palette.PlayerColors`;
- selects a hat, skin, visor and pet from the complete loaded `HatManager` catalog;
- keeps the current nameplate;
- applies the result to the local player with the existing cosmetic RPC methods;
- with `Save to profile` enabled, writes the selected IDs to the Among Us player profile;
- with profile saving disabled, restores the saved profile when the next lobby starts.

The random color is not filtered by room occupancy. A host or server can reject or react to an already occupied color.

## Menu guide

| Tab | Purpose |
| :-- | :------ |
| **General** | Project information, language, saved menu profiles and configurable keybinds. |
| **Self** | Movement, identity and level spoofing, local abilities, outfits and personal gameplay options. |
| **Visuals** | ESP, roles, ghosts, protection effects, visibility filters, tracers, lighting and camera tools. |
| **Players** | Player selection, history, identifiers and local, RPC or host actions for a selected player. |
| **Sabotages** | Trigger or repair systems, control vents and operate doors globally or by room. |
| **Host Only** | Lobby controls, role management, Anti-Cheat, Auto Host, maps, task rules, starts and end-game actions. |
| **Votekick** | Vote-kick information, automatic rounds and related protection options. |
| **Menu** | Themes, background, performance, privacy, cosmetic unlocks, notifications, logging and reset options. |
| **Animations** | Supported task, scanner, camera, shield and other local animation effects. |

## Installation

> [!WARNING]
> Close Among Us before changing BepInEx or plugin files.

### 1. Install BepInEx IL2CPP

Elysium Mod Menu requires the IL2CPP build of BepInEx. The normal Mono build will not load this plugin.

- [BepInEx releases](https://github.com/BepInEx/BepInEx/releases)
- [BepInEx bleeding-edge builds](https://builds.bepinex.dev/projects/bepinex_be)

Choose the archive matching the architecture of your Among Us executable. If Task Manager marks `Among Us.exe` as a 32-bit process, use x86; otherwise use x64.

### 2. Extract BepInEx into the game directory

Open the directory containing:

```text
Among Us.exe
GameAssembly.dll
```

Extract BepInEx directly into this directory. The resulting structure should be similar to:

```text
Among Us/
├─ Among Us.exe
├─ GameAssembly.dll
├─ winhttp.dll
├─ dotnet/
└─ BepInEx/
```

Launch Among Us once after installing BepInEx. The first launch can take longer while BepInEx creates its folders and configuration files. Close the game after reaching the main menu.

### 3. Install Elysium Mod Menu

Download `ElysiumModMenu.dll` from the [latest release](https://github.com/Wextikit/ElysiumModMenu/releases/latest) and place it here:

```text
Among Us/BepInEx/plugins/ElysiumModMenu.dll
```

Create the `plugins` directory if BepInEx has not created it automatically.

### Linux / Steam Proton

On Linux or Steam Deck, install BepInEx and `ElysiumModMenu.dll` into the Among Us game directory the same way as above.

Then open **Steam → Among Us → Properties → General → Launch Options** and add:

```text
WINEDLLOVERRIDES="winhttp.dll=n,b" %command%
```

This makes Proton load BepInEx through `winhttp.dll` when the game starts.

### 4. Open the menu

Start Among Us and press **Insert**. On compact keyboards, **Fn + Insert** may be required. The key can be changed later in the menu settings.

## Updating

1. Close Among Us.
2. Download the new `ElysiumModMenu.dll` from [Releases](https://github.com/Wextikit/ElysiumModMenu/releases/latest).
3. Replace the previous DLL inside `Among Us/BepInEx/plugins/`.
4. Start the game again.

Existing menu configuration is stored separately from the DLL and is not removed when replacing the plugin file.

## Configuration and useful files

Elysium keeps its own configuration and moderation lists under the game directory:

```text
Among Us/ElysiumModMenu/ElysiumModMenu.cfg
Among Us/ElysiumModMenu/ElysiumModMenuBanList.txt
Among Us/ElysiumModMenu/ElysiumBotBanList.txt
Among Us/ElysiumModMenu/ElysiumPlatformBanList.txt
Among Us/ElysiumModMenu/ElysiumFriendEspIgnore.txt
```

Menu profiles are separate from the Among Us player profile. A menu profile stores Elysium settings, while options such as `Save to profile` in Random Outfit write to the game's player customization data.

## Cosmetics and Cosmicubes

- **Unlock All except Cosmicubes** makes regular cosmetics available to the local selection UI.
- **Unlock Cosmicubes** exposes Cosmicubes locally without changing completion progress or server ownership.
- **Activate 100% Cosmicubes** allows a completed Cosmicube to be selected locally.

These options affect local purchase checks and UI availability. They do not create purchases, currency, permanent server-side ownership or account progression.

## Troubleshooting

<details>
<summary><strong>The menu does not appear</strong></summary>

- Confirm that the IL2CPP build of BepInEx is installed.
- Confirm that `ElysiumModMenu.dll` is directly inside `BepInEx/plugins/`.
- Launch the game once and inspect the BepInEx console or log for load errors.
- Try **Insert** and **Fn + Insert**.
- Check whether another overlay captures the same key.

</details>

<details>
<summary><strong>The game stops loading after an update</strong></summary>

Game updates can invalidate BepInEx interop assemblies or plugin API signatures. Keep a copy of the previous working setup, update BepInEx when required and check the latest release notes before replacing files.

</details>

<details>
<summary><strong>A host action is unavailable</strong></summary>

Confirm that you are the current lobby host and that the required game object exists. Lobby, match, meeting and post-game screens expose different actions.

</details>

<details>
<summary><strong>Preparing a useful bug report</strong></summary>

Include the Elysium version, game platform, what happened, exact reproduction steps and the relevant log excerpt. Remove lobby codes, Friend Codes, PUIDs, chat messages and personal paths before posting logs publicly.

</details>

## Screenshots

<details>
<summary><strong>Open screenshot gallery</strong></summary>

### New captures

<p><strong>Menu customization and animated character background</strong></p>
<img width="960" alt="Elysium menu customization screen" src="docs/screenshots/menu-customization.png" />

<p><strong>Visuals and ESP in a live match</strong></p>
<img width="960" alt="Elysium visuals and ESP screen in Among Us" src="docs/screenshots/visuals-esp.png" />

### Earlier captures

<img width="1919" height="1079" alt="Elysium Mod Menu screenshot 1" src="https://github.com/user-attachments/assets/e295ce9d-557e-4420-8f57-37f8b79e47b1" />
<img width="1919" height="1079" alt="Elysium Mod Menu screenshot 2" src="https://github.com/user-attachments/assets/e1cc97d3-edfb-46d4-9049-0fcd95be5226" />
<img width="1919" height="1079" alt="Elysium Mod Menu screenshot 3" src="https://github.com/user-attachments/assets/e9062d61-424a-471f-a739-ec4508858cc0" />
<img width="1919" height="1079" alt="Elysium Mod Menu screenshot 4" src="https://github.com/user-attachments/assets/3bf4ade8-96d5-44d-a3f2-5607f101dc95" />

</details>

## Build from source

The project targets .NET 6 and resolves local Among Us/BepInEx interop assemblies through `AmongUsDir` in the project file.

```powershell
dotnet build .\ElysiumModMenu.slnx -c Release
```

The build does not install or launch the plugin automatically.

## Community

The project is built around a small feedback loop: try a feature in a consenting lobby, report what happened, and include enough context to reproduce it.

| Place | Best for |
| :---- | :------- |
| [Elysium Discord](https://discord.gg/ZP8MgMcB8C) | Live support, release updates, previews and feature ideas. |
| [GitHub Issues](https://github.com/Wextikit/ElysiumModMenu/issues) | Reproducible bugs and implementation requests. |
| [Releases](https://github.com/Wextikit/ElysiumModMenu/releases) | DLL downloads and version history. |

When posting logs, remove lobby codes, Friend Codes, PUIDs, chat messages and personal filesystem paths.

## Disclaimer

> [!IMPORTANT]
> Elysium Mod Menu is an independent, unofficial modification. It is not affiliated with, endorsed by, sponsored by or approved by Innersloth LLC. Among Us, its name, trademarks and game assets belong to their respective owners.

The software is provided **as-is**, without warranties of functionality, compatibility, availability, security or fitness for a particular purpose. Game updates may break features or cause crashes.

You are responsible for installing and using the mod, complying with applicable rules and laws, reviewing diagnostic data and accepting any account, lobby, moderation or data-loss consequences.

The maintainer is not responsible for bans, restrictions, corrupted files, lost progress, game instability, third-party modifications, misuse or damage arising from use of this software.

Support is not provided for harassment, disruption, moderation evasion, unauthorized access or other malicious activity.

## License and author

Elysium Mod Menu is distributed under the [GNU General Public License v3.0](LICENSE).

Developed and maintained by **Meowchelo**.
