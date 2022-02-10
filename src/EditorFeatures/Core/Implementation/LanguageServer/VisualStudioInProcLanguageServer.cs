// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;
using StreamJsonRpc;

using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.Editor.Implementation.LanguageClient
{
    /// <summary>
    /// Implementation of <see cref="LanguageServerTarget"/> that also supports
    /// VS LSP extension methods.
    /// </summary>
    internal class VisualStudioInProcLanguageServer : LanguageServerTarget
    {
        internal VisualStudioInProcLanguageServer(
            AbstractRequestDispatcherFactory requestDispatcherFactory,
            JsonRpc jsonRpc,
            ICapabilitiesProvider capabilitiesProvider,
            LspWorkspaceRegistrationService workspaceRegistrationService,
            IGlobalOptionService globalOptions,
            IAsynchronousOperationListenerProvider listenerProvider,
            ILspLogger logger,
            ImmutableArray<string> supportedLanguages,
            string? clientName,
            WellKnownLspServerKinds serverKind,
            LspMiscellaneousFilesWorkspace? miscellaneousFilesWorkspace = null)
            : base(requestDispatcherFactory, jsonRpc, capabilitiesProvider, workspaceRegistrationService, miscellaneousFilesWorkspace, globalOptions, listenerProvider, logger, supportedLanguages, clientName, serverKind)
        {
        }

        internal TestAccessor GetTestAccessor() => new(this);

        internal readonly struct TestAccessor
        {
            private readonly VisualStudioInProcLanguageServer _server;

            internal TestAccessor(VisualStudioInProcLanguageServer server)
            {
                _server = server;
            }

            internal RequestExecutionQueue.TestAccessor GetQueueAccessor()
                => _server.Queue.GetTestAccessor();

            internal LspWorkspaceManager.TestAccessor GetManagerAccessor()
                => _server.Queue.GetTestAccessor().GetLspWorkspaceManager().GetTestAccessor();

            internal RequestDispatcher.TestAccessor GetDispatcherAccessor()
                => _server.RequestDispatcher.GetTestAccessor();

            internal JsonRpc GetServerRpc() => _server.JsonRpc;

            internal bool HasShutdownStarted() => _server.HasShutdownStarted;

            internal void ShutdownServer() => _server.ShutdownImpl();

            internal void ExitServer() => _server.ExitImpl();
        }
    }
}
