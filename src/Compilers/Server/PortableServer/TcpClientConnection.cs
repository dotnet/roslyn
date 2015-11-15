// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CommandLine;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CompilerServer
{
    internal sealed class TcpClientConnectionHost : IClientConnectionHost
    {
        private readonly ICompilerServerHost _compilerServerHost;
        private readonly TcpListener _listener;
        private int _connectionCount;

        internal TcpClientConnectionHost(ICompilerServerHost compilerServerHost, IPEndPoint endPoint)
        {
            _compilerServerHost = compilerServerHost;
            _listener = new TcpListener(endPoint);
            _listener.Start();
        }

        public async Task<IClientConnection> CreateListenTask(CancellationToken cancellationToken)
        {
            var tcpClient = await _listener.AcceptTcpClientAsync().ConfigureAwait(true);
            return new TcpClientConnection(_compilerServerHost, tcpClient, _connectionCount++.ToString());
        }

        private sealed class TcpClientConnection : ClientConnection
        {
            private readonly TcpClient _client;

            internal TcpClientConnection(ICompilerServerHost compilerServerHost, TcpClient client, string loggingIdentifier) : base(compilerServerHost, loggingIdentifier, client.GetStream())
            {
                _client = client;
            }

            public override void Close()
            {
                _client.Dispose();
            }

            protected override Task CreateMonitorDisconnectTask(CancellationToken cancellationToken)
            {
                return Task.Delay(-1, cancellationToken);
            }
        }
    }
}

