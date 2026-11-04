// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.RoslynTools.PRFinder;

internal static class CommitHistoryFilter
{
    public static async Task<List<GitCommit>> FilterAsync(
        IReadOnlyList<GitCommit> commits,
        string headCommitId,
        Func<string, CancellationToken, Task<string>> getTreeIdAsync,
        CancellationToken cancellationToken = default)
    {
        if (commits.Count == 0)
        {
            return [];
        }

        var commitsById = commits.ToDictionary(commit => commit.CommitId, StringComparer.OrdinalIgnoreCase);
        if (!commitsById.ContainsKey(headCommitId))
        {
            throw new InvalidOperationException($"Commit range does not contain its head '{headCommitId}'.");
        }

        var included = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var trees = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var pending = new Stack<string>();
        pending.Push(headCommitId);

        while (pending.TryPop(out var commitId))
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Parents outside the range were already reachable from the previous build.
            if (!commitsById.TryGetValue(commitId, out var commit) || !included.Add(commitId))
            {
                continue;
            }

            var parents = commit.Parents
                ?? throw new InvalidOperationException($"Parents are unavailable for commit '{commitId}'.");
            if (parents.Length > 1)
            {
                var tree = await GetTreeIdAsync(commitId);
                var matchingParents = new List<string>();
                foreach (var parent in parents)
                {
                    if (StringComparer.OrdinalIgnoreCase.Equals(tree, await GetTreeIdAsync(parent)))
                    {
                        matchingParents.Add(parent);
                    }
                }

                if (matchingParents.Count > 0)
                {
                    parents = matchingParents.ToArray();
                }
            }

            foreach (var parent in parents)
            {
                pending.Push(parent);
            }
        }

        return commits.Where(commit => included.Contains(commit.CommitId)).ToList();

        async Task<string> GetTreeIdAsync(string commitId)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!trees.TryGetValue(commitId, out var tree))
            {
                tree = await getTreeIdAsync(commitId, cancellationToken);
                if (string.IsNullOrWhiteSpace(tree))
                {
                    throw new InvalidDataException($"Tree is unavailable for commit '{commitId}'.");
                }

                trees.Add(commitId, tree);
            }

            return tree;
        }
    }
}
