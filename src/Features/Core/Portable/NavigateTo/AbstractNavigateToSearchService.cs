// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.NavigateTo
{
    internal abstract partial class AbstractNavigateToSearchService : INavigateToSearchService_RemoveInterfaceAboveAndRenameThisAfterInternalsVisibleToUsersUpdate
    {
        public IImmutableSet<string> KindsProvided { get; } = ImmutableHashSet.Create(
            NavigateToItemKind.Class,
            NavigateToItemKind.Constant,
            NavigateToItemKind.Delegate,
            NavigateToItemKind.Enum,
            NavigateToItemKind.EnumItem,
            NavigateToItemKind.Event,
            NavigateToItemKind.Field,
            NavigateToItemKind.Interface,
            NavigateToItemKind.Method,
            NavigateToItemKind.Module,
            NavigateToItemKind.Property,
            NavigateToItemKind.Structure);

        public bool CanFilter => true;

        public async Task<ImmutableArray<INavigateToSearchResult>> SearchDocumentAsync(
            Document document, string searchPattern, IImmutableSet<string> kinds, CancellationToken cancellationToken)
        {
            var client = await TryGetRemoteHostClientAsync(document.Project, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                return await SearchDocumentInCurrentProcessAsync(
                    document, searchPattern, kinds, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                return await SearchDocumentInRemoteProcessAsync(
                    client, document, searchPattern, kinds, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task<ImmutableArray<INavigateToSearchResult>> SearchProjectAsync(
            Project project, ImmutableArray<Document> priorityDocuments, string searchPattern, IImmutableSet<string> kinds, CancellationToken cancellationToken)
        {
            var client = await TryGetRemoteHostClientAsync(project, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                return await SearchProjectInCurrentProcessAsync(
                    project, priorityDocuments, searchPattern, kinds, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                return await SearchProjectInRemoteProcessAsync(
                    client, project, priorityDocuments, searchPattern, kinds, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
