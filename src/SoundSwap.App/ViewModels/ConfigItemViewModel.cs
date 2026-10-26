using CommunityToolkit.Mvvm.ComponentModel;
using SoundSwap.Core.Hotkeys;
using SoundSwap.Core.Models;
using SoundSwap.Desktop.Localization;

namespace SoundSwap.Desktop.ViewModels;

/// <summary>Row of the configuration list. Updated in place so the list does not flicker or replay animations.</summary>
public sealed partial class ConfigItemViewModel : ObservableObject
{
    /// <summary>Creates a row.</summary>
    /// /// <param name="config">The configuration shown.</param>
    /// /// <param name="isActive">Whether it is the active one.</param>
    /// /// <param name="hotkeyFailed">Whether its shortcut could not be registered.</param>
    public ConfigItemViewModel(AudioConfiguration config, bool isActive, bool hotkeyFailed)
    {
        Config = config;
        Update(config, isActive, hotkeyFailed);
    }

    /// <summary>The configuration behind this row.</summary>
    public AudioConfiguration Config { get; private set; }
    /// <summary>True when <see cref="HotkeyError"/> has text.</summary>
    public bool HasError => HotkeyError.Length > 0;

    /// <summary>Name of the configuration.</summary>
    [ObservableProperty] private string _name = "";
    /// <summary>Localized "Output: ... · Input: ..." line.</summary>
    [ObservableProperty] private string _subtitle = "";
    /// <summary>Shortcut as shown on the card ("None" when unset).</summary>
    [ObservableProperty] private string _shortcutDisplay = "";
    /// <summary>Whether this configuration is currently applied.</summary>
    [ObservableProperty] private bool _isActive;

    // Drag & drop visual state (set by the view while a card is being dragged).
    /// <summary>True while the card is being dragged.</summary>
    [ObservableProperty] private bool _isDragging;
    /// <summary>Shows the insertion line above the card while dragging.</summary>
    [ObservableProperty] private bool _dropBefore;
    /// <summary>Shows the insertion line below the card while dragging.</summary>
    [ObservableProperty] private bool _dropAfter;
    /// <summary>Localized message when the shortcut is taken by another program.</summary>
    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasError))] private string _hotkeyError = "";

    /// <summary>Refreshes the row in place (keeps the list stable, no flicker).</summary>
    /// /// <param name="config">Current configuration.</param>
    /// /// <param name="isActive">Whether it is active.</param>
    /// /// <param name="hotkeyFailed">Whether its shortcut failed to register.</param>
    public void Update(AudioConfiguration config, bool isActive, bool hotkeyFailed)
    {
        var loc = Localizer.Instance;
        Config = config;
        Name = config.Name;
        Subtitle = $"{loc["output_device"]} : {config.OutputDeviceName}  ·  {loc["input_device"]} : {config.InputDeviceName}";
        ShortcutDisplay = HotkeyGesture.TryParse(config.Hotkey, out var gesture) ? gesture.Display : loc["none"];
        IsActive = isActive;
        HotkeyError = hotkeyFailed ? loc["hotkey_conflict"] : "";
    }
}
