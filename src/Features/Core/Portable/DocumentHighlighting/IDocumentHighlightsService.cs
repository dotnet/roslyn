﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
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
         public ImmutableArray<HighlightSpan> HighlightSpans { get; }
 
         public DocumentHighlights(Document document, ImmutableArray<HighlightSpan> highlightSpans)
         {
             this.Document = document;
             this.HighlightSpans = highlightSpans;
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
