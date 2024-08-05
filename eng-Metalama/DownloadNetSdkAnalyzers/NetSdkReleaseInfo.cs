using System.Net.Http.Json;
using System.Text.Json;
using NuGet.Versioning;

namespace DownloadNetSdkAnalyzers;

static class NetSdkReleaseInfo
{
    private const string ReleasesIndexUrl = "https://dotnetcli.blob.core.windows.net/dotnet/release-metadata/releases-index.json";

    static readonly HttpClient s_httpClient = new();

    static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.KebabCaseLower,
        Converters = { new SemanticVersionConverter(), new SemanticVersionDictionaryKeyConverterFactory() },
        WriteIndented = true,
    };

    public static async Task<NetSdkDownloader> GetLatestSdkDownloaderForRoslynVersionAsync(SemanticVersion requestedRoslynVersion)
    {
        var (foundReleaseVersion, foundRelease) = await GetLatestSdkForRoslynVersionAsync(requestedRoslynVersion);

        return await NetSdkDownloader.CreateAsync(foundRelease.SdkZipUrl, foundReleaseVersion);
    }

    public static async Task<SemanticVersion> GetLatestSdkVersionForRoslynVersionAsync(SemanticVersion requestedRoslynVersion)
    {
        var (foundReleaseVersion, _) = await GetLatestSdkForRoslynVersionAsync(requestedRoslynVersion);

        return foundReleaseVersion;
    }

    private static async Task<KeyValuePair<SemanticVersion, NetSdkRelease>> GetLatestSdkForRoslynVersionAsync(SemanticVersion requestedRoslynVersion)
    {
        // TODO: make this more efficient by not dowloading releases-index.json and all the releases.json every time?

        var netSdkReleasesPath = "net-sdk-releases.json";

        NetSdkReleasesDocument? netSdkReleases = null;

        if (File.Exists(netSdkReleasesPath))
        {
            using var stream = File.OpenRead(netSdkReleasesPath);

            try
            {
                netSdkReleases = JsonSerializer.Deserialize<NetSdkReleasesDocument>(stream, s_jsonOptions)!;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
            }
        }

        if (netSdkReleases == null)
        {
            netSdkReleases = new(new());
        }

        var releasesIndex = await s_httpClient.GetFromJsonAsync<ReleasesIndexDocument>(ReleasesIndexUrl, s_jsonOptions);

        // The four most recent .Net releases should be sufficient: preview (e.g. 9.0), current LTS (8.0), out-of support (7.0) and the previous LTS (6.0).
        var channels = releasesIndex!.Channels.Take(4);

        var change = false;

        foreach (var channel in channels)
        {
            var releases = await s_httpClient.GetFromJsonAsync<ReleasesDocument>(channel.ReleasesJsonUrl, s_jsonOptions);

            foreach (var release in releases!.Releases)
            {
                var sdkVersion = release.Sdk.VersionDisplay;

                // Only consider pre-release versions if the .Net version is still in preview.
                if (sdkVersion.IsPrerelease && channel.SupportPhase is not ("preview" or "go-live"))
                {
                    continue;
                }

                if (!netSdkReleases.Releases.ContainsKey(sdkVersion))
                {
                    var sdkZip = release.Sdk.Files.Single(file => file.Name == "dotnet-sdk-win-x64.zip");

                    using var downloader = await NetSdkDownloader.CreateAsync(sdkZip.Url, sdkVersion);

                    netSdkReleases.Releases.Add(
                        sdkVersion, new(sdkZip.Url, downloader.GetCodeAnalysisVersion()));

                    change = true;
                }
            }
        }

        if (change)
        {
            using var stream = File.Create(netSdkReleasesPath);

            JsonSerializer.Serialize(stream, netSdkReleases, s_jsonOptions);
        }

        // Only consider preview SDKs if the requested version is also preview.
        return netSdkReleases.Releases
            .Last(kvp => (!kvp.Key.IsPrerelease || requestedRoslynVersion.IsPrerelease) && kvp.Value.RoslynVersion <= requestedRoslynVersion);
    }
}
