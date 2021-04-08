// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Linq;
using System.Threading;
using Xunit;

namespace Microsoft.VisualStudio.Threading.Tasks
{
    public sealed class CancellationSeriesTests
    {
        [Fact]
        public void CreateNext_ReturnsNonCancelledToken()
        {
            using var series = new CancellationSeries();
            var token = series.CreateNext();

            Assert.False(token.IsCancellationRequested);
            Assert.True(token.CanBeCanceled);
        }

        [Fact]
        public void CreateNext_CancelsPreviousToken()
        {
            using var series = new CancellationSeries();
            var token1 = series.CreateNext();

            Assert.False(token1.IsCancellationRequested);

            var token2 = series.CreateNext();

            Assert.True(token1.IsCancellationRequested);
            Assert.False(token2.IsCancellationRequested);

            var token3 = series.CreateNext();

            Assert.True(token2.IsCancellationRequested);
            Assert.False(token3.IsCancellationRequested);
        }

        [Fact]
        public void CreateNext_ThrowsIfDisposed()
        {
            var series = new CancellationSeries();

            series.Dispose();

            Assert.Throws<ObjectDisposedException>(() => series.CreateNext());
        }

        [Fact]
        public void CreateNext_ReturnsCancelledTokenIfSuperTokenAlreadyCancelled()
        {
            var cts = new CancellationTokenSource();

            using var series = new CancellationSeries(cts.Token);
            cts.Cancel();

            var token = series.CreateNext();

            Assert.True(token.IsCancellationRequested);
        }

        [Fact]
        public void CreateNext_ReturnsCancelledTokenIfInputTokenAlreadyCancelled()
        {
            var cts = new CancellationTokenSource();

            using var series = new CancellationSeries();
            cts.Cancel();

            var token = series.CreateNext(cts.Token);

            Assert.True(token.IsCancellationRequested);
        }

        [Fact]
        public void CancellingSuperTokenCancelsIssuedToken()
        {
            var cts = new CancellationTokenSource();

            using var series = new CancellationSeries(cts.Token);
            var token = series.CreateNext();

            Assert.False(token.IsCancellationRequested);

            cts.Cancel();

            Assert.True(token.IsCancellationRequested);
        }

        [Fact]
        public void CancellingInputTokenCancelsIssuedToken()
        {
            var cts = new CancellationTokenSource();

            using var series = new CancellationSeries();
            var token = series.CreateNext(cts.Token);

            Assert.False(token.IsCancellationRequested);

            cts.Cancel();

            Assert.True(token.IsCancellationRequested);
        }

        [Fact]
        public void CreateNext_HandlesExceptionsFromPreviousTokenRegistration()
        {
            using var series = new CancellationSeries();
            var token1 = series.CreateNext();

            var exception = new Exception();

            token1.Register(() => throw exception);

            var aggregateException = Assert.Throws<AggregateException>(() => series.CreateNext());

            Assert.Same(exception, aggregateException.InnerExceptions.Single());
            Assert.True(token1.IsCancellationRequested);
        }
    }
}
