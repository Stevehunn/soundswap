using System;
using System.IO;

namespace SoundSwap.Desktop.Services;

/// <summary>
/// Minimal error log: SoundSwap often starts hidden at sign-in, so an unexpected error would otherwise leave no trace.
/// Errors are appended to <c>%APPDATA%\SoundSwap\error.log</c> (the file is reset when it exceeds 256 KB).
/// </summary>
public static class CrashLog
{
    private const long MaxBytes = 256 * 1024;

    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SoundSwap", "error.log");

    /// <summary>Appends an exception with the date and the app version. Never throws.</summary>
    /// <param name="exception">The error to record (ignored if <c>null</c>).</param>
    public static void Write(Exception? exception)
    {
        if (exception is null) return;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
            var file = new FileInfo(LogPath);
            if (file.Exists && file.Length > MaxBytes) file.Delete();
            File.AppendAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {AppInfo.VersionText}{Environment.NewLine}{exception}{Environment.NewLine}{Environment.NewLine}");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Logging must never be the cause of a second failure.
        }
    }
}
