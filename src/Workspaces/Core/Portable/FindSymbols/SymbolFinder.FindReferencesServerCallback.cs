// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    public static partial class SymbolFinder
    {
        /// <summary>
        /// Callback object we pass to the OOP server to hear about the result 
        /// of the FindReferencesEngine as it executes there.
        /// </summary>
        internal sealed class FindReferencesServerCallback
        {
            private readonly Solution _solution;
            private readonly IStreamingFindReferencesProgress _progress;
            private readonly CancellationToken _cancellationToken;

            private readonly object _gate = new();
            private readonly Dictionary<SerializableSymbolGroup, SymbolGroup> _groupMap = new();
            private readonly Dictionary<SerializableSymbolAndProjectId, ISymbol> _definitionMap = new();

            public FindReferencesServerCallback(
                Solution solution,
                IStreamingFindReferencesProgress progress,
                CancellationToken cancellationToken)
            {
                _solution = solution;
                _progress = progress;
                _cancellationToken = cancellationToken;
            }

            public ValueTask AddItemsAsync(int count)
                => _progress.ProgressTracker.AddItemsAsync(count);

            public ValueTask ItemCompletedAsync()
                => _progress.ProgressTracker.ItemCompletedAsync();

            public ValueTask OnStartedAsync()
                => _progress.OnStartedAsync();

            public ValueTask OnCompletedAsync()
                => _progress.OnCompletedAsync();

            public ValueTask OnFindInDocumentStartedAsync(DocumentId documentId)
            {
                var document = _solution.GetDocument(documentId);
                return _progress.OnFindInDocumentStartedAsync(document);
            }

            public ValueTask OnFindInDocumentCompletedAsync(DocumentId documentId)
            {
                var document = _solution.GetDocument(documentId);
                return _progress.OnFindInDocumentCompletedAsync(document);
            }

            public async ValueTask OnDefinitionFoundAsync(SerializableSymbolGroup dehydrated)
            {
                Contract.ThrowIfTrue(dehydrated.Symbols.Count == 0);

                using var _ = PooledDictionary<SerializableSymbolAndProjectId, ISymbol>.GetInstance(out var map);

                foreach (var symbolAndProjectId in dehydrated.Symbols)
                {
                    var symbol = await symbolAndProjectId.TryRehydrateAsync(_solution, _cancellationToken).ConfigureAwait(false);
                    if (symbol == null)
                        return;

                    map[symbolAndProjectId] = symbol;
                }

                var symbolGroup = new SymbolGroup(map.Values.ToImmutableArray());
                lock (_gate)
                {
                    _groupMap[dehydrated] = symbolGroup;
                    foreach (var pair in map)
                        _definitionMap[pair.Key] = pair.Value;
                }

                await _progress.OnDefinitionFoundAsync(symbolGroup).ConfigureAwait(false);
            }

            public async ValueTask OnReferenceFoundAsync(
                SerializableSymbolGroup serializableSymbolGroup,
                SerializableSymbolAndProjectId serializableSymbol,
                SerializableReferenceLocation reference)
            {
                SymbolGroup symbolGroup;
                ISymbol symbol;
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
                        return;
                    }
                }

                var referenceLocation = await reference.RehydrateAsync(
                    _solution, _cancellationToken).ConfigureAwait(false);

                await _progress.OnReferenceFoundAsync(symbolGroup, symbol, referenceLocation).ConfigureAwait(false);
            }
        }
    }
}
