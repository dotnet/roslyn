// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using Microsoft.RoslynTools.Utilities;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Newtonsoft.Json.Linq;
using System.Collections.Immutable;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Microsoft.RoslynTools.CreateReleaseTags;

public static class CreateReleaseTags
{
    public static async Task<int> CreateReleaseTagsAsync(ILogger logger)
    {
        try
        {
            var client = new SecretClient(
                vaultUri: new Uri("https://managedlanguages.vault.azure.net"),
                credential: new DefaultAzureCredential(includeInteractiveCredentials: true));

            using var devdivConnection = new AzDOConnection("https://devdiv.visualstudio.com/DefaultCollection", "DevDiv", "Roslyn-Signed", client, "vslsnap-vso-auth-token");
            using var dncengConnection = new AzDOConnection("https://dnceng.visualstudio.com/DefaultCollection", "internal", "dotnet-roslyn CI", client, "vslsnap-build-auth-token");

            var connections = new[] { devdivConnection, dncengConnection };

            logger.LogInformation("Opening Roslyn repo and gathering tags...");

            var roslynRepository = new Repository(Environment.CurrentDirectory);
            var existingTags = roslynRepository.Tags.ToImmutableArray();

            var isRoslynRepo = existingTags.Any(tag => tag.FriendlyName == "Visual-Studio-2019-Version-16.11");
            if (!isRoslynRepo)
            {
                logger.LogError("Repo does not appear to be the Roslyn repo. Please fetch tags if tags are not already fetched and try again.");
                return 1;
            }

            logger.LogInformation("Loading VS releases...");

            var visualStudioReleases = await GetVisualStudioReleasesAsync(devdivConnection.GitClient);

            logger.LogInformation("Tagging releases...");

            foreach (var visualStudioRelease in visualStudioReleases)
            {
                var roslynTagName = TryGetRoslynTagName(visualStudioRelease);

                if (roslynTagName is not null)
                {
                    if (!existingTags.Any(t => t.FriendlyName == roslynTagName))
                    {
                        logger.LogInformation($"Tag '{roslynTagName}' is missing.");

                        RoslynBuildInformation? roslynBuild = null;
                        foreach (var connection in connections)
                        {
                            roslynBuild = await TryGetRoslynBuildForReleaseAsync(visualStudioRelease, devdivConnection, connection);

                            if (roslynBuild is not null)
                            {
                                break;
                            }
                        }

                        if (roslynBuild is not null)
                        {
                            logger.LogInformation($"Tagging {roslynBuild.CommitSha} as '{roslynTagName}'.");

                            string message = $"Build Branch: {roslynBuild.SourceBranch}\r\nInternal ID: {roslynBuild.BuildId}\r\nInternal VS ID: {visualStudioRelease.BuildId}";

                            roslynRepository.ApplyTag(roslynTagName, roslynBuild.CommitSha, new Signature("dotnet bot", "dotnet-bot@microsoft.com", when: visualStudioRelease.CreationTime), message);
                        }
                        else
                        {
                            logger.LogWarning($"Unable to find the build for '{roslynTagName}'.");
                        }
                    }
                    else
                    {
                        logger.LogInformation($"Tag '{roslynTagName}' already exists.");
                    }
                }
            }

            logger.LogInformation("Tagging complete.");

            return 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unable to open Roslyn repo. Please run from you Roslyn repo directory.");
            return 1;
        }
    }

    private static async Task<RoslynBuildInformation?> TryGetRoslynBuildForReleaseAsync(VisualStudioVersion release, AzDOConnection vsConnection, AzDOConnection connection)
    {
        try
        {
            var (branchName, buildNumber) = await TryGetRoslynBranchAndBuildNumberForReleaseAsync(release, vsConnection.GitClient);
            if (string.IsNullOrEmpty(branchName) || string.IsNullOrEmpty(buildNumber))
            {
                return null;
            }

            var commitSha = await TryGetRoslynCommitShaFromBuildAsync(connection, buildNumber)
                ?? await TryGetRoslynCommitShaFromNuspecAsync(vsConnection.NuGetClient, release, vsConnection.GitClient);
            if (string.IsNullOrEmpty(commitSha))
            {
                return null;
            }

            var buildId = connection.BuildDefinitionName + "_" + buildNumber;

            return new RoslynBuildInformation(commitSha, branchName, buildId);
        }
        catch
        {
            return null;
        }
    }

    private static async Task<(string branchName, string buildNumber)> TryGetRoslynBranchAndBuildNumberForReleaseAsync(
        VisualStudioVersion release,
        GitHttpClient vsGitClient)
    {
        GitRepository vsRepository = await GetVSRepositoryAsync(vsGitClient);
        var commit = new GitVersionDescriptor { VersionType = GitVersionType.Commit, Version = release.CommitSha };

        using var componentsJsonStream = await vsGitClient.GetItemContentAsync(
            vsRepository.Id,
            @".corext\Configs\dotnetcodeanalysis-components.json",
            download: true,
            versionDescriptor: commit);

        var componentsJsonContents = await new StreamReader(componentsJsonStream).ReadToEndAsync();
        var componentsJson = JObject.Parse(componentsJsonContents);

        var languageServicesUrlAndManifestName = (string?)componentsJson["Components"]?["Microsoft.CodeAnalysis.LanguageServices"]?["url"];
        if (languageServicesUrlAndManifestName is null)
        {
            return default;
        }

        var parts = languageServicesUrlAndManifestName.Split(';');
        if (parts.Length != 2)
        {
            return default;
        }

        if (!parts[1].EndsWith(".vsman"))
        {
            return default;
        }

        var urlSegments = new Uri(parts[0]).Segments;
        var branchName = string.Join("", urlSegments.SkipWhile(segment => !segment.EndsWith("roslyn/")).Skip(1).TakeWhile(segment => segment.EndsWith("/"))).TrimEnd('/');
        var buildNumber = urlSegments.Last();

        return (branchName, buildNumber);
    }

    private static async Task<string?> TryGetRoslynCommitShaFromBuildAsync(
        AzDOConnection buildConnection,
        string buildNumber)
    {
        var buildDefinition = (await buildConnection.BuildClient.GetDefinitionsAsync(buildConnection.BuildProjectName, name: buildConnection.BuildDefinitionName)).Single();
        var build = (await buildConnection.BuildClient.GetBuildsAsync(buildDefinition.Project.Id, definitions: new[] { buildDefinition.Id }, buildNumber: buildNumber)).SingleOrDefault();

        if (build == null)
        {
            return null;
        }

        return build.SourceVersion;
    }

    private static async Task<string?> TryGetRoslynCommitShaFromNuspecAsync(
        HttpClient nugetClient,
        VisualStudioVersion release,
        GitHttpClient vsGitClient)
    {
        GitRepository vsRepository = await GetVSRepositoryAsync(vsGitClient);
        var commit = new GitVersionDescriptor { VersionType = GitVersionType.Commit, Version = release.CommitSha };

        using var defaultConfigStream = await vsGitClient.GetItemContentAsync(
            vsRepository.Id,
            @".corext\Configs\default.config",
            download: true,
            versionDescriptor: commit);
        var defaultConfigContents = await new StreamReader(defaultConfigStream).ReadToEndAsync();
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

        var nuspecResult = await nugetClient.GetAsync(nuspecUrl);
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
        GitRepository vsRepository = await GetVSRepositoryAsync(gitClient);
        var tags = await gitClient.GetRefsAsync(vsRepository.Id, filterContains: "release/vs", peelTags: true);

        var builder = ImmutableArray.CreateBuilder<VisualStudioVersion>();

        foreach (var tag in tags)
        {
            const string tagPrefix = "refs/tags/release/vs/";

            if (!tag.Name.StartsWith(tagPrefix))
            {
                continue;
            }

            string[] parts = tag.Name.Substring(tagPrefix.Length).Split('-');

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
            string buildId = $"{buildInformation["Branch"]?.ToString().Replace("/", ".")}-{buildInformation["BuildNumber"]}";

            if (parts.Length == 2)
            {
                const string previewPrefix = "preview.";

                if (!parts[1].StartsWith(previewPrefix))
                {
                    continue;
                }

                string possiblePreviewVersion = parts[1].Substring(previewPrefix.Length);

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

    private static string? TryGetRoslynTagName(VisualStudioVersion release)
    {
        string tag = "Visual-Studio-";

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
