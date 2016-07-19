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

            var hostChecksums = GetHostAnalyzerReferences(snapshotService, _analyzerService.GetHostAnalyzerReferences(), cancellationToken);
            var analyzerMap = CreateAnalyzerMap(analyzerDriver.Analyzers);

            using (var session = await remoteHost.CreateCodeAnalysisServiceSessionAsync(solution, cancellationToken).ConfigureAwait(false))
            {
                return await session.InvokeAsync<CompilerAnalysisResult>(
                    WellKnownServiceHubServices.CodeAnalysisService_GetDiagnostics,
                    new object[] {
                        session.SolutionSnapshot.Id.Checksum.ToArray(),
                        project.Id.Id,
                        project.Id.DebugName,
                        hostChecksums.ToArray(),
                        analyzerMap.Keys.ToArray() },
                    (s, c) => GetCompilerAnalysisResultAsync(analyzerMap, project, s, c), cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task<CompilerAnalysisResult> GetCompilerAnalysisResultAsync(Dictionary<string, DiagnosticAnalyzer> analyzerMap, Project project, Stream stream, CancellationToken cancellationToken)
        {
            var version = await DiagnosticIncrementalAnalyzer.GetDiagnosticVersionAsync(project, cancellationToken).ConfigureAwait(false);

            var diagnosticDataSerializer = new DiagnosticDataSerializer(VersionStamp.Default, VersionStamp.Default);

            using (var reader = new ObjectReader(stream))
            {
                var analysisCount = reader.ReadInt32();

                var analysisMap = ImmutableDictionary.CreateBuilder<DiagnosticAnalyzer, CodeAnalysis.Diagnostics.EngineV2.AnalysisResult>();
                for (var i = 0; i < analysisCount; i++)
                {
                    var analyzer = analyzerMap[reader.ReadString()];

                    var syntaxLocalCount = reader.ReadInt32();
                    var syntaxLocalMap = ImmutableDictionary.CreateBuilder<DocumentId, ImmutableArray<DiagnosticData>>();
                    for (var j = 0; j < syntaxLocalCount; j++)
                    {
                        var documentId = Serializer.DeserializeDocumentId(reader, cancellationToken);
                        var diagnostics = diagnosticDataSerializer.ReadFrom(reader, project.GetDocument(documentId), cancellationToken);

                        syntaxLocalMap.Add(documentId, diagnostics);
                    }

                    var semanticLocalCount = reader.ReadInt32();
                    var semanticLocalMap = ImmutableDictionary.CreateBuilder<DocumentId, ImmutableArray<DiagnosticData>>();
                    for (var j = 0; j < semanticLocalCount; j++)
                    {
                        var documentId = Serializer.DeserializeDocumentId(reader, cancellationToken);
                        var diagnostics = diagnosticDataSerializer.ReadFrom(reader, project.GetDocument(documentId), cancellationToken);

                        semanticLocalMap.Add(documentId, diagnostics);
                    }

                    var nonLocalCount = reader.ReadInt32();
                    var nonLocalMap = ImmutableDictionary.CreateBuilder<DocumentId, ImmutableArray<DiagnosticData>>();
                    for (var j = 0; j < nonLocalCount; j++)
                    {
                        var documentId = Serializer.DeserializeDocumentId(reader, cancellationToken);
                        var diagnostics = diagnosticDataSerializer.ReadFrom(reader, project.GetDocument(documentId), cancellationToken);

                        nonLocalMap.Add(documentId, diagnostics);
                    }

                    var others = diagnosticDataSerializer.ReadFrom(reader, project, cancellationToken);

                    var analysisResult = new CodeAnalysis.Diagnostics.EngineV2.AnalysisResult(
                        project.Id, version,
                        syntaxLocalMap.ToImmutable(),
                        semanticLocalMap.ToImmutable(),
                        nonLocalMap.ToImmutable(),
                        others,
                        documentIds: null,
                        fromBuild: false);

                    analysisMap.Add(analyzer, analysisResult);
                }

                var telemetryCount = reader.ReadInt32();

                var telemetryMap = ImmutableDictionary.CreateBuilder<DiagnosticAnalyzer, AnalyzerTelemetryInfo>();
                for (var i = 0; i < telemetryCount; i++)
                {
                    var analyzer = analyzerMap[reader.ReadString()];

                    var compilationStartActionsCount = reader.ReadInt32();
                    var compilationEndActionsCount = reader.ReadInt32();
                    var compilationActionsCount = reader.ReadInt32();
                    var syntaxTreeActionsCount = reader.ReadInt32();
                    var semanticModelActionsCount = reader.ReadInt32();
                    var symbolActionsCount = reader.ReadInt32();
                    var syntaxNodeActionsCount = reader.ReadInt32();
                    var codeBlockStartActionsCount = reader.ReadInt32();
                    var codeBlockEndActionsCount = reader.ReadInt32();
                    var codeBlockActionsCount = reader.ReadInt32();
                    var operationActionsCount = reader.ReadInt32();
                    var operationBlockActionsCount = reader.ReadInt32();
                    var operationBlockStartActionsCount = reader.ReadInt32();
                    var operationBlockEndActionsCount = reader.ReadInt32();
                    var executionTime = new TimeSpan(reader.ReadInt64());

                    var telemetryInfo = new AnalyzerTelemetryInfo(
                        compilationStartActionsCount,
                        compilationEndActionsCount,
                        compilationActionsCount,
                        syntaxTreeActionsCount,
                        semanticModelActionsCount,
                        symbolActionsCount,
                        syntaxNodeActionsCount,
                        codeBlockStartActionsCount,
                        codeBlockEndActionsCount,
                        codeBlockActionsCount,
                        operationActionsCount,
                        operationBlockStartActionsCount,
                        operationBlockEndActionsCount,
                        operationBlockActionsCount,
                        executionTime);

                    telemetryMap.Add(analyzer, telemetryInfo);
                }

                return new CompilerAnalysisResult(analysisMap.ToImmutable(), telemetryMap.ToImmutable());
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
