// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Roslyn.Utilities
{
    /// <inheritdoc cref="AsyncBatchingWorkQueue{TItem, TResult}"/>
    internal class AsyncBatchingWorkQueue : AsyncBatchingWorkQueue<VoidResult>
    {
        public AsyncBatchingWorkQueue(
            TimeSpan delay,
            Func<CancellationToken, ValueTask> processBatchAsync,
            IAsynchronousOperationListener asyncListener,
            CancellationToken cancellationToken)
            : this(delay, cancelOnNewWork: false, processBatchAsync, asyncListener, cancellationToken)
        {
        }

        public AsyncBatchingWorkQueue(
            TimeSpan delay,
            bool cancelOnNewWork,
            Func<CancellationToken, ValueTask> processBatchAsync,
            IAsynchronousOperationListener asyncListener,
            CancellationToken cancellationToken)
            : base(delay, cancelOnNewWork, Convert(processBatchAsync), EqualityComparer<VoidResult>.Default, asyncListener, cancellationToken)
        {
        }

        private static Func<ImmutableSegmentedList<VoidResult>, CancellationToken, ValueTask> Convert(Func<CancellationToken, ValueTask> processBatchAsync)
            => (items, ct) => processBatchAsync(ct);

        public void AddWork()
            => base.AddWork(default(VoidResult));
    }
}
