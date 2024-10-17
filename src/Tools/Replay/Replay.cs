// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Basic.CompilerLog.Util;
using Microsoft.CodeAnalysis.CommandLine;
using Microsoft.CodeAnalysis.Options;
using Mono.Options;

var options = ParseOptions(args);
if (Directory.Exists(options.OutputDirectory))
{
    Directory.Delete(options.OutputDirectory, recursive: true);
}
Directory.CreateDirectory(options.OutputDirectory);
Directory.CreateDirectory(options.TempDirectory);

return await RunAsync(options).ConfigureAwait(false);

static ReplayOptions ParseOptions(string[] args)
{
    string? outputDirectory = null;
    string? binlogPath = null;
    int maxParallel = 6;
    bool wait = false;
    int iterations = 1;

    var options = new Mono.Options.OptionSet()
    {
        { "b|binlogPath=", "The binary log to replay", v => binlogPath = v },
        { "o|outputDirectory=", "The directory to output the build results", v => outputDirectory = v },
        { "m|maxParallel=", "The maximum number of parallel builds", (int v) => maxParallel = v },
        { "w|wait", "Wait for a key press after starting the server", o => wait = o is object },
        { "i|iterations=", "Perform the compilation multiple times", (int v) => iterations = v },
    };

    var rest = options.Parse(args);
    if (rest.Count == 1)
    {
        binlogPath = rest[0];
    }
    else if (rest.Count > 1)
    {
        throw new Exception($"Too many arguments: {string.Join(" ", rest)}");
    }

    if (string.IsNullOrEmpty(binlogPath))
    {
        throw new Exception("Missing binlogPath");
    }

    if (string.IsNullOrEmpty(outputDirectory))
    {
        outputDirectory = Path.Combine(Path.GetTempPath(), "replay");
    }

    return new ReplayOptions(
        PipeName: Guid.NewGuid().ToString(),
        ClientDirectory: AppDomain.CurrentDomain.BaseDirectory!,
        OutputDirectory: outputDirectory,
        BinlogPath: binlogPath,
        MaxParallel: maxParallel,
        Wait: wait,
        Iterations: iterations);
}

static async Task<int> RunAsync(ReplayOptions options)
{
    Console.WriteLine($"Binary Log: {options.BinlogPath}");
    Console.WriteLine($"Client Directory: {options.ClientDirectory}");
    Console.WriteLine($"Output Directory: {options.OutputDirectory}");
    Console.WriteLine($"Pipe Name: {options.PipeName}");
    Console.WriteLine($"Parallel: {options.MaxParallel}");
    Console.WriteLine($"Iterations: {options.Iterations}");
    Console.WriteLine();
    Console.WriteLine("Starting server");

    using var compilerServerLogger = new CompilerServerLogger("replay", Path.Combine(options.OutputDirectory, "server.log"));
    if (!BuildServerConnection.TryCreateServer(options.ClientDirectory, options.PipeName, compilerServerLogger, out int serverProcessId))
    {
        Console.WriteLine("Failed to start server");
        return 1;
    }

    Console.WriteLine($"Process Id: {serverProcessId}");
    if (options.Wait)
    {
        Console.WriteLine("Press any key to continue");
        Console.ReadKey(intercept: true);
        Console.WriteLine("Continuing");
    }

    try
    {
        for (var i = 0; i < options.Iterations; i++)
        {
            Console.WriteLine();
            Console.WriteLine($"Iteration: {i + 1}");
            var compilerCalls = ReadAllCompilerCalls(options.BinlogPath);
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            await foreach (var buildData in BuildAllAsync(options, compilerCalls, compilerServerLogger, CancellationToken.None))
            {
                Console.WriteLine($"{buildData.CompilerCall.GetDiagnosticName()} ... {buildData.BuildResponse.Type}");
            }

            stopwatch.Stop();
            Console.WriteLine($"Pipe Name: {options.PipeName}");
            Console.WriteLine($"Compilation Events: {compilerCalls.Count}");
            Console.WriteLine($"Time: {stopwatch.Elapsed:mm\\:ss}");
        }

        return 0;
    }
    finally
    {
        Console.WriteLine("Shutting down server");

        await BuildServerConnection.RunServerShutdownRequestAsync(
            options.PipeName,
            timeoutOverride: null,
            waitForProcess: true,
            compilerServerLogger,
            cancellationToken: CancellationToken.None).ConfigureAwait(false);
    }
}

static List<CompilerCall> ReadAllCompilerCalls(string binlogPath)
{
    using var stream = new FileStream(binlogPath, FileMode.Open, FileAccess.Read, FileShare.Read);
    return BinaryLogUtil.ReadAllCompilerCalls(stream, new List<string>());
}

static async IAsyncEnumerable<BuildData> BuildAllAsync(
    ReplayOptions options,
    List<CompilerCall> compilerCalls,
    CompilerServerLogger compilerServerLogger,
    [EnumeratorCancellation] CancellationToken cancellationToken)
{
    var index = 0;
    var maxParallel = options.MaxParallel;
    var tasks = new List<Task<BuildData>>(capacity: maxParallel);
    var outputSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    do
    {
        while (tasks.Count < maxParallel && index < compilerCalls.Count)
        {
            var compilerCall = compilerCalls[index];
            tasks.Add(BuildAsync(options, compilerCall, GetOutputName(compilerCall), compilerServerLogger, cancellationToken));
            index++;
        }

        var completedTask = await Task.WhenAny(tasks).ConfigureAwait(false);
        if (!tasks.Remove(completedTask))
        {
            throw new Exception("Task was not in the list");
        }

        var buildData = await completedTask.ConfigureAwait(false);
        yield return buildData;
    } while (index < compilerCalls.Count);

    string GetOutputName(CompilerCall compilerCall)
    {
        string name;
        if (compilerCall.Kind is CompilerCallKind.Regular)
        {
            name = $"{compilerCall.ProjectFileName}-{compilerCall.TargetFramework}";
        }
        else
        {
            name = $"{compilerCall.ProjectFileName}-{compilerCall.TargetFramework}-{compilerCall.Kind}";
        }

        if (!outputSet.Add(name))
        {
            name = $"{name}-{outputSet.Count}";
        }

        return name;
    }
}

static async Task<BuildData> BuildAsync(
    ReplayOptions options,
    CompilerCall compilerCall,
    string outputName,
    CompilerServerLogger compilerServerLogger,
    CancellationToken cancellationToken)
{
    var args = compilerCall.GetArguments();
    var outputDirectory = Path.Combine(options.OutputDirectory, outputName);
    Directory.CreateDirectory(outputDirectory);
    RewriteOutputPaths(outputDirectory, args);

    var request = BuildServerConnection.CreateBuildRequest(
        outputName,
        compilerCall.IsCSharp ? RequestLanguage.CSharpCompile : RequestLanguage.VisualBasicCompile,
        args.ToList(),
        workingDirectory: compilerCall.ProjectDirectory,
        tempDirectory: options.TempDirectory,
        keepAlive: null,
        libDirectory: null);
    var response = await BuildServerConnection.RunServerBuildRequestAsync(
        request,
        options.PipeName,
        options.ClientDirectory,
        compilerServerLogger,
        cancellationToken).ConfigureAwait(false);
    return new BuildData(compilerCall, response);
}

static void RewriteOutputPaths(string outputDirectory, string[] args)
{
    var comparison = StringComparison.OrdinalIgnoreCase;
    var hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    for (var i = 0; i < args.Length; i++)
    {
        var line = args[i];
        var isOption = line.Length > 0 && line[0] is '-' or '/';
        if (!isOption)
        {
            continue;
        }

        line = line.Substring(1);
        // Map all of the output items to the build output directory
        if (line.StartsWith("out", comparison) ||
            line.StartsWith("refout", comparison) ||
            line.StartsWith("doc", comparison) ||
            line.StartsWith("errorlog", comparison))
        {
            var index = line.IndexOf(':');
            var argValue = line.AsSpan().Slice(index + 1).ToString();

            string fileName;
            if (Path.IsPathRooted(argValue))
            {
                fileName = Path.GetFileName(argValue);
            }
            else
            {
                fileName = argValue;
            }

            if (!hashSet.Add(fileName))
            {
                fileName = Path.Combine(hashSet.Count.ToString(), fileName);
            }

            var filePath = Path.Combine(outputDirectory, fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            var argName = line.AsSpan().Slice(0, index).ToString();
            args[i] = $"/{argName}:{filePath}";
        }

        if (line.StartsWith("generatedfilesout", comparison))
        {
            var generatedDir = Path.Combine(outputDirectory, "generated");
            Directory.CreateDirectory(generatedDir);
            args[i] = $"/generatedfilesout:{generatedDir}";
        }
    }
}

internal sealed record ReplayOptions(string PipeName, string ClientDirectory, string OutputDirectory, string BinlogPath, int MaxParallel, bool Wait, int Iterations)
{
    internal string TempDirectory { get; } = Path.Combine(OutputDirectory, "temp");
}

internal readonly record struct BuildData(CompilerCall CompilerCall, BuildResponse BuildResponse);
