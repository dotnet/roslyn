// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Workspaces.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV2
{
    internal partial class DiagnosticIncrementalAnalyzer
    {
        // internal for testing
        internal class InProcOrRemoteHostAnalyzerRunner
        {
            private readonly IAsynchronousOperationListener _asyncOperationListener;
            private readonly DiagnosticAnalyzerInfoCache _analyzerInfoCache;
            private readonly AbstractHostDiagnosticUpdateSource _hostDiagnosticUpdateSource;

            // TODO: this should be removed once we move options down to compiler layer
            private readonly ConcurrentDictionary<string, ValueTuple<OptionSet, CustomAsset>> _lastOptionSetPerLanguage;

            public InProcOrRemoteHostAnalyzerRunner(IAsynchronousOperationListener operationListener, DiagnosticAnalyzerInfoCache analyzerInfoCache, AbstractHostDiagnosticUpdateSource hostDiagnosticUpdateSource)
            {
                _asyncOperationListener = operationListener;
                _analyzerInfoCache = analyzerInfoCache;
                _hostDiagnosticUpdateSource = hostDiagnosticUpdateSource;

                // currently option is a bit weird since it is not part of snapshot and 
                // we can't load all options without loading all language specific dlls.
                // we have tracking issue for this.
                // https://github.com/dotnet/roslyn/issues/13643
                _lastOptionSetPerLanguage = new ConcurrentDictionary<string, ValueTuple<OptionSet, CustomAsset>>();
            }

            public async Task<DiagnosticAnalysisResultMap<DiagnosticAnalyzer, DiagnosticAnalysisResult>> AnalyzeAsync(CompilationWithAnalyzers compilation, Project project, bool forcedAnalysis, CancellationToken cancellationToken)
            {
                Contract.ThrowIfFalse(compilation.Analyzers.Length != 0);

                var workspace = project.Solution.Workspace;
                var service = workspace.Services.GetService<IRemoteHostClientService>();
                if (service == null)
                {
                    // host doesn't support RemoteHostService such as under unit test
                    return await AnalyzeInProcAsync(compilation, project, client: null, cancellationToken).ConfigureAwait(false);
                }

                var remoteHostClient = await service.TryGetRemoteHostClientAsync(cancellationToken).ConfigureAwait(false);
                if (remoteHostClient == null)
                {
                    // remote host is not running. this can happen if remote host is disabled.
                    return await AnalyzeInProcAsync(compilation, project, client: null, cancellationToken).ConfigureAwait(false);
                }

                // out of proc analysis will use 2 source of analyzers. one is AnalyzerReference from project (nuget). and the other is host analyzers (vsix) 
                // that are not part of roslyn solution. these host analyzers must be sync to OOP before hand by the Host. 
                return await AnalyzeOutOfProcAsync(remoteHostClient, compilation, project, forcedAnalysis, cancellationToken).ConfigureAwait(false);
            }

            private async Task<DiagnosticAnalysisResultMap<DiagnosticAnalyzer, DiagnosticAnalysisResult>> AnalyzeInProcAsync(
                CompilationWithAnalyzers compilation, Project project, RemoteHostClient? client, CancellationToken cancellationToken)
            {
                Debug.Assert(compilation.Analyzers.Length != 0);

                var version = await GetDiagnosticVersionAsync(project, cancellationToken).ConfigureAwait(false);

                // PERF: Run all analyzers at once using the new GetAnalysisResultAsync API.
                var analysisResult = await compilation.GetAnalysisResultAsync(cancellationToken).ConfigureAwait(false);

                // if remote host is there, report performance data
                var asyncToken = _asyncOperationListener.BeginAsyncOperation(nameof(AnalyzeInProcAsync));
                var _ = FireAndForgetReportAnalyzerPerformanceAsync(project, client, analysisResult, cancellationToken).CompletesAsyncOperation(asyncToken);

                // get compiler result builder map
                var builderMap = analysisResult.ToResultBuilderMap(project, version, compilation.Compilation, compilation.Analyzers, cancellationToken);

                return DiagnosticAnalysisResultMap.Create(
                    builderMap.ToImmutableDictionary(kv => kv.Key, kv => DiagnosticAnalysisResult.CreateFromBuilder(kv.Value)),
                    analysisResult.AnalyzerTelemetryInfo);
            }

            private async Task FireAndForgetReportAnalyzerPerformanceAsync(Project project, RemoteHostClient? client, AnalysisResult analysisResult, CancellationToken cancellationToken)
            {
                if (client == null)
                {
                    return;
                }

                try
                {
                    _ = await client.TryRunRemoteAsync(
                        WellKnownServiceHubServices.CodeAnalysisService,
                        nameof(IRemoteDiagnosticAnalyzerService.ReportAnalyzerPerformance),
                        new object[]
                        {
                            analysisResult.AnalyzerTelemetryInfo.ToAnalyzerPerformanceInfo(_analyzerInfoCache),
                            // +1 for project itself
                            project.DocumentIds.Count + 1
                        },
                        solution: null,
                        callbackTarget: null,
                        cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (FatalError.ReportWithoutCrashUnlessCanceled(ex))
                {
                    // ignore all, this is fire and forget method
                }
            }

            private async Task<DiagnosticAnalysisResultMap<DiagnosticAnalyzer, DiagnosticAnalysisResult>> AnalyzeOutOfProcAsync(
                RemoteHostClient client, CompilationWithAnalyzers analyzerDriver, Project project, bool forcedAnalysis, CancellationToken cancellationToken)
            {
                var solution = project.Solution;
                var snapshotService = solution.Workspace.Services.GetService<IRemotableDataService>();

                using var pooledObject = SharedPools.Default<Dictionary<string, DiagnosticAnalyzer>>().GetPooledObject();
                var analyzerMap = pooledObject.Object;

                analyzerMap.AppendAnalyzerMap(analyzerDriver.Analyzers.Where(a => forcedAnalysis || !a.IsOpenFileOnly(solution.Options)));
                if (analyzerMap.Count == 0)
                {
                    return DiagnosticAnalysisResultMap<DiagnosticAnalyzer, DiagnosticAnalysisResult>.Empty;
                }

                var argument = new DiagnosticArguments(
                    forcedAnalysis, analyzerDriver.AnalysisOptions.ReportSuppressedDiagnostics, analyzerDriver.AnalysisOptions.LogAnalyzerExecutionTime,
                    project.Id, analyzerMap.Keys.ToArray());

                using var session = await client.TryCreateSessionAsync(WellKnownServiceHubServices.CodeAnalysisService, solution, callbackTarget: null, cancellationToken).ConfigureAwait(false);
                if (session == null)
                {
                    // session is not available
                    return DiagnosticAnalysisResultMap<DiagnosticAnalyzer, DiagnosticAnalysisResult>.Empty;
                }

                return await session.Connection.InvokeAsync(
                    nameof(IRemoteDiagnosticAnalyzerService.CalculateDiagnosticsAsync),
                    new object[] { argument },
                    (stream, cancellationToken) => ReadCompilerAnalysisResultAsync(stream, analyzerMap, project, cancellationToken), cancellationToken).ConfigureAwait(false);
            }

            private async Task<DiagnosticAnalysisResultMap<DiagnosticAnalyzer, DiagnosticAnalysisResult>> ReadCompilerAnalysisResultAsync(Stream stream, Dictionary<string, DiagnosticAnalyzer> analyzerMap, Project project, CancellationToken cancellationToken)
            {
                // handling of cancellation and exception
                var version = await GetDiagnosticVersionAsync(project, cancellationToken).ConfigureAwait(false);

                using var reader = ObjectReader.TryGetReader(stream, leaveOpen: true, cancellationToken);

                // We only get a reader for data transmitted between live processes.
                // This data should always be correct as we're never persisting the data between sessions.
                Contract.ThrowIfNull(reader);

                return DiagnosticResultSerializer.ReadDiagnosticAnalysisResults(reader, analyzerMap, project, version, cancellationToken);
            }
        }
    }
}
