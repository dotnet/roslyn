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
    [ExportLspRequestHandler(LiveShareConstants.RoslynContractName, Methods.TextDocumentDocumentSymbolName)]
    internal class DocumentSymbolsHandlerShim : AbstractLiveShareHandlerShim<DocumentSymbolParams, SymbolInformation[]>
    {
        [ImportingConstructor]
        public DocumentSymbolsHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers)
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
}
