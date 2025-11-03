#!/usr/bin/env dotnet
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// NOTE: This should work for as many repos as possible, not just dotnet/roslyn, e.g., at least dotnet/razor, too.
// NOTE: This script shouldn't assume anything in the current working directory, hence it also doesn't use `git`, but only GitHub APIs (mostly via the `gh` CLI).

// Workaround for https://github.com/dotnet/roslyn/issues/76197.
#:property SignAssembly=false

#:property PublishAot=false
#:package CliWrap
#:package Microsoft.DotNet.DarcLib
#:package Spectre.Console
#:package System.CommandLine

#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task

using System.Collections.Concurrent;
using System.CommandLine;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
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
using Command = CliWrap.Command;

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
var dryRunOption = new Option<bool>("--dry-run");
var waitForDebuggerOption = new Option<bool>("--wait-for-debugger");
var rootCommand = new RootCommand("Snap script")
{
    dryRunOption,
    waitForDebuggerOption,
};
rootCommand.TreatUnmatchedTokensAsErrors = true;
var parsedArgs = rootCommand.Parse(args);
var argsParsingResult = parsedArgs.Invoke(); // validates args
if (argsParsingResult != 0)
{
    return argsParsingResult;
}
var dryRun = parsedArgs.GetValue(dryRunOption);
var waitForDebugger = parsedArgs.GetValue(waitForDebuggerOption);

// Wait for debugger (VSCode's debugger cannot handle interactive input).
if (waitForDebugger)
{
    console.MarkupLineInterpolated($"[yellow]Waiting for debugger to attach (PID: {Environment.ProcessId}, Process: {Path.GetFileName(Environment.ProcessPath) ?? "unknown"})...[/]");
    while (!Debugger.IsAttached)
    {
        await Task.Delay(1000);
    }
    Debugger.Break();
}

// Welcome message.
console.MarkupLineInterpolated($"Welcome to [grey]{getFileName()}[/], an interactive script to help with snap-related infra tasks");
static string getFileName([CallerFilePath] string filePath = "unknownFile") => Path.GetFileName(filePath);

console.MarkupLine(dryRun
    ? "[yellow]Note:[/] Running in [green]dry run[/] mode, no changes will be made"
    : "[yellow]Note:[/] Running in [red]live[/] mode, changes will be made at the end (after confirmation)");

// Setup.
var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("dotnet-roslyn-snap-script");
var actions = new ActionList(console);

// Check that the `gh` CLI is available.
bool ghExists;
try
{
    ghExists = 0 == (await Cli.Wrap("gh")
        .WithArguments(["--version"])
        .WithValidation(CommandResultValidation.None)
        .ExecuteBufferedAsync(logger))
        .ExitCode;
}
catch (Exception ex)
{
    logger.Log(ex.ToString());
    ghExists = false;
}
if (!ghExists)
{
    console.MarkupLine("[red]Error:[/] GitHub CLI 'gh' is not installed or not available in PATH. Please install it from https://cli.github.com/.");
    return 1;
}

console.WriteLine();
console.MarkupLine("[purple]Configuration[/]");
console.WriteLine();

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
var gitHub = new GitHubUtil(console, httpClient, logger, sourceRepoShort, dryRun: dryRun);

// Ask for source and target branches.

var sourceBranchName = console.Prompt(TextPrompt<string>.Create("Source branch",
    defaultValue: "main"));

// Find version number.
var sourceVersionsProps = await VersionsProps.LoadAsync(httpClient, sourceRepoShort, sourceBranchName);
console.MarkupLineInterpolated($"Branch [teal]{sourceBranchName}[/] has version [teal]{sourceVersionsProps?.Data.ToString() ?? "N/A"}[/]");

string? suggestedTargetBranchName = null;

// From the version number, infer the VS version it inserts to.
if (sourceVersionsProps is { Data: { } data })
{
    var vsVersion = sourceVersionsProps.CorrespondingVsVersion;
    suggestedTargetBranchName = $"release/dev{vsVersion.ToMajorMinorString()}";
}

var targetBranchName = console.Prompt(TextPrompt<string>.Create("Target branch",
    defaultValue: suggestedTargetBranchName ?? "release/insiders"));

// Find Roslyn version number.
var targetVersionsProps = await VersionsProps.LoadAsync(httpClient, sourceRepoShort, targetBranchName);
console.MarkupLineInterpolated($"Branch [teal]{targetBranchName}[/] has version [teal]{targetVersionsProps?.Data.ToString() ?? "N/A"}[/]");

// Find which VS the branches insert to.
var sourcePublishDataTask = PublishData.LoadAsync(httpClient, gitHub, sourceBranchName);
var targetPublishDataTask = PublishData.LoadAsync(httpClient, gitHub, targetBranchName);
var sourcePublishData = await sourcePublishDataTask;
sourcePublishData.Report(console);
var targetPublishData = await targetPublishDataTask;
targetPublishData.Report(console);

// Where should branches insert after the snap?

var inferredVsVersion = VsVersion.TryParse(targetBranchName);
var suggestedSourceVsVersionAfterSnap =
    // When the target branch does not exist, that likely means a snap to a new minor version.
    targetPublishData.Data is null ? inferredVsVersion?.Increase() : inferredVsVersion;
var suggestedTargetVsVersionAfterSnap = inferredVsVersion;

var sourceVsBranchAfterSnap = console.Prompt(TextPrompt<string>.Create($"After snap, [teal]{sourceBranchName}[/] should insert to",
    defaultValueIfNotNullOrEmpty: suggestedSourceVsVersionAfterSnap?.AsVsBranchName()));
var sourceVsAsDraftAfterSnap = console.Confirm($"Should insertion PRs be in draft mode for [teal]{sourceBranchName}[/]?", defaultValue: false);
var sourceVsPrefixAfterSnap = console.Prompt(TextPrompt<string>.Create($"What prefix should insertion PR titles have for [teal]{sourceBranchName}[/]?",
    defaultValueIfNotNullOrEmpty: suggestedSourceVsVersionAfterSnap?.AsVsInsertionTitlePrefix() ?? sourcePublishData.Data?.RequiredBranchInfo.InsertionTitlePrefix));
var sourceVersionsPropsUpdater = sourceVersionsProps.GetUpdater(console, gitHub, sourceBranchName);
var targetVsBranchAfterSnap = console.Prompt(TextPrompt<string>.Create($"After snap, [teal]{targetBranchName}[/] should insert to",
    defaultValueIfNotNullOrEmpty: suggestedTargetVsVersionAfterSnap?.AsVsBranchName()));
var targetVsAsDraftAfterSnap = console.Confirm($"Should insertion PRs be in draft mode for [teal]{targetBranchName}[/]?", defaultValue: false);
var targetVsPrefixAfterSnap = console.Prompt(TextPrompt<string>.Create($"What prefix should insertion PR titles have for [teal]{targetBranchName}[/]?",
    defaultValueIfNotNullOrEmpty: suggestedTargetVsVersionAfterSnap?.AsVsInsertionTitlePrefix() ?? targetPublishData.Data?.RequiredBranchInfo.InsertionTitlePrefix));

// Check subscriptions.
console.WriteLine();
console.MarkupLine("[purple]Subscriptions[/]");
console.WriteLine();

var darc = new DarcHelper(console, dryRun: dryRun);
var printers = await Task.WhenAll([
    darc.ListSubscriptionsAsync(sourceRepoUrl, DarcHelper.VmrRepoUrl, "VMR"),
    darc.ListBackflowsAsync(sourceRepoUrl),
    darc.ListSubscriptionsAsync(sourceRepoUrl, "https://github.com/dotnet/sdk", "SDK"),
    darc.ListSubscriptionsAsync(sourceRepoUrl, "https://github.com/dotnet/runtime", "runtime"),
]);
foreach (var printer in printers)
{
    printer();
}

// Determine subscription changes.
if (console.Confirm("Change some subscriptions in this snap script run?", defaultValue: true))
{
    // Source -> VMR
    var existingSourceBranchFlow = darc.FoundFlows.FirstOrDefault(flow =>
        flow.SourceRepoUrl == sourceRepoUrl &&
        flow.SourceBranch == sourceBranchName &&
        flow.TargetRepoUrl == DarcHelper.VmrRepoUrl);
    var sourceChannelAfterSnap = console.Prompt(TextPrompt<string>.Create($"After snap, [teal]{sourceBranchName}[/] should publish to darc channel",
        defaultValueIfNotNullOrEmpty: suggestedSourceVsVersionAfterSnap?.AsDarcChannelName()));
    var sourceVmrBranchAfterSnap = console.Prompt(TextPrompt<string>.Create("And flow to VMR branch",
        defaultValueIfNotNullOrEmpty: existingSourceBranchFlow?.TargetBranch));
    suggestSubscriptionChange(existingFlow: existingSourceBranchFlow, expectedFlow: new Flow(
        SourceRepoUrl: sourceRepoUrl,
        SourceBranch: sourceBranchName,
        Channel: sourceChannelAfterSnap,
        TargetRepoUrl: DarcHelper.VmrRepoUrl,
        TargetBranch: sourceVmrBranchAfterSnap));

    // VMR -> Source
    var existingSourceBranchBackFlow = darc.FoundFlows.FirstOrDefault(flow =>
        flow.SourceRepoUrl == DarcHelper.VmrRepoUrl &&
        flow.TargetRepoUrl == sourceRepoUrl &&
        flow.SourceBranch == sourceVmrBranchAfterSnap);
    var suggestedSourceBranchBackFlowChannel = existingSourceBranchBackFlow?.Channel
        ?? await darc.TryGetDefaultChannelAsync(DarcHelper.VmrRepoUrl, sourceVmrBranchAfterSnap)
        ?? console.Prompt(new TextPrompt<string>($"After snap, [teal]{sourceBranchName}[/] should flow back from VMR's [teal]{sourceVmrBranchAfterSnap}[/] via darc channel:"));
    suggestSubscriptionChange(existingFlow: existingSourceBranchBackFlow, expectedFlow: new Flow(
        SourceRepoUrl: DarcHelper.VmrRepoUrl,
        SourceBranch: sourceVmrBranchAfterSnap,
        Channel: suggestedSourceBranchBackFlowChannel,
        TargetRepoUrl: sourceRepoUrl,
        TargetBranch: sourceBranchName));

    // Target -> VMR
    var existingTargetBranchFlow = darc.FoundFlows.FirstOrDefault(flow =>
        flow.SourceRepoUrl == sourceRepoUrl &&
        flow.SourceBranch == targetBranchName &&
        flow.TargetRepoUrl == DarcHelper.VmrRepoUrl) ?? darc.FoundFlows.FirstOrDefault(flow =>
        flow.SourceRepoUrl == sourceRepoUrl &&
        flow.TargetRepoUrl == DarcHelper.VmrRepoUrl);
    var targetChannelAfterSnap = console.Prompt(TextPrompt<string>.Create($"After snap, [teal]{targetBranchName}[/] should publish to darc channel",
        defaultValueIfNotNullOrEmpty: suggestedTargetVsVersionAfterSnap?.AsDarcChannelName()));
    var targetVmrBranchAfterSnap = console.Prompt(TextPrompt<string>.Create("And flow to VMR branch",
        defaultValueIfNotNullOrEmpty: existingTargetBranchFlow?.TargetBranch));
    suggestSubscriptionChange(existingFlow: existingTargetBranchFlow, expectedFlow: new Flow(
        SourceRepoUrl: sourceRepoUrl,
        SourceBranch: targetBranchName,
        Channel: targetChannelAfterSnap,
        TargetRepoUrl: DarcHelper.VmrRepoUrl,
        TargetBranch: targetVmrBranchAfterSnap));

    // VMR -> Target
    var existingTargetBranchBackFlow = darc.FoundFlows.FirstOrDefault(flow =>
        flow.SourceRepoUrl == DarcHelper.VmrRepoUrl &&
        flow.TargetRepoUrl == sourceRepoUrl &&
        flow.SourceBranch == targetVmrBranchAfterSnap);
    var suggestedTargetBranchBackFlowChannel = existingTargetBranchBackFlow?.Channel
        ?? await darc.TryGetDefaultChannelAsync(DarcHelper.VmrRepoUrl, targetVmrBranchAfterSnap)
        ?? console.Prompt(new TextPrompt<string>($"After snap, [teal]{targetBranchName}[/] should flow back from VMR's [teal]{targetVmrBranchAfterSnap}[/] via darc channel:"));
    suggestSubscriptionChange(existingFlow: existingTargetBranchBackFlow, expectedFlow: new Flow(
        SourceRepoUrl: DarcHelper.VmrRepoUrl,
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
            if (existingFlow != null)
            {
                if (actions.Add($"Update flow {existingFlow.ToFullString()} {Flow.DescribeChanges(existingFlow, expectedFlow)}",
                    () => darc.UpdateFlowAsync(existingFlow: existingFlow, expectedFlow: expectedFlow)))
                {
                    return;
                }
            }

            actions.Add($"Add flow {expectedFlow.ToFullString()}", () => darc.SetupNewFlowAsync(expectedFlow));
        }
    }
}

// Find last 5 PRs merged to current branch.

console.WriteLine();
console.MarkupLine("[purple]Pull Requests[/] (point of snap)");
console.WriteLine();

var lastMergedSearchFilter = $"is:merged base:{sourceBranchName} sort:updated-desc";
var lastMergedPullRequests = (await Cli.Wrap("gh")
    .WithArguments(["pr", "list",
        "--repo", sourceRepoShort,
        "--search", lastMergedSearchFilter,
        "--json", PullRequest.JsonFields,
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

var milestoneSearchFilter = $"is:merged milestone:{nextMilestoneName} base:{sourceBranchName} sort:updated-desc";
var milestonePullRequests = (await Cli.Wrap("gh")
    .WithArguments(["pr", "list",
        "--repo", sourceRepoShort,
        "--search", milestoneSearchFilter,
        "--json", PullRequest.JsonFields])
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
        "--json", PullRequest.JsonFields])
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

async Task<Milestone> ensureTargetMilestoneCreatedAsync()
{
    if (selectedMilestone is null)
    {
        console.MarkupLineInterpolated($"Creating milestone [teal]{targetMilestone}[/]");
        if (dryRun)
        {
            console.MarkupLineInterpolated($"[yellow]Dry run:[/] no change made");
            selectedMilestone = new Milestone(-1, targetMilestone);
        }
        else
        {
            selectedMilestone = (await Cli.Wrap("gh")
                .WithArguments(["api", "-X", "POST", $"repos/{sourceRepoShort}/milestones",
                    "--field", $"title={targetMilestone}"])
                .ExecuteBufferedAsync(logger))
                .StandardOutput
                .ParseJson<Milestone>()
                ?? throw new InvalidOperationException($"Null returned when creating milestone {targetMilestone}");
        }
    }

    return selectedMilestone;
}

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
        lastMilestonePr = milestonePullRequests.First(pr => pr.Number == lastMilestonePrNumber);
    }
    var lastMilestonePrIndex = milestonePullRequests.IndexOf(lastMilestonePr);
    Debug.Assert(lastMilestonePrIndex >= 0);

    // Move PRs to target milestone.
    var prsToChangeMilestonesOf = milestonePullRequests.AsSpan(lastMilestonePrIndex..).ToArray();
    actions.Add($"Move [teal]{prsToChangeMilestonesOf.Length}[/] PRs from milestone [teal]{nextMilestoneName}[/] to [teal]{targetMilestone}[/]", async () =>
    {
        await ensureTargetMilestoneCreatedAsync();

        await console.ProgressLine().StartAsync(async progress =>
        {
            var task = progress.AddTask("Processing PRs", maxValue: prsToChangeMilestonesOf.Length);
            foreach (var pr in prsToChangeMilestonesOf)
            {
                if (!dryRun)
                {
                    await Cli.Wrap("gh")
                        .WithArguments(["pr", "edit", $"{pr.Number}",
                            "--repo", sourceRepoShort,
                            "--milestone", targetMilestone])
                        .ExecuteBufferedAsync(logger);
                }
                task.Increment(1);
            }
            task.StopTask();
            if (dryRun)
            {
                console.MarkupLineInterpolated($"[yellow]Dry run:[/] no changes made");
            }
        });
    });
}

// Find closed issues in milestone Next.
var issueMilestoneSearchFilter = $"is:closed milestone:{nextMilestoneName}";
var milestoneIssues = (await Cli.Wrap("gh")
    .WithArguments(["issue", "list",
        "--repo", sourceRepoShort,
        "--search", issueMilestoneSearchFilter,
        "--json", Issue.JsonFields])
    .ExecuteBufferedAsync())
    .StandardOutput
    .ParseJsonList<Issue>()
    ?.ToArray()
    ?? throw new InvalidOperationException($"Null issue list in milestone {nextMilestoneName}");

// Move closed issues from Next to target milestone.
if (milestoneIssues.Length != 0)
{
    actions.Add($"Move [teal]{milestoneIssues.Length}[/] issues from milestone [teal]{nextMilestoneName}[/] to [teal]{targetMilestone}[/]", async () =>
    {
        await ensureTargetMilestoneCreatedAsync();

        await console.ProgressLine().StartAsync(async progress =>
        {
            var task = progress.AddTask("Processing issues", maxValue: milestoneIssues.Length);
            foreach (var issue in milestoneIssues)
            {
                if (!dryRun)
                {
                    await Cli.Wrap("gh")
                        .WithArguments(["issue", "edit", $"{issue.Number}",
                            "--repo", sourceRepoShort,
                            "--milestone", targetMilestone])
                        .ExecuteBufferedAsync(logger);
                }
                task.Increment(1);
            }
            task.StopTask();
            if (dryRun)
            {
                console.MarkupLineInterpolated($"[yellow]Dry run:[/] no changes made");
            }
        });
    });
}

var snapBranchName = $"snap-{sourceBranchName.Replace('/', '-')}-to-{targetBranchName.Replace('/', '-')}-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}";

// Merge between branches.
actions.Add($"Merge changes from [teal]{sourceBranchName}[/] to [teal]{targetBranchName}[/] up to and including PR [teal]#{lastPr.Number}[/]", async () =>
{
    var prTitle = $"Snap {sourceBranchName} into {targetBranchName}";

    // Check whether the target branch exists.
    var targetBranchExists = await gitHub.DoesBranchExistAsync(targetBranchName);

    if (!targetBranchExists)
    {
        // Target branch does not exist, checkout the commit and just push to the new branch.
        console.MarkupLineInterpolated($"Target branch [teal]{targetBranchName}[/] does not exist, will be created.");
        await gitHub.CreateBranchAsync(targetBranchName, lastPrCommitDetails.ToCommit());
        console.MarkupLineInterpolated($"Pushed new branch [teal]{targetBranchName}[/] to [teal]{sourceRepoShort}[/].");
    }
    else
    {
        // Target branch exists, merge changes up to the commit.

        // Create a branch for the PR.
        await gitHub.CreateBranchAsync(snapBranchName, lastPrCommitDetails.ToCommit());

        // Open the PR.
        console.WriteLine("Opening snap PR.");
        await gitHub.OpenPrAsync(
            title: prTitle,
            head: snapBranchName,
            baseBranch: targetBranchName,
            repo: sourceRepoShort);
    }
});

// Change PublishData.json, Versions.props.
// Needs to be done after the merge which would create the target branch if it doesn't exist yet.
var snapConfigsBranchName = $"snap-configs-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}";
var sourcePublishDataAfterSnap = sourcePublishData.WithBranchInfo(new(
    VsBranch: sourceVsBranchAfterSnap,
    InsertionTitlePrefix: sourceVsPrefixAfterSnap,
    InsertionCreateDraftPR: sourceVsAsDraftAfterSnap), targetPublishData.Data);
sourcePublishDataAfterSnap.PushOrOpenPrIfNeeded(actions, sourcePublishData, snapConfigsBranchName);
sourceVersionsPropsUpdater(actions, snapConfigsBranchName);
var targetPublishDataAfterSnap = targetPublishData.WithBranchInfo(new(
    VsBranch: targetVsBranchAfterSnap,
    InsertionTitlePrefix: targetVsPrefixAfterSnap,
    InsertionCreateDraftPR: targetVsAsDraftAfterSnap), sourcePublishData.Data);
targetPublishDataAfterSnap.PushOrOpenPrIfNeeded(actions, targetPublishData, snapBranchName);

// Perform actions.
console.WriteLine();
console.MarkupLine("[purple]Modifications[/]");
console.WriteLine();
await actions.PerformAllAsync();

return 0;

file sealed record PullRequest(int Number, string Title, DateTimeOffset MergedAt, Commit MergeCommit)
{
    public const string JsonFields = "number,title,mergedAt,mergeCommit";

    public override string ToString() => $"#{Number}: {Title} ({MergedAt})";
}

file sealed record Issue(int Number, string Title)
{
    public const string JsonFields = "number,title";

    public override string ToString() => $"#{Number}: {Title}";
}

file sealed record Commit(string Oid)
{
    public override string ToString() => Oid;
}

file sealed record CommitDetails(string Sha, CommitDetailsCommit Commit)
{
    public Commit ToCommit() => new(Sha);
}

file sealed record CommitDetailsCommit(string Message);

file sealed record Milestone(int Number, string Title)
{
    public override string ToString() => Title;
}

file sealed class GitHubUtil(
    IAnsiConsole console,
    HttpClient httpClient,
    Logger logger,
    string repoOwnerAndName,
    bool dryRun)
{
    public const string UpdateConfigsPrTitle = "Update snap configuration files";

    public Logger Logger => logger;
    public string RepoOwnerAndName => repoOwnerAndName;

    public async Task<string> FetchFileAsStringAsync(string branchName, string path)
    {
        return await httpClient.GetStringAsync($"https://raw.githubusercontent.com/{repoOwnerAndName}/{branchName}/{path}");
    }

    public async Task<bool> DoesBranchExistAsync(string branchName)
    {
        return (await Cli.Wrap("gh")
            .WithArguments(["api", $"repos/{repoOwnerAndName}/branches/{branchName}"])
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync(logger))
            .IsSuccess;
    }

    public async Task CreateBranchAsync(string name, Commit head)
    {
        console.MarkupLineInterpolated($"Creating branch [teal]{name}[/] in [teal]{repoOwnerAndName}[/] at commit [teal]{head.Oid}[/]");
        if (dryRun)
        {
            console.MarkupLineInterpolated($"[yellow]Dry run:[/] no change made");
        }
        else
        {
            await Cli.Wrap("gh")
                .WithArguments(["api", "-X", "POST", $"repos/{repoOwnerAndName}/git/refs",
                    "--field", $"ref=refs/heads/{name}",
                    "--field", $"sha={head.Oid}"])
                .ExecuteBufferedAsync(logger);
        }
    }

    public async Task OpenPrAsync(string title, string head, string baseBranch, string repo)
    {
        console.MarkupLineInterpolated($"Opening PR [teal]{title}[/] from [teal]{head}[/] to [teal]{baseBranch}[/] in [teal]{repo}[/]");
        if (dryRun)
        {
            console.MarkupLineInterpolated($"[yellow]Dry run:[/] no change made");
        }
        else
        {
            var createPrResult = await Cli.Wrap("gh")
                .WithArguments([
                    "pr", "create",
                    "--title", title,
                    "--body", "Auto-generated by snap script.",
                    "--head", head,
                    "--base", baseBranch,
                    "--repo", repo])
                .ExecuteBufferedAsync(logger);
            console.MarkupLineInterpolated($"=> [teal]{createPrResult.StandardOutput.Trim()}[/]");
        }
    }

    public async Task UpdateFileAsync(string filePath, string branchName, string fileSha, byte[] bytes)
    {
        console.MarkupLineInterpolated($"Updating file [teal]{filePath}[/] in branch [teal]{branchName}[/] of [teal]{RepoOwnerAndName}[/]");
        if (dryRun)
        {
            console.MarkupLineInterpolated($"[yellow]Dry run:[/] no change made");
        }
        else
        {
            // Write to temp file to avoid issues with large content on command line.
            var tempFilePath = Path.GetTempFileName();
            try
            {
                await File.WriteAllTextAsync(tempFilePath, Convert.ToBase64String(bytes));
                await Cli.Wrap("gh")
                    .WithArguments(["api", "-X", "PUT", $"repos/{RepoOwnerAndName}/contents/{filePath}",
                        "--field", $"message=Update {Path.GetFileName(filePath)}",
                        "--field", $"branch={branchName}",
                        "--field", $"sha={fileSha}",
                        "--field", $"content=@{tempFilePath}"])
                    .ExecuteBufferedAsync(logger);
            }
            finally
            {
                try
                {
                    File.Delete(tempFilePath);
                }
                catch (Exception ex)
                {
                    logger.Log($"Failed to delete temp file '{tempFilePath}': {ex}");
                }
            }
        }
    }

    public async Task<Commit> GetBranchHeadAsync(string branchName)
    {
        var sha = (await Cli.Wrap("gh")
            .WithArguments(["api", $"repos/{repoOwnerAndName}/git/refs/heads/{branchName}"])
            .ExecuteBufferedAsync(logger))
            .StandardOutput
            .Trim()
            .ParseJson<JsonElement>()
            .GetProperty("object")
            .GetProperty("sha")
            .GetString()
            ?? throw new InvalidOperationException($"Null branch '{branchName}' in '{repoOwnerAndName}'");
        return new Commit(sha);
    }

    public void PushOrOpenPrToUpdateFile(ActionList actions, string title, string branchName, string updateBranchName, Func<IAsyncEnumerable<(string FilePath, byte[] Bytes)>> files, string prTitle)
    {
        var console = actions.Console;
        actions.Add(title, async () =>
        {
            var updateBranchExists = await DoesBranchExistAsync(updateBranchName);

            // Create a branch for the PR.
            if (!updateBranchExists)
            {
                var baseBranchHead = await GetBranchHeadAsync(branchName);
                await CreateBranchAsync(updateBranchName, baseBranchHead);
            }

            await foreach (var (filePath, bytes) in files())
            {
                // Obtain SHA of the file (needed for the update API call).
                var fileSha = (await Cli.Wrap("gh")
                    .WithArguments(["api", "-X", "GET", $"repos/{RepoOwnerAndName}/contents/{filePath}",
                        "--field", $"ref=refs/heads/{updateBranchName}"])
                    .ExecuteBufferedAsync(logger))
                    .StandardOutput
                    .Trim()
                    .ParseJson<JsonElement>()
                    .GetProperty("sha")
                    .GetString()
                    ?? throw new InvalidOperationException("Null file SHA");

                // Apply changes. Write to temp file to avoid issues with large content on command line.
                await UpdateFileAsync(
                    filePath: filePath,
                    branchName: updateBranchName,
                    fileSha: fileSha,
                    bytes: bytes);
            }

            // Open the PR (unless the branch already exists - then this is part of already-opened PR).
            if (!updateBranchExists)
            {
                await OpenPrAsync(
                    title: prTitle,
                    head: updateBranchName,
                    baseBranch: branchName,
                    repo: RepoOwnerAndName);
            }
            else
            {
                console.MarkupLineInterpolated($"[green]Note:[/] Reusing existing branch [teal]{updateBranchName}[/].");
            }
        });
    }

    /// <summary>
    /// For <c>"https://github.com/dotnet/roslyn"</c>, returns <c>"roslyn"</c>.
    /// </summary>
    public static string GetRepoNameFromUrl(string repoUrl)
    {
        var uri = new Uri(repoUrl);
        var segments = uri.AbsolutePath.Trim('/').Split('/');
        if (segments.Length != 2)
        {
            throw new InvalidOperationException($"Cannot extract repo name from URL '{repoUrl}'");
        }
        return segments[1];
    }
}

file sealed class PublishData(
    GitHubUtil gitHub,
    string branchName,
    PublishDataJson? data)
{
    public const string FileName = "PublishData.json";
    private const string FilePath = $"eng/config/{FileName}";

    public string RepoOwnerAndName => gitHub.RepoOwnerAndName;
    public string BranchName => branchName;
    public PublishDataJson? Data => data;

    public PublishData WithBranchInfo(BranchInfo branchInfo, PublishDataJson? templateData)
    {
        return new(gitHub, branchName, Data is null
            ? (templateData is null
                ? throw new InvalidOperationException($"Cannot create {FileName} without existing data or template data")
                : templateData.WithBranchInfo(branchInfo))
            : Data.WithBranchInfo(branchInfo));
    }

    /// <returns>
    /// <see langword="null"/> if the repo or branch does not exist.
    /// </returns>
    public static async Task<PublishData> LoadAsync(HttpClient httpClient, GitHubUtil gitHub, string branchName)
    {
        try
        {
            var data = await httpClient.GetFromJsonAsync<PublishDataJson>($"https://raw.githubusercontent.com/{gitHub.RepoOwnerAndName}/{branchName}/{FilePath}")
                ?? throw new InvalidOperationException($"Null {FileName}");
            _ = data.RequiredBranchInfo;
            return new PublishData(gitHub, branchName, data);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return new PublishData(gitHub, branchName, data: null);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Cannot load '{FilePath}' from '{gitHub.RepoOwnerAndName}' branch '{branchName}'", ex);
        }
    }

    public void Report(IAnsiConsole console)
    {
        console.MarkupLineInterpolated($"Branch [teal]{branchName}[/] inserts to VS [teal]{data?.RequiredBranchInfo.Summarize() ?? "N/A"}[/] with prefix [teal]{data?.RequiredBranchInfo.InsertionTitlePrefix ?? "N/A"}[/]");
    }

    public void PushOrOpenPrIfNeeded(ActionList actions, PublishData? original, string updateBranchName)
    {
        var console = actions.Console;

        Debug.Assert(original is null || (original.RepoOwnerAndName == RepoOwnerAndName && original.BranchName == BranchName));

        if (this.Equals(original))
        {
            console.MarkupLineInterpolated($"[green]No change needed:[/] [teal]{BranchName}[/] {FileName} already up to date");
            return;
        }

        Debug.Assert(data != null);
        Debug.Assert(!data.Equals(original?.Data));

        gitHub.PushOrOpenPrToUpdateFile(
            actions,
            title: $"Update [teal]{BranchName}[/] {FileName}",
            branchName: branchName,
            updateBranchName: updateBranchName,
            files: () => GetFilesAsync(data),
            prTitle: GitHubUtil.UpdateConfigsPrTitle);
    }

    private static async IAsyncEnumerable<(string FilePath, byte[] Bytes)> GetFilesAsync(PublishDataJson data)
    {
        yield return (FilePath, Encoding.UTF8.GetBytes(data.ToJson()));
    }
}

file sealed record PublishDataJson
{
    private static readonly JsonSerializerOptions s_jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        NewLine = "\n",
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtraProperties { get; init; }

    public BranchInfo? BranchInfo { get; init; } // roslyn spelling

    public BranchInfo? Branches { get; init; } // razor spelling

    [JsonIgnore]
    public BranchInfo RequiredBranchInfo => BranchInfo ?? Branches ?? throw new InvalidOperationException($"Missing {nameof(BranchInfo)} in {PublishData.FileName}");

    public string ToJson() => JsonSerializer.Serialize(this, s_jsonOptions) + "\n";

    public PublishDataJson WithBranchInfo(BranchInfo branchInfo)
    {
        if (BranchInfo != null)
        {
            return this with { BranchInfo = branchInfo };
        }
        else if (Branches != null)
        {
            return this with { Branches = branchInfo };
        }
        else
        {
            throw new InvalidOperationException($"Missing {nameof(BranchInfo)} in {PublishData.FileName}");
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

file sealed class VersionsProps
{
    public const string FileName = "Versions.props";
    private const string FilePath = $"eng/{FileName}";

    public required VersionsPropsData Data { get; init; }
    public required XmlDocument Document { get; init; }
    public required bool VsixVersionPrefix { get; init; }

    public VersionsPropsData CorrespondingVsVersion => VsixVersionPrefix
        ? Data
        // Roslyn 4.x corresponds to VS 17.x and so on.
        : new VersionsPropsData(
            MajorVersion: Data.MajorVersion + 13,
            MinorVersion: Data.MinorVersion,
            PatchVersion: Data.PatchVersion);

    /// <returns>
    /// <see langword="null"/> if the repo or branch does not exist.
    /// </returns>
    public static async Task<VersionsProps?> LoadAsync(HttpClient httpClient, string repoOwnerAndName, string branchName)
    {
        try
        {
            var xml = await httpClient.GetAsXmlDocumentAsync($"https://raw.githubusercontent.com/{repoOwnerAndName}/{branchName}/{FilePath}");

            // If the file has VsixVersionPrefix (e.g., razor), use that.
            if (xml.FindSingleNode(nameof(VsixVersionPrefix))?.InnerText is { } vsixVersionPrefix &&
                VersionsPropsData.TryParse(vsixVersionPrefix, CultureInfo.InvariantCulture, out var data))
            {
                return new()
                {
                    Data = data,
                    Document = xml,
                    VsixVersionPrefix = true,
                };
            }

            data = new VersionsPropsData(
                MajorVersion: int.Parse(xml.FindSingleRequiredNode(nameof(VersionsPropsData.MajorVersion)).InnerText),
                MinorVersion: int.Parse(xml.FindSingleRequiredNode(nameof(VersionsPropsData.MinorVersion)).InnerText),
                PatchVersion: int.Parse(xml.FindSingleRequiredNode(nameof(VersionsPropsData.PatchVersion)).InnerText));

            return new()
            {
                Data = data,
                Document = xml,
                VsixVersionPrefix = false,
            };
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Cannot load '{FilePath}' from '{repoOwnerAndName}' branch '{branchName}'", ex);
        }
    }

    public void SaveTo(XmlDocument xml)
    {
        if (VsixVersionPrefix)
        {
            xml.FindSingleRequiredNode(nameof(VsixVersionPrefix)).InnerText = Data.ToString();
            xml.FindSingleRequiredNode("AddinMajorVersion").InnerText = Data.ToMajorMinorString();
        }
        else
        {
            xml.FindSingleRequiredNode(nameof(Data.MajorVersion)).InnerText = Data.MajorVersion.ToString(CultureInfo.InvariantCulture);
            xml.FindSingleRequiredNode(nameof(Data.MinorVersion)).InnerText = Data.MinorVersion.ToString(CultureInfo.InvariantCulture);
            xml.FindSingleRequiredNode(nameof(Data.PatchVersion)).InnerText = Data.PatchVersion.ToString(CultureInfo.InvariantCulture);
        }
    }

    public void PushOrOpenPrIfNeeded(GitHubUtil gitHub, ActionList actions, string branchName, VersionsPropsData newData, string updateBranchName)
    {
        var console = actions.Console;

        if (Data.Equals(newData))
        {
            console.MarkupLineInterpolated($"[green]No change needed:[/] [teal]{branchName}[/] {FileName} already up to date");
            return;
        }

        bool updateSarifFiles = gitHub.RepoOwnerAndName.EndsWith("/roslyn", StringComparison.Ordinal);

        gitHub.PushOrOpenPrToUpdateFile(
            actions,
            title: $"Update [teal]{branchName}[/] {FileName}{(updateSarifFiles ? " and SARIF files" : "")}",
            branchName: branchName,
            updateBranchName: updateBranchName,
            files: () => GetFilesAsync(gitHub, branchName, newData, (XmlDocument)Document.CloneNode(deep: true), updateSarifFiles),
            prTitle: GitHubUtil.UpdateConfigsPrTitle);
    }

    private async IAsyncEnumerable<(string FilePath, byte[] Bytes)> GetFilesAsync(GitHubUtil gitHub, string branchName, VersionsPropsData newData, XmlDocument xml, bool updateSarifFiles)
    {
        this.SaveTo(xml);
        yield return (FilePath, Encoding.UTF8.GetBytes(xml.OuterXml));

        if (!updateSarifFiles)
        {
            gitHub.Logger.Log($"Not a Roslyn repo ({gitHub.RepoOwnerAndName}), skipping SARIF files update.");
            yield break;
        }

        // Update sarif files too.
        var previousVersion = this.Data.ToString();
        var newVersion = newData.ToString();
        if (previousVersion == newVersion)
        {
            gitHub.Logger.Log($"Version didn't change ({previousVersion}), skipping SARIF files update.");
            yield break;
        }
        string[] files =
        [
            "src/RoslynAnalyzers/Microsoft.CodeAnalysis.Analyzers/Microsoft.CodeAnalysis.Analyzers.sarif",
            "src/RoslynAnalyzers/Microsoft.CodeAnalysis.BannedApiAnalyzers/Microsoft.CodeAnalysis.BannedApiAnalyzers.sarif",
            "src/RoslynAnalyzers/PerformanceSensitiveAnalyzers/Microsoft.CodeAnalysis.PerformanceSensitiveAnalyzers.sarif",
            "src/RoslynAnalyzers/PublicApiAnalyzers/Microsoft.CodeAnalysis.PublicApiAnalyzers.sarif",
            "src/RoslynAnalyzers/Roslyn.Diagnostics.Analyzers/Roslyn.Diagnostics.Analyzers.sarif",
            "src/RoslynAnalyzers/Text.Analyzers/Text.Analyzers.sarif",
        ];
        foreach (var filePath in files)
        {
            var originalContent = await gitHub.FetchFileAsStringAsync(branchName, filePath);
            var updatedContent = originalContent.Replace(previousVersion, newVersion, StringComparison.Ordinal);
            if (originalContent == updatedContent)
            {
                gitHub.Logger.Log($"No changes in SARIF file '{filePath}'.");
                continue;
            }
            yield return (filePath, Encoding.Utf8WithBom.GetBytes(updatedContent));
        }
    }
}

[TypeConverter(typeof(ParsableTypeConverter<VersionsPropsData>))]
file sealed record VersionsPropsData(
    int MajorVersion,
    int MinorVersion,
    int PatchVersion)
    : IParsable<VersionsPropsData>
{
    public static VersionsPropsData Parse(string s, IFormatProvider? provider)
    {
        if (TryParse(s, provider, out var result))
        {
            return result;
        }

        throw new FormatException($"Cannot parse {nameof(VersionsPropsData)} from '{s}'");
    }

    public static bool TryParse([NotNullWhen(returnValue: true)] string? s, IFormatProvider? provider, [MaybeNullWhen(returnValue: false)] out VersionsPropsData result)
    {
        result = null;

        if (s is null)
        {
            return false;
        }

        var versionParts = s.Split('.', 3);
        if (versionParts.Length != 3)
        {
            return false;
        }

        if (!int.TryParse(versionParts[0], out var majorVersion))
        {
            return false;
        }

        if (!int.TryParse(versionParts[1], out var minorVersion))
        {
            return false;
        }

        if (!int.TryParse(versionParts[2], out var patchVersion))
        {
            return false;
        }

        result = new VersionsPropsData(majorVersion, minorVersion, patchVersion);
        return true;
    }

    public override string ToString() => $"{MajorVersion.ToString(CultureInfo.InvariantCulture)}.{MinorVersion.ToString(CultureInfo.InvariantCulture)}.{PatchVersion.ToString(CultureInfo.InvariantCulture)}";

    public string ToMajorMinorString() => $"{MajorVersion.ToString(CultureInfo.InvariantCulture)}.{MinorVersion.ToString(CultureInfo.InvariantCulture)}";

    public VersionsPropsData WithIncrementedMinor() => this with { MinorVersion = MinorVersion + 1 };
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
    private static readonly Encoding s_utf8WithBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

    extension(Command command)
    {
        public async Task<BufferedCommandResult> ExecuteBufferedAsync(Logger logger)
        {
            logger.Log($"Executing command: {command}");

            var originalValidation = command.Validation;
            var result = await command.WithValidation(CommandResultValidation.None).ExecuteBufferedAsync();

            logger.Log($"Command completed ({result.ExitCode}):\nStdout:{result.StandardOutput}\nStderr:{result.StandardError}");

            if (!result.IsSuccess && originalValidation == CommandResultValidation.ZeroExitCode)
            {
                throw new InvalidOperationException($"Command '{command}' failed with exit code {result.ExitCode}.\nStdout:{result.StandardOutput}\nStderr:{result.StandardError}");
            }

            return result;
        }
    }

    extension(Encoding)
    {
        public static Encoding Utf8WithBom => s_utf8WithBom;
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
            var doc = new XmlDocument { PreserveWhitespace = true };
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

    extension(VersionsProps? versionsProps)
    {
        public Action<ActionList, string> GetUpdater(IAnsiConsole console, GitHubUtil gitHub, string branchName)
        {
            if (versionsProps is null)
            {
                console.MarkupLineInterpolated($"[yellow]Warning:[/] Cannot update [teal]{branchName}[/] {VersionsProps.FileName} as original XML document is missing");
                return delegate { };
            }

            var versionAfterSnap = console.Prompt(TextPrompt<VersionsPropsData>.Create($"After snap, [teal]{branchName}[/] should have version",
                defaultValue: versionsProps.Data.WithIncrementedMinor()));
            return (ActionList actions, string updateBranchName) => versionsProps.PushOrOpenPrIfNeeded(gitHub, actions, branchName, versionAfterSnap, updateBranchName);
        }
    }

    extension(XmlDocument document)
    {
        public XmlNode? FindSingleNode(string localName)
        {
            return document.SelectSingleNode($"//*[local-name()='{localName}']");
        }

        public XmlNode FindSingleRequiredNode(string localName)
        {
            return document.FindSingleNode(localName)
                ?? throw new InvalidOperationException($"{localName} not found");
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

file sealed class DarcHelper(IAnsiConsole console, bool dryRun)
{
    public const string VmrRepoUrl = "https://github.com/dotnet/dotnet";

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
            select new Flow(
                SourceRepoUrl: sourceRepoUrl,
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

    private async Task DeleteDefaultChannelsAsync(string? repository = null, string? branch = null, string? channel = null)
    {
        var defaultChannels = (await _barApiClient.GetDefaultChannelsAsync(repository, branch, channel)).ToArray();
        if (defaultChannels.Length == 0)
        {
            console.MarkupLineInterpolated($"No default channels found for repository [teal]{repository ?? "N/A"}[/], branch [teal]{branch ?? "N/A"}[/], channel [teal]{channel ?? "N/A"}[/], skipping deletion");
            return;
        }

        foreach (var defaultChannel in defaultChannels)
        {
            console.MarkupLineInterpolated($"Deleting default channel [teal]{defaultChannel.Id}[/] for repository [teal]{defaultChannel.Repository}[/], branch [teal]{defaultChannel.Branch}[/], channel [teal]{defaultChannel.Channel.Name}[/]");
            if (dryRun)
            {
                console.MarkupLineInterpolated($"[yellow]Dry run:[/] no change made");
            }
            else
            {
                await _barApiClient.DeleteDefaultChannelAsync(defaultChannel.Id);
            }
        }
    }

    private async Task SetDefaultChannelAsync(Flow flow)
    {
        console.MarkupLineInterpolated($"Only one branch should publish to a given channel, deleting other default channel associations for repository [teal]{flow.SourceRepoUrl}[/] and channel [teal]{flow.Channel}[/]");
        await DeleteDefaultChannelsAsync(
            repository: flow.SourceRepoUrl,
            branch: null,
            channel: flow.Channel);

        console.MarkupLineInterpolated($"Adding default channel [teal]{flow.Channel}[/] for repository [teal]{flow.SourceRepoUrl}[/] and branch [teal]{flow.SourceBranch}[/]");
        if (dryRun)
        {
            console.MarkupLineInterpolated($"[yellow]Dry run:[/] no change made");
        }
        else
        {
            await _barApiClient.AddDefaultChannelAsync(
                repository: flow.SourceRepoUrl,
                branch: flow.SourceBranch,
                channel: flow.Channel);
        }
    }

    private async Task CreateSubscriptionAsync(Flow flow)
    {
        var updateFrequency = UpdateFrequency.EveryDay;
        var targetDirectory = flow.TargetRepoUrl == VmrRepoUrl ? GitHubUtil.GetRepoNameFromUrl(flow.SourceRepoUrl) : "";
        var sourceDirectory = flow.SourceRepoUrl == VmrRepoUrl ? GitHubUtil.GetRepoNameFromUrl(flow.TargetRepoUrl) : "";
        var sourceEnabled = targetDirectory != "" || sourceDirectory != "";
        console.MarkupLineInterpolated($"Creating subscription (source repository: [teal]{flow.SourceRepoUrl}[/], channel: [teal]{flow.Channel}[/], target repository: [teal]{flow.TargetRepoUrl}[/], target branch: [teal]{flow.TargetBranch}[/], source enabled: [teal]{sourceEnabled}[/], source directory: [teal]'{sourceDirectory}'[/], target directory: [teal]'{targetDirectory}'[/])");
        if (dryRun)
        {
            console.MarkupLineInterpolated($"[yellow]Dry run:[/] no change made");
        }
        else
        {
            var subscription = await _barApiClient.CreateSubscriptionAsync(
                channelName: flow.Channel,
                sourceRepo: flow.SourceRepoUrl,
                targetRepo: flow.TargetRepoUrl,
                targetBranch: flow.TargetBranch,
                updateFrequency: updateFrequency.ToString(),
                batchable: false,
                mergePolicies: [],
                failureNotificationTags: "",
                sourceEnabled: sourceEnabled,
                sourceDirectory: sourceDirectory,
                targetDirectory: targetDirectory,
                excludedAssets: []);
            console.MarkupLineInterpolated($"=> [teal]{subscription.Id}[/]");
        }
    }

    private async Task<Subscription> FindSubscriptionAsync(Flow flow)
    {
        var subscriptions = await _barApiClient.GetSubscriptionsAsync(
            sourceRepo: flow.SourceRepoUrl,
            targetRepo: flow.TargetRepoUrl);
        return subscriptions.FirstOrDefault(s =>
            s.Channel.Name == flow.Channel &&
            s.TargetBranch == flow.TargetBranch)
            ?? throw new InvalidOperationException($"Subscription not found for flow {flow}");
    }

    private async Task UpdateSubscriptionAsync(Guid id, string channelName)
    {
        console.MarkupLineInterpolated($"Updating subscription [teal]{id}[/] to have channel [teal]{channelName}[/]");
        if (dryRun)
        {
            console.MarkupLineInterpolated($"[yellow]Dry run:[/] no change made");
        }
        else
        {
            await _barApiClient.UpdateSubscriptionAsync(id, new SubscriptionUpdate
            {
                ChannelName = channelName,
            });
        }
    }

    public async Task SetupNewFlowAsync(Flow flow)
    {
        await SetDefaultChannelAsync(flow);
        await CreateSubscriptionAsync(flow);
    }

    public async Task UpdateFlowAsync(Flow existingFlow, Flow expectedFlow)
    {
        if (existingFlow.SourceBranch != expectedFlow.SourceBranch ||
            existingFlow.Channel != expectedFlow.Channel)
        {
            await SetDefaultChannelAsync(expectedFlow);
        }

        if (existingFlow.Channel != expectedFlow.Channel)
        {
            if (existingFlow.TargetBranch == expectedFlow.TargetBranch)
            {
                var subscription = await FindSubscriptionAsync(existingFlow);
                await UpdateSubscriptionAsync(subscription.Id, expectedFlow.Channel);
            }
            else
            {
                await CreateSubscriptionAsync(expectedFlow);
            }
        }
        else if (existingFlow.TargetBranch != expectedFlow.TargetBranch)
        {
            await CreateSubscriptionAsync(expectedFlow);
        }
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

file sealed class ActionList(IAnsiConsole console)
{
    private readonly List<(string Name, Func<Task> Func)> _actions = [];

    public IAnsiConsole Console => console;

    public bool Add(string title, Func<Task> func)
    {
        if (console.Confirm($"[green]Add to plan:[/] {title}?", defaultValue: true))
        {
            var funcWithProgress = () =>
            {
                console.MarkupLine($"[yellow]Started:[/] {title}...");
                return func();
            };
            _actions.Add((title, funcWithProgress));
            return true;
        }

        return false;
    }

    public async Task PerformAllAsync()
    {
        if (_actions.Count == 0)
        {
            console.MarkupLine("[yellow]Aborted[/]: No actions added to plan");
        }
        else
        {
            var actionList = string.Join("\n", _actions.Select(static a => $"- {a.Name}"));
            if (console.Confirm($"[red]Perform actions added to plan?[/]\n{actionList}\nContinue?", defaultValue: true))
            {
                foreach (var action in _actions)
                {
                    try
                    {
                        await action.Func();
                    }
                    catch (Exception ex)
                    {
                        console.MarkupLineInterpolated($"[red]Error:[/] Action [teal]{Markup.Remove(action.Name)}[/] failed: {ex.ToString()}");
                        if (!console.Confirm($"Continue executing the remaining actions?", defaultValue: true))
                        {
                            return;
                        }
                    }
                }

                console.MarkupLine("[green]Done[/]");
            }
            else
            {
                console.MarkupLine("[yellow]Aborted[/]: No changes made");
            }
        }
    }
}

file sealed class ParsableTypeConverter<T> : TypeConverter where T : IParsable<T>
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    }

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is string s && T.TryParse(s, culture, out var result))
        {
            return result;
        }

        return base.ConvertFrom(context, culture, value);
    }
}
