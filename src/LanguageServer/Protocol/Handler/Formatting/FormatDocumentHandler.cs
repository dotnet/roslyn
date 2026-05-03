// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

[ExportCSharpVisualBasicStatelessLspService(typeof(FormatDocumentHandler)), Shared]
[Method(LSP.Methods.TextDocumentFormattingName)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class FormatDocumentHandler(IGlobalOptionService globalOptions) : AbstractFormatDocumentHandlerBase<LSP.DocumentFormattingParams, LSP.TextEdit[]?>
{
    private readonly IGlobalOptionService _globalOptions = globalOptions;

    public override LSP.TextDocumentIdentifier GetTextDocumentIdentifier(LSP.DocumentFormattingParams request) => request.TextDocument;

    public override Task<LSP.TextEdit[]?> HandleRequestAsync(
        LSP.DocumentFormattingParams request,
        RequestContext context,
        CancellationToken cancellationToken)
        => GetTextEditsAsync(context, request.Options, _globalOptions, cancellationToken);
}
