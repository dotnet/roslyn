// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Roslyn.Test.Utilities;

/// <summary>
/// Sequential scheduler for test <see cref="StreamJsonRpc.JsonRpc"/> instances.
/// </summary>
/// <remarks>
/// StreamJsonRpc's default scheduler is <c>NonConcurrentSynchronizationContext</c>, which calls
/// <see cref="Environment.FailFast(string, Exception)"/> if the scheduler itself throws. On .NET Framework,
/// xUnit unloads the test AppDomain and aborts any thread still inside that scheduler. The abort becomes a
/// <see cref="ThreadAbortException"/> and crashes the test host. See https://github.com/dotnet/roslyn/issues/84579.
/// </remarks>
internal sealed class TestJsonRpcSynchronizationContext : SynchronizationContext
{
    private readonly Queue<(SendOrPostCallback Callback, object? State)> _queue = new();
    private readonly SemaphoreSlim _signal = new(0);
    private readonly Task _processor;
    private int _activeManagedThreadId;
    private int _stopRequested;

    public TestJsonRpcSynchronizationContext()
    {
        _processor = Task.Run(ProcessQueueAsync);
    }

    public override void Post(SendOrPostCallback d, object? state)
    {
        Contract.ThrowIfNull(d);

        lock (_queue)
        {
            if (Volatile.Read(ref _stopRequested) != 0)
                return;

            _queue.Enqueue((d, state));
        }

        _signal.Release();
    }

    public override void Send(SendOrPostCallback d, object? state)
    {
        Contract.ThrowIfNull(d);

        if (Volatile.Read(ref _activeManagedThreadId) == Environment.CurrentManagedThreadId)
        {
            d(state);
            return;
        }

        var completed = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        Post(
            static s =>
            {
                var (callback, callbackState, completion) = ((SendOrPostCallback, object?, TaskCompletionSource<object?>))s!;
                try
                {
                    callback(callbackState);
                    completion.TrySetResult(null);
                }
                catch (Exception ex) when (!IsThreadAbort(ex))
                {
                    completion.TrySetException(ex);
                }
            },
            (d, state, completed));

        completed.Task.GetAwaiter().GetResult();
    }

    public override SynchronizationContext CreateCopy() => this;

    /// <summary>
    /// Runs already queued work, then exits the processor so AppDomain unload cannot abort it.
    /// </summary>
    internal async Task DrainAndStopAsync()
    {
        if (Volatile.Read(ref _stopRequested) != 0)
        {
            await _processor.ConfigureAwait(false);
            return;
        }

        var drained = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        Post(static state => ((TaskCompletionSource<object?>)state!).TrySetResult(null), drained);

        var drainCompleted = await Task.WhenAny(drained.Task, Task.Delay(TimeSpan.FromSeconds(30))).ConfigureAwait(false);
        if (drainCompleted != drained.Task)
            throw new TimeoutException("Timed out draining the test JsonRpc synchronization context.");

        Volatile.Write(ref _stopRequested, 1);
        _signal.Release();

        var stopCompleted = await Task.WhenAny(_processor, Task.Delay(TimeSpan.FromSeconds(30))).ConfigureAwait(false);
        if (stopCompleted != _processor)
            throw new TimeoutException("Timed out stopping the test JsonRpc synchronization context.");

        await _processor.ConfigureAwait(false);
    }

    private async Task ProcessQueueAsync()
    {
        while (true)
        {
            await _signal.WaitAsync().ConfigureAwait(false);

            while (true)
            {
                (SendOrPostCallback Callback, object? State) work;
                lock (_queue)
                {
                    if (_queue.Count == 0)
                    {
                        if (Volatile.Read(ref _stopRequested) != 0)
                            return;

                        break;
                    }

                    work = _queue.Dequeue();
                }

                Volatile.Write(ref _activeManagedThreadId, Environment.CurrentManagedThreadId);
                try
                {
                    work.Callback(work.State);
                }
                catch (Exception ex)
                {
                    // Callback failures must not tear down the scheduler.
                }
                finally
                {
                    Volatile.Write(ref _activeManagedThreadId, 0);
                }
            }
        }
    }
}
