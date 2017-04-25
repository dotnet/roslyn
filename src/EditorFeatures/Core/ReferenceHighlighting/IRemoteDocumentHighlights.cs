// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.ReferenceHighlighting
{
    internal interface IRemoteDocumentHighlights
    {
        Task<SerializableDocumentHighlights[]> GetDocumentHighlightsAsync(
            DocumentId documentId, int position, DocumentId[] documentIdsToSearch);
    }

    internal struct SerializableDocumentHighlights
    {
        public DocumentId DocumentId;
        public SerializableHighlightSpan[] HighlightSpans;

        public static ImmutableArray<DocumentHighlights> Rehydrate(SerializableDocumentHighlights[] array, Solution solution)
        {
            var result = ArrayBuilder<DocumentHighlights>.GetInstance(array.Length);
            foreach (var dehydrated in array)
            {
                result.Push(dehydrated.Rehydrate(solution));
            }

            return result.ToImmutableAndFree();
        }

        private DocumentHighlights Rehydrate(Solution solution)
            => new DocumentHighlights(solution.GetDocument(DocumentId), SerializableHighlightSpan.Rehydrate(HighlightSpans));

        public static SerializableDocumentHighlights[] Dehydrate(ImmutableArray<DocumentHighlights> array)
        {
            var result = new SerializableDocumentHighlights[array.Length];
            var index = 0;
            foreach (var highlights in array)
            {
                result[index] = Dehydrate(highlights);
                index++;
            }

            return result;
        }

        private static SerializableDocumentHighlights Dehydrate(DocumentHighlights highlights)
            => new SerializableDocumentHighlights
            {
                DocumentId = highlights.Document.Id,
                HighlightSpans = SerializableHighlightSpan.Dehydrate(highlights.HighlightSpans)
            };
    }

    internal struct SerializableHighlightSpan
    {
        public TextSpan TextSpan;
        public HighlightSpanKind Kind;

        internal static SerializableHighlightSpan[] Dehydrate(ImmutableArray<HighlightSpan> array)
        {
            var result = new SerializableHighlightSpan[array.Length];
            var index = 0;
            foreach (var span in array)
            {
                result[index] = Dehydrate(span);
                index++;
            }

            return result;
        }

        private static SerializableHighlightSpan Dehydrate(HighlightSpan span)
            => new SerializableHighlightSpan
            {
                Kind = span.Kind,
                TextSpan = span.TextSpan
            };

        internal static ImmutableArray<HighlightSpan> Rehydrate(SerializableHighlightSpan[] array)
        {
            var result = ArrayBuilder<HighlightSpan>.GetInstance(array.Length);
            foreach (var dehydrated in array)
            {
                result.Push(dehydrated.Rehydrate());
            }

            return result.ToImmutableAndFree();
        }

        private HighlightSpan Rehydrate()
            => new HighlightSpan(TextSpan, Kind);
    }
}