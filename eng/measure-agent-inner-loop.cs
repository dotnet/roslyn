#!/usr/bin/env dotnet
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;

var options = ParseArguments(args);
var repoDirectory = GetRepositoryDirectory();
var slices = new Dictionary<string, SliceDefinition>(StringComparer.OrdinalIgnoreCase)
{
    ["CSharpCodeStyle"] = new(
        ProductProject: Path.Join(
            "src", "CodeStyle", "CSharp", "CodeFixes",
            "Microsoft.CodeAnalysis.CSharp.CodeStyle.Fixes.csproj"),
        TestProject: Path.Join(
            "src", "CodeStyle", "CSharp", "Tests",
            "Microsoft.CodeAnalysis.CSharp.CodeStyle.UnitTests.csproj"),
        RepresentativeSource: Path.Join(
            "src", "Analyzers", "CSharp", "Analyzers", "AddRequiredParentheses",
            "CSharpAddRequiredPatternParenthesesDiagnosticAnalyzer.cs"),
        TargetFrameworkProperty: "NetRoslyn",
        TestFilter: "FullyQualifiedName~Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.AddRequiredParentheses.AddRequiredPatternParenthesesTests"),
    ["CSharpFormatting"] = new(
        ProductProject: Path.Join(
            "src", "Workspaces", "CSharp", "Portable",
            "Microsoft.CodeAnalysis.CSharp.Workspaces.csproj"),
        TestProject: Path.Join(
            "src", "Workspaces", "CSharpTest",
            "Microsoft.CodeAnalysis.CSharp.Workspaces.UnitTests.csproj"),
        RepresentativeSource: Path.Join(
            "src", "Workspaces", "SharedUtilitiesAndExtensions", "Workspace", "CSharp",
            "Formatting", "CSharpSyntaxFormattingService.cs"),
        TargetFrameworkProperty: "NetVSShared",
        TestFilter: "FullyQualifiedName=Microsoft.CodeAnalysis.CSharp.UnitTests.Formatting.FormattingTests.Format1"),
};

var supportedSlices = slices.Keys.Order(StringComparer.Ordinal).ToArray();
if (options.Help)
{
    PrintUsage(supportedSlices);
    return 0;
}

if (string.IsNullOrWhiteSpace(options.Slice))
{
    PrintUsage(supportedSlices);
    return Fail("Specify a slice with --slice.");
}

if (!slices.TryGetValue(options.Slice, out var sliceDefinition))
{
    PrintUsage(supportedSlices);
    return Fail($"Unknown slice '{options.Slice}'.");
}

var sliceName = supportedSlices.Single(name => name.Equals(options.Slice, StringComparison.OrdinalIgnoreCase));
var dotnetExecutable = "dotnet";
var testProjectPath = Path.Join(repoDirectory, sliceDefinition.TestProject);
var targetFrameworkOutput = RunDotNetAndCapture(
    dotnetExecutable,
    repoDirectory,
    ["msbuild", testProjectPath, $"-getProperty:{sliceDefinition.TargetFrameworkProperty}", "-nologo"]);
if (targetFrameworkOutput.ExitCode != 0)
{
    return Fail($"Could not determine {sliceDefinition.TargetFrameworkProperty} for {sliceDefinition.TestProject}.");
}

var targetFramework = targetFrameworkOutput.StandardOutput
    .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    .LastOrDefault();
if (string.IsNullOrWhiteSpace(targetFramework))
{
    return Fail($"Could not determine {sliceDefinition.TargetFrameworkProperty} for {sliceDefinition.TestProject}.");
}

var testFilter = string.IsNullOrWhiteSpace(options.TestFilter)
    ? sliceDefinition.TestFilter
    : options.TestFilter;
var outputPath = string.IsNullOrWhiteSpace(options.OutputPath)
    ? Path.Join(
        repoDirectory,
        "artifacts",
        "log",
        $"agent-inner-loop-{sliceName}-{DateTime.Now:yyyyMMdd-HHmmss}.json")
    : Path.GetFullPath(options.OutputPath, repoDirectory);

var measurements = new List<Measurement>();
var testResultsDirectory = Path.Join(repoDirectory, "artifacts", "TestResults", "AgentInnerLoop");
var representativeSourcePath = Path.Join(repoDirectory, sliceDefinition.RepresentativeSource);
var originalRepresentativeSourceLastWriteTimeUtc = File.GetLastWriteTimeUtc(representativeSourcePath);

try
{
    if (!options.SkipPreparation)
    {
        measurements.Add(RunMeasuredDotNet(
            "Restore test graph",
            ["restore", sliceDefinition.TestProject, "--nologo", "--verbosity", "minimal"]));

        measurements.Add(RunMeasuredDotNet(
            "Initial product build",
            [
                "build", sliceDefinition.ProductProject,
                "--configuration", options.Configuration,
                "--no-restore",
                "--nologo",
                "--verbosity", "minimal",
            ]));

        measurements.Add(RunMeasuredDotNet(
            "Prepare test project",
            [
                "build", sliceDefinition.TestProject,
                "--configuration", options.Configuration,
                "--no-restore",
                "--framework", targetFramework,
                "--nologo",
                "--verbosity", "minimal",
            ]));
    }

    for (var iteration = 1; iteration <= options.Iterations; iteration++)
    {
        File.SetLastWriteTimeUtc(representativeSourcePath, DateTime.UtcNow);

        measurements.Add(RunMeasuredDotNet(
            $"Representative edit validation build {iteration}",
            [
                "build", sliceDefinition.TestProject,
                "--configuration", options.Configuration,
                "--no-restore",
                "--framework", targetFramework,
                "--nologo",
                "--verbosity", "minimal",
            ]));

        Directory.CreateDirectory(testResultsDirectory);
        var trxFileName = $"{sliceName}-{iteration}.trx";
        var trxPath = Path.Join(testResultsDirectory, trxFileName);
        File.Delete(trxPath);

        measurements.Add(RunMeasuredDotNet(
            $"Filtered test {iteration}",
            [
                "test", sliceDefinition.TestProject,
                "--configuration", options.Configuration,
                "--framework", targetFramework,
                "--no-build",
                "--no-restore",
                "--filter", testFilter,
                "--logger", $"trx;LogFileName={trxFileName}",
                "--results-directory", testResultsDirectory,
                "--nologo",
                "--verbosity", "minimal",
            ],
            trxPath));
    }
}
finally
{
    File.SetLastWriteTimeUtc(representativeSourcePath, originalRepresentativeSourceLastWriteTimeUtc);
}

var validationBuildDurations = measurements
    .Where(measurement => measurement.Name.StartsWith("Representative edit validation build ", StringComparison.Ordinal))
    .Select(measurement => measurement.DurationSeconds)
    .ToArray();
var filteredTestDurations = measurements
    .Where(measurement => measurement.Name.StartsWith("Filtered test ", StringComparison.Ordinal))
    .Select(measurement => measurement.DurationSeconds)
    .ToArray();
var dotnetVersion = RunDotNetAndCapture(dotnetExecutable, repoDirectory, ["--version"]);

var result = new MeasurementResult(
    Slice: sliceName,
    Configuration: options.Configuration,
    Iterations: options.Iterations,
    ProductProject: sliceDefinition.ProductProject,
    TestProject: sliceDefinition.TestProject,
    RepresentativeSource: sliceDefinition.RepresentativeSource,
    TargetFrameworkProperty: sliceDefinition.TargetFrameworkProperty,
    TargetFramework: targetFramework,
    RepresentativeTestFilter: sliceDefinition.TestFilter,
    TestFilter: testFilter,
    TimestampUtc: DateTime.UtcNow,
    Machine: new MachineInfo(
        OS: Environment.OSVersion.VersionString,
        ProcessorCount: Environment.ProcessorCount,
        DotNetVersion: dotnetVersion.StandardOutput.Trim()),
    Summary: new MeasurementSummary(
        RestoreSeconds: GetMeasurementDuration("Restore test graph"),
        InitialProductBuildSeconds: GetMeasurementDuration("Initial product build"),
        TestProjectPreparationSeconds: GetMeasurementDuration("Prepare test project"),
        RepresentativeEditValidationBuildMedianSeconds: Math.Round(GetMedian(validationBuildDurations), 3),
        FilteredTestMedianSeconds: Math.Round(GetMedian(filteredTestDurations), 3)),
    Measurements: measurements);

Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
await File.WriteAllTextAsync(
    outputPath,
    JsonSerializer.Serialize(result, AgentInnerLoopJsonContext.Default.MeasurementResult));

Console.WriteLine();
Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("=== Summary ===");
Console.ResetColor();
Console.WriteLine($"Representative edit validation build median: {result.Summary.RepresentativeEditValidationBuildMedianSeconds} seconds");
Console.WriteLine($"Filtered test median:                       {result.Summary.FilteredTestMedianSeconds} seconds");
Console.WriteLine($"Results: {outputPath}");
return 0;

Measurement RunMeasuredDotNet(string name, IReadOnlyList<string> arguments, string? testResultPath = null)
{
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine($"=== {name} ===");
    Console.ResetColor();
    Console.WriteLine($"dotnet {string.Join(' ', arguments)}");

    var stopwatch = Stopwatch.StartNew();
    var exitCode = RunProcess(dotnetExecutable, repoDirectory, arguments);
    stopwatch.Stop();

    var measurement = new Measurement(
        Name: name,
        DurationSeconds: Math.Round(stopwatch.Elapsed.TotalSeconds, 3),
        ExitCode: exitCode,
        Command: $"dotnet {string.Join(' ', arguments)}");

    if (exitCode != 0)
        throw new InvalidOperationException($"{name} failed with exit code {exitCode}.");

    if (testResultPath is not null)
    {
        if (!File.Exists(testResultPath))
            throw new InvalidOperationException($"{name} did not produce the expected TRX result file: {testResultPath}");

        var counters = XDocument.Load(testResultPath)
            .Descendants()
            .Single(element => element.Name.LocalName == "Counters");
        measurement.TestsExecuted = int.Parse(counters.Attribute("total")!.Value);
        if (measurement.TestsExecuted == 0)
            throw new InvalidOperationException($"{name} completed without executing any tests. Check the test filter.");
    }

    return measurement;
}

double? GetMeasurementDuration(string name)
    => measurements.SingleOrDefault(measurement => measurement.Name == name)?.DurationSeconds;

static Options ParseArguments(string[] arguments)
{
    var options = new Options();
    for (var i = 0; i < arguments.Length; i++)
    {
        switch (arguments[i])
        {
            case "--slice":
            case "-slice":
                options.Slice = ReadValue(arguments, ref i);
                break;
            case "--iterations":
            case "-iterations":
                if (!int.TryParse(ReadValue(arguments, ref i), out var iterations) || iterations is < 1 or > 20)
                    throw new ArgumentException("--iterations must be an integer from 1 through 20.");

                options.Iterations = iterations;
                break;
            case "--configuration":
            case "-configuration":
                options.Configuration = ReadValue(arguments, ref i);
                if (options.Configuration is not ("Debug" or "Release"))
                    throw new ArgumentException("--configuration must be Debug or Release.");

                break;
            case "--test-filter":
            case "-testFilter":
                options.TestFilter = ReadValue(arguments, ref i);
                break;
            case "--output-path":
            case "-outputPath":
                options.OutputPath = ReadValue(arguments, ref i);
                break;
            case "--skip-preparation":
            case "-skipPreparation":
                options.SkipPreparation = true;
                break;
            case "--help":
            case "-help":
            case "-h":
                options.Help = true;
                break;
            default:
                throw new ArgumentException($"Unknown argument: {arguments[i]}");
        }
    }

    return options;
}

static string ReadValue(string[] arguments, ref int index)
{
    if (++index >= arguments.Length)
        throw new ArgumentException($"Missing value for {arguments[index - 1]}.");

    return arguments[index];
}

static string GetRepositoryDirectory()
{
    if (AppContext.GetData("EntryPointFilePath") is not string sourceFilePath ||
        Path.GetDirectoryName(sourceFilePath) is not string engDirectory ||
        Path.GetDirectoryName(engDirectory) is not string repoDirectory ||
        !File.Exists(Path.Join(repoDirectory, "eng", Path.GetFileName(sourceFilePath))))
    {
        throw new InvalidOperationException(
            "Could not determine the source file path. This file-based app must be located in the 'eng' directory of the Roslyn repo.");
    }

    return repoDirectory;
}

static int RunProcess(string fileName, string workingDirectory, IReadOnlyList<string> arguments)
{
    using var process = new Process
    {
        StartInfo = CreateProcessStartInfo(fileName, workingDirectory, arguments, redirectOutput: false),
    };

    process.Start();
    process.WaitForExit();
    return process.ExitCode;
}

static ProcessOutput RunDotNetAndCapture(
    string dotnetExecutable,
    string workingDirectory,
    IReadOnlyList<string> arguments)
{
    using var process = new Process
    {
        StartInfo = CreateProcessStartInfo(dotnetExecutable, workingDirectory, arguments, redirectOutput: true),
    };

    process.Start();
    var standardOutput = process.StandardOutput.ReadToEnd();
    var standardError = process.StandardError.ReadToEnd();
    process.WaitForExit();
    if (standardError.Length > 0)
        Console.Error.Write(standardError);

    return new ProcessOutput(process.ExitCode, standardOutput);
}

static ProcessStartInfo CreateProcessStartInfo(
    string fileName,
    string workingDirectory,
    IReadOnlyList<string> arguments,
    bool redirectOutput)
{
    var startInfo = new ProcessStartInfo
    {
        FileName = fileName,
        WorkingDirectory = workingDirectory,
        UseShellExecute = false,
        RedirectStandardOutput = redirectOutput,
        RedirectStandardError = redirectOutput,
    };

    foreach (var argument in arguments)
        startInfo.ArgumentList.Add(argument);

    return startInfo;
}

static double GetMedian(double[] values)
{
    Array.Sort(values);
    var middle = values.Length / 2;
    return values.Length % 2 == 1
        ? values[middle]
        : (values[middle - 1] + values[middle]) / 2;
}

static void PrintUsage(IEnumerable<string> supportedSlices)
{
    Console.WriteLine("Usage: dotnet run --file eng/measure-agent-inner-loop.cs -- --slice <name> [--iterations 3]");
    Console.WriteLine("       [--configuration Debug] [--test-filter <filter>] [--output-path <path>]");
    Console.WriteLine("       [--skip-preparation]");
    Console.WriteLine();
    Console.WriteLine("Measures product/test preparation, representative edit validation builds, and");
    Console.WriteLine("filtered tests for a documented agent inner-loop slice.");
    Console.WriteLine();
    Console.WriteLine($"Supported slices: {string.Join(", ", supportedSlices)}");
    Console.WriteLine();
    Console.WriteLine("Use --skip-preparation only when the product and test projects are already built.");
}

static int Fail(string message)
{
    Console.Error.WriteLine(message);
    return 1;
}

sealed record SliceDefinition(
    string ProductProject,
    string TestProject,
    string RepresentativeSource,
    string TargetFrameworkProperty,
    string TestFilter);

sealed class Options
{
    public string Slice { get; set; } = "";
    public int Iterations { get; set; } = 3;
    public string Configuration { get; set; } = "Debug";
    public string TestFilter { get; set; } = "";
    public string OutputPath { get; set; } = "";
    public bool SkipPreparation { get; set; }
    public bool Help { get; set; }
}

sealed record ProcessOutput(int ExitCode, string StandardOutput);

sealed class Measurement(string Name, double DurationSeconds, int ExitCode, string Command)
{
    public string Name { get; } = Name;
    public double DurationSeconds { get; } = DurationSeconds;
    public int ExitCode { get; } = ExitCode;
    public string Command { get; } = Command;
    public int? TestsExecuted { get; set; }
}

sealed record MachineInfo(string OS, int ProcessorCount, string DotNetVersion);

sealed record MeasurementSummary(
    double? RestoreSeconds,
    double? InitialProductBuildSeconds,
    double? TestProjectPreparationSeconds,
    double RepresentativeEditValidationBuildMedianSeconds,
    double FilteredTestMedianSeconds);

sealed record MeasurementResult(
    string Slice,
    string Configuration,
    int Iterations,
    string ProductProject,
    string TestProject,
    string RepresentativeSource,
    string TargetFrameworkProperty,
    string TargetFramework,
    string RepresentativeTestFilter,
    string TestFilter,
    DateTime TimestampUtc,
    MachineInfo Machine,
    MeasurementSummary Summary,
    IReadOnlyList<Measurement> Measurements);

[JsonSerializable(typeof(MeasurementResult))]
partial class AgentInnerLoopJsonContext : JsonSerializerContext;
