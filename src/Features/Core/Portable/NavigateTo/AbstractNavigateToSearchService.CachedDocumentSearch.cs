// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PatternMatching;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Storage;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.NavigateTo
{
    using CachedIndexMap = ConcurrentDictionary<(IChecksummedPersistentStorageService service, DocumentKey documentKey, StringTable stringTable), AsyncLazy<TopLevelSyntaxTreeIndex?>>;

    internal abstract partial class AbstractNavigateToSearchService
    {
        /// <summary>
        /// Cached map from document key to the (potentially stale) syntax tree index for it we use prior to the 
        /// full solution becoming available.  Once the full solution is available, this will be dropped
        /// (set to <see langword="null"/>) to release all cached data.
        /// </summary>
        private static CachedIndexMap? s_cachedIndexMap = new();

        /// <summary>
        /// String table we use to dedupe common values while deserializing <see cref="SyntaxTreeIndex"/>s.  Once the 
        /// full solution is available, this will be dropped (set to <see langword="null"/>) to release all cached data.
        /// </summary>
        private static StringTable? s_stringTable = new();

        private static void ClearCachedData()
        {
            // Volatiles are technically not necessary due to automatic fencing of reference-type writes.  However,
            // i prefer the explicitness here as we are reading and writing these fields from different threads.
            Volatile.Write(ref s_cachedIndexMap, null);
            Volatile.Write(ref s_stringTable, null);
        }

        private static bool ShouldSearchCachedDocuments(
            [NotNullWhen(true)] out CachedIndexMap? cachedIndexMap,
            [NotNullWhen(true)] out StringTable? stringTable)
        {
            cachedIndexMap = Volatile.Read(ref s_cachedIndexMap);
            stringTable = Volatile.Read(ref s_stringTable);
            return cachedIndexMap != null && stringTable != null;
        }

        public async Task SearchCachedDocumentsAsync(
            Project project,
            ImmutableArray<Document> priorityDocuments,
            string searchPattern,
            IImmutableSet<string> kinds,
            Func<INavigateToSearchResult, Task> onResultFound,
            CancellationToken cancellationToken)
        {
            var solution = project.Solution;
            var client = await RemoteHostClient.TryGetClientAsync(project, cancellationToken).ConfigureAwait(false);
            var onItemFound = GetOnItemFoundCallback(solution, onResultFound, cancellationToken);

            var documentKeys = project.Documents.SelectAsArray(d => DocumentKey.ToDocumentKey(d));
            var priorityDocumentKeys = priorityDocuments.SelectAsArray(d => DocumentKey.ToDocumentKey(d));
            if (client != null)
            {
                var callback = new NavigateToSearchServiceCallback(onItemFound);
                await client.TryInvokeAsync<IRemoteNavigateToSearchService>(
                    (service, callbackId, cancellationToken) =>
                        service.SearchCachedDocumentsAsync(documentKeys, priorityDocumentKeys, searchPattern, kinds.ToImmutableArray(), callbackId, cancellationToken),
                    callback, cancellationToken).ConfigureAwait(false);

                return;
            }

            var storageService = solution.Workspace.Services.GetPersistentStorageService();
            await SearchCachedDocumentsInCurrentProcessAsync(
                storageService, documentKeys, priorityDocumentKeys, searchPattern, kinds, onItemFound, cancellationToken).ConfigureAwait(false);
        }

        public static async Task SearchCachedDocumentsInCurrentProcessAsync(
            IChecksummedPersistentStorageService storageService,
            ImmutableArray<DocumentKey> documentKeys,
            ImmutableArray<DocumentKey> priorityDocumentKeys,
            string searchPattern,
            IImmutableSet<string> kinds,
            Func<RoslynNavigateToItem, Task> onItemFound,
            CancellationToken cancellationToken)
        {
            // Quick abort if OOP is now fully loaded.
            if (!ShouldSearchCachedDocuments(out _, out _))
                return;

            var highPriDocsSet = priorityDocumentKeys.ToSet();
            var lowPriDocs = documentKeys.WhereAsArray(d => !highPriDocsSet.Contains(d));

            // If the user created a dotted pattern then we'll grab the last part of the name
            var (patternName, patternContainer) = PatternMatcher.GetNameAndContainer(searchPattern);
            var declaredSymbolInfoKindsSet = new DeclaredSymbolInfoKindSet(kinds);

            await SearchCachedDocumentsInCurrentProcessAsync(
                storageService, patternName, patternContainer, declaredSymbolInfoKindsSet,
                onItemFound, priorityDocumentKeys, cancellationToken).ConfigureAwait(false);

            await SearchCachedDocumentsInCurrentProcessAsync(
                storageService, patternName, patternContainer, declaredSymbolInfoKindsSet,
                onItemFound, lowPriDocs, cancellationToken).ConfigureAwait(false);
        }

        private static async Task SearchCachedDocumentsInCurrentProcessAsync(
            IChecksummedPersistentStorageService storageService,
            string patternName,
            string patternContainer,
            DeclaredSymbolInfoKindSet kinds,
            Func<RoslynNavigateToItem, Task> onItemFound,
            ImmutableArray<DocumentKey> documentKeys,
            CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<Task>.GetInstance(out var tasks);

            foreach (var documentKey in documentKeys)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var index = await GetIndexAsync(storageService, documentKey, cancellationToken).ConfigureAwait(false);
                    if (index == null)
                        return;

                    await ProcessIndexAsync(
                        documentKey.Id, document: null, patternName, patternContainer, kinds, onItemFound, index, cancellationToken).ConfigureAwait(false);
                }, cancellationToken));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        private static Task<TopLevelSyntaxTreeIndex?> GetIndexAsync(
            IChecksummedPersistentStorageService storageService,
            DocumentKey documentKey,
            CancellationToken cancellationToken)
        {
            // Retrieve the string table we use to dedupe strings.  If we can't get it, that means the solution has 
            // fully loaded and we've switched over to normal navto lookup.
            if (!ShouldSearchCachedDocuments(out var cachedIndexMap, out var stringTable))
                return SpecializedTasks.Null<TopLevelSyntaxTreeIndex>();

            // Add the async lazy to compute the index for this document.  Or, return the existing cached one if already
            // present.  This ensures that subsequent searches that are run while the solution is still loading are fast
            // and avoid the cost of loading from the persistence service every time.
            //
            // Pass in null for the checksum as we want to search stale index values regardless if the documents don't
            // match on disk anymore.
            var asyncLazy = cachedIndexMap.GetOrAdd(
                (storageService, documentKey, stringTable),
                static t => new AsyncLazy<TopLevelSyntaxTreeIndex?>(
                    c => TopLevelSyntaxTreeIndex.LoadAsync(
                        t.service, t.documentKey, checksum: null, t.stringTable, c), cacheResult: true));
            return asyncLazy.GetValueAsync(cancellationToken);
        }
    }
}
