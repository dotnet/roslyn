// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

extern alias csc;
extern alias vbc;

using System;
using System.IO;
using Microsoft.CodeAnalysis.CommandLine;
using System.Runtime.InteropServices;
using Moq;
using Xunit;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using System.Threading;
using System.IO.Pipes;

namespace Microsoft.CodeAnalysis.CompilerServer.UnitTests
{
    public abstract class DesktopBuildClientTests : TestBase
    {
        private sealed class TestableDesktopBuildClient : DesktopBuildClient
        {
            /// <summary>
            /// Stores fully constructed pipe name.
            /// </summary>
            private readonly string _pipeName;
            /// <summary>
            /// Stores unhashed "uniqueifying" pipe name component.
            /// </summary>
            private readonly string _sharedCompilationId;
            private readonly Func<string, bool> _createServerFunc;
            private readonly Func<Task<BuildResponse>> _runServerCompilationFunc;

            public TestableDesktopBuildClient(
                RequestLanguage langauge,
                CompileFunc compileFunc,
                string pipeName,
                string sharedCompilationId,
                Func<string, bool> createServerFunc,
                Func<Task<BuildResponse>> runServerCompilationFunc) : base(langauge, compileFunc, new Mock<IAnalyzerAssemblyLoader>().Object)
            {
                _pipeName = pipeName;
                _sharedCompilationId = sharedCompilationId;
                _createServerFunc = createServerFunc;
                _runServerCompilationFunc = runServerCompilationFunc;
            }

            protected override string ConstructPipeName(BuildPaths buildPaths, string sharedCompilationId = null)
            {
                return _pipeName;
            }

            protected override bool TryCreateServer(string clientDir, string pipeName)
            {
                return _createServerFunc(pipeName);
            }

            protected override Task<BuildResponse> RunServerCompilation(List<string> arguments, BuildPaths buildPaths, string pipeName, string keepAlive, string libDirectory, CancellationToken cancellationToken)
            {
                if (_runServerCompilationFunc != null)
                {
                    return _runServerCompilationFunc();
                }

                return base.RunServerCompilation(arguments, buildPaths, pipeName, keepAlive, libDirectory, cancellationToken);
            }

            public bool TryConnectToNamedPipeWithSpinWait(int timeoutMs, CancellationToken cancellationToken)
            {
                using (var pipeStream = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut, PipeOptions.Asynchronous))
                {
                    return BuildServerConnection.TryConnectToNamedPipeWithSpinWait(pipeStream, timeoutMs, cancellationToken);
                }
            }
        }

        public sealed class ServerTests : DesktopBuildClientTests
        {
            private static readonly string _sharedCompilationId = Guid.NewGuid().ToString("N");
            private static readonly string _pipeName = BuildServerConnection.GetPipeNameForPathOpt(ServerUtil.DefaultClientDirectory, _sharedCompilationId);
            private readonly BuildPaths _buildPaths;
            private readonly List<ServerData> _serverDataList = new List<ServerData>();
            private bool _allowServer = true;
            private int _failedCreatedServerCount = 0;

            public ServerTests()
            {
                _buildPaths = ServerUtil.CreateBuildPaths(
                    workingDir: Temp.CreateDirectory().Path,
                    tempDir: Temp.CreateDirectory().Path);
            }

            public override void Dispose()
            {
                foreach (var serverData in _serverDataList)
                {
                    serverData.CancellationTokenSource.Cancel();
                    serverData.ServerTask.Wait();
                }

                base.Dispose();
            }

            private TestableDesktopBuildClient CreateClient(
                RequestLanguage? language = null,
                CompileFunc compileFunc = null,
                Func<string, bool> createServerFunc = null,
                Func<Task<BuildResponse>> runServerCompilationFunc = null)
            {
                language = language ?? RequestLanguage.CSharpCompile;
                compileFunc = compileFunc ?? delegate { return 0; };
                createServerFunc = createServerFunc ?? TryCreateServer;
                return new TestableDesktopBuildClient(language.Value, compileFunc, _pipeName, _sharedCompilationId, createServerFunc, runServerCompilationFunc);
            }

            /// <param name="ignored">
            /// Pipe name is not used to create a server here as ServerUtil only expects the SharedCompilationId component.
            /// </param>
            private bool TryCreateServer(string ignored)
            {
                if (!_allowServer)
                {
                    _failedCreatedServerCount++;
                    return false;
                }

                var serverData = ServerUtil.CreateServer(_sharedCompilationId);
                _serverDataList.Add(serverData);
                return true;
            }

            [Fact]
            public void ConnectToServerFails()
            {
                // Create and grab the mutex for the server. This should make
                // the client believe that a server is active and it will try
                // to connect. When it fails it should fall back to in-proc
                // compilation.
                bool holdsMutex;
                using (var serverMutex = new Mutex(initiallyOwned: true,
                                                   name: BuildServerConnection.GetServerMutexName(_pipeName),
                                                   createdNew: out holdsMutex))
                {
                    Assert.True(holdsMutex);
                    var ranLocal = false;
                    // Note: Connecting to a server can take up to a second to time out
                    var client = CreateClient(
                        compileFunc: delegate
                        {
                            ranLocal = true;
                            return 0;
                        });
                    var exitCode = client.RunCompilation(new[] { "/shared" }, _buildPaths).ExitCode;
                    Assert.Equal(0, exitCode);
                    Assert.True(ranLocal);
                }
            }

            [Fact]
            public async Task ConnectToPipeWithSpinWait()
            {
                // No server should be started with the current pipe name
                var client = CreateClient();
                var oneSec = TimeSpan.FromSeconds(1);

                Assert.False(client.TryConnectToNamedPipeWithSpinWait((int)oneSec.TotalMilliseconds,
                                                                      default(CancellationToken)));

                // Try again with infinite timeout and cancel
                var cts = new CancellationTokenSource();
                var connection = Task.Run(() => client.TryConnectToNamedPipeWithSpinWait(Timeout.Infinite,
                                                                                         cts.Token),
                                          cts.Token);
                Assert.False(connection.IsCompleted);
                cts.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(
                    async () => await connection.ConfigureAwait(false)).ConfigureAwait(false);

                // Create server and try again
                Assert.True(TryCreateServer(_sharedCompilationId));
                Assert.True(client.TryConnectToNamedPipeWithSpinWait((int)oneSec.TotalMilliseconds,
                                                                     default(CancellationToken)));
                // With infinite timeout
                Assert.True(client.TryConnectToNamedPipeWithSpinWait(Timeout.Infinite,
                                                                     default(CancellationToken)));
            }

            [Fact]
            public void OnlyStartsOneServer()
            {
                var ranLocal = false;
                var client = CreateClient(
                    compileFunc: delegate
                    {
                        ranLocal = true;
                        throw new Exception();
                    });

                for (var i = 0; i < 5; i++)
                {
                    client.RunCompilation(new[] { "/shared" }, _buildPaths, new StringWriter());
                }

                Assert.Equal(1, _serverDataList.Count);
                Assert.False(ranLocal);
            }

            [Fact]
            public void FallbackToCsc()
            {
                _allowServer = false;
                var ranLocal = false;
                var client = CreateClient(compileFunc: delegate
                {
                    ranLocal = true;
                    return 0;
                });

                var exitCode = client.RunCompilation(new[] { "/shared" }, _buildPaths).ExitCode;
                Assert.Equal(0, exitCode);
                Assert.True(ranLocal);
                Assert.Equal(1, _failedCreatedServerCount);
                Assert.Equal(0, _serverDataList.Count);
            }

            [Fact]
            [WorkItem(7866, "https://github.com/dotnet/roslyn/issues/7866")]
            public void RunServerCompilationThrows()
            {
                bool ranLocal;
                Func<int> compileFunc = () =>
                {
                    ranLocal = true;
                    return CommonCompiler.Succeeded;
                };

                TestableDesktopBuildClient client;
                RunCompilationResult result;

                ranLocal = false;
                client = CreateClient(
                    compileFunc: delegate { return compileFunc(); },
                    runServerCompilationFunc: () => Task.FromException<BuildResponse>(new Exception()));
                result = client.RunCompilation(new[] { "/shared" }, _buildPaths);
                Assert.Equal(CommonCompiler.Succeeded, result.ExitCode);
                Assert.False(result.RanOnServer);
                Assert.True(ranLocal);

                ranLocal = false;
                client = CreateClient(
                    compileFunc: delegate { return compileFunc(); },
                    runServerCompilationFunc: () => { throw new Exception(); });
                result = client.RunCompilation(new[] { "/shared" }, _buildPaths);
                Assert.Equal(CommonCompiler.Succeeded, result.ExitCode);
                Assert.False(result.RanOnServer);
                Assert.True(ranLocal);
            }
        }

        public sealed class TryParseClientArgsTest : DesktopBuildClientTests
        {
            private bool _hasShared;
            private string _keepAlive;
            private string _errorMessage;
            private string _sharedCompilationId;
            private List<string> _parsedArgs;

            private bool Parse(params string[] args)
            {
                return CommandLineParser.TryParseClientArgs(
                    args,
                    out _parsedArgs,
                    out _hasShared,
                    out _keepAlive,
                    out _sharedCompilationId,
                    out _errorMessage);
            }

            [Fact]
            public void Shared()
            {
                Assert.True(Parse("/shared", "test.cs"));
                Assert.True(_hasShared);
                Assert.Null(_sharedCompilationId);
                Assert.Equal(new[] { "test.cs" }, _parsedArgs);
            }

            [Fact]
            public void SharedWithCompilationId()
            {
                Assert.True(Parse("/shared:pipe", "test.cs"));
                Assert.True(_hasShared);
                Assert.Equal("pipe", _sharedCompilationId);
                Assert.Equal(new[] { "test.cs" }, _parsedArgs);

                Assert.True(Parse("/shared:1:2", "test.cs"));
                Assert.True(_hasShared);
                Assert.Equal("1:2", _sharedCompilationId);
                Assert.Equal(new[] { "test.cs" }, _parsedArgs);

                Assert.True(Parse("/shared=1:2", "test.cs"));
                Assert.True(_hasShared);
                Assert.Equal("1:2", _sharedCompilationId);
                Assert.Equal(new[] { "test.cs" }, _parsedArgs);
            }

            [Fact]
            public void SharedWithEmptyCompilationId()
            {
                Assert.False(Parse("/shared:", "test.cs"));
                Assert.False(_hasShared);
                Assert.Equal(CodeAnalysisResources.SharedArgumentMissing, _errorMessage);
            }

            [Fact]
            public void SharedPrefix()
            {
                Assert.True(Parse("/sharedstart", "test.cs"));
                Assert.False(_hasShared);
                Assert.Equal(new[] { "/sharedstart", "test.cs" }, _parsedArgs);
            }

            [Fact]
            public void Basic()
            {
                Assert.True(Parse("test.cs"));
                Assert.False(_hasShared);
                Assert.Null(_sharedCompilationId);
                Assert.Equal(new[] { "test.cs" }, _parsedArgs);

                Assert.True(Parse("/keepalive:100", "/shared", "test.cs"));
                Assert.True(_hasShared);
                Assert.Null(_sharedCompilationId);
                Assert.Equal("100", _keepAlive);
                Assert.Equal(new[] { "test.cs" }, _parsedArgs);
            }

            [Fact]
            public void KeepAliveBad()
            {
                Assert.False(Parse("/keepalive", "test.cs"));
                Assert.Equal(CodeAnalysisResources.MissingKeepAlive, _errorMessage);

                Assert.False(Parse("/keepalive:", "test.cs"));
                Assert.Equal(CodeAnalysisResources.MissingKeepAlive, _errorMessage);

                Assert.False(Parse("/keepalive:-100", "test.cs"));
                Assert.Equal(CodeAnalysisResources.KeepAliveIsTooSmall, _errorMessage);

                Assert.False(Parse("/keepalive:100", "test.cs"));
                Assert.Equal(CodeAnalysisResources.KeepAliveWithoutShared, _errorMessage);
            }

            [Fact]
            public void KeepAlivePrefix()
            {
                Assert.True(Parse("/keepalivestart", "test.cs"));
                Assert.Null(_keepAlive);
                Assert.Equal(new[] { "/keepalivestart", "test.cs" }, _parsedArgs);
            }

            [Fact]
            public void KeepAlive()
            {
                Assert.True(Parse("/keepalive:100", "/shared", "test.cs"));
                Assert.Equal("100", _keepAlive);
                Assert.Equal(new[] { "test.cs" }, _parsedArgs);
                Assert.True(_hasShared);
                Assert.Null(_sharedCompilationId);

                Assert.True(Parse("/keepalive=100", "/shared", "test.cs"));
                Assert.Equal("100", _keepAlive);
                Assert.Equal(new[] { "test.cs" }, _parsedArgs);
                Assert.True(_hasShared);
                Assert.Null(_sharedCompilationId);
            }
        }

        public class MiscTest
        {
            [Fact]
            public void GetBasePipeName()
            {
                var path = string.Format(@"q:{0}the{0}path", Path.DirectorySeparatorChar);
                var sharedCompilationId = Guid.NewGuid().ToString();
                var name = BuildServerConnection.GetBasePipeName(path);
                var nameWithId = BuildServerConnection.GetBasePipeName(path, sharedCompilationId);
                Assert.Equal(name, BuildServerConnection.GetBasePipeName(path));
                Assert.Equal(name, BuildServerConnection.GetBasePipeName(path + Path.DirectorySeparatorChar));
                Assert.Equal(name, BuildServerConnection.GetBasePipeName(path + Path.DirectorySeparatorChar + Path.DirectorySeparatorChar));

                Assert.NotEqual(name, nameWithId);
                Assert.Equal(nameWithId, BuildServerConnection.GetBasePipeName(path, sharedCompilationId + " \t\n"));
                Assert.Equal(name, BuildServerConnection.GetBasePipeName(path, " \t\n"));
            }
        }
    }
}
