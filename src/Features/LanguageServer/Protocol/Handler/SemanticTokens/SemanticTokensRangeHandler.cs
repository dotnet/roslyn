// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Api;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServer.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens
{
    [Method(Methods.TextDocumentSemanticTokensRangeName)]
    internal class SemanticTokensRangeHandler : ILspServiceDocumentRequestHandler<SemanticTokensRangeParams, LSP.SemanticTokens>
    {
        private readonly SemanticTokensRangesHandler _semanticTokensRangesHandler;

        public bool MutatesSolutionState => false;
        public bool RequiresLSPSolution => true;

        public SemanticTokensRangeHandler(
            SemanticTokensRangesHandler semanticTokensRangesHandler)
        {
            _semanticTokensRangesHandler = semanticTokensRangesHandler;
        }

        public TextDocumentIdentifier GetTextDocumentIdentifier(LSP.SemanticTokensRangeParams request)
        {
            Contract.ThrowIfNull(request.TextDocument);
            return request.TextDocument;
        }

        public async Task<LSP.SemanticTokens> HandleRequestAsync(
            SemanticTokensRangeParams request,
            RequestContext context,
            CancellationToken cancellationToken)
        {
            return await _semanticTokensRangesHandler.HandleRequestAsync(request, context, cancellationToken).ConfigureAwait(false);
        }
    }
}
