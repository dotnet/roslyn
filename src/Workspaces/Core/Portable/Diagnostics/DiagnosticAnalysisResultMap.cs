// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.Telemetry;

namespace Microsoft.CodeAnalysis.Workspaces.Diagnostics
{
    /// <summary>
    /// Basically typed tuple.
    /// </summary>
    internal static class DiagnosticAnalysisResultMap
    {
        public static DiagnosticAnalysisResultMap<TKey, TValue> Create<TKey, TValue>(
            ImmutableDictionary<TKey, TValue> analysisResult,
            ImmutableDictionary<TKey, AnalyzerTelemetryInfo> telemetryInfo)
            where TKey : notnull
        {
            return new DiagnosticAnalysisResultMap<TKey, TValue>(analysisResult, telemetryInfo);
        }
    }

    internal readonly struct DiagnosticAnalysisResultMap<TKey, TValue>
        where TKey : notnull
    {
        public static readonly DiagnosticAnalysisResultMap<TKey, TValue> Empty = new(
            ImmutableDictionary<TKey, TValue>.Empty,
            ImmutableDictionary<TKey, AnalyzerTelemetryInfo>.Empty);

        public readonly ImmutableDictionary<TKey, TValue> AnalysisResult;
        public readonly ImmutableDictionary<TKey, AnalyzerTelemetryInfo> TelemetryInfo;

        public DiagnosticAnalysisResultMap(
            ImmutableDictionary<TKey, TValue> analysisResult,
            ImmutableDictionary<TKey, AnalyzerTelemetryInfo> telemetryInfo)
        {
            Debug.Assert(telemetryInfo.IsEmpty || telemetryInfo.Count == analysisResult.Count);

            AnalysisResult = analysisResult;
            TelemetryInfo = telemetryInfo;
        }
    }
}
