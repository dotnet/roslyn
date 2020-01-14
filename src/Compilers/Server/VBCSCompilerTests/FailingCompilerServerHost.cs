// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CommandLine;

namespace Microsoft.CodeAnalysis.CompilerServer.UnitTests
{
    internal sealed class FailingClientConnectionHost : IClientConnectionHost
    {
        private readonly IClientConnectionHost _underlyingHost;

        public FailingClientConnectionHost(IClientConnectionHost underlyingHost)
        {
            _underlyingHost = underlyingHost;
        }

        public async Task<IClientConnection> CreateListenTask(CancellationToken cancellationToken)
        {
            var underlyingConnection = await _underlyingHost.CreateListenTask(cancellationToken);
            return new FailingClientConnection(underlyingConnection);
        }

        private class FailingClientConnection : IClientConnection
        {
            private IClientConnection _underlyingConnection;

            public FailingClientConnection(IClientConnection underlyingConnection)
            {
                _underlyingConnection = underlyingConnection;
            }

            public string LoggingIdentifier => _underlyingConnection.LoggingIdentifier;

            public void Close() => _underlyingConnection.Close();

            public Task<ConnectionData> HandleConnection(bool allowCompilationRequests, CancellationToken cancellationToken)
            {
                // Forcibly kill the connection
                _underlyingConnection.Close();
                return Task.FromResult(new ConnectionData(CompletionReason.ClientDisconnect));
            }
        }
    }
}
