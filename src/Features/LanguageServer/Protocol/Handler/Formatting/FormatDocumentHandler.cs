// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [ExportRoslynLanguagesLspRequestHandlerProvider(typeof(FormatDocumentHandler)), Shared]
    [Method(LSP.Methods.TextDocumentFormattingName)]
    internal sealed class FormatDocumentHandler : AbstractFormatDocumentHandlerBase<LSP.DocumentFormattingParams, LSP.TextEdit[]?>
    {
        private readonly IGlobalOptionService _globalOptions;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FormatDocumentHandler(IGlobalOptionService globalOptions)
        {
            _globalOptions = globalOptions;
        }

        public override LSP.TextDocumentIdentifier? GetTextDocumentIdentifier(LSP.DocumentFormattingParams request) => request.TextDocument;

        public override Task<LSP.TextEdit[]?> HandleRequestAsync(
            LSP.DocumentFormattingParams request,
            RequestContext context,
            CancellationToken cancellationToken)
            => GetTextEditsAsync(context, request.Options, _globalOptions, cancellationToken);
    }
}
