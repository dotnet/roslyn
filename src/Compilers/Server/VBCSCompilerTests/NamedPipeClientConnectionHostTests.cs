// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CommandLine;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CompilerServer.UnitTests
{
    public class NamedPipeClientConnectionHostTests : IDisposable
    {
        private readonly NamedPipeClientConnectionHost _host;

        public NamedPipeClientConnectionHostTests(ITestOutputHelper testOutputHelper)
        {
            _host = new NamedPipeClientConnectionHost(ServerUtil.GetPipeName(), new XunitCompilerServerLogger(testOutputHelper));
        }

        public void Dispose()
        {
            if (_host.IsListening)
            {
                _host.EndListening();
            }

            Assert.True(NamedPipeTestUtil.IsPipeFullyClosed(_host.PipeName));
        }

        private Task<NamedPipeClientStream?> ConnectAsync(CancellationToken cancellationToken = default) => BuildServerConnection.TryConnectToServerAsync(
            _host.PipeName,
            timeoutMs: Timeout.Infinite,
            logger: _host.Logger,
            cancellationToken);

        [ConditionalFact(typeof(WindowsOrLinuxOnly), Reason = "https://github.com/dotnet/runtime/issues/40301")]
        public async Task CallBeforeListen()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => _host.GetNextClientConnectionAsync());
        }

        [ConditionalFact(typeof(WindowsOnly), Reason = "https://github.com/dotnet/runtime/issues/40301")]
        public async Task CallAfterComplete()
        {
            _host.BeginListening();
            var task = _host.GetNextClientConnectionAsync();
            using var clientStream = await ConnectAsync();
            await task;
            Assert.NotNull(_host.GetNextClientConnectionAsync());
            _host.EndListening();
        }

        [ConditionalFact(typeof(WindowsOrLinuxOnly), Reason = "https://github.com/dotnet/runtime/issues/40301")]
        public async Task EndListenCancelsIncompleteTask()
        {
            _host.BeginListening();
            var task = _host.GetNextClientConnectionAsync();
            _host.EndListening();

            await Assert.ThrowsAsync<TaskCanceledException>(async () => await task);
        }

        /// <summary>
        /// It is the responsibility of the caller of <see cref="NamedPipeClientConnectionHost.GetNextClientConnectionAsync"/>
        /// to dispose the returned client, not the hosts
        /// </summary>
        [ConditionalFact(typeof(WindowsOnly), Reason = "https://github.com/dotnet/runtime/issues/40301")]
        public async Task EndListenDoesNotDisposeCompletedConnection()
        {
            _host.BeginListening();
            var task = _host.GetNextClientConnectionAsync();
            using var clientStream = await ConnectAsync();
            using var namedPipeClientConnection = (NamedPipeClientConnection)(await task);
            _host.EndListening();
            Assert.False(namedPipeClientConnection.IsDisposed);
        }

        /// <summary>
        /// Ensure that the host can handle many connections before they are acknowledged / dequeued
        /// by the caller.
        /// </summary>
        [ConditionalFact(typeof(WindowsOnly), Reason = "https://github.com/dotnet/runtime/issues/40301")]
        public async Task ManyConnectsBeforeAcknowledged()
        {
            const int count = 20;
            _host.BeginListening();
            var list = new List<Task<NamedPipeClientStream?>>();
            for (int i = 0; i < count; i++)
            {
                list.Add(ConnectAsync());
            }

            await Task.WhenAll(list);

            for (int i = 0; i < count; i++)
            {
                var clientConnection = await _host.GetNextClientConnectionAsync();
                clientConnection.Dispose();
            }

            foreach (var item in list)
            {
                item.Result?.Dispose();
            }

            _host.EndListening();
        }

        /// <summary>
        /// When EndListen is called the host should be closing all of the queue'd client connections
        /// </summary>
        [ConditionalFact(typeof(WindowsOnly), Reason = "https://github.com/dotnet/runtime/issues/40301")]
        public async Task EndListenClosesQueuedConnections()
        {
            const int count = 20;
            _host.BeginListening();
            var list = new List<Task<NamedPipeClientStream?>>();
            for (int i = 0; i < count; i++)
            {
                list.Add(ConnectAsync());
            }

            await Task.WhenAll(list);

            _host.EndListening();

            var buffer = new byte[10];
            foreach (var streamTask in list)
            {
                using var stream = await streamTask;
                AssertEx.NotNull(stream);
                var readCount = await stream.ReadAsync(buffer, 0, buffer.Length);
                Assert.Equal(0, readCount);
                Assert.False(stream.IsConnected);
            }
        }

        [ConditionalFact(typeof(WindowsOnly), Reason = "https://github.com/dotnet/runtime/issues/40301")]
        public async Task SupportsMultipleBeginEndCycles()
        {
            for (int i = 0; i < 10; i++)
            {
                _host.BeginListening();
                Assert.True(_host.IsListening);
                using var client = await ConnectAsync();
                using var server = await _host.GetNextClientConnectionAsync();
                _host.EndListening();
                Assert.False(_host.IsListening);
            }
        }
    }
}
