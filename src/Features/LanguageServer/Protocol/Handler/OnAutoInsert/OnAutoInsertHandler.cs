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
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [Shared]
    [ExportLspMethod(LSP.MSLSPMethods.OnAutoInsertName)]
    internal class OnAutoInsertHandler : AbstractRequestHandler<LSP.DocumentOnAutoInsertParams, LSP.DocumentOnAutoInsertResponseItem[]>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public OnAutoInsertHandler(ILspSolutionProvider solutionProvider) : base(solutionProvider)
        {
        }

        public override async Task<LSP.DocumentOnAutoInsertResponseItem[]> HandleRequestAsync(LSP.DocumentOnAutoInsertParams autoInsertParams, RequestContext context, CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<LSP.DocumentOnAutoInsertResponseItem>.GetInstance(out var response);

            var document = SolutionProvider.GetDocument(autoInsertParams.TextDocument, context.ClientName);

            if (document == null)
            {
                return response.ToArray();
            }

            var service = document.GetRequiredLanguageService<IDocumentationCommentSnippetService>();

            // The editor calls this handler for C# and VB comment characters, but we only need to process the one for the language that matches the document
            if (autoInsertParams.Character != "\n" && autoInsertParams.Character != service.DocumentationCommentCharacter)
            {
                return response.ToArray();
            }

            var syntaxTree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);

            var linePosition = ProtocolConversions.PositionToLinePosition(autoInsertParams.Position);
            var position = sourceText.Lines.GetPosition(linePosition);

            var result = autoInsertParams.Character == "\n"
                ? service.GetDocumentationCommentSnippetOnEnterTyped(syntaxTree, sourceText, position, options, cancellationToken)
                : service.GetDocumentationCommentSnippetOnCharacterTyped(syntaxTree, sourceText, position, options, cancellationToken);

            if (result == null)
            {
                return response.ToArray();
            }

            response.Add(new LSP.DocumentOnAutoInsertResponseItem
            {
                TextEditFormat = LSP.InsertTextFormat.Snippet,
                TextEdit = new LSP.TextEdit
                {
                    NewText = result.SnippetText.Insert(result.CaretOffset, "$0"),
                    Range = ProtocolConversions.TextSpanToRange(result.SpanToReplace, sourceText)
                }
            });

            return response.ToArray();
        }
    }
}
