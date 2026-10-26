using System;
using System.Reflection;

namespace SoundSwap.Desktop;

/// <summary>Static facts about the running application (version, project links).</summary>
public static class AppInfo
{
    /// <summary>GitHub repository of the project (source code, issues, releases).</summary>
    public const string RepositoryUrl = "https://github.com/Stevehunn/soundswap";

    /// <summary>Presentation page on the SoundDeck website.</summary>
    public const string WebsiteUrl = "https://sounddeck.stream/soundswap";

    /// <summary>
    /// Version of the running executable (set at build time with <c>-p:Version=x.y.z</c>;
    /// the release workflow takes it from the git tag).
    /// </summary>
    public static Version Version { get; } = Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(0, 0, 0, 0);

    /// <summary>Version formatted for display, e.g. <c>v1.2.0</c>.</summary>
    public static string VersionText => "v" + Version.ToString(3);
}
