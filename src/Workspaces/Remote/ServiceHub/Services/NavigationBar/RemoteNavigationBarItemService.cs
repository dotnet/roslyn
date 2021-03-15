﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.NavigationBar;
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

        public ValueTask<ImmutableArray<SerializableNavigationBarItem>> GetItemsAsync(
            PinnedSolutionInfo solutionInfo, DocumentId documentId, bool supportsCodeGeneration, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async cancellationToken =>
            {
                var solution = await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);

                var document = solution.GetRequiredDocument(documentId);
                var navigationBarService = document.GetRequiredLanguageService<INavigationBarItemService>();
                var result = await navigationBarService.GetItemsAsync(document, supportsCodeGeneration, cancellationToken).ConfigureAwait(false);

                return SerializableNavigationBarItem.Dehydrate(result);
            }, cancellationToken);
        }
    }
}
