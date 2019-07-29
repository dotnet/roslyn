// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            CancellationToken cancellationToken, bool keepThreadContext = false)
        {
            return await GetTextEdits(solution, request.TextDocument.Uri, keepThreadContext, cancellationToken, range: request.Range).ConfigureAwait(keepThreadContext);
        }
    }
}
