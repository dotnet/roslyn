// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.RoslynTools.Insertion;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace Microsoft.RoslynTools.UnitTests.Insertion;

public class AzureInsertionChangelogTests
{
    private const string RepoUrl = "https://dev.azure.com/example/project/_git/repo";

    [Fact]
    public async Task SnapAndPullRequestMergeExcludeReplacedTargetHistory()
    {
        var commits = new[]
        {
            Commit("head", "Update Gladstone (#85834)", "snap-pr"),
            Commit("snap-pr", "Promote compiler snapshot (#85819)", "discarded", "config"),
            Commit("config", "Update PublishData.json", "snap"),
            Commit("snap", "Replace branch content", "discarded", "base"),
            Commit("discarded", "Old insiders update (#85189)", "base"),
        };
        using var client = new Client(commits, new Dictionary<string, string>
        {
            ["snap-pr"] = "config-tree",
            ["config"] = "config-tree",
            ["snap"] = "source-tree",
            ["base"] = "source-tree",
            ["discarded"] = "old-target-tree",
        });

        var (changes, diffLink) = await RoslynInsertionTool.GetChangesBetweenBuildsFromAzDOAsync(client, "project", "repo", RepoUrl, "base", "head");

        Assert.Equal(["head", "snap-pr", "config", "snap"], changes.Select(commit => commit.CommitId));
        Assert.Equal(RepoUrl + "/branchCompare?baseVersion=GCbase&targetVersion=GChead", diffLink);
        Assert.Equal("base", Assert.Single(client.TreeLookups));
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
        using var client = new Client(commits, new Dictionary<string, string>
        {
            ["snap"] = "merged-tree",
            ["source"] = "source-tree",
            ["target"] = "target-tree",
        });

        var (changes, _) = await RoslynInsertionTool.GetChangesBetweenBuildsFromAzDOAsync(client, "project", "repo", RepoUrl, "base", "snap");

        Assert.Equal(["snap", "target", "source"], changes.Select(commit => commit.CommitId));
        Assert.Empty(client.TreeLookups);
    }

    [Fact]
    public async Task FetchesAllCommitPagesBeforeFiltering()
    {
        var commits = Enumerable.Range(1, 1001).Reverse()
            .Select(i => Commit($"commit-{i}", $"Update (#{i})", i == 1 ? "base" : $"commit-{i - 1}"))
            .ToArray();
        using var client = new Client(commits, new Dictionary<string, string>());

        var (changes, _) = await RoslynInsertionTool.GetChangesBetweenBuildsFromAzDOAsync(client, "project", "repo", RepoUrl, "base", "commit-1001");

        Assert.Equal(commits.Select(commit => commit.CommitId), changes.Select(commit => commit.CommitId));
    }

    [Fact]
    public async Task SharedOutOfRangeParentTreeIsFetchedOnlyOnce()
    {
        var commits = new[]
        {
            Commit("head", "Combine histories (#5)", "left", "right"),
            Commit("left", "Replace left content (#3)", "old-left", "base"),
            Commit("right", "Replace right content (#4)", "old-right", "base"),
            Commit("old-left", "Old left update (#1)", "base"),
            Commit("old-right", "Old right update (#2)", "base"),
        };
        using var client = new Client(commits, new Dictionary<string, string>
        {
            ["left"] = "base-tree",
            ["right"] = "base-tree",
            ["base"] = "base-tree",
        });

        var (changes, _) = await RoslynInsertionTool.GetChangesBetweenBuildsFromAzDOAsync(client, "project", "repo", RepoUrl, "base", "head");

        Assert.Equal(["head", "left", "right"], changes.Select(commit => commit.CommitId));
        Assert.Equal("base", Assert.Single(client.TreeLookups));
    }

    private static GitCommit Commit(string id, string message, params string[] parents)
    {
        var links = new ReferenceLinks();
        links.AddLink("web", RepoUrl + "/commit/" + id);
        return new GitCommit
        {
            CommitId = id,
            TreeId = $"tree-{id}",
            Comment = message,
            Author = new GitUserDate { Name = "Contributor" },
            Committer = new GitUserDate { Name = "GitHub" },
            Parents = parents,
            Links = links,
        };
    }

    private sealed class Client : GitHttpClient
    {
        private readonly IReadOnlyList<GitCommit> _commits;
        private readonly Dictionary<string, GitCommit> _commitsById;
        private readonly IReadOnlyDictionary<string, string> _trees;

        public List<string> TreeLookups { get; } = [];

        public Client(IReadOnlyList<GitCommit> commits, IReadOnlyDictionary<string, string> trees)
            : base(new Uri("https://dev.azure.com/example"), new VssBasicCredential("", "test-token"))
        {
            _commits = commits;
            _commitsById = commits.ToDictionary(commit => commit.CommitId);
            _trees = trees;
            foreach (var commit in commits)
            {
                if (trees.TryGetValue(commit.CommitId, out var tree))
                {
                    commit.TreeId = tree;
                }
            }
        }

        public override Task<List<GitCommitRef>> GetCommitsAsync(
            string project,
            string repositoryId,
            GitQueryCommitsCriteria searchCriteria,
            int? skip = null,
            int? top = null,
            object? userState = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_commits.Skip(skip ?? 0).Take(top ?? _commits.Count).Cast<GitCommitRef>().ToList());
        }

        public override Task<GitCommit> GetCommitAsync(
            string project,
            string commitId,
            string repositoryId,
            int? changeCount = null,
            object? userState = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_commitsById[commitId]);
        }

        public override Task<GitItem> GetItemAsync(
            string project,
            string repositoryId,
            string path,
            string? scopePath = null,
            VersionControlRecursionType? recursionLevel = null,
            bool? includeContentMetadata = null,
            bool? latestProcessedChange = null,
            bool? download = null,
            GitVersionDescriptor? versionDescriptor = null,
            bool? includeContent = null,
            bool? resolveLfs = null,
            bool? sanitize = null,
            object? userState = null,
            CancellationToken cancellationToken = default)
        {
            Assert.Equal("/", path);
            Assert.NotNull(versionDescriptor);
            var commitId = versionDescriptor.Version;
            TreeLookups.Add(commitId);
            return Task.FromResult(new GitItem
            {
                GitObjectType = GitObjectType.Tree,
                ObjectId = _trees[commitId],
            });
        }
    }
}
