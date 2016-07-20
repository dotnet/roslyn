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
                return new DiagnosticAnalysisResultMap(ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult>.Empty, ImmutableDictionary<DiagnosticAnalyzer, AnalyzerTelemetryInfo>.Empty);
            }

            var hostChecksums = GetHostAnalyzerReferences(snapshotService, _analyzerService.GetHostAnalyzerReferences(), cancellationToken);
            var analyzerMap = CreateAnalyzerMap(analyzerDriver.Analyzers);

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

        private Dictionary<string, DiagnosticAnalyzer> CreateAnalyzerMap(ImmutableArray<DiagnosticAnalyzer> analyzers)
        {
            // TODO: this needs to be cached. we can have 300+ analyzers
            return analyzers.ToDictionary(a => a.GetAnalyzerIdAndVersion().Item1, a => a);
        }
    }
}
