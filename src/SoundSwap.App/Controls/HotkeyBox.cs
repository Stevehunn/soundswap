using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SoundSwap.Core.Hotkeys;
using SoundSwap.Desktop.Localization;

namespace SoundSwap.Desktop.Controls;

/// <summary>Read-only text box that records a key combination. Esc / Backspace / Delete clears it.</summary>
public sealed class HotkeyBox : TextBox
{
    /// <summary>Raised with true when recording starts and false when it ends.</summary>
    public static event Action<bool>? CaptureChanged;

    /// <summary>Backing property of <see cref="Value"/>.</summary>
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(string), typeof(HotkeyBox),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            (d, _) => ((HotkeyBox)d).UpdateText(((HotkeyBox)d).IsKeyboardFocused)));

    /// <summary>Creates the box (read-only: keys are captured, not typed).</summary>
    public HotkeyBox()
    {
        IsReadOnly = true;
        IsReadOnlyCaretVisible = false;
        Cursor = Cursors.Arrow;
        SetResourceReference(StyleProperty, typeof(TextBox)); // reuse the app's TextBox style
        UpdateText(false);

        // Refresh the placeholder text when the language changes (unsubscribe on unload: no leak).
        Loaded += (_, _) => { Localizer.Instance.PropertyChanged += OnLanguageChanged; UpdateText(IsKeyboardFocused); };
        Unloaded += (_, _) => Localizer.Instance.PropertyChanged -= OnLanguageChanged;
    }

    /// <summary>The recorded shortcut in storage form (e.g. <c>Ctrl+Alt+D1</c>), or <c>null</c>.</summary>
    public string? Value
    {
        get => (string?)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <inheritdoc/>
    protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
    {
        base.OnGotKeyboardFocus(e);
        UpdateText(true);
        CaptureChanged?.Invoke(true);
    }

    /// <inheritdoc/>
    protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
    {
        base.OnLostKeyboardFocus(e);
        UpdateText(false);
        CaptureChanged?.Invoke(false);
    }

    /// <inheritdoc/>
    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        var modifiers = Keyboard.Modifiers;
        if (key == Key.Tab && modifiers == ModifierKeys.None) return; // keep keyboard navigation

        e.Handled = true;
        switch (key)
        {
            case Key.Escape: case Key.Back: case Key.Delete:
                Value = null;
                return;
            case Key.LeftCtrl: case Key.RightCtrl: case Key.LeftAlt: case Key.RightAlt:
            case Key.LeftShift: case Key.RightShift: case Key.LWin: case Key.RWin:
            case Key.None: case Key.DeadCharProcessed: case Key.ImeProcessed:
                return;
        }

        var result = HotkeyModifiers.None;
        if (modifiers.HasFlag(ModifierKeys.Control)) result |= HotkeyModifiers.Control;
        if (modifiers.HasFlag(ModifierKeys.Alt)) result |= HotkeyModifiers.Alt;
        if (modifiers.HasFlag(ModifierKeys.Shift)) result |= HotkeyModifiers.Shift;
        if (modifiers.HasFlag(ModifierKeys.Windows)) result |= HotkeyModifiers.Windows;
        if (result == HotkeyModifiers.None) return; // a global shortcut needs at least one modifier

        Value = new HotkeyGesture(result, key.ToString()).ToString();
    }

    private void OnLanguageChanged(object? sender, PropertyChangedEventArgs e) => UpdateText(IsKeyboardFocused);

    /// <summary>A recorded shortcut is always shown; the hint only appears while recording an empty field.</summary>
    private void UpdateText(bool focused)
    {
        Text = HotkeyGesture.TryParse(Value, out var gesture)
            ? gesture.Display
            : Localizer.Instance[focused ? "shortcut_hint" : "none"];
    }
}
