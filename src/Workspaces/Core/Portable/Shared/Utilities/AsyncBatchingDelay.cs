// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Utilities;

namespace Roslyn.Utilities
{
    internal sealed class AsyncBatchingDelay
    {
        private readonly AsyncBatchingWorkQueue<bool> _workQueue;
        private readonly Func<CancellationToken, Task> _processAsync;

        public AsyncBatchingDelay(
            TimeSpan delay,
            Func<CancellationToken, Task> processAsync,
            IAsynchronousOperationListener? asyncListener,
            CancellationToken cancellationToken)
        {
            _processAsync = processAsync;

            // We use an AsyncBatchingWorkQueue with a boolean, and just always add the
            // same value at all times.
            _workQueue = new AsyncBatchingWorkQueue<bool>(
                delay,
                OnNotifyAsync,
                equalityComparer: EqualityComparer<bool>.Default,
                asyncListener,
                cancellationToken);
        }

        private Task OnNotifyAsync(ImmutableArray<bool> _, CancellationToken cancellationToken)
        {
            return _processAsync(cancellationToken);
        }

        public void RequeueWork()
        {
            // Value doesn't matter here as long as we're consistent.
            _workQueue.AddWork(item: false);
        }
    }
}
