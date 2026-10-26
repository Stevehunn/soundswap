using System.Windows;
using System.Windows.Controls;

namespace SoundSwap.Desktop.Controls;

/// <summary>Shortcut recorder + a "✕" button that resets the shortcut to "none".</summary>
public partial class HotkeyField : UserControl
{
    /// <summary>Backing property of <see cref="Value"/>.</summary>
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(string), typeof(HotkeyField),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));

    /// <summary>Creates the field.</summary>
    public HotkeyField() => InitializeComponent();

    /// <summary>The recorded shortcut, or <c>null</c>. Bind it two-way.</summary>
    public string? Value
    {
        get => (string?)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((HotkeyField)d).ClearButton.Visibility = string.IsNullOrEmpty(e.NewValue as string) ? Visibility.Collapsed : Visibility.Visible;

    private void OnClear(object sender, RoutedEventArgs e) => Value = null;
}
