using SoundSwap.Core.Models;
using SoundSwap.Core.Services;
using Xunit;

namespace SoundSwap.Core.Tests;

public class ConfigurationServiceTests
{
    private static (ConfigurationService service, FakeAudio audio) Create(params string[] names)
    {
        var audio = new FakeAudio();
        var service = new ConfigurationService(audio, new MemoryStore());
        foreach (var name in names)
        {
            audio.Devices.Add(new AudioDevice("out-" + name, "Out " + name, DeviceFlow.Output));
            audio.Devices.Add(new AudioDevice("in-" + name, "In " + name, DeviceFlow.Input));
            service.Add(new AudioConfiguration { Name = name, OutputDeviceId = "out-" + name, InputDeviceId = "in-" + name });
        }
        return (service, audio);
    }

    [Fact]
    public void ActivateNext_CyclesThroughConfigurations()
    {
        var (service, audio) = Create("a", "b", "c");
        service.ActivateNext();
        Assert.Equal("out-a", audio.Defaults[DeviceFlow.Output]);
        service.ActivateNext();
        service.ActivateNext();
        Assert.Equal("in-c", audio.Defaults[DeviceFlow.Input]);
        service.ActivateNext();
        Assert.Equal("out-a", audio.Defaults[DeviceFlow.Output]);
    }

    [Fact]
    public void ActivateNext_SkipsConfigurationsWithMissingDevices()
    {
        var (service, audio) = Create("a", "b", "c");
        service.ActivateNext();
        audio.Devices.RemoveAll(d => d.Id == "out-b");
        Assert.Equal(SwitchStatus.Success, service.ActivateNext());
        Assert.Equal("out-c", audio.Defaults[DeviceFlow.Output]);
    }

    [Fact]
    public void Activate_ReportsMissingDevice_AndRaisesFailure()
    {
        var (service, audio) = Create("a");
        audio.Devices.Clear();
        var failed = false;
        service.ActivationFailed += (_, _) => failed = true;

        Assert.Equal(SwitchStatus.DeviceMissing, service.Activate(service.Configurations[0].Id));
        Assert.True(failed);
    }

    [Fact]
    public void RefreshActiveFromSystem_DetectsMatchingConfiguration()
    {
        var (service, audio) = Create("a", "b");
        audio.Defaults[DeviceFlow.Output] = "out-b";
        audio.Defaults[DeviceFlow.Input] = "in-b";
        service.RefreshActiveFromSystem();
        Assert.Equal(service.Configurations[1].Id, service.ActiveId);
    }

    [Fact]
    public void IsHotkeyUsed_ComparesNormalizedGestures()
    {
        var (service, _) = Create("a");
        service.Configurations[0].Hotkey = "Ctrl+Alt+D1";
        Assert.True(service.IsHotkeyUsed("alt+ctrl+d1", null));
        Assert.False(service.IsHotkeyUsed("alt+ctrl+d1", service.Configurations[0].Id));
        Assert.True(service.IsHotkeyUsed("Ctrl+Alt+Right", null));
    }

    [Fact]
    public void JsonStateStore_RoundTrips()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        try
        {
            var store = new JsonStateStore(path);
            var state = new AppState();
            state.Settings.Theme = ThemeMode.Light;
            state.Configurations.Add(new AudioConfiguration { Name = "x", Hotkey = "Ctrl+Alt+D2" });
            store.Save(state);

            var loaded = new JsonStateStore(path).Load();
            Assert.Equal(ThemeMode.Light, loaded.Settings.Theme);
            Assert.Equal("x", loaded.Configurations.Single().Name);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void JsonStateStore_RecoversFromCorruptFile()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        File.WriteAllText(path, "{ not json");
        var loaded = new JsonStateStore(path).Load();
        Assert.Empty(loaded.Configurations);
        Assert.True(File.Exists(path + ".bak"));
        File.Delete(path + ".bak");
    }

    [Fact]
    public void Move_ReordersConfigurations_AndRaisesChanged()
    {
        var (service, _) = Create("a", "b", "c");
        var changed = 0;
        service.Changed += (_, _) => changed++;

        service.Move(service.Configurations[2].Id, 0);

        Assert.Equal(new[] { "c", "a", "b" }, service.Configurations.Select(c => c.Name));
        Assert.Equal(1, changed);
    }

    [Fact]
    public void Move_ChangesTheCyclingOrder()
    {
        var (service, audio) = Create("a", "b", "c");
        service.Move(service.Configurations[2].Id, 0); // c, a, b

        service.ActivateNext();
        Assert.Equal("out-c", audio.Defaults[DeviceFlow.Output]);
        service.ActivateNext();
        Assert.Equal("out-a", audio.Defaults[DeviceFlow.Output]);
    }

    [Fact]
    public void Move_IgnoresUnknownIdAndNoOp()
    {
        var (service, _) = Create("a", "b");
        var changed = 0;
        service.Changed += (_, _) => changed++;

        service.Move(Guid.NewGuid(), 0);
        service.Move(service.Configurations[0].Id, 0);

        Assert.Equal(0, changed);
    }

    [Fact]
    public void Move_ClampsIndexToTheEnd()
    {
        var (service, _) = Create("a", "b", "c");
        service.Move(service.Configurations[0].Id, 99);
        Assert.Equal(new[] { "b", "c", "a" }, service.Configurations.Select(c => c.Name));
    }
}
