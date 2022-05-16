// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Newtonsoft.Json.Linq;
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

        public IRazorLanguageServerTarget CreateLanguageServer(JsonRpc jsonRpc, IRazorCapabilitiesProvider razorCapabilitiesProvider)
        {
            var capabilitiesProvider = new RazorCapabilitiesProvider(razorCapabilitiesProvider);
            var languageServer = _languageServerFactory.Create(jsonRpc, capabilitiesProvider, NoOpLspLogger.Instance);

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

            return DocumentInfo.Create(id, name, folders, sourceCodeKind, loader, filePath, isGenerated, designTimeOnly, documentServiceProvider);
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
