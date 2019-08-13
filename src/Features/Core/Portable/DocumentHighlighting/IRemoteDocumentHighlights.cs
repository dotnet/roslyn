// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.DocumentHighlighting
{
    internal interface IRemoteDocumentHighlights
    {
        Task<IList<SerializableDocumentHighlights>> GetDocumentHighlightsAsync(
            DocumentId documentId, int position, DocumentId[] documentIdsToSearch, CancellationToken cancellationToken);
    }

    internal struct SerializableDocumentHighlights
    {
        public DocumentId DocumentId;
        public IList<HighlightSpan> HighlightSpans;

        public DocumentHighlights Rehydrate(Solution solution)
            => new DocumentHighlights(solution.GetDocument(DocumentId), HighlightSpans.ToImmutableArray());

        public static SerializableDocumentHighlights Dehydrate(DocumentHighlights highlights)
            => new SerializableDocumentHighlights
            {
                DocumentId = highlights.Document.Id,
                HighlightSpans = highlights.HighlightSpans
            };
    }
}
