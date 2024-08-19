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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.RoslynTools.Utilities;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using static System.Net.WebRequestMethods;

namespace Microsoft.RoslynTools.DartTest;

internal static class DartTest
{
    public static async Task<int> RunDartPipeline(
        RemoteConnections remoteConnections,
        ILogger logger,
        int prNumber,
        string? sha)
    {
        var cancellationToken = CancellationToken.None;
        var azureBranchName = $"dart-test/{prNumber}";
        if (sha is null)
        {
            sha = await GetLatestShaFromPullRequest(remoteConnections.GitHubClient, prNumber, logger, cancellationToken).ConfigureAwait(false);
            if (sha is null)
            {
                logger.LogError("Could not find a SHA for the given PR number.");
                return -1;
            }
        }

        var directory = await ClonePullRequest(prNumber, azureBranchName, logger, sha, cancellationToken).ConfigureAwait(false);
        await remoteConnections.DevDivConnection.TryRunPipelineAsync(azureBranchName, "Roslyn Integration CI DartLab", sha, prNumber, logger).ConfigureAwait(false);
        CleanupDirectory(directory, logger);
        return 0;
    }

    private static async Task<string?> GetLatestShaFromPullRequest(HttpClient gitHubClient, int prNumber, ILogger logger, CancellationToken cancellationToken)
    {
        try
        {
            var requestUri = $"/repos/dotnet/roslyn/pulls/{prNumber}";
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


    private static async Task<string?> ClonePullRequest(int prNumber, string azureBranchName, ILogger logger, string sha, CancellationToken cancellationToken)
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

            var initCommand = $"git init";
            await RunCommand(initCommand, logger, targetDirectory, cancellationToken).ConfigureAwait(false); ;

            var addGithubRemoteCommand = $"git remote add roslyn https://github.com/dotnet/roslyn.git";
            await RunCommand(addGithubRemoteCommand, logger, targetDirectory, cancellationToken).ConfigureAwait(false);

            var addInternalRemoteCommand = $"git remote add internal https://dnceng.visualstudio.com/internal/_git/dotnet-roslyn";
            await RunCommand(addInternalRemoteCommand, logger, targetDirectory, cancellationToken).ConfigureAwait(false);

            var fetchCommand = $"git fetch roslyn pull/{prNumber}/head";
            await RunCommand(fetchCommand, logger, targetDirectory, cancellationToken).ConfigureAwait(false);

            var checkoutCommand = $"git checkout {sha}";
            await RunCommand(checkoutCommand, logger, targetDirectory, cancellationToken).ConfigureAwait(false);

            var checkoutNewBranchCommand = $"git checkout -b {azureBranchName}";
            await RunCommand(checkoutNewBranchCommand, logger, targetDirectory, cancellationToken).ConfigureAwait(false);

            var pushCommand = $"git push internal {azureBranchName}";
            await RunCommand(pushCommand, logger, targetDirectory, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
        }

        return targetDirectory;
    }

    private static async Task RunCommand(string command, ILogger logger, string workingDirectory, CancellationToken cancellationToken)
    {
        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {command}",
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
