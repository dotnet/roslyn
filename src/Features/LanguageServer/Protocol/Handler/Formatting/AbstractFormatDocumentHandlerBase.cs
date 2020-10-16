// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    internal abstract class AbstractFormatDocumentHandlerBase<RequestType, ResponseType> : IRequestHandler<RequestType, ResponseType>
    {
        protected async Task<LSP.TextEdit[]> GetTextEditsAsync(RequestContext context, CancellationToken cancellationToken, LSP.Range? range = null)
        {
            var edits = new ArrayBuilder<LSP.TextEdit>();
            var document = context.Document;

            if (document != null)
            {
                var formattingService = document.Project.LanguageServices.GetRequiredService<IEditorFormattingService>();
                var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                TextSpan? textSpan = null;
                if (range != null)
                {
                    textSpan = ProtocolConversions.RangeToTextSpan(range, text);
                }

                var textChanges = await GetFormattingChangesAsync(formattingService, document, textSpan, cancellationToken).ConfigureAwait(false);
                edits.AddRange(textChanges.Select(change => ProtocolConversions.TextChangeToTextEdit(change, text)));
            }

            return edits.ToArrayAndFree();
        }

        protected virtual Task<IList<TextChange>> GetFormattingChangesAsync(IEditorFormattingService formattingService, Document document, TextSpan? textSpan, CancellationToken cancellationToken)
            => formattingService.GetFormattingChangesAsync(document, textSpan, cancellationToken);
        public abstract TextDocumentIdentifier? GetTextDocumentIdentifier(RequestType request);
        public abstract Task<ResponseType> HandleRequestAsync(RequestType request, RequestContext context, CancellationToken cancellationToken);
    }
}
