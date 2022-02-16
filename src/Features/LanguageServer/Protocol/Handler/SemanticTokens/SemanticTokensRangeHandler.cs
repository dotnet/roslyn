// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens
{
    /// <summary>
    /// Computes the semantic tokens for a given range.
    /// </summary>
    [ExportRoslynLanguagesLspRequestHandlerProvider(typeof(SemanticTokensRangeHandler)), Shared]
    [Method(Methods.TextDocumentSemanticTokensRangeName)]
    internal class SemanticTokensRangeHandler : AbstractStatelessRequestHandler<LSP.SemanticTokensRangeParams, LSP.SemanticTokens>
    {
        private readonly IGlobalOptionService _globalOptions;

        public override bool MutatesSolutionState => false;
        public override bool RequiresLSPSolution => true;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public SemanticTokensRangeHandler(IGlobalOptionService globalOptions)
        {
            _globalOptions = globalOptions;
        }

        public override LSP.TextDocumentIdentifier? GetTextDocumentIdentifier(LSP.SemanticTokensRangeParams request)
        {
            Contract.ThrowIfNull(request.TextDocument);
            return request.TextDocument;
        }

        public override async Task<LSP.SemanticTokens> HandleRequestAsync(
            LSP.SemanticTokensRangeParams request,
            RequestContext context,
            CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(request.TextDocument, "TextDocument is null.");
            Contract.ThrowIfNull(context.Document, "Document is null.");

            var options = _globalOptions.GetClassificationOptions(context.Document.Project.Language);

            // The results from the range handler should not be cached since we don't want to cache
            // partial token results. In addition, a range request is only ever called with a whole
            // document request, so caching range results is unnecessary since the whole document
            // handler will cache the results anyway.
            var (tokensData, isFinalized) = await SemanticTokensHelpers.ComputeSemanticTokensDataAsync(
                context.Document,
                SemanticTokensHelpers.TokenTypeToIndex,
                request.Range,
                options,
                includeSyntacticClassifications: context.Document.IsRazorDocument(),
                cancellationToken).ConfigureAwait(false);

            return new RoslynSemanticTokens { Data = tokensData, IsFinalized = isFinalized };
        }
    }
}
