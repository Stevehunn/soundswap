# User guide

## Install
1. Download `SoundSwap-Setup-<version>.exe` from the [latest release](https://github.com/Stevehunn/soundswap/releases/latest).
2. Run it. The installation is **per user** (no administrator rights) and adds a Start menu entry.
3. A portable zip (`SoundSwap-<version>-win-x64-portable.zip`) is also published: unzip anywhere and run `SoundSwap.exe`.

Windows may show a SmartScreen warning because the installer is not code-signed yet: click *More info → Run anyway*.
Requirements: Windows 10 or 11, 64-bit. .NET is included.

## Create configurations
A **configuration** pairs one output device (speakers, headset) with one input device (microphone).
1. Click **+ New configuration**.
2. Type a name, pick the output and input devices, optionally record a shortcut, then **Confirm**.
3. Click **▶** on a card (or use its shortcut) to apply it. The active card is highlighted.

Edit or delete a card with its buttons. **Drag a card** to reorder: the order is also the order used by the
"next configuration" shortcut. If a device is unplugged, the "next" shortcut skips the configurations that need it.

## Shortcuts
- Click a shortcut field, then press the combination (for example `Ctrl + Alt + 1`). It must contain at least one of Ctrl, Alt, Shift or Win.
- `Esc`, `Backspace`, `Delete` or the **✕** button clears it.
- The shortcut at the top (default `Ctrl + Alt + →`) switches to the next configuration.
- Shortcuts work system-wide, also when the window is minimized or hidden, **as long as SoundSwap is running** in the notification area.
- A red message appears if another program already uses the combination.

## Notification area
Left click opens the window; right click lists your configurations (click one to apply it) and **Quit**.
The window's **X** asks: *Minimize to the notification area*, *Quit completely* (shortcuts stop), or *Cancel*.

## Themes and languages
- Themes: Dark, Light, System (follows Windows) and **Custom**: pick four colors (background, cards, text, accent), the rest is derived.
  Presets and a live preview are included; a warning appears if the text would be hard to read.
- Languages: Français, English, Español, Deutsch, Português, Italiano, Русский, 中文, 日本語, العربية.

## Start with Windows
Tick **Start with Windows**: SoundSwap then starts minimized in the notification area when you sign in
(per-user registry entry, no administrator rights). If you disable it in the Task Manager (*Startup* tab), SoundSwap respects that.

## Updates
SoundSwap checks the latest GitHub release shortly after it starts and every 12 hours. When a newer version exists a
button *New version available* appears: click it to download, then click *Install* and follow the installer.
See [Updates](updates.md). You can turn the check off (**Check for updates automatically**) or check manually.

## Your data
Settings and configurations are stored in `%APPDATA%\SoundSwap\data.json`; unexpected errors are written to `%APPDATA%\SoundSwap\error.log` (useful when reporting a bug). Nothing is sent anywhere; the only network
request is the optional update check to `api.github.com`.
