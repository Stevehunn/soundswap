using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SoundSwap.Core.Models;
using SoundSwap.Core.Theming;
using SoundSwap.Desktop.Services;
using System.Collections.Generic;

namespace SoundSwap.Desktop.ViewModels;

/// <summary>One of the four editable colors.</summary>
public enum ColorSlot { Background, Surface, Text, Accent }

/// <summary>Edits the four custom-theme colors; every valid change is applied to the whole app immediately.</summary>
public sealed partial class ThemeEditorViewModel : ObservableObject
{
    private readonly CustomThemeColors _colors;
    private bool _batch;

    /// <summary>Creates the editor on the live custom-theme colors.</summary>
    /// /// <param name="colors">The settings object that is edited (changes are applied immediately).</param>
    public ThemeEditorViewModel(CustomThemeColors colors)
    {
        _colors = colors;
        _backgroundHex = colors.Background;
        _surfaceHex = colors.Surface;
        _textHex = colors.Text;
        _accentHex = colors.Accent;
        _lowContrast = ThemePalette.From(colors).HasLowContrast;
    }

    /// <summary>Ready-made color sets.</summary>
    public IReadOnlyList<ThemePreset> Presets => ThemePresets.All;

    /// <summary>Background color as <c>#RRGGBB</c> text.</summary>
    [ObservableProperty] private string _backgroundHex;
    /// <summary>Cards and panels color.</summary>
    [ObservableProperty] private string _surfaceHex;
    /// <summary>Text color.</summary>
    [ObservableProperty] private string _textHex;
    /// <summary>Accent color.</summary>
    [ObservableProperty] private string _accentHex;
    /// <summary>True when the current text color is hard to read.</summary>
    [ObservableProperty] private bool _lowContrast;

    partial void OnBackgroundHexChanged(string value) => Update(ColorSlot.Background, value);
    partial void OnSurfaceHexChanged(string value) => Update(ColorSlot.Surface, value);
    partial void OnTextHexChanged(string value) => Update(ColorSlot.Text, value);
    partial void OnAccentHexChanged(string value) => Update(ColorSlot.Accent, value);

    /// <summary>Current text of a color.</summary>
    /// /// <param name="slot">Which color.</param>
    public string GetHex(ColorSlot slot) => slot switch
    {
        ColorSlot.Background => BackgroundHex,
        ColorSlot.Surface => SurfaceHex,
        ColorSlot.Text => TextHex,
        _ => AccentHex
    };

    /// <summary>Sets a color (from the color dialog).</summary>
    /// /// <param name="slot">Which color.</param>
    /// /// <param name="hex"><c>#RRGGBB</c> value.</param>
    public void SetColor(ColorSlot slot, string hex)
    {
        switch (slot)
        {
            case ColorSlot.Background: BackgroundHex = hex; break;
            case ColorSlot.Surface: SurfaceHex = hex; break;
            case ColorSlot.Text: TextHex = hex; break;
            default: AccentHex = hex; break;
        }
    }

    [RelayCommand]
    private void ApplyPreset(ThemePreset preset)
    {
        _batch = true; // four assignments, one refresh
        BackgroundHex = preset.Colors.Background;
        SurfaceHex = preset.Colors.Surface;
        TextHex = preset.Colors.Text;
        AccentHex = preset.Colors.Accent;
        _batch = false;
        Refresh();
    }

    [RelayCommand]
    private void Reset() => ApplyPreset(ThemePresets.All[0]);

    // Invalid text (while typing) is ignored: the last valid color stays applied.
    private void Update(ColorSlot slot, string text)
    {
        if (!Rgb.TryParse(text, out var rgb)) return;
        var hex = rgb.ToHex();
        switch (slot)
        {
            case ColorSlot.Background: _colors.Background = hex; break;
            case ColorSlot.Surface: _colors.Surface = hex; break;
            case ColorSlot.Text: _colors.Text = hex; break;
            default: _colors.Accent = hex; break;
        }
        if (!_batch) Refresh();
    }

    private void Refresh()
    {
        ThemeService.Apply(ThemeMode.Custom, _colors);
        LowContrast = ThemePalette.From(_colors).HasLowContrast;
    }
}
