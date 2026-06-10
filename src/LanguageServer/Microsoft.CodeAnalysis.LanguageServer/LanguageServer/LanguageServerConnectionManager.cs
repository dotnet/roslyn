// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.LanguageServer.LanguageServer;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.VisualStudio.Composition;
using Roslyn.Utilities;

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
        var entry = new ServerEntry();

        lock (_gate)
        {
            _servers = _servers.Add(entry);
        }

        try
        {
            var server = new LanguageServerHost(inputStream, outputStream, exportProvider, typeRefResolver);
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

    public ImmutableArray<LanguageServerHost> GetStartedServers()
    {
        lock (_gate)
        {
            var builder = ImmutableArray.CreateBuilder<LanguageServerHost>();

            return _servers.Where(entry => entry.Server is { HasStarted: true }).SelectAsArray(entry => entry.Server!);
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

                exitTask = _servers[0].Exited.Task;
            }

            await exitTask.ConfigureAwait(false);
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