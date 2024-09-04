// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.NavigationBar;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote;

internal sealed class RemoteNavigationBarItemService(in BrokeredServiceBase.ServiceConstructionArguments arguments)
    : BrokeredServiceBase(arguments), IRemoteNavigationBarItemService
{
    internal sealed class Factory : FactoryBase<IRemoteNavigationBarItemService>
    {
        protected override IRemoteNavigationBarItemService CreateService(in ServiceConstructionArguments arguments)
            => new RemoteNavigationBarItemService(arguments);
    }

    public ValueTask<ImmutableArray<SerializableNavigationBarItem>> GetItemsAsync(
        Checksum solutionChecksum, DocumentId documentId, bool supportsCodeGeneration, bool frozenPartialSemantics, CancellationToken cancellationToken)
    {
        return RunServiceAsync(solutionChecksum, async solution =>
        {
            var document = await solution.GetDocumentAsync(documentId, includeSourceGenerated: true, cancellationToken).ConfigureAwait(false);
            Contract.ThrowIfNull(document);

            // Frozen partial semantics is not automatically passed to OOP, so enable it explicitly when desired
            if (frozenPartialSemantics)
                document = document.WithFrozenPartialSemantics(cancellationToken);

            var navigationBarService = document.GetRequiredLanguageService<INavigationBarItemService>();
            var result = await navigationBarService.GetItemsAsync(document, supportsCodeGeneration, frozenPartialSemantics, cancellationToken).ConfigureAwait(false);

            return SerializableNavigationBarItem.Dehydrate(result);
        }, cancellationToken);
    }
}
