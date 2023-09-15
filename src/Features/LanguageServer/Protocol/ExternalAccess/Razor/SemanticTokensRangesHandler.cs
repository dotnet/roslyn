// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.ExternalAccess.Razor;

[ExportCSharpVisualBasicStatelessLspService(typeof(SemanticTokensRangesHandler)), Shared]
[Method(SemanticRangesMethodName)]
internal class SemanticTokensRangesHandler : ILspServiceRequestHandler<SemanticTokensRangesParams, SemanticTokens>
{
    public const string SemanticRangesMethodName = "textDocument/semanticTokens/ranges";
    private readonly IGlobalOptionService _globalOptions;
    private readonly SemanticTokensRefreshQueue _semanticTokenRefreshQueue;

    public bool MutatesSolutionState => false;

    public bool RequiresLSPSolution => true;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public SemanticTokensRangesHandler(
        IGlobalOptionService globalOptions,
        SemanticTokensRefreshQueue semanticTokensRefreshQueue)
    {
        _globalOptions = globalOptions;
        _semanticTokenRefreshQueue = semanticTokensRefreshQueue;
    }

    public async Task<SemanticTokens> HandleRequestAsync(
            SemanticTokensRangesParams request,
            RequestContext context,
            CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(request.TextDocument, "TextDocument is null.");
        var tokensData = await SemanticTokensHelpers.HandleRequestHelperAsync(_globalOptions, _semanticTokenRefreshQueue, request.Ranges, context, cancellationToken).ConfigureAwait(false);
        return new SemanticTokens { Data = tokensData };
    }
}
