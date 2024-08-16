// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols;

public static partial class SymbolFinder
{
    /// <summary>
    /// Callback object we pass to the OOP server to hear about the result 
    /// of the FindReferencesEngine as it executes there.
    /// </summary>
    internal sealed class FindReferencesServerCallback(
        Solution solution,
        IStreamingFindReferencesProgress progress)
    {
        private readonly object _gate = new();
        private readonly Dictionary<SerializableSymbolGroup, SymbolGroup> _groupMap = [];
        private readonly Dictionary<SerializableSymbolAndProjectId, ISymbol> _definitionMap = [];

        public ValueTask AddItemsAsync(int count, CancellationToken cancellationToken)
            => progress.ProgressTracker.AddItemsAsync(count, cancellationToken);

        public ValueTask ItemsCompletedAsync(int count, CancellationToken cancellationToken)
            => progress.ProgressTracker.ItemsCompletedAsync(count, cancellationToken);

        public ValueTask OnStartedAsync(CancellationToken cancellationToken)
            => progress.OnStartedAsync(cancellationToken);

        public ValueTask OnCompletedAsync(CancellationToken cancellationToken)
            => progress.OnCompletedAsync(cancellationToken);

        public async ValueTask OnDefinitionFoundAsync(SerializableSymbolGroup dehydrated, CancellationToken cancellationToken)
        {
            Contract.ThrowIfTrue(dehydrated.Symbols.Count == 0);

            using var _ = PooledDictionary<SerializableSymbolAndProjectId, ISymbol>.GetInstance(out var map);

            foreach (var symbolAndProjectId in dehydrated.Symbols)
            {
                var symbol = await symbolAndProjectId.TryRehydrateAsync(solution, cancellationToken).ConfigureAwait(false);
                if (symbol == null)
                    return;

                map[symbolAndProjectId] = symbol;
            }

            var symbolGroup = new SymbolGroup([.. map.Values]);
            lock (_gate)
            {
                _groupMap[dehydrated] = symbolGroup;
                foreach (var pair in map)
                    _definitionMap[pair.Key] = pair.Value;
            }

            await progress.OnDefinitionFoundAsync(symbolGroup, cancellationToken).ConfigureAwait(false);
        }

        public async ValueTask OnReferencesFoundAsync(
            ImmutableArray<(SerializableSymbolGroup serializableSymbolGroup, SerializableSymbolAndProjectId serializableSymbol, SerializableReferenceLocation reference)> references,
            CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<(SymbolGroup group, ISymbol symbol, ReferenceLocation location)>.GetInstance(references.Length, out var rehydrated);

            foreach (var (serializableSymbolGroup, serializableSymbol, reference) in references)
            {
                SymbolGroup? symbolGroup;
                ISymbol? symbol;
                lock (_gate)
                {
                    // The definition may not be in the map if we failed to map it over using TryRehydrateAsync in OnDefinitionFoundAsync.
                    // Just ignore this reference.  Note: while this is a degraded experience:
                    //
                    // 1. TryRehydrateAsync logs an NFE so we can track down while we're failing to roundtrip the
                    //    definition so we can track down that issue.
                    // 2. NFE'ing and failing to show a result, is much better than NFE'ing and then crashing
                    //    immediately afterwards.
                    if (!_groupMap.TryGetValue(serializableSymbolGroup, out symbolGroup) ||
                        !_definitionMap.TryGetValue(serializableSymbol, out symbol))
                    {
                        continue;
                    }
                }

                var referenceLocation = await reference.RehydrateAsync(
                    solution, cancellationToken).ConfigureAwait(false);
                rehydrated.Add((symbolGroup, symbol, referenceLocation));
            }

            await progress.OnReferencesFoundAsync(rehydrated.ToImmutableAndClear(), cancellationToken).ConfigureAwait(false);
        }
    }
}
