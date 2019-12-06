// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.DocumentHighlighting;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LanguageServices.LiveShare.Protocol;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client.Highlights
{
    internal class RoslynDocumentHighlightsService : IDocumentHighlightsService
    {
        private readonly AbstractLspClientServiceFactory _roslynLspClientServiceFactory;

        public RoslynDocumentHighlightsService(AbstractLspClientServiceFactory roslynLspClientServiceFactory)
        {
            _roslynLspClientServiceFactory = roslynLspClientServiceFactory ?? throw new ArgumentNullException(nameof(roslynLspClientServiceFactory));
        }

        public async Task<ImmutableArray<DocumentHighlights>> GetDocumentHighlightsAsync(Document document, int position, IImmutableSet<Document> documentsToSearch, CancellationToken cancellationToken)
        {
            var lspClient = _roslynLspClientServiceFactory.ActiveLanguageServerClient;
            if (lspClient == null)
            {
                return ImmutableArray<DocumentHighlights>.Empty;
            }

            var text = await document.GetTextAsync().ConfigureAwait(false);
            var textDocumentPositionParams = ProtocolConversions.PositionToTextDocumentPositionParams(position, text, document);

            var highlights = await lspClient.RequestAsync(Methods.TextDocumentDocumentHighlight.ToLSRequest(), textDocumentPositionParams, cancellationToken).ConfigureAwait(false);
            if (highlights == null)
            {
                return ImmutableArray<DocumentHighlights>.Empty;
            }

            var highlightSpans = highlights.Select(highlight => new HighlightSpan(ProtocolConversions.RangeToTextSpan(highlight.Range, text), ToHighlightSpanKind(highlight.Kind)));
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
