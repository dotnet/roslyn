// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System.Collections.Immutable;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.RoslynTools.Products;
using Microsoft.RoslynTools.Utilities;

namespace Microsoft.RoslynTools.CreateReleaseTags;

internal sealed partial class SdkReleaseTagger
    : AbstractReleaseTagger<SdkReleaseTagger.SdkReleaseInformation, SdkReleaseTagger.SdkBuildInformation>
{
    private const string SdkRepoBaseUrl = "https://api.github.com/repos/dotnet/sdk/";

    public override string Name => ".NET SDK";

    public override async Task<ImmutableArray<SdkReleaseInformation>> GetReleasesAsync(RemoteConnections connections)
    {
        var githubClient = connections.GitHubClient;

        var gitRefs = await githubClient.GetFromJsonAsync<RefResponse[]>(SdkRepoBaseUrl + "git/matching-refs/tags/");
        if (gitRefs is null)
        {
            return [];
        }

        var releases = ImmutableArray.CreateBuilder<SdkReleaseInformation>();

        foreach (var gitRef in gitRefs)
        {
            if (gitRef is not { Object: { Type: "commit", Sha: { } sha } })
            {
                continue;
            }

            var match = VersionTag().Match(gitRef.Ref);
            if (!match.Success)
            {
                continue;
            }

            var mainVersion = match.Groups["mainVersion"].Value;
            var majorVersion = int.Parse(mainVersion.Split('.')[0]);
            // Only return tags for .NET 8+
            if (majorVersion < 8)
            {
                continue;
            }

            var previewVersion = match.Groups["previewVersion"].Value;

            var gitCommit = await githubClient.GetFromJsonAsync<CommitResponse>(SdkRepoBaseUrl + $"git/commits/{sha}");
            if (gitCommit is null)
            {
                continue;
            }

            var tagName = match.Groups["tagName"].Value;

            releases.Add(new SdkReleaseInformation(mainVersion, previewVersion, gitCommit.Author.Date, tagName));
        }

        return releases.ToImmutable();
    }

    public override async Task<SdkBuildInformation?> TryGetBuildAsync(RemoteConnections connections, IProduct product, SdkReleaseInformation release)
    {
        var versionDetailsXml = await connections.GitHubClient
            .GetStringAsync($"https://raw.githubusercontent.com/dotnet/sdk/refs/tags/{release.SdkTagName}/eng/Version.Details.xml");
        var versionDetails = XDocument.Parse(versionDetailsXml);

        var dependencyElement = versionDetails.Descendants("Dependency")
            .SingleOrDefault(element => element.Attribute("Name")?.Value == product.SdkPackageName);
        if (dependencyElement is null)
        {
            return null;
        }

        var version = dependencyElement.Attribute("Version")?.Value;
        if (version is null)
        {
            return null;
        }

        var buildId = ConvertVersionToBuildId(version);

        return dependencyElement.Element("Sha")?.Value is { } sha
            ? new SdkBuildInformation(sha, buildId, version)
            : null;

        static string ConvertVersionToBuildId(string version)
        {
            // Take 4.12.0-3.24574.8 or 4.12.0-preview.3.24574.8 and construct 20241124.8
            var previewParts = version.Split('-')[1].Split('.');
            var buildDate = previewParts.Length == 3
                ? previewParts[1]
                : previewParts[2];
            var run = previewParts.Length == 3
                ? previewParts[2]
                : previewParts[3];

            var buildYear = buildDate[..2];
            var buildMonth = int.Parse(buildDate[2..]) / 50;
            var buildDay = int.Parse(buildDate[2..]) % 50;

            return $"20{buildYear}{buildMonth:D2}{buildDay:D2}.{run}";
        }
    }

    public override async Task<SdkBuildInformation?> TryGetBuildAsync(RemoteConnections connections, IProduct product, SdkBuildInformation vmrBuild)
    {
        // This is used when the SHA returned from the Release build does not exist in the target repo. In SDK scenarios it
        // gives us a chance to query the dotnet VMR and identify the SHA for our repo.
        VmrSourceManifest? sourceManifest = null;

        try
        {
            sourceManifest = await connections.GitHubClient
                .GetFromJsonAsync<VmrSourceManifest?>($"https://raw.githubusercontent.com/dotnet/dotnet/{vmrBuild.CommitSha}/src/source-manifest.json");
        }
        catch (Exception) { }

        if (sourceManifest is null)
        {
            return null;
        }

        var repository = sourceManifest.Repositories
            .SingleOrDefault(repository => repository.RemoteUri == product.RepoHttpBaseUrl);
        if (repository is null)
        {
            return null;
        }

        return new SdkUnifiedBuildInformation(
            repository.CommitSha,
            vmrBuild.BuildId,
            vmrBuild.ProductVersion,
            vmrBuild.CommitSha);
    }

    public override string GetTagName(SdkReleaseInformation release)
    {
        var tag = "NET-SDK-" + release.MainVersion;
        return string.IsNullOrEmpty(release.PreviewVersion)
            ? tag
            : tag + "-" + release.PreviewVersion;
    }

    public override string CreateTagMessage(IProduct product, SdkReleaseInformation release, SdkBuildInformation build)
    {
        if (build is SdkUnifiedBuildInformation unifiedBuild)
        {
            return $"{product.Name} VMR Version: {build.ProductVersion}\r\nVMR Commit SHA: {unifiedBuild.VmrCommitSha}\r\nVMR Internal ID: {build.BuildId}\r\nSDK Tag: {release.SdkTagName}";
        }

        return $"{product.Name} Version: {build.ProductVersion}\r\nInternal ID: {build.BuildId}\r\nSDK Tag: {release.SdkTagName}";
    }

    public class RefResponse
    {
        [JsonPropertyName("ref")]
        public string Ref { get; set; } = null!;
        [JsonPropertyName("object")]
        public RefObject Object { get; set; } = null!;
    }

    public class RefObject
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = null!;
        [JsonPropertyName("sha")]
        public string Sha { get; set; } = null!;
    }

    public class CommitResponse
    {
        [JsonPropertyName("sha")]
        public string Sha { get; set; } = null!;
        public Author Author { get; set; } = null!;
    }

    public class Author
    {
        [JsonPropertyName("date")]
        public DateTime Date { get; set; }
    }

    internal class SdkReleaseInformation(
        string mainVersion,
        string? previewVersion,
        DateTime creationTime,
        string sdkTagName) : ReleaseInformation(mainVersion, previewVersion, creationTime)
    {
        public readonly string SdkTagName = sdkTagName;
    }

    internal class SdkBuildInformation(
        string commitSha,
        string buildId,
        string productVersion) : BuildInformation(commitSha, buildId)
    {
        public readonly string ProductVersion = productVersion;
    }

    internal class SdkUnifiedBuildInformation(
        string commitSha,
        string vmrBuildId,
        string vmrProductVersion,
        string vmrCommitSha) : SdkBuildInformation(commitSha, vmrBuildId, vmrProductVersion)
    {
        public readonly string VmrCommitSha = vmrCommitSha;
    }

    internal class VmrSourceManifest
    {
        [JsonPropertyName("repositories")]
        public VmrSourceRepository[] Repositories { get; set; } = null!;
    }

    internal class VmrSourceRepository
    {
        [JsonPropertyName("barId")]
        public int? BarId { get; set; } = null;
        [JsonPropertyName("path")]
        public string Path { get; set; } = null!;
        [JsonPropertyName("remoteUri")]
        public string RemoteUri { get; set; } = null!;
        [JsonPropertyName("commitSha")]
        public string CommitSha { get; set; } = null!;
    }

    [GeneratedRegex(@"^refs/tags/(?<tagName>v(?<mainVersion>\d+\.\d+\.\d+)(?:-(?<previewVersion>.+))?)$")]
    private static partial Regex VersionTag();
}
