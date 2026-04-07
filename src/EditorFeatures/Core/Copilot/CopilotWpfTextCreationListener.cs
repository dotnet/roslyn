// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Threading;
using Microsoft.VisualStudio.Language.Proposals;
using Microsoft.VisualStudio.Language.Suggestions;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Copilot;

[Export(typeof(IWpfTextViewCreationListener))]
[ContentType(ContentTypeNames.RoslynContentType)]
[TextViewRole(PredefinedTextViewRoles.Document)]
internal sealed class CopilotWpfTextViewCreationListener : IWpfTextViewCreationListener
{
    private readonly IGlobalOptionService _globalOptions;
    private readonly IThreadingContext _threadingContext;
    private readonly Lazy<SuggestionServiceBase> _suggestionServiceBase;
    private readonly IAsynchronousOperationListener _listener;

    private readonly AsyncBatchingWorkQueue<(bool accepted, ProposalBase proposal)> _completionWorkQueue;

    private int _started;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CopilotWpfTextViewCreationListener(
        IGlobalOptionService globalOptions,
        IThreadingContext threadingContext,
        Lazy<SuggestionServiceBase> suggestionServiceBase,
        IAsynchronousOperationListenerProvider listenerProvider)
    {
        _globalOptions = globalOptions;
        _threadingContext = threadingContext;
        _suggestionServiceBase = suggestionServiceBase;
        _listener = listenerProvider.GetListener(FeatureAttribute.CopilotChangeAnalysis);

        _completionWorkQueue = new AsyncBatchingWorkQueue<(bool accepted, ProposalBase proposal)>(
            DelayTimeSpan.Idle,
            ProcessCompletionEventsAsync,
            _listener,
            _threadingContext.DisposalToken);
    }

    public void TextViewCreated(IWpfTextView textView)
    {
        // On the first roslyn text view created, kick off work to hydrate the suggestion service and register to events
        // from it.
        if (Interlocked.CompareExchange(ref _started, 1, 0) == 0)
        {
            var token = _listener.BeginAsyncOperation(nameof(TextViewCreated));
            Task.Run(() =>
            {
                var suggestionService = _suggestionServiceBase.Value;
                suggestionService.SuggestionAccepted += OnCompletionSuggestionAccepted;
                suggestionService.SuggestionDismissed += OnCompletionSuggestionDismissed;
            }).CompletesAsyncOperation(token);
        }
    }

    private void OnCompletionSuggestionAccepted(object sender, SuggestionAcceptedEventArgs e)
        => OnCompletionSuggestionEvent(accepted: true, e.FinalProposal);

    private void OnCompletionSuggestionDismissed(object sender, SuggestionDismissedEventArgs e)
        => OnCompletionSuggestionEvent(accepted: false, e.FinalProposal);

    private void OnCompletionSuggestionEvent(bool accepted, ProposalBase? proposal)
    {
        if (proposal is not { Edits.Count: > 0 })
            return;

        _completionWorkQueue.AddWork((accepted, proposal));
    }

    private async ValueTask ProcessCompletionEventsAsync(
        ImmutableSegmentedList<(bool accepted, ProposalBase proposal)> list, CancellationToken cancellationToken)
    {
        // Ignore if analyzing changes is disabled for this user.
        if (!_globalOptions.GetOption(CopilotOptions.AnalyzeCopilotChanges))
            return;

        foreach (var (accepted, proposal) in list)
            await ProcessCompletionEventAsync(accepted, proposal, cancellationToken).ConfigureAwait(false);
    }

    private static async ValueTask ProcessCompletionEventAsync(
        bool accepted, ProposalBase proposal, CancellationToken cancellationToken)
    {
        const string featureId = "Completion";
        var proposalId = proposal.ProposalId;

        var (solution, _) = CopilotEditorUtilities.TryGetAffectedSolution(proposal);
        if (solution is null)
            return;

        // We're about to potentially make multiple calls to oop here.  So keep a session alive to avoid
        // resyncing any data unnecessary.
        using var _1 = await RemoteKeepAliveSession.CreateAsync(solution, cancellationToken).ConfigureAwait(false);

        foreach (var editGroup in proposal.Edits.GroupBy(e => e.Span.Snapshot))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var snapshot = editGroup.Key;
            var document = snapshot.GetOpenDocumentInCurrentContextWithChanges();

            if (document is null)
                continue;

            var normalizedEdits = CopilotEditorUtilities.TryGetNormalizedTextChanges(editGroup);

            await CopilotChangeAnalysisUtilities.AnalyzeCopilotChangeAsync(
                document, accepted, featureId, proposalId, normalizedEdits, cancellationToken).ConfigureAwait(false);
        }
    }
}
