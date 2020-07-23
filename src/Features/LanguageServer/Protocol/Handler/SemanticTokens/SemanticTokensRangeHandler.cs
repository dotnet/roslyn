// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens
{
    internal class SemanticTokensRangeHandler : AbstractRequestHandler<LSP.SemanticTokensRangeParams, LSP.SemanticTokens>
    {
        private IProgress<SumType<LSP.SemanticTokens, SemanticTokensEdits>>? _progress;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public SemanticTokensRangeHandler(ILspSolutionProvider solutionProvider) : base(solutionProvider)
        {
        }

        public override async Task<LSP.SemanticTokens> HandleRequestAsync(
            SemanticTokensRangeParams request,
            ClientCapabilities clientCapabilities,
            string? clientName,
            CancellationToken cancellationToken)
        {
            _progress = request.PartialResultToken;

            return await SemanticTokensHelpers.ComputeSemanticTokensAsync(
                request.TextDocument, clientName, useStreaming: request.PartialResultToken != null,
                ReportTokensAsync, SolutionProvider, cancellationToken, request.Range).ConfigureAwait(false);
        }

        private Task ReportTokensAsync(ImmutableArray<int> tokensToReport, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_progress);
            SemanticTokensHelpers.ReportTokens(_progress, tokensToReport);
            return Task.CompletedTask;
        }
    }
}
