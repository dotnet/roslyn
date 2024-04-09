// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.TaskList;
using Roslyn.Utilities;


namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;

internal abstract class AbstractWorkspaceDiagnosticSourceProvider(
    IDiagnosticAnalyzerService diagnosticAnalyzerService,
    IGlobalOptionService globalOptions,
    ImmutableArray<string> sourceNames)
    : IDiagnosticSourceProvider
{
    public bool IsDocument => false;
    public ImmutableArray<string> SourceNames => sourceNames;

    public async ValueTask<ImmutableArray<IDiagnosticSource>> CreateDiagnosticSourcesAsync(RequestContext context, string sourceName, CancellationToken cancellationToken)
    {
        // If we're being called from razor, we do not support WorkspaceDiagnostics at all.  For razor, workspace
        // diagnostics will be handled by razor itself, which will operate by calling into Roslyn and asking for
        // document-diagnostics instead.
        if (context.ServerKind == WellKnownLspServerKinds.RazorLspServer)
            return [];

        if (sourceName == PullDiagnosticCategories.Task)
            return GetTaskListDiagnosticSources(context, globalOptions);

        if (sourceName == PullDiagnosticCategories.EditAndContinue)
            return await EditAndContinueDiagnosticSource.CreateWorkspaceDiagnosticSourcesAsync(context.Solution!, document => context.IsTracking(document.GetURI()), cancellationToken).ConfigureAwait(false);

        // if this request doesn't have a category at all (legacy behavior, assume they're asking about everything).
        if (sourceName == PullDiagnosticCategories.WorkspaceDocumentsAndProject)
            return await GetDiagnosticSourcesAsync(context, globalOptions, diagnosticAnalyzerService, cancellationToken).ConfigureAwait(false);

        // if it's a category we don't recognize, return nothing.
        return [];
    }

    private static ImmutableArray<IDiagnosticSource> GetTaskListDiagnosticSources(
            RequestContext context, IGlobalOptionService globalOptions)
    {
        Contract.ThrowIfNull(context.Solution);

        // Only compute task list items for closed files if the option is on for it.
        var taskListEnabled = globalOptions.GetTaskListOptions().ComputeForClosedFiles;
        if (!taskListEnabled)
            return [];

        using var _ = ArrayBuilder<IDiagnosticSource>.GetInstance(out var result);

        foreach (var project in GetProjectsInPriorityOrder(context.Solution, context.SupportedLanguages))
        {
            foreach (var document in project.Documents)
            {
                if (!ShouldSkipDocument(context, document))
                    result.Add(new TaskListDiagnosticSource(document, globalOptions));
            }
        }

        return result.ToImmutableAndClear();
    }

    private static IEnumerable<Project> GetProjectsInPriorityOrder(
            Solution solution, ImmutableArray<string> supportedLanguages)
    {
        return GetProjectsInPriorityOrderWorker(solution)
            .WhereNotNull()
            .Distinct()
            .Where(p => supportedLanguages.Contains(p.Language));

        static IEnumerable<Project?> GetProjectsInPriorityOrderWorker(Solution solution)
        {
            var documentTrackingService = solution.Services.GetRequiredService<IDocumentTrackingService>();

            // Collect all the documents from the solution in the order we'd like to get diagnostics for.  This will
            // prioritize the files from currently active projects, but then also include all other docs in all projects
            // (depending on current FSA settings).

            var activeDocument = documentTrackingService.GetActiveDocument(solution);
            var visibleDocuments = documentTrackingService.GetVisibleDocuments(solution);

            yield return activeDocument?.Project;
            foreach (var doc in visibleDocuments)
                yield return doc.Project;

            foreach (var project in solution.Projects)
                yield return project;
        }
    }

    private static bool ShouldSkipDocument(RequestContext context, TextDocument document)
    {
        // Only consider closed documents here (and only open ones in the DocumentPullDiagnosticHandler).
        // Each handler treats those as separate worlds that they are responsible for.
        if (context.IsTracking(document.GetURI()))
        {
            context.TraceInformation($"Skipping tracked document: {document.GetURI()}");
            return true;
        }

        // Do not attempt to get workspace diagnostics for Razor files, Razor will directly ask us for document diagnostics
        // for any razor file they are interested in.
        return document.IsRazorDocument();
    }

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
    public static async ValueTask<ImmutableArray<IDiagnosticSource>> GetDiagnosticSourcesAsync(
        RequestContext context, IGlobalOptionService globalOptions, IDiagnosticAnalyzerService diagnosticAnalyzerService, CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(context.Solution);

        using var _ = ArrayBuilder<IDiagnosticSource>.GetInstance(out var result);

        var solution = context.Solution;
        var enableDiagnosticsInSourceGeneratedFiles = solution.Services.GetService<ISolutionCrawlerOptionsService>()?.EnableDiagnosticsInSourceGeneratedFiles == true;
        var codeAnalysisService = solution.Services.GetRequiredService<ICodeAnalysisDiagnosticAnalyzerService>();

        foreach (var project in GetProjectsInPriorityOrder(solution, context.SupportedLanguages))
            await AddDocumentsAndProjectAsync(project, diagnosticAnalyzerService, cancellationToken).ConfigureAwait(false);

        return result.ToImmutableAndClear();

        async Task AddDocumentsAndProjectAsync(Project project, IDiagnosticAnalyzerService diagnosticAnalyzerService, CancellationToken cancellationToken)
        {
            var fullSolutionAnalysisEnabled = globalOptions.IsFullSolutionAnalysisEnabled(project.Language, out var compilerFullSolutionAnalysisEnabled, out var analyzersFullSolutionAnalysisEnabled);
            if (!fullSolutionAnalysisEnabled && !codeAnalysisService.HasProjectBeenAnalyzed(project.Id))
                return;

            Func<DiagnosticAnalyzer, bool>? shouldIncludeAnalyzer = !compilerFullSolutionAnalysisEnabled || !analyzersFullSolutionAnalysisEnabled
                ? ShouldIncludeAnalyzer : null;

            AddDocumentSources(project.Documents);
            AddDocumentSources(project.AdditionalDocuments);

            // If all features are enabled for source generated documents, then compute todo-comments/diagnostics for them.
            if (enableDiagnosticsInSourceGeneratedFiles)
            {
                var sourceGeneratedDocuments = await project.GetSourceGeneratedDocumentsAsync(cancellationToken).ConfigureAwait(false);
                AddDocumentSources(sourceGeneratedDocuments);
            }

            // Finally, add the appropriate FSA or CodeAnalysis project source to get project specific diagnostics, not associated with any document.
            AddProjectSource();

            return;

            void AddDocumentSources(IEnumerable<TextDocument> documents)
            {
                foreach (var document in documents)
                {
                    if (!ShouldSkipDocument(context, document))
                    {
                        // Add the appropriate FSA or CodeAnalysis document source to get document diagnostics.
                        var documentDiagnosticSource = fullSolutionAnalysisEnabled
                            ? AbstractWorkspaceDocumentDiagnosticSource.CreateForFullSolutionAnalysisDiagnostics(document, diagnosticAnalyzerService, shouldIncludeAnalyzer)
                            : AbstractWorkspaceDocumentDiagnosticSource.CreateForCodeAnalysisDiagnostics(document, codeAnalysisService);
                        result.Add(documentDiagnosticSource);
                    }
                }
            }

            void AddProjectSource()
            {
                var projectDiagnosticSource = fullSolutionAnalysisEnabled
                    ? AbstractProjectDiagnosticSource.CreateForFullSolutionAnalysisDiagnostics(project, diagnosticAnalyzerService, shouldIncludeAnalyzer)
                    : AbstractProjectDiagnosticSource.CreateForCodeAnalysisDiagnostics(project, codeAnalysisService);
                result.Add(projectDiagnosticSource);
            }

            bool ShouldIncludeAnalyzer(DiagnosticAnalyzer analyzer)
                => analyzer.IsCompilerAnalyzer() ? compilerFullSolutionAnalysisEnabled : analyzersFullSolutionAnalysisEnabled;
        }
    }
}

