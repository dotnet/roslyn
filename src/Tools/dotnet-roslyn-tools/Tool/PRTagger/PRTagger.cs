// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System.Collections.Immutable;
using System.Net.Http.Headers;
using System.Text;
using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using Microsoft.RoslynTools.Authentication;
using Microsoft.RoslynTools.Extensions;
using Microsoft.RoslynTools.Products;
using Microsoft.RoslynTools.Utilities;
using Microsoft.RoslynTools.VS;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Newtonsoft.Json;
using Octokit;
using Repository = LibGit2Sharp.Repository;

namespace Microsoft.RoslynTools.PRTagger;

internal static class PRTagger
{
    private const string InsertionLabel = "vs-insertion";

    /// <summary>
    /// Creates GitHub issues containing the PRs inserted into a given VS build.
    /// </summary>
    /// <param name="vsBuild">VS build number</param>
    /// <param name="vsCommitSha">Commit SHA for VS build</param>
    /// <param name="settings">Authentication tokens</param>
    /// <param name="logger"></param>
    /// <returns>Exit code indicating whether issue was successfully created.</returns>
    public static async Task<int> TagPRs(
        ImmutableArray<(string vsBuild, string vsCommitSha, string previousVsCommitSha)> vsBuildsAndCommitSha,
        RoslynToolsSettings settings,
        AzDOConnection devdivConnection,
        GitHubClient gitHubClient,
        ILogger logger)
    {
        using var dncengConnection = new AzDOConnection(settings.DncEngAzureDevOpsBaseUri, "internal", settings.DncEngAzureDevOpsToken);
        // vsBuildsAndCommitSha is ordered from new to old.
        // For each of the product, check if the product is changed from the newest build, keep creating issues if the product has change.
        // Stop when
        // 1. All the VS build has been checked.
        // 2. If some errors happens.
        // 3. If we found the issue with the same title has been created. It means the issue is created because the last run of the tagger.
        foreach (var product in VSBranchInfo.AllProducts)
        {
            foreach (var (vsBuild, vsCommitSha, previousVsCommitSha) in vsBuildsAndCommitSha)
            {
                var result = await TagProductAsync(product, logger, vsCommitSha, vsBuild, previousVsCommitSha, settings, devdivConnection, dncengConnection, gitHubClient).ConfigureAwait(false);
                if (result is TagResult.Failed or TagResult.IssueAlreadyCreated)
                {
                    break;
                }
            }
        }

        return 0;
    }

    private static async Task<TagResult> TagProductAsync(
        IProduct product, ILogger logger, string vsCommitSha, string vsBuild, string previousVsCommitSha, RoslynToolsSettings settings, AzDOConnection devdivConnection, AzDOConnection dncengConnection, GitHubClient gitHubClient)
    {
        var connections = new[] { devdivConnection, dncengConnection };
        // We currently only support creating issues for GitHub repos
        if (!product.RepoHttpBaseUrl.Contains("github.com"))
        {
            return TagResult.Failed;
        }

        var gitHubRepoName = product.RepoHttpBaseUrl.Split('/').Last();
        logger.LogInformation($"GitHub repo: {gitHubRepoName}");

        // Get associated product build for current and previous VS commit SHAs
        var currentBuild = await TryGetBuildNumberForReleaseAsync(product.ComponentJsonFileName, product.ComponentName, vsCommitSha, devdivConnection, logger).ConfigureAwait(false);
        var previousBuild = await TryGetBuildNumberForReleaseAsync(product.ComponentJsonFileName, product.ComponentName, previousVsCommitSha, devdivConnection, logger).ConfigureAwait(false);

        if (currentBuild is null)
        {
            logger.LogError($"{gitHubRepoName} build not found for VS commit SHA {currentBuild}.");
            return TagResult.Failed;
        }

        if (previousBuild is null)
        {
            logger.LogError($"{gitHubRepoName} build not found for VS commit SHA {previousBuild}.");
            return TagResult.Failed;
        }

        // If builds are the same, there are no PRs to tag
        if (currentBuild.Equals(previousBuild))
        {
            logger.LogInformation($"No PRs found to tag; {gitHubRepoName} build numbers are equal: {currentBuild}.");
            return TagResult.NoChangeBetweenVSBuilds;
        }

        // Get commit SHAs for product builds
        var previousProductCommitSha = await TryGetProductCommitShaFromBuildAsync(product, connections, previousBuild, logger).ConfigureAwait(false);
        var currentProductCommitSha = await TryGetProductCommitShaFromBuildAsync(product, connections, currentBuild, logger).ConfigureAwait(false);

        if (previousProductCommitSha is null || currentProductCommitSha is null)
        {
            logger.LogError($"Error retrieving {gitHubRepoName} commit SHAs.");
            return TagResult.Failed;
        }

        logger.LogInformation($"Finding PRs between {gitHubRepoName} commit SHAs {previousProductCommitSha} and {currentProductCommitSha}.");

        // Retrieve GitHub repo
        string? gitHubRepoPath;
        try
        {
            gitHubRepoPath = Environment.CurrentDirectory + "\\" + gitHubRepoName;
            if (!Repository.IsValid(gitHubRepoPath))
            {
                logger.LogInformation("Cloning GitHub repo...");
                gitHubRepoPath = Repository.Clone(product.RepoHttpBaseUrl, workdirPath: gitHubRepoPath);
            }
            else
            {
                logger.LogInformation($"Repo already exists at {gitHubRepoPath}");
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"Exception while cloning repo: " + ex);
            return TagResult.Failed;
        }

        // Find PRs between product commit SHAs
        var prDescription = new StringBuilder();
        var isSuccess = PRFinder.PRFinder.FindPRs(previousProductCommitSha, currentProductCommitSha, PRFinder.PRFinder.DefaultFormat, logger, gitHubRepoPath, prDescription);
        if (isSuccess != 0)
        {
            // Error occurred; should be logged in FindPRs method
            return TagResult.Failed;
        }

        var issueTitle = $"[Automated] PRs inserted in VS build {vsBuild}";
        var hasIssueAlreadyCreated = await HasIssueAlreadyCreatedAsync(gitHubClient, gitHubRepoName, issueTitle).ConfigureAwait(false);
        if (hasIssueAlreadyCreated)
        {
            logger.LogInformation($"Issue with name: {issueTitle} exists in repo: {gitHubRepoName}. Skip creation.");
            return TagResult.IssueAlreadyCreated;
        }

        logger.LogInformation($"Creating issue...");

        // Create issue
        await TryCreateIssueAsync(gitHubClient, issueTitle, gitHubRepoName, prDescription.ToString(), logger).ConfigureAwait(false);
        return TagResult.Succeed;
    }

    public static async Task<ImmutableArray<(string vsBuild, string vsCommit, string previousVsCommitSha)>> GetVSBuildsAndCommitsAsync(
        AzDOConnection devdivConnection,
        ILogger logger,
        int vsBuildNumber,
        CancellationToken cancellationToken)
    {
        var builds = await devdivConnection.TryGetBuildsAsync(
            "DD-CB-TestSignVS",
            logger: logger,
            maxBuildNumberFetch: vsBuildNumber,
            resultsFilter: BuildResult.Succeeded,
            buildQueryOrder: BuildQueryOrder.FinishTimeDescending).ConfigureAwait(false);
        var vsRepository = await GetVSRepositoryAsync(devdivConnection.GitClient);
        if (builds is not null)
        {
            // Find previous VS commit SHA
            var buildInfoTask = builds.Select(async build =>
            {
                var vsCommit = await devdivConnection.GitClient.GetCommitAsync(
                    build.SourceVersion, vsRepository.Id, cancellationToken: cancellationToken).ConfigureAwait(false);
                var previousVsCommitSha = vsCommit.Parents.First();
                return (build.BuildNumber, build.SourceVersion, previousVsCommitSha);
            });

            var vsBuildAndCommitSha = await Task.WhenAll(buildInfoTask).ConfigureAwait(false);
            return vsBuildAndCommitSha.ToImmutableArray();
        }
        else
        {
            return ImmutableArray<(string vsBuild, string vsCommit, string previousVsCommitSha)>.Empty;
        }
    }

    private static async Task<GitRepository> GetVSRepositoryAsync(GitHttpClient gitClient)
    {
        return await gitClient.GetRepositoryAsync("DevDiv", "VS");
    }

    private static async Task<string?> TryGetBuildNumberForReleaseAsync(
        string componentJsonFileName,
        string componentName,
        string vsCommitSha,
        AzDOConnection vsConnection,
        ILogger logger)
    {
        var url = await VisualStudioRepository.GetUrlFromComponentJsonFileAsync(
            vsCommitSha, GitVersionType.Commit, vsConnection, componentJsonFileName, componentName);
        if (url is null)
        {
            logger.LogError($"Could not retrieve URL from component JSON file.");
            return null;
        }

        try
        {
            var buildNumber = VisualStudioRepository.GetBuildNumberFromUrl(url);
            logger.LogInformation($"Retrieved build number from URL: {buildNumber}");
            return buildNumber;
        }
        catch (Exception ex)
        {
            logger.LogError($"Error retrieving build number from URL: {ex}");
            return null;
        }
    }

    private static async Task<string?> TryGetProductCommitShaFromBuildAsync(
        IProduct product,
        AzDOConnection[] connections,
        string buildNumber,
        ILogger logger)
    {
        foreach (var connection in connections)
        {
            var buildPipelineName = product.GetBuildPipelineName(connection.BuildProjectName);
            logger.LogInformation($"Build pipeline name: {buildPipelineName}");
            if (buildPipelineName is not null)
            {
                var build = (await connection.TryGetBuildsAsync(buildPipelineName, buildNumber, logger))?.SingleOrDefault();
                if (build is not null)
                {
                    logger.LogInformation($"Build source version: {build.SourceVersion}");
                    return build.SourceVersion;
                }
            }
        }

        return null;
    }

    private static async Task TryCreateIssueAsync(
        string title,
        string gitHubRepoName,
        string issueBody,
        string gitHubToken,
        ILogger logger)
    {
        var client = new HttpClient
        {
            BaseAddress = new("https://api.github.com/")
        };

        var authArray = Encoding.ASCII.GetBytes($"{gitHubToken}");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(authArray));
        client.DefaultRequestHeaders.Add(
            "user-agent",
            "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2;)");

        // https://docs.github.com/en/rest/issues/issues#create-an-issue
        var response = await client.PostAsyncAsJson($"repos/dotnet/{gitHubRepoName}/issues", JsonConvert.SerializeObject(
            new
            {
                title = title,
                body = issueBody,
                labels = new string[] { "vs-insertion" }
            }));

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError($"Issue creation failed with status code: {response.StatusCode}");
        }

        logger.LogInformation("Successfully created issue.");
    }

    /// <summary>
    /// Check if the issue with <param name="title"/> exists in repo.
    /// </summary>
    private static async Task<bool> HasIssueAlreadyCreatedAsync(
        GitHubClient client,
        string repoName,
        string title)
    {
        var searchRequest = new SearchIssuesRequest(title)
        {
            Type = IssueTypeQualifier.Issue,
            Labels = new[] { InsertionLabel },
            Repos = new RepositoryCollection{ {"dotnet", repoName} },
            In = new[] { IssueInQualifier.Title }
        };

        var searchIssueResult = await client.Search.SearchIssues(searchRequest).ConfigureAwait(false);
        return searchIssueResult.TotalCount != 0;
    }
}
