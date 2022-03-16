// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindSymbols.FindReferences;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.InheritanceMargin
{
    internal abstract class InheritanceMarginItemFinder
    {
        protected abstract Task<ImmutableArray<ISymbol>> GetUpSymbolsAsync(ISymbol symbol, Solution solution, CancellationToken cancellationToken);

        protected abstract Task<ImmutableArray<ISymbol>> GetDownSymbolsAsync(ISymbol symbol, Solution solution, CancellationToken cancellationToken);

        private async Task GetSymbolGroupsAsync(
            ISymbol initialSymbol,
            Solution solution,
            IDictionary<ISymbol, SymbolGroup> builder,
            bool getUpOrDownSymbols,
            CancellationToken cancellationToken)
        {
            var queue = new Queue<ISymbol>();
            var initialSymbols = await SymbolFinder.FindLinkedSymbolsAsync(initialSymbol, solution, cancellationToken).ConfigureAwait(false);
            var visitedSet = new MetadataUnifyingSymbolHashSet();
            EnqueueAll(queue, initialSymbols);
            while (queue.Count > 0)
            {
                var currentSymbol = queue.Dequeue();
                if (visitedSet.Add(currentSymbol))
                {
                    var symbols = getUpOrDownSymbols
                        ? await GetUpSymbolsAsync(currentSymbol, solution, cancellationToken).ConfigureAwait(false)
                        : await GetDownSymbolsAsync(currentSymbol, solution, cancellationToken).ConfigureAwait(false);

                    foreach (var symbol in symbols)
                    {
                        var linkedSymbols = await SymbolFinder.FindLinkedSymbolsAsync(symbol, solution, cancellationToken).ConfigureAwait(false);
                        EnqueueAll(queue, linkedSymbols);
                        if (!builder.ContainsKey(symbol))
                        {
                            builder[symbol] = new SymbolGroup(linkedSymbols);
                        }
                    }
                }
            }
        }

        protected Task GetDownSymbolGroupsAsync(
            ISymbol initialSymbol,
            Solution solution,
            Dictionary<ISymbol, SymbolGroup> builder,
            CancellationToken cancellationToken)
            => GetSymbolGroupsAsync(initialSymbol, solution, builder, getUpOrDownSymbols: false, cancellationToken);

        protected Task GetUpSymbolGroupsAsync(
            ISymbol initialSymbol,
            Solution solution,
            Dictionary<ISymbol, SymbolGroup> builder,
            CancellationToken cancellationToken)
            => GetSymbolGroupsAsync(initialSymbol, solution, builder, getUpOrDownSymbols: true, cancellationToken);

        private static void EnqueueAll(Queue<ISymbol> queue, ImmutableArray<ISymbol> symbols)
        {
            foreach (var symbol in symbols)
            {
                queue.Enqueue(symbol);
            }
        }
    }

    internal class BaseTypeAndInterfaceFinder : InheritanceMarginItemFinder
    {
        protected override Task<ImmutableArray<ISymbol>> GetUpSymbolsAsync(
            ISymbol symbol,
            Solution solution,
            CancellationToken cancellationToken)
            => Task.FromResult(BaseTypeFinder.FindBaseTypesAndInterfaces((INamedTypeSymbol)symbol).CastArray<ISymbol>());

        protected override Task<ImmutableArray<ISymbol>> GetDownSymbolsAsync(
            ISymbol symbol,
            Solution solution,
            CancellationToken cancellationToken) => throw ExceptionUtilities.Unreachable;

        public Task GetBaseTypeAndBaseInterfaceAsync(ISymbol symbol, Solution solution, CancellationToken cancellationToken)
        {
        }
    }
}
