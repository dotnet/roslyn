// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.NavigationBar
{
    internal abstract class AbstractNavigationBarItemService : INavigationBarItemService
    {
        protected abstract Task<ImmutableArray<RoslynNavigationBarItem>> GetItemsInCurrentProcessAsync(Document document, bool supportsCodeGeneration, CancellationToken cancellationToken);

        public async Task<ImmutableArray<RoslynNavigationBarItem>> GetItemsAsync(Document document, bool supportsCodeGeneration, CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(document.Project, cancellationToken).ConfigureAwait(false);
            if (client != null)
            {
                var solution = document.Project.Solution;

                var result = await client.TryInvokeAsync<IRemoteNavigationBarItemService, ImmutableArray<SerializableNavigationBarItem>>(
                    solution,
                    (service, solutionInfo, cancellationToken) => service.GetItemsAsync(solutionInfo, document.Id, supportsCodeGeneration, cancellationToken),
                    cancellationToken).ConfigureAwait(false);

                return result.HasValue
                    ? result.Value.SelectAsArray(v => v.Rehydrate())
                    : ImmutableArray<RoslynNavigationBarItem>.Empty;
            }

            var items = await GetItemsInCurrentProcessAsync(document, supportsCodeGeneration, cancellationToken).ConfigureAwait(false);
            return items;
        }
    }
}
