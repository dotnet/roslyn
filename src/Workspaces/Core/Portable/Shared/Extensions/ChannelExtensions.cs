// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal static class ChannelExtensions
{
    public static async Task BatchProcessAsync<TElement>(
        this Channel<TElement> channel,
        Func<Action<TElement>, CancellationToken, ValueTask> produceElementsAsync,
        Func<ImmutableArray<TElement>, CancellationToken, ValueTask> processBatchAsync,
        CancellationToken cancellationToken)
    {
        await Task.WhenAll(
            ProduceElementsAndWriteToChannelAsync(),
            ReadElementsFromChannelAndReportToCallbackAsync()).ConfigureAwait(false);

        return;

        async Task ReadElementsFromChannelAndReportToCallbackAsync()
        {
            await Task.Yield().ConfigureAwait(false);
            using var _ = ArrayBuilder<TElement>.GetInstance(out var batch);

            while (await channel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            {
                // Grab as many items as we can from the channel at once and report in a batch.
                while (channel.Reader.TryRead(out var item))
                    batch.Add(item);

                await processBatchAsync(batch.ToImmutableAndClear(), cancellationToken).ConfigureAwait(false);
            }
        }

        async Task ProduceElementsAndWriteToChannelAsync()
        {
            Exception? exception = null;
            try
            {
                await Task.Yield().ConfigureAwait(false);
                await produceElementsAsync(item => channel.Writer.TryWrite(item), cancellationToken).ConfigureAwait(false);
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
