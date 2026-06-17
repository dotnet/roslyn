// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.LanguageServer.LanguageServer;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.CodeAnalysis.LanguageServer;

internal sealed class LanguageServerConnectionManager
{
    private readonly object _gate = new();
    private ImmutableArray<ServerEntry> _servers = [];

    public LanguageServerHost CreateLanguageServerHost(
        Stream inputStream,
        Stream outputStream,
        ExportProvider exportProvider,
        AbstractTypeRefResolver typeRefResolver)
    {
        var server = new LanguageServerHost(inputStream, outputStream, exportProvider, typeRefResolver);
        var entry = new ServerEntry(
            server,
            server.WaitForExitAsync().ContinueWith(
                static (task, state) =>
                {
                    var (connectionManager, server) = ((LanguageServerConnectionManager, LanguageServerHost))state!;
                    connectionManager.Unregister(server);
                    task.GetAwaiter().GetResult();
                },
                (this, server),
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.RunContinuationsAsynchronously,
                TaskScheduler.Default));

        lock (_gate)
        {
            _servers = _servers.Add(entry);
        }

        server.Start();
        return server;
    }

    public ImmutableArray<LanguageServerHost> GetStartedServers()
    {
        lock (_gate)
        {
            return _servers.SelectAsArray(entry => entry.Server.HasStarted, entry => entry.Server);
        }
    }

    public async Task WaitForExitAsync()
    {
        while (true)
        {
            Task exitTask;

            lock (_gate)
            {
                if (_servers.IsEmpty)
                    return;

                exitTask = _servers[0].ExitTask;
            }

            await exitTask.ConfigureAwait(false);
        }
    }

    private void Unregister(LanguageServerHost server)
    {
        lock (_gate)
        {
            _servers = _servers.RemoveAll(entry => entry.Server == server);
        }
    }

    private sealed class ServerEntry
    {
        public LanguageServerHost Server { get; }
        public Task ExitTask { get; }

        public ServerEntry(LanguageServerHost server, Task exitTask)
        {
            Server = server;
            ExitTask = exitTask;
        }
    }
}