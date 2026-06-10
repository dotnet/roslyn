// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.LanguageServer.LanguageServer;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.CodeAnalysis.LanguageServer;

internal sealed class LanguageServerConnectionManager
{
    private readonly object _gate = new();
    private ImmutableArray<ServerEntry> _servers = [];

    /// <summary>
    /// Runs an independent language server for each connection yielded by <paramref name="connectionSource"/>.
    /// A finite source (single-server mode) yields exactly one connection and then completes, so this returns once that server exits.
    /// The daemon listener yields connections indefinitely; it shuts down when <paramref name="keepAlive"/> elapses with no active servers, or when
    /// <paramref name="cancellationToken"/> is signaled.
    /// </summary>
    public async Task RunAsync(
        ILanguageServerConnectionSource connectionSource,
        TimeSpan keepAlive,
        ExportProvider exportProvider,
        AbstractTypeRefResolver typeRefResolver,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        // idleCts cancels the accept enumeration. While the source has a finite keepalive (the daemon), the idle
        // timer is "armed" (CancelAfter(keepAlive)) whenever no servers are active and "disarmed"
        // (CancelAfter(Infinite)) once one starts, so an idle daemon shuts itself down. Single-server mode uses an
        // infinite keepalive, so the timer is never armed and the loop ends only when the source is exhausted.
        using var idleCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // For a source that isolates faults (the daemon), a server fault is logged and confined to that one
        // connection; otherwise a single misbehaving client would tear down the whole daemon. Single-server mode
        // instead lets the fault surface out of this method as a process crash for telemetry/dump collection.
        var isolateFaults = connectionSource.IsolateConnectionFaults;

        // In single-server mode the per-server supervisor is collected here and drained at the end so an
        // unexpected fault surfaces to the caller. Daemon supervisors isolate their own faults, so they run
        // fire-and-forget instead.
        var supervisors = new List<Task>();

        // The daemon starts idle; arm the keepalive so a daemon that never sees a client still shuts itself down.
        lock (_gate)
            RefreshKeepAlive_NoLock();

        try
        {
            await foreach (var connection in connectionSource.AcceptConnectionsAsync(idleCts.Token).ConfigureAwait(false))
            {
                if (TryStartServer(connection) is not { } entry)
                    continue;

                if (isolateFaults)
                {
                    // Daemon mode: the supervisor logs/isolates its own faults, so it is safe to fire-and-forget.
                    _ = SuperviseAsync(entry);
                }
                else
                {
                    // Single-server mode: observe the exit so an unexpected fault surfaces to the caller below.
                    supervisors.Add(SuperviseAsync(entry));
                }
            }
        }
        catch (OperationCanceledException)
        {
            // The accept loop was cancelled: either the keepalive elapsed while idle, or we were asked to shut down.
            if (!cancellationToken.IsCancellationRequested && keepAlive != Timeout.InfiniteTimeSpan)
                logger.LogInformation("Keepalive elapsed with no active connections; shutting down.");
        }

        // Surface an unexpected server fault to the caller (single-server mode; isolated supervisors never fault).
        // By this point a normal exhaustion/keepalive exit has already drained all servers, so this completes
        // promptly. Skipped on external cancellation, where each server tears down on its own as its transport closes.
        if (!cancellationToken.IsCancellationRequested)
            await Task.WhenAll(supervisors).ConfigureAwait(false);

        // Creates, registers, and starts a language server for the connection. Returns its entry, or null if the
        // server failed to start and that failure was isolated (daemon mode).
        ServerEntry? TryStartServer(LanguageServerConnection connection)
        {
            var server = new LanguageServerHost(connection.InputStream, connection.OutputStream, exportProvider, typeRefResolver);
            var entry = new ServerEntry(server, connection.Resource);

            // Register before starting so the server is observable (e.g. to the global logger) and the idle
            // keepalive is disarmed while it runs.
            lock (_gate)
            {
                _servers = _servers.Add(entry);
                RefreshKeepAlive_NoLock();
            }

            try
            {
                server.Start();
            }
            catch (Exception ex)
            {
                // The server failed to start: roll back the registration so the daemon doesn't appear busy and the
                // keepalive re-arms if this leaves us idle.
                lock (_gate)
                {
                    _servers = _servers.Remove(entry);
                    RefreshKeepAlive_NoLock();
                }

                if (!isolateFaults)
                    throw;

                // Isolated (daemon) mode: a single server failing to start must not take down the daemon.
                logger.LogError(ex, "Failed to start a language server for the accepted connection.");
                connection.Resource?.Dispose();
                return null;
            }

            return entry;
        }

        // Awaits a server's exit, then unregisters it and disposes its connection. In isolated (daemon) mode a
        // fault is observed and logged; otherwise it propagates so Task.WhenAll above re-raises it to the caller.
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
                    RefreshKeepAlive_NoLock();
                }

                // Dispose this connection's transport (e.g. the daemon's NamedPipeServerStream) now that its server
                // has fully exited. Disposal is idempotent, so it's safe even if the transport already closed.
                entry.Connection?.Dispose();
            }
        }

        // Arms the idle keepalive timer when no servers are active, and disarms it (infinite due time) otherwise.
        // No-op for an infinite keepalive (single-server mode). Must be called under <see cref="_gate"/>.
        void RefreshKeepAlive_NoLock()
        {
            if (keepAlive == Timeout.InfiniteTimeSpan)
                return;

            try
            {
                idleCts.CancelAfter(_servers.IsEmpty ? keepAlive : Timeout.InfiniteTimeSpan);
            }
            catch (ObjectDisposedException)
            {
                // RunAsync has already finished and disposed idleCts (a fire-and-forget daemon supervisor can run
                // its cleanup after an external shutdown); there is no timer left to (re)schedule.
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

    private sealed class ServerEntry(LanguageServerHost server, IDisposable? connection)
    {
        public LanguageServerHost Server { get; } = server;
        public IDisposable? Connection { get; } = connection;
    }
}
