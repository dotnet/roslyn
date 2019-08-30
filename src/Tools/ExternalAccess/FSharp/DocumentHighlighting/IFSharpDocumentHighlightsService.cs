// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.DocumentHighlighting
{
    internal enum FSharpHighlightSpanKind
    {
        None,
        Definition,
        Reference,
        WrittenReference,
    }

    internal readonly struct FSharpHighlightSpan
    {
        public TextSpan TextSpan { get; }
        public FSharpHighlightSpanKind Kind { get; }

        public FSharpHighlightSpan(TextSpan textSpan, FSharpHighlightSpanKind kind) : this()
        {
            this.TextSpan = textSpan;
            this.Kind = kind;
        }
    }

    internal readonly struct FSharpDocumentHighlights
    {
        public Document Document { get; }
        public ImmutableArray<FSharpHighlightSpan> HighlightSpans { get; }

        public FSharpDocumentHighlights(Document document, ImmutableArray<FSharpHighlightSpan> highlightSpans)
        {
            this.Document = document;
            this.HighlightSpans = highlightSpans;
        }
    }

    /// <summary>
    /// Note: This is the new version of the language service and superceded the same named type
    /// in the EditorFeatures layer.
    /// </summary>
    internal interface IFSharpDocumentHighlightsService
    {
        Task<ImmutableArray<FSharpDocumentHighlights>> GetDocumentHighlightsAsync(
            Document document, int position, IImmutableSet<Document> documentsToSearch, CancellationToken cancellationToken);
    }
}
