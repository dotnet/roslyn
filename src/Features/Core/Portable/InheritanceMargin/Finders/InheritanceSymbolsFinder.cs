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
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.InheritanceMargin.Finders
{
    internal abstract partial class InheritanceSymbolsFinder
    {
        /// <summary>
        /// Get the assoicated symbols for this finder for the given starting <param name="symbol"/>.
        /// If the return symbols are not topologically sorted, then this method might be called unnecessarily.
        /// </summary>
        protected abstract Task<ImmutableArray<ISymbol>> GetAssociatedSymbolsAsync(ISymbol symbol, Solution solution, CancellationToken cancellationToken);

        protected async Task GetSymbolGroupsAsync(
            ISymbol initialSymbol,
            Solution solution,
            PooledDictionary<ISymbol, PooledHashSet<ISymbol>> builder,
            CancellationToken cancellationToken)
        {
            var queue = new Queue<ISymbol>();

            var initialSymbols = await SymbolFinder.FindLinkedSymbolsAsync(initialSymbol, solution, cancellationToken).ConfigureAwait(false);
            // Use a normal Hashset here to make sure it has visited all the linked symbols seperately
            // e.g.
            // currentSymbol -> SubClass1 (TFM1)
            //                      ↓ 
            //                  SubClass1' -> SubClass2' (TFM2)
            // We need to make sure SubClass1 (TFM1) and SubClass1' (TFM2) are visited seperately so that we could get to SubClass2 (TFM2).
            using var _ = PooledHashSet<ISymbol>.GetInstance(out var visitedSet);
            EnqueueAll(queue, initialSymbols);
            while (queue.Count > 0)
            {
                var currentSymbol = queue.Dequeue();
                if (visitedSet.Add(currentSymbol))
                {
                    // Note: If the assoicatedSymbols are not in topologic order. GetAssociatedSymbolsAsync might be called unnecessarily
                    // e.g.
                    // currentSymbol -> SubClass1 -> SubClass2 -> ... SubClassN
                    //                     ↓             ↓                ↓
                    //                  SubClass1' -> SubClass2' -> ... SubClassN' 
                    // If SubClass1' is returned first, then SubClass1'(in different TargetFramework) would be in the queue first,
                    // and in the next iteration, all SubClass for the different TargetFramework would be visited.
                    // However, we get SubClassN' first, then we will end up with calling N times GetAssociatedSymbolsAsync
                    var associatedSymbols = await GetAssociatedSymbolsAsync(currentSymbol, solution, cancellationToken).ConfigureAwait(false);
                    foreach (var associatedSymbol in associatedSymbols)
                    {
                        var originalAssociatedSymbol = await SymbolFinder.FindSourceDefinitionAsync(associatedSymbol.OriginalDefinition, solution, cancellationToken).ConfigureAwait(false)
                            ?? associatedSymbol.OriginalDefinition;

                        // Find and enqueue other linked symbols, we need to check their assoiciatedSymbols so we don't miss any symbols.
                        // e.g.
                        // Symbol0   ->  Symbol1  ->  Symbol2
                        //                 ↓           ↓
                        //               Symbol1' ->  Symbol2' -> Symbol3'
                        // For Symbol0, its assoicated symbols are Symbol1 and Symbol2. Symbol1 and Symbol1' are linked, Symbol2, and Symbol2' are linked.
                        // Here we enqueue Symbol1'and Symbol2' so we could find Symbol3'. We would not have duplicate call to 'GetAssociatedSymbolsAsync' on Symbol2' since all the assoicated symbols are topologically sorted.
                        var linkedSymbols = await SymbolFinder.FindLinkedSymbolsAsync(originalAssociatedSymbol, solution, cancellationToken).ConfigureAwait(false);
                        EnqueueAll(queue, linkedSymbols.Except(new[] { originalAssociatedSymbol }));
                        visitedSet.Add(originalAssociatedSymbol);

                        if (!builder.ContainsKey(originalAssociatedSymbol) && AbstractInheritanceMarginService.IsNavigableSymbol(originalAssociatedSymbol))
                        {
                            var linkedGroupSymbols = linkedSymbols.SelectAsArray(s => s.OriginalDefinition);
                            var symbolSet = s_symbolHashSetPool.Allocate();
                            symbolSet.AddRange(linkedGroupSymbols);
                            builder[originalAssociatedSymbol] = symbolSet;
                        }
                    }
                }
            }
        }

        protected static ImmutableArray<ISymbol> TopologicalSortAsArray(
            ImmutableArray<ISymbol> symbols, PooledDictionary<ISymbol, PooledHashSet<ISymbol>> incomingSymbolsMap)
        {
            using var _ = ArrayBuilder<ISymbol>.GetInstance(out var builder);
            foreach (var sortedSymbol in symbols.TopologicalSort(symbol => incomingSymbolsMap[symbol]))
            {
                builder.Add(sortedSymbol);
            }

            return builder.ToImmutable();
        }

        private static void EnqueueAll(Queue<ISymbol> queue, IEnumerable<ISymbol> symbols)
        {
            foreach (var symbol in symbols)
            {
                queue.Enqueue(symbol);
            }
        }
    }
}
