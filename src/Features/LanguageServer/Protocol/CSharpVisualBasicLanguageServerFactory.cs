// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.LanguageServer
{
    [Export(typeof(ILanguageServerFactory)), Shared]
    internal class CSharpVisualBasicLanguageServerFactory : ILanguageServerFactory
    {
        public const string UserVisibleName = "C#/Visual Basic Language Server Client";

        private readonly CSharpVisualBasicRequestDispatcherFactory _dispatcherFactory;
        private readonly IAsynchronousOperationListenerProvider _listenerProvider;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpVisualBasicLanguageServerFactory(CSharpVisualBasicRequestDispatcherFactory dispatcherFactory,
            IAsynchronousOperationListenerProvider listenerProvider)
        {
            _dispatcherFactory = dispatcherFactory;
            _listenerProvider = listenerProvider;
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
                _listenerProvider,
                logger,
                clientName: null,
                userVisibleServerName: UserVisibleName,
                telemetryServerTypeName: this.GetType().Name);
        }
    }
}
