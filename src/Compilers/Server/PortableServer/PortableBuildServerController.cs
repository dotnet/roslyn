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
    internal sealed class PortableBuildServerController : BuildServerController
    {
        // As a proof of concept implementation the portable code is using TCP as a communication
        // mechanism.  This will eventually switch to domain sockets or named pipes
        // https://github.com/dotnet/roslyn/issues/9696

        internal const int DefaultPort = 4242;
        internal const string DefaultAddress = "127.0.0.1";

        protected override async Task<Stream> ConnectForShutdownAsync(string pipeName, int timeout)
        {
            var port = int.Parse(pipeName);
            var ipAddress = IPAddress.Parse(DefaultAddress);
            var client = new TcpClient();
            await client.ConnectAsync(ipAddress, port).ConfigureAwait(false);
            return client.GetStream();
        }

        protected override IClientConnectionHost CreateClientConnectionHost(string pipeName)
        {
            var port = int.Parse(pipeName);
            var ipAddress = IPAddress.Parse(DefaultAddress);
            var endPoint = new IPEndPoint(ipAddress, port: port);
            var clientDirectory = AppContext.BaseDirectory;
            var compilerHost = new CoreClrCompilerServerHost(clientDirectory);
            var connectionHost = new TcpClientConnectionHost(compilerHost, endPoint);
            return connectionHost;
        }

        protected internal override TimeSpan? GetKeepAliveTimeout()
        {
            return null;
        }

        protected override string GetDefaultPipeName()
        {
            return $"{DefaultPort}";
        }
    }
}
