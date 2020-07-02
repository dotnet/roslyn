﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics.EngineV2;
using Microsoft.CodeAnalysis.Diagnostics.Telemetry;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Workspaces.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal class InProcOrRemoteHostAnalyzerRunner
    {
        private readonly IAsynchronousOperationListener _asyncOperationListener;
        private readonly IDocumentTrackingService? _documentTrackingService;
        public DiagnosticAnalyzerInfoCache AnalyzerInfoCache { get; }

        public InProcOrRemoteHostAnalyzerRunner(
            DiagnosticAnalyzerInfoCache analyzerInfoCache,
            Workspace workspace,
            IAsynchronousOperationListener? operationListener = null)
        {
            AnalyzerInfoCache = analyzerInfoCache;
            _asyncOperationListener = operationListener ?? AsynchronousOperationListenerProvider.NullListener;
            _documentTrackingService = workspace.Services.GetService<IDocumentTrackingService>();
        }

        public Task<DiagnosticAnalysisResultMap<DiagnosticAnalyzer, DiagnosticAnalysisResult>> AnalyzeDocumentAsync(
            DocumentAnalysisScope documentAnalysisScope,
            CompilationWithAnalyzers compilationWithAnalyzers,
            bool logPerformanceInfo,
            bool getTelemetryInfo,
            CancellationToken cancellationToken)
            => AnalyzeAsync(documentAnalysisScope, documentAnalysisScope.Document.Project, compilationWithAnalyzers,
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

                var workspace = project.Solution.Workspace;
                var service = workspace.Services.GetService<IRemoteHostClientProvider>();
                if (service != null)
                {
                    var remoteHostClient = await service.TryGetRemoteHostClientAsync(cancellationToken).ConfigureAwait(false);
                    if (remoteHostClient != null)
                    {
                        return await AnalyzeOutOfProcAsync(documentAnalysisScope, project, compilationWithAnalyzers, remoteHostClient,
                            forceExecuteAllAnalyzers, logPerformanceInfo, getTelemetryInfo, cancellationToken).ConfigureAwait(false);
                    }
                }

                // Either the host doesn't support RemoteHostService (such as under unit test) OR
                // remote host is not running(this can happen if remote host is disabled)
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
            var builderMap = analysisResult.ToResultBuilderMap(
                additionalPragmaSuppressionDiagnostics, documentAnalysisScope, project, version,
                compilationWithAnalyzers.Compilation, analyzers, skippedAnalyzersInfo,
                compilationWithAnalyzers.AnalysisOptions.ReportSuppressedDiagnostics, cancellationToken);

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
                await client.RunRemoteAsync(
                    WellKnownServiceHubService.CodeAnalysis,
                    nameof(IRemoteDiagnosticAnalyzerService.ReportAnalyzerPerformance),
                    solution: null,
                    new object[]
                    {
                            analysisResult.AnalyzerTelemetryInfo.ToAnalyzerPerformanceInfo(AnalyzerInfoCache),
                            // +1 for project itself
                            documentAnalysisScope != null ? 1 : project.DocumentIds.Count + 1
                    },
                    callbackTarget: null,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (FatalError.ReportWithoutCrashUnlessCanceled(ex))
            {
                // ignore all, this is fire and forget method
            }
        }

        private async Task<DiagnosticAnalysisResultMap<DiagnosticAnalyzer, DiagnosticAnalysisResult>> AnalyzeOutOfProcAsync(
            DocumentAnalysisScope? documentAnalysisScope,
            Project project,
            CompilationWithAnalyzers compilationWithAnalyzers,
            RemoteHostClient client,
            bool forceExecuteAllAnalyzers,
            bool logPerformanceInfo,
            bool getTelemetryInfo,
            CancellationToken cancellationToken)
        {
            var solution = project.Solution;

            using var pooledObject = SharedPools.Default<Dictionary<string, DiagnosticAnalyzer>>().GetPooledObject();
            var analyzerMap = pooledObject.Object;

            var analyzers = documentAnalysisScope?.Analyzers ??
                compilationWithAnalyzers.Analyzers.Where(a => forceExecuteAllAnalyzers || !a.IsOpenFileOnly(solution.Options));
            analyzerMap.AppendAnalyzerMap(analyzers);

            if (analyzerMap.Count == 0)
            {
                return DiagnosticAnalysisResultMap<DiagnosticAnalyzer, DiagnosticAnalysisResult>.Empty;
            }

            // Use high priority if we are force executing all analyzers for user action OR serving an active document request.
            var isHighPriority = forceExecuteAllAnalyzers ||
                documentAnalysisScope != null && _documentTrackingService?.TryGetActiveDocument() == documentAnalysisScope.Document.Id;

            var argument = new DiagnosticArguments(
                isHighPriority,
                compilationWithAnalyzers.AnalysisOptions.ReportSuppressedDiagnostics,
                logPerformanceInfo,
                getTelemetryInfo,
                documentAnalysisScope?.Document.Id,
                documentAnalysisScope?.Span,
                documentAnalysisScope?.Kind,
                project.Id,
                analyzerMap.Keys.ToArray());

            return await client.RunRemoteAsync(
                WellKnownServiceHubService.CodeAnalysis,
                nameof(IRemoteDiagnosticAnalyzerService.CalculateDiagnosticsAsync),
                solution,
                new object[] { argument },
                callbackTarget: null,
                (s, c) => ReadCompilerAnalysisResultAsync(s, analyzerMap, project, c),
                cancellationToken).ConfigureAwait(false);
        }

        private static async Task<DiagnosticAnalysisResultMap<DiagnosticAnalyzer, DiagnosticAnalysisResult>> ReadCompilerAnalysisResultAsync(Stream stream, Dictionary<string, DiagnosticAnalyzer> analyzerMap, Project project, CancellationToken cancellationToken)
        {
            // handling of cancellation and exception
            var version = await DiagnosticIncrementalAnalyzer.GetDiagnosticVersionAsync(project, cancellationToken).ConfigureAwait(false);

            using var reader = ObjectReader.TryGetReader(stream, leaveOpen: true, cancellationToken);

            // We only get a reader for data transmitted between live processes.
            // This data should always be correct as we're never persisting the data between sessions.
            Contract.ThrowIfNull(reader);

            return DiagnosticResultSerializer.ReadDiagnosticAnalysisResults(reader, analyzerMap, project, version, cancellationToken);
        }
    }
}
