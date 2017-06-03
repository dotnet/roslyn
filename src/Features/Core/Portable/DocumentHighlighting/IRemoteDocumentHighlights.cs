// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.DocumentHighlighting
{
    internal interface IRemoteDocumentHighlights
    {
        Task<SerializableDocumentHighlights[]> GetDocumentHighlightsAsync(
            DocumentId documentId, int position, DocumentId[] documentIdsToSearch);
    }

    internal struct SerializableDocumentHighlights
    {
        public DocumentId DocumentId;
        public HighlightSpan[] HighlightSpans;

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
            => new DocumentHighlights(solution.GetDocument(DocumentId), HighlightSpans.ToImmutableArray());

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
                HighlightSpans = highlights.HighlightSpans.ToArray()
            };
    }
}