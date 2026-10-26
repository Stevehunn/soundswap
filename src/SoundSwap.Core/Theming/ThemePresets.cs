using SoundSwap.Core.Models;

namespace SoundSwap.Core.Theming;

/// <summary>A named set of custom-theme colors.</summary>
/// <param name="Name">Preset name (e.g. "Ocean").</param>
/// <param name="Colors">The four colors of the preset.</param>
public sealed record ThemePreset(string Name, CustomThemeColors Colors);

/// <summary>Ready-made starting points for the custom theme (first one is the default).</summary>
public static class ThemePresets
{
    /// <summary>All presets, in display order. The first one (Ocean) is also the default of <see cref="CustomThemeColors"/>.</summary>
    public static IReadOnlyList<ThemePreset> All { get; } = new[]
    {
        Make("Ocean",  "#0B1620", "#12263A", "#E6F1FF", "#4CC9F0"),
        Make("Violet", "#16111F", "#231A33", "#F0E9FF", "#A78BFA"),
        Make("Sunset", "#1C1412", "#2B1D19", "#FFEFE6", "#FF8A4C"),
        Make("Forest", "#0F1A14", "#17281F", "#E8F5EC", "#5BD68A"),
        Make("Paper",  "#F7F3EE", "#FFFFFF", "#2B2622", "#E07A5F"),
        Make("Rose",   "#FDF2F5", "#FFFFFF", "#3A2630", "#E5527C"),
    };

    private static ThemePreset Make(string name, string background, string surface, string text, string accent) =>
        new(name, new CustomThemeColors { Background = background, Surface = surface, Text = text, Accent = accent });
}
