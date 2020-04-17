// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [Shared]
    [ExportLspMethod(Methods.TextDocumentRangeFormattingName)]
    internal class FormatDocumentRangeHandler : FormatDocumentHandlerBase, IRequestHandler<DocumentRangeFormattingParams, TextEdit[]>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FormatDocumentRangeHandler()
        {
        }

        public async Task<TextEdit[]> HandleRequestAsync(Solution solution, DocumentRangeFormattingParams request, ClientCapabilities clientCapabilities,
            string? clientName, CancellationToken cancellationToken)
        {
            return await GetTextEditsAsync(solution, request.TextDocument.Uri, clientName, cancellationToken, range: request.Range).ConfigureAwait(false);
        }
    }
}
