﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal partial class DiagnosticAnalyzerService
{
    private partial class DiagnosticIncrementalAnalyzer
    {
        public Task<ImmutableArray<DiagnosticData>> GetDiagnosticsForIdsAsync(Solution solution, ProjectId projectId, DocumentId? documentId, ImmutableHashSet<string>? diagnosticIds, Func<DiagnosticAnalyzer, bool>? shouldIncludeAnalyzer, bool includeLocalDocumentDiagnostics, bool includeNonLocalDocumentDiagnostics, CancellationToken cancellationToken)
            => new IdeLatestDiagnosticGetter(this, solution, projectId, documentId, diagnosticIds, shouldIncludeAnalyzer, includeLocalDocumentDiagnostics, includeNonLocalDocumentDiagnostics).GetDiagnosticsAsync(cancellationToken);

        public Task<ImmutableArray<DiagnosticData>> GetProjectDiagnosticsForIdsAsync(Solution solution, ProjectId projectId, ImmutableHashSet<string>? diagnosticIds, Func<DiagnosticAnalyzer, bool>? shouldIncludeAnalyzer, bool includeNonLocalDocumentDiagnostics, CancellationToken cancellationToken)
            => new IdeLatestDiagnosticGetter(this, solution, projectId, documentId: null, diagnosticIds, shouldIncludeAnalyzer, includeLocalDocumentDiagnostics: false, includeNonLocalDocumentDiagnostics).GetProjectDiagnosticsAsync(cancellationToken);

        private abstract class DiagnosticGetter(
            DiagnosticIncrementalAnalyzer owner,
            Solution solution,
            ProjectId projectId,
            DocumentId? documentId,
            bool includeLocalDocumentDiagnostics,
            bool includeNonLocalDocumentDiagnostics)
        {
            protected readonly DiagnosticIncrementalAnalyzer Owner = owner;

            protected readonly Solution Solution = solution;
            protected readonly ProjectId ProjectId = projectId;
            protected readonly DocumentId? DocumentId = documentId;
            protected readonly bool IncludeLocalDocumentDiagnostics = includeLocalDocumentDiagnostics;
            protected readonly bool IncludeNonLocalDocumentDiagnostics = includeNonLocalDocumentDiagnostics;

            protected StateManager StateManager => Owner._stateManager;

            protected virtual bool ShouldIncludeDiagnostic(DiagnosticData diagnostic) => true;

            protected abstract Task ProduceDiagnosticsAsync(
                Project project, IReadOnlyList<DocumentId> documentIds, bool includeProjectNonLocalResult, ArrayBuilder<DiagnosticData> builder, CancellationToken cancellationToken);

            public async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(CancellationToken cancellationToken)
            {
                var project = Solution.GetProject(ProjectId);
                if (project == null)
                    return [];

                // return diagnostics specific to one project or document
                var includeProjectNonLocalResult = DocumentId == null;
                return await ProduceProjectDiagnosticsAsync(
                    project,
                    // Ensure we compute and return diagnostics for both the normal docs and the additional docs in this
                    // project if no specific document id was requested.
                    this.DocumentId != null ? [this.DocumentId] : [.. project.DocumentIds, .. project.AdditionalDocumentIds],
                    includeProjectNonLocalResult, cancellationToken).ConfigureAwait(false);
            }

            protected async Task<ImmutableArray<DiagnosticData>> ProduceProjectDiagnosticsAsync(
                Project project, IReadOnlyList<DocumentId> documentIds,
                bool includeProjectNonLocalResult, CancellationToken cancellationToken)
            {
                using var _ = ArrayBuilder<DiagnosticData>.GetInstance(out var builder);
                await this.ProduceDiagnosticsAsync(
                    project, documentIds, includeProjectNonLocalResult, builder, cancellationToken).ConfigureAwait(false);
                return builder.ToImmutableAndClear();
            }

            protected void AddIncludedDiagnostics(ArrayBuilder<DiagnosticData> builder, ImmutableArray<DiagnosticData> diagnostics)
            {
                foreach (var diagnostic in diagnostics)
                {
                    if (ShouldIncludeDiagnostic(diagnostic))
                        builder.Add(diagnostic);
                }
            }
        }

        private sealed class IdeLatestDiagnosticGetter(
            DiagnosticIncrementalAnalyzer owner,
            Solution solution,
            ProjectId projectId,
            DocumentId? documentId,
            ImmutableHashSet<string>? diagnosticIds,
            Func<DiagnosticAnalyzer, bool>? shouldIncludeAnalyzer,
            bool includeLocalDocumentDiagnostics,
            bool includeNonLocalDocumentDiagnostics)
            : DiagnosticGetter(
                owner, solution, projectId, documentId, includeLocalDocumentDiagnostics, includeNonLocalDocumentDiagnostics)
        {
            private readonly ImmutableHashSet<string>? _diagnosticIds = diagnosticIds;
            private readonly Func<DiagnosticAnalyzer, bool>? _shouldIncludeAnalyzer = shouldIncludeAnalyzer;

            public async Task<ImmutableArray<DiagnosticData>> GetProjectDiagnosticsAsync(CancellationToken cancellationToken)
            {
                var project = Solution.GetProject(ProjectId);
                if (project is null)
                    return [];

                return await ProduceProjectDiagnosticsAsync(
                    project, documentIds: [], includeProjectNonLocalResult: true, cancellationToken).ConfigureAwait(false);
            }

            protected override bool ShouldIncludeDiagnostic(DiagnosticData diagnostic)
                => _diagnosticIds == null || _diagnosticIds.Contains(diagnostic.Id);

            protected override async Task ProduceDiagnosticsAsync(
                Project project,
                IReadOnlyList<DocumentId> documentIds,
                bool includeProjectNonLocalResult,
                ArrayBuilder<DiagnosticData> builder,
                CancellationToken cancellationToken)
            {
                // get analyzers that are not suppressed.
                var stateSetsForProject = await StateManager.GetOrCreateStateSetsAsync(project, cancellationToken).ConfigureAwait(false);
                var stateSets = stateSetsForProject.Where(s => ShouldIncludeStateSet(project, s)).ToImmutableArrayOrEmpty();

                // unlike the suppressed (disabled) analyzer, we will include hidden diagnostic only analyzers here.
                var compilation = await CreateCompilationWithAnalyzersAsync(project, stateSets, Owner.AnalyzerService.CrashOnAnalyzerException, cancellationToken).ConfigureAwait(false);

                var result = await Owner.GetProjectAnalysisDataAsync(compilation, project, stateSets, cancellationToken).ConfigureAwait(false);

                foreach (var stateSet in stateSets)
                {
                    var analysisResult = result.GetResult(stateSet.Analyzer);

                    foreach (var documentId in documentIds)
                    {
                        if (IncludeLocalDocumentDiagnostics)
                        {
                            AddIncludedDiagnostics(builder, analysisResult.GetDocumentDiagnostics(documentId, AnalysisKind.Syntax));
                            AddIncludedDiagnostics(builder, analysisResult.GetDocumentDiagnostics(documentId, AnalysisKind.Semantic));
                        }

                        if (IncludeNonLocalDocumentDiagnostics)
                            AddIncludedDiagnostics(builder, analysisResult.GetDocumentDiagnostics(documentId, AnalysisKind.NonLocal));
                    }

                    if (includeProjectNonLocalResult)
                    {
                        // include project diagnostics if there is no target document
                        AddIncludedDiagnostics(builder, analysisResult.GetOtherDiagnostics());
                    }
                }
            }

            private bool ShouldIncludeStateSet(Project project, StateSet stateSet)
            {
                if (!DocumentAnalysisExecutor.IsAnalyzerEnabledForProject(stateSet.Analyzer, project, Owner.GlobalOptions))
                {
                    return false;
                }

                if (_shouldIncludeAnalyzer != null && !_shouldIncludeAnalyzer(stateSet.Analyzer))
                {
                    return false;
                }

                if (_diagnosticIds != null && Owner.DiagnosticAnalyzerInfoCache.GetDiagnosticDescriptors(stateSet.Analyzer).All(d => !_diagnosticIds.Contains(d.Id)))
                {
                    return false;
                }

                return true;
            }
        }
    }
}
