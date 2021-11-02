// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PatternMatching;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Storage;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.NavigateTo
{
    using CachedIndexMap = ConcurrentDictionary<(IChecksummedPersistentStorageService service, DocumentKey documentKey, StringTable stringTable), AsyncLazy<SyntaxTreeIndex?>>;

    internal abstract partial class AbstractNavigateToSearchService
    {
        /// <summary>
        /// Cached map from document key to the (potentially stale) syntax tree index for it we use prior to the 
        /// full solution becoming available.  Once the full solution is available, this will be cleared to drop
        /// all cached data.
        /// </summary>
        private static CachedIndexMap? s_cachedIndexMap = new();

        /// <summary>
        /// String table we use to dedupe common values while deserializing <see cref="SyntaxTreeIndex"/>s.  Like
        /// <see cref="s_cachedIndexMap"/> it is dropped once the full solution is available.
        /// </summary>
        private static StringTable? s_stringTable = new();

        public static async Task SearchCachedDocumentsInCurrentProcessAsync(
            IChecksummedPersistentStorageService storageService,
            ImmutableArray<DocumentKey> documentKeys,
            ImmutableArray<DocumentKey> priorityDocumentKeys,
            string searchPattern,
            IImmutableSet<string> kinds,
            Func<RoslynNavigateToItem, Task> onItemFound,
            CancellationToken cancellationToken)
        {
            // Retrieve the string table we use to dedupe strings.  If we can't get it, that means the solution has 
            // fully loaded and we've switched over to normal navto lookup.
            var cachedIndexMap = Volatile.Read(ref s_cachedIndexMap);
            var stringTable = Volatile.Read(ref s_stringTable);
            if (cachedIndexMap == null || stringTable == null)
                return;

            var highPriDocsSet = priorityDocumentKeys.ToSet();
            var lowPriDocs = documentKeys.WhereAsArray(d => !highPriDocsSet.Contains(d));

            // If the user created a dotted pattern then we'll grab the last part of the name
            var (patternName, patternContainer) = PatternMatcher.GetNameAndContainer(searchPattern);
            var declaredSymbolInfoKindsSet = new DeclaredSymbolInfoKindSet(kinds);

            await SearchCachedDocumentsInCurrentProcessAsync(
                cachedIndexMap, stringTable, storageService, priorityDocumentKeys, patternName, patternContainer, declaredSymbolInfoKindsSet, onItemFound, cancellationToken).ConfigureAwait(false);

            await SearchCachedDocumentsInCurrentProcessAsync(
                cachedIndexMap, stringTable, storageService, lowPriDocs, patternName, patternContainer, declaredSymbolInfoKindsSet, onItemFound, cancellationToken).ConfigureAwait(false);
        }

        private static async Task SearchCachedDocumentsInCurrentProcessAsync(
            CachedIndexMap cachedIndexMap,
            StringTable stringTable,
            IChecksummedPersistentStorageService storageService,
            ImmutableArray<DocumentKey> documentKeys,
            string patternName,
            string patternContainer,
            DeclaredSymbolInfoKindSet kinds,
            Func<RoslynNavigateToItem, Task> onItemFound,
            CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<Task>.GetInstance(out var tasks);

            foreach (var documentKey in documentKeys)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var index = await GetIndexAsync(cachedIndexMap, stringTable, storageService, documentKey, cancellationToken).ConfigureAwait(false);
                    if (index == null)
                        return;

                    await ProcessIndexAsync(
                        documentKey.Id, document: null, patternName, patternContainer, kinds, onItemFound, index, cancellationToken).ConfigureAwait(false);
                }, cancellationToken));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        private static Task<SyntaxTreeIndex?> GetIndexAsync(
            CachedIndexMap cachedIndexMap,
            StringTable stringTable,
            IChecksummedPersistentStorageService storageService,
            DocumentKey documentKey,
            CancellationToken cancellationToken)
        {
            // Add the async lazy to compute the index for this document.  Or, return the existing cached one if already
            // present.  This ensures that subsequent searches that are run while the solution is still loading are fast
            // and avoid the cost of loading from the persistence service every time.
            //
            // Pass in null for the checksum as we want to search stale index values regardless if the documents don't
            // match on disk anymore.
            var asyncLazy = cachedIndexMap.GetOrAdd(
                (storageService, documentKey, stringTable),
                static t => new AsyncLazy<SyntaxTreeIndex?>(
                    c => SyntaxTreeIndex.LoadAsync(
                        t.service, t.documentKey, checksum: null, t.stringTable, c), cacheResult: true));
            return asyncLazy.GetValueAsync(cancellationToken);
        }
    }
}
