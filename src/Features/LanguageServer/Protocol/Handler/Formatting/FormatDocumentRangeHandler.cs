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
    internal class FormatDocumentRangeHandler : AbstractFormatDocumentHandlerBase<DocumentRangeFormattingParams, TextEdit[]>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FormatDocumentRangeHandler(ILspSolutionProvider solutionProvider) : base(solutionProvider)
        {
        }

        public override Task<TextEdit[]> HandleRequestAsync(DocumentRangeFormattingParams request, RequestContext context, CancellationToken cancellationToken)
            => GetTextEditsAsync(request.TextDocument, context, cancellationToken, range: request.Range);
    }
}
