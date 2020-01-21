// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.DocumentHighlighting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    // root level service for all Roslyn services
    internal partial class CodeAnalysisService : IRemoteDocumentHighlights
    {
        public Task<IList<SerializableDocumentHighlights>> GetDocumentHighlightsAsync(
            DocumentId documentId, int position, DocumentId[] documentIdsToSearch, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async () =>
            {
                // NOTE: In projection scenarios, we might get a set of documents to search
                // that are not all the same language and might not exist in the OOP process
                // (like the JS parts of a .cshtml file). Filter them out here.  This will
                // need to be revisited if we someday support FAR between these languages.
                var solution = await GetSolutionAsync(cancellationToken).ConfigureAwait(false);
                var document = solution.GetDocument(documentId);
                var documentsToSearch = ImmutableHashSet.CreateRange(
                    documentIdsToSearch.Select(solution.GetDocument).WhereNotNull());

                var service = document.GetLanguageService<IDocumentHighlightsService>();
                var result = await service.GetDocumentHighlightsAsync(
                    document, position, documentsToSearch, cancellationToken).ConfigureAwait(false);

                return (IList<SerializableDocumentHighlights>)result.SelectAsArray(SerializableDocumentHighlights.Dehydrate);
            }, cancellationToken);
        }
    }
}
