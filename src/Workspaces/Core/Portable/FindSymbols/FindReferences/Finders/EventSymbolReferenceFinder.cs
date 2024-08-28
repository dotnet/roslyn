// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders;

internal sealed class EventSymbolReferenceFinder : AbstractMethodOrPropertyOrEventSymbolReferenceFinder<IEventSymbol>
{
    protected override bool CanFind(IEventSymbol symbol)
        => true;

    // old change
    protected sealed override async ValueTask<ImmutableArray<ISymbol>> DetermineCascadedSymbolsAsync(
        IEventSymbol symbol,
        Solution solution,
        FindReferencesSearchOptions options,
        CancellationToken cancellationToken)
    {
        using var _ = ArrayBuilder<ISymbol>.GetInstance(out var symbols);

        await DiscoverImpliedSymbolsAsync(symbol, solution, symbols, cancellationToken).ConfigureAwait(false);

        var backingFields = symbol.ContainingType
            .GetMembers()
            .OfType<IFieldSymbol>()
            .Where(f => symbol.Equals(f.AssociatedSymbol));
        symbols.AddRange(backingFields);

        var associatedNamedTypes = symbol.ContainingType
            .GetTypeMembers()
            .Where(n => symbol.Equals(n.AssociatedSymbol));
        symbols.AddRange(associatedNamedTypes);

        return symbols.ToImmutable();
    }

    protected sealed override async Task DetermineDocumentsToSearchAsync<TData>(
        IEventSymbol symbol,
        HashSet<string>? globalAliases,
        Project project,
        IImmutableSet<Document>? documents,
        Action<Document, TData> processResult,
        TData processResultData,
        FindReferencesSearchOptions options,
        CancellationToken cancellationToken)
    {
        await FindDocumentsAsync(project, documents, processResult, processResultData, cancellationToken, symbol.Name).ConfigureAwait(false);
        await FindDocumentsWithGlobalSuppressMessageAttributeAsync(project, documents, processResult, processResultData, cancellationToken).ConfigureAwait(false);
    }

    protected sealed override void FindReferencesInDocument<TData>(
        IEventSymbol symbol,
        FindReferencesDocumentState state,
        Action<FinderLocation, TData> processResult,
        TData processResultData,
        FindReferencesSearchOptions options,
        CancellationToken cancellationToken)
    {
        FindReferencesInDocumentUsingSymbolName(symbol, state, processResult, processResultData, cancellationToken);
    }
}
