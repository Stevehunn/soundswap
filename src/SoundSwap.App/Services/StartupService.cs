using System;
using System.IO;
using System.Security;
using Microsoft.Win32;
using SoundSwap.Core.Models;

namespace SoundSwap.Desktop.Services;

/// <summary>
/// Starts SoundSwap with Windows. Uses the per-user registry key
/// <c>HKCU\Software\Microsoft\Windows\CurrentVersion\Run</c>: no administrator rights needed.
/// </summary>
/// <remarks>
/// <para>The stored command is <c>"C:\...\SoundSwap.exe" --minimized</c>, so the app boots straight into the
/// notification area. Windows also keeps a per-user "approved" flag (the Startup tab of the Task Manager);
/// when the user disables SoundSwap there, <see cref="IsDisabledByUser"/> reports it and SoundSwap respects it.</para>
/// <para>At every launch <see cref="SyncWithSettings"/> repairs the stored path, so moving or updating the
/// application never leaves a dead startup entry.</para>
/// </remarks>
public static class StartupService
{
    /// <summary>Command-line switch that starts the app hidden in the notification area.</summary>
    public const string MinimizedArgument = "--minimized";

    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ApprovedKey = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";
    private const string ValueName = "SoundSwap";
    private const string ExecutableName = "SoundSwap.exe";

    /// <summary>
    /// Builds the registry command for an executable, or <c>null</c> if the path is not SoundSwap's own
    /// executable (for instance when started with <c>dotnet SoundSwap.dll</c>, where the process is <c>dotnet.exe</c>).
    /// </summary>
    /// <param name="exePath">Path of the running executable.</param>
    public static string? BuildCommand(string? exePath) =>
        !string.IsNullOrEmpty(exePath) && string.Equals(Path.GetFileName(exePath), ExecutableName, StringComparison.OrdinalIgnoreCase)
            ? $"\"{exePath}\" {MinimizedArgument}"
            : null;

    /// <summary>Enables or disables the startup entry, returning <c>false</c> instead of throwing if Windows refuses.</summary>
    /// <param name="enabled"><c>true</c> to start with Windows.</param>
    public static bool TrySet(bool enabled)
    {
        try
        {
            Set(enabled);
            return true;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or SecurityException or IOException or InvalidOperationException)
        {
            return false;
        }
    }

    /// <summary>True if the user turned SoundSwap off in the Windows Task Manager (Startup tab).</summary>
    public static bool IsDisabledByUser()
    {
        using var key = Registry.CurrentUser.OpenSubKey(ApprovedKey);
        // The first byte is even (02/06) when enabled and odd (03/07) when disabled.
        return key?.GetValue(ValueName) is byte[] { Length: > 0 } data && (data[0] & 1) == 1;
    }

    /// <summary>
    /// Called at every launch. If the user enabled "start with Windows": drops the setting when they disabled the
    /// entry in the Task Manager, otherwise rewrites the stored path so it always points to the current executable.
    /// </summary>
    /// <param name="settings">The application settings (<see cref="AppSettings.StartWithWindows"/> may be changed).</param>
    /// <returns><c>true</c> if a setting changed and must be saved.</returns>
    public static bool SyncWithSettings(AppSettings settings)
    {
        if (!settings.StartWithWindows) return false;
        if (IsDisabledByUser())
        {
            settings.StartWithWindows = false;
            return true;
        }

        try
        {
            var command = BuildCommand(Environment.ProcessPath);
            if (command is null) return false;
            using var run = Registry.CurrentUser.CreateSubKey(RunKey, writable: true);
            if (!string.Equals(run.GetValue(ValueName) as string, command, StringComparison.OrdinalIgnoreCase))
                run.SetValue(ValueName, command);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or SecurityException or IOException)
        {
            // Best effort: a startup entry that cannot be repaired must never prevent the app from running.
        }
        return false;
    }

    private static void Set(bool enabled)
    {
        using var run = Registry.CurrentUser.CreateSubKey(RunKey, writable: true);
        if (!enabled)
        {
            run.DeleteValue(ValueName, throwOnMissingValue: false);
            return;
        }

        var command = BuildCommand(Environment.ProcessPath)
                      ?? throw new InvalidOperationException("Start with Windows needs the SoundSwap.exe executable.");
        run.SetValue(ValueName, command);

        // Clear a "disabled in Task Manager" flag, otherwise Windows would still ignore the entry.
        using var approved = Registry.CurrentUser.OpenSubKey(ApprovedKey, writable: true);
        approved?.DeleteValue(ValueName, throwOnMissingValue: false);
    }
}
