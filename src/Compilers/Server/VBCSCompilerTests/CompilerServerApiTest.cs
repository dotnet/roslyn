// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

namespace Microsoft.CodeAnalysis.CompilerServer.UnitTests
{
    public class CompilerServerApiTest : TestBase
    {
        private static readonly BuildRequest s_emptyCSharpBuildRequest = new BuildRequest(
            BuildProtocolConstants.ProtocolVersion,
            RequestLanguage.CSharpCompile,
            BuildProtocolConstants.GetCommitHash(),
            ImmutableArray<BuildRequest.Argument>.Empty);

        private static readonly BuildResponse s_emptyBuildResponse = new CompletedBuildResponse(
            returnCode: 0,
            utf8output: false,
            output: string.Empty);

        private const string HelloWorldSourceText = @"
using System;
class Hello
{
    static void Main()
    {
        Console.WriteLine(""Hello, world.""); 
    }
}";

        private static Task TaskFromException(Exception e)
        {
            return TaskFromException<bool>(e);
        }

        private static Task<T> TaskFromException<T>(Exception e)
        {
            var source = new TaskCompletionSource<T>();
            source.SetException(e);
            return source.Task;
        }

        private static IClientConnection CreateClientConnection(CompletionReason completionReason, TimeSpan? keepAlive = null)
        {
            var task = Task.FromResult(new ConnectionData(completionReason, keepAlive));
            return CreateClientConnection(task);
        }

        private static IClientConnection CreateClientConnection(Task<ConnectionData> task)
        {
            var connection = new Mock<IClientConnection>();
            connection
                .Setup(x => x.HandleConnection(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .Returns(task);
            return connection.Object;
        }

        private static IClientConnectionHost CreateClientConnectionHost(params Task<IClientConnection>[] connections)
        {
            var host = new Mock<IClientConnectionHost>();
            var index = 0;
            host
                .Setup(x => x.CreateListenTask(It.IsAny<CancellationToken>()))
                .Returns((CancellationToken ct) => connections[index++]);

            return host.Object;
        }

        private async Task<BuildRequest> CreateBuildRequest(string sourceText, TimeSpan? keepAlive = null)
        {
            var directory = Temp.CreateDirectory();
            var file = directory.CreateFile("temp.cs");
            await file.WriteAllTextAsync(sourceText).ConfigureAwait(false);

            var builder = ImmutableArray.CreateBuilder<BuildRequest.Argument>();
            if (keepAlive.HasValue)
            {
                builder.Add(new BuildRequest.Argument(BuildProtocolConstants.ArgumentId.KeepAlive, argumentIndex: 0, value: keepAlive.Value.TotalSeconds.ToString()));
            }

            builder.Add(new BuildRequest.Argument(BuildProtocolConstants.ArgumentId.CurrentDirectory, argumentIndex: 0, value: directory.Path));
            builder.Add(new BuildRequest.Argument(BuildProtocolConstants.ArgumentId.CommandLineArgument, argumentIndex: 0, value: file.Path));

            return new BuildRequest(
                BuildProtocolConstants.ProtocolVersion,
                RequestLanguage.CSharpCompile,
                BuildProtocolConstants.GetCommitHash(),
                builder.ToImmutable());
        }

        /// <summary>
        /// Run a C# compilation against the given source text using the provided named pipe name.
        /// </summary>
        private async Task<BuildResponse> RunCSharpCompile(string pipeName, string sourceText, TimeSpan? keepAlive = null)
        {
            using (var namedPipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut))
            {
                var buildRequest = await CreateBuildRequest(sourceText, keepAlive).ConfigureAwait(false);
                namedPipe.Connect(Timeout.Infinite);
                await buildRequest.WriteAsync(namedPipe, default(CancellationToken)).ConfigureAwait(false);
                return await BuildResponse.ReadAsync(namedPipe, default(CancellationToken)).ConfigureAwait(false);
            }
        }

        private static Mock<IClientConnectionHost> CreateNopClientConnectionHost()
        {
            var host = new Mock<IClientConnectionHost>();
            host
                .Setup(x => x.CreateListenTask(It.IsAny<CancellationToken>()))
                .Returns(new TaskCompletionSource<IClientConnection>().Task);
            return host;
        }

        private static Task<T> FromException<T>(Exception ex)
        {
            var source = new TaskCompletionSource<T>();
            source.SetException(ex);
            return source.Task;
        }

        [Fact]
        public async Task ClientConnectionThrowsHandlingBuild()
        {
            var ex = new Exception();
            var clientConnection = new Mock<IClientConnection>();
            clientConnection
                .Setup(x => x.HandleConnection(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .Returns(FromException<ConnectionData>(ex));

            var task = Task.FromResult(clientConnection.Object);

            var connectionData = await ServerDispatcher.HandleClientConnection(task).ConfigureAwait(true);
            Assert.Equal(CompletionReason.ClientException, connectionData.CompletionReason);
            Assert.Null(connectionData.KeepAlive);
        }

        [Fact]
        public async Task ClientConnectionThrowsConnecting()
        {
            var ex = new Exception();
            var task = FromException<IClientConnection>(ex);
            var connectionData = await ServerDispatcher.HandleClientConnection(task).ConfigureAwait(true);
            Assert.Equal(CompletionReason.CompilationNotStarted, connectionData.CompletionReason);
            Assert.Null(connectionData.KeepAlive);
        }

        [Fact]
        public void KeepAliveNoConnections()
        {
            var keepAlive = TimeSpan.FromSeconds(3);
            var connectionHost = new Mock<IClientConnectionHost>();
            connectionHost
                .Setup(x => x.CreateListenTask(It.IsAny<CancellationToken>()))
                .Returns(new TaskCompletionSource<IClientConnection>().Task);

            var listener = new TestableDiagnosticListener();
            var dispatcher = new ServerDispatcher(connectionHost.Object, listener);
            var startTime = DateTime.Now;
            dispatcher.ListenAndDispatchConnections(keepAlive);

            Assert.True(listener.HitKeepAliveTimeout);
        }

        /// <summary>
        /// Ensure server respects keep alive and shuts down after processing a single connection.
        /// </summary>
        [Fact]
        public void KeepAliveAfterSingleConnection()
        {
            var connection = CreateClientConnection(CompletionReason.CompilationCompleted);
            var host = CreateClientConnectionHost(
                Task.FromResult(connection),
                new TaskCompletionSource<IClientConnection>().Task);
            var listener = new TestableDiagnosticListener();
            var keepAlive = TimeSpan.FromSeconds(1);
            var dispatcher = new ServerDispatcher(host, listener);
            dispatcher.ListenAndDispatchConnections(keepAlive);

            Assert.Equal(1, listener.CompletedCount);
            Assert.True(listener.LastProcessedTime.HasValue);
            Assert.True(listener.HitKeepAliveTimeout);
        }

        /// <summary>
        /// Ensure server respects keep alive and shuts down after processing multiple connections.
        /// </summary>
        [Fact]
        public void KeepAliveAfterMultipleConnection()
        {
            var count = 5;
            var list = new List<Task<IClientConnection>>();
            for (var i = 0; i < count; i++)
            {
                var connection = CreateClientConnection(CompletionReason.CompilationCompleted);
                list.Add(Task.FromResult(connection));
            }

            list.Add(new TaskCompletionSource<IClientConnection>().Task);
            var host = CreateClientConnectionHost(list.ToArray());
            var listener = new TestableDiagnosticListener();
            var keepAlive = TimeSpan.FromSeconds(1);
            var dispatcher = new ServerDispatcher(host, listener);
            dispatcher.ListenAndDispatchConnections(keepAlive);

            Assert.Equal(count, listener.CompletedCount);
            Assert.True(listener.LastProcessedTime.HasValue);
            Assert.True(listener.HitKeepAliveTimeout);
        }

        /// <summary>
        /// Ensure server respects keep alive and shuts down after processing simultaneous connections.
        /// </summary>
        [Fact]
        public async Task KeepAliveAfterSimultaneousConnection()
        {
            var totalCount = 2;
            var readySource = new TaskCompletionSource<bool>();
            var list = new List<TaskCompletionSource<ConnectionData>>();
            var host = new Mock<IClientConnectionHost>();
            host
                .Setup(x => x.CreateListenTask(It.IsAny<CancellationToken>()))
                .Returns((CancellationToken ct) =>
                {
                    if (list.Count < totalCount)
                    {
                        var source = new TaskCompletionSource<ConnectionData>();
                        var client = CreateClientConnection(source.Task);
                        list.Add(source);
                        return Task.FromResult(client);
                    }

                    readySource.SetResult(true);
                    return new TaskCompletionSource<IClientConnection>().Task;
                });

            var keepAlive = TimeSpan.FromSeconds(1);
            var listener = new TestableDiagnosticListener();
            var dispatcherTask = Task.Run(() =>
            {
                var dispatcher = new ServerDispatcher(host.Object, listener);
                dispatcher.ListenAndDispatchConnections(keepAlive);
            });

            await readySource.Task.ConfigureAwait(true);
            foreach (var source in list)
            {
                source.SetResult(new ConnectionData(CompletionReason.CompilationCompleted));
            }

            await dispatcherTask.ConfigureAwait(true);
            Assert.Equal(totalCount, listener.CompletedCount);
            Assert.True(listener.LastProcessedTime.HasValue);
            Assert.True(listener.HitKeepAliveTimeout);
        }

        [Fact]
        public void ClientExceptionShouldBeginShutdown()
        {
            var client = new Mock<IClientConnection>();
            client
                .Setup(x => x.HandleConnection(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .Throws(new Exception());

            var listenCancellationToken = default(CancellationToken);
            var first = true;

            var host = new Mock<IClientConnectionHost>();
            host
                .Setup(x => x.CreateListenTask(It.IsAny<CancellationToken>()))
                .Returns((CancellationToken cancellationToken) =>
                {
                    if (first)
                    {
                        first = false;
                        return Task.FromResult(client.Object);
                    }
                    else
                    {
                        listenCancellationToken = cancellationToken;
                        return Task.Delay(-1, cancellationToken).ContinueWith<IClientConnection>(_ => null, TaskScheduler.Default);
                    }
                });

            var listener = new TestableDiagnosticListener();
            var dispatcher = new ServerDispatcher(host.Object, listener);
            dispatcher.ListenAndDispatchConnections(TimeSpan.FromSeconds(10));

            Assert.True(listener.HasDetectedBadConnection);
            Assert.True(listenCancellationToken.IsCancellationRequested);
        }

        [Fact]
        public void MutexStopsServerStarting()
        {
            var pipeName = Guid.NewGuid().ToString("N");
            var mutexName = BuildServerConnection.GetServerMutexName(pipeName);

            bool holdsMutex;
            using (var mutex = new Mutex(initiallyOwned: true,
                                         name: mutexName,
                                         createdNew: out holdsMutex))
            {
                Assert.True(holdsMutex);
                try
                {
                    var host = new Mock<IClientConnectionHost>(MockBehavior.Strict);
                    var result = BuildServerController.CreateAndRunServer(
                        pipeName,
                        Path.GetTempPath(),
                        host.Object,
                        keepAlive: null);
                    Assert.Equal(CommonCompiler.Failed, result);
                }
                finally
                {
                    mutex.ReleaseMutex();
                }
            }
        }

        [Fact]
        public void MutexAcquiredWhenRunningServer()
        {
            var pipeName = Guid.NewGuid().ToString("N");
            var mutexName = BuildServerConnection.GetServerMutexName(pipeName);
            var host = new Mock<IClientConnectionHost>(MockBehavior.Strict);
            host
                .Setup(x => x.CreateListenTask(It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    // Use a thread instead of Task to guarantee this code runs on a different
                    // thread and we can validate the mutex state. 
                    var source = new TaskCompletionSource<bool>();
                    var thread = new Thread(_ =>
                    {
                        try
                        {
                            Assert.True(BuildServerConnection.WasServerMutexOpen(mutexName));
                            source.SetResult(true);
                        }
                        catch (Exception ex)
                        {
                            source.SetException(ex);
                            throw;
                        }
                    });

                    // Synchronously wait here.  Don't returned a Task value because we need to 
                    // ensure the above check completes before the server hits a timeout and 
                    // releases the mutex. 
                    thread.Start();
                    source.Task.Wait();

                    return new TaskCompletionSource<IClientConnection>().Task;
                });

            var result = BuildServerController.CreateAndRunServer(
                pipeName,
                Path.GetTempPath(),
                host.Object,
                keepAlive: TimeSpan.FromSeconds(1));
            Assert.Equal(CommonCompiler.Succeeded, result);
        }

        [Fact]
        public async Task ShutdownRequestDirect()
        {
            using (var serverData = await ServerUtil.CreateServer())
            {
                var serverProcessId = await ServerUtil.SendShutdown(serverData.PipeName);
                Assert.Equal(Process.GetCurrentProcess().Id, serverProcessId);
                await serverData.Verify(connections: 1, completed: 1);
            }
        }

        /// <summary>
        /// A shutdown request should not abort an existing compilation.  It should be allowed to run to 
        /// completion.
        /// </summary>
        [ConditionalFact(typeof(DesktopOnly))]
        public async Task ShutdownDoesNotAbortCompilation()
        {
            var host = new TestableCompilerServerHost();

            using (var startedMre = new ManualResetEvent(initialState: false))
            using (var finishedMre = new ManualResetEvent(initialState: false))
            using (var serverData = await ServerUtil.CreateServer(compilerServerHost: host))
            {
                // Create a compilation that is guaranteed to complete after the shutdown is seen. 
                host.RunCompilation = (request, cancellationToken) =>
                {
                    startedMre.Set();
                    finishedMre.WaitOne();
                    return s_emptyBuildResponse;
                };

                var compileTask = ServerUtil.Send(serverData.PipeName, s_emptyCSharpBuildRequest);
                startedMre.WaitOne();

                // The compilation is now in progress, send the shutdown.
                await ServerUtil.SendShutdown(serverData.PipeName);
                Assert.False(compileTask.IsCompleted);
                finishedMre.Set();

                var response = await compileTask;
                Assert.Equal(BuildResponse.ResponseType.Completed, response.Type);
                Assert.Equal(0, ((CompletedBuildResponse)response).ReturnCode);

                await serverData.Verify(connections: 2, completed: 2);
            }
        }

        /// <summary>
        /// Multiple clients should be able to send shutdown requests to the server.
        /// </summary>
        /// <returns></returns>
        [ConditionalFact(typeof(DesktopOnly))]
        public async Task ShutdownRepeated()
        {
            var host = new TestableCompilerServerHost();

            using (var startedMre = new ManualResetEvent(initialState: false))
            using (var finishedMre = new ManualResetEvent(initialState: false))
            using (var serverData = await ServerUtil.CreateServer(compilerServerHost: host))
            {
                // Create a compilation that is guaranteed to complete after the shutdown is seen. 
                host.RunCompilation = (request, cancellationToken) =>
                {
                    startedMre.Set();
                    finishedMre.WaitOne();
                    return s_emptyBuildResponse;
                };

                var compileTask = ServerUtil.Send(serverData.PipeName, s_emptyCSharpBuildRequest);
                startedMre.WaitOne();

                for (var i = 0; i < 10; i++)
                {
                    // The compilation is now in progress, send the shutdown.
                    var processId = await ServerUtil.SendShutdown(serverData.PipeName);
                    Assert.Equal(Process.GetCurrentProcess().Id, processId);
                    Assert.False(compileTask.IsCompleted);
                }

                finishedMre.Set();

                var response = await compileTask;
                Assert.Equal(BuildResponse.ResponseType.Completed, response.Type);
                Assert.Equal(0, ((CompletedBuildResponse)response).ReturnCode);

                await serverData.Verify(connections: 11, completed: 11);
            }
        }

        [ConditionalFact(typeof(DesktopOnly))]
        public async Task CancelWillCancelCompilation()
        {
            var host = new TestableCompilerServerHost();

            using (var serverData = await ServerUtil.CreateServer(compilerServerHost: host))
            using (var mre = new ManualResetEvent(initialState: false))
            {
                const int requestCount = 5;
                var count = 0;

                host.RunCompilation = (request, cancellationToken) =>
                {
                    if (Interlocked.Increment(ref count) == requestCount)
                    {
                        mre.Set();
                    }

                    cancellationToken.WaitHandle.WaitOne();
                    return new RejectedBuildResponse();
                };

                var list = new List<Task<BuildResponse>>();
                for (var i = 0; i < requestCount; i++)
                {
                    var task = ServerUtil.Send(serverData.PipeName, s_emptyCSharpBuildRequest);
                    list.Add(task);
                }

                // Wait until all of the connections are being processed by the server then cancel. 
                mre.WaitOne();
                serverData.CancellationTokenSource.Cancel();

                var stats = await serverData.Complete();
                Assert.Equal(requestCount, stats.Connections);
                Assert.Equal(requestCount, count);

                foreach (var task in list)
                {
                    var threw = false;
                    try
                    {
                        await task;
                    }
                    catch
                    {
                        threw = true;
                    }

                    Assert.True(threw);
                }
            }
        }

        [WorkItem(13995, "https://github.com/dotnet/roslyn/issues/13995")]
        [Fact]
        public async Task RejectEmptyTempPath()
        {
            using var temp = new TempRoot();
            using var serverData = await ServerUtil.CreateServer();
            var request = BuildRequest.Create(RequestLanguage.CSharpCompile, workingDirectory: temp.CreateDirectory().Path, tempDirectory: null, BuildProtocolConstants.GetCommitHash(), libDirectory: null, args: Array.Empty<string>());
            var response = await ServerUtil.Send(serverData.PipeName, request);
            Assert.Equal(ResponseType.Rejected, response.Type);
        }

        [Fact]
        public async Task IncorrectProtocolReturnsMismatchedVersionResponse()
        {
            using (var serverData = await ServerUtil.CreateServer())
            {
                var buildResponse = await ServerUtil.Send(serverData.PipeName, new BuildRequest(1, RequestLanguage.CSharpCompile, "abc", new List<BuildRequest.Argument> { }));
                Assert.Equal(BuildResponse.ResponseType.MismatchedVersion, buildResponse.Type);
            }
        }

        [Fact]
        public async Task IncorrectServerHashReturnsIncorrectHashResponse()
        {
            using (var serverData = await ServerUtil.CreateServer())
            {
                var buildResponse = await ServerUtil.Send(serverData.PipeName, new BuildRequest(BuildProtocolConstants.ProtocolVersion, RequestLanguage.CSharpCompile, "abc", new List<BuildRequest.Argument> { }));
                Assert.Equal(BuildResponse.ResponseType.IncorrectHash, buildResponse.Type);
            }
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
        [WorkItem(33452, "https://github.com/dotnet/roslyn/issues/33452")]
        public void QuotePipeName_Desktop()
        {
            var serverInfo = BuildServerConnection.GetServerProcessInfo(@"q:\tools", "name with space");
            Assert.Equal(@"q:\tools\VBCSCompiler.exe", serverInfo.processFilePath);
            Assert.Equal(@"q:\tools\VBCSCompiler.exe", serverInfo.toolFilePath);
            Assert.Equal(@"""-pipename:name with space""", serverInfo.commandLineArguments);
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
            Assert.Equal(vbcsFilePath, serverInfo.toolFilePath);
            Assert.Equal($@"exec ""{vbcsFilePath}"" ""-pipename:name with space""", serverInfo.commandLineArguments);
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
