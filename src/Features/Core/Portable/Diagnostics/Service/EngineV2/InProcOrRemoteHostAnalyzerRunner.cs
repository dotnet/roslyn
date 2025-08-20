// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics.Telemetry;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Workspaces.Diagnostics;
using RoslynLogger = Microsoft.CodeAnalysis.Internal.Log.Logger;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal sealed class InProcOrRemoteHostAnalyzerRunner(
    DiagnosticAnalyzerInfoCache analyzerInfoCache,
    IAsynchronousOperationListener? operationListener = null)
{
    private readonly IAsynchronousOperationListener _asyncOperationListener = operationListener ?? AsynchronousOperationListenerProvider.NullListener;
    public DiagnosticAnalyzerInfoCache AnalyzerInfoCache { get; } = analyzerInfoCache;

    public Task<DiagnosticAnalysisResultMap<DiagnosticAnalyzer, DiagnosticAnalysisResult>> AnalyzeDocumentAsync(
        DocumentAnalysisScope documentAnalysisScope,
        CompilationWithAnalyzersPair compilationWithAnalyzers,
        bool logPerformanceInfo,
        bool getTelemetryInfo,
        CancellationToken cancellationToken)
        => AnalyzeAsync(documentAnalysisScope, documentAnalysisScope.TextDocument.Project, compilationWithAnalyzers, logPerformanceInfo, getTelemetryInfo, cancellationToken);

    public Task<DiagnosticAnalysisResultMap<DiagnosticAnalyzer, DiagnosticAnalysisResult>> AnalyzeProjectAsync(
        Project project,
        CompilationWithAnalyzersPair compilationWithAnalyzers,
        bool logPerformanceInfo,
        bool getTelemetryInfo,
        CancellationToken cancellationToken)
        => AnalyzeAsync(documentAnalysisScope: null, project, compilationWithAnalyzers, logPerformanceInfo, getTelemetryInfo, cancellationToken);

    private async Task<DiagnosticAnalysisResultMap<DiagnosticAnalyzer, DiagnosticAnalysisResult>> AnalyzeAsync(
        DocumentAnalysisScope? documentAnalysisScope,
        Project project,
        CompilationWithAnalyzersPair compilationWithAnalyzers,
        bool logPerformanceInfo,
        bool getTelemetryInfo,
        CancellationToken cancellationToken)
    {
        var result = await AnalyzeInProcAsync(
            documentAnalysisScope, project, compilationWithAnalyzers,
            logPerformanceInfo, getTelemetryInfo, cancellationToken).ConfigureAwait(false);
        Debug.Assert(getTelemetryInfo || result.TelemetryInfo.IsEmpty);
        return result;
    }

    private async Task<DiagnosticAnalysisResultMap<DiagnosticAnalyzer, DiagnosticAnalysisResult>> AnalyzeInProcAsync(
        DocumentAnalysisScope? documentAnalysisScope,
        Project project,
        CompilationWithAnalyzersPair compilationWithAnalyzers,
        bool logPerformanceInfo,
        bool getTelemetryInfo,
        CancellationToken cancellationToken)
    {
        var (analysisResult, additionalPragmaSuppressionDiagnostics) = await compilationWithAnalyzers.GetAnalysisResultAsync(
            documentAnalysisScope, project, AnalyzerInfoCache, cancellationToken).ConfigureAwait(false);

        if (logPerformanceInfo)
        {
            // if remote host is there, report performance data
            var asyncToken = _asyncOperationListener.BeginAsyncOperation(nameof(AnalyzeInProcAsync));
            var _ = Task.Run(
                () => ReportAnalyzerPerformance(documentAnalysisScope, project, analysisResult, cancellationToken),
                cancellationToken).CompletesAsyncOperation(asyncToken);
        }

        var projectAnalyzers = documentAnalysisScope?.ProjectAnalyzers ?? compilationWithAnalyzers.ProjectAnalyzers;
        var hostAnalyzers = documentAnalysisScope?.HostAnalyzers ?? compilationWithAnalyzers.HostAnalyzers;
        var skippedAnalyzersInfo = project.Solution.SolutionState.Analyzers.GetSkippedAnalyzersInfo(project.State, AnalyzerInfoCache);

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

    private void ReportAnalyzerPerformance(
        DocumentAnalysisScope? documentAnalysisScope,
        Project project,
        AnalysisResultPair? analysisResult,
        CancellationToken cancellationToken)
    {
        try
        {
            // +1 for project itself
            var count = documentAnalysisScope != null ? 1 : project.DocumentIds.Count + 1;
            var forSpanAnalysis = documentAnalysisScope?.Span.HasValue ?? false;

            ImmutableArray<AnalyzerPerformanceInfo> performanceInfo = [];
            if (analysisResult is not null)
            {
                performanceInfo = performanceInfo.AddRange(analysisResult.MergedAnalyzerTelemetryInfo.ToAnalyzerPerformanceInfo(AnalyzerInfoCache));
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
