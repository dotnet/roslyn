// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.LanguageServer.LanguageServer;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.CodeAnalysis.LanguageServer.Logging;

internal sealed class LanguageServerConnectionManager
{
    private readonly object _gate = new();
    private ImmutableArray<ServerEntry> _servers = [];

    public LanguageServerHost CreateLanguageServerHost(
        Stream inputStream,
        Stream outputStream,
        ExportProvider exportProvider,
        AbstractTypeRefResolver typeRefResolver,
        ServerConfiguration serverConfiguration)
    {
        var entry = new ServerEntry();

        lock (_gate)
        {
            _servers = _servers.Add(entry);
        }

        try
        {
            var server = new LanguageServerHost(inputStream, outputStream, exportProvider, typeRefResolver, serverConfiguration);
            entry.Server = server;

            server.Start();

            _ = TrackServerExitAsync(entry);
            return server;
        }
        catch (Exception ex)
        {
            Unregister(entry);
            entry.Exited.TrySetException(ex);
            throw;
        }
    }

    public bool ForEachStartedServer(Func<LanguageServerHost, bool> action)
    {
        var startedServers = GetStartedServers();

        foreach (var server in startedServers)
        {
            if (!action(server))
                break;
        }

        return !startedServers.IsEmpty;
    }

    public async Task WaitForExitAsync()
    {
        while (true)
        {
            Task[] serverExitTasks;

            lock (_gate)
            {
                if (_servers.IsEmpty)
                    return;

                serverExitTasks = [.. _servers.Select(static entry => entry.Exited.Task)];
            }

            var completedTask = await Task.WhenAny(serverExitTasks).ConfigureAwait(false);
            await completedTask.ConfigureAwait(false);
        }
    }

    private async Task TrackServerExitAsync(ServerEntry entry)
    {
        Contract.ThrowIfNull(entry.Server);

        try
        {
            await entry.Server.WaitForExitAsync().ConfigureAwait(false);
            Unregister(entry);
            entry.Exited.TrySetResult();
        }
        catch (Exception ex)
        {
            Unregister(entry);
            entry.Exited.TrySetException(ex);
        }
    }

    private ImmutableArray<LanguageServerHost> GetStartedServers()
    {
        lock (_gate)
        {
            var builder = ImmutableArray.CreateBuilder<LanguageServerHost>();

            foreach (var entry in _servers)
            {
                if (entry.Server is { HasStarted: true } server)
                    builder.Add(server);
            }

            return builder.ToImmutable();
        }
    }

    private void Unregister(ServerEntry entry)
    {
        lock (_gate)
        {
            _servers = _servers.Remove(entry);
        }
    }

    private sealed class ServerEntry
    {
        public LanguageServerHost? Server;
        public TaskCompletionSource Exited { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}