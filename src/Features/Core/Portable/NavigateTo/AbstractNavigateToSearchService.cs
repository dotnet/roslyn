// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
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

    private static async Task PerformSearchAsync(
        Func<Action<RoslynNavigateToItem>, Task> searchAsync,
        Func<ImmutableArray<RoslynNavigateToItem>, Task> onItemsFound,
        CancellationToken cancellationToken)
    {
        var channel = Channel.CreateUnbounded<RoslynNavigateToItem>(s_channelOptions);

        await Task.WhenAll(
            FindAllItemsAndWriteToChannelAsync(channel.Writer, searchAsync),
            ReadItemsFromChannelAndReportToCallbackAsync(channel.Reader, onItemsFound, cancellationToken)).ConfigureAwait(false);

        return;

        static async Task ReadItemsFromChannelAndReportToCallbackAsync(
            ChannelReader<RoslynNavigateToItem> channelReader,
            Func<ImmutableArray<RoslynNavigateToItem>, Task> onItemsFound,
            CancellationToken cancellationToken)
        {
            await Task.Yield().ConfigureAwait(false);
            using var _ = ArrayBuilder<RoslynNavigateToItem>.GetInstance(out var items);

            while (await channelReader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            {
                // Grab as many items as we can from the channel at once and report in a batch.
                while (channelReader.TryRead(out var item))
                    items.Add(item);

                await onItemsFound(items.ToImmutableAndClear()).ConfigureAwait(false);
            }
        }

        static async Task FindAllItemsAndWriteToChannelAsync(
           ChannelWriter<RoslynNavigateToItem> channelWriter,
           Func<Action<RoslynNavigateToItem>, Task> findWorker)
        {
            Exception? exception = null;
            try
            {
                await Task.Yield().ConfigureAwait(false);
                await findWorker(item => channelWriter.TryWrite(item)).ConfigureAwait(false);
            }
            catch (Exception ex) when ((exception = ex) == null)
            {
                throw ExceptionUtilities.Unreachable();
            }
            finally
            {
                // No matter what path we take (exceptional or non-exceptional), always complete the channel so the
                // writing task knows it's done.
                channelWriter.TryComplete(exception);
            }
        }
    }

#pragma warning disable CA1068 // CancellationToken parameters must come last
    private static async Task ParallelForEachAsync<TSource>(
#pragma warning restore CA1068 // CancellationToken parameters must come last
        IEnumerable<TSource> source,
        CancellationToken cancellationToken,
        Func<TSource, CancellationToken, ValueTask> body)
    {
#if NET
        await Parallel.ForEachAsync(source, cancellationToken, body).ConfigureAwait(false);
#else
        using var _ = ArrayBuilder<Task>.GetInstance(out var tasks);

        foreach (var item in source)
        {
            tasks.Add(Task.Run(async () =>
            {
                await body(item, cancellationToken).ConfigureAwait(false);
            }, cancellationToken));
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
#endif
    }
}
