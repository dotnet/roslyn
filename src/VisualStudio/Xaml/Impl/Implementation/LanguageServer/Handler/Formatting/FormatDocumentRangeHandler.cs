// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServices.Xaml.LanguageServer.Handler
{
    [ExportStatelessXamlLspService(typeof(FormatDocumentRangeHandler)), Shared]
    [Method(Methods.TextDocumentRangeFormattingName)]
    internal class FormatDocumentRangeHandler : AbstractFormatDocumentHandlerBase<DocumentRangeFormattingParams, TextEdit[]>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FormatDocumentRangeHandler()
        {
        }

        public override TextDocumentIdentifier GetTextDocumentIdentifier(DocumentRangeFormattingParams request) => request.TextDocument;

        public override Task<TextEdit[]> HandleRequestAsync(DocumentRangeFormattingParams request, RequestContext context, CancellationToken cancellationToken)
            => GetTextEditsAsync(request.Options, context, cancellationToken, range: request.Range);
    }
}
