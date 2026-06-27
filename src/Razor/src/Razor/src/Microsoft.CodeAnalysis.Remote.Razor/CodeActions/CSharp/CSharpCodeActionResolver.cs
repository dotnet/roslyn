// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.CodeActions;
using Microsoft.CodeAnalysis.Razor.Formatting;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.CodeAnalysis.Razor.Workspaces.Settings;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;

namespace Microsoft.CodeAnalysis.Remote.Razor.CodeActions;

[Export(typeof(ICSharpCodeActionResolver)), Shared]
[method: ImportingConstructor]
internal sealed class CSharpCodeActionResolver(
    IRazorFormattingService razorFormattingService,
    IClientSettingsManager clientSettingsManager,
    IFilePathService filePathService,
    RemoteSnapshotManager snapshotManager,
    ILoggerFactory loggerFactory) : ICSharpCodeActionResolver
{
    private readonly IRazorFormattingService _razorFormattingService = razorFormattingService;
    private readonly IClientSettingsManager _clientSettingsManager = clientSettingsManager;
    private readonly IFilePathService _filePathService = filePathService;
    private readonly RemoteSnapshotManager _snapshotManager = snapshotManager;
    private readonly ILogger _logger = loggerFactory.GetOrCreateLogger<CSharpCodeActionResolver>();

    public string Action => LanguageServerConstants.CodeActions.Default;

    public async Task<CodeAction> ResolveAsync(
        RemoteDocumentContext documentContext,
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

            var generatedDocumentUri = textDocumentEdit.TextDocument.DocumentUri.GetRequiredSystemUri();

            // If Roslyn wants to edit a random .cs file, then who are we to interfere
            if (!_filePathService.IsVirtualCSharpFile(generatedDocumentUri))
            {
                continue;
            }

            // We know this is a virtual C# file, but we have to jump through a couple of hoops to make sure we get the right info
            var solution = snapshot.TextDocument.Project.Solution;

            var razorDocument = await _snapshotManager.TryGetRazorDocumentAsync(solution, generatedDocumentUri, cancellationToken).ConfigureAwait(false);
            if (razorDocument is null)
            {
                _logger.LogWarning($"Could not get razor document for {generatedDocumentUri} processing {codeAction.Title}, so leaving original edit in place.");
                continue;
            }

            var razorSnapshot = _snapshotManager.GetSnapshot(razorDocument);
            var codeDocument = await razorSnapshot.GetGeneratedOutputAsync(cancellationToken).ConfigureAwait(false);

            if (!solution.TryGetSourceGeneratedDocumentIdentity(generatedDocumentUri, out var identity))
            {
                _logger.LogWarning($"Could not get generated document identity for {generatedDocumentUri} processing {codeAction.Title}, so leaving original edit in place.");
                continue;
            }

            var csharpDocument = codeDocument.GetCSharpDocumentForHintName(identity.HintName);
            var csharpSourceText = csharpDocument.Text;
            var csharpTextChanges = textDocumentEdit.Edits.SelectAsArray(e => csharpSourceText.GetTextChange((TextEdit)e));

            // Remaps the text edits from the generated C# to the razor file,
            // as well as applying appropriate formatting.
            var formattedChange = await _razorFormattingService.TryGetCSharpCodeActionEditAsync(
                razorSnapshot,
                csharpTextChanges,
                declarationDocument: csharpDocument.IsDeclarationDocument,
                formattingOptions,
                cancellationToken).ConfigureAwait(false);

            if (formattedChange is { } change)
            {
                textDocumentEdit.TextDocument = new() { DocumentUri = razorDocument.GetURI() };
                textDocumentEdit.Edits = [codeDocument.Source.Text.GetTextEdit(change)];
            }
            else
            {
                _logger.LogWarning($"Formatting dropped all C# code edits for {codeAction.Title} in {razorDocument.GetURI()}");
                textDocumentEdit.Edits = [];
            }
        }

        return codeAction;
    }
}
