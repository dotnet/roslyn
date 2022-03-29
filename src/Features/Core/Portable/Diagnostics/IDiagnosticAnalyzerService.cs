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
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal interface IDiagnosticAnalyzerService
    {
        public IGlobalOptionService GlobalOptions { get; }

        /// <summary>
        /// Provides and caches analyzer information.
        /// </summary>
        DiagnosticAnalyzerInfoCache AnalyzerInfoCache { get; }

        /// <summary>
        /// Re-analyze given projects and documents
        /// </summary>
        void Reanalyze(Workspace workspace, IEnumerable<ProjectId>? projectIds = null, IEnumerable<DocumentId>? documentIds = null, bool highPriority = false);

        /// <summary>
        /// Get specific diagnostics currently stored in the source. returned diagnostic might be out-of-date if solution has changed but analyzer hasn't run for the new solution.
        /// </summary>
        Task<ImmutableArray<DiagnosticData>> GetSpecificCachedDiagnosticsAsync(Workspace workspace, object id, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get diagnostics currently stored in the source. returned diagnostic might be out-of-date if solution has changed but analyzer hasn't run for the new solution.
        /// </summary>
        Task<ImmutableArray<DiagnosticData>> GetCachedDiagnosticsAsync(Workspace workspace, ProjectId? projectId = null, DocumentId? documentId = null, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get diagnostics for the given solution. all diagnostics returned should be up-to-date with respect to the given solution.
        /// </summary>
        Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(Solution solution, ProjectId? projectId = null, DocumentId? documentId = null, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Force computes diagnostics and raises diagnostic events for the given project or solution. all diagnostics returned should be up-to-date with respect to the given project or solution.
        /// </summary>
        Task ForceAnalyzeAsync(Solution solution, Action<Project> onProjectAnalyzed, ProjectId? projectId = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// True if given project has any diagnostics
        /// </summary>
        bool ContainsDiagnostics(Workspace workspace, ProjectId projectId);

        /// <summary>
        /// Get diagnostics of the given diagnostic ids from the given solution. all diagnostics returned should be up-to-date with respect to the given solution.
        /// Note that for project case, this method returns diagnostics from all project documents as well. Use <see cref="GetProjectDiagnosticsForIdsAsync(Solution, ProjectId, ImmutableHashSet{string}, bool, CancellationToken)"/>
        /// if you want to fetch only project diagnostics without source locations.
        /// </summary>
        Task<ImmutableArray<DiagnosticData>> GetDiagnosticsForIdsAsync(Solution solution, ProjectId? projectId = null, DocumentId? documentId = null, ImmutableHashSet<string>? diagnosticIds = null, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get project diagnostics (diagnostics with no source location) of the given diagnostic ids from the given solution. all diagnostics returned should be up-to-date with respect to the given solution.
        /// Note that this method doesn't return any document diagnostics. Use <see cref="GetDiagnosticsForIdsAsync(Solution, ProjectId, DocumentId, ImmutableHashSet{string}, bool, CancellationToken)"/> to also fetch those.
        /// </summary>
        Task<ImmutableArray<DiagnosticData>> GetProjectDiagnosticsForIdsAsync(Solution solution, ProjectId? projectId = null, ImmutableHashSet<string>? diagnosticIds = null, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Try to return up to date diagnostics for the given span for the document.
        ///
        /// It will return true if it was able to return all up-to-date diagnostics.
        ///  otherwise, false indicating there are some missing diagnostics in the diagnostic list
        ///  
        /// This API will only force complete analyzers that support span based analysis, i.e. compiler analyzer and
        /// <see cref="IBuiltInAnalyzer"/>s that support <see cref="DiagnosticAnalyzerCategory.SemanticSpanAnalysis"/>.
        /// For the rest of the analyzers, it will only return diagnostics if the analyzer has already been executed.
        /// Use <see cref="GetDiagnosticsForSpanAsync(Document, TextSpan?, Func{string, bool}?, bool, CodeActionRequestPriority, Func{string, IDisposable?}?, CancellationToken)"/>
        /// if you want to force complete all analyzers and get up-to-date diagnostics for all analyzers for the given span.
        /// </summary>
        Task<bool> TryAppendDiagnosticsForSpanAsync(Document document, TextSpan range, ArrayBuilder<DiagnosticData> diagnostics, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Return up to date diagnostics for the given span for the document
        /// <para>
        /// This can be expensive since it is force analyzing diagnostics if it doesn't have up-to-date one yet.
        /// Predicate <paramref name="shouldIncludeDiagnostic"/> filters out analyzers from execution if 
        /// none of its reported diagnostics should be included in the result.
        /// </para>
        /// </summary>
        Task<ImmutableArray<DiagnosticData>> GetDiagnosticsForSpanAsync(Document document, TextSpan? range, Func<string, bool>? shouldIncludeDiagnostic, bool includeSuppressedDiagnostics = false, CodeActionRequestPriority priority = CodeActionRequestPriority.None, Func<string, IDisposable?>? addOperationScope = null, CancellationToken cancellationToken = default);
    }

    internal static class IDiagnosticAnalyzerServiceExtensions
    {
        public static Task<ImmutableArray<DiagnosticData>> GetDiagnosticsForSpanAsync(this IDiagnosticAnalyzerService service, Document document, TextSpan range, string? diagnosticId = null, bool includeSuppressedDiagnostics = false, Func<string, IDisposable?>? addOperationScope = null, CancellationToken cancellationToken = default)
            => service.GetDiagnosticsForSpanAsync(document, range, diagnosticId, includeSuppressedDiagnostics, CodeActionRequestPriority.None, addOperationScope, cancellationToken);

        /// <summary>
        /// Return up to date diagnostics for the given span for the document
        /// <para>
        /// This can be expensive since it is force analyzing diagnostics if it doesn't have up-to-date one yet. If
        /// <paramref name="diagnosticId"/> is not null, it gets diagnostics only for this given <paramref
        /// name="diagnosticId"/> value.
        /// </para>
        /// </summary>
        public static Task<ImmutableArray<DiagnosticData>> GetDiagnosticsForSpanAsync(this IDiagnosticAnalyzerService service, Document document, TextSpan? range, string? diagnosticId = null, bool includeSuppressedDiagnostics = false, CodeActionRequestPriority priority = CodeActionRequestPriority.None, Func<string, IDisposable?>? addOperationScope = null, CancellationToken cancellationToken = default)
        {
            Func<string, bool>? shouldIncludeDiagnostic = diagnosticId != null ? id => id == diagnosticId : null;
            return service.GetDiagnosticsForSpanAsync(document, range, shouldIncludeDiagnostic,
                includeSuppressedDiagnostics, priority, addOperationScope, cancellationToken);
        }
    }
}
