// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    internal abstract class AbstractFormatDocumentHandlerBase<RequestType, ResponseType> : AbstractStatelessRequestHandler<RequestType, ResponseType>
    {
        public override bool MutatesSolutionState => false;
        public override bool RequiresLSPSolution => true;

        protected async Task<LSP.TextEdit[]?> GetTextEditsAsync(
            RequestContext context,
            LSP.FormattingOptions options,
            CancellationToken cancellationToken,
            LSP.Range? range = null)
        {
            var document = context.Document;
            if (document == null)
                return null;

            var edits = new ArrayBuilder<LSP.TextEdit>();

            var formattingService = document.Project.LanguageServices.GetRequiredService<IFormattingInteractionService>();
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            TextSpan? textSpan = null;
            if (range != null)
            {
                textSpan = ProtocolConversions.RangeToTextSpan(range, text);
            }

            // We should use the options passed in by LSP instead of the document's options.
            var documentOptions = await ProtocolConversions.FormattingOptionsToDocumentOptionsAsync(
                options, document, cancellationToken).ConfigureAwait(false);

            var textChanges = await GetFormattingChangesAsync(formattingService, document, textSpan, documentOptions, cancellationToken).ConfigureAwait(false);
            edits.AddRange(textChanges.Select(change => ProtocolConversions.TextChangeToTextEdit(change, text)));

            return edits.ToArrayAndFree();
        }

        protected virtual Task<ImmutableArray<TextChange>> GetFormattingChangesAsync(
            IFormattingInteractionService formattingService,
            Document document,
            TextSpan? textSpan,
            DocumentOptionSet documentOptions,
            CancellationToken cancellationToken)
            => formattingService.GetFormattingChangesAsync(document, textSpan, documentOptions, cancellationToken);
    }
}
