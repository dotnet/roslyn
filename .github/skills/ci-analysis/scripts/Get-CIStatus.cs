#!/usr/bin/env dotnet
/*
SYNOPSIS
    Retrieves test failures from Azure DevOps builds and Helix test runs.

DESCRIPTION
    This script queries Azure DevOps for failed jobs in a build and retrieves
    the corresponding Helix console logs to show detailed test failure information.
    It can also directly query a specific Helix job and work item.

USAGE
    ./Get-CIStatus.cs -BuildId 1276327
    ./Get-CIStatus.cs -PRNumber 123445 -ShowLogs
    ./Get-CIStatus.cs -PRNumber 123445 -Repository dotnet/aspnetcore
    ./Get-CIStatus.cs -HelixJob "4b24b2c2-ad5a-4c46-8a84-844be03b1d51" -WorkItem "iOS.Device.Aot.Test"
    ./Get-CIStatus.cs -BuildId 1276327 -SearchMihuBot
    ./Get-CIStatus.cs -HelixJob "4b24b2c2-ad5a-4c46-8a84-844be03b1d51" -FindBinlogs
    ./Get-CIStatus.cs -ClearCache
*/
#nullable enable
// File-based app JSON serialization uses reflection-heavy APIs and is not intended for trimming or AOT publishing.
#pragma warning disable IL2026, IL3050

using System.Diagnostics;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

Options options;
try
{
    options = Options.Parse(args);
}
catch (ArgumentException ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    Environment.Exit(1);
    return;
}
var app = new CiAnalysisApp(options);
Environment.Exit(await app.RunAsync());

sealed class CiAnalysisApp
{
    private static readonly Regex RepositoryPattern = new("^[a-zA-Z0-9_.-]+/[a-zA-Z0-9_.-]+$", RegexOptions.Compiled);
    private static readonly Regex FailingBuildRegex = new("fail.*buildId=(\\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex AnyBuildRegex = new("buildId=(\\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex BuildAnalysisIssueRegex = new("<a href=\"(https://github\\.com/[^/]+/[^/]+/issues/(\\d+))\">([^<]+)</a>", RegexOptions.Compiled);
    private static readonly Regex HelixUrlRegex = new("https://helix\\.dot\\.net/api/[^/]+/jobs/[a-f0-9-]+/workitems/[^/\\s]+/console", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex TestFailureRegex = new("error\\s*:\\s*.*Test\\s+(\\S+)\\s+has failed", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex HelixLogUrlRegex = new("https://helix\\.dot\\.net/api/[^/]+/jobs/([a-f0-9-]+)/workitems/([^/\\s]+)/console", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex TestRunRegex = new("Published Test Run\\s*:\\s*(https://dev\\.azure\\.com/[^/]+/[^/]+/_TestManagement/Runs\\?runId=(\\d+)[^\\s]*)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex SafeSearchRegex = new("[^\\w\\s\\-.:/]", RegexOptions.Compiled);
    private static readonly Regex ValidShaRegex = new("^[a-fA-F0-9]{40}$", RegexOptions.Compiled);
    private static readonly Regex StackTraceSearchRegex = new("at\\s+(\\w+\\.\\w+)\\(", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex SpecificExceptionRegex = new("(System\\.(?:InvalidOperation|ArgumentNull|Format)\\w*Exception)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex FailedTestInMessageRegex = new("(\\S+)\\s+\\[FAIL\\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly string[] BuildErrorPatterns =
    [
        "error\\s+CS\\d+:.*",
        "error\\s+MSB\\d+:.*",
        "error\\s+NU\\d+:.*",
        "\\.pcm: No such file or directory",
        "EXEC\\s*:\\s*error\\s*:.*",
        "fatal error:.*",
        ":\\s*error:",
        "undefined reference to",
        "cannot find -l",
        "collect2: error:",
        "##\\[error\\].*"
    ];
    private static readonly string[] FailureStartPatterns =
    [
        "\\[FAIL\\]",
        "Assert\\.\\w+\\(\\)\\s+Failure",
        "Expected:.*but was:",
        "BUG:",
        "FAILED\\s*$",
        "END EXECUTION - FAILED",
        "System\\.\\w+Exception:",
        "Timed Out \\(timeout"
    ];

    private readonly Options _options;
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly string _tempDir;
    private readonly string _cacheDir;

    public CiAnalysisApp(Options options)
    {
        _options = options;
        _tempDir = GetTempDirectory();
        _cacheDir = Path.Combine(_tempDir, "ci-analysis-cache");
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Get-CIStatus", "1.0"));
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };

        if (!_options.ClearCache)
        {
            Directory.CreateDirectory(_cacheDir);
            if (!_options.NoCache)
            {
                ClearExpiredCache(_options.CacheTtlSeconds);
            }
        }
    }

    public async Task<int> RunAsync()
    {
        try
        {
            if (_options.ClearCache)
            {
                ClearCache();
                return 0;
            }

            if (_options.HelixJob is not null)
            {
                await RunHelixModeAsync();
                return 0;
            }

            var buildIds = new List<int>();
            var knownIssuesFromBuildAnalysis = new List<KnownIssue>();
            var prChangedFiles = new List<string>();
            string? noBuildReason = null;
            string? mergeState = null;

            if (_options.PrNumber is int prNumber)
            {
                var buildResult = await GetAzdoBuildIdFromPrAsync(prNumber);
                noBuildReason = buildResult.Reason;
                mergeState = buildResult.MergeState;
                if (buildResult.Reason is not null)
                {
                    var summary = new JsonObject
                    {
                        ["mode"] = "PRNumber",
                        ["repository"] = _options.Repository,
                        ["prNumber"] = prNumber,
                        ["builds"] = new JsonArray(),
                        ["totalFailedJobs"] = 0,
                        ["totalLocalFailures"] = 0,
                        ["lastBuildJobSummary"] = CreateJobSummaryObject(0, 0, 0, 0, 0, 0, 0),
                        ["failedJobNames"] = new JsonArray(),
                        ["failedJobDetails"] = new JsonArray(),
                        ["failedJobDetailsTruncated"] = false,
                        ["canceledJobNames"] = new JsonArray(),
                        ["knownIssues"] = new JsonArray(),
                        ["prCorrelation"] = new JsonObject
                        {
                            ["changedFileCount"] = 0,
                            ["hasCorrelation"] = false,
                            ["correlatedFiles"] = new JsonArray()
                        },
                        ["recommendationHint"] = noBuildReason == "MERGE_CONFLICTS" ? "MERGE_CONFLICTS" : "NO_BUILDS",
                        ["noBuildReason"] = noBuildReason,
                        ["mergeState"] = mergeState
                    };

                    Console.WriteLine();
                    EmitSummary(summary);
                    return 0;
                }

                buildIds.AddRange(buildResult.BuildIds);
                knownIssuesFromBuildAnalysis.AddRange(await GetBuildAnalysisKnownIssuesAsync(prNumber));
                prChangedFiles.AddRange(await GetPrChangedFilesAsync(prNumber));
            }
            else if (_options.BuildId is int buildId)
            {
                buildIds.Add(buildId);
            }

            var totalFailedJobs = 0;
            var totalLocalFailures = 0;
            var allFailuresForCorrelation = new List<CorrelationFailure>();
            var allFailedJobNames = new List<string>();
            var allCanceledJobNames = new List<string>();
            var allFailedJobDetails = new List<JsonObject>();
            JobSummary? lastBuildJobSummary = null;

            foreach (var currentBuildId in buildIds)
            {
                WriteSection($"=== Azure DevOps Build {currentBuildId} ===", ConsoleColor.Yellow);
                WriteLine($"URL: https://dev.azure.com/{_options.Organization}/{_options.Project}/_build/results?buildId={currentBuildId}", ConsoleColor.DarkGray);

                var buildStatus = await GetAzdoBuildStatusAsync(currentBuildId);
                if (buildStatus is not null)
                {
                    var statusColor = buildStatus.Status switch
                    {
                        "inProgress" => ConsoleColor.Cyan,
                        "completed" when string.Equals(buildStatus.Result, "succeeded", StringComparison.OrdinalIgnoreCase) => ConsoleColor.Green,
                        "completed" => ConsoleColor.Red,
                        _ => ConsoleColor.Gray,
                    };

                    var statusText = buildStatus.Status ?? "unknown";
                    if (string.Equals(buildStatus.Status, "completed", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(buildStatus.Result))
                    {
                        statusText = $"{buildStatus.Status} ({buildStatus.Result})";
                    }
                    else if (string.Equals(buildStatus.Status, "inProgress", StringComparison.OrdinalIgnoreCase))
                    {
                        statusText = "IN PROGRESS - showing failures so far";
                    }

                    WriteLine($"Status: {statusText}", statusColor);
                }

                var isInProgress = string.Equals(buildStatus?.Status, "inProgress", StringComparison.OrdinalIgnoreCase);
                var timeline = await GetAzdoTimelineAsync(currentBuildId, isInProgress);
                if (timeline is null)
                {
                    Console.WriteLine();
                    WriteLine("Could not fetch build timeline", ConsoleColor.Red);
                    WriteLine($"Build URL: https://dev.azure.com/{_options.Organization}/{_options.Project}/_build/results?buildId={currentBuildId}", ConsoleColor.DarkGray);
                    continue;
                }

                var failedJobs = GetFailedJobs(timeline).ToList();
                var canceledJobs = GetCanceledJobs(timeline).ToList();
                var localTestFailures = (await GetLocalTestFailuresAsync(timeline, currentBuildId)).ToList();

                totalFailedJobs += failedJobs.Count;
                totalLocalFailures += localTestFailures.Count;
                allFailedJobNames.AddRange(failedJobs.Select(static j => j.Name ?? string.Empty).Where(static n => !string.IsNullOrEmpty(n)));
                allCanceledJobNames.AddRange(canceledJobs.Select(static j => j.Name ?? string.Empty).Where(static n => !string.IsNullOrEmpty(n)));

                var allJobs = timeline.Records?.Where(static r => string.Equals(r.Type, "Job", StringComparison.OrdinalIgnoreCase)).ToList() ?? [];
                var succeededJobs = allJobs.Count(static j => string.Equals(j.Result, "succeeded", StringComparison.OrdinalIgnoreCase));
                var warningJobs = allJobs.Count(static j => string.Equals(j.Result, "succeededWithIssues", StringComparison.OrdinalIgnoreCase));
                var pendingJobs = allJobs.Count(static j => string.IsNullOrEmpty(j.Result) || string.Equals(j.State, "pending", StringComparison.OrdinalIgnoreCase) || string.Equals(j.State, "inProgress", StringComparison.OrdinalIgnoreCase));
                var canceledJobCount = allJobs.Count(static j => string.Equals(j.Result, "canceled", StringComparison.OrdinalIgnoreCase));
                var skippedJobs = allJobs.Count(static j => string.Equals(j.Result, "skipped", StringComparison.OrdinalIgnoreCase));

                lastBuildJobSummary = new JobSummary(allJobs.Count, succeededJobs, failedJobs.Count, canceledJobCount, pendingJobs, warningJobs, skippedJobs);

                if (failedJobs.Count == 0 && localTestFailures.Count == 0)
                {
                    Console.WriteLine();
                    if (isInProgress)
                    {
                        WriteLine("No failures yet - build still in progress", ConsoleColor.Cyan);
                        WriteLine("Run again later to check for failures, or use --no-cache to get fresh data", ConsoleColor.DarkGray);
                    }
                    else
                    {
                        WriteLine($"No failed jobs found in build {currentBuildId}", ConsoleColor.Green);
                    }

                    if (canceledJobs.Count > 0)
                    {
                        Console.WriteLine();
                        WriteLine($"Note: {canceledJobs.Count} job(s) were canceled (not failed):", ConsoleColor.DarkYellow);
                        foreach (var job in canceledJobs.Take(5))
                        {
                            WriteLine($"  - {job.Name}", ConsoleColor.DarkGray);
                        }

                        if (canceledJobs.Count > 5)
                        {
                            WriteLine($"  ... and {canceledJobs.Count - 5} more", ConsoleColor.DarkGray);
                        }

                        WriteLine("  (Canceled jobs are typically due to earlier stage failures or timeouts)", ConsoleColor.DarkGray);
                    }

                    continue;
                }

                if (localTestFailures.Count > 0)
                {
                    WriteSection("=== Local Test Failures (non-Helix) ===", ConsoleColor.Yellow);
                    WriteLine($"Build: https://dev.azure.com/{_options.Organization}/{_options.Project}/_build/results?buildId={currentBuildId}", ConsoleColor.DarkGray);

                    foreach (var failure in localTestFailures)
                    {
                        Console.WriteLine();
                        WriteLine($"--- {failure.TaskName} ---", ConsoleColor.Cyan);

                        var issueMessages = failure.Issues.Select(static i => i.Message ?? string.Empty).Where(static m => !string.IsNullOrEmpty(m)).ToList();
                        allFailuresForCorrelation.Add(new CorrelationFailure(failure.TaskName, "Local Test", issueMessages, [], []));

                        var jobLogUrl = new StringBuilder($"https://dev.azure.com/{_options.Organization}/{_options.Project}/_build/results?buildId={currentBuildId}&view=logs&j={failure.ParentJobId}");
                        if (!string.IsNullOrEmpty(failure.TaskId))
                        {
                            jobLogUrl.Append($"&t={failure.TaskId}");
                        }

                        WriteLine($"  Log: {jobLogUrl}", ConsoleColor.DarkGray);
                        foreach (var issue in failure.Issues)
                        {
                            WriteLine($"  {issue.Message}", ConsoleColor.Red);
                        }

                        if (failure.TestRunUrls.Count > 0)
                        {
                            await ShowTestRunResultsAsync(failure.TestRunUrls, $"https://dev.azure.com/{_options.Organization}");
                        }

                        if (failure.LogId is int logId)
                        {
                            var logContent = await GetBuildLogAsync(currentBuildId, logId);
                            if (!string.IsNullOrEmpty(logContent))
                            {
                                var additionalRuns = ExtractTestRunUrls(logContent);
                                if (additionalRuns.Count > 0 && failure.TestRunUrls.Count == 0)
                                {
                                    await ShowTestRunResultsAsync(additionalRuns, $"https://dev.azure.com/{_options.Organization}");
                                }

                                var buildErrors = ExtractBuildErrors(logContent, _options.ContextLines);
                                if (buildErrors.Count > 0)
                                {
                                    await ShowKnownIssuesAsync(errorMessage: string.Join("\n", buildErrors), includeMihuBot: _options.SearchMihubot);
                                }
                                else if (!string.IsNullOrEmpty(failure.TaskName))
                                {
                                    await ShowKnownIssuesAsync(testName: failure.TaskName, includeMihuBot: _options.SearchMihubot);
                                }
                            }
                        }
                    }
                }

                if (failedJobs.Count == 0)
                {
                    WriteSection("=== Summary ===", ConsoleColor.Yellow);
                    WriteLine($"Local test failures: {localTestFailures.Count}", ConsoleColor.Red);
                    WriteLine($"Build URL: https://dev.azure.com/{_options.Organization}/{_options.Project}/_build/results?buildId={currentBuildId}", ConsoleColor.Cyan);
                    continue;
                }

                Console.WriteLine();
                WriteLine($"Found {failedJobs.Count} failed job(s):", ConsoleColor.Red);
                if (canceledJobs.Count > 0)
                {
                    WriteLine($"Also {canceledJobs.Count} job(s) were canceled (due to earlier failures/timeouts):", ConsoleColor.DarkYellow);
                    foreach (var job in canceledJobs.Take(3))
                    {
                        WriteLine($"  - {job.Name}", ConsoleColor.DarkGray);
                    }
                    if (canceledJobs.Count > 3)
                    {
                        WriteLine($"  ... and {canceledJobs.Count - 3} more", ConsoleColor.DarkGray);
                    }
                }

                var processedJobs = 0;
                var errorCount = 0;
                foreach (var job in failedJobs)
                {
                    if (processedJobs >= _options.MaxJobs)
                    {
                        Console.WriteLine();
                        WriteLine($"... and {failedJobs.Count - _options.MaxJobs} more failed jobs (use --max-jobs to see more)", ConsoleColor.Yellow);
                        break;
                    }

                    try
                    {
                        Console.WriteLine();
                        WriteLine($"--- {job.Name} ---", ConsoleColor.Cyan);
                        WriteLine($"  Build: https://dev.azure.com/{_options.Organization}/{_options.Project}/_build/results?buildId={currentBuildId}&view=logs&j={job.Id}", ConsoleColor.DarkGray);

                        var jobDetail = new JsonObject
                        {
                            ["jobName"] = job.Name,
                            ["buildId"] = currentBuildId,
                            ["errorSnippet"] = string.Empty,
                            ["helixWorkItems"] = new JsonArray(),
                            ["errorCategory"] = "unclassified"
                        };

                        var helixTasks = GetHelixJobInfo(timeline, job.Id).ToList();
                        if (helixTasks.Count > 0)
                        {
                            foreach (var task in helixTasks)
                            {
                                if (task.Log?.Id is not int taskLogId)
                                {
                                    continue;
                                }

                                WriteLine("  Fetching Helix task log...", ConsoleColor.Gray);
                                var logContent = await GetBuildLogAsync(currentBuildId, taskLogId);
                                if (string.IsNullOrEmpty(logContent))
                                {
                                    continue;
                                }

                                var failures = ExtractTestFailures(logContent);
                                if (failures.Count > 0)
                                {
                                    WriteLine("  Failed tests:", ConsoleColor.Red);
                                    foreach (var failure in failures)
                                    {
                                        WriteLine($"    - {failure.TestName}", ConsoleColor.White);
                                    }

                                    allFailuresForCorrelation.Add(new CorrelationFailure(task.Name ?? string.Empty, job.Name ?? string.Empty, [], [], failures.Select(static f => f.TestName).ToList()));
                                    jobDetail["errorCategory"] = "test-failure";
                                    jobDetail["errorSnippet"] = string.Join("; ", failures.Take(3).Select(static f => f.TestName));
                                }

                                var helixUrls = ExtractHelixUrls(logContent);
                                if (helixUrls.Count > 0 && _options.ShowLogs)
                                {
                                    Console.WriteLine();
                                    WriteLine("  Helix Console Logs:", ConsoleColor.Yellow);

                                    foreach (var url in helixUrls.Take(3))
                                    {
                                        Console.WriteLine();
                                        WriteLine($"  {url}", ConsoleColor.DarkGray);

                                        string workItemName = string.Empty;
                                        var workItemMatch = Regex.Match(url, "/workitems/([^/]+)/console", RegexOptions.IgnoreCase);
                                        if (workItemMatch.Success)
                                        {
                                            workItemName = workItemMatch.Groups[1].Value;
                                            ((JsonArray)jobDetail["helixWorkItems"]!).Add(workItemName);
                                        }

                                        var helixLog = await GetHelixConsoleLogAsync(url);
                                        if (string.IsNullOrEmpty(helixLog))
                                        {
                                            continue;
                                        }

                                        var failureInfo = FormatTestFailure(helixLog, _options.MaxFailureLines);
                                        if (!string.IsNullOrEmpty(failureInfo))
                                        {
                                            WriteLine(failureInfo, ConsoleColor.White);

                                            if (Regex.IsMatch(failureInfo, "Timed Out \\(timeout", RegexOptions.IgnoreCase))
                                            {
                                                jobDetail["errorCategory"] = "test-timeout";
                                            }
                                            else if (Regex.IsMatch(failureInfo, "Exit Code:\\s*(139|134|-4)", RegexOptions.IgnoreCase) || Regex.IsMatch(helixLog, "createdump", RegexOptions.IgnoreCase))
                                            {
                                                jobDetail["errorCategory"] = "crash";
                                            }
                                            else if (Regex.IsMatch(failureInfo, "Traceback \\(most recent call last\\)", RegexOptions.IgnoreCase) && Regex.IsMatch(helixLog, "Tests run:.*Failures:\\s*0", RegexOptions.IgnoreCase))
                                            {
                                                var currentCategory = jobDetail["errorCategory"]?.GetValue<string>();
                                                if (string.Equals(currentCategory, "unclassified", StringComparison.OrdinalIgnoreCase) ||
                                                    string.Equals(currentCategory, "test-failure", StringComparison.OrdinalIgnoreCase))
                                                {
                                                    jobDetail["errorCategory"] = "tests-passed-reporter-failed";
                                                }
                                            }
                                            else if (string.Equals(jobDetail["errorCategory"]?.GetValue<string>(), "unclassified", StringComparison.OrdinalIgnoreCase))
                                            {
                                                jobDetail["errorCategory"] = "test-failure";
                                            }

                                            if (string.IsNullOrEmpty(jobDetail["errorSnippet"]?.GetValue<string>()))
                                            {
                                                jobDetail["errorSnippet"] = failureInfo[..Math.Min(200, failureInfo.Length)];
                                            }

                                            await ShowKnownIssuesAsync(workItemName, failureInfo, includeMihuBot: _options.SearchMihubot);
                                        }
                                        else
                                        {
                                            var tailText = string.Join("\n", SplitLines(helixLog).TakeLast(20));
                                            WriteLine(tailText, ConsoleColor.White);
                                            if (string.IsNullOrEmpty(jobDetail["errorSnippet"]?.GetValue<string>()))
                                            {
                                                jobDetail["errorSnippet"] = tailText[..Math.Min(200, tailText.Length)];
                                            }

                                            await ShowKnownIssuesAsync(workItemName, tailText, includeMihuBot: _options.SearchMihubot);
                                        }
                                    }
                                }
                                else if (helixUrls.Count > 0)
                                {
                                    Console.WriteLine();
                                    WriteLine("  Helix logs available (use -ShowLogs to fetch):", ConsoleColor.Yellow);
                                    foreach (var url in helixUrls.Take(3))
                                    {
                                        WriteLine($"    {url}", ConsoleColor.DarkGray);
                                    }
                                }
                            }
                        }
                        else
                        {
                            var buildTasks = timeline.Records?.Where(r => string.Equals(r.ParentId, job.Id, StringComparison.OrdinalIgnoreCase) && string.Equals(r.Result, "failed", StringComparison.OrdinalIgnoreCase)).Take(3).ToList() ?? [];
                            foreach (var task in buildTasks)
                            {
                                WriteLine($"  Failed task: {task.Name}", ConsoleColor.Red);
                                if (task.Log?.Id is not int taskLogId)
                                {
                                    continue;
                                }

                                var logUrl = $"https://dev.azure.com/{_options.Organization}/{_options.Project}/_build/results?buildId={currentBuildId}&view=logs&j={job.Id}&t={task.Id}";
                                WriteLine($"  Log: {logUrl}", ConsoleColor.DarkGray);
                                var logContent = await GetBuildLogAsync(currentBuildId, taskLogId);
                                if (string.IsNullOrEmpty(logContent))
                                {
                                    continue;
                                }

                                var buildErrors = ExtractBuildErrors(logContent, _options.ContextLines);
                                if (buildErrors.Count > 0)
                                {
                                    allFailuresForCorrelation.Add(new CorrelationFailure(task.Name ?? string.Empty, job.Name ?? string.Empty, buildErrors, [], []));
                                    jobDetail["errorCategory"] = "build-error";
                                    if (string.IsNullOrEmpty(jobDetail["errorSnippet"]?.GetValue<string>()))
                                    {
                                        var snippet = string.Join("; ", buildErrors.Take(2));
                                        jobDetail["errorSnippet"] = snippet[..Math.Min(200, snippet.Length)];
                                    }

                                    var helixLogUrls = ExtractHelixLogUrls(logContent);
                                    if (helixLogUrls.Count > 0)
                                    {
                                        WriteLine($"  Helix failures ({helixLogUrls.Count}):", ConsoleColor.Red);
                                        foreach (var helixLog in helixLogUrls.Take(5))
                                        {
                                            WriteLine($"    - {helixLog.WorkItem}", ConsoleColor.White);
                                            WriteLine($"      Log: {helixLog.Url}", ConsoleColor.DarkGray);
                                        }
                                        if (helixLogUrls.Count > 5)
                                        {
                                            WriteLine($"    ... and {helixLogUrls.Count - 5} more", ConsoleColor.Gray);
                                        }
                                    }
                                    else
                                    {
                                        WriteLine("  Build errors:", ConsoleColor.Red);
                                        foreach (var err in buildErrors.Take(5))
                                        {
                                            WriteLine($"    {err}", ConsoleColor.White);
                                        }
                                        if (buildErrors.Count > 5)
                                        {
                                            WriteLine($"    ... and {buildErrors.Count - 5} more errors", ConsoleColor.Gray);
                                        }
                                    }

                                    await ShowKnownIssuesAsync(errorMessage: string.Join("\n", buildErrors), includeMihuBot: _options.SearchMihubot);
                                }
                                else
                                {
                                    WriteLine("  (No specific errors extracted from log)", ConsoleColor.Gray);
                                }
                            }
                        }

                        allFailedJobDetails.Add(jobDetail);
                        processedJobs++;
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        if (_options.ContinueOnError)
                        {
                            WriteWarning($"  Error processing job '{job.Name}': {ex.Message}");
                        }
                        else
                        {
                            throw new Exception($"Error processing job '{job.Name}': {ex.Message}", ex);
                        }
                    }
                }

                Console.WriteLine();
                WriteSection($"=== Build {currentBuildId} Summary ===", ConsoleColor.Yellow);
                if (allJobs.Count > 0)
                {
                    var parts = new List<string>();
                    if (succeededJobs > 0) parts.Add($"{succeededJobs} passed");
                    if (warningJobs > 0) parts.Add($"{warningJobs} passed with warnings");
                    if (failedJobs.Count > 0) parts.Add($"{failedJobs.Count} failed");
                    if (canceledJobCount > 0) parts.Add($"{canceledJobCount} canceled");
                    if (skippedJobs > 0) parts.Add($"{skippedJobs} skipped");
                    if (pendingJobs > 0) parts.Add($"{pendingJobs} pending");
                    var jobSummaryText = string.Join(", ", parts);
                    var allSucceeded = failedJobs.Count == 0 && pendingJobs == 0 && canceledJobCount == 0 && (succeededJobs + warningJobs + skippedJobs) == allJobs.Count;
                    var summaryColor = allSucceeded ? ConsoleColor.Green : failedJobs.Count > 0 ? ConsoleColor.Red : ConsoleColor.Cyan;
                    WriteLine($"Jobs: {allJobs.Count} total ({jobSummaryText})", summaryColor);
                }
                else
                {
                    WriteLine($"Failed jobs: {failedJobs.Count}", ConsoleColor.Red);
                }
                if (localTestFailures.Count > 0)
                {
                    WriteLine($"Local test failures: {localTestFailures.Count}", ConsoleColor.Red);
                }
                if (errorCount > 0)
                {
                    WriteLine($"API errors (partial results): {errorCount}", ConsoleColor.Yellow);
                }
                WriteLine($"Build URL: https://dev.azure.com/{_options.Organization}/{_options.Project}/_build/results?buildId={currentBuildId}", ConsoleColor.Cyan);
            }

            if (prChangedFiles.Count > 0 && allFailuresForCorrelation.Count > 0)
            {
                ShowPrCorrelationSummary(prChangedFiles, allFailuresForCorrelation);
            }

            if (buildIds.Count > 1)
            {
                Console.WriteLine();
                WriteSection("=== Overall Summary ===", ConsoleColor.Magenta);
                WriteLine($"Analyzed {buildIds.Count} builds", ConsoleColor.White);
                WriteLine($"Total failed jobs: {totalFailedJobs}", ConsoleColor.Red);
                WriteLine($"Total local test failures: {totalLocalFailures}", ConsoleColor.Red);

                if (knownIssuesFromBuildAnalysis.Count > 0)
                {
                    Console.WriteLine();
                    WriteLine("Known Issues (from Build Analysis):", ConsoleColor.Yellow);
                    foreach (var issue in knownIssuesFromBuildAnalysis)
                    {
                        WriteLine($"  - #{issue.Number}: {issue.Title}", ConsoleColor.Gray);
                        WriteLine($"    {issue.Url}", ConsoleColor.DarkGray);
                    }
                }
            }

            var summaryObject = new JsonObject
            {
                ["mode"] = _options.PrNumber.HasValue ? "PRNumber" : "BuildId",
                ["repository"] = _options.Repository,
                ["prNumber"] = _options.PrNumber,
                ["builds"] = new JsonArray(buildIds.Select(id => (JsonNode)new JsonObject
                {
                    ["buildId"] = id,
                    ["url"] = $"https://dev.azure.com/{_options.Organization}/{_options.Project}/_build/results?buildId={id}"
                }).ToArray()),
                ["totalFailedJobs"] = totalFailedJobs,
                ["totalLocalFailures"] = totalLocalFailures,
                ["lastBuildJobSummary"] = lastBuildJobSummary is null
                    ? CreateJobSummaryObject(0, 0, 0, 0, 0, 0, 0)
                    : CreateJobSummaryObject(lastBuildJobSummary.Total, lastBuildJobSummary.Succeeded, lastBuildJobSummary.Failed, lastBuildJobSummary.Canceled, lastBuildJobSummary.Pending, lastBuildJobSummary.Warnings, lastBuildJobSummary.Skipped),
                ["failedJobNames"] = new JsonArray(allFailedJobNames.Select(static n => (JsonNode)JsonValue.Create(n)!).ToArray()),
                ["failedJobDetails"] = new JsonArray(allFailedJobDetails.Select(static d => (JsonNode)d).ToArray()),
                ["failedJobDetailsTruncated"] = allFailedJobNames.Count > allFailedJobDetails.Count,
                ["canceledJobNames"] = new JsonArray(allCanceledJobNames.Select(static n => (JsonNode)JsonValue.Create(n)!).ToArray()),
                ["knownIssues"] = new JsonArray(knownIssuesFromBuildAnalysis.Select(issue => (JsonNode)new JsonObject
                {
                    ["number"] = issue.Number,
                    ["title"] = issue.Title,
                    ["url"] = issue.Url
                }).ToArray()),
                ["prCorrelation"] = new JsonObject
                {
                    ["changedFileCount"] = prChangedFiles.Count,
                    ["hasCorrelation"] = false,
                    ["correlatedFiles"] = new JsonArray()
                },
                ["recommendationHint"] = string.Empty
            };

            if (prChangedFiles.Count > 0 && allFailuresForCorrelation.Count > 0)
            {
                var correlation = GetPrCorrelation(prChangedFiles, allFailuresForCorrelation);
                var allCorrelated = correlation.CorrelatedFiles.Concat(correlation.TestFiles).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                summaryObject["prCorrelation"] = new JsonObject
                {
                    ["changedFileCount"] = prChangedFiles.Count,
                    ["hasCorrelation"] = allCorrelated.Count > 0,
                    ["correlatedFiles"] = new JsonArray(allCorrelated.Select(static f => (JsonNode)JsonValue.Create(f)!).ToArray())
                };
            }

            summaryObject["recommendationHint"] = GetRecommendationHint(
                lastBuildJobSummary,
                buildIds.Count,
                knownIssuesFromBuildAnalysis.Count,
                totalFailedJobs,
                totalLocalFailures,
                prChangedFiles.Count,
                allFailuresForCorrelation.Count,
                summaryObject["prCorrelation"]?.AsObject()["hasCorrelation"]?.GetValue<bool>() == true);

            Console.WriteLine();
            EmitSummary(summaryObject);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private string GetRecommendationHint(JobSummary? lastBuildJobSummary, int buildCount, int knownIssuesCount, int totalFailedJobs, int totalLocalFailures, int changedFileCount, int failureCount, bool hasCorrelation)
        => lastBuildJobSummary is null && buildCount > 0 ? "REVIEW_REQUIRED"
         : knownIssuesCount > 0 ? "KNOWN_ISSUES_DETECTED"
         : totalFailedJobs == 0 && totalLocalFailures == 0 ? "BUILD_SUCCESSFUL"
         : hasCorrelation ? "LIKELY_PR_RELATED"
         : changedFileCount > 0 && failureCount > 0 ? "POSSIBLY_TRANSIENT"
         : "REVIEW_REQUIRED";

    private async Task RunHelixModeAsync()
    {
        var helixJob = _options.HelixJob!;
        Console.WriteLine();
        WriteSection($"=== Helix Job {helixJob} ===", ConsoleColor.Yellow);
        WriteLine($"URL: https://helix.dot.net/api/jobs/{helixJob}", ConsoleColor.DarkGray);

        var jobDetails = await GetHelixJobDetailsAsync(helixJob);
        if (jobDetails is not null)
        {
            Console.WriteLine();
            WriteLine($"Queue: {jobDetails.QueueId}", ConsoleColor.Cyan);
            WriteLine($"Source: {jobDetails.Source}", ConsoleColor.Cyan);
        }

        if (!string.IsNullOrEmpty(_options.WorkItem))
        {
            Console.WriteLine();
            WriteLine($"--- Work Item: {_options.WorkItem} ---", ConsoleColor.Cyan);
            var workItemDetails = await GetHelixWorkItemDetailsAsync(helixJob, _options.WorkItem!);
            if (workItemDetails is null)
            {
                return;
            }

            WriteLine($"  State: {workItemDetails.State}", string.Equals(workItemDetails.State, "Passed", StringComparison.OrdinalIgnoreCase) ? ConsoleColor.Green : ConsoleColor.Red);
            WriteLine($"  Exit Code: {workItemDetails.ExitCode}", ConsoleColor.White);
            WriteLine($"  Machine: {workItemDetails.MachineName}", ConsoleColor.Gray);
            WriteLine($"  Duration: {workItemDetails.Duration}", ConsoleColor.Gray);

            if (workItemDetails.Files?.Count > 0)
            {
                Console.WriteLine();
                WriteLine("  Artifacts:", ConsoleColor.Yellow);
                var binlogs = workItemDetails.Files.Where(static f => f.FileName?.EndsWith(".binlog", StringComparison.OrdinalIgnoreCase) == true)
                    .GroupBy(static f => (f.FileName, f.Uri))
                    .Select(static g => g.First())
                    .ToList();
                var otherFiles = workItemDetails.Files.Where(static f => f.FileName?.EndsWith(".binlog", StringComparison.OrdinalIgnoreCase) != true)
                    .GroupBy(static f => (f.FileName, f.Uri))
                    .Select(static g => g.First())
                    .ToList();

                foreach (var file in binlogs)
                {
                    WriteLine($"    📋 {file.FileName}: {file.Uri}", ConsoleColor.Cyan);
                }
                if (binlogs.Count > 0)
                {
                    WriteLine("    (Tip: Use MSBuild MCP server or https://live.msbuildlog.com/ to analyze binlogs)", ConsoleColor.DarkGray);
                }
                foreach (var file in otherFiles.Take(10))
                {
                    WriteLine($"    {file.FileName}: {file.Uri}", ConsoleColor.Gray);
                }
            }

            var encodedWorkItem = Uri.EscapeDataString(_options.WorkItem!);
            var consoleUrl = $"https://helix.dot.net/api/2019-06-17/jobs/{helixJob}/workitems/{encodedWorkItem}/console";
            Console.WriteLine();
            WriteLine($"  Console Log: {consoleUrl}", ConsoleColor.Yellow);
            var consoleLog = await GetHelixConsoleLogAsync(consoleUrl);
            if (!string.IsNullOrEmpty(consoleLog))
            {
                var failureInfo = FormatTestFailure(consoleLog, _options.MaxFailureLines);
                if (!string.IsNullOrEmpty(failureInfo))
                {
                    WriteLine(failureInfo, ConsoleColor.White);
                    await ShowKnownIssuesAsync(_options.WorkItem, failureInfo, includeMihuBot: _options.SearchMihubot);
                }
                else
                {
                    WriteLine(string.Join("\n", SplitLines(consoleLog).TakeLast(50)), ConsoleColor.White);
                }
            }

            return;
        }

        Console.WriteLine();
        WriteLine("Work Items:", ConsoleColor.Yellow);
        var workItems = await GetHelixWorkItemsAsync(helixJob);
        if (workItems is null)
        {
            return;
        }

        WriteLine($"  Total: {workItems.Count}", ConsoleColor.Cyan);
        WriteLine("  Checking for failures...", ConsoleColor.Gray);

        var failedItems = new List<(string Name, int ExitCode, string? State)>();
        foreach (var wi in workItems.Take(20))
        {
            var details = await GetHelixWorkItemDetailsAsync(helixJob, wi.Name ?? string.Empty);
            if (details?.ExitCode is int exitCode && exitCode != 0)
            {
                failedItems.Add((wi.Name ?? string.Empty, exitCode, details.State));
            }
        }

        if (failedItems.Count > 0)
        {
            Console.WriteLine();
            WriteLine("  Failed Work Items:", ConsoleColor.Red);
            foreach (var wi in failedItems.Take(_options.MaxJobs))
            {
                WriteLine($"    - {wi.Name} (Exit: {wi.ExitCode})", ConsoleColor.White);
            }
            Console.WriteLine();
            WriteLine("  Use -WorkItem '<name>' to see details", ConsoleColor.Gray);
        }
        else
        {
            WriteLine("  No failures found in first 20 work items", ConsoleColor.Green);
        }

        Console.WriteLine();
        WriteLine("  All work items:", ConsoleColor.Yellow);
        foreach (var wi in workItems.Take(10))
        {
            WriteLine($"    - {wi.Name}", ConsoleColor.White);
        }
        if (workItems.Count > 10)
        {
            WriteLine($"    ... and {workItems.Count - 10} more", ConsoleColor.Gray);
        }

        if (_options.FindBinlogs)
        {
            Console.WriteLine();
            WriteLine("  === Binlog Search ===", ConsoleColor.Yellow);
            var binlogResults = await FindWorkItemsWithBinlogsAsync(helixJob, 30, includeDetails: true);
            if (binlogResults.Count > 0)
            {
                Console.WriteLine();
                WriteLine("  Work items with binlogs:", ConsoleColor.Cyan);
                foreach (var result in binlogResults)
                {
                    var stateColor = result.ExitCode == 0 ? ConsoleColor.Green : ConsoleColor.Red;
                    WriteLine($"    {result.Name}", stateColor);
                    WriteLine($"      Binlogs ({result.BinlogCount}):", ConsoleColor.Gray);
                    foreach (var binlog in result.Binlogs.Take(5))
                    {
                        WriteLine($"        - {binlog}", ConsoleColor.White);
                    }
                    if (result.Binlogs.Count > 5)
                    {
                        WriteLine($"        ... and {result.Binlogs.Count - 5} more", ConsoleColor.DarkGray);
                    }
                }
                Console.WriteLine();
                WriteLine("  Tip: Use -WorkItem '<name>' to get full binlog URIs", ConsoleColor.DarkGray);
            }
            else
            {
                WriteLine("  No binlogs found in scanned work items.", ConsoleColor.Yellow);
                WriteLine("  This job may contain only unit tests (which don't produce binlogs).", ConsoleColor.Gray);
            }
        }
    }

    private void ClearCache()
    {
        if (Directory.Exists(_cacheDir))
        {
            var count = Directory.EnumerateFiles(_cacheDir).Count();
            Directory.Delete(_cacheDir, recursive: true);
            WriteLine($"Cleared {count} cached files from {_cacheDir}", ConsoleColor.Green);
        }
        else
        {
            WriteLine($"Cache directory does not exist: {_cacheDir}", ConsoleColor.Yellow);
        }
    }

    private string GetTempDirectory()
    {
        var tempPath = Environment.GetEnvironmentVariable("TEMP");
        if (string.IsNullOrWhiteSpace(tempPath)) tempPath = Environment.GetEnvironmentVariable("TMP");
        if (string.IsNullOrWhiteSpace(tempPath)) tempPath = Environment.GetEnvironmentVariable("TMPDIR");
        if (string.IsNullOrWhiteSpace(tempPath) && OperatingSystem.IsLinux()) tempPath = "/tmp";
        if (string.IsNullOrWhiteSpace(tempPath) && OperatingSystem.IsMacOS()) tempPath = "/tmp";
        if (string.IsNullOrWhiteSpace(tempPath))
        {
            var home = Environment.GetEnvironmentVariable("HOME") ?? Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrWhiteSpace(home))
            {
                tempPath = Path.Combine(home, ".cache");
                Directory.CreateDirectory(tempPath);
            }
        }

        if (string.IsNullOrWhiteSpace(tempPath))
        {
            throw new InvalidOperationException("Could not determine temp directory. Set TEMP, TMP, or TMPDIR environment variable.");
        }

        return tempPath;
    }

    private void ClearExpiredCache(int ttlSeconds)
    {
        var cutoff = DateTime.UtcNow.AddSeconds(-(ttlSeconds * 2));
        foreach (var file in Directory.EnumerateFiles(_cacheDir, "*.json", SearchOption.TopDirectoryOnly))
        {
            try
            {
                if (File.GetLastWriteTimeUtc(file) < cutoff)
                {
                    WriteVerbose($"Removing expired cache file: {Path.GetFileName(file)}");
                    File.Delete(file);
                }
            }
            catch (Exception ex)
            {
                WriteVerbose($"Failed to remove cache file '{Path.GetFileName(file)}': {ex.Message}");
            }
        }
    }

    private string GetUrlHash(string url)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(url));
        return Convert.ToHexString(bytes);
    }

    private string? GetCachedResponse(string url, int? ttlSeconds = null)
    {
        if (_options.NoCache)
        {
            return null;
        }

        var cacheFile = Path.Combine(_cacheDir, $"{GetUrlHash(url)}.json");
        if (!File.Exists(cacheFile))
        {
            return null;
        }

        var age = DateTime.UtcNow - File.GetLastWriteTimeUtc(cacheFile);
        if (age.TotalSeconds < (ttlSeconds ?? _options.CacheTtlSeconds))
        {
            return File.ReadAllText(cacheFile);
        }

        return null;
    }

    private void SetCachedResponse(string url, string content)
    {
        if (_options.NoCache)
        {
            return;
        }

        var hash = GetUrlHash(url);
        var cacheFile = Path.Combine(_cacheDir, $"{hash}.json");
        var tempFile = Path.Combine(_cacheDir, $"{hash}.tmp.{Guid.NewGuid():N}");
        try
        {
            File.WriteAllText(tempFile, content);
            File.Move(tempFile, cacheFile, overwrite: true);
        }
        catch
        {
            try
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
            catch
            {
            }
        }
    }

    private async Task<string> InvokeCachedRestMethodAsync(string uri, bool skipCache = false, bool skipCacheWrite = false)
    {
        if (!skipCache)
        {
            var cached = GetCachedResponse(uri);
            if (cached is not null)
            {
                return cached;
            }
        }

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_options.TimeoutSec));
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync(cts.Token);
        if (!skipCache && !skipCacheWrite)
        {
            SetCachedResponse(uri, content);
        }

        return content;
    }

    private async Task<T?> InvokeCachedRestMethodAsync<T>(string uri, bool skipCache = false, bool skipCacheWrite = false)
    {
        if (!skipCache)
        {
            var cached = GetCachedResponse(uri);
            if (cached is not null)
            {
                try
                {
                    return JsonSerializer.Deserialize<T>(cached, _jsonOptions);
                }
                catch
                {
                }
            }
        }

        var content = await InvokeCachedRestMethodAsync(uri, skipCache: true, skipCacheWrite: true);
        if (!skipCache && !skipCacheWrite)
        {
            SetCachedResponse(uri, content);
        }

        return JsonSerializer.Deserialize<T>(content, _jsonOptions);
    }

    private async Task<BuildLookupResult> GetAzdoBuildIdFromPrAsync(int pr)
    {
        EnsureGhAvailable();
        TestRepositoryFormat(_options.Repository);

        WriteLine($"Finding builds for PR #{pr} in {_options.Repository}...", ConsoleColor.Cyan);

        var checksOutput = await RunProcessCaptureAsync("gh", ["pr", "checks", pr.ToString(), "--repo", _options.Repository]);
        var combinedChecks = checksOutput.CombinedOutput;
        if (checksOutput.ExitCode != 0 && !FailingBuildRegex.IsMatch(combinedChecks) && !AnyBuildRegex.IsMatch(combinedChecks))
        {
            throw new InvalidOperationException($"Failed to fetch CI status for PR #{pr} in {_options.Repository} - check PR number and permissions");
        }

        string? prMergeState = null;
        try
        {
            var mergeableState = await RunProcessCaptureAsync("gh", ["api", $"repos/{_options.Repository}/pulls/{pr}", "--jq", ".mergeable_state"]);
            if (mergeableState.ExitCode == 0 && !string.IsNullOrWhiteSpace(mergeableState.StdOut))
            {
                prMergeState = mergeableState.StdOut.Trim();
            }
        }
        catch
        {
        }

        var failingBuilds = new Dictionary<int, string>();
        foreach (var line in combinedChecks.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var match = FailingBuildRegex.Match(line);
            if (!match.Success)
            {
                continue;
            }

            var buildId = int.Parse(match.Groups[1].Value);
            var pipelineNameMatch = Regex.Match(line, "^(.*?)\\s+fail", RegexOptions.IgnoreCase);
            var pipelineName = pipelineNameMatch.Success ? pipelineNameMatch.Groups[1].Value.Trim() : string.Empty;
            failingBuilds.TryAdd(buildId, pipelineName);
        }

        if (failingBuilds.Count == 0)
        {
            var anyBuildMatch = AnyBuildRegex.Match(combinedChecks);
            if (anyBuildMatch.Success && int.TryParse(anyBuildMatch.Groups[1].Value, out var buildId))
            {
                return new BuildLookupResult([buildId], null, prMergeState);
            }

            if (string.Equals(prMergeState, "dirty", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine();
                WriteLine($"PR #{pr} has merge conflicts (mergeable_state: dirty)", ConsoleColor.Red);
                WriteLine("CI will not run until conflicts are resolved.", ConsoleColor.Yellow);
                WriteLine("Resolve conflicts and push to trigger CI, or use -BuildId to analyze a previous build.", ConsoleColor.Gray);
                return new BuildLookupResult([], "MERGE_CONFLICTS", prMergeState);
            }

            Console.WriteLine();
            WriteLine($"No CI build found for PR #{pr} in {_options.Repository}", ConsoleColor.Red);
            WriteLine("The CI pipeline has not been triggered yet.", ConsoleColor.Yellow);
            return new BuildLookupResult([], "NO_BUILDS", prMergeState);
        }

        var buildIds = failingBuilds.Keys.OrderBy(static id => id).ToList();
        if (buildIds.Count > 1)
        {
            WriteLine($"Found {buildIds.Count} failing builds:", ConsoleColor.Yellow);
            foreach (var id in buildIds)
            {
                WriteLine($"  - Build {id} ({failingBuilds[id]})", ConsoleColor.Gray);
            }
        }

        return new BuildLookupResult(buildIds, null, prMergeState);
    }

    private async Task<List<KnownIssue>> GetBuildAnalysisKnownIssuesAsync(int pr)
    {
        if (!IsToolAvailable("gh"))
        {
            return [];
        }

        try
        {
            var headShaResult = await RunProcessCaptureAsync("gh", ["pr", "view", pr.ToString(), "--repo", _options.Repository, "--json", "headRefOid", "--jq", ".headRefOid"]);
            var headSha = headShaResult.StdOut.Trim();
            if (headShaResult.ExitCode != 0 || !ValidShaRegex.IsMatch(headSha))
            {
                return [];
            }

            var checkRunsResult = await RunProcessCaptureAsync("gh", ["api", $"repos/{_options.Repository}/commits/{headSha}/check-runs", "--jq", ".check_runs[] | select(.name == \"Build Analysis\") | .output"]);
            if (checkRunsResult.ExitCode != 0 || string.IsNullOrWhiteSpace(checkRunsResult.StdOut))
            {
                return [];
            }

            JsonNode? outputNode;
            try
            {
                outputNode = JsonNode.Parse(checkRunsResult.StdOut.Trim());
            }
            catch
            {
                return [];
            }

            var outputText = outputNode?["text"]?.GetValue<string>();
            if (string.IsNullOrEmpty(outputText))
            {
                return [];
            }

            var knownIssues = new List<KnownIssue>();
            foreach (Match match in BuildAnalysisIssueRegex.Matches(outputText))
            {
                var issue = new KnownIssue(match.Groups[2].Value, match.Groups[3].Value, match.Groups[1].Value, null, null, null, null);
                if (!knownIssues.Any(i => i.Number == issue.Number))
                {
                    knownIssues.Add(issue);
                }
            }

            if (knownIssues.Count > 0)
            {
                Console.WriteLine();
                WriteLine($"Build Analysis found {knownIssues.Count} known issue(s):", ConsoleColor.Yellow);
                foreach (var issue in knownIssues)
                {
                    WriteLine($"  - #{issue.Number}: {issue.Title}", ConsoleColor.Gray);
                    WriteLine($"    {issue.Url}", ConsoleColor.DarkGray);
                }
            }

            return knownIssues;
        }
        catch
        {
            return [];
        }
    }

    private async Task<List<string>> GetPrChangedFilesAsync(int pr, int maxFiles = 100)
    {
        if (!IsToolAvailable("gh"))
        {
            return [];
        }

        try
        {
            var fileCountResult = await RunProcessCaptureAsync("gh", ["pr", "view", pr.ToString(), "--repo", _options.Repository, "--json", "files", "--jq", ".files | length"]);
            if (fileCountResult.ExitCode != 0)
            {
                return [];
            }

            var count = int.Parse(fileCountResult.StdOut.Trim());
            if (count > maxFiles)
            {
                WriteLine($"PR has {count} changed files - skipping detailed correlation (limit: {maxFiles})", ConsoleColor.Gray);
                return [];
            }

            var filesResult = await RunProcessCaptureAsync("gh", ["pr", "view", pr.ToString(), "--repo", _options.Repository, "--json", "files", "--jq", ".files[].path"]);
            if (filesResult.ExitCode != 0)
            {
                return [];
            }

            return filesResult.StdOut.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        }
        catch
        {
            return [];
        }
    }

    private PrCorrelationResult GetPrCorrelation(List<string> changedFiles, List<CorrelationFailure> allFailures)
    {
        var correlatedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var testFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (changedFiles.Count == 0 || allFailures.Count == 0)
        {
            return new PrCorrelationResult([], []);
        }

        var failureText = string.Join("\n", allFailures.Select(f => string.Join("\n", new[] { f.TaskName, f.JobName }.Concat(f.Errors).Concat(f.HelixLogs).Concat(f.FailedTests))));

        foreach (var file in changedFiles)
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            var fileNameWithExt = Path.GetFileName(file);
            var baseTestName = fileName.Contains('.') ? fileName[..fileName.LastIndexOf('.')] : fileName;
            var isCorrelated = failureText.Contains(fileName, StringComparison.OrdinalIgnoreCase)
                || failureText.Contains(fileNameWithExt, StringComparison.OrdinalIgnoreCase)
                || failureText.Contains(file, StringComparison.OrdinalIgnoreCase)
                || (!string.IsNullOrEmpty(baseTestName) && failureText.Contains(baseTestName, StringComparison.OrdinalIgnoreCase));

            if (!isCorrelated)
            {
                continue;
            }

            var isTestFile = Regex.IsMatch(file, "\\.Tests?\\.", RegexOptions.IgnoreCase)
                || Regex.IsMatch(file, "[/\\\\]tests?[/\\\\]", RegexOptions.IgnoreCase)
                || file.EndsWith("Test.cs", StringComparison.OrdinalIgnoreCase)
                || file.EndsWith("Tests.cs", StringComparison.OrdinalIgnoreCase);
            if (isTestFile)
            {
                testFiles.Add(file);
            }
            else
            {
                correlatedFiles.Add(file);
            }
        }

        return new PrCorrelationResult(correlatedFiles.ToList(), testFiles.ToList());
    }

    private void ShowPrCorrelationSummary(List<string> changedFiles, List<CorrelationFailure> allFailures)
    {
        if (changedFiles.Count == 0)
        {
            return;
        }

        var correlation = GetPrCorrelation(changedFiles, allFailures);
        if (correlation.CorrelatedFiles.Count == 0 && correlation.TestFiles.Count == 0)
        {
            return;
        }

        Console.WriteLine();
        WriteSection("=== PR Change Correlation ===", ConsoleColor.Magenta);
        if (correlation.TestFiles.Count > 0)
        {
            WriteLine("⚠️  Test files changed by this PR are failing:", ConsoleColor.Yellow);
            foreach (var file in correlation.TestFiles.Take(10))
            {
                WriteLine($"    {file}", ConsoleColor.Red);
            }
            if (correlation.TestFiles.Count > 10)
            {
                WriteLine($"    ... and {correlation.TestFiles.Count - 10} more test files", ConsoleColor.Gray);
            }
        }

        if (correlation.CorrelatedFiles.Count > 0)
        {
            WriteLine("⚠️  Files changed by this PR appear in failures:", ConsoleColor.Yellow);
            foreach (var file in correlation.CorrelatedFiles.Take(10))
            {
                WriteLine($"    {file}", ConsoleColor.Red);
            }
            if (correlation.CorrelatedFiles.Count > 10)
            {
                WriteLine($"    ... and {correlation.CorrelatedFiles.Count - 10} more files", ConsoleColor.Gray);
            }
        }

        Console.WriteLine();
        WriteLine("Correlated files found — check JSON summary for details.", ConsoleColor.Yellow);
    }

    private async Task<BuildStatus?> GetAzdoBuildStatusAsync(int build)
    {
        var url = $"https://dev.azure.com/{_options.Organization}/{_options.Project}/_apis/build/builds/{build}?api-version=7.0";
        try
        {
            var cached = GetCachedResponse(url);
            if (cached is not null)
            {
                var cachedData = JsonSerializer.Deserialize<BuildStatusResponse>(cached, _jsonOptions);
                if (cachedData is not null && string.Equals(cachedData.Status, "completed", StringComparison.OrdinalIgnoreCase))
                {
                    return new BuildStatus(cachedData.Status, cachedData.Result, cachedData.StartTime, cachedData.FinishTime);
                }
            }

            var content = await InvokeCachedRestMethodAsync(url, skipCache: true, skipCacheWrite: true);
            var response = JsonSerializer.Deserialize<BuildStatusResponse>(content, _jsonOptions);
            if (response is null)
            {
                return null;
            }

            if (string.Equals(response.Status, "completed", StringComparison.OrdinalIgnoreCase))
            {
                SetCachedResponse(url, content);
            }

            return new BuildStatus(response.Status, response.Result, response.StartTime, response.FinishTime);
        }
        catch
        {
            return null;
        }
    }

    private async Task<TimelineResponse?> GetAzdoTimelineAsync(int build, bool buildInProgress)
    {
        var url = $"https://dev.azure.com/{_options.Organization}/{_options.Project}/_apis/build/builds/{build}/timeline?api-version=7.0";
        WriteLine("Fetching build timeline...", ConsoleColor.Cyan);
        try
        {
            return await InvokeCachedRestMethodAsync<TimelineResponse>(url, skipCache: buildInProgress, skipCacheWrite: buildInProgress);
        }
        catch (Exception ex)
        {
            if (_options.ContinueOnError)
            {
                WriteWarning($"Failed to fetch build timeline: {ex.Message}");
                return null;
            }

            throw new Exception($"Failed to fetch build timeline: {ex.Message}", ex);
        }
    }

    private static IEnumerable<TimelineRecord> GetFailedJobs(TimelineResponse timeline)
        => timeline.Records?.Where(static r => string.Equals(r.Type, "Job", StringComparison.OrdinalIgnoreCase) && string.Equals(r.Result, "failed", StringComparison.OrdinalIgnoreCase)) ?? [];

    private static IEnumerable<TimelineRecord> GetCanceledJobs(TimelineResponse timeline)
        => timeline.Records?.Where(static r => string.Equals(r.Type, "Job", StringComparison.OrdinalIgnoreCase) && string.Equals(r.Result, "canceled", StringComparison.OrdinalIgnoreCase)) ?? [];

    private static IEnumerable<TimelineRecord> GetHelixJobInfo(TimelineResponse timeline, string? jobId)
        => timeline.Records?.Where(r => string.Equals(r.ParentId, jobId, StringComparison.OrdinalIgnoreCase)
            && (r.Name?.Contains("Helix", StringComparison.OrdinalIgnoreCase) ?? false)
            && string.Equals(r.Result, "failed", StringComparison.OrdinalIgnoreCase)) ?? [];

    private async Task<string?> GetBuildLogAsync(int build, int logId)
    {
        var url = $"https://dev.azure.com/{_options.Organization}/{_options.Project}/_apis/build/builds/{build}/logs/{logId}?api-version=7.0";
        try
        {
            return await InvokeCachedRestMethodAsync(url);
        }
        catch (Exception ex)
        {
            WriteWarning($"Failed to fetch log {logId}: {ex.Message}");
            return null;
        }
    }

    private static List<string> ExtractHelixUrls(string logContent)
    {
        var normalizedContent = logContent.Replace("\r\n", string.Empty, StringComparison.Ordinal).Replace("\n", string.Empty, StringComparison.Ordinal);
        return HelixUrlRegex.Matches(normalizedContent).Select(static m => m.Value).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static List<TestFailure> ExtractTestFailures(string logContent)
        => TestFailureRegex.Matches(logContent).Select(static m => new TestFailure(m.Groups[1].Value, m.Value)).ToList();

    private static List<string> ExtractBuildErrors(string logContent, int context)
    {
        var errors = new List<string>();
        var lines = SplitLines(logContent).ToArray();
        var combinedPattern = string.Join('|', BuildErrorPatterns);
        var foundRealErrors = false;
        var msbWrapperLines = new List<int>();

        for (var i = 0; i < lines.Length; i++)
        {
            if (!Regex.IsMatch(lines[i], combinedPattern, RegexOptions.IgnoreCase))
            {
                continue;
            }

            if (Regex.IsMatch(lines[i], "exited with code \\d+", RegexOptions.IgnoreCase))
            {
                msbWrapperLines.Add(i);
                continue;
            }

            if (Regex.IsMatch(lines[i], "error MSB3073.*exited with code", RegexOptions.IgnoreCase))
            {
                continue;
            }

            foundRealErrors = true;
            var cleanLine = Regex.Replace(lines[i], "^\\d{4}-\\d{2}-\\d{2}T[\\d:.]+Z\\s*", string.Empty);
            cleanLine = cleanLine.Replace("##[error]", "ERROR: ", StringComparison.OrdinalIgnoreCase).Trim();

            if (context > 0)
            {
                var contextStart = Math.Max(0, i - context);
                var contextLines = new List<string>();
                for (var j = contextStart; j < i; j++)
                {
                    contextLines.Add("  " + lines[j].Trim());
                }
                if (contextLines.Count > 0)
                {
                    errors.Add(string.Join("\n", contextLines));
                }
            }

            errors.Add(cleanLine);
        }

        if (!foundRealErrors && msbWrapperLines.Count > 0)
        {
            var wrapperLine = msbWrapperLines[0];
            var searchStart = Math.Max(0, wrapperLine - 50);
            for (var i = searchStart; i < wrapperLine; i++)
            {
                var line = lines[i];
                if (Regex.IsMatch(line, ":\\s*error:", RegexOptions.IgnoreCase) || Regex.IsMatch(line, "fatal error:", RegexOptions.IgnoreCase) || Regex.IsMatch(line, "undefined reference", RegexOptions.IgnoreCase))
                {
                    errors.Add(Regex.Replace(line, "^\\d{4}-\\d{2}-\\d{2}T[\\d:.]+Z\\s*", string.Empty).Trim());
                }
            }
        }

        return errors.Distinct(StringComparer.OrdinalIgnoreCase).Take(20).ToList();
    }

    private static List<HelixLogUrl> ExtractHelixLogUrls(string logContent)
    {
        var unique = new Dictionary<string, HelixLogUrl>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in HelixLogUrlRegex.Matches(logContent))
        {
            var value = new HelixLogUrl(match.Value, match.Groups[1].Value, match.Groups[2].Value);
            unique.TryAdd(value.Url, value);
        }

        return unique.Values.ToList();
    }

    private async Task<List<KnownIssue>> SearchMihubotIssuesAsync(List<string> searchTerms, string extraContext = "", string? repository = null, bool includeOpen = true, bool includeClosed = true)
    {
        if (searchTerms.Count == 0)
        {
            return [];
        }

        try
        {
            var payload = new JsonObject
            {
                ["jsonrpc"] = "2.0",
                ["method"] = "tools/call",
                ["id"] = Guid.NewGuid().ToString(),
                ["params"] = new JsonObject
                {
                    ["name"] = "search_dotnet_repos",
                    ["arguments"] = new JsonObject
                    {
                        ["repository"] = repository ?? _options.Repository,
                        ["searchTerms"] = new JsonArray(searchTerms.Select(static t => (JsonNode)JsonValue.Create(t)!).ToArray()),
                        ["extraSearchContext"] = extraContext,
                        ["includeOpen"] = includeOpen,
                        ["includeClosed"] = includeClosed,
                        ["includeIssues"] = true,
                        ["includePullRequests"] = true,
                        ["includeComments"] = false
                    }
                }
            };

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_options.TimeoutSec));
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://mihubot.xyz/mcp")
            {
                Content = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json")
            };
            using var response = await _httpClient.SendAsync(request, cts.Token);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync(cts.Token);
            var responseNode = JsonNode.Parse(content);
            var resultContent = responseNode?["result"]?["content"]?.AsArray();
            if (resultContent is null)
            {
                return [];
            }

            var results = new List<KnownIssue>();
            foreach (var item in resultContent)
            {
                if (!string.Equals(item?["type"]?.GetValue<string>(), "text", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var text = item?["text"]?.GetValue<string>();
                if (string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                JsonNode? issueData;
                try
                {
                    issueData = JsonNode.Parse(text);
                }
                catch
                {
                    continue;
                }

                if (issueData is not JsonArray issuesArray)
                {
                    continue;
                }

                foreach (var issue in issuesArray)
                {
                    results.Add(new KnownIssue(
                        issue?["Number"]?.ToString() ?? string.Empty,
                        issue?["Title"]?.ToString() ?? string.Empty,
                        issue?["Url"]?.ToString() ?? string.Empty,
                        null,
                        issue?["Repository"]?.ToString(),
                        issue?["State"]?.ToString(),
                        "MihuBot"));
                }
            }

            return results
                .Where(static issue => !string.IsNullOrWhiteSpace(issue.Number) && !string.IsNullOrWhiteSpace(issue.Repository))
                .GroupBy(static issue => $"{issue.Repository}#{issue.Number}", StringComparer.OrdinalIgnoreCase)
                .Select(static g => g.First())
                .Take(5)
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    private async Task<List<KnownIssue>> SearchKnownIssuesAsync(string? testName, string? errorMessage, string? repository = null)
    {
        if (!IsToolAvailable("gh"))
        {
            return [];
        }

        try
        {
            var searchTerms = new List<string>();
            if (!string.IsNullOrEmpty(errorMessage))
            {
                var failMatch = FailedTestInMessageRegex.Match(errorMessage);
                if (failMatch.Success)
                {
                    var failedTest = failMatch.Groups[1].Value;
                    var lastDot = failedTest.LastIndexOf('.');
                    if (lastDot >= 0 && lastDot < failedTest.Length - 1)
                    {
                        searchTerms.Add(failedTest[(lastDot + 1)..]);
                    }
                    searchTerms.Add(failedTest);
                }
            }

            if (!string.IsNullOrEmpty(errorMessage) && searchTerms.Count == 0)
            {
                var stackMatch = StackTraceSearchRegex.Match(errorMessage);
                if (stackMatch.Success)
                {
                    searchTerms.Add(stackMatch.Groups[1].Value);
                }
            }

            if (!string.IsNullOrEmpty(testName))
            {
                var lastDot = testName.LastIndexOf('.');
                if (lastDot >= 0 && lastDot < testName.Length - 1)
                {
                    var methodName = testName[(lastDot + 1)..];
                    if (!string.Equals(methodName, "Tests", StringComparison.OrdinalIgnoreCase) && methodName.Length > 5)
                    {
                        searchTerms.Add(methodName);
                    }
                }
                if (testName.Length < 100 && !Regex.IsMatch(testName, "^System\\.\\w+\\.Tests$", RegexOptions.IgnoreCase))
                {
                    searchTerms.Add(testName);
                }
            }

            if (!string.IsNullOrEmpty(errorMessage) && searchTerms.Count == 0)
            {
                var exceptionMatch = SpecificExceptionRegex.Match(errorMessage);
                if (exceptionMatch.Success)
                {
                    searchTerms.Add(exceptionMatch.Groups[1].Value);
                }
            }

            var knownIssues = new List<KnownIssue>();
            foreach (var term in searchTerms.Distinct(StringComparer.OrdinalIgnoreCase).Take(3))
            {
                var safeTerm = GetSafeSearchTerm(term);
                if (string.IsNullOrWhiteSpace(safeTerm))
                {
                    continue;
                }

                var issueList = await RunProcessCaptureAsync("gh", ["issue", "list", "--repo", repository ?? _options.Repository, "--label", "Known Build Error", "--state", "open", "--search", safeTerm, "--limit", "3", "--json", "number,title,url"]);
                if (issueList.ExitCode == 0 && !string.IsNullOrWhiteSpace(issueList.StdOut))
                {
                    var issues = JsonSerializer.Deserialize<List<GhIssue>>(issueList.StdOut, _jsonOptions) ?? [];
                    foreach (var issue in issues)
                    {
                        if (!string.IsNullOrEmpty(issue.Title) && issue.Title.Contains(safeTerm, StringComparison.OrdinalIgnoreCase))
                        {
                            knownIssues.Add(new KnownIssue(issue.Number.ToString(), issue.Title, issue.Url ?? string.Empty, safeTerm, null, null, null));
                        }
                    }
                }

                if (knownIssues.Count > 0)
                {
                    break;
                }
            }

            return knownIssues.GroupBy(static issue => issue.Number, StringComparer.OrdinalIgnoreCase).Select(static g => g.First()).ToList();
        }
        catch
        {
            return [];
        }
    }

    private async Task ShowKnownIssuesAsync(string? testName = null, string? errorMessage = null, string? repository = null, bool includeMihuBot = false)
    {
        if (string.IsNullOrEmpty(testName) && string.IsNullOrEmpty(errorMessage))
        {
            return;
        }

        var knownIssues = await SearchKnownIssuesAsync(testName, errorMessage, repository);
        if (knownIssues.Count > 0)
        {
            Console.WriteLine();
            WriteLine("  Known Issues:", ConsoleColor.Magenta);
            foreach (var issue in knownIssues)
            {
                WriteLine($"    #{issue.Number}: {issue.Title}", ConsoleColor.Magenta);
                WriteLine($"    {issue.Url}", ConsoleColor.Gray);
            }
        }

        if (!includeMihuBot)
        {
            return;
        }

        var searchTerms = new List<string>();
        if (!string.IsNullOrEmpty(errorMessage))
        {
            var failMatch = FailedTestInMessageRegex.Match(errorMessage);
            if (failMatch.Success)
            {
                var failedTest = failMatch.Groups[1].Value;
                var lastDot = failedTest.LastIndexOf('.');
                if (lastDot >= 0 && lastDot < failedTest.Length - 1)
                {
                    searchTerms.Add(failedTest[(lastDot + 1)..]);
                }
            }
        }

        if (!string.IsNullOrEmpty(testName))
        {
            var lastDot = testName.LastIndexOf('.');
            if (lastDot >= 0 && lastDot < testName.Length - 1)
            {
                var methodName = testName[(lastDot + 1)..];
                if (!string.Equals(methodName, "Tests", StringComparison.OrdinalIgnoreCase) && methodName.Length > 5)
                {
                    searchTerms.Add(methodName);
                }
            }
            searchTerms.Add(testName);
        }

        var mihuBotResults = await SearchMihubotIssuesAsync(searchTerms.Distinct(StringComparer.OrdinalIgnoreCase).Take(3).ToList(), $"test failure {testName}", repository ?? _options.Repository);
        if (mihuBotResults.Count == 0)
        {
            return;
        }

        var knownNumbers = new HashSet<string>(knownIssues.Select(static i => i.Number), StringComparer.OrdinalIgnoreCase);
        var newResults = mihuBotResults.Where(issue => !knownNumbers.Contains(issue.Number)).ToList();
        if (newResults.Count == 0)
        {
            return;
        }

        Console.WriteLine();
        WriteLine("  Related Issues (MihuBot):", ConsoleColor.Blue);
        foreach (var issue in newResults)
        {
            var stateIcon = string.Equals(issue.State, "open", StringComparison.OrdinalIgnoreCase) ? "[open]" : "[closed]";
            WriteLine($"    #{issue.Number}: {issue.Title} {stateIcon}", ConsoleColor.Blue);
            WriteLine($"    {issue.Url}", ConsoleColor.Gray);
        }
    }

    private async Task<List<AzdoTestResult>> GetAzdoTestResultsAsync(string runId, string? org = null)
    {
        if (!IsToolAvailable("az"))
        {
            return [];
        }

        try
        {
            var result = await RunProcessCaptureAsync("az",
            [
                "devops", "invoke",
                "--org", org ?? $"https://dev.azure.com/{_options.Organization}",
                "--area", "test",
                "--resource", "Results",
                "--route-parameters", $"project={_options.Project}", $"runId={runId}",
                "--api-version", "7.0",
                "--query", "value[?outcome=='Failed'].{name:testCaseTitle, outcome:outcome, error:errorMessage}",
                "-o", "json"
            ]);
            if (result.ExitCode != 0 || string.IsNullOrWhiteSpace(result.StdOut))
            {
                return [];
            }
            return JsonSerializer.Deserialize<List<AzdoTestResult>>(result.StdOut, _jsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static List<TestRunUrl> ExtractTestRunUrls(string logContent)
        => TestRunRegex.Matches(logContent).Select(static m => new TestRunUrl(m.Groups[1].Value, m.Groups[2].Value)).ToList();

    private async Task<List<LocalTestFailure>> GetLocalTestFailuresAsync(TimelineResponse timeline, int buildId)
    {
        var localFailures = new List<LocalTestFailure>();
        var records = timeline.Records ?? [];
        var testTasks = records.Where(r => ((r.Name is not null && Regex.IsMatch(r.Name, "Test|xUnit", RegexOptions.IgnoreCase)) || string.Equals(r.Type, "Task", StringComparison.OrdinalIgnoreCase))
            && r.Issues is { Count: > 0 }).ToList();

        foreach (var task in testTasks)
        {
            var testErrors = (task.Issues ?? []).Where(static issue => !string.IsNullOrEmpty(issue.Message)
                && (issue.Message.Contains("Tests failed:", StringComparison.OrdinalIgnoreCase)
                    || Regex.IsMatch(issue.Message, "error\\s*:.*Test.*failed", RegexOptions.IgnoreCase))).ToList();
            if (testErrors.Count == 0)
            {
                continue;
            }

            var parentJob = records.FirstOrDefault(r => string.Equals(r.Id, task.ParentId, StringComparison.OrdinalIgnoreCase) && string.Equals(r.Type, "Job", StringComparison.OrdinalIgnoreCase));
            var failure = new LocalTestFailure(task.Name ?? string.Empty, task.Id, parentJob?.Id ?? task.ParentId, task.Log?.Id, testErrors, []);

            var publishTask = records.FirstOrDefault(r => string.Equals(r.ParentId, task.ParentId, StringComparison.OrdinalIgnoreCase)
                && r.Name is not null && Regex.IsMatch(r.Name, "Publish.*Test.*Results", RegexOptions.IgnoreCase)
                && r.Log is not null);
            if (publishTask?.Log?.Id is int publishLogId)
            {
                var logContent = await GetBuildLogAsync(buildId, publishLogId);
                if (!string.IsNullOrEmpty(logContent))
                {
                    failure = failure with { TestRunUrls = ExtractTestRunUrls(logContent) };
                }
            }

            localFailures.Add(failure);
        }

        return localFailures;
    }

    private async Task<HelixJobDetails?> GetHelixJobDetailsAsync(string jobId)
    {
        var url = $"https://helix.dot.net/api/2019-06-17/jobs/{jobId}";
        try
        {
            return await InvokeCachedRestMethodAsync<HelixJobDetails>(url);
        }
        catch (Exception ex)
        {
            WriteWarning($"Failed to fetch Helix job {jobId}: {ex.Message}");
            return null;
        }
    }

    private async Task<List<HelixWorkItemRef>?> GetHelixWorkItemsAsync(string jobId)
    {
        var url = $"https://helix.dot.net/api/2019-06-17/jobs/{jobId}/workitems";
        try
        {
            return await InvokeCachedRestMethodAsync<List<HelixWorkItemRef>>(url);
        }
        catch (Exception ex)
        {
            WriteWarning($"Failed to fetch work items for job {jobId}: {ex.Message}");
            return null;
        }
    }

    private async Task<List<HelixListFile>?> GetHelixWorkItemFilesAsync(string jobId, string workItemName)
    {
        var encodedWorkItem = Uri.EscapeDataString(workItemName);
        var url = $"https://helix.dot.net/api/2019-06-17/jobs/{jobId}/workitems/{encodedWorkItem}/files";
        try
        {
            return await InvokeCachedRestMethodAsync<List<HelixListFile>>(url);
        }
        catch (Exception ex)
        {
            WriteWarning($"Failed to fetch files for work item {workItemName}: {ex.Message}");
            return null;
        }
    }

    private async Task<HelixWorkItemDetails?> GetHelixWorkItemDetailsAsync(string jobId, string workItemName)
    {
        var encodedWorkItem = Uri.EscapeDataString(workItemName);
        var url = $"https://helix.dot.net/api/2019-06-17/jobs/{jobId}/workitems/{encodedWorkItem}";
        try
        {
            var response = await InvokeCachedRestMethodAsync<HelixWorkItemDetails>(url);
            if (response is null)
            {
                return null;
            }

            var listFiles = await GetHelixWorkItemFilesAsync(jobId, workItemName);
            if (listFiles is not null)
            {
                response.Files = listFiles.Select(static f => new HelixArtifact { FileName = f.Name, Uri = f.Link }).ToList();
            }
            return response;
        }
        catch (Exception ex)
        {
            WriteWarning($"Failed to fetch work item {workItemName}: {ex.Message}");
            return null;
        }
    }

    private async Task<string?> GetHelixConsoleLogAsync(string url)
    {
        try
        {
            return await InvokeCachedRestMethodAsync(url);
        }
        catch (Exception ex)
        {
            WriteWarning($"Failed to fetch Helix log from {url}: {ex.Message}");
            return null;
        }
    }

    private async Task<List<BinlogWorkItemResult>> FindWorkItemsWithBinlogsAsync(string jobId, int maxItems, bool includeDetails)
    {
        var workItems = await GetHelixWorkItemsAsync(jobId);
        if (workItems is null)
        {
            WriteWarning($"No work items found for job {jobId}");
            return [];
        }

        WriteLine($"Scanning up to {maxItems} work items for binlogs...", ConsoleColor.Gray);
        var results = new List<BinlogWorkItemResult>();
        var scanned = 0;
        foreach (var wi in workItems.Take(maxItems))
        {
            scanned++;
            var details = await GetHelixWorkItemDetailsAsync(jobId, wi.Name ?? string.Empty);
            if (details?.Files is { Count: > 0 })
            {
                var binlogs = details.Files.Where(static f => f.FileName?.EndsWith(".binlog", StringComparison.OrdinalIgnoreCase) == true).ToList();
                if (binlogs.Count > 0)
                {
                    results.Add(new BinlogWorkItemResult(
                        wi.Name ?? string.Empty,
                        binlogs.Count,
                        binlogs.Select(static b => b.FileName ?? string.Empty).Where(static n => !string.IsNullOrEmpty(n)).ToList(),
                        includeDetails ? binlogs.Select(static b => b.Uri ?? string.Empty).Where(static u => !string.IsNullOrEmpty(u)).ToList() : [],
                        details.ExitCode ?? 0,
                        details.State));
                }
            }

            if (scanned % 10 == 0)
            {
                WriteLine($"  Scanned {scanned}/{maxItems}...", ConsoleColor.DarkGray);
            }
        }

        return results;
    }

    private static string? FormatTestFailure(string logContent, int maxLines, int maxFailures = 3)
    {
        var lines = SplitLines(logContent).ToArray();
        var allFailures = new List<string>();
        var currentFailure = new List<string>();
        var inFailure = false;
        var emptyLineCount = 0;
        var failureCount = 0;
        var combinedPattern = string.Join('|', FailureStartPatterns);

        foreach (var line in lines)
        {
            if (Regex.IsMatch(line, combinedPattern, RegexOptions.IgnoreCase))
            {
                if (currentFailure.Count > 0)
                {
                    allFailures.Add(string.Join("\n", currentFailure));
                    failureCount++;
                    if (failureCount >= maxFailures)
                    {
                        break;
                    }
                }

                currentFailure = [line];
                inFailure = true;
                emptyLineCount = 0;
                continue;
            }

            if (!inFailure)
            {
                continue;
            }

            currentFailure.Add(line);
            emptyLineCount = string.IsNullOrWhiteSpace(line) ? emptyLineCount + 1 : 0;
            if (emptyLineCount >= 2 || currentFailure.Count >= maxLines)
            {
                allFailures.Add(string.Join("\n", currentFailure));
                currentFailure = [];
                inFailure = false;
                failureCount++;
                if (failureCount >= maxFailures)
                {
                    break;
                }
            }
        }

        if (currentFailure.Count > 0 && failureCount < maxFailures)
        {
            allFailures.Add(string.Join("\n", currentFailure));
        }

        if (allFailures.Count == 0)
        {
            return null;
        }

        var result = string.Join("\n\n--- Next Failure ---\n\n", allFailures);
        if (failureCount >= maxFailures)
        {
            result += $"\n\n... (more failures exist, showing first {maxFailures})";
        }

        return result;
    }

    private async Task ShowTestRunResultsAsync(List<TestRunUrl> testRunUrls, string? org = null)
    {
        if (testRunUrls.Count == 0)
        {
            return;
        }

        Console.WriteLine();
        WriteLine("  Test Results:", ConsoleColor.Yellow);
        foreach (var testRun in testRunUrls)
        {
            WriteLine($"    Run {testRun.RunId}: {testRun.Url}", ConsoleColor.Gray);
            var testResults = await GetAzdoTestResultsAsync(testRun.RunId, org);
            if (testResults.Count == 0)
            {
                continue;
            }

            Console.WriteLine();
            WriteLine($"    Failed tests ({testResults.Count}):", ConsoleColor.Red);
            foreach (var result in testResults.Take(10))
            {
                WriteLine($"      - {result.Name}", ConsoleColor.White);
            }
            if (testResults.Count > 10)
            {
                WriteLine($"      ... and {testResults.Count - 10} more", ConsoleColor.Gray);
            }
        }
    }

    private void TestRepositoryFormat(string repo)
    {
        if (!RepositoryPattern.IsMatch(repo))
        {
            throw new InvalidOperationException($"Invalid repository format '{repo}'. Expected 'owner/repo' (e.g., 'dotnet/runtime').");
        }
    }

    private static string GetSafeSearchTerm(string term) => SafeSearchRegex.Replace(term, string.Empty).Trim();

    private void EnsureGhAvailable()
    {
        if (!IsToolAvailable("gh"))
        {
            throw new InvalidOperationException("GitHub CLI (gh) is required for PR lookup. Install from https://cli.github.com/ or use -BuildId instead.");
        }
    }

    private bool IsToolAvailable(string tool)
    {
        try
        {
            var result = RunProcessCaptureAsync(tool, ["--version"], allowFailure: true).GetAwaiter().GetResult();
            return result.ExitCode == 0 || !string.IsNullOrWhiteSpace(result.CombinedOutput);
        }
        catch
        {
            return false;
        }
    }

    private async Task<ProcessResult> RunProcessCaptureAsync(string fileName, IReadOnlyList<string> arguments, bool allowFailure = false)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = fileName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        foreach (var arg in arguments)
        {
            process.StartInfo.ArgumentList.Add(arg);
        }

        try
        {
            process.Start();
        }
        catch (Exception ex)
        {
            if (allowFailure)
            {
                return new ProcessResult(-1, string.Empty, ex.Message);
            }

            throw;
        }

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_options.TimeoutSec));
        var stdoutTask = process.StandardOutput.ReadToEndAsync(cts.Token);
        var stderrTask = process.StandardError.ReadToEndAsync(cts.Token);
        await process.WaitForExitAsync(cts.Token);
        var stdout = await stdoutTask;
        var stderr = await stderrTask;
        return new ProcessResult(process.ExitCode, stdout, stderr);
    }

    private static JsonObject CreateJobSummaryObject(int total, int succeeded, int failed, int canceled, int pending, int warnings, int skipped)
        => new()
        {
            ["total"] = total,
            ["succeeded"] = succeeded,
            ["failed"] = failed,
            ["canceled"] = canceled,
            ["pending"] = pending,
            ["warnings"] = warnings,
            ["skipped"] = skipped
        };

    private void EmitSummary(JsonObject summary)
    {
        Console.WriteLine("[CI_ANALYSIS_SUMMARY]");
        Console.WriteLine(summary.ToJsonString(new JsonSerializerOptions { WriteIndented = false }));
        Console.WriteLine("[/CI_ANALYSIS_SUMMARY]");
    }

    private static IEnumerable<string> SplitLines(string text)
        => text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');

    private static void WriteLine(string text, ConsoleColor color)
    {
        if (!Console.IsOutputRedirected)
        {
            var original = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ForegroundColor = original;
        }
        else
        {
            Console.WriteLine(text);
        }
    }

    private static void WriteSection(string text, ConsoleColor color) => WriteLine(text, color);

    private static void WriteWarning(string text)
    {
        if (!Console.IsErrorRedirected)
        {
            var original = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Error.WriteLine($"WARNING: {text}");
            Console.ForegroundColor = original;
        }
        else
        {
            Console.Error.WriteLine($"WARNING: {text}");
        }
    }

    private static void WriteVerbose(string text)
    {
    }
}

sealed record Options(
    int? PrNumber,
    int? BuildId,
    string? HelixJob,
    string? WorkItem,
    string Repository,
    string Organization,
    string Project,
    bool ShowLogs,
    int MaxJobs,
    int MaxFailureLines,
    int TimeoutSec,
    int ContextLines,
    bool NoCache,
    int CacheTtlSeconds,
    bool ClearCache,
    bool ContinueOnError,
    bool SearchMihubot,
    bool FindBinlogs)
{
    public static Options Parse(string[] args)
    {
        int? prNumber = null;
        int? buildId = null;
        string? helixJob = null;
        string? workItem = null;
        // Keep the Roslyn-specific default from the prior PowerShell script in this repository copy of the skill.
        var repository = "dotnet/roslyn";
        var organization = "dnceng-public";
        var project = "cbb18261-c48f-4abb-8651-8cdcb5474649";
        var showLogs = false;
        var maxJobs = 5;
        var maxFailureLines = 50;
        var timeoutSec = 30;
        var contextLines = 0;
        var noCache = false;
        var cacheTtlSeconds = 30;
        var clearCache = false;
        var continueOnError = false;
        var searchMihubot = false;
        var findBinlogs = false;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            string NextValue()
            {
                if (i + 1 >= args.Length)
                {
                    throw new ArgumentException($"Missing value for {arg}");
                }
                return args[++i];
            }

            switch (arg)
            {
                case "-PRNumber":
                case "--pr-number":
                    prNumber = int.Parse(NextValue());
                    break;
                case "-BuildId":
                case "--build-id":
                    buildId = int.Parse(NextValue());
                    break;
                case "-HelixJob":
                case "--helix-job":
                    helixJob = NextValue();
                    break;
                case "-WorkItem":
                case "--work-item":
                    workItem = NextValue();
                    break;
                case "-Repository":
                case "--repository":
                    repository = NextValue();
                    break;
                case "-Organization":
                case "--organization":
                    organization = NextValue();
                    break;
                case "-Project":
                case "--project":
                    project = NextValue();
                    break;
                case "-ShowLogs":
                case "--show-logs":
                    showLogs = true;
                    break;
                case "-MaxJobs":
                case "--max-jobs":
                    maxJobs = int.Parse(NextValue());
                    break;
                case "-MaxFailureLines":
                case "--max-failure-lines":
                    maxFailureLines = int.Parse(NextValue());
                    break;
                case "-TimeoutSec":
                case "--timeout-sec":
                    timeoutSec = int.Parse(NextValue());
                    break;
                case "-ContextLines":
                case "--context-lines":
                    contextLines = int.Parse(NextValue());
                    break;
                case "-NoCache":
                case "--no-cache":
                    noCache = true;
                    break;
                case "-CacheTtlSeconds":
                case "-CacheTTLSeconds":
                case "--cache-ttl-seconds":
                    cacheTtlSeconds = int.Parse(NextValue());
                    break;
                case "-ClearCache":
                case "--clear-cache":
                    clearCache = true;
                    break;
                case "-ContinueOnError":
                case "--continue-on-error":
                    continueOnError = true;
                    break;
                case "-SearchMihuBot":
                case "--search-mihubot":
                    searchMihubot = true;
                    break;
                case "-FindBinlogs":
                case "--find-binlogs":
                    findBinlogs = true;
                    break;
                default:
                    throw new ArgumentException($"Unknown argument: {arg}");
            }
        }

        var modeCount = (prNumber.HasValue ? 1 : 0) + (buildId.HasValue ? 1 : 0) + (!string.IsNullOrEmpty(helixJob) ? 1 : 0) + (clearCache ? 1 : 0);
        if (modeCount == 0)
        {
            throw new ArgumentException(
                "One of -PRNumber, -BuildId, -HelixJob, or -ClearCache is required.\n" +
                "Usage:\n" +
                "  Get-CIStatus.cs -PRNumber <int> [-ShowLogs] [-Repository owner/repo]\n" +
                "  Get-CIStatus.cs -BuildId <int> [-ShowLogs]\n" +
                "  Get-CIStatus.cs -HelixJob <guid> [-WorkItem <name>]\n" +
                "  Get-CIStatus.cs -ClearCache");
        }
        if (modeCount > 1)
        {
            throw new ArgumentException("Modes are mutually exclusive: -PRNumber, -BuildId, -HelixJob, -ClearCache.");
        }

        if (workItem is not null && helixJob is null)
        {
            throw new ArgumentException("-WorkItem requires -HelixJob.");
        }

        return new Options(prNumber, buildId, helixJob, workItem, repository, organization, project, showLogs, maxJobs, maxFailureLines, timeoutSec, contextLines, noCache, cacheTtlSeconds, clearCache, continueOnError, searchMihubot, findBinlogs);
    }
}

sealed record BuildLookupResult(List<int> BuildIds, string? Reason, string? MergeState);
sealed record BuildStatus(string? Status, string? Result, string? StartTime, string? FinishTime);
sealed record KnownIssue(string Number, string Title, string Url, string? SearchTerm, string? Repository, string? State, string? Source);
sealed record CorrelationFailure(string TaskName, string JobName, List<string> Errors, List<string> HelixLogs, List<string> FailedTests);
sealed record PrCorrelationResult(List<string> CorrelatedFiles, List<string> TestFiles);
sealed record TestFailure(string TestName, string FullMatch);
sealed record HelixLogUrl(string Url, string JobId, string WorkItem);
sealed record TestRunUrl(string Url, string RunId);
sealed record LocalTestFailure(string TaskName, string? TaskId, string? ParentJobId, int? LogId, List<TimelineIssue> Issues, List<TestRunUrl> TestRunUrls);
sealed record BinlogWorkItemResult(string Name, int BinlogCount, List<string> Binlogs, List<string> BinlogUris, int ExitCode, string? State);
sealed record JobSummary(int Total, int Succeeded, int Failed, int Canceled, int Pending, int Warnings, int Skipped);
sealed record ProcessResult(int ExitCode, string StdOut, string StdErr)
{
    public string CombinedOutput => string.IsNullOrWhiteSpace(StdErr) ? StdOut : string.IsNullOrWhiteSpace(StdOut) ? StdErr : StdOut + "\n" + StdErr;
}

sealed class BuildStatusResponse
{
    public string? Status { get; set; }
    public string? Result { get; set; }
    public string? StartTime { get; set; }
    public string? FinishTime { get; set; }
}

sealed class TimelineResponse
{
    public List<TimelineRecord>? Records { get; set; }
}

sealed class TimelineRecord
{
    public string? Id { get; set; }
    public string? ParentId { get; set; }
    public string? Type { get; set; }
    public string? Name { get; set; }
    public string? Result { get; set; }
    public string? State { get; set; }
    public TimelineLog? Log { get; set; }
    public List<TimelineIssue>? Issues { get; set; }
}

sealed class TimelineLog
{
    public int Id { get; set; }
}

sealed class TimelineIssue
{
    public string? Message { get; set; }
}

sealed class HelixJobDetails
{
    public string? QueueId { get; set; }
    public string? Source { get; set; }
}

sealed class HelixWorkItemRef
{
    public string? Name { get; set; }
}

sealed class HelixWorkItemDetails
{
    public string? State { get; set; }
    public int? ExitCode { get; set; }
    public string? MachineName { get; set; }
    public string? Duration { get; set; }
    public List<HelixArtifact>? Files { get; set; }
}

sealed class HelixArtifact
{
    public string? FileName { get; set; }
    public string? Uri { get; set; }
}

sealed class HelixListFile
{
    public string? Name { get; set; }
    public string? Link { get; set; }
}

sealed class GhIssue
{
    public int Number { get; set; }
    public string? Title { get; set; }
    public string? Url { get; set; }
}

sealed class AzdoTestResult
{
    public string? Name { get; set; }
    public string? Outcome { get; set; }
    public string? Error { get; set; }
}
