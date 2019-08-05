// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    internal class FormatDocumentHandlerBase
    {
        protected async Task<LSP.TextEdit[]> GetTextEdits(Solution solution, Uri documentUri, bool keepThreadContext, CancellationToken cancellationToken, LSP.Range range = null)
        {
            var edits = new ArrayBuilder<LSP.TextEdit>();
            var document = solution.GetDocumentFromURI(documentUri);

            if (document != null)
            {
                var formattingService = document.Project.LanguageServices.GetService<IEditorFormattingService>();
                var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(keepThreadContext);
                TextSpan? textSpan = null;
                if (range != null)
                {
                    textSpan = ProtocolConversions.RangeToTextSpan(range, text);
                }

                var textChanges = await formattingService.GetFormattingChangesAsync(document, textSpan, cancellationToken).ConfigureAwait(keepThreadContext);
                edits.AddRange(textChanges.Select(change => ProtocolConversions.TextChangeToTextEdit(change, text)));
            }

            return edits.ToArrayAndFree();
        }
    }
}
