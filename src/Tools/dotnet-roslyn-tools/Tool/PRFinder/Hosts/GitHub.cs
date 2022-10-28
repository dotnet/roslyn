// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System.Text.RegularExpressions;
using LibGit2Sharp;

namespace Microsoft.RoslynTools.PRFinder.Hosts;

public class GitHub : IRepositoryHost
{
    private static readonly Regex IsGitHubReleaseFlowCommit = new(@"^Merge pull request #\d+ from dotnet/merges/");
    private static readonly Regex IsGitHubMergePRCommit = new(@"^Merge pull request #(\d+) from");
    private static readonly Regex IsGitHubSquashedPRCommit = new(@"\(#(\d+)\)(?:\n|$)");

    public string GetCommitUrl(string repoUrl, string commitSha)
        => $"{repoUrl}/commit/{commitSha}";

    public string GetDiffUrl(string repoUrl, string previousSha, string currentSha)
        => $"{repoUrl}/compare/{previousSha}...{currentSha}?w=1";

    public string GetPullRequestUrl(string repoUrl, string prNumber)
        => $"{repoUrl}/pull/{prNumber}";

    public bool ShouldSkip(Commit commit, ref bool mergePRFound)
    {
        // Once we've found a Merge PR we can exclude commits not committed by GitHub since Merge and Squash commits are committed by GitHub
        if (commit.Committer.Name != "GitHub" && mergePRFound)
        {
            return true;
        }

        // Exclude arcade dependency updates
        if (commit.Author.Name == "dotnet-maestro[bot]")
        {
            mergePRFound = true;
            return true;
        }

        // Exclude merge commits from auto code-flow PRs (e.g. merges/main-to-main-vs-deps)
        if (IsGitHubReleaseFlowCommit.Match(commit.MessageShort).Success)
        {
            mergePRFound = true;
            return true;
        }

        return false;
    }

    public bool TryParseMergeInfo(Commit commit, out string prNumber, out string comment)
    {
        var match = IsGitHubMergePRCommit.Match(commit.MessageShort);
        if (match.Success)
        {
            prNumber = match.Groups[1].Value;

            // Merge PR Messages are in the form "Merge pull request #39526 from mavasani/GetValueUsageInfoAssert\n\nFix an assert in IOperationExtension.GetValueUsageInfo"
            // Try and extract the 1st non-empty line since it is the useful part of the message, otherwise take the first line.
            var lines = commit.Message.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            comment = lines.Length > 1
                ? $"{lines[1]} (#{prNumber})"
                : commit.MessageShort;
            return true;
        }
        else
        {
            match = IsGitHubSquashedPRCommit.Match(commit.MessageShort);
            if (match.Success)
            {
                prNumber = match.Groups[1].Value;

                // Squash PR Messages are in the form "Nullable annotate TypeCompilationState and MessageID (#39449)"
                // Take the 1st line since it should be descriptive.
                comment = commit.MessageShort;
                return true;
            }
        }

        prNumber = string.Empty;
        comment = string.Empty;
        return false;
    }
}
