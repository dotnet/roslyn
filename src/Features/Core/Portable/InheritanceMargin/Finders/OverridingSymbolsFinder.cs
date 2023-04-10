// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        /// <summary>
        /// Get all the overriding members for <param name="symbol"/> in topological order.
        /// </summary>
        protected override async Task<ImmutableArray<ISymbol>> GetAssociatedSymbolsAsync(ISymbol symbol, Solution solution, CancellationToken cancellationToken)
        {
            var overridingSymbols = await SymbolFinder.FindOverridesArrayAsync(symbol, solution, cancellationToken: cancellationToken).ConfigureAwait(false);

            // Consider each of the symbols as a vertice, and it would be pointed by its overridden member.
            // We need an 'IncomingSymbolMap' whose key is the vertice, and value is a set of the overridden member to perform topological sort.
            // e.g 
            // class Bar1 { public virtual void Sub(); }
            // class Bar2 : Bar1 { public overridden void Sub() { } }
            // class Bar3 : Bar1 { public overridden void Sub() { } }
            // The map would looks like
            // {
            //     "Bar1.Sub()" : [],
            //     "Bar2.Sub()" : ["Bar1.Sub"],
            //     "Bar3.Sub()" : ["Bar1.Sub"],
            // }
            using var _ = GetPooledHashSetDictionary(out var mapBuilder);
            foreach (var overridingSymbol in overridingSymbols)
            {
                var indegreeSymbols = s_symbolHashSetPool.Allocate();
                var overriddenSymbol = overridingSymbol.GetOverriddenMember();
                if (overriddenSymbol != null && overridingSymbols.Contains(overriddenSymbol))
                {
                    indegreeSymbols.Add(overriddenSymbol);
                }

                mapBuilder[overridingSymbol] = indegreeSymbols;
            }

            return TopologicalSortAsArray(overridingSymbols, mapBuilder);
        }

        public async Task<ImmutableArray<SymbolGroup>> GetOverridingSymbolsGroupAsync(ISymbol initialSymbol, Solution solution, CancellationToken cancellationToken)
        {
            using var _1 = GetPooledHashSetDictionary(out var builder);
            await GetSymbolGroupsAsync(initialSymbol, solution, builder, cancellationToken).ConfigureAwait(false);

            using var _2 = ArrayBuilder<SymbolGroup>.GetInstance(out var overridingSymbolGroupsBuilder);
            foreach (var (symbol, symbolSet) in builder)
            {
                if (symbol.Locations.Any(l => l.IsInSource))
                    overridingSymbolGroupsBuilder.Add(new SymbolGroup(symbolSet));
            }

            return overridingSymbolGroupsBuilder.ToImmutable();
        }
    }
}
