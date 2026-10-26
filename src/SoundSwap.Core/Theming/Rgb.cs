using System.Globalization;

namespace SoundSwap.Core.Theming;

/// <summary>Minimal UI-independent color with the math needed to derive a theme (mix, luminance, contrast).</summary>
public readonly record struct Rgb(byte R, byte G, byte B)
{
    /// <summary>Accepts "#RRGGBB" or "RRGGBB".</summary>
    public static bool TryParse(string? text, out Rgb color)
    {
        color = default;
        if (text is null) return false;
        var hex = text.Trim().TrimStart('#');
        if (hex.Length != 6 || !int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value)) return false;
        color = new Rgb((byte)(value >> 16), (byte)(value >> 8), (byte)value);
        return true;
    }

    /// <summary><c>#RRGGBB</c> uppercase form.</summary>
    public string ToHex() => $"#{R:X2}{G:X2}{B:X2}";

    /// <summary>"#AARRGGBB" form (WPF), with the given opacity.</summary>
    public string ToHex(byte alpha) => $"#{alpha:X2}{R:X2}{G:X2}{B:X2}";

    /// <summary>Linear mix: t = 0 gives <paramref name="a"/>, t = 1 gives <paramref name="b"/>.</summary>
    public static Rgb Mix(Rgb a, Rgb b, double t) => new(Lerp(a.R, b.R, t), Lerp(a.G, b.G, t), Lerp(a.B, b.B, t));

    /// <summary>WCAG relative luminance (0 = black, 1 = white).</summary>
    public double Luminance => 0.2126 * Channel(R) + 0.7152 * Channel(G) + 0.0722 * Channel(B);

    /// <summary>WCAG contrast ratio, from 1 (identical) to 21 (black on white).</summary>
    public static double Contrast(Rgb a, Rgb b)
    {
        var (light, dark) = a.Luminance >= b.Luminance ? (a.Luminance, b.Luminance) : (b.Luminance, a.Luminance);
        return (light + 0.05) / (dark + 0.05);
    }

    private static byte Lerp(byte a, byte b, double t) => (byte)Math.Round(a + (b - a) * Math.Clamp(t, 0, 1));

    private static double Channel(byte value)
    {
        var c = value / 255.0;
        return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
    }
}
