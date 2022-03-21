// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics
{
    [Method(VSInternalMethods.WorkspacePullDiagnosticName)]
    internal class WorkspacePullDiagnosticHandler : AbstractPullDiagnosticHandler<VSInternalWorkspaceDiagnosticsParams, VSInternalWorkspaceDiagnosticReport, VSInternalWorkspaceDiagnosticReport[]>
    {
        public WorkspacePullDiagnosticHandler(WellKnownLspServerKinds serverKind, IDiagnosticService diagnosticService, EditAndContinueDiagnosticUpdateSource editAndContinueDiagnosticUpdateSource)
            : base(serverKind, diagnosticService, editAndContinueDiagnosticUpdateSource)
        {
        }

        public override TextDocumentIdentifier? GetTextDocumentIdentifier(VSInternalWorkspaceDiagnosticsParams request)
            => null;

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

        protected override ImmutableArray<PreviousPullResult>? GetPreviousResults(VSInternalWorkspaceDiagnosticsParams diagnosticsParams)
            => diagnosticsParams.PreviousResults?.Where(d => d.PreviousResultId != null).Select(d => new PreviousPullResult(d.PreviousResultId!, d.TextDocument!)).ToImmutableArray();

        protected override DiagnosticTag[] ConvertTags(DiagnosticData diagnosticData)
        {
            // All workspace diagnostics are potential duplicates given that they can be overridden by the diagnostics
            // produced by document diagnostics.
            return ConvertTags(diagnosticData, potentialDuplicate: true);
        }

        protected override ValueTask<ImmutableArray<Document>> GetOrderedDocumentsAsync(RequestContext context, CancellationToken cancellationToken)
        {
            return GetWorkspacePullDocumentsAsync(context, DiagnosticService.GlobalOptions, cancellationToken);
        }

        protected override Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(
            RequestContext context, Document document, DiagnosticMode diagnosticMode, CancellationToken cancellationToken)
        {
            // For closed files, go to the IDiagnosticService for results.  These won't necessarily be totally up to
            // date.  However, that's fine as these are closed files and won't be in the process of being edited.  So
            // any deviations in the spans of diagnostics shouldn't be impactful for the user.
            return DiagnosticService.GetPullDiagnosticsAsync(document, includeSuppressedDiagnostics: false, diagnosticMode, cancellationToken).AsTask();
        }

        protected override VSInternalWorkspaceDiagnosticReport[]? CreateReturn(BufferedProgress<VSInternalWorkspaceDiagnosticReport> progress)
        {
            return progress.GetValues();
        }

        internal static async ValueTask<ImmutableArray<Document>> GetWorkspacePullDocumentsAsync(RequestContext context, IGlobalOptionService globalOptions, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(context.Solution);

            // If we're being called from razor, we do not support WorkspaceDiagnostics at all.  For razor, workspace
            // diagnostics will be handled by razor itself, which will operate by calling into Roslyn and asking for
            // document-diagnostics instead.
            if (context.ClientName != null)
                return ImmutableArray<Document>.Empty;

            using var _ = ArrayBuilder<Document>.GetInstance(out var result);

            var solution = context.Solution;

            var documentTrackingService = solution.Workspace.Services.GetRequiredService<IDocumentTrackingService>();

            // Collect all the documents from the solution in the order we'd like to get diagnostics for.  This will
            // prioritize the files from currently active projects, but then also include all other docs in all projects
            // (depending on current FSA settings).

            var activeDocument = documentTrackingService.GetActiveDocument(solution);
            var visibleDocuments = documentTrackingService.GetVisibleDocuments(solution);

            // Now, prioritize the projects related to the active/visible files.
            await AddDocumentsFromProjectAsync(activeDocument?.Project, context.SupportedLanguages, isOpen: true, cancellationToken).ConfigureAwait(false);
            foreach (var doc in visibleDocuments)
                await AddDocumentsFromProjectAsync(doc.Project, context.SupportedLanguages, isOpen: true, cancellationToken).ConfigureAwait(false);

            // finally, add the remainder of all documents.
            foreach (var project in solution.Projects)
                await AddDocumentsFromProjectAsync(project, context.SupportedLanguages, isOpen: false, cancellationToken).ConfigureAwait(false);

            // Ensure that we only process documents once.
            result.RemoveDuplicates();
            return result.ToImmutable();

            async Task AddDocumentsFromProjectAsync(Project? project, ImmutableArray<string> supportedLanguages, bool isOpen, CancellationToken cancellationToken)
            {
                if (project == null)
                    return;

                if (!supportedLanguages.Contains(project.Language))
                {
                    // This project is for a language not supported by the LSP server making the request.
                    // Do not report diagnostics for these projects.
                    return;
                }

                // if the project doesn't necessarily have an open file in it, then only include it if the user has full
                // solution analysis on.
                if (!isOpen)
                {
                    if (globalOptions.GetBackgroundAnalysisScope(project.Language) != BackgroundAnalysisScope.FullSolution)
                    {
                        context.TraceInformation($"Skipping project '{project.Name}' as it has no open document and Full Solution Analysis is off");
                        return;
                    }
                }

                // Otherwise, if the user has an open file from this project, or FSA is on, then include all the
                // documents from it. If all features are enabled for source generated documents, make sure they are
                // included as well.
                var documents = project.Documents;
                if (solution.Workspace.Services.GetService<ISyntaxTreeConfigurationService>() is { EnableOpeningSourceGeneratedFilesInWorkspace: true })
                {
                    documents = documents.Concat(await project.GetSourceGeneratedDocumentsAsync(cancellationToken).ConfigureAwait(false));
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

                    result.Add(document);
                }
            }
        }
    }
}
