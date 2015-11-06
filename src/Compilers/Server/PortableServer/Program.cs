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
            var clientDirectory = AppContext.BaseDirectory;
            var server = new PortableServer(clientDirectory);
            server.Go().GetAwaiter().GetResult();
        }
    }
}
