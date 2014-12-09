// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CompilerServer;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.Win32;
using Moq;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.BuildTasks.UnitTests
{
    public class CompilerServerApiTest : TestBase
    {
        private sealed class TestableClientConnection : IClientConnection
        {
            internal string LoggingIdentifier = string.Empty;
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

        private static readonly BuildRequest EmptyCSharpBuildRequest = new BuildRequest(
            1,
            BuildProtocolConstants.RequestLanguage.CSharpCompile,
            ImmutableArray<BuildRequest.Argument>.Empty);

        private static readonly BuildResponse EmptyBuildResponse = new CompletedBuildResponse(
            returnCode: 0,
            utf8output: false,
            output: string.Empty,
            errorOutput: string.Empty);

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

        [Fact]
        public void NotifyCallBackOnRequestHandlerException()
        {
            var clientConnection = new TestableClientConnection();
            clientConnection.MonitorTask = Task.Delay(-1);
            clientConnection.ReadBuildRequestTask = Task.FromResult(EmptyCSharpBuildRequest);

            var ex = new Exception();
            var handler = new Mock<IRequestHandler>();
            handler
                .Setup(x => x.HandleRequest(It.IsAny<BuildRequest>(), It.IsAny<CancellationToken>()))
                .Throws(ex);

            var invoked = false;
            FatalError.Handler = (providedEx) =>
            {
                Assert.Same(ex, providedEx);
                invoked = true;
            };
            var client = new ServerDispatcher.Connection(clientConnection, handler.Object);

            Assert.Throws(typeof(AggregateException), () => client.ServeConnection(new TaskCompletionSource<TimeSpan?>()).Wait());
            Assert.True(invoked);
        }

        [Fact]
        public void ClientDisconnectCancelBuildAndReturnsFailure()
        {
            var clientConnection = new TestableClientConnection();
            clientConnection.ReadBuildRequestTask = Task.FromResult(EmptyCSharpBuildRequest);

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
                .Returns(EmptyBuildResponse);

            var client = new ServerDispatcher.Connection(clientConnection, handler.Object);
            var serveTask = client.ServeConnection(new TaskCompletionSource<TimeSpan?>());

            // Once this returns we know the Connection object has kicked off a compilation and 
            // started monitoring the disconnect task.  Can now initiate a disconnect in a known
            // state.
            var cancellationToken = handlerTaskSource.Task.Result;
            monitorTaskSource.SetResult(true);

            Assert.Equal(ServerDispatcher.CompletionReason.ClientDisconnect, serveTask.Result);
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

            var client = new ServerDispatcher.Connection(clientConnection, handler.Object);
            Assert.Equal(ServerDispatcher.CompletionReason.CompilationNotStarted, client.ServeConnection(new TaskCompletionSource<TimeSpan?>()).Result);
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
            clientConnection.ReadBuildRequestTask = Task.FromResult(EmptyCSharpBuildRequest);
            clientConnection.WriteBuildResponseTask = TaskFromException(new Exception());
            var handler = new Mock<IRequestHandler>();
            handler
                .Setup(x => x.HandleRequest(It.IsAny<BuildRequest>(), It.IsAny<CancellationToken>()))
                .Returns(EmptyBuildResponse);

            var client = new ServerDispatcher.Connection(clientConnection, handler.Object);
            Assert.Equal(ServerDispatcher.CompletionReason.ClientDisconnect, client.ServeConnection(new TaskCompletionSource<TimeSpan?>()).Result);
        }
    }
}
