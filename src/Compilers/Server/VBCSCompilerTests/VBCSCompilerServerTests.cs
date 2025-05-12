// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CommandLine;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CompilerServer.UnitTests
{
    public class VBCSCompilerServerTests : IDisposable
    {
        public TempRoot TempRoot { get; } = new TempRoot();

        public void Dispose()
        {
            TempRoot.Dispose();
        }

        public class StartupTests : VBCSCompilerServerTests
        {
            [Fact]
            [WorkItem(217709, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/217709")]
            public async Task ShadowCopyAnalyzerAssemblyLoaderMissingDirectory()
            {
                var baseDirectory = Path.Combine(Path.GetTempPath(), TestBase.GetUniqueName());
                var shadowResolver = new ShadowCopyAnalyzerPathResolver(baseDirectory);
                var task = shadowResolver.DeleteLeftoverDirectoriesTask;
                await task;
                Assert.False(task.IsFaulted);
            }
        }

        public class ShutdownTests : VBCSCompilerServerTests
        {
            internal XunitCompilerServerLogger Logger { get; }

            public ShutdownTests(ITestOutputHelper testOutputHelper)
            {
                Logger = new XunitCompilerServerLogger(testOutputHelper);
            }

            private Task<int> RunShutdownAsync(string pipeName, bool waitForProcess = true, CancellationToken cancellationToken = default(CancellationToken))
            {
                var appSettings = new NameValueCollection();
                return new BuildServerController(appSettings, Logger).RunShutdownAsync(pipeName, waitForProcess, Timeout.Infinite, cancellationToken);
            }

            [Fact]
            public async Task Standard()
            {
                using var serverData = await ServerUtil.CreateServer(Logger);
                var exitCode = await RunShutdownAsync(serverData.PipeName, waitForProcess: false);
                Assert.Equal(CommonCompiler.Succeeded, exitCode);

                // Await the server task here to verify it actually shuts down vs. us shutting down the server.
                var listener = await serverData.ServerTask;
                Assert.Equal(
                    new CompletionData(CompletionReason.RequestCompleted, shutdownRequested: true),
                    listener.CompletionDataList.Single());
            }

            /// <summary>
            /// If there is no server running with the specified pipe name then it's not running and hence
            /// shutdown succeeded.
            /// </summary>
            /// <returns></returns>
            [Fact]
            public async Task NoServerMutex()
            {
                var pipeName = Guid.NewGuid().ToString();
                var exitCode = await RunShutdownAsync(pipeName, waitForProcess: false);
                Assert.Equal(CommonCompiler.Succeeded, exitCode);
            }

            [Fact]
            [WorkItem(34880, "https://github.com/dotnet/roslyn/issues/34880")]
            public async Task NoServerConnection()
            {
                using (var readyMre = new ManualResetEvent(initialState: false))
                using (var doneMre = new ManualResetEvent(initialState: false))
                {
                    var pipeName = Guid.NewGuid().ToString();
                    var mutexName = BuildServerConnection.GetServerMutexName(pipeName);
                    bool created = false;
                    bool connected = false;

                    var thread = new Thread(() =>
                    {
                        using (var mutex = BuildServerConnection.OpenOrCreateMutex(name: mutexName, createdNew: out created))
                        using (var stream = NamedPipeUtil.CreateServer(pipeName))
                        {
                            readyMre.Set();

                            // Get a client connection and then immediately close it.  Don't give any response.
                            stream.WaitForConnection();
                            connected = true;
                            stream.Close();

                            doneMre.WaitOne();
                            mutex.Dispose();
                        }
                    });

                    // Block until the mutex and named pipe is setup.
                    thread.Start();
                    readyMre.WaitOne();

                    var exitCode = await RunShutdownAsync(pipeName, waitForProcess: false);

                    // Let the fake server exit.
                    doneMre.Set();
                    thread.Join();

                    Assert.Equal(CommonCompiler.Failed, exitCode);
                    Assert.True(connected);
                    Assert.True(created);
                }
            }

            /// <summary>
            /// Here the server doesn't respond to the shutdown request but successfully shuts down before
            /// the client can error out.
            /// </summary>
            /// <returns></returns>
            [Fact]
            [WorkItem(34880, "https://github.com/dotnet/roslyn/issues/34880")]
            public async Task ServerShutdownsDuringProcessing()
            {
                using (var readyMre = new ManualResetEvent(initialState: false))
                using (var doneMre = new ManualResetEvent(initialState: false))
                {
                    var pipeName = Guid.NewGuid().ToString();
                    var mutexName = BuildServerConnection.GetServerMutexName(pipeName);
                    bool created = false;
                    bool connected = false;

                    var thread = new Thread(() =>
                    {
                        using (var stream = NamedPipeUtil.CreateServer(pipeName))
                        {
                            var mutex = BuildServerConnection.OpenOrCreateMutex(name: mutexName, createdNew: out created);
                            readyMre.Set();

                            stream.WaitForConnection();
                            connected = true;

                            // Client is waiting for a response.  Close the mutex now.  Then close the connection 
                            // so the client gets an error.
                            mutex.Dispose();
                            stream.Close();

                            doneMre.WaitOne();
                        }
                    });

                    // Block until the mutex and named pipe is setup.
                    thread.Start();
                    readyMre.WaitOne();

                    var exitCode = await RunShutdownAsync(pipeName, waitForProcess: false);

                    // Let the fake server exit.
                    doneMre.Set();
                    thread.Join();

                    Assert.Equal(CommonCompiler.Succeeded, exitCode);
                    Assert.True(connected);
                    Assert.True(created);
                }
            }

            /// <summary>
            /// A shutdown request should not abort an existing compilation.  It should be allowed to run to 
            /// completion.
            /// </summary>
            [Fact]
            public async Task ShutdownDoesNotAbortCompilation()
            {
                using var startedMre = new ManualResetEvent(initialState: false);
                using var finishedMre = new ManualResetEvent(initialState: false);

                // Create a compilation that is guaranteed to complete after the shutdown is seen. 
                var compilerServerHost = new TestableCompilerServerHost((request, cancellationToken) =>
                {
                    startedMre.Set();
                    finishedMre.WaitOne();
                    return ProtocolUtil.EmptyBuildResponse;
                });

                using var serverData = await ServerUtil.CreateServer(Logger, compilerServerHost: compilerServerHost);

                // Get the server to the point that it is running the compilation.
                var compileTask = serverData.SendAsync(ProtocolUtil.EmptyCSharpBuildRequest);
                startedMre.WaitOne();

                // The compilation is now in progress, send the shutdown and verify that the 
                // compilation is still running.
                await serverData.SendShutdownAsync();
                Assert.False(compileTask.IsCompleted);

                // Now complete the compilation and verify that it actually ran to completion despite
                // there being a shutdown request.
                finishedMre.Set();
                var response = await compileTask;
                Assert.True(response is CompletedBuildResponse { ReturnCode: 0 });

                // Now verify the server actually shuts down since there is no work remaining.
                var listener = await serverData.ServerTask;
                Assert.False(listener.KeepAliveHit);
            }

            /// <summary>
            /// Multiple clients should be able to send shutdown requests to the server.
            /// </summary>
            [Fact]
            public async Task ShutdownRepeated()
            {
                using var startedMre = new ManualResetEvent(initialState: false);
                using var finishedMre = new ManualResetEvent(initialState: false);

                // Create a compilation that is guaranteed to complete after the shutdown is seen. 
                var compilerServerHost = new TestableCompilerServerHost((request, cancellationToken) =>
                {
                    startedMre.Set();
                    finishedMre.WaitOne();
                    return ProtocolUtil.EmptyBuildResponse;
                });

                using var serverData = await ServerUtil.CreateServer(Logger, compilerServerHost: compilerServerHost);

                // Get the server to the point that it is running the compilation.
                var compileTask = serverData.SendAsync(ProtocolUtil.EmptyCSharpBuildRequest);
                startedMre.WaitOne();

                // The compilation is now in progress, send the shutdown and verify that the 
                // compilation is still running.
                await serverData.SendShutdownAsync();
                await serverData.SendShutdownAsync();

                // Now complete the compilation and verify that it actually ran to completion despite
                // there being a shutdown request.
                finishedMre.Set();
                var response = await compileTask;
                Assert.True(response is CompletedBuildResponse { ReturnCode: 0 });

                // Now verify the server actually shuts down since there is no work remaining.
                var listener = await serverData.ServerTask;
                Assert.False(listener.KeepAliveHit);
            }
        }

        public class KeepAliveTests : VBCSCompilerServerTests
        {
            internal XunitCompilerServerLogger Logger { get; }

            public KeepAliveTests(ITestOutputHelper testOutputHelper)
            {
                Logger = new XunitCompilerServerLogger(testOutputHelper);
            }

            /// <summary>
            /// Ensure server hits keep alive when processing no connections
            /// </summary>
            [Fact]
            public async Task NoConnections()
            {
                var compilerServerHost = new TestableCompilerServerHost((request, cancellationToken) => ProtocolUtil.EmptyBuildResponse);
                using var serverData = await ServerUtil.CreateServer(
                    Logger,
                    keepAlive: TimeSpan.FromSeconds(3),
                    compilerServerHost: compilerServerHost);

                // Don't use Complete here because we want to see the server shutdown naturally
                var listener = await serverData.ServerTask;
                Assert.True(listener.KeepAliveHit);
                Assert.Equal(0, listener.CompletionDataList.Count);
            }

            /// <summary>
            /// Ensure server respects keep alive and shuts down after processing a single connection.
            /// </summary>
            [ConditionalTheory(typeof(WindowsOnly), Reason = "https://github.com/dotnet/roslyn/issues/46447")]
            [InlineData(1)]
            [InlineData(2)]
            [InlineData(3)]
            public async Task SimpleCases(int connectionCount)
            {
                var compilerServerHost = new TestableCompilerServerHost((request, cancellationToken) => ProtocolUtil.EmptyBuildResponse);
                using var serverData = await ServerUtil.CreateServer(Logger, compilerServerHost: compilerServerHost);
                var workingDirectory = TempRoot.CreateDirectory().Path;

                for (var i = 0; i < connectionCount; i++)
                {
                    var request = i + 1 >= connectionCount
                        ? ProtocolUtil.CreateEmptyCSharpWithKeepAlive(TimeSpan.FromSeconds(3), workingDirectory)
                        : ProtocolUtil.EmptyCSharpBuildRequest;
                    await serverData.SendAsync(request);
                }

                // Don't use Complete here because we want to see the server shutdown naturally
                var listener = await serverData.ServerTask;
                Assert.True(listener.KeepAliveHit);
                Assert.Equal(connectionCount, listener.CompletionDataList.Count);
                Assert.All(listener.CompletionDataList, cd => Assert.Equal(CompletionReason.RequestCompleted, cd.Reason));
            }

            /// <summary>
            /// Ensure server respects keep alive and shuts down after processing simultaneous connections.
            /// </summary>
            [ConditionalTheory(typeof(WindowsOnly), Reason = "https://github.com/dotnet/roslyn/issues/46447")]
            [InlineData(2)]
            [InlineData(3)]
            public async Task SimultaneousConnections(int connectionCount)
            {
                using var readyMre = new ManualResetEvent(initialState: false);
                var compilerServerHost = new TestableCompilerServerHost((request, cancellationToken) =>
                {
                    readyMre.WaitOne();
                    return ProtocolUtil.EmptyBuildResponse;
                });

                using var serverData = await ServerUtil.CreateServer(Logger, compilerServerHost: compilerServerHost);
                var list = new List<Task>();
                for (var i = 0; i < connectionCount; i++)
                {
                    list.Add(serverData.SendAsync(ProtocolUtil.EmptyCSharpBuildRequest));
                }

                readyMre.Set();

                var workingDirectory = TempRoot.CreateDirectory().Path;
                await serverData.SendAsync(ProtocolUtil.CreateEmptyCSharpWithKeepAlive(TimeSpan.FromSeconds(3), workingDirectory));
                await Task.WhenAll(list);

                // Don't use Complete here because we want to see the server shutdown naturally
                var listener = await serverData.ServerTask;
                Assert.True(listener.KeepAliveHit);
                Assert.Equal(connectionCount + 1, listener.CompletionDataList.Count);
                Assert.All(listener.CompletionDataList, cd => Assert.Equal(CompletionReason.RequestCompleted, cd.Reason));
            }
        }

        public class MiscTests : VBCSCompilerServerTests
        {
            internal XunitCompilerServerLogger Logger { get; }

            public MiscTests(ITestOutputHelper testOutputHelper)
            {
                Logger = new XunitCompilerServerLogger(testOutputHelper);
            }

            [Fact]
            public async Task CompilationExceptionShouldShutdown()
            {
                var hitCompilation = false;
                var compilerServerHost = new TestableCompilerServerHost(delegate
                {
                    hitCompilation = true;
                    throw new Exception("");
                });
                using var serverData = await ServerUtil.CreateServer(Logger, compilerServerHost: compilerServerHost);

                var response = await serverData.SendAsync(ProtocolUtil.EmptyBasicBuildRequest);
                Assert.True(response is RejectedBuildResponse);

                // Don't use Complete here because we want to see the server shutdown naturally
                var listener = await serverData.ServerTask;
                Assert.False(listener.KeepAliveHit);
                Assert.Equal(CompletionData.RequestError, listener.CompletionDataList.Single());
                Assert.True(hitCompilation);
            }

            [Fact]
            public async Task AnalyzerInconsistencyShouldShutdown()
            {
                var compilerServerHost = new TestableCompilerServerHost(delegate
                {
                    return new AnalyzerInconsistencyBuildResponse(new ReadOnlyCollection<string>(Array.Empty<string>()));
                });

                using var serverData = await ServerUtil.CreateServer(Logger, compilerServerHost: compilerServerHost);

                var response = await serverData.SendAsync(ProtocolUtil.EmptyBasicBuildRequest);
                Assert.True(response is AnalyzerInconsistencyBuildResponse);

                // Don't use Complete here because we want to see the server shutdown naturally
                var listener = await serverData.ServerTask;
                Assert.False(listener.KeepAliveHit);
                Assert.Equal(CompletionData.RequestError, listener.CompletionDataList.Single());
            }
        }

        public class ParseCommandLineTests : VBCSCompilerServerTests
        {
            private string _pipeName;
            private bool _shutdown;

            private bool Parse(params string[] args)
            {
                return BuildServerController.ParseCommandLine(args, out _pipeName, out _shutdown);
            }

            [Fact]
            public void Nothing()
            {
                Assert.True(Parse());
                Assert.Null(_pipeName);
                Assert.False(_shutdown);
            }

            [Fact]
            public void PipeOnly()
            {
                Assert.True(Parse("-pipename:test"));
                Assert.Equal("test", _pipeName);
                Assert.False(_shutdown);
            }

            [Fact]
            public void Shutdown()
            {
                Assert.True(Parse("-shutdown"));
                Assert.Null(_pipeName);
                Assert.True(_shutdown);
            }

            [Fact]
            public void PipeAndShutdown()
            {
                Assert.True(Parse("-pipename:test", "-shutdown"));
                Assert.Equal("test", _pipeName);
                Assert.True(_shutdown);
            }

            [Fact]
            public void BadArg()
            {
                Assert.False(Parse("-invalid"));
                Assert.False(Parse("name"));
            }
        }
    }
}
