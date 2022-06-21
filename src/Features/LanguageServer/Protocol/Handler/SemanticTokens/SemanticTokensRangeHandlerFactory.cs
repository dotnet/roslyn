// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens
{
    [ExportCSharpVisualBasicLspServiceFactory(typeof(SemanticTokensRangeHandler)), Shared]
    internal sealed class SemanticTokensRangeHandlerFactory : ILspServiceFactory
    {
        private readonly IGlobalOptionService _globalOptions;
        private readonly IAsynchronousOperationListenerProvider _asyncListenerProvider;
        private readonly LspWorkspaceRegistrationService _lspWorkspaceRegistrationService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public SemanticTokensRangeHandlerFactory(
            IGlobalOptionService globalOptions,
            IAsynchronousOperationListenerProvider asynchronousOperationListenerProvider,
            LspWorkspaceRegistrationService lspWorkspaceRegistrationService)
        {
            _globalOptions = globalOptions;
            _asyncListenerProvider = asynchronousOperationListenerProvider;
            _lspWorkspaceRegistrationService = lspWorkspaceRegistrationService;
        }

        public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
        {
            var clientCapabilities = lspServices.GetRequiredService<IClientCapabilitiesProvider>().GetClientCapabilities();
            var notificationManager = lspServices.GetRequiredService<ILanguageServerNotificationManager>();
            var lspWorkspaceManager = lspServices.GetRequiredService<LspWorkspaceManager>();
            return new SemanticTokensRangeHandler(_globalOptions, _asyncListenerProvider, _lspWorkspaceRegistrationService, lspWorkspaceManager, notificationManager, clientCapabilities);
        }
    }
}
