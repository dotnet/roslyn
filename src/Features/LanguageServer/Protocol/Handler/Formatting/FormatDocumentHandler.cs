// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [Shared]
    [ExportLspMethod(LSP.Methods.TextDocumentFormattingName)]
    internal class FormatDocumentHandler : FormatDocumentHandlerBase, IRequestHandler<LSP.DocumentFormattingParams, LSP.TextEdit[]>
    {
        public async Task<LSP.TextEdit[]> HandleRequestAsync(Solution solution, LSP.DocumentFormattingParams request, LSP.ClientCapabilities clientCapabilities, CancellationToken cancellationToken)
        {
            return await GetTextEdits(solution, request.TextDocument.Uri, cancellationToken).ConfigureAwait(false);
        }
    }
}
