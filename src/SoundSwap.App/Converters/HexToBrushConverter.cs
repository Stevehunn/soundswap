using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using SoundSwap.Core.Theming;

namespace SoundSwap.Desktop.Converters;

/// <summary>"#RRGGBB" text to a brush (used by the color swatches of the theme editor).</summary>
public sealed class HexToBrushConverter : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string text && Rgb.TryParse(text, out var rgb))
        {
            var brush = new SolidColorBrush(Color.FromRgb(rgb.R, rgb.G, rgb.B));
            brush.Freeze();
            return brush;
        }
        return Brushes.Transparent;
    }

    /// <inheritdoc/>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
