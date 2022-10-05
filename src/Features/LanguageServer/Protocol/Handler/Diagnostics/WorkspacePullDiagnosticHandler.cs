// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.EditAndContinue;
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
    internal sealed partial class WorkspacePullDiagnosticHandler : AbstractPullDiagnosticHandler<VSInternalWorkspaceDiagnosticsParams, VSInternalWorkspaceDiagnosticReport, VSInternalWorkspaceDiagnosticReport[]>
    {
        public WorkspacePullDiagnosticHandler(IDiagnosticAnalyzerService analyzerService, EditAndContinueDiagnosticUpdateSource editAndContinueDiagnosticUpdateSource, IGlobalOptionService globalOptions)
            : base(analyzerService, editAndContinueDiagnosticUpdateSource, globalOptions)
        {
        }

        protected override VSInternalWorkspaceDiagnosticReport CreateReport(TextDocumentIdentifier identifier, VisualStudio.LanguageServer.Protocol.Diagnostic[]? diagnostics, string? resultId)
            => new VSInternalWorkspaceDiagnosticReport
            {
                TextDocument = identifier,
                Diagnostics = diagnostics,
                ResultId = resultId,
                // Mark these diagnostics as having come from us.  They will be superseded by any diagnostics for the
                // same file produced by the DocumentPullDiagnosticHandler.
                Identifier = WorkspaceDiagnosticIdentifier,
            };

        protected override VSInternalWorkspaceDiagnosticReport CreateRemovedReport(TextDocumentIdentifier identifier)
            => CreateReport(identifier, diagnostics: null, resultId: null);

        protected override VSInternalWorkspaceDiagnosticReport CreateUnchangedReport(TextDocumentIdentifier identifier, string resultId)
            => CreateReport(identifier, diagnostics: null, resultId);

        protected override ImmutableArray<PreviousPullResult>? GetPreviousResults(VSInternalWorkspaceDiagnosticsParams diagnosticsParams)
            => diagnosticsParams.PreviousResults?.Where(d => d.PreviousResultId != null).Select(d => new PreviousPullResult(d.PreviousResultId!, d.TextDocument!)).ToImmutableArray();

        protected override DiagnosticTag[] ConvertTags(DiagnosticData diagnosticData)
        {
            // All workspace diagnostics are potential duplicates given that they can be overridden by the diagnostics
            // produced by document diagnostics.
            return ConvertTags(diagnosticData, potentialDuplicate: true);
        }

        protected override ValueTask<ImmutableArray<IDiagnosticSource>> GetOrderedDiagnosticSourcesAsync(RequestContext context, CancellationToken cancellationToken)
            => GetDiagnosticSourcesAsync(context, GlobalOptions, cancellationToken);

        protected override VSInternalWorkspaceDiagnosticReport[]? CreateReturn(BufferedProgress<VSInternalWorkspaceDiagnosticReport> progress)
        {
            return progress.GetValues();
        }

        internal static async ValueTask<ImmutableArray<IDiagnosticSource>> GetDiagnosticSourcesAsync(RequestContext context, IGlobalOptionService globalOptions, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(context.Solution);

            // If we're being called from razor, we do not support WorkspaceDiagnostics at all.  For razor, workspace
            // diagnostics will be handled by razor itself, which will operate by calling into Roslyn and asking for
            // document-diagnostics instead.
            if (context.ServerKind == WellKnownLspServerKinds.RazorLspServer)
                return ImmutableArray<IDiagnosticSource>.Empty;

            using var _ = ArrayBuilder<IDiagnosticSource>.GetInstance(out var result);

            var solution = context.Solution;

            var documentTrackingService = solution.Services.GetRequiredService<IDocumentTrackingService>();

            // Collect all the documents from the solution in the order we'd like to get diagnostics for.  This will
            // prioritize the files from currently active projects, but then also include all other docs in all projects
            // (depending on current FSA settings).

            var activeDocument = documentTrackingService.GetActiveDocument(solution);
            var visibleDocuments = documentTrackingService.GetVisibleDocuments(solution);

            // Now, prioritize the projects related to the active/visible files.
            await AddDocumentsAndProject(activeDocument?.Project, context.SupportedLanguages, cancellationToken).ConfigureAwait(false);
            foreach (var doc in visibleDocuments)
                await AddDocumentsAndProject(doc.Project, context.SupportedLanguages, cancellationToken).ConfigureAwait(false);

            // finally, add the remainder of all documents.
            foreach (var project in solution.Projects)
                await AddDocumentsAndProject(project, context.SupportedLanguages, cancellationToken).ConfigureAwait(false);

            // Ensure that we only process documents once.
            result.RemoveDuplicates();
            return result.ToImmutable();

            async Task AddDocumentsAndProject(Project? project, ImmutableArray<string> supportedLanguages, CancellationToken cancellationToken)
            {
                if (project == null)
                    return;

                if (!supportedLanguages.Contains(project.Language))
                {
                    // This project is for a language not supported by the LSP server making the request.
                    // Do not report diagnostics for these projects.
                    return;
                }

                var fullSolutionAnalysisEnabled = globalOptions.IsFullSolutionAnalysisEnabled(project.Language);
                var taskListEnabled = globalOptions.GetTaskListOptions().ComputeForClosedFiles;
                if (!fullSolutionAnalysisEnabled && !taskListEnabled)
                    return;

                var documents = ImmutableArray<TextDocument>.Empty.AddRange(project.Documents).AddRange(project.AdditionalDocuments);

                // If all features are enabled for source generated documents, then compute todo-comments/diagnostics for them.
                if (solution.Services.GetService<IWorkspaceConfigurationService>()?.Options.EnableOpeningSourceGeneratedFiles == true)
                {
                    var sourceGeneratedDocuments = await project.GetSourceGeneratedDocumentsAsync(cancellationToken).ConfigureAwait(false);
                    documents = documents.AddRange(sourceGeneratedDocuments);
                }

                foreach (var document in documents)
                {
                    // Only consider closed documents here (and only open ones in the DocumentPullDiagnosticHandler).
                    // Each handler treats those as separate worlds that they are responsible for.
                    if (context.IsTracking(document.GetURI()))
                    {
                        context.TraceInformation($"Skipping tracked document: {document.GetURI()}");
                        continue;
                    }

                    // Do not attempt to get workspace diagnostics for Razor files, Razor will directly ask us for document diagnostics
                    // for any razor file they are interested in.
                    if (document.IsRazorDocument())
                        continue;

                    result.Add(new WorkspaceDocumentDiagnosticSource(document, includeTaskListItems: true, includeStandardDiagnostics: fullSolutionAnalysisEnabled));
                }

                // Finally if fsa is on, we also want to check for diagnostics associated with the project itself.
                if (fullSolutionAnalysisEnabled)
                    result.Add(new ProjectDiagnosticSource(project));
            }
        }
    }
}
