// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

extern alias csc;
extern alias vbc;

using Microsoft.CodeAnalysis.CommandLine;
using System;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;

namespace Microsoft.CodeAnalysis.CompilerServer.UnitTests
{
    internal struct ServerStats
    {
        internal readonly int Connections;
        internal readonly int CompletedConnections;

        internal ServerStats(int connections, int completedConnections)
        {
            Connections = connections;
            CompletedConnections = completedConnections;
        }
    }

    internal sealed class ServerData : IDisposable
    {
        internal CancellationTokenSource CancellationTokenSource { get; }
        internal Task<ServerStats> ServerTask { get; }
        internal Task ListenTask { get; }

        /// <summary>
        /// Fully constructed server pipe name
        /// </summary>
        internal string PipeName { get; }

        /// <summary>
        /// "Uniqueifying" component for pipe name, unhashed
        /// </summary>
        internal string SharedCompilationId { get; }

        internal ServerData(CancellationTokenSource cancellationTokenSource, string clientDirectory, string sharedCompilationId, Task<ServerStats> serverTask, Task listenTask)
        {
            CancellationTokenSource = cancellationTokenSource;
            PipeName = BuildServerConnection.GetPipeNameForPathOpt(clientDirectory, sharedCompilationId);
            SharedCompilationId = sharedCompilationId;
            ServerTask = serverTask;
            ListenTask = listenTask;
        }

        internal async Task<ServerStats> Complete()
        {
            CancellationTokenSource.Cancel();
            return await ServerTask;
        }

        internal async Task Verify(int connections, int completed)
        {
            var stats = await Complete().ConfigureAwait(false);
            Assert.Equal(connections, stats.Connections);
            Assert.Equal(completed, stats.CompletedConnections);
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

        internal static BuildPaths CreateBuildPaths(string workingDir, string tempDir)
        {
            return new BuildPaths(
                clientDir: DefaultClientDirectory,
                workingDir: workingDir,
                sdkDir: DefaultSdkDirectory,
                tempDir: tempDir);
        }

        /// <summary>
        /// Gets the client directory from a host if it has one, or a default otherwise
        /// </summary>
        private static string GetClientDirectoryOrDefault(ICompilerServerHost compilerServerHost)
        {
            var compilerHostImpl = compilerServerHost as CompilerServerHost;
            return compilerHostImpl?.ClientDirectory ?? DefaultClientDirectory;
        }

        internal static ServerData CreateServer(
            string sharedCompilationId = null,
            TimeSpan? timeout = null,
            ICompilerServerHost compilerServerHost = null,
            IClientConnectionHost clientConnectionHost = null)
        {
            sharedCompilationId = sharedCompilationId ?? Guid.NewGuid().ToString();
            compilerServerHost = compilerServerHost ?? new DesktopCompilerServerHost(DefaultClientDirectory, DefaultSdkDirectory);

            string clientDirectory = GetClientDirectoryOrDefault(compilerServerHost);
            // Get the fully constructed, hashed pipe name.
            string pipeName = BuildServerConnection.GetPipeNameForPathOpt(clientDirectory, sharedCompilationId);

            var serverStatsSource = new TaskCompletionSource<ServerStats>();
            var serverListenSource = new TaskCompletionSource<bool>();
            var cts = new CancellationTokenSource();
            var mutexName = BuildServerConnection.GetServerMutexName(pipeName);
            var thread = new Thread(_ =>
            {
                var listener = new TestableDiagnosticListener();
                listener.Listening += (sender, e) => { serverListenSource.TrySetResult(true); };
                try
                {
                    clientConnectionHost = clientConnectionHost ?? new NamedPipeClientConnectionHost(compilerServerHost, pipeName);

                    DesktopBuildServerController.RunServer(
                        pipeName,
                        clientConnectionHost,
                        listener,
                        timeout ?? TimeSpan.FromMilliseconds(-1),
                        cts.Token);
                }
                finally
                {
                    var serverStats = new ServerStats(connections: listener.ConnectionCount, completedConnections: listener.CompletedCount);
                    serverStatsSource.SetResult(serverStats);
                }
            });

            thread.Start();

            // The contract of this function is that it will return once the server has started.  Spin here until
            // we can verify the server has started or simply failed to start.
            while (BuildServerConnection.WasServerMutexOpen(mutexName) != true && thread.IsAlive)
            {
                Thread.Yield();
            }

            return new ServerData(cts, clientDirectory, sharedCompilationId, serverStatsSource.Task, serverListenSource.Task);
        }

        /// <summary>
        /// Create a compiler server that fails all connections.
        /// </summary>
        internal static ServerData CreateServerFailsConnection(string sharedCompilationId = null)
        {
            // This is the "uniqueifying" component of the pipe name.
            sharedCompilationId = sharedCompilationId ?? Guid.NewGuid().ToString();
            // Get the fully constructed, hashed pipe name.
            string pipeName = BuildServerConnection.GetPipeNameForPathOpt(DefaultClientDirectory, sharedCompilationId);

            var taskSource = new TaskCompletionSource<ServerStats>();
            var cts = new CancellationTokenSource();
            using (var mre = new ManualResetEvent(initialState: false))
            {
                var thread = new Thread(_ =>
                {
                    var mutexName = BuildServerConnection.GetServerMutexName(pipeName);
                    bool holdsMutex;
                    using (var serverMutex = new Mutex(initiallyOwned: true,
                                                       name: mutexName,
                                                       createdNew: out holdsMutex))
                    {
                        mre.Set();
                        if (!holdsMutex)
                        {
                            throw new InvalidOperationException("Mutex should be unique");
                        }

                        var connections = CreateServerFailsConnectionCore(pipeName, cts.Token).Result;
                        taskSource.SetResult(new ServerStats(connections: connections, completedConnections: 0));
                    }
                });

                thread.Start();

                // Can't exit until the mutex is acquired.  Otherwise the client can end up in a race 
                // condition trying to start the server.
                mre.WaitOne();
            }

            return new ServerData(cts, DefaultClientDirectory, sharedCompilationId, taskSource.Task, Task.FromException(new Exception()));
        }

        internal static async Task<BuildResponse> Send(string pipeName, BuildRequest request)
        {
            using (var client = new NamedPipeClientStream(pipeName))
            {
                await client.ConnectAsync();
                await request.WriteAsync(client);
                return await BuildResponse.ReadAsync(client);
            }
        }

        internal static async Task<int> SendShutdown(string pipeName)
        {
            var response = await Send(pipeName, BuildRequest.CreateShutdown());
            return ((ShutdownBuildResponse)response).ServerProcessId;
        }

        internal static DesktopBuildClient CreateBuildClient(
            RequestLanguage language,
            CompileFunc compileFunc = null,
            TextWriter textWriter = null,
            IAnalyzerAssemblyLoader analyzerAssemblyLoader = null)
        {
            compileFunc = compileFunc ?? GetCompileFunc(language);
            textWriter = textWriter ?? new StringWriter();
            analyzerAssemblyLoader = analyzerAssemblyLoader ?? new Mock<IAnalyzerAssemblyLoader>(MockBehavior.Strict).Object;
            return new DesktopBuildClient(language, compileFunc, analyzerAssemblyLoader);
        }

        internal static CompileFunc GetCompileFunc(RequestLanguage language)
        {
            Func<string[], string, string, string, string, TextWriter, IAnalyzerAssemblyLoader, int> func;
            switch (language)
            {
                case RequestLanguage.CSharpCompile:
                    func = csc.Microsoft.CodeAnalysis.CSharp.CommandLine.Program.Run;
                    break;
                case RequestLanguage.VisualBasicCompile:
                    func = vbc.Microsoft.CodeAnalysis.VisualBasic.CommandLine.Program.Run;
                    break;
                default:
                    throw new InvalidOperationException();
            }

            return (args, buildPaths, textWriter, loader) => func(args, buildPaths.ClientDirectory, buildPaths.WorkingDirectory, buildPaths.SdkDirectory, buildPaths.TempDirectory, textWriter, loader);
        }

        private static async Task<int> CreateServerFailsConnectionCore(string pipeName, CancellationToken cancellationToken)
        {
            var connections = 0;
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    using (var pipeStream = new NamedPipeServerStream(pipeName, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
                    {
                        await pipeStream.WaitForConnectionAsync(cancellationToken);
                        connections++;
                    }
                }
            }
            catch (Exception)
            {
                // Exceptions are okay and expected here
            }

            return connections;
        }
    }
}