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
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Utilities;

internal readonly record struct ProducerConsumerOptions
{
    public static readonly ProducerConsumerOptions SingleReaderOptions = new() { SingleReader = true };
    public static readonly ProducerConsumerOptions SingleReaderWriterOptions = new() { SingleReader = true, SingleWriter = true };

    /// <inheritdoc cref="ChannelOptions.SingleWriter"/>
    public bool SingleWriter { get; init; }

    /// <inheritdoc cref="ChannelOptions.SingleReader"/>
    public bool SingleReader { get; init; }
}

internal static class ProducerConsumer<TItem>
{
    /// <summary>
    /// Version of <see cref="RunImplAsync"/> when caller the prefers the results being pre-packaged into arrays to process.
    /// </summary>
    public static Task RunAsync<TArgs>(
        ProducerConsumerOptions options,
        Func<Action<TItem>, TArgs, Task> produceItems,
        Func<ImmutableArray<TItem>, TArgs, Task> consumeItems,
        TArgs args,
        CancellationToken cancellationToken)
    {
        return RunImplAsync(
            options,
            static (onItemFound, args) => args.produceItems(onItemFound, args.args),
            static (reader, args) => ConsumeItemsAsArrayAsync(reader, args.consumeItems, args.args, args.cancellationToken),
            (produceItems, consumeItems, args, cancellationToken),
            cancellationToken);

        static async Task ConsumeItemsAsArrayAsync(
            ChannelReader<TItem> reader,
            Func<ImmutableArray<TItem>, TArgs, Task> consumeItems,
            TArgs args,
            CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<TItem>.GetInstance(out var items);

            while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            {
                // Grab as many items as we can from the channel at once and report in a single array. Then wait for the
                // next set of items to be available.
                while (reader.TryRead(out var item))
                    items.Add(item);

                await consumeItems(items.ToImmutableAndClear(), args).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Version of <see cref="RunImplAsync"/> when the caller prefers working with a stream of results.
    /// </summary>
    public static Task RunAsync<TArgs>(
        ProducerConsumerOptions options,
        Func<Action<TItem>, TArgs, Task> produceItems,
        Func<IAsyncEnumerable<TItem>, TArgs, Task> consumeItems,
        TArgs args,
        CancellationToken cancellationToken)
    {
        return RunImplAsync(
            options,
            static (onItemFound, args) => args.produceItems(onItemFound, args.args),
            static (reader, args) => args.consumeItems(reader.ReadAllAsync(args.cancellationToken), args.args),
            (produceItems, consumeItems, args, cancellationToken),
            cancellationToken);
    }

    /// <summary>
    /// Helper utility for the pattern of a pair of a production routine and consumption routine using a channel to
    /// coordinate data transfer.  The provided <paramref name="options"/> are used to create a <see
    /// cref="Channel{T}"/>, which will then then manage the rules and behaviors around the routines. Importantly, the
    /// channel handles backpressure, ensuring that if the consumption routine cannot keep up, that the production
    /// routine will be throttled.
    /// <para>
    /// <paramref name="produceItems"/> is the routine called to actually produce the items.  It will be passed an
    /// action that can be used to write items to the channel.  Note: the channel itself will have rules depending on if
    /// that writing can happen concurrently multiple write threads or just a single writer.  See <see
    /// cref="ChannelOptions.SingleWriter"/> for control of this when creating the channel.
    /// </para>
    /// <paramref name="consumeItems"/> is the routine called to consume the items.  Similarly, reading can have just a
    /// single reader or multiple readers, depending on the value passed into <see cref="ChannelOptions.SingleReader"/>.
    /// </summary>
    private static async Task RunImplAsync<TArgs>(
        ProducerConsumerOptions options,
        Func<Action<TItem>, TArgs, Task> produceItems,
        Func<ChannelReader<TItem>, TArgs, Task> consumeItems,
        TArgs args,
        CancellationToken cancellationToken)
    {
        var channel = Channel.CreateUnbounded<TItem>(new()
        {
            SingleReader = options.SingleReader,
            SingleWriter = options.SingleWriter,
        });

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
            await consumeItems(channel.Reader, args).ConfigureAwait(false);
        }

        async Task ProduceItemsAndWriteToChannelAsync()
        {
            Exception? exception = null;
            try
            {
                await Task.Yield().ConfigureAwait(false);

                // It's ok to use TryWrite here.  TryWrite always succeeds unless the channel is completed. And the
                // channel is only ever completed by us (after produceItems completes or throws an exception) or if the
                // cancellationToken is triggered above in RunAsync. In that latter case, it's ok for writing to the
                // channel to do nothing as we no longer need to write out those assets to the pipe.
                await produceItems(item => channel.Writer.TryWrite(item), args).ConfigureAwait(false);
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
