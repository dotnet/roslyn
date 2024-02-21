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
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.TaskList;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;
internal abstract class AbstractWorkspacePullDiagnosticsHandler<TDiagnosticsParams, TReport, TReturn>
    : AbstractPullDiagnosticHandler<TDiagnosticsParams, TReport, TReturn>, IDisposable
    where TDiagnosticsParams : IPartialResultParams<TReport>
{
    private readonly LspWorkspaceRegistrationService _workspaceRegistrationService;
    private readonly LspWorkspaceManager _workspaceManager;

    /// <summary>
    /// Flag that represents whether the LSP view of the world has changed.
    /// It is totally fine for this to somewhat over-report changes
    /// as it is an optimization used to delay closing workspace diagnostic requests
    /// until something has changed.
    /// </summary>
    private int _lspChanged = 0;

    protected AbstractWorkspacePullDiagnosticsHandler(
        LspWorkspaceManager workspaceManager,
        LspWorkspaceRegistrationService registrationService,
        IDiagnosticAnalyzerService diagnosticAnalyzerService,
        IDiagnosticsRefresher diagnosticRefresher,
        IGlobalOptionService globalOptions) : base(diagnosticAnalyzerService, diagnosticRefresher, globalOptions)
    {
        _workspaceManager = workspaceManager;
        _workspaceRegistrationService = registrationService;

        _workspaceRegistrationService.LspSolutionChanged += OnLspSolutionChanged;
        _workspaceManager.LspTextChanged += OnLspTextChanged;
    }

    public void Dispose()
    {
        _workspaceManager.LspTextChanged -= OnLspTextChanged;
        _workspaceRegistrationService.LspSolutionChanged -= OnLspSolutionChanged;
    }

    protected override async ValueTask<ImmutableArray<IDiagnosticSource>> GetOrderedDiagnosticSourcesAsync(TDiagnosticsParams diagnosticsParams, RequestContext context, CancellationToken cancellationToken)
    {
        // If we're being called from razor, we do not support WorkspaceDiagnostics at all.  For razor, workspace
        // diagnostics will be handled by razor itself, which will operate by calling into Roslyn and asking for
        // document-diagnostics instead.
        if (context.ServerKind == WellKnownLspServerKinds.RazorLspServer)
            return ImmutableArray<IDiagnosticSource>.Empty;

        var category = GetDiagnosticCategory(diagnosticsParams);

        if (category == PullDiagnosticCategories.Task)
            return GetTaskListDiagnosticSources(context, GlobalOptions);

        // if this request doesn't have a category at all (legacy behavior, assume they're asking about everything).
        if (category == null || category == PullDiagnosticCategories.WorkspaceDocumentsAndProject)
            return await GetDiagnosticSourcesAsync(context, GlobalOptions, cancellationToken).ConfigureAwait(false);

        // if it's a category we don't recognize, return nothing.
        return ImmutableArray<IDiagnosticSource>.Empty;
    }

    private void OnLspSolutionChanged(object? sender, WorkspaceChangeEventArgs e)
    {
        UpdateLspChanged();
    }

    private void OnLspTextChanged(object? sender, EventArgs e)
    {
        UpdateLspChanged();
    }

    private void UpdateLspChanged()
    {
        Interlocked.Exchange(ref _lspChanged, 1);
    }

    protected override async Task WaitForChangesAsync(RequestContext context, CancellationToken cancellationToken)
    {
        // Spin waiting until our LSP change flag has been set.  When the flag is set (meaning LSP has changed),
        // we reset the flag to false and exit out of the loop allowing the request to close.
        // The client will automatically trigger a new request as soon as we close it, bringing us up to date on diagnostics.
        while (Interlocked.CompareExchange(ref _lspChanged, value: 0, comparand: 1) == 0)
        {
            // There have been no changes between now and when the last request finished - we will hold the connection open while we poll for changes.
            await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken).ConfigureAwait(false);
        }

        context.TraceInformation("Closing workspace/diagnostics request");
        // We've hit a change, so we close the current request to allow the client to open a new one.
        return;
    }

    private static ImmutableArray<IDiagnosticSource> GetTaskListDiagnosticSources(
            RequestContext context, IGlobalOptionService globalOptions)
    {
        Contract.ThrowIfNull(context.Solution);

        // Only compute task list items for closed files if the option is on for it.
        var taskListEnabled = globalOptions.GetTaskListOptions().ComputeForClosedFiles;
        if (!taskListEnabled)
            return ImmutableArray<IDiagnosticSource>.Empty;

        using var _ = ArrayBuilder<IDiagnosticSource>.GetInstance(out var result);

        foreach (var project in GetProjectsInPriorityOrder(context.Solution, context.SupportedLanguages))
        {
            foreach (var document in project.Documents)
            {
                if (!ShouldSkipDocument(context, document))
                    result.Add(new TaskListDiagnosticSource(document, globalOptions));
            }
        }

        return result.ToImmutable();
    }

    public static async ValueTask<ImmutableArray<IDiagnosticSource>> GetDiagnosticSourcesAsync(
        RequestContext context, IGlobalOptionService globalOptions, CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(context.Solution);

        using var _ = ArrayBuilder<IDiagnosticSource>.GetInstance(out var result);

        var solution = context.Solution;
        var enableDiagnosticsInSourceGeneratedFiles = solution.Services.GetService<ISolutionCrawlerOptionsService>()?.EnableDiagnosticsInSourceGeneratedFiles == true;
        var codeAnalysisService = solution.Workspace.Services.GetRequiredService<ICodeAnalysisDiagnosticAnalyzerService>();

        foreach (var project in GetProjectsInPriorityOrder(solution, context.SupportedLanguages))
            await AddDocumentsAndProjectAsync(project, cancellationToken).ConfigureAwait(false);

        return result.ToImmutable();

        async Task AddDocumentsAndProjectAsync(Project project, CancellationToken cancellationToken)
        {
            // There are two potential sources for reporting workspace diagnostics:
            //
            //  1. Full solution analysis: If the user has enabled Full solution analysis, we always run analysis on the latest
            //                             project snapshot and return up-to-date diagnostics computed from this analysis.
            //
            //  2. Code analysis service: Otherwise, if full solution analysis is disabled, and if we have diagnostics from an explicitly
            //                            triggered code analysis execution on either the current or a prior project snapshot, we return
            //                            diagnostics from this execution. These diagnostics may be stale with respect to the current
            //                            project snapshot, but they match user's intent of not enabling continuous background analysis
            //                            for always having up-to-date workspace diagnostics, but instead computing them explicitly on
            //                            specific project snapshots by manually running the "Run Code Analysis" command on a project or solution.
            //
            // If full solution analysis is disabled AND code analysis was never executed for the given project,
            // we have no workspace diagnostics to report and bail out.
            var fullSolutionAnalysisEnabled = globalOptions.IsFullSolutionAnalysisEnabled(project.Language, out var compilerFullSolutionAnalysisEnabled, out var analyzersFullSolutionAnalysisEnabled);
            if (!fullSolutionAnalysisEnabled && !codeAnalysisService.HasProjectBeenAnalyzed(project.Id))
                return;

            var documents = ImmutableArray<TextDocument>.Empty.AddRange(project.Documents).AddRange(project.AdditionalDocuments);

            // If all features are enabled for source generated documents, then compute todo-comments/diagnostics for them.
            if (enableDiagnosticsInSourceGeneratedFiles)
            {
                var sourceGeneratedDocuments = await project.GetSourceGeneratedDocumentsAsync(cancellationToken).ConfigureAwait(false);
                documents = documents.AddRange(sourceGeneratedDocuments);
            }

            Func<DiagnosticAnalyzer, bool>? shouldIncludeAnalyzer = !compilerFullSolutionAnalysisEnabled || !analyzersFullSolutionAnalysisEnabled
                ? ShouldIncludeAnalyzer : null;
            foreach (var document in documents)
            {
                if (!ShouldSkipDocument(context, document))
                {
                    // Add the appropriate FSA or CodeAnalysis document source to get document diagnostics.
                    var documentDiagnosticSource = fullSolutionAnalysisEnabled
                        ? AbstractWorkspaceDocumentDiagnosticSource.CreateForFullSolutionAnalysisDiagnostics(document, shouldIncludeAnalyzer)
                        : AbstractWorkspaceDocumentDiagnosticSource.CreateForCodeAnalysisDiagnostics(document, codeAnalysisService);
                    result.Add(documentDiagnosticSource);
                }
            }

            // Finally, add the appropriate FSA or CodeAnalysis project source to get project specific diagnostics, not associated with any document.
            var projectDiagnosticSource = fullSolutionAnalysisEnabled
                ? AbstractProjectDiagnosticSource.CreateForFullSolutionAnalysisDiagnostics(project, shouldIncludeAnalyzer)
                : AbstractProjectDiagnosticSource.CreateForCodeAnalysisDiagnostics(project, codeAnalysisService);
            result.Add(projectDiagnosticSource);

            bool ShouldIncludeAnalyzer(DiagnosticAnalyzer analyzer)
            {
                return analyzer.IsCompilerAnalyzer() ? compilerFullSolutionAnalysisEnabled : analyzersFullSolutionAnalysisEnabled;
            }
        }
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

    internal abstract TestAccessor GetTestAccessor();

    internal readonly struct TestAccessor(AbstractWorkspacePullDiagnosticsHandler<TDiagnosticsParams, TReport, TReturn> handler)
    {
        public void TriggerConnectionClose() => handler.UpdateLspChanged();
    }
}
