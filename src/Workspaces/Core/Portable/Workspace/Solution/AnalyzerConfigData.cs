// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

/// <summary>
/// Aggregate analyzer config options for a specific path.
/// </summary>
internal readonly struct AnalyzerConfigData
{
    public readonly StructuredAnalyzerConfigOptions ConfigOptions;
    public readonly ImmutableDictionary<string, string> AnalyzerOptions;
    public readonly ImmutableDictionary<string, ReportDiagnostic> TreeOptions;

    public AnalyzerConfigData(AnalyzerConfigOptionsResult result)
    {
        ConfigOptions = StructuredAnalyzerConfigOptions.Create(GetAggregatedOptions(result));
        AnalyzerOptions = result.AnalyzerOptions;
        TreeOptions = result.TreeOptions;
    }

    private static ImmutableDictionary<string, string> GetAggregatedOptions(AnalyzerConfigOptionsResult result)
    {
        if (result.TreeOptions.IsEmpty)
            return result.AnalyzerOptions;

        var builder = ImmutableDictionary.CreateBuilder<string, string>(result.AnalyzerOptions.KeyComparer, result.AnalyzerOptions.ValueComparer);
        builder.AddRange(result.AnalyzerOptions);

        foreach (var (id, severity) in result.TreeOptions)
        {
            var key = $"dotnet_diagnostic.{id}.severity";
            var value = severity.ToEditorConfigString();
            builder.Add(key, value);
        }

        return builder.ToImmutable();

    }
}
