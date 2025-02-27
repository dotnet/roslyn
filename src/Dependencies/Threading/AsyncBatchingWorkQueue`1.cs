// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Microsoft.CodeAnalysis.Threading;

/// <inheritdoc cref="AsyncBatchingWorkQueue{TItem, TResult}"/>
internal class AsyncBatchingWorkQueue<TItem>(
    TimeSpan delay,
    Func<ImmutableSegmentedList<TItem>, CancellationToken, ValueTask> processBatchAsync,
    IEqualityComparer<TItem>? equalityComparer,
    IAsynchronousOperationListener asyncListener,
    CancellationToken cancellationToken) : AsyncBatchingWorkQueue<TItem, VoidResult>(delay, Convert(processBatchAsync), equalityComparer, asyncListener, cancellationToken)
{
    public AsyncBatchingWorkQueue(
        TimeSpan delay,
        Func<ImmutableSegmentedList<TItem>, CancellationToken, ValueTask> processBatchAsync,
        IAsynchronousOperationListener asyncListener,
        CancellationToken cancellationToken)
        : this(delay,
               processBatchAsync,
               equalityComparer: null,
               asyncListener,
               cancellationToken)
    {
    }

    private static Func<ImmutableSegmentedList<TItem>, CancellationToken, ValueTask<VoidResult>> Convert(Func<ImmutableSegmentedList<TItem>, CancellationToken, ValueTask> processBatchAsync)
        => async (items, ct) =>
        {
            await processBatchAsync(items, ct).ConfigureAwait(false);
            return default;
        };

    public new Task WaitUntilCurrentBatchCompletesAsync()
        => base.WaitUntilCurrentBatchCompletesAsync();
}
