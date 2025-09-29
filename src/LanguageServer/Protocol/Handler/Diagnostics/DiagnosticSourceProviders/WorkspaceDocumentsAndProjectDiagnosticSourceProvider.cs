// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;

[Export(typeof(IDiagnosticSourceProvider)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class WorkspaceDocumentsAndProjectDiagnosticSourceProvider(
    [Import] IGlobalOptionService globalOptions)
    : IDiagnosticSourceProvider
{
    public bool IsDocument => false;
    public string Name => PullDiagnosticCategories.WorkspaceDocumentsAndProject;

    public bool IsEnabled(ClientCapabilities clientCapabilities) => true;

    /// <summary>
    /// There are three potential sources for reporting workspace diagnostics:
    ///
    ///  1. Full solution analysis: If the user has enabled Full solution analysis, we always run analysis on the latest
    ///                             project snapshot and return up-to-date diagnostics computed from this analysis.
    ///
    ///  2. Code analysis service: Otherwise, if full solution analysis is disabled, and if we have diagnostics from an explicitly
    ///                            triggered code analysis execution on either the current or a prior project snapshot, we return
    ///                            diagnostics from this execution. These diagnostics may be stale with respect to the current
    ///                            project snapshot, but they match user's intent of not enabling continuous background analysis
    ///                            for always having up-to-date workspace diagnostics, but instead computing them explicitly on
    ///                            specific project snapshots by manually running the "Run Code Analysis" command on a project or solution.
    ///
    ///  3. EnC analysis: Emit and debugger diagnostics associated with a closed document or not associated with any document.
    ///
    /// If full solution analysis is disabled AND code analysis was never executed for the given project,
    /// we have no workspace diagnostics to report and bail out.
    /// </summary>
    public async ValueTask<ImmutableArray<IDiagnosticSource>> CreateDiagnosticSourcesAsync(RequestContext context, CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(context.Solution);

        using var _ = ArrayBuilder<IDiagnosticSource>.GetInstance(out var result);

        var solution = context.Solution;
        var codeAnalysisService = solution.Services.GetRequiredService<ICodeAnalysisDiagnosticAnalyzerService>();

        foreach (var project in WorkspaceDiagnosticSourceHelpers.GetProjectsInPriorityOrder(solution, context.SupportedLanguages))
            await AddDocumentsAndProjectAsync(project, cancellationToken).ConfigureAwait(false);

        return result.ToImmutableAndClear();

        async Task AddDocumentsAndProjectAsync(Project project, CancellationToken cancellationToken)
        {
            var fullSolutionAnalysisEnabled = globalOptions.IsFullSolutionAnalysisEnabled(project.Language, out var compilerFullSolutionAnalysisEnabled, out var analyzersFullSolutionAnalysisEnabled);
            if (!fullSolutionAnalysisEnabled && !codeAnalysisService.HasProjectBeenAnalyzed(project.Id))
                return;

            var filter =
                (compilerFullSolutionAnalysisEnabled ? AnalyzerFilter.CompilerAnalyzer : 0) |
                (analyzersFullSolutionAnalysisEnabled ? AnalyzerFilter.NonCompilerAnalyzer : 0);

            AddDocumentSources(project.Documents);
            AddDocumentSources(project.AdditionalDocuments);

            var sourceGeneratedDocuments = await project.GetSourceGeneratedDocumentsAsync(cancellationToken).ConfigureAwait(false);
            AddDocumentSources(sourceGeneratedDocuments);

            // Finally, add the appropriate FSA or CodeAnalysis project source to get project specific diagnostics, not associated with any document.
            AddProjectSource();

            return;

            void AddDocumentSources(IEnumerable<TextDocument> documents)
            {
                foreach (var document in documents)
                {
                    if (!WorkspaceDiagnosticSourceHelpers.ShouldSkipDocument(context, document))
                    {
                        // Add the appropriate FSA or CodeAnalysis document source to get document diagnostics.
                        var documentDiagnosticSource = fullSolutionAnalysisEnabled
                            ? AbstractWorkspaceDocumentDiagnosticSource.CreateForFullSolutionAnalysisDiagnostics(document, filter)
                            : AbstractWorkspaceDocumentDiagnosticSource.CreateForCodeAnalysisDiagnostics(document, codeAnalysisService);
                        result.Add(documentDiagnosticSource);
                    }
                }
            }

            void AddProjectSource()
            {
                var projectDiagnosticSource = fullSolutionAnalysisEnabled
                    ? AbstractProjectDiagnosticSource.CreateForFullSolutionAnalysisDiagnostics(project, filter)
                    : AbstractProjectDiagnosticSource.CreateForCodeAnalysisDiagnostics(project, codeAnalysisService);
                result.Add(projectDiagnosticSource);
            }
        }
    }
}
