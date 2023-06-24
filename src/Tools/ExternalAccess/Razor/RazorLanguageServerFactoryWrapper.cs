// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Newtonsoft.Json.Linq;
using StreamJsonRpc;
using Microsoft.VisualStudio.Composition;
using Newtonsoft.Json;

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

        public IRazorLanguageServerTarget CreateLanguageServer(JsonRpc jsonRpc, IRazorTestCapabilitiesProvider razorCapabilitiesProvider, HostServices hostServices)
        {
            var capabilitiesProvider = new RazorCapabilitiesProvider(razorCapabilitiesProvider);
            var languageServer = _languageServerFactory.Create(jsonRpc, capabilitiesProvider, WellKnownLspServerKinds.RazorLspServer, NoOpLspLogger.Instance, hostServices);

            return new RazorLanguageServerTargetWrapper(languageServer);
        }

        [Obsolete("Use the overload that takes IRazorTestCapabilitiesProvider")]
        public IRazorLanguageServerTarget CreateLanguageServer(JsonRpc jsonRpc, IRazorCapabilitiesProvider razorCapabilitiesProvider, HostServices hostServices)
        {
            var capabilitiesProvider = new RazorCapabilitiesProvider(razorCapabilitiesProvider);
            var languageServer = _languageServerFactory.Create(jsonRpc, capabilitiesProvider, WellKnownLspServerKinds.RazorLspServer, NoOpLspLogger.Instance, hostServices);

            return new RazorLanguageServerTargetWrapper(languageServer);
        }

        public DocumentInfo CreateDocumentInfo(
            DocumentId id,
            string name,
            IReadOnlyList<string>? folders = null,
            SourceCodeKind sourceCodeKind = SourceCodeKind.Regular,
            TextLoader? loader = null,
            string? filePath = null,
            bool isGenerated = false,
            bool designTimeOnly = false,
            IRazorDocumentServiceProvider? razorDocumentServiceProvider = null)
        {
            folders ??= new List<string>();

            IDocumentServiceProvider? documentServiceProvider = null;
            if (razorDocumentServiceProvider is not null)
            {
                documentServiceProvider = new RazorDocumentServiceProviderWrapper(razorDocumentServiceProvider);
            }

            return DocumentInfo.Create(id, name, folders, sourceCodeKind, loader, filePath, isGenerated)
                .WithDesignTimeOnly(designTimeOnly)
                .WithDocumentServiceProvider(documentServiceProvider);
        }

        private class RazorCapabilitiesProvider : ICapabilitiesProvider
        {
#pragma warning disable CS0618 // Type or member is obsolete
            private readonly IRazorCapabilitiesProvider? _razorCapabilitiesProvider;
#pragma warning restore CS0618 // Type or member is obsolete
            private readonly IRazorTestCapabilitiesProvider? _razorTestCapabilitiesProvider;

            public RazorCapabilitiesProvider(IRazorTestCapabilitiesProvider razorTestCapabilitiesProvider)
            {
                _razorTestCapabilitiesProvider = razorTestCapabilitiesProvider;
            }

#pragma warning disable CS0618 // Type or member is obsolete
            public RazorCapabilitiesProvider(IRazorCapabilitiesProvider razorCapabilitiesProvider)
#pragma warning restore CS0618 // Type or member is obsolete
            {
                _razorCapabilitiesProvider = razorCapabilitiesProvider;
            }

            public ServerCapabilities GetCapabilities(ClientCapabilities clientCapabilities)
            {
                if (_razorTestCapabilitiesProvider is not null)
                {
                    // To avoid exposing types from MS.VS.LanguageServer.Protocol types we serialize and deserialize the capabilities
                    // so we can just pass string around. This is obviously not great for perf, but it is only used in Razor tests.
                    var clientCapabilitiesJson = JsonConvert.SerializeObject(clientCapabilities);
                    var serverCapabilitiesJson = _razorTestCapabilitiesProvider.GetServerCapabilitiesJson(clientCapabilitiesJson);
                    var serverCapabilities = JsonConvert.DeserializeObject<VSInternalServerCapabilities>(serverCapabilitiesJson);

                    if (serverCapabilities is null)
                    {
                        throw new InvalidOperationException("Could not deserialize server capabilities as VSInternalServerCapabilities");
                    }

                    return serverCapabilities;
                }

                return _razorCapabilitiesProvider!.GetCapabilities(clientCapabilities);
            }
        }
    }
}
