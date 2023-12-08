// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis;

/// <summary>
/// Aggregate analyzer config options for a specific path.
/// </summary>
internal readonly struct AnalyzerConfigData(AnalyzerConfigOptionsResult result)
{
    public readonly StructuredAnalyzerConfigOptions ConfigOptions = StructuredAnalyzerConfigOptions.Create(result.AnalyzerOptions);
    public readonly ImmutableDictionary<string, string> AnalyzerOptions = result.AnalyzerOptions;
    public readonly ImmutableDictionary<string, ReportDiagnostic> TreeOptions = result.TreeOptions;
}
