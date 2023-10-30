using NuGet.Versioning;
using System.Text.Json.Serialization;

namespace DownloadNetSdkAnalyzers;

record NetSdkReleasesDocument(SortedDictionary<SemanticVersion, NetSdkRelease> Releases);

record NetSdkRelease(string SdkZipUrl, SemanticVersion RoslynVersion);

record ReleasesDocument(IList<Release> Releases);

record Release(Sdk Sdk);

record Sdk(SemanticVersion VersionDisplay, IList<SdkFile> Files);

record SdkFile(string Name, string Url);

record ReleasesIndexDocument(
    [property: JsonPropertyName("releases-index")] IList<Channel> Channels);

record Channel(
    string ChannelVersion,
    SemanticVersion LatestRelease,
    DateOnly LatestReleaseDate,
    string SupportPhase,
    [property: JsonPropertyName("releases.json")] string ReleasesJsonUrl);
