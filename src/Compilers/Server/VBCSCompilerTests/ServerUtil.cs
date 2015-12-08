// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CommandLine;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CompilerServer.UnitTests
{
    internal sealed class ServerData : IDisposable
    {
        internal CancellationTokenSource CancellationTokenSource { get; }
        internal Task ServerTask { get; }
        internal string PipeName { get; }

        internal ServerData(CancellationTokenSource cancellationTokenSource, Task serverTask, string pipeName)
        {
            CancellationTokenSource = cancellationTokenSource;
            ServerTask = serverTask;
            PipeName = pipeName;
        }

        public void Dispose()
        {
            if (!CancellationTokenSource.IsCancellationRequested)
            {
                CancellationTokenSource.Cancel();
            }

            ServerTask.Wait();
        }
    }

    internal static class ServerUtil
    {
        internal static string DefaultClientDirectory { get; } = Path.GetDirectoryName(typeof(DesktopBuildClientTests).Assembly.Location);
        internal static string DefaultSdkDirectory { get; } = RuntimeEnvironment.GetRuntimeDirectory();

        internal static BuildPaths CreateBuildPaths(string workingDir)
        {
            return new BuildPaths(
                clientDir: DefaultClientDirectory,
                workingDir: workingDir,
                sdkDir: DefaultSdkDirectory);
        }

        internal static ServerData CreateServer(
            string pipeName = null, 
            TimeSpan? timeout = null,
            ICompilerServerHost compilerServerHost = null)
        {
            pipeName = pipeName ?? Guid.NewGuid().ToString();
            compilerServerHost = compilerServerHost ?? new DesktopCompilerServerHost(ServerUtil.DefaultClientDirectory, ServerUtil.DefaultSdkDirectory);

            var taskSource = new TaskCompletionSource<bool>();
            var cts = new CancellationTokenSource();
            var thread = new Thread(_ =>
            {
                try
                {
                    var clientConnectionHost = new NamedPipeClientConnectionHost(compilerServerHost, pipeName);
                    var mutexName = BuildProtocolConstants.GetServerMutexName(pipeName);
                    VBCSCompiler.Run(
                        mutexName, 
                        clientConnectionHost, 
                        timeout ?? TimeSpan.FromMilliseconds(-1), 
                        cts.Token);
                }
                finally
                {
                    taskSource.SetResult(true);
                }
            });

            thread.Start();

            return new ServerData(cts, taskSource.Task, pipeName);
        }

        /// <summary>
        /// Create a compiler server that fails all connections.
        /// </summary>
        internal static ServerData CreateServerFailsConnection(string pipeName = null)
        {
            pipeName = pipeName ?? Guid.NewGuid().ToString();

            var taskSource = new TaskCompletionSource<bool>();
            var cts = new CancellationTokenSource();

            var thread = new Thread(_ =>
            {
                try
                {
                    CreateServerFailsConnectionCore(pipeName, cts.Token).Wait();
                }
                finally
                {
                    taskSource.SetResult(true);
                }
            });

            thread.Start();

            return new ServerData(cts, taskSource.Task, pipeName);
        }

        private static async Task CreateServerFailsConnectionCore(string pipeName, CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var pipeStream = new NamedPipeServerStream(pipeName, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                    await pipeStream.WaitForConnectionAsync(cancellationToken);
                    pipeStream.Close();
                }
            }
            catch (Exception)
            {
                // Exceptions are okay and expected here
            }
        }
    }
}