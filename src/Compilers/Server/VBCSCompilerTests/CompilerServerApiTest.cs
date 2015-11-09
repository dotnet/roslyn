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

namespace Microsoft.CodeAnalysis.CompilerServer.UnitTests
{
    public class CompilerServerApiTest : TestBase
    {
        private sealed class TestableClientConnection : IClientConnection
        {
            internal readonly string LoggingIdentifier = string.Empty;
            internal Task<BuildRequest> ReadBuildRequestTask = TaskFromException<BuildRequest>(new Exception());
            internal Task WriteBuildResponseTask = TaskFromException(new Exception());
            internal Task MonitorTask = TaskFromException(new Exception());
            internal Action CloseAction = delegate { };

            string IClientConnection.LoggingIdentifier
            {
                get { return LoggingIdentifier; }
            }

            Task<BuildRequest> IClientConnection.ReadBuildRequest(CancellationToken cancellationToken)
            {
                return ReadBuildRequestTask;
            }

            Task IClientConnection.WriteBuildResponse(BuildResponse response, CancellationToken cancellationToken)
            {
                return WriteBuildResponseTask;
            }

            Task IClientConnection.CreateMonitorDisconnectTask(CancellationToken cancellationToken)
            {
                return MonitorTask;
            }

            void IClientConnection.Close()
            {
                CloseAction();
            }
        }

        private sealed class TestableDiagnosticListener : IDiagnosticListener
        {
            public int ProcessedCount;
            public DateTime? LastProcessedTime;
            public TimeSpan? KeepAlive;

            public void ConnectionProcessed(int count)
            {
                ProcessedCount += count;
                LastProcessedTime = DateTime.Now;
            }

            public void UpdateKeepAlive(TimeSpan timeSpan)
            {
                KeepAlive = timeSpan;
            }
        }

        private static readonly BuildRequest s_emptyCSharpBuildRequest = new BuildRequest(
            1,
            RequestLanguage.CSharpCompile,
            ImmutableArray<BuildRequest.Argument>.Empty);

        private static readonly BuildResponse s_emptyBuildResponse = new CompletedBuildResponse(
            returnCode: 0,
            utf8output: false,
            output: string.Empty,
            errorOutput: string.Empty);

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

        /// <summary>
        /// This returns an <see cref="IRequestHandler"/> that always returns <see cref="CompletedBuildResponse"/> without
        /// doing any work.
        /// </summary>
        private static Mock<IRequestHandler> CreateNopRequestHandler()
        {
            var requestHandler = new Mock<IRequestHandler>();
            requestHandler
                .Setup(x => x.HandleRequest(It.IsAny<BuildRequest>(), It.IsAny<CancellationToken>()))
                .Returns(new CompletedBuildResponse(0, utf8output: false, output: string.Empty, errorOutput: string.Empty));
            return requestHandler;
        }

        private static Mock<ICompilerServerHost> CreateNopCompilerServerHost()
        {
            var host = new Mock<ICompilerServerHost>();
            host
                .Setup(x => x.CreateListenTask(It.IsAny<CancellationToken>()))
                .Returns(new TaskCompletionSource<IClientConnection>().Task);
            return host;
        }

        [Fact]
        public void NotifyCallBackOnRequestHandlerException()
        {
            var clientConnection = new TestableClientConnection();
            clientConnection.MonitorTask = Task.Delay(-1);
            clientConnection.ReadBuildRequestTask = Task.FromResult(s_emptyCSharpBuildRequest);

            var ex = new Exception();
            var handler = new Mock<IRequestHandler>();
            handler
                .Setup(x => x.HandleRequest(It.IsAny<BuildRequest>(), It.IsAny<CancellationToken>()))
                .Throws(ex);

            var invoked = false;
            FatalError.OverwriteHandler((providedEx) =>
            {
                Assert.Same(ex, providedEx);
                invoked = true;
            });
            var client = new Connection(clientConnection, handler.Object);

            Assert.Throws(typeof(AggregateException), () => client.ServeConnection().Wait());
            Assert.True(invoked);
        }

        [Fact]
        public void ClientDisconnectCancelBuildAndReturnsFailure()
        {
            var clientConnection = new TestableClientConnection();
            clientConnection.ReadBuildRequestTask = Task.FromResult(s_emptyCSharpBuildRequest);

            var monitorTaskSource = new TaskCompletionSource<bool>();
            clientConnection.MonitorTask = monitorTaskSource.Task;

            var handler = new Mock<IRequestHandler>();
            var handlerTaskSource = new TaskCompletionSource<CancellationToken>();
            var releaseHandlerSource = new TaskCompletionSource<bool>();
            handler
                .Setup(x => x.HandleRequest(It.IsAny<BuildRequest>(), It.IsAny<CancellationToken>()))
                .Callback<BuildRequest, CancellationToken>((_, t) =>
                {
                    handlerTaskSource.SetResult(t);
                    releaseHandlerSource.Task.Wait();
                })
                .Returns(s_emptyBuildResponse);

            var client = new Connection(clientConnection, handler.Object);
            var serveTask = client.ServeConnection();

            // Once this returns we know the Connection object has kicked off a compilation and 
            // started monitoring the disconnect task.  Can now initiate a disconnect in a known
            // state.
            var cancellationToken = handlerTaskSource.Task.Result;
            monitorTaskSource.SetResult(true);

            Assert.Equal(CompletionReason.ClientDisconnect, serveTask.Result.CompletionReason);
            Assert.True(cancellationToken.IsCancellationRequested);

            // Now that the asserts are done unblock the "build" long running task.  Have to do this
            // last to simulate a build which is still running when the client disconnects.
            releaseHandlerSource.SetResult(true);
        }

        [Fact]
        public void ReadError()
        {
            var handler = new Mock<IRequestHandler>(MockBehavior.Strict);
            var ex = new Exception("Simulated read error.");
            var clientConnection = new TestableClientConnection();
            var calledClose = false;
            clientConnection.ReadBuildRequestTask = TaskFromException<BuildRequest>(ex);
            clientConnection.CloseAction = delegate { calledClose = true; };

            var client = new Connection(clientConnection, handler.Object);
            Assert.Equal(CompletionReason.CompilationNotStarted, client.ServeConnection().Result.CompletionReason);
            Assert.True(calledClose);
        }

        /// <summary>
        /// A failure to write the results to the client is considered a client disconnection.  Any error
        /// from when the build starts to when the write completes should be handled this way. 
        /// </summary>
        [Fact]
        public void WriteError()
        {
            var clientConnection = new TestableClientConnection();
            clientConnection.MonitorTask = Task.Delay(-1);
            clientConnection.ReadBuildRequestTask = Task.FromResult(s_emptyCSharpBuildRequest);
            clientConnection.WriteBuildResponseTask = TaskFromException(new Exception());
            var handler = new Mock<IRequestHandler>();
            handler
                .Setup(x => x.HandleRequest(It.IsAny<BuildRequest>(), It.IsAny<CancellationToken>()))
                .Returns(s_emptyBuildResponse);

            var client = new Connection(clientConnection, handler.Object);
            Assert.Equal(CompletionReason.ClientDisconnect, client.ServeConnection().Result.CompletionReason);
        }

        [Fact]
        public void KeepAliveNoConnections()
        {
            var keepAlive = TimeSpan.FromSeconds(3);
            var requestHandler = new Mock<IRequestHandler>(MockBehavior.Strict);
            var dispatcher = new ServerDispatcher(CreateNopCompilerServerHost().Object, requestHandler.Object, new EmptyDiagnosticListener());
            var startTime = DateTime.Now;
            dispatcher.ListenAndDispatchConnections(keepAlive);

            Assert.True((DateTime.Now - startTime) > keepAlive);
        }

        [Fact]
        public async Task FailedConnectionShouldCreateFailedConnectionData()
        {
            var tcs = new TaskCompletionSource<IClientConnection>();
            var handler = new Mock<IRequestHandler>(MockBehavior.Strict);
            var connectionDataTask = ServerDispatcher.CreateHandleConnectionTask(tcs.Task, handler.Object, CancellationToken.None);

            tcs.SetException(new Exception());
            var connectionData = await connectionDataTask.ConfigureAwait(false);
            Assert.Equal(CompletionReason.CompilationNotStarted, connectionData.CompletionReason);
            Assert.Null(connectionData.KeepAlive);
        }

        /// <summary>
        /// Ensure server respects keep alive and shuts down after processing a single connection.
        /// </summary>
        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/4301")]
        public async Task KeepAliveAfterSingleConnection()
        {
            var keepAlive = TimeSpan.FromSeconds(1);
            var listener = new TestableDiagnosticListener();
            var pipeName = Guid.NewGuid().ToString();
            var dispatcherTask = Task.Run(() =>
            {
                var dispatcher = new ServerDispatcher(CreateNopCompilerServerHost().Object, CreateNopRequestHandler().Object, listener);
                dispatcher.ListenAndDispatchConnections(keepAlive);
            });

            await RunCSharpCompile(pipeName, HelloWorldSourceText).ConfigureAwait(false);
            await dispatcherTask.ConfigureAwait(false);

            Assert.Equal(1, listener.ProcessedCount);
            Assert.True(listener.LastProcessedTime.HasValue);
            Assert.True((DateTime.Now - listener.LastProcessedTime.Value) > keepAlive);
        }

        /// <summary>
        /// Ensure server respects keep alive and shuts down after processing multiple connections.
        /// </summary>
        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/4301")]
        public async Task KeepAliveAfterMultipleConnection()
        {
            var keepAlive = TimeSpan.FromSeconds(1);
            var listener = new TestableDiagnosticListener();
            var pipeName = Guid.NewGuid().ToString();
            var dispatcherTask = Task.Run(() =>
            {
                var dispatcher = new ServerDispatcher(
                    CreateNopCompilerServerHost().Object,
                    new CompilerRequestHandler(CreateNopCompilerServerHost().Object, Temp.CreateDirectory().Path, RuntimeEnvironment.GetRuntimeDirectory()), 
                    listener);
                dispatcher.ListenAndDispatchConnections(keepAlive);
            });

            for (int i = 0; i < 5; i++)
            {
                await RunCSharpCompile(pipeName, HelloWorldSourceText).ConfigureAwait(false);
            }

            await dispatcherTask.ConfigureAwait(false);
            Assert.Equal(5, listener.ProcessedCount);
            Assert.True(listener.LastProcessedTime.HasValue);
            Assert.True((DateTime.Now - listener.LastProcessedTime.Value) > keepAlive);
        }

        /// <summary>
        /// Ensure server respects keep alive and shuts down after processing simultaneous connections.
        /// </summary>
        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/4301")]
        public async Task KeepAliveAfterSimultaneousConnection()
        {
            var keepAlive = TimeSpan.FromSeconds(1);
            var listener = new TestableDiagnosticListener();
            var pipeName = Guid.NewGuid().ToString();
            var dispatcherTask = Task.Run(() =>
            {
                var dispatcher = new ServerDispatcher(
                    CreateNopCompilerServerHost().Object,
                    new CompilerRequestHandler(CreateNopCompilerServerHost().Object, Temp.CreateDirectory().Path, RuntimeEnvironment.GetRuntimeDirectory()), 
                    listener);
                dispatcher.ListenAndDispatchConnections(keepAlive);
            });

            var list = new List<Task>();
            for (int i = 0; i < 5; i++)
            {
                var task = Task.Run(() => RunCSharpCompile(pipeName, HelloWorldSourceText));
                list.Add(task);
            }

            foreach (var current in list)
            {
                await current.ConfigureAwait(false);
            }

            await dispatcherTask.ConfigureAwait(false);
            Assert.Equal(5, listener.ProcessedCount);
            Assert.True(listener.LastProcessedTime.HasValue);
            Assert.True((DateTime.Now - listener.LastProcessedTime.Value) > keepAlive);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/4301")]
        public async Task FirstClientCanOverrideDefaultTimeout()
        {
            var cts = new CancellationTokenSource();
            var listener = new TestableDiagnosticListener();
            TimeSpan? newTimeSpan = null;
            var connectionSource = new TaskCompletionSource<int>();
            var diagnosticListener = new Mock<IDiagnosticListener>();
            diagnosticListener
                .Setup(x => x.UpdateKeepAlive(It.IsAny<TimeSpan>()))
                .Callback<TimeSpan>(ts => { newTimeSpan = ts; });
            diagnosticListener
                .Setup(x => x.ConnectionProcessed(It.IsAny<int>()))
                .Callback<int>(count => connectionSource.SetResult(count));

            var pipeName = Guid.NewGuid().ToString();
            var dispatcherTask = Task.Run(() =>
            {
                var dispatcher = new ServerDispatcher(CreateNopCompilerServerHost().Object, CreateNopRequestHandler().Object, diagnosticListener.Object);
                dispatcher.ListenAndDispatchConnections(TimeSpan.FromSeconds(1), cancellationToken: cts.Token);
            });

            var seconds = 10;
            var response = await RunCSharpCompile(pipeName, HelloWorldSourceText, TimeSpan.FromSeconds(seconds)).ConfigureAwait(false);
            Assert.Equal(BuildResponse.ResponseType.Completed, response.Type);
            Assert.Equal(1, await connectionSource.Task.ConfigureAwait(false));
            Assert.True(newTimeSpan.HasValue);
            Assert.Equal(seconds, newTimeSpan.Value.TotalSeconds);

            cts.Cancel();
            await dispatcherTask.ConfigureAwait(false);
        }
    }
}
