// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Host;
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
    /// Forces analyzers to run on the given project and return all diagnostics, regardless of current environment
    /// settings (like 'only run analyzers on open files', etc.).  This is meant to be used by explicit invocations
    /// of features like "Run Code Analysis".  Note: not all analyzers will necessarily run.  For example, analyzers
    /// where all diagnostic descriptors are currently hidden will not run, as they would not produce any actual
    /// diagnostics.
    /// </summary>
    Task<ImmutableArray<DiagnosticData>> ForceRunCodeAnalysisDiagnosticsAsync(
        Project project, CancellationToken cancellationToken);

    /// <summary>
    /// Returns <see langword="true"/> if any of the given diagnostic IDs belong to an analyzer that is considered
    /// 'deprioritized'.  Deprioritized analyzers are ones that are considered expensive, due to registering symbol-end
    /// and semantic-model actions.  Because of their high cost, we want to avoid running them in the <see
    /// cref="CodeActionRequestPriority.Default"/> priority case, and only run them in the <see
    /// cref="CodeActionRequestPriority.Low"/> case.
    /// </summary>
    Task<bool> IsAnyDiagnosticIdDeprioritizedAsync(
        Project project, ImmutableArray<string> diagnosticIds, CancellationToken cancellationToken);

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
    /// <param name="analyzerFilter">Which analyzers to run.</param>
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
        Project project, ImmutableArray<DocumentId> documentIds, ImmutableHashSet<string>? diagnosticIds, AnalyzerFilter analyzerFilter, bool includeLocalDocumentDiagnostics, CancellationToken cancellationToken);

    /// <summary>
    /// Get project diagnostics (diagnostics with no source location) of the given diagnostic ids and/or analyzers from
    /// the given solution. all diagnostics returned should be up-to-date with respect to the given solution. Note that
    /// this method doesn't return any document diagnostics. Use <see cref="GetDiagnosticsForIdsAsync"/> to also fetch
    /// those.
    /// </summary>
    /// <param name="project">Project to fetch the diagnostics for.</param>
    /// <param name="diagnosticIds">Optional set of diagnostic IDs to scope the returned diagnostics.</param>
    /// <param name="analyzerFilter">Which analyzers to run.</param>
    Task<ImmutableArray<DiagnosticData>> GetProjectDiagnosticsForIdsAsync(
        Project project, ImmutableHashSet<string>? diagnosticIds, AnalyzerFilter analyzerFilter, CancellationToken cancellationToken);

    /// <summary>
    /// Return up to date diagnostics for the given span for the document
    /// <para/>
    /// Non-local diagnostics for the requested document are not returned.  In other words, only the diagnostics
    /// produced by running the requested filtered set of analyzers <em>only</em> on this document are returned here.
    /// To get non-local diagnostics for a document, use <see cref="GetDiagnosticsForIdsAsync"/>.  Non-local diagnostics
    /// will always be returned for the document in that case.
    /// </summary>
    Task<ImmutableArray<DiagnosticData>> GetDiagnosticsForSpanAsync(
        TextDocument document, TextSpan? range,
        DiagnosticIdFilter diagnosticIdFilter,
        CodeActionRequestPriority? priority,
        DiagnosticKind diagnosticKind,
        CancellationToken cancellationToken);

    /// <inheritdoc cref="HostDiagnosticAnalyzers.GetDiagnosticDescriptorsPerReference"/>
    Task<ImmutableDictionary<string, ImmutableArray<DiagnosticDescriptor>>> GetDiagnosticDescriptorsPerReferenceAsync(
        Solution solution, ProjectId? projectId, CancellationToken cancellationToken);

    /// <param name="projectId">A project within <paramref name="solution"/> where <paramref name="analyzerReference"/> can be found</param>
    Task<ImmutableArray<DiagnosticDescriptor>> GetDiagnosticDescriptorsAsync(
        Solution solution, ProjectId projectId, AnalyzerReference analyzerReference, string language, CancellationToken cancellationToken);

    /// <summary>
    /// For all analyzers in the given solution, return the descriptor ids of all compilation end diagnostics.
    /// Note: this does not include the "built in compiler analyzer".
    /// </summary>
    Task<ImmutableArray<string>> GetCompilationEndDiagnosticDescriptorIdsAsync(
        Solution solution, CancellationToken cancellationToken);
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
    public static Task<ImmutableArray<DiagnosticData>> GetDiagnosticsForSpanAsync(
        this IDiagnosticAnalyzerService service, TextDocument document, TextSpan? range, DiagnosticKind diagnosticKind, CancellationToken cancellationToken)
        => service.GetDiagnosticsForSpanAsync(
            document, range,
            diagnosticId: null,
            priority: null,
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
        CodeActionRequestPriority? priority,
        DiagnosticKind diagnosticKind,
        CancellationToken cancellationToken)
    {
        var filter = diagnosticId != null
            ? DiagnosticIdFilter.Include([diagnosticId])
            : DiagnosticIdFilter.All;
        return service.GetDiagnosticsForSpanAsync(
            document, range, filter, priority, diagnosticKind, cancellationToken);
    }

    public static Task<ImmutableDictionary<string, ImmutableArray<DiagnosticDescriptor>>> GetDiagnosticDescriptorsPerReferenceAsync(
        this IDiagnosticAnalyzerService service, Solution solution, CancellationToken cancellationToken)
        => service.GetDiagnosticDescriptorsPerReferenceAsync(solution, projectId: null, cancellationToken);

    public static Task<ImmutableDictionary<string, ImmutableArray<DiagnosticDescriptor>>> GetDiagnosticDescriptorsPerReferenceAsync(
        this IDiagnosticAnalyzerService service, Project project, CancellationToken cancellationToken)
        => service.GetDiagnosticDescriptorsPerReferenceAsync(project.Solution, project.Id, cancellationToken);

}
