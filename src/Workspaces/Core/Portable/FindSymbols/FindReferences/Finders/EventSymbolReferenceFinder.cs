// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders;

internal sealed class EventSymbolReferenceFinder : AbstractMethodOrPropertyOrEventSymbolReferenceFinder<IEventSymbol>
{
    protected override bool CanFind(IEventSymbol symbol)
        => true;

    protected sealed override async ValueTask<ImmutableArray<ISymbol>> DetermineCascadedSymbolsAsync(
        IEventSymbol symbol,
        Solution solution,
        FindReferencesSearchOptions options,
        CancellationToken cancellationToken)
    {
        var backingFields = symbol.ContainingType.GetMembers()
                                                 .OfType<IFieldSymbol>()
                                                 .Where(f => symbol.Equals(f.AssociatedSymbol))
                                                 .ToImmutableArray<ISymbol>();

        var associatedNamedTypes = symbol.ContainingType.GetTypeMembers()
                                                        .WhereAsArray(n => symbol.Equals(n.AssociatedSymbol))
                                                        .CastArray<ISymbol>();

        return [.. GetOtherPartsOfPartial(symbol), .. backingFields, .. associatedNamedTypes];
    }

    private static ImmutableArray<ISymbol> GetOtherPartsOfPartial(IEventSymbol symbol)
    {
        if (symbol.PartialDefinitionPart != null)
            return [symbol.PartialDefinitionPart];

        if (symbol.PartialImplementationPart != null)
            return [symbol.PartialImplementationPart];

        return [];
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
