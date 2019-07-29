// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [Shared]
    [ExportLspMethod(Methods.TextDocumentOnTypeFormattingName)]
    internal class FormatDocumentOnTypeHandler : IRequestHandler<DocumentOnTypeFormattingParams, TextEdit[]>
    {
        public async Task<TextEdit[]> HandleRequestAsync(Solution solution, DocumentOnTypeFormattingParams request, ClientCapabilities clientCapabilities,
            CancellationToken cancellationToken, bool keepThreadContext = false)
        {
            var edits = new ArrayBuilder<TextEdit>();
            var document = solution.GetDocumentFromURI(request.TextDocument.Uri);
            if (document != null)
            {
                var formattingService = document.Project.LanguageServices.GetService<IEditorFormattingService>();
                var position = await document.GetPositionFromLinePositionAsync(ProtocolConversions.PositionToLinePosition(request.Position), cancellationToken).ConfigureAwait(keepThreadContext);

                if (string.IsNullOrEmpty(request.Character))
                {
                    return edits.ToArrayAndFree();
                }

                IList<TextChange> textChanges;
                if (SyntaxFacts.IsNewLine(request.Character[0]))
                {
                    textChanges = await formattingService.GetFormattingChangesOnReturnAsync(document, position, cancellationToken).ConfigureAwait(keepThreadContext);
                }
                else
                {
                    textChanges = await formattingService.GetFormattingChangesAsync(document, request.Character[0], position, cancellationToken).ConfigureAwait(keepThreadContext);
                }

                var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(keepThreadContext);
                if (textChanges != null)
                {
                    edits.AddRange(textChanges.Select(change => ProtocolConversions.TextChangeToTextEdit(change, text)));
                }
            }

            return edits.ToArrayAndFree();
        }
    }
}
