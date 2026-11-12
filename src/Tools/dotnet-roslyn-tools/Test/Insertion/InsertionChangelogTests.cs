// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using System.Web;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.RoslynTools.Authentication;
using Microsoft.RoslynTools.Insertion;
using Microsoft.RoslynTools.PRFinder.Formatters;
using Microsoft.RoslynTools.PRFinder.Hosts;
using Microsoft.RoslynTools.Utilities;
using Newtonsoft.Json;

namespace Microsoft.RoslynTools.UnitTests.Insertion;

public class InsertionChangelogTests
{
    [Fact]
    public async Task SnapChangelogContainsOnlySnapAndPostSnapUpdate()
    {
        using var client = new HttpClient(new Handler(request =>
        {
            var path = GetRequestUri(request).AbsolutePath;
            return path switch
            {
                "/repos/dotnet/roslyn/compare/base...head" => Response(new
                {
                    total_commits = 5,
                    commits = new[]
                    {
                        Commit("discarded", "Old insiders update (#85189)", "old-target-tree", "base"),
                        Commit("snap", "Merge main into release/insiders", "source-tree", "discarded", "base"),
                        Commit("config", "Update PublishData.json", "config-tree", "snap"),
                        Commit("snap-pr", "Snap main into release/insiders (18.12) (#85819)", "config-tree", "discarded", "config"),
                        Commit("head", "Update Gladstone (#85834)", "head-tree", "snap-pr"),
                    }
                }),
                "/repos/dotnet/roslyn/commits/base" => Response(new { commit = new { tree = new { sha = "source-tree" } } }),
                _ => throw new InvalidOperationException($"Unexpected request: {path}"),
            };
        }));

        var (changes, diffLink) = await RoslynInsertionTool.GetChangesBetweenBuildsFromGitHubAsync(client, "dotnet/roslyn", "base", "head");

        Assert.Equal(["head", "snap-pr", "config", "snap"], changes.Select(commit => commit.CommitId));
        Assert.Equal("//github.com/dotnet/roslyn/compare/base...head?w=1", diffLink);

        using var connections = new RemoteConnections(new RoslynToolsSettings { GitHubToken = "test-token" }, NullLogger.Instance, loginToAzureDevOps: false);
        var host = new GitHub("https://github.com/dotnet/roslyn", connections, NullLogger.Instance);
        var description = new StringBuilder();
        await Microsoft.RoslynTools.PRFinder.PRFinder.AppendChangesToDescriptionAsync(changes, host, new DefaultFormatter(), [], description);

        Assert.Equal(
            "- [Update Gladstone](https://github.com/dotnet/roslyn/pull/85834)" + Environment.NewLine
            + "- [Snap main into release/insiders (18.12)](https://github.com/dotnet/roslyn/pull/85819)" + Environment.NewLine,
            description.ToString());
    }

    [Fact]
    public async Task MissingParentTreeIsFetchedOnlyOnceAcrossMerges()
    {
        var requests = new List<string>();
        using var client = new HttpClient(new Handler(request =>
        {
            var path = GetRequestUri(request).AbsolutePath;
            requests.Add(path);
            return path switch
            {
                "/repos/dotnet/roslyn/compare/base...head" => Response(new
                {
                    total_commits = 5,
                    commits = new[]
                    {
                        Commit("old-left", "Old left update (#1)", "old-left-tree", "base"),
                        Commit("old-right", "Old right update (#2)", "old-right-tree", "base"),
                        Commit("left", "Replace left content (#3)", "base-tree", "old-left", "base"),
                        Commit("right", "Replace right content (#4)", "base-tree", "old-right", "base"),
                        Commit("head", "Combine histories (#5)", "head-tree", "left", "right"),
                    }
                }),
                "/repos/dotnet/roslyn/commits/base" => Response(new { commit = new { tree = new { sha = "base-tree" } } }),
                _ => throw new InvalidOperationException($"Unexpected request: {path}"),
            };
        }));

        var (changes, _) = await RoslynInsertionTool.GetChangesBetweenBuildsFromGitHubAsync(client, "dotnet/roslyn", "base", "head");

        Assert.Equal(["head", "right", "left"], changes.Select(commit => commit.CommitId));
        Assert.Single(requests, path => path == "/repos/dotnet/roslyn/commits/base");
    }

    [Fact]
    public async Task FetchesAllComparisonPagesBeforeFiltering()
    {
        var commits = Enumerable.Range(1, 101)
            .Select(i => Commit($"commit-{i}", $"Update (#{i})", $"tree-{i}", i == 1 ? "base" : $"commit-{i - 1}"))
            .ToArray();
        using var client = new HttpClient(new Handler(request =>
        {
            var query = HttpUtility.ParseQueryString(GetRequestUri(request).Query);
            var pageSize = int.Parse(query["per_page"] ?? "30");
            var page = int.Parse(query["page"] ?? "1");
            return Response(new
            {
                total_commits = commits.Length,
                commits = commits.Skip((page - 1) * pageSize).Take(pageSize).ToArray(),
            });
        }));

        var (changes, _) = await RoslynInsertionTool.GetChangesBetweenBuildsFromGitHubAsync(client, "dotnet/roslyn", "base", "commit-101");

        Assert.Equal(Enumerable.Range(1, 101).Reverse().Select(i => $"commit-{i}"), changes.Select(commit => commit.CommitId));
    }

    private static object Commit(string id, string message, string tree, params string[] parents)
        => new
        {
            sha = id,
            parents = parents.Select(parent => new { sha = parent }).ToArray(),
            commit = new
            {
                author = new { name = "Contributor", date = "2026-09-30T00:00:00Z" },
                committer = new { name = message.Contains("(#", StringComparison.Ordinal) ? "GitHub" : "Contributor" },
                message,
                tree = new { sha = tree },
            },
            html_url = $"https://github.com/dotnet/roslyn/commit/{id}",
        };

    private static HttpResponseMessage Response(object content)
        => new() { Content = new StringContent(JsonConvert.SerializeObject(content)) };

    private static Uri GetRequestUri(HttpRequestMessage request)
        => request.RequestUri ?? throw new InvalidOperationException("Request URI was not set.");

    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(send(request));
        }
    }
}
