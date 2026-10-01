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
            Commit("head", "Update (#3)", "snap"),
            Commit("snap", "Replace content from another branch", "discarded", "source"),
            Commit("discarded", "Old target update (#1)", "base"),
            Commit("source", "New source update (#2)", "base"),
        };

        var result = await FilterAsync(commits, "head", ("snap", "source"));

        Assert.Equal(["head", "snap", "source"], result.Select(commit => commit.CommitId));
    }

    [Fact]
    public async Task MatchingFirstParentExcludesSecondParentHistory()
    {
        var commits = new[]
        {
            Commit("merge", "Keep existing content (#3)", "target", "source"),
            Commit("target", "Target update (#1)", "base"),
            Commit("source", "Source update (#2)", "base"),
        };

        var result = await FilterAsync(commits, "merge", ("merge", "target"));

        Assert.Equal(["merge", "target"], result.Select(commit => commit.CommitId));
    }

    [Fact]
    public async Task IdenticalParentTreesPreserveBothHistories()
    {
        var commits = new[]
        {
            Commit("merge", "Merge equivalent snapshots (#3)", "target", "source"),
            Commit("target", "Target update (#1)", "base"),
            Commit("source", "Source update (#2)", "base"),
        };

        var result = await FilterAsync(commits, "merge", ("merge", "source"), ("target", "source"));

        Assert.Equal(commits, result);
    }

    [Fact]
    public async Task MultipleMatchingParentsExcludeOnlyNonMatchingHistory()
    {
        var commits = new[]
        {
            Commit("merge", "Merge three branches (#4)", "first", "second", "third"),
            Commit("first", "First update (#1)", "base"),
            Commit("second", "Second update (#2)", "base"),
            Commit("third", "Third update (#3)", "base"),
        };

        var result = await FilterAsync(commits, "merge", ("merge", "first"), ("third", "first"));

        Assert.Equal(["merge", "first", "third"], result.Select(commit => commit.CommitId));
    }

    [Fact]
    public async Task DistinctMergeTreePreservesBothParents()
    {
        var commits = new[]
        {
            Commit("snap", "Merge main into release/insiders", "target", "source"),
            Commit("target", "Target update (#1)", "base"),
            Commit("source", "Source update (#2)", "base"),
        };

        var result = await FilterAsync(commits, "snap");

        Assert.Equal(commits, result);
    }

    [Fact]
    public async Task TargetHistoryReachedThroughLaterMergeIsPreserved()
    {
        var commits = new[]
        {
            Commit("head", "Merge another branch (#3)", "snap", "target"),
            Commit("snap", "Merge main into release/insiders", "target", "base"),
            Commit("target", "Target update (#1)", "base"),
        };

        var result = await FilterAsync(commits, "head", ("snap", "base"));

        Assert.Equal(commits, result);
    }

    [Fact]
    public async Task CascadedSnapsExcludeBothReplacedHistories()
    {
        var commits = new[]
        {
            Commit("stable", "Merge release/insiders into release/stable", "old-stable", "insiders"),
            Commit("insiders", "Merge main into release/insiders", "old-insiders", "source"),
            Commit("old-stable", "Old stable update (#1)", "base"),
            Commit("old-insiders", "Old insiders update (#2)", "base"),
            Commit("source", "New source update (#3)", "base"),
        };

        var result = await FilterAsync(commits, "stable", ("stable", "source"), ("insiders", "source"));

        Assert.Equal(["stable", "insiders", "source"], result.Select(commit => commit.CommitId));
    }

    [Fact]
    public async Task PreservesOriginalOrdering()
    {
        var commits = new[]
        {
            Commit("source", "Source update (#2)", "base"),
            Commit("target", "Target update (#1)", "base"),
            Commit("snap", "Merge main into release/insiders", "target", "source"),
            Commit("head", "Update (#3)", "snap"),
        };

        var result = await FilterAsync(commits, "head", ("snap", "source"));

        Assert.Equal(["source", "snap", "head"], result.Select(commit => commit.CommitId));
    }

    [Fact]
    public async Task EmptyRangeHasNoChanges()
    {
        var result = await CommitHistoryFilter.FilterAsync([], "head", (_, _) => Task.FromResult("tree"));

        Assert.Empty(result);
    }

    [Fact]
    public async Task SingleParentRevertRetainsHistory()
    {
        var commits = new[]
        {
            Commit("revert", "Restore earlier content (#2)", "update"),
            Commit("update", "Update (#1)", "base"),
        };

        var result = await FilterAsync(commits, "revert", ("revert", "base"));

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
