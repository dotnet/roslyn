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
using System.Security.AccessControl;
using System.Security.Principal;

namespace Microsoft.CodeAnalysis.CompilerServer.UnitTests
{
    public abstract class DesktopBuildClientTests : TestBase
    {
        private sealed class TestableDesktopBuildClient : DesktopBuildClient
        {
            private readonly string _pipeName;
            private readonly Func<string, bool> _createServerFunc;
            private readonly Func<Task<BuildResponse>> _runServerCompilationFunc;

            public TestableDesktopBuildClient(
                RequestLanguage language,
                CompileFunc compileFunc,
                string pipeName,
                Func<string, bool> createServerFunc,
                Func<Task<BuildResponse>> runServerCompilationFunc) : base(language, compileFunc, new Mock<IAnalyzerAssemblyLoader>().Object)
            {
                _pipeName = pipeName;
                _createServerFunc = createServerFunc;
                _runServerCompilationFunc = runServerCompilationFunc;
            }

            protected override string GetSessionKey(BuildPaths buildPaths)
            {
                return _pipeName;
            }

            protected override bool TryCreateServer(string clientDir, string pipeName)
            {
                return _createServerFunc(pipeName);
            }

            protected override Task<BuildResponse> RunServerCompilation(List<string> arguments, BuildPaths buildPaths, string sessionKey, string keepAlive, string libDirectory, CancellationToken cancellationToken)
            {
                if (_runServerCompilationFunc != null)
                {
                    return _runServerCompilationFunc();
                }

                return base.RunServerCompilation(arguments, buildPaths, sessionKey, keepAlive, libDirectory, cancellationToken);
            }

            public static async Task<bool> TryConnectToNamedPipe(string pipeName, int timeoutMs, CancellationToken cancellationToken)
            {
                using (var pipeStream = await BuildServerConnection.TryConnectToServerAsync(pipeName, timeoutMs, cancellationToken))
                {
                    return pipeStream != null;
                }
            }
        }

        public sealed class ServerTests : DesktopBuildClientTests
        {
            private readonly string _pipeName = Guid.NewGuid().ToString("N");
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
                return new TestableDesktopBuildClient(language.Value, compileFunc, _pipeName, createServerFunc, runServerCompilationFunc);
            }

            private bool TryCreateServer(string pipeName)
            {
                if (!_allowServer)
                {
                    _failedCreatedServerCount++;
                    return false;
                }

                var serverData = ServerUtil.CreateServer(pipeName).GetAwaiter().GetResult();
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

#if NET472
            [Fact]
            public void TestMutexConstructorException()
            {
                using (var outer = new Mutex(initiallyOwned: true, name: BuildServerConnection.GetClientMutexName(_pipeName), out bool createdNew))
                {
                    Assert.True(createdNew);
                    var mutexSecurity = outer.GetAccessControl();
                    mutexSecurity.AddAccessRule(new MutexAccessRule(WindowsIdentity.GetCurrent().Owner, MutexRights.FullControl, AccessControlType.Deny));
                    outer.SetAccessControl(mutexSecurity);

                    var ranLocal = false;
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
#endif

            [Fact]
            public async Task ConnectToPipe()
            {
                string pipeName = Guid.NewGuid().ToString("N");

                var oneSec = TimeSpan.FromSeconds(1);

                Assert.False(await TestableDesktopBuildClient.TryConnectToNamedPipe(pipeName, (int)oneSec.TotalMilliseconds, cancellationToken: default));

                // Try again with infinite timeout and cancel
                var cts = new CancellationTokenSource();
                var connection = TestableDesktopBuildClient.TryConnectToNamedPipe(pipeName, Timeout.Infinite, cts.Token);
                Assert.False(connection.IsCompleted);
                cts.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(
                    async () => await connection.ConfigureAwait(false)).ConfigureAwait(false);

                // Create server and try again
                Assert.True(TryCreateServer(pipeName));
                Assert.True(await TestableDesktopBuildClient.TryConnectToNamedPipe(pipeName, (int)oneSec.TotalMilliseconds, cancellationToken: default));
                // With infinite timeout
                Assert.True(await TestableDesktopBuildClient.TryConnectToNamedPipe(pipeName, Timeout.Infinite, cancellationToken: default));
            }

            [ConditionalFact(typeof(DesktopOnly))]
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
            private string _sessionKey;
            private List<string> _parsedArgs;

            private bool Parse(params string[] args)
            {
                return CommandLineParser.TryParseClientArgs(
                    args,
                    out _parsedArgs,
                    out _hasShared,
                    out _keepAlive,
                    out _sessionKey,
                    out _errorMessage);
            }

            [Theory]
            [InlineData('-')]
            [InlineData('/')]
            public void Shared(char optionPrefix)
            {
                Assert.True(Parse(optionPrefix + "shared", "test.cs"));
                Assert.True(_hasShared);
                Assert.Null(_sessionKey);
                Assert.Equal(new[] { "test.cs" }, _parsedArgs);
            }

            [Theory]
            [InlineData('-')]
            [InlineData('/')]
            public void SharedWithSessionKey(char optionPrefix)
            {
                Assert.True(Parse(optionPrefix + "shared:pipe", "test.cs"));
                Assert.True(_hasShared);
                Assert.Equal("pipe", _sessionKey);
                Assert.Equal(new[] { "test.cs" }, _parsedArgs);

                Assert.True(Parse(optionPrefix + "shared:1:2", "test.cs"));
                Assert.True(_hasShared);
                Assert.Equal("1:2", _sessionKey);
                Assert.Equal(new[] { "test.cs" }, _parsedArgs);

                Assert.True(Parse(optionPrefix + "shared=1:2", "test.cs"));
                Assert.True(_hasShared);
                Assert.Equal("1:2", _sessionKey);
                Assert.Equal(new[] { "test.cs" }, _parsedArgs);
            }

            [Theory]
            [InlineData('-')]
            [InlineData('/')]
            public void SharedWithEmptySessionKey(char optionPrefix)
            {
                Assert.False(Parse(optionPrefix + "shared:", "test.cs"));
                Assert.False(_hasShared);
                Assert.Equal(CodeAnalysisResources.SharedArgumentMissing, _errorMessage);
            }

            [Theory]
            [InlineData('-')]
            [InlineData('/')]
            public void SharedPrefix(char optionPrefix)
            {
                Assert.True(Parse(optionPrefix + "sharedstart", "test.cs"));
                Assert.False(_hasShared);
                Assert.Equal(new[] { optionPrefix + "sharedstart", "test.cs" }, _parsedArgs);
            }

            [Theory]
            [InlineData('-')]
            [InlineData('/')]
            public void Basic(char optionPrefix)
            {
                Assert.True(Parse("test.cs"));
                Assert.False(_hasShared);
                Assert.Null(_sessionKey);
                Assert.Equal(new[] { "test.cs" }, _parsedArgs);

                Assert.True(Parse(optionPrefix + "keepalive:100", "/shared", "test.cs"));
                Assert.True(_hasShared);
                Assert.Null(_sessionKey);
                Assert.Equal("100", _keepAlive);
                Assert.Equal(new[] { "test.cs" }, _parsedArgs);
            }

            [Theory]
            [InlineData('-')]
            [InlineData('/')]
            public void KeepAliveBad(char optionPrefix)
            {
                Assert.False(Parse(optionPrefix + "keepalive", "test.cs"));
                Assert.Equal(CodeAnalysisResources.MissingKeepAlive, _errorMessage);

                Assert.False(Parse(optionPrefix + "keepalive:", "test.cs"));
                Assert.Equal(CodeAnalysisResources.MissingKeepAlive, _errorMessage);

                Assert.False(Parse(optionPrefix + "keepalive:-100", "test.cs"));
                Assert.Equal(CodeAnalysisResources.KeepAliveIsTooSmall, _errorMessage);

                Assert.False(Parse(optionPrefix + "keepalive:100", "test.cs"));
                Assert.Equal(CodeAnalysisResources.KeepAliveWithoutShared, _errorMessage);
            }

            [Theory]
            [InlineData('-')]
            [InlineData('/')]
            public void KeepAlivePrefix(char optionPrefix)
            {
                Assert.True(Parse(optionPrefix + "keepalivestart", "test.cs"));
                Assert.Null(_keepAlive);
                Assert.Equal(new[] { optionPrefix + "keepalivestart", "test.cs" }, _parsedArgs);
            }

            [Theory]
            [InlineData('-')]
            [InlineData('/')]
            public void KeepAlive(char optionPrefix)
            {
                Assert.True(Parse(optionPrefix + "keepalive:100", optionPrefix + "shared", "test.cs"));
                Assert.Equal("100", _keepAlive);
                Assert.Equal(new[] { "test.cs" }, _parsedArgs);
                Assert.True(_hasShared);
                Assert.Null(_sessionKey);

                Assert.True(Parse(optionPrefix + "keepalive=100", optionPrefix + "shared", "test.cs"));
                Assert.Equal("100", _keepAlive);
                Assert.Equal(new[] { "test.cs" }, _parsedArgs);
                Assert.True(_hasShared);
                Assert.Null(_sessionKey);
            }
        }

        public class MiscTest
        {
            [Fact]
            public void GetPipeNameForPathOptSlashes()
            {
                var path = string.Format(@"q:{0}the{0}path", Path.DirectorySeparatorChar);
                var name = BuildServerConnection.GetPipeNameForPathOpt(path);
                Assert.Equal(name, BuildServerConnection.GetPipeNameForPathOpt(path));
                Assert.Equal(name, BuildServerConnection.GetPipeNameForPathOpt(path + Path.DirectorySeparatorChar));
                Assert.Equal(name, BuildServerConnection.GetPipeNameForPathOpt(path + Path.DirectorySeparatorChar + Path.DirectorySeparatorChar));
            }

            [Fact]
            public void GetPipeNameForPathOptLength()
            {
                var path = string.Format(@"q:{0}the{0}path", Path.DirectorySeparatorChar);
                var name = BuildServerConnection.GetPipeNameForPathOpt(path);
                // We only have ~50 total bytes to work with on mac, so the base path must be small
                Assert.Equal(43, name.Length);
            }
        }
    }
}
