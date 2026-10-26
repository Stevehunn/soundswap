using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SoundSwap.Core.Models;
using SoundSwap.Core.Services;
using SoundSwap.Desktop.Localization;
using SoundSwap.Desktop.Services;

namespace SoundSwap.Desktop.ViewModels;

/// <summary>Entry of the theme dropdown (label re-translated when the language changes).</summary>
public sealed class ThemeOption : ObservableObject
{
    private readonly string _key;
    /// <summary>Creates an option.</summary>
    /// /// <param name="mode">The theme it selects.</param>
    /// /// <param name="key">Resource key of its label.</param>
    public ThemeOption(ThemeMode mode, string key) { Mode = mode; _key = key; }
    /// <summary>The theme it selects.</summary>
    public ThemeMode Mode { get; }
    /// <summary>Localized label.</summary>
    public string Label => Localizer.Instance[_key];
    /// <summary>Re-reads the label after a language change.</summary>
    public void Refresh() => OnPropertyChanged(nameof(Label));
}

/// <summary>State and commands of the main window: configuration list, settings, theme, language and updates.</summary>
public sealed partial class MainViewModel : ObservableObject
{
    private readonly ConfigurationService _config;
    private readonly HotkeyCoordinator _hotkeys;
    private readonly IAudioDeviceService _audio;

    /// <summary>Creates the view model of the main window.</summary>
    /// <param name="config">Configurations, settings and device switching.</param>
    /// <param name="hotkeys">Global shortcut registration (to show conflicts).</param>
    /// <param name="audio">Windows audio devices (for the editor dialog).</param>
    /// <param name="update">Update flow shown in the header and the footer.</param>
    public MainViewModel(ConfigurationService config, HotkeyCoordinator hotkeys, IAudioDeviceService audio, UpdateService update)
    {
        Update = update;
        _config = config;
        _hotkeys = hotkeys;
        _audio = audio;

        var settings = config.State.Settings;
        ThemeOptions = new[]
        {
            new ThemeOption(ThemeMode.System, "theme_system"),
            new ThemeOption(ThemeMode.Dark, "theme_dark"),
            new ThemeOption(ThemeMode.Light, "theme_light"),
            new ThemeOption(ThemeMode.Custom, "theme_custom"),
        };
        _selectedLanguage = Localizer.Languages.First(l => l.Code == Localizer.Instance.Code);
        _selectedTheme = ThemeOptions.First(t => t.Mode == settings.Theme);
        _nextHotkey = settings.NextHotkey;
        _startWithWindows = settings.StartWithWindows;
        _notifyOnSwitch = settings.NotifyOnSwitch;
        _checkForUpdates = settings.CheckForUpdates;

        hotkeys.Refreshed += Reload;
        config.ActiveChanged += (_, _) => Reload();
        Localizer.Instance.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != Binding.IndexerName) return;
            foreach (var option in ThemeOptions) option.Refresh();
            Reload();
        };
        Reload();
    }

    /// <summary>The configuration cards, in order.</summary>
    public ObservableCollection<ConfigItemViewModel> Items { get; } = new();
    /// <summary>Languages for the dropdown.</summary>
    public System.Collections.Generic.IReadOnlyList<LanguageInfo> Languages => Localizer.Languages;
    /// <summary>Themes for the dropdown.</summary>
    public ThemeOption[] ThemeOptions { get; }
    /// <summary>True when there is no configuration (shows the hint text).</summary>
    public bool IsEmpty => Items.Count == 0;
    /// <summary>True when the custom theme is selected (shows the Customize button).</summary>
    public bool IsCustomTheme => SelectedTheme.Mode == ThemeMode.Custom;
    /// <summary>Version shown in the footer, e.g. <c>v1.2.0</c> (lets users check which build is running).</summary>
    public string VersionText => AppInfo.VersionText;

    /// <summary>Update flow: header button, footer status and "check now" command.</summary>
    public UpdateService Update { get; }

    /// <summary>Set by the view: shows the custom-theme editor (modal).</summary>
    public Action<ThemeEditorViewModel>? ThemeEditorDialog { get; set; }

    /// <summary>Set by the view: shows the editor dialog and returns true if validated.</summary>
    public Func<ConfigEditorViewModel, bool>? EditorDialog { get; set; }

    /// <summary>Selected interface language.</summary>
    [ObservableProperty] private LanguageInfo _selectedLanguage;
    /// <summary>Selected theme.</summary>
    [ObservableProperty, NotifyPropertyChangedFor(nameof(IsCustomTheme))] private ThemeOption _selectedTheme;
    /// <summary>Shortcut for the next configuration.</summary>
    [ObservableProperty] private string? _nextHotkey;
    /// <summary>True when that shortcut is taken by another program.</summary>
    [ObservableProperty] private bool _nextHotkeyError;
    /// <summary>Start with Windows.</summary>
    [ObservableProperty] private bool _startWithWindows;
    /// <summary>Show a notification after each switch.</summary>
    [ObservableProperty] private bool _notifyOnSwitch;
    /// <summary>Check GitHub for new releases automatically.</summary>
    [ObservableProperty] private bool _checkForUpdates;
    private bool _revertingStartup;

    partial void OnSelectedLanguageChanged(LanguageInfo value)
    {
        Localizer.Instance.Load(value.Code);
        _config.State.Settings.Language = value.Code;
        _config.Commit();
    }

    partial void OnSelectedThemeChanged(ThemeOption value)
    {
        ThemeService.Apply(value.Mode, _config.State.Settings.CustomTheme);
        _config.State.Settings.Theme = value.Mode;
        _config.Commit();
        // First choice of "Custom": open the color editor right away (deferred so the dropdown closes first).
        if (value.Mode == ThemeMode.Custom)
            System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(OpenThemeEditor));
    }

    partial void OnNextHotkeyChanged(string? value)
    {
        _config.State.Settings.NextHotkey = value;
        _config.Commit();
    }

    partial void OnStartWithWindowsChanged(bool value)
    {
        if (_revertingStartup) return;
        if (!StartupService.TrySet(value))
        {
            // Windows refused (policy, antivirus...) or this is not SoundSwap.exe: show the real state.
            _revertingStartup = true;
            StartWithWindows = !value;
            _revertingStartup = false;
            return;
        }
        _config.State.Settings.StartWithWindows = value;
        _config.Commit();
    }

    partial void OnCheckForUpdatesChanged(bool value)
    {
        _config.State.Settings.CheckForUpdates = value;
        _config.Commit();
        if (value) _ = Update.CheckAsync(); // turned on: check right away
    }

    partial void OnNotifyOnSwitchChanged(bool value)
    {
        _config.State.Settings.NotifyOnSwitch = value;
        _config.Commit();
    }

    [RelayCommand] private void CustomizeTheme() => OpenThemeEditor();
    [RelayCommand] private void NewConfig() => OpenEditor(null);
    [RelayCommand] private void Edit(ConfigItemViewModel item) => OpenEditor(item.Config);
    [RelayCommand] private void Delete(ConfigItemViewModel item) => _config.Remove(item.Config.Id);
    [RelayCommand] private void Activate(ConfigItemViewModel item) => _config.Activate(item.Config.Id);

    /// <summary>Moves a configuration to its final position (called when a card is dropped).</summary>
    public void Move(ConfigItemViewModel item, int newIndex) => _config.Move(item.Config.Id, newIndex);

    private void OpenThemeEditor()
    {
        ThemeEditorDialog?.Invoke(new ThemeEditorViewModel(_config.State.Settings.CustomTheme));
        _config.Commit(); // the editor applies colors live; persist them once it is closed
    }

    private void OpenEditor(AudioConfiguration? existing)
    {
        var editor = new ConfigEditorViewModel(_audio.GetDevices(DeviceFlow.Output), _audio.GetDevices(DeviceFlow.Input), _config, existing);
        if (EditorDialog?.Invoke(editor) != true) return;
        if (existing is null) _config.Add(editor.Result);
        else _config.Update(editor.Result);
    }

    /// <summary>Synchronizes the list with the configurations, keeping existing rows (no rebuild).</summary>
    private void Reload()
    {
        var configs = _config.Configurations;

        for (var i = Items.Count - 1; i >= 0; i--)
            if (!configs.Any(c => c.Id == Items[i].Config.Id)) Items.RemoveAt(i);

        for (var i = 0; i < configs.Count; i++)
        {
            var configuration = configs[i];
            var active = configuration.Id == _config.ActiveId;
            var failed = _hotkeys.Failed.Contains(configuration.Id.ToString());
            var row = Items.FirstOrDefault(r => r.Config.Id == configuration.Id);
            if (row is null)
            {
                Items.Insert(i, new ConfigItemViewModel(configuration, active, failed));
                continue;
            }
            row.Update(configuration, active, failed);
            var current = Items.IndexOf(row);
            if (current != i) Items.Move(current, i);
        }

        NextHotkeyError = _hotkeys.Failed.Contains(HotkeyCoordinator.NextId);
        OnPropertyChanged(nameof(IsEmpty));
    }
}
