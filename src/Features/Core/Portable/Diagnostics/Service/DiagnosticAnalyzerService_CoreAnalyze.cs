// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics.Telemetry;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Threading;
using Microsoft.CodeAnalysis.Workspaces.Diagnostics;
using Roslyn.Utilities;
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
            var analysisResult = await GetAnalysisResultAsync().ConfigureAwait(false);
            var additionalPragmaSuppressionDiagnostics = await GetPragmaSuppressionAnalyzerDiagnosticsAsync().ConfigureAwait(false);

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

        async Task<AnalysisResultPair?> GetAnalysisResultAsync()
        {
            if (documentAnalysisScope == null)
            {
                return await compilationWithAnalyzers.GetAnalysisResultAsync(cancellationToken).ConfigureAwait(false);
            }

            Debug.Assert(documentAnalysisScope.ProjectAnalyzers.ToSet().IsSubsetOf(compilationWithAnalyzers.ProjectAnalyzers));
            Debug.Assert(documentAnalysisScope.HostAnalyzers.ToSet().IsSubsetOf(compilationWithAnalyzers.HostAnalyzers));

            switch (documentAnalysisScope.Kind)
            {
                case AnalysisKind.Syntax:
                    if (documentAnalysisScope.TextDocument is Document document)
                    {
                        var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                        return await compilationWithAnalyzers.GetAnalysisResultAsync(tree, documentAnalysisScope.Span, documentAnalysisScope.ProjectAnalyzers, documentAnalysisScope.HostAnalyzers, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        return await compilationWithAnalyzers.GetAnalysisResultAsync(documentAnalysisScope.AdditionalFile, documentAnalysisScope.Span, documentAnalysisScope.ProjectAnalyzers, documentAnalysisScope.HostAnalyzers, cancellationToken).ConfigureAwait(false);
                    }

                case AnalysisKind.Semantic:
                    var model = await ((Document)documentAnalysisScope.TextDocument).GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                    return await compilationWithAnalyzers.GetAnalysisResultAsync(model, documentAnalysisScope.Span, documentAnalysisScope.ProjectAnalyzers, documentAnalysisScope.HostAnalyzers, cancellationToken).ConfigureAwait(false);

                default:
                    throw ExceptionUtilities.UnexpectedValue(documentAnalysisScope.Kind);
            }
        }

        async Task<ImmutableArray<Diagnostic>> GetPragmaSuppressionAnalyzerDiagnosticsAsync()
        {
            var hostAnalyzers = documentAnalysisScope?.HostAnalyzers ?? compilationWithAnalyzers.HostAnalyzers;
            var suppressionAnalyzer = hostAnalyzers.OfType<IPragmaSuppressionsAnalyzer>().FirstOrDefault();
            if (suppressionAnalyzer == null)
                return [];

            RoslynDebug.AssertNotNull(compilationWithAnalyzers.HostCompilationWithAnalyzers);

            if (documentAnalysisScope != null)
            {
                if (documentAnalysisScope.TextDocument is not Document document)
                    return [];

                using var _ = ArrayBuilder<Diagnostic>.GetInstance(out var diagnosticsBuilder);
                await AnalyzeDocumentAsync(
                    compilationWithAnalyzers.HostCompilationWithAnalyzers, _analyzerInfoCache, suppressionAnalyzer,
                    document, documentAnalysisScope.Span, diagnosticsBuilder.Add, cancellationToken).ConfigureAwait(false);
                return diagnosticsBuilder.ToImmutableAndClear();
            }
            else
            {
                if (compilationWithAnalyzers.ConcurrentAnalysis)
                {
                    return await ProducerConsumer<Diagnostic>.RunParallelAsync(
                        source: project.GetAllRegularAndSourceGeneratedDocumentsAsync(cancellationToken),
                        produceItems: static async (document, callback, args, cancellationToken) =>
                        {
                            var (hostCompilationWithAnalyzers, analyzerInfoCache, suppressionAnalyzer) = args;
                            await AnalyzeDocumentAsync(
                                hostCompilationWithAnalyzers, analyzerInfoCache, suppressionAnalyzer,
                                document, span: null, callback, cancellationToken).ConfigureAwait(false);
                        },
                        args: (compilationWithAnalyzers.HostCompilationWithAnalyzers, _analyzerInfoCache, suppressionAnalyzer),
                        cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    using var _ = ArrayBuilder<Diagnostic>.GetInstance(out var diagnosticsBuilder);
                    await foreach (var document in project.GetAllRegularAndSourceGeneratedDocumentsAsync(cancellationToken).ConfigureAwait(false))
                    {
                        await AnalyzeDocumentAsync(
                            compilationWithAnalyzers.HostCompilationWithAnalyzers, _analyzerInfoCache, suppressionAnalyzer,
                            document, span: null, diagnosticsBuilder.Add, cancellationToken).ConfigureAwait(false);
                    }

                    return diagnosticsBuilder.ToImmutableAndClear();
                }
            }

            static async Task AnalyzeDocumentAsync(
                CompilationWithAnalyzers hostCompilationWithAnalyzers,
                DiagnosticAnalyzerInfoCache analyzerInfoCache,
                IPragmaSuppressionsAnalyzer suppressionAnalyzer,
                Document document,
                TextSpan? span,
                Action<Diagnostic> reportDiagnostic,
                CancellationToken cancellationToken)
            {
                var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                await suppressionAnalyzer.AnalyzeAsync(
                    semanticModel, span, hostCompilationWithAnalyzers, analyzerInfoCache.GetDiagnosticDescriptors, reportDiagnostic, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
