// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CommandLine;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CompilerServer.UnitTests
{
    public class NamedPipeClientConnectionHostTests : IDisposable
    {
        private NamedPipeClientConnectionHost _host;

        public NamedPipeClientConnectionHostTests()
        {
            _host = new NamedPipeClientConnectionHost(ServerUtil.GetPipeName());
        }

        public void Dispose()
        {
            if (_host.IsListening)
            {
                _host.EndListening();
            }
        }

        private Task<NamedPipeClientStream> ConnectAsync(CancellationToken cancellationToken = default) => BuildServerConnection.TryConnectToServerAsync(
            _host.PipeName,
            timeoutMs: (int)(TimeSpan.FromMinutes(1).TotalMilliseconds),
            cancellationToken);

        public class GetNextClientConnectionAsyncTests : NamedPipeClientConnectionHostTests
        {
            /// <summary>
            /// Not legal to call this until the previous task has completed.
            /// </summary>
            [Fact]
            public async Task CallBeforePreviousComplete()
            {
                _host.BeginListening();
                var task = _host.GetNextClientConnectionAsync();
                await Assert.ThrowsAsync<InvalidOperationException>(() => _host.GetNextClientConnectionAsync()).ConfigureAwait(false);
                _host.EndListening();
            }

            [Fact]
            public async Task CallBeforeListen()
            {
                await Assert.ThrowsAsync<InvalidOperationException>(() => _host.GetNextClientConnectionAsync()).ConfigureAwait(false);
            }

            [Fact]
            public async Task CallAfterComplete()
            {
                _host.BeginListening();
                var task = _host.GetNextClientConnectionAsync();
                using var clientStream = await ConnectAsync().ConfigureAwait(false);
                await task.ConfigureAwait(false);
                Assert.NotNull( _host.GetNextClientConnectionAsync());
                _host.EndListening();
            }

            [Fact]
            public void EndListenCancelsIncompleteTask()
            {
                _host.BeginListening();
                var task = _host.GetNextClientConnectionAsync();
                _host.EndListening();

                Assert.ThrowsAsync<OperationCanceledException>(() => task).ConfigureAwait(false);
            }

            /// <summary>
            /// It is the responsibility of the caller of <see cref="NamedPipeClientConnectionHost.GetNextClientConnectionAsync"/>
            /// to dispose the returned client, not the hosts
            /// </summary>
            [Fact]
            public async Task EndListenDoesNotDisposeCompletedConnection()
            {
                _host.BeginListening();
                var task = _host.GetNextClientConnectionAsync();
                using var clientStream = await ConnectAsync().ConfigureAwait(false);
                using var namedPipeClientConnection = (NamedPipeClientConnection)(await task.ConfigureAwait(false));
                _host.EndListening();
                Assert.False(namedPipeClientConnection.IsDisposed);
            }
        }
    }
}
