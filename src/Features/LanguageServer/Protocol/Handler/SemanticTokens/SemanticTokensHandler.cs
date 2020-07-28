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
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens
{
    /// <summary>
    /// Computes the semantic tokens for a whole document.
    /// </summary>
    /// <remarks>
    /// This handler is invoked when a user opens a file. Depending on the size of the file, the full token set may be
    /// slow to compute, so the <see cref="SemanticTokensRangeHandler"/> is also called when a file is opened in order
    /// to render UI results quickly until this handler finishes running.
    /// Unlike the range handler, the whole document handler may be called again if the LSP client finds an edit that
    /// is difficult to correctly apply to their tags cache. This allows for reliable recovery from errors and accounts
    /// for limitations in the edits application logic.
    /// </remarks>
    [ExportLspMethod(LSP.SemanticTokensMethods.TextDocumentSemanticTokensName), Shared]
    internal class SemanticTokensHandler : AbstractSemanticTokensRequestHandler<LSP.SemanticTokensParams, LSP.SemanticTokens>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public SemanticTokensHandler(ILspSolutionProvider solutionProvider) : base(solutionProvider)
        {
        }

        public override async Task<LSP.SemanticTokens> HandleRequestAsync(
            SemanticTokensParams request,
            SemanticTokensCache tokensCache,
            ClientCapabilities clientCapabilities,
            string? clientName,
            CancellationToken cancellationToken)
        {
            var previousResultId = tokensCache.Tokens.ResultId == null ? 0 : int.Parse(tokensCache.Tokens.ResultId);

            // Since whole document requests are usually sent upon opening a file, previousRequestId is usually 0.
            // However, we can't always make this assumption since whole document requests can also be sent in the
            // case of errors or if LSP finds an edit we sent them too difficult to apply.
            return await SemanticTokensHelpers.ComputeSemanticTokensAsync(
                request.TextDocument, previousResultId, clientName, SolutionProvider, range: null,
                cancellationToken).ConfigureAwait(false);
        }
    }
}
