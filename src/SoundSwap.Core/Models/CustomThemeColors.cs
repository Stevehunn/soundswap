namespace SoundSwap.Core.Models;

/// <summary>The four colors the user picks for the custom theme (hex "#RRGGBB"). Everything else is derived.</summary>
public sealed class CustomThemeColors
{
    /// <summary>Window background (<c>#RRGGBB</c>).</summary>
    public string Background { get; set; } = "#0B1620";
    /// <summary>Cards, panels and dropdowns.</summary>
    public string Surface { get; set; } = "#12263A";
    /// <summary>Main text color.</summary>
    public string Text { get; set; } = "#E6F1FF";
    /// <summary>Buttons, active card, highlights.</summary>
    public string Accent { get; set; } = "#4CC9F0";

    /// <summary>Returns an independent copy.</summary>
    public CustomThemeColors Clone() => new() { Background = Background, Surface = Surface, Text = Text, Accent = Accent };
}
