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
    /// <summary>
    /// Should only be called from other "InProcess" methods as this loads and realizes the DiagnosticAnalyzers.
    /// </summary>
    private ImmutableArray<DiagnosticAnalyzer> GetDiagnosticAnalyzersInProcess(
       Project project,
       ImmutableHashSet<string>? diagnosticIds,
       AnalyzerFilter analyzerFilter)
    {
        var analyzersForProject = GetProjectAnalyzers_OnlyCallInProcess(project);
        return analyzersForProject.WhereAsArray(ShouldIncludeAnalyzer);

        bool ShouldIncludeAnalyzer(DiagnosticAnalyzer analyzer)
        {
            if (analyzer.IsCompilerAnalyzer())
            {
                if ((analyzerFilter & AnalyzerFilter.CompilerAnalyzer) == 0)
                    return false;
            }
            else
            {
                if ((analyzerFilter & AnalyzerFilter.NonCompilerAnalyzer) == 0)
                    return false;
            }

            if (!DocumentAnalysisExecutor.IsAnalyzerEnabledForProject(analyzer, project, this._globalOptions))
                return false;

            if (diagnosticIds != null && _analyzerInfoCache.GetDiagnosticDescriptors(analyzer).All(d => !diagnosticIds.Contains(d.Id)))
                return false;

            return true;
        }
    }

    private Task<ImmutableArray<DiagnosticData>> GetDiagnosticsForIdsInProcessAsync(
        Project project,
        ImmutableArray<DocumentId> documentIds,
        ImmutableHashSet<string>? diagnosticIds,
        AnalyzerFilter analyzerFilter,
        bool includeLocalDocumentDiagnostics,
        CancellationToken cancellationToken)
    {
        return GetDiagnosticsForIdsInProcessAsync(
            project, documentIds, diagnosticIds,
            GetDiagnosticAnalyzersInProcess(project, diagnosticIds, analyzerFilter),
            includeLocalDocumentDiagnostics, cancellationToken);
    }

    private Task<ImmutableArray<DiagnosticData>> GetDiagnosticsForIdsInProcessAsync(
        Project project,
        ImmutableArray<DocumentId> documentIds,
        ImmutableHashSet<string>? diagnosticIds,
        ImmutableArray<DiagnosticAnalyzer> analyzers,
        bool includeLocalDocumentDiagnostics,
        CancellationToken cancellationToken)
    {
        return ProduceProjectDiagnosticsInProcessAsync(
            project, diagnosticIds,
            // Ensure we compute and return diagnostics for both the normal docs and the additional docs in this
            // project if no specific document id was requested.
            documentIds.IsDefault ? [.. project.DocumentIds, .. project.AdditionalDocumentIds] : documentIds,
            analyzers,
            includeLocalDocumentDiagnostics,
            includeNonLocalDocumentDiagnostics: true,
            // return diagnostics specific to one project or document
            includeProjectNonLocalResult: documentIds.IsDefault,
            cancellationToken);
    }

    private Task<ImmutableArray<DiagnosticData>> GetProjectDiagnosticsForIdsInProcessAsync(
        Project project,
        ImmutableHashSet<string>? diagnosticIds,
        AnalyzerFilter analyzerFilter,
        CancellationToken cancellationToken)
    {
        return GetProjectDiagnosticsForIdsInProcessAsync(
            project, diagnosticIds,
            GetDiagnosticAnalyzersInProcess(project, diagnosticIds, analyzerFilter),
            cancellationToken);
    }

    private Task<ImmutableArray<DiagnosticData>> GetProjectDiagnosticsForIdsInProcessAsync(
        Project project,
        ImmutableHashSet<string>? diagnosticIds,
        ImmutableArray<DiagnosticAnalyzer> analyzers,
        CancellationToken cancellationToken)
    {
        return ProduceProjectDiagnosticsInProcessAsync(
            project, diagnosticIds, documentIds: [],
            analyzers,
            includeLocalDocumentDiagnostics: false,
            includeNonLocalDocumentDiagnostics: false,
            includeProjectNonLocalResult: true,
            cancellationToken);
    }

    private async Task<ImmutableArray<DiagnosticData>> ProduceProjectDiagnosticsInProcessAsync(
        Project project,
        ImmutableHashSet<string>? diagnosticIds,
        ImmutableArray<DocumentId> documentIds,
        ImmutableArray<DiagnosticAnalyzer> analyzers,
        bool includeLocalDocumentDiagnostics,
        bool includeNonLocalDocumentDiagnostics,
        bool includeProjectNonLocalResult,
        CancellationToken cancellationToken)
    {
        using var _ = ArrayBuilder<DiagnosticData>.GetInstance(out var builder);

        var hostAnalyzerInfo = GetOrCreateHostAnalyzerInfo_OnlyCallInProcess(project);
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
            var compilation = await GetOrCreateCompilationWithAnalyzers_OnlyCallInProcessAsync(
                project, analyzers, hostAnalyzerInfo, this.CrashOnAnalyzerException, cancellationToken).ConfigureAwait(false);

            var result = await ComputeDiagnosticAnalysisResultsInProcessAsync(
                compilation, project, [.. analyzers.OfType<DocumentDiagnosticAnalyzer>()], cancellationToken).ConfigureAwait(false);
            return result;
        }
    }
}
