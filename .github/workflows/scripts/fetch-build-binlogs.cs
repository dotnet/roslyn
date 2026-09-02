#!/usr/bin/env dotnet
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Collect the binary logs of a completed, failed Azure Pipelines `roslyn-CI` PR
// build so the analysis agent can read them. Nothing here builds or executes PR
// code - it only downloads published artifacts.
//
// Advisory and best-effort: any gap emits `binlog-found=false`, which leaves the
// rest of the workflow inert. The one thing it will not do is analyze a partial
// or stale picture, so a missing failed-job artifact or a moved PR revision
// fails closed instead.
//
// Build resolution depends on RESOLVE_MODE:
//   check_run  parse the build id out of CHECK_DETAILS_URL
//   dispatch   take DISPATCH_BUILD_ID verbatim
//   latest     query the PR's newest build and require it to be completed
//
// Required environment: RESOLVE_MODE, PR_NUMBER, GH_TOKEN, GH_AW_REPO, ADO_API,
// ADO_BUILD_UI, ADO_BUILD_DEFINITION_ID, BINLOG_DIR, GITHUB_OUTPUT.
//
// Usage: dotnet run ./fetch-build-binlogs.cs
//        dotnet run ./fetch-build-binlogs.cs -- --extract <archive> <dest> <prefix> <budget> [label]
//
// The `--extract` form runs one artifact's extraction on its own, which is what
// the archive-handling tests drive; the workflow never uses it.

using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

if (args.Length > 0 && args[0] == "--extract")
{
    return RunExtractOnly(args[1..]);
}

var githubOutput = Environment.GetEnvironmentVariable("GITHUB_OUTPUT") ?? string.Empty;
if (githubOutput.Length == 0 || !TryAppendOutput(string.Empty))
{
    Console.Error.WriteLine("::error::GITHUB_OUTPUT is unset or not writable; refusing to run without a way to emit step outputs.");
    return 1;
}

var repo = Env("GH_AW_REPO");
var adoApi = Env("ADO_API");
var adoBuildUi = Env("ADO_BUILD_UI");
var adoDefinitionId = Env("ADO_BUILD_DEFINITION_ID");
var binlogDir = Env("BINLOG_DIR");

using var github = new HttpClient();
github.DefaultRequestHeaders.UserAgent.ParseAdd("roslyn-build-failure-analysis");
github.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
github.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
var token = Env("GH_TOKEN");
if (token.Length != 0)
{
    github.DefaultRequestHeaders.Authorization = new("Bearer", token);
}

using var ado = new HttpClient();
ado.DefaultRequestHeaders.UserAgent.ParseAdd("roslyn-build-failure-analysis");

// --- 1. Validate the PR number ---------------------------------------------
// `check_run.pull_requests` is empty whenever the PR comes from a fork, which
// on this repo is most of them, so a check_run alone cannot name the PR. The
// base repo's `commits/<sha>/pulls` does not list fork PRs either; the search
// index does. Require exactly one open match and then confirm that PR is
// actually at this commit, because binding to the wrong PR would post an
// analysis to someone else's thread. Step 4 re-checks the binding against the
// build's sourceBranch, so a wrong guess here still cannot reach a comment.
var prNumber = Env("PR_NUMBER");
var checkHeadSha = Env("CHECK_HEAD_SHA");
if (prNumber.Length == 0 && Regex.IsMatch(checkHeadSha, "^[0-9a-f]{40}$"))
{
    var search = await GitHubGet($"search/issues?q=repo:{repo}+is:pr+is:open+sha:{checkHeadSha}");
    if (!int.TryParse(Json.Scalar(search, "total_count"), out var matches))
    {
        matches = 0;
    }

    if (matches == 1)
    {
        var candidate = Json.Scalar(Json.First(Json.Prop(search, "items")), "number");
        var candidateHead = candidate.Length == 0
            ? string.Empty
            : Json.Scalar(await GitHubGet($"repos/{repo}/pulls/{candidate}"), "head", "sha");
        if (candidate.Length != 0 && candidateHead == checkHeadSha)
        {
            prNumber = candidate;
            Console.WriteLine($"Resolved PR #{prNumber} from check run head {checkHeadSha} (fork PRs have an empty pull_requests).");
        }
        else
        {
            Console.WriteLine($"::warning::PR #{candidate} is no longer at {checkHeadSha}; skipping a stale check run.");
            EmitNone();
        }
    }
    else if (matches > 1)
    {
        Console.WriteLine($"::warning::Head {checkHeadSha} matches {matches} open PRs; refusing to guess which to analyze.");
        EmitNone();
    }
}

// It is interpolated into GitHub API paths and into the `refs/pull/<n>/merge`
// comparison, and on dispatch and slash commands it is free-form input.
if (!Regex.IsMatch(prNumber, "^[0-9]+$"))
{
    Console.WriteLine($"::warning::Resolved PR number '{prNumber}' is not numeric or empty; refusing.");
    EmitNone();
}

// --- 2. Scope check: only PRs that roslyn-CI targets ------------------------
var prJson = await GitHubGet($"repos/{repo}/pulls/{prNumber}");
var baseRef = Json.Scalar(prJson, "base", "ref");
// An empty base ref means the API call failed, not that the PR is out of scope.
if (baseRef.Length == 0)
{
    Console.WriteLine($"::warning::Could not resolve the base ref for PR #{prNumber}; treating as a data-resolution failure.");
    EmitNone();
}

var inScope = baseRef is "main" or "main-vs-deps" or "community"
    || baseRef.StartsWith("release/", StringComparison.Ordinal)
    || baseRef.StartsWith("features/", StringComparison.Ordinal)
    || baseRef.StartsWith("demos/", StringComparison.Ordinal);
if (!inScope)
{
    Console.WriteLine($"::warning::PR #{prNumber} base '{baseRef}' is not targeted by roslyn-CI; skipping.");
    EmitNone();
}

Console.WriteLine($"PR #{prNumber} base '{baseRef}' is in scope.");

// --- 3. Resolve and validate the Azure DevOps build id ----------------------
var resolveMode = Env("RESOLVE_MODE");
var buildId = string.Empty;
switch (resolveMode)
{
    case "dispatch":
        buildId = Env("DISPATCH_BUILD_ID");
        break;

    case "check_run":
        // details_url looks like: .../_build/results?buildId=NNN&view=...
        var details = Regex.Match(Env("CHECK_DETAILS_URL"), "buildId=([0-9]+)");
        buildId = details.Success ? details.Groups[1].Value : string.Empty;
        break;

    case "latest":
        // Take the newest build regardless of status. If it is still running -
        // e.g. right after a force-push - skip rather than pair an older
        // failure with the PR's current head.
        var buildsJson = await AdoGet($"build list for PR #{prNumber}",
            $"{adoApi}/build/builds?definitions={adoDefinitionId}&branchName=refs/pull/{prNumber}/merge&queryOrder=queueTimeDescending&$top=1&api-version=7.1");
        if (buildsJson is null)
        {
            EmitNone();
        }

        var newest = Json.First(Json.Prop(buildsJson, "value"));
        buildId = Json.Scalar(newest, "id");
        var buildStatus = Json.Scalar(newest, "status");
        Console.WriteLine($"Newest roslyn-CI build for PR #{prNumber}: id='{buildId}' status='{buildStatus}'");
        if (buildId.Length != 0 && buildStatus != "completed")
        {
            Console.WriteLine($"::warning::PR #{prNumber}'s newest roslyn-CI build ({buildId}) is still '{buildStatus}'; wait for it to finish.");
            EmitNone();
        }

        break;

    default:
        Console.WriteLine($"::warning::Unknown RESOLVE_MODE '{resolveMode}'; refusing.");
        EmitNone();
        break;
}

// The id is interpolated into ADO API URLs, so require it to be purely numeric.
if (!Regex.IsMatch(buildId, "^[0-9]+$"))
{
    Console.WriteLine($"::warning::Resolved ADO build id '{buildId}' is not numeric or empty; refusing.");
    EmitNone();
}

// --- 4. Validate the build on every trigger path ---------------------------
// On `check_run` the build id comes from a payload we don't fully trust; on
// dispatch the build id and PR number are independent inputs. Either way the
// build must be roslyn-CI, must have failed, and must belong to this PR.
var buildJson = await AdoGet($"details of build {buildId}", $"{adoApi}/build/builds/{buildId}?api-version=7.1");
if (buildJson is null)
{
    EmitNone();
}

var result = Json.Scalar(buildJson, "result");
var definitionId = Json.Scalar(buildJson, "definition", "id");
var sourceBranch = Json.Scalar(buildJson, "sourceBranch");
Console.WriteLine($"ADO build {buildId}: result='{result}' definition='{definitionId}' sourceBranch='{sourceBranch}'");
if (definitionId != adoDefinitionId)
{
    Console.WriteLine($"::warning::ADO build {buildId} is definition '{definitionId}', not roslyn-CI ({adoDefinitionId}); refusing.");
    EmitNone();
}

if (result != "failed")
{
    Console.WriteLine($"::warning::ADO build {buildId} did not fail (result='{result}'); nothing to analyze.");
    EmitNone();
}

if (sourceBranch != $"refs/pull/{prNumber}/merge")
{
    Console.WriteLine($"::warning::ADO build {buildId} sourceBranch '{sourceBranch}' does not match PR #{prNumber}; refusing to avoid posting to the wrong PR.");
    EmitNone();
}

// --- 5. Require the build to describe the PR's current revision ------------
// ADO builds GitHub's `refs/pull/<n>/merge`, so `sourceVersion` is the merge
// commit as of build time. Comparing it as well as the head catches a base
// branch that advanced while the PR head stayed put.
var buildPrSha = Json.Scalar(buildJson, "triggerInfo", "pr.sourceSha");
var buildMergeSha = Json.Scalar(buildJson, "sourceVersion");
var currentHead = Json.Scalar(prJson, "head", "sha");
var currentMerge = Json.Scalar(prJson, "merge_commit_sha");
if (buildPrSha.Length == 0 || currentHead.Length == 0 || buildMergeSha.Length == 0 || currentMerge.Length == 0)
{
    Console.WriteLine("::warning::Could not resolve all build/current head and merge revisions; skipping to avoid analyzing a stale binlog.");
    EmitNone();
}

if (buildPrSha != currentHead)
{
    Console.WriteLine($"::warning::Build {buildId} analyzed '{buildPrSha}' but PR #{prNumber} head is now '{currentHead}'; skipping stale build.");
    EmitNone();
}

if (buildMergeSha != currentMerge)
{
    Console.WriteLine($"::warning::Build {buildId} merge revision '{buildMergeSha}' but PR #{prNumber} current merge is '{currentMerge}' (base advanced); skipping stale merge.");
    EmitNone();
}

var headSha = currentHead;
Console.WriteLine($"Analyzing build {buildId} at PR head revision '{headSha}'.");

// --- 6. Select the log artifacts of failed or canceled jobs ----------------
// Roslyn publishes "<job> Attempt <N> Logs" for most jobs, with explicit
// exceptions for Source Build and the bootstrap-correctness leg. Bases are
// matched exactly, and every retry attempt is kept.
var timelineJson = await AdoGet($"timeline of build {buildId}", $"{adoApi}/build/builds/{buildId}/timeline?api-version=7.1");
if (timelineJson is null)
{
    EmitNone();
}

var failedJobNames = Json.Items(Json.Prop(timelineJson, "records"))
    .Where(record => Json.Scalar(record, "type") == "Job"
        && Json.Scalar(record, "result") is "failed" or "canceled")
    .Select(record => Json.Scalar(record, "name"))
    .Where(name => name.Trim().Length != 0)
    .Distinct(StringComparer.Ordinal)
    .ToList();
if (failedJobNames.Count == 0)
{
    Console.WriteLine($"::warning::No failed or canceled jobs in the timeline for build {buildId}.");
    EmitNone();
}

var artifactsJson = await AdoGet($"artifact list of build {buildId}", $"{adoApi}/build/builds/{buildId}/artifacts?api-version=7.1");
if (artifactsJson is null)
{
    EmitNone();
}

var artifacts = Json.Items(Json.Prop(artifactsJson, "value")).ToList();
var attemptLogs = new Regex(@"^(.+) Attempt ([0-9]+) Logs$");
var sourceBuildLogs = new Regex(@"^BuildLogs_SourceBuild_Managed_Attempt[0-9]+$");
var allNames = artifacts
    .Select(artifact => Json.Scalar(artifact, "name"))
    .Where(name => Regex.IsMatch(name, " Attempt [0-9]+ Logs$") || sourceBuildLogs.IsMatch(name))
    .ToList();

var matched = new List<string>();
foreach (var jobName in failedJobNames)
{
    var expected = jobName;
    var sourceBuild = false;
    switch (jobName)
    {
        case "Source-Build (Managed)":
            sourceBuild = true;
            break;
        case "Correctness_Bootstrap_Build_Default":
            expected = "Correctness_Bootstrap_Build - Default";
            break;
    }

    foreach (var name in allNames)
    {
        if (sourceBuild && sourceBuildLogs.IsMatch(name))
        {
            matched.Add(name);
            continue;
        }

        var attempt = attemptLogs.Match(name);
        if (attempt.Success && attempt.Groups[1].Value == expected)
        {
            matched.Add(name);
        }
    }
}

var names = matched
    .Where(name => name.Trim().Length != 0)
    .Distinct(StringComparer.Ordinal)
    .ToList();
if (names.Count == 0)
{
    Console.WriteLine($"::warning::No build-log artifacts matched the failed or canceled jobs in build {buildId}; the failure is likely outside a build leg.");
    EmitNone();
}

Console.WriteLine($"Selected {names.Count} of {allNames.Count} build-log artifacts for {failedJobNames.Count} failed or canceled jobs.");

// --- 7. Download and extract each selected artifact ------------------------
// Per-artifact compressed cap. Roslyn's `Correctness_Analyzers` log artifact is
// routinely ~600 MB, so this has to be well clear of that or the workflow
// silently skips exactly the correctness legs it exists to diagnose. Only one
// archive is on disk at a time (each is truncated before the next download), so
// this bounds peak zip disk use, not the sum across artifacts.
const long MaxZipBytes = 2147483648;    // 2 GB compressed per artifact
const long MaxTotalBytes = 4294967296;  // 4 GB extracted across all artifacts
// Raising the per-artifact cap would otherwise raise the worst-case number of
// bytes pulled over the network by the same factor, since nothing else bounds
// the sum across artifacts. Cap the total download too, and charge it *before*
// each transfer (see zipCap below) rather than after, so the last artifact
// can't start just under the limit and still pull a full MaxZipBytes.
const long MaxTotalZipBytes = 3221225472;  // 3 GB compressed across all artifacts
var totalZipBytes = 0L;
// Per-transfer bounds only. The *phase* is bounded by the `timeout 600` wrapper
// the workflow puts around this app: if the fetch runs long it is killed,
// `binlog-found` is never written, and the activation gate (plus the follow-up
// step in the workflow) turns that into a warning and a no-op. That is the same
// outcome an in-process deadline produced, without tracking the clock here.
var maxAttempt = TimeSpan.FromSeconds(120);      // per attempt; the full set really takes ~30s
var maxRetryWindow = TimeSpan.FromSeconds(240);  // whole retry window for one artifact
var remainingBytes = MaxTotalBytes;

// One private scratch file for every download. A fixed /tmp name is a
// pre-created symlink, or a second job on the same runner, away from being
// someone else's file.
var zipTmp = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
Directory.CreateDirectory(binlogDir);
// Only binlogs extracted by this run may be analyzed. Anything left in the
// directory by an earlier run on the same runner would otherwise be uploaded
// and attributed to this build.
foreach (var stale in Directory.EnumerateFiles(binlogDir, "*.binlog"))
{
    TryDelete(stale);
}

var count = 0;
var stagedLegs = 0;
var ai = 0;
foreach (var name in names)
{
    ai++;
    // `name` is PR-controlled artifact metadata; keep a sanitized copy for log
    // output and use the original only as an artifact lookup key.
    var safeName = Sanitize(name);
    var artifact = artifacts.FirstOrDefault(candidate => Json.Scalar(candidate, "name") == name);
    var url = Json.Scalar(artifact, "resource", "downloadUrl");
    if (url.Length == 0)
    {
        Console.WriteLine($"::warning::Skipping {safeName}: no download URL.");
        continue;
    }

    // Start every artifact from an empty file, so a body retained by a previous
    // artifact can never be measured, charged or extracted twice.
    Truncate(zipTmp);
    // Bound this transfer by whatever is left of the cumulative budget as well
    // as by the per-artifact cap, so the two limits together are a real ceiling
    // on bytes pulled rather than `MaxTotalZipBytes + MaxZipBytes`.
    var zipCap = Math.Min(MaxZipBytes, MaxTotalZipBytes - totalZipBytes);
    if (zipCap <= 0)
    {
        Console.WriteLine($"::warning::Cumulative compressed download budget {MaxTotalZipBytes} is exhausted before {safeName}; stopping downloads.");
        break;
    }

    // Download to a file, never to memory: these are gigabyte-scale responses,
    // and a retry has to start from a clean slate. The file is truncated before
    // each attempt, so an error body followed by a successful retry cannot
    // leave a corrupt `<error page><zip>` behind.
    var (zipBytes, downloadError) = await Download(url, zipTmp, zipCap);
    // Charge the budget with the bytes retained on disk, including those of an
    // artifact about to be skipped. This is a disk and extraction budget, not a
    // meter of network egress: the file is truncated before each retry, so
    // failed attempts are not counted here. What bounds those is the
    // `timeout 600` wrapper around this app plus the per-transfer cap, which
    // stops every individual attempt at zipCap.
    totalZipBytes += zipBytes;
    if (zipBytes == 0)
    {
        Console.WriteLine($"::warning::Skipping {safeName}: empty or failed download.");
        continue;
    }

    if (zipBytes >= zipCap)
    {
        Console.WriteLine($"::warning::Skipping {safeName}: download reached the {zipCap}-byte cap.");
        continue;
    }

    if (downloadError is not null)
    {
        Console.WriteLine($"::warning::Skipping {safeName}: download failed or was truncated ({downloadError}).");
        continue;
    }

    // The extractor writes generated `<ai>_<n>_<name>.binlog` names straight
    // into binlogDir and stops once it has written remainingBytes, so it bounds
    // both where bytes land and how many there are.
    int extracted;
    long written;
    try
    {
        (extracted, written) = Extractor.Extract(zipTmp, binlogDir, ai.ToString(), remainingBytes, safeName);
    }
    catch (Exception ex)
    {
        // A rejected or aborted extraction may have left partial files behind.
        DeletePartials(ai);
        Console.WriteLine($"::warning::Skipping {safeName}: extraction failed ({ex.Message.ReplaceLineEndings(" ")}).");
        continue;
    }

    if (extracted == 0)
    {
        DeletePartials(ai);
        Console.WriteLine($"::warning::Skipping {safeName}: no binlogs found in the artifact.");
        continue;
    }

    // Charge the budget by bytes actually written rather than by any size the
    // archive declares about itself.
    remainingBytes = Math.Max(0, remainingBytes - written);
    count += extracted;
    stagedLegs++;
    Console.WriteLine($"Extracted {extracted} binlog(s) ({written} bytes) from {safeName}.");
}

TryDelete(zipTmp);

Console.WriteLine($"Extracted {count} binlog(s) from {stagedLegs}/{names.Count} selected artifacts into {binlogDir}:");
foreach (var staged in Directory.EnumerateFiles(binlogDir).Order(StringComparer.Ordinal))
{
    Console.WriteLine($"  {new FileInfo(staged).Length,12}  {Path.GetFileName(staged)}");
}

if (count == 0)
{
    Console.WriteLine($"::warning::No *.binlog found in the selected build-log artifacts of build {buildId}.");
    EmitNone();
}

// Fail closed on a partial set: the artifact that failed to yield a binlog could
// be the attempt holding the root cause.
if (stagedLegs != names.Count)
{
    Console.WriteLine($"::warning::Only {stagedLegs} of {names.Count} selected artifacts produced a usable binlog; skipping incomplete failed-job data.");
    EmitNone();
}

// --- 8. Re-check the revision after a download that can take minutes -------
// A force-push or base advance during the download would leave the analyzed
// binlogs stale relative to the diff that inline comments are pinned to.
var latestPr = await GitHubGet($"repos/{repo}/pulls/{prNumber}");
var latestHead = Json.Scalar(latestPr, "head", "sha");
var latestMerge = Json.Scalar(latestPr, "merge_commit_sha");
if (latestHead.Length == 0 || latestHead != headSha)
{
    Console.WriteLine($"::warning::PR #{prNumber} head changed during download ('{headSha}' -> '{latestHead}') or could not be re-resolved; skipping.");
    EmitNone();
}

if (latestMerge.Length == 0 || latestMerge != buildMergeSha)
{
    Console.WriteLine($"::warning::PR #{prNumber} merge revision changed during download ('{buildMergeSha}' -> '{latestMerge}') or could not be re-resolved; skipping.");
    EmitNone();
}

TryAppendOutput(
    "binlog-found=true\n" +
    $"pr-number={prNumber}\n" +
    $"pr-head-sha={headSha}\n" +
    $"pr-merge-sha={buildMergeSha}\n" +
    $"ado-build-id={buildId}\n" +
    $"ado-build-url={adoBuildUi}?buildId={buildId}\n");
return 0;

static string Env(string name) => Environment.GetEnvironmentVariable(name) ?? string.Empty;

bool TryAppendOutput(string text)
{
    try
    {
        File.AppendAllText(githubOutput, text);
        return true;
    }
    catch (Exception)
    {
        return false;
    }
}

void EmitNone()
{
    TryAppendOutput("binlog-found=false\n");
    Environment.Exit(0);
}

static void TryDelete(string path)
{
    try
    {
        File.Delete(path);
    }
    catch (Exception)
    {
        // Best-effort cleanup; a leftover file under the runner's temp
        // directory is not worth failing the fetch over.
    }
}

static void Truncate(string path)
{
    using var _ = new FileStream(path, FileMode.Create, FileAccess.Write);
}

void DeletePartials(int prefix)
{
    foreach (var partial in Directory.EnumerateFiles(binlogDir, $"{prefix}_*.binlog"))
    {
        TryDelete(partial);
    }
}

// Build metadata that reaches a log line is PR-controlled, so map it to the
// same conservative set the extractor uses for generated file names.
static string Sanitize(string value)
{
    var builder = new StringBuilder(value.Length);
    foreach (var c in value)
    {
        builder.Append(char.IsAsciiLetterOrDigit(c) || c is '.' or '_' or '-' ? c : '_');
    }

    return builder.ToString();
}

// Fetch a GitHub API document. A failure is reported as an absent document, so
// the caller reads empty fields and takes its own data-resolution branch.
async Task<JsonElement?> GitHubGet(string path)
{
    try
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        using var response = await github.GetAsync($"https://api.github.com/{path}", cts.Token);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cts.Token));
        return document.RootElement.Clone();
    }
    catch (Exception)
    {
        return null;
    }
}

// Fetch an Azure DevOps API document. A network failure or a non-JSON body is a
// data-resolution failure, not evidence that there is nothing to analyze, so it
// is reported as such instead of falling through to an empty `records`/`value`
// and a misleading "no failed jobs" warning.
async Task<JsonElement?> AdoGet(string what, string url)
{
    // These are small JSON documents; cap them so a stalled endpoint fails in
    // seconds rather than hanging the job until its overall timeout. The
    // per-attempt cap keeps one hung connection cheap, and the retry window is
    // what actually bounds the call: without it these few metadata fetches
    // could cumulatively consume the job's `timeout-minutes` on their own. The
    // artifact download below sets its own, much larger, budget.
    var (body, error) = await Get(ado, url, TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(40), attempt => TimeSpan.FromSeconds(1 << attempt));
    if (error is not null || body!.Length == 0)
    {
        Console.WriteLine($"::warning::Could not fetch the {what} from Azure DevOps ({error ?? "empty body"}); treating as a data-resolution failure.");
        return null;
    }

    try
    {
        using var document = JsonDocument.Parse(body);
        return document.RootElement.Clone();
    }
    catch (JsonException)
    {
        Console.WriteLine($"::warning::Azure DevOps returned a non-JSON {what}; treating as a data-resolution failure.");
        return null;
    }
}

// Retry transient failures only: a 404 is an answer, and asking again just
// spends the window. Timeouts, dropped connections and the 408/429/5xx range
// are the responses worth repeating.
static bool IsTransient(HttpStatusCode status)
    => status is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests
        or HttpStatusCode.InternalServerError or HttpStatusCode.BadGateway
        or HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout;

static async Task<(string? Body, string? Error)> Get(
    HttpClient client, string url, TimeSpan perAttempt, TimeSpan retryWindow, Func<int, TimeSpan> delay)
{
    var started = DateTime.UtcNow;
    var error = "no attempt was made";
    for (var attempt = 0; attempt <= 3; attempt++)
    {
        if (attempt != 0)
        {
            // The retry window is measured from the first attempt, so a retry
            // that could not start inside it is not started at all.
            var wait = delay(attempt - 1);
            if (DateTime.UtcNow - started + wait >= retryWindow)
            {
                break;
            }

            await Task.Delay(wait);
        }

        try
        {
            using var cts = new CancellationTokenSource(perAttempt);
            using var response = await client.GetAsync(url, cts.Token);
            if (response.IsSuccessStatusCode)
            {
                return (await response.Content.ReadAsStringAsync(cts.Token), null);
            }

            error = $"HTTP {(int)response.StatusCode}";
            if (!IsTransient(response.StatusCode))
            {
                return (null, error);
            }
        }
        catch (Exception ex)
        {
            error = ex is OperationCanceledException ? "timed out" : ex.GetType().Name;
        }
    }

    return (null, error);
}

// Stream the artifact to disk, stopping at `cap` bytes. The cap is applied to
// the bytes actually written rather than to any length the response declares,
// so a response with no Content-Length - or a lying one - cannot fill the disk.
// Returns the bytes retained on disk, which the caller charges to its budget
// whether or not it goes on to use them.
async Task<(long Bytes, string? Error)> Download(string url, string path, long cap)
{
    var started = DateTime.UtcNow;
    var error = "no attempt was made";
    for (var attempt = 0; attempt <= 3; attempt++)
    {
        if (attempt != 0)
        {
            var wait = TimeSpan.FromSeconds(2);
            if (DateTime.UtcNow - started + wait >= maxRetryWindow)
            {
                break;
            }

            await Task.Delay(wait);
        }

        try
        {
            using var cts = new CancellationTokenSource(maxAttempt);
            using var response = await ado.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cts.Token);
            if (!response.IsSuccessStatusCode)
            {
                error = $"HTTP {(int)response.StatusCode}";
                if (!IsTransient(response.StatusCode))
                {
                    return (0, error);
                }

                continue;
            }

            using var source = await response.Content.ReadAsStreamAsync(cts.Token);
            // Truncate before each attempt so a partial body from a failed
            // attempt is never prepended to a successful one.
            using var output = new FileStream(path, FileMode.Create, FileAccess.Write);
            var buffer = new byte[1024 * 1024];
            var written = 0L;
            int read;
            while ((read = await source.ReadAsync(buffer, cts.Token)) > 0)
            {
                // Stop exactly at the cap. The caller treats a transfer that
                // reached it as over-budget and skips the artifact, so there is
                // no reason to spend another byte on it.
                if (written + read >= cap)
                {
                    output.Write(buffer, 0, (int)(cap - written));
                    return (cap, "reached the transfer cap");
                }

                output.Write(buffer, 0, read);
                written += read;
            }

            return (written, null);
        }
        catch (Exception ex)
        {
            error = ex is OperationCanceledException ? "timed out" : ex.GetType().Name;
        }
    }

    return (RetainedBytes(path), error);
}

static long RetainedBytes(string path)
{
    try
    {
        var info = new FileInfo(path);
        return info.Exists ? info.Length : 0;
    }
    catch (Exception)
    {
        return 0;
    }
}

static int RunExtractOnly(string[] extractArgs)
{
    if (extractArgs.Length is < 4 or > 5)
    {
        Console.Error.WriteLine("usage: fetch-build-binlogs.cs --extract <archive> <dest> <prefix> <budget> [label]");
        return 1;
    }

    if (!long.TryParse(extractArgs[3], out var budgetBytes))
    {
        Console.Error.WriteLine("budget-bytes must be an integer");
        return 1;
    }

    try
    {
        var (count, written) = Extractor.Extract(
            extractArgs[0], extractArgs[1], extractArgs[2], budgetBytes,
            extractArgs.Length == 5 ? extractArgs[4] : string.Empty);
        Console.Out.WriteLine($"{count} {written}");
        return 0;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine(ex.Message);
        return 1;
    }
}

// Extract the *.binlog entries of one Azure DevOps build-log artifact.
//
// The archive is produced by a PR-triggered build, so its entry paths, metadata
// and contents are untrusted. Two properties keep extraction safe:
//
//   * destination names are generated here, never taken from the archive, so a
//     traversal or absolute path cannot choose where bytes land;
//   * writing stops as soon as the remaining byte budget is exceeded, so a zip
//     bomb cannot fill the runner disk.
//
// Entry paths and types are still validated up front - an archive containing a
// traversal path or a link/device entry is hostile rather than merely odd, so
// the whole artifact is rejected instead of partially extracted.
static class Extractor
{
    private const int ChunkSize = 1024 * 1024;

    public static (int Count, long Written) Extract(
        string archivePath, string destination, string prefix, long budgetBytes, string label)
    {
        // Artifact names are untrusted build metadata, so re-sanitize here
        // rather than trusting the caller: only the destination name generated
        // in this method may decide where bytes land.
        var safeLabel = label.Length == 0 ? string.Empty : GetSafeLabel(label);

        using var zip = ZipFile.OpenRead(archivePath);

        // Validate every entry before reading any payload.
        for (var i = 0; i < zip.Entries.Count; i++)
        {
            var entry = zip.Entries[i];
            if (IsUnsafePath(entry.FullName))
            {
                throw new InvalidDataException($"archive entry {i} has an unsafe path");
            }

            if (IsUnsupportedType(entry))
            {
                throw new InvalidDataException($"archive entry {i} has an unsupported type");
            }
        }

        var selected = zip.Entries
            .Where(entry => !IsDirectoryEntry(entry) && entry.FullName.EndsWith(".binlog", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Directory.CreateDirectory(destination);

        var written = 0L;
        var buffer = new byte[ChunkSize];

        for (var index = 0; index < selected.Length; index++)
        {
            var stem = safeLabel.Length == 0 ? $"{prefix}_{index}" : $"{prefix}_{index}_{safeLabel}";
            var target = Path.Combine(destination, $"{stem}.binlog");

            using var source = selected[index].Open();

            // CreateNew, so a name that somehow already exists is an error
            // rather than a silent overwrite of a previous artifact's binlog.
            using var output = new FileStream(target, FileMode.CreateNew, FileAccess.Write);

            int read;
            while ((read = source.Read(buffer, 0, buffer.Length)) > 0)
            {
                written += read;
                if (written > budgetBytes)
                {
                    throw new InvalidDataException("extracted binlogs exceed the remaining budget");
                }

                output.Write(buffer, 0, read);
            }
        }

        return (selected.Length, written);
    }

    private static string GetSafeLabel(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var c in value)
        {
            builder.Append(char.IsAsciiLetterOrDigit(c) || c is '.' or '_' or '-' ? c : '_');
        }

        var result = builder.ToString().Trim('.', '_', '-');
        return result.Length > 80 ? result[..80] : result;
    }

    private static bool IsUnsafePath(string name)
    {
        if (name.Contains('\0'))
        {
            return true;
        }

        var normalized = name.Replace('\\', '/');
        if (normalized.StartsWith('/'))
        {
            return true;
        }

        var parts = normalized.Split('/');
        if (Array.IndexOf(parts, "..") >= 0)
        {
            return true;
        }

        // A Windows drive spec such as "c:/foo" or "c:foo" is not rooted by
        // POSIX rules but is still an attempt to escape the destination.
        var first = parts[0];
        return first.Length >= 2 && char.IsAsciiLetter(first[0]) && first[1] == ':';
    }

    private static bool IsUnsupportedType(ZipArchiveEntry entry)
    {
        // S_IFMT mask and the only entry types a log artifact may legitimately contain.
        const int FileTypeMask = 0xF000;
        const int RegularFile = 0x8000;
        const int DirectoryType = 0x4000;

        // Entries written on Windows carry no Unix mode; 0 means "unspecified",
        // which is not evidence of a hostile type.
        var mode = (entry.ExternalAttributes >> 16) & 0xFFFF;
        var fileType = mode & FileTypeMask;
        return fileType != 0 && fileType != RegularFile && fileType != DirectoryType;
    }

    private static bool IsDirectoryEntry(ZipArchiveEntry entry)
        => entry.FullName.EndsWith('/') || entry.Name.Length == 0;
}

// Every document here is third-party JSON whose shape is not guaranteed, so
// reads are total: a missing property, a null, or the wrong kind all read as
// absent rather than throwing partway through a validation sequence.
static class Json
{
    public static JsonElement? Prop(JsonElement? element, string name)
        => element is { ValueKind: JsonValueKind.Object } value && value.TryGetProperty(name, out var property)
            ? property
            : null;

    public static JsonElement? First(JsonElement? element)
        => element is { ValueKind: JsonValueKind.Array } array && array.GetArrayLength() != 0
            ? array[0]
            : null;

    public static IEnumerable<JsonElement> Items(JsonElement? element)
        => element is { ValueKind: JsonValueKind.Array } array ? array.EnumerateArray() : [];

    public static string Scalar(JsonElement? element, params string[] path)
    {
        var current = element;
        foreach (var name in path)
        {
            current = Prop(current, name);
        }

        return current switch
        {
            { ValueKind: JsonValueKind.String } value => value.GetString() ?? string.Empty,
            { ValueKind: JsonValueKind.Number } value => value.GetRawText(),
            { ValueKind: JsonValueKind.True } => "true",
            { ValueKind: JsonValueKind.False } => "false",
            _ => string.Empty,
        };
    }
}
