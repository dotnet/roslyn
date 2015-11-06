// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Net;
using System.Threading;

namespace Microsoft.CodeAnalysis.CompilerServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var ipAddress = IPAddress.Parse("127.0.0.1");
            var endPoint = new IPEndPoint(ipAddress, port: 12000);
            var clientDirectory = AppContext.BaseDirectory;
            var compilerHost = new CoreClrCompilerServerHost(endPoint);
            var compilerRequestHandler = new CompilerRequestHandler(compilerHost, clientDirectory);
            var serverDispatcher = new ServerDispatcher(compilerHost, compilerRequestHandler, new EmptyDiagnosticListener());
            serverDispatcher.ListenAndDispatchConnections(keepAlive: null, cancellationToken: CancellationToken.None);
        }
    }
}
