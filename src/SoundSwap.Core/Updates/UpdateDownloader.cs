using System.Security.Cryptography;

namespace SoundSwap.Core.Updates;

/// <summary>
/// Downloads an installer from GitHub safely: trusted hosts only, streamed to a temporary
/// <c>.part</c> file, size and SHA-256 verified, and only then renamed to its final name.
/// </summary>
public sealed class UpdateDownloader
{
    private readonly HttpClient _http;

    /// <summary>Creates a downloader.</summary>
    /// <param name="http">HTTP client to use (not disposed). Give it an infinite timeout and cancel through the token.</param>
    public UpdateDownloader(HttpClient http) => _http = http;

    /// <summary>Default download folder: <c>%TEMP%\SoundSwap\update</c>.</summary>
    public static string DefaultDirectory => Path.Combine(Path.GetTempPath(), "SoundSwap", "update");

    /// <summary>Downloads and verifies <paramref name="asset"/>.</summary>
    /// <param name="asset">The installer to fetch.</param>
    /// <param name="directory">Destination folder (created if needed).</param>
    /// <param name="progress">Receives values from 0 to 1 while downloading (optional).</param>
    /// <param name="cancellationToken">Cancels the download; the partial file is deleted.</param>
    /// <returns>Full path of the verified installer.</returns>
    /// <exception cref="InvalidOperationException">The URL is not a trusted GitHub URL, or the file is not an .exe/.msi.</exception>
    /// <exception cref="InvalidDataException">The downloaded size or SHA-256 does not match what GitHub announced.</exception>
    /// <exception cref="HttpRequestException">Network failure or HTTP error.</exception>
    public async Task<string> DownloadAsync(InstallerAsset asset, string directory,
        IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        if (!UpdateSecurity.IsTrustedUrl(asset.DownloadUrl))
            throw new InvalidOperationException("The download URL is not a trusted GitHub URL.");

        var fileName = Path.GetFileName(asset.Name); // strips any directory part ("../x.exe" -> "x.exe")
        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(fileName) ||
            !(extension.Equals(".exe", StringComparison.OrdinalIgnoreCase) || extension.Equals(".msi", StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Only .exe and .msi installers can be downloaded.");

        Directory.CreateDirectory(directory);
        var finalPath = Path.Combine(directory, fileName);
        var partPath = finalPath + ".part";

        try
        {
            using var response = await _http.GetAsync(asset.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            // GitHub redirects to its CDN: the final host must be trusted as well.
            var finalUrl = response.RequestMessage?.RequestUri;
            if (finalUrl is not null && !UpdateSecurity.IsTrustedUrl(finalUrl))
                throw new InvalidOperationException("The download was redirected to an untrusted host.");

            var total = response.Content.Headers.ContentLength ?? asset.Size;
            await using (var source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false))
            await using (var target = File.Create(partPath))
            {
                var buffer = new byte[81920];
                long done = 0;
                int read;
                while ((read = await source.ReadAsync(buffer.AsMemory(), cancellationToken).ConfigureAwait(false)) > 0)
                {
                    await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                    done += read;
                    if (total > 0) progress?.Report(Math.Min(1.0, (double)done / total));
                }
            }

            if (asset.Size > 0 && new FileInfo(partPath).Length != asset.Size)
                throw new InvalidDataException("The downloaded file has an unexpected size.");

            if (asset.Sha256 is not null)
            {
                await using var verify = File.OpenRead(partPath);
                var hash = Convert.ToHexString(await SHA256.HashDataAsync(verify, cancellationToken).ConfigureAwait(false));
                if (!hash.Equals(asset.Sha256, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("The downloaded file does not match the published SHA-256.");
            }

            File.Move(partPath, finalPath, overwrite: true);
            progress?.Report(1.0);
            return finalPath;
        }
        catch
        {
            try { File.Delete(partPath); } catch (IOException) { /* best effort */ }
            throw;
        }
    }
}
