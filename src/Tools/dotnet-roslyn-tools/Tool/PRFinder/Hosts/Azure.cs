// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System.Text.RegularExpressions;
using LibGit2Sharp;

namespace Microsoft.RoslynTools.PRFinder.Hosts;

public class Azure(string repoUrl) : IRepositoryHost
{
    internal static readonly Regex IsAzDOReleaseFlowCommit = new Regex(@"^Merged PR \d+: Merging .* to ");
    internal static readonly Regex IsAzDOMergePRCommit = new Regex(@"^Merged PR (\d+):");
    private readonly string _repoUrl = repoUrl;

    public string GetCommitUrl(string commitSha)
        => $"{_repoUrl}/commit/{commitSha}";

    public string GetDiffUrl(string startRef, string endRef)
        // AzDO does not have a UI for comparing two commits. Instead generate the REST API call to retrieve commits between two SHAs.
        => $"{_repoUrl.Replace("_git", "_apis/git/repositories")}/commits?searchCriteria.itemVersion.version={startRef}&searchCriteria.itemVersion.versionType=commit&searchCriteria.compareVersion.version={endRef}&searchCriteria.compareVersion.versionType=commit";

    public string GetPullRequestUrl(string prNumber)
        => $"{_repoUrl}/pullrequest/{prNumber}";

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

    public Task<MergeInfo?> TryParseMergeInfoAsync(Commit commit)
    {
        var match = IsAzDOMergePRCommit.Match(commit.Message);
        if (match.Success)
        {
            // Merge PR Messages are in the form "Merged PR 320820: Resolving encoding issue on test summary pane, using UTF8 now\n\nAdded a StreamWriterWrapper to resolve encoding issue"
            return Task.FromResult<MergeInfo?>(new(match.Groups[1].Value, commit.Message));
        }
        else
        {
            // Todo: Determine if there is a format for AzDO squash merges that preserves the PR #
        }

        return Task.FromResult<MergeInfo?>(null);
    }
}
