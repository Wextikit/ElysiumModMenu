# NjordMenu🌊 - Among Us

**NjordMenu** - multi-functional [BepInEx](https://github.com/BepInEx/BepInEx) mod for Among Us, developed by **Meowchelo**. Built with the intention of providing ultimate control over your game, it features deep quality-of-life improvements, extensive host privileges, hilarious trolling utilities.

## 📋 Features

> [!NOTE]
> NjordMenu contains highly advanced network manipulation tools. Forced bans, and global network manipulation are exceptionally powerful. Please use them responsibly and refer to the Disclaimer below.

* **Extensive ESP & Visuals:** See ghosts, reveal player roles (even in meetings), view player info (Platform, Level, FriendCode), and enable Tracers or Full Bright.
* **Account Spoofer:** Spoof your Name, Level, Platform (Epic, Steam, Xbox, Starlight, etc.), and bypass Chat Filters.
* **Advanced Movement:** True NoClip, SpeedHack, Teleport to Cursor, Magnet Cursor, and Inverted Controls.
* **Role Buffs:** Infinite kill reach, kill anyone, endless Shapeshift/Battery/Vent time, and zero cooldowns.
* **Lobby & Host Controls:** Force pre-game roles (without black screens), Smart End Game, unlimited settings, and Spawn/Despawn maps or lobbies on the fly.
* **Legal Meeting:** Safely force a meeting in public lobbies without triggering anti-cheat kicks by simulating a legitimate body report or emergency button press.
* **Advanced Auto-Host System:** Fully automate your lobbies! Includes automatic return to the lobby after matches, customizable minimum player counts to begin countdowns, fast-start overrides for full lobbies, load-wait protections (waits for all players to fully load in), and last-minute force starts to keep your lobby alive indefinitely.
* **Anti-Cheat & Ban List:** Maintain a persistent, local blacklist (`NjordMenuBanList.txt`). Add specific FriendCodes to automatically ban and kick malicious players or rule-breakers the exact moment they join your lobby.
* **Trolling & Fun:**  Rainbow Colors, custom Chat Commands (`/color`, `/w`), and fake animations.
* **UI Customization:** Fully modular GUI with RGB mode, custom image backgrounds (`MenuBG.png`), and smooth animations.
<img width="1422" height="670" alt="изображение" src="https://github.com/user-attachments/assets/1f4ef367-e3de-4112-8b11-0f4e0f90cd54" />



## 🛡️ RPC Sniffer

NjordMenu comes with an advanced detection system:

* **RPC Sniffer (Radar):** NjordMenu silently analyzes all incoming network traffic. It compares incoming RPC packets against a built-in database of known mod menus (such as *GNC, HostGuard, Polar, BanMod, KillNetwork, etc.*). If a player uses a hidden cheat feature, you will instantly receive an on-screen notification identifying the exact mod they are using.

## ⚙️ Installation and Usage

> [!WARNING]
> Before using NjordMenu, please make sure to understand and fully consent to the warnings provided in the Disclaimer section.

As NjordMenu is a [BepInEx](https://github.com/BepInEx/BepInEx) mod, you will need to install **[BepInEx IL2CPP](https://github.com/BepInEx/BepInEx)**.

1. Download the required BepInEx IL2CPP build for your architecture (x64 for Epic/Microsoft Store, x86 for Steam usually).
2. Extract the BepInEx files into your Among Us installation directory (where `Among Us.exe` is located).
3. Run the game once to allow BepInEx to generate its folder structure, then close the game.
4. Download `NjordMenu.dll` from the Releases tab and place it into the `Among Us/BepInEx/plugins` folder.
5. Launch Among Us. The first launch may take slightly longer as BepInEx hooks into the game.
6. Press the **`Insert`** key (default) to open the NjordMenu UI.

### Customizing the Menu

* Place a file named `MenuBG.png` or `MenuBG.jpg` inside the `BepInEx/config` folder to use a custom background for your mod menu.
* To change the menu toggle key, click "Bind Key" in the Menu tab and press your desired key.


<img width="1140" height="604" alt="изображение" src="https://github.com/user-attachments/assets/147d1f84-5bb7-4bb4-9bee-b46361ec6e23" />


## ⚠️ Disclaimer & Caution

> [!CAUTION]
> NjordMenu should NEVER, under any circumstances, be used to impair the experiences of other legitimate players. If you use some of the trolling, crashing, or forceful features, please make sure you are doing so in a private lobby with consenting friends. You are free to join public lobbies with NjordMenu enabled as long as you use it with the intention of improving your own game (e.g., using the Anticheat, ESP, or QoL features). With great power comes great responsibility!

I recognize that utility mods like NjordMenu open the door for malicious users to cause destruction. Even with safeguards, there is always a chance for abuse. All I can do is ask you, the person using this mod, to please **do not use NjordMenu for malicious purposes** and follow the Innersloth Code of Conduct.

If you fail to follow this suggestion, do not expect to receive any kind of support. Your account may be sanctioned or banned by Innersloth, resulting in the loss of your friends list, unlocked cosmetics, and purchases.

*This mod is not affiliated with Among Us or Innersloth LLC, and the content contained therein is not endorsed or otherwise sponsored by Innersloth LLC. Portions of the materials contained herein are property of Innersloth LLC. © Innersloth LLC.*
