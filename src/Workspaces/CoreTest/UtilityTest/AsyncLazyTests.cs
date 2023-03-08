// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    [Trait(Traits.Feature, Traits.Features.AsyncLazy)]
    public partial class AsyncLazyTests
    {
        [Fact]
        public void GetValueAsyncReturnsCompletedTaskIfAsyncComputationCompletesImmediately()
        {
            // Note, this test may pass even if GetValueAsync posted a task to the threadpool, since the 
            // current thread may context switch out and allow the threadpool to complete the task before
            // we check the state.  However, a failure here definitely indicates a bug in AsyncLazy.
            var lazy = AsyncLazy.Create(c => Task.FromResult(5));
            var t = lazy.GetValueAsync(CancellationToken.None);
            Assert.Equal(TaskStatus.RanToCompletion, t.Status);
            Assert.Equal(5, t.Result);
        }

        [Fact]
        public void SynchronousContinuationsDoNotRunWithinGetValueCallForCompletedTask()
            => SynchronousContinuationsDoNotRunWithinGetValueCallCore(TaskStatus.RanToCompletion);

        [Fact]
        public void SynchronousContinuationsDoNotRunWithinGetValueCallForCancelledTask()
            => SynchronousContinuationsDoNotRunWithinGetValueCallCore(TaskStatus.Canceled);

        [Fact]
        public void SynchronousContinuationsDoNotRunWithinGetValueCallForFaultedTask()
            => SynchronousContinuationsDoNotRunWithinGetValueCallCore(TaskStatus.Faulted);

        private static void SynchronousContinuationsDoNotRunWithinGetValueCallCore(TaskStatus expectedTaskStatus)
        {
            var synchronousComputationStartedEvent = new ManualResetEvent(initialState: false);
            var synchronousComputationShouldCompleteEvent = new ManualResetEvent(initialState: false);

            var requestCancellationTokenSource = new CancellationTokenSource();

            // First, create an async lazy that will only ever do synchronous computations.
            var lazy = new AsyncLazy<int>(
                asynchronousComputeFunction: c => { throw new Exception("We should not get an asynchronous computation."); },
                synchronousComputeFunction: c =>
                {
                    // Notify that the synchronous computation started
                    synchronousComputationStartedEvent.Set();

                    // And now wait when we should finish
                    synchronousComputationShouldCompleteEvent.WaitOne();

                    c.ThrowIfCancellationRequested();

                    if (expectedTaskStatus == TaskStatus.Faulted)
                    {
                        // We want to see what happens if this underlying task faults, so let's fault!
                        throw new Exception("Task blew up!");
                    }

                    return 42;
                });

            // Second, start a synchronous request. While we are in the GetValue, we will record which thread is being occupied by the request
            Thread synchronousRequestThread = null;
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

            Assert.False(asyncContinuationRanSynchronously.Value, "The continuation did not run asynchronously.");
            Assert.Equal(expectedTaskStatus, observedAntecedentTaskStatus.Value);
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
            Func<CancellationToken, object> synchronousComputation = null;

            if (includeSynchronousComputation)
            {
                synchronousComputation = c =>
                {
                    computeFunctionRunning.Set();
                    while (true)
                    {
                        c.ThrowIfCancellationRequested();
                    }
                };
            }

            lazy = new AsyncLazy<object>(c =>
            {
                computeFunctionRunning.Set();
                while (true)
                {
                    c.ThrowIfCancellationRequested();
                }
            }, synchronousComputeFunction: synchronousComputation);

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
                AssertEx.Fail(nameof(AsyncLazy<object>.GetValue) + " did not throw an exception.");
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

            var lazy = AsyncLazy.Create(c => Task.Run((Func<object>)(() =>
            {
                cancellationTokenSource.Cancel();
                while (true)
                {
                    c.ThrowIfCancellationRequested();
                }
            }), c));

            var task = lazy.GetValueAsync(cancellationTokenSource.Token);

            // Now wait until the task completes
            try
            {
                task.Wait();
                AssertEx.Fail(nameof(AsyncLazy<object>.GetValueAsync) + " did not throw an exception.");
            }
            catch (AggregateException ex)
            {
                var operationCancelledException = (OperationCanceledException)ex.Flatten().InnerException;
                Assert.Equal(cancellationTokenSource.Token, operationCancelledException.CancellationToken);
            }
        }
    }
}
