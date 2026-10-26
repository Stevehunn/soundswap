# Changelog

All notable changes are documented here. Format: [Keep a Changelog](https://keepachangelog.com/), versions follow [SemVer](https://semver.org/).

## [1.2.0]
### Added
- Built-in updater: checks the latest GitHub release, shows a "new version available" button, downloads the installer (size + SHA-256 verified) and starts it.
- Custom theme with live preview (4 colors, presets, low-contrast warning).
- Drag & drop to reorder configurations (also the cycling order of the "next" shortcut).
- Windows installer (Inno Setup, per-user, 8 languages) and a GitHub Actions release workflow.
- Version number in the window footer; option to turn automatic update checks off.
- XML documentation of the whole public API; DocFX documentation site; README in English, French and Spanish.
### Changed
- "Start with Windows": the stored path is repaired at each launch and a Task Manager "Disabled" choice is respected.
- Delete / Quit buttons turn red on hover; icons are drawn as vectors.
### Fixed
- Language and device dropdowns showed the object instead of its name.
- Shortcut text appeared only after leaving the field.

## [1.0.0]
- First version: configurations, global shortcuts, tray icon, 10 languages, dark/light/system themes.
