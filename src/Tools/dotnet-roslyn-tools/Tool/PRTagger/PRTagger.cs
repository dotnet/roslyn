// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System.Collections.Immutable;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.RoslynTools.Extensions;
using Microsoft.RoslynTools.Products;
using Microsoft.RoslynTools.Utilities;
using Microsoft.RoslynTools.VS;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Newtonsoft.Json;
using Repository = LibGit2Sharp.Repository;

namespace Microsoft.RoslynTools.PRTagger;

internal static class PRTagger
{
    private const string InsertionLabel = "vs-insertion";

    /// <summary>
    /// Creates GitHub issues containing the PRs inserted into a given VS build.
    /// </summary>
    /// <param name="vsBuild">VS build number. Use if the tagger is invoked to check from a specify vs build.</param>
    /// <param name="logger"></param>
    /// <returns>Exit code indicating whether issue was successfully created.</returns>
    public static async Task<int> TagPRs(
        RemoteConnections remoteConnections,
        ILogger logger,
        int maxFetchingVSBuildNumber,
        string? vsBuild)
    {
        var cancellationToken = CancellationToken.None;
        var builds = await remoteConnections.DevDivConnection.TryGetBuildsAsync(
            "DD-CB-TestSignVS",
            logger: logger,
            buildNumber: vsBuild,
            maxFetchingVsBuildNumber: maxFetchingVSBuildNumber,
            buildQueryOrder: BuildQueryOrder.FinishTimeDescending).ConfigureAwait(false);

        if (builds is null)
        {
            logger.LogError("Failed to fetch builds from DevDiv.");
            return -1;
        }

        var vsBuildsAndCommitSha =
            await GetVsBuildCommitShaAsync(remoteConnections.DevDivConnection, builds.ToImmutableArray(), cancellationToken).ConfigureAwait(false);

        // vsBuildsAndCommitSha is ordered from new to old.
        // For each of the product, check if the product is changed from the newest build, keep creating issues if the product has change.
        // Stop when
        // 1. All the VS build has been checked.
        // 2. If some errors happens.
        // 3. If we found the issue with the same title has been created. It means the issue is created because the last run of the tagger.
        foreach (var product in VSBranchInfo.AllProducts)
        {
            // We currently only support creating issues for GitHub repos
            if (!product.IsGitHubRepo())
            {
                logger.LogWarning($"Only GitHub repos are supported. Skipped repo: {product.Name}");
                continue;
            }

            var gitHubRepoName = product.RepoHttpBaseUrl.Split('/').Last();
            var buildsAndCommitsToTag = await GetVSBuildsAndCommitsAsync(
                gitHubRepoName,
                vsBuildsAndCommitSha,
                remoteConnections.GitHubClient,
                logger).ConfigureAwait(false);

            foreach (var (buildNumber, vsCommitSha, previousVsCommitSha) in buildsAndCommitsToTag)
            {
                var result = await TagProductAsync(product, gitHubRepoName, logger, vsCommitSha, buildNumber, previousVsCommitSha, remoteConnections).ConfigureAwait(false);
                if (result is TagResult.Failed or TagResult.IssueAlreadyCreated)
                {
                    break;
                }
            }
        }

        return 0;
    }

    private static async Task<TagResult> TagProductAsync(
        IProduct product, string gitHubRepoName, ILogger logger, string vsCommitSha, string vsBuild, string previousVsCommitSha, RemoteConnections remoteConnections)
    {
        var connections = new[] { remoteConnections.DevDivConnection, remoteConnections.DncEngConnection };
        logger.LogInformation($"GitHub repo: {gitHubRepoName}");

        var issueTitle = $"[Automated] PRs inserted in VS build {vsBuild}";
        var hasIssueAlreadyCreated = await HasIssueAlreadyCreatedAsync(remoteConnections.GitHubClient, gitHubRepoName, issueTitle, logger).ConfigureAwait(false);
        if (hasIssueAlreadyCreated)
        {
            logger.LogInformation($"Issue with name: {issueTitle} exists in repo: {gitHubRepoName}. Skip creation.");
            return TagResult.IssueAlreadyCreated;
        }

        // Get associated product build for current and previous VS commit SHAs
        var currentBuild = await TryGetBuildNumberForReleaseAsync(product.ComponentJsonFileName, product.ComponentName, vsCommitSha, remoteConnections.DevDivConnection, logger).ConfigureAwait(false);
        var previousBuild = await TryGetBuildNumberForReleaseAsync(product.ComponentJsonFileName, product.ComponentName, previousVsCommitSha, remoteConnections.DevDivConnection, logger).ConfigureAwait(false);

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
        var isSuccess = await PRFinder.PRFinder.FindPRsAsync(previousProductCommitSha, currentProductCommitSha, path: null, PRFinder.PRFinder.DefaultFormat, logger, gitHubRepoPath, prDescription);
        if (isSuccess != 0)
        {
            // Error occurred; should be logged in FindPRs method
            return TagResult.Failed;
        }


        logger.LogInformation($"Creating issue...");

        // Create issue
        return await TryCreateIssueAsync(remoteConnections.GitHubClient, issueTitle, gitHubRepoName, prDescription.ToString(), logger).ConfigureAwait(false);
    }

    private static async Task<(string vsBuild, string vsCommit, string previousVsCommitSha)> GetVsBuildAndCommitAsync(
        AzDOConnection devdivConnection,
        ILogger logger,
        string? vsbuild,
        CancellationToken cancellationToken)
    {
        var builds = await devdivConnection.TryGetBuildsAsync(
            "DD-CB-TestSignVS",
            buildNumber: vsbuild,
            logger: logger,
            resultsFilter: BuildResult.Succeeded,
            buildQueryOrder: BuildQueryOrder.FinishTimeDescending).ConfigureAwait(false);
        if (builds is { Count: not 1 })
        {
            var build = builds.Single();
            var vsRepository = await GetVSRepositoryAsync(devdivConnection.GitClient);
            var vsCommit = await devdivConnection.GitClient.GetCommitAsync(
                build.SourceVersion, vsRepository.Id, cancellationToken: cancellationToken).ConfigureAwait(false);
            var previousVsCommitSha = vsCommit.Parents.First();
            return (build.BuildNumber, build.SourceVersion, previousVsCommitSha);
        }
        else
        {
            throw new ArgumentException($"Can't find {vsbuild} number in pipeline.");
        }
    }

    private static async Task<ImmutableArray<(string vsBuild, string vsCommit, string previousVsCommitSha)>> GetVsBuildCommitShaAsync(
        AzDOConnection devdivConnection,
        ImmutableArray<Build> builds,
        CancellationToken cancellationToken)
    {
        var vsRepository = await GetVSRepositoryAsync(devdivConnection.GitClient);
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

    private static async Task<ImmutableArray<(string vsBuild, string vsCommit, string previousVsCommitSha)>> GetVSBuildsAndCommitsAsync(
        string repoName,
        ImmutableArray<(string vsBuild, string vsCommit, string previousVsCommitSha)> buildsAndCommitSha,
        HttpClient gitHubClient,
        ILogger logger)
    {
        var lastVsBuildNumberReported = await FindTheLastReportedVSBuildAsync(gitHubClient, repoName, logger).ConfigureAwait(false);
        if (lastVsBuildNumberReported is not null)
        {
            logger.LogInformation($"Last reported VS build number: {lastVsBuildNumberReported}.");
        }
        else
        {
            logger.LogInformation($"Can't find the last reported VS Build info in {repoName}.");
        }

        var buildAndCommitShaList = buildsAndCommitSha.ToList();
        var builds = buildsAndCommitSha;
        if (lastVsBuildNumberReported is not null)
        {
            var lastReportedBuildIndex = buildAndCommitShaList.FindIndex(buildAndCommitSha => buildAndCommitSha.vsBuild == lastVsBuildNumberReported);
            if (lastReportedBuildIndex == -1)
            {
                logger.LogWarning($"VS build: {lastVsBuildNumberReported} can't be found in the build list.");
            }
            else
            {
                builds = buildAndCommitShaList.Take(lastReportedBuildIndex).ToImmutableArray();
            }
        }

        return builds;
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

    private static async Task<TagResult> TryCreateIssueAsync(
        HttpClient client,
        string title,
        string gitHubRepoName,
        string issueBody,
        ILogger logger)
    {
        // https://docs.github.com/en/rest/issues/issues#create-an-issue
        var response = await client.PostAsyncAsJson($"repos/dotnet/{gitHubRepoName}/issues", JsonConvert.SerializeObject(
            new
            {
                title = title,
                body = issueBody,
                labels = new string[] { InsertionLabel }
            }));

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError($"Issue creation failed with status code: {response.StatusCode}");
            return TagResult.Failed;
        }

        logger.LogInformation("Successfully created issue.");
        return TagResult.Succeed;
    }

    /// <summary>
    /// Check if the issue with <param name="title"/> exists in repo.
    /// </summary>
    private static async Task<bool> HasIssueAlreadyCreatedAsync(
        HttpClient client,
        string repoName,
        string title,
        ILogger logger)
    {
        // This method would be called frequently. GitHub suggests to wait at least one sec to avoid the rate limit issue.
        await Task.Delay(3000);
        var response = await SearchIssuesOnGitHubAsync(client, repoName, logger, title: title, label: InsertionLabel).ConfigureAwait(false);
        var issueNumber = TotalCountNumber(response);
        return issueNumber != 0;
    }

    private static async Task<string?> FindTheLastReportedVSBuildAsync(
        HttpClient client,
        string repoName,
        ILogger logger)
    {
        var jsonResponse = await SearchIssuesOnGitHubAsync(client, repoName, logger, label: InsertionLabel).ConfigureAwait(false);
        var totalCountNumber = TotalCountNumber(jsonResponse);
        if (totalCountNumber == 0)
        {
            logger.LogInformation($"No existing issue has been found for repo: {repoName}.");
            return null;
        }

        // 'Items' is required in response schema.
        // https://docs.github.com/en/rest/search/search?apiVersion=2022-11-28
        var lastReportedIssue = jsonResponse["items"]!.AsArray().First();
        var lastReportedIssueTitle = lastReportedIssue!["title"]!.ToString();
        return lastReportedIssueTitle["[Automated] PRs inserted in VS build".Length..];

    }

    private static int TotalCountNumber(JsonNode response)
    {
        // https://docs.github.com/en/rest/search/search?apiVersion=2022-11-28
        // total_count is required in response schema
        return int.Parse(response["total_count"]!.ToString());
    }

    /// <summary>
    /// Search issues by using <param name="title"/> and <param name="label"/> in <param name="repoName"/>
    /// By default this is ordered from new to old. See https://docs.github.com/en/rest/search/search?apiVersion=2022-11-28#ranking-search-results
    /// </summary>
    /// <param name="client"></param>
    /// <param name="repoName"></param>
    /// <param name="logger"></param>
    /// <param name="title"></param>
    /// <param name="label"></param>
    /// <returns></returns>
    private static async Task<JsonNode> SearchIssuesOnGitHubAsync(
        HttpClient client,
        string repoName,
        ILogger logger,
        string? title = null,
        string? label = null)
    {
        // If title and label are both null, there is nothing to search.
        if (title is null && label is null)
        {
            throw new ArgumentException($"$title and label are both null.");
        }

        var queryBuilder = new StringBuilder();
        queryBuilder.Append("search/issues?q=");
        if (title is not null)
        {
            queryBuilder.Append($"{title}+");
        }

        if (label is not null)
        {
            queryBuilder.Append($"label:{label}+");
        }

        queryBuilder.Append($"is:issue+repo:dotnet/{repoName}");
        var query = queryBuilder.ToString();

        logger.LogInformation($"Searching query is {query}.");
        var response = await client.GetAsync(query).ConfigureAwait(false);
        var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        var jsonResponseContent = JsonObject.Parse(content)!;
        logger.LogInformation($"Response object is {jsonResponseContent.ToJsonString()}.");
        return jsonResponseContent;
    }
}
