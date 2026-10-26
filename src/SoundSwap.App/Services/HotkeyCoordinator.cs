using System;
using System.Collections.Generic;
using SoundSwap.Core.Hotkeys;
using SoundSwap.Core.Services;
using SoundSwap.Desktop.Controls;

namespace SoundSwap.Desktop.Services;

/// <summary>Registers the "next" shortcut and one shortcut per configuration, and routes presses to the core.</summary>
public sealed class HotkeyCoordinator
{
    /// <summary>Identifier of the "next configuration" shortcut (the others use the configuration id).</summary>
    public const string NextId = "next";

    private readonly HotkeyService _service;
    private readonly ConfigurationService _config;

    /// <summary>Creates the coordinator and starts listening to shortcut presses.</summary>
    /// /// <param name="service">Low-level shortcut registration.</param>
    /// /// <param name="config">Configurations to activate.</param>
    public HotkeyCoordinator(HotkeyService service, ConfigurationService config)
    {
        _service = service;
        _config = config;
        _service.Pressed += OnPressed;
        _config.Changed += (_, _) => Refresh();
        // While the user records a shortcut, release all global hotkeys so the combo reaches the box.
        HotkeyBox.CaptureChanged += capturing => { if (capturing) _service.UnregisterAll(); else Refresh(); };
    }

    /// <summary>Ids whose shortcut could not be registered (already taken by another program).</summary>
    public HashSet<string> Failed { get; } = new();

    /// <summary>Raised after every re-registration (the UI refreshes its conflict messages).</summary>
    public event Action? Refreshed;

    /// <summary>Unregisters then registers again the "next" shortcut and one shortcut per configuration.</summary>
    public void Refresh()
    {
        _service.UnregisterAll();
        Failed.Clear();
        Try(NextId, _config.State.Settings.NextHotkey);
        foreach (var configuration in _config.Configurations) Try(configuration.Id.ToString(), configuration.Hotkey);
        Refreshed?.Invoke();
    }

    private void Try(string id, string? text)
    {
        if (!HotkeyGesture.TryParse(text, out var gesture)) return;
        if (!_service.Register(id, gesture)) Failed.Add(id);
    }

    private void OnPressed(string id)
    {
        if (id == NextId) _config.ActivateNext();
        else if (Guid.TryParse(id, out var guid)) _config.Activate(guid);
    }
}
