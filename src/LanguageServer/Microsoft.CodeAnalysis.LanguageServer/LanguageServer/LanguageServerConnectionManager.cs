// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.LanguageServer.LanguageServer;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.CodeAnalysis.LanguageServer;

internal sealed class LanguageServerConnectionManager
{
    /// <summary>
    /// Time a newly started daemon waits for its first client. This prevents a short configured keepalive
    /// from shutting down the daemon between signaling readiness and accepting the client that launched it.
    /// </summary>
    private static readonly TimeSpan s_initialConnectionTimeout = TimeSpan.FromMinutes(1);

    private readonly object _gate = new();
    private ImmutableArray<ServerEntry> _servers = [];

    // Test hook: invoked just before LanguageServerHost.Start(). Throw to simulate a startup failure.
    private Action? _onBeforeStartServer;

    /// <summary>
    /// Runs an independent language server for each connection yielded by <paramref name="connectionSource"/>.
    /// A <see cref="SingleLanguageServerConnectionSource"/> yields exactly one connection and then completes, so
    /// this returns once that server exits. The daemon listener yields connections indefinitely; it shuts down when
    /// <paramref name="keepAlive"/> elapses with no active servers, or when <paramref name="cancellationToken"/>
    /// is signaled. Before the daemon accepts its first connection, it uses a separate initial-connection timeout
    /// so a short keepalive cannot race initial connection.
    /// </summary>
    public async Task RunAsync(
        ILanguageServerConnectionSource connectionSource,
        TimeSpan keepAlive,
        ExportProvider exportProvider,
        AbstractTypeRefResolver typeRefResolver,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        // For a source that isolates faults (the daemon), a server fault is logged and confined to that one
        // connection; otherwise a single misbehaving client would tear down the whole daemon.
        var isolateFaults = connectionSource.ShouldIsolateConnectionFaults;

        // All per-connection supervisors are tracked so they can be drained on graceful shutdown. Previously
        // daemon supervisors were fire-and-forgotten, which required catching ObjectDisposedException in the
        // keepalive helper; tracking them here eliminates that race.
        var supervisors = new List<Task>();

        // acceptCts stops the accept enumeration. Cancelled by:
        //   (a) the connection idle timeout (the initial-connection timeout or keepalive elapsed), or
        //   (b) the external cancellationToken (process shutdown).
        using var idleTimeout = new ConnectionIdleTimeout(s_initialConnectionTimeout, keepAlive, logger);
        using var acceptCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, idleTimeout.TimeoutToken);

        try
        {
            await foreach (var connection in connectionSource.AcceptConnectionsAsync(acceptCts.Token).ConfigureAwait(false))
            {
                // Record the accepted connection before doing any startup work so the idle timeout treats the
                // daemon as busy during server construction. A timeout may have won just before this yield.
                if (!idleTimeout.TryOpenConnection())
                {
                    connection.Resource?.Dispose();
                    continue;
                }

                if (isolateFaults)
                {
                    // Daemon mode: start server construction and supervision in a background task so this
                    // accept loop can immediately loop back to WaitForConnectionAsync for the next client,
                    // without waiting for (potentially slow) MEF composition to finish.
                    var supervisor = Task.Run(() => StartAndSuperviseAsync(connection), CancellationToken.None);
                    lock (_gate)
                        supervisors.Add(supervisor);

                    // Remove completed supervisors immediately so a long-running daemon does not retain one task
                    // per historical connection. Register after adding so even an already-completed task is removed.
                    _ = supervisor.ContinueWith(
                        RemoveCompletedSupervisor,
                        CancellationToken.None,
                        TaskContinuationOptions.ExecuteSynchronously,
                        TaskScheduler.Default);
                }
                else
                {
                    // Single-server mode: StartAndSuperviseAsync starts synchronously until it begins waiting for
                    // server exit, so no Task.Run or parallel startup is needed.
                    supervisors.Add(StartAndSuperviseAsync(connection));
                }
            }
        }
        catch (OperationCanceledException) when (acceptCts.IsCancellationRequested)
        {
            // The accept loop was cancelled: either the keepalive elapsed (already logged by the idle timeout) or
            // we received an external shutdown request. Both are expected; swallow and proceed.
        }
        finally
        {
            // Stop any idle delay and ensure the accept enumeration cannot continue after RunAsync starts draining.
            idleTimeout.Stop();
            acceptCts.Cancel();
        }

        await idleTimeout.Completion.ConfigureAwait(false);

        if (cancellationToken.IsCancellationRequested && isolateFaults)
        {
            ImmutableArray<IDisposable> connections;
            lock (_gate)
                connections = _servers.SelectAsArray(
                    static entry => entry.Connection is not null,
                    static entry => entry.Connection!);

            foreach (var connection in connections)
                connection.Dispose();
        }

        // Drain all daemon supervisors even on external shutdown so no per-server task can outlive the shared
        // export provider and logger factory owned by Program. Single-server mode preserves the prior behavior
        // of returning promptly on external cancellation when its transport is not manager-owned.
        if (!cancellationToken.IsCancellationRequested || isolateFaults)
        {
            Task[] remainingSupervisors;
            lock (_gate)
                remainingSupervisors = [.. supervisors];

            await Task.WhenAll(remainingSupervisors).ConfigureAwait(false);
        }

        void RemoveCompletedSupervisor(Task supervisor)
        {
            lock (_gate)
                supervisors.Remove(supervisor);
        }

        async Task StartAndSuperviseAsync(LanguageServerConnection connection)
        {
            try
            {
                var entry = await TryStartServerAsync(connection).ConfigureAwait(false);
                if (entry is not null)
                    await SuperviseAsync(entry).ConfigureAwait(false);
            }
            catch (Exception ex) when (isolateFaults)
            {
                // This is the daemon supervisor's startup fault boundary. TryStartServerAsync cleans up before
                // propagating failures here so one connection cannot tear down the daemon.
                logger.LogError(ex, "Language server connection supervisor faulted.");
            }
            finally
            {
                idleTimeout.CloseConnection();
            }
        }

        // Creates, registers, and starts a language server for the connection. Returns null if shutdown won the
        // race with startup; construction and startup failures are cleaned up and propagated to the caller.
        async Task<ServerEntry?> TryStartServerAsync(LanguageServerConnection connection)
        {
            // --- Phase 1: construct the LanguageServerHost (MEF composition happens here) ---
            LanguageServerHost server;
            try
            {
                server = new LanguageServerHost(connection.InputStream, connection.OutputStream, exportProvider, typeRefResolver);
            }
            catch
            {
                connection.Resource?.Dispose();
                throw;
            }

            var entry = new ServerEntry(server, connection.Resource);
            var abortStartup = false;

            // --- Phase 2: register and start ---
            // Register before starting so GetStartedServers reflects the server before its JSON-RPC listen loop
            // is active.
            lock (_gate)
            {
                if (!acceptCts.IsCancellationRequested)
                {
                    _servers = _servers.Add(entry);
                }
                else
                {
                    abortStartup = true;
                }
            }

            if (abortStartup)
            {
                await AbortServerAsync(server).ConfigureAwait(false);
                connection.Resource?.Dispose();
                return null;
            }

            try
            {
                _onBeforeStartServer?.Invoke();
                server.Start();
            }
            catch
            {
                lock (_gate)
                    _servers = _servers.Remove(entry);

                await AbortServerAsync(server).ConfigureAwait(false);
                connection.Resource?.Dispose();
                throw;
            }

            return entry;

            async Task AbortServerAsync(LanguageServerHost server)
            {
                try
                {
                    await server.AbortAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to clean up a language server after startup was aborted.");
                }
            }
        }

        // Awaits a server's exit, then unregisters it and disposes its connection. In isolated (daemon)
        // mode a fault is observed and logged; otherwise it propagates so Task.WhenAll above re-raises it.
        async Task SuperviseAsync(ServerEntry entry)
        {
            try
            {
                // Wait until the server exits. We specifically do not also wait on the JsonRpc completion; the
                // server exiting (via an explicit 'exit' or an observed disconnect) is the only signal we need.
                await entry.Server.WaitForExitAsync().ConfigureAwait(false);
            }
            catch (Exception ex) when (isolateFaults)
            {
                logger.LogError(ex, "Language server connection faulted; tearing down that connection.");
            }
            finally
            {
                lock (_gate)
                    _servers = _servers.Remove(entry);

                // Dispose this connection's transport (e.g. the daemon's NamedPipeServerStream) now that its
                // server has fully exited. Disposal is idempotent, so it is safe even if transport already closed.
                entry.Connection?.Dispose();
            }
        }
    }

    public ImmutableArray<LanguageServerHost> GetStartedServers()
    {
        lock (_gate)
        {
            return _servers.SelectAsArray(entry => entry.Server.HasStarted, entry => entry.Server);
        }
    }

    internal TestAccessor GetTestAccessor() => new(this);

    internal readonly struct TestAccessor
    {
        private readonly LanguageServerConnectionManager _instance;

        internal TestAccessor(LanguageServerConnectionManager instance) => _instance = instance;

        /// <summary>
        /// When set, invoked just before each <see cref="LanguageServerHost.Start"/> call. Throw from
        /// this delegate to simulate a startup failure (for daemon-mode fault-isolation tests).
        /// </summary>
        internal Action? OnBeforeStartServer
        {
            set => _instance._onBeforeStartServer = value;
        }
    }

    private sealed class ServerEntry(LanguageServerHost server, IDisposable? connection)
    {
        public LanguageServerHost Server { get; } = server;
        public IDisposable? Connection { get; } = connection;
    }
}
