// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.CommonLanguageServerProtocol.Framework;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.LanguageServer
{
    [Export(typeof(ILanguageServerFactory)), Shared]
    internal class CSharpVisualBasicLanguageServerFactory : ILanguageServerFactory
    {
        private readonly AbstractLspServiceProvider _lspServiceProvider;
        private readonly IAsynchronousOperationListenerProvider _listenerProvider;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpVisualBasicLanguageServerFactory(
            CSharpVisualBasicLspServiceProvider lspServiceProvider,
            IAsynchronousOperationListenerProvider listenerProvider)
        {
            _lspServiceProvider = lspServiceProvider;
            _listenerProvider = listenerProvider;
        }

        public async Task<AbstractLanguageServer<RequestContext>> CreateAsync(
            JsonRpc jsonRpc,
            ICapabilitiesProvider capabilitiesProvider,
            IRoslynLspLogger logger)
        {
            var server = new RoslynLanguageServer(
                _lspServiceProvider,
                jsonRpc,
                capabilitiesProvider,
                _listenerProvider,
                logger,
                ProtocolConstants.RoslynLspLanguages,
                WellKnownLspServerKinds.CSharpVisualBasicLspServer);
            await server.InitializeAsync();

            return server;
        }

        public Task<AbstractLanguageServer<RequestContext>> CreateAsync(Stream input, Stream output, ICapabilitiesProvider capabilitiesProvider, IRoslynLspLogger logger)
        {
            var jsonRpc = new JsonRpc(new HeaderDelimitedMessageHandler(output, input));
            return CreateAsync(jsonRpc, capabilitiesProvider, logger);
        }
    }
}
