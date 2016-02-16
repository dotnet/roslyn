// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CompilerServer
{
    internal sealed class PortableServerClient : ServerClient
    {
        internal static int DefaultPort = 4242;

        protected override async Task<Stream> ConnectForShutdownAsync(string pipeName, int timeout)
        {
            var port = int.Parse(pipeName);
            var ipAddress = IPAddress.Parse("127.0.0.1");
            var client = new TcpClient();
            await client.ConnectAsync(ipAddress, port).ConfigureAwait(false);
            return client.GetStream();
        }

        protected override IClientConnectionHost CreateClientConnectionHost(string pipeName)
        {
            var port = int.Parse(pipeName);
            var ipAddress = IPAddress.Parse("127.0.0.1");
            var endPoint = new IPEndPoint(ipAddress, port: port);
            var clientDirectory = AppContext.BaseDirectory;
            var compilerHost = new CoreClrCompilerServerHost(clientDirectory);
            var connectionHost = new TcpClientConnectionHost(compilerHost, endPoint);
            return connectionHost;
        }

        protected override TimeSpan? GetKeepAliveTimeout()
        {
            return null;
        }

        protected override string GetDefaultPipeName()
        {
            return $"{DefaultPort}";
        }
    }
}
