// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Extensibility.NavigationBar;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Remote
{
    internal sealed class RemoteNavigationBarItemService : BrokeredServiceBase, IRemoteNavigationBarItemService
    {
        internal sealed class Factory : FactoryBase<IRemoteNavigationBarItemService>
        {
            protected override IRemoteNavigationBarItemService CreateService(in ServiceConstructionArguments arguments)
                => new RemoteNavigationBarItemService(arguments);
        }

        public RemoteNavigationBarItemService(in ServiceConstructionArguments arguments)
            : base(arguments)
        {
        }

        public ValueTask<ImmutableArray<RoslynNavigationBarItem>> GetItemsAsync(
            PinnedSolutionInfo solutionInfo, DocumentId documentId, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async cancellationToken =>
            {
                var solution = await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);

                var document = solution.GetRequiredDocument(documentId);
                var navigationBarService = (AbstractNavigationBarItemService)document.GetRequiredLanguageService<INavigationBarItemService>();
                var result = await navigationBarService.GetItemsInCurrentProcessAsync(document, cancellationToken).ConfigureAwait(false);

                return result;
            }, cancellationToken);
        }
    }
}
