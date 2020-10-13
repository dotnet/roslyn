// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
