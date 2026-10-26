using SoundSwap.Core.Models;
using SoundSwap.Core.Services;

namespace SoundSwap.Core.Tests;

internal sealed class FakeAudio : IAudioDeviceService
{
    public List<AudioDevice> Devices { get; } = new();
    public Dictionary<DeviceFlow, string?> Defaults { get; } = new() { [DeviceFlow.Output] = null, [DeviceFlow.Input] = null };

    public IReadOnlyList<AudioDevice> GetDevices(DeviceFlow flow) => Devices.Where(d => d.Flow == flow).ToList();
    public string? GetDefaultDeviceId(DeviceFlow flow) => Defaults[flow];
    public void SetDefaultDevice(string deviceId, DeviceFlow flow) => Defaults[flow] = deviceId;
}

internal sealed class MemoryStore : IStateStore
{
    public AppState State { get; set; } = new();
    public AppState Load() => State;
    public void Save(AppState state) => State = state;
}
