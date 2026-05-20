// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace RunTests;

internal static class AzdoParallelTestRunner
{
    internal static async Task<RunAllResult> RunAsync(
        Options options,
        ImmutableArray<AssemblyInfo> assemblies,
        TestRunner testRunner,
        CancellationToken cancellationToken)
    {
        var workItems = AzdoAssemblyPartitioner.GetWorkItemsForJob(
            assemblies,
            options.AzdoParallelTotalJobs!.Value,
            options.AzdoParallelJobIndex,
            options.ArtifactsDirectory,
            options.PlanPath);

        await RehydrateAsync(workItems, cancellationToken);

        return await testRunner.RunWorkItemsAsync(workItems, cancellationToken);
    }

    private static async Task RehydrateAsync(ImmutableArray<WorkItemInfo> workItems, CancellationToken cancellationToken)
    {
        var assemblyDirectories = workItems
            .SelectMany(workItem => workItem.Filters.Keys)
            .Select(assembly => Path.GetDirectoryName(assembly.AssemblyPath)!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToImmutableArray();

        if (assemblyDirectories.Length == 0)
        {
            ConsoleUtil.WriteLine("Azure DevOps partition has no assemblies to rehydrate.");
            return;
        }

        var scriptName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "rehydrate.cmd" : "rehydrate.sh";
        var scripts = assemblyDirectories
            .Select(directory => Path.Combine(directory, scriptName))
            .Where(File.Exists)
            .ToImmutableArray();
        if (scripts.Length == 0)
        {
            ConsoleUtil.WriteLine("No rehydration scripts found for selected Azure DevOps partition assemblies; assuming assemblies are already hydrated.");
            return;
        }

        var correlationPayload = Environment.GetEnvironmentVariable("HELIX_CORRELATION_PAYLOAD");
        if (string.IsNullOrEmpty(correlationPayload) || !Directory.Exists(correlationPayload))
        {
            throw new InvalidOperationException($"Azure DevOps partition rehydration requires HELIX_CORRELATION_PAYLOAD to point to the .duplicate directory. Current value: {correlationPayload}");
        }

        ConsoleUtil.WriteLine($"Rehydrating {scripts.Length} selected Azure DevOps partition assembly directories.");
        foreach (var scriptPath in scripts)
        {
            ConsoleUtil.WriteLine($"Rehydrating {Path.GetDirectoryName(scriptPath)}");
            var processInfo = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? ProcessRunner.CreateProcess("cmd.exe", $"/c \"{scriptPath}\"", captureOutput: true, cancellationToken: cancellationToken)
                : ProcessRunner.CreateProcess("bash", $"\"{scriptPath}\"", captureOutput: true, cancellationToken: cancellationToken);

            var result = await processInfo.Result;
            foreach (var line in result.OutputLines)
            {
                ConsoleUtil.WriteLine(line);
            }

            foreach (var line in result.ErrorLines)
            {
                ConsoleUtil.WriteLine(ConsoleColor.Red, line);
            }

            if (result.ExitCode != 0)
            {
                throw new InvalidOperationException($"Rehydration failed for {scriptPath} with exit code {result.ExitCode}");
            }
        }
    }
}