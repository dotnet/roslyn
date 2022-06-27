// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.DocumentHighlighting
{
    internal interface IRemoteDocumentHighlightsService
    {
        ValueTask<ImmutableArray<SerializableDocumentHighlights>> GetDocumentHighlightsAsync(
            PinnedSolutionInfo solutionInfo, DocumentId documentId, int position, ImmutableArray<DocumentId> documentIdsToSearch, HighlightingOptions options, CancellationToken cancellationToken);
    }

    [DataContract]
    internal readonly struct SerializableDocumentHighlights
    {
        [DataMember(Order = 0)]
        public readonly DocumentId DocumentId;

        [DataMember(Order = 1)]
        public readonly ImmutableArray<HighlightSpan> HighlightSpans;

        public SerializableDocumentHighlights(DocumentId documentId, ImmutableArray<HighlightSpan> highlightSpans)
        {
            DocumentId = documentId;
            HighlightSpans = highlightSpans;
        }

        public async ValueTask<DocumentHighlights> RehydrateAsync(Solution solution)
            => new(await solution.GetRequiredDocumentAsync(DocumentId, includeSourceGenerated: true).ConfigureAwait(false), HighlightSpans);

        public static SerializableDocumentHighlights Dehydrate(DocumentHighlights highlights)
            => new(highlights.Document.Id, highlights.HighlightSpans);
    }
}
