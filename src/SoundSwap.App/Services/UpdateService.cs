using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SoundSwap.Core.Updates;
using SoundSwap.Desktop.Localization;

namespace SoundSwap.Desktop.Services;

/// <summary>Where the update process currently is.</summary>
public enum UpdateState
{
    /// <summary>Nothing to show.</summary>
    Idle,
    /// <summary>A manual check is running.</summary>
    Checking,
    /// <summary>A manual check found no newer version.</summary>
    UpToDate,
    /// <summary>A manual check failed (offline, GitHub unreachable, rate limit...).</summary>
    CheckFailed,
    /// <summary>A newer release exists: the header button invites the user to download it.</summary>
    Available,
    /// <summary>The installer is being downloaded.</summary>
    Downloading,
    /// <summary>The installer is downloaded and verified: the button now installs it.</summary>
    ReadyToInstall,
    /// <summary>The download failed or was rejected (wrong size/checksum): the button retries.</summary>
    DownloadFailed
}

/// <summary>
/// Orchestrates the update flow shown in the UI: check GitHub, show a button when a newer release exists,
/// download the installer on click, then launch it. The pure logic lives in
/// <see cref="UpdateChecker"/> and <see cref="UpdateDownloader"/> (SoundSwap.Core); this class only adds state and UI text.
/// </summary>
/// <remarks>
/// Typical flow: <see cref="CheckAsync"/> → <see cref="UpdateState.Available"/> → user clicks (<see cref="PrimaryActionCommand"/>)
/// → <see cref="UpdateState.Downloading"/> → <see cref="UpdateState.ReadyToInstall"/> → click again → installer starts and
/// <see cref="InstallerLaunched"/> is raised so the application can exit and let the installer replace its files.
/// </remarks>
public sealed partial class UpdateService : ObservableObject
{
    private readonly UpdateChecker _checker;
    private readonly UpdateDownloader _downloader;
    private readonly Version _current;
    private ReleaseInfo? _release;
    private string? _installerPath;
    private bool _busy;

    /// <summary>Creates the service.</summary>
    /// <param name="http">Shared HTTP client (infinite timeout; cancellation is handled per request).</param>
    /// <param name="currentVersion">Version of the running application.</param>
    /// <param name="options">Repository to watch; defaults to the official one.</param>
    public UpdateService(HttpClient http, Version currentVersion, UpdateOptions? options = null)
    {
        _checker = new UpdateChecker(http, options);
        _downloader = new UpdateDownloader(http);
        _current = currentVersion;

        // Button and status texts are localized: refresh them when the language changes.
        Localizer.Instance.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != Binding.IndexerName) return;
            OnPropertyChanged(nameof(ButtonText));
            OnPropertyChanged(nameof(StatusText));
        };
    }

    /// <summary>Raised right after the installer was started: the application should now exit.</summary>
    public event EventHandler? InstallerLaunched;

    /// <summary>Current step of the update flow.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsButtonVisible), nameof(IsButtonEnabled), nameof(ButtonText), nameof(StatusText))]
    private UpdateState _state;

    /// <summary>Download progress from 0 to 1.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ButtonText))]
    private double _progress;

    /// <summary>The newer release found by the last check, if any.</summary>
    public ReleaseInfo? Release => _release;

    /// <summary>Whether the "new version" button in the header is shown.</summary>
    public bool IsButtonVisible => State is UpdateState.Available or UpdateState.Downloading or UpdateState.ReadyToInstall or UpdateState.DownloadFailed;

    /// <summary>The button is disabled while downloading.</summary>
    public bool IsButtonEnabled => State != UpdateState.Downloading;

    /// <summary>Localized label of the header button (new version, progress, install, retry).</summary>
    public string ButtonText => State switch
    {
        UpdateState.Available => Localizer.Instance.Format("update_available", VersionLabel),
        UpdateState.Downloading => Localizer.Instance.Format("update_downloading", (int)Math.Round(Progress * 100)),
        UpdateState.ReadyToInstall => Localizer.Instance.Format("update_install", VersionLabel),
        UpdateState.DownloadFailed => Localizer.Instance["update_download_failed"],
        _ => ""
    };

    /// <summary>Localized text shown next to "Check for updates" in the footer.</summary>
    public string StatusText => State switch
    {
        UpdateState.Checking => Localizer.Instance["update_checking"],
        UpdateState.UpToDate => Localizer.Instance["update_up_to_date"],
        UpdateState.CheckFailed => Localizer.Instance["update_check_failed"],
        _ => ""
    };

    private string VersionLabel => "v" + _release?.Version.ToString(3);

    /// <summary>Deletes installers left in the temporary folder by a previous update.</summary>
    public static void CleanUpDownloads()
    {
        try { if (Directory.Exists(UpdateDownloader.DefaultDirectory)) Directory.Delete(UpdateDownloader.DefaultDirectory, true); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { /* best effort */ }
    }

    /// <summary>
    /// Asks GitHub whether a newer release exists. Never throws: network problems are reported through
    /// <see cref="UpdateState.CheckFailed"/> for manual checks and ignored silently for automatic ones.
    /// </summary>
    /// <param name="manual"><c>true</c> when the user clicked "Check for updates" (shows progress and the result).</param>
    public async Task CheckAsync(bool manual = false)
    {
        if (_busy || State is UpdateState.Downloading or UpdateState.ReadyToInstall) return;
        _busy = true;
        if (manual) State = UpdateState.Checking;
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            var release = await _checker.CheckForUpdateAsync(_current, timeout.Token);
            if (release is null)
            {
                State = manual ? UpdateState.UpToDate : UpdateState.Idle;
                return;
            }
            _release = release;
            State = UpdateState.Available;
        }
        catch (Exception)
        {
            // Offline, DNS, rate limit, timeout...: an update check must never disturb the user.
            if (manual) State = UpdateState.CheckFailed;
        }
        finally
        {
            _busy = false;
        }
    }

    /// <summary>Manual "Check for updates" from the footer.</summary>
    [RelayCommand]
    private Task CheckNowAsync() => CheckAsync(manual: true);

    /// <summary>Click on the header button: download when a version is available, install when it is downloaded.</summary>
    [RelayCommand]
    private async Task PrimaryActionAsync()
    {
        switch (State)
        {
            case UpdateState.Available or UpdateState.DownloadFailed:
                await DownloadAsync();
                break;
            case UpdateState.ReadyToInstall:
                Install();
                break;
        }
    }

    private async Task DownloadAsync()
    {
        var release = _release;
        if (release is null) return;
        if (release.Installer is not { } asset)
        {
            OpenInBrowser(release.PageUrl); // release without installer: let the user pick the file on GitHub
            return;
        }

        State = UpdateState.Downloading;
        Progress = 0;
        try
        {
            var progress = new Progress<double>(value => Progress = value); // created on the UI thread: callbacks come back to it
            _installerPath = await _downloader.DownloadAsync(asset, UpdateDownloader.DefaultDirectory, progress);
            State = UpdateState.ReadyToInstall;
        }
        catch (Exception)
        {
            State = UpdateState.DownloadFailed;
        }
    }

    private void Install()
    {
        if (_installerPath is null || !File.Exists(_installerPath))
        {
            State = UpdateState.Available; // the file vanished (temp cleanup): download again
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(_installerPath) { UseShellExecute = true });
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            State = UpdateState.DownloadFailed;
            return;
        }
        InstallerLaunched?.Invoke(this, EventArgs.Empty);
    }

    private static void OpenInBrowser(Uri url)
    {
        if (!UpdateSecurity.IsTrustedUrl(url)) return;
        try { Process.Start(new ProcessStartInfo(url.AbsoluteUri) { UseShellExecute = true }); }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException) { /* no browser: nothing to do */ }
    }
}
