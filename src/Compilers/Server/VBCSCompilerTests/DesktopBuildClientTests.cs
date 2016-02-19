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
                RequestLanguage langauge,
                CompileFunc compileFunc,
                string pipeName,
                Func<string, bool> createServerFunc,
                Func<Task<BuildResponse>> runServerCompilationFunc) : base(langauge, compileFunc, new Mock<IAnalyzerAssemblyLoader>().Object)
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
        }

        public sealed class ServerTests : DesktopBuildClientTests
        {
            private readonly string _pipeName = Guid.NewGuid().ToString("N");
            private readonly TempDirectory _tempDirectory;
            private readonly BuildPaths _buildPaths;
            private readonly List<ServerData> _serverDataList = new List<ServerData>();
            private bool _allowServer = true;
            private int _failedCreatedServerCount = 0;

            public ServerTests()
            {
                _tempDirectory = Temp.CreateDirectory();
                _buildPaths = ServerUtil.CreateBuildPaths(workingDir: _tempDirectory.Path);
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

                var serverData = ServerUtil.CreateServer(pipeName);
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
                                                   name: BuildProtocolConstants.GetServerMutexName(_pipeName),
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

            [Fact]
            public void Shared()
            {
                Assert.True(Parse("/shared", "test.cs"));
                Assert.True(_hasShared);
                Assert.Null(_sessionKey);
                Assert.Equal(new[] { "test.cs" }, _parsedArgs);
            }

            [Fact]
            public void SharedWithSessionKey()
            {
                Assert.True(Parse("/shared:pipe", "test.cs"));
                Assert.True(_hasShared);
                Assert.Equal("pipe", _sessionKey);
                Assert.Equal(new[] { "test.cs" }, _parsedArgs);

                Assert.True(Parse("/shared:1:2", "test.cs"));
                Assert.True(_hasShared);
                Assert.Equal("1:2", _sessionKey);
                Assert.Equal(new[] { "test.cs" }, _parsedArgs);

                Assert.True(Parse("/shared=1:2", "test.cs"));
                Assert.True(_hasShared);
                Assert.Equal("1:2", _sessionKey);
                Assert.Equal(new[] { "test.cs" }, _parsedArgs);
            }

            [Fact]
            public void SharedWithEmptySessionKey()
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
                Assert.Null(_sessionKey);
                Assert.Equal(new[] { "test.cs" }, _parsedArgs);

                Assert.True(Parse("/keepalive:100", "/shared", "test.cs"));
                Assert.True(_hasShared);
                Assert.Null(_sessionKey);
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
                Assert.Null(_sessionKey);

                Assert.True(Parse("/keepalive=100", "/shared", "test.cs"));
                Assert.Equal("100", _keepAlive);
                Assert.Equal(new[] { "test.cs" }, _parsedArgs);
                Assert.True(_hasShared);
                Assert.Null(_sessionKey);
            }
        }

        public class MiscTest
        {
            [Fact]
            public void GetBasePipeNameSlashes()
            {
                var path = string.Format(@"q:{0}the{0}path", Path.DirectorySeparatorChar);
                var name = DesktopBuildClient.GetBasePipeName(path);
                Assert.Equal(name, DesktopBuildClient.GetBasePipeName(path));
                Assert.Equal(name, DesktopBuildClient.GetBasePipeName(path + Path.DirectorySeparatorChar));
                Assert.Equal(name, DesktopBuildClient.GetBasePipeName(path + Path.DirectorySeparatorChar + Path.DirectorySeparatorChar));
            }
        }
    }
}
