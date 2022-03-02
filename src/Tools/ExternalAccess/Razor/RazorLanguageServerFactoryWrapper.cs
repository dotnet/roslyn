// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.Text;
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
            bool designTimeOnly = false)
        {
            folders ??= new List<string>();
            return DocumentInfo.Create(id, name, folders, sourceCodeKind, loader, filePath, isGenerated, designTimeOnly, new RazorTestSpanMapperProvider());
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

        private class RazorTestSpanMapperProvider : IDocumentServiceProvider
        {
            TService IDocumentServiceProvider.GetService<TService>()
                => (TService)(object)new RazorTestSpanMappingService();
        }

        private class RazorTestSpanMappingService : ISpanMappingService
        {
            public bool SupportsMappingImportDirectives => throw new NotImplementedException();

            public Task<ImmutableArray<(string mappedFilePath, TextChange mappedTextChange)>> GetMappedTextChangesAsync(
                Document oldDocument,
                Document newDocument,
                CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public Task<ImmutableArray<MappedSpanResult>> MapSpansAsync(
                Document document,
                IEnumerable<TextSpan> spans,
                CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }
        }
    }
}
