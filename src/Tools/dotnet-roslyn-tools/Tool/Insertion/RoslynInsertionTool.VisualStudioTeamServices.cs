// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.Policy.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.RoslynTools.PRFinder.Hosts;
using Microsoft.RoslynTools.Utilities;
using Microsoft.VisualStudio.Services.WebApi;
using Newtonsoft.Json;
using Task = System.Threading.Tasks.Task;
using GitCommit = Microsoft.RoslynTools.PRFinder.GitCommit;

namespace Microsoft.RoslynTools.Insertion;

internal static partial class RoslynInsertionTool
{
    private enum ComponentConnectionKind
    {
        DevDiv,
        DncEng,
    }

    private static readonly Lazy<ComponentConnectionKind> s_lazyComponentConnectionKind = new(GetComponentConnectionKind);

    /// <summary>
    /// Used to connect to the AzDO instance which contains the VS repo.
    /// </summary>
    private static AzDOConnection VisualStudioRepoConnection => Connections.DevDivConnection;

    /// <summary>
    /// Used to connect to the AzDO instance which contains the repo of the Component being inserted.
    /// </summary>
    private static AzDOConnection ComponentBuildConnection => s_lazyComponentConnectionKind.Value == ComponentConnectionKind.DevDiv
        ? Connections.DevDivConnection
        : Connections.DncEngConnection;

    private static string ComponentAzdoToken => s_lazyComponentConnectionKind.Value == ComponentConnectionKind.DevDiv
        ? Options.DevDivAzdoToken
        : Options.DncEngAzdoToken;

    private static async Task EnsureAuthenticatedAsync(AzDOConnection connection, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _ = await connection.ProjectClient.GetProject(connection.BuildProjectName).ConfigureAwait(false);
    }

    private static ComponentConnectionKind GetComponentConnectionKind()
    {
        if (string.IsNullOrWhiteSpace(Options.ComponentBuildAzdoUri))
        {
            return ComponentConnectionKind.DevDiv;
        }

        if (Options.ComponentBuildAzdoUri.Contains("devdiv"))
        {
            return ComponentConnectionKind.DevDiv;
        }

        if (Options.ComponentBuildAzdoUri.Contains("dnceng"))
        {
            return ComponentConnectionKind.DncEng;
        }

        throw new ArgumentException($"Component AzDO uri must target either DevDiv or DncEng. Value: '{Options.ComponentBuildAzdoUri}'.");
    }

    private static GitPullRequest CreatePullRequest(string sourceBranch, string targetBranch, string description, string buildToInsert, string reviewerId)
    {
        LogInformation($"Creating pull request sourceBranch:{sourceBranch} targetBranch:{targetBranch} description:{description}");

        return new GitPullRequest
        {
            Title = GetPullRequestTitle(buildToInsert),
            Description = description,
            SourceRefName = sourceBranch,
            TargetRefName = targetBranch,
            IsDraft = Options.CreateDraftPr,
            Reviewers = !string.IsNullOrEmpty(reviewerId) ? [new IdentityRefWithVote { Id = reviewerId }] : null
        };
    }

    private static string GetPullRequestTitle(string buildToInsert)
    {
        var prefix = string.IsNullOrEmpty(Options.TitlePrefix)
            ? string.Empty
            : Options.TitlePrefix + " ";
        var suffix = string.IsNullOrEmpty(Options.TitleSuffix)
            ? string.Empty
            : " " + Options.TitleSuffix;

        return $"{prefix}{Options.InsertionName} '{Options.ComponentBranchName}/{buildToInsert}' Insertion into {Options.VisualStudioBranchName}{suffix}";
    }

    private static async Task<GitPullRequest> CreateVSPullRequestAsync(string branchName, string message, string buildToInsert, string reviewerId, CancellationToken cancellationToken)
    {
        var gitClient = VisualStudioRepoConnection.GitClient;
        LogInformation($"Getting remote repository from {Options.VisualStudioBranchName} in {Options.VisualStudioRepoProjectName}");
        var repository = await gitClient.GetRepositoryAsync(project: Options.VisualStudioRepoProjectName, repositoryId: "VS", cancellationToken: cancellationToken);
        return await gitClient.CreatePullRequestAsync(
                CreatePullRequest("refs/heads/" + branchName, "refs/heads/" + Options.VisualStudioBranchName, message, buildToInsert, reviewerId),
                repository.Id,
                supportsIterations: null,
                userState: null,
                cancellationToken);
    }

    public static async Task<GitPullRequest> OverwritePullRequestAsync(int pullRequestId, string message, string buildToInsert, CancellationToken cancellationToken)
    {
        var gitClient = VisualStudioRepoConnection.GitClient;

        return await gitClient.UpdatePullRequestAsync(
            new GitPullRequest
            {
                Title = GetPullRequestTitle(buildToInsert),
                Description = message,
                IsDraft = Options.CreateDraftPr
            },
            VSRepoId,
            pullRequestId,
            cancellationToken: cancellationToken);
    }

    [Obsolete]
    public static async Task RetainComponentBuild(Build buildToInsert)
    {
        var buildClient = ComponentBuildConnection.BuildClient;

        LogInformation("Marking inserted build for retention.");
        buildToInsert.KeepForever = true;

        await buildClient.UpdateBuildAsync(buildToInsert);
    }

    private static async Task<IEnumerable<Build>> GetComponentBuildsAsync(BuildHttpClient buildClient, List<BuildDefinitionReference> definitions, CancellationToken cancellationToken, BuildResult? resultFilter = null)
    {
        IEnumerable<Build> builds = await GetComponentBuildsByBranchAsync(buildClient, definitions, Options.ComponentBranchName, resultFilter, cancellationToken);
        builds = builds.Concat(await GetComponentBuildsByBranchAsync(buildClient, definitions, "refs/heads/" + Options.ComponentBranchName, resultFilter, cancellationToken));
        return builds;
    }

    private static async Task<List<Build>> GetComponentBuildsByBranchAsync(BuildHttpClient buildClient, List<BuildDefinitionReference> definitions, string branchName, BuildResult? resultFilter, CancellationToken cancellationToken)
    {
        return await buildClient.GetBuildsAsync(
            project: Options.ComponentBuildProjectNameOrFallback,
            definitions: definitions.Select(d => d.Id),
            branchName: branchName,
            statusFilter: BuildStatus.Completed,
            resultFilter: resultFilter,
            cancellationToken: cancellationToken);
    }

    private static async Task<Build?> GetLatestComponentBuildAsync(CancellationToken cancellationToken, BuildResult? resultFilter = null)
    {
        var buildClient = ComponentBuildConnection.BuildClient;
        var definitions = await buildClient.GetDefinitionsAsync(project: Options.ComponentBuildProjectNameOrFallback, name: Options.ComponentBuildQueueName);
        var builds = await GetComponentBuildsAsync(buildClient, definitions, cancellationToken, resultFilter);

        return (await GetInsertableComponentBuildsAsync(buildClient,
                    from build in builds
                    orderby build.FinishTime descending
                    select build,
                    cancellationToken)).FirstOrDefault();
    }

    /// <summary>
    /// Insertable builds have valid artifacts and are not marked as 'DoesNotRequireInsertion_[TargetBranchName]'.
    /// </summary>
    private static async Task<List<Build>> GetInsertableComponentBuildsAsync(
        BuildHttpClient buildClient,
        IEnumerable<Build> builds,
        CancellationToken cancellationToken)
    {
        var buildsWithValidArtifacts = new List<Build>();
        foreach (var build in builds)
        {
            if (build.Tags?.Contains($"DoesNotRequireInsertion_{Options.VisualStudioBranchName}") == true)
            {
                continue;
            }

            // The artifact name passed to PublishBuildArtifacts task:
            var arcadeArtifactName = ArcadeInsertionArtifacts.ArtifactName;
            var legacyArtifactName = LegacyInsertionArtifacts.GetArtifactName(build.BuildNumber);

            var artifacts = await buildClient.GetArtifactsAsync(build.Project.Id, build.Id, cancellationToken: cancellationToken);
            if (artifacts.Any(a => a.Name == arcadeArtifactName || a.Name == legacyArtifactName))
            {
                buildsWithValidArtifacts.Add(build);
            }
        }
        return buildsWithValidArtifacts;
    }

    private static async Task<Build> GetLatestPassedComponentBuildAsync(CancellationToken cancellationToken)
    {
        // ********************* Verify Build Passed *****************************
        cancellationToken.ThrowIfCancellationRequested();
        Build? newestBuild;
        LogInformation($"Get Latest Passed Component Build");
        try
        {
            LogInformation($"Getting latest passing build for project {Options.ComponentBuildProjectNameOrFallback}, queue {Options.ComponentBuildQueueName}, and branch {Options.ComponentBranchName}");
            // Get the latest build with valid artifacts.
            newestBuild = await GetLatestComponentBuildAsync(cancellationToken, BuildResult.Succeeded | BuildResult.PartiallySucceeded);

            if (newestBuild?.Result == BuildResult.PartiallySucceeded)
            {
                LogWarning($"The latest build being used, {newestBuild.BuildNumber} has partially succeeded!");
            }
        }
        catch (Exception ex)
        {
            throw new IOException($"Unable to get latest build for '{Options.ComponentBuildQueueName}' from project '{Options.ComponentBuildProjectNameOrFallback}' in '{Options.ComponentBuildAzdoUri}': {ex.Message}");
        }

        if (newestBuild == null)
        {
            throw new IOException($"Unable to get latest build for '{Options.ComponentBuildQueueName}' from project '{Options.ComponentBuildProjectNameOrFallback}' in '{Options.ComponentBuildAzdoUri}'");
        }

        // ********************* Get New Build Version****************************
        cancellationToken.ThrowIfCancellationRequested();
        return newestBuild;
    }

    // Similar to: https://devdiv.visualstudio.com/DevDiv/_git/PostBuildSteps#path=%2Fsrc%2FSubmitPullRequest%2FProgram.cs&version=GBmaster&_a=contents
    private static async Task QueueVSBuildPolicy(GitPullRequest pullRequest, string buildPolicy)
    {
        var policyClient = VisualStudioRepoConnection.GetClient<PolicyHttpClient>();
        var repository = pullRequest.Repository;
        var timeout = TimeSpan.FromSeconds(30);
        var stopwatch = Stopwatch.StartNew();
        while (true)
        {
            var evaluations = await policyClient.GetPolicyEvaluationsAsync(repository.ProjectReference.Id, $"vstfs:///CodeReview/CodeReviewId/{repository.ProjectReference.Id}/{pullRequest.PullRequestId}");
            var evaluation = evaluations.FirstOrDefault(x =>
            {
                if (x.Configuration.Type.DisplayName.Equals("Build", StringComparison.OrdinalIgnoreCase))
                {
                    var policyName = x.Configuration.Settings["displayName"];
                    if (policyName != null)
                    {
                        return policyName.ToString().Equals(buildPolicy, StringComparison.OrdinalIgnoreCase);
                    }
                }

                return false;
            });

            if (evaluation != null)
            {
                await policyClient.RequeuePolicyEvaluationAsync(repository.ProjectReference.Id, evaluation.EvaluationId);
                LogInformation($"Started '{buildPolicy}' build policy on {pullRequest.Title}");
                break;
            }

            if (stopwatch.Elapsed > timeout)
            {
                throw new ArgumentException($"Cannot find a '{buildPolicy}' build policy in {pullRequest.Title}.");
            }
        }
    }

    private static async Task TryQueueVSBuildPolicy(GitPullRequest pullRequest, string buildPolicy, string insertionBranchName)
    {
        try
        {
            await QueueVSBuildPolicy(pullRequest, buildPolicy);
        }
        catch (Exception ex)
        {
            LogWarning($"Unable to start {buildPolicy} for '{insertionBranchName}'");
            LogWarning(ex);
        }
    }

    /// <summary>
    /// There is no enum or class in Microsoft.TeamFoundation.SourceControl.WebApi defined for vote values so made my own here.
    /// Values are documented at https://docs.microsoft.com/en-us/dotnet/api/microsoft.teamfoundation.sourcecontrol.webapi.identityrefwithvote.vote?view=azure-devops-dotnet.
    /// </summary>
    internal enum Vote : short
    {
        Approved = 10,
        ApprovedWithComment = 5,
        NoResponse = 0,
        NotReady = -5,
        Rejected = -10
    }

    // Similar to: https://devdiv.visualstudio.com/DevDiv/_git/PostBuildSteps#path=%2Fsrc%2FSubmitPullRequest%2FProgram.cs&version=GBmaster&_a=contents
    private static async Task SetAutoCompleteAsync(GitPullRequest pullRequest, string commitMessage, CancellationToken cancellationToken)
    {
        var gitClient = VisualStudioRepoConnection.GitClient;
        var repository = pullRequest.Repository;
        try
        {
            var idRefWithVote = await gitClient.CreatePullRequestReviewerAsync(
                new IdentityRefWithVote { Vote = (short)Vote.Approved },
                repository.Id,
                pullRequest.PullRequestId,
                DotNetBotUserId.ToString(),
                cancellationToken: cancellationToken
                );
            LogInformation($"Updated {pullRequest.Description} with AutoApprove");

            pullRequest = await gitClient.UpdatePullRequestAsync(
                new GitPullRequest
                {
                    AutoCompleteSetBy = idRefWithVote,
                    CompletionOptions = new GitPullRequestCompletionOptions
                    {
                        DeleteSourceBranch = true,
                        MergeCommitMessage = commitMessage,
                        SquashMerge = true,
                    }
                },
                repository.Id,
                pullRequest.PullRequestId,
                cancellationToken: cancellationToken
                );
            LogInformation($"Updated {pullRequest.Description} with AutoComplete");
        }
        catch (Exception e)
        {
            LogWarning($"Could not set AutoComplete: {e.GetType().Name} : {e.Message}");
            LogWarning(e);
        }
    }

    private static async Task<Build?> GetSpecificComponentBuildAsync(BuildVersion version, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        LogInformation($"Getting build with build number {version}");
        var buildClient = ComponentBuildConnection.BuildClient;

        var definitions = await buildClient.GetDefinitionsAsync(project: Options.ComponentBuildProjectNameOrFallback, name: Options.ComponentBuildQueueName, cancellationToken: cancellationToken);
        var builds = await buildClient.GetBuildsAsync(
            project: Options.ComponentBuildProjectNameOrFallback,
            definitions: definitions.Select(d => d.Id),
            buildNumber: version.ToString(),
            cancellationToken: cancellationToken);

        return (from build in builds
                where version == BuildVersion.FromTfsBuildNumber(build.BuildNumber, Options.ComponentBuildQueueName)
                orderby build.FinishTime descending
                select build).FirstOrDefault();
    }

    internal static async Task<InsertionArtifacts> GetInsertionArtifactsAsync(Build build, CancellationToken cancellationToken)
    {
        // used for local testing:
        if ((LegacyInsertionArtifacts.TryCreateFromLocalBuild(Options.BuildDropPath, out var artifacts) ||
            ArcadeInsertionArtifacts.TryCreateFromLocalBuild(Options.BuildDropPath, out artifacts)) && artifacts is not null)
        {
            return artifacts;
        }

        var buildClient = ComponentBuildConnection.BuildClient;
        Debug.Assert(ReferenceEquals(build,
            (await GetInsertableComponentBuildsAsync(buildClient, [build], cancellationToken)).Single()));

        // Pull the VSSetup directory from artifacts store.
        var buildArtifacts = await buildClient.GetArtifactsAsync(build.Project.Id, build.Id, cancellationToken: cancellationToken);

        // The artifact name passed to PublishBuildArtifacts task:
        var arcadeArtifactName = ArcadeInsertionArtifacts.ArtifactName;
        var legacyArtifactName = LegacyInsertionArtifacts.GetArtifactName(build.BuildNumber);

        foreach (var artifact in buildArtifacts)
        {
            if (artifact.Name == arcadeArtifactName)
            {
                var artifactType = artifact.Resource.Type;
                // artifact.Resource.Data should be available and non-null due to BuildWithValidArtifactsAsync,
                // which checks this precondition
                if (!artifactType.Equals("container", StringComparison.OrdinalIgnoreCase) &&
                    !artifactType.Equals("pipelineArtifact", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"Could not find artifact '{arcadeArtifactName}' associated with build '{build.Id}'");
                }

                return new ArcadeInsertionArtifacts(await DownloadBuildArtifactsAsync(buildClient, build, artifact, cancellationToken));
            }
            else if (artifact.Name == legacyArtifactName)
            {
                // artifact.Resource.Data should be available and non-null due to BuildWithValidArtifactsAsync,
                // which checks this precondition
                if (string.Compare(artifact.Resource.Type, "container", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    // This is a build where the artifacts are published to the artifacts server instead of a UNC path.
                    // Download this artifacts to a temp folder and provide that path instead.
                    return new LegacyInsertionArtifacts(await DownloadBuildArtifactsAsync(buildClient, build, artifact, cancellationToken));
                }

                return new LegacyInsertionArtifacts(Path.Combine(artifact.Resource.Data, build.BuildNumber));
            }
        }

        // Should never happen since we already filtered for containing valid paths
        throw new InvalidOperationException("Could not find drop path");
    }

    private static async Task<string> DownloadBuildArtifactsAsync(BuildHttpClient buildClient, Build build, BuildArtifact artifact, CancellationToken cancellationToken)
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);

        var archiveDownloadPath = Path.Combine(tempDirectory, artifact.Name);
        LogInformation($"Downloading artifacts to {archiveDownloadPath}");

        var artifactType = artifact.Resource.Type;
        Debug.Assert(artifactType.Equals("container", StringComparison.OrdinalIgnoreCase) || artifactType.Equals("pipelineArtifact", StringComparison.OrdinalIgnoreCase));

        var watch = Stopwatch.StartNew();
        if (artifactType.Equals("container", StringComparison.OrdinalIgnoreCase))
        {
            using var contentStream = await buildClient.GetArtifactContentZipAsync(Options.ComponentBuildProjectNameOrFallback, build.Id, artifact.Name, cancellationToken: cancellationToken);
            await ExtractToDirectoryAsync(contentStream, tempDirectory).ConfigureAwait(false);
            LogInformation($"Artifact download took {watch.ElapsedMilliseconds / 1000} seconds");
            return archiveDownloadPath;
        }
        else
        {
            // When the published by Publish artifacts pipeline task, buildClient.GetArtifactContentZipAsync() is unable to get the content of the artifacts.
            // See https://developercommunity.visualstudio.com/t/exception-is-being-thrown-for-getartifactcontentzi/1270336
            // It recommended to use http client to download the payload directly from the Url.
            var downloadUrl = artifact.Resource.DownloadUrl;
            using var response = await ComponentBuildConnection.HttpClient.GetAsync(downloadUrl, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            await ExtractToDirectoryAsync(contentStream, tempDirectory).ConfigureAwait(false);
            LogInformation($"Artifact download took {watch.ElapsedMilliseconds / 1000} seconds");
            return archiveDownloadPath;
        }

        static async Task ExtractToDirectoryAsync(Stream contentStream, string tempDirectory)
        {
            using var ms = new MemoryStream();
            await contentStream.CopyToAsync(ms).ConfigureAwait(false);
            using var archive = new ZipArchive(ms);
            archive.ExtractToDirectory(tempDirectory);
        }
    }

    private static Component[] GetLatestBuildComponents(InsertionArtifacts buildArtifacts)
    {
        var components = Directory.EnumerateFiles(buildArtifacts.RootDirectory, "*.vsman", SearchOption.AllDirectories)
            .Select(GetComponentFromManifestFile)
            .OfType<Component>()
            .ToArray();

        var distinctComponents = components.Select(c => c.Name).Distinct();
        if (distinctComponents.Count() != components.Length)
        {
            LogWarning($"Found duplicate component vsman files in the build artifacts: {string.Join(", ", components.Select(c => $"{c.Name}:{c.Filename}"))}");
        }

        return components;
    }

    private static Component? GetComponentFromManifestFile(string filePath)
    {
        LogInformation($"GetComponentFromManifestFile: Opening manifest from {filePath}.");
        var fileName = Path.GetFileName(filePath);
        var manifestJson = File.ReadAllText(filePath);

        var manifest = JsonConvert.DeserializeAnonymousType(manifestJson,
            new
            {
                info = new
                {
                    manifestName = "",
                    buildVersion = ""
                },
                packages = new[]
                {
                    new
                    {
                        payloads = new[]
                        {
                            new
                            {
                                url = ""
                            }
                        }
                    }
                }
            });

        // Find the first package payload where the url is in the expected format `http://{drop url};{filename}`
        var payload = manifest?.packages
            .Where(package => package?.payloads is not null)
            .SelectMany(package => package.payloads)
            .FirstOrDefault(payload => payload?.url?.Contains(";") == true);

        if (payload is null)
        {
            LogInformation($"GetComponentFromManifestFile: Manifest {filePath} did not contain an insertable component.");
            return null;
        }

        // Everything is uploaded to the same drop, so we can take the url of a package and generate the manifest url.
        var url = new Uri($"{payload.url.Split(';')[0]};{fileName}");
        if (manifest?.info is null)
        {
            LogInformation($"GetComponentFromManifestFile: Manifest {filePath} did not contain valid info metadata.");
            return null;
        }

        return new Component(manifest.info.manifestName, fileName, url, manifest.info.buildVersion);
    }

    internal static async Task<(List<GitCommit> changes, string diffLink)> GetChangesBetweenBuildsAsync(Build fromBuild, Build tobuild)
    {
        var repoId = !string.IsNullOrEmpty(Options.ComponentGitHubRepoName)
            ? Options.ComponentGitHubRepoName
            : tobuild.Repository.Id; // e.g. dotnet/roslyn when GitHub, 7b863b8d-8cc3-431d-b06b-7136cc32bbe6 when AzDO

        var fromSHA = fromBuild.SourceVersion;
        var toSHA = tobuild.SourceVersion;

        if (tobuild.Repository.Type == "GitHub" || !string.IsNullOrEmpty(Options.ComponentGitHubRepoName))
        {
            return await GetChangesBetweenBuildsFromGitHubAsync(repoId, fromSHA, toSHA);
        }
        else if (tobuild.Repository.Type == "TfsGit")
        {
            return await GetChangesBetweenBuildsFromAzDOAsync(tobuild, repoId, fromSHA, toSHA);
        }

        throw new NotSupportedException("Only builds created from GitHub & AzDO repos support enumerating commits.");
    }

    private static async Task<(List<GitCommit> changes, string diffLink)> GetChangesBetweenBuildsFromAzDOAsync(Build tobuild, string repoId, string fromSHA, string toSHA)
    {
        var gitClient = ComponentBuildConnection.GitClient;
        var project = Options.ComponentBuildProjectNameOrFallback;
        var getCommits = (await gitClient.GetCommitsAsync(
            project,
            repoId,
            new GitQueryCommitsCriteria()
            {
                ItemVersion = new GitVersionDescriptor() { Version = fromSHA, VersionType = GitVersionType.Commit },
                CompareVersion = new GitVersionDescriptor() { Version = toSHA, VersionType = GitVersionType.Commit }
            }))
            // AzDO does not provide the full commit message, so we must query for each commit to provide better messages for PR merge commits.
            .Select(c => gitClient.GetCommitAsync(project, c.CommitId, repoId));
        var commits = (await Task.WhenAll(getCommits))
            .Select(c =>
                new GitCommit()
                {
                    Author = c.Author.Name,
                    Committer = c.Committer.Name,
                    CommitDate = c.Committer.Date,
                    Message = c.Comment,
                    CommitId = c.CommitId,
                    RemoteUrl = ((ReferenceLink)c.Links.Links["web"]).Href
                })
            .ToList();

        // AzDO does not have a UI for comparing two commits. Instead generate the REST API call to retrieve commits between two SHAs.
        return (commits, $"{tobuild.Repository.Url.OriginalString.Replace("_git", "_apis/git/repositories")}/commits?searchCriteria.itemVersion.version={fromSHA}&searchCriteria.itemVersion.versionType=commit&searchCriteria.compareVersion.version={toSHA}&searchCriteria.compareVersion.versionType=commit");
    }

    private static async Task<(List<GitCommit> changes, string diffLink)> GetChangesBetweenBuildsFromGitHubAsync(string repoId, string fromSHA, string toSHA)
    {
        var restEndpoint = $"https://api.github.com/repos/{repoId}/compare/{fromSHA}...{toSHA}";
        var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Get, restEndpoint);
        request.Headers.Add("User-Agent", "RoslynInsertionTool");

        var response = await client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        // https://developer.github.com/v3/repos/commits/
        var data = JsonConvert.DeserializeAnonymousType(content,
            new
            {
                commits = new[]
                {
                    new
                    {
                        sha = "",
                        commit = new
                        {
                            author = new
                            {
                                name = "",
                                email = "",
                                date = ""
                            },
                            committer = new
                            {
                                name = ""
                            },
                            message = ""
                        },
                        html_url = ""
                    }
                }
            });

        var result = (data?.commits ?? Array.Empty<dynamic>())
            .Select(d =>
                new GitCommit()
                {
                    Author = d.commit.author.name,
                    Committer = d.commit.committer.name,
                    CommitDate = DateTime.Parse(d.commit.author.date),
                    Message = d.commit.message,
                    CommitId = d.sha,
                    RemoteUrl = d.html_url
                })
            // show HEAD first, base last
            .Reverse()
            .ToList();

        return (result, $"//github.com/{repoId}/compare/{fromSHA}...{toSHA}?w=1");
    }

    internal static async Task<string> AppendChangesToDescriptionAsync(string prDescription, Build oldBuild, List<PRFinder.GitCommit> changes)
    {
        const int HardLimit = 4000; // Azure DevOps limitation

        if (changes.Count == 0)
        {
            return prDescription;
        }

        var description = new StringBuilder(prDescription + Environment.NewLine);

        var repoId = !string.IsNullOrEmpty(Options.ComponentGitHubRepoName)
            ? Options.ComponentGitHubRepoName
            : oldBuild.Repository.Id; // e.g. dotnet/roslyn when GitHub, 7b863b8d-8cc3-431d-b06b-7136cc32bbe6 when AzDO

        if (Options.Product?.TryGetHost(Connections, Logger, out var host) != true)
        {
            if (oldBuild.Repository.Type == "GitHub" || !string.IsNullOrEmpty(Options.ComponentGitHubRepoName))
            {
                host = new GitHub($"//github.com/{repoId}", Connections, Logger);
            }
            else if (oldBuild.Repository.Type == "TfsGit")
            {
                host = new PRFinder.Hosts.Azure(oldBuild.Repository.Url.AbsoluteUri);
            }
            else
            {
                return prDescription;
            }
        }

        var formatter = new PRFinder.Formatters.DefaultFormatter();
        await PRFinder.PRFinder.AppendChangesToDescriptionAsync(changes, host!, formatter, [], description);

        var result = description.ToString();
        if (result.Length > HardLimit)
        {
            LogInformation($"PR description is {result.Length} characters long, but the limit is {HardLimit}.");
            LogInformation("Full description before truncation:");
            LogInformation(result);

            result = TruncateDesciptionIfNeeded(result, HardLimit);
        }

        return result;
    }

    private static string TruncateDesciptionIfNeeded(string description, int hardLimit)
    {
        const string LimitMessage = "Changelog truncated due to description length limit.";

        if (description.Length <= hardLimit)
            return description;

        var lastIndexOfNewLine = hardLimit;

        while (lastIndexOfNewLine > 0 && lastIndexOfNewLine + LimitMessage.Length + Environment.NewLine.Length + 1 > hardLimit)
        {
            lastIndexOfNewLine = description.LastIndexOf(Environment.NewLine, lastIndexOfNewLine);
        }

        if (lastIndexOfNewLine >= 0)
        {
            return string.Concat(description.AsSpan(0, lastIndexOfNewLine + Environment.NewLine.Length), LimitMessage);
        }

        return LimitMessage;
    }

    public static string GetGitHubPullRequestUrl(string repoURL, string prNumber)
        => PRFinder.Hosts.GitHub.GetPullRequestUrl(repoURL, prNumber);
}
