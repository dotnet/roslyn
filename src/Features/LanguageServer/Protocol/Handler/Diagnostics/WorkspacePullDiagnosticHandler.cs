// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

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
        private readonly IDocumentTrackingService _documentTrackingService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public WorkspacePullDiagnosticHandler(
            ILspSolutionProvider solutionProvider,
            IDiagnosticService diagnosticService,
            IDocumentTrackingService documentTrackingService)
            : base(solutionProvider, diagnosticService)
        {
            _documentTrackingService = documentTrackingService;
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
            using var _ = ArrayBuilder<Document>.GetInstance(out var result);

            var solution = context.Solution;

            // The active and visible docs always get priority in terms or results.
            var activeDocument = _documentTrackingService.GetActiveDocument(solution);
            var visibleDocuments = _documentTrackingService.GetVisibleDocuments(solution);

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
                    var analysisScope = solution.Options.GetOption(SolutionCrawlerOptions.BackgroundAnalysisScopeOption, project.Language);
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
