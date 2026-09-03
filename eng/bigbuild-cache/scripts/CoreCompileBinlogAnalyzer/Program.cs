using System.Text.Json;
using Microsoft.Build.Logging;

if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: CoreCompileBinlogAnalyzer <binlog> <output-json>");
    return 1;
}

var result = Analyze(Path.GetFullPath(args[0]));
var outputPath = Path.GetFullPath(args[1]);
Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
File.WriteAllText(
    outputPath,
    JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));

Console.WriteLine($"[binlog] CoreCompile ran completely: {result.RanCompletely}");
Console.WriteLine($"[binlog] CoreCompile skipped up to date: {result.SkippedUpToDate}");
Console.WriteLine($"[binlog] wrote {outputPath}");
return 0;

static CoreCompileResult Analyze(string binlog)
{
    const string RunPrefix = "Building target \"CoreCompile\" completely";
    const string SkipPrefix = "Skipping target \"CoreCompile\" because all output files are up-to-date";

    var projects = new Dictionary<int, ProjectInfo>();
    var activeTargets = new Dictionary<(int ProjectContextId, int TargetId), CoreCompileProject>();
    var result = new CoreCompileResult();
    var replay = new BinaryLogReplayEventSource();

    replay.ProjectStarted += (_, e) =>
    {
        if (e.BuildEventContext is not { } context)
        {
            return;
        }

        var targetFramework = "";
        e.GlobalProperties?.TryGetValue("TargetFramework", out targetFramework);
        projects[context.ProjectContextId] = new ProjectInfo(
            e.ProjectFile ?? "",
            targetFramework ?? "");
    };

    replay.TargetStarted += (_, e) =>
    {
        if (e.TargetName != "CoreCompile" || e.BuildEventContext is not { } context)
        {
            return;
        }

        activeTargets[(context.ProjectContextId, context.TargetId)] =
            CreateEntry(e.ProjectFile, context.ProjectContextId, projects);
    };

    replay.MessageRaised += (_, e) =>
    {
        var message = e.Message ?? "";
        var ran = message.StartsWith(RunPrefix, StringComparison.Ordinal);
        var skipped = message.StartsWith(SkipPrefix, StringComparison.Ordinal);
        var context = e.BuildEventContext;
        var key = context is null
            ? ((int ProjectContextId, int TargetId)?)null
            : (context.ProjectContextId, context.TargetId);

        if (ran)
        {
            var entry = key is { } targetKey && activeTargets.TryGetValue(targetKey, out var active)
                ? active
                : CreateEntry(e.ProjectFile, context?.ProjectContextId, projects);
            result.RanCompletely++;
            result.Compiled.Add(entry);
            return;
        }

        if (skipped)
        {
            var entry = CreateEntry(e.ProjectFile, context?.ProjectContextId, projects);
            entry.Reasons.Add(message);
            result.SkippedUpToDate++;
            result.Skipped.Add(entry);
            return;
        }

        if (key is { } activeKey &&
            activeTargets.TryGetValue(activeKey, out var activeEntry) &&
            IsRebuildReason(message) &&
            !activeEntry.Reasons.Contains(message, StringComparer.Ordinal))
        {
            activeEntry.Reasons.Add(message);
        }
    };

    replay.TargetFinished += (_, e) =>
    {
        if (e.TargetName == "CoreCompile" && e.BuildEventContext is { } context)
        {
            activeTargets.Remove((context.ProjectContextId, context.TargetId));
        }
    };

    replay.Replay(binlog);
    return result;
}

static CoreCompileProject CreateEntry(
    string? projectFile,
    int? projectContextId,
    Dictionary<int, ProjectInfo> projects)
{
    var project = projectFile ?? "";
    var targetFramework = "";
    if (projectContextId is { } contextId && projects.TryGetValue(contextId, out var info))
    {
        project = info.Project;
        targetFramework = info.TargetFramework;
    }

    return new CoreCompileProject
    {
        Project = project,
        TargetFramework = targetFramework,
    };
}

static bool IsRebuildReason(string message) =>
    message.StartsWith("Input file ", StringComparison.Ordinal) ||
    message.StartsWith("Output file ", StringComparison.Ordinal) ||
    message.StartsWith("The input file ", StringComparison.Ordinal) ||
    message.Contains(" is newer than output file ", StringComparison.Ordinal) ||
    message.Contains(" does not exist", StringComparison.Ordinal);

internal sealed record ProjectInfo(string Project, string TargetFramework);

internal sealed class CoreCompileProject
{
    public string Project { get; set; } = "";

    public string TargetFramework { get; set; } = "";

    public List<string> Reasons { get; } = [];
}

internal sealed class CoreCompileResult
{
    public int RanCompletely { get; set; }

    public int SkippedUpToDate { get; set; }

    public List<CoreCompileProject> Compiled { get; } = [];

    public List<CoreCompileProject> Skipped { get; } = [];
}
