﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
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
using Microsoft.CodeAnalysis.Workspaces.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal class InProcOrRemoteHostAnalyzerRunner
    {
        private readonly bool _enabled;
        private readonly IAsynchronousOperationListener _asyncOperationListener;
        public DiagnosticAnalyzerInfoCache AnalyzerInfoCache { get; }

        public InProcOrRemoteHostAnalyzerRunner(
            bool enabled,
            DiagnosticAnalyzerInfoCache analyzerInfoCache,
            IAsynchronousOperationListener? operationListener = null)
        {
            _enabled = enabled;
            AnalyzerInfoCache = analyzerInfoCache;
            _asyncOperationListener = operationListener ?? AsynchronousOperationListenerProvider.NullListener;
        }

        public Task<DiagnosticAnalysisResultMap<DiagnosticAnalyzer, DiagnosticAnalysisResult>> AnalyzeDocumentAsync(
            DocumentAnalysisScope documentAnalysisScope,
            CompilationWithAnalyzersPair compilationWithAnalyzers,
            bool isExplicit,
            bool logPerformanceInfo,
            bool getTelemetryInfo,
            CancellationToken cancellationToken)
            => AnalyzeAsync(documentAnalysisScope, documentAnalysisScope.TextDocument.Project, compilationWithAnalyzers,
                isExplicit, logPerformanceInfo, getTelemetryInfo, cancellationToken);

        public Task<DiagnosticAnalysisResultMap<DiagnosticAnalyzer, DiagnosticAnalysisResult>> AnalyzeProjectAsync(
            Project project,
            CompilationWithAnalyzersPair compilationWithAnalyzers,
            bool logPerformanceInfo,
            bool getTelemetryInfo,
            CancellationToken cancellationToken)
            => AnalyzeAsync(documentAnalysisScope: null, project, compilationWithAnalyzers,
                isExplicit: false, logPerformanceInfo, getTelemetryInfo, cancellationToken);

        private async Task<DiagnosticAnalysisResultMap<DiagnosticAnalyzer, DiagnosticAnalysisResult>> AnalyzeAsync(
            DocumentAnalysisScope? documentAnalysisScope,
            Project project,
            CompilationWithAnalyzersPair compilationWithAnalyzers,
            bool isExplicit,
            bool logPerformanceInfo,
            bool getTelemetryInfo,
            CancellationToken cancellationToken)
        {
            if (!_enabled)
                return DiagnosticAnalysisResultMap<DiagnosticAnalyzer, DiagnosticAnalysisResult>.Empty;

            var result = await AnalyzeCoreAsync().ConfigureAwait(false);
            Debug.Assert(getTelemetryInfo || result.TelemetryInfo.IsEmpty);
            return result;

            async Task<DiagnosticAnalysisResultMap<DiagnosticAnalyzer, DiagnosticAnalysisResult>> AnalyzeCoreAsync()
            {
                Contract.ThrowIfFalse(compilationWithAnalyzers.HasAnalyzers);

                var remoteHostClient = await RemoteHostClient.TryGetClientAsync(project, cancellationToken).ConfigureAwait(false);
                if (remoteHostClient != null)
                {
                    return await AnalyzeOutOfProcAsync(documentAnalysisScope, project, compilationWithAnalyzers, remoteHostClient,
                        isExplicit, logPerformanceInfo, getTelemetryInfo, cancellationToken).ConfigureAwait(false);
                }

                return await AnalyzeInProcAsync(documentAnalysisScope, project, compilationWithAnalyzers,
                    client: null, logPerformanceInfo, getTelemetryInfo, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task<ImmutableArray<Diagnostic>> GetSourceGeneratorDiagnosticsAsync(Project project, CancellationToken cancellationToken)
        {
            if (!_enabled)
                return [];

            var options = project.Solution.Services.GetRequiredService<IWorkspaceConfigurationService>().Options;
            var remoteHostClient = await RemoteHostClient.TryGetClientAsync(project, cancellationToken).ConfigureAwait(false);
            if (remoteHostClient != null)
            {
                var result = await remoteHostClient.TryInvokeAsync<IRemoteDiagnosticAnalyzerService, ImmutableArray<DiagnosticData>>(
                    project.Solution,
                    invocation: (service, solutionInfo, cancellationToken) => service.GetSourceGeneratorDiagnosticsAsync(solutionInfo, project.Id, cancellationToken),
                    cancellationToken).ConfigureAwait(false);

                if (!result.HasValue)
                    return [];

                return await result.Value.ToDiagnosticsAsync(project, cancellationToken).ConfigureAwait(false);
            }

            return await project.GetSourceGeneratorDiagnosticsAsync(cancellationToken).ConfigureAwait(false);
        }

        private async Task<DiagnosticAnalysisResultMap<DiagnosticAnalyzer, DiagnosticAnalysisResult>> AnalyzeInProcAsync(
            DocumentAnalysisScope? documentAnalysisScope,
            Project project,
            CompilationWithAnalyzersPair compilationWithAnalyzers,
            RemoteHostClient? client,
            bool logPerformanceInfo,
            bool getTelemetryInfo,
            CancellationToken cancellationToken)
        {
            var version = await DiagnosticIncrementalAnalyzer.GetDiagnosticVersionAsync(project, cancellationToken).ConfigureAwait(false);

            var (projectAnalysisResult, hostAnalysisResult, additionalPragmaSuppressionDiagnostics) = await compilationWithAnalyzers.GetAnalysisResultAsync(
                documentAnalysisScope, project, AnalyzerInfoCache, cancellationToken).ConfigureAwait(false);

            if (logPerformanceInfo)
            {
                // if remote host is there, report performance data
                var asyncToken = _asyncOperationListener.BeginAsyncOperation(nameof(AnalyzeInProcAsync));
                var _ = FireAndForgetReportAnalyzerPerformanceAsync(documentAnalysisScope, project, client, projectAnalysisResult, hostAnalysisResult, cancellationToken).CompletesAsyncOperation(asyncToken);
            }

            var projectAnalyzers = documentAnalysisScope?.ProjectAnalyzers ?? compilationWithAnalyzers.ProjectAnalyzers;
            var hostAnalyzers = documentAnalysisScope?.HostAnalyzers ?? compilationWithAnalyzers.HostAnalyzers;
            var skippedAnalyzersInfo = project.GetSkippedAnalyzersInfo(AnalyzerInfoCache);

            // get compiler result builder map
            var builderMap = ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResultBuilder>.Empty;
            if (projectAnalysisResult is not null)
            {
                var map = await projectAnalysisResult.ToResultBuilderMapAsync(
                    additionalPragmaSuppressionDiagnostics, documentAnalysisScope, project, version,
                    compilationWithAnalyzers.ProjectCompilation!, projectAnalyzers, skippedAnalyzersInfo,
                    compilationWithAnalyzers.ReportSuppressedDiagnostics, cancellationToken).ConfigureAwait(false);
                builderMap = builderMap.AddRange(map);
            }

            if (hostAnalysisResult is not null)
            {
                var map = await hostAnalysisResult.ToResultBuilderMapAsync(
                    additionalPragmaSuppressionDiagnostics, documentAnalysisScope, project, version,
                    compilationWithAnalyzers.HostCompilation!, hostAnalyzers, skippedAnalyzersInfo,
                    compilationWithAnalyzers.ReportSuppressedDiagnostics, cancellationToken).ConfigureAwait(false);
                builderMap = builderMap.AddRange(map);
            }

            var result = builderMap.ToImmutableDictionary(kv => kv.Key, kv => DiagnosticAnalysisResult.CreateFromBuilder(kv.Value));
            var telemetry = ImmutableDictionary<DiagnosticAnalyzer, AnalyzerTelemetryInfo>.Empty;
            if (getTelemetryInfo)
            {
                if (projectAnalysisResult is not null)
                {
                    telemetry = telemetry.AddRange(projectAnalysisResult.AnalyzerTelemetryInfo);
                }

                if (hostAnalysisResult is not null)
                {
                    telemetry = telemetry.AddRange(hostAnalysisResult.AnalyzerTelemetryInfo);
                }
            }

            return DiagnosticAnalysisResultMap.Create(result, telemetry);
        }

        private async Task FireAndForgetReportAnalyzerPerformanceAsync(
            DocumentAnalysisScope? documentAnalysisScope,
            Project project,
            RemoteHostClient? client,
            AnalysisResult? projectAnalysisResult,
            AnalysisResult? hostAnalysisResult,
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
                var forSpanAnalysis = documentAnalysisScope?.Span.HasValue ?? false;

                ImmutableArray<AnalyzerPerformanceInfo> performanceInfo = [];
                if (projectAnalysisResult is not null)
                {
                    performanceInfo = performanceInfo.AddRange(projectAnalysisResult.AnalyzerTelemetryInfo.ToAnalyzerPerformanceInfo(AnalyzerInfoCache));
                }

                if (hostAnalysisResult is not null)
                {
                    performanceInfo = performanceInfo.AddRange(hostAnalysisResult.AnalyzerTelemetryInfo.ToAnalyzerPerformanceInfo(AnalyzerInfoCache));
                }

                _ = await client.TryInvokeAsync<IRemoteDiagnosticAnalyzerService>(
                    (service, cancellationToken) => service.ReportAnalyzerPerformanceAsync(performanceInfo, count, forSpanAnalysis, cancellationToken),
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
            CompilationWithAnalyzersPair compilationWithAnalyzers,
            RemoteHostClient client,
            bool isExplicit,
            bool logPerformanceInfo,
            bool getTelemetryInfo,
            CancellationToken cancellationToken)
        {
            using var pooledObject1 = SharedPools.Default<Dictionary<string, DiagnosticAnalyzer>>().GetPooledObject();
            using var pooledObject2 = SharedPools.Default<Dictionary<string, DiagnosticAnalyzer>>().GetPooledObject();
            var projectAnalyzerMap = pooledObject1.Object;
            var hostAnalyzerMap = pooledObject2.Object;

            var projectAnalyzers = documentAnalysisScope?.ProjectAnalyzers ?? compilationWithAnalyzers.ProjectAnalyzers;
            var hostAnalyzers = documentAnalysisScope?.HostAnalyzers ?? compilationWithAnalyzers.HostAnalyzers;

            projectAnalyzerMap.AppendAnalyzerMap(projectAnalyzers);
            hostAnalyzerMap.AppendAnalyzerMap(hostAnalyzers);

            if (projectAnalyzerMap.Count == 0 && hostAnalyzerMap.Count == 0)
            {
                return DiagnosticAnalysisResultMap<DiagnosticAnalyzer, DiagnosticAnalysisResult>.Empty;
            }

            var argument = new DiagnosticArguments(
                compilationWithAnalyzers.ReportSuppressedDiagnostics,
                logPerformanceInfo,
                getTelemetryInfo,
                documentAnalysisScope?.TextDocument.Id,
                documentAnalysisScope?.Span,
                documentAnalysisScope?.Kind,
                project.Id,
                [.. projectAnalyzerMap.Keys],
                [.. hostAnalyzerMap.Keys],
                isExplicit);

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
                    entry => IReadOnlyDictionaryExtensions.GetValueOrDefault(projectAnalyzerMap, entry.analyzerId) ?? hostAnalyzerMap[entry.analyzerId],
                    entry => DiagnosticAnalysisResult.Create(
                        project,
                        version,
                        syntaxLocalMap: Hydrate(entry.diagnosticMap.Syntax, project),
                        semanticLocalMap: Hydrate(entry.diagnosticMap.Semantic, project),
                        nonLocalMap: Hydrate(entry.diagnosticMap.NonLocal, project),
                        others: entry.diagnosticMap.Other,
                        documentIds)),
                result.Value.Telemetry.ToImmutableDictionary(
                    entry => IReadOnlyDictionaryExtensions.GetValueOrDefault(projectAnalyzerMap, entry.analyzerId) ?? hostAnalyzerMap[entry.analyzerId],
                    entry => entry.telemetry));
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
