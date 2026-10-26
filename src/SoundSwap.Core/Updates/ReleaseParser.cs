using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace SoundSwap.Core.Updates;

/// <summary>Turns the JSON returned by the GitHub "latest release" API into a <see cref="ReleaseInfo"/>.</summary>
public static partial class ReleaseParser
{
    [GeneratedRegex(@"^\s*v?(\d+)\.(\d+)(?:\.(\d+))?", RegexOptions.CultureInvariant)]
    private static partial Regex VersionRegex();

    /// <summary>
    /// Parses a release tag such as <c>v1.2.3</c>, <c>1.2</c> or <c>v2.0.0-beta.1</c>
    /// (a pre-release suffix is ignored; a missing patch number means 0).
    /// </summary>
    /// <param name="tag">The git tag.</param>
    /// <param name="version">The parsed version when the method returns <c>true</c>.</param>
    /// <returns><c>true</c> if the tag starts with a <c>major.minor[.patch]</c> number.</returns>
    public static bool TryParseVersion(string? tag, out Version version)
    {
        version = new Version(0, 0, 0);
        if (tag is null) return false;
        var match = VersionRegex().Match(tag);
        if (!match.Success) return false;

        var patch = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : 0;
        version = new Version(int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value), patch);
        return true;
    }

    /// <summary>
    /// Parses the GitHub API response. Returns <c>null</c> for invalid JSON, drafts, pre-releases,
    /// tags that are not versions, or a release page outside GitHub.
    /// </summary>
    /// <param name="json">Body of <c>GET /repos/{owner}/{repo}/releases/latest</c>.</param>
    public static ReleaseInfo? Parse(string json)
    {
        ReleaseDto? dto;
        try { dto = JsonSerializer.Deserialize<ReleaseDto>(json); }
        catch (JsonException) { return null; }

        if (dto is null || dto.Draft || dto.Prerelease) return null;
        if (!TryParseVersion(dto.TagName, out var version)) return null;
        if (!Uri.TryCreate(dto.HtmlUrl, UriKind.Absolute, out var page) || !UpdateSecurity.IsTrustedUrl(page)) return null;

        return new ReleaseInfo(version, dto.TagName!, dto.Name ?? dto.TagName!, dto.Body ?? "", page, PickInstaller(dto.Assets));
    }

    /// <summary>Chooses the installer among the assets: a "setup" .exe first, then an .msi. Portable zips are ignored.</summary>
    internal static InstallerAsset? PickInstaller(IEnumerable<AssetDto>? assets)
    {
        if (assets is null) return null;
        var usable = assets
            .Where(a => !string.IsNullOrWhiteSpace(a.Name)
                        && Uri.TryCreate(a.Url, UriKind.Absolute, out var u) && UpdateSecurity.IsTrustedUrl(u))
            .ToList();

        var chosen = usable.FirstOrDefault(a => a.Name!.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                                                && a.Name.Contains("setup", StringComparison.OrdinalIgnoreCase))
                     ?? usable.FirstOrDefault(a => a.Name!.EndsWith(".msi", StringComparison.OrdinalIgnoreCase));

        return chosen is null ? null : new InstallerAsset(chosen.Name!, new Uri(chosen.Url!), chosen.Size, ParseDigest(chosen.Digest));
    }

    /// <summary>GitHub publishes asset digests as <c>sha256:&lt;64 hex&gt;</c>.</summary>
    private static string? ParseDigest(string? digest) =>
        digest is not null && digest.Length == 71 && digest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase)
            ? digest[7..].ToLowerInvariant()
            : null;

    internal sealed class ReleaseDto
    {
        [JsonPropertyName("tag_name")] public string? TagName { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("body")] public string? Body { get; set; }
        [JsonPropertyName("html_url")] public string? HtmlUrl { get; set; }
        [JsonPropertyName("draft")] public bool Draft { get; set; }
        [JsonPropertyName("prerelease")] public bool Prerelease { get; set; }
        [JsonPropertyName("assets")] public List<AssetDto>? Assets { get; set; }
    }

    internal sealed class AssetDto
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("size")] public long Size { get; set; }
        [JsonPropertyName("browser_download_url")] public string? Url { get; set; }
        [JsonPropertyName("digest")] public string? Digest { get; set; }
    }
}
