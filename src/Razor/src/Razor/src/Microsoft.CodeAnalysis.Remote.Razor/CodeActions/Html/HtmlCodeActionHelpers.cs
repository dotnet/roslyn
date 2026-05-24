// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Razor.Formatting;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;

namespace Microsoft.CodeAnalysis.Razor.CodeActions;

internal static class HtmlCodeActionHelpers
{
    internal static async Task MapAndFixHtmlCodeActionEditAsync(IRazorEditService razorEditService, RemoteDocumentSnapshot documentSnapshot, CodeAction codeAction, CancellationToken cancellationToken)
    {
        Assumes.NotNull(codeAction.Edit);

        await razorEditService.MapWorkspaceEditAsync(documentSnapshot, codeAction.Edit, cancellationToken).ConfigureAwait(false);

        var codeDocument = await documentSnapshot.GetGeneratedOutputAsync(cancellationToken).ConfigureAwait(false);
        var htmlSourceText = codeDocument.GetHtmlSourceText(cancellationToken);

        foreach (var edit in codeAction.Edit.EnumerateTextDocumentEdits())
        {
            edit.Edits = FormattingUtilities.FixHtmlTextEdits(htmlSourceText, edit.Edits);
        }
    }
}
