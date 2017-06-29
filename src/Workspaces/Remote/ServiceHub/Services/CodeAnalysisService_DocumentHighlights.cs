// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.DocumentHighlighting;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Remote
{
    // root level service for all Roslyn services
    internal partial class CodeAnalysisService : IRemoteDocumentHighlights
    {
        public async Task<ImmutableArray<SerializableDocumentHighlights>> GetDocumentHighlightsAsync(
            DocumentId documentId, int position, DocumentId[] documentIdsToSearch, CancellationToken cancellationToken)
        {
            var solution = await GetSolutionAsync(cancellationToken).ConfigureAwait(false);
            var document = solution.GetDocument(documentId);
            var documentsToSearch = ImmutableHashSet.CreateRange(documentIdsToSearch.Select(solution.GetDocument));

            var service = document.GetLanguageService<IDocumentHighlightsService>();
            var result = await service.GetDocumentHighlightsAsync(
                document, position, documentsToSearch, cancellationToken).ConfigureAwait(false);

            return result.SelectAsArray(SerializableDocumentHighlights.Dehydrate);
        }
    }
}
