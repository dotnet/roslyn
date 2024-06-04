// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    internal abstract class AbstractFormatDocumentHandlerBase<RequestType, ResponseType> : ILspServiceDocumentRequestHandler<RequestType, ResponseType>
    {
        public bool MutatesSolutionState => false;
        public bool RequiresLSPSolution => true;

        protected static async Task<LSP.TextEdit[]?> GetTextEditsAsync(
            RequestContext context,
            LSP.FormattingOptions options,
            IGlobalOptionService globalOptions,
            CancellationToken cancellationToken,
            LSP.Range? range = null)
        {
            var document = context.Document;
            if (document is null)
                return null;

            var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var rangeSpan = (range != null) ? ProtocolConversions.RangeToTextSpan(range, text) : new TextSpan(0, root.FullSpan.Length);
            var formattingSpan = CommonFormattingHelpers.GetFormattingSpan(root, rangeSpan);

            // We should use the options passed in by LSP instead of the document's options.
            var formattingOptions = await ProtocolConversions.GetFormattingOptionsAsync(options, document, globalOptions, cancellationToken).ConfigureAwait(false);

            var services = document.Project.Solution.Services;
            var textChanges = Formatter.GetFormattedTextChanges(root, [formattingSpan], services, formattingOptions, rules: default, cancellationToken);

            var edits = new ArrayBuilder<LSP.TextEdit>();
            edits.AddRange(textChanges.Select(change => ProtocolConversions.TextChangeToTextEdit(change, text)));
            return edits.ToArrayAndFree();
        }

        public abstract LSP.TextDocumentIdentifier GetTextDocumentIdentifier(RequestType request);
        public abstract Task<ResponseType> HandleRequestAsync(RequestType request, RequestContext context, CancellationToken cancellationToken);
    }
}
