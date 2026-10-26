using System.Net;
using System.Net.Http.Headers;

namespace SoundSwap.Core.Updates;

/// <summary>Asks GitHub for the latest release and tells whether it is newer than the running version.</summary>
public sealed class UpdateChecker
{
    private readonly HttpClient _http;
    private readonly UpdateOptions _options;

    /// <summary>Creates a checker.</summary>
    /// <param name="http">HTTP client to use (it is not disposed by the checker).</param>
    /// <param name="options">Repository to watch; defaults to the official SoundSwap repository.</param>
    public UpdateChecker(HttpClient http, UpdateOptions? options = null)
    {
        _http = http;
        _options = options ?? new UpdateOptions();
    }

    /// <summary>Fetches the latest published release.</summary>
    /// <returns>The release, or <c>null</c> when none exists yet (HTTP 404) or the response is not a usable release.</returns>
    /// <exception cref="HttpRequestException">Network failure or an HTTP error other than 404 (e.g. rate limit).</exception>
    public async Task<ReleaseInfo?> GetLatestReleaseAsync(CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, _options.LatestReleaseApiUrl);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("SoundSwap-Updater", "1.0")); // required by the GitHub API
        request.Headers.TryAddWithoutValidation("X-GitHub-Api-Version", "2022-11-28");

        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return ReleaseParser.Parse(json);
    }

    /// <summary>Returns the latest release only if it is newer than <paramref name="current"/>.</summary>
    /// <param name="current">The running version.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The newer release, or <c>null</c> if SoundSwap is up to date.</returns>
    /// <exception cref="HttpRequestException">See <see cref="GetLatestReleaseAsync"/>.</exception>
    public async Task<ReleaseInfo?> CheckForUpdateAsync(Version current, CancellationToken cancellationToken = default)
    {
        var latest = await GetLatestReleaseAsync(cancellationToken).ConfigureAwait(false);
        return latest is not null && latest.IsNewerThan(current) ? latest : null;
    }
}
