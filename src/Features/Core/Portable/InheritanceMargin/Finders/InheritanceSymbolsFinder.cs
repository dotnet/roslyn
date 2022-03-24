// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;

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
            var visitedSet = new MetadataUnifyingSymbolHashSet();
            EnqueueAll(queue, initialSymbols);
            while (queue.Count > 0)
            {
                var currentSymbol = queue.Dequeue();
                var sourceSymbol = await SymbolFinder.FindSourceDefinitionAsync(currentSymbol, solution, cancellationToken).ConfigureAwait(false);
                var searchSymbol = sourceSymbol ?? currentSymbol;
                if (visitedSet.Add(searchSymbol))
                {
                    var symbols = await GetAssociatedSymbolsAsync(currentSymbol, solution, cancellationToken).ConfigureAwait(false);

                    foreach (var symbol in symbols)
                    {
                        var linkedSymbols = await SymbolFinder.FindLinkedSymbolsAsync(symbol, solution, cancellationToken).ConfigureAwait(false);
                        EnqueueAll(queue, linkedSymbols);
                        if (!builder.ContainsKey(symbol))
                            builder[symbol] = new SymbolGroup(linkedSymbols.SelectAsArray(s => s.OriginalDefinition));
                    }
                }
            }
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
