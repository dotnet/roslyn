// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Razor.Formatting;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.CodeAnalysis.Razor.Workspaces.Settings;

namespace Microsoft.CodeAnalysis.Razor.CodeActions;

internal abstract class CSharpCodeActionResolver(IRazorFormattingService razorFormattingService, IClientSettingsManager clientSettingsManager, IFilePathService filePathService, ILoggerFactory loggerFactory) : ICSharpCodeActionResolver
{
    private readonly IRazorFormattingService _razorFormattingService = razorFormattingService;
    private readonly IClientSettingsManager _clientSettingsManager = clientSettingsManager;
    private readonly IFilePathService _filePathService = filePathService;
    private readonly ILogger _logger = loggerFactory.GetOrCreateLogger<CSharpCodeActionResolver>();

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

        var snapshot = documentContext.Snapshot;
        var formattingOptions = _clientSettingsManager.GetClientSettings().ToRazorFormattingOptions();

        foreach (var textDocumentEdit in codeAction.Edit.EnumerateTextDocumentEdits())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var generatedDocumentUri = textDocumentEdit.TextDocument.DocumentUri.GetRequiredParsedUri();

            // If Roslyn wants to edit a random .cs file, then who are we to interfere
            if (!_filePathService.IsVirtualCSharpFile(generatedDocumentUri))
            {
                continue;
            }

            var editDocumentContext = await CreateDocumentContextAsync(snapshot, generatedDocumentUri, cancellationToken).ConfigureAwait(false);
            if (editDocumentContext is null)
            {
                _logger.LogWarning($"Could not create document context for {generatedDocumentUri} processing {codeAction.Title}, so leaving original edit in place.");
                continue;
            }

            var csharpSourceText = await editDocumentContext.GetCSharpSourceTextAsync(cancellationToken).ConfigureAwait(false);
            var csharpTextChanges = textDocumentEdit.Edits.SelectAsArray(e => csharpSourceText.GetTextChange((TextEdit)e));

            // Remaps the text edits from the generated C# to the razor file,
            // as well as applying appropriate formatting.
            var formattedChange = await _razorFormattingService.TryGetCSharpCodeActionEditAsync(
                editDocumentContext,
                csharpTextChanges,
                formattingOptions,
                cancellationToken).ConfigureAwait(false);

            if (formattedChange is { } change)
            {
                var sourceText = await editDocumentContext.GetSourceTextAsync(cancellationToken).ConfigureAwait(false);
                textDocumentEdit.TextDocument = new() { DocumentUri = new(editDocumentContext.Uri) };
                textDocumentEdit.Edits = [sourceText.GetTextEdit(change)];
            }
            else
            {
                _logger.LogWarning($"Formatting dropped all C# code edits for {codeAction.Title} in {editDocumentContext.Uri}");
                textDocumentEdit.Edits = [];
            }
        }

        return codeAction;
    }

    protected abstract Task<DocumentContext?> CreateDocumentContextAsync(IDocumentSnapshot originDocumentSnapshot, Uri generatedDocumentUri, CancellationToken cancellationToken);
}
