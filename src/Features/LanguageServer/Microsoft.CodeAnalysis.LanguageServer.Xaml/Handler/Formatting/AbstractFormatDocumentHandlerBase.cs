// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.CodeAnalysis.ExternalAccess.Xaml.Formatting;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Xaml.Handler
{
    internal abstract class AbstractFormatDocumentHandlerBase<RequestType, ResponseType> : ILspServiceDocumentRequestHandler<RequestType, ResponseType>
    {
        public bool MutatesSolutionState => false;
        public bool RequiresLSPSolution => true;

        public abstract TextDocumentIdentifier GetTextDocumentIdentifier(RequestType request);
        public abstract Task<ResponseType> HandleRequestAsync(RequestType request, RequestContext context, CancellationToken cancellationToken);

        protected static async Task<LSP.TextEdit[]> GetTextEditsAsync(LSP.FormattingOptions formattingOptions, RequestContext context, CancellationToken cancellationToken, LSP.Range? range = null)
        {
            using var _ = ArrayBuilder<LSP.TextEdit>.GetInstance(out var edits);

            var document = context.TextDocument;
            var formattingService = document?.Project.Services.GetService<IXamlFormattingService>();

            if (document != null && formattingService != null)
            {
                var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
                TextSpan? textSpan = null;
                if (range != null)
                {
                    textSpan = ProtocolConversions.RangeToTextSpan(range, text);
                }

                var options = new XamlFormattingOptions(formattingOptions.TabSize, formattingOptions.InsertSpaces, formattingOptions.OtherOptions);
                var textChanges = await formattingService.GetFormattingChangesAsync(document, options, textSpan, cancellationToken).ConfigureAwait(false);
                edits.AddRange(textChanges.Select(change => ProtocolConversions.TextChangeToTextEdit(change, text)));
            }

            return edits.ToArray();
        }
    }
}
