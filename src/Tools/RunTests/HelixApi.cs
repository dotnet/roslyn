// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace RunTests;

/// <summary>
/// Helper class for interacting with the Helix API to query job and work item status, fetch logs, and cancel jobs. Based
/// on the API documented at https://helix.dot.net/swagger/ui
/// </summary>
internal sealed class HelixApi : IDisposable
{
    private const string HelixBaseUrl = "https://helix.dot.net";
    private const string ApiVersion = "2019-06-17";

    private readonly HttpClient _httpClient;

    public HelixApi(string? bearerToken = null)
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(HelixBaseUrl)
        };
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (!string.IsNullOrEmpty(bearerToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        }
    }

    /// <summary>
    /// GET /api/jobs/{job}/details
    /// Provides job summary data along with aggregated work item statistics.
    /// </summary>
    public async Task<HelixJobDetails> GetJobDetailsAsync(string job, CancellationToken cancellationToken = default)
    {
        var url = $"/api/jobs/{Uri.EscapeDataString(job)}/details?api-version={ApiVersion}";
        return await GetAsync<HelixJobDetails>(url, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// POST /api/jobs/{job}/cancel
    /// Cancels a running job. Requires authentication.
    /// </summary>
    public async Task CancelJobAsync(string job, string? jobCancellationToken = null, CancellationToken cancellationToken = default)
    {
        var url = $"/api/jobs/{Uri.EscapeDataString(job)}/cancel?api-version={ApiVersion}";
        if (!string.IsNullOrEmpty(jobCancellationToken))
        {
            url += $"&jobCancellationToken={Uri.EscapeDataString(jobCancellationToken)}";
        }

        using var response = await _httpClient.PostAsync(url, content: null, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// GET /api/jobs/{job}/workitems/{id}
    /// Fetches details about a single work item including logs and uploaded files.
    /// </summary>
    public async Task<HelixWorkItemDetails> GetWorkItemDetailsAsync(string job, string id, CancellationToken cancellationToken = default)
    {
        var url = $"/api/jobs/{Uri.EscapeDataString(job)}/workitems/{Uri.EscapeDataString(id)}?api-version={ApiVersion}";
        return await GetAsync<HelixWorkItemDetails>(url, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// GET /api/jobs/{job}/workitems
    /// Lists all work items for a given job.
    /// </summary>
    public async Task<List<HelixWorkItemSummary>> GetWorkItemsAsync(string job, CancellationToken cancellationToken = default)
    {
        var url = $"/api/jobs/{Uri.EscapeDataString(job)}/workitems?api-version={ApiVersion}";
        return await GetAsync<List<HelixWorkItemSummary>>(url, cancellationToken).ConfigureAwait(false);
    }

    private async Task<T> GetAsync<T>(string url, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Failed to deserialize response from {url}");
    }

    /// <summary>
    /// Gets the URL for a work item details page.
    /// </summary>
    public static string GetWorkItemUrl(string job, string workItemName)
        => $"{HelixBaseUrl}/api/jobs/{Uri.EscapeDataString(job)}/workitems/{Uri.EscapeDataString(workItemName)}?api-version={ApiVersion}";

    /// <summary>
    /// Gets the URL for a work item's console output.
    /// </summary>
    public static string GetWorkItemConsoleUrl(string job, string workItemName)
        => $"{HelixBaseUrl}/api/jobs/{Uri.EscapeDataString(job)}/workitems/{Uri.EscapeDataString(workItemName)}/console?api-version={ApiVersion}";

    public void Dispose() => _httpClient.Dispose();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
}

// ---- Response model types ----

internal sealed class HelixFailureReasonPart
{
    public string? Display { get; set; }
    public string? Link { get; set; }
}

internal sealed class HelixFailureReason
{
    public HelixFailureReasonPart? Issue { get; set; }
    public HelixFailureReasonPart? Owner { get; set; }
}

internal sealed class HelixWorkItemError
{
    public string Id { get; set; } = "";
    public string Message { get; set; } = "";
    public string? LogUri { get; set; }
}

internal sealed class HelixJobWorkItemCounts
{
    public int Unscheduled { get; set; }
    public int Waiting { get; set; }
    public int Running { get; set; }
    public int Finished { get; set; }
    public string ListUrl { get; set; } = "";
}

internal sealed class HelixJobDetails
{
    public HelixFailureReason? FailureReason { get; set; }
    public string? QueueId { get; set; }
    public string JobList { get; set; } = "";
    public HelixJobWorkItemCounts WorkItems { get; set; } = new();
    public string Name { get; set; } = "";
    public string? Creator { get; set; }
    public string? Created { get; set; }
    public string? Finished { get; set; }
    public int? InitialWorkItemCount { get; set; }
    public string WaitUrl { get; set; } = "";
    public string Source { get; set; } = "";
    public string Type { get; set; } = "";
    public string Build { get; set; } = "";
    public object? Properties { get; set; }
    public List<HelixWorkItemError>? Errors { get; set; }
}

internal sealed class HelixWorkItemSummary
{
    public string DetailsUrl { get; set; } = "";
    public int? ExitCode { get; set; }
    public string? ConsoleOutputUri { get; set; }
    public string Job { get; set; } = "";
    public string Name { get; set; } = "";
    public string State { get; set; } = "";
}

internal sealed class HelixWorkItemLog
{
    public string Module { get; set; } = "";
    public string Uri { get; set; } = "";
}

internal sealed class HelixWorkItemFile
{
    public string FileName { get; set; } = "";
    public string Uri { get; set; } = "";
}

internal sealed class HelixWorkItemDetails
{
    public HelixFailureReason? FailureReason { get; set; }
    public string? Queued { get; set; }
    public string? Started { get; set; }
    public string? Finished { get; set; }
    public string? Delay { get; set; }
    public string? Duration { get; set; }
    public string Id { get; set; } = "";
    public string MachineName { get; set; } = "";
    public int? ExitCode { get; set; }
    public string? ConsoleOutputUri { get; set; }
    public List<HelixWorkItemError>? Errors { get; set; }
    public List<HelixWorkItemError>? Warnings { get; set; }
    public List<HelixWorkItemLog>? Logs { get; set; }
    public List<HelixWorkItemFile>? Files { get; set; }
    public List<object>? OtherEvents { get; set; }
    public string Job { get; set; } = "";
    public string Name { get; set; } = "";
    public string State { get; set; } = "";
}
