// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using LibGit2Sharp;
using Microsoft.Extensions.Logging;

namespace Microsoft.RoslynTools.PRFinder.Hosts;

public class GitHub : IRepositoryHost
{
    internal static readonly Regex IsGitHubReleaseFlowCommit = new(@"^Merge pull request #\d+ from dotnet/merges/");
    internal static readonly Regex IsGitHubMergePRCommit = new(@"^Merge pull request #(\d+) from");
    internal static readonly Regex IsGitHubSquashedPRCommit = new(@"\(#(\d+)\)(?:\n|$)");
    private readonly string _repoUrl;

    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;

    public GitHub(string repoUrl, ILogger logger)
    {
        _repoUrl = repoUrl;
        _logger = logger;
        _logger.LogTrace($"Creating GitHub repository host with base url: {repoUrl}");

        var split = _repoUrl.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var owner = split[^2];
        var repo = split[^1];
        _httpClient = new HttpClient(new LoggingHandler(logger))
        {
            BaseAddress = new Uri($"https://api.github.com/repos/{owner}/{repo}/pulls/"),
        };

        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.text+json"));
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "roslyn-pr-finder");
        _httpClient.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
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
        if (IsGitHubReleaseFlowCommit.Match(commit.MessageShort).Success)
        {
            mergePRFound = true;
            return true;
        }

        return false;
    }

    public async Task<MergeInfo?> TryParseMergeInfoAsync(Commit commit)
    {
        var match = IsGitHubMergePRCommit.Match(commit.MessageShort);
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
            match = IsGitHubSquashedPRCommit.Match(commit.MessageShort);
            if (match.Success)
            {
                var prNumber = match.Groups[1].Value;

                // Squash PR Messages are in the form "Nullable annotate TypeCompilationState and MessageID (#39449)"
                // Take the 1st line since it should be descriptive.
                var comment = commit.MessageShort;
                return new(prNumber, comment);
            }
        }

        return null;
    }

    private async Task<string?> TryGetPrTitleAsync(string prNumber)
    {
        try
        {
            var relativeUrl = $"{prNumber}";
            _logger.LogTrace($"Attempting to fetch PR Title for {prNumber} at {_httpClient.BaseAddress}{relativeUrl}");
            var response = await _httpClient.GetFromJsonAsync<PullRequestResponse>(relativeUrl);

            return response?.Title;
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
            return null;
        }
    }

    private class PullRequestResponse
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = "";

        [JsonPropertyName("description")]
        public string Body { get; set; } = "";
    }

    private class LoggingHandler(ILogger logger) : DelegatingHandler(new HttpClientHandler())
    {
        private ILogger _logger = logger;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _logger.LogTrace("Request:");
            _logger.LogTrace(request.RequestUri?.ToString() ?? request.ToString());
            if (request.Content != null)
            {
                _logger.LogTrace(await request.Content.ReadAsStringAsync());
            }

            _logger.LogTrace("");

            var response = await base.SendAsync(request, cancellationToken);

            _logger.LogTrace("Response:");
            _logger.LogTrace(response.ToString());
            if (response.Content != null)
            {
                _logger.LogTrace(await response.Content.ReadAsStringAsync());
            }

            _logger.LogTrace("");

            return response;
        }
    }
}
