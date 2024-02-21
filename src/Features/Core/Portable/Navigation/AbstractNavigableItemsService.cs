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

internal class AbstractNavigableItemsService : INavigableItemsService
{
    public async Task<ImmutableArray<INavigableItem>> GetNavigableItemsAsync(
        Document document, int position, CancellationToken cancellationToken)
    {
        var symbolService = document.GetRequiredLanguageService<IGoToDefinitionSymbolService>();
        var (symbol, project, _) = await symbolService.GetSymbolProjectAndBoundSpanAsync(document, position, cancellationToken).ConfigureAwait(false);

        var solution = project.Solution;
        symbol = await SymbolFinder.FindSourceDefinitionAsync(symbol, solution, cancellationToken).ConfigureAwait(false) ?? symbol;
        symbol = await GoToDefinitionFeatureHelpers.TryGetPreferredSymbolAsync(solution, symbol, cancellationToken).ConfigureAwait(false);

        // Try to compute source definitions from symbol.
        return symbol != null
            ? NavigableItemFactory.GetItemsFromPreferredSourceLocations(solution, symbol, displayTaggedParts: FindUsagesHelpers.GetDisplayParts(symbol), cancellationToken: cancellationToken)
            : ImmutableArray<INavigableItem>.Empty;
    }
}
