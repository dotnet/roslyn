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
        private sealed class TestableDiagnosticListener : IDiagnosticListener
        {
            public int ProcessedCount;
            public DateTime? LastProcessedTime;
            public TimeSpan? KeepAlive;
            public bool HasDetectedBadConnection;
            public bool HitKeepAliveTimeout;

            public void ConnectionProcessed(int count)
            {
                ProcessedCount += count;
                LastProcessedTime = DateTime.Now;
            }

            public void UpdateKeepAlive(TimeSpan timeSpan)
            {
                KeepAlive = timeSpan;
            }

            public void DetectedBadConnection()
            {
                HasDetectedBadConnection = true;
            }

            public void KeepAliveReached()
            {
                HitKeepAliveTimeout = true;
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

        private static IClientConnection CreateClientConnection(CompletionReason completionReason, TimeSpan? keepAlive = null)
        {
            var task = Task.FromResult(new ConnectionData(completionReason, keepAlive));
            return CreateClientConnection(task);
        }

        private static IClientConnection CreateClientConnection(Task<ConnectionData> task)
        {
            var connection = new Mock<IClientConnection>();
            connection
                .Setup(x => x.HandleConnection(It.IsAny<CancellationToken>()))
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
                .Setup(x => x.HandleConnection(It.IsAny<CancellationToken>()))
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
            var connection = CreateClientConnection(CompletionReason.Completed);
            var host = CreateClientConnectionHost(
                Task.FromResult(connection),
                new TaskCompletionSource<IClientConnection>().Task);
            var listener = new TestableDiagnosticListener();
            var keepAlive = TimeSpan.FromSeconds(1);
            var dispatcher = new ServerDispatcher(host, listener);
            dispatcher.ListenAndDispatchConnections(keepAlive);

            Assert.Equal(1, listener.ProcessedCount);
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
                var connection = CreateClientConnection(CompletionReason.Completed);
                list.Add(Task.FromResult(connection));
            }

            list.Add(new TaskCompletionSource<IClientConnection>().Task);
            var host = CreateClientConnectionHost(list.ToArray());
            var listener = new TestableDiagnosticListener();
            var keepAlive = TimeSpan.FromSeconds(1);
            var dispatcher = new ServerDispatcher(host, listener);
            dispatcher.ListenAndDispatchConnections(keepAlive);

            Assert.Equal(count, listener.ProcessedCount);
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
                source.SetResult(new ConnectionData(CompletionReason.Completed));
            }

            await dispatcherTask.ConfigureAwait(true);
            Assert.Equal(totalCount, listener.ProcessedCount);
            Assert.True(listener.LastProcessedTime.HasValue);
            Assert.True(listener.HitKeepAliveTimeout);
        }

        [Fact]
        public void ClientExceptionShouldBeginShutdown()
        {
            var client = new Mock<IClientConnection>();
            client
                .Setup(x => x.HandleConnection(It.IsAny<CancellationToken>()))
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
            var mutexName = Guid.NewGuid().ToString("N");

            bool holdsMutex;
            using (var mutex = new Mutex(initiallyOwned: true,
                                         name: mutexName,
                                         createdNew: out holdsMutex))
            {
                Assert.True(holdsMutex);
                try
                {
                    var host = new Mock<IClientConnectionHost>(MockBehavior.Strict);
                    var result = VBCSCompiler.Run(mutexName, host.Object, keepAlive: null);
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
            var mutexName = Guid.NewGuid().ToString("N");
            var host = new Mock<IClientConnectionHost>(MockBehavior.Strict);
            host
                .Setup(x => x.CreateListenTask(It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    var task = new Task(() =>
                    {
                        Mutex mutex;
                        Assert.True(Mutex.TryOpenExisting(mutexName, out mutex));
                        Assert.False(mutex.WaitOne(millisecondsTimeout: 0));
                    });
                    task.Start(TaskScheduler.Default);
                    task.Wait();

                    return new TaskCompletionSource<IClientConnection>().Task;
                });

            var result = VBCSCompiler.Run(mutexName, host.Object, keepAlive: TimeSpan.FromSeconds(1));
            Assert.Equal(CommonCompiler.Succeeded, result);
        }
    }
}
