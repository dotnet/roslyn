// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System.Text.RegularExpressions;
using LibGit2Sharp;

namespace Microsoft.RoslynTools.PRFinder.Hosts;

public class Azure : IRepositoryHost
{
    private static readonly Regex IsAzDOReleaseFlowCommit = new Regex(@"^Merged PR \d+: Merging .* to ");
    private static readonly Regex IsAzDOMergePRCommit = new Regex(@"^Merged PR (\d+):");

    public string GetCommitUrl(string repoUrl, string commitSha)
        => $"{repoUrl}/commit/{commitSha}";

    public string GetDiffUrl(string repoUrl, string previousSha, string currentSha)
        // AzDO does not have a UI for comparing two commits. Instead generate the REST API call to retrieve commits between two SHAs.
        => $"{repoUrl.Replace("_git", "_apis/git/repositories")}/commits?searchCriteria.itemVersion.version={previousSha}&searchCriteria.itemVersion.versionType=commit&searchCriteria.compareVersion.version={currentSha}&searchCriteria.compareVersion.versionType=commit";

    public string GetPullRequestUrl(string repoUrl, string prNumber)
        => $"{repoUrl}/pullrequest/{prNumber}";

    public bool ShouldSkip(Commit commit, ref bool mergePRFound)
    {
        // Exclude arcade dependency updates
        if (commit.Author.Name == "DotNet Bot")
        {
            mergePRFound = true;
            return true;
        }

        // Exclude OneLoc localization PRs
        if (commit.Author.Name == "Project Collection Build Service (devdiv)")
        {
            mergePRFound = true;
            return true;
        }

        // Exclude merge commits from auto code-flow PRs (e.g. main to Dev17)
        if (IsAzDOReleaseFlowCommit.Match(commit.MessageShort).Success)
        {
            mergePRFound = true;
            return true;
        }

        return false;
    }

    public bool TryParseMergeInfo(Commit commit, out string prNumber, out string comment)
    {
        var match = IsAzDOMergePRCommit.Match(commit.Message);
        if (match.Success)
        {
            // Merge PR Messages are in the form "Merged PR 320820: Resolving encoding issue on test summary pane, using UTF8 now\n\nAdded a StreamWriterWrapper to resolve encoding issue"
            comment = commit.MessageShort;
            prNumber = match.Groups[1].Value;
            return true;
        }
        else
        {
            // Todo: Determine if there is a format for AzDO squash merges that preserves the PR #
        }

        comment = string.Empty;
        prNumber = string.Empty;
        return false;
    }
}
