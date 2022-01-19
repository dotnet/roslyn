﻿// Licensed to the .NET Foundation under one or more agreements.
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
    internal class AsyncBatchingWorkQueue : AsyncBatchingWorkQueue<VoidResult>
    {
        public AsyncBatchingWorkQueue(
            TimeSpan delay,
            Func<CancellationToken, ValueTask> processBatchAsync,
            IAsynchronousOperationListener asyncListener,
            CancellationToken cancellationToken)
            : base(delay, Convert(processBatchAsync), EqualityComparer<VoidResult>.Default, asyncListener, cancellationToken)
        {
        }

        private static Func<ImmutableArray<VoidResult>, CancellationToken, ValueTask> Convert(Func<CancellationToken, ValueTask> processBatchAsync)
            => (items, ct) => processBatchAsync(ct);

        public void AddWork()
            => base.AddWork(default(VoidResult));
    }
}
