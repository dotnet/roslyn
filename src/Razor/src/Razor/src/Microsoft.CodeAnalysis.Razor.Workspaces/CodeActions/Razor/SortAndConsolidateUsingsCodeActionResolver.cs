// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Razor.Formatting;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor.Protocol;

namespace Microsoft.CodeAnalysis.Razor.CodeActions.Razor;

internal class SortAndConsolidateUsingsCodeActionResolver : IRazorCodeActionResolver
{
    public string Action => LanguageServerConstants.CodeActions.SortAndConsolidateUsings;

    public async Task<WorkspaceEdit?> ResolveAsync(DocumentContext documentContext, JsonElement data, RazorFormattingOptions options, CancellationToken cancellationToken)
    {
        var codeDocument = await documentContext.GetCodeDocumentAsync(cancellationToken).ConfigureAwait(false);

        if (!UsingDirectiveHelper.NeedsSortOrConsolidate(codeDocument))
        {
            return null;
        }

        var edits = UsingDirectiveHelper.GetSortAndConsolidateEdits(codeDocument);

        var documentChanges = new TextDocumentEdit[]
        {
            new TextDocumentEdit
            {
                TextDocument = new OptionalVersionedTextDocumentIdentifier { DocumentUri = new(documentContext.Uri) },
                Edits = [.. edits],
            }
        };

        return new WorkspaceEdit
        {
            DocumentChanges = documentChanges,
        };
    }
}
