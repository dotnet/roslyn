// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor
{
    internal enum HighlightSpanKind
    {
        None,
        Definition,
        Reference,
        WrittenReference,
    }

    internal struct HighlightSpan
    {
        public TextSpan TextSpan { get; }
        public HighlightSpanKind Kind { get; }

        public HighlightSpan(TextSpan textSpan, HighlightSpanKind kind) : this()
        {
            this.TextSpan = textSpan;
            this.Kind = kind;
        }
    }

    internal struct DocumentHighlights
    {
        public Document Document { get; }
        public IList<HighlightSpan> HighlightSpans { get; }

        public DocumentHighlights(Document document, IList<HighlightSpan> highlightSpans) : this()
        {
            this.Document = document;
            this.HighlightSpans = highlightSpans;
        }
    }

    internal interface IDocumentHighlightsService : ILanguageService
    {
        Task<IEnumerable<DocumentHighlights>> GetDocumentHighlightsAsync(Document document, int position, IEnumerable<Document> documentsToSearch, CancellationToken cancellationToken);
    }
}
