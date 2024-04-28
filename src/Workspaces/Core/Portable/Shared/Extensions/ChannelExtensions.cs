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

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal static class ChannelExtensions
{
    /// <summary>
    /// Version of <see cref="RunProducerConsumerImplAsync"/> when caller the prefers the results being pre-packaged into arrays
    /// to process.
    /// </summary>
    public static Task RunProducerConsumerAsync<TItem>(
        this Channel<TItem> channel,
        Func<Action<TItem>, Task> produceItemsAsync,
        Func<ImmutableArray<TItem>, Task> consumeItemsAsync,
        CancellationToken cancellationToken)
    {
        return RunProducerConsumerImplAsync(
            channel,
            produceItemsAsync,
            ConsumeItemsAsArrayAsync,
            cancellationToken);

        async Task ConsumeItemsAsArrayAsync(ChannelReader<TItem> reader)
        {
            using var _ = ArrayBuilder<TItem>.GetInstance(out var items);

            while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            {
                // Grab as many items as we can from the channel at once and report in a single array. Then wait for the
                // next set of items to be available.
                while (channel.Reader.TryRead(out var item))
                    items.Add(item);

                await consumeItemsAsync(items.ToImmutableAndClear()).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Version of <see cref="RunProducerConsumerImplAsync"/> when the caller prefers working with a stream of results.
    /// </summary>
    public static Task RunProducerConsumerAsync<TItem>(
        this Channel<TItem> channel,
        Func<Action<TItem>, Task> produceItemsAsync,
        Func<IAsyncEnumerable<TItem>, Task> consumeItemsAsync,
        CancellationToken cancellationToken)
    {
        return RunProducerConsumerImplAsync(
            channel,
            produceItemsAsync,
            ConsumeItemsAsStreamAsync,
            cancellationToken);

        Task ConsumeItemsAsStreamAsync(ChannelReader<TItem> reader)
            => consumeItemsAsync(reader.ReadAllAsync(cancellationToken));
    }

    /// <summary>
    /// Helper utility for the pattern of a pair of a production routine and consumption routine using a channel to
    /// coordinate data transfer.  The channel itself is provided by the caller, but manages the rules and behaviors
    /// around the routines.  Importantly, it handles backpressure, ensuring that if the consumption routine cannot keep
    /// up, that the production routine will be throttled.
    /// <para>
    /// <paramref name="produceItemsAsync"/> is the routine
    /// called to actually produce the items.  It will be passed an action that can be used to write items to the
    /// channel.  Note: the channel itself will have rules depending on if that writing can happen concurrently multiple
    /// write threads or just a single writer.  See <see cref="ChannelOptions.SingleWriter"/> for control of this when
    /// creating the channel.
    /// </para>
    /// <paramref name="consumeItemsAsync"/> is the routine called to consume the items.
    /// </summary>
    private static async Task RunProducerConsumerImplAsync<TItem>(
        this Channel<TItem> channel,
        Func<Action<TItem>, Task> produceItemsAsync,
        Func<ChannelReader<TItem>, Task> consumeItemsAsync,
        CancellationToken cancellationToken)
    {
        // When cancellation happens, attempt to close the channel.  That will unblock the task processing the items.
        // Capture-free version is only available on netcore unfortunately.
        using var _ = cancellationToken.Register(
#if NET
            static (obj, cancellationToken) => ((Channel<TItem>)obj!).Writer.TryComplete(new OperationCanceledException(cancellationToken)),
            state: channel);
#else
            () => channel.Writer.TryComplete(new OperationCanceledException(cancellationToken)));
#endif

        await Task.WhenAll(
            ProduceItemsAndWriteToChannelAsync(),
            ReadFromChannelAndConsumeItemsAsync()).ConfigureAwait(false);

        return;

        async Task ReadFromChannelAndConsumeItemsAsync()
        {
            await Task.Yield().ConfigureAwait(false);
            await consumeItemsAsync(channel.Reader).ConfigureAwait(false);
        }

        async Task ProduceItemsAndWriteToChannelAsync()
        {
            Exception? exception = null;
            try
            {
                await Task.Yield().ConfigureAwait(false);
                await produceItemsAsync(item => channel.Writer.TryWrite(item)).ConfigureAwait(false);
            }
            catch (Exception ex) when ((exception = ex) == null)
            {
                throw ExceptionUtilities.Unreachable();
            }
            finally
            {
                // No matter what path we take (exceptional or non-exceptional), always complete the channel so the
                // writing task knows it's done.
                channel.Writer.TryComplete(exception);
            }
        }
    }
}
