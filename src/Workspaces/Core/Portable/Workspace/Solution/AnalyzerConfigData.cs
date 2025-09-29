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
    private readonly AnalyzerConfigOptions _dictionaryConfigOptions;

    public readonly StructuredAnalyzerConfigOptions ConfigOptionsWithoutFallback;

    public readonly StructuredAnalyzerConfigOptions ConfigOptionsWithFallback;

    /// <summary>
    /// These options do not fall back.
    /// </summary>
    public readonly ImmutableDictionary<string, ReportDiagnostic> TreeOptions;

    public AnalyzerConfigData(AnalyzerConfigOptionsResult result, StructuredAnalyzerConfigOptions fallbackOptions)
    {
        _dictionaryConfigOptions = new DictionaryAnalyzerConfigOptions(result.AnalyzerOptions);
        ConfigOptionsWithoutFallback = StructuredAnalyzerConfigOptions.Create(_dictionaryConfigOptions, StructuredAnalyzerConfigOptions.Empty);
        ConfigOptionsWithFallback = StructuredAnalyzerConfigOptions.Create(_dictionaryConfigOptions, fallbackOptions);
        TreeOptions = result.TreeOptions;
    }
}
