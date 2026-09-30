// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.RoslynTools.PRFinder;

namespace Microsoft.RoslynTools.UnitTests.PRFinder;

public class CommitHistoryFilterTests
{
    [Theory]
    [InlineData("Merge main into release/insiders")]
    [InlineData("Merge release/insiders into release/stable")]
    [InlineData("Snap main into release/insiders (18.12) (#85819)")]
    [InlineData("Snap release/insiders into release/stable (#85820)")]
    [InlineData("Replace content from another branch")]
    [InlineData("")]
    public async Task MatchingSecondParentExcludesFirstParentHistory(string message)
    {
        var commits = new[]
        {
            Commit("head", "Update (#3)", "snap"),
            Commit("snap", message, "discarded", "source"),
            Commit("discarded", "Old target update (#1)", "base"),
            Commit("source", "New source update (#2)", "base"),
        };

        var result = await FilterAsync(commits, "head", ("snap", "source"));

        Assert.Equal(["head", "snap", "source"], result.Select(commit => commit.CommitId));
    }

    [Fact]
    public async Task SnapPullRequestMergeAlsoExcludesTargetHistory()
    {
        var commits = new[]
        {
            Commit("head", "Update (#3)", "snap-pr"),
            Commit("snap-pr", "Snap main into release/insiders (18.12) (#85819)", "discarded", "config"),
            Commit("config", "Update PublishData.json", "snap"),
            Commit("snap", "Merge main into release/insiders", "discarded", "base"),
            Commit("discarded", "Old target update (#1)", "base"),
        };
        var trees = CreateTrees(commits, ("snap-pr", "config"), ("snap", "base"));

        var result = await CommitHistoryFilter.FilterAsync(commits, "head", (id, _) => Task.FromResult(trees[id]));

        Assert.Equal(["head", "snap-pr", "config", "snap"], result.Select(commit => commit.CommitId));
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

    [Theory]
    [InlineData("Merge pull request #3 from contributor/feature")]
    [InlineData("Snap main into release/insiders-extra (#3)")]
    public async Task OrdinaryMergePreservesBothParents(string message)
    {
        var commits = new[]
        {
            Commit("merge", message, "target", "source"),
            Commit("target", "Target update (#1)", "base"),
            Commit("source", "Source update (#2)", "base"),
        };

        var result = await FilterAsync(commits, "merge");

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
    public async Task MissingHeadIsReported()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CommitHistoryFilter.FilterAsync([Commit("other", "Update (#1)")], "head", (_, _) => Task.FromResult("tree")));
    }

    [Fact]
    public async Task CancellationIsPropagated()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            CommitHistoryFilter.FilterAsync([Commit("head", "Update (#1)")], "head", (_, _) => Task.FromResult("tree"), cancellation.Token));
    }

    [Fact]
    public async Task TreeLookupFailureIsPropagated()
    {
        await Assert.ThrowsAsync<IOException>(() =>
            CommitHistoryFilter.FilterAsync(
                [Commit("snap", "Merge main into release/insiders", "target", "source")],
                "snap",
                (_, _) => Task.FromException<string>(new IOException("Tree lookup failed."))));
    }

    [Fact]
    public async Task EmptyTreeIsReported()
    {
        await Assert.ThrowsAsync<InvalidDataException>(() =>
            CommitHistoryFilter.FilterAsync(
                [Commit("merge", "Merge (#1)", "target", "source")],
                "merge",
                (_, _) => Task.FromResult("")));
    }

    [Fact]
    public async Task SingleParentRevertRetainsHistoryWithoutTreeLookups()
    {
        var commits = new[]
        {
            Commit("revert", "Restore earlier content (#2)", "update"),
            Commit("update", "Update (#1)", "base"),
        };

        var result = await CommitHistoryFilter.FilterAsync(commits, "revert", (_, _) =>
            Task.FromException<string>(new InvalidOperationException("Single-parent commits must not query trees.")));

        Assert.Equal(commits, result);
    }

    [Fact]
    public async Task TreeLookupsAreCachedAcrossMerges()
    {
        var commits = new[]
        {
            Commit("head", "Combined merge (#3)", "left", "right"),
            Commit("left", "Replace left content (#1)", "old-left", "base"),
            Commit("right", "Replace right content (#2)", "old-right", "base"),
            Commit("old-left", "Old left content", "base"),
            Commit("old-right", "Old right content", "base"),
        };
        var trees = CreateTrees(commits, ("left", "base"), ("right", "base"));
        var lookups = new List<string>();

        var result = await CommitHistoryFilter.FilterAsync(commits, "head", (id, _) =>
        {
            lookups.Add(id);
            return Task.FromResult(trees[id]);
        });

        Assert.Equal(["head", "left", "right"], result.Select(commit => commit.CommitId));
        Assert.Contains("base", lookups);
        Assert.All(lookups.GroupBy(id => id), group => Assert.Single(group));
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
