// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Copilot;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Language.Suggestions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.DocumentationComments;

[Export(typeof(CopilotGenerateDocumentationCommentManager))]
internal sealed class CopilotGenerateDocumentationCommentManager
{
    private readonly SuggestionServiceBase? _suggestionServiceBase;
    private readonly IThreadingContext _threadingContext;
    private readonly IAsynchronousOperationListener _asyncListener;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CopilotGenerateDocumentationCommentManager([Import(AllowDefault = true)] SuggestionServiceBase? suggestionServiceBase, IThreadingContext threadingContext,
        IAsynchronousOperationListenerProvider listenerProvider)
    {
        _suggestionServiceBase = suggestionServiceBase;
        _threadingContext = threadingContext;
        _asyncListener = listenerProvider.GetListener(FeatureAttribute.GenerateDocumentation);
    }

    public void StartSuggestionSession(ITextBuffer subjectBuffer, ITextView textView, CancellationToken cancellationToken)
    {
        if (_suggestionServiceBase is null)
        {
            return;
        }

        var token = _asyncListener.BeginAsyncOperation(nameof(StartSuggestionSessionAsync));
        _ = StartSuggestionSessionAsync(subjectBuffer, textView, cancellationToken).CompletesAsyncOperation(token);
    }

    private static async Task StartSuggestionSessionAsync(ITextBuffer subjectBuffer, ITextView textView, CancellationToken cancellationToken)
    {
        var document = subjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
        if (document is null)
        {
            return;
        }

        if (await IsCopilotAvailableAsync(document, cancellationToken).ConfigureAwait(false) is null)
        {
            return;
        }

        if (textView.Properties.TryGetProperty<CopilotGenerateDocumentationCommentProvider>(
            typeof(CopilotGenerateDocumentationCommentProvider), out var existingProvider))
        {
            // Start the suggestion session early to claim exclusive control
            await existingProvider.StartSuggestionSessionAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public void TriggerDocumentationCommentProposalGeneration(Document document,
        DocumentationCommentSnippet snippet, ITextSnapshot snapshot, VirtualSnapshotPoint caret, ITextView textView, CancellationToken cancellationToken)
    {
        if (_suggestionServiceBase is null)
        {
            return;
        }

        var token = _asyncListener.BeginAsyncOperation(nameof(GenerateDocumentationCommentProposalsAsync));
        _ = GenerateDocumentationCommentProposalsAsync(document, snippet, snapshot, caret, textView, cancellationToken).CompletesAsyncOperation(token);
    }

    private async Task GenerateDocumentationCommentProposalsAsync(Document document, DocumentationCommentSnippet snippet, ITextSnapshot snapshot, VirtualSnapshotPoint caret, ITextView textView, CancellationToken cancellationToken)
    {
        var generateDocumentationCommentProvider = await CreateProviderAsync(document, textView, snippet.MemberNode, cancellationToken).ConfigureAwait(false);
        if (generateDocumentationCommentProvider is not null)
        {
            await generateDocumentationCommentProvider.GenerateDocumentationProposalAsync(snippet, snapshot, caret, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<CopilotGenerateDocumentationCommentProvider?> CreateProviderAsync(Document document, ITextView textView, SyntaxNode? memberNode, CancellationToken cancellationToken)
    {
        var copilotService = await IsGenerateDocumentationAvailableAsync(document, memberNode, cancellationToken).ConfigureAwait(false);

        if (copilotService is null)
        {
            return null;
        }

        var provider = textView.Properties.GetOrCreateSingletonProperty(typeof(CopilotGenerateDocumentationCommentProvider),
            () => new CopilotGenerateDocumentationCommentProvider(_threadingContext, copilotService));

        await provider.InitializeAsync(textView, _suggestionServiceBase!, cancellationToken).ConfigureAwait(false);

        return provider;
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
