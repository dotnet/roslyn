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

internal abstract partial class AbstractNavigateToSearchService
{
    /// <summary>
    /// Index of data used when we're doing our search of cached data.  This is used up to the point that OOP is fully
    /// loaded and we can transition to doing normal searches on up-to-date data.  The internal data for this is
    /// initialized the first time we do a cached search, and the entire object itself is dropped when we're done with
    /// it.
    /// </summary>
    private static CachedIndexMap? _cachedIndexMap_DoNotAccessDirectly = new();

    private static void ClearCachedData()
    {
        // Just drop the map entirely.  The GC will take care of the rest.
        _cachedIndexMap_DoNotAccessDirectly = null;
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

        var onItemsFound = GetOnItemsFoundCallback(solution, activeDocument, onResultsFound, cancellationToken);

        var documentKeys = projects.SelectManyAsArray(p => p.Documents.Select(DocumentKey.ToDocumentKey));
        var priorityDocumentKeys = priorityDocuments.SelectAsArray(DocumentKey.ToDocumentKey);

        var client = await RemoteHostClient.TryGetClientAsync(solution.Services, cancellationToken).ConfigureAwait(false);
        if (client != null)
        {
            var callback = new NavigateToSearchServiceCallback(onItemsFound, onProjectCompleted);
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
        Func<ImmutableArray<RoslynNavigateToItem>, Task> onItemsFound,
        Func<Task> onProjectCompleted,
        CancellationToken cancellationToken)
    {
        // Quick abort if OOP is now fully loaded.
        var cachedIndexMap = _cachedIndexMap_DoNotAccessDirectly;
        if (cachedIndexMap is null)
            return;

        // Process the documents by project group.  That way, when each project is done, we can
        // report that back to the host for progress.
        var groups = documentKeys.GroupBy(d => d.Project).ToImmutableArray();
        if (groups.Length == 0)
            return;

        await cachedIndexMap.InitializeAsync(storageService, groups[0].Key.Solution, cancellationToken).ConfigureAwait(false);

        var (patternName, patternContainer) = PatternMatcher.GetNameAndContainer(searchPattern);
        var declaredSymbolInfoKindsSet = new DeclaredSymbolInfoKindSet(kinds);

        using var _1 = GetPooledHashSet(priorityDocumentKeys, out var priorityDocumentKeysSet);

        // Sort the groups into a high pri group (projects that contain a high-pri doc), and low pri groups (those that
        // don't), and process in that order.
        await PerformParallelSearchAsync(
            Prioritize(groups, g => g.Any(priorityDocumentKeysSet.Contains)),
            ProcessSingleProjectGroupAsync, onItemsFound, cancellationToken).ConfigureAwait(false);
        return;

        async ValueTask ProcessSingleProjectGroupAsync(
            IGrouping<ProjectKey, DocumentKey> group,
            Action<RoslynNavigateToItem> onItemFound)
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
                    var index = await cachedIndexMap.GetIndexAsync(documentKey, cancellationToken).ConfigureAwait(false);
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
}
