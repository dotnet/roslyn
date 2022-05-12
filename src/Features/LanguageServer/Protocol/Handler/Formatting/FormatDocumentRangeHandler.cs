// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [ExportRoslynLanguagesLspRequestHandlerProvider(typeof(FormatDocumentRangeHandler)), Shared]
    [Method(Methods.TextDocumentRangeFormattingName)]
    internal sealed class FormatDocumentRangeHandler : AbstractFormatDocumentHandlerBase<DocumentRangeFormattingParams, TextEdit[]?>
    {
        private readonly IGlobalOptionService _globalOptions;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FormatDocumentRangeHandler(IGlobalOptionService globalOptions)
        {
            _globalOptions = globalOptions;
        }

        public override TextDocumentIdentifier? GetTextDocumentIdentifier(DocumentRangeFormattingParams request) => request.TextDocument;

        public override Task<TextEdit[]?> HandleRequestAsync(
            DocumentRangeFormattingParams request,
            RequestContext context,
            CancellationToken cancellationToken)
            => GetTextEditsAsync(context, request.Options, _globalOptions, cancellationToken, range: request.Range);
    }
}
