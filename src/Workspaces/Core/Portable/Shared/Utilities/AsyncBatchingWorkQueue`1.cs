// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Roslyn.Utilities
{
    /// <inheritdoc cref="AsyncBatchingWorkQueue{TItem, TResult}"/>
    internal class AsyncBatchingWorkQueue<TItem> : AsyncBatchingWorkQueue<TItem, bool>
    {
        public AsyncBatchingWorkQueue(
            TimeSpan delay,
            Func<ImmutableArray<TItem>, CancellationToken, Task> processBatchAsync,
            CancellationToken cancellationToken)
            : this(delay,
                   processBatchAsync,
                   equalityComparer: null,
                   asyncListener: null,
                   cancellationToken)
        {
        }

        public AsyncBatchingWorkQueue(
            TimeSpan delay,
            Func<ImmutableArray<TItem>, CancellationToken, Task> processBatchAsync,
            IEqualityComparer<TItem>? equalityComparer,
            IAsynchronousOperationListener? asyncListener,
            CancellationToken cancellationToken)
            : base(delay, Convert(processBatchAsync), equalityComparer, asyncListener, cancellationToken)
        {
        }

        private static Func<ImmutableArray<TItem>, CancellationToken, Task<bool>> Convert(Func<ImmutableArray<TItem>, CancellationToken, Task> processBatchAsync)
            => async (items, ct) =>
            {
                await processBatchAsync(items, ct).ConfigureAwait(false);
                return true;
            };

        public new Task WaitUntilCurrentBatchCompletesAsync()
            => base.WaitUntilCurrentBatchCompletesAsync();
    }
}
