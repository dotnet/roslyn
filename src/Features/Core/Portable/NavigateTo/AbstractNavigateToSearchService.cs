// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.NavigateTo
{
    internal abstract partial class AbstractNavigateToSearchService : INavigateToSearchService
    {
        public async Task<ImmutableArray<INavigateToSearchResult>> SearchDocumentAsync(
            Document document, string searchPattern, ISet<string> kinds, CancellationToken cancellationToken)
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
            Project project, string searchPattern, ISet<string> kinds, CancellationToken cancellationToken)
        {
            var client = await TryGetRemoteHostClientAsync(project, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                return await SearchProjectInCurrentProcessAsync(
                    project, searchPattern, kinds, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                return await SearchProjectInRemoteProcessAsync(
                    client, project, searchPattern, kinds, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
