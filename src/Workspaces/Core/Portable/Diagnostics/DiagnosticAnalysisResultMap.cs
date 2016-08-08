// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
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
        {
            return Create(analysisResult, telemetryInfo, ImmutableDictionary<TKey, ImmutableArray<DiagnosticData>>.Empty);
        }

        public static DiagnosticAnalysisResultMap<TKey, TValue> Create<TKey, TValue>(
            ImmutableDictionary<TKey, TValue> analysisResult,
            ImmutableDictionary<TKey, AnalyzerTelemetryInfo> telemetryInfo,
            ImmutableDictionary<TKey, ImmutableArray<DiagnosticData>> exceptions)
        {
            return new DiagnosticAnalysisResultMap<TKey, TValue>(analysisResult, telemetryInfo, exceptions);
        }
    }

    internal struct DiagnosticAnalysisResultMap<TKey, TValue>
    {
        public readonly ImmutableDictionary<TKey, TValue> AnalysisResult;
        public readonly ImmutableDictionary<TKey, AnalyzerTelemetryInfo> TelemetryInfo;
        public readonly ImmutableDictionary<TKey, ImmutableArray<DiagnosticData>> Exceptions;

        public DiagnosticAnalysisResultMap(
            ImmutableDictionary<TKey, TValue> analysisResult,
            ImmutableDictionary<TKey, AnalyzerTelemetryInfo> telemetryInfo,
            ImmutableDictionary<TKey, ImmutableArray<DiagnosticData>> exceptions)
        {
            AnalysisResult = analysisResult;
            TelemetryInfo = telemetryInfo;
            Exceptions = exceptions;
        }
    }
}
