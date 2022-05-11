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
    private readonly AnalyzerConfigOptionsResult _result;
    private readonly StructuredAnalyzerConfigOptions _configOptions;

    public AnalyzerConfigData(AnalyzerConfigOptionsResult result)
    {
        _result = result;
        _configOptions = new StructuredAnalyzerConfigOptions(result.AnalyzerOptions);
    }

    public AnalyzerConfigOptionsResult Result => _result;
    public StructuredAnalyzerConfigOptions AnalyzerConfigOptions => _configOptions;
    public ImmutableDictionary<string, string> AnalyzerOptions => _result.AnalyzerOptions;
    public ImmutableDictionary<string, ReportDiagnostic> TreeOptions => _result.TreeOptions;
}
