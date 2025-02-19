﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal interface IDiagnosticAnalyzerService
{
    /// <summary>
    /// Provides and caches analyzer information.
    /// </summary>
    DiagnosticAnalyzerInfoCache AnalyzerInfoCache { get; }

    /// <summary>
    /// Re-analyze all projects and documents.  This will cause an LSP diagnostic refresh request to be sent.
    /// </summary>
    void RequestDiagnosticRefresh();

    /// <summary>
    /// Force analyzes the given project by running all applicable analyzers on the project.
    /// </summary>
    Task<ImmutableArray<DiagnosticData>> ForceAnalyzeProjectAsync(Project project, CancellationToken cancellationToken);

    /// <summary>
    /// Get diagnostics of the given diagnostic ids and/or analyzers from the given solution. all diagnostics returned
    /// should be up-to-date with respect to the given solution. Note that for project case, this method returns
    /// diagnostics from all project documents as well. Use <see cref="GetProjectDiagnosticsForIdsAsync"/> if you want
    /// to fetch only project diagnostics without source locations.
    /// </summary>
    /// <param name="project">Project to fetch the diagnostics for.</param>
    /// <param name="documentId">Optional document to scope the returned diagnostics.</param>
    /// <param name="diagnosticIds">Optional set of diagnostic IDs to scope the returned diagnostics.</param>
    /// <param name="shouldIncludeAnalyzer">Option callback to filter out analyzers to execute for computing diagnostics.</param>
    /// <param name="includeLocalDocumentDiagnostics">
    /// Indicates if local document diagnostics must be returned.
    /// Local diagnostics are the ones that are reported by analyzers on the same file for which the callback was received
    /// and hence can be computed by analyzing a single file in isolation.
    /// </param>
    /// <param name="includeNonLocalDocumentDiagnostics">
    /// Indicates if non-local document diagnostics must be returned. Non-local diagnostics are the ones reported by
    /// analyzers either at compilation end callback OR in a different file from which the callback was made. Entire
    /// project must be analyzed to get the complete set of non-local document diagnostics.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<ImmutableArray<DiagnosticData>> GetDiagnosticsForIdsAsync(Project project, DocumentId? documentId, ImmutableHashSet<string>? diagnosticIds, Func<DiagnosticAnalyzer, bool>? shouldIncludeAnalyzer, bool includeLocalDocumentDiagnostics, bool includeNonLocalDocumentDiagnostics, CancellationToken cancellationToken);

    /// <summary>
    /// Get project diagnostics (diagnostics with no source location) of the given diagnostic ids and/or analyzers from
    /// the given solution. all diagnostics returned should be up-to-date with respect to the given solution. Note that
    /// this method doesn't return any document diagnostics. Use <see cref="GetDiagnosticsForIdsAsync"/> to also fetch
    /// those.
    /// </summary>
    /// <param name="project">Project to fetch the diagnostics for.</param>
    /// <param name="diagnosticIds">Optional set of diagnostic IDs to scope the returned diagnostics.</param>
    /// <param name="shouldIncludeAnalyzer">Option callback to filter out analyzers to execute for computing diagnostics.</param>
    /// <param name="includeNonLocalDocumentDiagnostics">
    /// Indicates if non-local document diagnostics must be returned.
    /// Non-local diagnostics are the ones reported by analyzers either at compilation end callback.
    /// Entire project must be analyzed to get the complete set of non-local diagnostics.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<ImmutableArray<DiagnosticData>> GetProjectDiagnosticsForIdsAsync(Project project, ImmutableHashSet<string>? diagnosticIds, Func<DiagnosticAnalyzer, bool>? shouldIncludeAnalyzer, bool includeNonLocalDocumentDiagnostics, CancellationToken cancellationToken);

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
        ICodeActionRequestPriorityProvider priorityProvider,
        DiagnosticKind diagnosticKind,
        bool isExplicit,
        CancellationToken cancellationToken);
}

internal static class IDiagnosticAnalyzerServiceExtensions
{
    /// <summary>
    /// Return up to date diagnostics of the given <paramref name="diagnosticKind"/> for the given <paramref name="range"/>
    /// for the given <paramref name="document"/>.
    /// <para>
    /// This can be expensive since it is force analyzing diagnostics if it doesn't have up-to-date one yet.
    /// </para>
    /// </summary>
    public static Task<ImmutableArray<DiagnosticData>> GetDiagnosticsForSpanAsync(this IDiagnosticAnalyzerService service,
        TextDocument document, TextSpan? range, DiagnosticKind diagnosticKind, CancellationToken cancellationToken)
        => service.GetDiagnosticsForSpanAsync(
            document, range,
            diagnosticId: null,
            priorityProvider: new DefaultCodeActionRequestPriorityProvider(),
            diagnosticKind, isExplicit: false, cancellationToken);

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
        ICodeActionRequestPriorityProvider priorityProvider,
        DiagnosticKind diagnosticKind,
        bool isExplicit,
        CancellationToken cancellationToken)
    {
        Func<string, bool>? shouldIncludeDiagnostic = diagnosticId != null ? id => id == diagnosticId : null;
        return service.GetDiagnosticsForSpanAsync(document, range, shouldIncludeDiagnostic,
            priorityProvider, diagnosticKind, isExplicit, cancellationToken);
    }
}
