// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.NavigateTo
{
    [Obsolete("Use " + nameof(INavigateToSearchService_RemoveInterfaceAboveAndRenameThisAfterInternalsVisibleToUsersUpdate) + " instead.")]
    internal interface INavigateToSearchService : ILanguageService
    {
        Task<ImmutableArray<INavigateToSearchResult>> SearchProjectAsync(Project project, string searchPattern, CancellationToken cancellationToken);
        Task<ImmutableArray<INavigateToSearchResult>> SearchDocumentAsync(Document document, string searchPattern, CancellationToken cancellationToken);
    }

    // This will be renamed to replace INavigateToSearchService as part of https://github.com/dotnet/roslyn/issues/28343
    internal interface INavigateToSearchService_RemoveInterfaceAboveAndRenameThisAfterInternalsVisibleToUsersUpdate : ILanguageService
    {
        IImmutableSet<string> KindsProvided
        {
            get;
        }

        bool CanFilter
        {
            get;
        }

        Task<ImmutableArray<INavigateToSearchResult>> SearchProjectAsync(Project project, ImmutableArray<Document> priorityDocuments, string searchPattern, IImmutableSet<string> kinds, CancellationToken cancellationToken);
        Task<ImmutableArray<INavigateToSearchResult>> SearchDocumentAsync(Document document, string searchPattern, IImmutableSet<string> kinds, CancellationToken cancellationToken);
    }
}
