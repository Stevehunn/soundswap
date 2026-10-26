using System.Runtime.InteropServices;
using NAudio.CoreAudioApi;
using SoundSwap.Audio.Interop;
using SoundSwap.Core.Models;
using SoundSwap.Core.Services;

namespace SoundSwap.Audio;

/// <summary>Enumerates devices with NAudio (WASAPI) and changes defaults through IPolicyConfig.</summary>
public sealed class NAudioDeviceService : IAudioDeviceService
{
    /// <inheritdoc/>
    public IReadOnlyList<AudioDevice> GetDevices(DeviceFlow flow)
    {
        using var enumerator = new MMDeviceEnumerator();
        var result = new List<AudioDevice>();
        foreach (var device in enumerator.EnumerateAudioEndPoints(ToDataFlow(flow), DeviceState.Active))
        {
            result.Add(new AudioDevice(device.ID, device.FriendlyName, flow));
            device.Dispose();
        }
        return result;
    }

    /// <inheritdoc/>
    public string? GetDefaultDeviceId(DeviceFlow flow)
    {
        using var enumerator = new MMDeviceEnumerator();
        try
        {
            using var device = enumerator.GetDefaultAudioEndpoint(ToDataFlow(flow), Role.Console);
            return device.ID;
        }
        catch (COMException)
        {
            return null; // no device of this kind
        }
    }

    /// <inheritdoc/>
    /// <remarks>Uses the undocumented <c>IPolicyConfig</c> COM interface (the one behind the Windows Sound panel) for the Console, Multimedia and Communications roles.</remarks>
    public void SetDefaultDevice(string deviceId, DeviceFlow flow)
    {
        var policy = (IPolicyConfig)new PolicyConfigClient();
        try
        {
            foreach (var role in new[] { ERole.Console, ERole.Multimedia, ERole.Communications })
                Marshal.ThrowExceptionForHR(policy.SetDefaultEndpoint(deviceId, role));
        }
        finally
        {
            Marshal.ReleaseComObject(policy);
        }
    }

    private static DataFlow ToDataFlow(DeviceFlow flow) => flow == DeviceFlow.Output ? DataFlow.Render : DataFlow.Capture;
}
