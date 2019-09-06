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
using System.Collections.Concurrent;

namespace Microsoft.CodeAnalysis.CompilerServer.UnitTests
{
    internal readonly struct ServerStats
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
        internal BlockingCollection<CompletionReason> ConnectionCompletionCollection { get; }
        internal Task ListenTask { get; }
        internal string PipeName { get; }

        internal ServerData(CancellationTokenSource cancellationTokenSource, string pipeName, Task<ServerStats> serverTask, Task listenTask, BlockingCollection<CompletionReason> connectionCompletionCollection)
        {
            CancellationTokenSource = cancellationTokenSource;
            PipeName = pipeName;
            ServerTask = serverTask;
            ListenTask = listenTask;
            ConnectionCompletionCollection = connectionCompletionCollection;
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
        internal static string DefaultSdkDirectory { get; } = BuildClient.GetSystemSdkDirectory();

        internal static BuildPaths CreateBuildPaths(string workingDir, string tempDir)
        {
            return new BuildPaths(
                clientDir: DefaultClientDirectory,
                workingDir: workingDir,
                sdkDir: DefaultSdkDirectory,
                tempDir: tempDir);
        }

        internal static async Task<ServerData> CreateServer(
            string pipeName = null,
            ICompilerServerHost compilerServerHost = null,
            bool failingServer = false,
            string tempPath = null)
        {
            // The total pipe path must be < 92 characters on Unix, so trim this down to 10 chars
            pipeName = pipeName ?? Guid.NewGuid().ToString().Substring(0, 10);
            compilerServerHost = compilerServerHost ?? BuildServerController.CreateCompilerServerHost();
            tempPath = tempPath ?? Path.GetTempPath();
            var clientConnectionHost = BuildServerController.CreateClientConnectionHostForServerHost(compilerServerHost, pipeName);

            if (failingServer)
            {
                clientConnectionHost = new FailingClientConnectionHost(clientConnectionHost);
            }

            var serverStatsSource = new TaskCompletionSource<ServerStats>();
            var serverListenSource = new TaskCompletionSource<bool>();
            var cts = new CancellationTokenSource();
            var mutexName = BuildServerConnection.GetServerMutexName(pipeName);
            var listener = new TestableDiagnosticListener();
            var task = Task.Run(() =>
            {
                listener.Listening += (sender, e) => { serverListenSource.TrySetResult(true); };
                try
                {
                    BuildServerController.CreateAndRunServer(
                        pipeName,
                        tempPath,
                        clientConnectionHost,
                        listener,
                        keepAlive: TimeSpan.FromMilliseconds(-1),
                        cancellationToken: cts.Token);
                }
                finally
                {
                    var serverStats = new ServerStats(connections: listener.ConnectionCount, completedConnections: listener.CompletedCount);
                    serverStatsSource.SetResult(serverStats);
                }
            });

            // The contract of this function is that it will return once the server has started.  Spin here until
            // we can verify the server has started or simply failed to start.
            while (BuildServerConnection.WasServerMutexOpen(mutexName) != true && !task.IsCompleted)
            {
                await Task.Yield();
            }

            if (task.IsFaulted)
            {
                throw task.Exception;
            }

            return new ServerData(cts, pipeName, serverStatsSource.Task, serverListenSource.Task, listener.ConnectionCompletedCollection);
        }

        /// <summary>
        /// Create a compiler server that fails all connections.
        /// </summary>
        internal static Task<ServerData> CreateServerFailsConnection(string pipeName = null)
        {
            return CreateServer(pipeName, failingServer: true);
        }

        internal static async Task<BuildResponse> Send(string pipeName, BuildRequest request)
        {
            using (var client = await BuildServerConnection.TryConnectToServerAsync(pipeName, Timeout.Infinite, cancellationToken: default).ConfigureAwait(false))
            {
                await request.WriteAsync(client).ConfigureAwait(false);
                return await BuildResponse.ReadAsync(client).ConfigureAwait(false);
            }
        }

        internal static async Task<int> SendShutdown(string pipeName)
        {
            var response = await Send(pipeName, BuildRequest.CreateShutdown());
            return ((ShutdownBuildResponse)response).ServerProcessId;
        }

        internal static BuildClient CreateBuildClient(
            RequestLanguage language,
            CompileFunc compileFunc = null,
            TextWriter textWriter = null,
            int? timeoutOverride = null)
        {
            compileFunc = compileFunc ?? GetCompileFunc(language);
            textWriter = textWriter ?? new StringWriter();
            return new BuildClient(language, compileFunc, timeoutOverride: timeoutOverride);
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
    }
}
