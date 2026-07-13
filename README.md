<div align="center">

# Elysium Mod Menu

**A configurable BepInEx IL2CPP menu for Among Us**

<br>

<a href="https://github.com/Wextikit/ElysiumModMenu/releases/latest">
  <img src="https://img.shields.io/badge/Download-Latest%20Release-2ea44f?style=for-the-badge&logo=github&logoColor=white" alt="Download">
</a>
<a href="docs/CHANGELOG.md">
  <img src="https://img.shields.io/badge/Changelog-View-0969da?style=for-the-badge" alt="Changelog">
</a>
<a href="https://github.com/Wextikit/ElysiumModMenu/issues">
  <img src="https://img.shields.io/badge/Report-Issue-da3633?style=for-the-badge&logo=github&logoColor=white" alt="Report an issue">
</a>

<br>
<br>

<a href="https://discord.gg/CdrpKJzFp">
  <img src="https://img.shields.io/badge/Discord-ElysiumModMenu-5865F2?style=for-the-badge&logo=discord&logoColor=white" alt="Join Discord">
</a>

</div>

> [!CAUTION]
> This menu includes host, network, spoofing, and moderation tools. Use it only in private or consenting lobbies. Misuse can disrupt games and may result in account or server moderation. Elysium Mod Menu is not affiliated with Innersloth.

## Features

| Area                | Included tools                                                                                                                                                                     |
| :------------------ | :--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Visuals & ESP**   | Roles, player information, ghosts, vents, protection effects, filtered tracers, Full Bright, freecam, camera zoom, meeting roles, revealed votes, and Phantom visibility           |
| **Host & Lobby**    | Auto Host, role manager, lobby controls, task settings, player actions, smart start/end controls, persistent bans, forced impostors, No Task Mode, and unrestricted settings       |
| **Anti-Cheat**      | RPC and flood protections, mod detection, bot checks, malformed identity checks, configurable auto-kicks/bans, pet-spam protection, vote-kick protection, and custom platform bans |
| **Account & Local** | Level, platform, name, and Friend Code spoofing; cosmetics and Cosmicube controls; saved outfits, local Friend Code display, guest-name support, and disconnect-penalty removal    |
| **Chat & QoL**      | Extended chat, history, clipboard support, whispers, filters, notifications, keybinds, custom themes, fast chat, color commands, and local chat logging                            |
| **Maps & Sabotage** | Sabotage controls, repairs, vent tools, global doors, per-room door controls, Mushroom Mixup, unfixable lights, and instant repair of all systems                                  |

> [!NOTE]
> Host-only actions require you to be the current lobby host. Local visual and identity options generally affect only your own client unless their description explicitly mentions RPC or network synchronization.

## Installation and usage

> [!WARNING]
> Before installing Elysium Mod Menu, make sure you understand the disclaimer and use the menu only in private, testing, or consenting lobbies.

###  1. Install BepInEx IL2CPP
 
 ♯ Elysium Mod Menu is a **BepInEx IL2CPP** mod.
Among Us is a Unity IL2CPP game, so the normal Mono version of BepInEx will not work.

 ♯ Download BepInEx IL2CPP from one of these sources:

* [BepInEx releases](https://github.com/BepInEx/BepInEx/releases)
* [BepInEx bleeding-edge builds](https://builds.bepinex.dev/projects/bepinex_be)

 ♯ Choose the archive that matches your Among Us build:

| Game version source     | Recommended BepInEx architecture |
| :---------------------- | :------------------------------- |
| Steam                   | x86                              |
| Itch.io                 | x86                              |
| Epic Games              | x64                              |
| Microsoft Store         | x64                              |
| Xbox App / PC Game Pass | x64                              |

If you are not sure which architecture your game uses, open Among Us, then open **Task Manager**.
If the process is shown as `Among Us.exe (32-bit)`, use **x86**. Otherwise, use **x64**.

### 2. Place BepInEx into the Among Us folder

ඩ Open your Among Us installation folder. It should contain files like:

```
Among Us.exe
GameAssembly.dll
```

♯ Extract the BepInEx archive directly into this folder.

♯ After extracting, your Among Us folder should look similar to this:

```
Among Us/
├─ Among Us.exe
├─ GameAssembly.dll
├─ winhttp.dll
├─ dotnet/
└─ BepInEx/
```

If you do not see `winhttp.dll`, `dotnet`, and `BepInEx` next to `Among Us.exe`, the archive was probably extracted into an extra folder. Open that folder and move its contents directly into the Among Us directory.

Launch Among Us once after installing BepInEx.
The first launch may take longer than usual and a console window may appear. This is normal. Once the game reaches the main menu, close it.

### 3. Install Elysium Mod Menu

Download `ElysiumModMenu.dll` from the [latest Elysium release](https://github.com/Wextikit/ElysiumModMenu/releases/latest).

Place the file here:

```
Among Us/BepInEx/plugins/ElysiumModMenu.dll
```

If the `plugins` folder does not exist, create it manually inside the `BepInEx` folder.

### 4. Launch and open the menu

 ♡ Start Among Us.

Press **Insert** to open Elysium Mod Menu.
On some keyboards, you may need to press **Fn + Insert** or enable/disable **Num Lock**.

The menu key can be changed later in the menu settings.

### Updating ☾ Elysium Mod Menu

To update the menu:

1. ♯ Close Among Us.

2. ♯ Download the new `ElysiumModMenu.dll` from the [latest release](https://github.com/Wextikit/ElysiumModMenu/releases/latest).

3. ♯ Replace the old file in:

   ```
   Among Us/BepInEx/plugins/
   ```

4. ♡ Start the game again.

## Supported platforms

| Platform                    | Status                                                |
| :-------------------------- | :---------------------------------------------------- |
| **Steam**                   | Supported                                             |
| **Itch.io**                 | Supported                                             |
| **Epic Games**              | Supported                                             |
| **Microsoft Store**         | Supported                                             |
| **Xbox App / PC Game Pass** | Supported                                             |
| Cracked / unofficial builds | May work inconsistently; not recommended or supported |
| Android                     | Not supported                                         |
| iOS                         | Not supported                                         |
| Switch / Xbox / PlayStation | Not supported                                         |

The Platform Spoof option can display supported platform labels to other clients. These are spoofing targets, not platforms on which the mod itself can run.

## ♯ Diagnostic logs

> [!WARNING]
> Diagnostic reporting is enabled by default. When the game detects a freeze, error burst, or log overload, the configured build may send a diagnostic summary and attach affected Unity/BepInEx log files to the maintainer's configured endpoint.

### Enable or disable diagnostic reporting

Open:

```
Among Us/ElysiumModMenu/ElysiumModMenu.cfg
```

Under `[ElysiumModMenu.Diagnostics]`, set:

```
EnableAnomalyLogReports = false
```

Use `true` to enable automatic anomaly reports. Restart the game after changing the file.

### Enable detailed local logs

Open **Menu → Notifications & Logging** and enable **Detailed Unity/RPC Logs**.

This restores verbose RPC, Message, Info, and Debug output. Disable it during normal play for better performance; warnings and errors remain available.

The main local BepInEx log is usually stored at:

```
Among Us/BepInEx/LogOutput.log
```

##  ♡ Community and support

* Join the [ElysiumModMenu Discord](https://discord.gg/CdrpKJzFp) for announcements, help, previews, and community discussion.
* Use [GitHub Issues](https://github.com/Wextikit/ElysiumModMenu/issues) for reproducible bugs and feature requests.
* When reporting a problem, include the menu version, game platform, what happened, reproduction steps, and the relevant log excerpt.
* Remove room codes, Friend Codes, PUIDs, chat messages, and personal paths before posting logs publicly.

> [!TIP]
> Check the [changelog](docs/CHANGELOG.md) before reporting an issue — the latest release may already contain the fix.

## ♯ Useful files

```
Among Us/ElysiumModMenu/ElysiumModMenu.cfg
Among Us/ElysiumModMenu/ElysiumModMenuBanList.txt
Among Us/ElysiumModMenu/ElysiumBotBanList.txt
Among Us/ElysiumModMenu/ElysiumPlatformBanList.txt
Among Us/ElysiumModMenu/ElysiumFriendEspIgnore.txt
```

<details>
<summary><strong>Build from source</strong></summary>

The project targets .NET 6 and requires local Among Us/BepInEx interop assemblies.

```
dotnet build .\ElysiumModMenu.slnx -c Release
```

</details>

<details>
<summary><strong>Screenshots</strong></summary>

<!-- Add screenshots here -->

<img width="1919" height="1079" alt="Снимок экрана 2026-06-22 131852" src="https://github.com/user-attachments/assets/e295ce9d-557e-4420-8f57-37f8b79e47b1" />
<img width="1919" height="1079" alt="Снимок экрана 2026-06-22 131643" src="https://github.com/user-attachments/assets/e1cc97d3-edfb-46d4-9049-0fcd95be5226" />
<img width="1919" height="1079" alt="Снимок экрана 2026-06-22 131614" src="https://github.com/user-attachments/assets/e9062d61-424a-471f-a739-ec4508858cc0" />
<img width="1919" height="1079" alt="Снимок экрана 2026-06-22 131538" src="https://github.com/user-attachments/assets/3bf4ade8-96d5-44d8-a3f2-5607f101dc95" />

</details>

<details>
<summary><strong>Menu function guide</strong></summary>

| Tab            | Purpose                                                                                                     |
| :------------- | :---------------------------------------------------------------------------------------------------------- |
| **General**    | Information, language, saved settings, and configurable keybinds                                            |
| **Self**       | Movement, identity and level spoofing, local abilities, outfits, and personal gameplay options              |
| **Visuals**    | ESP information, roles, ghosts, protection effects, visibility filters, tracers, lighting, and camera tools |
| **Players**    | Select players, inspect history, copy identifiers, report, teleport, morph, kill, revive, kick, or ban      |
| **Sabotages**  | Trigger or repair systems, control vents, and open, close, or lock doors globally or by room                |
| **Host Only**  | Lobby controls, role manager, Anti-Cheat, Auto Host, maps, task rules, starts, and end-game actions         |
| **Votekick**   | View and control vote-kick behavior and related protection options                                          |
| **Menu**       | Themes, background, performance, privacy, unlocks, notifications, logging, and reset options                |
| **Animations** | Play supported task, scanner, camera, shield, and other local animation effects                             |

Some actions are local-only, while actions marked as host or RPC tools can affect the lobby. Availability depends on the current game state and host permissions.

</details>

<details>
<summary><strong>Cosmetics and Cosmicube unlocks</strong></summary>

* **Unlock All except Cosmicubes** — locally makes regular cosmetics available for selection. It does not grant server-side ownership.
* **Unlock Cosmicubes** — locally exposes Cosmicubes without changing their completion progress or server data.
* **Activate 100% Cosmicubes** — allows a fully completed Cosmicube to be selected locally. No activation data is sent to the server.

These options are located in the **Menu** tab. Unlock behavior is client-side, may reset after updates, and does not create purchases, currency, permanent ownership, or server-side progression.

> ⚠️ **Warning**
> Unlock features modify only the local game interface and purchase checks. Do not treat locally visible items or progress as permanently owned account content.

</details>

## Disclaimer ❕❕❕

> [!IMPORTANT]
> Elysium Mod Menu is an independent, unofficial modification. It is not affiliated with, endorsed by, sponsored by, or approved by Innersloth LLC. Among Us, its name, trademarks, and game assets belong to their respective owners.

The software is provided **as-is**, without warranties of functionality, compatibility, availability, security, or fitness for a particular purpose. Game updates may break features or cause crashes.

You are solely responsible for installing and using the mod, complying with applicable rules and laws, reviewing diagnostic data, and accepting any account, lobby, moderation, or data-loss consequences.

The maintainers are not responsible for bans, restrictions, corrupted files, lost progress, game instability, third-party modifications, misuse, or damage arising from use of this software.

Support is not provided for harassment, disruption, moderation evasion, unauthorized access, or other malicious activity.
