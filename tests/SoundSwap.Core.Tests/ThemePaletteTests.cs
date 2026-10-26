using SoundSwap.Core.Models;
using SoundSwap.Core.Theming;
using Xunit;

namespace SoundSwap.Core.Tests;

public class ThemePaletteTests
{
    [Theory]
    [InlineData("#0b1620")]
    [InlineData("0B1620")]
    [InlineData("  #FFFFFF ")]
    public void Rgb_ParsesValidHex(string text) => Assert.True(Rgb.TryParse(text, out _));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("#12")]
    [InlineData("zzzzzz")]
    [InlineData("#1234567")]
    public void Rgb_RejectsInvalidHex(string? text) => Assert.False(Rgb.TryParse(text, out _));

    [Fact]
    public void Rgb_RoundTripsHex()
    {
        Rgb.TryParse("#4cc9f0", out var color);
        Assert.Equal("#4CC9F0", color.ToHex());
        Assert.Equal("#334CC9F0", color.ToHex(0x33));
    }

    [Fact]
    public void Contrast_BlackOnWhiteIs21() =>
        Assert.Equal(21.0, Rgb.Contrast(new Rgb(0, 0, 0), new Rgb(255, 255, 255)), 2);

    [Fact]
    public void Palette_DetectsDarkAndLightBackgrounds()
    {
        Assert.True(ThemePalette.From(new CustomThemeColors()).IsDark);
        Assert.False(ThemePalette.From(ThemePresets.All.Single(p => p.Name == "Paper").Colors).IsDark);
    }

    [Fact]
    public void Palette_PicksReadableTextOnAccent()
    {
        var onYellow = ThemePalette.From(new CustomThemeColors { Accent = "#FFFF00" }).OnAccent;
        var onNavy = ThemePalette.From(new CustomThemeColors { Accent = "#000080" }).OnAccent;
        Assert.NotEqual("#FFFFFF", onYellow); // dark text on a bright accent
        Assert.Equal("#FFFFFF", onNavy);      // white text on a dark accent
    }

    [Fact]
    public void Palette_FallsBackToDefaultsOnInvalidColors()
    {
        var broken = ThemePalette.From(new CustomThemeColors { Background = "nope", Surface = "", Text = "#12", Accent = "x" });
        var defaults = ThemePalette.From(new CustomThemeColors());
        Assert.Equal(defaults.Window, broken.Window);
        Assert.Equal(defaults.Accent, broken.Accent);
    }

    [Fact]
    public void Palette_FlagsLowContrast()
    {
        Assert.True(ThemePalette.From(new CustomThemeColors { Background = "#808080", Surface = "#888888", Text = "#999999" }).HasLowContrast);
        Assert.False(ThemePalette.From(new CustomThemeColors()).HasLowContrast);
    }

    [Fact]
    public void AllPresets_AreReadable()
    {
        foreach (var preset in ThemePresets.All)
            Assert.False(ThemePalette.From(preset.Colors).HasLowContrast, preset.Name);
    }
}
