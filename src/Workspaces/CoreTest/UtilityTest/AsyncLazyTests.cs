// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Threading;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests;

[Trait(Traits.Feature, Traits.Features.AsyncLazy)]
public sealed partial class AsyncLazyTests
{
    [Fact]
    public void GetValueAsyncReturnsCompletedTaskIfAsyncComputationCompletesImmediately()
    {
        // Note, this test may pass even if GetValueAsync posted a task to the threadpool, since the 
        // current thread may context switch out and allow the threadpool to complete the task before
        // we check the state.  However, a failure here definitely indicates a bug in AsyncLazy.
        var lazy = AsyncLazy.Create(static async c => 5);
        var t = lazy.GetValueAsync(CancellationToken.None);
        Assert.Equal(TaskStatus.RanToCompletion, t.Status);
        Assert.Equal(5, t.Result);
    }

    [Theory]
    [InlineData(TaskStatus.RanToCompletion)]
    [InlineData(TaskStatus.Canceled)]
    [InlineData(TaskStatus.Faulted)]
    public void SynchronousContinuationsDoNotRunWithinGetValueCall(TaskStatus expectedTaskStatus)
    {
        var synchronousComputationStartedEvent = new ManualResetEvent(initialState: false);
        var synchronousComputationShouldCompleteEvent = new ManualResetEvent(initialState: false);

        var requestCancellationTokenSource = new CancellationTokenSource();

        // First, create an async lazy that will only ever do synchronous computations.
        var lazy = AsyncLazy.Create(
            asynchronousComputeFunction: static (arg, c) => { throw new Exception("We should not get an asynchronous computation."); },
            synchronousComputeFunction: static (arg, c) =>
            {
                // Notify that the synchronous computation started
                arg.synchronousComputationStartedEvent.Set();

                // And now wait when we should finish
                arg.synchronousComputationShouldCompleteEvent.WaitOne();

                c.ThrowIfCancellationRequested();

                if (arg.expectedTaskStatus == TaskStatus.Faulted)
                {
                    // We want to see what happens if this underlying task faults, so let's fault!
                    throw new Exception("Task blew up!");
                }

                return 42;
            },
            arg: (synchronousComputationStartedEvent, synchronousComputationShouldCompleteEvent, expectedTaskStatus));

        // Second, start a synchronous request. While we are in the GetValue, we will record which thread is being occupied by the request
        Thread? synchronousRequestThread = null;
        Task.Factory.StartNew(() =>
        {
            try
            {
                synchronousRequestThread = Thread.CurrentThread;
                lazy.GetValue(requestCancellationTokenSource.Token);
            }
            finally // we do test GetValue in exceptional scenarios, so we should deal with this
            {
                synchronousRequestThread = null;
            }
        }, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Current);

        // Wait until this request has actually started
        synchronousComputationStartedEvent.WaitOne();

        // Good, we now have a synchronous request running. An async request should simply create a task that would
        // be completed when the synchronous request completes. We want to assert that if we were to run a continuation
        // from this task that's marked ExecuteSynchronously, we do not run it inline atop the synchronous request.
        bool? asyncContinuationRanSynchronously = null;
        TaskStatus? observedAntecedentTaskStatus = null;

        var asyncContinuation = lazy.GetValueAsync(requestCancellationTokenSource.Token).ContinueWith(antecedent =>
            {
                var currentSynchronousRequestThread = synchronousRequestThread;

                asyncContinuationRanSynchronously = currentSynchronousRequestThread != null && currentSynchronousRequestThread == Thread.CurrentThread;
                observedAntecedentTaskStatus = antecedent.Status;
            },
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);

        // Excellent, the async continuation is scheduled. Let's complete the underlying computation.
        if (expectedTaskStatus == TaskStatus.Canceled)
        {
            requestCancellationTokenSource.Cancel();
        }

        synchronousComputationShouldCompleteEvent.Set();

        // And wait for our continuation to run
        asyncContinuation.Wait();

        AssertEx.NotNull(asyncContinuationRanSynchronously, "The continuation never ran.");
        Assert.False(asyncContinuationRanSynchronously.Value, "The continuation did not run asynchronously.");
        Assert.Equal(expectedTaskStatus, observedAntecedentTaskStatus!.Value);
    }

    [Fact]
    public void GetValueThrowsCorrectExceptionDuringCancellation()
        => GetValueOrGetValueAsyncThrowsCorrectExceptionDuringCancellation((lazy, ct) => lazy.GetValue(ct), includeSynchronousComputation: false);

    [Fact]
    public void GetValueThrowsCorrectExceptionDuringCancellationWithSynchronousComputation()
        => GetValueOrGetValueAsyncThrowsCorrectExceptionDuringCancellation((lazy, ct) => lazy.GetValue(ct), includeSynchronousComputation: true);

    [Fact]
    public void GetValueAsyncThrowsCorrectExceptionDuringCancellation()
    {
        // NOTE: since GetValueAsync inlines the call to the async computation, the GetValueAsync call will throw
        // immediately instead of returning a task that transitions to the cancelled state
        GetValueOrGetValueAsyncThrowsCorrectExceptionDuringCancellation((lazy, ct) => lazy.GetValueAsync(ct), includeSynchronousComputation: false);
    }

    [Fact]
    public void GetValueAsyncThrowsCorrectExceptionDuringCancellationWithSynchronousComputation()
    {
        // In theory the synchronous computation isn't used during GetValueAsync, but just in case...
        GetValueOrGetValueAsyncThrowsCorrectExceptionDuringCancellation((lazy, ct) => lazy.GetValueAsync(ct), includeSynchronousComputation: true);
    }

    private static void GetValueOrGetValueAsyncThrowsCorrectExceptionDuringCancellation(Action<AsyncLazy<object>, CancellationToken> doGetValue, bool includeSynchronousComputation)
    {
        // A call to GetValue/GetValueAsync with a token that is cancelled should throw an OperationCancelledException, but it's
        // important to make sure the correct token is cancelled. It should be cancelled with the token passed
        // to GetValue, not the cancellation that was thrown by the computation function

        var computeFunctionRunning = new ManualResetEvent(initialState: false);

        AsyncLazy<object> lazy;
        Func<ManualResetEvent?, CancellationToken, object>? synchronousComputation = null;

        if (includeSynchronousComputation)
        {
            synchronousComputation = (arg, c) =>
            {
                computeFunctionRunning.Set();
                while (true)
                {
                    c.ThrowIfCancellationRequested();
                }
            };
        }

        lazy = AsyncLazy.Create(
            static (computeFunctionRunning, c) =>
            {
                computeFunctionRunning.Set();
                while (true)
                {
                    c.ThrowIfCancellationRequested();
                }
            },
            synchronousComputeFunction: synchronousComputation,
            arg: computeFunctionRunning);

        var cancellationTokenSource = new CancellationTokenSource();

        // Create a task that will cancel the request once it's started
        Task.Run(() =>
        {
            computeFunctionRunning.WaitOne();
            cancellationTokenSource.Cancel();
        });

        try
        {
            doGetValue(lazy, cancellationTokenSource.Token);
            AssertEx.Fail(nameof(AsyncLazy<>.GetValue) + " did not throw an exception.");
        }
        catch (OperationCanceledException oce)
        {
            Assert.Equal(cancellationTokenSource.Token, oce.CancellationToken);
        }
    }

    [Fact]
    public void GetValueAsyncThatIsCancelledReturnsTaskCancelledWithCorrectToken()
    {
        var cancellationTokenSource = new CancellationTokenSource();

        var lazy = AsyncLazy.Create(static (cancellationTokenSource, c) => Task.Run((Func<object>)(() =>
        {
            cancellationTokenSource.Cancel();
            while (true)
            {
                c.ThrowIfCancellationRequested();
            }
        }), c), arg: cancellationTokenSource);

        var task = lazy.GetValueAsync(cancellationTokenSource.Token);

        // Now wait until the task completes
        try
        {
            task.Wait();
            AssertEx.Fail(nameof(AsyncLazy<>.GetValueAsync) + " did not throw an exception.");
        }
        catch (AggregateException ex)
        {
            var operationCancelledException = (OperationCanceledException)ex.Flatten().InnerException!;
            Assert.Equal(cancellationTokenSource.Token, operationCancelledException.CancellationToken);
        }
    }

    [Theory, CombinatorialData]
    private static void CancellationDuringInlinedComputationFromGetValueOrGetValueAsyncStillCachesResult(bool includeSynchronousComputation)
    {
        var computations = 0;
        var requestCancellationTokenSource = new CancellationTokenSource();
        object? createdObject = null;

        Func<CancellationToken, object> synchronousComputation = c =>
        {
            Interlocked.Increment(ref computations);

            // We do not want to ever use the cancellation token that we are passed to this
            // computation. Rather, we will ignore it but cancel any request that is
            // outstanding.
            requestCancellationTokenSource.Cancel();

            createdObject = new object();
            return createdObject;
        };

        var lazy = AsyncLazy.Create(
            static async (synchronousComputation, c) => synchronousComputation(c),
            includeSynchronousComputation ? static (synchronousComputation, c) => synchronousComputation(c) : null,
            arg: synchronousComputation);

        var thrownException = Assert.Throws<OperationCanceledException>(() =>
        {
            // Do a first request. Even though we will get a cancellation during the evaluation,
            // since we handed a result back, that result must be cached.
            lazy.GetValue(requestCancellationTokenSource.Token);
        });

        // And a second request. We'll let this one complete normally.
        var secondRequestResult = lazy.GetValue(CancellationToken.None);

        // We should have gotten the same cached result, and we should have only computed once.
        Assert.Same(createdObject, secondRequestResult);
        Assert.Equal(1, computations);
    }

    [Fact]
    public void SynchronousRequestShouldCacheValueWithAsynchronousComputeFunction()
    {
        var lazy = AsyncLazy.Create(static async c => new object());

        var firstRequestResult = lazy.GetValue(CancellationToken.None);
        var secondRequestResult = lazy.GetValue(CancellationToken.None);

        Assert.Same(secondRequestResult, firstRequestResult);
    }

    [Theory, CombinatorialData]
    public async Task AwaitingProducesCorrectException(bool producerAsync, bool consumerAsync)
    {
        var exception = new ArgumentException();
        Func<CancellationToken, Task<object>> asynchronousComputeFunction =
            async cancellationToken =>
            {
                await Task.Yield();
                throw exception;
            };
        Func<CancellationToken, object> synchronousComputeFunction =
            cancellationToken =>
            {
                throw exception;
            };

        var lazy = producerAsync
            ? AsyncLazy.Create(asynchronousComputeFunction)
            : AsyncLazy.Create(asynchronousComputeFunction, synchronousComputeFunction);

        var actual = consumerAsync
            ? await Assert.ThrowsAsync<ArgumentException>(async () => await lazy.GetValueAsync(CancellationToken.None))
            : Assert.Throws<ArgumentException>(() => lazy.GetValue(CancellationToken.None));

        Assert.Same(exception, actual);
    }

    [Fact]
    public async Task CancelledAndReranAsynchronousComputationDoesNotBreakSynchronousRequest()
    {
        // We're going to create an AsyncLazy where we will call GetValue synchronously, and while that operation is
        // running we're going to call GetValueAsync() more than once; the first time we will let cancel, the second time will
        // run to completion.
        var synchronousComputationStartedEvent = new ManualResetEvent(initialState: false);
        var synchronousComputationShouldCompleteEvent = new ManualResetEvent(initialState: false);

        // We don't want the async path to run sooner than we expect, so we'll set it once ready
        Func<CancellationToken, Task<string>>? asynchronousComputation = null;

        var lazy = AsyncLazy.Create(
            asynchronousComputeFunction: static (arg, ct) =>
            {
                AssertEx.NotNull(arg.asynchronousComputation, $"The asynchronous computation was not expected to be running.");
                return arg.asynchronousComputation(ct);
            },
            synchronousComputeFunction: static (arg, ct) =>
            {
                // Let the test know we've started, and we'll continue once asked
                arg.synchronousComputationStartedEvent.Set();
                arg.synchronousComputationShouldCompleteEvent.WaitOne();
                return "Returned from synchronous computation: " + Guid.NewGuid();
            },
            arg: (asynchronousComputation, synchronousComputationStartedEvent, synchronousComputationShouldCompleteEvent));

        // Step 1: start the synchronous operation and wait for it to be running
        var synchronousRequest = Task.Run(() => lazy.GetValue(CancellationToken.None));
        synchronousComputationStartedEvent.WaitOne();

        // Step 2: it's running, so let's let a async operation get started and then cancel. We're ensuring that if this cancels, we might forget we have
        // the synchronous operation running if we weren't careful.
        var cancellationTokenSource = new CancellationTokenSource();

        var asynchronousRequestToBeCancelled = lazy.GetValueAsync(cancellationTokenSource.Token);
        cancellationTokenSource.Cancel();
        await asynchronousRequestToBeCancelled.NoThrowAwaitableInternal();
        Assert.Equal(TaskStatus.Canceled, asynchronousRequestToBeCancelled.Status);

        // Step 3: let's now let an async request run normally, producing a value
        asynchronousComputation = async _ => "Returned from asynchronous computation: " + Guid.NewGuid();

        var asynchronousRequest = lazy.GetValueAsync(CancellationToken.None);

        // Now let's finally complete our synchronous request that's been waiting for awhile
        synchronousComputationShouldCompleteEvent.Set();
        var valueReturnedFromSynchronousRequest = await synchronousRequest;

        // We expect that in this case, we should still get the same value back
        Assert.Equal(await asynchronousRequest, valueReturnedFromSynchronousRequest);
    }

    [Fact]
    public async Task AsynchronousResultThatWasCancelledDoesNotBreakSynchronousRequest()
    {
        // We're going to do the following sequence of operations:
        //
        // 1. Start an asynchronous request
        // 2. Cancel the asynchronous request (but it's still consuming CPU because it hasn't observed the cancellation yet)
        // 3. Start a synchronous request
        // 4. Let the asynchronous request complete, as if the cancellation was never observed
        // 5. Complete the synchronous request
        var synchronousComputationStartedEvent = new ManualResetEvent(initialState: false);
        var synchronousComputationShouldCompleteEvent = new ManualResetEvent(initialState: false);
        var asynchronousComputationReadyToComplete = new ManualResetEvent(initialState: false);
        var asynchronousComputationShouldCompleteEvent = new ManualResetEvent(initialState: false);

        var asynchronousRequestCancellationToken = new CancellationTokenSource();

        var lazy = AsyncLazy.Create(
            asynchronousComputeFunction: static async (arg, ct) =>
            {
                arg.asynchronousRequestCancellationToken.Cancel();

                // Now wait until the cancellation is sent to this underlying computation
                while (!ct.IsCancellationRequested)
                    Thread.Yield();

                // Now we're ready to complete, so this is when we want to pause
                arg.asynchronousComputationReadyToComplete.Set();
                arg.asynchronousComputationShouldCompleteEvent.WaitOne();

                return "Returned from asynchronous computation: " + Guid.NewGuid();
            },
            synchronousComputeFunction: static (arg, _) =>
            {
                // Let the test know we've started, and we'll continue once asked
                arg.synchronousComputationStartedEvent.Set();
                arg.synchronousComputationShouldCompleteEvent.WaitOne();
                return "Returned from synchronous computation: " + Guid.NewGuid();
            },
            arg: (asynchronousRequestCancellationToken, asynchronousComputationReadyToComplete, asynchronousComputationShouldCompleteEvent, synchronousComputationStartedEvent, synchronousComputationShouldCompleteEvent));

        // Steps 1 and 2: start asynchronous computation and wait until it's running; this will cancel itself once it's started
        var asynchronousRequest = Task.Run(() => lazy.GetValueAsync(asynchronousRequestCancellationToken.Token));

        asynchronousComputationReadyToComplete.WaitOne();

        // Step 3: while the async request is cancelled but still "thinking", let's start the synchronous request
        var synchronousRequest = Task.Run(() => lazy.GetValue(CancellationToken.None));
        synchronousComputationStartedEvent.WaitOne();

        // Step 4: let the asynchronous compute function now complete
        asynchronousComputationShouldCompleteEvent.Set();

        // At some point the asynchronous computation value is now going to be cached
        string? asyncResult;
        while (!lazy.TryGetValue(out asyncResult))
            Thread.Yield();

        // Step 5: let the synchronous request complete
        synchronousComputationShouldCompleteEvent.Set();

        var synchronousResult = await synchronousRequest;

        // We expect that in this case, the synchronous result should have been thrown away since the async result was computed first
        Assert.Equal(asyncResult, synchronousResult);
    }
}
