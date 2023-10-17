// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.IO;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CommonLanguageServerProtocol.Framework;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.LanguageServer
{
    [Export(typeof(ILanguageServerFactory)), Shared]
    internal class CSharpVisualBasicLanguageServerFactory : ILanguageServerFactory
    {
        private readonly AbstractLspServiceProvider _lspServiceProvider;
        private readonly IEnumerable<Lazy<ICapabilityRegistrationsProvider>> _capabilityRegistrationsProviders;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpVisualBasicLanguageServerFactory(
            CSharpVisualBasicLspServiceProvider lspServiceProvider,
            [ImportMany] IEnumerable<Lazy<ICapabilityRegistrationsProvider>> capabilityRegistrationsProviders)
        {
            _lspServiceProvider = lspServiceProvider;
            _capabilityRegistrationsProviders = capabilityRegistrationsProviders;
        }

        public AbstractLanguageServer<RequestContext> Create(
            JsonRpc jsonRpc,
            ICapabilitiesProvider capabilitiesProvider,
            WellKnownLspServerKinds serverKind,
            ILspServiceLogger logger,
            HostServices hostServices,
            IEnumerable<Lazy<ICapabilityRegistrationsProvider>>? capabilityRegistrationsProviders = null)
        {
            var server = new RoslynLanguageServer(
                _lspServiceProvider,
                jsonRpc,
                capabilitiesProvider,
                logger,
                hostServices,
                ProtocolConstants.RoslynLspLanguages,
                serverKind,
                _capabilityRegistrationsProviders);

            return server;
        }

        public AbstractLanguageServer<RequestContext> Create(Stream input, Stream output, ICapabilitiesProvider capabilitiesProvider, ILspServiceLogger logger, HostServices hostServices)
        {
            var jsonRpc = new JsonRpc(new HeaderDelimitedMessageHandler(output, input));
            return Create(jsonRpc, capabilitiesProvider, WellKnownLspServerKinds.CSharpVisualBasicLspServer, logger, hostServices);
        }
    }
}
