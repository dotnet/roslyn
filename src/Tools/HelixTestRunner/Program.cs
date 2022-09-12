// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System.Collections.Immutable;
using System.CommandLine;
using System.CommandLine.Binding;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using HelixTestRunner;
using RunTestsUtils;

// Setup cancellation for ctrl-c key presses
using var cts = new CancellationTokenSource();
Console.CancelKeyPress += delegate
{
    cts.Cancel();
};

var rootCommand = new RootCommand("Partitions and runs tests on helix");

// Required options
rootCommand.AddOption(new Option<string>("--artifactsDirectory", "Path to the artifacts directory") { IsRequired = true });
rootCommand.AddOption(new Option<string>("--testAssembliesPath", "Path to the file containing the list of assemblies to test") { IsRequired = true });
rootCommand.AddOption(new Option<string>("--dotnetExecutablePath", "Path to dotnet executable") { IsRequired = true });
rootCommand.AddOption(new Option<string>("--logDirectory", "Log file directory") { IsRequired = true });
rootCommand.AddOption(new Option<string>("--architecture", "Architecture to test on: x86, x64 or arm64") { IsRequired = true });

// Optional options
rootCommand.AddOption(new Option<string>("--helixQueueName", () => "Windows.10.Amd64.Open", "Log file directory"));
rootCommand.AddOption(new Option<string?>("--accessToken", "Pipeline access token with permissions to view test history"));
rootCommand.AddOption(new Option<string?>("--projectUri", "Azure Devops project containing the pipeline"));
rootCommand.AddOption(new Option<string?>("--pipelineDefinitionId", "DefinitionId of the pipeline running the tests"));
rootCommand.AddOption(new Option<string?>("--phaseName", "Pipeline phase name associated with this test run"));
rootCommand.AddOption(new Option<string?>("--targetBranchName", "Target branch of this pipeline run"));
rootCommand.AddOption(new Option<string?>("--testFilter", "xUnit string to pass to --filter, e.g. FullyQualifiedName~TestClass1|Category=CategoryA"));


rootCommand.Handler = CommandHandler.Create(HandleAsync);

return await rootCommand.InvokeAsync(args);

async Task<int> HandleAsync(HelixOptions helixOptions)
{
    try
    {
        var cancellationToken = cts.Token;

        // Find the assemblies to partition.
        if (!File.Exists(helixOptions.TestAssembliesPath))
        {
            throw new ArgumentException($"{helixOptions.TestAssembliesPath} does not exist");
        }

        var assemblies = File.ReadAllLines(helixOptions.TestAssembliesPath);
        foreach (var assembly in assemblies)
        {
            if (!File.Exists(assembly))
            {
                throw new ArgumentException($"{assembly} does not exist on disk");
            }
        }

        // Partition the assemblies.
        var workItems = await AssemblyScheduler.ScheduleAsync(assemblies.Select(a => new AssemblyInfo(a)).ToImmutableArray(), helixOptions, cancellationToken);

        // Run the partitions on helix.
        return await HelixRunner.RunAllOnHelixAsync(workItems, helixOptions, cancellationToken);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"##[Error]Hit exception while trying to run tests on helix:");
        Console.WriteLine(ex.ToString());
        return 1;
    }
    finally
    {
        WriteLogFile(helixOptions.LogDirectory);
    }
}

static void WriteLogFile(string logDirectory)
{
    var logFilePath = Path.Combine(logDirectory, "partitiontests.log");
    try
    {
        Directory.CreateDirectory(logDirectory);
        using (var writer = new StreamWriter(logFilePath, append: false))
        {
            Logger.WriteTo(writer);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error writing log file {logFilePath}");
        Console.WriteLine(ex.ToString());
    }

    Logger.Clear();
}

record HelixOptions(
    string ArtifactsDirectory,
    string TestAssembliesPath,
    string DotnetExecutablePath,
    string LogDirectory,
    string Architecture,
    string HelixQueueName,
    string? AccessToken,
    string? ProjectUri,
    string? PipelineDefinitionId,
    string? PhaseName,
    string? TargetBranchName,
    string? TestFilter);
