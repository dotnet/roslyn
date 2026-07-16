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

        // Counts connections that have been accepted but whose LanguageServerHost is not yet registered in
        // _servers (i.e. TryStartServer has not yet run for them). Protected by _gate. The keepalive monitor
        // treats the daemon as busy while this is non-zero, preventing a spurious keepalive fire during
        // the window between accepting a client and starting its server.
        var pendingConnections = 0;

        // A newly started daemon is idle before its launching client can connect. Until the first connection is
        // accepted, use s_initialConnectionTimeout rather than the configured keepalive.
        var hasAcceptedConnection = false;

        // acceptCts stops the accept enumeration. Cancelled by:
        //   (a) the idle monitor (the initial-connection timeout or keepalive elapsed), or
        //   (b) the external cancellationToken (process shutdown).
        using var acceptCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // Explicit lifecycle: true while the accept loop should run. Set to false under _gate before
        // RunAsync returns so no keepalive state changes can be signaled after the monitor is drained.
        var accepting = true;

        // A single serialized monitor owns the keepalive timeout. State changes wake it so it can restart
        // the timeout or wait while the daemon is busy; this avoids races with queued Timer callbacks.
        using var idleStateChanged = new SemaphoreSlim(initialCount: 0);
        var keepAliveTask = MonitorKeepAliveAsync();

        try
        {
            await foreach (var connection in connectionSource.AcceptConnectionsAsync(acceptCts.Token).ConfigureAwait(false))
            {
                // Reserve a pending slot before doing any work. This prevents the idle timer from treating
                // the daemon as idle during the window between accept and TryStartServer completing.
                lock (_gate)
                {
                    hasAcceptedConnection = true;
                    pendingConnections++;
                    NotifyIdleStateChanged_NoLock();
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
                    // Single-server mode: synchronous startup (exactly one connection; no parallelism needed).
                    if (await TryStartServerAsync(connection).ConfigureAwait(false) is not { } entry)
                        continue;

                    supervisors.Add(SuperviseAsync(entry));
                }
            }
        }
        catch (OperationCanceledException) when (acceptCts.IsCancellationRequested)
        {
            // The accept loop was cancelled: either the keepalive elapsed (already logged by the monitor) or
            // we received an external shutdown request. Both are expected; swallow and proceed.
        }
        finally
        {
            // Ensure the accept loop is stopped and wake the keepalive monitor so it can observe the lifecycle
            // transition and exit before its semaphore and acceptCts are disposed.
            lock (_gate)
            {
                accepting = false;
                idleStateChanged.Release();
            }

            acceptCts.Cancel();
        }

        await keepAliveTask.ConfigureAwait(false);

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

        // Signals that a value used to determine whether the daemon is idle has changed. Must be called under
        // _gate. Once accepting is false, RunAsync may return without waiting for a single-server supervisor,
        // so do not touch the semaphore after that point.
        void NotifyIdleStateChanged_NoLock()
        {
            if (accepting)
                idleStateChanged.Release();
        }

        // Serially monitors idle state. When idle, a semaphore signal means activity changed and the timeout
        // must be reconsidered; a timeout causes shutdown only after authoritative state is rechecked under
        // _gate. Since this is the only timeout observer, there are no stale callbacks to distinguish.
        async Task MonitorKeepAliveAsync()
        {
            while (true)
            {
                // Discard notifications already represented by the state snapshot below.
                while (idleStateChanged.Wait(0, CancellationToken.None))
                {
                }

                bool isIdle;
                TimeSpan idleTimeout;
                lock (_gate)
                {
                    if (!accepting)
                        return;

                    isIdle = _servers.IsEmpty && pendingConnections == 0;
                    idleTimeout = hasAcceptedConnection ? keepAlive : s_initialConnectionTimeout;
                }

                if (!isIdle)
                {
                    await idleStateChanged.WaitAsync(CancellationToken.None).ConfigureAwait(false);
                    continue;
                }

                if (await idleStateChanged.WaitAsync(idleTimeout, CancellationToken.None).ConfigureAwait(false))
                    continue;

                // A state change can race with the timeout. Check for a queued notification and recheck state
                // under the lock before committing to shutdown. The notification is necessary even when the
                // current state is idle: the daemon may have become busy and then idle again, starting a new
                // idle interval, between the timeout and acquiring this lock.
                bool timedOutWaitingForInitialConnection;
                lock (_gate)
                {
                    if (!accepting)
                        return;

                    if (idleStateChanged.Wait(0, CancellationToken.None) || !_servers.IsEmpty || pendingConnections > 0)
                        continue;

                    timedOutWaitingForInitialConnection = !hasAcceptedConnection;
                    accepting = false;
                }

                acceptCts.Cancel();
                logger.LogInformation(
                    timedOutWaitingForInitialConnection
                        ? "Initial connection timeout elapsed; shutting down."
                        : "Keepalive elapsed with no active connections; shutting down.");
                return;
            }
        }

        async Task StartAndSuperviseAsync(LanguageServerConnection connection)
        {
            try
            {
                var entry = await TryStartServerAsync(connection).ConfigureAwait(false);
                if (entry is not null)
                    await SuperviseAsync(entry).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // This is the daemon supervisor's fault boundary. TryStartServerAsync and SuperviseAsync handle
                // their expected failures; catch anything else here so the task can be safely removed on completion.
                logger.LogError(ex, "Language server connection supervisor faulted.");
            }
        }

        // Creates, registers, and starts a language server for the connection. Returns its entry, or null
        // if the server failed to start and that failure was isolated (daemon mode).
        // Always decrements pendingConnections (the caller incremented it on accept).
        async Task<ServerEntry?> TryStartServerAsync(LanguageServerConnection connection)
        {
            var rejectConnection = false;
            lock (_gate)
            {
                if (!accepting)
                {
                    pendingConnections--;
                    rejectConnection = true;
                }
            }

            if (rejectConnection)
            {
                connection.Resource?.Dispose();
                return null;
            }

            // --- Phase 1: construct the LanguageServerHost (MEF composition happens here) ---
            LanguageServerHost server;
            try
            {
                server = new LanguageServerHost(connection.InputStream, connection.OutputStream, exportProvider, typeRefResolver);
            }
            catch (Exception ex)
            {
                lock (_gate)
                {
                    pendingConnections--;
                    NotifyIdleStateChanged_NoLock();
                }

                connection.Resource?.Dispose();

                if (!isolateFaults)
                    throw;

                logger.LogError(ex, "Failed to create a language server for the accepted connection.");
                return null;
            }

            var entry = new ServerEntry(server, connection.Resource);
            var abortStartup = false;

            // --- Phase 2: register and start ---
            // Register before starting so the keepalive monitor observes the server while it runs and
            // GetStartedServers reflects the server before its JSON-RPC listen loop is active.
            lock (_gate)
            {
                pendingConnections--;
                if (accepting)
                {
                    _servers = _servers.Add(entry);
                }
                else
                {
                    abortStartup = true;
                }

                NotifyIdleStateChanged_NoLock();
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
            catch (Exception ex)
            {
                lock (_gate)
                {
                    _servers = _servers.Remove(entry);
                    NotifyIdleStateChanged_NoLock();
                }

                await AbortServerAsync(server).ConfigureAwait(false);
                connection.Resource?.Dispose();

                if (!isolateFaults)
                    throw;

                logger.LogError(ex, "Failed to start a language server for the accepted connection.");
                return null;
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
                {
                    _servers = _servers.Remove(entry);
                    NotifyIdleStateChanged_NoLock();
                }

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
