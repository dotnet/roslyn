// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.NavigateTo;

internal abstract partial class AbstractNavigateToSearchService : IAdvancedNavigateToSearchService
{
    public static readonly IImmutableSet<string> AllKinds = [
        NavigateToItemKind.Class,
        NavigateToItemKind.Constant,
        NavigateToItemKind.Delegate,
        NavigateToItemKind.Enum,
        NavigateToItemKind.EnumItem,
        NavigateToItemKind.Event,
        NavigateToItemKind.Field,
        NavigateToItemKind.Interface,
        NavigateToItemKind.Method,
        NavigateToItemKind.Module,
        NavigateToItemKind.Property,
        NavigateToItemKind.Structure];

    public IImmutableSet<string> KindsProvided { get; } = AllKinds;

    public bool CanFilter => true;

    private static Func<ImmutableArray<RoslynNavigateToItem>, Task> GetOnItemsFoundCallback(
        Solution solution, Document? activeDocument, Func<ImmutableArray<INavigateToSearchResult>, Task> onResultsFound, CancellationToken cancellationToken)
    {
        return async items =>
        {
            using var _ = ArrayBuilder<INavigateToSearchResult>.GetInstance(items.Length, out var results);

            foreach (var item in items)
            {
                var result = await item.TryCreateSearchResultAsync(solution, activeDocument, cancellationToken).ConfigureAwait(false);
                if (result != null)
                    results.Add(result);
            }

            if (results.Count > 0)
                await onResultsFound(results.ToImmutableAndClear()).ConfigureAwait(false);
        };
    }

    private static PooledDisposer<PooledHashSet<T>> GetPooledHashSet<T>(IEnumerable<T> items, out PooledHashSet<T> instance)
    {
        var disposer = PooledHashSet<T>.GetInstance(out instance);
        instance.AddRange(items);
        return disposer;
    }

    private static PooledDisposer<PooledHashSet<T>> GetPooledHashSet<T>(ImmutableArray<T> items, out PooledHashSet<T> instance)
    {
        var disposer = PooledHashSet<T>.GetInstance(out instance);
        instance.AddRange(items);
        return disposer;
    }

    private static IEnumerable<T> Prioritize<T>(IEnumerable<T> items, Func<T, bool> isPriority)
    {
        using var _ = ArrayBuilder<T>.GetInstance(out var normalItems);

        foreach (var item in items)
        {
            if (isPriority(item))
                yield return item;
            else
                normalItems.Add(item);
        }

        foreach (var item in normalItems)
            yield return item;
    }

    /// <summary>
    /// Main utility for searching across items in a solution.  The actual code to search the item should be provided in
    /// <paramref name="callback"/>.  Each item in <paramref name="items"/> will be processed using
    /// <code>Parallel.ForEachAsync</code>, allowing for parallel processing of the items, with a preference towards
    /// earlier items.
    /// </summary>
    private static Task PerformParallelSearchAsync<T>(
        IEnumerable<T> items,
        Func<T, Action<RoslynNavigateToItem>, ValueTask> callback,
        Func<ImmutableArray<RoslynNavigateToItem>, Task> onItemsFound,
        CancellationToken cancellationToken)
        => ProducerConsumer<RoslynNavigateToItem>.RunAsync(
            ProducerConsumerOptions.SingleReaderOptions,
            produceItems: static (onItemFound, args) => RoslynParallel.ForEachAsync(args.items, args.cancellationToken, (item, cancellationToken) => args.callback(item, onItemFound)),
            consumeItems: static (items, args) => args.onItemsFound(items),
            args: (items, callback, onItemsFound, cancellationToken),
            cancellationToken);
}
