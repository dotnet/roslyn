// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LiveShare.LanguageServices;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare
{
    internal class DocumentSymbolsHandlerShim : AbstractLiveShareHandlerShim<DocumentSymbolParams, SymbolInformation[]>
    {
        public DocumentSymbolsHandlerShim(IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers)
            : base(requestHandlers, Methods.TextDocumentDocumentSymbolName)
        {
        }

        public override async Task<SymbolInformation[]> HandleAsync(DocumentSymbolParams param, RequestContext<Solution> requestContext, CancellationToken cancellationToken)
        {
            var clientCapabilities = requestContext.ClientCapabilities?.ToObject<ClientCapabilities>();
            var hierarchicalSupport = clientCapabilities?.TextDocument?.DocumentSymbol?.HierarchicalDocumentSymbolSupport;
            if (hierarchicalSupport == true)
            {
                // If the value is true, set it to false.  Liveshare does not support hierarchical document symbols.
                clientCapabilities.TextDocument.DocumentSymbol.HierarchicalDocumentSymbolSupport = false;
            }

            var handler = (IRequestHandler<DocumentSymbolParams, object[]>)LazyRequestHandler.Value;
            var response = await handler.HandleRequestAsync(requestContext.Context, param, clientCapabilities, cancellationToken).ConfigureAwait(false);

            // Since hierarchicalSupport will never be true, it is safe to cast the response to SymbolInformation[]
            return response?.Select(obj => (SymbolInformation)obj).ToArray();
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.RoslynContractName, Methods.TextDocumentDocumentSymbolName)]
    [Obsolete("Used for backwards compatibility with old liveshare clients.")]
    internal class RoslynDocumentSymbolsHandlerShim : DocumentSymbolsHandlerShim
    {
        [ImportingConstructor]
        public RoslynDocumentSymbolsHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers) : base(requestHandlers)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.CSharpContractName, Methods.TextDocumentDocumentSymbolName)]
    internal class CSharpDocumentSymbolsHandlerShim : DocumentSymbolsHandlerShim
    {
        [ImportingConstructor]
        public CSharpDocumentSymbolsHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers) : base(requestHandlers)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.VisualBasicContractName, Methods.TextDocumentDocumentSymbolName)]
    internal class VisualBasicDocumentSymbolsHandlerShim : DocumentSymbolsHandlerShim
    {
        [ImportingConstructor]
        public VisualBasicDocumentSymbolsHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers) : base(requestHandlers)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.TypeScriptContractName, Methods.TextDocumentDocumentSymbolName)]
    internal class TypeScriptDocumentSymbolsHandlerShim : DocumentSymbolsHandlerShim
    {
        [ImportingConstructor]
        public TypeScriptDocumentSymbolsHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers) : base(requestHandlers)
        {
        }
    }
}
