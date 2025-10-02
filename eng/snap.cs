#!/usr/bin/env dotnet
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Need this fix to delete the static graph disable: https://github.com/dotnet/sdk/pull/50532, 10.0.100-rc.2
#:property RestoreUseStaticGraphEvaluation=false
#:property PublishAot=false
#:package Spectre.Console
#:package CliWrap

#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task

using System.Diagnostics;
using System.Text.Json;
using CliWrap;
using CliWrap.Buffered;
using Spectre.Console;
using Spectre.Console.Rendering;

const string nextMilestoneName = "Next";

var console = AnsiConsole.Console;

// Setup audit logging.
var logFilePath = Path.Join(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "snap-script", "log.txt");
Directory.CreateDirectory(Path.GetDirectoryName(logFilePath)!);
// This is intentionally not disposed so it can be used in UnhandledException handler below.
var logWriter = new StreamWriter(File.Open(logFilePath, FileMode.Append, FileAccess.Write, FileShare.Read))
{
    AutoFlush = true
};
console.MarkupLineInterpolated($"Logging to [gray]{logFilePath}[/]");
logWriter.WriteLine();
log("Starting snap script run");

AppDomain.CurrentDomain.UnhandledException += (s, e) =>
{
    log($"Unhandled exception: {e.ExceptionObject}");
};

TaskScheduler.UnobservedTaskException += (s, e) =>
{
    log($"Unobserved task exception: {e.Exception}");
};

console.Pipeline.Attach(new LoggingRenderHook(logWriter));

// Get gh default repo.

var defaultRepo = (await Cli.Wrap("gh")
    .WithArguments(["repo", "set-default", "--view"])
    .ExecuteBufferedAsync())
    .StandardOutput
    .Trim();

if (string.IsNullOrEmpty(defaultRepo))
{
    console.MarkupLine("[red]error:[/] No default repo set for [gray]gh[/] CLI, please run [gray]gh repo set-default[/]");
    return 1;
}

console.MarkupLineInterpolated($"Default repo for [gray]gh[/] CLI is [teal]{defaultRepo}[/]");

// Ask for source and target branches.

var sourceBranchName = console.Prompt(new TextPrompt<string>("Source branch")
    .DefaultValue("main"));

var targetBranchName = console.Prompt(new TextPrompt<string>("Target branch")
    .DefaultValue("release/insiders"));

// Find last 5 PRs merged to current branch.

const string prJsonFields = "number,title,mergedAt,mergeCommit";

var lastMergedPullRequests = (await Cli.Wrap("gh")
    .WithArguments(["pr", "list",
        "--search", $"is:merged base:{sourceBranchName}",
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

var lastPrNumber = console.Prompt(new TextPrompt<int>("Number of last PR to include")
    .DefaultValueIfNotNull(lastMergedPullRequests is [var defaultLastPr, ..] ? defaultLastPr.Number : null));
var lastPr = lastMergedPullRequests.FirstOrDefault(pr => pr.Number == lastPrNumber)
    ?? (await Cli.Wrap("gh")
    .WithArguments(["pr", "view", $"{lastPrNumber}",
        "--json", prJsonFields])
    .ExecuteBufferedAsync())
    .StandardOutput
    .ParseJsonList<PullRequest>()
    ?.FirstOrDefault()
    ?? throw new InvalidOperationException($"Cannot find PR #{lastPrNumber}");

// Find PRs in milestone Next.

var searchFilter = $"is:merged milestone:{nextMilestoneName} base:{sourceBranchName}";
var milestonePullRequests = (await Cli.Wrap("gh")
    .WithArguments(["pr", "list",
        "--search", searchFilter,
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
    console.MarkupLineInterpolated($" - ... for more, run [gray]gh pr list --search '{searchFilter}'[/]");
}
if (milestonePullRequests.Length > 5)
{
    console.WriteLine($" - {milestonePullRequests[^1]}");
}
console.WriteLine();

if (milestonePullRequests is [var defaultLastMilestonePr, ..])
{
    // Determine last PR to include.

    var lastMilestonePr = milestonePullRequests.FirstOrDefault(pr => pr.Number == lastPr.Number);
    if (lastMilestonePr is null)
    {
        var lastMilestonePrNumber = console.Prompt(new TextPrompt<int>("Number of last PR to include")
            .DefaultValue(defaultLastMilestonePr.Number)
            .Validate(prNumber => milestonePullRequests.Any(pr => pr.Number == prNumber)
                ? ValidationResult.Success()
                : ValidationResult.Error($"No PR with number {prNumber} found in milestone {nextMilestoneName}")));
        lastMilestonePr = milestonePullRequests.First(pr => pr.Number == lastPrNumber);
    }
    var lastMilestonePrIndex = milestonePullRequests.IndexOf(lastMilestonePr);
    Debug.Assert(lastMilestonePrIndex >= 0);

    // Find all milestones.

    var milestones = (await Cli.Wrap("gh")
        .WithArguments(["api", "repos/{owner}/{repo}/milestones", "--paginate", "--jq", ".[] | {number:.number,title:.title}"])
        .ExecuteBufferedAsync())
        .StandardOutput
        .ParseJsonNewLineDelimitedList<Milestone>()
        .AssertNonNullElements("Null milestone in list")
        .OrderByDescending(static m => m.Number)
        .ToArray();

    if (milestones is not [var newestMilestone, ..])
    {
        console.MarkupLine("[red]error:[/] No milestones found in repo");
    }
    else
    {
        // Determine target milestone.

        var targetMilestone = console.Prompt(new TextPrompt<string>("Target milestone")
            .DefaultValue(newestMilestone.Title));

        // TODO: Schedule to move PRs to the selected milestone.
        console.MarkupLineInterpolated($"[blue]Added to plan:[/] Move [teal]{milestonePullRequests.Length - lastMilestonePrIndex}[/] PRs to milestone [teal]{targetMilestone}[/]");
    }
}

return 0;

void log(string message)
{
    logWriter.WriteLine($"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss K}] {message}");
}

file sealed record PullRequest(int Number, string Title, DateTimeOffset MergedAt, Commit MergeCommit)
{
    public override string ToString() => $"#{Number}: {Title} ({MergedAt})";
}

file sealed record Commit(string Oid)
{
    public override string ToString() => Oid;
}

file sealed record Milestone(int Number, string Title)
{
    public override string ToString() => Title;
}

file static class Extensions
{
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

    extension(string s)
    {
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
    }

    extension<T>(TextPrompt<T> prompt) where T : struct
    {
        public TextPrompt<T> DefaultValueIfNotNull(T? value)
        {
            return value is { } v ? prompt.DefaultValue(v) : prompt;
        }
    }
}

file class LoggingRenderHook(StreamWriter logWriter) : IRenderHook
{
    private long _lastOffset;

    public IEnumerable<IRenderable> Process(RenderOptions options, IEnumerable<IRenderable> renderables)
    {
        // Timestamp will be added on each intercepted newline,
        // but not if someone from the outside wrote to the log in the meantime.
        if (_lastOffset != logWriter.BaseStream.Position)
        {
            logWriter.Write($"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss K}] ");
        }

        foreach (var renderable in renderables)
        {
            var segments = renderable.Render(options, int.MaxValue).ToArray();
            var text = string.Concat(segments.Select(static s => s.Text));
            text = text.ReplaceLineEndings($"\n[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss K}] ");
            logWriter.Write(text);
        }

        _lastOffset = logWriter.BaseStream.Position;

        return renderables;
    }
}
