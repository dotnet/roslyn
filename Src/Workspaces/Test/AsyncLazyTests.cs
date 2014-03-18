// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public partial class AsyncLazyTests
    {
        [Fact]
        [Trait(Traits.Feature, Traits.Features.AsyncLazy)]
        public void GetValueAsyncReturnsCompletedTaskIfAsyncComputationCompletesImmediately()
        {
            // Note, this test may pass even if GetValueAsync posted a task to the threadpool, since the 
            // current thread may context switch out and allow the threadpool to complete the task before
            // we check the state.  However, a failure here definitely indicates a bug in AsyncLazy.
            var lazy = new AsyncLazy<int>(c => Task.FromResult(5), cacheResult: true);
            var t = lazy.GetValueAsync(CancellationToken.None);
            Assert.Equal(TaskStatus.RanToCompletion, t.Status);
            Assert.Equal(5, t.Result);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.AsyncLazy)]
        public void SynchronousContinuationsDoNotRunWithinGetValueCallForCompletedTask()
        {
            SynchronousContinuationsDoNotRunWithinGetValueCallCore(TaskStatus.RanToCompletion);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.AsyncLazy)]
        public void SynchronousContinuationsDoNotRunWithinGetValueCallForCancelledTask()
        {
            SynchronousContinuationsDoNotRunWithinGetValueCallCore(TaskStatus.Canceled);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.AsyncLazy)]
        public void SynchronousContinuationsDoNotRunWithinGetValueCallForFaultedTask()
        {
            SynchronousContinuationsDoNotRunWithinGetValueCallCore(TaskStatus.Faulted);
        }

        private void SynchronousContinuationsDoNotRunWithinGetValueCallCore(TaskStatus expectedTaskStatus)
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
                },
                cacheResult: false);

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
            }, CancellationToken.None);

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
        [Trait(Traits.Feature, Traits.Features.AsyncLazy)]
        public void CancellationDuringInlinedComputationFromGetValueAsyncStillCachesResult()
        {
            using (new StopTheThreadPoolContext())
            {
                int computations = 0;
                var requestCancellationTokenSource = new CancellationTokenSource();

                var lazy = new AsyncLazy<object>(c =>
                    {
                        Interlocked.Increment(ref computations);

                        // We do not want to ever use the cancellation token that we are passed to this
                        // computation. Rather, we will ignore it but cancel any request that is
                        // outstanding.
                        requestCancellationTokenSource.Cancel();

                        return Task.FromResult(new object());
                    }, cacheResult: true);

                // Do a first request. Even though we will get a cancellation during the evaluation,
                // since we handed a result back, that result must be cached.
                var firstRequestResult = lazy.GetValueAsync(requestCancellationTokenSource.Token).Result;

                // And a second request. We'll let this one complete normally.
                var secondRequestResult = lazy.GetValueAsync(CancellationToken.None).Result;

                // We should have gotten the same cached result, and we should have only computed once.
                Assert.Same(secondRequestResult, firstRequestResult);
                Assert.Equal(1, computations);
            }
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.AsyncLazy)]
        public void CancellationDuringInlinedComputationFromGetValueWithoutSynchronousComputationStillCachesResult()
        {
            using (new StopTheThreadPoolContext())
            {
                int computations = 0;
                var requestCancellationTokenSource = new CancellationTokenSource();

                var lazy = new AsyncLazy<object>(c =>
                {
                    Interlocked.Increment(ref computations);

                    // We do not want to ever use the cancellation token that we are passed to this
                    // computation. Rather, we will ignore it but cancel any request that is
                    // outstanding.
                    requestCancellationTokenSource.Cancel();

                    return Task.FromResult(new object());
                }, cacheResult: true);

                // Do a first request. Even though we will get a cancellation during the evaluation,
                // since we handed a result back, that result must be cached.
                var firstRequestResult = lazy.GetValue(requestCancellationTokenSource.Token);

                // And a second request. We'll let this one complete normally.
                var secondRequestResult = lazy.GetValue(CancellationToken.None);

                // We should have gotten the same cached result, and we should have only computed once.
                Assert.Same(secondRequestResult, firstRequestResult);
                Assert.Equal(1, computations);
            }
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.AsyncLazy)]
        public void SynchronousRequestShouldCacheValueWithSynchronousComputeFunction()
        {
            var lazy = new AsyncLazy<object>(c => { throw new Exception("The asynchronous compute function should never be called."); }, c => new object(), cacheResult: true);

            var firstRequestResult = lazy.GetValue(CancellationToken.None);
            var secondRequestResult = lazy.GetValue(CancellationToken.None);

            Assert.Same(secondRequestResult, firstRequestResult);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.AsyncLazy)]
        public void SynchronousRequestShouldCacheValueWithAsynchronousComputeFunction()
        {
            var lazy = new AsyncLazy<object>(c => Task.FromResult(new object()), cacheResult: true);

            var firstRequestResult = lazy.GetValue(CancellationToken.None);
            var secondRequestResult = lazy.GetValue(CancellationToken.None);

            Assert.Same(secondRequestResult, firstRequestResult);
        }
    }
}
