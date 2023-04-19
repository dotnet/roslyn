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
    public class AsyncLazyTests
    {
        [Fact]
        public void CancellationDuringInlinedComputationFromGetValueStillCachesResult()
        {
            CancellationDuringInlinedComputationFromGetValueOrGetValueAsyncStillCachesResultCore((lazy, ct) => lazy.GetValue(ct), includeSynchronousComputation: true);
            CancellationDuringInlinedComputationFromGetValueOrGetValueAsyncStillCachesResultCore((lazy, ct) => lazy.GetValue(ct), includeSynchronousComputation: false);
        }

        private static void CancellationDuringInlinedComputationFromGetValueOrGetValueAsyncStillCachesResultCore(Func<AsyncLazy<object>, CancellationToken, object> doGetValue, bool includeSynchronousComputation)
        {
            var computations = 0;
            var requestCancellationTokenSource = new CancellationTokenSource();
            object createdObject = null;

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

            var lazy = new AsyncLazy<object>(
                c => Task.FromResult(synchronousComputation(c)),
                includeSynchronousComputation ? synchronousComputation : null);

            var thrownException = Assert.Throws<OperationCanceledException>(() =>
                {
                    // Do a first request. Even though we will get a cancellation during the evaluation,
                    // since we handed a result back, that result must be cached.
                    doGetValue(lazy, requestCancellationTokenSource.Token);
                });

            // And a second request. We'll let this one complete normally.
            var secondRequestResult = doGetValue(lazy, CancellationToken.None);

            // We should have gotten the same cached result, and we should have only computed once.
            Assert.Same(createdObject, secondRequestResult);
            Assert.Equal(1, computations);
        }

        [Fact]
        public void SynchronousRequestShouldCacheValueWithAsynchronousComputeFunction()
        {
            var lazy = new AsyncLazy<object>(c => Task.FromResult(new object()));

            var firstRequestResult = lazy.GetValue(CancellationToken.None);
            var secondRequestResult = lazy.GetValue(CancellationToken.None);

            Assert.Same(secondRequestResult, firstRequestResult);
        }

        [Theory]
        [CombinatorialData]
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
                ? new AsyncLazy<object>(asynchronousComputeFunction)
                : new AsyncLazy<object>(asynchronousComputeFunction, synchronousComputeFunction);

            var actual = consumerAsync
                ? await Assert.ThrowsAsync<ArgumentException>(async () => await lazy.GetValueAsync(CancellationToken.None))
                : Assert.Throws<ArgumentException>(() => lazy.GetValue(CancellationToken.None));

            Assert.Same(exception, actual);
        }
    }
}
