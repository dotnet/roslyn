// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics
{
    [ExportLspMethod(MSLSPMethods.WorkspacePullDiagnosticName, mutatesSolutionState: false), Shared]
    internal class WorkspacePullDiagnosticHandler : AbstractPullDiagnosticHandler<WorkspaceDocumentDiagnosticsParams, WorkspaceDiagnosticReport>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public WorkspacePullDiagnosticHandler(
            ILspSolutionProvider solutionProvider,
            IDiagnosticService diagnosticService)
            : base(solutionProvider, diagnosticService)
        {
        }

        public override TextDocumentIdentifier? GetTextDocumentIdentifier(WorkspaceDocumentDiagnosticsParams request)
            => null;

        protected override WorkspaceDiagnosticReport CreateReport(TextDocumentIdentifier? identifier, VSDiagnostic[]? diagnostics, string? resultId)
            => new WorkspaceDiagnosticReport { TextDocument = identifier, Diagnostics = diagnostics, ResultId = resultId };

        protected override IProgress<WorkspaceDiagnosticReport[]>? GetProgress(WorkspaceDocumentDiagnosticsParams diagnosticsParams)
            => diagnosticsParams.PartialResultToken;

        protected override DiagnosticParams[]? GetPreviousResults(WorkspaceDocumentDiagnosticsParams diagnosticsParams)
            => diagnosticsParams.PreviousResults;

        protected override ImmutableArray<Document> GetOrderedDocuments(RequestContext context)
        {
            // If we're being called from razor, we do not support WorkspaceDiagnostics at all.  For razor, workspace
            // diagnostics will be handled by razor itself, which will operate by calling into Roslyn and asking for
            // document-diagnostics instead.
            if (context.ClientName != null)
                return ImmutableArray<Document>.Empty;

            using var _1 = ArrayBuilder<Document>.GetInstance(out var result);

            var solution = context.Solution;

            var documentTrackingService = solution.Workspace.Services.GetRequiredService<IDocumentTrackingService>();

            // Collect all the documents from the solution in the order we'd like to get diagnostics for.  This will
            // prioritize the files from currently active projects, but then also include all other docs in all projects
            // (depending on current FSA settings).

            // The active and visible docs will not be included as those are handled by DocumentPullDiagnosticHandler.

            using var _2 = PooledHashSet<Document>.GetInstance(out var openDocuments);

            var activeDocument = documentTrackingService.GetActiveDocument(solution);
            var visibleDocuments = documentTrackingService.GetVisibleDocuments(solution);

            openDocuments.AddRange(visibleDocuments);
            if (activeDocument != null)
                openDocuments.Add(activeDocument);

            // Now, prioritize the projects related to the active/visible files.
            AddDocumentsFromProject(activeDocument?.Project, isOpen: true);
            foreach (var doc in visibleDocuments)
                AddDocumentsFromProject(doc.Project, isOpen: true);

            // finally, add the remainder of all documents.
            foreach (var project in solution.Projects)
                AddDocumentsFromProject(project, isOpen: false);

            // Ensure that we only process documents once.
            result.RemoveDuplicates();
            return result.ToImmutable();

            void AddDocumentsFromProject(Project? project, bool isOpen)
            {
                if (project == null)
                    return;

                // if the project doesn't necessarily have an open file in it, then only include it if the user has full
                // solution analysis on.
                if (!isOpen)
                {
                    var analysisScope = solution.Workspace.Options.GetOption(SolutionCrawlerOptions.BackgroundAnalysisScopeOption, project.Language);
                    if (analysisScope != BackgroundAnalysisScope.FullSolution)
                        return;
                }

                // Otherwise, if the user has an open file from this project, or FSA is on, then include all the
                // documents from it.
                foreach (var document in project.Documents)
                {
                    // Don't process any open documents.  Those are handled by DocumentPullDiagnosticHandler.
                    if (!openDocuments.Contains(document))
                        result.AddRange(document);
                }
            }
        }

        protected override Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(Document document, Option2<DiagnosticMode> diagnosticMode, CancellationToken cancellationToken)
        {
            // For closed files, go to the IDiagnosticService for results.  These won't necessarily be totally up to
            // date.  However, that's fine as these are closed files and won't be in the process of being edited.  So
            // any deviations in the spans of diagnostics shouldn't be impactful for the user.
            var diagnostics = this.DiagnosticService.GetPullDiagnostics(document, includeSuppressedDiagnostics: false, diagnosticMode, cancellationToken);
            return Task.FromResult(diagnostics);
        }
    }
}
