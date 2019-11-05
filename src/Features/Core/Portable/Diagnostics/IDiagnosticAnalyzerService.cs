// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal interface IDiagnosticAnalyzerService
    {
        /// <summary>
        /// Re-analyze given projects and documents
        /// </summary>
        void Reanalyze(Workspace workspace, IEnumerable<ProjectId> projectIds = null, IEnumerable<DocumentId> documentIds = null, bool highPriority = false);

        /// <summary>
        /// Get specific diagnostics currently stored in the source. returned diagnostic might be out-of-date if solution has changed but analyzer hasn't run for the new solution.
        /// </summary>
        Task<ImmutableArray<DiagnosticData>> GetSpecificCachedDiagnosticsAsync(Workspace workspace, object id, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get diagnostics currently stored in the source. returned diagnostic might be out-of-date if solution has changed but analyzer hasn't run for the new solution.
        /// </summary>
        Task<ImmutableArray<DiagnosticData>> GetCachedDiagnosticsAsync(Workspace workspace, ProjectId projectId = null, DocumentId documentId = null, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get specific diagnostics for the given solution. all diagnostics returned should be up-to-date with respect to the given solution.
        /// </summary>
        Task<ImmutableArray<DiagnosticData>> GetSpecificDiagnosticsAsync(Solution solution, object id, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get diagnostics for the given solution. all diagnostics returned should be up-to-date with respect to the given solution.
        /// </summary>
        Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(Solution solution, ProjectId projectId = null, DocumentId documentId = null, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Force computes diagnostics and raises diagnostic events for the given project or solution. all diagnostics returned should be up-to-date with respect to the given project or solution.
        /// </summary>
        Task ForceAnalyzeAsync(Solution solution, ProjectId projectId = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// True if given project has any diagnostics
        /// </summary>
        bool ContainsDiagnostics(Workspace workspace, ProjectId projectId);

        /// <summary>
        /// Get diagnostics of the given diagnostic ids from the given solution. all diagnostics returned should be up-to-date with respect to the given solution.
        /// Note that for project case, this method returns diagnostics from all project documents as well. Use <see cref="GetProjectDiagnosticsForIdsAsync(Solution, ProjectId, ImmutableHashSet{string}, bool, CancellationToken)"/>
        /// if you want to fetch only project diagnostics without source locations.
        /// </summary>
        Task<ImmutableArray<DiagnosticData>> GetDiagnosticsForIdsAsync(Solution solution, ProjectId projectId = null, DocumentId documentId = null, ImmutableHashSet<string> diagnosticIds = null, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get project diagnostics (diagnostics with no source location) of the given diagnostic ids from the given solution. all diagnostics returned should be up-to-date with respect to the given solution.
        /// Note that this method doesn't return any document diagnostics. Use <see cref="GetDiagnosticsForIdsAsync(Solution, ProjectId, DocumentId, ImmutableHashSet{string}, bool, CancellationToken)"/> to also fetch those.
        /// </summary>
        Task<ImmutableArray<DiagnosticData>> GetProjectDiagnosticsForIdsAsync(Solution solution, ProjectId projectId = null, ImmutableHashSet<string> diagnosticIds = null, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Try to return up to date diagnostics for the given span for the document.
        ///
        /// It will return true if it was able to return all up-to-date diagnostics.
        ///  otherwise, false indicating there are some missing diagnostics in the diagnostic list
        /// </summary>
        Task<bool> TryAppendDiagnosticsForSpanAsync(Document document, TextSpan range, List<DiagnosticData> diagnostics, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Return up to date diagnostics for the given span for the document
        ///
        /// This can be expensive since it is force analyzing diagnostics if it doesn't have up-to-date one yet.
        /// If diagnosticIdOpt is not null, it gets diagnostics only for this given diagnosticIdOpt value
        /// </summary>
        Task<IEnumerable<DiagnosticData>> GetDiagnosticsForSpanAsync(Document document, TextSpan range, string diagnosticIdOpt = null, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a list of <see cref="DiagnosticAnalyzer"/>s for the given <see cref="Project"/>
        /// </summary>
        ImmutableArray<DiagnosticAnalyzer> GetDiagnosticAnalyzers(Project project);

        /// <summary>
        /// Gets a list of <see cref="DiagnosticDescriptor"/>s per <see cref="AnalyzerReference"/>
        /// If the given <paramref name="projectOpt"/> is non-null, then gets <see cref="DiagnosticDescriptor"/>s for the project.
        /// Otherwise, returns the global set of <see cref="DiagnosticDescriptor"/>s enabled for the workspace.
        /// </summary>
        /// <returns>A mapping from <see cref="AnalyzerReference.Display"/> to the <see cref="DiagnosticDescriptor"/></returns>
        ImmutableDictionary<string, ImmutableArray<DiagnosticDescriptor>> CreateDiagnosticDescriptorsPerReference(Project projectOpt);

        /// <summary>
        /// Gets supported <see cref="DiagnosticDescriptor"/>s of <see cref="DiagnosticAnalyzer"/>.
        /// </summary>
        /// <returns>A list of the diagnostic descriptors of the analyzer</returns>
        ImmutableArray<DiagnosticDescriptor> GetDiagnosticDescriptors(DiagnosticAnalyzer analyzer);

        /// <summary>
        /// Check whether given diagnostic is compiler diagnostic or not
        /// </summary>
        bool IsCompilerDiagnostic(string language, DiagnosticData diagnostic);

        /// <summary>
        /// Get compiler analyzer for the given language
        /// </summary>
        DiagnosticAnalyzer GetCompilerDiagnosticAnalyzer(string language);

        /// <summary>
        /// Check whether given <see cref="DiagnosticAnalyzer"/> is compiler analyzer for the language or not.
        /// </summary>
        bool IsCompilerDiagnosticAnalyzer(string language, DiagnosticAnalyzer analyzer);

        /// <summary>
        /// Check whether given <see cref="DiagnosticAnalyzer"/> is compilation end analyzer
        /// By compilation end analyzer, it means compilation end analysis here
        /// </summary>
        bool IsCompilationEndAnalyzer(DiagnosticAnalyzer analyzer, Project project, Compilation compilation);

        /// <summary>
        /// Return host <see cref="AnalyzerReference"/>s. (ex, analyzers installed by vsix)
        /// </summary>
        IEnumerable<AnalyzerReference> GetHostAnalyzerReferences();
    }
}
