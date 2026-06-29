// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Copilot;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Language.InlinePrompts;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.DocumentationComments;

[Export(typeof(CopilotGenerateDocumentationCommentManager))]
internal sealed class CopilotGenerateDocumentationCommentManager
{
    private readonly InlinePromptServiceBase? _inlinePromptService;
    private readonly IThreadingContext _threadingContext;
    private readonly IAsynchronousOperationListener _asyncListener;

    // Internal InlinePrompt provider id for the documentation-comment chip.
    private const string GenerateDocumentationProviderName = "Roslyn.GenerateDocumentation";

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CopilotGenerateDocumentationCommentManager(IThreadingContext threadingContext,
        IAsynchronousOperationListenerProvider listenerProvider, [Import(AllowDefault = true)] InlinePromptServiceBase? inlinePromptService = null)
    {
        _inlinePromptService = inlinePromptService;
        _threadingContext = threadingContext;
        _asyncListener = listenerProvider.GetListener(FeatureAttribute.GenerateDocumentation);
    }

    public void TriggerDocumentationCommentProposalGeneration(Document document,
        DocumentationCommentSnippet snippet, ITextSnapshot snapshot, VirtualSnapshotPoint caret, ITextView textView, CancellationToken cancellationToken)
    {
        // No InlinePrompt service means no chip UX, so the typed '///' just leaves the normal doc-comment skeleton.
        if (_inlinePromptService is null)
        {
            return;
        }

        var token = _asyncListener.BeginAsyncOperation(nameof(ShowGenerateDocumentationPromptAsync));
        _ = ShowGenerateDocumentationPromptAsync(document, snippet, snapshot, caret, textView, cancellationToken).CompletesAsyncOperation(token);
    }

    // Show the chip; on accept, generate the Copilot documentation and apply the edits to the buffer.
    private async Task ShowGenerateDocumentationPromptAsync(Document document,
        DocumentationCommentSnippet snippet, ITextSnapshot snapshot, VirtualSnapshotPoint caret, ITextView textView, CancellationToken cancellationToken)
    {
        // Only offer the chip when Copilot is available, the option is enabled, and the file isn't excluded.
        if (await IsGenerateDocumentationAvailableAsync(document, snippet.MemberNode, cancellationToken).ConfigureAwait(false) is null)
        {
            return;
        }

        await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        _inlinePromptService!.Show(
            textView,
            caret,
            onAcceptAsync: dismissToken => GenerateAndApplyDocumentationAsync(document, snippet, snapshot, textView, dismissToken),
            new InlinePromptOptions
            {
                ProviderName = GenerateDocumentationProviderName,
                AcceptDescription = EditorFeaturesResources.Generate_documentation,
                AutoAccept = false,

                // Opt out: the buffer edit and caret move right after showing would otherwise dismiss the chip.
                AutoDismiss = false,
            });
    }

    private async Task GenerateAndApplyDocumentationAsync(Document document,
        DocumentationCommentSnippet snippet, ITextSnapshot snapshot, ITextView textView, CancellationToken cancellationToken)
    {
        // Re-check Copilot availability / file-exclusion at accept time; degrade quietly otherwise.
        var copilotService = await IsGenerateDocumentationAvailableAsync(document, snippet.MemberNode, cancellationToken).ConfigureAwait(false);
        if (copilotService is null)
        {
            return;
        }

        // MemberNode is non-null once IsGenerateDocumentationAvailableAsync succeeds.
        var proposal = CopilotDocumentationCommentGenerator.GetSnippetProposal(snippet.SnippetText, snippet.MemberNode!, snippet.Position, snippet.CaretOffset);
        if (proposal is null)
        {
            return;
        }

        var edits = await CopilotDocumentationCommentGenerator.GenerateEditsAsync(
            proposal, copilotService, snippet.IndentText, cancellationToken).ConfigureAwait(false);
        if (edits.IsEmpty)
        {
            return;
        }

        // Apply the edit fire-and-forget: returning tears the session down (cancelling the token), so the write
        // must run afterward and not flow that token.
        var token = _asyncListener.BeginAsyncOperation(nameof(ApplyDocumentationEditsAsync));
        _ = ApplyDocumentationEditsAsync(textView.TextBuffer, snapshot, edits).CompletesAsyncOperation(token);
    }

    private async Task ApplyDocumentationEditsAsync(ITextBuffer buffer, ITextSnapshot snapshot, ImmutableArray<DocumentationCommentEdit> edits)
    {
        await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync();
        ApplyEdits(buffer, snapshot, edits);
    }

    /// <summary>
    /// Applies the generated documentation edits to <paramref name="buffer"/> in a single transaction. The edit
    /// spans are positions in <paramref name="snapshot"/> (captured when the chip was shown), so they are
    /// translated to the buffer's current snapshot in case it changed before the user accepted.
    /// </summary>
    internal static void ApplyEdits(ITextBuffer buffer, ITextSnapshot snapshot, ImmutableArray<DocumentationCommentEdit> edits)
    {
        var currentSnapshot = buffer.CurrentSnapshot;
        using var bufferEdit = buffer.CreateEdit();
        foreach (var edit in edits)
        {
            var span = new SnapshotSpan(snapshot, edit.SpanToReplace.Start, edit.SpanToReplace.Length)
                .TranslateTo(currentSnapshot, SpanTrackingMode.EdgeInclusive);
            bufferEdit.Replace(span, edit.ReplacementText);
        }

        bufferEdit.Apply();
    }

    private static async Task<ICopilotCodeAnalysisService?> IsCopilotAvailableAsync(Document document, CancellationToken cancellationToken)
    {
        // Bailing out if copilot is not available or the option is not enabled.
        if (document.GetLanguageService<ICopilotOptionsService>() is not { } copilotOptionService ||
            !await copilotOptionService.IsGenerateDocumentationCommentOptionEnabledAsync().ConfigureAwait(false))
        {
            return null;
        }

        if (document.GetLanguageService<ICopilotCodeAnalysisService>() is not { } copilotService ||
                await copilotService.IsAvailableAsync(cancellationToken).ConfigureAwait(false) is false)
        {
            return null;
        }

        return copilotService;
    }

    private static async Task<ICopilotCodeAnalysisService?> IsGenerateDocumentationAvailableAsync(Document document, SyntaxNode? memberNode, CancellationToken cancellationToken)
    {
        var copilotService = await IsCopilotAvailableAsync(document, cancellationToken).ConfigureAwait(false);
        if (copilotService is null)
        {
            return null;
        }

        if (memberNode is null)
        {
            return null;
        }

        // Check to see if the file containing the member being documented has been excluded.
        if (await copilotService.IsFileExcludedAsync(memberNode.SyntaxTree.FilePath, cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        return copilotService;
    }
}
