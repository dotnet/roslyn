// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
