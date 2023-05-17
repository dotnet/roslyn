// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Api;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens
{
    [Method(Methods.TextDocumentSemanticTokensRangeName)]
    internal class SemanticTokensRangeHandler : ILspServiceDocumentRequestHandler<SemanticTokensRangeParams, LSP.SemanticTokens>
    {
        private readonly IGlobalOptionService _globalOptions;
        private readonly SemanticTokensRefreshQueue _semanticTokenRefreshQueue;

        public bool MutatesSolutionState => false;
        public bool RequiresLSPSolution => true;

        public SemanticTokensRangeHandler(
            IGlobalOptionService globalOptions,
            SemanticTokensRefreshQueue semanticTokensRefreshQueue)
        {
            _globalOptions = globalOptions;
            _semanticTokenRefreshQueue = semanticTokensRefreshQueue;
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
            Contract.ThrowIfNull(request.TextDocument, "TextDocument is null.");
            var contextDocument = context.GetRequiredDocument();

            // If the full compilation is not yet available, we'll try getting a partial one. It may contain inaccurate
            // results but will speed up how quickly we can respond to the client's request.
            var document = contextDocument.WithFrozenPartialSemantics(cancellationToken);
            var project = document.Project;
            var options = _globalOptions.GetClassificationOptions(project.Language) with { ForceFrozenPartialSemanticsForCrossProcessOperations = true };

            // The results from the range handler should not be cached since we don't want to cache
            // partial token results. In addition, a range request is only ever called with a whole
            // document request, so caching range results is unnecessary since the whole document
            // handler will cache the results anyway.
            var capabilities = context.GetRequiredClientCapabilities();
            var tokensData = await SemanticTokensHelpers.ComputeSemanticTokensDataAsync(
                capabilities,
                document,
                request.Range,
                options,
                cancellationToken).ConfigureAwait(false);

            // The above call to get semantic tokens may be inaccurate (because we use frozen partial semantics).  Kick
            // off a request to ensure that the OOP side gets a fully up to compilation for this project.  Once it does
            // we can optionally choose to notify our caller to do a refresh if we computed a compilation for a new
            // solution snapshot.
            await _semanticTokenRefreshQueue.TryEnqueueRefreshComputationAsync(project, cancellationToken).ConfigureAwait(false);

            return new LSP.SemanticTokens { Data = tokensData };
        }
    }
}
