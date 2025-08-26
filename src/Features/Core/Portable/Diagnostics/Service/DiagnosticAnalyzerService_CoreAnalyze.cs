// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics.Telemetry;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Workspaces.Diagnostics;
using RoslynLogger = Microsoft.CodeAnalysis.Internal.Log.Logger;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal sealed partial class DiagnosticAnalyzerService
{
    private async Task<DiagnosticAnalysisResultMap<DiagnosticAnalyzer, DiagnosticAnalysisResult>> AnalyzeInProcessAsync(
        DocumentAnalysisScope? documentAnalysisScope,
        Project project,
        CompilationWithAnalyzersPair compilationWithAnalyzers,
        bool logPerformanceInfo,
        bool getTelemetryInfo,
        CancellationToken cancellationToken)
    {
        var result = await AnalyzeAsync().ConfigureAwait(false);
        Debug.Assert(getTelemetryInfo || result.TelemetryInfo.IsEmpty);
        return result;

        async Task<DiagnosticAnalysisResultMap<DiagnosticAnalyzer, DiagnosticAnalysisResult>> AnalyzeAsync()
        {
            var (analysisResult, additionalPragmaSuppressionDiagnostics) = await compilationWithAnalyzers.GetAnalysisResultAsync(
                documentAnalysisScope, project, _analyzerInfoCache, cancellationToken).ConfigureAwait(false);

            if (logPerformanceInfo)
            {
                // if remote host is there, report performance data
                var asyncToken = _listener.BeginAsyncOperation(nameof(AnalyzeInProcessAsync));
                var _ = Task.Run(
                    () => ReportAnalyzerPerformance(analysisResult),
                    cancellationToken).CompletesAsyncOperation(asyncToken);
            }

            var projectAnalyzers = documentAnalysisScope?.ProjectAnalyzers ?? compilationWithAnalyzers.ProjectAnalyzers;
            var hostAnalyzers = documentAnalysisScope?.HostAnalyzers ?? compilationWithAnalyzers.HostAnalyzers;
            var skippedAnalyzersInfo = project.Solution.SolutionState.Analyzers.GetSkippedAnalyzersInfo(project.State, _analyzerInfoCache);

            // get compiler result builder map
            var builderMap = ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResultBuilder>.Empty;
            if (analysisResult is not null)
            {
                var map = await analysisResult.ToResultBuilderMapAsync(
                    additionalPragmaSuppressionDiagnostics, documentAnalysisScope, project,
                    projectAnalyzers, hostAnalyzers, skippedAnalyzersInfo, cancellationToken).ConfigureAwait(false);
                builderMap = builderMap.AddRange(map);
            }

            var result = builderMap.ToImmutableDictionary(kv => kv.Key, kv => DiagnosticAnalysisResult.CreateFromBuilder(kv.Value));
            var telemetry = ImmutableDictionary<DiagnosticAnalyzer, AnalyzerTelemetryInfo>.Empty;
            if (getTelemetryInfo && analysisResult is not null)
            {
                telemetry = analysisResult.MergedAnalyzerTelemetryInfo;
            }

            return DiagnosticAnalysisResultMap.Create(result, telemetry);
        }

        void ReportAnalyzerPerformance(AnalysisResultPair? analysisResult)
        {
            try
            {
                // +1 for project itself
                var count = documentAnalysisScope != null ? 1 : project.DocumentIds.Count + 1;
                var forSpanAnalysis = documentAnalysisScope?.Span.HasValue ?? false;

                ImmutableArray<AnalyzerPerformanceInfo> performanceInfo = [];
                if (analysisResult is not null)
                {
                    performanceInfo = performanceInfo.AddRange(analysisResult.MergedAnalyzerTelemetryInfo.ToAnalyzerPerformanceInfo(_analyzerInfoCache));
                }

                using (RoslynLogger.LogBlock(FunctionId.CodeAnalysisService_ReportAnalyzerPerformance, cancellationToken))
                {
                    var service = project.Solution.Services.GetService<IPerformanceTrackerService>();
                    service?.AddSnapshot(performanceInfo, count, forSpanAnalysis);
                }
            }
            catch (Exception ex) when (FatalError.ReportAndCatchUnlessCanceled(ex, cancellationToken))
            {
                // ignore all, this is fire and forget method
            }
        }
    }
}
