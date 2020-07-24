// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [Shared]
    [ExportLspMethod(LSP.MSLSPMethods.OnAutoInsertName)]
    internal class OnAutoInsertHandler : AbstractRequestHandler<LSP.DocumentOnAutoInsertParams, LSP.DocumentOnAutoInsertResponseItem>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public OnAutoInsertHandler(ILspSolutionProvider solutionProvider) : base(solutionProvider)
        {
        }

        public override Task<LSP.DocumentOnAutoInsertResponseItem> HandleRequestAsync(LSP.DocumentOnAutoInsertParams request, LSP.ClientCapabilities clientCapabilities, string? clientName,
            CancellationToken cancellationToken)
            => OnAutoInsertAsync(request, clientName, cancellationToken);

        private async Task<LSP.DocumentOnAutoInsertResponseItem> OnAutoInsertAsync(LSP.DocumentOnAutoInsertParams autoInsertParams, string? clientName, CancellationToken cancellationToken)
        {
            var document = SolutionProvider.GetDocument(autoInsertParams.TextDocument, clientName);

            if (document != null)
            {
                var service = document.GetRequiredLanguageService<IDocumentationCommentSnippetService>();

                var syntaxTree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);

                var linePosition = ProtocolConversions.PositionToLinePosition(autoInsertParams.Position);
                var position = sourceText.Lines.GetPosition(linePosition);

                var exteriorTriviaText = "///";

                var result = autoInsertParams.Character == "\n"
                    ? service.GetDocumentationCommentSnippetOnEnterTyped(syntaxTree, sourceText, position, options, exteriorTriviaText, cancellationToken)
                    : service.GetDocumentationCommentSnippetOnCharacterTyped(syntaxTree, sourceText, position, options, cancellationToken);

                if (result != null)
                {
                    return new LSP.DocumentOnAutoInsertResponseItem
                    {
                        TextEditFormat = LSP.InsertTextFormat.Snippet,
                        TextEdit = new LSP.TextEdit
                        {
                            NewText = result.SnippetText.Insert(result.CaretOffset, "$0"),
                            Range = new LSP.Range
                            {
                                // GetDocumentationCommentOnCharacterTyped returns the text to insert _after_ the triple-slash so we
                                // can just insert it at the current position
                                Start = autoInsertParams.Position,
                                End = autoInsertParams.Position
                            }
                        }
                    };
                }
            }

            return new LSP.DocumentOnAutoInsertResponseItem();
        }
    }
}
