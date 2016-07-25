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
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Remote.Diagnostics;
using Microsoft.CodeAnalysis.Workspaces.Diagnostics;
using Microsoft.VisualStudio.LanguageServices.Implementation.Remote;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Diagnostics
{
    [ExportWorkspaceService(typeof(ICompilerDiagnosticAnalyzer), layer: ServiceLayer.Host), Shared]
    internal class OutOfProcCompilerDiagnosticAnalyzer : ICompilerDiagnosticAnalyzer
    {
        private static readonly ICompilerDiagnosticAnalyzer _inProcAnalyzer = new InProcCompilerDiagnosticAnalyzer();

        private readonly IDiagnosticAnalyzerService _analyzerService;

        // TODO: solution snapshot tracking for current solution should be its own service
        private ChecksumScope _lastSnapshot;

        [ImportingConstructor]
        public OutOfProcCompilerDiagnosticAnalyzer(IDiagnosticAnalyzerService analyzerService)
        {
            _analyzerService = analyzerService;
        }

        public async Task<DiagnosticAnalysisResultMap<DiagnosticAnalyzer, DiagnosticAnalysisResult>> AnalyzeAsync(CompilationWithAnalyzers analyzerDriver, Project project, CancellationToken cancellationToken)
        {
            var remoteHostClient = await project.Solution.Workspace.Services.GetService<IRemoteHostClientService>().GetRemoteHostClientAsync(cancellationToken).ConfigureAwait(false);
            if (remoteHostClient == null)
            {
                // remote host is not running. this can happen if remote host is disabled.
                return await AnalyzeInProcAsync(analyzerDriver, project, cancellationToken).ConfigureAwait(false);
            }

            // TODO: later, make sure we can run all analyzer on remote host. 
            //       for now, we will check whether built in analyzer can run on remote host and only those run on remote host.
            var inProcResult = await AnalyzeInProcAsync(CreateAnalyzerDriver(analyzerDriver, a => a.MustRunInProc()), project, cancellationToken).ConfigureAwait(false);
            var outOfProcResult = await AnalyzeOutOfProcAsync(remoteHostClient, analyzerDriver, project, cancellationToken).ConfigureAwait(false);

            // merge 2 results
            return DiagnosticAnalysisResultMap.Create(
                inProcResult.AnalysisResult.AddRange(outOfProcResult.AnalysisResult),
                inProcResult.TelemetryInfo.AddRange(outOfProcResult.TelemetryInfo));
        }

        private async Task<DiagnosticAnalysisResultMap<DiagnosticAnalyzer, DiagnosticAnalysisResult>> AnalyzeInProcAsync(CompilationWithAnalyzers analyzerDriver, Project project, CancellationToken cancellationToken)
        {
            return await _inProcAnalyzer.AnalyzeAsync(analyzerDriver, project, cancellationToken).ConfigureAwait(false);
        }

        private async Task<DiagnosticAnalysisResultMap<DiagnosticAnalyzer, DiagnosticAnalysisResult>> AnalyzeOutOfProcAsync(
            RemoteHostClient client, CompilationWithAnalyzers analyzerDriver, Project project, CancellationToken cancellationToken)
        {
            var solution = project.Solution;

            var snapshotService = solution.Workspace.Services.GetService<ISolutionChecksumService>();

            // TODO: incremental build of solution snapshot should be its own service
            await UpdateLastSolutionSnapshotAsync(snapshotService, solution).ConfigureAwait(false);

            // TODO: this should be moved out
            var hostChecksums = GetHostAnalyzerReferences(snapshotService, _analyzerService.GetHostAnalyzerReferences(), cancellationToken);
            var analyzerMap = CreateAnalyzerMap(analyzerDriver.Analyzers.Where(a => !a.MustRunInProc()));
            if (analyzerMap.Count == 0)
            {
                return DiagnosticAnalysisResultMap.Create(ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult>.Empty, ImmutableDictionary<DiagnosticAnalyzer, AnalyzerTelemetryInfo>.Empty);
            }

            // TODO: send telemetry on session
            using (var session = await client.CreateCodeAnalysisServiceSessionAsync(solution, cancellationToken).ConfigureAwait(false))
            {
                var argument = new DiagnosticArguments()
                {
                    ReportSuppressedDiagnostics = analyzerDriver.AnalysisOptions.ReportSuppressedDiagnostics,
                    LogAnalyzerExecutionTime = analyzerDriver.AnalysisOptions.LogAnalyzerExecutionTime,
                    ProjectIdGuid = project.Id.Id,
                    ProjectIdDebugName = project.Id.DebugName,
                    HostAnalyzerChecksumsByteArray = hostChecksums.ToArray(),
                    AnalyzerIds = analyzerMap.Keys.ToArray()
                };

                return await session.InvokeAsync(
                    WellKnownServiceHubServices.CodeAnalysisService_CalculateDiagnosticsAsync,
                    new object[] { argument },
                    (s, c) => GetCompilerAnalysisResultAsync(s, analyzerMap, project, c)).ConfigureAwait(false);
            }
        }

        private async Task UpdateLastSolutionSnapshotAsync(ISolutionChecksumService snapshotService, Solution solution)
        {
            // TODO: actual incremental build of solution snapshot should be its own service
            // this is needed to make sure we incrementally update solution checksums. otherwise, we will always create from
            // scratch which can be quite expansive for big solution
            var lastSnapshot = _lastSnapshot;
            _lastSnapshot = await snapshotService.CreateChecksumAsync(solution, CancellationToken.None).ConfigureAwait(false);
            lastSnapshot?.Dispose();
        }

        private CompilationWithAnalyzers CreateAnalyzerDriver(CompilationWithAnalyzers analyzerDriver, Func<DiagnosticAnalyzer, bool> predicate)
        {
            var analyzers = analyzerDriver.Analyzers.Where(predicate).ToImmutableArray();
            return analyzerDriver.Compilation.WithAnalyzers(analyzers, analyzerDriver.AnalysisOptions);
        }

        private async Task<DiagnosticAnalysisResultMap<DiagnosticAnalyzer, DiagnosticAnalysisResult>> GetCompilerAnalysisResultAsync(Stream stream, Dictionary<string, DiagnosticAnalyzer> analyzerMap, Project project, CancellationToken cancellationToken)
        {
            // handling of cancellation and exception
            var version = await DiagnosticIncrementalAnalyzer.GetDiagnosticVersionAsync(project, cancellationToken).ConfigureAwait(false);

            using (var reader = new ObjectReader(stream))
            {
                return DiagnosticResultSerializer.Deserialize(reader, analyzerMap, project, version, cancellationToken);
            }
        }

        private ImmutableArray<byte[]> GetHostAnalyzerReferences(ISolutionChecksumService snapshotService, IEnumerable<AnalyzerReference> references, CancellationToken cancellationToken)
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
