// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.DocumentHighlighting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [Shared]
    [ExportLspMethod(Methods.TextDocumentDocumentHighlightName)]
    internal class DocumentHighlightsHandler : AbstractRequestHandler<TextDocumentPositionParams, DocumentHighlight[]>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DocumentHighlightsHandler(ILspSolutionProvider solutionProvider) : base(solutionProvider)
        {
        }

        public override async Task<DocumentHighlight[]> HandleRequestAsync(TextDocumentPositionParams request, RequestContext context, CancellationToken cancellationToken)
        {
            var document = SolutionProvider.GetDocument(request.TextDocument, context.ClientName);
            if (document == null)
            {
                return Array.Empty<DocumentHighlight>();
            }

            var documentHighlightService = document.Project.LanguageServices.GetRequiredService<IDocumentHighlightsService>();
            var position = await document.GetPositionFromLinePositionAsync(ProtocolConversions.PositionToLinePosition(request.Position), cancellationToken).ConfigureAwait(false);

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
