// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics.Telemetry;
using Microsoft.CodeAnalysis.Workspaces.Diagnostics;

namespace Microsoft.CodeAnalysis.Remote.Diagnostics
{
    internal struct DiagnosticResult
    {
        public readonly ImmutableDictionary<string, DiagnosticAnalysisResultBuilder> AnalysisResult;
        public readonly ImmutableDictionary<string, AnalyzerTelemetryInfo> TelemetryInfo;

        public DiagnosticResult(
            ImmutableDictionary<string, DiagnosticAnalysisResultBuilder> analysisResult,
            ImmutableDictionary<string, AnalyzerTelemetryInfo> telemetryInfo)
        {
            AnalysisResult = analysisResult;
            TelemetryInfo = telemetryInfo;
        }
    }
}
