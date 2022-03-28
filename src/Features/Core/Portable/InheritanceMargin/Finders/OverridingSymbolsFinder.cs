// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.InheritanceMargin.Finders
{
    internal class OverridingSymbolsFinder : InheritanceSymbolsFinder
    {
        public static readonly OverridingSymbolsFinder Instance = new();

        protected override async Task<ImmutableArray<ISymbol>> GetAssociatedSymbolsAsync(ISymbol symbol, Solution solution, CancellationToken cancellationToken)
        {
            var overridingSymbols = await SymbolFinder.FindOverridesArrayAsync(symbol, solution, cancellationToken: cancellationToken).ConfigureAwait(false);
            var indegreeSymbolsMap = GetIndegreeSymbolMap(overridingSymbols);
            return TopologicalSortAsArray(overridingSymbols, indegreeSymbolsMap);
        }

        private static ImmutableDictionary<ISymbol, HashSet<ISymbol>> GetIndegreeSymbolMap(ImmutableArray<ISymbol> overriddingSymbols)
        {
            using var _ = PooledDictionary<ISymbol, HashSet<ISymbol>>.GetInstance(out var mapBuilder);
            foreach (var symbol in overriddingSymbols)
            {
                var indegreeSymbols = new HashSet<ISymbol>();
                var overriddenSymbol = symbol.GetOverriddenMember();
                if (overriddenSymbol != null && overriddingSymbols.Contains(overriddenSymbol))
                {
                    indegreeSymbols.Add(overriddenSymbol);
                }

                mapBuilder[symbol] = indegreeSymbols;
            }

            return mapBuilder.ToImmutableDictionary();
        }

        public async Task<ImmutableArray<SymbolGroup>> GetOverridingSymbolsGroupAsync(ISymbol initialSymbol, Solution solution, CancellationToken cancellationToken)
        {
            var builder = new Dictionary<ISymbol, SymbolGroup>(MetadataUnifyingEquivalenceComparer.Instance);
            await GetSymbolGroupsAsync(initialSymbol, solution, builder, cancellationToken).ConfigureAwait(false);

            using var _ = ArrayBuilder<SymbolGroup>.GetInstance(out var overridingSymbolGroupsBuilder);
            foreach (var (symbol, symbolGroup) in builder)
            {
                if (symbol.Locations.Any(l => l.IsInSource) && InheritanceMarginServiceHelper.IsNavigableSymbol(symbol))
                    overridingSymbolGroupsBuilder.Add(symbolGroup);
            }

            return overridingSymbolGroupsBuilder.ToImmutable();
        }
    }
}
