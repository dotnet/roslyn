// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
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
        {
            _solutionProvider = solutionProvider;
            _diagnosticService = diagnosticService;
            _documentTrackingService = documentTrackingService;
            _diagnosticService.DiagnosticsUpdated += OnDiagnosticsUpdated;
        }

        private void OnDiagnosticsUpdated(object? sender, DiagnosticsUpdatedArgs updateArgs)
        {
            if (updateArgs.DocumentId == null)
                return;

            // Whenever we hear about changes to a document, drop the data we've stored for it.  We'll recompute it as
            // necessary on the next request.
            _documentIdToLastResultId.Remove((updateArgs.Workspace, updateArgs.DocumentId));
        }

        public TextDocumentIdentifier? GetTextDocumentIdentifier(WorkspaceDocumentDiagnosticsParams request)
            => null;

        public async Task<WorkspaceDiagnosticReport[]?> HandleRequestAsync(
            WorkspaceDocumentDiagnosticsParams diagnosticsParams, RequestContext context, CancellationToken cancellationToken)
        {
            var solution = context.Solution;

            // First, let the client know if any workspace documents have gone away.  That way it can remove those for
            // the user from squiggles or error-list.
            NotifyClientOfRemovedDocuments(diagnosticsParams);

            // Now, process all our actual documents and report if diagnostics did or didn't change for every document.
            var requestDocumentToDiagnosticParams = GetDocumentToDiagnosticParams(diagnosticsParams);
            foreach (var document in GetOrderedDocuments(solution))
            {
                // If the client has already asked for diagnostics for this document, see if we have actually recorded any
                // differences, or if they should just use the same diagnostics as before.
                if (DiagnosticsAreUnchanged(requestDocumentToDiagnosticParams, document))
                {
                    // Nothing changed between the last request and this one.  Report a null response to the client
                    // to know they don't need to do anything.
                    diagnosticsParams.PartialResultToken!.Report(new[]
                    {
                        new WorkspaceDiagnosticReport
                        {
                            TextDocument = requestDocumentToDiagnosticParams[document].TextDocument,
                            Diagnostics = null,
                        },
                    });
                }

                // Being asked about this document for the first time.  Or being asked again and we have different diagnostics.

                var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                using var _ = ArrayBuilder<VSDiagnostic>.GetInstance(out var diagnostics);
                foreach (var diagnostic in _diagnosticService.GetDiagnostics(document, includeSuppressedDiagnostics: false, cancellationToken))
                    diagnostics.Add(DiagnosticUtilities.Convert(text, diagnostic));

                var report = RecordDiagnosticReport(document, diagnostics);
                diagnosticsParams.PartialResultToken!.Report(new[] { report });
            }

            return null;
        }

        private WorkspaceDiagnosticReport RecordDiagnosticReport(Document document, ArrayBuilder<VSDiagnostic> diagnostics)
        {
            lock (_gate)
            {
                // Keep track of the diagnostics we reported here so that we can short-circuit producing diagnostics for
                // the same diagnostic set in the future.
                var resultId = _nextResultId++.ToString();
                _documentIdToLastResultId[(document.Project.Solution.Workspace, document.Id)] = resultId;
                return new WorkspaceDiagnosticReport { ResultId = resultId, Diagnostics = diagnostics.ToArray() };
            }
        }

        private bool DiagnosticsAreUnchanged(Dictionary<Document, DiagnosticParams> requestDocumentToDiagnosticParams, Document document)
        {
            lock (_gate)
            {
                var workspace = document.Project.Solution.Workspace;
                return requestDocumentToDiagnosticParams.TryGetValue(document, out var previousDocDiagnosticParams) &&
                       _documentIdToLastResultId.TryGetValue((workspace, document.Id), out var lastReportedResultId) &&
                       lastReportedResultId == previousDocDiagnosticParams.PreviousResultId;
            }
        }

        private void NotifyClientOfRemovedDocuments(WorkspaceDocumentDiagnosticsParams documentDiagnosticsParams)
        {
            if (documentDiagnosticsParams.PreviousResults != null)
            {
                foreach (var previousResult in documentDiagnosticsParams.PreviousResults)
                {
                    var textDocument = previousResult.TextDocument;
                    if (textDocument != null)
                    {
                        var document = _solutionProvider.GetDocument(textDocument);
                        if (document == null)
                        {
                            // Client is asking server about a document that no longer exists (i.e. was removed/deleted from the
                            // workspace).  In that case we need to return an actual diagnostic report with `null` for the
                            // diagnostics to let the client know to dump that file entirely.
                            documentDiagnosticsParams.PartialResultToken!.Report(new[] { new WorkspaceDiagnosticReport { TextDocument = textDocument, ResultId = null, Diagnostics = null } });
                        }
                    }
                }
            }
        }

        private Dictionary<Document, DiagnosticParams> GetDocumentToDiagnosticParams(WorkspaceDocumentDiagnosticsParams documentDiagnosticsParams)
        {
            var requestDocumentToTextDocumentIdentifier = new Dictionary<Document, DiagnosticParams>();

            if (documentDiagnosticsParams.PreviousResults != null)
            {
                foreach (var previousResult in documentDiagnosticsParams.PreviousResults)
                {
                    if (previousResult.TextDocument != null && previousResult.PreviousResultId != null)
                    {
                        var document = _solutionProvider.GetDocument(previousResult.TextDocument);
                        if (document != null)
                            requestDocumentToTextDocumentIdentifier[document] = previousResult;
                    }
                }
            }

            return requestDocumentToTextDocumentIdentifier;
        }

        private ImmutableArray<Document> GetOrderedDocuments(Solution solution)
        {
            using var _ = ArrayBuilder<Document>.GetInstance(out var result);

            // The active and visible docs always get priority in terms or results.
            var activeDocument = _documentTrackingService.GetActiveDocument(solution);
            var visibleDocuments = _documentTrackingService.GetVisibleDocuments(solution);

            result.AddIfNotNull(activeDocument);
            result.AddRange(visibleDocuments);

            // Now, prioritize the projects related to the active/visible files.
            AddDocumentsFromProject(activeDocument?.Project);
            foreach (var doc in visibleDocuments)
                AddDocumentsFromProject(doc.Project);

            // finally, add the remainder of all documents.
            foreach (var project in solution.Projects)
                AddDocumentsFromProject(project);

            return result.Distinct().ToImmutableArray();

            void AddDocumentsFromProject(Project? project)
            {
                if (project != null)
                    result.AddRange(project.Documents);
            }
        }
    }
}
