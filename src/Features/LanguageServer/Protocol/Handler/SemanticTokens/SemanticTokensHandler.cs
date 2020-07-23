// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens
{
    [ExportLspMethod(LSP.SemanticTokensMethods.TextDocumentSemanticTokensName), Shared]
    internal class SemanticTokensHandler : AbstractRequestHandler<LSP.SemanticTokensParams, LSP.SemanticTokens>
    {
        private IProgress<SumType<LSP.SemanticTokens, SemanticTokensEdits>>? _progress;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public SemanticTokensHandler(ILspSolutionProvider solutionProvider) : base(solutionProvider)
        {
        }

        public override async Task<LSP.SemanticTokens> HandleRequestAsync(
            SemanticTokensParams request,
            ClientCapabilities clientCapabilities,
            string? clientName,
            CancellationToken cancellationToken)
        {
            _progress = request.PartialResultToken;

            return await SemanticTokensHelpers.ComputeSemanticTokensAsync(
                request.TextDocument, clientName, useStreaming: request.PartialResultToken != null,
                ReportTokensAsync, SolutionProvider, cancellationToken).ConfigureAwait(false);
        }

        private Task ReportTokensAsync(ImmutableArray<int> tokensToReport, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_progress);
            SemanticTokensHelpers.ReportTokens(_progress, tokensToReport);
            return Task.CompletedTask;
        }
    }
}
