// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

#nullable enable

namespace Microsoft.CodeAnalysis.CompilerServer.UnitTests
{
    public class ClientConnectionHandlerTests
    {
        [Fact]
        public async Task ThrowDuringBuild()
        {
            var compilerServerHost = new TestableCompilerServerHost(delegate { throw new Exception(); });
            var clientConnectionHandler = new ClientConnectionHandler(compilerServerHost);
            var clientConnection = new TestableClientConnection()
            {
                ReadBuildRequestFunc = _ => Task.FromResult(ProtocolUtil.EmptyCSharpBuildRequest),
            };
            var completionData = await clientConnectionHandler.ProcessAsync(Task.FromResult<IClientConnection>(clientConnection)).ConfigureAwait(false);
            Assert.Equal(CompletionData.RequestError, completionData);
        }

        [Fact]
        public async Task ThrowReadingRequest()
        {
            var compilerServerHost = new TestableCompilerServerHost(delegate { throw new Exception(); });
            var clientConnectionHandler = new ClientConnectionHandler(compilerServerHost);
            var clientConnection = new TestableClientConnection()
            {
                ReadBuildRequestFunc = _ => throw new Exception(),
            };
            var completionData = await clientConnectionHandler.ProcessAsync(Task.FromResult<IClientConnection>(clientConnection)).ConfigureAwait(false);
            Assert.Equal(CompletionData.RequestError, completionData);
        }

        [Fact]
        public async Task ClientDisconnectDuringBuild()
        {
            using var buildStartedMre = new ManualResetEvent(initialState: false);
            using var clientClosedMre = new ManualResetEvent(initialState: false);
            var compilerServerHost = new TestableCompilerServerHost((request, cancellationToken) =>
            {
                buildStartedMre.Set();
                clientClosedMre.WaitOne();
                Assert.True(cancellationToken.IsCancellationRequested);
                return ProtocolUtil.EmptyBuildResponse;
            });

            var disconnectTaskCompletionSource = new TaskCompletionSource<object?>();
            var isDisposed = false;
            var clientConnection = new TestableClientConnection()
            {
                ReadBuildRequestFunc = _ => Task.FromResult(ProtocolUtil.EmptyBasicBuildRequest),
                DisconnectTask = disconnectTaskCompletionSource.Task,
                DisposeFunc = () => { isDisposed = true; },
            };

            var clientConnectionHandler = new ClientConnectionHandler(compilerServerHost);
            var task = clientConnectionHandler.ProcessAsync(Task.FromResult<IClientConnection>(clientConnection));

            // Don't trigger the disconnect until we confirm that the client has issueed a 
            // build request.
            buildStartedMre.WaitOne();
            disconnectTaskCompletionSource.TrySetResult(null);

            var completionData = await task.ConfigureAwait(false);
            Assert.Equal(CompletionData.RequestError, completionData);
            Assert.True(isDisposed);

            clientClosedMre.Set();
        }
    }
}
