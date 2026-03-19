// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CommandLine;
using Moq;
using Roslyn.Test.Utilities;
using Xunit;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.IO;
using Microsoft.CodeAnalysis.Test.Utilities;
using static Microsoft.CodeAnalysis.CommandLine.BuildResponse;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CompilerServer.UnitTests
{
    public class CompilerServerApiTest : TestBase
    {
        internal ICompilerServerLogger Logger { get; }

        public CompilerServerApiTest(ITestOutputHelper testOutputHelper)
        {
            Logger = new XunitCompilerServerLogger(testOutputHelper);
        }

        [Fact]
        public void MutexStopsServerStarting()
        {
            var pipeName = Guid.NewGuid().ToString("N");
            var mutexName = BuildServerConnection.GetServerMutexName(pipeName);

            bool holdsMutex;
            using (var mutex = BuildServerConnection.OpenOrCreateMutex(
                                         name: mutexName,
                                         createdNew: out holdsMutex))
            {
                Assert.True(holdsMutex);
                try
                {
                    var host = new Mock<IClientConnectionHost>(MockBehavior.Strict);
                    var result = BuildServerController.CreateAndRunServer(
                        pipeName,
                        clientConnectionHost: host.Object,
                        keepAlive: null);
                    Assert.Equal(CommonCompiler.Failed, result);
                }
                finally
                {
                    mutex.Dispose();
                }
            }
        }

        [Fact]
        public void MutexAcquiredWhenRunningServer()
        {
            var pipeName = Guid.NewGuid().ToString("N");
            var mutexName = BuildServerConnection.GetServerMutexName(pipeName);
            var host = new TestableClientConnectionHost();
            bool? wasServerMutexOpen = null;
            host.Add(() =>
            {
                // Use a thread instead of Task to guarantee this code runs on a different
                // thread and we can validate the mutex state. 
                var tcs = new TaskCompletionSource<IClientConnection>();
                var thread = new Thread(_ =>
                {
                    wasServerMutexOpen = BuildServerConnection.WasServerMutexOpen(mutexName);

                    var client = new TestableClientConnection()
                    {
                        ReadBuildRequestFunc = _ => Task.FromResult(ProtocolUtil.EmptyCSharpBuildRequest),
                        WriteBuildResponseFunc = (r, _) => Task.CompletedTask,
                    };
                    tcs.SetResult(client);
                });

                thread.Start();
                return tcs.Task;
            });

            host.Add(() =>
            {
                var client = new TestableClientConnection()
                {
                    ReadBuildRequestFunc = _ => Task.FromResult(BuildRequest.CreateShutdown()),
                    WriteBuildResponseFunc = (r, _) => Task.CompletedTask,
                };
                return Task.FromResult<IClientConnection>(client);
            });

            var result = BuildServerController.CreateAndRunServer(
                pipeName,
                clientConnectionHost: host,
                keepAlive: TimeSpan.FromMilliseconds(-1));
            Assert.Equal(CommonCompiler.Succeeded, result);
            Assert.True(wasServerMutexOpen);
        }

        [WorkItem(13995, "https://github.com/dotnet/roslyn/issues/13995")]
        [Fact]
        public async Task RejectEmptyTempPath()
        {
            using var temp = new TempRoot();
            using var serverData = await ServerUtil.CreateServer(Logger);
            var request = BuildRequest.Create(RequestLanguage.CSharpCompile, workingDirectory: temp.CreateDirectory().Path, tempDirectory: null, compilerHash: BuildProtocolConstants.GetCommitHash(), libDirectory: null, args: Array.Empty<string>());
            var response = await serverData.SendAsync(request);
            Assert.Equal(ResponseType.Rejected, response.Type);
        }

        [Fact]
        public async Task IncorrectServerHashReturnsIncorrectHashResponse()
        {
            using var serverData = await ServerUtil.CreateServer(Logger);
            var buildResponse = await serverData.SendAsync(new BuildRequest(RequestLanguage.CSharpCompile, "abc", new List<BuildRequest.Argument> { }));
            Assert.Equal(BuildResponse.ResponseType.IncorrectHash, buildResponse.Type);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
        [WorkItem(33452, "https://github.com/dotnet/roslyn/issues/33452")]
        public void QuotePipeName_Desktop()
        {
            var serverInfo = BuildServerConnection.GetServerProcessInfo(@"q:\tools", "name with space");
            Assert.EndsWith(@"\dotnet.exe", serverInfo.processFilePath);
            AssertEx.Equal(@"exec ""q:\tools\VBCSCompiler.dll"" ""-pipename:name with space""", serverInfo.commandLineArguments);
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        [WorkItem(33452, "https://github.com/dotnet/roslyn/issues/33452")]
        public void QuotePipeName_CoreClr()
        {
            var toolDir = ExecutionConditionUtil.IsWindows
                ? @"q:\tools"
                : "/tools";
            var serverInfo = BuildServerConnection.GetServerProcessInfo(toolDir, "name with space");
            var vbcsFilePath = Path.Combine(toolDir, "VBCSCompiler.dll");
            AssertEx.Equal($@"exec ""{vbcsFilePath}"" ""-pipename:name with space""", serverInfo.commandLineArguments);
        }

        [Theory]
        [InlineData(@"OLqrNgkgZRf14qL91MdaUn8coiKckUIZCIEkpy0Lt18", "name with space", true, "basename")]
        [InlineData(@"8VDiJptv892LtWpeN86z76_YI0Yg0BV6j0SOv8CjQVA", @"ha""ha", true, "basename")]
        [InlineData(@"wKSU9psJMbkw+5+TFKLEf94aeslpEb3dDRpAw+9j4nw", @"jared", true, @"ha""ha")]
        [InlineData(@"0BDP4_GPWYQh9J_BknwhS9uAZAF_64PK4_VnNsddGZE", @"jared", false, @"ha""ha")]
        [InlineData(@"XroHfrjD1FTk7PcXcif2hZdmlVH_L0Pg+RUX01d_uQc", @"jared", false, @"ha\ha")]
        public void GetPipeNameCore(string expectedName, string userName, bool isAdmin, string compilerExeDir)
        {
            Assert.Equal(expectedName, BuildServerConnection.GetPipeName(userName, isAdmin, compilerExeDir));
        }
    }
}
