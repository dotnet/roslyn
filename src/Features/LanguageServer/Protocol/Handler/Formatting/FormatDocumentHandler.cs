// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [Shared]
    [ExportLspMethod(LSP.Methods.TextDocumentFormattingName, mutatesSolutionState: false)]
    internal class FormatDocumentHandler : AbstractFormatDocumentHandlerBase<LSP.DocumentFormattingParams, LSP.TextEdit[]>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FormatDocumentHandler()
        {
        }

        public override LSP.TextDocumentIdentifier? GetTextDocumentIdentifier(LSP.DocumentFormattingParams request) => request.TextDocument;

        public override Task<LSP.TextEdit[]> HandleRequestAsync(LSP.DocumentFormattingParams request, RequestContext context, CancellationToken cancellationToken)
            => GetTextEditsAsync(context, cancellationToken);
    }
}
