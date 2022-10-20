// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PatternMatching;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Extensions;
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

        public async IAsyncEnumerable<INavigateToSearchResult> SearchCachedDocumentsAsync(
            Project project,
            ImmutableArray<Document> priorityDocuments,
            string searchPattern,
            IImmutableSet<string> kinds,
            Document? activeDocument,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var solution = project.Solution;

            var documentKeys = project.Documents.SelectAsArray(DocumentKey.ToDocumentKey);
            var priorityDocumentKeys = priorityDocuments.SelectAsArray(DocumentKey.ToDocumentKey);

            var client = await RemoteHostClient.TryGetClientAsync(project, cancellationToken).ConfigureAwait(false);
            if (client != null)
            {
                var result = client.TryInvokeStreamAsync<IRemoteNavigateToSearchService, RoslynNavigateToItem>(
                    (service, cancellationToken) =>
                        service.SearchCachedDocumentsAsync(documentKeys, priorityDocumentKeys, searchPattern, kinds.ToImmutableArray(), cancellationToken),
                    cancellationToken);

                await foreach (var item in ConvertItemsAsync(solution, activeDocument, result, cancellationToken).ConfigureAwait(false))
                    yield return item;
            }
            else
            {
                var storageService = solution.Services.GetPersistentStorageService();
                var result = SearchCachedDocumentsInCurrentProcessAsync(
                    storageService, documentKeys, priorityDocumentKeys, searchPattern, kinds, cancellationToken);

                await foreach (var item in ConvertItemsAsync(solution, activeDocument, result, cancellationToken).ConfigureAwait(false))
                    yield return item;
            }
        }

        public static async IAsyncEnumerable<RoslynNavigateToItem> SearchCachedDocumentsInCurrentProcessAsync(
            IChecksummedPersistentStorageService storageService,
            ImmutableArray<DocumentKey> documentKeys,
            ImmutableArray<DocumentKey> priorityDocumentKeys,
            string searchPattern,
            IImmutableSet<string> kinds,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            // Quick abort if OOP is now fully loaded.
            if (!ShouldSearchCachedDocuments(out _, out _))
                yield break;

            var highPriDocsSet = priorityDocumentKeys.ToSet();
            var lowPriDocs = documentKeys.WhereAsArray(d => !highPriDocsSet.Contains(d));

            // If the user created a dotted pattern then we'll grab the last part of the name
            var (patternName, patternContainer) = PatternMatcher.GetNameAndContainer(searchPattern);
            var declaredSymbolInfoKindsSet = new DeclaredSymbolInfoKindSet(kinds);

            await foreach (var item in SearchCachedDocumentsInCurrentProcessAsync(
                storageService, patternName, patternContainer, declaredSymbolInfoKindsSet,
                priorityDocumentKeys, cancellationToken).ConfigureAwait(false))
            {
                yield return item;
            }

            await foreach (var item in SearchCachedDocumentsInCurrentProcessAsync(
                storageService, patternName, patternContainer, declaredSymbolInfoKindsSet,
                lowPriDocs, cancellationToken).ConfigureAwait(false))
            {
                yield return item;
            }
        }

        private static IAsyncEnumerable<RoslynNavigateToItem> SearchCachedDocumentsInCurrentProcessAsync(
            IChecksummedPersistentStorageService storageService,
            string patternName,
            string? patternContainer,
            DeclaredSymbolInfoKindSet kinds,
            ImmutableArray<DocumentKey> documentKeys,
            CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<IAsyncEnumerable<RoslynNavigateToItem>>.GetInstance(out var builder);

            foreach (var documentKey in documentKeys)
                builder.Add(ProcessStaleIndexAsync(storageService, patternName, patternContainer, kinds, documentKey, cancellationToken));

            return builder.ToImmutable().MergeAsync(cancellationToken);
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
