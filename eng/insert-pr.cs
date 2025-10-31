#!/usr/bin/env dotnet
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This script can be used to start the PR validation pipeline.

#:property PublishAot=false
#:package CliWrap
#:package Spectre.Console
#:package System.CommandLine
#:project ./utils

#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task

using CliWrap;
using Roslyn.Utils;
using Spectre.Console;
using System.Collections.Immutable;
using System.CommandLine;
using System.Diagnostics;

var console = AnsiConsole.Console;

// Setup audit logging.
var logger = new Logger(console, "insert-pr");

// Parse args.
var workDirOption = new Option<string>("--working-directory", "-C")
{
    DefaultValueFactory = _ => Environment.CurrentDirectory,
};
var waitForDebuggerOption = new Option<bool>("--wait-for-debugger");
var rootCommand = new RootCommand("Insert script")
{
    workDirOption,
    waitForDebuggerOption,
};
rootCommand.TreatUnmatchedTokensAsErrors = true;
var parsedArgs = rootCommand.Parse(args);
var argsParsingResult = parsedArgs.Invoke(); // validates args
if (argsParsingResult != 0)
{
    return argsParsingResult;
}
var workDir = parsedArgs.GetRequiredValue(workDirOption);
var waitForDebugger = parsedArgs.GetValue(waitForDebuggerOption);
console.MarkupLineInterpolated($"Working directory: [grey]{workDir}[/]");

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

console.WriteLine("Checking prerequisites...");

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

// Check that the `az` CLI is available.
bool azExists;
try
{
    azExists = 0 == (await Cli.Wrap("az")
        .WithArguments(["--version"])
        .WithValidation(CommandResultValidation.None)
        .ExecuteBufferedAsync(logger))
        .ExitCode;
}
catch (Exception ex)
{
    logger.Log(ex.ToString());
    azExists = false;
}
if (!azExists)
{
    console.MarkupLine("[red]Error:[/] Azure CLI 'az' is not installed or not available in PATH. Please install it from https://learn.microsoft.com/cli/azure/install-azure-cli.");
    return 1;
}

// Ensure the `az` `azure-devops` extension is installed.
console.WriteLine("Ensuring Azure DevOps extension is installed for 'az' CLI...");
await Cli.Wrap("az")
    .WithArguments(["extension", "add", "--name", "azure-devops"])
    .ExecuteBufferedAsync(logger);

// Determine PR in the working directory.
console.WriteLine("Determining PR in the working directory...");
var currentPrResult = await Cli.Wrap("gh")
    .WithWorkingDirectory(workDir)
    .WithArguments(["pr", "view", "--json", "number,headRefOid,title,commits"])
    .WithValidation(CommandResultValidation.None)
    .ExecuteBufferedAsync(logger);
var currentPr = currentPrResult.IsSuccess
    ? currentPrResult.StandardOutput.ParseJson<GitHubPr>()
    : null;

// Print PR info.

if (currentPr != null)
{
    console.MarkupLineInterpolated($"PR in working directory: [teal]#{currentPr.Number}[/]: [teal]{currentPr.Title}[/]");

    var latestCommit = currentPr.Commits.LastOrDefault(c => c.Oid == currentPr.HeadRefOid);
    if (latestCommit != null)
    {
        console.MarkupLineInterpolated($"Latest commit: [teal]{latestCommit.Oid}[/] ([teal]{latestCommit.CommittedDate.ToLocalTime()}[/]): [teal]{latestCommit.MessageHeadline}[/]");
    }
}
else
{
    console.MarkupLine("No PR detected in working directory.");
}

// Gather inputs.

var prNumber = console.Prompt(TextPrompt<int>.Create("PR number",
    defaultValueIfNotNull: currentPr?.Number));

var headCommit = console.Prompt(TextPrompt<string?>.CreateExt("Head commit SHA",
    defaultValueIfNotNull: currentPr?.HeadRefOid)
    .AllowEmpty());

var enforceLatestCommit = console.ConfirmEx("Enforce latest commit?", defaultValue: true);

var visualStudioBranchName = console.Prompt(TextPrompt<string>.Create("Visual Studio branch name", defaultValue: "default"));

var titlePrefix = console.Prompt(TextPrompt<string>.Create("Title prefix", defaultValue: $"[PR Validation {prNumber}]"));

var visualStudioCherryPickSha = console.Prompt(TextPrompt<string?>.CreateExt("Visual Studio cherry-pick commit SHA (leave empty to skip cherry-pick)",
    defaultValueIfNotNull: null)
    .AllowEmpty());

var skipApplyOptimizationData = console.ConfirmEx("Skip applying optimization data?", defaultValue: false);

// Start the pipeline.
console.WriteLine("Starting the pipeline...");
var result = (await Cli.Wrap("az")
    .WithArguments([
        "pipelines",
        "run",
        "--name", "Roslyn PR Validation",
        "--branch", "main",
        "--org", "https://dev.azure.com/devdiv",
        "--project", "DevDiv",
        "--parameters",
        $"PRNumber={prNumber}",
        .. (ReadOnlySpan<string>)(string.IsNullOrWhiteSpace(headCommit) ? [] : [$"CommitSHA={headCommit}"]),
        $"EnforceLatestCommit={(enforceLatestCommit ? "true" : "false")}",
        $"VisualStudioBranchName={visualStudioBranchName}",
        $"OptionalTitlePrefix={titlePrefix}",
        .. (ReadOnlySpan<string>)(string.IsNullOrWhiteSpace(visualStudioCherryPickSha) ? [] : [$"VisualStudioCherryPickSHA={visualStudioCherryPickSha}"]),
        "InsertToolset=true",
        $"SkipApplyOptimizationData={(skipApplyOptimizationData ? "true" : "false")}",
    ])
    .ExecuteBufferedAsync(logger))
    .StandardOutput
    .ParseJson<PipelineResult>()
    ?? throw new InvalidOperationException($"Null {nameof(PipelineResult)}");
console.MarkupLineInterpolated($"Started: [teal]https://devdiv.visualstudio.com/DevDiv/_build/results?buildId={result.Id}[/]");

return 0;

file sealed class GitHubPr
{
    public required int Number { get; init; }
    public required string HeadRefOid { get; init; }
    public required string Title { get; init; }
    public required ImmutableArray<GitCommit> Commits { get; init; }
}

file sealed class GitCommit
{
    public required string Oid { get; init; }
    public required string MessageHeadline { get; init; }
    public required DateTimeOffset CommittedDate { get; init; }
}

file sealed class PipelineResult
{
    public required int Id { get; init; }
}
