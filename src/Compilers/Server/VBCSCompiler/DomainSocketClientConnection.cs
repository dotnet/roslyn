// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Utilities;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CommandLine;
using System.Security.AccessControl;
using System.Net.Sockets;

namespace Microsoft.CodeAnalysis.CompilerServer
{
    internal sealed class DomainSocketClientConnectionHost : IClientConnectionHost
    {
        ICompilerServerHost _compilerServerHost;
        UnixDomainSocket _serverSocket;
        private int _loggingIdentifier;

        internal DomainSocketClientConnectionHost(ICompilerServerHost compilerServerHost, string pipeName)
        {
            _compilerServerHost = compilerServerHost;
            _serverSocket = UnixDomainSocket.CreateServer(pipeName);
        }

        public async Task<IClientConnection> CreateListenTask(CancellationToken cancellationToken)
        {
            var client = await _serverSocket.WaitOne().ConfigureAwait(false);
            return new DomainSocketClientConnection(_compilerServerHost, _loggingIdentifier++.ToString(), client);
        }
    }

    internal sealed class DomainSocketClientConnection : ClientConnection
    {
        private readonly NetworkStream _stream;

        public DomainSocketClientConnection(ICompilerServerHost compilerServerHost, string loggingIdentifier, NetworkStream stream)
            : base(compilerServerHost, loggingIdentifier, stream)
        {
            _stream = stream;
        }

        public override void Close()
        {
            _stream.Dispose();
        }

        protected override Task CreateMonitorDisconnectTask(CancellationToken cancellationToken)
        {
            return Task.Delay(TimeSpan.FromMilliseconds(-1), cancellationToken);
        }
    }
}
