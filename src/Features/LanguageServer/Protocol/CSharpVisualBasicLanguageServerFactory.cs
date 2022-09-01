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

        public ILanguageServerTarget Create(
            JsonRpc jsonRpc,
            ICapabilitiesProvider capabilitiesProvider,
            ILspLogger logger)
        {
            return new LanguageServerTarget(
                _lspServiceProvider, jsonRpc,
                capabilitiesProvider,
                _listenerProvider,
                logger,
                ProtocolConstants.RoslynLspLanguages,
                WellKnownLspServerKinds.CSharpVisualBasicLspServer);
        }
    }
}
