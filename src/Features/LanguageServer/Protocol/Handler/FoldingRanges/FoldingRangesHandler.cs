// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [Shared]
    [ExportLspMethod(Methods.TextDocumentFoldingRangeName, mutatesSolutionState: false)]
    internal sealed class FoldingRangesHandler : IRequestHandler<FoldingRangeParams, FoldingRange[]>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FoldingRangesHandler()
        {
        }

        public TextDocumentIdentifier? GetTextDocumentIdentifier(FoldingRangeParams request) => request.TextDocument;

        public async Task<FoldingRange[]> HandleRequestAsync(FoldingRangeParams request, RequestContext context, CancellationToken cancellationToken)
        {
            var document = context.Document;
            if (document == null)
            {
                return Array.Empty<FoldingRange>();
            }

            return await FoldingRangesHelper.GetFoldingRangesAsync(document, cancellationToken).ConfigureAwait(false);
        }
    }
}
