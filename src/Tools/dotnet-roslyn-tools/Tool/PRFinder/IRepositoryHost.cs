// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using LibGit2Sharp;

namespace Microsoft.RoslynTools.PRFinder;

public interface IRepositoryHost
{
    bool ShouldSkip(Commit commit, ref bool mergePRFound);
    bool TryParseMergeInfo(Commit commit, out string prNumber, out string comment);
    string GetPullRequestUrl(string repoUrl, string prNumber);
    string GetCommitUrl(string repoUrl, string commitSha);
    string GetDiffUrl(string repoUrl, string previousSha, string currentSha);
}
