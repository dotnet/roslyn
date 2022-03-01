// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor
{
    [Export(typeof(IRazorLanguageServerFactoryWrapper))]
    [Shared]
    internal class RazorLanguageServerFactoryWrapper : IRazorLanguageServerFactoryWrapper
    {
        private readonly ILanguageServerFactory _languageServerFactory;

        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        [ImportingConstructor]
        public RazorLanguageServerFactoryWrapper(ILanguageServerFactory languageServerFactory)
        {
            if (languageServerFactory is null)
            {
                throw new ArgumentNullException(nameof(languageServerFactory));
            }

            _languageServerFactory = languageServerFactory;
        }

        public IRazorLanguageServerTarget Create(JsonRpc jsonRpc, IRazorCapabilitiesProvider razorCapabilitiesProvider)
        {
            var capabilitiesProvider = new RazorCapabilitiesProvider(razorCapabilitiesProvider);
            var languageServer = _languageServerFactory.Create(jsonRpc, capabilitiesProvider, NoOpLspLogger.Instance, clientName: ProtocolConstants.RazorCSharp);

            return new RazorLanguageServerTargetWrapper(languageServer);
        }

        private class RazorCapabilitiesProvider : ICapabilitiesProvider
        {
            private readonly IRazorCapabilitiesProvider _razorCapabilitiesProvider;

            public RazorCapabilitiesProvider(IRazorCapabilitiesProvider razorCapabilitiesProvider)
            {
                _razorCapabilitiesProvider = razorCapabilitiesProvider;
            }

            public ServerCapabilities GetCapabilities(ClientCapabilities clientCapabilities)
                => _razorCapabilitiesProvider.GetCapabilities(clientCapabilities);
        }
    }
}
