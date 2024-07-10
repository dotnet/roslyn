// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

namespace Microsoft.RoslynTools.PRFinder;

using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using System.Runtime.Serialization;
using System.Text;

internal class PRFinder
{
    public const string DefaultFormat = "default";
    public const string OmniSharpFormat = "o#";
    public const string ChangelogFormat = "changelog";

    /// <summary>
    /// Finds the PRs merged between two given commits.
    /// </summary>
    /// <param name="previousCommitSha">Previous commit SHA.</param>
    /// <param name="currentCommitSha">Current commit SHA.</param>
    /// <param name="logger">Logger where result will be output.</param>
    /// <param name="repoPath">Optional path to product repo. Current directory will be used otherwise.</param>
    /// <param name="builder">Optional if the caller wants result as a string.</param>
    /// <returns></returns>
    public static async Task<int> FindPRsAsync(
        string previousCommitSha,
        string currentCommitSha,
        string format,
        ILogger logger,
        string? repoPath = null,
        StringBuilder? builder = null)
    {
        using var repo = new Repository(repoPath ?? Environment.CurrentDirectory);

        var currentCommit = repo.Lookup<Commit>(currentCommitSha);
        var previousCommit = repo.Lookup<Commit>(previousCommitSha);

        if (previousCommit is null)
        {
            logger.LogError($"Previous commit SHA '{previousCommitSha}' does not exist. Please fetch and try again.");
            return -1;
        }

        if (currentCommit is null)
        {
            logger.LogError($"Current commit SHA '{currentCommitSha}' does not exist. Please fetch and try again.");
            return -1;
        }

        var remotes = repo.Network.Remotes.ToDictionary(remote => remote.Name);
        if (!remotes.TryGetValue("upstream", out var remote)
            && !remotes.TryGetValue("origin", out remote))
        {
            remote = remotes.Values.FirstOrDefault();
        }

        if (remote is null)
        {
            logger.LogError("No configured remote for this repository.");
            return 1;
        }

        string repoUrl = string.Empty;
        if (remote.Url.StartsWith("https://")) // https://github.com/dotnet/roslyn.git
        {
            repoUrl = remote.Url.EndsWith(".git")
                ? remote.Url[..^4]
                : remote.Url;
        }
        else if (remote.Url.StartsWith("git@")) // git@github.com:dotnet/roslyn.git
        {
            var colonIndex = remote.Url.IndexOf(':');
            repoUrl = remote.Url.EndsWith(".git")
                ? $"https://{remote.Url[4..colonIndex]}/{remote.Url[++colonIndex..^4]}"
                : $"https://{remote.Url[4..colonIndex]}/{remote.Url[++colonIndex..]}";
        }
        else
        {
            logger.LogError($"Remote '{remote.Name}' has an unsupported Url format '{remote.Url}'.");
            return 1;
        }

        var isGitHub = repoUrl.Contains("github.com");
        var isAzure = repoUrl.Contains("azure.com");
        if (!isGitHub && !isAzure)
        {
            logger.LogError($"Remote '{remote.Name}' has an unsupported URL host '{remote.Url}'.");
            return 1;
        }

        IRepositoryHost host = isGitHub
            ? new Hosts.GitHub(repoUrl, logger)
            : new Hosts.Azure(repoUrl);

        IPRLogFormatter formatter = format switch
        {
            DefaultFormat => new Formatters.DefaultFormatter(),
            OmniSharpFormat => new Formatters.OmniSharpFormatter(),
            ChangelogFormat => new Formatters.ChangelogFormatter(),
            _ => throw new InvalidOperationException($"Uknown format '{format}'")
        };

        // Get commit history starting at the current commit and ending at the previous commit
        var commitLog = repo.Commits.QueryBy(
            new CommitFilter
            {
                IncludeReachableFrom = currentCommit,
                ExcludeReachableFrom = previousCommit
            });

        logger.LogDebug(formatter.FormatChangesHeader(previousCommitSha, host.GetCommitUrl(previousCommitSha), currentCommitSha, host.GetCommitUrl(currentCommitSha)));

        RecordLine(formatter.FormatDiffHeader(host.GetDiffUrl(previousCommitSha, currentCommitSha)), logger, builder);

        var commitHeaderAdded = false;
        var mergePRHeaderAdded = false;
        var mergePRFound = false;

        foreach (var commit in commitLog)
        {
            if (host.ShouldSkip(commit, ref mergePRFound))
            {
                continue;
            }

            var mergeInfo = await host.TryParseMergeInfoAsync(commit);

            // We will print commit comments until we find the first merge PR
            if (mergeInfo is null && mergePRFound)
            {
                continue;
            }

            string prLink;

            if (mergeInfo is not null)
            {
                if (commitHeaderAdded && !mergePRHeaderAdded)
                {
                    mergePRHeaderAdded = true;
                    RecordLine(formatter.GetPRSectionHeader(), logger, builder);
                }

                mergePRFound = true;

                prLink = formatter.FormatPRListItem(mergeInfo.Comment, mergeInfo.PrNumber, host.GetPullRequestUrl(mergeInfo.PrNumber));
            }
            else
            {
                if (!commitHeaderAdded)
                {
                    commitHeaderAdded = true;
                    RecordLine(formatter.GetCommitSectionHeader(), logger, builder);
                }

                var fullSHA = commit.Sha;
                var shortSHA = fullSHA.Substring(0, 7);

                prLink = formatter.FormatCommitListItem(commit.MessageShort, shortSHA, host.GetCommitUrl(fullSHA));
            }

            RecordLine(prLink, logger, builder);
        }

        return 0;
    }

    private static void RecordLine(string line, ILogger logger, StringBuilder? builder)
    {
        logger.LogInformation(line);
        builder?.AppendLine(line);
    }
}
