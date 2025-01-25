// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System.Collections.Immutable;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using Microsoft.RoslynTools.Authentication;
using Microsoft.RoslynTools.Products;
using Microsoft.RoslynTools.Utilities;
using Microsoft.RoslynTools.VS;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Newtonsoft.Json.Linq;

namespace Microsoft.RoslynTools.CreateReleaseTags;

internal static class CreateReleaseTags
{
    public static async Task<int> CreateReleaseTagsAsync(string productName, RoslynToolsSettings settings, ILogger logger)
    {
        try
        {
            using var remoteConnections = new RemoteConnections(settings);
            var devdivConnection = remoteConnections.DevDivConnection;
            var dncengConnection = remoteConnections.DncEngConnection;

            var product = VSBranchInfo.AllProducts.Single(p => p.Name.Equals(productName, StringComparison.OrdinalIgnoreCase));

            var connections = new[] { dncengConnection, devdivConnection };

            logger.LogInformation($"Opening {product.Name} repo and gathering tags...");

            var repository = new Repository(Environment.CurrentDirectory);
            var existingTags = repository.Tags.ToImmutableArray();

            if (!repository.Network.Remotes.Any(r =>
                    r.Url.Equals(product.RepoHttpBaseUrl, StringComparison.OrdinalIgnoreCase) ||
                    r.Url.Equals(product.RepoSshBaseUrl, StringComparison.OrdinalIgnoreCase) ||
                    r.Url.Equals(product.RepoHttpBaseUrl + ".git", StringComparison.OrdinalIgnoreCase)))
            {
                logger.LogError($"Repo does not appear to be the {product.Name} repo. Please fetch tags if tags are not already fetched and try again.");
                return 1;
            }

            logger.LogInformation("Loading VS releases...");

            var visualStudioReleases = await GetVisualStudioReleasesAsync(devdivConnection.GitClient);

            logger.LogInformation("Tagging releases...");

            foreach (var visualStudioRelease in visualStudioReleases)
            {
                var tagName = TryGetTagName(visualStudioRelease);

                if (tagName is not null)
                {
                    if (!existingTags.Any(t => t.FriendlyName == tagName))
                    {
                        logger.LogInformation($"Tag '{tagName}' is missing.");

                        BuildInformation? build = null;
                        foreach (var connection in connections)
                        {
                            build = await TryGetBuildInfoForReleaseAsync(product, visualStudioRelease, devdivConnection, connection);

                            if (build is not null)
                            {
                                break;
                            }
                        }

                        if (build is not null)
                        {
                            logger.LogInformation($"Tagging {build.CommitSha} as '{tagName}'.");

                            var message = $"Build Branch: {build.SourceBranch}\r\nInternal ID: {build.BuildId}\r\nInternal VS ID: {visualStudioRelease.BuildId}";

                            try
                            {
                                repository.ApplyTag(tagName, build.CommitSha, new Signature(product.GitUserName, product.GitEmail, when: visualStudioRelease.CreationTime), message);
                            }
                            catch (Exception ex)
                            {
                                logger.LogWarning(ex, $"Unable to tag the commit '{build.CommitSha}' with '{tagName}'.");
                            }
                        }
                        else
                        {
                            logger.LogWarning($"Unable to find the build for '{tagName}'.");
                        }
                    }
                    else
                    {
                        logger.LogInformation($"Tag '{tagName}' already exists.");
                    }
                }
            }

            logger.LogInformation("Tagging complete.");

            return 0;
        }
        catch (VssUnauthorizedException vssEx)
        {
            logger.LogError(vssEx, "Authentication error occurred: {Message}. Run `roslyn-tools authenticate` to configure the AzDO authentication tokens.", vssEx.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unable to open repo. Please run from your repo directory for the product in question.");
        }

        return 1;
    }

    private static async Task<BuildInformation?> TryGetBuildInfoForReleaseAsync(IProduct product, VisualStudioVersion release, AzDOConnection vsConnection, AzDOConnection connection)
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
                            return new BuildInformation(build.SourceVersion, build.SourceBranch, build.BuildNumber);
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

            return new BuildInformation(commitSha, branchName, buildId);
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
            var branchName = string.Join("", urlSegments.SkipWhile(segment => !segment.EndsWith("roslyn/")).Skip(1).TakeWhile(segment => segment.EndsWith("/"))).TrimEnd('/');

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
        VisualStudioVersion release)
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

    private static async Task<ImmutableArray<VisualStudioVersion>> GetVisualStudioReleasesAsync(GitHttpClient gitClient)
    {
        var vsRepository = await GetVSRepositoryAsync(gitClient);
        var tags = await gitClient.GetRefsAsync(vsRepository.Id, filterContains: "release/vs", peelTags: true);

        var builder = ImmutableArray.CreateBuilder<VisualStudioVersion>();

        foreach (var tag in tags)
        {
            const string tagPrefix = "refs/tags/release/vs/";

            if (!tag.Name.StartsWith(tagPrefix))
            {
                continue;
            }

            var parts = tag.Name.Substring(tagPrefix.Length).Split('-');

            if (parts.Length != 1 && parts.Length != 2)
            {
                continue;
            }

            var isDottedVersionRegex = new Regex("^[0-9]+(\\.[0-9]+)*$");

            if (!isDottedVersionRegex.IsMatch(parts[0]))
            {
                continue;
            }

            // If there is no peeled object, it means it's a simple tag versus an annotated tag; those aren't usually how
            // VS releases are tagged so skip it
            if (tag.PeeledObjectId == null)
            {
                continue;
            }

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

            // It's not entirely clear to me how this format was chosen, but for consistency with old tags, we'll keep it
            var buildId = $"{buildInformation["Branch"]?.ToString().Replace("/", ".")}-{buildInformation["BuildNumber"]}";

            if (parts.Length == 2)
            {
                const string previewPrefix = "preview.";

                if (!parts[1].StartsWith(previewPrefix))
                {
                    continue;
                }

                var possiblePreviewVersion = parts[1].Substring(previewPrefix.Length);

                if (!isDottedVersionRegex.IsMatch(possiblePreviewVersion))
                {
                    continue;
                }

                builder.Add(new VisualStudioVersion(parts[0], possiblePreviewVersion, tag.PeeledObjectId, annotatedTag.TaggedBy.Date, buildId));
            }
            else
            {
                builder.Add(new VisualStudioVersion(parts[0], previewVersion: null, tag.PeeledObjectId, annotatedTag.TaggedBy.Date, buildId));
            }
        }

        return builder.ToImmutable();
    }

    private static async Task<GitRepository> GetVSRepositoryAsync(GitHttpClient gitClient)
    {
        return await gitClient.GetRepositoryAsync("DevDiv", "VS");
    }

    private static string? TryGetTagName(VisualStudioVersion release)
    {
        var tag = "Visual-Studio-";

        if (release.MainVersion.StartsWith("16."))
        {
            tag += "2019-";
        }
        else if (release.MainVersion.StartsWith("17."))
        {
            tag += "2022-";
        }
        else
        {
            // We won't worry about tagging earlier things than VS2019 releases for now
            return null;
        }

        tag += "Version-" + release.MainVersion;

        if (release.PreviewVersion != null)
        {
            tag += "-Preview-" + release.PreviewVersion;
        }

        return tag;
    }
}
