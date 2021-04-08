// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
#if DEBUG
using System.Diagnostics;
#endif
using System.Threading;

namespace Microsoft.VisualStudio.Threading.Tasks
{
    /// <summary>
    /// Produces a series of <see cref="CancellationToken"/> objects such that requesting a new token
    /// causes the previously issued token to be cancelled.
    /// </summary>
    /// <remarks>
    /// <para>Consuming code is responsible for managing overlapping asynchronous operations.</para>
    /// <para>This class has a lock-free implementation to minimise latency and contention.</para>
    /// </remarks>
    internal sealed class CancellationSeries : IDisposable
    {
        private CancellationTokenSource? _cts = new();

        private readonly CancellationToken _superToken;

        /// <summary>
        /// Initializes a new instance of <see cref="CancellationSeries"/>.
        /// </summary>
        /// <param name="token">An optional cancellation token that, when cancelled, cancels the last
        /// issued token and causes any subsequent tokens to be issued in a cancelled state.</param>
        public CancellationSeries(CancellationToken token = default)
        {
            _superToken = token;

#if DEBUG
            _ctorStack = new StackTrace();
#endif
        }

#if DEBUG
        private readonly StackTrace _ctorStack;

        ~CancellationSeries()
        {
            Debug.Assert(
                Environment.HasShutdownStarted || _cts == null,
                "Instance of CancellationSeries not disposed before being finalized",
                "Stack at construction:{0}{1}",
                Environment.NewLine,
                _ctorStack);
        }
#endif

        /// <summary>
        /// Creates the next <see cref="CancellationToken"/> in the series, ensuring the last issued
        /// token (if any) is cancelled first.
        /// </summary>
        /// <param name="token">An optional cancellation token that, when cancelled, cancels the
        /// returned token.</param>
        /// <returns>
        /// A cancellation token that will be cancelled when either:
        /// <list type="bullet">
        /// <item><see cref="CreateNext"/> is called again</item>
        /// <item>The token passed to this method (if any) is cancelled</item>
        /// <item>The token passed to the constructor (if any) is cancelled</item>
        /// <item><see cref="Dispose"/> is called</item>
        /// </list>
        /// </returns>
        /// <exception cref="ObjectDisposedException">This object has been disposed.</exception>
        public CancellationToken CreateNext(CancellationToken token = default)
        {
            var nextSource = CancellationTokenSource.CreateLinkedTokenSource(token, _superToken);

            // Obtain the token before exchange, as otherwise the CTS may be cancelled before
            // we request the Token, which will result in an ObjectDisposedException.
            // This way we would return a cancelled token, which is reasonable.
            CancellationToken nextToken = nextSource.Token;

            CancellationTokenSource? priorSource = Interlocked.Exchange(ref _cts, nextSource);

            if (priorSource == null)
            {
                nextSource.Dispose();

                throw new ObjectDisposedException(nameof(CancellationSeries));
            }

            try
            {
                priorSource.Cancel();
            }
            finally
            {
                // A registered action on the token may throw, which would surface here.
                // Ensure we always dispose the prior CTS.
                priorSource.Dispose();
            }

            return nextToken;
        }

        public void Dispose()
        {
#if DEBUG
            GC.SuppressFinalize(this);
#endif

            CancellationTokenSource? source = Interlocked.Exchange(ref _cts, null);

            if (source == null)
            {
                // Already disposed
                return;
            }

            try
            {
                source.Cancel();
            }
            finally
            {
                source.Dispose();
            }
        }
    }
}
