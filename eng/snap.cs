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

var console = AnsiConsole.Console;

// Setup audit logging.
var logFilePath = Path.Join(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "snap-script", "log.txt");
Directory.CreateDirectory(Path.GetDirectoryName(logFilePath)!);
using var logWriter = new StreamWriter(File.Open(logFilePath, FileMode.Append, FileAccess.Write, FileShare.Read))
{
    AutoFlush = true
};
console.MarkupLineInterpolated($"Logging to [gray]{logFilePath}[/]");
log("Starting snap script run");

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

// Get current branch.

var currentBranchName = (await Cli.Wrap("git")
    .WithArguments(["branch", "--show-current"])
    .ExecuteBufferedAsync())
    .StandardOutput
    .Trim();

console.MarkupLineInterpolated($"Current branch is [teal]{currentBranchName}[/], last commit:");

// Get last commit.

var lastCommit = (await Cli.Wrap("git")
    .WithArguments(["log", "-1"])
    .ExecuteBufferedAsync())
    .StandardOutput
    .Trim();

console.MarkupLineInterpolated($"[grey]{lastCommit}[/]");
console.WriteLine();

// Find PRs in milestone Next.

var nextMilestonePullRequests = (await Cli.Wrap("gh")
    .WithArguments(["pr", "list", "--search", "is:merged milestone:Next", "--json", "number,title,mergedAt"])
    .ExecuteBufferedAsync())
    .StandardOutput
    .ParseJsonList(new { Number = 0, Title = "", MergedAt = default(DateTimeOffset) })
    ?.OrderByDescending(static pr => pr.MergedAt)
    .ToArray()
    ?? throw new InvalidOperationException("Null PR list in milestone Next");

var formatPr = nextMilestonePullRequests.Func(pr => $"#{pr.Number}: {pr.Title} ({pr.MergedAt})");

console.MarkupLineInterpolated($"Found PRs in milestone Next ([teal]{nextMilestonePullRequests.Length}[/])");
foreach (var pr in nextMilestonePullRequests.Take(5))
{
    console.MarkupLineInterpolated($" - {formatPr(pr)}");
}
if (nextMilestonePullRequests.Length > 6)
{
    console.MarkupLine(" - ... for more, run [gray]gh pr list --search 'is:merged milestone:Next'[/]");
}
if (nextMilestonePullRequests.Length > 5)
{
    console.MarkupLineInterpolated($" - {formatPr(nextMilestonePullRequests[^1])}");
}
console.WriteLine();

if (nextMilestonePullRequests is [var defaultLastPr, ..])
{
    // Determine last PR to include.

    var lastPrNumber = console.Prompt(new TextPrompt<int>("Number of last PR to include")
        .DefaultValue(defaultLastPr.Number)
        .Validate(prNumber => nextMilestonePullRequests.Any(pr => pr.Number == prNumber)
            ? ValidationResult.Success()
            : ValidationResult.Error($"No PR with number {prNumber} found in milestone Next")));
    var lastPrIndex = nextMilestonePullRequests.IndexOf(nextMilestonePullRequests.First(pr => pr.Number == lastPrNumber));
    Debug.Assert(lastPrIndex >= 0);

    // Find all milestones.

    var milestones = (await Cli.Wrap("gh")
        .WithArguments(["api", "repos/{owner}/{repo}/milestones", "--jq", ".[] | {number:.number,title:.title}"])
        .ExecuteBufferedAsync())
        .StandardOutput
        .ParseJsonNewLineDelimitedList(new { Number = 0, Title = "" })
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
        console.MarkupLineInterpolated($"[blue]Added to plan:[/] Move [teal]{nextMilestonePullRequests.Length - lastPrIndex}[/] PRs to milestone [teal]{targetMilestone}[/]");
    }
}

return 0;

var remoteNames = (await Cli.Wrap("git")
    .WithArguments(["remote"])
    .ExecuteBufferedAsync())
    .StandardOutput
    .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
var defaultRemoteName = remoteNames.FirstOrDefault(static n => n is "upstream")
    ?? remoteNames.FirstOrDefault(static n => n is "origin")
    ?? remoteNames.FirstOrDefault(static n => n is "dotnet")
    ?? remoteNames.FirstOrDefault()
    ?? throw new InvalidOperationException("No git remotes found");
var remoteName = console.Prompt(new TextPrompt<string>("Remote to use")
    .AddChoices(remoteNames)
    .DefaultValue(defaultRemoteName));

var remoteUrl = (await Cli.Wrap("git")
    .WithArguments(["remote", "get-url", remoteName])
    .ExecuteBufferedAsync())
    .StandardOutput;

console.MarkupLine($"Using remote [green]{remoteName}[/] with URL [gray]{remoteUrl}[/]");

// var sourceBranch = console.Prompt(new TextPrompt<string>("Source branch").DefaultValue("main"));
// var targetBranch = console.Prompt(new TextPrompt<string>("Target branch").DefaultValue("release/insiders"));

void log(string message)
{
    logWriter.WriteLine($"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss K}] {message}");
}

file static class Extensions
{
    extension<T>(IEnumerable<T> _)
    {
        public Func<T, TResult> Func<TResult>(Func<T, TResult> f) => f;
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

    extension(string s)
    {
        public T[]? ParseJsonList<T>(T _)
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

        public List<T?> ParseJsonNewLineDelimitedList<T>(T _)
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
}

file class LoggingRenderHook(StreamWriter logWriter) : IRenderHook
{
    public IEnumerable<IRenderable> Process(RenderOptions options, IEnumerable<IRenderable> renderables)
    {
        foreach (var renderable in renderables)
        {
            var segments = renderable.Render(options, int.MaxValue).ToArray();
            var text = string.Concat(segments.Select(static s => s.Text));
            logWriter.WriteLine($"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss K}] {text.TrimEnd()}");
        }

        return renderables;
    }
}
