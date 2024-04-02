// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.GoToDefinition;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Navigation;

internal abstract class AbstractNavigableItemsService : INavigableItemsService
{
    public async Task<ImmutableArray<INavigableItem>> GetNavigableItemsAsync(
        Document document, int position, CancellationToken cancellationToken)
    {
        var symbolService = document.GetRequiredLanguageService<IGoToDefinitionSymbolService>();

        // First try with frozen partial semantics.  For the common case where no symbols referenced though skeleton
        // references are involved, this can be much faster.  If that fails, try again, this time allowing skeletons to
        // be built.
        var symbolAndSolution =
            await GetSymbolAsync(document.WithFrozenPartialSemantics(cancellationToken)).ConfigureAwait(false) ??
            await GetSymbolAsync(document).ConfigureAwait(false);

        if (symbolAndSolution is null)
            return [];

        var (symbol, solution) = symbolAndSolution.Value;

        // Try to compute source definitions from symbol.
        return NavigableItemFactory.GetItemsFromPreferredSourceLocations(solution, symbol, FindUsagesHelpers.GetDisplayParts(symbol), cancellationToken);

        async Task<(ISymbol symbol, Solution solution)?> GetSymbolAsync(Document document)
        {
            var (symbol, project, _) = await symbolService.GetSymbolProjectAndBoundSpanAsync(document, position, cancellationToken).ConfigureAwait(false);

            var solution = project.Solution;

            symbol = await SymbolFinder.FindSourceDefinitionAsync(symbol, solution, cancellationToken).ConfigureAwait(false) ?? symbol;
            symbol = await GoToDefinitionFeatureHelpers.TryGetPreferredSymbolAsync(solution, symbol, cancellationToken).ConfigureAwait(false);

            if (symbol is null or IErrorTypeSymbol)
                return null;

            return (symbol, solution);
        }
    }
}
