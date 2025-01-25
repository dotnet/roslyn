// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using LibGit2Sharp;

namespace Microsoft.RoslynTools.PRFinder;

public interface IRepositoryHost
{
    bool ShouldSkip(Commit commit, ref bool mergePRFound);
    Task<MergeInfo?> TryParseMergeInfoAsync(Commit commit);
    string GetPullRequestUrl(string prNumber);
    string GetCommitUrl(string commitSha);
    string GetDiffUrl(string startRef, string endRef);
}

public record class MergeInfo(string PrNumber, string Comment);
