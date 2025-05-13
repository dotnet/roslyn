// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Roslyn.LanguageServer.Protocol;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens;

[Method(Methods.TextDocumentSemanticTokensFullName)]
internal sealed class SemanticTokensFullHandler(
    IGlobalOptionService globalOptions,
    SemanticTokensRefreshQueue semanticTokensRefreshQueue)
    : ILspServiceDocumentRequestHandler<SemanticTokensFullParams, LSP.SemanticTokens>
{
    private readonly IGlobalOptionService _globalOptions = globalOptions;
    private readonly SemanticTokensRefreshQueue _semanticTokenRefreshQueue = semanticTokensRefreshQueue;

    public bool MutatesSolutionState => false;
    public bool RequiresLSPSolution => true;

    public TextDocumentIdentifier GetTextDocumentIdentifier(LSP.SemanticTokensFullParams request)
    {
        Contract.ThrowIfNull(request.TextDocument);
        return request.TextDocument;
    }

    public async Task<LSP.SemanticTokens> HandleRequestAsync(
        SemanticTokensFullParams request,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(request.TextDocument);

        // Passing an null array of ranges will cause the helper to return tokens for the entire document.
        var tokensData = await SemanticTokensHelpers.HandleRequestHelperAsync(
            _globalOptions, _semanticTokenRefreshQueue, ranges: null, context, cancellationToken).ConfigureAwait(false);
        return new LSP.SemanticTokens { Data = tokensData };
    }
}
