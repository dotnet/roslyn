// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.LanguageServer
{
    [Export(typeof(ILanguageServerFactory)), Shared]
    internal class CSharpVisualBasicLanguageServerFactory : ILanguageServerFactory
    {
        private readonly RequestDispatcherFactory _dispatcherFactory;
        private readonly LspWorkspaceRegistrationService _lspWorkspaceRegistrationService;
        private readonly IAsynchronousOperationListenerProvider _listenerProvider;
        private readonly IGlobalOptionService _globalOptions;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpVisualBasicLanguageServerFactory(
            RequestDispatcherFactory dispatcherFactory,
            LspWorkspaceRegistrationService lspWorkspaceRegistrationService,
            IAsynchronousOperationListenerProvider listenerProvider,
            IGlobalOptionService globalOptions)
        {
            _dispatcherFactory = dispatcherFactory;
            _lspWorkspaceRegistrationService = lspWorkspaceRegistrationService;
            _listenerProvider = listenerProvider;
            _globalOptions = globalOptions;
        }

        public ILanguageServerTarget Create(
            JsonRpc jsonRpc,
            ICapabilitiesProvider capabilitiesProvider,
            ILspLogger logger)
        {
            var lspMiscellaneousFilesWorkspace = new LspMiscellaneousFilesWorkspace(logger);

            return new LanguageServerTarget(
                _dispatcherFactory,
                jsonRpc,
                capabilitiesProvider,
                _lspWorkspaceRegistrationService,
                lspMiscellaneousFilesWorkspace,
                _globalOptions,
                _listenerProvider,
                logger,
                ProtocolConstants.RoslynLspLanguages,
                WellKnownLspServerKinds.CSharpVisualBasicLspServer);
        }
    }
}
