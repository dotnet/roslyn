// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace RunTests;

internal static class AzdoAssemblyPartitioner
{
    /// <summary>
    /// Picks the work items for the current Azure DevOps slice.
    ///
    /// When a partition plan is provided, the slice's work items are looked up by
    /// <paramref name="jobIndex"/> from the precomputed plan. Otherwise we fall back to the legacy
    /// test-count balancing across <paramref name="totalJobs"/> bins so legs without a plan still run.
    /// </summary>
    internal static ImmutableArray<WorkItemInfo> GetWorkItemsForJob(
        ImmutableArray<AssemblyInfo> assemblies,
        int totalJobs,
        int jobIndex,
        string artifactsDirectory,
        string? planPath)
    {
        if (totalJobs < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(totalJobs));
        }

        if (jobIndex < 0 || jobIndex >= totalJobs)
        {
            throw new ArgumentOutOfRangeException(nameof(jobIndex));
        }

        if (!string.IsNullOrEmpty(planPath) && File.Exists(planPath))
        {
            return GetWorkItemsFromPlan(planPath, totalJobs, jobIndex, artifactsDirectory);
        }

        if (!string.IsNullOrEmpty(planPath))
        {
            ConsoleUtil.Warning($"Partition plan file not found at '{planPath}'. Falling back to test-count partitioning.");
        }

        return GetWorkItemsByTestCount(assemblies, totalJobs, jobIndex);
    }

    private static ImmutableArray<WorkItemInfo> GetWorkItemsFromPlan(
        string planPath,
        int totalJobs,
        int jobIndex,
        string artifactsDirectory)
    {
        var plan = PartitionPlanner.ReadPlanFile(planPath);

        if (plan.PartitionCount != totalJobs)
        {
            ConsoleUtil.Warning(
                $"Partition plan declares {plan.PartitionCount} partitions but Azure DevOps slice job count is {totalJobs}. " +
                "The plan's partitionCount should be used to drive strategy.parallel.");
        }

        if (jobIndex >= plan.Partitions.Length)
        {
            throw new InvalidOperationException(
                $"Slice index {jobIndex} (1-based position {jobIndex + 1}) is out of range for plan with {plan.Partitions.Length} partitions.");
        }

        var entry = plan.Partitions[jobIndex];
        ConsoleUtil.WriteLine(
            $"Loaded partition {entry.Index} from plan: ~{TimeSpan.FromSeconds(entry.EstimatedDurationSeconds):hh\\:mm\\:ss} across {entry.WorkItems.Length} work item(s).");

        var workItems = ImmutableArray.CreateBuilder<WorkItemInfo>(entry.WorkItems.Length);
        foreach (var workItem in entry.WorkItems)
        {
            var filters = ImmutableSortedDictionary.CreateBuilder<AssemblyInfo, ImmutableArray<TestMethodInfo>>();
            foreach (var assembly in workItem.Assemblies)
            {
                var resolved = PartitionPlanner.ResolveAssemblyPath(assembly.RelativePath, artifactsDirectory);
                if (!File.Exists(resolved))
                {
                    throw new InvalidOperationException(
                        $"Partition plan references assembly '{assembly.RelativePath}' which does not exist at '{resolved}'. " +
                        "Slice machines must download the same test artifact the planner used.");
                }

                var testMethods = assembly.TestFullyQualifiedNames
                    .Select(fqn => new TestMethodInfo(GetSimpleName(fqn), fqn, TimeSpan.Zero))
                    .ToImmutableArray();
                filters[new AssemblyInfo(resolved)] = testMethods;
            }

            workItems.Add(new WorkItemInfo(filters.ToImmutable(), workItem.Id));
        }

        return workItems.ToImmutable();

        static string GetSimpleName(string fullyQualifiedName)
        {
            var lastDot = fullyQualifiedName.LastIndexOf('.');
            return lastDot < 0 ? fullyQualifiedName : fullyQualifiedName[(lastDot + 1)..];
        }
    }

    private static ImmutableArray<WorkItemInfo> GetWorkItemsByTestCount(
        ImmutableArray<AssemblyInfo> assemblies,
        int totalJobs,
        int jobIndex)
    {
        var assemblyInfos = assemblies
            .Select((assembly, index) => new AssemblyPartitionInfo(assembly, GetTestCount(assembly), index))
            .OrderByDescending(info => info.TestCount)
            .ThenBy(info => info.Assembly.AssemblyPath, StringComparer.Ordinal)
            .ToImmutableArray();

        var bins = Enumerable.Range(0, totalJobs)
            .Select(index => new PartitionBin(index))
            .ToArray();

        foreach (var assemblyInfo in assemblyInfos)
        {
            var bin = bins
                .OrderBy(bin => bin.TestCount)
                .ThenBy(bin => bin.Index)
                .First();
            bin.Add(assemblyInfo);
        }

        LogPartitionSummary(bins, jobIndex);

        return bins[jobIndex].Assemblies
            .OrderBy(info => info.PartitionIndex)
            .Select(info => new WorkItemInfo(
                ImmutableSortedDictionary<AssemblyInfo, ImmutableArray<TestMethodInfo>>.Empty.Add(info.Assembly, ImmutableArray<TestMethodInfo>.Empty),
                info.PartitionIndex))
            .ToImmutableArray();
    }

    private static int GetTestCount(AssemblyInfo assembly)
    {
        var assemblyDirectory = Path.GetDirectoryName(assembly.AssemblyPath)
            ?? throw new InvalidOperationException($"Could not get directory for {assembly.AssemblyPath}");
        var testListPath = Path.Combine(assemblyDirectory, "testlist.json");
        if (!File.Exists(testListPath))
        {
            throw new InvalidOperationException($"Could not find test list for Azure DevOps partitioning: {testListPath}");
        }

        try
        {
            var testList = JsonSerializer.Deserialize<List<string>>(File.ReadAllText(testListPath));
            return testList?.Count ?? throw new InvalidOperationException($"Could not deserialize {testListPath}");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Could not deserialize {testListPath}", ex);
        }
    }

    private static void LogPartitionSummary(PartitionBin[] bins, int selectedJobIndex)
    {
        ConsoleUtil.WriteLine($"Azure DevOps static partitioning across {bins.Length} jobs. Selected job position: {selectedJobIndex + 1}.");
        foreach (var bin in bins)
        {
            ConsoleUtil.WriteLine($"- Job {bin.Index + 1}: {bin.Assemblies.Count} assemblies, {bin.TestCount} tests");
            foreach (var assembly in bin.Assemblies.OrderBy(info => info.Assembly.AssemblyPath, StringComparer.Ordinal))
            {
                ConsoleUtil.WriteLine($"  {Path.GetFileName(assembly.Assembly.AssemblyPath)} ({assembly.TestCount} tests)");
            }
        }
    }

    private sealed class PartitionBin(int index)
    {
        private readonly List<AssemblyPartitionInfo> _assemblies = [];

        internal int Index { get; } = index;

        internal int TestCount { get; private set; }

        internal IReadOnlyList<AssemblyPartitionInfo> Assemblies => _assemblies;

        internal void Add(AssemblyPartitionInfo assemblyInfo)
        {
            _assemblies.Add(assemblyInfo);
            TestCount += assemblyInfo.TestCount;
        }
    }

    private readonly record struct AssemblyPartitionInfo(AssemblyInfo Assembly, int TestCount, int PartitionIndex);
}