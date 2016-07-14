// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.EngineV2;
using Microsoft.CodeAnalysis.Diagnostics.Telemetry;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.VisualStudio.LanguageServices.Implementation.Remote;
using Microsoft.VisualStudio.LanguageServices.Remote;

namespace Microsoft.VisualStudio.LanguageServices.Diagnostics
{
    [ExportHostSpecificService(typeof(ICompilerDiagnosticExecutor), HostKinds.OutOfProc), Shared]
    internal class DiagnosticOutOfProcHostSpecificService : ICompilerDiagnosticExecutor
    {
        // TODO: solution snapshot tracking for current solution should be its own service
        private SolutionSnapshot _lastSnapshot;

        public async Task<CompilerAnalysisResult> AnalyzeAsync(CompilationWithAnalyzers analyzerDriver, Project project, CancellationToken cancellationToken)
        {
            var solution = project.Solution;

            // TODO: this is just for testing. actual incremental build of solution snapshot should be its own service
            var snapshotService = solution.Workspace.Services.GetService<ISolutionSnapshotService>();

            var lastSnapshot = _lastSnapshot;
            _lastSnapshot = await snapshotService.CreateSnapshotAsync(solution, CancellationToken.None).ConfigureAwait(false);
            lastSnapshot?.Dispose();

            var remoteHost = await solution.Workspace.Services.GetService<IRemoteHostService>().GetRemoteHostAsync(cancellationToken).ConfigureAwait(false);
            if (remoteHost == null)
            {
                // TODO: call inproc version when out of proc can't be used.
                return new CompilerAnalysisResult(ImmutableDictionary<DiagnosticAnalyzer, CodeAnalysis.Diagnostics.EngineV2.AnalysisResult>.Empty, ImmutableDictionary<DiagnosticAnalyzer, AnalyzerTelemetryInfo>.Empty);
            }

            var analyzerMap = CreateAnalyzerMap(analyzerDriver.Analyzers);

            using (var session = await remoteHost.CreateSnapshotSessionAsync(solution, cancellationToken).ConfigureAwait(false))
            using (var client = new JsonRpcClient(await remoteHost.CreateCodeAnalysisServiceStreamAsync(cancellationToken).ConfigureAwait(false)))
            {
                // TODO: change it to use direct stream rather than returning string
                var result = await client.InvokeAsync<string>(
                    WellKnownServiceHubServices.CodeAnalysisService_GetDiagnostics, session.SolutionSnapshot.Id.Checksum.ToArray(), project.Id.Id, project.Id.DebugName, analyzerMap.Keys.ToArray()).ConfigureAwait(false);
            }

            return new CompilerAnalysisResult(ImmutableDictionary<DiagnosticAnalyzer, CodeAnalysis.Diagnostics.EngineV2.AnalysisResult>.Empty, ImmutableDictionary<DiagnosticAnalyzer, AnalyzerTelemetryInfo>.Empty);
        }

        private Dictionary<string, DiagnosticAnalyzer> CreateAnalyzerMap(ImmutableArray<DiagnosticAnalyzer> analyzers)
        {
            // TODO: this needs to be cached. we can have 300+ analyzers
            return analyzers.ToDictionary(a => a.GetAnalyzerIdAndVersion().Item1, a => a);
        }
    }
}
