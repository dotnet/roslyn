// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.DocumentHighlighting;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Remote
{
    // root level service for all Roslyn services
    internal partial class CodeAnalysisService : IRemoteDocumentHighlights
    {
        public async Task<ImmutableArray<SerializableDocumentHighlights>> GetDocumentHighlightsAsync(
            DocumentId documentId, int position, DocumentId[] documentIdsToSearch)
        {
            var solution = await GetSolutionAsync().ConfigureAwait(false);
            var document = solution.GetDocument(documentId);
            var documentsToSearch = ImmutableHashSet.CreateRange(documentIdsToSearch.Select(solution.GetDocument));

            var service = document.GetLanguageService<IDocumentHighlightsService>();
            var result = await service.GetDocumentHighlightsAsync(
                document, position, documentsToSearch, CancellationToken).ConfigureAwait(false);

            return result.SelectAsArray(SerializableDocumentHighlights.Dehydrate);
        }
    }
}