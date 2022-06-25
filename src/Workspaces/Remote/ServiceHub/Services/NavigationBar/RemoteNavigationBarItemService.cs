// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.NavigationBar;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

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
            Checksum solutionChecksum, DocumentId documentId, bool supportsCodeGeneration, bool forceFrozenPartialSemanticsForCrossProcessOperations, CancellationToken cancellationToken)
        {
            return RunServiceAsync(solutionChecksum, async solution =>
            {
                var document = await solution.GetDocumentAsync(documentId, includeSourceGenerated: true, cancellationToken).ConfigureAwait(false);
                Contract.ThrowIfNull(document);

                if (forceFrozenPartialSemanticsForCrossProcessOperations)
                {
                    // Frozen partial semantics is not automatically passed to OOP, so enable it explicitly when desired
                    document = document.WithFrozenPartialSemantics(cancellationToken);
                }

                var navigationBarService = document.GetRequiredLanguageService<INavigationBarItemService>();
                var result = await navigationBarService.GetItemsAsync(document, supportsCodeGeneration, forceFrozenPartialSemanticsForCrossProcessOperations, cancellationToken).ConfigureAwait(false);

                return SerializableNavigationBarItem.Dehydrate(result);
            }, cancellationToken);
        }
    }
}
