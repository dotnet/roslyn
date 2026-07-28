// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Microsoft.CodeAnalysis.LanguageServer;

/// <summary>
/// Tracks accepted connections and cancels the current <see cref="TimeoutToken"/> when the initial-connection
/// timeout or keepalive elapses with no active connections. Timeout cancellation is tentative until
/// <see cref="CommitTimeout"/> is called: a concurrently accepted connection can still open and advance to a fresh
/// timeout generation.
/// </summary>
internal sealed class ConnectionIdleTimeout : IDisposable
{
    private readonly object _gate = new();
    private readonly TimeSpan _keepAlive;
    private readonly ILogger _logger;
    private TimeoutGeneration? _currentGeneration;

    private int _activeConnections;
    private bool _stopped;

    public ConnectionIdleTimeout(TimeSpan initialConnectionTimeout, TimeSpan keepAlive, ILogger logger)
    {
        _keepAlive = keepAlive;
        _logger = logger;
        _currentGeneration = new TimeoutGeneration(initialConnectionTimeout, isInitialConnectionTimeout: true);
        StartTimeout_NoLock();
    }

    /// <summary>
    /// Cancelled when the current idle timeout elapses. A successfully accepted connection advances this to a fresh
    /// token, even if the previous token was cancelled concurrently.
    /// </summary>
    public CancellationToken TimeoutToken
    {
        get
        {
            lock (_gate)
            {
                Debug.Assert(_currentGeneration is not null);
                return _currentGeneration.Token;
            }
        }
    }

    /// <summary>
    /// Records an accepted connection, cancels the current idle delay, and advances to a fresh timeout generation.
    /// An accepted connection wins even if the previous generation's timeout elapsed concurrently.
    /// </summary>
    public void OpenConnection()
    {
        TimeoutGeneration previousGeneration;
        lock (_gate)
        {
            Debug.Assert(!_stopped);
            Debug.Assert(_currentGeneration is not null);

            previousGeneration = _currentGeneration;
            _currentGeneration = new TimeoutGeneration(_keepAlive, isInitialConnectionTimeout: false);
            _activeConnections++;
        }

        previousGeneration.Dispose();
    }

    /// <summary>
    /// Commits shutdown after the current pipe wait observes timeout cancellation.
    /// </summary>
    public void CommitTimeout()
    {
        TimeoutGeneration generation;
        bool isInitialConnectionTimeout;
        lock (_gate)
        {
            Debug.Assert(!_stopped);
            Debug.Assert(_activeConnections == 0);
            Debug.Assert(_currentGeneration is not null && _currentGeneration.Token.IsCancellationRequested);

            _stopped = true;
            generation = _currentGeneration;
            _currentGeneration = null;
            isInitialConnectionTimeout = generation.IsInitialConnectionTimeout;
        }

        generation.Dispose();
        _logger.LogInformation(
            isInitialConnectionTimeout
                ? "Initial connection timeout elapsed; shutting down."
                : "Keepalive elapsed with no active connections; shutting down.");
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
                StartTimeout_NoLock();
        }
    }

    public void Dispose()
    {
        TimeoutGeneration? generation;
        lock (_gate)
        {
            if (_stopped)
                return;

            _stopped = true;
            generation = _currentGeneration;
            _currentGeneration = null;
        }

        generation?.Dispose();
    }

    private void StartTimeout_NoLock()
    {
        Debug.Assert(_activeConnections == 0);
        Debug.Assert(!_stopped);
        var generation = _currentGeneration;
        Debug.Assert(generation is not null);

        generation.StartTimeout();
    }

    internal TestAccessor GetTestAccessor() => new(this);

    private sealed class TimeoutGeneration : IDisposable
    {
        private readonly CancellationTokenSource _cancellationSource = new();
        private readonly TimeSpan _timeout;

        public TimeoutGeneration(TimeSpan timeout, bool isInitialConnectionTimeout)
        {
            _timeout = timeout;
            Token = _cancellationSource.Token;
            IsInitialConnectionTimeout = isInitialConnectionTimeout;
        }

        public CancellationToken Token { get; }
        public bool IsInitialConnectionTimeout { get; }

        public void StartTimeout()
            => _cancellationSource.CancelAfter(_timeout);

        public void Cancel()
            => _cancellationSource.Cancel();

        public void Dispose()
            => _cancellationSource.Dispose();
    }

    internal readonly struct TestAccessor
    {
        private readonly ConnectionIdleTimeout _instance;

        internal TestAccessor(ConnectionIdleTimeout instance)
            => _instance = instance;

        internal void TriggerTimeout()
        {
            TimeoutGeneration generation;
            lock (_instance._gate)
            {
                Debug.Assert(_instance._currentGeneration is not null);
                generation = _instance._currentGeneration;
            }

            generation.Cancel();
        }
    }
}
