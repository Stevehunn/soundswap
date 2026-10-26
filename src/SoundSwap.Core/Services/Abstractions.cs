using SoundSwap.Core.Models;

namespace SoundSwap.Core.Services;

/// <summary>Access to Windows audio endpoints. Implemented in SoundSwap.Audio.</summary>
public interface IAudioDeviceService
{
    /// <summary>Lists the active devices of the given direction.</summary>
    /// /// <param name="flow">Output (playback) or input (recording).</param>
    IReadOnlyList<AudioDevice> GetDevices(DeviceFlow flow);
    /// <summary>Id of the current Windows default device, or <c>null</c> if there is none.</summary>
    /// /// <param name="flow">Output (playback) or input (recording).</param>
    string? GetDefaultDeviceId(DeviceFlow flow);
    /// <summary>Makes a device the Windows default for every role (console, multimedia, communications).</summary>
    /// /// <param name="deviceId">Endpoint id from <see cref="GetDevices"/>.</param>
    /// /// <param name="flow">Output (playback) or input (recording).</param>
    void SetDefaultDevice(string deviceId, DeviceFlow flow);
}

/// <summary>Persistence of <see cref="AppState"/>.</summary>
public interface IStateStore
{
    /// <summary>Reads the saved state, or returns a new default state when nothing is saved.</summary>
    AppState Load();
    /// <summary>Writes the state.</summary>
    /// /// <param name="state">The state to persist.</param>
    void Save(AppState state);
}

/// <summary>Outcome of an attempt to activate a configuration.</summary>
public enum SwitchStatus
{
    /// <summary>The devices were switched.</summary>
    Success,
    /// <summary>No configuration has the requested id.</summary>
    NotFound,
    /// <summary>There is no configuration to switch to.</summary>
    NoConfigurations,
    /// <summary>One of the devices is unplugged or disabled.</summary>
    DeviceMissing,
    /// <summary>Windows refused the change (unexpected error).</summary>
    Failed
}
