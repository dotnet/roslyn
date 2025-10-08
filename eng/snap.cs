#!/usr/bin/env dotnet
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Need this fix to delete the static graph disable: https://github.com/dotnet/sdk/pull/50532, 10.0.100-rc.2
#:property RestoreUseStaticGraphEvaluation=false
#:property PublishAot=false
// warning CS8002: Referenced assembly 'Microsoft.DotNet.DarcLib' does not have a strong name.
#:property NoWarn=$(NoWarn);CS8002
#:package CliWrap
#:package Microsoft.DotNet.DarcLib
#:package Spectre.Console

#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Xml;
using CliWrap;
using CliWrap.Buffered;
using Microsoft.DotNet.DarcLib;
using Microsoft.DotNet.ProductConstructionService.Client.Models;
using Spectre.Console;
using Spectre.Console.Rendering;

const string nextMilestoneName = "Next";

var console = AnsiConsole.Console;

// Setup audit logging.
var logger = new Logger();
console.MarkupLineInterpolated($"Logging to [grey]{logger.LogFilePath}[/]");
logger.Log("Starting snap script run");

AppDomain.CurrentDomain.UnhandledException += (s, e) =>
{
    logger.Log($"Unhandled exception: {e.ExceptionObject}");
};

TaskScheduler.UnobservedTaskException += (s, e) =>
{
    logger.Log($"Unobserved task exception: {e.Exception}");
};

console.Pipeline.Attach(new LoggingRenderHook(logger));

// Parse args.
const string dryRunLong = "--dry-run";
const string dryRunShort = "-n";
bool dryRun = args is [dryRunLong or dryRunShort];
if (!dryRun && args is not [])
{
    console.MarkupLineInterpolated($"[red]Error:[/] This script does not take any arguments except [teal]{dryRunLong}[/]/[teal]{dryRunShort}[/], found: [grey]{string.Join(' ', args)}[/]");
    return 1;
}

// Welcome message.
console.MarkupLineInterpolated($"Welcome to [grey]{getFileName()}[/], an interactive script to help with snap-related infra tasks");
static string getFileName([CallerFilePath] string filePath = "unknownFile") => Path.GetFileName(filePath);

console.MarkupLine(dryRun
    ? "[yellow]Note:[/] Running in [green]dry run[/] mode, no changes will be made"
    : "[yellow]Note:[/] Running in [red]live[/] mode, changes will be made at the end (after confirmation)");

console.WriteLine();
console.MarkupLine("[purple]Configuration[/]");
console.WriteLine();

var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("dotnet-roslyn-snap-script");

// Ask for source repo.

var defaultRepo = (await Cli.Wrap("gh")
    .WithArguments(["repo", "set-default", "--view"])
    .ExecuteBufferedAsync())
    .StandardOutput
    .Trim();

var sourceRepoShort = console.Prompt(TextPrompt<string>.Create("Source repo in the format owner/repo",
    defaultValueIfNotNullOrEmpty: defaultRepo)
    .Validate(static repo =>
    {
        if (repo.Count(c => c == '/') != 1)
        {
            return ValidationResult.Error("Repo must be in the format owner/repo");
        }

        return ValidationResult.Success();
    }));

var sourceRepoUrl = $"https://github.com/{sourceRepoShort}";

// Ask for source and target branches.

var sourceBranchName = console.Prompt(TextPrompt<string>.Create("Source branch",
    defaultValue: "main"));

// Find Roslyn version number.
var sourceVersionsProps = await VersionsProps.LoadAsync(httpClient, sourceRepoShort, sourceBranchName);
console.MarkupLineInterpolated($"Branch [teal]{sourceBranchName}[/] has version [teal]{sourceVersionsProps?.ToString() ?? "N/A"}[/]");

string? suggestedTargetBranchName = null;

// From the version number, infer the VS version it inserts to.
if (sourceVersionsProps != null)
{
    // Roslyn 4.x corresponds to VS 17.x and so on.
    var vsMajorVersion = sourceVersionsProps.MajorVersion + 13;
    suggestedTargetBranchName = $"release/dev{vsMajorVersion}.{sourceVersionsProps.MinorVersion}";
}

// Find the latest branch starting with release/.
suggestedTargetBranchName ??= (await Cli.Wrap("git")
    .WithArguments(["for-each-ref", "--sort=-committerdate", "--format=%(refname:short)", "refs/remotes/*/release/*", "--count=1"])
    .ExecuteBufferedAsync())
    .StandardOutput
    .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
    .Select(static s => s.IndexOf('/') is var slashIndex and >= 0 ? s[(slashIndex + 1)..] : s)
    .FirstOrDefault();

var targetBranchName = console.Prompt(TextPrompt<string>.Create("Target branch",
    defaultValue: suggestedTargetBranchName ?? "release/insiders"));

// Find Roslyn version number.
var targetVersionsProps = await VersionsProps.LoadAsync(httpClient, sourceRepoShort, targetBranchName);
console.MarkupLineInterpolated($"Branch [teal]{targetBranchName}[/] has version [teal]{targetVersionsProps?.ToString() ?? "N/A"}[/]");

// Find which VS the branches insert to.
var sourcePublishDataTask = PublishData.LoadAsync(httpClient, sourceRepoShort, sourceBranchName);
var targetPublishDataTask = PublishData.LoadAsync(httpClient, sourceRepoShort, targetBranchName);
var sourcePublishData = await sourcePublishDataTask;
console.MarkupLineInterpolated($"Branch [teal]{sourceBranchName}[/] inserts to VS [teal]{sourcePublishData?.BranchInfo.Summarize() ?? "N/A"}[/] with prefix [teal]{sourcePublishData?.BranchInfo.InsertionTitlePrefix ?? "N/A"}[/]");
var targetPublishData = await targetPublishDataTask;
console.MarkupLineInterpolated($"Branch [teal]{targetBranchName}[/] inserts to VS [teal]{targetPublishData?.BranchInfo.Summarize() ?? "N/A"}[/] with prefix [teal]{targetPublishData?.BranchInfo.InsertionTitlePrefix ?? "N/A"}[/]");

// Initialize action collection.
var actions = new List<(string Name, Func<Task> Func)>();
void addAction(string title, Func<Task> func)
{
    if (console.Confirm($"[green]Add to plan:[/] {title}?", defaultValue: true))
    {
        var funcWithProgress = () =>
        {
            console.MarkupLine($"[yellow]Started:[/] {title}...");
            return func();
        };
        actions.Add((title, funcWithProgress));
    }
}

// Where should branches insert after the snap?

var inferredVsVersion = VsVersion.TryParse(targetBranchName);
var suggestedSourceVsVersionAfterSnap =
    // When the target branch does not exist, that likely means a snap to a new minor version.
    targetPublishData is null ? inferredVsVersion?.Increase() : inferredVsVersion;
var suggestedTargetVsVersionAfterSnap = inferredVsVersion;

var sourceVsBranchAfterSnap = console.Prompt(TextPrompt<string>.Create($"After snap, [teal]{sourceBranchName}[/] should insert to",
    defaultValueIfNotNullOrEmpty: suggestedSourceVsVersionAfterSnap?.AsVsBranchName()));
var sourceVsAsDraftAfterSnap = console.Confirm($"Should insertion PRs be in draft mode for [teal]{sourceBranchName}[/]?", defaultValue: false);
var sourceVsPrefixAfterSnap = console.Prompt(TextPrompt<string>.Create($"What prefix should insertion PR titles have for [teal]{sourceBranchName}[/]?",
    defaultValueIfNotNullOrEmpty: suggestedSourceVsVersionAfterSnap?.AsVsInsertionTitlePrefix() ?? sourcePublishData?.BranchInfo.InsertionTitlePrefix));
var targetVsBranchAfterSnap = console.Prompt(TextPrompt<string>.Create($"After snap, [teal]{targetBranchName}[/] should insert to",
    defaultValueIfNotNullOrEmpty: suggestedTargetVsVersionAfterSnap?.AsVsBranchName()));
var targetVsAsDraftAfterSnap = console.Confirm($"Should insertion PRs be in draft mode for [teal]{targetBranchName}[/]?", defaultValue: false);
var targetVsPrefixAfterSnap = console.Prompt(TextPrompt<string>.Create($"What prefix should insertion PR titles have for [teal]{targetBranchName}[/]?",
    defaultValueIfNotNullOrEmpty: suggestedTargetVsVersionAfterSnap?.AsVsInsertionTitlePrefix() ?? targetPublishData?.BranchInfo.InsertionTitlePrefix));

// Check subscriptions.
console.WriteLine();
console.MarkupLine("[purple]Subscriptions[/]");
console.WriteLine();

var darc = new DarcHelper(console);
var vmrRepoUrl = "https://github.com/dotnet/dotnet";
var printers = await Task.WhenAll([
    darc.ListSubscriptionsAsync(sourceRepoUrl, vmrRepoUrl, "VMR"),
    darc.ListBackflowsAsync(sourceRepoUrl),
    darc.ListSubscriptionsAsync(sourceRepoUrl, "https://github.com/dotnet/sdk", "SDK"),
    darc.ListSubscriptionsAsync(sourceRepoUrl, "https://github.com/dotnet/runtime", "runtime"),
]);
foreach (var printer in printers)
{
    printer();
}

// Determine subscription changes.

// Source -> VMR
var existingSourceBranchFlow = darc.FoundFlows.FirstOrDefault(flow =>
    flow.SourceRepoUrl == sourceRepoUrl &&
    flow.SourceBranch == sourceBranchName &&
    flow.TargetRepoUrl == vmrRepoUrl);
var sourceChannelAfterSnap = console.Prompt(TextPrompt<string>.Create($"After snap, [teal]{sourceBranchName}[/] should publish to darc channel",
    defaultValueIfNotNullOrEmpty: suggestedSourceVsVersionAfterSnap?.AsDarcChannelName()));
var sourceVmrBranchAfterSnap = console.Prompt(TextPrompt<string>.Create("And flow to VMR branch",
    defaultValueIfNotNullOrEmpty: existingSourceBranchFlow?.TargetBranch));
suggestSubscriptionChange(existingFlow: existingSourceBranchFlow, expectedFlow: new Flow(
    SourceRepoUrl: sourceRepoUrl,
    SourceBranch: sourceBranchName,
    Channel: sourceChannelAfterSnap,
    TargetRepoUrl: vmrRepoUrl,
    TargetBranch: sourceVmrBranchAfterSnap));

// VMR -> Source
var existingSourceBranchBackFlow = darc.FoundFlows.FirstOrDefault(flow =>
    flow.SourceRepoUrl == vmrRepoUrl &&
    flow.TargetRepoUrl == sourceRepoUrl &&
    flow.SourceBranch == sourceVmrBranchAfterSnap);
var suggestedSourceBranchBackFlowChannel = existingSourceBranchBackFlow?.Channel
    ?? await darc.TryGetDefaultChannelAsync(vmrRepoUrl, sourceVmrBranchAfterSnap)
    ?? console.Prompt(new TextPrompt<string>($"After snap, [teal]{sourceBranchName}[/] should flow back from VMR's [teal]{sourceVmrBranchAfterSnap}[/] via darc channel:"));
suggestSubscriptionChange(existingFlow: existingSourceBranchBackFlow, expectedFlow: new Flow(
    SourceRepoUrl: vmrRepoUrl,
    SourceBranch: sourceVmrBranchAfterSnap,
    Channel: suggestedSourceBranchBackFlowChannel,
    TargetRepoUrl: sourceRepoUrl,
    TargetBranch: sourceBranchName));

// Target -> VMR
var existingTargetBranchFlow = darc.FoundFlows.FirstOrDefault(flow =>
    flow.SourceRepoUrl == sourceRepoUrl &&
    flow.SourceBranch == targetBranchName &&
    flow.TargetRepoUrl == vmrRepoUrl) ?? darc.FoundFlows.FirstOrDefault(flow =>
    flow.SourceRepoUrl == sourceRepoUrl &&
    flow.TargetRepoUrl == vmrRepoUrl);
var targetChannelAfterSnap = console.Prompt(TextPrompt<string>.Create($"After snap, [teal]{targetBranchName}[/] should publish to darc channel",
    defaultValueIfNotNullOrEmpty: suggestedTargetVsVersionAfterSnap?.AsDarcChannelName()));
var targetVmrBranchAfterSnap = console.Prompt(TextPrompt<string>.Create("And flow to VMR branch",
    defaultValueIfNotNullOrEmpty: existingTargetBranchFlow?.TargetBranch));
suggestSubscriptionChange(existingFlow: existingTargetBranchFlow, expectedFlow: new Flow(
    SourceRepoUrl: sourceRepoUrl,
    SourceBranch: targetBranchName,
    Channel: targetChannelAfterSnap,
    TargetRepoUrl: vmrRepoUrl,
    TargetBranch: targetVmrBranchAfterSnap));

// VMR -> Target
var existingTargetBranchBackFlow = darc.FoundFlows.FirstOrDefault(flow =>
    flow.SourceRepoUrl == vmrRepoUrl &&
    flow.TargetRepoUrl == sourceRepoUrl &&
    flow.SourceBranch == targetVmrBranchAfterSnap);
var suggestedTargetBranchBackFlowChannel = existingTargetBranchBackFlow?.Channel
    ?? await darc.TryGetDefaultChannelAsync(vmrRepoUrl, targetVmrBranchAfterSnap)
    ?? console.Prompt(new TextPrompt<string>($"After snap, [teal]{targetBranchName}[/] should flow back from VMR's [teal]{targetVmrBranchAfterSnap}[/] via darc channel:"));
suggestSubscriptionChange(existingFlow: existingTargetBranchBackFlow, expectedFlow: new Flow(
    SourceRepoUrl: vmrRepoUrl,
    SourceBranch: targetVmrBranchAfterSnap,
    Channel: suggestedTargetBranchBackFlowChannel,
    TargetRepoUrl: sourceRepoUrl,
    TargetBranch: targetBranchName));

void suggestSubscriptionChange(Flow? existingFlow, Flow expectedFlow)
{
    if (darc.FoundFlows.Contains(expectedFlow))
    {
        console.MarkupLineInterpolated($"[green]Already exists:[/] Flow {expectedFlow.ToFullString()}");
    }
    else
    {
        if (existingFlow != null &&
            console.Confirm($"[green]Add to plan:[/] Update flow {existingFlow.ToFullString()} {Flow.DescribeChanges(existingFlow, expectedFlow)}?", defaultValue: true))
        {
            // TODO: Add to plan.
        }
        else
        {
            // TODO: Add to plan.
            console.Confirm($"[green]Add to plan:[/] Add flow {expectedFlow.ToFullString()}?", defaultValue: true);
        }
    }
}

// Find last 5 PRs merged to current branch.

console.WriteLine();
console.MarkupLine("[purple]Pull Requests[/] (point of snap)");
console.WriteLine();

const string prJsonFields = "number,title,mergedAt,mergeCommit";

var lastMergedSearchFilter = $"is:merged base:{sourceBranchName}";
var lastMergedPullRequests = (await Cli.Wrap("gh")
    .WithArguments(["pr", "list",
        "--repo", sourceRepoShort,
        "--search", lastMergedSearchFilter,
        "--json", prJsonFields,
        "--limit", "5"])
    .ExecuteBufferedAsync())
    .StandardOutput
    .ParseJsonList<PullRequest>()
    ?.OrderByDescending(static pr => pr.MergedAt)
    .ToArray()
    ?? throw new InvalidOperationException("Null PR list");

console.MarkupLineInterpolated($"Last PRs merged to [teal]{sourceBranchName}[/] ([teal]{lastMergedPullRequests.Length}[/]):");
foreach (var pr in lastMergedPullRequests)
{
    console.WriteLine($" - {pr}");
}
console.MarkupLineInterpolated($" - ... for more, run [grey]gh pr list --repo {sourceRepoShort} --search '{lastMergedSearchFilter}'[/]");

// Find PRs in milestone Next.

var milestoneSearchFilter = $"is:merged milestone:{nextMilestoneName} base:{sourceBranchName}";
var milestonePullRequests = (await Cli.Wrap("gh")
    .WithArguments(["pr", "list",
        "--repo", sourceRepoShort,
        "--search", milestoneSearchFilter,
        "--json", prJsonFields])
    .ExecuteBufferedAsync())
    .StandardOutput
    .ParseJsonList<PullRequest>()
    ?.OrderByDescending(static pr => pr.MergedAt)
    .ToArray()
    ?? throw new InvalidOperationException($"Null PR list in milestone {nextMilestoneName}");

console.MarkupLineInterpolated($"Found PRs in milestone {nextMilestoneName} ([teal]{milestonePullRequests.Length}[/])");
foreach (var pr in milestonePullRequests.Take(5))
{
    console.WriteLine($" - {pr}");
}
if (milestonePullRequests.Length > 6)
{
    console.MarkupLineInterpolated($" - ... for more, run [grey]gh pr list --repo {sourceRepoShort} --search '{milestoneSearchFilter}'[/]");
}
if (milestonePullRequests.Length > 5)
{
    console.WriteLine($" - {milestonePullRequests[^1]}");
}
console.WriteLine();

// Determine the last PR to include.

var lastPrNumber = console.Prompt(TextPrompt<int>.Create($"Number of the last PR to include in [teal]{targetBranchName}[/]",
    defaultValueIfNotNull: lastMergedPullRequests is [var defaultLastPr, ..] ? defaultLastPr.Number : null));
var lastPr = lastMergedPullRequests.FirstOrDefault(pr => pr.Number == lastPrNumber)
    ?? (await Cli.Wrap("gh")
    .WithArguments(["pr", "view", $"{lastPrNumber}",
        "--repo", sourceRepoShort,
        "--json", prJsonFields])
    .ExecuteBufferedAsync())
    .StandardOutput
    .ParseJsonList<PullRequest>()
    ?.FirstOrDefault()
    ?? throw new InvalidOperationException($"Cannot find PR #{lastPrNumber}");

var lastPrCommitDetails = (await Cli.Wrap("gh")
    .WithArguments(["api", $"repos/{sourceRepoShort}/commits/{lastPr.MergeCommit.Oid}"])
    .ExecuteBufferedAsync())
    .StandardOutput
    .ParseJson<CommitDetails>()
    ?? throw new InvalidOperationException($"Null commit details for {lastPr.MergeCommit.Oid}");

if (lastPr.MergeCommit.Oid != lastPrCommitDetails.Sha)
{
    console.MarkupLineInterpolated($"[red]Unexpected:[/] Commit ID mismatch: PR says {lastPr.MergeCommit.Oid} but API returned {lastPrCommitDetails.Sha}");
}

console.MarkupLineInterpolated($"Last included commit will be [teal]{lastPrCommitDetails.Sha}[/]: {lastPrCommitDetails.Commit.Message.GetFirstLine()}");

// Determine PRs to move between milestones.

if (milestonePullRequests is [var defaultLastMilestonePr, ..])
{
    var lastMilestonePr = milestonePullRequests.FirstOrDefault(pr => pr.Number == lastPr.Number);
    if (lastMilestonePr is null)
    {
        var lastMilestonePrNumber = console.Prompt(TextPrompt<int>.Create($"Number of last PR to include from milestone {nextMilestoneName}",
            defaultValue: defaultLastMilestonePr.Number)
            .Validate(prNumber => milestonePullRequests.Any(pr => pr.Number == prNumber)
                ? ValidationResult.Success()
                : ValidationResult.Error($"No PR with number {prNumber} found in milestone {nextMilestoneName}")));
        lastMilestonePr = milestonePullRequests.First(pr => pr.Number == lastPrNumber);
    }
    var lastMilestonePrIndex = milestonePullRequests.IndexOf(lastMilestonePr);
    Debug.Assert(lastMilestonePrIndex >= 0);

    // Find all milestones.
    var milestones = (await Cli.Wrap("gh")
        .WithArguments(["api", $"repos/{sourceRepoShort}/milestones", "--paginate",
            "--jq", ".[] | {number:.number,title:.title}"])
        .ExecuteBufferedAsync())
        .StandardOutput
        .ParseJsonNewLineDelimitedList<Milestone>()
        .AssertNonNullElements("Null milestone in list")
        .OrderByDescending(static m => m.Number)
        .ToArray();

    // Determine target milestone.
    var suggestedTargetVsVersion = VsVersion.TryParse(targetVsBranchAfterSnap) ?? suggestedTargetVsVersionAfterSnap;
    var targetMilestone = console.Prompt(TextPrompt<string>.Create("Target milestone",
        defaultValueIfNotNullOrEmpty: suggestedTargetVsVersion != null
            ? $"VS {suggestedTargetVsVersion.Major}.{suggestedTargetVsVersion.Minor}"
            : milestones.FirstOrDefault()?.Title));

    var selectedMilestone = milestones.FirstOrDefault(m => m.Title == targetMilestone);
    if (selectedMilestone is null)
    {
        console.MarkupLineInterpolated($"[green]Note:[/] Milestone [teal]{targetMilestone}[/] does not exist yet (will be created when needed)");
    }

    // Move PRs to target milestone.
    var prsToChangeMilestonesOf = milestonePullRequests.AsSpan(lastMilestonePrIndex..).ToArray();
    addAction($"Move [teal]{prsToChangeMilestonesOf.Length}[/] PRs from milestone [teal]{nextMilestoneName}[/] to [teal]{targetMilestone}[/]", async () =>
    {
        if (selectedMilestone is null)
        {
            console.MarkupLineInterpolated($"Creating milestone [teal]{targetMilestone}[/]");
            var createMilestoneResult = await Cli.Wrap("gh")
                .WithArguments(["api", "-X", "POST", $"repos/{sourceRepoShort}/milestones",
                    "--field", $"title={targetMilestone}"])
                .ExecuteBufferedAsync(logger);
        }

        await console.ProgressLine().StartAsync(async progress =>
        {
            var task = progress.AddTask("Processing PRs", maxValue: prsToChangeMilestonesOf.Length);
            foreach (var pr in prsToChangeMilestonesOf)
            {
                await Cli.Wrap("gh")
                    .WithArguments(["pr", "edit", $"{pr.Number}",
                        "--repo", sourceRepoShort,
                        "--milestone", targetMilestone])
                    .ExecuteBufferedAsync(logger);
                task.Increment(1);
            }
            task.StopTask();
        });
    });
}

// Merge between branches.
addAction($"Merge changes from [teal]{sourceBranchName}[/] to [teal]{targetBranchName}[/] up to and including PR [teal]#{lastPr.Number}[/]", async () =>
{
    var prTitle = $"Snap {sourceBranchName} into {targetBranchName}";

    // Checkout the repo.
    var repoCheckoutDir = Path.Join(Path.GetTempPath(), "snap-script", $"{sourceRepoShort.Split('/').Last()}");
    if (!Directory.Exists(repoCheckoutDir))
    {
        console.WriteLine($"Checking out repository '{sourceRepoShort}' to '{repoCheckoutDir}'");
        Directory.CreateDirectory(repoCheckoutDir);
        await Cli.Wrap("git")
            .WithArguments(["clone", sourceRepoUrl, repoCheckoutDir])
            .ExecuteBufferedAsync(logger);
    }
    else
    {
        console.WriteLine($"Using existing checkout of repository '{sourceRepoShort}' at '{repoCheckoutDir}'");
        await Cli.Wrap("git")
            .WithWorkingDirectory(repoCheckoutDir)
            .WithArguments(["remote", "set-url", "origin", sourceRepoUrl])
            .ExecuteBufferedAsync(logger);
    }

    // Fetch the commit.
    await Cli.Wrap("git")
        .WithWorkingDirectory(repoCheckoutDir)
        .WithArguments(["fetch", "origin", lastPrCommitDetails.Sha])
        .ExecuteBufferedAsync(logger);

    // Check whether the target branch exists.
    var targetBranchExists = (await Cli.Wrap("gh")
        .WithArguments(["api", $"repos/{sourceRepoShort}/branches/{targetBranchName}"])
        .WithValidation(CommandResultValidation.None)
        .ExecuteBufferedAsync(logger)).IsSuccess;

    if (!targetBranchExists)
    {
        // Target branch does not exist, checkout the commit and just push to the new branch.
        console.MarkupLineInterpolated($"Target branch [teal]{targetBranchName}[/] does not exist, will be created.");
        await Cli.Wrap("git")
            .WithWorkingDirectory(repoCheckoutDir)
            .WithArguments(["checkout", lastPrCommitDetails.Sha, "-b", targetBranchName])
            .ExecuteBufferedAsync(logger);
        await Cli.Wrap("git")
            .WithWorkingDirectory(repoCheckoutDir)
            .WithArguments(["push", "origin", "HEAD"])
            .ExecuteBufferedAsync(logger);
        console.MarkupLineInterpolated($"Pushed new branch [teal]{targetBranchName}[/] to [teal]{sourceRepoShort}[/].");
    }
    else
    {
        // Target branch exists, merge changes up to the commit.
        var snapBranchName = $"snap-{sourceBranchName.Replace('/', '-')}-to-{targetBranchName.Replace('/', '-')}-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}";
        console.WriteLine("Merging branches.");
        await Cli.Wrap("git")
            .WithWorkingDirectory(repoCheckoutDir)
            .WithArguments(["fetch", "origin", targetBranchName])
            .ExecuteBufferedAsync(logger);
        await Cli.Wrap("git")
            .WithWorkingDirectory(repoCheckoutDir)
            .WithArguments(["checkout", "FETCH_HEAD", "-b", snapBranchName])
            .ExecuteBufferedAsync(logger);
        await Cli.Wrap("git")
            .WithWorkingDirectory(repoCheckoutDir)
            .WithArguments(["merge", "--no-ff", "--no-commit", lastPrCommitDetails.Sha])
            .ExecuteBufferedAsync(logger);
        await Cli.Wrap("git")
            .WithWorkingDirectory(repoCheckoutDir)
            .WithArguments(["commit", "-m", prTitle])
            .ExecuteBufferedAsync(logger);

        // Push the changes.
        await Cli.Wrap("git")
            .WithWorkingDirectory(repoCheckoutDir)
            .WithArguments(["push", "origin", "HEAD"])
            .ExecuteBufferedAsync(logger);

        // Open the PR.
        console.WriteLine("Opening snap PR.");
        var createPrResult = await Cli.Wrap("gh")
            .WithWorkingDirectory(repoCheckoutDir)
            .WithArguments([
                "pr", "create",
                "--title", prTitle,
                "--body", $"Snap {sourceBranchName} into {targetBranchName}\n\nAuto-generated by snap script.",
                "--head", snapBranchName,
                "--base", targetBranchName,
                "--repo", sourceRepoShort])
            .ExecuteBufferedAsync(logger);
        console.WriteLine(createPrResult.StandardOutput.Trim());
    }
});

// Perform actions.
console.WriteLine();
console.MarkupLine("[purple]Modifications[/]");
console.WriteLine();
if (dryRun)
{
    console.MarkupLine("[yellow]Dry run[/]: No changes made");
}
else if (actions.Count == 0)
{
    console.MarkupLine("[yellow]Aborted[/]: No actions added to plan");
}
else
{
    var actionList = string.Join("\n", actions.Select(static a => $"- {a.Name}"));
    if (console.Confirm($"[red]Perform actions added to plan?[/]\n{actionList}\nContinue?", defaultValue: true))
    {
        foreach (var action in actions)
        {
            await action.Func();
        }

        console.MarkupLine("[green]Done[/]");
    }
    else
    {
        console.MarkupLine("[yellow]Aborted[/]: No changes made");
    }
}

return 0;

file sealed record PullRequest(int Number, string Title, DateTimeOffset MergedAt, Commit MergeCommit)
{
    public override string ToString() => $"#{Number}: {Title} ({MergedAt})";
}

file sealed record Commit(string Oid)
{
    public override string ToString() => Oid;
}

file sealed record CommitDetails(string Sha, CommitDetailsCommit Commit);

file sealed record CommitDetailsCommit(string Message);

file sealed record Milestone(int Number, string Title)
{
    public override string ToString() => Title;
}

file sealed record PublishData(
    [property: JsonRequired] BranchInfo BranchInfo)
{
    /// <returns>
    /// <see langword="null"/> if the repo or branch does not exist.
    /// </returns>
    public static async Task<PublishData?> LoadAsync(HttpClient httpClient, string repoOwnerAndName, string branchName)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<PublishData>($"https://raw.githubusercontent.com/{repoOwnerAndName}/{branchName}/eng/config/PublishData.json")
                ?? throw new InvalidOperationException("Null PublishData.json");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Cannot load PublishData.json from '{repoOwnerAndName}' branch '{branchName}'", ex);
        }
    }
}

file sealed record BranchInfo(
    string VsBranch,
    bool InsertionCreateDraftPR,
    string InsertionTitlePrefix)
{
    public string Summarize() => VsBranch + (InsertionCreateDraftPR ? " (as draft)" : null);
}

file sealed record VersionsProps(
    int MajorVersion,
    int MinorVersion,
    int PatchVersion,
    string PreReleaseVersionLabel)
{
    /// <returns>
    /// <see langword="null"/> if the repo or branch does not exist.
    /// </returns>
    public static async Task<VersionsProps?> LoadAsync(HttpClient httpClient, string repoOwnerAndName, string branchName)
    {
        try
        {
            var xml = await httpClient.GetAsXmlDocumentAsync($"https://raw.githubusercontent.com/{repoOwnerAndName}/{branchName}/eng/Versions.props");

            var majorVersion = int.Parse(xml.SelectSingleNode("//MajorVersion")?.InnerText
                ?? throw new InvalidOperationException("MajorVersion not found"));
            var minorVersion = int.Parse(xml.SelectSingleNode("//MinorVersion")?.InnerText
                ?? throw new InvalidOperationException("MinorVersion not found"));
            var patchVersion = int.Parse(xml.SelectSingleNode("//PatchVersion")?.InnerText
                ?? throw new InvalidOperationException("PatchVersion not found"));
            var preReleaseVersionLabel = xml.SelectSingleNode("//PreReleaseVersionLabel")?.InnerText
                ?? throw new InvalidOperationException("PreReleaseVersionLabel not found");

            return new VersionsProps(majorVersion, minorVersion, patchVersion, preReleaseVersionLabel);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Cannot load Versions.props from '{repoOwnerAndName}' branch '{branchName}'", ex);
        }
    }

    public override string ToString() => $"{MajorVersion}.{MinorVersion}.{PatchVersion}-{PreReleaseVersionLabel}";
}

file sealed record VsVersion(int Major, int Minor)
{
    public static VsVersion? TryParse(string s)
    {
        if (Patterns.VsVersion.Match(s) is { Success: true } match)
        {
            return new VsVersion(int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value));
        }

        return null;
    }

    public string AsDarcChannelName() => $"VS {Major}.{Minor}";

    public string AsVsBranchName() => $"rel/d{Major}.{Minor}";

    public string AsVsInsertionTitlePrefix() => $"[d{Major}.{Minor}]";

    public VsVersion Increase() => new(Major, Minor + 1);
}

static partial class Patterns
{
    [GeneratedRegex(@"(\d+)\.(\d+)")]
    public static partial Regex VsVersion { get; }
}

file static class Extensions
{
    extension(Command command)
    {
        public async Task<BufferedCommandResult> ExecuteBufferedAsync(Logger logger)
        {
            logger.Log($"Executing command: {command}");
            var result = await command.ExecuteBufferedAsync();
            logger.Log($"Command completed ({result.ExitCode}):\nStdout:{result.StandardOutput}\nStderr:{result.StandardError}");
            return result;
        }
    }

    extension(IAnsiConsole console)
    {
        public bool Confirm(string prompt, bool defaultValue)
        {
            return new ConfirmationPrompt(prompt)
            {
                DefaultValue = defaultValue,
                DefaultValueStyle = Style.Plain.Foreground(Color.Grey),
                ChoicesStyle = Style.Plain,
            }
            .Show(console);
        }

        /// <summary>
        /// Original <see cref="AnsiConsoleExtensions.Progress"/> can overwrite existing text, this avoids that.
        /// </summary>
        public Progress ProgressLine()
        {
            console.WriteLine();
            console.WriteLine();
            return console.Progress();
        }
    }

    extension<T>(IEnumerable<T?> collection)
    {
        public IEnumerable<T> AssertNonNullElements(string message)
        {
            foreach (var item in collection)
            {
                if (item is null)
                {
                    throw new InvalidOperationException(message);
                }

                yield return item;
            }
        }
    }

    extension(HttpClient httpClient)
    {
        public async Task<XmlDocument> GetAsXmlDocumentAsync(string requestUri)
        {
            using var stream = await httpClient.GetStreamAsync(requestUri);
            var doc = new XmlDocument();
            doc.Load(stream);
            return doc;
        }
    }

    extension(string s)
    {
        public string? GetFirstLine()
        {
            foreach (var line in s.EnumerateLines())
            {
                return line.ToString();
            }

            return null;
        }

        public string GetRepoShortcut() => s.TrimPrefix("https://github.com/");

        public T? ParseJson<T>()
        {
            try
            {
                return JsonSerializer.Deserialize<T>(s, JsonSerializerOptions.Web);
            }
            catch (JsonException e)
            {
                throw new Exception($"Cannot deserialize JSON '{s}'", e);
            }
        }

        public T[]? ParseJsonList<T>()
        {
            try
            {
                return JsonSerializer.Deserialize<T[]>(s, JsonSerializerOptions.Web);
            }
            catch (JsonException e)
            {
                throw new Exception($"Cannot deserialize JSON '{s}'", e);
            }
        }

        public List<T?> ParseJsonNewLineDelimitedList<T>()
        {
            var result = new List<T?>();
            foreach (var line in s.EnumerateLines())
            {
                if (line.IsWhiteSpace())
                {
                    continue;
                }

                try
                {
                    result.Add(JsonSerializer.Deserialize<T>(line, JsonSerializerOptions.Web));
                }
                catch (JsonException e)
                {
                    throw new Exception($"Cannot deserialize JSON '{line}'", e);
                }
            }

            return result;
        }

        public string TrimPrefix(string prefix)
        {
            if (s.StartsWith(prefix, StringComparison.Ordinal))
            {
                return s[prefix.Length..];
            }

            return s;
        }
    }

    extension<T>(TextPrompt<T>)
    {
        /// <summary>
        /// Use this instead of <see cref="TextPromptExtensions.DefaultValue"/> to work around
        /// <see href="https://github.com/spectreconsole/spectre.console/issues/1181"/>.
        /// </summary>
        public static TextPrompt<T> Create(string text, T defaultValue)
        {
            return new TextPrompt<T>($"{text} [grey](default: {Markup.Escape($"{defaultValue}")})[/]:")
                .DefaultValue(defaultValue)
                .HideDefaultValue();
        }
    }

    extension<T>(TextPrompt<T>) where T : struct
    {
        public static TextPrompt<T> Create(string text, T? defaultValueIfNotNull)
        {
            return defaultValueIfNotNull is { } v
                ? TextPrompt<T>.Create(text, defaultValue: v)
                : new TextPrompt<T>($"{text}:");
        }
    }

    extension(TextPrompt<string>)
    {
        public static TextPrompt<string> Create(string text, string? defaultValueIfNotNullOrEmpty)
        {
            return !string.IsNullOrEmpty(defaultValueIfNotNullOrEmpty)
                ? TextPrompt<string>.Create(text, defaultValue: defaultValueIfNotNullOrEmpty)
                : new TextPrompt<string>($"{text}:");
        }
    }
}

file sealed class LoggingRenderHook(Logger logger) : IRenderHook
{
    private long _lastOffset;

    public IEnumerable<IRenderable> Process(RenderOptions options, IEnumerable<IRenderable> renderables)
    {
        // Timestamp will be added on each intercepted newline,
        // but not if someone from the outside wrote to the log in the meantime.
        if (_lastOffset != logger.Writer.BaseStream.Position)
        {
            logger.Writer.Write($"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss K}] ");
        }

        foreach (var renderable in renderables)
        {
            var segments = renderable.Render(options, int.MaxValue).ToArray();
            var text = string.Concat(segments.Select(static s => s.Text));
            text = text.ReplaceLineEndings($"\n[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss K}] ");
            logger.Writer.Write(text);
        }

        _lastOffset = logger.Writer.BaseStream.Position;

        return renderables;
    }
}

file sealed class DarcHelper(IAnsiConsole console)
{
    private readonly BarApiClient _barApiClient = new(
        buildAssetRegistryPat: null,
        managedIdentityId: null,
        disableInteractiveAuth: false);

    private readonly ConcurrentDictionary<string, Task<IEnumerable<DefaultChannel>>> _defaultChannelsCache = new();

    public ConcurrentBag<Flow> FoundFlows { get; } = [];

    public Task<IEnumerable<DefaultChannel>> GetDefaultChannelsAsync(string repoUrl)
    {
        return _defaultChannelsCache.GetOrAdd(repoUrl, static (repoUrl, @this) => @this._barApiClient.GetDefaultChannelsAsync(repoUrl), this);
    }

    public async Task<string?> TryGetDefaultChannelAsync(string repoUrl, string branchName)
    {
        var channels = await GetDefaultChannelsAsync(repoUrl);
        return channels.FirstOrDefault(c => c.Branch == branchName)?.Channel.Name;
    }

    public async Task<Action> ListSubscriptionsAsync(string sourceRepoUrl, string targetRepoUrl, string targetRepoFriendlyName, string subscriptionsText = "subscriptions to")
    {
        var subscriptionsTask = _barApiClient.GetSubscriptionsAsync(sourceRepoUrl, targetRepoUrl);
        var defaultChannels = await GetDefaultChannelsAsync(sourceRepoUrl);
        var subscriptions = await subscriptionsTask;
        var flows = (
            from channel in defaultChannels
            join subscription in subscriptions on channel.Channel.Id equals subscription.Channel.Id
            where subscription.Enabled
            select new Flow(SourceRepoUrl: sourceRepoUrl,
                SourceBranch: channel.Branch,
                Channel: channel.Channel.Name,
                TargetRepoUrl: targetRepoUrl,
                TargetBranch: subscription.TargetBranch))
            .ToArray();

        foreach (var flow in flows)
        {
            FoundFlows.Add(flow);
        }

        return () =>
        {
            console.MarkupLineInterpolated($"Found [teal]{flows.Length}[/] {subscriptionsText} {targetRepoFriendlyName}:");
            foreach (var flow in flows)
            {
                console.WriteLine($" - {flow.ToShortString()}");
            }
        };
    }

    public Task<Action> ListBackflowsAsync(string targetRepoUrl, string sourceRepoUrl = "https://github.com/dotnet/dotnet", string sourceRepoFriendlyName = "VMR")
    {
        return ListSubscriptionsAsync(sourceRepoUrl, targetRepoUrl, sourceRepoFriendlyName, "back flows from");
    }
}

file sealed record Flow(string SourceRepoUrl, string SourceBranch, string Channel, string TargetRepoUrl, string TargetBranch)
{
    public static string DescribeChanges(Flow existingFlow, Flow expectedFlow)
    {
        Debug.Assert(existingFlow.SourceRepoUrl == expectedFlow.SourceRepoUrl);
        Debug.Assert(existingFlow.TargetRepoUrl == expectedFlow.TargetRepoUrl);

        var changes = string.Join(" and ", DescribeChangesCore(existingFlow, expectedFlow));
        Debug.Assert(!string.IsNullOrWhiteSpace(changes));
        return changes;
    }

    private static IEnumerable<string> DescribeChangesCore(Flow existingFlow, Flow expectedFlow)
    {
        if (existingFlow.SourceBranch != expectedFlow.SourceBranch)
        {
            yield return $"to be from source branch [teal]{expectedFlow.SourceBranch}[/]";
        }

        if (existingFlow.Channel != expectedFlow.Channel)
        {
            yield return $"to have channel [teal]{expectedFlow.Channel}[/]";
        }

        if (existingFlow.TargetBranch != expectedFlow.TargetBranch)
        {
            yield return $"to target branch [teal]{expectedFlow.TargetBranch}[/]";
        }
    }

    public string ToShortString() => $"{SourceBranch} -> {Channel} -> {TargetBranch}";

    public string ToFullString() => $"{SourceRepoUrl.GetRepoShortcut()}/{SourceBranch} -> {Channel} -> {TargetRepoUrl.GetRepoShortcut()}/{TargetBranch}";

    public override string ToString() => ToFullString();
}

file sealed class Logger
{
    public Logger()
    {
        LogFilePath = Path.Join(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "snap-script", "log.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(LogFilePath)!);
        // This is disposed inside ProcessExit event, not here in Main, so it can be also used in UnhandledException handler.
        Writer = new StreamWriter(File.Open(LogFilePath, FileMode.Append, FileAccess.Write, FileShare.Read))
        {
            AutoFlush = true,
        };
        Writer.WriteLine();
        Writer.WriteLine();

        AppDomain.CurrentDomain.ProcessExit += (s, e) =>
        {
            Writer.Dispose();
        };
    }

    public StreamWriter Writer { get; }
    public string LogFilePath { get; }

    public void Log(string message)
    {
        Writer.WriteLine($"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss K}] {message}");
    }
}
