// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [Shared]
    [ExportLspMethod(Methods.TextDocumentRangeFormattingName)]
    internal class FormatDocumentRangeHandler : FormatDocumentHandlerBase, IRequestHandler<DocumentRangeFormattingParams, TextEdit[]>
    {
        public async Task<TextEdit[]> HandleRequestAsync(Solution solution, DocumentRangeFormattingParams request, ClientCapabilities clientCapabilities,
            CancellationToken cancellationToken)
        {
            return await GetTextEditsAsync(solution, request.TextDocument.Uri, cancellationToken, range: request.Range).ConfigureAwait(false);
        }
    }
}
