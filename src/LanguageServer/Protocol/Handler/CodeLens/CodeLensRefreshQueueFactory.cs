// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.CodeLens
{
    [ExportCSharpVisualBasicLspServiceFactory(typeof(CodeLensRefreshQueue)), Shared]
    internal sealed class CodeLensRefreshQueueFactory : ILspServiceFactory
    {
        private readonly IAsynchronousOperationListenerProvider _asyncListenerProvider;
        private readonly LspWorkspaceRegistrationService _lspWorkspaceRegistrationService;
        private readonly IGlobalOptionService _globalOptionService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CodeLensRefreshQueueFactory(
            IAsynchronousOperationListenerProvider asynchronousOperationListenerProvider,
            LspWorkspaceRegistrationService lspWorkspaceRegistrationService,
            IGlobalOptionService globalOptionService)
        {
            _asyncListenerProvider = asynchronousOperationListenerProvider;
            _lspWorkspaceRegistrationService = lspWorkspaceRegistrationService;
            _globalOptionService = globalOptionService;
        }

        public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
        {
            var notificationManager = lspServices.GetRequiredService<IClientLanguageServerManager>();
            var lspWorkspaceManager = lspServices.GetRequiredService<LspWorkspaceManager>();

            return new CodeLensRefreshQueue(_asyncListenerProvider, _lspWorkspaceRegistrationService, lspWorkspaceManager, notificationManager, _globalOptionService);
        }
    }
}
