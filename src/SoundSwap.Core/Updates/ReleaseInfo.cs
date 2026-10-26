namespace SoundSwap.Core.Updates;

/// <summary>A downloadable installer attached to a GitHub release.</summary>
/// <param name="Name">File name, e.g. <c>SoundSwap-Setup-1.3.0.exe</c>.</param>
/// <param name="DownloadUrl">HTTPS download URL (always on a GitHub host, see <see cref="UpdateSecurity"/>).</param>
/// <param name="Size">Expected size in bytes (0 if unknown).</param>
/// <param name="Sha256">Lowercase hex SHA-256 published by GitHub for the asset, or <c>null</c> if not provided.</param>
public sealed record InstallerAsset(string Name, Uri DownloadUrl, long Size, string? Sha256);

/// <summary>A published release of SoundSwap.</summary>
/// <param name="Version">Version parsed from the tag (<c>v1.3.0</c> becomes 1.3.0).</param>
/// <param name="Tag">Original git tag.</param>
/// <param name="Title">Release title.</param>
/// <param name="Notes">Release notes in Markdown (may be empty).</param>
/// <param name="PageUrl">Web page of the release on GitHub.</param>
/// <param name="Installer">The installer to download, or <c>null</c> when the release has none (the user is then sent to <paramref name="PageUrl"/>).</param>
public sealed record ReleaseInfo(Version Version, string Tag, string Title, string Notes, Uri PageUrl, InstallerAsset? Installer)
{
    /// <summary>Whether this release is strictly newer than <paramref name="current"/> (compared on major.minor.build).</summary>
    /// <param name="current">The running version, typically the assembly version (e.g. 1.2.0.0).</param>
    public bool IsNewerThan(Version current) => Normalize(Version) > Normalize(current);

    private static Version Normalize(Version v) => new(Math.Max(v.Major, 0), Math.Max(v.Minor, 0), Math.Max(v.Build, 0));
}
