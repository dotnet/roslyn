// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens
{
    /// <summary>
    /// Computes the semantic tokens for a given range.
    /// </summary>
    /// <remarks>
    /// When a user opens a file, it can be beneficial to only compute the semantic tokens for the visible range
    /// for faster UI rendering.
    /// The range handler is only invoked when a file is opened. When the first whole document request completes
    /// via <see cref="SemanticTokensHandler"/>, the range handler is not invoked again for the rest of the session.
    /// </remarks>
    [ExportLspMethod(LSP.SemanticTokensMethods.TextDocumentSemanticTokensRangeName), Shared]
    internal class SemanticTokensRangeHandler : AbstractRequestHandler<LSP.SemanticTokensRangeParams, LSP.SemanticTokens>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public SemanticTokensRangeHandler(ILspSolutionProvider solutionProvider) : base(solutionProvider)
        {
        }

        public override async Task<LSP.SemanticTokens> HandleRequestAsync(
            LSP.SemanticTokensRangeParams request,
            LSP.ClientCapabilities clientCapabilities,
            string? clientName,
            CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(request.TextDocument);

            // The range handler should not be involved in caching. We don't want to cache partial token results.
            // In addition, a range request is only ever called with a whole document request, so caching range
            // results is unnecessary since the whole document handler will cache the results anyway.
            // We pass in 0 for the previousResultId argument in the call below, but it doesn't really matter
            // what we pass in since we won't update the cache with these results.
            return await SemanticTokensHelpers.ComputeSemanticTokensAsync(
                request.TextDocument, resultId: "0", clientName, SolutionProvider, request.Range,
                cancellationToken).ConfigureAwait(false);
        }
    }
}
