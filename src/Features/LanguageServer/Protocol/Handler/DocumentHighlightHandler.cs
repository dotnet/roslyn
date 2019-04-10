// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.DocumentHighlighting;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer
{
    internal class DocumentHighlightsHandler
    {
        public static async Task<DocumentHighlight[]> GetDocumentHighlightsAsync(Solution solution, TextDocumentPositionParams request, CancellationToken cancellationToken)
        {
            var document = solution.GetDocumentFromURI(request.TextDocument.Uri);
            if (document == null)
            {
                return Array.Empty<DocumentHighlight>();
            }

            var documentHighlightService = document.Project.LanguageServices.GetService<IDocumentHighlightsService>();
            var position = await document.GetPositionFromLinePosition(ProtocolConversions.PositionToLinePosition(request.Position), cancellationToken).ConfigureAwait(false);

            var highlights = await documentHighlightService.GetDocumentHighlightsAsync(
                document,
                position,
                ImmutableHashSet.Create(document),
                cancellationToken).ConfigureAwait(false);

            if (!highlights.IsDefaultOrEmpty)
            {
                // LSP requests are only for a single document. So just get the highlights for the requested document.
                var highlightsForDocument = highlights.FirstOrDefault(h => h.Document.Id == document.Id);
                var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

                return highlightsForDocument.HighlightSpans.Select(h =>
                {
                    return new DocumentHighlight
                    {
                        Range = ProtocolConversions.TextSpanToRange(h.TextSpan, text),
                        Kind = ProtocolConversions.HighlightSpanKindToDocumentHighlightKind(h.Kind),
                    };
                }).ToArray();
            }

            return Array.Empty<DocumentHighlight>();
        }
    }
}
