// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.SourceGeneratorTelemetry;

internal sealed class SourceGeneratorTelemetryCollectorWorkspaceService : ISourceGeneratorTelemetryCollectorWorkspaceService
{
    private sealed record GeneratorTelemetryKey
    {
        [SetsRequiredMembers]
        public GeneratorTelemetryKey(ISourceGenerator generator, AnalyzerReference analyzerReference)
        {
            Identity = SourceGeneratorIdentity.Create(generator, analyzerReference);
            FileVersion = "(null)";

            if (Identity.AssemblyPath != null)
            {
                FileVersion = IOUtilities.PerformIO(() => FileVersionInfo.GetVersionInfo(Identity.AssemblyPath).FileVersion, defaultValue: "(reading version threw exception)")!;
            }
        }

        public required SourceGeneratorIdentity Identity { get; init; }
        public required string FileVersion { get; init; }
    }

    /// <summary>
    /// Cache of the <see cref="GeneratorTelemetryKey"/> for a generator to avoid repeatedly reading version information from disk;
    /// this is a ConditionalWeakTable so having telemetry for older runs doesn't keep the generator itself alive.
    /// </summary>
    private readonly ConditionalWeakTable<ISourceGenerator, GeneratorTelemetryKey> _generatorTelemetryKeys = new();

    private readonly StatisticLogAggregator<GeneratorTelemetryKey> _elapsedTimeByGenerator = new();
    private readonly StatisticLogAggregator<GeneratorTelemetryKey> _producedFilesByGenerator = new();

    private GeneratorTelemetryKey GetTelemetryKey(ISourceGenerator generator, Func<ISourceGenerator, AnalyzerReference> getAnalyzerReference)
        => _generatorTelemetryKeys.GetValue(generator, g => new GeneratorTelemetryKey(g, getAnalyzerReference(g)));

    public void CollectRunResult(
        GeneratorDriverRunResult driverRunResult,
        GeneratorDriverTimingInfo driverTimingInfo,
        Func<ISourceGenerator, AnalyzerReference> getAnalyzerReference)
    {
        foreach (var generatorTime in driverTimingInfo.GeneratorTimes)
        {
            _elapsedTimeByGenerator.AddDataPoint(GetTelemetryKey(generatorTime.Generator, getAnalyzerReference), generatorTime.ElapsedTime);
        }

        foreach (var generatorResult in driverRunResult.Results)
        {
            _producedFilesByGenerator.AddDataPoint(GetTelemetryKey(generatorResult.Generator, getAnalyzerReference), generatorResult.GeneratedSources.Length);
        }
    }

    public ImmutableArray<ImmutableDictionary<string, object?>> FetchKeysAndAndClear()
    {
        var arrayBuilder = ImmutableArray.CreateBuilder<ImmutableDictionary<string, object?>>();

        // We'll create one set of keys for each generator
        foreach (var (telemetryKey, elapsedTimeCounter) in _elapsedTimeByGenerator)
        {
            var map = ImmutableDictionary.CreateBuilder<string, object?>();

            // TODO: have a policy for when we don't have to hash them
            map[nameof(telemetryKey.Identity.AssemblyName) + "Hashed"] = AnalyzerNameForTelemetry.ComputeSha256Hash(telemetryKey.Identity.AssemblyName);
            map[nameof(telemetryKey.Identity.AssemblyVersion)] = telemetryKey.Identity.AssemblyVersion.ToString();
            map[nameof(telemetryKey.Identity.TypeName) + "Hashed"] = AnalyzerNameForTelemetry.ComputeSha256Hash(telemetryKey.Identity.TypeName);
            map[nameof(telemetryKey.FileVersion)] = telemetryKey.FileVersion;

            var result = elapsedTimeCounter.GetStatisticResult();
            result.WriteTelemetryPropertiesTo(map, prefix: "ElapsedTimePerRun");

            var producedFileCount = _producedFilesByGenerator.GetStatisticResult(telemetryKey);
            producedFileCount.WriteTelemetryPropertiesTo(map, prefix: "GeneratedFileCountPerRun");

            arrayBuilder.Add(map.ToImmutable());
        }

        // Clear the counters so we can see performance over time and across different solutions. The StatisticLogAggregators we are using safe to use concurrently,
        // so this clear won't break if there's a concurrent write. There's a possibility here we might be clearing a telemetry report we haven't sent, but that's
        // fine since this is aggregate telemetry.
        _elapsedTimeByGenerator.Clear();
        _producedFilesByGenerator.Clear();

        return arrayBuilder.ToImmutable();
    }
}
