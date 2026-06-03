// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace RunTests;

/// <summary>
/// A lightweight Azure DevOps REST API client that replaces the heavy
/// Microsoft.TeamFoundationServer.Client package. Inspired by
/// https://github.com/jaredpar/tiger/blob/main/src/Tiger/AzdoClient.cs
/// </summary>
internal sealed class AzdoClient : IDisposable
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _httpClient;

    private AzdoClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public static AzdoClient Create(string projectUri, string accessToken)
    {
        // projectUri is the collection URI (e.g. https://dev.azure.com/dnceng-public/)
        // Ensure it ends with a slash for relative URI resolution
        if (!projectUri.EndsWith("/"))
            projectUri += "/";

        var httpClient = new HttpClient
        {
            BaseAddress = new Uri(projectUri),
        };
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($":{accessToken}")));

        return new AzdoClient(httpClient);
    }

    public void Dispose() => _httpClient.Dispose();

    /// <summary>
    /// Gets the most recent successful CI build for the given definition and branch.
    /// </summary>
    public async Task<AzdoBuild?> GetLastSuccessfulBuildAsync(
        string project,
        int definitionId,
        string branchName,
        CancellationToken cancellationToken)
    {
        var url = $"{project}/_apis/build/builds?api-version=7.1" +
            $"&definitions={definitionId}" +
            $"&resultFilter=succeeded" +
            $"&queryOrder=finishTimeDescending" +
            $"&$top=1" +
            $"&reasonFilter=individualCI" +
            $"&branchName={Uri.EscapeDataString(branchName)}";

        var builds = await GetListAsync<AzdoBuild>(url, cancellationToken);
        return builds.FirstOrDefault();
    }

    /// <summary>
    /// Gets test runs for a build within the build's time range, matching a stage/phase name.
    /// </summary>
    public async Task<AzdoTestRun?> GetRunForStageAsync(
        string project,
        AzdoBuild build,
        string phaseName,
        CancellationToken cancellationToken)
    {
        var minTime = build.QueueTime!.Value.ToString("o");
        var maxTime = build.FinishTime!.Value.ToString("o");

        var url = $"{project}/_apis/test/runs?api-version=7.1" +
            $"&minLastUpdatedDate={Uri.EscapeDataString(minTime)}" +
            $"&maxLastUpdatedDate={Uri.EscapeDataString(maxTime)}" +
            $"&buildIds={build.Id}";

        var runs = await GetListAsync<AzdoTestRun>(url, cancellationToken);

        // If the last successful build had multiple attempts then there are potentially multiple runs with
        // the same name. Take the last one as it will be the successful one.
        return runs.LastOrDefault(r => r.Name.Contains(phaseName));
    }

    /// <summary>
    /// Gets test results for a run with pagination support.
    /// When <paramref name="includeSubResults"/> is true, the request includes
    /// <c>detailsToInclude=SubResults</c> which populates <see cref="AzdoTestResult.SubResultsCount"/>
    /// with the number of sub-results (e.g. individual xUnit theory instances) for grouped tests.
    /// </summary>
    public async Task<List<AzdoTestResult>> GetTestResultsAsync(
        string project,
        int runId,
        int skip,
        int top,
        bool includeSubResults,
        CancellationToken cancellationToken)
    {
        var url = $"{project}/_apis/test/Runs/{runId}/results?api-version=7.1" +
            $"&$skip={skip}" +
            $"&$top={top}";

        if (includeSubResults)
        {
            url += "&detailsToInclude=SubResults";
        }

        return await GetListAsync<AzdoTestResult>(url, cancellationToken);
    }

    private async Task<List<T>> GetListAsync<T>(string url, CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonSerializer.Deserialize<AzdoListResponse<T>>(json, s_jsonOptions)
            ?? throw new InvalidOperationException($"Failed to deserialize response from {url}");

        return result.Value;
    }

    private sealed record AzdoListResponse<T>
    {
        public int Count { get; init; }
        public required List<T> Value { get; init; }
    }
}

/// <summary>
/// Represents an Azure DevOps build from the REST API.
/// </summary>
internal sealed record AzdoBuild
{
    public int Id { get; init; }
    public string? BuildNumber { get; init; }
    public string? Status { get; init; }
    public string? Result { get; init; }
    public string? Url { get; init; }
    public string? SourceBranch { get; init; }
    public DateTime? QueueTime { get; init; }
    public DateTime? FinishTime { get; init; }
}

/// <summary>
/// Represents an Azure DevOps test run from the REST API.
/// </summary>
internal sealed record AzdoTestRun
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public int TotalTests { get; init; }
    public int PassedTests { get; init; }
    public int UnanalyzedTests { get; init; }
    public int NotApplicableTests { get; init; }
}

/// <summary>
/// Represents an individual test result from the Azure DevOps REST API.
/// Includes <see cref="SubResultsCount"/> which exposes the number of sub-results
/// (e.g. xUnit theory instances) that are not available through the old TFS client library.
/// </summary>
internal sealed record AzdoTestResult
{
    public int Id { get; init; }
    public required string AutomatedTestName { get; init; }
    public double DurationInMs { get; init; }
    public string? Outcome { get; init; }

    /// <summary>
    /// The number of sub-results for this test result. For grouped tests (e.g. xUnit theories),
    /// this indicates how many individual theory instances were executed under this parent result.
    /// This is only populated when the request includes <c>detailsToInclude=SubResults</c>.
    /// This field is only available through the REST API and was the motivation for removing
    /// the Microsoft.TeamFoundationServer.Client dependency.
    /// </summary>
    public int SubResultsCount { get; init; }

    public string? ResultGroupType { get; init; }
}
