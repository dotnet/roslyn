// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        public async Task<LSP.TextEdit[]> HandleRequestAsync(Solution solution, LSP.DocumentFormattingParams request, LSP.ClientCapabilities clientCapabilities,
            CancellationToken cancellationToken)
        {
            return await GetTextEdits(solution, request.TextDocument.Uri, cancellationToken).ConfigureAwait(false);
        }
    }
}
