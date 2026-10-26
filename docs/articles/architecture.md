# Architecture

| Project | Role | Depends on |
|---|---|---|
| `SoundSwap.Core` | Models, `ConfigurationService`, JSON store, `HotkeyGesture`, theme palette, updater (`UpdateChecker`, `UpdateDownloader`). No UI, no Windows API: fully unit-tested | none |
| `SoundSwap.Audio` | `IAudioDeviceService` implementation: NAudio (WASAPI) to list, COM `IPolicyConfig` to set defaults | Core |
| `SoundSwap.App` | WPF app (MVVM with CommunityToolkit.Mvvm): views, tray icon, global hotkeys, themes, localization, `UpdateService` | Core, Audio |
| `SoundSwap.Core.Tests` | xUnit tests with a fake audio service and an in-memory store | Core |

## Data flow
```
Hotkey / tray menu / ▶ button
        │
        ▼
HotkeyCoordinator ──► ConfigurationService.Activate ──► IAudioDeviceService (NAudio + IPolicyConfig)
                              │
                              ├─ events: ActiveChanged, Activated, ActivationFailed ─► tray toast, UI refresh
                              └─ IStateStore (JsonStateStore) ─► %APPDATA%\SoundSwap\data.json
```

## Decisions
- **Device switching** uses `IPolicyConfig`, the undocumented COM interface behind the Windows Sound panel. It has been stable since
  Windows 7 but is not guaranteed by Microsoft; it is isolated in `SoundSwap.Audio` so it can be replaced.
- **Global shortcuts** use `RegisterHotKey` on a hidden message-only window: they work while the main window is hidden.
  While a shortcut is being recorded they are released so the combination reaches the text box.
- **State** is one JSON file written atomically; a corrupt file is kept as `.bak`.
- **Localization**: one embedded JSON file per language; XAML uses `{loc:Loc key}` bindings so the language changes live.
- **Themes**: brushes are looked up with `DynamicResource`; `ThemeService` swaps the dictionary (XAML files, or one built from
  the `ThemePalette` derived from four user colors).
- **Single instance**: a named mutex; a second launch signals the first one to show its window.

## Adding things
- *A language*: new `Localization/Lang/xx.json` + one line in `Localizer.Languages`.
- *A setting*: property in `AppSettings`, bind it in `MainViewModel`, persist with `ConfigurationService.Commit()`.
- *A theme preset*: one line in `ThemePresets`.
