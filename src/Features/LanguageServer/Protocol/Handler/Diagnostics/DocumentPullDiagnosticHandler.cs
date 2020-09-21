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
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics
{
    using LspDiagnostic = Microsoft.VisualStudio.LanguageServer.Protocol.Diagnostic;

    [ExportLspMethod(MSLSPMethods.DocumentPullDiagnosticName), Shared]
    internal class DocumentPullDiagnosticHandler : AbstractRequestHandler<DocumentDiagnosticsParams, DiagnosticReport[]?>
    {
        private static readonly DiagnosticTag[] s_unnecessaryTags = new[] { DiagnosticTag.Unnecessary };

        private readonly IDiagnosticService _diagnosticService;

        /// <summary>
        /// Lock to product <see cref="_documentIdToLastResultId"/> and <see cref="_nextResultId"/>.
        /// </summary>
        private readonly object _gate = new object();

        /// <summary>
        /// Mapping of a document to the last result id we reported for it.
        /// </summary>
        private readonly Dictionary<(Workspace workspace, DocumentId documentId), string> _documentIdToLastResultId =
            new Dictionary<(Workspace workspace, DocumentId documentId), string>();

        /// <summary>
        /// The next available id to label results with.
        /// </summary>
        private long _nextResultId;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DocumentPullDiagnosticHandler(
            ILspSolutionProvider solutionProvider,
            IDiagnosticService diagnosticService) : base(solutionProvider)
        {
            _diagnosticService = diagnosticService;
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

        public override async Task<DiagnosticReport[]?> HandleRequestAsync(
            DocumentDiagnosticsParams request, RequestContext context, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(request.TextDocument, $"Got a DocumentPullDiagnostic request that did not specify a {nameof(request.TextDocument)}");

            var document = SolutionProvider.GetDocument(request.TextDocument, context.ClientName);
            if (document == null)
            {
                // Client is asking server about a document that no longer exists (i.e. was removed/deleted from the
                // workspace).  In that case we need to return an actual diagnostic report with `null` for the
                // diagnostics to let the client know to dump that file entirely.
                return new[] { new DiagnosticReport { ResultId = null, Diagnostics = null } };
            }

            var project = document.Project;
            var solution = project.Solution;
            var workspace = solution.Workspace;

            // If the client has already asked for diagnostics for this document, see if we have actually recorded any
            // differences, or if they should just use the same diagnostics as before.
            if (request.PreviousResultId != null)
            {
                lock (_gate)
                {
                    if (_documentIdToLastResultId.TryGetValue((workspace, document.Id), out var lastReportedResultId) &&
                        lastReportedResultId == request.PreviousResultId)
                    {
                        // Nothing changed between the last request and this one.  Report a null response to the client
                        // to know they don't need to do anything.
                        return null;
                    }
                }
            }

            // Being asked about this document for the first time.  Or being asked again and we have different diagnostics.

            var diagnosticKeys = _diagnosticService.GetDiagnosticsUpdatedEventArgs(workspace, project.Id, document.Id, cancellationToken).ToImmutableArray();

            using var _ = ArrayBuilder<LspDiagnostic>.GetInstance(out var result);

            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            foreach (var key in diagnosticKeys)
            {
                var id = key.Id;
                var documentId = key.DocumentId;

                // ignore diagnostics unrelated to an actual document.
                if (documentId == null)
                    continue;

                var diagnostics = _diagnosticService.GetDiagnostics(key.Workspace, key.ProjectId, key.DocumentId, key.Id, includeSuppressedDiagnostics: false, cancellationToken);
                if (diagnostics == null)
                    continue;

                foreach (var diagnostic in diagnostics)
                    result.AddIfNotNull(Convert(text, diagnostic));
            }

            string resultId;
            lock (_gate)
            {
                // Keep track of the diagnostics we reported here so that we can short-circuit producing diagnostics for
                // the same diagnostic set in the future.
                resultId = _nextResultId++.ToString();
                _documentIdToLastResultId[(workspace, document.Id)] = resultId;
            }

            return new[] { new DiagnosticReport { ResultId = resultId, Diagnostics = result.ToArray() } };
        }

        private static LspDiagnostic? Convert(SourceText text, DiagnosticData diagnosticData)
        {
            Contract.ThrowIfNull(diagnosticData.Message, $"Got a document diagnostic that did not have a {nameof(diagnosticData.Message)}");
            Contract.ThrowIfNull(diagnosticData.DataLocation, $"Got a document diagnostic that did not have a {nameof(diagnosticData.DataLocation)}");

            return new LspDiagnostic
            {
                Code = diagnosticData.Id,
                Message = diagnosticData.Message,
                Severity = ProtocolConversions.DiagnosticSeverityToLspDiagnositcSeverity(diagnosticData.Severity),
                Range = ProtocolConversions.LinePositionToRange(DiagnosticData.GetLinePositionSpan(diagnosticData.DataLocation, text, useMapped: true)),
                // Only the unnecessary diagnostic tag is currently supported via LSP.
                Tags = diagnosticData.CustomTags.Contains(WellKnownDiagnosticTags.Unnecessary)
                    ? s_unnecessaryTags
                    : Array.Empty<DiagnosticTag>()
            };
        }
    }
}
