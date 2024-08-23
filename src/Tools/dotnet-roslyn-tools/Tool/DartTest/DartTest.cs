// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using LibGit2Sharp;
using Microsoft.Azure.Pipelines.WebApi;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.RoslynTools.Products;
using Microsoft.RoslynTools.Utilities;
using Microsoft.RoslynTools.VS;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using static System.Net.WebRequestMethods;

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
        var cancellationToken = CancellationToken.None;
        var azureBranchName = $"dart-test/{prNumber}";
        var product = VSBranchInfo.AllProducts.Single(p => p.Name.Equals(productName, StringComparison.OrdinalIgnoreCase));

        // If the user doesn't pass the SHA, retrieve the most recent from the PR.
        if (sha is null)
        {
            if (product.Name.Equals("roslyn", StringComparison.OrdinalIgnoreCase))
            {
                sha = await GetLatestShaFromPullRequest(product, remoteConnections.GitHubClient, prNumber, logger, cancellationToken).ConfigureAwait(false);
                if (sha is null)
                {
                    logger.LogError("Could not find a SHA for the given PR number.");
                    return -1;
                }
            }
            else
            {
                logger.LogError("A SHA is required for the given product.");
                return -1;
            }
        }

        var directory = await PushPRToInternal(product, prNumber, azureBranchName, logger, sha, cancellationToken).ConfigureAwait(false);
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

        var pipelineName = GetPipelineName();
        await remoteConnections.DevDivConnection.TryRunPipelineAsync(pipelineName, repositoryParams, runPipelineParameters, logger).ConfigureAwait(false);
        CleanupDirectory(directory, logger);
        return 0;

        string GetPipelineName()
        {
            switch (product.Name.ToLower())
            {
                case "roslyn":
                    return "Roslyn Integration CI DartLab";
                default:
                    return "";
            }
        }
    }

    private static async Task<string?> GetLatestShaFromPullRequest(IProduct product, HttpClient gitHubClient, int prNumber, ILogger logger, CancellationToken cancellationToken)
    {
        try
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
        }
        catch (Exception e)
        {
            logger.LogError(e, "Exception while retrieving the latest SHA from GitHub.");
        }

        return null;
    }


    private static async Task<string?> PushPRToInternal(IProduct product, int prNumber, string azureBranchName, ILogger logger, string sha, CancellationToken cancellationToken)
    {
        string? targetDirectory = null;

        try
        {
            targetDirectory = Path.Combine(Path.GetTempPath(), $"pr-{prNumber}");
            var counter = 1;
            while (Directory.Exists(targetDirectory))
            {
                targetDirectory = Path.Combine(Path.GetTempPath(), $"pr-{prNumber}-{counter}");
                counter++;
            }

            Directory.CreateDirectory(targetDirectory);

            var initCommand = $"init";
            await RunGitCommand(initCommand, logger, targetDirectory, cancellationToken).ConfigureAwait(false); ;

            var addGithubRemoteCommand = $"remote add {product.Name.ToLower()} {product.RepoHttpBaseUrl}.git";
            await RunGitCommand(addGithubRemoteCommand, logger, targetDirectory, cancellationToken).ConfigureAwait(false);

            var addInternalRemoteCommand = $"remote add internal {product.InternalRepoBaseUrl}";
            await RunGitCommand(addInternalRemoteCommand, logger, targetDirectory, cancellationToken).ConfigureAwait(false);

            var fetchCommand = $"fetch {product.Name.ToLower()} pull/{prNumber}/head";
            await RunGitCommand(fetchCommand, logger, targetDirectory, cancellationToken).ConfigureAwait(false);

            var checkoutCommand = $"checkout {sha}";
            await RunGitCommand(checkoutCommand, logger, targetDirectory, cancellationToken).ConfigureAwait(false);

            var checkoutNewBranchCommand = $"checkout -b {azureBranchName}";
            await RunGitCommand(checkoutNewBranchCommand, logger, targetDirectory, cancellationToken).ConfigureAwait(false);

            var pushCommand = $"push internal {azureBranchName}";
            await RunGitCommand(pushCommand, logger, targetDirectory, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
        }

        return targetDirectory;
    }

    private static async Task RunGitCommand(string command, ILogger logger, string workingDirectory, CancellationToken cancellationToken)
    {
        try
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
                logger.LogInformation($"Command succeeded: {command}");
                logger.LogInformation(output);
            }
            else
            {
                logger.LogError($"Command failed: {command}");
                logger.LogError(error);
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Exception while running command: {command}");
        }
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

            foreach (string file in files)
            {
                System.IO.File.SetAttributes(file, FileAttributes.Normal);
                System.IO.File.Delete(file);
            }

            foreach (string dir in dirs)
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
