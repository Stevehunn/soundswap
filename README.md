<p align="center"><img src="docs/banner.png" alt="SoundSwap"></p>

<p align="center">
  <a href="https://github.com/Stevehunn/soundswap/releases/latest"><img alt="Release" src="https://img.shields.io/github/v/release/Stevehunn/soundswap?color=3DDBB0"></a>
  <a href="https://github.com/Stevehunn/soundswap/actions/workflows/build.yml"><img alt="Build" src="https://github.com/Stevehunn/soundswap/actions/workflows/build.yml/badge.svg"></a>
  <a href="LICENSE"><img alt="License: MIT" src="https://img.shields.io/badge/license-MIT-3DDBB0"></a>
  <img alt="Platform: Windows 10/11" src="https://img.shields.io/badge/platform-Windows%2010%2F11-1E8FD8">
</p>

**English** · [Français](README.fr.md) · [Español](README.es.md)

# SoundSwap

**Switch your Windows microphone and headset (or speakers) with one keyboard shortcut.**
SoundSwap sits in the notification area and changes the default **output** and **input** audio devices on the fly.
Free, open source ([MIT](LICENSE)), and part of the [SoundDeck](https://sounddeck.stream/soundswap) family.

## Features
- **Configurations**: a name + an output device + an input device (+ an optional shortcut), shown as cards you can **drag to reorder**.
- **Global shortcuts**: one shortcut for "next configuration" and one per configuration, working even when the window is minimized or hidden.
- **Notification area**: left click opens the window, right click switches configuration. Closing the window asks: minimize, quit, or cancel.
- **Themes**: dark, light, system, or **custom** (pick 4 colors, live preview, presets).
- **10 languages**: English, Français, Español, Deutsch, Português, Italiano, Русский, 中文, 日本語, العربية.
- **Built-in updates**: a *New version available* button downloads the latest GitHub release for you (verified), then you just install it.
- **Start with Windows** (minimized), single instance, optional notification on each switch.

## Install
1. Download **`SoundSwap-Setup-<version>.exe`** from the [latest release](https://github.com/Stevehunn/soundswap/releases/latest) and run it.
   It installs for the current user only: no administrator rights needed.
2. Prefer no installer? Download the `…-portable.zip`, unzip it anywhere and run `SoundSwap.exe`.

Requirements: Windows 10 or 11 (64-bit); .NET is included.
Windows SmartScreen may warn because the installer is not code-signed yet: *More info → Run anyway*.

## User guide
1. **Create a configuration**: *+ New configuration* → name, output device, input device, optional shortcut → *Confirm*.
2. **Apply it**: click **▶** on its card, press its shortcut, or pick it from the tray icon's menu.
3. **Cycle**: press the top shortcut (default `Ctrl + Alt + →`) to go to the next configuration. Order = card order (drag to change it).
   Configurations whose devices are unplugged are skipped.
4. **Record a shortcut**: click the field, press the keys (at least one of Ctrl / Alt / Shift / Win). `Esc`, `Backspace`, `Delete` or **✕** clears it.
   A red message means another program already uses that combination.
5. **Keep it running**: close the window with **X** → *Minimize to the notification area*. Shortcuts only work while SoundSwap runs.
6. **Start with Windows**: tick the box at the bottom. SoundSwap then starts hidden in the notification area when you sign in.
   (If you disable it in the Task Manager *Startup* tab, SoundSwap respects that.)
7. **Customize**: pick a theme and a language at the top. With *Custom*, click **Customize…** to choose the colors.

More: [full user guide](docs/articles/user-guide.md).

## Updates and privacy
SoundSwap checks the latest release of this repository ~10 seconds after launch and every 12 hours. If a newer version exists, a button appears:
click it to download the installer (only from `github.com`; size and SHA-256 are verified), click again to install.
The check is the **only** network request SoundSwap makes: no telemetry, no account. Untick *Check for updates automatically* to turn it off.
Details: [Updates](docs/articles/updates.md).

## Troubleshooting
| Problem | What to do |
|---|---|
| A shortcut shows a red "unavailable" message | Another program uses it: choose another combination |
| Shortcuts do nothing | SoundSwap was quit (not minimized): start it again and check the notification area |
| "Device unavailable" | One device of the configuration is unplugged or disabled in Windows |
| Not starting with Windows | Tick the option in SoundSwap; check the Task Manager *Startup* tab isn't set to Disabled |
| Lists show odd text / no update button | An old build is running: reinstall, or `dotnet clean` and rebuild; the footer shows the version (`v1.2.0`) |

Settings are saved in `%APPDATA%\SoundSwap\data.json`; errors are logged in `%APPDATA%\SoundSwap\error.log` (please attach it to bug reports).

## Build from source
Windows 10/11 + [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0):
```powershell
git clone https://github.com/Stevehunn/soundswap.git
cd soundswap
dotnet build SoundSwap.sln -c Release
dotnet test tests/SoundSwap.Core.Tests
dotnet run --project src/SoundSwap.App -c Release
.\scripts\publish.ps1 -Version 1.2.0 -Installer -Portable   # installer needs Inno Setup 6
```

## Project layout
| Project | Role |
|---|---|
| `src/SoundSwap.Core` | Logic: configurations, shortcuts, theme palette, updater. No UI, unit-tested |
| `src/SoundSwap.Audio` | Windows audio devices (NAudio + `IPolicyConfig`) |
| `src/SoundSwap.App` | WPF app (MVVM): views, tray, hotkeys, themes, localization |
| `tests/SoundSwap.Core.Tests` | xUnit tests |
| `installer/`, `scripts/`, `.github/workflows/` | Installer, build scripts, CI/release |

## Documentation
Every public type and member is documented with XML comments; the API reference and articles are generated with
[DocFX](https://dotnet.github.io/docfx/) from [`docs/`](docs/) (GitHub Pages). Start with [Architecture](docs/articles/architecture.md).

## Contributing
Issues and pull requests are welcome: see [CONTRIBUTING.md](CONTRIBUTING.md) (adding a language takes one JSON file).
Security: [SECURITY.md](SECURITY.md).

## License
[MIT](LICENSE) © 2026 Stevehunn. Third-party components: [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
