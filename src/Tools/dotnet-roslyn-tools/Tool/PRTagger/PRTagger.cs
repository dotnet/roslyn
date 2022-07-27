// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

namespace Microsoft.RoslynTools.PRTagger;

using Microsoft.Extensions.Logging;
using Microsoft.RoslynTools.Authentication;
using Microsoft.RoslynTools.Extensions;
using Microsoft.RoslynTools.Products;
using Microsoft.RoslynTools.Utilities;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Text;

internal class PRTagger
{
    private static readonly Roslyn s_roslynInfo = new();
    private static readonly Razor s_razorInfo = new();

    /// <summary>
    /// Creates a GitHub issue containing the PRs inserted into a given VS build.
    /// </summary>
    /// <param name="productName">Name of product (e.g. 'Roslyn' or 'Razor')</param>
    /// <param name="vsBuild">VS build number</param>
    /// <param name="vsCommitSha">Commit SHA for VS build</param>
    /// <param name="settings">Authentication tokens</param>
    /// <param name="logger"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>Exit code indicating whether issue was successfully created.</returns>
    public static async Task<int> TagPRs(
        string productName,
        string productRepoPath,
        string vsBuild,
        string vsCommitSha,
        RoslynToolsSettings settings,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        using var devdivConnection = new AzDOConnection(settings.DevDivAzureDevOpsBaseUri, "DevDiv", settings.DevDivAzureDevOpsToken);
        using var dncengConnection = new AzDOConnection(settings.DncEngAzureDevOpsBaseUri, "internal", settings.DncEngAzureDevOpsToken);

        // Retrieve VS repo
        var vsRepository = await GetVSRepositoryAsync(devdivConnection.GitClient);

        // Find previous VS commit SHA
        var vsCommit = await devdivConnection.GitClient.GetCommitAsync(
            vsCommitSha, vsRepository.Id, cancellationToken: cancellationToken).ConfigureAwait(false);
        var previousVsCommitSha = vsCommit.Parents.First();

        // Figure out which repo we're working with
        if (!TryGetProductInfo(productName, out var productInfo))
        {
            logger.LogError($"Product info not found for {productName}.");
            return -1;
        }

        // Get associated product build for current and previous VS commit SHAs
        var currentBuild = await TryGetBuildNumberForReleaseAsync(productInfo, vsCommitSha, devdivConnection).ConfigureAwait(false);
        var previousBuild = await TryGetBuildNumberForReleaseAsync(productInfo, previousVsCommitSha, devdivConnection).ConfigureAwait(false);

        if (currentBuild is null)
        {
            logger.LogError($"{productInfo.Name} build not found for VS commit SHA {currentBuild}.");
            return -1;
        }

        if (previousBuild is null)
        {
            logger.LogError($"{productInfo.Name} build not found for VS commit SHA {previousBuild}.");
            return -1;
        }

        // If builds are the same, there are no PRs to tag
        if (currentBuild.Equals(previousBuild))
        {
            logger.LogInformation($"No PRs found to tag; {productInfo.Name} build numbers are equal: {currentBuild}.");
            return 0;
        }

        // Get commit SHAs for product builds
        var previousProductCommitSha = await TryGetProductCommitShaFromBuildAsync(productInfo, dncengConnection, previousBuild).ConfigureAwait(false);
        var currentProductCommitSha = await TryGetProductCommitShaFromBuildAsync(productInfo, dncengConnection, currentBuild).ConfigureAwait(false);

        if (previousProductCommitSha is null || currentProductCommitSha is null)
        {
            logger.LogError($"Error retrieving {productInfo.Name} commit SHAs.");
            return -1;
        }

        logger.LogInformation($"Finding PRs between {productInfo.Name} commit SHAs {previousProductCommitSha} and {currentProductCommitSha}.");

        // Find PRs between product commit SHAs
        var prDescription = new StringBuilder();
        var isSuccess = PRFinder.PRFinder.FindPRs(previousProductCommitSha, currentProductCommitSha, logger, productRepoPath, prDescription);
        if (isSuccess != 0)
        {
            return isSuccess;
        }

        logger.LogInformation($"Creating issue...");

        // Create issue
        var issueCreated = await TryCreateIssue(productInfo, vsBuild, prDescription.ToString(), settings.GitHubToken, logger);
        return issueCreated ? 0 : -1;
    }

    private static async Task<GitRepository> GetVSRepositoryAsync(GitHttpClient gitClient)
    {
        return await gitClient.GetRepositoryAsync("DevDiv", "VS");
    }

    private static bool TryGetProductInfo(string productName, [NotNullWhen(true)] out IProduct? productInfo)
    {
        if (productName.Equals(s_roslynInfo.Name, StringComparison.OrdinalIgnoreCase))
        {
            productInfo = s_roslynInfo;
            return true;
        }
        else if (productName.Equals(s_razorInfo.Name, StringComparison.OrdinalIgnoreCase))
        {
            productInfo = s_razorInfo;
            return true;
        }

        productInfo = null;
        return false;
    }

    private static async Task<string?> TryGetBuildNumberForReleaseAsync(
        IProduct productInfo,
        string vsCommitSha,
        AzDOConnection vsConnection)
    {
        var url = await VisualStudioRepository.GetUrlFromComponentJsonFileAsync(
            vsCommitSha, GitVersionType.Commit, vsConnection, productInfo.ComponentJsonFileName, productInfo.ComponentName);
        if (url is null)
        {
            return null;
        }

        try
        {
            var buildNumber = VisualStudioRepository.GetBuildNumberFromUrl(url);
            return buildNumber;
        }
        catch
        {
            return null;
        }
    }

    private static async Task<string?> TryGetProductCommitShaFromBuildAsync(
        IProduct productInfo,
        AzDOConnection buildConnection,
        string buildNumber)
    {
        var build = (await buildConnection.TryGetBuildsAsync(
            productInfo.GetBuildPipelineName(buildConnection.BuildProjectName)!, buildNumber))?.SingleOrDefault();

        return build?.SourceVersion;
    }

    private async static Task<bool> TryCreateIssue(
        IProduct product,
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

        var repoName = product.RepoBaseUrl.Split('/').Last();

        // https://docs.github.com/en/rest/issues/issues#create-an-issue
        var response = await client.PostAsyncAsJson($"repos/dotnet/{repoName}/issues", JsonConvert.SerializeObject(
            new
            {
                title = $"[Automated] PRs inserted in VS build {vsBuildNumber}",
                body = issueBody,
                labels = new string[] { "vs-insertion" }
            }));

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError($"Issue creation failed with status code: {response.StatusCode}");
            return false;
        }

        logger.LogInformation("Successfully created issue.");
        return true;
    }
}
