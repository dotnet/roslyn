// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Composition;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.Razor.CodeActions;
using Microsoft.CodeAnalysis.Razor.CodeActions.Models;
using Microsoft.CodeAnalysis.Razor.Formatting;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;

namespace Microsoft.CodeAnalysis.Remote.Razor.CodeActions;

[Export(typeof(IRazorCodeActionResolver)), Shared]
internal sealed class RemoveUnnecessaryDirectivesCodeActionResolver : IRazorCodeActionResolver
{
    public string Action => LanguageServerConstants.CodeActions.RemoveUnnecessaryDirectives;

    public async Task<WorkspaceEdit?> ResolveAsync(RemoteDocumentSnapshot documentSnapshot, JsonElement data, RazorFormattingOptions options, CancellationToken cancellationToken)
    {
        var actionParams = data.Deserialize<RemoveUnnecessaryDirectivesCodeActionParams>();
        if (actionParams is null)
        {
            return null;
        }

        var sourceText = await documentSnapshot.GetTextAsync(cancellationToken).ConfigureAwait(false);

        using var edits = new PooledArrayBuilder<Roslyn.LanguageServer.Protocol.TextDocumentEdit._EditsItem>();
        foreach (var directiveSpan in actionParams.UnusedDirectiveSpans)
        {
            edits.Add(UsingDirectiveHelper.GetRemoveDirectiveEdit(sourceText, directiveSpan.ToTextSpan()));
        }

        return new WorkspaceEdit
        {
            DocumentChanges = new WorkspaceEdit._DocumentChangesItem[]
            {
                new TextDocumentEdit
                {
                    TextDocument = new OptionalVersionedTextDocumentIdentifier() { Uri = documentSnapshot.Uri },
                    Edits = edits.ToArrayAndClear(),
                }
            }
        };
    }
}
