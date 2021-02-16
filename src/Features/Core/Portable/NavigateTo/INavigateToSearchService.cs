// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.NavigateTo
{
    internal interface INavigateToSearchService : ILanguageService
    {
        IImmutableSet<string> KindsProvided { get; }
        bool CanFilter { get; }

        Task<ImmutableArray<INavigateToSearchResult>> SearchProjectAsync(Project project, ImmutableArray<Document> priorityDocuments, string searchPattern, IImmutableSet<string> kinds, CancellationToken cancellationToken);
        Task<ImmutableArray<INavigateToSearchResult>> SearchDocumentAsync(Document document, string searchPattern, IImmutableSet<string> kinds, CancellationToken cancellationToken);
    }

    [Obsolete("Use " + nameof(INavigateToSearchResult) + " instead", error: false)]
    internal interface INavigateToSeINavigateToSearchService_RemoveInterfaceAboveAndRenameThisAfterInternalsVisibleToUsersUpdatearchService : ILanguageService
    {
        IImmutableSet<string> KindsProvided { get; }
        bool CanFilter { get; }

        Task<ImmutableArray<INavigateToSearchResult>> SearchProjectAsync(Project project, ImmutableArray<Document> priorityDocuments, string searchPattern, IImmutableSet<string> kinds, CancellationToken cancellationToken);
        Task<ImmutableArray<INavigateToSearchResult>> SearchDocumentAsync(Document document, string searchPattern, IImmutableSet<string> kinds, CancellationToken cancellationToken);
    }
}
