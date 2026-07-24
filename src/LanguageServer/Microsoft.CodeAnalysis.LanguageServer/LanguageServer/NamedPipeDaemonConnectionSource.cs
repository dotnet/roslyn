// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

extern alias MSBuildWorkspaces;

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis.LanguageServer.Daemon;
using Microsoft.Extensions.Logging;

// Reuse the compiler server's named-pipe helper (Asynchronous | WriteThrough | CurrentUserOnly,
// MaxAllowedServerInstances, and Unix /tmp socket-path handling). It is source-linked into
// Microsoft.CodeAnalysis.Workspaces.MSBuild, which this project already references under the
// MSBuildWorkspaces alias, so we use that already-compiled copy rather than source-linking another
// copy into this assembly (which would collide with the MSBuild build host's copy of the same type).
using NamedPipeUtil = MSBuildWorkspaces::Microsoft.CodeAnalysis.NamedPipeUtil;

namespace Microsoft.CodeAnalysis.LanguageServer;

/// <summary>
/// A connection source for daemon mode: owns the server mutex (which signals "a daemon is running" for
/// this pipe) and accepts client connections on a named pipe, handing each a dedicated, independent
/// <see cref="System.IO.Pipes.NamedPipeServerStream"/>.
/// </summary>
internal sealed class NamedPipeDaemonConnectionSource : ILanguageServerConnectionSource, IDisposable
{
    private static readonly TimeSpan s_initialConnectionTimeout = TimeSpan.FromMinutes(1);

    private readonly string _pipeName;
    private readonly ILogger _logger;
    private readonly Mutex _serverMutex;
    private readonly ConnectionIdleTimeout _idleTimeout;

    private Action? _onConnectionAccepted;

    private NamedPipeDaemonConnectionSource(
        string pipeName,
        Mutex serverMutex,
        TimeSpan initialConnectionTimeout,
        TimeSpan keepAlive,
        ILogger logger)
    {
        _pipeName = pipeName;
        _serverMutex = serverMutex;
        _idleTimeout = new ConnectionIdleTimeout(initialConnectionTimeout, keepAlive, logger);
        _logger = logger;
    }

    public bool ShouldIsolateConnectionFaults => true;

    /// <summary>
    /// Attempts to become the daemon for <paramref name="pipeName"/> by acquiring the server mutex.
    /// Returns <see langword="false"/> (without creating a source) if another daemon already owns it.
    /// </summary>
    public static bool TryCreate(
        string pipeName,
        TimeSpan keepAlive,
        ILogger logger,
        [NotNullWhen(true)] out NamedPipeDaemonConnectionSource? source,
        TimeSpan? initialConnectionTimeout = null)
    {
        if (!DaemonServerMutex.TryAcquire(pipeName, out var serverMutex))
        {
            logger.LogWarning(
                "A language server daemon already owns pipe '{pipeName}'; this instance will exit so clients use the existing daemon.",
                pipeName);
            source = null;
            return false;
        }

        source = new NamedPipeDaemonConnectionSource(
            pipeName, serverMutex, initialConnectionTimeout ?? s_initialConnectionTimeout, keepAlive, logger);
        return true;
    }

    public async IAsyncEnumerable<LanguageServerConnection> AcceptConnectionsAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var timeoutToken = _idleTimeout.TimeoutToken;
            var pipeStream = NamedPipeUtil.CreateServer(_pipeName);

            // Wait for a client (outside any 'yield return', which C# disallows inside a try/catch). On success
            // the stream's ownership passes to the yielded connection; on failure we dispose it here.
            try
            {
                using var acceptCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutToken);
                await pipeStream.WaitForConnectionAsync(acceptCancellationSource.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                await pipeStream.DisposeAsync().ConfigureAwait(false);
                throw;
            }
            catch (OperationCanceledException) when (timeoutToken.IsCancellationRequested)
            {
                await pipeStream.DisposeAsync().ConfigureAwait(false);
                _idleTimeout.CommitTimeout();
                yield break;
            }
            catch (Exception ex)
            {
                // Failing to accept one connection shouldn't take down the daemon; log and try again.
                _logger.LogError(ex, "Daemon encountered an error while waiting for a client connection.");
                await pipeStream.DisposeAsync().ConfigureAwait(false);
                continue;
            }

            _onConnectionAccepted?.Invoke();
            _idleTimeout.OpenConnection();
            _logger.LogInformation("Daemon accepted a new client connection.");

            // The accepted stream is both input and output, and is disposed when its language server exits.
            yield return new LanguageServerConnection(pipeStream, pipeStream, new ConnectionResource(pipeStream, this));
        }
    }

    internal TestAccessor GetTestAccessor() => new(this);

    internal readonly struct TestAccessor
    {
        private readonly NamedPipeDaemonConnectionSource _instance;

        internal TestAccessor(NamedPipeDaemonConnectionSource instance) => _instance = instance;

        internal bool HasTimedOut => _instance._idleTimeout.TimeoutToken.IsCancellationRequested;

        internal void TriggerTimeout() => _instance._idleTimeout.GetTestAccessor().TriggerTimeout();

        internal Action? OnConnectionAccepted
        {
            set => _instance._onConnectionAccepted = value;
        }
    }

    private sealed class ConnectionResource(IDisposable resource, NamedPipeDaemonConnectionSource source) : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;

            try
            {
                resource.Dispose();
            }
            finally
            {
                source._idleTimeout.CloseConnection();
            }
        }
    }

    public void Dispose()
    {
        _idleTimeout.Dispose();
        _serverMutex.Dispose();
    }
}
