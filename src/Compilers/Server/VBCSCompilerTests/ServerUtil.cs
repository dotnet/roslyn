// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CommandLine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CompilerServer.UnitTests
{
    internal struct ServerData
    {
        internal CancellationTokenSource CancellationTokenSource { get; }
        internal Task ServerTask { get; }

        internal ServerData(CancellationTokenSource cancellationTokenSource, Task serverTask)
        {
            CancellationTokenSource = cancellationTokenSource;
            ServerTask = serverTask;
        }
    }

    internal static class ServerUtil
    {
        internal static ServerData CreateServer(string clientDir, string pipeName)
        {
            var taskSource = new TaskCompletionSource<bool>();
            var cts = new CancellationTokenSource();

            var thread = new Thread(_ =>
            {
                try
                {
                    var clientDirectory = Path.GetDirectoryName(typeof(DesktopBuildClientTests).Assembly.Location);
                    var sdkDirectory = RuntimeEnvironment.GetRuntimeDirectory();
                    var compilerServerHost = new DesktopCompilerServerHost(clientDirectory, sdkDirectory);
                    var clientConnectionHost = new NamedPipeClientConnectionHost(compilerServerHost, pipeName);
                    var mutexName = BuildProtocolConstants.GetServerMutexName(pipeName);
                    VBCSCompiler.Run(mutexName, clientConnectionHost, TimeSpan.MaxValue, cts.Token);
                }
                finally
                {
                    taskSource.SetResult(true);
                }
            });

            thread.Start();

            return new ServerData(cts, taskSource.Task);
        }
    }
}