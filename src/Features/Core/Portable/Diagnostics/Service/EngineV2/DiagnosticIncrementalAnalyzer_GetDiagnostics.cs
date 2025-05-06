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
    private sealed partial class DiagnosticIncrementalAnalyzer
    {
        public Task<ImmutableArray<DiagnosticData>> GetDiagnosticsForIdsAsync(Project project, DocumentId? documentId, ImmutableHashSet<string>? diagnosticIds, Func<DiagnosticAnalyzer, bool>? shouldIncludeAnalyzer, bool includeLocalDocumentDiagnostics, bool includeNonLocalDocumentDiagnostics, CancellationToken cancellationToken)
        {
            return ProduceProjectDiagnosticsAsync(
                project, diagnosticIds, shouldIncludeAnalyzer,
                // Ensure we compute and return diagnostics for both the normal docs and the additional docs in this
                // project if no specific document id was requested.
                documentId != null ? [documentId] : [.. project.DocumentIds, .. project.AdditionalDocumentIds],
                includeLocalDocumentDiagnostics,
                includeNonLocalDocumentDiagnostics,
                // return diagnostics specific to one project or document
                includeProjectNonLocalResult: documentId == null,
                cancellationToken);
        }

        public Task<ImmutableArray<DiagnosticData>> GetProjectDiagnosticsForIdsAsync(
            Project project,
            ImmutableHashSet<string>? diagnosticIds,
            Func<DiagnosticAnalyzer, bool>? shouldIncludeAnalyzer,
            bool includeNonLocalDocumentDiagnostics,
            CancellationToken cancellationToken)
        {
            return ProduceProjectDiagnosticsAsync(
               project, diagnosticIds, shouldIncludeAnalyzer,
               documentIds: [],
               includeLocalDocumentDiagnostics: false,
               includeNonLocalDocumentDiagnostics: includeNonLocalDocumentDiagnostics,
               includeProjectNonLocalResult: true,
               cancellationToken);
        }

        private async Task<ImmutableArray<DiagnosticData>> ProduceProjectDiagnosticsAsync(
            Project project,
            ImmutableHashSet<string>? diagnosticIds,
            Func<DiagnosticAnalyzer, bool>? shouldIncludeAnalyzer,
            IReadOnlyList<DocumentId> documentIds,
            bool includeLocalDocumentDiagnostics,
            bool includeNonLocalDocumentDiagnostics,
            bool includeProjectNonLocalResult,
            CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<DiagnosticData>.GetInstance(out var builder);

            var solution = project.Solution;
            var analyzersForProject = await _stateManager.GetOrCreateAnalyzersAsync(
                solution.SolutionState, project.State, cancellationToken).ConfigureAwait(false);
            var hostAnalyzerInfo = await _stateManager.GetOrCreateHostAnalyzerInfoAsync(
                solution.SolutionState, project.State, cancellationToken).ConfigureAwait(false);
            var analyzers = analyzersForProject.WhereAsArray(a => ShouldIncludeAnalyzer(project, a));

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
                // If there was a 'ForceAnalyzeProjectAsync' run for this project, we can piggy back off of the
                // prior computed/cached results as they will be a superset of the results we want.
                //
                // Note: the caller will loop over *its* analyzers, grabbing from the full set of data we've cached for
                // this project, and filtering down further.  So it's ok to return this potentially larger set.
                //
                // Note: While ForceAnalyzeProjectAsync should always run with a larger set of analyzers than us
                // (since it runs all analyzers), we still run a paranoia check that the analyzers we care about are
                // a subset of that call so that we don't accidentally reuse results that would not correspond to
                // what we are computing ourselves.
                if (s_projectToForceAnalysisData.TryGetValue(project.State, out var box) &&
                    analyzers.IsSubsetOf(box.Value.analyzers))
                {
                    var checksum = await project.GetDiagnosticChecksumAsync(cancellationToken).ConfigureAwait(false);
                    if (box.Value.checksum == checksum)
                        return box.Value.diagnosticAnalysisResults;
                }

                // Otherwise, just compute for the analyzers we care about.
                var compilation = await GetOrCreateCompilationWithAnalyzersAsync(
                    project, analyzers, hostAnalyzerInfo, AnalyzerService.CrashOnAnalyzerException, cancellationToken).ConfigureAwait(false);

                var result = await ComputeDiagnosticAnalysisResultsAsync(
                    compilation, project, [.. analyzers.OfType<DocumentDiagnosticAnalyzer>()], cancellationToken).ConfigureAwait(false);
                return result;
            }

            bool ShouldIncludeAnalyzer(Project project, DiagnosticAnalyzer analyzer)
            {
                if (!DocumentAnalysisExecutor.IsAnalyzerEnabledForProject(analyzer, project, this.GlobalOptions))
                    return false;

                if (shouldIncludeAnalyzer != null && !shouldIncludeAnalyzer(analyzer))
                    return false;

                if (diagnosticIds != null && this.DiagnosticAnalyzerInfoCache.GetDiagnosticDescriptors(analyzer).All(d => !diagnosticIds.Contains(d.Id)))
                    return false;

                return true;
            }
        }
    }
}
