# Changelog

## v1.4.5 — 2026-07-29

### Changed

- Extended host bot bans to detect `bot` or `бот` anywhere in raw platform names without case sensitivity.
- Removed the `Hidden` Friend Code mode and its temporary-account and serialization patches.
- Renamed `Public Friend Code` to `Spoof Friend Code` in the identity menu and registration notifications.

## v1.4.4 — 2026-07-25

### Added

- Added a saved `Realistic Mode` for the radar with detailed layouts for The Skeld, Mira HQ, Polus, Airship, and Fungle while keeping the original radar available.

## v1.3.9 — 2026-06-22

### Added

- Added separate options for unlocking regular cosmetics, unlocking Cosmicubes, and locally activating 100% completed Cosmicubes.
- Added selected-player actions: copy PUID, copy Friend Code, and report a player with a selectable reason.
- Added `See Phantoms` and support for revealing active Guardian Angel protection effects.
- Added tracer filters for all players, crewmates, impostors, dead players/ghosts, and dead bodies. Body tracers are yellow.
- Added `Block Innersloth Telemetry`, `No Disconnect Penalty`, and automatic room-code copying on disconnect.
- Added a detailed Unity log rate control: `0` disables ordinary logs, `1–19` limits lines per second, and `∞` removes the limit.

### Changed

- Reorganized Visuals into Visibility, ESP, Tracers, and Other sections.
- Renamed `Show Tracers` to `All Tracers`.
- Reworked remote kill-cooldown tracking using in-game timer state.
- Kill-cooldown labels now use the menu accent color, colored timer thresholds, and burgundy `READY` status.
- `No Disconnect Penalty` is enabled by default and appears below the telemetry option with a description.
- Improved `Kill Anyone` selection for other impostor-team roles; hosts can confirm these kills directly.
- Made Door Lockdown controls responsive at narrow menu widths.
- Updated log monitoring to ignore the first 40 startup lines and detect warning bursts from 7 events with throttling.
- Staggered screen notifications by 0.3 seconds to prevent simultaneous notification bursts.
- Reduced player-history polling frequency and cached unchanged ESP name and information text.
- Moved expensive tracer visibility and dead-body scans to a 0.3-second interval while keeping tracer positions smooth each frame.
- Moved bot detection to a 0.5-second interval without delaying regular autobans.
- Changed `No Task Mode` to apply zero task settings once when enabled and once after changing lobbies instead of every frame.
- Pre-indexed autoban Friend Codes in a case-insensitive `HashSet`, preserving immediate bans without per-player `Split`, `Trim`, and lowercase conversions.
- Reused notification `GUIStyle` instances instead of allocating new styles during every `OnGUI` pass.
- Changed `Level Spoof` to apply only when enabled, when its value changes, and when entering a different lobby instead of every frame.

### Fixed

- Fixed random fatal CLR crashes caused by repeatedly reading and mutating IL2CPP FriendCode strings from per-frame GUI and local spoof updates.
- Fixed Door Lockdown rows overflowing because of long room names and the scrollbar.
- Fixed an IL2CPP `ObjectCollectedException` caused by `GUILayout.MaxWidth`, which also unbalanced Unity's GUIClip stack.
- Fixed kill cooldown incorrectly showing `READY` at round start.
- Fixed repeated null-reference errors in player visibility handling.
- Improved fully vanished Phantom and protection-effect visibility restoration.
- Fixed high FPS loss caused by unrestricted `RpcFlag`, task, role, and other verbose Unity console messages.

## v1.3.7

- Fixed known issues.
- Improved UI.
- Updated configuration options.
- Added new features and stability improvements.
