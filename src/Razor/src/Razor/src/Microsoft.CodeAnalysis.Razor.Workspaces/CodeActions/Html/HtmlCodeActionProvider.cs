// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.Razor.CodeActions.Models;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Razor.Formatting;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor.Protocol;

namespace Microsoft.CodeAnalysis.Razor.CodeActions;

internal class HtmlCodeActionProvider(IRazorEditService razorEditService) : IHtmlCodeActionProvider
{
    private readonly IRazorEditService _razorEditService = razorEditService;

    public async Task<ImmutableArray<RazorVSInternalCodeAction>> ProvideAsync(
        RazorCodeActionContext context,
        ImmutableArray<RazorVSInternalCodeAction> codeActions,
        CancellationToken cancellationToken)
    {
        using var results = new PooledArrayBuilder<RazorVSInternalCodeAction>(codeActions.Length);
        foreach (var codeAction in codeActions)
        {
            if (codeAction.Edit is not null)
            {
                await MapAndFixHtmlCodeActionEditAsync(_razorEditService, context.DocumentSnapshot, codeAction, cancellationToken).ConfigureAwait(false);

                results.Add(codeAction);
            }
            else
            {
                results.Add(codeAction.WrapResolvableCodeAction(context, language: RazorLanguageKind.Html));
            }
        }

        return results.ToImmutable();
    }

    public static async Task MapAndFixHtmlCodeActionEditAsync(IRazorEditService razorEditService, IDocumentSnapshot documentSnapshot, CodeAction codeAction, CancellationToken cancellationToken)
    {
        Assumes.NotNull(codeAction.Edit);

        await razorEditService.MapWorkspaceEditAsync(documentSnapshot, codeAction.Edit, cancellationToken).ConfigureAwait(false);

        var codeDocument = await documentSnapshot.GetGeneratedOutputAsync(cancellationToken).ConfigureAwait(false);
        var htmlSourceText = codeDocument.GetHtmlSourceText(cancellationToken);

        // NOTE: We iterate over just the TextDocumentEdit objects and modify them in place.
        // We intentionally do NOT create a new WorkspaceEdit here to avoid losing any
        // CreateFile, RenameFile, or DeleteFile operations that may be in DocumentChanges.
        foreach (var edit in codeAction.Edit.EnumerateTextDocumentEdits())
        {
            edit.Edits = FormattingUtilities.FixHtmlTextEdits(htmlSourceText, edit.Edits);
        }
    }
}
