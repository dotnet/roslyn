// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Microsoft.CodeAnalysis.LanguageServer;

/// <summary>
/// Tracks accepted connections and cancels <see cref="TimeoutToken"/> when the initial-connection timeout or
/// keepalive elapses with no active connections.
/// </summary>
internal sealed class ConnectionIdleTimeout : IDisposable
{
    private readonly object _gate = new();
    private readonly TimeSpan _keepAlive;
    private readonly ILogger _logger;
    private readonly CancellationTokenSource _timeoutCancellationSource = new();
    private readonly TaskCompletionSource _completionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

    // The token is used only by Task.Delay, so cancelling under _gate cannot invoke external callbacks.
    private CancellationTokenSource? _currentDelayCancellationSource;
    private int _activeConnections;
    private bool _stopped;

    public ConnectionIdleTimeout(TimeSpan initialConnectionTimeout, TimeSpan keepAlive, ILogger logger)
    {
        _keepAlive = keepAlive;
        _logger = logger;
        StartTimeout_NoLock(initialConnectionTimeout, isInitialConnectionTimeout: true);
    }

    /// <summary>
    /// Cancelled only when an idle timeout elapses. Normal shutdown through <see cref="Stop"/> does not cancel it.
    /// </summary>
    public CancellationToken TimeoutToken => _timeoutCancellationSource.Token;

    /// <summary>
    /// Completes after an idle timeout has been handled or <see cref="Stop"/> has stopped the current delay.
    /// </summary>
    public Task Completion => _completionSource.Task;

    /// <summary>
    /// Records an accepted connection and cancels the current idle delay. Returns false if an idle timeout or
    /// normal shutdown has already stopped the helper.
    /// </summary>
    public bool TryOpenConnection()
    {
        lock (_gate)
        {
            if (_stopped)
                return false;

            _activeConnections++;
            _currentDelayCancellationSource?.Cancel();
            _currentDelayCancellationSource = null;
        }

        return true;
    }

    /// <summary>
    /// Records that an accepted connection has finished. The keepalive starts when the last connection finishes.
    /// </summary>
    public void CloseConnection()
    {
        lock (_gate)
        {
            Debug.Assert(_activeConnections > 0);
            _activeConnections--;

            if (_activeConnections == 0 && !_stopped)
                StartTimeout_NoLock(_keepAlive, isInitialConnectionTimeout: false);
        }
    }

    public void Stop()
    {
        lock (_gate)
        {
            if (_stopped)
                return;

            _stopped = true;
            _currentDelayCancellationSource?.Cancel();
            _currentDelayCancellationSource = null;
        }

        _completionSource.TrySetResult();
    }

    private void StartTimeout_NoLock(TimeSpan timeout, bool isInitialConnectionTimeout)
    {
        Debug.Assert(_currentDelayCancellationSource is null);
        Debug.Assert(_activeConnections == 0);
        Debug.Assert(!_stopped);

        var delayCancellationSource = new CancellationTokenSource();
        _currentDelayCancellationSource = delayCancellationSource;
        _ = WaitForTimeoutAsync(timeout, delayCancellationSource, isInitialConnectionTimeout);
    }

    private async Task WaitForTimeoutAsync(
        TimeSpan timeout,
        CancellationTokenSource delayCancellationSource,
        bool isInitialConnectionTimeout)
    {
        try
        {
            await Task.Delay(timeout, delayCancellationSource.Token).ConfigureAwait(false);

            lock (_gate)
            {
                if (!ReferenceEquals(_currentDelayCancellationSource, delayCancellationSource))
                    return;

                Debug.Assert(_activeConnections == 0);
                Debug.Assert(!_stopped);

                _currentDelayCancellationSource = null;
                _stopped = true;
            }

            _timeoutCancellationSource.Cancel();
            _logger.LogInformation(
                isInitialConnectionTimeout
                    ? "Initial connection timeout elapsed; shutting down."
                    : "Keepalive elapsed with no active connections; shutting down.");
            _completionSource.TrySetResult();
        }
        catch (OperationCanceledException) when (delayCancellationSource.IsCancellationRequested)
        {
        }
        finally
        {
            delayCancellationSource.Dispose();
        }
    }

    public void Dispose()
    {
        Stop();
        _timeoutCancellationSource.Dispose();
    }
}