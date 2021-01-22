// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServices.Xaml.LanguageServer.Handler
{
    [LspMethod(LSP.Methods.TextDocumentFormattingName, mutatesSolutionState: false)]
    internal class FormatDocumentHandler : AbstractFormatDocumentHandlerBase<LSP.DocumentFormattingParams, LSP.TextEdit[]>
    {
        public override LSP.TextDocumentIdentifier? GetTextDocumentIdentifier(LSP.DocumentFormattingParams request) => request.TextDocument;

        public override Task<LSP.TextEdit[]> HandleRequestAsync(LSP.DocumentFormattingParams request, RequestContext context, CancellationToken cancellationToken)
            => GetTextEditsAsync(request.TextDocument, request.Options, context, cancellationToken);
    }
}
