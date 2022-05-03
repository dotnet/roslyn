// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics.EngineV2;
using Microsoft.CodeAnalysis.Diagnostics.Telemetry;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Workspaces.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal class InProcOrRemoteHostAnalyzerRunner
    {
        private readonly IAsynchronousOperationListener _asyncOperationListener;
        public DiagnosticAnalyzerInfoCache AnalyzerInfoCache { get; }

        public InProcOrRemoteHostAnalyzerRunner(
            DiagnosticAnalyzerInfoCache analyzerInfoCache,
            IAsynchronousOperationListener? operationListener = null)
        {
            AnalyzerInfoCache = analyzerInfoCache;
            _asyncOperationListener = operationListener ?? AsynchronousOperationListenerProvider.NullListener;
        }

        public Task<DiagnosticAnalysisResultMap<DiagnosticAnalyzer, DiagnosticAnalysisResult>> AnalyzeDocumentAsync(
            DocumentAnalysisScope documentAnalysisScope,
            CompilationWithAnalyzers compilationWithAnalyzers,
            bool logPerformanceInfo,
            bool getTelemetryInfo,
            CancellationToken cancellationToken)
            => AnalyzeAsync(documentAnalysisScope, documentAnalysisScope.TextDocument.Project, compilationWithAnalyzers,
                forceExecuteAllAnalyzers: false, logPerformanceInfo, getTelemetryInfo, cancellationToken);

        public Task<DiagnosticAnalysisResultMap<DiagnosticAnalyzer, DiagnosticAnalysisResult>> AnalyzeProjectAsync(
            Project project,
            CompilationWithAnalyzers compilationWithAnalyzers,
            bool forceExecuteAllAnalyzers,
            bool logPerformanceInfo,
            bool getTelemetryInfo,
            CancellationToken cancellationToken)
            => AnalyzeAsync(documentAnalysisScope: null, project, compilationWithAnalyzers,
                forceExecuteAllAnalyzers, logPerformanceInfo, getTelemetryInfo, cancellationToken);

        private async Task<DiagnosticAnalysisResultMap<DiagnosticAnalyzer, DiagnosticAnalysisResult>> AnalyzeAsync(
            DocumentAnalysisScope? documentAnalysisScope,
            Project project,
            CompilationWithAnalyzers compilationWithAnalyzers,
            bool forceExecuteAllAnalyzers,
            bool logPerformanceInfo,
            bool getTelemetryInfo,
            CancellationToken cancellationToken)
        {
            var result = await AnalyzeCoreAsync().ConfigureAwait(false);
            Debug.Assert(getTelemetryInfo || result.TelemetryInfo.IsEmpty);
            return result;

            async Task<DiagnosticAnalysisResultMap<DiagnosticAnalyzer, DiagnosticAnalysisResult>> AnalyzeCoreAsync()
            {
                Contract.ThrowIfFalse(!compilationWithAnalyzers.Analyzers.IsEmpty);

                var remoteHostClient = await RemoteHostClient.TryGetClientAsync(project, cancellationToken).ConfigureAwait(false);
                if (remoteHostClient != null)
                {
                    return await AnalyzeOutOfProcAsync(documentAnalysisScope, project, compilationWithAnalyzers, remoteHostClient,
                        forceExecuteAllAnalyzers, logPerformanceInfo, getTelemetryInfo, cancellationToken).ConfigureAwait(false);
                }

                return await AnalyzeInProcAsync(documentAnalysisScope, project, compilationWithAnalyzers,
                    client: null, logPerformanceInfo, getTelemetryInfo, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task<DiagnosticAnalysisResultMap<DiagnosticAnalyzer, DiagnosticAnalysisResult>> AnalyzeInProcAsync(
            DocumentAnalysisScope? documentAnalysisScope,
            Project project,
            CompilationWithAnalyzers compilationWithAnalyzers,
            RemoteHostClient? client,
            bool logPerformanceInfo,
            bool getTelemetryInfo,
            CancellationToken cancellationToken)
        {
            var version = await DiagnosticIncrementalAnalyzer.GetDiagnosticVersionAsync(project, cancellationToken).ConfigureAwait(false);

            var (analysisResult, additionalPragmaSuppressionDiagnostics) = await compilationWithAnalyzers.GetAnalysisResultAsync(
                documentAnalysisScope, project, AnalyzerInfoCache, cancellationToken).ConfigureAwait(false);

            if (logPerformanceInfo)
            {
                // if remote host is there, report performance data
                var asyncToken = _asyncOperationListener.BeginAsyncOperation(nameof(AnalyzeInProcAsync));
                var _ = FireAndForgetReportAnalyzerPerformanceAsync(documentAnalysisScope, project, client, analysisResult, cancellationToken).CompletesAsyncOperation(asyncToken);
            }

            var analyzers = documentAnalysisScope?.Analyzers ?? compilationWithAnalyzers.Analyzers;
            var skippedAnalyzersInfo = project.GetSkippedAnalyzersInfo(AnalyzerInfoCache);

            // get compiler result builder map
            var builderMap = await analysisResult.ToResultBuilderMapAsync(
                additionalPragmaSuppressionDiagnostics, documentAnalysisScope, project, version,
                compilationWithAnalyzers.Compilation, analyzers, skippedAnalyzersInfo,
                compilationWithAnalyzers.AnalysisOptions.ReportSuppressedDiagnostics, cancellationToken).ConfigureAwait(false);

            var result = builderMap.ToImmutableDictionary(kv => kv.Key, kv => DiagnosticAnalysisResult.CreateFromBuilder(kv.Value));
            var telemetry = getTelemetryInfo
                ? analysisResult.AnalyzerTelemetryInfo
                : ImmutableDictionary<DiagnosticAnalyzer, AnalyzerTelemetryInfo>.Empty;
            return DiagnosticAnalysisResultMap.Create(result, telemetry);
        }

        private async Task FireAndForgetReportAnalyzerPerformanceAsync(
            DocumentAnalysisScope? documentAnalysisScope,
            Project project,
            RemoteHostClient? client,
            AnalysisResult analysisResult,
            CancellationToken cancellationToken)
        {
            if (client == null)
            {
                return;
            }

            try
            {
                // +1 for project itself
                var count = documentAnalysisScope != null ? 1 : project.DocumentIds.Count + 1;

                var performanceInfo = analysisResult.AnalyzerTelemetryInfo.ToAnalyzerPerformanceInfo(AnalyzerInfoCache).ToImmutableArray();

                _ = await client.TryInvokeAsync<IRemoteDiagnosticAnalyzerService>(
                    (service, cancellationToken) => service.ReportAnalyzerPerformanceAsync(performanceInfo, count, cancellationToken),
                    cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (FatalError.ReportAndCatchUnlessCanceled(ex, cancellationToken))
            {
                // ignore all, this is fire and forget method
            }
        }

        private static async Task<DiagnosticAnalysisResultMap<DiagnosticAnalyzer, DiagnosticAnalysisResult>> AnalyzeOutOfProcAsync(
            DocumentAnalysisScope? documentAnalysisScope,
            Project project,
            CompilationWithAnalyzers compilationWithAnalyzers,
            RemoteHostClient client,
            bool forceExecuteAllAnalyzers,
            bool logPerformanceInfo,
            bool getTelemetryInfo,
            CancellationToken cancellationToken)
        {
            using var pooledObject = SharedPools.Default<Dictionary<string, DiagnosticAnalyzer>>().GetPooledObject();
            var analyzerMap = pooledObject.Object;

            var ideOptions = ((WorkspaceAnalyzerOptions)compilationWithAnalyzers.AnalysisOptions.Options!).IdeOptions;

            var analyzers = documentAnalysisScope?.Analyzers ??
                compilationWithAnalyzers.Analyzers.Where(a => forceExecuteAllAnalyzers || !a.IsOpenFileOnly(ideOptions.CleanupOptions?.SimplifierOptions));

            analyzerMap.AppendAnalyzerMap(analyzers);

            if (analyzerMap.Count == 0)
            {
                return DiagnosticAnalysisResultMap<DiagnosticAnalyzer, DiagnosticAnalysisResult>.Empty;
            }

            var argument = new DiagnosticArguments(
                compilationWithAnalyzers.AnalysisOptions.ReportSuppressedDiagnostics,
                logPerformanceInfo,
                getTelemetryInfo,
                documentAnalysisScope?.TextDocument.Id,
                documentAnalysisScope?.Span,
                documentAnalysisScope?.Kind,
                project.Id,
                analyzerMap.Keys.ToArray(),
                ideOptions);

            var result = await client.TryInvokeAsync<IRemoteDiagnosticAnalyzerService, SerializableDiagnosticAnalysisResults>(
                project.Solution,
                invocation: (service, solutionInfo, cancellationToken) => service.CalculateDiagnosticsAsync(solutionInfo, argument, cancellationToken),
                cancellationToken).ConfigureAwait(false);

            if (!result.HasValue)
            {
                return DiagnosticAnalysisResultMap<DiagnosticAnalyzer, DiagnosticAnalysisResult>.Empty;
            }

            // handling of cancellation and exception
            var version = await DiagnosticIncrementalAnalyzer.GetDiagnosticVersionAsync(project, cancellationToken).ConfigureAwait(false);

            var documentIds = (documentAnalysisScope != null) ? ImmutableHashSet.Create(documentAnalysisScope.TextDocument.Id) : null;

            return new DiagnosticAnalysisResultMap<DiagnosticAnalyzer, DiagnosticAnalysisResult>(
                result.Value.Diagnostics.ToImmutableDictionary(
                    entry => analyzerMap[entry.analyzerId],
                    entry => DiagnosticAnalysisResult.Create(
                        project,
                        version,
                        syntaxLocalMap: Hydrate(entry.diagnosticMap.Syntax, project),
                        semanticLocalMap: Hydrate(entry.diagnosticMap.Semantic, project),
                        nonLocalMap: Hydrate(entry.diagnosticMap.NonLocal, project),
                        others: entry.diagnosticMap.Other,
                        documentIds)),
                result.Value.Telemetry.ToImmutableDictionary(entry => analyzerMap[entry.analyzerId], entry => entry.telemetry));
        }

        // TODO: filter in OOP https://github.com/dotnet/roslyn/issues/47859
        private static ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>> Hydrate(ImmutableArray<(DocumentId documentId, ImmutableArray<DiagnosticData> diagnostics)> diagnosticByDocument, Project project)
            => diagnosticByDocument
                .Where(
                    entry =>
                    {
                        // Source generated documents (for which GetTextDocument returns null) support diagnostics. Only
                        // filter out diagnostics where the document is non-null and SupportDiagnostics() is false.
                        return project.GetTextDocument(entry.documentId)?.SupportsDiagnostics() != false;
                    })
                .ToImmutableDictionary(entry => entry.documentId, entry => entry.diagnostics);
    }
}
