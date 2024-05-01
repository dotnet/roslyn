// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders;

internal sealed class FieldSymbolReferenceFinder : AbstractReferenceFinder<IFieldSymbol>
{
    protected override bool CanFind(IFieldSymbol symbol)
        => true;

    protected override ValueTask<ImmutableArray<ISymbol>> DetermineCascadedSymbolsAsync(
        IFieldSymbol symbol,
        Solution solution,
        FindReferencesSearchOptions options,
        CancellationToken cancellationToken)
    {
        return symbol.AssociatedSymbol != null
            ? new(ImmutableArray.Create(symbol.AssociatedSymbol))
            : new(ImmutableArray<ISymbol>.Empty);
    }

    protected override async Task DetermineDocumentsToSearchAsync<TData>(
        FindReferencesSearchEngine searchEngine,
        IFieldSymbol symbol,
        HashSet<string>? globalAliases,
        Project project,
        IImmutableSet<Document>? documents,
        Action<Document, TData> processResult,
        TData processResultData,
        CancellationToken cancellationToken)
    {
        await FindDocumentsAsync(searchEngine, project, documents, processResult, processResultData, cancellationToken, symbol.Name).ConfigureAwait(false);
        await FindDocumentsWithGlobalSuppressMessageAttributeAsync(searchEngine, project, documents, processResult, processResultData, cancellationToken).ConfigureAwait(false);
    }

    protected override ValueTask FindReferencesInDocumentAsync<TData>(
        IFieldSymbol symbol,
        FindReferencesDocumentState state,
        Action<FinderLocation, TData> processResult,
        TData processResultData,
        FindReferencesSearchOptions options,
        CancellationToken cancellationToken)
    {
        FindReferencesInDocumentUsingSymbolName(
            symbol, state, processResult, processResultData, cancellationToken);
        FindReferencesInDocumentInsideGlobalSuppressions(
            symbol, state, processResult, processResultData, cancellationToken);
        return ValueTaskFactory.CompletedTask;
    }
}
