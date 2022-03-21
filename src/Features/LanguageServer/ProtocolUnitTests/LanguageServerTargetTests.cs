// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests
{
    [UseExportProvider]
    public class LanguageServerTargetTests : AbstractLanguageServerProtocolTests
    {
        [Fact]
        public async Task LanguageServerQueueEmptyOnShutdownMessage()
        {
            var server = await CreateTestLspServerAsync("");
            AssertServerAlive(server);

            server.GetServerAccessor().ShutdownServer();
            await AssertServerQueueClosed(server).ConfigureAwait(false);
            Assert.False(server.GetServerAccessor().GetServerRpc().IsDisposed);
        }

        [Fact]
        public async Task LanguageServerCleansUpOnExitMessage()
        {
            var server = await CreateTestLspServerAsync("");
            AssertServerAlive(server);

            server.GetServerAccessor().ShutdownServer();
            server.GetServerAccessor().ExitServer();
            await AssertServerQueueClosed(server).ConfigureAwait(false);
            Assert.True(server.GetServerAccessor().GetServerRpc().IsDisposed);
        }

        [Fact]
        public async Task LanguageServerCleansUpOnUnexpectedJsonRpcDisconnectAsync()
        {
            using var server = await CreateTestLspServerAsync("");
            AssertServerAlive(server);

            server.GetServerAccessor().GetServerRpc().Dispose();
            await AssertServerQueueClosed(server).ConfigureAwait(false);
            Assert.True(server.GetServerAccessor().GetServerRpc().IsDisposed);
        }

        private static void AssertServerAlive(TestLspServer server)
        {
            Assert.False(server.GetServerAccessor().HasShutdownStarted());
            Assert.False(server.GetQueueAccessor().IsComplete());
        }

        private static async Task AssertServerQueueClosed(TestLspServer server)
        {
            await server.GetQueueAccessor().WaitForProcessingToStopAsync().ConfigureAwait(false);
            Assert.True(server.GetServerAccessor().HasShutdownStarted());
            Assert.True(server.GetQueueAccessor().IsComplete());
        }
    }
}
