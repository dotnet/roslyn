// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.DocumentHighlighting;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [Shared]
    [ExportLspMethod(Methods.TextDocumentDocumentHighlightName)]
    internal class DocumentHighlightsHandler : IRequestHandler<TextDocumentPositionParams, DocumentHighlight[]>
    {
        public async Task<DocumentHighlight[]> HandleRequestAsync(Solution solution, TextDocumentPositionParams request,
            ClientCapabilities clientCapabilities, CancellationToken cancellationToken, bool keepThreadContext)
        {
            var document = solution.GetDocumentFromURI(request.TextDocument.Uri);
            if (document == null)
            {
                return Array.Empty<DocumentHighlight>();
            }

            var documentHighlightService = document.Project.LanguageServices.GetService<IDocumentHighlightsService>();
            var position = await document.GetPositionFromLinePositionAsync(ProtocolConversions.PositionToLinePosition(request.Position), cancellationToken).ConfigureAwait(keepThreadContext);

            var highlights = await documentHighlightService.GetDocumentHighlightsAsync(
                document,
                position,
                ImmutableHashSet.Create(document),
                cancellationToken).ConfigureAwait(keepThreadContext);

            if (!highlights.IsDefaultOrEmpty)
            {
                // LSP requests are only for a single document. So just get the highlights for the requested document.
                var highlightsForDocument = highlights.FirstOrDefault(h => h.Document.Id == document.Id);
                var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(keepThreadContext);

                return highlightsForDocument.HighlightSpans.Select(h => new DocumentHighlight
                {
                    Range = ProtocolConversions.TextSpanToRange(h.TextSpan, text),
                    Kind = ProtocolConversions.HighlightSpanKindToDocumentHighlightKind(h.Kind),
                }).ToArray();
            }

            return Array.Empty<DocumentHighlight>();
        }
    }
}
