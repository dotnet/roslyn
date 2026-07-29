// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Threading;
using Xunit;

namespace Roslyn.Utilities;

public sealed class AsyncBatchingWorkQueueTests
{
    [Fact]
    public async Task AddWorkAfterDisposeDoesNotRun()
    {
        var processed = false;
        var listener = new AsynchronousOperationListener();
        var queue = new AsyncBatchingWorkQueue<int>(
            TimeSpan.FromSeconds(1),
            async (items, cancellationToken) => processed = true,
            listener);

        queue.Dispose();
        queue.AddWork(1);

        await listener.ExpeditedWaitAsync();

        Assert.False(processed);
    }

    [Fact]
    public async Task AddWorkAfterCancellingTokenDoesNotRun()
    {
        var processed = false;
        using var source = new CancellationTokenSource();
        var listener = new AsynchronousOperationListener();
        var queue = new AsyncBatchingWorkQueue<int>(
            TimeSpan.FromSeconds(1),
            async (items, cancellationToken) => processed = true,
            listener,
            source.Token);

        source.Cancel();
        queue.AddWork(1);

        await listener.ExpeditedWaitAsync();

        Assert.False(processed);
    }

    [Fact]
    public async Task DisposeStopsWorkAlreadyQueued()
    {
        var processed = false;
        var listener = new AsynchronousOperationListener();
        var queue = new AsyncBatchingWorkQueue<int>(
            TimeSpan.FromDays(1),
            async (items, cancellationToken) => processed = true,
            listener);

        // Queue up work, then dispose before the batch has had a chance to run.
        queue.AddWork(1);
        queue.Dispose();

        await listener.ExpeditedWaitAsync();

        Assert.False(processed);
    }

    [Fact]
    public async Task CancelingTokenStopsWorkAlreadyQueued()
    {
        var processed = false;
        using var source = new CancellationTokenSource();
        var listener = new AsynchronousOperationListener();
        var queue = new AsyncBatchingWorkQueue<int>(
            TimeSpan.FromDays(1),
            async (items, cancellationToken) => processed = true,
            listener,
            source.Token);

        queue.AddWork(1);
        source.Cancel();

        await listener.ExpeditedWaitAsync();

        Assert.False(processed);
    }

    [Fact]
    public void DisposeIsIdempotent()
    {
        var queue = new AsyncBatchingWorkQueue<int>(
            TimeSpan.Zero,
            async (items, cancellationToken) => { },
            AsynchronousOperationListenerProvider.NullListener);

        queue.Dispose();
        queue.Dispose();
    }

    [Fact]
    public async Task AlreadyCancelledTokenProducesDisposedQueue()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();

        // The cancellation registration runs synchronously inside the constructor when the token is already canceled.
        // Ensure that is safe, and leaves the queue fully shut down.
        var processed = false;
        var listener = new AsynchronousOperationListener();

        var queue = new AsyncBatchingWorkQueue<int>(
            TimeSpan.Zero,
            async (items, cancellationToken) => processed = true,
            listener,
            source.Token);

        queue.AddWork(1);

        await listener.ExpeditedWaitAsync();

        Assert.False(processed);
    }

    [Fact]
    public void CancelExistingWorkAfterDisposeDoesNotThrow()
    {
        var queue = new AsyncBatchingWorkQueue<int>(
            TimeSpan.Zero,
            async (items, cancellationToken) => { },
            AsynchronousOperationListenerProvider.NullListener);

        queue.Dispose();
        queue.CancelExistingWork();
    }
}
