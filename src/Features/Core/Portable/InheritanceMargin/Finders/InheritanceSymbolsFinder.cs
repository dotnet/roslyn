// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.InheritanceMargin.Finders
{
    internal abstract class InheritanceSymbolsFinder
    {
        /// <summary>
        /// Get the assoicated symbols for this finder for the given starting <param name="symbol"/>.
        /// If the return symbols are not topologically sorted, then this method might be called unnecessarily.
        /// The current strategy is:
        /// For all the down symbols searching, the results would be topologically sorted before return. Because searching downwards is quite expansive.
        /// For all the up symbols searching, the results would not be strictly topologically sorted. Because search upwards is less expansive, and 
        /// we want to avoid the cost to sort the results.
        /// </summary>
        protected abstract Task<ImmutableArray<ISymbol>> GetAssociatedSymbolsAsync(ISymbol symbol, Solution solution, CancellationToken cancellationToken);

        protected async Task GetSymbolGroupsAsync(
            ISymbol initialSymbol,
            Solution solution,
            IDictionary<ISymbol, SymbolGroup> builder,
            CancellationToken cancellationToken)
        {
            var queue = new Queue<ISymbol>();
            var initialSymbols = await SymbolFinder.FindLinkedSymbolsAsync(initialSymbol, solution, cancellationToken).ConfigureAwait(false);
            using var _ = PooledHashSet<ISymbol>.GetInstance(out var visitedSet);
            EnqueueAll(queue, initialSymbols);
            while (queue.Count > 0)
            {
                var currentSymbol = queue.Dequeue();
                if (visitedSet.Add(currentSymbol))
                {
                    // Note: If the assoicatedSymbols are not in topologic order. GetAssociatedSymbolsAsync might be called unnecessarily
                    // e.g.
                    // currentSymbol -> SubClass1 -> SubClass2 -> ... SubClassN (TFM1)
                    //                     ↓             ↓                ↓
                    //                  SubClass1 -> SubClass1 -> ... SubClassN (TFM2)
                    // If 'SubClass1'(TFM1) is returned first, then 'SubClass1'(TFM2) would be in the queue first,
                    // and in the next iteration, all SubClass for (TFM2) would be visited.
                    // But If 'SubClassN' (TFM1) is returned first, then 'GetAssociatedSymbolsAsync' would be called for
                    // all the SubClass for (TFM2)
                    var associatedSymbols = await GetAssociatedSymbolsAsync(currentSymbol, solution, cancellationToken).ConfigureAwait(false);
                    foreach (var associatedSymbol in associatedSymbols)
                    {
                        var originalAssociatedSymbol = await SymbolFinder.FindSourceDefinitionAsync(associatedSymbol.OriginalDefinition, solution, cancellationToken).ConfigureAwait(false)
                            ?? associatedSymbol.OriginalDefinition;
                        visitedSet.Add(originalAssociatedSymbol);
                        var linkedSymbols = await SymbolFinder.FindLinkedSymbolsAsync(originalAssociatedSymbol, solution, cancellationToken).ConfigureAwait(false);
                        EnqueueAll(queue, linkedSymbols);
                        if (!builder.ContainsKey(originalAssociatedSymbol) && InheritanceMarginServiceHelper.IsNavigableSymbol(originalAssociatedSymbol))
                        {
                            var linkedGroupSymbols = linkedSymbols.SelectAsArray(s => s.OriginalDefinition);
                            builder[originalAssociatedSymbol] = new SymbolGroup(linkedSymbols);
                        }
                    }
                }
            }
        }

        protected static ImmutableArray<ISymbol> TopologicalSortAsArray(
            ImmutableArray<ISymbol> symbols, ImmutableDictionary<ISymbol, HashSet<ISymbol>> indegreeSymbolsMap)
        {
            using var _ = ArrayBuilder<ISymbol>.GetInstance(out var builder);
            foreach (var sortedSymbol in symbols.TopologicalSort(symbol => indegreeSymbolsMap[symbol]))
            {
                builder.Add(sortedSymbol);
            }

            return builder.ToImmutable();
        }

        private static void EnqueueAll(Queue<ISymbol> queue, ImmutableArray<ISymbol> symbols)
        {
            foreach (var symbol in symbols)
            {
                queue.Enqueue(symbol);
            }
        }
    }
}
