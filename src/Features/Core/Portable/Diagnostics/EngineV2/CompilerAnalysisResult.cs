// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics.Telemetry;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV2
{
    /// <summary>
    /// Basically typed tuple.
    /// </summary>
    internal struct CompilerAnalysisResult
    {
        public readonly ImmutableDictionary<DiagnosticAnalyzer, AnalysisResult> AnalysisResult;
        public readonly ImmutableDictionary<DiagnosticAnalyzer, AnalyzerTelemetryInfo> TelemetryInfo;

        public CompilerAnalysisResult(
            ImmutableDictionary<DiagnosticAnalyzer, AnalysisResult> analysisResult,
            ImmutableDictionary<DiagnosticAnalyzer, AnalyzerTelemetryInfo> telemetryInfo)
        {
            AnalysisResult = analysisResult;
            TelemetryInfo = telemetryInfo;
        }
    }
}
