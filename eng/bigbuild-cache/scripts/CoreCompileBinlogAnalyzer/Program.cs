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

    replay.MessageRaised += (_, e) =>
    {
        var message = e.Message ?? "";
        var ran = message.StartsWith(RunPrefix, StringComparison.Ordinal);
        var skipped = message.StartsWith(SkipPrefix, StringComparison.Ordinal);
        if (!ran && !skipped)
        {
            return;
        }

        var project = e.ProjectFile ?? "";
        var targetFramework = "";
        if (e.BuildEventContext is { } context &&
            projects.TryGetValue(context.ProjectContextId, out var info))
        {
            project = info.Project;
            targetFramework = info.TargetFramework;
        }

        var entry = new CoreCompileProject(project, targetFramework, message);
        if (ran)
        {
            result.RanCompletely++;
            result.Compiled.Add(entry);
        }
        else
        {
            result.SkippedUpToDate++;
            result.Skipped.Add(entry);
        }
    };

    replay.Replay(binlog);
    return result;
}

internal sealed record ProjectInfo(string Project, string TargetFramework);

internal sealed record CoreCompileProject(string Project, string TargetFramework, string Reason);

internal sealed class CoreCompileResult
{
    public int RanCompletely { get; set; }

    public int SkippedUpToDate { get; set; }

    public List<CoreCompileProject> Compiled { get; } = [];

    public List<CoreCompileProject> Skipped { get; } = [];
}
