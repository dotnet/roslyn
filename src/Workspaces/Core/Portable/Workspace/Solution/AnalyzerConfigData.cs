// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;

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
        ConfigOptions = StructuredAnalyzerConfigOptions.Create(result.AnalyzerOptions);
        AnalyzerOptions = result.AnalyzerOptions;
        TreeOptions = result.TreeOptions;
    }
}
