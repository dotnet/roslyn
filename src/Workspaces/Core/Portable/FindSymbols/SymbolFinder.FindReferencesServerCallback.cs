// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    public static partial class SymbolFinder
    {
        /// <summary>
        /// Callback object we pass to the OOP server to hear about the result 
        /// of the FindReferencesEngine as it executes there.
        /// </summary>
        internal sealed class FindReferencesServerCallback : IRemoteSymbolFinderService.ICallback, IEqualityComparer<SerializableSymbolAndProjectId>
        {
            private readonly Solution _solution;
            private readonly IStreamingFindReferencesProgress _progress;
            private readonly CancellationToken _cancellationToken;

            private readonly object _gate = new();
            private readonly Dictionary<SerializableSymbolAndProjectId, ISymbol> _definitionMap;

            public FindReferencesServerCallback(
                Solution solution,
                IStreamingFindReferencesProgress progress,
                CancellationToken cancellationToken)
            {
                _solution = solution;
                _progress = progress;
                _cancellationToken = cancellationToken;
                _definitionMap = new Dictionary<SerializableSymbolAndProjectId, ISymbol>(this);
            }

            public ValueTask AddItemsAsync(int count) => _progress.ProgressTracker.AddItemsAsync(count);
            public ValueTask ItemCompletedAsync() => _progress.ProgressTracker.ItemCompletedAsync();

            public ValueTask OnStartedAsync() => _progress.OnStartedAsync();
            public ValueTask OnCompletedAsync() => _progress.OnCompletedAsync();

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

            public async ValueTask OnDefinitionFoundAsync(SerializableSymbolAndProjectId definition)
            {
                var symbol = await definition.TryRehydrateAsync(
                    _solution, _cancellationToken).ConfigureAwait(false);

                if (symbol == null)
                    return;

                lock (_gate)
                {
                    _definitionMap[definition] = symbol;
                }

                await _progress.OnDefinitionFoundAsync(symbol).ConfigureAwait(false);
            }

            public async ValueTask OnReferenceFoundAsync(
                SerializableSymbolAndProjectId definition, SerializableReferenceLocation reference)
            {
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
                    if (!_definitionMap.TryGetValue(definition, out symbol))
                        return;
                }

                var referenceLocation = await reference.RehydrateAsync(
                    _solution, _cancellationToken).ConfigureAwait(false);

                await _progress.OnReferenceFoundAsync(symbol, referenceLocation).ConfigureAwait(false);
            }

            bool IEqualityComparer<SerializableSymbolAndProjectId>.Equals(SerializableSymbolAndProjectId x, SerializableSymbolAndProjectId y)
                => y.SymbolKeyData.Equals(x.SymbolKeyData);

            int IEqualityComparer<SerializableSymbolAndProjectId>.GetHashCode(SerializableSymbolAndProjectId obj)
                => obj.SymbolKeyData.GetHashCode();

            public ValueTask OnLiteralReferenceFoundAsync(DocumentId documentId, TextSpan span)
                => throw ExceptionUtilities.Unreachable;
        }
    }
}
