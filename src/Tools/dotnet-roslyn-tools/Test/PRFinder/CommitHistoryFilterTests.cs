// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.RoslynTools.PRFinder;

namespace Microsoft.RoslynTools.UnitTests.PRFinder;

public class CommitHistoryFilterTests
{
    [Fact]
    public async Task MatchingSecondParentExcludesFirstParentHistory()
    {
        var commits = new[]
        {
            Commit(id: "head", message: "Update (#3)", parents: ["snap"]),
            Commit(id: "snap", message: "Replace content from another branch", parents: ["discarded", "source"]),
            Commit(id: "discarded", message: "Old target update (#1)", parents: ["base"]),
            Commit(id: "source", message: "New source update (#2)", parents: ["base"]),
        };

        var result = await FilterAsync(commits, head: "head", overrides: [(Commit: "snap", Tree: "source")]);

        Assert.Equal(["head", "snap", "source"], result.Select(commit => commit.CommitId));
    }

    [Fact]
    public async Task MatchingFirstParentExcludesSecondParentHistory()
    {
        var commits = new[]
        {
            Commit(id: "merge", message: "Keep existing content (#3)", parents: ["target", "source"]),
            Commit(id: "target", message: "Target update (#1)", parents: ["base"]),
            Commit(id: "source", message: "Source update (#2)", parents: ["base"]),
        };

        var result = await FilterAsync(commits, head: "merge", overrides: [(Commit: "merge", Tree: "target")]);

        Assert.Equal(["merge", "target"], result.Select(commit => commit.CommitId));
    }

    [Fact]
    public async Task IdenticalParentTreesPreserveBothHistories()
    {
        var commits = new[]
        {
            Commit(id: "merge", message: "Merge equivalent snapshots (#3)", parents: ["target", "source"]),
            Commit(id: "target", message: "Target update (#1)", parents: ["base"]),
            Commit(id: "source", message: "Source update (#2)", parents: ["base"]),
        };

        var result = await FilterAsync(
            commits, head: "merge", overrides: [(Commit: "merge", Tree: "source"), (Commit: "target", Tree: "source")]);

        Assert.Equal(commits, result);
    }

    [Fact]
    public async Task MultipleMatchingParentsExcludeOnlyNonMatchingHistory()
    {
        var commits = new[]
        {
            Commit(id: "merge", message: "Merge three branches (#4)", parents: ["first", "second", "third"]),
            Commit(id: "first", message: "First update (#1)", parents: ["base"]),
            Commit(id: "second", message: "Second update (#2)", parents: ["base"]),
            Commit(id: "third", message: "Third update (#3)", parents: ["base"]),
        };

        var result = await FilterAsync(
            commits, head: "merge", overrides: [(Commit: "merge", Tree: "first"), (Commit: "third", Tree: "first")]);

        Assert.Equal(["merge", "first", "third"], result.Select(commit => commit.CommitId));
    }

    [Fact]
    public async Task DistinctMergeTreePreservesBothParents()
    {
        var commits = new[]
        {
            Commit(id: "snap", message: "Merge main into release/insiders", parents: ["target", "source"]),
            Commit(id: "target", message: "Target update (#1)", parents: ["base"]),
            Commit(id: "source", message: "Source update (#2)", parents: ["base"]),
        };

        var result = await FilterAsync(commits, head: "snap");

        Assert.Equal(commits, result);
    }

    [Fact]
    public async Task TargetHistoryReachedThroughLaterMergeIsPreserved()
    {
        var commits = new[]
        {
            Commit(id: "head", message: "Merge another branch (#3)", parents: ["snap", "target"]),
            Commit(id: "snap", message: "Merge main into release/insiders", parents: ["target", "base"]),
            Commit(id: "target", message: "Target update (#1)", parents: ["base"]),
        };

        var result = await FilterAsync(commits, head: "head", overrides: [(Commit: "snap", Tree: "base")]);

        Assert.Equal(commits, result);
    }

    [Fact]
    public async Task CascadedSnapsExcludeBothReplacedHistories()
    {
        var commits = new[]
        {
            Commit(id: "stable", message: "Merge release/insiders into release/stable", parents: ["old-stable", "insiders"]),
            Commit(id: "insiders", message: "Merge main into release/insiders", parents: ["old-insiders", "source"]),
            Commit(id: "old-stable", message: "Old stable update (#1)", parents: ["base"]),
            Commit(id: "old-insiders", message: "Old insiders update (#2)", parents: ["base"]),
            Commit(id: "source", message: "New source update (#3)", parents: ["base"]),
        };

        var result = await FilterAsync(
            commits, head: "stable", overrides: [(Commit: "stable", Tree: "source"), (Commit: "insiders", Tree: "source")]);

        Assert.Equal(["stable", "insiders", "source"], result.Select(commit => commit.CommitId));
    }

    [Fact]
    public async Task PreservesOriginalOrdering()
    {
        var commits = new[]
        {
            Commit(id: "source", message: "Source update (#2)", parents: ["base"]),
            Commit(id: "target", message: "Target update (#1)", parents: ["base"]),
            Commit(id: "snap", message: "Merge main into release/insiders", parents: ["target", "source"]),
            Commit(id: "head", message: "Update (#3)", parents: ["snap"]),
        };

        var result = await FilterAsync(commits, head: "head", overrides: [(Commit: "snap", Tree: "source")]);

        Assert.Equal(["source", "snap", "head"], result.Select(commit => commit.CommitId));
    }

    [Fact]
    public async Task EmptyRangeHasNoChanges()
    {
        var result = await CommitHistoryFilter.FilterAsync(
            commits: [], headCommitId: "head", getTreeIdAsync: (_, _) => Task.FromResult("tree"));

        Assert.Empty(result);
    }

    [Fact]
    public async Task SingleParentRevertRetainsHistory()
    {
        var commits = new[]
        {
            Commit(id: "revert", message: "Restore earlier content (#2)", parents: ["update"]),
            Commit(id: "update", message: "Update (#1)", parents: ["base"]),
        };

        var result = await FilterAsync(commits, head: "revert", overrides: [(Commit: "revert", Tree: "base")]);

        Assert.Equal(commits, result);
    }

    private static Task<List<GitCommit>> FilterAsync(IReadOnlyList<GitCommit> commits, string head, params (string Commit, string Tree)[] overrides)
    {
        var trees = CreateTrees(commits, overrides);
        return CommitHistoryFilter.FilterAsync(commits, head, (id, _) => Task.FromResult(trees[id]));
    }

    private static Dictionary<string, string> CreateTrees(IReadOnlyList<GitCommit> commits, params (string Commit, string Tree)[] overrides)
    {
        var trees = new Dictionary<string, string>();
        foreach (var commit in commits)
        {
            trees.TryAdd(commit.CommitId, commit.CommitId);
            foreach (var parent in commit.Parents)
            {
                trees.TryAdd(parent, parent);
            }
        }

        foreach (var (commit, tree) in overrides)
        {
            trees[commit] = tree;
        }

        return trees;
    }

    private static GitCommit Commit(string id, string message, params string[] parents)
        => new() { CommitId = id, Message = message, Parents = parents };
}
