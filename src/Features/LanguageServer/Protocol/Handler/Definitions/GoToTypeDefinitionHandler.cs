// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [Shared]
    [ExportLspMethod(LSP.Methods.TextDocumentTypeDefinitionName)]
    internal class GoToTypeDefinitionHandler : GoToDefinitionHandlerBase, IRequestHandler<LSP.TextDocumentPositionParams, LSP.Location[]>
    {
        [ImportingConstructor]
        public GoToTypeDefinitionHandler(IMetadataAsSourceFileService metadataAsSourceFileService) : base(metadataAsSourceFileService)
        {
        }

        public async Task<LSP.Location[]> HandleRequestAsync(Solution solution, LSP.TextDocumentPositionParams request,
            LSP.ClientCapabilities clientCapabilities, CancellationToken cancellationToken)
        {
            return await GetDefinitionAsync(solution, request, true, cancellationToken).ConfigureAwait(false);
        }
    }
}
