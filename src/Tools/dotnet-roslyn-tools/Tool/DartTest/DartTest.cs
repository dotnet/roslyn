// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System.Diagnostics;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Azure.Pipelines.WebApi;
using Microsoft.Extensions.Logging;
using Microsoft.RoslynTools.Products;
using Microsoft.RoslynTools.Utilities;
using Microsoft.RoslynTools.VS;

namespace Microsoft.RoslynTools.DartTest;

internal static class DartTest
{
    public static async Task<int> RunDartPipeline(
        string productName,
        RemoteConnections remoteConnections,
        ILogger logger,
        int prNumber,
        string? sha)
    {
        string? targetDirectory = null;
        try
        {
            var cancellationToken = CancellationToken.None;
            var azureBranchName = $"dart-test/{prNumber}";
            var product = VSBranchInfo.AllProducts.Single(p => p.Name.Equals(productName, StringComparison.OrdinalIgnoreCase));

            // If the user doesn't pass the SHA, retrieve the most recent from the PR.
            if (sha is null)
            {
                sha = await GetLatestShaFromPullRequestAsync(product, remoteConnections.GitHubClient, prNumber, logger, cancellationToken).ConfigureAwait(false);
                if (sha is null)
                {
                    logger.LogError("Could not find a SHA for the given PR number.");
                    return -1;
                }
            }

            targetDirectory = CreateDirectory(prNumber);
            await PushPRToInternalAsync(product, prNumber, azureBranchName, logger, sha, targetDirectory, cancellationToken).ConfigureAwait(false);
            var repositoryParams = new Dictionary<string, RepositoryResourceParameters>
            {
                {
                    "self", new RepositoryResourceParameters
                    {
                        RefName = $"refs/heads/{azureBranchName}",
                        Version = sha
                    }
                }
            };

            var runPipelineParameters = new RunPipelineParameters
            {
                Resources = new RunResourcesParameters
                {

                },
                TemplateParameters = new Dictionary<string, string> { { "prNumber", prNumber.ToString() }, { "sha", sha } }
            };

            await remoteConnections.DevDivConnection.TryRunPipelineAsync(product.DartLabPipelineName, repositoryParams, runPipelineParameters, logger).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
            return -1;
        }
        finally
        {
            CleanupDirectory(targetDirectory, logger);
        }

        return 0;
    }

    private static async Task<string?> GetLatestShaFromPullRequestAsync(IProduct product, HttpClient gitHubClient, int prNumber, ILogger logger, CancellationToken cancellationToken)
    {
        var requestUri = $"/repos/dotnet/{product.Name.ToLower()}/pulls/{prNumber}";
        var response = await gitHubClient.GetAsync(requestUri, cancellationToken).ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
        {
            var prData = await response.Content.ReadFromJsonAsync<JsonObject>(cancellationToken: cancellationToken).ConfigureAwait(false);
            return prData?["head"]?["sha"]?.ToString();
        }
        else
        {
            logger.LogError($"Failed to retrieve PR data from GitHub. Status code: {response.StatusCode}");
        }

        return null;
    }

    private static async Task PushPRToInternalAsync(IProduct product, int prNumber, string azureBranchName, ILogger logger, string sha, string targetDirectory, CancellationToken cancellationToken)
    {
        var initCommand = $"init";
        await RunGitCommandAsync(initCommand, logger, targetDirectory, cancellationToken).ConfigureAwait(false);
        ;

        var addGithubRemoteCommand = $"remote add {product.Name.ToLower()} {product.RepoHttpBaseUrl}.git";
        await RunGitCommandAsync(addGithubRemoteCommand, logger, targetDirectory, cancellationToken).ConfigureAwait(false);

        var repoBaseUrl = string.IsNullOrEmpty(product.InternalRepoBaseUrl) ? product.RepoHttpBaseUrl : product.InternalRepoBaseUrl;
        var addInternalRemoteCommand = $"remote add internal {repoBaseUrl}";
        await RunGitCommandAsync(addInternalRemoteCommand, logger, targetDirectory, cancellationToken).ConfigureAwait(false);

        var fetchCommand = $"fetch {product.Name.ToLower()} pull/{prNumber}/head";
        await RunGitCommandAsync(fetchCommand, logger, targetDirectory, cancellationToken).ConfigureAwait(false);

        var checkoutCommand = $"checkout {sha}";
        await RunGitCommandAsync(checkoutCommand, logger, targetDirectory, cancellationToken).ConfigureAwait(false);

        var checkoutNewBranchCommand = $"checkout -b {azureBranchName}";
        await RunGitCommandAsync(checkoutNewBranchCommand, logger, targetDirectory, cancellationToken).ConfigureAwait(false);

        var pushCommand = $"push internal {azureBranchName}";
        await RunGitCommandAsync(pushCommand, logger, targetDirectory, cancellationToken).ConfigureAwait(false);
    }

    private static async Task RunGitCommandAsync(string command, ILogger logger, string workingDirectory, CancellationToken cancellationToken)
    {
        logger.LogInformation($"Running command: {command}");
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = $"{command}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory
        };

        using var process = new Process
        {
            StartInfo = processStartInfo
        };

        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        process.OutputDataReceived += (sender, args) => outputBuilder.AppendLine(args.Data);
        process.ErrorDataReceived += (sender, args) => errorBuilder.AppendLine(args.Data);

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        // Ensure the output and error streams are fully read
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        var output = outputBuilder.ToString();
        var error = errorBuilder.ToString();

        if (process.ExitCode == 0)
        {
            logger.LogInformation($"Command succeeded!");
            logger.LogInformation(output);
        }
        else
        {
            logger.LogError($"Command failed!");
            logger.LogError(error);
        }
    }

    private static string CreateDirectory(int prNumber)
    {
        var targetDirectory = Path.Combine(Path.GetTempPath(), $"pr-{prNumber}");
        var counter = 1;
        while (Directory.Exists(targetDirectory))
        {
            targetDirectory = Path.Combine(Path.GetTempPath(), $"pr-{prNumber}-{counter}");
            counter++;
        }

        Directory.CreateDirectory(targetDirectory);
        return targetDirectory;
    }

    private static void CleanupDirectory(string? directory, ILogger logger)
    {
        if (directory is null)
        {
            return;
        }

        try
        {
            var files = Directory.GetFiles(directory);
            var dirs = Directory.GetDirectories(directory);

            foreach (var file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            foreach (var dir in dirs)
            {
                CleanupDirectory(dir, logger);
            }

            Directory.Delete(directory, false);
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
        }
    }
}
