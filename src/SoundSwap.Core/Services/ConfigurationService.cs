using SoundSwap.Core.Hotkeys;
using SoundSwap.Core.Models;

namespace SoundSwap.Core.Services;

/// <summary>Owns the configurations and the application state; performs the actual device switching.</summary>
public sealed class ConfigurationService
{
    private readonly IAudioDeviceService _audio;
    private readonly IStateStore _store;

    /// <summary>Creates the service and loads the saved state.</summary>
    /// /// <param name="audio">Access to the Windows audio devices.</param>
    /// /// <param name="store">Where the state is persisted.</param>
    public ConfigurationService(IAudioDeviceService audio, IStateStore store)
    {
        _audio = audio;
        _store = store;
        State = store.Load();
    }

    /// <summary>The live application state (settings, configurations, active id). Call <see cref="Commit"/> after modifying it.</summary>
    public AppState State { get; }
    /// <summary>The configurations, in display and cycling order.</summary>
    public IReadOnlyList<AudioConfiguration> Configurations => State.Configurations;
    /// <summary>Id of the active configuration, or <c>null</c>.</summary>
    public Guid? ActiveId => State.ActiveConfigurationId;

    /// <summary>Configurations or settings changed (hotkeys must be re-registered).</summary>
    public event EventHandler? Changed;
    /// <summary>The active configuration changed.</summary>
    public event EventHandler? ActiveChanged;
    /// <summary>Raised after a configuration was successfully applied.</summary>
    public event EventHandler<AudioConfiguration>? Activated;
    /// <summary>Raised when a configuration could not be applied (device unplugged or Windows error).</summary>
    public event EventHandler<AudioConfiguration>? ActivationFailed;

    /// <summary>Appends a configuration and saves.</summary>
    /// /// <param name="configuration">The configuration to add.</param>
    public void Add(AudioConfiguration configuration)
    {
        State.Configurations.Add(configuration);
        Commit();
    }

    /// <summary>Replaces the configuration that has the same id, and saves. Does nothing if the id is unknown.</summary>
    /// /// <param name="configuration">The new version of the configuration.</param>
    public void Update(AudioConfiguration configuration)
    {
        var index = State.Configurations.FindIndex(c => c.Id == configuration.Id);
        if (index < 0) return;
        State.Configurations[index] = configuration;
        Commit();
    }

    /// <summary>Deletes a configuration and saves.</summary>
    /// /// <param name="id">Id of the configuration to remove.</param>
    public void Remove(Guid id)
    {
        if (State.Configurations.RemoveAll(c => c.Id == id) == 0) return;
        if (State.ActiveConfigurationId == id) State.ActiveConfigurationId = null;
        Commit();
    }

    /// <summary>Moves a configuration to <paramref name="newIndex"/> (final position). This is also the cycling order.</summary>
    public void Move(Guid id, int newIndex)
    {
        var list = State.Configurations;
        var oldIndex = list.FindIndex(c => c.Id == id);
        if (oldIndex < 0) return;

        newIndex = Math.Clamp(newIndex, 0, list.Count - 1);
        if (newIndex == oldIndex) return;

        var item = list[oldIndex];
        list.RemoveAt(oldIndex);
        list.Insert(newIndex, item);
        Commit();
    }

    /// <summary>Persists the state and raises <see cref="Changed"/>.</summary>
    public void Commit()
    {
        _store.Save(State);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Tells whether a shortcut is already assigned (to the "next" shortcut or to another configuration). Comparison is on the normalized form, so <c>alt+ctrl+d1</c> equals <c>Ctrl+Alt+D1</c>.</summary>
    /// /// <param name="hotkey">The shortcut to look for.</param>
    /// /// <param name="exceptConfiguration">A configuration to ignore (the one being edited).</param>
    public bool IsHotkeyUsed(string? hotkey, Guid? exceptConfiguration)
    {
        var wanted = HotkeyGesture.Normalize(hotkey);
        if (wanted is null) return false;
        bool Same(string? other) => string.Equals(HotkeyGesture.Normalize(other), wanted, StringComparison.OrdinalIgnoreCase);

        return Same(State.Settings.NextHotkey) ||
               State.Configurations.Any(c => c.Id != exceptConfiguration && Same(c.Hotkey));
    }

    /// <summary>Applies a configuration: sets its output and input devices as the Windows defaults.</summary>
    /// /// <param name="id">Id of the configuration.</param>
    /// /// <returns>The outcome; on failure <see cref="ActivationFailed"/> is raised.</returns>
    public SwitchStatus Activate(Guid id)
    {
        var configuration = State.Configurations.Find(c => c.Id == id);
        if (configuration is null) return SwitchStatus.NotFound;
        var status = TrySwitch(configuration);
        if (status != SwitchStatus.Success) ActivationFailed?.Invoke(this, configuration);
        return status;
    }

    /// <summary>Switches to the next configuration, skipping those whose devices are unplugged.</summary>
    public SwitchStatus ActivateNext()
    {
        var list = State.Configurations;
        if (list.Count == 0) return SwitchStatus.NoConfigurations;

        var start = list.FindIndex(c => c.Id == State.ActiveConfigurationId);
        var last = SwitchStatus.DeviceMissing;
        for (var step = 1; step <= list.Count; step++)
        {
            var candidate = list[(start + step + list.Count) % list.Count];
            last = TrySwitch(candidate);
            if (last == SwitchStatus.Success) return last;
        }
        ActivationFailed?.Invoke(this, list[(start + 1 + list.Count) % list.Count]);
        return last;
    }

    /// <summary>Marks as active the configuration matching the current Windows defaults, if any.</summary>
    public void RefreshActiveFromSystem()
    {
        try
        {
            var output = _audio.GetDefaultDeviceId(DeviceFlow.Output);
            var input = _audio.GetDefaultDeviceId(DeviceFlow.Input);
            var match = State.Configurations.Find(c => c.OutputDeviceId == output && c.InputDeviceId == input);
            if (match?.Id == State.ActiveConfigurationId) return;
            State.ActiveConfigurationId = match?.Id;
            ActiveChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception)
        {
            // Audio subsystem unavailable: keep the previous state.
        }
    }

    private SwitchStatus TrySwitch(AudioConfiguration configuration)
    {
        try
        {
            var outputs = _audio.GetDevices(DeviceFlow.Output);
            var inputs = _audio.GetDevices(DeviceFlow.Input);
            if (!outputs.Any(d => d.Id == configuration.OutputDeviceId) ||
                !inputs.Any(d => d.Id == configuration.InputDeviceId))
                return SwitchStatus.DeviceMissing;

            _audio.SetDefaultDevice(configuration.OutputDeviceId, DeviceFlow.Output);
            _audio.SetDefaultDevice(configuration.InputDeviceId, DeviceFlow.Input);
        }
        catch (Exception)
        {
            return SwitchStatus.Failed;
        }

        State.ActiveConfigurationId = configuration.Id;
        _store.Save(State);
        ActiveChanged?.Invoke(this, EventArgs.Empty);
        Activated?.Invoke(this, configuration);
        return SwitchStatus.Success;
    }
}
