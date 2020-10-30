// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.NavigateTo;

namespace Microsoft.CodeAnalysis.Editor.Implementation.NavigateTo
{
    internal partial class NavigateToItemProvider
    {
        private partial class Searcher
        {
            private class WrappedNavigateToSearchService : INavigateToSearchService
            {
#pragma warning disable CS0618 // Type or member is obsolete
                private readonly INavigateToSeINavigateToSearchService_RemoveInterfaceAboveAndRenameThisAfterInternalsVisibleToUsersUpdatearchService _legacySearchService;

                public WrappedNavigateToSearchService(INavigateToSeINavigateToSearchService_RemoveInterfaceAboveAndRenameThisAfterInternalsVisibleToUsersUpdatearchService legacySearchService)
                {
                    _legacySearchService = legacySearchService;
                }
#pragma warning restore CS0618 // Type or member is obsolete

                public IImmutableSet<string> KindsProvided => _legacySearchService.KindsProvided;

                public bool CanFilter => _legacySearchService.CanFilter;

                public Task<ImmutableArray<INavigateToSearchResult>> SearchDocumentAsync(Document document, string searchPattern, IImmutableSet<string> kinds, CancellationToken cancellationToken)
                    => _legacySearchService.SearchDocumentAsync(document, searchPattern, kinds, cancellationToken);

                public Task<ImmutableArray<INavigateToSearchResult>> SearchProjectAsync(Project project, ImmutableArray<Document> priorityDocuments, string searchPattern, IImmutableSet<string> kinds, CancellationToken cancellationToken)
                    => _legacySearchService.SearchProjectAsync(project, priorityDocuments, searchPattern, kinds, cancellationToken);
            }
        }
    }
}
