// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.DocumentChanges;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using StreamJsonRpc;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests
{
    [UseExportProvider]
    public class LanguageServerTargetTests : AbstractLanguageServerProtocolTests
    {
        public LanguageServerTargetTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        protected override TestComposition Composition => base.Composition.AddParts(typeof(StatefulLspServiceFactory), typeof(StatelessLspService));

        [Theory, CombinatorialData]
        public async Task LanguageServerQueueEmptyOnShutdownMessage(bool mutatingLspWorkspace)
        {
            await using var server = await CreateTestLspServerAsync("", mutatingLspWorkspace);
            AssertServerAlive(server);

            await server.ShutdownTestServerAsync();
            await AssertServerQueueClosed(server).ConfigureAwait(false);
            Assert.False(server.GetServerAccessor().GetServerRpc().IsDisposed);
            await server.ExitTestServerAsync();
        }

        [Theory, CombinatorialData]
        public async Task LanguageServerCleansUpOnExitMessage(bool mutatingLspWorkspace)
        {
            await using var server = await CreateTestLspServerAsync("", mutatingLspWorkspace);
            AssertServerAlive(server);

            await server.ShutdownTestServerAsync();
            await server.ExitTestServerAsync();
            await AssertServerQueueClosed(server).ConfigureAwait(false);
            Assert.True(server.GetServerAccessor().GetServerRpc().IsDisposed);
        }

        [Theory, CombinatorialData]
        public async Task LanguageServerCleansUpOnUnexpectedJsonRpcDisconnectAsync(bool mutatingLspWorkspace)
        {
            await using var server = await CreateTestLspServerAsync("", mutatingLspWorkspace);
            AssertServerAlive(server);

            server.GetServerAccessor().GetServerRpc().Dispose();
            await AssertServerQueueClosed(server).ConfigureAwait(false);
            Assert.True(server.GetServerAccessor().GetServerRpc().IsDisposed);
        }

        [Theory, CombinatorialData]
        public async Task LanguageServerHasSeparateServiceInstances(bool mutatingLspWorkspace)
        {
            await using var serverOne = await CreateTestLspServerAsync("", mutatingLspWorkspace);
            await using var serverTwo = await CreateTestLspServerAsync("", mutatingLspWorkspace);

            // Get an LSP service and verify each server has its own instance per server.
            Assert.NotSame(serverOne.GetRequiredLspService<LspWorkspaceManager>(), serverTwo.GetRequiredLspService<LspWorkspaceManager>());
            Assert.Same(serverOne.GetRequiredLspService<LspWorkspaceManager>(), serverOne.GetRequiredLspService<LspWorkspaceManager>());
            Assert.Same(serverTwo.GetRequiredLspService<LspWorkspaceManager>(), serverTwo.GetRequiredLspService<LspWorkspaceManager>());

            // Get a stateless request handler and verify each server has the same instance.
            Assert.Same(serverOne.GetRequiredLspService<DidOpenHandler>(), serverTwo.GetRequiredLspService<DidOpenHandler>());
        }

        [Theory, CombinatorialData]
        public async Task LanguageServerSucceedsAfterInitializedCalled(bool mutatingLspWorkspace)
        {
            // Arrange
            await using var server = await CreateTestLspServerAsync("", mutatingLspWorkspace);

            var initializedParams = new InitializedParams();

            await server.ExecuteRequestAsync<InitializedParams, object>(Methods.InitializedName, initializedParams, CancellationToken.None);

            // Act & Assert
            var didOpenParams = new DidOpenTextDocumentParams
            {
                TextDocument = new TextDocumentItem
                {
                    Text = "sometext",
                    Uri = ProtocolConversions.CreateAbsoluteUri(@"C:\location\file.json"),
                }
            };

            // We just want this to not throw.
            await server.ExecuteRequestAsync<DidOpenTextDocumentParams, object>(Methods.TextDocumentDidOpenName, didOpenParams, CancellationToken.None);
        }

        [Theory(Skip = "https://github.com/dotnet/razor/issues/8311"), CombinatorialData]
        public async Task LanguageServerRejectsRequestsBeforeInitialized(bool mutatingLspWorkspace)
        {
            await using var server = await CreateTestLspServerAsync("", mutatingLspWorkspace, new InitializationOptions { CallInitialized = false });

            var didOpenParams = new DidOpenTextDocumentParams
            {
                TextDocument = new TextDocumentItem
                {
                    Text = "sometext",
                    Uri = ProtocolConversions.CreateAbsoluteUri(@"C:\location\file.json"),
                }
            };
            var ex = await Assert.ThrowsAsync<RemoteInvocationException>(async () => await server.ExecuteRequestAsync<DidOpenTextDocumentParams, object>(Methods.TextDocumentDidOpenName, didOpenParams, CancellationToken.None));
            Assert.Equal("'initialized' has not been called.", ex.Message);
        }

        [Theory, CombinatorialData]
        public async Task LanguageServerDisposesOfServicesOnShutdown(bool mutatingLspWorkspace)
        {
            await using var server = await CreateTestLspServerAsync("", mutatingLspWorkspace);

            var statefulService = server.GetRequiredLspService<StatefulLspService>();
            var statelessService = server.GetRequiredLspService<StatelessLspService>();

            Assert.False(statefulService.IsDisposed);
            Assert.False(statelessService.IsDisposed);

            await server.ShutdownTestServerAsync();
            await server.ExitTestServerAsync();

            // Only the stateful service should be disposed of on server shutdown.
            Assert.True(statefulService.IsDisposed);
            Assert.False(statelessService.IsDisposed);
        }

        private static void AssertServerAlive(TestLspServer server)
        {
            Assert.False(server.GetServerAccessor().HasShutdownStarted());
            Assert.False(server.GetQueueAccessor()!.Value.IsComplete());
        }

        private static async Task AssertServerQueueClosed(TestLspServer server)
        {
            var queueAccessor = server.GetQueueAccessor()!.Value;
            await queueAccessor.WaitForProcessingToStopAsync().ConfigureAwait(false);
            Assert.True(server.GetServerAccessor().HasShutdownStarted());
            Assert.True(queueAccessor.IsComplete());
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
