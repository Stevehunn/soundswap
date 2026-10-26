namespace SoundSwap.Core.Updates;

/// <summary>Rules deciding which URLs the updater may download from or open.</summary>
public static class UpdateSecurity
{
    /// <summary>
    /// True for <c>https</c> URLs on <c>github.com</c>, <c>*.github.com</c> or <c>*.githubusercontent.com</c>
    /// (GitHub serves release assets from those hosts). Anything else is refused.
    /// </summary>
    /// <param name="url">The URL to check.</param>
    public static bool IsTrustedUrl(Uri? url)
    {
        if (url is null || !url.IsAbsoluteUri || url.Scheme != Uri.UriSchemeHttps) return false;
        var host = url.Host.ToLowerInvariant();
        return host == "github.com"
            || host.EndsWith(".github.com", StringComparison.Ordinal)
            || host.EndsWith(".githubusercontent.com", StringComparison.Ordinal);
    }
}
