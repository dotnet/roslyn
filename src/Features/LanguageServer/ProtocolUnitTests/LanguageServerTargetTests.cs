// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Nerdbank.Streams;
using Roslyn.Test.Utilities;
using StreamJsonRpc;
using Xunit;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests
{
    [UseExportProvider]
    public class LanguageServerTargetTests : AbstractLanguageServerProtocolTests
    {
        [Fact]
        public async Task LanguageServerQueueEmptyOnShutdownMessage()
        {
            await using var languageServerTarget = CreateLanguageServer(out var jsonRpc);
            AssertServerAlive(languageServerTarget);

            await languageServerTarget.ShutdownAsync(CancellationToken.None).ConfigureAwait(false);
            await AssertServerQueueClosed(languageServerTarget).ConfigureAwait(false);
            Assert.False(jsonRpc.IsDisposed);
        }

        [Fact]
        public async Task LanguageServerCleansUpOnExitMessage()
        {
            await using var languageServerTarget = CreateLanguageServer(out var jsonRpc);
            AssertServerAlive(languageServerTarget);

            await languageServerTarget.ShutdownAsync(CancellationToken.None).ConfigureAwait(false);
            await languageServerTarget.ExitAsync(CancellationToken.None).ConfigureAwait(false);
            await AssertServerQueueClosed(languageServerTarget).ConfigureAwait(false);
            Assert.True(jsonRpc.IsDisposed);
        }

        [Fact]
        public async Task LanguageServerCleansUpOnUnexpectedJsonRpcDisconnectAsync()
        {
            await using var languageServerTarget = CreateLanguageServer(out var jsonRpc);
            AssertServerAlive(languageServerTarget);

            jsonRpc.Dispose();
            await AssertServerQueueClosed(languageServerTarget).ConfigureAwait(false);
            Assert.True(jsonRpc.IsDisposed);
        }

        private static void AssertServerAlive(LanguageServerTarget server)
        {
            Assert.False(server.HasShutdownStarted);
            Assert.False(server.GetTestAccessor().GetQueueAccessor().IsComplete());
        }

        private static async Task AssertServerQueueClosed(LanguageServerTarget server)
        {
            await server.GetTestAccessor().GetQueueAccessor().WaitForProcessingToStopAsync().ConfigureAwait(false);
            Assert.True(server.HasShutdownStarted);
            Assert.True(server.GetTestAccessor().GetQueueAccessor().IsComplete());
        }

        private LanguageServerTarget CreateLanguageServer(out JsonRpc serverJsonRpc)
        {
            using var workspace = TestWorkspace.CreateCSharp("", composition: Composition);

            var (_, serverStream) = FullDuplexStream.CreatePair();
            var dispatcherFactory = workspace.GetService<RequestDispatcherFactory>();
            var lspWorkspaceRegistrationService = workspace.GetService<LspWorkspaceRegistrationService>();
            var capabilitiesProvider = workspace.GetService<DefaultCapabilitiesProvider>();
            var globalOptions = workspace.GetService<IGlobalOptionService>();
            var listenerProvider = workspace.GetService<IAsynchronousOperationListenerProvider>();

            serverJsonRpc = new JsonRpc(new HeaderDelimitedMessageHandler(serverStream, serverStream))
            {
                ExceptionStrategy = ExceptionProcessing.ISerializable,
            };

            var languageServer = new LanguageServerTarget(
                dispatcherFactory,
                serverJsonRpc,
                capabilitiesProvider,
                lspWorkspaceRegistrationService,
                new LspMiscellaneousFilesWorkspace(NoOpLspLogger.Instance),
                globalOptions,
                listenerProvider,
                NoOpLspLogger.Instance,
                ProtocolConstants.RoslynLspLanguages,
                clientName: null,
                WellKnownLspServerKinds.AlwaysActiveVSLspServer);

            serverJsonRpc.StartListening();
            return languageServer;
        }
    }
}
