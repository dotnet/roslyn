// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Storage;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.NavigateTo;

internal abstract partial class AbstractNavigateToSearchService
{
    private sealed class CachedIndexMap
    {
        private readonly SemaphoreSlim _gate = new(initialCount: 1);

        /// <summary>
        /// Cached map from document key to the (potentially stale) syntax tree index for it we use prior to the full
        /// solution becoming available.
        /// </summary>
        private readonly ConcurrentDictionary<DocumentKey, AsyncLazy<TopLevelSyntaxTreeIndex?>> _map = new();

        /// <summary>
        /// String table we use to dedupe common values while deserializing <see cref="SyntaxTreeIndex"/>s.
        /// </summary>
        private readonly StringTable _stringTable = new();

        private IChecksummedPersistentStorage? _storage;

        public async ValueTask InitializeAsync(
            IChecksummedPersistentStorageService storageService, SolutionKey solutionKey, CancellationToken cancellationToken)
        {
            using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                if (_storage != null)
                    return;

                _storage = await storageService.GetStorageAsync(solutionKey, cancellationToken).ConfigureAwait(false);
            }
        }

        ~CachedIndexMap()
        {
            var storage = Interlocked.Exchange(ref _storage, null);
            storage?.Dispose();
        }

        public Task<TopLevelSyntaxTreeIndex?> GetIndexAsync(DocumentKey documentKey, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_storage);

            if (cancellationToken.IsCancellationRequested)
                return SpecializedTasks.Null<TopLevelSyntaxTreeIndex>();

            // Add the async lazy to compute the index for this document.  Or, return the existing cached one if already
            // present.  This ensures that subsequent searches that are run while the solution is still loading are fast
            // and avoid the cost of loading from the persistence service every time.
            //
            // Pass in null for the checksum as we want to search stale index values regardless if the documents don't
            // match on disk anymore.
            var asyncLazy = _map.GetOrAdd(
                documentKey,
                documentKey => AsyncLazy.Create(static (tuple, c) =>
                    TopLevelSyntaxTreeIndex.LoadAsync(tuple._storage, tuple.documentKey, checksum: null, tuple._stringTable, c),
                    arg: (_storage, _stringTable, documentKey)));
            return asyncLazy.GetValueAsync(cancellationToken);
        }
    }
}
