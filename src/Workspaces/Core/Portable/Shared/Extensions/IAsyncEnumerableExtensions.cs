// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class AsyncEnumerable<T>
    {
        public static readonly IAsyncEnumerable<T> Empty = GetEmptyAsync();

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        private static async IAsyncEnumerable<T> GetEmptyAsync()
        {
            yield break;
        }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    }

    internal static class IAsyncEnumerableExtensions
    {
        public static async Task<ImmutableArray<T>> ToImmutableArrayAsync<T>(this IAsyncEnumerable<T> values, CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<T>.GetInstance(out var result);
            await foreach (var value in values.WithCancellation(cancellationToken).ConfigureAwait(false))
                result.Add(value);

            return result.ToImmutable();
        }

        /// <summary>
        /// Takes an array of <see cref="IAsyncEnumerable{T}"/>s and produces a single resultant <see
        /// cref="IAsyncEnumerable{T}"/> with all their values merged together.  Absolutely no ordering guarantee is
        /// provided.  It will be expected that the individual values from distinct enumerables will be interleaved
        /// together.
        /// </summary>
        /// <remarks>This helper is useful when doign parallel processing of work where each job returns an <see
        /// cref="IAsyncEnumerable{T}"/>, but one final stream is desired as the result.</remarks>
        public static IAsyncEnumerable<T> MergeAsync<T>(this IAsyncEnumerable<T>[] enumerables, CancellationToken cancellationToken)
        {
            // Code provided by Stephen Toub, but heavily modified after that.

            var channel = Channel.CreateUnbounded<T>();

            var tasks = new Task[enumerables.Length];
            for (var i = 0; i < enumerables.Length; i++)
                tasks[i] = Process(enumerables[i], channel.Writer, cancellationToken);

            Task.WhenAll(tasks).ContinueWith(t =>
            {
                if (t.IsFaulted)
                    channel.Writer.Complete(t.Exception);
                else
                    channel.Writer.Complete();
            }, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);

            return ReadAllAsync(channel.Reader, cancellationToken);

            static async Task Process(IAsyncEnumerable<T> ae, ChannelWriter<T> writer, CancellationToken cancellationToken)
            {
                await foreach (var t in ae)
                    await writer.WriteAsync(t, cancellationToken).ConfigureAwait(false);
            }
        }

        private static async IAsyncEnumerable<T> ReadAllAsync<T>(ChannelReader<T> reader, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            {
                while (reader.TryRead(out var item))
                    yield return item;
            }
        }
    }
}
