// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.LanguageServer
{
    [Export(typeof(ILanguageServerFactory)), Shared]
    internal class CSharpVisualBasicLanguageServerFactory : ILanguageServerFactory
    {
        public const string UserVisibleName = "Roslyn Language Server Client";

        private readonly RequestDispatcherFactory _dispatcherFactory;
        private readonly IAsynchronousOperationListenerProvider _listenerProvider;
        private readonly IGlobalOptionService _globalOptions;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpVisualBasicLanguageServerFactory(
            RequestDispatcherFactory dispatcherFactory,
            IAsynchronousOperationListenerProvider listenerProvider,
            IGlobalOptionService globalOptions)
        {
            _dispatcherFactory = dispatcherFactory;
            _listenerProvider = listenerProvider;
            _globalOptions = globalOptions;
        }

        public ILanguageServerTarget Create(
            JsonRpc jsonRpc,
            ICapabilitiesProvider capabilitiesProvider,
            ILspWorkspaceRegistrationService workspaceRegistrationService,
            ILspLogger logger)
        {
            return new LanguageServerTarget(
                _dispatcherFactory,
                jsonRpc,
                capabilitiesProvider,
                workspaceRegistrationService,
                _globalOptions,
                _listenerProvider,
                logger,
                ProtocolConstants.RoslynLspLanguages,
                clientName: null,
                userVisibleServerName: UserVisibleName,
                telemetryServerTypeName: this.GetType().Name);
        }
    }
}
