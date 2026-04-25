// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Razor.Formatting;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.Workspaces.Settings;

namespace Microsoft.CodeAnalysis.Razor.CodeActions;

internal class CSharpCodeActionResolver(IRazorFormattingService razorFormattingService, IClientSettingsManager clientSettingsManager) : ICSharpCodeActionResolver
{
    private readonly IRazorFormattingService _razorFormattingService = razorFormattingService;
    private readonly IClientSettingsManager _clientSettingsManager = clientSettingsManager;

    public string Action => LanguageServerConstants.CodeActions.Default;

    public async Task<CodeAction> ResolveAsync(
        DocumentContext documentContext,
        CodeAction codeAction,
        CancellationToken cancellationToken)
    {
        if (codeAction.Edit?.DocumentChanges is null)
        {
            // Unable to resolve code action with server, return original code action
            return codeAction;
        }

        if (codeAction.Edit.DocumentChanges.Value.Count() != 1)
        {
            // We don't yet support multi-document code actions, return original code action
            return codeAction;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var documentChanged = codeAction.Edit.DocumentChanges.Value.First();
        if (!documentChanged.TryGetFirst(out var textDocumentEdit))
        {
            // Only Text Document Edit changes are supported currently, return original code action
            return codeAction;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var csharpSourceText = await documentContext.GetCSharpSourceTextAsync(cancellationToken).ConfigureAwait(false);
        var csharpTextChanges = textDocumentEdit.Edits.SelectAsArray(e => csharpSourceText.GetTextChange((TextEdit)e));

        // Remaps the text edits from the generated C# to the razor file,
        // as well as applying appropriate formatting.
        var formattedChange = await _razorFormattingService.TryGetCSharpCodeActionEditAsync(
            documentContext,
            csharpTextChanges,
            _clientSettingsManager.GetClientSettings().ToRazorFormattingOptions(),
            cancellationToken).ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();

        var sourceText = await documentContext.GetSourceTextAsync(cancellationToken).ConfigureAwait(false);
        var codeDocumentIdentifier = new OptionalVersionedTextDocumentIdentifier()
        {
            DocumentUri = new(documentContext.Uri)
        };
        codeAction.Edit = new WorkspaceEdit()
        {
            DocumentChanges = new TextDocumentEdit[] {
                new TextDocumentEdit()
                {
                    TextDocument = codeDocumentIdentifier,
                    Edits = formattedChange is { } change ? [sourceText.GetTextEdit(change)] : [],
                }
            }
        };

        return codeAction;
    }
}
