using SoundSwap.Core.Models;

namespace SoundSwap.Core.Theming;

/// <summary>All colors of the UI, derived from the four the user chose. Values are hex strings ("#RRGGBB" / "#AARRGGBB").</summary>
public sealed class ThemePalette
{
    /// <summary>True when the background is dark (used for the native title bar and the danger color).</summary>
    public bool IsDark { get; private init; }
    /// <summary>True when the text is hard to read on the background or on the cards (WCAG contrast below 4.5).</summary>
    public bool HasLowContrast { get; private init; }

    /// <summary>Window background.</summary>
    public string Window { get; private init; } = "";
    /// <summary>Cards and panels.</summary>
    public string Panel { get; private init; } = "";
    /// <summary>Card of the active configuration.</summary>
    public string ActivePanel { get; private init; } = "";
    /// <summary>Text fields and the shortcut chip.</summary>
    public string Input { get; private init; } = "";
    /// <summary>Hovered list items.</summary>
    public string Hover { get; private init; } = "";
    /// <summary>Borders and separators.</summary>
    public string Line { get; private init; } = "";
    /// <summary>Main text.</summary>
    public string Text { get; private init; } = "";
    /// <summary>Secondary text.</summary>
    public string Muted { get; private init; } = "";
    /// <summary>Highlights and primary buttons.</summary>
    public string Accent { get; private init; } = "";
    /// <summary>Text drawn on the accent color (black or white, whichever is more readable).</summary>
    public string OnAccent { get; private init; } = "";
    /// <summary>Hover overlay color on buttons (white on dark themes, black on light).</summary>
    public string Overlay { get; private init; } = "";
    /// <summary>Destructive actions and errors.</summary>
    public string Danger { get; private init; } = "";
    /// <summary>Translucent background of a destructive button on hover (<c>#AARRGGBB</c>).</summary>
    public string DangerSoft { get; private init; } = "";

    /// <summary>Luminance at which black and white text have the same contrast: below it, the color is "dark".</summary>
    private const double DarkThreshold = 0.18;
    private const double MinTextContrast = 4.5;

    /// <summary>Derives the full palette from the four user colors. Invalid colors fall back to the defaults.</summary>
    /// /// <param name="colors">The four colors chosen by the user.</param>
    public static ThemePalette From(CustomThemeColors colors)
    {
        var defaults = new CustomThemeColors();
        var background = Parse(colors.Background, defaults.Background);
        var surface = Parse(colors.Surface, defaults.Surface);
        var text = Parse(colors.Text, defaults.Text);
        var accent = Parse(colors.Accent, defaults.Accent);

        var dark = background.Luminance < DarkThreshold;
        var white = new Rgb(255, 255, 255);
        var nearBlack = new Rgb(0x0E, 0x15, 0x14);
        var danger = dark ? new Rgb(0xFF, 0x7B, 0x7B) : new Rgb(0xC6, 0x28, 0x28);

        return new ThemePalette
        {
            IsDark = dark,
            HasLowContrast = Rgb.Contrast(text, background) < MinTextContrast || Rgb.Contrast(text, surface) < MinTextContrast,
            Window = background.ToHex(),
            Panel = surface.ToHex(),
            ActivePanel = Rgb.Mix(surface, accent, 0.15).ToHex(),
            Input = Rgb.Mix(background, surface, 0.35).ToHex(),
            Hover = Rgb.Mix(surface, text, 0.08).ToHex(),
            Line = Rgb.Mix(surface, text, 0.15).ToHex(),
            Text = text.ToHex(),
            Muted = Rgb.Mix(text, surface, 0.35).ToHex(),
            Accent = accent.ToHex(),
            OnAccent = (Rgb.Contrast(nearBlack, accent) >= Rgb.Contrast(white, accent) ? nearBlack : white).ToHex(),
            Overlay = (surface.Luminance < DarkThreshold ? white : new Rgb(0, 0, 0)).ToHex(),
            Danger = danger.ToHex(),
            DangerSoft = danger.ToHex(dark ? (byte)0x33 : (byte)0x1A)
        };
    }

    private static Rgb Parse(string? hex, string fallback) =>
        Rgb.TryParse(hex, out var color) ? color : Rgb.TryParse(fallback, out var f) ? f : new Rgb(0, 0, 0);
}
