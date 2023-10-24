// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.OrganizeImports;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

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
            LSP.Range? range,
            CancellationToken cancellationToken)
        {
            if (context.Document is not { } document)
                return null;

            IList<TextChange>? textChanges = null;
            if (range is null && globalOptions.GetOption(LspOptionsStorage.LspFormattingSortImports, document.Project.Language))
            {
                var organizeImports = document.GetRequiredLanguageService<IOrganizeImportsService>();
                var organizeImportsOptions = await document.GetOrganizeImportsOptionsAsync(globalOptions, cancellationToken).ConfigureAwait(false);
                var organizedDocument = await organizeImports.OrganizeImportsAsync(document, organizeImportsOptions, cancellationToken).ConfigureAwait(false);
                textChanges = (await organizedDocument.GetTextChangesAsync(document, cancellationToken).ConfigureAwait(false)).ToList();
                document = organizedDocument;
            }

            var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var rangeSpan = (range != null) ? ProtocolConversions.RangeToTextSpan(range, text) : new TextSpan(0, root.FullSpan.Length);
            var formattingSpan = CommonFormattingHelpers.GetFormattingSpan(root, rangeSpan);

            // We should use the options passed in by LSP instead of the document's options.
            var formattingOptions = await ProtocolConversions.GetFormattingOptionsAsync(options, document, globalOptions, cancellationToken).ConfigureAwait(false);

            var services = document.Project.Solution.Services;
            var formattingTextChanges = Formatter.GetFormattedTextChanges(root, SpecializedCollections.SingletonEnumerable(formattingSpan), services, formattingOptions, rules: null, cancellationToken);
            if (textChanges is { Count: > 0 })
            {
                if (formattingTextChanges.Count > 0)
                {
                    // Translate the formatting changes as a follow-up operation to the organization operation
                    var formattedDocument = document.WithText(text.WithChanges(formattingTextChanges));
                    textChanges = (await formattedDocument.GetTextChangesAsync(context.Document, cancellationToken).ConfigureAwait(false)).ToList();
                }
                else
                {
                    // The only changes come from organizing imports
                }
            }
            else
            {
                textChanges = formattingTextChanges;
            }

            var edits = new ArrayBuilder<LSP.TextEdit>();
            var originalText = await context.Document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
            edits.AddRange(textChanges.Select(change => ProtocolConversions.TextChangeToTextEdit(change, originalText)));
            return edits.ToArrayAndFree();
        }

        public abstract LSP.TextDocumentIdentifier GetTextDocumentIdentifier(RequestType request);
        public abstract Task<ResponseType> HandleRequestAsync(RequestType request, RequestContext context, CancellationToken cancellationToken);
    }
}
