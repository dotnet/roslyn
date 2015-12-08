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

namespace Microsoft.CodeAnalysis.CompilerServer.UnitTests
{
    public class DesktopBuildClientTests : TestBase
    {
        private sealed class TestableDesktopBuildClient : DesktopBuildClient
        {
            private readonly string _pipeName;
            private readonly Func<string, bool> _createServerFunc;

            public TestableDesktopBuildClient(
                RequestLanguage langauge,
                CompileFunc compileFunc,
                string pipeName,
                Func<string, bool> createServerFunc) : base(langauge, compileFunc, new Mock<IAnalyzerAssemblyLoader>().Object)
            {
                _pipeName = pipeName;
                _createServerFunc = createServerFunc;
            }

            protected override string GetPipeName(BuildPaths buildPaths)
            {
                return _pipeName;
            }

            protected override bool TryCreateServer(string clientDir, string pipeName)
            {
                return _createServerFunc(pipeName);
            }

            protected override int HandleResponse(BuildResponse response, List<string> arguments, BuildPaths buildPaths)
            {
                // Override the base so we don't print the compilation output to Console.Out
                return 0;
            }
        }

        public sealed class ServerTests : DesktopBuildClientTests
        {
            private readonly string _pipeName = Guid.NewGuid().ToString("N");
            private readonly TempDirectory _tempDirectory;
            private readonly BuildPaths _buildPaths;
            private readonly List<ServerData> _serverDataList = new List<ServerData>();

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

            private TestableDesktopBuildClient CreateClient(RequestLanguage? language = null, CompileFunc compileFunc = null)
            {
                language = language ?? RequestLanguage.CSharpCompile;
                compileFunc = compileFunc ?? delegate { return 0; };
                return new TestableDesktopBuildClient(language.Value, compileFunc, _pipeName, TryCreateServer);
            }

            private bool TryCreateServer(string pipeName)
            {
                var serverData = ServerUtil.CreateServer(pipeName);
                _serverDataList.Add(serverData);
                return true;
            }

            [Fact]
            public void OnlyStartsOneServer()
            {
                var ranLocal = false;
                var client = CreateClient(compileFunc: delegate
                {
                    ranLocal = true;
                    throw new Exception();
                });

                for (var i = 0; i < 5; i++)
                {
                    client.RunCompilation(new[] { "/shared" }, _buildPaths);
                }

                Assert.Equal(1, _serverDataList.Count);
                Assert.False(ranLocal);
            }
        }

        public sealed class TryParseClientArgsTest : DesktopBuildClientTests
        {
            private bool _hasShared;
            private string _keepAlive;
            private string _errorMessage;
            private string _pipeName;
            private List<string> _parsedArgs;

            private bool Parse(params string[] args)
            {
                return CommandLineParser.TryParseClientArgs(
                    args,
                    out _parsedArgs,
                    out _hasShared,
                    out _keepAlive,
                    out _pipeName,
                    out _errorMessage);
            }

            [Fact]
            public void Shared()
            {
                Assert.True(Parse("/shared", "test.cs"));
                Assert.True(_hasShared);
                Assert.Null(_pipeName);
                Assert.Equal(_parsedArgs, new[] { "test.cs" });
            }

            [Fact]
            public void SharedWithPipe()
            {
                Assert.True(Parse("/shared:pipe", "test.cs"));
                Assert.True(_hasShared);
                Assert.Equal("pipe", _pipeName);
                Assert.Equal(_parsedArgs, new[] { "test.cs" });
            }

            [Fact]
            public void Basic()
            {
                Assert.True(Parse("test.cs"));
                Assert.False(_hasShared);
                Assert.Null(_pipeName);
                Assert.Equal(_parsedArgs, new[] { "test.cs" });
            }
        }
    }
}
