// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    // TODO: Remove all optional parameters from IDiagnosticAnalyzerService
    // Tracked with https://github.com/dotnet/roslyn/issues/67434
    internal interface IDiagnosticAnalyzerService
    {
        public IGlobalOptionService GlobalOptions { get; }

        /// <summary>
        /// Provides and caches analyzer information.
        /// </summary>
        DiagnosticAnalyzerInfoCache AnalyzerInfoCache { get; }

        /// <summary>
        /// Re-analyze given projects and documents. If both <paramref name="projectIds"/> and <paramref name="documentIds"/> are null,
        /// then re-analyzes the entire <see cref="Workspace.CurrentSolution"/> for the given <paramref name="workspace"/>.
        /// </summary>
        void Reanalyze(Workspace workspace, IEnumerable<ProjectId>? projectIds, IEnumerable<DocumentId>? documentIds, bool highPriority);

        /// <summary>
        /// Get specific diagnostics currently stored in the source. returned diagnostic might be out-of-date if solution has changed but analyzer hasn't run for the new solution.
        /// </summary>
        Task<ImmutableArray<DiagnosticData>> GetSpecificCachedDiagnosticsAsync(Workspace workspace, object id, bool includeSuppressedDiagnostics, bool includeNonLocalDocumentDiagnostics, CancellationToken cancellationToken);

        /// <summary>
        /// Get diagnostics currently stored in the source. returned diagnostic might be out-of-date if solution has changed but analyzer hasn't run for the new solution.
        /// </summary>
        Task<ImmutableArray<DiagnosticData>> GetCachedDiagnosticsAsync(Workspace workspace, ProjectId? projectId, DocumentId? documentId, bool includeSuppressedDiagnostics, bool includeNonLocalDocumentDiagnostics, CancellationToken cancellationToken);

        /// <summary>
        /// Get diagnostics for the given solution. all diagnostics returned should be up-to-date with respect to the given solution.
        /// </summary>
        Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(Solution solution, ProjectId? projectId, DocumentId? documentId, bool includeSuppressedDiagnostics, bool includeNonLocalDocumentDiagnostics, CancellationToken cancellationToken);

        /// <summary>
        /// Force computes diagnostics and raises diagnostic events for the given project or solution. all diagnostics returned should be up-to-date with respect to the given project or solution.
        /// </summary>
        Task ForceAnalyzeAsync(Solution solution, Action<Project> onProjectAnalyzed, ProjectId? projectId, CancellationToken cancellationToken);

        /// <summary>
        /// Get diagnostics of the given diagnostic ids and/or analyzers from the given solution. all diagnostics returned should be up-to-date with respect to the given solution.
        /// Note that for project case, this method returns diagnostics from all project documents as well. Use <see cref="GetProjectDiagnosticsForIdsAsync(Solution, ProjectId?, ImmutableHashSet{string}?, Func{DiagnosticAnalyzer, bool}?, bool, bool, CancellationToken)"/>
        /// if you want to fetch only project diagnostics without source locations.
        /// </summary>
        Task<ImmutableArray<DiagnosticData>> GetDiagnosticsForIdsAsync(Solution solution, ProjectId? projectId, DocumentId? documentId, ImmutableHashSet<string>? diagnosticIds, Func<DiagnosticAnalyzer, bool>? shouldIncludeAnalyzer, bool includeSuppressedDiagnostics, bool includeLocalDocumentDiagnostics, bool includeNonLocalDocumentDiagnostics, CancellationToken cancellationToken);

        /// <summary>
        /// Get project diagnostics (diagnostics with no source location) of the given diagnostic ids and/or analyzers from the given solution. all diagnostics returned should be up-to-date with respect to the given solution.
        /// Note that this method doesn't return any document diagnostics. Use <see cref="GetDiagnosticsForIdsAsync(Solution, ProjectId, DocumentId, ImmutableHashSet{string}, Func{DiagnosticAnalyzer, bool}?, bool, bool, bool, CancellationToken)"/> to also fetch those.
        /// </summary>
        Task<ImmutableArray<DiagnosticData>> GetProjectDiagnosticsForIdsAsync(Solution solution, ProjectId? projectId, ImmutableHashSet<string>? diagnosticIds, Func<DiagnosticAnalyzer, bool>? shouldIncludeAnalyzer, bool includeSuppressedDiagnostics, bool includeNonLocalDocumentDiagnostics, CancellationToken cancellationToken);

        /// <summary>
        /// Try to return up to date diagnostics for the given span for the document.
        ///
        /// It will return true if it was able to return all up-to-date diagnostics.
        ///  otherwise, false indicating there are some missing diagnostics in the diagnostic list
        ///  
        /// This API will only force complete analyzers that support span based analysis, i.e. compiler analyzer and
        /// <see cref="IBuiltInAnalyzer"/>s that support <see cref="DiagnosticAnalyzerCategory.SemanticSpanAnalysis"/>.
        /// For the rest of the analyzers, it will only return diagnostics if the analyzer has already been executed.
        /// Use <see cref="GetDiagnosticsForSpanAsync(TextDocument, TextSpan?, Func{string, bool}?, bool, bool, ICodeActionRequestPriorityProvider, Func{string, IDisposable?}?, DiagnosticKind, bool, CancellationToken)"/>
        /// if you want to force complete all analyzers and get up-to-date diagnostics for all analyzers for the given span.
        /// </summary>
        Task<(ImmutableArray<DiagnosticData> diagnostics, bool upToDate)> TryGetDiagnosticsForSpanAsync(
            TextDocument document, TextSpan range, Func<string, bool>? shouldIncludeDiagnostic,
            bool includeSuppressedDiagnostics,
            ICodeActionRequestPriorityProvider priorityProvider,
            DiagnosticKind diagnosticKind,
            bool isExplicit,
            CancellationToken cancellationToken);

        /// <summary>
        /// Return up to date diagnostics for the given span for the document
        /// <para>
        /// This can be expensive since it is force analyzing diagnostics if it doesn't have up-to-date one yet.
        /// Predicate <paramref name="shouldIncludeDiagnostic"/> filters out analyzers from execution if 
        /// none of its reported diagnostics should be included in the result.
        /// </para>
        /// </summary>
        Task<ImmutableArray<DiagnosticData>> GetDiagnosticsForSpanAsync(
            TextDocument document, TextSpan? range, Func<string, bool>? shouldIncludeDiagnostic,
            bool includeCompilerDiagnostics,
            bool includeSuppressedDiagnostics,
            ICodeActionRequestPriorityProvider priorityProvider,
            Func<string, IDisposable?>? addOperationScope,
            DiagnosticKind diagnosticKind,
            bool isExplicit,
            CancellationToken cancellationToken);
    }

    internal static class IDiagnosticAnalyzerServiceExtensions
    {
        /// <summary>
        /// Return up to date diagnostics for the given <paramref name="range"/> for the given <paramref name="document"/>.
        /// <para>
        /// This can be expensive since it is force analyzing diagnostics if it doesn't have up-to-date one yet.
        /// </para>
        /// </summary>
        public static Task<ImmutableArray<DiagnosticData>> GetDiagnosticsForSpanAsync(this IDiagnosticAnalyzerService service,
            TextDocument document, TextSpan? range, CancellationToken cancellationToken)
            => service.GetDiagnosticsForSpanAsync(document, range, DiagnosticKind.All, includeSuppressedDiagnostics: false, cancellationToken);

        /// <summary>
        /// Return up to date diagnostics of the given <paramref name="diagnosticKind"/> for the given <paramref name="range"/>
        /// for the given <paramref name="document"/>.
        /// <para>
        /// This can be expensive since it is force analyzing diagnostics if it doesn't have up-to-date one yet.
        /// </para>
        /// </summary>
        public static Task<ImmutableArray<DiagnosticData>> GetDiagnosticsForSpanAsync(this IDiagnosticAnalyzerService service,
            TextDocument document, TextSpan? range, DiagnosticKind diagnosticKind, bool includeSuppressedDiagnostics, CancellationToken cancellationToken)
            => service.GetDiagnosticsForSpanAsync(document, range,
                diagnosticId: null, includeSuppressedDiagnostics,
                priorityProvider: new DefaultCodeActionRequestPriorityProvider(),
                addOperationScope: null, diagnosticKind, isExplicit: false, cancellationToken);

        /// <summary>
        /// Return up to date diagnostics for the given <paramref name="range"/> and parameters for the given <paramref name="document"/>.
        /// <para>
        /// This can be expensive since it is force analyzing diagnostics if it doesn't have up-to-date one yet. If
        /// <paramref name="diagnosticId"/> is not null, it gets diagnostics only for this given <paramref
        /// name="diagnosticId"/> value.
        /// </para>
        /// </summary>
        public static Task<ImmutableArray<DiagnosticData>> GetDiagnosticsForSpanAsync(this IDiagnosticAnalyzerService service,
            TextDocument document, TextSpan? range, string? diagnosticId,
            bool includeSuppressedDiagnostics,
            ICodeActionRequestPriorityProvider priorityProvider,
            Func<string, IDisposable?>? addOperationScope,
            DiagnosticKind diagnosticKind,
            bool isExplicit,
            CancellationToken cancellationToken)
        {
            Func<string, bool>? shouldIncludeDiagnostic = diagnosticId != null ? id => id == diagnosticId : null;
            return service.GetDiagnosticsForSpanAsync(document, range, shouldIncludeDiagnostic,
                includeCompilerDiagnostics: true, includeSuppressedDiagnostics, priorityProvider,
                addOperationScope, diagnosticKind, isExplicit, cancellationToken);
        }
    }
}
