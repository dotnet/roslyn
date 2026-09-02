#!/usr/bin/env dotnet
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Collects the binary logs of a completed, failed Azure Pipelines `roslyn-CI` PR
// build so the analysis agent can read them. Only downloads published artifacts;
// nothing here builds or executes PR code.
//
// Advisory: any gap emits `binlog-found=false`, which leaves the rest of the
// workflow inert. It will not analyze a partial or stale picture, so a missing
// failed-job artifact or a moved PR revision fails closed instead.
//
// Environment: RESOLVE_MODE, PR_NUMBER, GH_TOKEN, GH_AW_REPO, ADO_API,
// ADO_BUILD_UI, ADO_BUILD_DEFINITION_ID, BINLOG_DIR, GITHUB_OUTPUT.
//
// Usage: dotnet run --file ./fetch-build-binlogs.cs
//        dotnet run --file ./fetch-build-binlogs.cs -- --extract <archive> <dest> <prefix> <budget> [label]

using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
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

// --- 1. Resolve and validate the PR number ---------------------------------
// `check_run.pull_requests` is empty for fork PRs, and the base repo's
// `commits/<sha>/pulls` does not list them either; the search index does.
// Binding to the wrong PR would post an analysis to someone else's thread, so
// require exactly one open match that is still at this commit.
var prNumber = Env("PR_NUMBER");
var checkHeadSha = Env("CHECK_HEAD_SHA");
if (prNumber.Length == 0 && Regex.IsMatch(checkHeadSha, "^[0-9a-f]{40}$"))
{
    var search = await GitHubGet($"search/issues?q=repo:{repo}+is:pr+is:open+sha:{checkHeadSha}");
    if (!int.TryParse(search.At("total_count").Text(), out var matches))
    {
        matches = 0;
    }

    EmitNoneIf(matches > 1, $"Head {checkHeadSha} matches {matches} open PRs; refusing to guess which to analyze.");
    if (matches == 1)
    {
        var candidate = search.At("items").Items().FirstOrDefault().At("number").Text();
        var candidateHead = candidate.Length == 0
            ? string.Empty
            : (await GitHubGet($"repos/{repo}/pulls/{candidate}")).At("head", "sha").Text();
        EmitNoneIf(candidate.Length == 0 || candidateHead != checkHeadSha,
            $"PR #{candidate} is no longer at {checkHeadSha}; skipping a stale check run.");
        prNumber = candidate;
        Console.WriteLine($"Resolved PR #{prNumber} from check run head {checkHeadSha}.");
    }
}

// Interpolated into API paths and into the `refs/pull/<n>/merge` comparison.
EmitNoneIf(!Regex.IsMatch(prNumber, "^[0-9]+$"), $"Resolved PR number '{prNumber}' is not numeric or empty; refusing.");

// --- 2. Scope check: only PRs that roslyn-CI targets ------------------------
var prJson = await GitHubGet($"repos/{repo}/pulls/{prNumber}");
var baseRef = prJson.At("base", "ref").Text();
// An empty base ref means the API call failed, not that the PR is out of scope.
EmitNoneIf(baseRef.Length == 0, $"Could not resolve the base ref for PR #{prNumber}; treating as a data-resolution failure.");
EmitNoneIf(
    baseRef is not ("main" or "main-vs-deps" or "community")
        && !baseRef.StartsWith("release/", StringComparison.Ordinal)
        && !baseRef.StartsWith("features/", StringComparison.Ordinal)
        && !baseRef.StartsWith("demos/", StringComparison.Ordinal),
    $"PR #{prNumber} base '{baseRef}' is not targeted by roslyn-CI; skipping.");
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
        // e.g. right after a force-push - skip rather than pair an older failure
        // with the PR's current head.
        var newest = (await AdoGet($"build list for PR #{prNumber}",
            $"{adoApi}/build/builds?definitions={adoDefinitionId}&branchName=refs/pull/{prNumber}/merge&queryOrder=queueTimeDescending&$top=1&api-version=7.1"))
            .At("value").Items().FirstOrDefault();
        buildId = newest.At("id").Text();
        var buildStatus = newest.At("status").Text();
        Console.WriteLine($"Newest roslyn-CI build for PR #{prNumber}: id='{buildId}' status='{buildStatus}'");
        EmitNoneIf(buildId.Length != 0 && buildStatus != "completed",
            $"PR #{prNumber}'s newest roslyn-CI build ({buildId}) is still '{buildStatus}'; wait for it to finish.");
        break;

    default:
        EmitNone($"Unknown RESOLVE_MODE '{resolveMode}'; refusing.");
        break;
}

// Interpolated into ADO API URLs.
EmitNoneIf(!Regex.IsMatch(buildId, "^[0-9]+$"), $"Resolved ADO build id '{buildId}' is not numeric or empty; refusing.");

// --- 4. Validate the build on every trigger path ---------------------------
// On `check_run` the build id comes from a payload we don't fully trust; on
// dispatch the build id and PR number are independent inputs. Either way the
// build must be roslyn-CI, must have failed, and must belong to this PR.
var buildJson = await AdoGet($"details of build {buildId}", $"{adoApi}/build/builds/{buildId}?api-version=7.1");
var result = buildJson.At("result").Text();
var definitionId = buildJson.At("definition", "id").Text();
var sourceBranch = buildJson.At("sourceBranch").Text();
Console.WriteLine($"ADO build {buildId}: result='{result}' definition='{definitionId}' sourceBranch='{sourceBranch}'");
EmitNoneIf(definitionId != adoDefinitionId,
    $"ADO build {buildId} is definition '{definitionId}', not roslyn-CI ({adoDefinitionId}); refusing.");
EmitNoneIf(result != "failed", $"ADO build {buildId} did not fail (result='{result}'); nothing to analyze.");
EmitNoneIf(sourceBranch != $"refs/pull/{prNumber}/merge",
    $"ADO build {buildId} sourceBranch '{sourceBranch}' does not match PR #{prNumber}; refusing to avoid posting to the wrong PR.");

// --- 5. Require the build to describe the PR's current revision ------------
// ADO builds GitHub's `refs/pull/<n>/merge`, so `sourceVersion` is the merge
// commit as of build time. Comparing it as well as the head catches a base
// branch that advanced while the PR head stayed put.
var buildPrSha = buildJson.At("triggerInfo", "pr.sourceSha").Text();
var buildMergeSha = buildJson.At("sourceVersion").Text();
var currentHead = prJson.At("head", "sha").Text();
var currentMerge = prJson.At("merge_commit_sha").Text();
EmitNoneIf(buildPrSha.Length == 0 || currentHead.Length == 0 || buildMergeSha.Length == 0 || currentMerge.Length == 0,
    "Could not resolve all build/current head and merge revisions; skipping to avoid analyzing a stale binlog.");
EmitNoneIf(buildPrSha != currentHead,
    $"Build {buildId} analyzed '{buildPrSha}' but PR #{prNumber} head is now '{currentHead}'; skipping stale build.");
EmitNoneIf(buildMergeSha != currentMerge,
    $"Build {buildId} merge revision '{buildMergeSha}' but PR #{prNumber} current merge is '{currentMerge}' (base advanced); skipping stale merge.");
var headSha = currentHead;
Console.WriteLine($"Analyzing build {buildId} at PR head revision '{headSha}'.");

// --- 6. Select the log artifacts of failed or canceled jobs ----------------
// Roslyn publishes "<job> Attempt <N> Logs" for most jobs, with explicit
// exceptions for Source Build and the bootstrap-correctness leg. Bases are
// matched exactly, and every retry attempt is kept.
var records = (await AdoGet($"timeline of build {buildId}", $"{adoApi}/build/builds/{buildId}/timeline?api-version=7.1"))
    .At("records").Items().ToList();
var failedJobs = records
    .Where(record => record.At("type").Text() == "Job" && record.At("result").Text() is "failed" or "canceled")
    .Select(record => (Name: record.At("name").Text(), Id: record.At("id").Text()))
    .Where(job => job.Name.Trim().Length != 0)
    .ToList();
EmitNoneIf(failedJobs.Count == 0, $"No failed or canceled jobs in the timeline for build {buildId}.");

// Only jobs that ran a publish-logs task can be expected to have an artifact.
// Orchestration legs such as `Monitor Helix Jobs` fail without producing one -
// that is how a Helix test failure surfaces - and demanding an artifact for
// those would skip most real failures rather than analyze them. Roslyn spells
// the task `Publish Logs`, and `Publish BuildLogs` in Source-Build.
var publishedLogs = records
    .Where(record => record.At("result").Text() == "succeeded")
    .Select(record => (Task: record.At("name").Text(), Parent: record.At("parentId").Text()))
    .Where(record => record.Task.StartsWith("Publish", StringComparison.Ordinal) && record.Task.EndsWith("Logs", StringComparison.Ordinal))
    .Select(record => record.Parent)
    .ToHashSet(StringComparer.OrdinalIgnoreCase);
var expectedJobNames = failedJobs
    .Where(job => publishedLogs.Contains(job.Id))
    .Select(job => job.Name)
    .Distinct(StringComparer.Ordinal)
    .ToList();

var attemptLogs = new Regex(@"^(.+) Attempt ([0-9]+) Logs$");
var sourceBuildLogs = new Regex(@"^BuildLogs_SourceBuild_Managed_Attempt[0-9]+$");
var allArtifacts = (await AdoGet($"artifact list of build {buildId}", $"{adoApi}/build/builds/{buildId}/artifacts?api-version=7.1"))
    .At("value").Items()
    .Select(artifact => (Node: artifact, Name: artifact.At("name").Text()))
    .Where(artifact => attemptLogs.IsMatch(artifact.Name) || sourceBuildLogs.IsMatch(artifact.Name))
    .ToList();

bool MatchesJob(string jobName, string artifactName) => jobName switch
{
    "Source-Build (Managed)" => sourceBuildLogs.IsMatch(artifactName),
    "Correctness_Bootstrap_Build_Default" => attemptLogs.Match(artifactName).Groups[1].Value == "Correctness_Bootstrap_Build - Default",
    _ => attemptLogs.Match(artifactName).Groups[1].Value == jobName,
};

// A failed job that published logs but whose artifact cannot be found is
// missing data, and it could be the one holding the root cause.
var uncovered = expectedJobNames
    .Where(jobName => !allArtifacts.Any(artifact => MatchesJob(jobName, artifact.Name)))
    .ToList();
EmitNoneIf(uncovered.Count != 0,
    $"Build {buildId} is missing the log artifact of {uncovered.Count} of {expectedJobNames.Count} failed jobs that published logs "
        + $"({string.Join(", ", uncovered.Take(3).Select(Extractor.Sanitize))}); skipping incomplete failed-job data.");

var selectedArtifacts = expectedJobNames
    .SelectMany(jobName => allArtifacts.Where(artifact => MatchesJob(jobName, artifact.Name)))
    .Where(artifact => artifact.Name.Trim().Length != 0)
    .DistinctBy(artifact => artifact.Name, StringComparer.Ordinal)
    .ToList();
EmitNoneIf(selectedArtifacts.Count == 0,
    $"No build-log artifacts matched the failed or canceled jobs in build {buildId}; the failure is likely outside a build leg.");
Console.WriteLine($"Selected {selectedArtifacts.Count} of {allArtifacts.Count} build-log artifacts for {expectedJobNames.Count} log-publishing jobs of {failedJobs.Count} failed or canceled.");

// --- 7. Download and extract each selected artifact ------------------------
// Roslyn's `Correctness_Analyzers` log artifact is routinely ~600 MB, so the
// per-artifact cap has to be well clear of that or the workflow silently skips
// exactly the correctness legs it exists to diagnose. Only one archive is on
// disk at a time, so the cumulative cap is what bounds the sum; it is charged
// before each transfer so the last artifact cannot start just under the limit
// and still pull a full MaxZipBytes.
const long MaxZipBytes = 2147483648;       // 2 GB compressed per artifact
const long MaxTotalBytes = 4294967296;     // 4 GB extracted across all artifacts
const long MaxTotalZipBytes = 3221225472;  // 3 GB compressed across all artifacts
var totalZipBytes = 0L;
// Per-transfer bounds only; the phase is bounded by the workflow's `timeout 600`.
var maxAttempt = TimeSpan.FromSeconds(120);
var maxRetryWindow = TimeSpan.FromSeconds(240);
var remainingBytes = MaxTotalBytes;

// A fixed /tmp name is a pre-created symlink, or a second job on the same
// runner, away from being someone else's file.
var zipTmp = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
// Only binlogs extracted by this run may be analyzed, so make the reset
// authoritative rather than best-effort.
if (Directory.Exists(binlogDir))
{
    Directory.Delete(binlogDir, recursive: true);
}

Directory.CreateDirectory(binlogDir);

var count = 0;
var stagedLegs = 0;
var ai = 0;
foreach (var (node, name) in selectedArtifacts)
{
    ai++;
    // `name` is PR-controlled artifact metadata, so log a sanitized copy only.
    var safeName = Extractor.Sanitize(name);
    var url = node.At("resource", "downloadUrl").Text();
    if (url.Length == 0)
    {
        Console.WriteLine($"::warning::Skipping {safeName}: no download URL.");
        continue;
    }

    // The build that produced this artifact ran PR code, so treat its metadata
    // as untrusted and require a URL this workflow is willing to fetch. ADO does
    // not serve artifacts from one host: across 612 artifacts in 33 real builds,
    // `Container` downloads came from dev.azure.com and `PipelineArtifact` ones
    // from a regional artprod*.artifacts.visualstudio.com, so match those
    // domains rather than a single origin. An unexpected host skips the
    // artifact, which fails closed on the run rather than fetching it.
    if (!IsTrustedArtifactUrl(url))
    {
        Console.WriteLine($"::warning::Skipping {safeName}: download URL is not an Azure DevOps artifact URL.");
        continue;
    }

    // Start empty, so a body retained by a previous artifact can never be
    // measured, charged or extracted twice.
    File.WriteAllBytes(zipTmp, []);

    // Bound this transfer by what is left of the cumulative budget as well as by
    // the per-artifact cap, so the caps compose by the smaller of the two rather
    // than granting every artifact the full per-artifact allowance.
    var zipCap = Math.Min(MaxZipBytes, MaxTotalZipBytes - totalZipBytes);
    if (zipCap <= 0)
    {
        Console.WriteLine($"::warning::Cumulative compressed download budget {MaxTotalZipBytes} is exhausted before {safeName}; stopping downloads.");
        break;
    }

    var (zipBytes, downloadError) = await Download(url, zipTmp, zipCap);
    // Charge the bytes retained on disk, including an artifact about to be
    // skipped. This is a disk and extraction budget, not a meter of egress.
    totalZipBytes += zipBytes;
    if (downloadError is not null || zipBytes == 0)
    {
        Console.WriteLine($"::warning::Skipping {safeName}: download failed, was truncated or was empty ({downloadError ?? "empty body"}).");
        continue;
    }

    int extracted;
    long written;
    try
    {
        (extracted, written) = Extractor.Extract(zipTmp, binlogDir, ai.ToString(), remainingBytes, safeName);
    }
    catch (Exception ex)
    {
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

    remainingBytes -= written;
    count += extracted;
    stagedLegs++;
    Console.WriteLine($"Extracted {extracted} binlog(s) ({written} bytes) from {safeName}.");
}

TryDelete(zipTmp);

Console.WriteLine($"Extracted {count} binlog(s) from {stagedLegs}/{selectedArtifacts.Count} selected artifacts into {binlogDir}:");
foreach (var staged in Directory.EnumerateFiles(binlogDir).Order(StringComparer.Ordinal))
{
    Console.WriteLine($"  {new FileInfo(staged).Length,12}  {Path.GetFileName(staged)}");
}

EmitNoneIf(count == 0, $"No *.binlog found in the selected build-log artifacts of build {buildId}.");
EmitNoneIf(stagedLegs != selectedArtifacts.Count,
    $"Only {stagedLegs} of {selectedArtifacts.Count} selected artifacts produced a usable binlog; skipping incomplete failed-job data.");

// --- 8. Re-check the revision after a download that can take minutes -------
// A force-push or base advance during the download would leave the binlogs
// stale relative to the diff that inline comments are pinned to.
var latestPr = await GitHubGet($"repos/{repo}/pulls/{prNumber}");
var latestHead = latestPr.At("head", "sha").Text();
var latestMerge = latestPr.At("merge_commit_sha").Text();
EmitNoneIf(latestHead != headSha,
    $"PR #{prNumber} head changed during download ('{headSha}' -> '{latestHead}') or could not be re-resolved; skipping.");
EmitNoneIf(latestMerge != buildMergeSha,
    $"PR #{prNumber} merge revision changed during download ('{buildMergeSha}' -> '{latestMerge}') or could not be re-resolved; skipping.");

TryAppendOutput(
    "binlog-found=true\n" +
    $"pr-number={prNumber}\n" +
    $"pr-head-sha={headSha}\n" +
    $"pr-merge-sha={buildMergeSha}\n" +
    $"ado-build-id={buildId}\n" +
    $"ado-build-url={Env("ADO_BUILD_UI")}?buildId={buildId}\n");
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

void EmitNone(string? reason = null)
{
    if (reason is not null)
    {
        Console.WriteLine($"::warning::{reason}");
    }

    TryAppendOutput("binlog-found=false\n");
    Environment.Exit(0);
}

void EmitNoneIf(bool condition, string reason)
{
    if (condition)
    {
        EmitNone(reason);
    }
}

static void TryDelete(string path)
{
    try
    {
        File.Delete(path);
    }
    catch (Exception)
    {
    }
}

void DeletePartials(int prefix)
{
    foreach (var partial in Directory.EnumerateFiles(binlogDir, $"{prefix}_*.binlog"))
    {
        TryDelete(partial);
    }
}

// A failure is reported as an absent document, so the caller reads empty fields
// and takes its own data-resolution branch. A zero window means one attempt.
async Task<JsonNode?> GitHubGet(string path)
{
    var (body, error) = await Fetch(github, $"https://api.github.com/{path}", TimeSpan.FromSeconds(60),
        TimeSpan.Zero, _ => TimeSpan.Zero, HttpCompletionOption.ResponseContentRead,
        (response, cancellation) => response.Content.ReadAsStringAsync(cancellation));
    return error is null ? Parse(body!) : null;
}

// A network failure or non-JSON body is a data-resolution failure, not evidence
// that there is nothing to analyze, so it stops the run rather than falling
// through to an empty `records`/`value` and a misleading "no failed jobs".
async Task<JsonNode?> AdoGet(string what, string url)
{
    var (body, error) = await Fetch(ado, url, TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(40),
        attempt => TimeSpan.FromSeconds(1 << attempt), HttpCompletionOption.ResponseContentRead,
        (response, cancellation) => response.Content.ReadAsStringAsync(cancellation));
    var document = error is null && body!.Length != 0 ? Parse(body!) : null;
    if (document is null)
    {
        EmitNone($"Could not fetch a usable {what} from Azure DevOps ({error ?? "empty or non-JSON body"}); treating as a data-resolution failure.");
    }

    return document;
}

static JsonNode? Parse(string body)
{
    try
    {
        return JsonNode.Parse(body);
    }
    catch (JsonException)
    {
        return null;
    }
}

// A 404 is an answer, and asking again just spends the window. A programming or
// disk error must not trigger another multi-gigabyte transfer either.
static bool IsTransient(HttpStatusCode status)
    => status is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests
        or HttpStatusCode.InternalServerError or HttpStatusCode.BadGateway
        or HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout;

// The leading dot is load-bearing: a bare suffix test would also accept a
// lookalike host such as `notvisualstudio.com`.
static bool IsTrustedArtifactUrl(string url)
    => Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && uri.Scheme == Uri.UriSchemeHttps
        && (uri.Host.Equals("dev.azure.com", StringComparison.OrdinalIgnoreCase)
            || uri.Host.EndsWith(".dev.azure.com", StringComparison.OrdinalIgnoreCase)
            || uri.Host.EndsWith(".visualstudio.com", StringComparison.OrdinalIgnoreCase));

// Runs `consume` against a successful response, retrying under a per-attempt
// timeout and an overall window. `consume` runs inside the attempt's
// cancellation scope, so the timeout covers reading the body too. Every attempt
// is clamped to what is left of the window, so one URL cannot outlast it.
// `window == TimeSpan.Zero` means "one attempt, no retries".
static async Task<(T? Value, string? Error)> Fetch<T>(
    HttpClient client, string url, TimeSpan perAttempt, TimeSpan window, Func<int, TimeSpan> backoff,
    HttpCompletionOption completion, Func<HttpResponseMessage, CancellationToken, Task<T>> consume)
{
    var deadline = DateTime.UtcNow + window;
    var error = "no attempt was made";
    for (var attempt = 0; attempt <= 3; attempt++)
    {
        if (attempt != 0)
        {
            var wait = backoff(attempt - 1);
            if (DateTime.UtcNow + wait >= deadline)
            {
                break;
            }

            await Task.Delay(wait);
        }

        // Clamp the attempt to what is left of the window, so neither a slow
        // first transfer nor a retry can overrun it. A zero window means "one
        // attempt, no retries" and does not bound that attempt.
        var timeout = perAttempt;
        if (window != TimeSpan.Zero)
        {
            var remaining = deadline - DateTime.UtcNow;
            if (remaining <= TimeSpan.Zero)
            {
                break;
            }

            timeout = remaining < perAttempt ? remaining : perAttempt;
        }

        try
        {
            using var cts = new CancellationTokenSource(timeout);
            using var response = await client.GetAsync(url, completion, cts.Token);
            if (response.IsSuccessStatusCode)
            {
                return (await consume(response, cts.Token), null);
            }

            error = $"HTTP {(int)response.StatusCode}";
            if (!IsTransient(response.StatusCode))
            {
                return (default, error);
            }
        }
        catch (Exception ex) when (ex is OperationCanceledException or HttpRequestException or IOException)
        {
            error = ex is OperationCanceledException ? "timed out" : ex.GetType().Name;
        }
    }

    return (default, error);
}

// Streams the artifact to disk, stopping at `cap` bytes actually written rather
// than at any length the response declares, so a missing or lying Content-Length
// cannot fill the disk. Reaching the cap is an error because the caller skips
// any artifact that large. Returns the bytes retained on disk either way.
async Task<(long Bytes, string? Error)> Download(string url, string path, long cap)
{
    var capped = false;
    var (bytes, error) = await Fetch(ado, url, maxAttempt, maxRetryWindow, _ => TimeSpan.FromSeconds(2),
        HttpCompletionOption.ResponseHeadersRead,
        async (response, cancellation) =>
        {
            using var source = await response.Content.ReadAsStreamAsync(cancellation);
            // Truncate per attempt so a partial body is never prepended to a retry.
            using var output = new FileStream(path, FileMode.Create, FileAccess.Write);
            var buffer = new byte[1024 * 1024];
            var written = 0L;
            int read;
            while ((read = await source.ReadAsync(buffer, cancellation)) > 0)
            {
                if (written + read >= cap)
                {
                    output.Write(buffer, 0, (int)(cap - written));
                    capped = true;
                    return cap;
                }

                output.Write(buffer, 0, read);
                written += read;
            }

            return written;
        });

    if (error is not null)
    {
        return (RetainedBytes(path), error);
    }

    return capped ? (bytes, $"reached the {cap}-byte transfer cap") : (bytes, null);
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

// Not reachable from the workflow; a manual seam for running archive handling
// against a local zip while iterating on it.
static int RunExtractOnly(string[] extractArgs)
{
    if (extractArgs.Length is < 4 or > 5 || !long.TryParse(extractArgs[3], out var budgetBytes))
    {
        Console.Error.WriteLine("usage: fetch-build-binlogs.cs --extract <archive> <dest> <prefix> <budget> [label]");
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

// Extracts the *.binlog entries of one Azure DevOps build-log artifact.
//
// The archive comes from a PR-triggered build, so its entry paths, metadata and
// contents are untrusted. Two properties keep extraction safe: destination names
// are generated here rather than taken from the archive, so a traversal or
// absolute path cannot choose where bytes land; and writing stops as soon as the
// remaining budget is exceeded, so a zip bomb cannot fill the runner disk.
// Paths and types are validated up front because an archive containing a
// traversal path or a link entry is hostile rather than merely odd, so the whole
// artifact is rejected instead of partially extracted.
static class Extractor
{
    public static (int Count, long Written) Extract(
        string archivePath, string destination, string prefix, long budgetBytes, string label)
    {
        // Re-sanitize rather than trusting the caller.
        var safeLabel = Sanitize(label);

        using var zip = ZipFile.OpenRead(archivePath);

        for (var i = 0; i < zip.Entries.Count; i++)
        {
            if (IsUnsafePath(zip.Entries[i].FullName) || IsUnsupportedType(zip.Entries[i]))
            {
                throw new InvalidDataException($"archive entry {i} has an unsafe path or an unsupported type");
            }
        }

        var selected = zip.Entries
            .Where(entry => !IsDirectoryEntry(entry) && entry.FullName.EndsWith(".binlog", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Directory.CreateDirectory(destination);

        var written = 0L;
        var buffer = new byte[1024 * 1024];

        for (var index = 0; index < selected.Length; index++)
        {
            var stem = safeLabel.Length == 0 ? $"{prefix}_{index}" : $"{prefix}_{index}_{safeLabel}";

            using var source = selected[index].Open();
            // CreateNew, so a name that somehow already exists is an error rather
            // than a silent overwrite of a previous artifact's binlog.
            using var output = new FileStream(Path.Combine(destination, $"{stem}.binlog"), FileMode.CreateNew, FileAccess.Write);

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

    // Maps anything PR-controlled to a conservative set before it reaches a file
    // name or a log line. Trimming before truncating keeps the 80-char bound.
    public static string Sanitize(string value)
    {
        var result = Regex.Replace(value, "[^A-Za-z0-9._-]", "_").Trim('.', '_', '-');
        return result.Length > 80 ? result[..80] : result;
    }

    private static bool IsUnsafePath(string name)
    {
        var parts = name.Replace('\\', '/').Split('/');
        var first = parts[0];
        return name.Contains('\0')
            || name.StartsWith('/') || name.StartsWith('\\')
            || Array.IndexOf(parts, "..") >= 0
            // "c:/foo" is not rooted by POSIX rules but is still an escape attempt.
            || (first.Length >= 2 && char.IsAsciiLetter(first[0]) && first[1] == ':');
    }

    private static bool IsUnsupportedType(ZipArchiveEntry entry)
    {
        // S_IFMT type bits, then regular-file and directory only. Entries written
        // on Windows carry no Unix mode; 0 is unspecified, not evidence of a
        // hostile type.
        var fileType = (entry.ExternalAttributes >> 16) & 0xF000;
        return fileType is not (0 or 0x8000 or 0x4000);
    }

    private static bool IsDirectoryEntry(ZipArchiveEntry entry)
        => entry.FullName.EndsWith('/') || entry.Name.Length == 0;
}

// Third-party JSON whose shape is not guaranteed, so reads are total: a missing
// property, a null, or the wrong kind all read as absent rather than throwing
// partway through a validation sequence.
static class Json
{
    public static JsonNode? At(this JsonNode? node, params string[] path)
    {
        foreach (var name in path)
        {
            node = (node as JsonObject)?[name];
        }

        return node;
    }

    public static string Text(this JsonNode? node) => (node as JsonValue)?.ToString() ?? string.Empty;

    public static IEnumerable<JsonNode?> Items(this JsonNode? node) => node as JsonArray ?? [];
}
