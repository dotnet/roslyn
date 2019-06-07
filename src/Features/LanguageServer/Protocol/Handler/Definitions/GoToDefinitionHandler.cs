// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [Shared]
    [ExportLspMethod(LSP.Methods.TextDocumentDefinitionName)]
    internal class GoToDefinitionHandler : GoToDefinitionHandlerBase, IRequestHandler<LSP.TextDocumentPositionParams, object>
    {
        [ImportingConstructor]
        public GoToDefinitionHandler(IMetadataAsSourceFileService metadataAsSourceFileService) : base(metadataAsSourceFileService)
        {
        }

        public async Task<object> HandleRequestAsync(Solution solution, LSP.TextDocumentPositionParams request,
            LSP.ClientCapabilities clientCapabilities, CancellationToken cancellationToken)
        {
            return await GetDefinitionAsync(solution, request, false, cancellationToken).ConfigureAwait(false);
        }
    }
}
