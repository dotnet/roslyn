// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.OrganizeImports;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

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
        if (context.Document is not { } document)
            return null;

        var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        var rangeSpan = (range != null) ? ProtocolConversions.RangeToTextSpan(range, text) : new TextSpan(0, root.FullSpan.Length);
        var formattingSpan = CommonFormattingHelpers.GetFormattingSpan(root, rangeSpan);

        // We should use the options passed in by LSP instead of the document's options.
        var formattingOptions = await ProtocolConversions.GetFormattingOptionsAsync(options, document, cancellationToken).ConfigureAwait(false);
        var services = document.Project.Solution.Services;
        var formattingChanges = Formatter.GetFormattedTextChanges(root, SpecializedCollections.SingletonEnumerable(formattingSpan), services, formattingOptions, cancellationToken);

        // We only organize the imports when formatting the entire document. This means we can stop
        // if we are provided a range or sorting imports is disabled/
        if (range is not null || !globalOptions.GetOption(LspOptionsStorage.LspOrganizeImportsOnFormat, document.Project.Language))
        {
            return [.. formattingChanges.Select(change => ProtocolConversions.TextChangeToTextEdit(change, text))];
        }

        var formattedDocument = document.WithText(text.WithChanges(formattingChanges));

        var organizeImports = formattedDocument.GetRequiredLanguageService<IOrganizeImportsService>();
        var organizeImportsOptions = await formattedDocument.GetOrganizeImportsOptionsAsync(cancellationToken).ConfigureAwait(false);
        var organizedDocument = await organizeImports.OrganizeImportsAsync(formattedDocument, organizeImportsOptions, cancellationToken).ConfigureAwait(false);

        var textChanges = await organizedDocument.GetTextChangesAsync(context.Document).ConfigureAwait(false);
        return [.. textChanges.Select(change => ProtocolConversions.TextChangeToTextEdit(change, text))];
    }

    public abstract LSP.TextDocumentIdentifier GetTextDocumentIdentifier(RequestType request);
    public abstract Task<ResponseType> HandleRequestAsync(RequestType request, RequestContext context, CancellationToken cancellationToken);
}
