namespace SoundSwap.Core.Models;

/// <summary>Direction of an audio endpoint.</summary>
public enum DeviceFlow
{
    /// <summary>Playback device (speakers, headphones...).</summary>
    Output,
    /// <summary>Recording device (microphone...).</summary>
    Input
}

/// <summary>An active audio endpoint (speakers, headset, microphone...).</summary>
public sealed record AudioDevice(string Id, string Name, DeviceFlow Flow)
{
    /// <summary>Safety net: any UI that falls back to ToString() shows the friendly name, never the technical id.</summary>
    public override string ToString() => Name;
}
