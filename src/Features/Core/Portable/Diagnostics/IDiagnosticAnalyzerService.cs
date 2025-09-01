// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal interface IDiagnosticAnalyzerService : IWorkspaceService
{
    /// <summary>
    /// Re-analyze all projects and documents.  This will cause an LSP diagnostic refresh request to be sent.
    /// </summary>
    /// <remarks>
    /// This implementation must be safe to call on any thread.
    /// </remarks>
    void RequestDiagnosticRefresh();

    /// <summary>
    /// The default analyzer filter that will be used in functions like <see cref="GetDiagnosticsForIdsAsync"/> if
    /// no filter is provided.  The default filter has the following rules:
    /// <list type="number">
    /// <item>The standard compiler analyzer will not be run if the compiler diagnostic scope is <see cref="CompilerDiagnosticsScope.None"/>.</item>
    /// <item>A regular analyzer will not be run if <see cref="ProjectState.RunAnalyzers"/> is false.</item>
    /// <item>A regular analyzer will not be run if if the background analysis scope is <see cref="BackgroundAnalysisScope.None"/>.</item>
    /// <item>If a set of diagnostic ids are provided, the analyzer will not be run unless it declares at least one
    /// descriptor in that set.</item>
    /// <item>Otherwise, the analyzer will be run</item>
    /// </list>
    /// </summary>
    /// <param name="additionalFilter">An additional filter that can accept or reject analyzers that the default
    /// rules have accepted.</param>
    Func<DiagnosticAnalyzer, bool> GetDefaultAnalyzerFilter(
        Project project, ImmutableHashSet<string>? diagnosticIds, Func<DiagnosticAnalyzer, bool>? additionalFilter = null);

    /// <inheritdoc cref="IRemoteDiagnosticAnalyzerService.GetDeprioritizationCandidatesAsync"/>
    Task<ImmutableArray<DiagnosticAnalyzer>> GetDeprioritizationCandidatesAsync(
        Project project, ImmutableArray<DiagnosticAnalyzer> analyzers, CancellationToken cancellationToken);

    /// <summary>
    /// Gets document diagnostics of the given diagnostic ids and/or analyzers from the given project.
    /// All diagnostics returned should be up-to-date with respect to the given solution snapshot.
    /// Use <see cref="GetProjectDiagnosticsForIdsAsync"/> if you want to fetch only project diagnostics
    /// not associated ith a particular document.  Note that this operation can be quite expensive as it
    /// will execute all the analyers fully on this project, including through compilation-end analyzers.
    /// </summary>
    /// <param name="project">Project to fetch the diagnostics for.</param>
    /// <param name="documentIds">Optional documents to scope the returned diagnostics.  If <see langword="default"/>,
    /// then diagnostics will be returned for <see cref="Project.DocumentIds"/> and <see cref="Project.AdditionalDocumentIds"/>.</param>
    /// <param name="diagnosticIds">Optional set of diagnostic IDs to scope the returned diagnostics.</param>
    /// <param name="shouldIncludeAnalyzer">Optional callback to filter out analyzers to execute for computing diagnostics.
    /// If not present, <see cref="GetDefaultAnalyzerFilter"/> will be used.  If present, no default behavior
    /// is used, and the callback is defered to entirely.  To augment the existing default rules call
    /// <see cref="GetDefaultAnalyzerFilter"/> explicitly, and pass the result of that into this method.</param>
    /// <param name="includeLocalDocumentDiagnostics">
    /// Indicates if local document diagnostics must be returned.
    /// Local diagnostics are the ones that are reported by analyzers on the same file for which the callback was received
    /// and hence can be computed by analyzing a single file in isolation.
    /// </param>
    /// <remarks>
    /// Non local document diagnostics will be returned. Non-local diagnostics are the ones reported by
    /// analyzers either at compilation end callback OR in a different file from which the callback was made. Entire
    /// project must be analyzed to get the complete set of non-local document diagnostics.
    /// </remarks>
    Task<ImmutableArray<DiagnosticData>> GetDiagnosticsForIdsAsync(
        Project project, ImmutableArray<DocumentId> documentIds, ImmutableHashSet<string>? diagnosticIds, Func<DiagnosticAnalyzer, bool>? shouldIncludeAnalyzer, bool includeLocalDocumentDiagnostics, CancellationToken cancellationToken);

    /// <summary>
    /// Get project diagnostics (diagnostics with no source location) of the given diagnostic ids and/or analyzers from
    /// the given solution. all diagnostics returned should be up-to-date with respect to the given solution. Note that
    /// this method doesn't return any document diagnostics. Use <see cref="GetDiagnosticsForIdsAsync"/> to also fetch
    /// those.
    /// </summary>
    /// <param name="project">Project to fetch the diagnostics for.</param>
    /// <param name="diagnosticIds">Optional set of diagnostic IDs to scope the returned diagnostics.</param>
    /// <param name="shouldIncludeAnalyzer">Optional callback to filter out analyzers to execute for computing diagnostics.
    /// If not present, <see cref="GetDefaultAnalyzerFilter"/> will be used.  If present, no default behavior
    /// is used, and the callback is defered to entirely.  To augment the existing default rules call
    /// <see cref="GetDefaultAnalyzerFilter"/> explicitly, and pass the result of that into this method.</param>
    Task<ImmutableArray<DiagnosticData>> GetProjectDiagnosticsForIdsAsync(
        Project project, ImmutableHashSet<string>? diagnosticIds, Func<DiagnosticAnalyzer, bool>? shouldIncludeAnalyzer, CancellationToken cancellationToken);

    /// <summary>
    /// Return up to date diagnostics for the given span for the document
    /// <para/>
    /// This can be expensive since it is force analyzing diagnostics if it doesn't have up-to-date one yet.
    /// Predicate <paramref name="shouldIncludeDiagnostic"/> filters out analyzers from execution if 
    /// none of its reported diagnostics should be included in the result.
    /// <para/>
    /// Non-local diagnostics for the requested document are not returned.  In other words, only the diagnostics
    /// produced by running the requested filtered set of analyzers <em>only</em> on this document are returned here.
    /// To get non-local diagnostics for a document, use <see cref="GetDiagnosticsForIdsAsync"/>.  Non-local diagnostics
    /// will always be returned for the document in that case.
    /// </summary>
    Task<ImmutableArray<DiagnosticData>> GetDiagnosticsForSpanAsync(
        TextDocument document, TextSpan? range, Func<string, bool>? shouldIncludeDiagnostic,
        ICodeActionRequestPriorityProvider priorityProvider,
        DiagnosticKind diagnosticKind,
        CancellationToken cancellationToken);

    /// <param name="projectId">A project within <paramref name="solution"/> where <paramref name="analyzerReference"/> can be found</param>
    Task<ImmutableArray<DiagnosticDescriptor>> GetDiagnosticDescriptorsAsync(
        Solution solution, ProjectId projectId, AnalyzerReference analyzerReference, string language, CancellationToken cancellationToken);

    /// <inheritdoc cref="HostDiagnosticAnalyzers.GetDiagnosticDescriptorsPerReference(DiagnosticAnalyzerInfoCache)"/>
    Task<ImmutableDictionary<string, ImmutableArray<DiagnosticDescriptor>>> GetDiagnosticDescriptorsPerReferenceAsync(
        Solution solution, CancellationToken cancellationToken);

    /// <inheritdoc cref="HostDiagnosticAnalyzers.GetDiagnosticDescriptorsPerReference(DiagnosticAnalyzerInfoCache, Project)"/>
    Task<ImmutableDictionary<string, ImmutableArray<DiagnosticDescriptor>>> GetDiagnosticDescriptorsPerReferenceAsync(
        Project project, CancellationToken cancellationToken);
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
            diagnosticKind,
            cancellationToken);

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
        CancellationToken cancellationToken)
    {
        Func<string, bool>? shouldIncludeDiagnostic = diagnosticId != null ? id => id == diagnosticId : null;
        return service.GetDiagnosticsForSpanAsync(document, range, shouldIncludeDiagnostic,
            priorityProvider, diagnosticKind, cancellationToken);
    }
}
