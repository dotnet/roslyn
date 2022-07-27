// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.IO;
using CommonLanguageServerProtocol.Framework;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
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

        public ILanguageServer Create(
            JsonRpc jsonRpc,
            ICapabilitiesProvider capabilitiesProvider,
            IRoslynLspLogger logger)
        {
            return new RoslynLanguageServerTarget(
                _lspServiceProvider, jsonRpc,
                capabilitiesProvider,
                _listenerProvider,
                logger,
                ProtocolConstants.RoslynLspLanguages,
                WellKnownLspServerKinds.CSharpVisualBasicLspServer);
        }

        public ILanguageServer Create(Stream input, Stream output, ICapabilitiesProvider capabilitiesProvider, IRoslynLspLogger logger)
        {
            var jsonRpc = new JsonRpc(new HeaderDelimitedMessageHandler(output, input));
            return Create(jsonRpc, capabilitiesProvider, logger);
        }
    }
}
