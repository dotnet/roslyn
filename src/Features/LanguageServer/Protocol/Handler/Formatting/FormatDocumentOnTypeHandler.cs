// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [Shared]
    [ExportLspMethod(Methods.TextDocumentOnTypeFormattingName)]
    internal class FormatDocumentOnTypeHandler : IRequestHandler<DocumentOnTypeFormattingParams, TextEdit[]>
    {
        public async Task<TextEdit[]> HandleRequestAsync(Solution solution, DocumentOnTypeFormattingParams request, ClientCapabilities clientCapabilities, CancellationToken cancellationToken)
        {
            var edits = new List<TextEdit>();
            var document = solution.GetDocumentFromURI(request.TextDocument.Uri);
            if (document != null)
            {
                var formattingService = document.Project.LanguageServices.GetService<IEditorFormattingService>();
                var position = await document.GetPositionFromLinePositionAsync(ProtocolConversions.PositionToLinePosition(request.Position), cancellationToken).ConfigureAwait(false);

                IList<TextChange> textChanges;
                // Formatting on new lines must be handled differently.
                if (request.Character == "\n")
                {
                    textChanges = await formattingService.GetFormattingChangesOnReturnAsync(document, position, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    textChanges = await formattingService.GetFormattingChangesAsync(document, request.Character[0], position, cancellationToken).ConfigureAwait(false);
                }

                var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                if (textChanges != null)
                {
                    edits.AddRange(textChanges.Select(change => ProtocolConversions.TextChangeToTextEdit(change, text)));
                }
            }

            return edits.ToArray();
        }
    }
}
