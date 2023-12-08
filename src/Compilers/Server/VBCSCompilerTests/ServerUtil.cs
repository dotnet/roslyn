// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CompilerServer.UnitTests
{
    internal static class ProtocolUtil
    {
        internal static readonly BuildRequest EmptyCSharpBuildRequest = new BuildRequest(
            RequestLanguage.CSharpCompile,
            BuildProtocolConstants.GetCommitHash(),
            ImmutableArray<BuildRequest.Argument>.Empty);

        internal static readonly BuildRequest EmptyBasicBuildRequest = new BuildRequest(
            RequestLanguage.VisualBasicCompile,
            BuildProtocolConstants.GetCommitHash(),
            ImmutableArray<BuildRequest.Argument>.Empty);

        internal static readonly BuildResponse EmptyBuildResponse = new CompletedBuildResponse(
            returnCode: 0,
            utf8output: false,
            output: string.Empty);

        internal static BuildRequest CreateEmptyCSharp(string workingDirectory, string tempDirectory = null) => BuildRequest.Create(
            RequestLanguage.CSharpCompile,
            Array.Empty<string>(),
            workingDirectory,
            tempDirectory ?? Path.GetTempPath(),
            compilerHash: BuildProtocolConstants.GetCommitHash());

        internal static BuildRequest CreateEmptyCSharpWithKeepAlive(TimeSpan keepAlive, string workingDirectory, string tempDirectory = null) => BuildRequest.Create(
            RequestLanguage.CSharpCompile,
            Array.Empty<string>(),
            workingDirectory,
            tempDirectory ?? Path.GetTempPath(),
            compilerHash: BuildProtocolConstants.GetCommitHash(),
            keepAlive: keepAlive.TotalSeconds.ToString());

    }

    internal sealed class ServerData : IDisposable
    {
        internal CancellationTokenSource CancellationTokenSource { get; }
        internal Task<TestableDiagnosticListener> ServerTask { get; }
        internal string PipeName { get; }
        internal ICompilerServerLogger Logger { get; }

        private ServerData(CancellationTokenSource cancellationTokenSource, string pipeName, ICompilerServerLogger logger, Task<TestableDiagnosticListener> serverTask)
        {
            CancellationTokenSource = cancellationTokenSource;
            PipeName = pipeName;
            Logger = logger;
            ServerTask = serverTask;
        }

        /// <summary>
        /// Create a new server at the specified pipe name. This function will return immediately 
        /// after creating the server and does _not_ wait for it to begin listening
        /// </summary>
        internal static ServerData Create(
            ICompilerServerLogger logger,
            string pipeName = null,
            ICompilerServerHost compilerServerHost = null,
            IClientConnectionHost clientConnectionHost = null,
            TimeSpan? keepAlive = null)
        {
            // The total pipe path must be < 92 characters on Unix, so trim this down to 10 chars
            pipeName ??= ServerUtil.GetPipeName();
            compilerServerHost ??= BuildServerController.CreateCompilerServerHost(logger);
            clientConnectionHost ??= BuildServerController.CreateClientConnectionHost(pipeName, logger);
            keepAlive ??= TimeSpan.FromMilliseconds(-1);

            var listener = new TestableDiagnosticListener();
            var serverListenSource = new TaskCompletionSource<bool>();
            var cts = new CancellationTokenSource();
            var mutexName = BuildServerConnection.GetServerMutexName(pipeName);
            var task = Task.Run(() =>
            {
                BuildServerController.CreateAndRunServer(
                    pipeName,
                    compilerServerHost,
                    clientConnectionHost,
                    listener,
                    keepAlive: keepAlive,
                    cancellationToken: cts.Token);
                return listener;
            });

            return new ServerData(cts, pipeName, logger, task);
        }

        /// <summary>
        /// This function will wait until the server is either listening on the named pipe or 
        /// if it has exited.
        /// </summary>
        internal async Task WaitForServerAsync()
        {
            // The contract of this function is that it will return once the server has started.  Spin here until
            // we can verify the server has started or simply failed to start.
            var mutexName = BuildServerConnection.GetServerMutexName(PipeName);
            while (BuildServerConnection.WasServerMutexOpen(mutexName) != true && !ServerTask.IsCompleted)
            {
                await Task.Yield();
            }
        }

        internal Task<BuildResponse> SendAsync(BuildRequest request, CancellationToken cancellationToken = default) =>
            BuildServerConnection.RunServerBuildRequestAsync(
                request,
                PipeName,
                timeoutOverride: Timeout.Infinite,
                tryCreateServerFunc: (_, _) => false,
                Logger,
                cancellationToken);

        internal async Task<int> SendShutdownAsync(CancellationToken cancellationToken = default)
        {
            var response = await SendAsync(BuildRequest.CreateShutdown(), cancellationToken).ConfigureAwait(false);
            return ((ShutdownBuildResponse)response).ServerProcessId;
        }

        internal async Task<TestableDiagnosticListener> Complete()
        {
            CancellationTokenSource.Cancel();
            return await ServerTask;
        }

        public void Dispose()
        {
            if (!CancellationTokenSource.IsCancellationRequested)
            {
                CancellationTokenSource.Cancel();
            }

            ServerTask.Wait();
            Assert.True(NamedPipeTestUtil.IsPipeFullyClosed(PipeName));
        }
    }

    internal static class ServerUtil
    {
        internal static string DefaultClientDirectory { get; } = Path.GetDirectoryName(typeof(BuildClientTests).Assembly.Location);
        internal static string DefaultSdkDirectory { get; } = BuildClient.GetSystemSdkDirectory();

        internal static BuildPaths CreateBuildPaths(string workingDir, string tempDir)
        {
            return new BuildPaths(
                clientDir: DefaultClientDirectory,
                workingDir: workingDir,
                sdkDir: DefaultSdkDirectory,
                tempDir: tempDir);
        }

        internal static string GetPipeName() => Guid.NewGuid().ToString().Substring(0, 10);
        internal static async Task<ServerData> CreateServer(
            ICompilerServerLogger logger,
            string pipeName = null,
            ICompilerServerHost compilerServerHost = null,
            IClientConnectionHost clientConnectionHost = null,
            TimeSpan? keepAlive = null)
        {
            var serverData = ServerData.Create(logger, pipeName, compilerServerHost, clientConnectionHost, keepAlive);
            await serverData.WaitForServerAsync();
            return serverData;
        }

        internal static BuildClient CreateBuildClient(
            RequestLanguage language,
            ICompilerServerLogger logger)
        {
            // Create a client to run the build.  Infinite timeout is used to account for the
            // case where these tests are run under extreme load.  In high load scenarios the
            // client will correctly drop down to a local compilation if the server doesn't respond
            // fast enough.
            CompileOnServerFunc compileOnServerFunc = (request, pipeName, cancellationToken) =>
                BuildServerConnection.RunServerBuildRequestAsync(
                    request,
                    pipeName,
                    timeoutOverride: Timeout.Infinite,
                    tryCreateServerFunc: (_, _) => false,
                    logger,
                    cancellationToken);

            var compileFunc = GetCompileFunc(language);
            return new BuildClient(logger, language, compileFunc, compileOnServerFunc);
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
