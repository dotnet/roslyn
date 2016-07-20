// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.Telemetry;

namespace Microsoft.CodeAnalysis.Workspaces.Diagnostics
{
    /// <summary>
    /// Basically typed tuple.
    /// </summary>
    internal struct DiagnosticAnalysisResultMap
    {
        public readonly ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult> AnalysisResult;
        public readonly ImmutableDictionary<DiagnosticAnalyzer, AnalyzerTelemetryInfo> TelemetryInfo;

        public DiagnosticAnalysisResultMap(
            ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult> analysisResult,
            ImmutableDictionary<DiagnosticAnalyzer, AnalyzerTelemetryInfo> telemetryInfo)
        {
            AnalysisResult = analysisResult;
            TelemetryInfo = telemetryInfo;
        }
    }
}
