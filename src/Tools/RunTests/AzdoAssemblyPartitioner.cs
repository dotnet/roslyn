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
    internal static ImmutableArray<WorkItemInfo> GetWorkItemsForJob(
        ImmutableArray<AssemblyInfo> assemblies,
        int totalJobs,
        int jobIndex)
    {
        if (totalJobs < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(totalJobs));
        }

        if (jobIndex < 0 || jobIndex >= totalJobs)
        {
            throw new ArgumentOutOfRangeException(nameof(jobIndex));
        }

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