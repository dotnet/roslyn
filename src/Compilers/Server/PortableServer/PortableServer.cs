// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CompilerServer
{
    internal sealed class PortableServer
    {
        private readonly CoreClrCompilerServerHost _compilerServerHost;
        private readonly CompilerRunHandler _compilerRunHandler;

        internal PortableServer(string clientDirectory)
        {
            _compilerServerHost = new CoreClrCompilerServerHost();
            _compilerRunHandler = new CompilerRunHandler(_compilerServerHost, clientDirectory);
        }

        internal async Task Go()
        {
            var ipAddress = IPAddress.Parse("127.0.0.1");
            var listener = new TcpListener(ipAddress, port: 12000);
            listener.Start();

            var list = new List<Task>();
            while (true)
            {
                var client = await listener.AcceptTcpClientAsync().ConfigureAwait(true);
                Console.WriteLine("Got a new connection");
                list.Add(RunCompilation(client, CancellationToken.None));
            }
        }

        private async Task RunCompilation(TcpClient client, CancellationToken cancellationToken)
        {
            var stream = client.GetStream();
            var buildRequest = await BuildRequest.ReadAsync(stream, cancellationToken).ConfigureAwait(true);
            var buildResponse = _compilerRunHandler.HandleRequest(buildRequest, cancellationToken);
            await buildResponse.WriteAsync(stream, cancellationToken).ConfigureAwait(true);
        }
    }
}
