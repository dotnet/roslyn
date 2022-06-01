// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.DocumentChanges;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests
{
    [UseExportProvider]
    public class LanguageServerTargetTests : AbstractLanguageServerProtocolTests
    {
        protected override TestComposition Composition => base.Composition.AddParts(typeof(StatefulLspServiceFactory), typeof(StatelessLspService));

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

        [Fact]
        public async Task LanguageServerHasSeparateServiceInstances()
        {
            using var serverOne = await CreateTestLspServerAsync("");
            using var serverTwo = await CreateTestLspServerAsync("");

            // Get an LSP service and verify each server has its own instance per server.
            Assert.NotSame(serverOne.GetRequiredLspService<LspWorkspaceManager>(), serverTwo.GetRequiredLspService<LspWorkspaceManager>());
            Assert.Same(serverOne.GetRequiredLspService<LspWorkspaceManager>(), serverOne.GetRequiredLspService<LspWorkspaceManager>());
            Assert.Same(serverTwo.GetRequiredLspService<LspWorkspaceManager>(), serverTwo.GetRequiredLspService<LspWorkspaceManager>());

            // Get a stateless request handler and verify each server has the same instance.
            Assert.Same(serverOne.GetRequiredLspService<DidOpenHandler>(), serverTwo.GetRequiredLspService<DidOpenHandler>());
        }

        [Fact]
        public async Task LanguageServerDisposesOfServicesOnShutdown()
        {
            using var server = await CreateTestLspServerAsync("");

            var statefulService = server.GetRequiredLspService<StatefulLspService>();
            var statelessService = server.GetRequiredLspService<StatelessLspService>();

            Assert.False(statefulService.IsDisposed);
            Assert.False(statelessService.IsDisposed);

            server.GetServerAccessor().ShutdownServer();
            server.GetServerAccessor().ExitServer();

            // Only the stateful service should be disposed of on server shutdown.
            Assert.True(statefulService.IsDisposed);
            Assert.False(statelessService.IsDisposed);
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

        [ExportCSharpVisualBasicLspServiceFactory(typeof(StatefulLspService)), Shared]
        internal class StatefulLspServiceFactory : ILspServiceFactory
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public StatefulLspServiceFactory()
            {
            }

            public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind) => new StatefulLspService();
        }

        internal class StatefulLspService : ILspService, IDisposable
        {
            public bool IsDisposed { get; private set; } = false;
            public void Dispose()
            {
                IsDisposed = true;
            }
        }

        [ExportCSharpVisualBasicStatelessLspService(typeof(StatelessLspService)), Shared]
        internal class StatelessLspService : ILspService, IDisposable
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public StatelessLspService()
            {
            }

            public bool IsDisposed { get; private set; } = false;
            public void Dispose()
            {
                IsDisposed = true;
            }
        }
    }
}
