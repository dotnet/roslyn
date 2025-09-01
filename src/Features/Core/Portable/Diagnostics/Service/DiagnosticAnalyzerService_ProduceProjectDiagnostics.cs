// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Workspaces.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal sealed partial class DiagnosticAnalyzerService
{
    public async Task<ImmutableArray<DiagnosticData>> ProduceProjectDiagnosticsInProcessAsync(
        Project project,
        ImmutableArray<DiagnosticAnalyzer> analyzers,
        ImmutableHashSet<string>? diagnosticIds,
        ImmutableArray<DocumentId> documentIds,
        bool includeLocalDocumentDiagnostics,
        bool includeNonLocalDocumentDiagnostics,
        bool includeProjectNonLocalResult,
        CancellationToken cancellationToken)
    {
        using var _ = ArrayBuilder<DiagnosticData>.GetInstance(out var builder);

        var hostAnalyzerInfo = GetOrCreateHostAnalyzerInfo(project);
        var result = await GetOrComputeDiagnosticAnalysisResultsAsync(analyzers).ConfigureAwait(false);

        foreach (var analyzer in analyzers)
        {
            if (!result.TryGetValue(analyzer, out var analysisResult))
                continue;

            foreach (var documentId in documentIds)
            {
                if (includeLocalDocumentDiagnostics)
                {
                    AddIncludedDiagnostics(builder, analysisResult.GetDocumentDiagnostics(documentId, AnalysisKind.Syntax));
                    AddIncludedDiagnostics(builder, analysisResult.GetDocumentDiagnostics(documentId, AnalysisKind.Semantic));
                }

                if (includeNonLocalDocumentDiagnostics)
                    AddIncludedDiagnostics(builder, analysisResult.GetDocumentDiagnostics(documentId, AnalysisKind.NonLocal));
            }

            // include project diagnostics if there is no target document
            if (includeProjectNonLocalResult)
                AddIncludedDiagnostics(builder, analysisResult.GetOtherDiagnostics());
        }

        return builder.ToImmutableAndClear();

        bool ShouldIncludeDiagnostic(DiagnosticData diagnostic)
            => diagnosticIds == null || diagnosticIds.Contains(diagnostic.Id);

        void AddIncludedDiagnostics(ArrayBuilder<DiagnosticData> builder, ImmutableArray<DiagnosticData> diagnostics)
        {
            foreach (var diagnostic in diagnostics)
            {
                if (ShouldIncludeDiagnostic(diagnostic))
                    builder.Add(diagnostic);
            }
        }

        async Task<ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult>> GetOrComputeDiagnosticAnalysisResultsAsync(
            ImmutableArray<DiagnosticAnalyzer> analyzers)
        {
            // Otherwise, just compute for the analyzers we care about.
            var compilation = await GetOrCreateCompilationWithAnalyzersAsync(
                project, analyzers, hostAnalyzerInfo, this.CrashOnAnalyzerException, cancellationToken).ConfigureAwait(false);

            var result = await ComputeDiagnosticAnalysisResultsInProcessAsync(
                compilation, project, [.. analyzers.OfType<DocumentDiagnosticAnalyzer>()], cancellationToken).ConfigureAwait(false);
            return result;
        }
    }
}
