// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics
{
    using LspDiagnostic = Microsoft.VisualStudio.LanguageServer.Protocol.Diagnostic;

    internal abstract class AbstractPullDiagnosticHandler<TParams, TReport> : IRequestHandler<TParams, TReport[]?>
        where TReport : DiagnosticReport
    {
        private readonly IDiagnosticService _diagnosticService;

        /// <summary>
        /// Lock to product <see cref="_documentIdToLastResultId"/> and <see cref="_nextResultId"/>.
        /// </summary>
        private readonly object _gate = new object();

        /// <summary>
        /// Mapping of a document to the last result id we reported for it.
        /// </summary>
        private readonly Dictionary<(Workspace workspace, DocumentId documentId), string> _documentIdToLastResultId = new();

        /// <summary>
        /// The next available id to label results with.
        /// </summary>
        private long _nextResultId;

        protected AbstractPullDiagnosticHandler(
            ILspSolutionProvider solutionProvider,
            IDiagnosticService diagnosticService)
        {
            _diagnosticService = diagnosticService;
            _diagnosticService.DiagnosticsUpdated += OnDiagnosticsUpdated;
        }

        public abstract TextDocumentIdentifier? GetTextDocumentIdentifier(TParams request);
        protected abstract TextDocumentIdentifier? GetTextDocument(Document? document);
        protected abstract DiagnosticParams GetPreviousParams(TParams? diagnosticParams, Document? document);
        protected abstract TReport CreateReport(TextDocumentIdentifier? identifier, ArrayBuilder<LspDiagnostic>? result, string? resultId);

        private void OnDiagnosticsUpdated(object? sender, DiagnosticsUpdatedArgs updateArgs)
        {
            if (updateArgs.DocumentId == null)
                return;

            // Whenever we hear about changes to a document, drop the data we've stored for it.  We'll recompute it as
            // necessary on the next request.
            _documentIdToLastResultId.Remove((updateArgs.Workspace, updateArgs.DocumentId));
        }

        public async Task<TReport[]?> HandleRequestAsync(
            TParams diagnosticParams, RequestContext context, CancellationToken cancellationToken)
        {
            var document = context.Document;
            var diagnosticReport = await GetDiagnosticReportAsync(
                document, GetTextDocument(document), GetPreviousParams(diagnosticParams, document), cancellationToken).ConfigureAwait(false);

            if (diagnosticReport == null)
            {
                // Nothing changed between the last request and this one.  Report a null response to the client
                // to know they don't need to do anything.
                return null;
            }

            return new[] { diagnosticReport };
        }

        protected async Task<TReport?> GetDiagnosticReportAsync(
            Document? document, TextDocumentIdentifier? identifier, DiagnosticParams previousDiagnosticParams, CancellationToken cancellationToken)
        {
            if (document == null)
            {
                // Client is asking server about a document that no longer exists (i.e. was removed/deleted from the
                // workspace).  In that case we need to return an actual diagnostic report with `null` for the
                // diagnostics to let the client know to dump that file entirely.
                return CreateReport(identifier: null, result: null, resultId: null);
            }

            var project = document.Project;
            var solution = project.Solution;
            var workspace = solution.Workspace;

            // If the client has already asked for diagnostics for this document, see if we have actually recorded any
            // differences, or if they should just use the same diagnostics as before.
            if (previousDiagnosticParams.PreviousResultId != null)
            {
                lock (_gate)
                {
                    if (_documentIdToLastResultId.TryGetValue((workspace, document.Id), out var lastReportedResultId) &&
                        lastReportedResultId == previousDiagnosticParams.PreviousResultId)
                    {
                        // Nothing changed between the last request and this one.  Report a null response to the client
                        // to know they don't need to do anything.
                        return null;
                    }
                }
            }

            // Being asked about this document for the first time.  Or being asked again and we have different diagnostics.

            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            using var _ = ArrayBuilder<LspDiagnostic>.GetInstance(out var result);
            foreach (var diagnostic in _diagnosticService.GetDiagnostics(document, includeSuppressedDiagnostics: false, cancellationToken))
                result.Add(DiagnosticUtilities.Convert(text, diagnostic));

            lock (_gate)
            {
                // Keep track of the diagnostics we reported here so that we can short-circuit producing diagnostics for
                // the same diagnostic set in the future.
                var resultId = _nextResultId++.ToString();
                _documentIdToLastResultId[(workspace, document.Id)] = resultId;
                return CreateReport(identifier, result, resultId);
            }
        }
    }
}
