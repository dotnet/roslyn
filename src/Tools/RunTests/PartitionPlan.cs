// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace RunTests;

/// <summary>
/// Serialized partition plan produced by <see cref="PartitionPlanner"/> and consumed by
/// <see cref="AzdoAssemblyPartitioner"/> for Azure DevOps parallel slice execution.
///
/// Paths are stored relative to the test-run artifacts directory so a plan produced on the
/// planner machine can be loaded by every slice machine without absolute-path conflicts.
/// </summary>
internal sealed record PartitionPlan(
    [property: JsonPropertyName("partitionCount")] int PartitionCount,
    [property: JsonPropertyName("estimatedDurationSeconds")] double EstimatedDurationSeconds,
    [property: JsonPropertyName("usedHistory")] bool UsedHistory,
    [property: JsonPropertyName("partitions")] ImmutableArray<PartitionPlanEntry> Partitions);

internal sealed record PartitionPlanEntry(
    [property: JsonPropertyName("index")] int Index,
    [property: JsonPropertyName("estimatedDurationSeconds")] double EstimatedDurationSeconds,
    [property: JsonPropertyName("workItems")] ImmutableArray<PartitionPlanWorkItem> WorkItems);

internal sealed record PartitionPlanWorkItem(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("estimatedDurationSeconds")] double EstimatedDurationSeconds,
    [property: JsonPropertyName("assemblies")] ImmutableArray<PartitionPlanAssembly> Assemblies);

/// <summary>
/// One assembly in a work item.
///
/// <see cref="RelativePath"/> is rooted at the artifacts directory (e.g.
/// <c>bin/Microsoft.CodeAnalysis.CSharp.Symbol.UnitTests/Debug/net10.0/Microsoft.CodeAnalysis.CSharp.Symbol.UnitTests.dll</c>).
///
/// <see cref="TestFullyQualifiedNames"/> is empty when the entire assembly should run, and otherwise contains
/// the explicit set of fully-qualified test method names that will be passed to vstest as a TestCaseFilter.
/// </summary>
internal sealed record PartitionPlanAssembly(
    [property: JsonPropertyName("relativePath")] string RelativePath,
    [property: JsonPropertyName("testFullyQualifiedNames")] ImmutableArray<string> TestFullyQualifiedNames);
