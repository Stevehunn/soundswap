using System.Net;
using System.Security.Cryptography;
using SoundSwap.Core.Updates;
using Xunit;

namespace SoundSwap.Core.Tests;

public class UpdateTests
{
    // sha256("hello")
    private const string HelloSha = "2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824";

    private const string ReleaseJson = """
    {
      "tag_name": "v1.3.0", "name": "SoundSwap 1.3.0", "body": "Notes", "draft": false, "prerelease": false,
      "html_url": "https://github.com/Stevehunn/soundswap/releases/tag/v1.3.0",
      "assets": [
        { "name": "SoundSwap-1.3.0-win-x64-portable.zip", "size": 10,
          "browser_download_url": "https://github.com/Stevehunn/soundswap/releases/download/v1.3.0/SoundSwap-1.3.0-win-x64-portable.zip" },
        { "name": "SoundSwap-Setup-1.3.0.exe", "size": 5,
          "browser_download_url": "https://github.com/Stevehunn/soundswap/releases/download/v1.3.0/SoundSwap-Setup-1.3.0.exe",
          "digest": "sha256:2CF24DBA5FB0A30E26E83B2AC5B9E29E1B161E5C1FA7425E73043362938B9824" }
      ]
    }
    """;

    // ---------- versions ----------
    [Theory]
    [InlineData("v1.2.3", true, "1.2.3")]
    [InlineData("1.2", true, "1.2.0")]
    [InlineData("v2.0.0-beta.1", true, "2.0.0")]
    [InlineData("release-5", false, "0.0.0")]
    [InlineData("", false, "0.0.0")]
    [InlineData(null, false, "0.0.0")]
    public void TryParseVersion_HandlesTags(string? tag, bool ok, string expected)
    {
        Assert.Equal(ok, ReleaseParser.TryParseVersion(tag, out var version));
        Assert.Equal(expected, version.ToString());
    }

    [Fact]
    public void IsNewerThan_ComparesOnThreeComponents()
    {
        var release = ReleaseParser.Parse(ReleaseJson)!;
        Assert.True(release.IsNewerThan(new Version(1, 2, 0, 0)));
        Assert.False(release.IsNewerThan(new Version(1, 3, 0, 0)));
        Assert.False(release.IsNewerThan(new Version(2, 0, 0, 0)));
    }

    // ---------- parsing ----------
    [Fact]
    public void Parse_PicksTheSetupInstaller_AndNormalizesTheDigest()
    {
        var release = ReleaseParser.Parse(ReleaseJson)!;
        Assert.Equal("1.3.0", release.Version.ToString());
        Assert.Equal("SoundSwap-Setup-1.3.0.exe", release.Installer!.Name);
        Assert.Equal(HelloSha, release.Installer.Sha256);
        Assert.Equal(5, release.Installer.Size);
    }

    [Fact]
    public void Parse_RejectsDraftsPrereleasesAndGarbage()
    {
        Assert.Null(ReleaseParser.Parse(ReleaseJson.Replace("\"draft\": false", "\"draft\": true")));
        Assert.Null(ReleaseParser.Parse(ReleaseJson.Replace("\"prerelease\": false", "\"prerelease\": true")));
        Assert.Null(ReleaseParser.Parse("{ not json"));
        Assert.Null(ReleaseParser.Parse(ReleaseJson.Replace("v1.3.0\", \"name", "latest\", \"name")));
    }

    [Fact]
    public void Parse_IgnoresAssetsAndPagesOutsideGitHub()
    {
        var evilAsset = ReleaseJson.Replace("https://github.com/Stevehunn/soundswap/releases/download/v1.3.0/SoundSwap-Setup-1.3.0.exe",
                                            "https://evil.example.com/SoundSwap-Setup-1.3.0.exe");
        Assert.Null(ReleaseParser.Parse(evilAsset)!.Installer);

        var evilPage = ReleaseJson.Replace("https://github.com/Stevehunn/soundswap/releases/tag/v1.3.0", "https://evil.example.com/");
        Assert.Null(ReleaseParser.Parse(evilPage));
    }

    [Theory]
    [InlineData("https://github.com/a/b", true)]
    [InlineData("https://objects.githubusercontent.com/x", true)]
    [InlineData("https://release-assets.githubusercontent.com/x", true)]
    [InlineData("http://github.com/a/b", false)]
    [InlineData("https://evilgithub.com/a", false)]
    [InlineData("https://github.com.evil.io/a", false)]
    public void IsTrustedUrl_OnlyAllowsHttpsGitHubHosts(string url, bool expected) =>
        Assert.Equal(expected, UpdateSecurity.IsTrustedUrl(new Uri(url)));

    // ---------- checker ----------
    [Fact]
    public async Task Checker_ReturnsNewerRelease_AndSendsRequiredHeaders()
    {
        var http = new FakeHttp(_ => Json(ReleaseJson));
        var checker = new UpdateChecker(new HttpClient(http));

        var release = await checker.CheckForUpdateAsync(new Version(1, 2, 0, 0));

        Assert.Equal("1.3.0", release!.Version.ToString());
        var request = http.Requests.Single();
        Assert.Equal("https://api.github.com/repos/Stevehunn/soundswap/releases/latest", request.RequestUri!.ToString());
        Assert.True(request.Headers.UserAgent.Count > 0);
    }

    [Fact]
    public async Task Checker_ReturnsNull_WhenUpToDate_OrNoReleaseYet()
    {
        var upToDate = new UpdateChecker(new HttpClient(new FakeHttp(_ => Json(ReleaseJson))));
        Assert.Null(await upToDate.CheckForUpdateAsync(new Version(1, 3, 0, 0)));

        var none = new UpdateChecker(new HttpClient(new FakeHttp(_ => new HttpResponseMessage(HttpStatusCode.NotFound))));
        Assert.Null(await none.CheckForUpdateAsync(new Version(1, 0, 0, 0)));
    }

    [Fact]
    public async Task Checker_Throws_OnServerError()
    {
        var checker = new UpdateChecker(new HttpClient(new FakeHttp(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError))));
        await Assert.ThrowsAsync<HttpRequestException>(() => checker.GetLatestReleaseAsync());
    }

    // ---------- downloader ----------
    [Fact]
    public async Task Downloader_SavesVerifiedFile_AndReportsProgress()
    {
        var bytes = "hello"u8.ToArray();
        var reports = new List<double>();
        var dir = TempDir();
        try
        {
            var path = await Downloader(bytes).DownloadAsync(Asset("SoundSwap-Setup-1.3.0.exe", bytes, HelloSha), dir, new SyncProgress(reports));
            Assert.True(bytes.SequenceEqual(await File.ReadAllBytesAsync(path)));
            Assert.Equal(1.0, reports[^1]);
            Assert.Empty(Directory.GetFiles(dir, "*.part"));
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public async Task Downloader_RejectsWrongSize_AndLeavesNothingBehind()
    {
        var dir = TempDir();
        var asset = Asset("SoundSwap-Setup-1.3.0.exe", "hello"u8.ToArray(), null) with { Size = 99 };
        try
        {
            await Assert.ThrowsAsync<InvalidDataException>(() => Downloader("hello"u8.ToArray()).DownloadAsync(asset, dir));
            Assert.Empty(Directory.GetFiles(dir));
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public async Task Downloader_RejectsChecksumMismatch()
    {
        var dir = TempDir();
        var wrong = new string('0', 64);
        try
        {
            await Assert.ThrowsAsync<InvalidDataException>(() =>
                Downloader("hello"u8.ToArray()).DownloadAsync(Asset("SoundSwap-Setup-1.3.0.exe", "hello"u8.ToArray(), wrong), dir));
            Assert.Empty(Directory.GetFiles(dir));
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public async Task Downloader_RefusesUntrustedHostsAndNonInstallers()
    {
        var bytes = "hello"u8.ToArray();
        var dir = TempDir();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Downloader(bytes).DownloadAsync(Asset("SoundSwap-Setup.exe", bytes, null, "evil.example.com"), dir));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Downloader(bytes).DownloadAsync(Asset("notes.txt", bytes, null), dir));
    }

    [Fact]
    public async Task Downloader_NeverWritesOutsideTheTargetFolder()
    {
        var bytes = "hello"u8.ToArray();
        var dir = TempDir();
        try
        {
            var path = await Downloader(bytes).DownloadAsync(Asset("../evil-setup.exe", bytes, null), dir);
            Assert.Equal(Path.GetFullPath(dir), Path.GetDirectoryName(path));
        }
        finally { Directory.Delete(dir, true); }
    }

    // ---------- helpers ----------
    private static string TempDir() => Path.Combine(Path.GetTempPath(), "soundswap-test-" + Guid.NewGuid());

    private static InstallerAsset Asset(string name, byte[] bytes, string? sha, string host = "github.com") =>
        new(name, new Uri($"https://{host}/Stevehunn/soundswap/releases/download/v1/{Path.GetFileName(name)}"), bytes.Length, sha);

    private static UpdateDownloader Downloader(byte[] content) =>
        new(new HttpClient(new FakeHttp(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(content) })));

    private static HttpResponseMessage Json(string json) =>
        new(HttpStatusCode.OK) { Content = new StringContent(json) };

    private sealed class SyncProgress : IProgress<double>
    {
        private readonly List<double> _values;
        public SyncProgress(List<double> values) => _values = values;
        public void Report(double value) => _values.Add(value);
    }

    private sealed class FakeHttp : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;
        public List<HttpRequestMessage> Requests { get; } = new();
        public FakeHttp(Func<HttpRequestMessage, HttpResponseMessage> respond) => _respond = respond;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(_respond(request));
        }
    }
}
