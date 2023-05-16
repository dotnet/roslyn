// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

namespace Microsoft.RoslynTools.PRTagger;

using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using Microsoft.RoslynTools.Authentication;
using Microsoft.RoslynTools.Extensions;
using Microsoft.RoslynTools.Products;
using Microsoft.RoslynTools.Utilities;
using Microsoft.RoslynTools.VS;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

internal class PRTagger
{
    /// <summary>
    /// Creates GitHub issues containing the PRs inserted into a given VS build.
    /// </summary>
    /// <param name="vsBuild">VS build number</param>
    /// <param name="vsCommitSha">Commit SHA for VS build</param>
    /// <param name="settings">Authentication tokens</param>
    /// <param name="logger"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>Exit code indicating whether issue was successfully created.</returns>
    public static async Task<int> TagPRs(
        string vsBuild,
        string vsCommitSha,
        RoslynToolsSettings settings,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        using var devdivConnection = new AzDOConnection(settings.DevDivAzureDevOpsBaseUri, "DevDiv", settings.DevDivAzureDevOpsToken);
        using var dncengConnection = new AzDOConnection(settings.DncEngAzureDevOpsBaseUri, "internal", settings.DncEngAzureDevOpsToken);
        var connections = new AzDOConnection[] { devdivConnection, dncengConnection };

        // Retrieve VS repo
        var vsRepository = await GetVSRepositoryAsync(devdivConnection.GitClient);

        // Find previous VS commit SHA
        var vsCommit = await devdivConnection.GitClient.GetCommitAsync(
            vsCommitSha, vsRepository.Id, cancellationToken: cancellationToken).ConfigureAwait(false);
        var previousVsCommitSha = vsCommit.Parents.First();

        foreach (var product in VSBranchInfo.AllProducts)
        {
            // We currently only support creating issues for GitHub repos
            if (!product.RepoHttpBaseUrl.Contains("github.com"))
            {
                continue;
            }

            var gitHubRepoName = product.RepoHttpBaseUrl.Split('/').Last();
            logger.LogInformation($"GitHub repo: {gitHubRepoName}");

            // Get associated product build for current and previous VS commit SHAs
            var currentBuild = await TryGetBuildNumberForReleaseAsync(product.ComponentJsonFileName, product.ComponentName, vsCommitSha, devdivConnection, logger).ConfigureAwait(false);
            var previousBuild = await TryGetBuildNumberForReleaseAsync(product.ComponentJsonFileName, product.ComponentName, previousVsCommitSha, devdivConnection, logger).ConfigureAwait(false);

            if (currentBuild is null)
            {
                logger.LogError($"{gitHubRepoName} build not found for VS commit SHA {currentBuild}.");
                continue;
            }

            if (previousBuild is null)
            {
                logger.LogError($"{gitHubRepoName} build not found for VS commit SHA {previousBuild}.");
                continue;
            }

            // If builds are the same, there are no PRs to tag
            if (currentBuild.Equals(previousBuild))
            {
                logger.LogInformation($"No PRs found to tag; {gitHubRepoName} build numbers are equal: {currentBuild}.");
                continue;
            }

            // Get commit SHAs for product builds
            var previousProductCommitSha = await TryGetProductCommitShaFromBuildAsync(product, connections, previousBuild, logger).ConfigureAwait(false);
            var currentProductCommitSha = await TryGetProductCommitShaFromBuildAsync(product, connections, currentBuild, logger).ConfigureAwait(false);

            if (previousProductCommitSha is null || currentProductCommitSha is null)
            {
                logger.LogError($"Error retrieving {gitHubRepoName} commit SHAs.");
                continue;
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
                continue;
            }

            // Find PRs between product commit SHAs
            var prDescription = new StringBuilder();
            var isSuccess = PRFinder.PRFinder.FindPRs(previousProductCommitSha, currentProductCommitSha, PRFinder.PRFinder.DefaultFormat, logger, gitHubRepoPath, prDescription);
            if (isSuccess != 0)
            {
                // Error occurred; should be logged in FindPRs method
                continue;
            }

            logger.LogInformation($"Creating issue...");

            // Create issue
            await TryCreateIssueAsync(gitHubRepoName, vsBuild, prDescription.ToString(), settings.GitHubToken, logger).ConfigureAwait(false);
        }

        return 0;
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

    private async static Task TryCreateIssueAsync(
        string gitHubRepoName,
        string vsBuildNumber,
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
                title = $"[Automated] PRs inserted in VS build {vsBuildNumber}",
                body = issueBody,
                labels = new string[] { "vs-insertion" }
            }));

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError($"Issue creation failed with status code: {response.StatusCode}");
        }

        logger.LogInformation("Successfully created issue.");
    }
}
