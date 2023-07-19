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
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Api;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.TaskList;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics
{
    [Method(VSInternalMethods.WorkspacePullDiagnosticName)]
    internal sealed partial class WorkspacePullDiagnosticHandler : AbstractPullDiagnosticHandler<VSInternalWorkspaceDiagnosticsParams, VSInternalWorkspaceDiagnosticReport[], VSInternalWorkspaceDiagnosticReport[]>
    {
        public WorkspacePullDiagnosticHandler(IDiagnosticAnalyzerService analyzerService, IDiagnosticsRefresher diagnosticsRefresher, IGlobalOptionService globalOptions)
            : base(analyzerService, diagnosticsRefresher, globalOptions)
        {
        }

        protected override string? GetDiagnosticCategory(VSInternalWorkspaceDiagnosticsParams diagnosticsParams)
            => diagnosticsParams.QueryingDiagnosticKind?.Value;

        protected override VSInternalWorkspaceDiagnosticReport[] CreateReport(TextDocumentIdentifier identifier, VisualStudio.LanguageServer.Protocol.Diagnostic[]? diagnostics, string? resultId)
            => new[] {
                new VSInternalWorkspaceDiagnosticReport
                {
                    TextDocument = identifier,
                    Diagnostics = diagnostics,
                    ResultId = resultId,
                    // Mark these diagnostics as having come from us.  They will be superseded by any diagnostics for the
                    // same file produced by the DocumentPullDiagnosticHandler.
                    Identifier = WorkspaceDiagnosticIdentifier,
                }
            };

        protected override VSInternalWorkspaceDiagnosticReport[] CreateRemovedReport(TextDocumentIdentifier identifier)
            => CreateReport(identifier, diagnostics: null, resultId: null);

        protected override VSInternalWorkspaceDiagnosticReport[] CreateUnchangedReport(TextDocumentIdentifier identifier, string resultId)
            => CreateReport(identifier, diagnostics: null, resultId);

        protected override ImmutableArray<PreviousPullResult>? GetPreviousResults(VSInternalWorkspaceDiagnosticsParams diagnosticsParams)
            => diagnosticsParams.PreviousResults?.Where(d => d.PreviousResultId != null).Select(d => new PreviousPullResult(d.PreviousResultId!, d.TextDocument!)).ToImmutableArray();

        protected override DiagnosticTag[] ConvertTags(DiagnosticData diagnosticData)
        {
            // All workspace diagnostics are potential duplicates given that they can be overridden by the diagnostics
            // produced by document diagnostics.
            return ConvertTags(diagnosticData, potentialDuplicate: true);
        }

        protected override async ValueTask<ImmutableArray<IDiagnosticSource>> GetOrderedDiagnosticSourcesAsync(
            VSInternalWorkspaceDiagnosticsParams diagnosticsParams,
            RequestContext context,
            CancellationToken cancellationToken)
        {
            // If we're being called from razor, we do not support WorkspaceDiagnostics at all.  For razor, workspace
            // diagnostics will be handled by razor itself, which will operate by calling into Roslyn and asking for
            // document-diagnostics instead.
            if (context.ServerKind == WellKnownLspServerKinds.RazorLspServer)
                return ImmutableArray<IDiagnosticSource>.Empty;

            var category = diagnosticsParams.QueryingDiagnosticKind?.Value;

            if (category == PullDiagnosticCategories.Task)
                return GetTaskListDiagnosticSources(context, GlobalOptions);

            // if this request doesn't have a category at all (legacy behavior, assume they're asking about everything).
            if (category == null || category == PullDiagnosticCategories.WorkspaceDocumentsAndProject)
                return await GetDiagnosticSourcesAsync(context, GlobalOptions, cancellationToken).ConfigureAwait(false);

            // if it's a category we don't recognize, return nothing.
            return ImmutableArray<IDiagnosticSource>.Empty;
        }

        protected override VSInternalWorkspaceDiagnosticReport[]? CreateReturn(BufferedProgress<VSInternalWorkspaceDiagnosticReport[]> progress)
        {
            return progress.GetFlattenedValues();
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
            if (document.IsRazorDocument())
                return true;

            return false;
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

            foreach (var project in GetProjectsInPriorityOrder(solution, context.SupportedLanguages))
                await AddDocumentsAndProject(project, cancellationToken).ConfigureAwait(false);

            return result.ToImmutable();

            async Task AddDocumentsAndProject(Project project, CancellationToken cancellationToken)
            {
                var fullSolutionAnalysisEnabled = globalOptions.IsFullSolutionAnalysisEnabled(project.Language, out var compilerFullSolutionAnalysisEnabled, out var analyzersFullSolutionAnalysisEnabled);
                if (!fullSolutionAnalysisEnabled)
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
                        result.Add(new WorkspaceDocumentDiagnosticSource(document, shouldIncludeAnalyzer));
                }

                // Finally, add the project source to get project specific diagnostics, not associated with any document.
                result.Add(new ProjectDiagnosticSource(project, shouldIncludeAnalyzer));

                bool ShouldIncludeAnalyzer(DiagnosticAnalyzer analyzer)
                {
                    if (analyzer.IsCompilerAnalyzer())
                        return compilerFullSolutionAnalysisEnabled;
                    else
                        return analyzersFullSolutionAnalysisEnabled;
                }
            }
        }
    }
}
