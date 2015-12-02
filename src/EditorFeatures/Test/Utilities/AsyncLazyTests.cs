// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public partial class AsyncLazyTests
    {
        [WpfFact, Trait(Traits.Feature, Traits.Features.AsyncLazy)]
        public void CancellationDuringInlinedComputationFromGetValueStillCachesResult()
        {
            CancellationDuringInlinedComputationFromGetValueOrGetValueAsyncStillCachesResultCore((lazy, ct) => lazy.GetValue(ct), includeSynchronousComputation: true);
            CancellationDuringInlinedComputationFromGetValueOrGetValueAsyncStillCachesResultCore((lazy, ct) => lazy.GetValue(ct), includeSynchronousComputation: false);
        }

        private static void CancellationDuringInlinedComputationFromGetValueOrGetValueAsyncStillCachesResultCore(Func<AsyncLazy<object>, CancellationToken, object> doGetValue, bool includeSynchronousComputation)
        {
            int computations = 0;
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
                includeSynchronousComputation ? synchronousComputation : null,
                cacheResult: true);

            var thrownException = Roslyn.Test.Utilities.AssertEx.Throws<Exception>(() =>
                {
                    // Do a first request. Even though we will get a cancellation during the evaluation,
                    // since we handed a result back, that result must be cached.
                    doGetValue(lazy, requestCancellationTokenSource.Token);
                }, allowDerived: true);

            // Assert it's either cancellation or aggregate exception
            Assert.True(thrownException is OperationCanceledException || ((AggregateException)thrownException).Flatten().InnerException is OperationCanceledException);

            // And a second request. We'll let this one complete normally.
            var secondRequestResult = doGetValue(lazy, CancellationToken.None);

            // We should have gotten the same cached result, and we should have only computed once.
            Assert.Same(createdObject, secondRequestResult);
            Assert.Equal(1, computations);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AsyncLazy)]
        public void SynchronousRequestShouldCacheValueWithAsynchronousComputeFunction()
        {
            var lazy = new AsyncLazy<object>(c => Task.FromResult(new object()), cacheResult: true);

            var firstRequestResult = lazy.GetValue(CancellationToken.None);
            var secondRequestResult = lazy.GetValue(CancellationToken.None);

            Assert.Same(secondRequestResult, firstRequestResult);
        }
    }
}