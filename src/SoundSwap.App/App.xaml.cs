using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using SoundSwap.Audio;
using SoundSwap.Core.Models;
using SoundSwap.Core.Services;
using SoundSwap.Desktop.Localization;
using SoundSwap.Desktop.Services;
using SoundSwap.Desktop.ViewModels;
using SoundSwap.Desktop.Views;

namespace SoundSwap.Desktop;

/// <summary>
/// Composition root: builds the services, wires their events and owns the process lifetime.
/// The app lives in the notification area; closing the window only hides it unless the user chooses to quit.
/// </summary>
public partial class App : Application
{
    private static readonly TimeSpan FirstUpdateCheckDelay = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan UpdateCheckInterval = TimeSpan.FromHours(12);

    private Mutex? _singleInstance;
    private EventWaitHandle? _showSignal;
    private HotkeyService? _hotkeyService;
    private TrayService? _tray;
    private MainWindow? _mainWindow;
    private DispatcherTimer? _updateTimer;
    private ConfigurationService _config = null!;
    private UpdateService _updates = null!;

    /// <summary>True once the application is really shutting down (the window's close button then stops asking).</summary>
    public bool IsExiting { get; private set; }

    /// <inheritdoc/>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // A tray app often starts hidden at sign-in: record unexpected errors, and keep running (shortcuts matter).
        DispatcherUnhandledException += (_, args) => { CrashLog.Write(args.Exception); args.Handled = true; };
        AppDomain.CurrentDomain.UnhandledException += (_, args) => CrashLog.Write(args.ExceptionObject as Exception);

        // Single instance: a second launch just asks the first one to show its window.
        _singleInstance = new Mutex(true, @"Local\SoundSwap.SingleInstance", out var isFirst);
        _showSignal = new EventWaitHandle(false, EventResetMode.AutoReset, @"Local\SoundSwap.Show");
        if (!isFirst)
        {
            _showSignal.Set();
            Shutdown();
            return;
        }
        new Thread(() => { while (_showSignal.WaitOne()) Dispatcher.BeginInvoke(ShowMainWindow); }) { IsBackground = true }.Start();

        SessionEnding += (_, _) => IsExiting = true; // Windows shutdown/logoff, installer restart: never block it

        var dataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SoundSwap", "data.json");
        var audio = new NAudioDeviceService();
        _config = new ConfigurationService(audio, new JsonStateStore(dataPath));
        var settings = _config.State.Settings;

        // Keep "start with Windows" truthful (path moved/updated, or disabled in the Task Manager).
        if (StartupService.SyncWithSettings(settings)) _config.Commit();

        Localizer.Instance.Load(string.IsNullOrEmpty(settings.Language) ? Localizer.Detect() : settings.Language);
        ThemeService.Initialize();
        ThemeService.Apply(settings.Theme, settings.CustomTheme);
        _config.RefreshActiveFromSystem();

        _hotkeyService = new HotkeyService();
        var hotkeys = new HotkeyCoordinator(_hotkeyService, _config);
        _tray = new TrayService(_config);
        _tray.OpenRequested += ShowMainWindow;
        _tray.QuitRequested += Quit;

        _config.Activated += (_, c) =>
        {
            if (settings.NotifyOnSwitch) _tray.ShowToast(Localizer.Instance.Format("switched", c.Name));
        };
        _config.ActivationFailed += (_, c) => _tray.ShowToast($"{Localizer.Instance["device_missing"]} — {c.Name}");

        UpdateService.CleanUpDownloads();
        _updates = new UpdateService(CreateHttpClient(), AppInfo.Version);
        _updates.InstallerLaunched += (_, _) => Quit(); // the installer needs SoundSwap.exe to be closed

        _mainWindow = new MainWindow(new MainViewModel(_config, hotkeys, audio, _updates));
        hotkeys.Refresh();
        ScheduleUpdateChecks(settings);

        if (!e.Args.Contains(StartupService.MinimizedArgument)) ShowMainWindow();
    }

    /// <summary>Brings the main window to the front (also called by the tray icon and by a second launch).</summary>
    public void ShowMainWindow()
    {
        if (_mainWindow is null) return;
        _config.RefreshActiveFromSystem();
        if (_mainWindow.WindowState == WindowState.Minimized) _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Show();
        _mainWindow.Activate();
    }

    /// <summary>Called when the user clicks the window's X: asks whether to quit, hide in the tray, or cancel.</summary>
    public void RequestClose()
    {
        var dialog = new CloseDialog { Owner = _mainWindow };
        dialog.ShowDialog();
        switch (dialog.Choice)
        {
            case CloseChoice.Minimize: _mainWindow?.Hide(); break;
            case CloseChoice.Quit: Quit(); break;
        }
    }

    /// <summary>Stops the application completely (shortcuts stop working).</summary>
    public void Quit()
    {
        IsExiting = true;
        Shutdown();
    }

    /// <inheritdoc/>
    protected override void OnExit(ExitEventArgs e)
    {
        _updateTimer?.Stop();
        _hotkeyService?.Dispose();
        _tray?.Dispose();
        base.OnExit(e);
    }

    // First check a few seconds after launch (so signing in to Windows is never slowed down), then twice a day.
    private void ScheduleUpdateChecks(AppSettings settings)
    {
        async void Check()
        {
            if (settings.CheckForUpdates) await _updates.CheckAsync();
        }

        var first = new DispatcherTimer { Interval = FirstUpdateCheckDelay };
        first.Tick += (_, _) => { first.Stop(); Check(); };
        first.Start();

        _updateTimer = new DispatcherTimer { Interval = UpdateCheckInterval };
        _updateTimer.Tick += (_, _) => Check();
        _updateTimer.Start();
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient { Timeout = Timeout.InfiniteTimeSpan }; // downloads can be long: cancellation is per request
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("SoundSwap", AppInfo.Version.ToString(3)));
        return client;
    }
}
