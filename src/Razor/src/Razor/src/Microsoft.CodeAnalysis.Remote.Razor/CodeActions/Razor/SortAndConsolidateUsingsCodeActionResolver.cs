// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Composition;
using Microsoft.CodeAnalysis.Razor.CodeActions;
using Microsoft.CodeAnalysis.Razor.Formatting;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;

namespace Microsoft.CodeAnalysis.Remote.Razor.CodeActions;

[Export(typeof(IRazorCodeActionResolver)), Shared]
internal sealed class SortAndConsolidateUsingsCodeActionResolver : IRazorCodeActionResolver
{
    public string Action => LanguageServerConstants.CodeActions.SortAndConsolidateUsings;

    public async Task<WorkspaceEdit?> ResolveAsync(RemoteDocumentContext documentContext, JsonElement data, RazorFormattingOptions options, CancellationToken cancellationToken)
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
