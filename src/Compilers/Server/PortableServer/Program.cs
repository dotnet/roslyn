// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.BuildTasks;
using System.Net.Sockets;
using System.Net;

namespace Microsoft.CodeAnalysis.CompilerServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Go().GetAwaiter().GetResult();
        }

        private static async Task Go()
        {
            var ipAddress = (await Dns.GetHostAddressesAsync("localhost").ConfigureAwait(true))[0];
            var listener = new TcpListener(ipAddress, port: 12000);
            while (true)
            {
                var client = await listener.AcceptTcpClientAsync().ConfigureAwait(true);
                await RunCompilation(client, CancellationToken.None).ConfigureAwait(true);
            }
        }

        private static Task RunCompilation(TcpClient client, CancellationToken cancellationToken)
        {
            throw new Exception();
            /*
            var stream = client.GetStream();
            var buildRequest = await BuildRequest.ReadAsync(stream, cancellationToken).ConfigureAwait(true);
            */
        }
    }
}
