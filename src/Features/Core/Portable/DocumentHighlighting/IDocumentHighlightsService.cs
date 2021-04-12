﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.DocumentHighlighting
{
    internal enum HighlightSpanKind
    {
        None,
        Definition,
        Reference,
        WrittenReference,
    }

    [DataContract]
    internal readonly struct HighlightSpan
    {
        [DataMember(Order = 0)]
        public TextSpan TextSpan { get; }

        [DataMember(Order = 1)]
        public HighlightSpanKind Kind { get; }

        public HighlightSpan(TextSpan textSpan, HighlightSpanKind kind) : this()
        {
            TextSpan = textSpan;
            Kind = kind;
        }
    }

    internal readonly struct DocumentHighlights
    {
        public Document Document { get; }
        public ImmutableArray<HighlightSpan> HighlightSpans { get; }

        public DocumentHighlights(Document document, ImmutableArray<HighlightSpan> highlightSpans)
        {
            Document = document;
            HighlightSpans = highlightSpans;
        }
    }

    /// <summary>
    /// Note: This is the new version of the language service and superceded the same named type
    /// in the EditorFeatures layer.
    /// </summary>
    internal interface IDocumentHighlightsService : ILanguageService
    {
        Task<ImmutableArray<DocumentHighlights>> GetDocumentHighlightsAsync(
            Document document, int position, IImmutableSet<Document> documentsToSearch, CancellationToken cancellationToken);
    }
}
