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
                    var associatedSymbols = await GetAssociatedSymbolsAsync(currentSymbol, solution, cancellationToken).ConfigureAwait(false);
                    foreach (var associatedSymbol in associatedSymbols)
                    {
                        visitedSet.Add(associatedSymbol);
                        var linkedSymbols = await SymbolFinder.FindLinkedSymbolsAsync(associatedSymbol, solution, cancellationToken).ConfigureAwait(false);
                        EnqueueAll(queue, linkedSymbols);

                        var originalSymbol = associatedSymbol.OriginalDefinition;
                        if (!builder.ContainsKey(originalSymbol))
                        {
                            var linkedGroupSymbols =
                                await linkedSymbols.SelectAsArrayAsync((s, cancellationToken) => FindOriginalSourceDefinitionInNeededAsync(s, solution, cancellationToken), cancellationToken).ConfigureAwait(false);
                            builder[originalSymbol] = new SymbolGroup(linkedSymbols);
                        }
                    }
                }
            }
        }

        private static async ValueTask<ISymbol> FindOriginalSourceDefinitionInNeededAsync(
            ISymbol initialSymbol, Solution solution, CancellationToken cancellationToken)
        {
            var symbol = await SymbolFinder.FindSourceDefinitionAsync(initialSymbol, solution, cancellationToken).ConfigureAwait(false) ?? initialSymbol;
            return symbol.OriginalDefinition;
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
