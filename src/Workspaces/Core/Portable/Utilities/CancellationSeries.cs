// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// NOTE: This code is derived from an implementation originally in dotnet/project-system:
// https://github.com/dotnet/project-system/blob/bdf69d5420ec8d894f5bf4c3d4692900b7f2479c/src/Microsoft.VisualStudio.ProjectSystem.Managed/Threading/Tasks/CancellationSeries.cs
//
// See the commentary in https://github.com/dotnet/roslyn/pull/50156 for notes on incorporating changes made to the
// reference implementation.

using System;
using System.Threading;

namespace Roslyn.Utilities;

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
    private CancellationTokenSource? _cts;

    private readonly CancellationToken _superToken;

    /// <summary>
    /// Initializes a new instance of <see cref="CancellationSeries"/>.
    /// </summary>
    /// <param name="token">An optional cancellation token that, when cancelled, cancels the last
    /// issued token and causes any subsequent tokens to be issued in a cancelled state.</param>
    public CancellationSeries(CancellationToken token = default)
    {
        // Initialize with a pre-cancelled source to ensure HasActiveToken has the correct state
        _cts = new CancellationTokenSource();
        _cts.Cancel();

        _superToken = token;
    }

    /// <summary>
    /// Determines if the cancellation series has an active token which has not been cancelled.
    /// </summary>
    public bool HasActiveToken
        => _cts is { IsCancellationRequested: false };

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
        var nextToken = nextSource.Token;

        // The following block is identical to Interlocked.Exchange, except no replacement is made if the current
        // field value is null (latch on null). This ensures state is not corrupted if CreateNext is called after
        // the object is disposed.
        var priorSource = Volatile.Read(ref _cts);
        while (priorSource is not null)
        {
            var candidate = Interlocked.CompareExchange(ref _cts, nextSource, priorSource);

            if (candidate == priorSource)
            {
                break;
            }
            else
            {
                priorSource = candidate;
            }
        }

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
        var source = Interlocked.Exchange(ref _cts, null);

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
