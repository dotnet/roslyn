// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace RunTests;

/// <summary>
/// Produces a <see cref="PartitionPlan"/> for a single test leg by combining historical per-test
/// runtime data from the previous successful build (see <see cref="TestHistoryManager"/>) with the
/// time-budgeted, sub-assembly scheduling logic in <see cref="AssemblyScheduler"/>.
///
/// The plan's <see cref="PartitionPlan.PartitionCount"/> is chosen so each Azure DevOps slice runs in
/// approximately <see cref="Options.TargetSliceMinutes"/>, clamped to
/// [<see cref="Options.MinPartitions"/>, <see cref="Options.MaxPartitions"/>].
/// </summary>
internal static class PartitionPlanner
{
    private static readonly JsonSerializerOptions s_serializerOptions = new()
    {
        WriteIndented = true,
    };

    internal static async Task<PartitionPlan> CreatePlanAsync(
        Options options,
        ImmutableArray<AssemblyInfo> assemblies,
        CancellationToken cancellationToken)
    {
        if (assemblies.IsDefaultOrEmpty)
        {
            throw new ArgumentException("At least one assembly is required to build a plan.", nameof(assemblies));
        }

        var testHistory = await TestHistoryManager.GetTestHistoryAsync(options, cancellationToken).ConfigureAwait(false);
        var usedHistory = !testHistory.IsEmpty;

        var helixWorkItems = AssemblyScheduler.Schedule(
            assemblies.Select(a => a.AssemblyPath),
            testHistory);

        if (helixWorkItems.IsDefaultOrEmpty)
        {
            throw new InvalidOperationException("AssemblyScheduler returned no work items.");
        }

        var totalSeconds = helixWorkItems.Sum(w => (w.EstimatedExecutionTime ?? TimeSpan.Zero).TotalSeconds);

        var partitionCount = ComputePartitionCount(
            totalSeconds,
            usedHistory,
            workItemCount: helixWorkItems.Length,
            options);

        var bins = BinPack(helixWorkItems, partitionCount);

        var partitionEntries = bins
            .Select((bin, index) => BuildPartitionEntry(index + 1, bin, options.ArtifactsDirectory))
            .ToImmutableArray();

        var plan = new PartitionPlan(
            PartitionCount: partitionCount,
            EstimatedDurationSeconds: totalSeconds,
            UsedHistory: usedHistory,
            Partitions: partitionEntries);

        LogPlan(plan, options);
        return plan;
    }

    internal static int ComputePartitionCount(
        double totalSeconds,
        bool usedHistory,
        int workItemCount,
        Options options)
    {
        int desired;
        if (!usedHistory || totalSeconds <= 0)
        {
            // No history -> we can't size by time. Use the configured fallback, but never ask for
            // more partitions than we have work items (otherwise some slices would do no work).
            desired = Math.Min(options.DefaultPartitionsWhenNoHistory, Math.Max(1, workItemCount));
        }
        else
        {
            var targetSeconds = options.TargetSliceMinutes * 60.0;
            desired = (int)Math.Ceiling(totalSeconds / targetSeconds);
            // Don't ask for more partitions than we have work items; bin packing can't split a single
            // work item across slices.
            desired = Math.Min(desired, Math.Max(1, workItemCount));
        }

        var clamped = Math.Clamp(desired, options.MinPartitions, options.MaxPartitions);
        return clamped;
    }

    /// <summary>
    /// Greedy longest-processing-time-first bin packing of work items into <paramref name="partitionCount"/> bins.
    ///
    /// Ties on accumulated seconds are broken by current item count, then by bin index. The count
    /// tie-break is what makes the no-history case work: when every work item has an estimated time
    /// of zero (because <see cref="AssemblyScheduler"/> fell back to test-count partitioning),
    /// items would otherwise all land in bin 0. With the count tie-break we round-robin across bins
    /// instead, giving each slice roughly equal work.
    /// </summary>
    internal static ImmutableArray<ImmutableArray<HelixWorkItem>> BinPack(
        ImmutableArray<HelixWorkItem> workItems,
        int partitionCount)
    {
        if (partitionCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(partitionCount));
        }

        var bins = Enumerable.Range(0, partitionCount)
            .Select(_ => new BinState())
            .ToArray();

        var ordered = workItems
            .OrderByDescending(w => (w.EstimatedExecutionTime ?? TimeSpan.Zero).TotalSeconds)
            .ThenBy(w => w.Id);

        foreach (var workItem in ordered)
        {
            // Pick the lightest bin. Break ties on time by item count so zero-time items still
            // distribute evenly across bins. Final tie-break is bin index for determinism.
            var lightestIndex = 0;
            for (var i = 1; i < bins.Length; i++)
            {
                if (bins[i].TotalSeconds < bins[lightestIndex].TotalSeconds
                    || (bins[i].TotalSeconds == bins[lightestIndex].TotalSeconds
                        && bins[i].Items.Count < bins[lightestIndex].Items.Count))
                {
                    lightestIndex = i;
                }
            }

            bins[lightestIndex].Items.Add(workItem);
            bins[lightestIndex].TotalSeconds += (workItem.EstimatedExecutionTime ?? TimeSpan.Zero).TotalSeconds;
        }

        return bins
            .Select(b => b.Items.OrderBy(w => w.Id).ToImmutableArray())
            .ToImmutableArray();
    }

    private sealed class BinState
    {
        public List<HelixWorkItem> Items { get; } = [];
        public double TotalSeconds { get; set; }
    }

    private static PartitionPlanEntry BuildPartitionEntry(int index, ImmutableArray<HelixWorkItem> bin, string artifactsDirectory)
    {
        var workItems = bin
            .Select(w => BuildPlanWorkItem(w, artifactsDirectory))
            .ToImmutableArray();

        var binSeconds = bin.Sum(w => (w.EstimatedExecutionTime ?? TimeSpan.Zero).TotalSeconds);
        return new PartitionPlanEntry(index, binSeconds, workItems);
    }

    private static PartitionPlanWorkItem BuildPlanWorkItem(HelixWorkItem workItem, string artifactsDirectory)
    {
        // Group the flat list of (assembly, test) pairs the scheduler produced by assembly. The scheduler
        // emits assemblies + test names as parallel arrays, but every work item is built from contiguous
        // tests in assembly order, so we can recover the per-assembly grouping by walking forward.
        //
        // For the AzDo flow we don't strictly need the per-assembly split (vstest applies the filter union
        // across all assemblies passed in), but recording it lets the plan stay human-readable and lets
        // future tooling reason about which assembly each test belongs to.
        var assemblies = workItem.AssemblyFilePaths
            .Select(p => new PartitionPlanAssembly(
                RelativePath: ToRelativePath(p, artifactsDirectory),
                TestFullyQualifiedNames: ImmutableArray<string>.Empty))
            .ToList();

        if (workItem.TestMethodNames.Length > 0)
        {
            // Attach all test names to the first assembly. vstest's filter is a union and matches by
            // FullyQualifiedName across all assemblies it loads, so this is functionally equivalent to
            // attaching each test to its real assembly while keeping the plan compact.
            assemblies[0] = assemblies[0] with { TestFullyQualifiedNames = workItem.TestMethodNames };
        }

        var seconds = (workItem.EstimatedExecutionTime ?? TimeSpan.Zero).TotalSeconds;
        return new PartitionPlanWorkItem(workItem.Id, seconds, assemblies.ToImmutableArray());
    }

    private static string ToRelativePath(string assemblyPath, string artifactsDirectory)
    {
        var relative = Path.GetRelativePath(artifactsDirectory, assemblyPath);
        // Use forward slashes for portability across planner and slice OS.
        return relative.Replace('\\', '/');
    }

    internal static string ResolveAssemblyPath(string relativePath, string artifactsDirectory)
    {
        return Path.GetFullPath(Path.Combine(artifactsDirectory, relativePath));
    }

    internal static void WritePlanFile(PartitionPlan plan, string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(plan, s_serializerOptions);
        File.WriteAllText(path, json);
    }

    internal static PartitionPlan ReadPlanFile(string path)
    {
        var json = File.ReadAllText(path);
        var plan = JsonSerializer.Deserialize<PartitionPlan>(json, s_serializerOptions)
            ?? throw new InvalidOperationException($"Failed to deserialize partition plan from {path}");

        if (plan.PartitionCount < 1 || plan.Partitions.IsDefault || plan.Partitions.Length != plan.PartitionCount)
        {
            throw new InvalidOperationException(
                $"Invalid partition plan at {path}: partitionCount={plan.PartitionCount}, partitions.Length={plan.Partitions.Length}");
        }

        return plan;
    }

    private static void LogPlan(PartitionPlan plan, Options options)
    {
        ConsoleUtil.WriteLine(
            $"Partition plan: {plan.PartitionCount} partitions, total estimated runtime {TimeSpan.FromSeconds(plan.EstimatedDurationSeconds):hh\\:mm\\:ss}, " +
            $"target slice {options.TargetSliceMinutes} min, history available: {plan.UsedHistory}.");

        foreach (var partition in plan.Partitions)
        {
            ConsoleUtil.WriteLine(
                $"- Partition {partition.Index}: ~{TimeSpan.FromSeconds(partition.EstimatedDurationSeconds):hh\\:mm\\:ss} " +
                $"across {partition.WorkItems.Length} work item(s)");
            foreach (var workItem in partition.WorkItems)
            {
                var filterDesc = workItem.Assemblies.Sum(a => a.TestFullyQualifiedNames.Length) is > 0 and var c
                    ? $"{c} filtered test(s)"
                    : "all tests";
                var assemblyNames = string.Join(", ", workItem.Assemblies.Select(a => Path.GetFileName(a.RelativePath)));
                ConsoleUtil.WriteLine(
                    $"    work item {workItem.Id} ~{TimeSpan.FromSeconds(workItem.EstimatedDurationSeconds):hh\\:mm\\:ss}: {assemblyNames} ({filterDesc})");
            }
        }
    }
}
