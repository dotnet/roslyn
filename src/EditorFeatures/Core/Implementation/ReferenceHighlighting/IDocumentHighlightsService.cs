// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor
{
    internal struct HighlightSpan
    {
        public TextSpan TextSpan { get; }
        public bool IsDefinition { get; }

        public HighlightSpan(TextSpan textSpan, bool isDefinition) : this()
        {
            this.TextSpan = textSpan;
            this.IsDefinition = isDefinition;
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
