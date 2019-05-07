//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.DocumentHighlighting;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client
{
    internal class RoslynDocumentHighlightsService : IDocumentHighlightsService
    {
        private readonly RoslynLSPClientServiceFactory roslynLSPClientServiceFactory;

        public RoslynDocumentHighlightsService(RoslynLSPClientServiceFactory roslynLSPClientServiceFactory)
        {
            this.roslynLSPClientServiceFactory = roslynLSPClientServiceFactory ?? throw new ArgumentNullException(nameof(roslynLSPClientServiceFactory));
        }

        public async Task<ImmutableArray<DocumentHighlights>> GetDocumentHighlightsAsync(Document document, int position, IImmutableSet<Document> documentsToSearch, CancellationToken cancellationToken)
        {
            var lspClient = this.roslynLSPClientServiceFactory.ActiveLanguageServerClient;
            if (lspClient == null)
            {
                return ImmutableArray<DocumentHighlights>.Empty;
            }

            var text = await document.GetTextAsync().ConfigureAwait(false);
            var textDocumentPositionParams = document.GetTextDocumentPositionParams(text, position);

            var highlights = await lspClient.RequestAsync(Methods.TextDocumentDocumentHighlight, textDocumentPositionParams, cancellationToken).ConfigureAwait(false);
            if (highlights == null)
            {
                return ImmutableArray<DocumentHighlights>.Empty;
            }

            var highlightSpans = highlights.Select(highlight => new HighlightSpan(highlight.Range.ToTextSpan(text), ToHighlightSpanKind(highlight.Kind)));

            return ImmutableArray.Create(new DocumentHighlights(document, highlightSpans.ToImmutableArray()));
        }

        private HighlightSpanKind ToHighlightSpanKind(DocumentHighlightKind kind)
        {
            switch (kind)
            {
                case DocumentHighlightKind.Text:
                    return HighlightSpanKind.Definition;
                case DocumentHighlightKind.Read:
                    return HighlightSpanKind.Reference;
                case DocumentHighlightKind.Write:
                    return HighlightSpanKind.WrittenReference;
                default:
                    return HighlightSpanKind.None;
            }
        }
    }
}
