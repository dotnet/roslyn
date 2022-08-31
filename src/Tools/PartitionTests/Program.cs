// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System.Collections.Immutable;
using System.CommandLine;
using System.CommandLine.Binding;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using PartitionTests;

// Setup cancellation for ctrl-c key presses
using var cts = new CancellationTokenSource();
Console.CancelKeyPress += delegate
{
    cts.Cancel();
};

var rootCommand = new RootCommand("Looks for test assemblies and partitions tests based on the requested options");

// Required options
rootCommand.AddOption(new Option<string>("--artifactsDirectory", "Path to the artifacts directory") { IsRequired = true });
rootCommand.AddOption(new Option<string[]>("--targetFrameworks", "Target frameworks to test") { IsRequired = true });
rootCommand.AddOption(new Option<string>(alias: "--configuration", "Configuration to test: Debug or Release") { IsRequired = true });
rootCommand.AddOption(new Option<string>("--logDirectory", "Log file directory") { IsRequired = true });
rootCommand.AddOption(new Option<string>("--outputFileName", "File name to write the partition information to.  Will be placed in the artifacts directory.") { IsRequired = true });

// Optional options
rootCommand.AddOption(new Option<bool>("--singleAssembly", () => false, "Test work items should be created per assembly"));
rootCommand.AddOption(new Option<string[]>("--include", () => new string[] { ".*UnitTests.*" }, "Expression for including unit test dlls: default *.UnitTests.*"));
rootCommand.AddOption(new Option<string[]>("--exclude", () => Array.Empty<string>(), "Expression for excluding unit test dlls: default is empty"));
rootCommand.AddOption(new Option<string?>("--accessToken", "Pipeline access token with permissions to view test history"));
rootCommand.AddOption(new Option<string?>("--phaseName", "Pipeline phase name associated with this test run"));
rootCommand.AddOption(new Option<string?>("--targetBranchName", "Target branch of this pipeline run"));

rootCommand.Handler = CommandHandler.Create(HandleAsync);

return await rootCommand.InvokeAsync(args);

async Task<int> HandleAsync(PartitionOptions partitionOptions)
{
    try
    {
        Console.WriteLine($"Input options: {partitionOptions}");

        // Find the assemblies to partition.
        var assemblies = TestAssemblyFinder.GetAssemblyFilePaths(partitionOptions.ArtifactsDirectory, partitionOptions.TargetFrameworks, partitionOptions.Configuration, (string[])partitionOptions.Include, partitionOptions.Exclude);
        
        // Partition the assemblies.
        var workItems = await AssemblyScheduler.ScheduleAsync(assemblies, partitionOptions, cts.Token);

        // Write the partition information to a file so we can get back to it in subsequent steps.
        using var fileStream = File.Create(Path.Combine(partitionOptions.ArtifactsDirectory, partitionOptions.OutputFileName));
        JsonSerializer.Serialize(fileStream, workItems);

        return 0;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"##[Error]Failed to partition tests");
        Console.WriteLine(ex.ToString());
        return 1;
    }
    finally
    {
        WriteLogFile(partitionOptions.LogDirectory);
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

record PartitionOptions(
    string ArtifactsDirectory,
    string[] TargetFrameworks,
    string Configuration,
    string LogDirectory,
    string OutputFileName,
    bool SingleAssembly,
    string[] Include,
    string[] Exclude,
    string AccessToken,
    string PhaseName,
    string TargetBranchName);
