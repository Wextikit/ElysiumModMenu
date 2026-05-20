# 🌊 NjordMenu — Advanced Among Us Mod Menu

**NjordMenu** is a multi-functional [BepInEx](https://github.com/BepInEx/BepInEx) mod menu for Among Us, developed by **Meowchelo**. Built with the intention of providing ultimate control over your game, it features deep quality-of-life improvements, extensive host privileges, advanced anti-cheat protections, and hilarious trolling utilities.

---

## 📦 Releases

| Version | Description | Download |
| :--- | :--- | :--- |
| **v1.2.5** ✨ | Latest update (18.05.2026) | [📥 Download .dll](https://github.com/meowchelo/NjordMenu/releases/latest) |
| **v1.2.4** | Previous stable build (18.05.2026)| [📥 Download .dll](https://github.com/meowchelo/NjordMenu/releases/tag/v1.2.4) |
| **v1.2.3** | 13.05.2026| [📥 Download .dll](https://github.com/meowchelo/NjordMenu/releases/tag/v1.2.3) |

---

## 📋 Comprehensive Feature List

> [!NOTE]  
> NjordMenu contains highly advanced network manipulation tools. Forced bans and global network manipulation are exceptionally powerful. Please use them responsibly.

### 🛡️ Anti-Cheat & Client Detection
NjordMenu acts as a powerful shield against malicious players, crashers, and other modders:
* **Advanced RPC Radar (Sniffer):** The menu silently analyzes all incoming network traffic. Vanilla Among Us only uses RPC IDs `0-65`. If a player sends non-vanilla packets, NjordMenu cross-references the ID with a massive built-in database of known third-party cheats (including Sicko, HostGuard, Polar, GNC, KillNetwork, BanMod, and more*). You will receive an instant on-screen notification identifying exactly who is cheating and what menu they are using.
* **Menu Identity Spoofer:** Want to hide your NjordMenu from other modders? You can spoof your outgoing RPCs to mimic *other* cheat clients, tricking their radars into thinking you are using something else.
* **Pet Spam Shield:** Detects malicious packet spam (e.g., 160+ packets per second). Locally drops the packets to prevent lag, and automatically bans the attacker if you are the host.
* **Anti Vote-Kick:** Completely disable the lobby vote-kick system, preventing trolls from kicking you out of your own lobby.
* **Fortegreen Auto-Kick:** Automatically detects and kicks bugged/glitched "Fortegreen" (Color ID 18) players that cause lobby crashes.
* **Persistent Ban List:** Maintain a local blacklist (`NjordMenuBanList.txt`). Add griefers by their FriendCode or PUID, and the mod will kick them the exact millisecond they try to join.
<img width="1884" height="1020" alt="изображение" src="https://github.com/user-attachments/assets/c4a2a364-bd2f-44e4-a27c-8d299ddd8415" />


### 💬 Ultimate Chat System
Experience the best chat enhancements available:
* **Bypass Restrictions:** Increase the character limit to 120, enable Fast Chat (removes the anti-spam cooldown), and allow links, emails, and special symbols.
* **QoL Integrations:** Native clipboard support (`Ctrl+C` / `Ctrl+V`) and Chat History navigation using the `Up` and `Down` arrow keys.
* **Always Show Chat & Ghost Chat:** Keep the chat window permanently visible and read what dead players (ghosts) are saying while you are still alive.
* **Local Chat Logging:** Automatically saves all lobby and in-game chat to a local `ChatLog.txt` file.
* **Commands & Whispers:** Use `/w [name] [message]` or `/pm` to send private messages that only the target player can see. Change your color on the fly using `/color [name/ID]`.
* **Host Filters:** Block exploit chats like Rainbow text or Fortegreen names globally.

### 👑 Host & Lobby Controls
* **Force Eject:** Instantly throw any player into space without a meeting or sirens! *(Host only)*
* **Pre-Game Role Manager:** Force specific roles (Impostor, Shapeshifter, Phantom, Engineer, Tracker, etc.) for yourself or others before the game even starts.
* **Advanced Auto-Host:** Fully automate your lobbies! Auto-return after matches, custom minimum player counts, fast-start overrides, and load-wait protections.
* **No Setting Limits:** Completely uncap game options (e.g., set unlimited Impostors, extreme speeds).
* **Extended Lobby:** Expands the public lobby finder UI to show up to 15 slots and displays extra metadata (Host Platform, Lobby Age, True Host Name).
<img width="1919" height="1079" alt="Снимок экрана 2026-05-18 225327" src="https://github.com/user-attachments/assets/8373b7c4-a0d2-4762-bf02-38263ad04636" />

### 🔧 Sabotage & Door Control
* **Global Control:** Trigger all critical sabotages (Reactor, O2, Comms, Lights) at once, or instantly fix all of them with a single click.
* **Door Management:** Close or open all doors on the map globally, or manipulate specific room doors (e.g., Cafeteria, Storage) directly from the menu.

### 🎭 Role Enhancements & Modifiers
* **Impostor Buffs:** Infinite kill reach, kill anyone (even other Impostors), and no kill cooldowns.
* **Shapeshifter:** Endless shapeshift duration and the ability to skip the morphing animation.
* **Crewmate Buffs:** Engineer (endless vent time, no cooldown), Scientist (endless battery, no vitals cooldown), Tracker (endless tracking), and Detective (unlimited interrogate range).

### 👁️ Visuals & ESP
* **Extensive ESP:** See ghosts, reveal player roles, and view player info (Platform, Level, FC) attached to their nameplate.
* **Reveal Votes:** Watch exactly who votes for whom in real-time during meetings.
* **Vision Hacks:** See players hiding inside vents and toggle Full Bright to remove all map shadows.
* **Camera Controls:** Detach your camera using Freecam (WASD) or zoom out using the mouse scroll wheel.
<img width="1806" height="918" alt="изображение" src="https://github.com/user-attachments/assets/69735f5d-31db-462e-abdb-de2dcce11f6a" />

---

## ⚙️ Installation Guide

Because NjordMenu is a BepInEx plugin, **you must install the BepInEx IL2CPP library first**. 

### Step 1: Install BepInEx IL2CPP
1. Download **BepInEx IL2CPP** from its [official repository](https://github.com/BepInEx/BepInEx). 
   * *Note: Usually, you need the **x86** version for Steam, and the **x64** version for Epic Games or Microsoft Store.*
2. Extract the contents of the BepInEx `.zip` file directly into your Among Us game folder (where `Among Us.exe` is located).
3. **Run the game once.** This is crucial! It allows BepInEx to unpack and generate its required `plugins` folder. Once you reach the main menu, close the game.

### Step 2: Install NjordMenu
1. Download `NjordMenu.dll` from the [Releases](#-releases) table above.
2. Navigate to your Among Us game folder, open the `BepInEx` folder, and then open the `plugins` folder.
3. Paste `NjordMenu.dll` into the `plugins` folder.
4. Launch Among Us! 

*(The first launch might take slightly longer as BepInEx hooks into the game).*

### 📁 How to find your Among Us game folder:
* 💨 **Steam:** Right-click Among Us in your Library → `Manage` → `Browse local files`.
* 🎮 **Epic Launcher:** Right-click Among Us in your Library → `Manage` → Click the `folder icon`.
* 👾 **Itch.io:** Open the Itch.io app → Right-click Among Us → `Manage` → `Open folder in Explorer`.
* 🪟 **Microsoft Store:** Open the folder where Windows apps are installed (typically `C:\Program Files\WindowsApps\`) → Search for `Among Us.exe` → Right-click the result → Select `Open file location`.
* 🟢 **Xbox App:** Right-click Among Us in your Library → `Manage` → `FILES` tab → `BROWSE...` → `Among Us` → `Content`.

> 💡 **Tip:** By default, you can toggle the cheat GUI on and off by pressing the **`INSERT`** (or `Right Shift`) key on your keyboard. Custom backgrounds can be applied by placing a `MenuBG.png` inside the `BepInEx/config` folder.

---

## ⚠️ Disclaimer & Caution

> [!CAUTION]  
> NjordMenu should **NEVER**, under any circumstances, be used to impair the experiences of other legitimate players. If you use trolling, crashing, or forceful features, please make sure you are doing so in a private lobby with consenting friends. You are free to join public lobbies with NjordMenu enabled as long as you use it with the intention of improving your own game (e.g., Anticheat, ESP, QoL features). **With great power comes great responsibility!**

I recognize that utility mods like NjordMenu open the door for malicious users to cause destruction. Even with safeguards, there is always a chance for abuse. All I can do is ask you to please **do not use NjordMenu for malicious purposes** and follow the Innersloth Code of Conduct. If you fail to follow this suggestion, do not expect to receive any kind of support. Your account may be sanctioned or banned by Innersloth.

*This mod is not affiliated with Among Us or Innersloth LLC, and the content contained therein is not endorsed or otherwise sponsored by Innersloth LLC. Portions of the materials contained herein are property of Innersloth LLC. © Innersloth LLC.*
