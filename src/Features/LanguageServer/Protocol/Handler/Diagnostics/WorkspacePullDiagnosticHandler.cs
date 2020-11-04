// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.VisualStudio.LanguageServer.Protocol;

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

            using var _ = ArrayBuilder<Document>.GetInstance(out var result);

            var solution = context.Solution;

            var documentTrackingService = solution.Workspace.Services.GetRequiredService<IDocumentTrackingService>();

            // Collect all the documents from the solution in the order we'd like to get diagnostics for.  This will
            // prioritize the current working files, but then also include all other docs in all projects (depending on
            // current FSA settings).  When adding the docs, we do so in a simple manner, not worrying about if a
            // document is added multiple.  That's not a problem though as we do a .Distinct call at the end to ensure
            // that a file only ever appears once in the collection.  As .Distinct preserves order and discards later
            // duplicates it finds, this won't affect important files that we put at the front of the list.

            // The active and visible docs always get priority in terms or results.
            var activeDocument = documentTrackingService.GetActiveDocument(solution);
            var visibleDocuments = documentTrackingService.GetVisibleDocuments(solution);

            result.AddIfNotNull(activeDocument);
            result.AddRange(visibleDocuments);

            // Now, prioritize the projects related to the active/visible files.
            AddDocumentsFromProject(activeDocument?.Project, isOpen: true);
            foreach (var doc in visibleDocuments)
                AddDocumentsFromProject(doc.Project, isOpen: true);

            // finally, add the remainder of all documents.
            foreach (var project in solution.Projects)
                AddDocumentsFromProject(project, isOpen: false);

            return result.Distinct().ToImmutableArray();

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
                result.AddRange(project.Documents);
            }
        }
    }
}
