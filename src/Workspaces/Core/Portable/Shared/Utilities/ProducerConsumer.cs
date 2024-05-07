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
    /// <summary>
    /// Used when the consumeItems routine will only pull items on a single thread (never concurrently). produceItems
    /// can be called concurrently on many threads.
    /// </summary>
    public static readonly ProducerConsumerOptions SingleReaderOptions = new() { SingleReader = true };

    /// <summary>
    /// Used when the consumeItems routine will only pull items on a single thread (never concurrently). produceItems
    /// can be called on a single thread as well (never concurrently).
    /// </summary>
    public static readonly ProducerConsumerOptions SingleReaderWriterOptions = new() { SingleReader = true, SingleWriter = true };

    /// <inheritdoc cref="ChannelOptions.SingleWriter"/>
    public bool SingleWriter { get; init; }

    /// <inheritdoc cref="ChannelOptions.SingleReader"/>
    public bool SingleReader { get; init; }
}

internal static class ProducerConsumer<TItem>
{
    private static async Task<VoidResult> BatchReaderIntoArraysAsync<TArgs>(
        ChannelReader<TItem> reader,
        Func<ImmutableArray<TItem>, TArgs, CancellationToken, Task> consumeItems,
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

            await consumeItems(items.ToImmutableAndClear(), args, cancellationToken).ConfigureAwait(false);
        }

        return default;
    }

    /// <summary>
    /// Version of <see cref="RunChannelAsync"/> when caller the prefers the results being pre-packaged into arrays to process.
    /// </summary>
    public static Task RunAsync<TArgs>(
        ProducerConsumerOptions options,
        Func<Action<TItem>, TArgs, CancellationToken, Task> produceItems,
        Func<ImmutableArray<TItem>, TArgs, CancellationToken, Task> consumeItems,
        TArgs args,
        CancellationToken cancellationToken)
    {
        return RunChannelAsync(
            options,
            static (onItemFound, args, cancellationToken) => args.produceItems(onItemFound, args.args, cancellationToken),
            static (reader, args, cancellationToken) => BatchReaderIntoArraysAsync(reader, args.consumeItems, args.args, cancellationToken),
            args: (produceItems, consumeItems, args),
            cancellationToken);
    }

    /// <summary>
    /// Version of <see cref="RunChannelAsync"/> when the caller prefers working with a stream of results.
    /// </summary>
    public static Task RunAsync<TArgs>(
        ProducerConsumerOptions options,
        Func<Action<TItem>, TArgs, CancellationToken, Task> produceItems,
        Func<IAsyncEnumerable<TItem>, TArgs, CancellationToken, Task> consumeItems,
        TArgs args,
        CancellationToken cancellationToken)
    {
        // Bridge to sibling helper that takes a consumeItems that returns a value.
        return RunChannelAsync(
            options,
            produceItems: static (callback, args, cancellationToken) => args.produceItems(callback, args.args, cancellationToken),
            consumeItems: static async (items, args, cancellationToken) =>
            {
                await args.consumeItems(items.ReadAllAsync(cancellationToken), args.args, cancellationToken).ConfigureAwait(false);
                return default(VoidResult);
            },
            args: (produceItems, consumeItems, args),
            cancellationToken);
    }

    /// <summary>
    /// <code>IEnumerable&lt;TSource&gt; -> Task</code>.  Callback receives IAsyncEnumerable items.
    /// </summary>
    public static Task RunParallelAsync<TSource, TArgs>(
        IEnumerable<TSource> source,
        Func<TSource, Action<TItem>, TArgs, CancellationToken, Task> produceItems,
        Func<IAsyncEnumerable<TItem>, TArgs, CancellationToken, Task> consumeItems,
        TArgs args,
        CancellationToken cancellationToken)
    {
        // Bridge to sibling helper that operates on an IAsyncEnumerable.
        return RunParallelAsync(source.AsAsyncEnumerable(), produceItems, consumeItems, args, cancellationToken);
    }

    /// <summary>
    /// <code>IAsyncEnumerable&lt;TSource&gt; -> Task</code>.  Callback receives IAsyncEnumerable items.
    /// </summary>
    public static Task RunParallelAsync<TSource, TArgs>(
        IAsyncEnumerable<TSource> source,
        Func<TSource, Action<TItem>, TArgs, CancellationToken, Task> produceItems,
        Func<IAsyncEnumerable<TItem>, TArgs, CancellationToken, Task> consumeItems,
        TArgs args,
        CancellationToken cancellationToken)
    {
        // Bridge to sibling helper that takes a consumeItems that returns a value.
        return RunParallelAsync(
            source,
            produceItems: static (item, callback, args, cancellationToken) => args.produceItems(item, callback, args.args, cancellationToken),
            consumeItems: static async (items, args, cancellationToken) =>
            {
                await args.consumeItems(items, args.args, cancellationToken).ConfigureAwait(false);
                return default(VoidResult);
            },
            args: (produceItems, consumeItems, args),
            cancellationToken);
    }

    /// <summary>
    /// <code>IEnumerable&lt;TSource&gt; -> Task</code>.  Callback receives ImmutableArray of items.
    /// </summary>
    public static Task RunParallelAsync<TSource, TArgs>(
        IEnumerable<TSource> source,
        Func<TSource, Action<TItem>, TArgs, CancellationToken, Task> produceItems,
        Func<ImmutableArray<TItem>, TArgs, CancellationToken, Task> consumeItems,
        TArgs args,
        CancellationToken cancellationToken)
    {
        // Bridge to sibling helper that operates on an IAsyncEnumerable.
        return RunParallelAsync(source.AsAsyncEnumerable(), produceItems, consumeItems, args, cancellationToken);
    }

    /// <summary>
    /// <code>IAsyncEnumerable&lt;TSource&gt; -> Task</code>.  Callback receives ImmutableArray of items.
    /// </summary>
    public static Task RunParallelAsync<TSource, TArgs>(
        IAsyncEnumerable<TSource> source,
        Func<TSource, Action<TItem>, TArgs, CancellationToken, Task> produceItems,
        Func<ImmutableArray<TItem>, TArgs, CancellationToken, Task> consumeItems,
        TArgs args,
        CancellationToken cancellationToken)
    {
        // Bridge to sibling helper that takes a consumeItems that returns a value.
        return RunParallelChannelAsync(
            source,
            produceItems: static (item, callback, args, cancellationToken) => args.produceItems(item, callback, args.args, cancellationToken),
            consumeItems: static (items, args, cancellationToken) => BatchReaderIntoArraysAsync(items, args.consumeItems, args.args, cancellationToken),
            args: (produceItems, consumeItems, args),
            cancellationToken);
    }

    /// <summary>
    /// <code>IEnumerable&lt;TSource&gt; -> Task&lt;TResult&gt;</code>  Callback receives an IAsyncEnumerable of items.
    /// </summary>
    public static Task<TResult> RunParallelAsync<TSource, TArgs, TResult>(
        IEnumerable<TSource> source,
        Func<TSource, Action<TItem>, TArgs, CancellationToken, Task> produceItems,
        Func<IAsyncEnumerable<TItem>, TArgs, CancellationToken, Task<TResult>> consumeItems,
        TArgs args,
        CancellationToken cancellationToken)
    {
        // Bridge to sibling helper that operates on an IAsyncEnumerable.
        return RunParallelAsync(source.AsAsyncEnumerable(), produceItems, consumeItems, args, cancellationToken);
    }

    /// <summary>
    /// <code>IAsyncEnumerable&lt;TSource&gt; -> Task&lt;TResult&gt;</code>.  Callback receives an IAsyncEnumerable of items.
    /// </summary>
    public static Task<TResult> RunParallelAsync<TSource, TArgs, TResult>(
        IAsyncEnumerable<TSource> source,
        Func<TSource, Action<TItem>, TArgs, CancellationToken, Task> produceItems,
        Func<IAsyncEnumerable<TItem>, TArgs, CancellationToken, Task<TResult>> consumeItems,
        TArgs args,
        CancellationToken cancellationToken)
    {
        return RunParallelChannelAsync(
            source,
            produceItems: static (item, callback, args, cancellationToken) => args.produceItems(item, callback, args.args, cancellationToken),
            consumeItems: static (reader, args, cancellationToken) => args.consumeItems(reader.ReadAllAsync(cancellationToken), args.args, cancellationToken),
            args: (produceItems, consumeItems, args),
            cancellationToken);
    }

    #region helpers that return arrays

    /// <summary>
    /// <code>IEnumerable&lt;TSource&gt; -> Task&lt;ImmutableArray&lt;TResult&gt;&gt;</code>
    /// </summary>
    public static Task<ImmutableArray<TItem>> RunParallelAsync<TSource, TArgs>(
        IEnumerable<TSource> source,
        Func<TSource, Action<TItem>, TArgs, CancellationToken, Task> produceItems,
        TArgs args,
        CancellationToken cancellationToken)
    {
        // Bridge to sibling helper that operates on an IAsyncEnumerable.
        return RunParallelAsync(source.AsAsyncEnumerable(), produceItems, args, cancellationToken);
    }

    /// <summary>
    /// <code>IAsyncEnumerable&lt;TSource&gt; -> Task&lt;ImmutableArray&lt;TResult&gt;&gt;</code>
    /// </summary>
    public static async Task<ImmutableArray<TItem>> RunParallelAsync<TSource, TArgs>(
        IAsyncEnumerable<TSource> source,
        Func<TSource, Action<TItem>, TArgs, CancellationToken, Task> produceItems,
        TArgs args,
        CancellationToken cancellationToken)
    {
        // Bridge to sibling helper that takes a consumeItems that returns a value.
        return await RunParallelAsync(
            source,
            produceItems: static (item, callback, args, cancellationToken) => args.produceItems(item, callback, args.args, cancellationToken),
            consumeItems: static (stream, args, cancellationToken) => stream.ToImmutableArrayAsync(cancellationToken),
            args: (produceItems, args),
            cancellationToken).ConfigureAwait(false);
    }

    #endregion

    #region Core channel-based impl

    private static Task<TResult> RunParallelChannelAsync<TSource, TArgs, TResult>(
        IAsyncEnumerable<TSource> source,
        Func<TSource, Action<TItem>, TArgs, CancellationToken, Task> produceItems,
        Func<ChannelReader<TItem>, TArgs, CancellationToken, Task<TResult>> consumeItems,
        TArgs args,
        CancellationToken cancellationToken)
    {
        return RunChannelAsync(
            // We're running in parallel, so we def have multiple writers
            ProducerConsumerOptions.SingleReaderOptions,
            produceItems: static (callback, args, cancellationToken) =>
                RoslynParallel.ForEachAsync(
                    args.source,
                    cancellationToken,
                    async (source, cancellationToken) =>
                        await args.produceItems(source, callback, args.args, cancellationToken).ConfigureAwait(false)),
            consumeItems: static (enumerable, args, cancellationToken) => args.consumeItems(enumerable, args.args, cancellationToken),
            args: (source, produceItems, consumeItems, args),
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
    private static async Task<TResult> RunChannelAsync<TArgs, TResult>(
        ProducerConsumerOptions options,
        Func<Action<TItem>, TArgs, CancellationToken, Task> produceItems,
        Func<ChannelReader<TItem>, TArgs, CancellationToken, Task<TResult>> consumeItems,
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

        var writeTask = ProduceItemsAndWriteToChannelAsync();
        var readTask = ReadFromChannelAndConsumeItemsAsync();
        await Task.WhenAll(writeTask, readTask).ConfigureAwait(false);

        return await readTask.ConfigureAwait(false);

        async Task<TResult> ReadFromChannelAndConsumeItemsAsync()
        {
            await Task.Yield().ConfigureAwait(false);
            return await consumeItems(channel.Reader, args, cancellationToken).ConfigureAwait(false);
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
                await produceItems(item => channel.Writer.TryWrite(item), args, cancellationToken).ConfigureAwait(false);
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

    #endregion
}
