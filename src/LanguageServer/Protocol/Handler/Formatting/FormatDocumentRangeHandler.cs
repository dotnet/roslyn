// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [ExportCSharpVisualBasicStatelessLspService(typeof(FormatDocumentRangeHandler)), Shared]
    [Method(Methods.TextDocumentRangeFormattingName)]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class FormatDocumentRangeHandler() : AbstractFormatDocumentHandlerBase<DocumentRangeFormattingParams, TextEdit[]?>
    {
        public override TextDocumentIdentifier GetTextDocumentIdentifier(DocumentRangeFormattingParams request) => request.TextDocument;

        public override Task<TextEdit[]?> HandleRequestAsync(
            DocumentRangeFormattingParams request,
            RequestContext context,
            CancellationToken cancellationToken)
            => GetTextEditsAsync(context, request.Options, cancellationToken, range: request.Range);
    }
}
