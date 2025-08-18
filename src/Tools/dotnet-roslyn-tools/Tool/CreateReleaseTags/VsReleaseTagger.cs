// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System.Collections.Immutable;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.RoslynTools.Products;
using Microsoft.RoslynTools.Utilities;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Newtonsoft.Json.Linq;

namespace Microsoft.RoslynTools.CreateReleaseTags;

internal sealed partial class VsReleaseTagger
    : AbstractReleaseTagger<VsReleaseTagger.VsReleaseInformation, VsReleaseTagger.VsBuildInformation>
{
    public override string Name => "VS";

    // Only tag VS builds that are under support.
    // See https://learn.microsoft.com/en-us/visualstudio/productinfo/vs-servicing#long-term-servicing-channel-ltsc-support
    private readonly ImmutableDictionary<string, string> _vsVersionMap = new Dictionary<string, string>
    {
        { "16.11.", "2019" },
        { "17.8.", "2022" },
        { "17.10.", "2022" },
        { "17.12.", "2022" },
        { "17.13.", "2022" },
        { "17.14.", "2022" },
    }.ToImmutableDictionary();

    public override async Task<ImmutableArray<VsReleaseInformation>> GetReleasesAsync(RemoteConnections connections)
    {
        var gitClient = connections.DevDivConnection.GitClient;
        var vsRepository = await GetVSRepositoryAsync(gitClient);
        var tags = await gitClient.GetRefsAsync(vsRepository.Id, filterContains: "release/vs", peelTags: true);

        var builder = ImmutableArray.CreateBuilder<VsReleaseInformation>();

        foreach (var tag in tags)
        {
            const string TagPrefix = "refs/tags/release/vs/";

            if (!tag.Name.StartsWith(TagPrefix))
            {
                continue;
            }

            var parts = tag.Name[TagPrefix.Length..].Split('-');

            if (parts.Length != 1 && parts.Length != 2)
            {
                continue;
            }

            if (!IsDottedVersion().IsMatch(parts[0]))
            {
                continue;
            }

            // If there is no peeled object, it means it's a simple tag versus an annotated tag; those aren't usually how
            // VS releases are tagged so skip it
            if (tag.PeeledObjectId == null)
            {
                continue;
            }

            var sha = tag.PeeledObjectId;

            var annotatedTag = await gitClient.GetAnnotatedTagAsync(vsRepository.ProjectReference.Id, vsRepository.Id, tag.ObjectId);
            if (annotatedTag == null)
            {
                continue;
            }

            JObject buildInformation;

            try
            {
                buildInformation = JObject.Parse(annotatedTag.Message);
            }
            catch
            {
                continue;
            }

            var mainVersion = parts[0];
            if (!_vsVersionMap.Any(pair => mainVersion.StartsWith(pair.Key)))
            {
                // We only support tagging builds specified in the version map.
                continue;
            }

            // It's not entirely clear to me how this format was chosen, but for consistency with old tags, we'll keep it
            var buildId = $"{buildInformation["Branch"]?.ToString().Replace("/", ".")}-{buildInformation["BuildNumber"]}";

            string? possiblePreviewVersion = null;
            if (parts.Length == 2)
            {
                const string PreviewPrefix = "preview.";
                if (!parts[1].StartsWith(PreviewPrefix))
                {
                    continue;
                }

                possiblePreviewVersion = parts[1][PreviewPrefix.Length..];
                if (!IsDottedVersion().IsMatch(possiblePreviewVersion))
                {
                    continue;
                }
            }

            builder.Add(new VsReleaseInformation(mainVersion, possiblePreviewVersion, annotatedTag.TaggedBy.Date, sha, buildId));
        }

        return builder.ToImmutable();

        static Task<GitRepository> GetVSRepositoryAsync(GitHttpClient gitClient)
            => gitClient.GetRepositoryAsync("DevDiv", "VS");
    }

    public override async Task<VsBuildInformation?> TryGetBuildAsync(RemoteConnections connections, IProduct product, VsReleaseInformation release)
    {
        var devdivConnection = connections.DevDivConnection;

        VsBuildInformation? build = null;
        foreach (var connection in new[] { connections.DncEngConnection, devdivConnection })
        {
            build = await TryGetBuildInfoForReleaseAsync(product, release, devdivConnection, connection);
            if (build is not null)
            {
                return build;
            }
        }

        return build;
    }

    public override Task<VsBuildInformation?> TryGetBuildAsync(RemoteConnections connections, IProduct product, VsBuildInformation vmrBuild)
    {
        // This is used when the SHA returned from the Release build does not exist in the target repo. In SDK scenarios it
        // gives us a chance to query the dotnet VMR and identify the SHA for our repo.
        return Task.FromResult<VsBuildInformation?>(null);
    }

    private static async Task<VsBuildInformation?> TryGetBuildInfoForReleaseAsync(IProduct product, VsReleaseInformation release, AzDOConnection vsConnection, AzDOConnection connection)
    {
        try
        {
            var url = await VisualStudioRepository.GetUrlFromComponentJsonFileAsync(release.CommitSha, GitVersionType.Commit, vsConnection, product.ComponentJsonFileName, product.ComponentName);
            if (url is null)
            {
                return default;
            }

            // First we try to get the info we want from the build directly, if we can find it
            var buildNumber = VisualStudioRepository.GetBuildNumberFromUrl(url);
            var pipelineName = product.GetBuildPipelineName(connection.BuildProjectName);
            if (pipelineName is not null)
            {
                var builds = await connection.TryGetBuildsAsync(pipelineName, buildNumber);
                if (builds is not null)
                {
                    foreach (var build in builds)
                    {
                        if (!string.IsNullOrWhiteSpace(build.SourceBranch))
                        {
                            return new VsBuildInformation(build.SourceVersion, build.SourceBranch, build.BuildNumber);
                        }
                    }
                }
            }

            // Fallback if we can't get the info from the build, to parse the url or nuspec file
            var branchName = TryGetRoslynBranchForRelease(url);
            if (string.IsNullOrEmpty(branchName) || string.IsNullOrEmpty(buildNumber))
            {
                return null;
            }

            var commitSha = await TryGetCommitShaFromBuildAsync(product, connection, buildNumber)
                ?? await TryGetRoslynCommitShaFromNuspecAsync(vsConnection, release);
            if (string.IsNullOrEmpty(commitSha))
            {
                return null;
            }

            var buildId = product.GetBuildPipelineName(connection.BuildProjectName) + "_" + buildNumber;

            return new VsBuildInformation(commitSha, branchName, buildId);
        }
        catch
        {
            return null;
        }
    }

    private static string? TryGetRoslynBranchForRelease(string url)
    {
        try
        {
            var parts = url.Split(';');
            if (parts.Length != 2)
            {
                return null;
            }

            if (!parts[1].EndsWith(".vsman"))
            {
                return null;
            }

            var urlSegments = new Uri(parts[0]).Segments;
            var branchName = string.Join("", urlSegments.SkipWhile(segment => !segment.EndsWith("roslyn/")).Skip(1).TakeWhile(segment => segment.EndsWith('/'))).TrimEnd('/');

            return branchName;
        }
        catch
        {
            return null;
        }
    }

    private static async Task<string?> TryGetCommitShaFromBuildAsync(
        IProduct product,
        AzDOConnection buildConnection,
        string buildNumber)
    {
        var build = (await buildConnection.TryGetBuildsAsync(product.GetBuildPipelineName(buildConnection.BuildProjectName)!, buildNumber))?.SingleOrDefault();

        if (build == null)
        {
            return null;
        }

        return build.SourceVersion;
    }

    private static async Task<string?> TryGetRoslynCommitShaFromNuspecAsync(
        AzDOConnection vsConnection,
        VsReleaseInformation release)
    {
        var defaultConfigContents = await VisualStudioRepository.GetFileContentsAsync(release.CommitSha, GitVersionType.Commit, vsConnection, @".corext\Configs\default.config");
        var defaultConfig = XDocument.Parse(defaultConfigContents);

        var packageElement = defaultConfig.Descendants("package")
            .SingleOrDefault(element => element.Attribute("id")?.Value == "VS.ExternalAPIs.Roslyn");
        if (packageElement == null)
        {
            return null;
        }

        var version = packageElement.Attribute("version")?.Value;
        if (version is null)
        {
            return null;
        }

        var nuspecUrl = $@"https://devdiv.pkgs.visualstudio.com/_packaging/VS-CoreXtFeeds/nuget/v3/flat2/vs.externalapis.roslyn/{version}/vs.externalapis.roslyn.nuspec";

        var nuspecResult = await vsConnection.NuGetClient.GetAsync(nuspecUrl);
        if (nuspecResult.StatusCode != HttpStatusCode.OK)
        {
            return null;
        }

        var nuspecContent = await nuspecResult.Content.ReadAsStringAsync();
        var nuspec = XElement.Parse(nuspecContent);

        var respository = nuspec.Elements()
            .SingleOrDefault()
            ?.Elements(XName.Get("repository", nuspec.Name.NamespaceName))
            .SingleOrDefault();
        if (respository == null)
        {
            return null;
        }

        return respository.Attribute("commit")?.Value;
    }

    public override string GetTagName(VsReleaseInformation release)
    {
        // This is verified to have a value when we create the release information
        var vsVersion = _vsVersionMap.First(pair => release.MainVersion.StartsWith(pair.Key)).Value;
        var tag = $"Visual-Studio-{vsVersion}-Version-{release.MainVersion}";

        return string.IsNullOrEmpty(release.PreviewVersion)
            ? tag
            : tag + "-Preview-" + release.PreviewVersion;
    }

    public override string CreateTagMessage(IProduct product, VsReleaseInformation release, VsBuildInformation build)
    {
        return $"Build Branch: {build.SourceBranch}\r\nInternal ID: {build.BuildId}\r\nInternal VS ID: {release.BuildId}";
    }

    public class VsReleaseInformation(
        string mainVersion,
        string? previewVersion,
        DateTime creationTime,
        string commitSha,
        string buildId) : ReleaseInformation(mainVersion, previewVersion, creationTime)
    {
        public readonly string CommitSha = commitSha;
        public readonly string BuildId = buildId;
    }

    internal class VsBuildInformation(
        string commitSha,
        string sourceBranch,
        string buildId) : BuildInformation(commitSha, buildId)
    {
        public readonly string SourceBranch = sourceBranch;
    }

    [GeneratedRegex("^[0-9]+(\\.[0-9]+)*$")]
    private static partial Regex IsDottedVersion();
}
