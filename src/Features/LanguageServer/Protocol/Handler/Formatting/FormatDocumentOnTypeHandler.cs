// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [Shared]
    [ExportLspMethod(Methods.TextDocumentOnTypeFormattingName)]
    internal class FormatDocumentOnTypeHandler : AbstractRequestHandler<DocumentOnTypeFormattingParams, TextEdit[]>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FormatDocumentOnTypeHandler(ILspSolutionProvider solutionProvider) : base(solutionProvider)
        {
        }

        public override async Task<TextEdit[]> HandleRequestAsync(DocumentOnTypeFormattingParams request, RequestContext context, CancellationToken cancellationToken)
        {
            var edits = new ArrayBuilder<TextEdit>();
            var document = SolutionProvider.GetDocument(request.TextDocument, context.ClientName);
            if (document != null)
            {
                var formattingService = document.Project.LanguageServices.GetRequiredService<IEditorFormattingService>();
                var position = await document.GetPositionFromLinePositionAsync(ProtocolConversions.PositionToLinePosition(request.Position), cancellationToken).ConfigureAwait(false);

                if (string.IsNullOrEmpty(request.Character))
                {
                    return edits.ToArrayAndFree();
                }

                IList<TextChange>? textChanges;
                if (SyntaxFacts.IsNewLine(request.Character[0]))
                {
                    textChanges = await GetFormattingChangesOnReturnAsync(formattingService, document, position, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    textChanges = await GetFormattingChangesAsync(formattingService, document, request.Character[0], position, cancellationToken).ConfigureAwait(false);
                }

                var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                if (textChanges != null)
                {
                    edits.AddRange(textChanges.Select(change => ProtocolConversions.TextChangeToTextEdit(change, text)));
                }
            }

            return edits.ToArrayAndFree();
        }

        protected virtual Task<IList<TextChange>?> GetFormattingChangesOnReturnAsync(IEditorFormattingService formattingService, Document document, int position, CancellationToken cancellationToken)
            => formattingService.GetFormattingChangesOnReturnAsync(document, position, cancellationToken);

        protected virtual Task<IList<TextChange>?> GetFormattingChangesAsync(IEditorFormattingService formattingService, Document document, char typedChar, int position, CancellationToken cancellationToken)
            => formattingService.GetFormattingChangesAsync(document, typedChar, position, cancellationToken);
    }
}
