// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace RunTests;

/// <summary>
/// Reads the real pass/fail of submitted Helix work items from the anonymous Helix job API.
///
/// On a submit-and-forget pipeline (for example the public dnceng roslyn-CI) the "run tests" step
/// returns as soon as the job is queued; the work items then execute on Helix afterward. A zero exit
/// from the submission therefore means "submitted", not "passed". The closure-fingerprint record must
/// be gated on the actual work-item exit codes, so a leg that had any failing work item records nothing
/// and re-runs next time.
/// </summary>
internal static class HelixResults
{
    private const string ApiBase = "https://helix.dot.net/api/2019-06-17/jobs";

    internal readonly record struct JobOutcome(bool Completed, int TestWorkItems, int Failed, bool HasJobErrors);

    /// <summary>
    /// Waits for every job to finish and reports whether all of their test work items exited zero. A
    /// missing job id, an unfinished job, a job-level error, or any failing work item all make this
    /// return false so that nothing is recorded.
    /// </summary>
    internal static async Task<bool> AllTestsPassedAsync(IReadOnlyCollection<string> jobIds, TimeSpan timeout, CancellationToken cancellationToken)
    {
        if (jobIds.Count == 0)
        {
            ConsoleUtil.WriteLine(ConsoleColor.Yellow, "No Helix job id was captured from the submission output; cannot verify results, so nothing is recorded.");
            return false;
        }

        using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
        var allPassed = true;
        foreach (var jobId in jobIds)
        {
            var outcome = await GetJobOutcomeAsync(http, jobId, timeout, cancellationToken).ConfigureAwait(false);
            ConsoleUtil.WriteLine($"Helix job {jobId}: completed={outcome.Completed} testWorkItems={outcome.TestWorkItems} failed={outcome.Failed} jobErrors={outcome.HasJobErrors}");
            if (!outcome.Completed || outcome.HasJobErrors || outcome.Failed > 0)
            {
                allPassed = false;
            }
        }

        return allPassed;
    }

    /// <summary>
    /// Polls a single job to completion, then counts failing test work items. Work items named
    /// "workitem_*" are the test partitions; other names (e.g. "HelixController Work Queueing") are
    /// infrastructure and are covered by the job-level error check instead.
    /// </summary>
    internal static async Task<JobOutcome> GetJobOutcomeAsync(HttpClient http, string jobId, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow + timeout;
        var pollDelay = TimeSpan.FromSeconds(30);
        JObject? details = null;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                details = JObject.Parse(await http.GetStringAsync($"{ApiBase}/{jobId}/details", cancellationToken).ConfigureAwait(false));

                // The job-level "Finished" timestamp is the authoritative completion signal: it is empty
                // while the job runs and set once every work item has finished. The per-state counts are
                // NOT a reliable "done" signal -- immediately after submission, before Helix schedules the
                // work items, running/waiting/unscheduled are all zero even though nothing has run yet.
                var finishedTimestamp = (string?)details["Finished"];
                if (!string.IsNullOrEmpty(finishedTimestamp))
                {
                    break;
                }

                var wi = details["WorkItems"];
                var running = (int?)wi?["Running"] ?? 0;
                var waiting = (int?)wi?["Waiting"] ?? 0;
                var unscheduled = (int?)wi?["Unscheduled"] ?? 0;
                var finished = (int?)wi?["Finished"] ?? 0;
                ConsoleUtil.WriteLine($"Waiting on Helix job {jobId}: finished={finished} running={running} waiting={waiting} unscheduled={unscheduled}");
            }
            catch (HttpRequestException ex)
            {
                // The job details can be briefly unavailable right after submission, or a transient
                // network blip can occur during the long poll. Keep polling until the deadline.
                ConsoleUtil.WriteLine(ConsoleColor.Yellow, $"Transient error polling Helix job {jobId}: {ex.Message}");
            }

            if (DateTime.UtcNow > deadline)
            {
                return new JobOutcome(Completed: false, TestWorkItems: 0, Failed: 0, HasJobErrors: false);
            }

            await Task.Delay(pollDelay, cancellationToken).ConfigureAwait(false);
        }

        var hasJobErrors = details?["Errors"] is JArray errors && errors.Count > 0;

        var items = JArray.Parse(await http.GetStringAsync($"{ApiBase}/{jobId}/workitems", cancellationToken).ConfigureAwait(false));
        var testItems = items.Where(i => ((string?)i["Name"])?.StartsWith("workitem_", StringComparison.Ordinal) == true).ToList();
        var failed = testItems.Count(i => ((int?)i["ExitCode"] ?? 0) != 0);
        return new JobOutcome(Completed: true, TestWorkItems: testItems.Count, Failed: failed, HasJobErrors: hasJobErrors);
    }
}
