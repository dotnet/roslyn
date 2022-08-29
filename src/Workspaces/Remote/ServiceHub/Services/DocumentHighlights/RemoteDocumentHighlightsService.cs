// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
    internal sealed class RemoteDocumentHighlightsService : BrokeredServiceBase, IRemoteDocumentHighlightsService
    {
        internal sealed class Factory : FactoryBase<IRemoteDocumentHighlightsService>
        {
            protected override IRemoteDocumentHighlightsService CreateService(in ServiceConstructionArguments arguments)
                => new RemoteDocumentHighlightsService(arguments);
        }

        public RemoteDocumentHighlightsService(in ServiceConstructionArguments arguments)
            : base(arguments)
        {
        }

        public ValueTask<ImmutableArray<SerializableDocumentHighlights>> GetDocumentHighlightsAsync(
            Checksum solutionChecksum, DocumentId documentId, int position, ImmutableArray<DocumentId> documentIdsToSearch, HighlightingOptions options, CancellationToken cancellationToken)
        {
            // NOTE: In projection scenarios, we might get a set of documents to search
            // that are not all the same language and might not exist in the OOP process
            // (like the JS parts of a .cshtml file). Filter them out here.  This will
            // need to be revisited if we someday support FAR between these languages.
            return RunServiceAsync(solutionChecksum, async solution =>
            {
                var document = await solution.GetDocumentAsync(documentId, includeSourceGenerated: true, cancellationToken).ConfigureAwait(false);
                var documentsToSearch = await documentIdsToSearch.SelectAsArrayAsync(id => solution.GetDocumentAsync(id, includeSourceGenerated: true, cancellationToken)).ConfigureAwait(false);
                var documentsToSearchSet = ImmutableHashSet.CreateRange(documentsToSearch.WhereNotNull());

                var service = document.GetLanguageService<IDocumentHighlightsService>();
                var result = await service.GetDocumentHighlightsAsync(
                    document, position, documentsToSearchSet, options, cancellationToken).ConfigureAwait(false);

                return result.SelectAsArray(SerializableDocumentHighlights.Dehydrate);
            }, cancellationToken);
        }
    }
}
