// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Text.Json;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.VisualStudio.Composition;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor
{
    [Shared]
    [Export(typeof(RazorTestLanguageServerFactory))]
    [Export(typeof(AbstractRazorLanguageServerFactoryWrapper))]
    [PartNotDiscoverable]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    [method: ImportingConstructor]
    internal class RazorTestLanguageServerFactory(ILanguageServerFactory languageServerFactory, RazorTestCapabilitiesProvider razorTestCapabilitiesProvider) : AbstractRazorLanguageServerFactoryWrapper
    {
        private readonly ILanguageServerFactory _languageServerFactory = languageServerFactory;

        internal override IRazorLanguageServerTarget CreateLanguageServer(JsonRpc jsonRpc, JsonSerializerOptions options, IRazorTestCapabilitiesProvider razorCapabilitiesProvider, HostServices hostServices)
        {
            return CreateLanguageServerCore(jsonRpc, options, razorCapabilitiesProvider, hostServices, WellKnownLspServerKinds.RazorLspServer);
        }

        private IRazorLanguageServerTarget CreateLanguageServerCore(JsonRpc jsonRpc, JsonSerializerOptions options, IRazorTestCapabilitiesProvider razorCapabilitiesProvider, HostServices hostServices, WellKnownLspServerKinds serverKind)
        {
            razorTestCapabilitiesProvider.RazorTestCapabilities = razorCapabilitiesProvider;
            razorTestCapabilitiesProvider.JsonSerializerOptions = options;
            var languageServer = _languageServerFactory.Create(jsonRpc, options, serverKind, NoOpLspLogger.Instance, hostServices);

            return new RazorLanguageServerTargetWrapper(languageServer);
        }

        internal override DocumentInfo CreateDocumentInfo(
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

        internal override void AddJsonConverters(JsonSerializerOptions options)
        {
            ProtocolConversions.AddLspSerializerOptions(options);
        }
    }
}
