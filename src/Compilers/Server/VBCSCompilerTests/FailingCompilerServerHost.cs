// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        public async Task<IClientConnection> ListenAsync(CancellationToken cancellationToken)
        {
            var underlyingConnection = await _underlyingHost.ListenAsync(cancellationToken);
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

            public Task<ConnectionData> HandleConnectionAsync(bool allowCompilationRequests, CancellationToken cancellationToken)
            {
                // Forcibly kill the connection
                _underlyingConnection.Close();
                return Task.FromResult(new ConnectionData(CompletionReason.ClientDisconnect));
            }
        }
    }
}
