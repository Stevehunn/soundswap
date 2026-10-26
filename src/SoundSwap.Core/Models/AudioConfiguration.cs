namespace SoundSwap.Core.Models;

/// <summary>A saved pair "output device + input device", optionally bound to a global shortcut.</summary>
public sealed class AudioConfiguration
{
    /// <summary>Stable unique identifier (used by shortcuts, the tray menu and the active-configuration marker).</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>Name chosen by the user, e.g. "Headset".</summary>
    public string Name { get; set; } = "";
    /// <summary>Windows endpoint id of the output (playback) device.</summary>
    public string OutputDeviceId { get; set; } = "";
    /// <summary>Friendly name of the output device when it was saved (still shown if it is unplugged).</summary>
    public string OutputDeviceName { get; set; } = "";
    /// <summary>Windows endpoint id of the input (recording) device.</summary>
    public string InputDeviceId { get; set; } = "";
    /// <summary>Friendly name of the input device when it was saved.</summary>
    public string InputDeviceName { get; set; } = "";
    /// <summary>Normalized gesture such as "Ctrl+Alt+D1", or null.</summary>
    public string? Hotkey { get; set; }
}
