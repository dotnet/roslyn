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
        Assert.Equal(["snap-pr", "discarded", "config", "snap", "base"], client.TreeLookups);
    }

    [Fact]
    public async Task SnapMessageWithoutSourceTreePreservesTargetChanges()
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
    }

    [Fact]
    public async Task FetchesAllCommitPagesBeforeFiltering()
    {
        var commits = Enumerable.Range(1, 1001).Reverse()
            .Select(i => Commit($"commit-{i}", $"Update (#{i})", i == 1 ? "base" : $"commit-{i - 1}"))
            .ToArray();
        var releaseDetails = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var pendingDetails = 0;
        using var client = new Client(commits, new Dictionary<string, string>())
        {
            BeforeCommitDetailsAsync = async (_, token) =>
            {
                var pending = Interlocked.Increment(ref pendingDetails);
                try
                {
                    Assert.InRange(pending, 1, 16);
                    await releaseDetails.Task.WaitAsync(token);
                    await Task.Yield();
                }
                finally
                {
                    Interlocked.Decrement(ref pendingDetails);
                }
            },
        };

        var getChanges = RoslynInsertionTool.GetChangesBetweenBuildsFromAzDOAsync(client, "project", "repo", RepoUrl, "base", "commit-1001");
        try
        {
            Assert.Equal(16, client.CommitDetailRequests.Count);
            Assert.Equal(16, Volatile.Read(ref pendingDetails));
        }
        finally
        {
            releaseDetails.SetResult();
        }

        var (changes, _) = await getChanges;

        Assert.Equal(commits.Select(commit => commit.CommitId), changes.Select(commit => commit.CommitId));
        Assert.Equal(commits.Select(commit => commit.CommitId), client.CommitDetailRequests);
        Assert.Equal(0, pendingDetails);
        Assert.Equal([0, 1000], client.PageOffsets);
        Assert.Empty(client.TreeLookups);
    }

    [Fact]
    public async Task OutOfOrderDetailResponsesPreserveCommitOrder()
    {
        var commits = new[]
        {
            Commit("head", "Latest update (#3)", "middle"),
            Commit("middle", "Middle update (#2)", "first"),
            Commit("first", "First update (#1)", "base"),
        };
        var responses = commits.ToDictionary(
            commit => commit.CommitId,
            _ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));
        using var client = new Client(commits, new Dictionary<string, string>())
        {
            BeforeCommitDetailsAsync = (id, token) => responses[id].Task.WaitAsync(token),
        };

        var getChanges = RoslynInsertionTool.GetChangesBetweenBuildsFromAzDOAsync(client, "project", "repo", RepoUrl, "base", "head");
        Assert.Equal(["head", "middle", "first"], client.CommitDetailRequests);
        responses["first"].SetResult();
        responses["middle"].SetResult();
        Assert.False(getChanges.IsCompleted);
        responses["head"].SetResult();
        var (changes, _) = await getChanges;

        Assert.Equal(["head", "middle", "first"], changes.Select(commit => commit.CommitId));
    }

    [Fact]
    public async Task DetailFailurePreventsLaterBatches()
    {
        var commits = Enumerable.Range(1, 33).Reverse()
            .Select(i => Commit($"commit-{i}", $"Update (#{i})", i == 1 ? "base" : $"commit-{i - 1}"))
            .ToArray();
        var releaseDetails = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var failure = new IOException("Commit details unavailable.");
        using var client = new Client(commits, new Dictionary<string, string>())
        {
            BeforeCommitDetailsAsync = (id, token) => id == "commit-33"
                ? Task.FromException(failure)
                : releaseDetails.Task.WaitAsync(token),
        };

        var getChanges = RoslynInsertionTool.GetChangesBetweenBuildsFromAzDOAsync(client, "project", "repo", RepoUrl, "base", "commit-33");
        Assert.Equal(16, client.CommitDetailRequests.Count);
        Assert.False(getChanges.IsCompleted);
        releaseDetails.SetResult();
        var actualFailure = await Assert.ThrowsAsync<IOException>(() => getChanges);

        Assert.Same(failure, actualFailure);
        Assert.Equal(commits.Take(16).Select(commit => commit.CommitId), client.CommitDetailRequests);
        Assert.Empty(client.TreeLookups);
    }

    [Fact]
    public async Task CancellationDuringDetailsPreventsLaterBatches()
    {
        var commits = Enumerable.Range(1, 33).Reverse()
            .Select(i => Commit($"commit-{i}", $"Update (#{i})", i == 1 ? "base" : $"commit-{i - 1}"))
            .ToArray();
        using var cancellation = new CancellationTokenSource();
        var releaseDetails = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var client = new Client(commits, new Dictionary<string, string>())
        {
            BeforeCommitDetailsAsync = (_, token) =>
            {
                Assert.Equal(cancellation.Token, token);
                return releaseDetails.Task.WaitAsync(token);
            },
        };

        var getChanges = RoslynInsertionTool.GetChangesBetweenBuildsFromAzDOAsync(client, "project", "repo", RepoUrl, "base", "commit-33", cancellation.Token);
        Assert.Equal(16, client.CommitDetailRequests.Count);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => getChanges);

        Assert.Equal(commits.Take(16).Select(commit => commit.CommitId), client.CommitDetailRequests);
        Assert.Empty(client.TreeLookups);
    }

    [Fact]
    public async Task CancellationBetweenBatchesPreventsFurtherRequests()
    {
        var commits = Enumerable.Range(1, 33).Reverse()
            .Select(i => Commit($"commit-{i}", $"Update (#{i})", i == 1 ? "base" : $"commit-{i - 1}"))
            .ToArray();
        using var cancellation = new CancellationTokenSource();
        using var client = new Client(commits, new Dictionary<string, string>())
        {
            BeforeCommitDetailsAsync = (id, _) =>
            {
                if (id == "commit-18")
                {
                    cancellation.Cancel();
                }

                return Task.CompletedTask;
            },
        };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            RoslynInsertionTool.GetChangesBetweenBuildsFromAzDOAsync(client, "project", "repo", RepoUrl, "base", "commit-33", cancellation.Token));

        Assert.Equal(commits.Take(16).Select(commit => commit.CommitId), client.CommitDetailRequests);
        Assert.Empty(client.TreeLookups);
    }

    [Fact]
    public async Task MatchingFirstParentExcludesSecondParentHistory()
    {
        var commits = new[]
        {
            Commit("merge", "Preserve target snapshot (#3)", "target", "source"),
            Commit("target", "Target update (#1)", "base"),
            Commit("source", "Source update (#2)", "base"),
        };
        using var client = new Client(commits, new Dictionary<string, string>
        {
            ["merge"] = "target-tree",
            ["target"] = "target-tree",
            ["source"] = "source-tree",
        });

        var (changes, _) = await RoslynInsertionTool.GetChangesBetweenBuildsFromAzDOAsync(client, "project", "repo", RepoUrl, "base", "merge");

        Assert.Equal(["merge", "target"], changes.Select(commit => commit.CommitId));
    }

    [Fact]
    public async Task NonTreeRootIsReported()
    {
        using var client = new Client(
            [Commit("merge", "Merge (#3)", "target", "source")],
            new Dictionary<string, string> { ["merge"] = "object-id" })
        {
            RootType = GitObjectType.Blob,
        };

        await Assert.ThrowsAsync<InvalidDataException>(() =>
            RoslynInsertionTool.GetChangesBetweenBuildsFromAzDOAsync(client, "project", "repo", RepoUrl, "base", "merge"));
    }

    private static GitCommit Commit(string id, string message, params string[] parents)
    {
        var links = new ReferenceLinks();
        links.AddLink("web", RepoUrl + "/commit/" + id);
        return new GitCommit
        {
            CommitId = id,
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

        public List<int> PageOffsets { get; } = [];
        public List<string> CommitDetailRequests { get; } = [];
        public List<string> TreeLookups { get; } = [];
        public GitObjectType RootType { get; init; } = GitObjectType.Tree;
        public Func<string, CancellationToken, Task>? BeforeCommitDetailsAsync { get; init; }

        public Client(IReadOnlyList<GitCommit> commits, IReadOnlyDictionary<string, string> trees)
            : base(new Uri("https://dev.azure.com/example"), new VssBasicCredential("", "test-token"))
        {
            _commits = commits;
            _commitsById = commits.ToDictionary(commit => commit.CommitId);
            _trees = trees;
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
            cancellationToken.ThrowIfCancellationRequested();
            Assert.Equal("base", searchCriteria.ItemVersion.Version);
            Assert.Equal(GitVersionType.Commit, searchCriteria.ItemVersion.VersionType);
            Assert.Equal(GitVersionType.Commit, searchCriteria.CompareVersion.VersionType);
            Assert.Equal(1000, top);
            PageOffsets.Add(skip ?? 0);
            return Task.FromResult(_commits.Skip(skip ?? 0).Take(top ?? 1000).Cast<GitCommitRef>().ToList());
        }

        public override async Task<GitCommit> GetCommitAsync(
            string project,
            string commitId,
            string repositoryId,
            int? changeCount = null,
            object? userState = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CommitDetailRequests.Add(commitId);
            if (BeforeCommitDetailsAsync is not null)
            {
                await BeforeCommitDetailsAsync(commitId, cancellationToken);
            }

            return _commitsById[commitId];
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
            cancellationToken.ThrowIfCancellationRequested();
            Assert.Equal("/", path);
            Assert.Equal(VersionControlRecursionType.None, recursionLevel);
            Assert.False(includeContent);
            Assert.NotNull(versionDescriptor);
            Assert.Equal(GitVersionType.Commit, versionDescriptor.VersionType);
            var commitId = versionDescriptor.Version;
            TreeLookups.Add(commitId);
            return Task.FromResult(new GitItem
            {
                GitObjectType = RootType,
                ObjectId = _trees[commitId],
            });
        }
    }
}
