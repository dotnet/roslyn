// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders;

internal sealed class LocalSymbolReferenceFinder : AbstractMemberScopedReferenceFinder<ILocalSymbol>
{
    protected override bool TokensMatch(FindReferencesDocumentState state, SyntaxToken token, string name)
        => IdentifiersMatch(state.SyntaxFacts, name, token);

    protected override async ValueTask<ImmutableArray<ISymbol>> DetermineCascadedSymbolsAsync(ILocalSymbol symbol, Solution solution, FindReferencesSearchOptions options, CancellationToken cancellationToken)
    {
        using var _ = ArrayBuilder<ISymbol>.GetInstance(out var symbols);
        await DiscoverImpliedSymbolsAsync(symbol, solution, symbols, cancellationToken).ConfigureAwait(false);
        return symbols.ToImmutable();
    }
}
