// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.Razor.CodeActions.Models;
using Microsoft.CodeAnalysis.Razor.Formatting;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor.Protocol;

namespace Microsoft.CodeAnalysis.Razor.CodeActions;

internal class RemoveUnnecessaryDirectivesCodeActionResolver : IRazorCodeActionResolver
{
    public string Action => LanguageServerConstants.CodeActions.RemoveUnnecessaryDirectives;

    public async Task<WorkspaceEdit?> ResolveAsync(DocumentContext documentContext, JsonElement data, RazorFormattingOptions options, CancellationToken cancellationToken)
    {
        var actionParams = data.Deserialize<RemoveUnnecessaryDirectivesCodeActionParams>();
        if (actionParams is null)
        {
            return null;
        }

        var sourceText = await documentContext.GetSourceTextAsync(cancellationToken).ConfigureAwait(false);

        using var edits = new PooledArrayBuilder<SumType<TextEdit, AnnotatedTextEdit>>();
        foreach (var directiveSpan in actionParams.UnusedDirectiveSpans)
        {
            edits.Add(UsingDirectiveHelper.GetRemoveDirectiveEdit(sourceText, directiveSpan.ToTextSpan()));
        }

        return new WorkspaceEdit
        {
            DocumentChanges = new TextDocumentEdit[]
            {
                new TextDocumentEdit
                {
                    TextDocument = new OptionalVersionedTextDocumentIdentifier() { DocumentUri = new(documentContext.Uri) },
                    Edits = edits.ToArrayAndClear(),
                }
            }
        };
    }
}
