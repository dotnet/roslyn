// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CompilerServer.UnitTests
{
    public abstract class BuildClientTests : TestBase
    {
        public sealed class ServerTests : BuildClientTests
        {
            private readonly string _pipeName = Guid.NewGuid().ToString("N");
            private readonly BuildPaths _buildPaths;
            private readonly List<ServerData> _serverDataList = new List<ServerData>();
            private readonly XunitCompilerServerLogger _logger;

            public ServerTests(ITestOutputHelper testOutputHelper)
            {
                _buildPaths = ServerUtil.CreateBuildPaths(
                    workingDir: Temp.CreateDirectory().Path,
                    tempDir: Temp.CreateDirectory().Path);
                _logger = new XunitCompilerServerLogger(testOutputHelper);
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

            private BuildClient CreateClient(
                RequestLanguage? language = null,
                CompileFunc compileFunc = null,
                CompileOnServerFunc compileOnServerFunc = null)
            {
                language ??= RequestLanguage.CSharpCompile;
                compileFunc ??= delegate { return 0; };
                compileOnServerFunc ??= delegate { throw new InvalidOperationException(); };
                return new BuildClient(_logger, language.Value, compileFunc, compileOnServerFunc);
            }

            private ServerData CreateServer(string pipeName)
            {
                var serverData = ServerUtil.CreateServer(_logger, pipeName).GetAwaiter().GetResult();
                _serverDataList.Add(serverData);
                return serverData;
            }

            [Fact]
            public void ConnectToServerFails()
            {
                // Create and grab the mutex for the server. This should make
                // the client believe that a server is active and it will try
                // to connect. When it fails it should fall back to in-proc
                // compilation.
                bool holdsMutex;
                using (var serverMutex = BuildServerConnection.OpenOrCreateMutex(
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
                    var exitCode = client.RunCompilation(new[] { "/shared" }, _buildPaths, pipeName: _pipeName).ExitCode;
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
                    var exitCode = client.RunCompilation(new[] { "/shared" }, _buildPaths, pipeName: _pipeName).ExitCode;
                    Assert.Equal(0, exitCode);
                    Assert.True(ranLocal);
                }
            }
#endif

            [Fact]
            [WorkItem(70166, "https://github.com/dotnet/roslyn/issues/70166")]
            public void TestIfMutexIsGlobal()
            {
                const string GlobalPrefix = "Global\\";

                var clientMutexName = BuildServerConnection.GetClientMutexName(_pipeName);
                Assert.True(clientMutexName.StartsWith(GlobalPrefix));

                var serverMutexName = BuildServerConnection.GetServerMutexName(_pipeName);
                Assert.True(serverMutexName.StartsWith(GlobalPrefix));
            }

            [Fact]
            public async Task ConnectToPipe()
            {
                string pipeName = ServerUtil.GetPipeName();

                var oneSec = TimeSpan.FromSeconds(1);

                Assert.False(await tryConnectToNamedPipe((int)oneSec.TotalMilliseconds, cancellationToken: default));

                // Try again with infinite timeout and cancel
                var cts = new CancellationTokenSource();
                var connection = tryConnectToNamedPipe(Timeout.Infinite, cts.Token);
                Assert.False(connection.IsCompleted);
                cts.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(
                    async () => await connection);

                // Create server and try again
                using var serverData = CreateServer(pipeName);
                Assert.True(await tryConnectToNamedPipe(Timeout.Infinite, cancellationToken: default));

                async Task<bool> tryConnectToNamedPipe(int timeoutMs, CancellationToken cancellationToken)
                {
                    using var pipeStream = await BuildServerConnection.TryConnectToServerAsync(pipeName, timeoutMs, _logger, cancellationToken);
                    return pipeStream != null;
                }
            }

            [Fact]
            public void FallbackToCsc()
            {
                var ranLocal = false;
                var ranServer = false;
                var client = CreateClient(
                    compileFunc: (_, _, _, _) =>
                    {
                        ranLocal = true;
                        return 0;
                    },
                    compileOnServerFunc: (_, _, _) =>
                    {
                        ranServer = true;
                        return Task.FromResult<BuildResponse>(new RejectedBuildResponse(""));
                    });

                var exitCode = client.RunCompilation(new[] { "/shared" }, _buildPaths, pipeName: _pipeName).ExitCode;
                Assert.Equal(0, exitCode);
                Assert.True(ranLocal);
                Assert.True(ranServer);
                Assert.Equal(0, _serverDataList.Count);
            }
        }

        public sealed class TryParseClientArgsTest : BuildClientTests
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
                var name = BuildServerConnection.GetPipeName(path);
                Assert.Equal(name, BuildServerConnection.GetPipeName(path));
                Assert.Equal(name, BuildServerConnection.GetPipeName(path + Path.DirectorySeparatorChar));
                Assert.Equal(name, BuildServerConnection.GetPipeName(path + Path.DirectorySeparatorChar + Path.DirectorySeparatorChar));
            }

            [Fact]
            public void GetPipeNameForPathOptLength()
            {
                var path = string.Format(@"q:{0}the{0}path", Path.DirectorySeparatorChar);
                var name = BuildServerConnection.GetPipeName(path);
                // We only have ~50 total bytes to work with on mac, so the base path must be small
                Assert.Equal(43, name.Length);
            }
        }
    }
}
