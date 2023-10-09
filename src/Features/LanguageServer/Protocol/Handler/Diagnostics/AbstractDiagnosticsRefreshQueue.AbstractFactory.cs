// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;

internal abstract partial class AbstractDiagnosticsRefreshQueue
{
    protected abstract class AbstractFactory : ILspServiceFactory
    {
        private readonly IAsynchronousOperationListenerProvider _asyncListenerProvider;
        private readonly LspWorkspaceRegistrationService _lspWorkspaceRegistrationService;
        private readonly Refresher _refresher;

        protected AbstractFactory(
            IAsynchronousOperationListenerProvider asynchronousOperationListenerProvider,
            LspWorkspaceRegistrationService lspWorkspaceRegistrationService,
            Refresher refresher)
        {
            _asyncListenerProvider = asynchronousOperationListenerProvider;
            _lspWorkspaceRegistrationService = lspWorkspaceRegistrationService;
            _refresher = refresher;
        }

        protected abstract AbstractDiagnosticsRefreshQueue CreateDiagnosticsRefreshQueue(
            IAsynchronousOperationListenerProvider asyncListenerProvider,
            LspWorkspaceRegistrationService lspWorkspaceRegistrationService,
            LspWorkspaceManager lspWorkspaceManager,
            IClientLanguageServerManager notificationManager,
            Refresher refresher);

        public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
        {
            var notificationManager = lspServices.GetRequiredService<IClientLanguageServerManager>();
            var lspWorkspaceManager = lspServices.GetRequiredService<LspWorkspaceManager>();

            return CreateDiagnosticsRefreshQueue(_asyncListenerProvider, _lspWorkspaceRegistrationService, lspWorkspaceManager, notificationManager, _refresher);
        }
    }
}
