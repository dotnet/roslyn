// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Roslyn.LanguageServer.Protocol;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens;

[Method(Methods.TextDocumentSemanticTokensRangeName)]
internal sealed class SemanticTokensRangeHandler(
    IGlobalOptionService globalOptions,
    SemanticTokensRefreshQueue semanticTokensRefreshQueue) : ILspServiceDocumentRequestHandler<SemanticTokensRangeParams, LSP.SemanticTokens>
{
    private readonly IGlobalOptionService _globalOptions = globalOptions;
    private readonly SemanticTokensRefreshQueue _semanticTokenRefreshQueue = semanticTokensRefreshQueue;

    public bool MutatesSolutionState => false;
    public bool RequiresLSPSolution => true;

    public TextDocumentIdentifier GetTextDocumentIdentifier(SemanticTokensRangeParams request)
    {
        Contract.ThrowIfNull(request.TextDocument);
        return request.TextDocument;
    }

    public async Task<LSP.SemanticTokens> HandleRequestAsync(
        SemanticTokensRangeParams request,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(request.TextDocument, "TextDocument is null.");

        var tokensData = await SemanticTokensHelpers.HandleRequestHelperAsync(
            _globalOptions, _semanticTokenRefreshQueue, [request.Range], context, cancellationToken).ConfigureAwait(false);
        return new LSP.SemanticTokens { Data = tokensData };
    }
}
