namespace SoundSwap.Core.Updates;

/// <summary>
/// Identifies the GitHub repository whose releases the updater watches.
/// Forks can point the updater at their own repository by passing different values.
/// </summary>
/// <param name="Owner">GitHub user or organization (default: <c>Stevehunn</c>).</param>
/// <param name="Repository">Repository name (default: <c>soundswap</c>).</param>
public sealed record UpdateOptions(string Owner = "Stevehunn", string Repository = "soundswap")
{
    /// <summary>GitHub REST endpoint returning the latest published (non-draft, non-prerelease) release.</summary>
    public Uri LatestReleaseApiUrl => new($"https://api.github.com/repos/{Owner}/{Repository}/releases/latest");

    /// <summary>Human-readable page listing all releases.</summary>
    public Uri ReleasesPageUrl => new($"https://github.com/{Owner}/{Repository}/releases");
}
