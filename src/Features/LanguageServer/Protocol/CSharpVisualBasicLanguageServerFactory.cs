// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.LanguageServer
{
    [Export(typeof(ILanguageServerFactory)), Shared]
    [Export(typeof(CSharpVisualBasicLanguageServerFactory))]
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

        public InProcLanguageServer Create(
            JsonRpc jsonRpc,
            ServerCapabilities serverCapabilities,
            ILspWorkspaceRegistrationService workspaceRegistrationService,
            ILspLogger logger)
        {
            return new InProcLanguageServer(
                _dispatcherFactory,
                jsonRpc,
                serverCapabilities,
                workspaceRegistrationService,
                _listenerProvider,
                logger,
                diagnosticService: null,
                clientName: null,
                userVisibleServerName: UserVisibleName,
                telemetryServerTypeName: this.GetType().Name);
        }
    }
}
