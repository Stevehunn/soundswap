using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using SoundSwap.Core.Hotkeys;
using SoundSwap.Core.Models;
using SoundSwap.Core.Services;
using SoundSwap.Desktop.Localization;

namespace SoundSwap.Desktop.ViewModels;

/// <summary>State of the "new / edit configuration" dialog.</summary>
public sealed partial class ConfigEditorViewModel : ObservableObject
{
    private readonly ConfigurationService _config;
    private readonly AudioConfiguration? _existing;

    /// <summary>Creates the dialog state.</summary>
    /// /// <param name="outputs">Available output devices.</param>
    /// /// <param name="inputs">Available input devices.</param>
    /// /// <param name="config">Used to detect shortcut conflicts.</param>
    /// /// <param name="existing">The configuration being edited, or <c>null</c> for a new one.</param>
    public ConfigEditorViewModel(IReadOnlyList<AudioDevice> outputs, IReadOnlyList<AudioDevice> inputs,
                                 ConfigurationService config, AudioConfiguration? existing)
    {
        _config = config;
        _existing = existing;
        Title = Localizer.Instance[existing is null ? "new_config_title" : "edit"];
        Outputs = WithSaved(outputs, existing?.OutputDeviceId, existing?.OutputDeviceName, DeviceFlow.Output);
        Inputs = WithSaved(inputs, existing?.InputDeviceId, existing?.InputDeviceName, DeviceFlow.Input);

        if (existing is null) return;
        _name = existing.Name;
        _selectedOutput = Outputs.FirstOrDefault(d => d.Id == existing.OutputDeviceId);
        _selectedInput = Inputs.FirstOrDefault(d => d.Id == existing.InputDeviceId);
        _hotkey = existing.Hotkey;
    }

    /// <summary>Window title (new or edit).</summary>
    public string Title { get; }
    /// <summary>Output devices to choose from (an unplugged saved device stays listed, marked).</summary>
    public IReadOnlyList<AudioDevice> Outputs { get; }
    /// <summary>Input devices to choose from.</summary>
    public IReadOnlyList<AudioDevice> Inputs { get; }
    /// <summary>The configuration to save, valid after <see cref="TryValidate"/> returned <c>true</c>.</summary>
    public AudioConfiguration Result { get; private set; } = new();
    /// <summary>True when <see cref="Error"/> has text.</summary>
    public bool HasError => !string.IsNullOrEmpty(Error);

    /// <summary>Name typed by the user.</summary>
    [ObservableProperty] private string _name = "";
    /// <summary>Chosen output device.</summary>
    [ObservableProperty] private AudioDevice? _selectedOutput;
    /// <summary>Chosen input device.</summary>
    [ObservableProperty] private AudioDevice? _selectedInput;
    /// <summary>Recorded shortcut or <c>null</c>.</summary>
    [ObservableProperty] private string? _hotkey;
    /// <summary>Validation message shown in the dialog.</summary>
    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasError))] private string? _error;

    /// <summary>Validates the form; on success <see cref="Result"/> holds the configuration to save.</summary>
    public bool TryValidate()
    {
        var loc = Localizer.Instance;
        if (string.IsNullOrWhiteSpace(Name) || SelectedOutput is null || SelectedInput is null)
        {
            Error = loc["error_invalid"];
            return false;
        }
        if (_config.IsHotkeyUsed(Hotkey, _existing?.Id))
        {
            Error = loc["error_hotkey_used"];
            return false;
        }

        Result = new AudioConfiguration
        {
            Id = _existing?.Id ?? Guid.NewGuid(),
            Name = Name.Trim(),
            OutputDeviceId = SelectedOutput.Id,
            OutputDeviceName = SelectedOutput.Name,
            InputDeviceId = SelectedInput.Id,
            InputDeviceName = SelectedInput.Name,
            Hotkey = HotkeyGesture.Normalize(Hotkey)
        };
        return true;
    }

    // Keeps an unplugged device visible (marked) so editing a configuration does not silently drop it.
    private static IReadOnlyList<AudioDevice> WithSaved(IReadOnlyList<AudioDevice> devices, string? id, string? name, DeviceFlow flow)
    {
        if (string.IsNullOrEmpty(id) || devices.Any(d => d.Id == id)) return devices;
        return devices.Prepend(new AudioDevice(id, $"{name ?? id} ({Localizer.Instance["device_missing"]})", flow)).ToList();
    }
}
