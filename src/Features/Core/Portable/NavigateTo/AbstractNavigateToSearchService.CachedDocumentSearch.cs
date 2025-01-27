// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PatternMatching;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Storage;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.NavigateTo;

using CachedIndexMap = ConcurrentDictionary<(IChecksummedPersistentStorageService service, DocumentKey documentKey, StringTable stringTable), AsyncLazy<TopLevelSyntaxTreeIndex?>>;

internal abstract partial class AbstractNavigateToSearchService
{
    /// <summary>
    /// Cached map from document key to the (potentially stale) syntax tree index for it we use prior to the 
    /// full solution becoming available.  Once the full solution is available, this will be dropped
    /// (set to <see langword="null"/>) to release all cached data.
    /// </summary>
    private static CachedIndexMap? s_cachedIndexMap = [];

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
        Solution solution,
        ImmutableArray<Project> projects,
        ImmutableArray<Document> priorityDocuments,
        string searchPattern,
        IImmutableSet<string> kinds,
        Document? activeDocument,
        Func<ImmutableArray<INavigateToSearchResult>, Task> onResultsFound,
        Func<Task> onProjectCompleted,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return;

        Contract.ThrowIfTrue(projects.IsEmpty);
        Contract.ThrowIfTrue(projects.Select(p => p.Language).Distinct().Count() != 1);

        Debug.Assert(priorityDocuments.All(d => projects.Contains(d.Project)));

        var onItemsFound = GetOnItemsFoundCallback(solution, activeDocument, onResultsFound);

        var documentKeys = projects.SelectManyAsArray(p => p.Documents.Select(DocumentKey.ToDocumentKey));
        var priorityDocumentKeys = priorityDocuments.SelectAsArray(DocumentKey.ToDocumentKey);

        var client = await RemoteHostClient.TryGetClientAsync(solution.Services, cancellationToken).ConfigureAwait(false);
        if (client != null)
        {
            var callback = new NavigateToSearchServiceCallback(onItemsFound, onProjectCompleted, cancellationToken);
            await client.TryInvokeAsync<IRemoteNavigateToSearchService>(
                (service, callbackId, cancellationToken) =>
                    service.SearchCachedDocumentsAsync(documentKeys, priorityDocumentKeys, searchPattern, [.. kinds], callbackId, cancellationToken),
                callback, cancellationToken).ConfigureAwait(false);

            return;
        }

        var storageService = solution.Services.GetPersistentStorageService();
        await SearchCachedDocumentsInCurrentProcessAsync(
            storageService, documentKeys, priorityDocumentKeys, searchPattern, kinds, onItemsFound, onProjectCompleted, cancellationToken).ConfigureAwait(false);
    }

    public static async Task SearchCachedDocumentsInCurrentProcessAsync(
        IChecksummedPersistentStorageService storageService,
        ImmutableArray<DocumentKey> documentKeys,
        ImmutableArray<DocumentKey> priorityDocumentKeys,
        string searchPattern,
        IImmutableSet<string> kinds,
        Func<ImmutableArray<RoslynNavigateToItem>, VoidResult, CancellationToken, Task> onItemsFound,
        Func<Task> onProjectCompleted,
        CancellationToken cancellationToken)
    {
        // Quick abort if OOP is now fully loaded.
        if (!ShouldSearchCachedDocuments(out _, out _))
            return;

        var (patternName, patternContainer) = PatternMatcher.GetNameAndContainer(searchPattern);
        var declaredSymbolInfoKindsSet = new DeclaredSymbolInfoKindSet(kinds);

        // Process the documents by project group.  That way, when each project is done, we can
        // report that back to the host for progress.
        var groups = documentKeys.GroupBy(d => d.Project).ToImmutableArray();

        using var _1 = GetPooledHashSet(priorityDocumentKeys, out var priorityDocumentKeysSet);

        // Sort the groups into a high pri group (projects that contain a high-pri doc), and low pri groups (those that
        // don't), and process in that order.
        await ProducerConsumer<RoslynNavigateToItem>.RunParallelAsync(
            Prioritize(groups, g => g.Any(priorityDocumentKeysSet.Contains)),
            ProcessSingleProjectGroupAsync, onItemsFound, args: default, cancellationToken).ConfigureAwait(false);
        return;

        async Task ProcessSingleProjectGroupAsync(
            IGrouping<ProjectKey, DocumentKey> group,
            Action<RoslynNavigateToItem> onItemFound,
            VoidResult _,
            CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            var project = group.Key;

            // Break the project into high-pri docs and low pri docs, and process in that order.
            await RoslynParallel.ForEachAsync(
                Prioritize(group, priorityDocumentKeysSet.Contains),
                cancellationToken,
                async (documentKey, cancellationToken) =>
                {
                    var index = await GetIndexAsync(storageService, documentKey, cancellationToken).ConfigureAwait(false);
                    if (index == null)
                        return;

                    ProcessIndex(
                        documentKey, document: null, patternName, patternContainer, declaredSymbolInfoKindsSet,
                        index, linkedIndices: null, onItemFound, cancellationToken);
                }).ConfigureAwait(false);

            // done with project.  Let the host know.
            await onProjectCompleted().ConfigureAwait(false);
        }
    }

    private static Task<TopLevelSyntaxTreeIndex?> GetIndexAsync(
        IChecksummedPersistentStorageService storageService,
        DocumentKey documentKey,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return SpecializedTasks.Null<TopLevelSyntaxTreeIndex>();

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
            static t => AsyncLazy.Create(static (t, c) =>
                TopLevelSyntaxTreeIndex.LoadAsync(t.service, t.documentKey, checksum: null, t.stringTable, c),
                arg: t));
        return asyncLazy.GetValueAsync(cancellationToken);
    }
}
