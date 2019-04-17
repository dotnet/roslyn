// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [Shared]
    [ExportLspMethod(Methods.TextDocumentFormattingName)]
    internal class FormatDocumentHandler : IRequestHandler<DocumentFormattingParams, TextEdit[]>
    {
        public async Task<TextEdit[]> HandleRequestAsync(Solution solution, DocumentFormattingParams request, ClientCapabilities clientCapabilities, CancellationToken cancellationToken)
        {
            var edits = new List<TextEdit>();
            var document = solution.GetDocumentFromURI(request.TextDocument.Uri);
            if (document != null)
            {
                var formattingService = document.Project.LanguageServices.GetService<IEditorFormattingService>();
                var textChanges = await formattingService.GetFormattingChangesAsync(document, null, cancellationToken).ConfigureAwait(false);
                var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                edits.AddRange(textChanges.Select(change => ProtocolConversions.TextChangeToTextEdit(change, text)));
            }

            return edits.ToArray();
        }
    }
}
