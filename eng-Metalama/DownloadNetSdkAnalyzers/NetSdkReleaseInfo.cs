using NuGet.Versioning;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

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

    public static async Task<NetSdkDownloader> GetLatestStableSdkForRoslynVersionAsync(Version requestedRoslynVersion)
    {
        // TODO: make this more efficient by not dowloading releases-index.json and all the releases.json every time?

        // Replace empty version components with zeroes. Version says that e.g. 4.6.0.0 > 4.6.0, but we want those two to be considered equal.
        if (requestedRoslynVersion.Build == -1)
        {
            requestedRoslynVersion = new Version(requestedRoslynVersion.Major, requestedRoslynVersion.Minor, 0, 0);
        }
        else if (requestedRoslynVersion.Revision == -1)
        {
            requestedRoslynVersion = new Version(requestedRoslynVersion.Major, requestedRoslynVersion.Minor, requestedRoslynVersion.Build, 0);
        }

        var netSdkReleasesPath = "net-sdk-releases.json";

        NetSdkReleasesDocument netSdkReleases;

        if (File.Exists(netSdkReleasesPath))
        {
            using var stream = File.OpenRead(netSdkReleasesPath);

            netSdkReleases = JsonSerializer.Deserialize<NetSdkReleasesDocument>(stream, s_jsonOptions)!;
        }
        else
        {
            netSdkReleases = new(new());
        }

        var releasesIndex = await s_httpClient.GetFromJsonAsync<ReleasesIndexDocument>(ReleasesIndexUrl, s_jsonOptions);

        // The three most recent .Net releases should be sufficient.
        var channels = releasesIndex!.Channels.Take(3);

        var change = false;

        foreach (var channel in channels)
        {
            if (channel.SupportPhase == "preview")
            {
                continue;
            }

            var releases = await s_httpClient.GetFromJsonAsync<ReleasesDocument>(channel.ReleasesJsonUrl, s_jsonOptions);

            foreach (var release in releases!.Releases)
            {
                if (release.Sdk.VersionDisplay.IsPrerelease)
                {
                    continue;
                }

                if (!netSdkReleases.Releases.ContainsKey(release.Sdk.VersionDisplay))
                {
                    var sdkZip = release.Sdk.Files.Single(file => file.Name == "dotnet-sdk-win-x64.zip");

                    var downloader = await NetSdkDownloader.CreateAsync(sdkZip.Url, release.Sdk.VersionDisplay);

                    netSdkReleases.Releases.Add(
                        release.Sdk.VersionDisplay, new(sdkZip.Url, downloader.GetCodeAnalysisVersion()));

                    change = true;
                }
            }
        }

        if (change)
        {
            using var stream = File.OpenWrite(netSdkReleasesPath);

            JsonSerializer.Serialize(stream, netSdkReleases, s_jsonOptions);
        }

        var (foundReleaseVersion, foundRelease) = netSdkReleases.Releases.Last(kvp => kvp.Value.RoslynVersion <= requestedRoslynVersion);

        // TODO: this could reuse downloader from the previous step
        return await NetSdkDownloader.CreateAsync(foundRelease.SdkZipUrl, foundReleaseVersion);
    }
}
