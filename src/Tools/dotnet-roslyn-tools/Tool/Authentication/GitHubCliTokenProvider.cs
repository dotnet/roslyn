// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Maestro.Common;
using Microsoft.Extensions.Logging;

namespace Microsoft.RoslynTools.Authentication;

/// <summary>
/// Uses a supplied token, or retrieves one from the GitHub CLI when no token was supplied.
/// </summary>
internal sealed class GitHubCliTokenProvider(string? staticToken, ILogger logger) : IRemoteTokenProvider
{
    private readonly object _lock = new();
    private string? _cachedToken;
    private bool _hasAttemptedGitHubCli;

    public string? GetTokenForRepository(string repoUri)
    {
        lock (_lock)
        {
            if (!string.IsNullOrEmpty(staticToken))
            {
                return staticToken;
            }

            if (_hasAttemptedGitHubCli)
            {
                return _cachedToken;
            }

            _cachedToken = TryGetGitHubTokenFromCliAsync().GetAwaiter().GetResult();
            _hasAttemptedGitHubCli = true;
            return _cachedToken;
        }
    }

    public Task<string?> GetTokenForRepositoryAsync(string repoUri)
        => Task.FromResult(GetTokenForRepository(repoUri));

    private async Task<string?> TryGetGitHubTokenFromCliAsync()
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "gh",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                }
            };
            process.StartInfo.ArgumentList.Add("auth");
            process.StartInfo.ArgumentList.Add("token");

            process.Start();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var standardOutputTask = process.StandardOutput.ReadToEndAsync(timeout.Token);
            var standardErrorTask = process.StandardError.ReadToEndAsync(timeout.Token);

            try
            {
                await process.WaitForExitAsync(timeout.Token);
            }
            catch (OperationCanceledException)
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }

                logger.LogDebug("Timed out while retrieving a GitHub token from 'gh auth token'.");
                return null;
            }

            var standardOutput = await standardOutputTask;
            await standardErrorTask;

            if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(standardOutput))
            {
                logger.LogDebug("Successfully retrieved GitHub token from 'gh auth token'.");
                return standardOutput.Trim();
            }

            logger.LogDebug("GitHub CLI did not return a valid token. Exit code: {ExitCode}", process.ExitCode);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Failed to retrieve a GitHub token from the 'gh' CLI. This is expected if 'gh' is not installed or not authenticated.");
        }

        return null;
    }
}
