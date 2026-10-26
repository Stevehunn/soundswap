namespace SoundSwap.Core.Models;

/// <summary>Which color theme the interface uses.</summary>
public enum ThemeMode
{
    /// <summary>Follow the Windows light/dark setting.</summary>
    System,
    /// <summary>Built-in dark theme.</summary>
    Dark,
    /// <summary>Built-in light theme.</summary>
    Light,
    /// <summary>The theme made from the four colors in <see cref="AppSettings.CustomTheme"/>.</summary>
    Custom
}

/// <summary>User preferences, saved in <c>%APPDATA%\SoundSwap\data.json</c>.</summary>
public sealed class AppSettings
{
    /// <summary>Two-letter language code; empty = detect from Windows.</summary>
    public string Language { get; set; } = "";
    /// <summary>Selected theme.</summary>
    public ThemeMode Theme { get; set; } = ThemeMode.Dark;
    /// <summary>Colors used when <see cref="Theme"/> is <see cref="ThemeMode.Custom"/>.</summary>
    public CustomThemeColors CustomTheme { get; set; } = new();
    /// <summary>Global shortcut that activates the next configuration (e.g. <c>Ctrl+Alt+Right</c>), or <c>null</c> for none.</summary>
    public string? NextHotkey { get; set; } = "Ctrl+Alt+Right";
    /// <summary>Whether SoundSwap starts (minimized) when the user signs in to Windows.</summary>
    public bool StartWithWindows { get; set; }
    /// <summary>Whether a notification is shown after each switch.</summary>
    public bool NotifyOnSwitch { get; set; } = true;
    /// <summary>Whether SoundSwap checks GitHub for a newer release (at startup, then every 12 hours).</summary>
    public bool CheckForUpdates { get; set; } = true;
}

/// <summary>Everything persisted to disk.</summary>
public sealed class AppState
{
    /// <summary>Preferences.</summary>
    public AppSettings Settings { get; set; } = new();
    /// <summary>The saved configurations, in display order. This order is also the order used by the "next configuration" shortcut.</summary>
    public List<AudioConfiguration> Configurations { get; set; } = new();
    /// <summary>Id of the configuration currently applied, or <c>null</c> if none matches the Windows defaults.</summary>
    public Guid? ActiveConfigurationId { get; set; }
}
