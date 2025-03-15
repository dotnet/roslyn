// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using Microsoft.RoslynTools.Utilities;

namespace Microsoft.RoslynTools.PRFinder.Hosts;

internal partial class GitHub : IRepositoryHost
{
    private readonly string _repoUrl;
    private readonly string _owner;
    private readonly string _repo;
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;

    public GitHub(string repoUrl, RemoteConnections connections, ILogger logger)
    {
        _repoUrl = repoUrl;
        var split = _repoUrl.Split('/', StringSplitOptions.RemoveEmptyEntries);
        _owner = split[^2];
        _repo = split[^1];

        _httpClient = connections.GitHubClient;
        _logger = logger;
    }

    public string GetCommitUrl(string commitSha)
        => $"{_repoUrl}/commit/{commitSha}";

    public string GetDiffUrl(string startRef, string endRef)
        => $"{_repoUrl}/compare/{startRef}...{endRef}?w=1";

    public string GetPullRequestUrl(string prNumber)
        => $"{_repoUrl}/pull/{prNumber}";

    public bool ShouldSkip(Commit commit, ref bool mergePRFound)
    {
        // Once we've found a Merge PR we can exclude commits not committed by GitHub since Merge and Squash commits are committed by GitHub
        if (commit.Committer.Name != "GitHub" && mergePRFound)
        {
            return true;
        }

        // Exclude arcade dependency updates
        if (commit.Author.Name == "dotnet-maestro[bot]")
        {
            mergePRFound = true;
            return true;
        }

        // Exclude merge commits from auto code-flow PRs (e.g. merges/main-to-main-vs-deps)
        if (IsGitHubReleaseFlowCommit().Match(commit.MessageShort).Success ||
            IsGitHubActionCodeFlowCommit().Match(commit.MessageShort).Success)
        {
            mergePRFound = true;
            return true;
        }

        return false;
    }

    public async Task<MergeInfo?> TryParseMergeInfoAsync(Commit commit)
    {
        var match = IsGitHubMergePRCommit().Match(commit.MessageShort);
        if (match.Success)
        {
            var prNumber = match.Groups[1].Value;

            // Merge PR Messages are in the form "Merge pull request #39526 from mavasani/GetValueUsageInfoAssert\n\nFix an assert in IOperationExtension.GetValueUsageInfo"
            // Try and extract the 1st non-empty line since it is the useful part of the message, otherwise take the first line.
            var lines = commit.Message.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            if (lines.Length > 1)
            {
                return new MergeInfo(prNumber, $"{lines[1]} (#{prNumber})");
            }

            var comment = await TryGetPrTitleAsync(prNumber);

            // Fallback to the commit message if the PR Title can't be retrieved
            comment ??= commit.MessageShort;

            return new(prNumber, comment);
        }
        else
        {
            match = IsGitHubSquashedPRCommit().Match(commit.MessageShort);
            if (match.Success)
            {
                var prNumber = match.Groups[2].Value;

                // Squash PR Messages are in the form "Nullable annotate TypeCompilationState and MessageID (#39449)"
                // Take the 1st line since it should be descriptive.
                var comment = match.Groups[1].Value;
                return new(prNumber, comment);
            }
        }

        return null;
    }

    private async Task<string?> TryGetPrTitleAsync(string prNumber)
    {
        try
        {
            _logger.LogTrace("Attempting to fetch PR Title for {PrNumber}", prNumber);
            var pullRequest = await GetPullRequestAsync(prNumber);

            return pullRequest?.Title;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "{Message}", e.Message);
            return null;
        }
    }

    public async Task<bool> HasAnyLabelAsync(string prNumber, string[] labels)
    {
        try
        {
            _logger.LogTrace("Attempting to fetch PR Labels for {PrNumber}", prNumber);
            var pullRequest = await GetPullRequestAsync(prNumber);

            return pullRequest?.Labels.Any(label => labels.Contains(label.Name, StringComparer.OrdinalIgnoreCase)) ?? false;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "{Message}", e.Message);
            return false;
        }
    }

    private Task<PullRequestResponse?> GetPullRequestAsync(string prNumber)
    {
        var relativeUrl = $"repos/{_owner}/{_repo}/pulls/{prNumber}";
        _logger.LogTrace("Attempting to fetch PR for {PrNumber} at {BaseAddress}{RelativeUrl}", prNumber, _httpClient.BaseAddress, relativeUrl);
        return _httpClient.GetFromJsonAsync<PullRequestResponse>(relativeUrl);
    }

    private class PullRequestResponse
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = "";

        [JsonPropertyName("description")]
        public string Body { get; set; } = "";

        [JsonPropertyName("labels")]
        public PullRequestLabel[] Labels { get; set; } = [];
    }

    private class PullRequestLabel
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";
    }

    private class LoggingHandler(ILogger logger) : DelegatingHandler(new HttpClientHandler())
    {
        private readonly ILogger _logger = logger;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _logger.LogTrace("Request:");
            _logger.LogTrace("{Request}", request.RequestUri?.ToString() ?? request.ToString());
            if (request.Content != null)
            {
                _logger.LogTrace("{Content}", await request.Content.ReadAsStringAsync(cancellationToken));
            }

            _logger.LogTrace("");

            var response = await base.SendAsync(request, cancellationToken);

            _logger.LogTrace("Response:");
            _logger.LogTrace("{Repsonse}", response.ToString());
            if (response.Content != null)
            {
                _logger.LogTrace("{Content}", await response.Content.ReadAsStringAsync(cancellationToken));
            }

            _logger.LogTrace("");

            return response;
        }
    }

    [GeneratedRegex(@"^Merge pull request #\d+ from dotnet/merges/")]
    private static partial Regex IsGitHubReleaseFlowCommit();
    [GeneratedRegex(@"^\[automated\] Merge branch '.*' => '.*' \(#\d+\)")]
    private static partial Regex IsGitHubActionCodeFlowCommit();
    [GeneratedRegex(@"^Merge pull request #(\d+) from")]
    private static partial Regex IsGitHubMergePRCommit();
    [GeneratedRegex(@"^(.*) \(#(\d+)\)(?:\n|$)")]
    private static partial Regex IsGitHubSquashedPRCommit();
}
