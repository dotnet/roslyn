// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.EngineV2;
using Microsoft.CodeAnalysis.Diagnostics.Telemetry;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Remote.Diagnostics;
using Microsoft.CodeAnalysis.Workspaces.Diagnostics;
using Microsoft.VisualStudio.LanguageServices.Implementation.Remote;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Diagnostics
{
    [ExportHostSpecificService(typeof(ICompilerDiagnosticExecutor), HostKinds.OutOfProc), Shared]
    internal class DiagnosticOutOfProcHostSpecificService : ICompilerDiagnosticExecutor
    {
        private readonly IDiagnosticAnalyzerService _analyzerService;

        // TODO: solution snapshot tracking for current solution should be its own service
        private SolutionSnapshot _lastSnapshot;

        [ImportingConstructor]
        public DiagnosticOutOfProcHostSpecificService(IDiagnosticAnalyzerService analyzerService)
        {
            _analyzerService = analyzerService;
        }

        public async Task<DiagnosticAnalysisResultMap> AnalyzeAsync(CompilationWithAnalyzers analyzerDriver, Project project, CancellationToken cancellationToken)
        {
            // TODO: later, make sure we can run all analyzer on remote host. 
            //       for now, we will check whether built in analyzer can run on remote host and only those run on remote host.
            var inProcResult = await AnalyzeInProcAsync(CreateAnalyzerDriver(analyzerDriver, a => a.MustRunInProc()), project, cancellationToken).ConfigureAwait(false);
            var outOfProcResult = await AnalyzeOutOfProcAsync(analyzerDriver, project, cancellationToken).ConfigureAwait(false);

            // merge 2 results
            return new DiagnosticAnalysisResultMap(
                inProcResult.AnalysisResult.AddRange(outOfProcResult.AnalysisResult),
                inProcResult.TelemetryInfo.AddRange(outOfProcResult.TelemetryInfo));
        }

        private async Task<DiagnosticAnalysisResultMap> AnalyzeOutOfProcAsync(CompilationWithAnalyzers analyzerDriver, Project project, CancellationToken cancellationToken)
        {
            var solution = project.Solution;

            var snapshotService = solution.Workspace.Services.GetService<ISolutionSnapshotService>();

            // TODO: this is just for testing. actual incremental build of solution snapshot should be its own service
            await UpdateLastSolutionSnapshotAsync(snapshotService, solution).ConfigureAwait(false);

            var remoteHost = await solution.Workspace.Services.GetService<IRemoteHostService>().GetRemoteHostAsync(cancellationToken).ConfigureAwait(false);
            if (remoteHost == null)
            {
                return await AnalyzeInProcAsync(CreateAnalyzerDriver(analyzerDriver, a => !a.MustRunInProc()), project, cancellationToken).ConfigureAwait(false);
            }

            // TODO: this should be moved out
            var hostChecksums = GetHostAnalyzerReferences(snapshotService, _analyzerService.GetHostAnalyzerReferences(), cancellationToken);
            var analyzerMap = CreateAnalyzerMap(analyzerDriver.Analyzers.Where(a => !a.MustRunInProc()));
            if (analyzerMap.Count == 0)
            {
                return new DiagnosticAnalysisResultMap(ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult>.Empty, ImmutableDictionary<DiagnosticAnalyzer, AnalyzerTelemetryInfo>.Empty);
            }

            using (var session = await remoteHost.CreateCodeAnalysisServiceSessionAsync(solution, cancellationToken).ConfigureAwait(false))
            {
                return await session.InvokeAsync(
                    WellKnownServiceHubServices.CodeAnalysisService_GetDiagnostics,
                    new object[] {
                        session.SolutionSnapshot.Id.Checksum.ToArray(),
                        project.Id.Id,
                        project.Id.DebugName,
                        hostChecksums.ToArray(),
                        analyzerMap.Keys.ToArray() },
                    (s, c) => GetCompilerAnalysisResultAsync(s, analyzerMap, project, c), cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task UpdateLastSolutionSnapshotAsync(ISolutionSnapshotService snapshotService, Solution solution)
        {
            // TODO: this is just for testing. actual incremental build of solution snapshot should be its own service
            var lastSnapshot = _lastSnapshot;
            _lastSnapshot = await snapshotService.CreateSnapshotAsync(solution, CancellationToken.None).ConfigureAwait(false);
            lastSnapshot?.Dispose();
        }

        private static async Task<DiagnosticAnalysisResultMap> AnalyzeInProcAsync(CompilationWithAnalyzers analyzerDriver, Project project, CancellationToken cancellationToken)
        {
            var inProcExecutor = project.Solution.Workspace.Services.GetHostSpecificServiceAvailable<ICompilerDiagnosticExecutor>(HostKinds.InProc);
            return await inProcExecutor.AnalyzeAsync(analyzerDriver, project, cancellationToken).ConfigureAwait(false);
        }

        private CompilationWithAnalyzers CreateAnalyzerDriver(CompilationWithAnalyzers analyzerDriver, Func<DiagnosticAnalyzer, bool> predicate)
        {
            var analyzers = analyzerDriver.Analyzers.Where(predicate).ToImmutableArray();
            return analyzerDriver.Compilation.WithAnalyzers(analyzers, analyzerDriver.AnalysisOptions);
        }

        private async Task<DiagnosticAnalysisResultMap> GetCompilerAnalysisResultAsync(Stream stream, Dictionary<string, DiagnosticAnalyzer> analyzerMap, Project project, CancellationToken cancellationToken)
        {
            var version = await DiagnosticIncrementalAnalyzer.GetDiagnosticVersionAsync(project, cancellationToken).ConfigureAwait(false);

            using (var reader = new ObjectReader(stream))
            {
                return DiagnosticResultSerializer.Deserialize(reader, analyzerMap, project, version, cancellationToken);
            }
        }

        private ImmutableArray<byte[]> GetHostAnalyzerReferences(ISolutionSnapshotService snapshotService, IEnumerable<AnalyzerReference> references, CancellationToken cancellationToken)
        {
            // TODO: cache this to somewhere
            var builder = ImmutableArray.CreateBuilder<byte[]>();
            foreach (var reference in references)
            {
                var asset = snapshotService.GetGlobalAsset(reference, cancellationToken);
                builder.Add(asset.Checksum.ToArray());
            }

            return builder.ToImmutable();
        }

        private Dictionary<string, DiagnosticAnalyzer> CreateAnalyzerMap(IEnumerable<DiagnosticAnalyzer> analyzers)
        {
            // TODO: this needs to be cached. we can have 300+ analyzers
            return analyzers.ToDictionary(a => a.GetAnalyzerIdAndVersion().Item1, a => a);
        }
    }
}
