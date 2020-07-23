// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens
{
    [ExportLspMethod(LSP.SemanticTokensMethods.TextDocumentSemanticTokensEditsName), Shared]
    internal class SemanticTokensEditsHandler : AbstractRequestHandler<LSP.SemanticTokensEditsParams, SumType<LSP.SemanticTokens, LSP.SemanticTokensEdits>>
    {
        private IProgress<SumType<LSP.SemanticTokens, SemanticTokensEdits>>? _progress;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public SemanticTokensEditsHandler(ILspSolutionProvider solutionProvider) : base(solutionProvider)
        {
        }

        public override async Task<SumType<LSP.SemanticTokens, SemanticTokensEdits>> HandleRequestAsync(
            SemanticTokensEditsParams request,
            ClientCapabilities clientCapabilities,
            string? clientName,
            CancellationToken cancellationToken)
        {
            _progress = request.PartialResultToken;

            if (request.PreviousResultId == null)
            {
                return await SemanticTokensHelpers.ComputeSemanticTokensAsync(
                    request.TextDocument, clientName, useStreaming: request.PartialResultToken != null,
                    ReportTokensAsync, SolutionProvider, cancellationToken).ConfigureAwait(false);
            }

            return new SemanticTokensEdits();
        }

        private Task ReportTokensAsync(ImmutableArray<int> tokensToReport, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_progress);
            SemanticTokensHelpers.ReportTokens(_progress, tokensToReport);
            return Task.CompletedTask;
        }
    }
}
