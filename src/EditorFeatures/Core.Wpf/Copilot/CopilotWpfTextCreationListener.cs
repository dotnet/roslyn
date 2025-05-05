// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
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
        if (proposal is not { Edits.Count: 0 })
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

        foreach (var editGroup in proposal.Edits.GroupBy(e => e.Span.Snapshot))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var snapshot = editGroup.Key;
            var document = snapshot.GetOpenDocumentInCurrentContextWithChanges();

            if (document is null)
                continue;

            // Currently we do not support analyzing languges other than C# and VB.  This is because we only want to do
            // this analsis in our OOP process to avoid perf impact on the VS process.  And we don't have OOP for other
            // languages yet.
            if (!document.SupportsSemanticModel)
                continue;

            var normalizedEdits = Normalize(editGroup);
            if (normalizedEdits.IsDefaultOrEmpty)
                continue;

            var changeAnalysisService = document.Project.Solution.Services.GetRequiredService<ICopilotChangeAnalysisService>();
            var analysisResult = await changeAnalysisService.AnalyzeChangeAsync(
                document, normalizedEdits, cancellationToken).ConfigureAwait(false);

            CopilotChangeAnalysisUtilities.LogCopilotChangeAnalysis(
                featureId, accepted, proposalId, analysisResult, cancellationToken).Dispose();
        }
    }

    private static ImmutableArray<TextChange> Normalize(IEnumerable<ProposedEdit> editGroup)
    {
        using var _ = PooledObjects.ArrayBuilder<TextChange>.GetInstance(out var builder);
        foreach (var edit in editGroup)
            builder.Add(new TextChange(edit.Span.Span.ToTextSpan(), edit.ReplacementText));

        // Ensure everything is sorted.
        builder.Sort(static (c1, c2) => c1.Span.Start - c2.Span.Start);

        // Now, go through and make sure no edit overlaps another.
        for (int i = 1, n = builder.Count; i < n; i++)
        {
            var lastEdit = builder[i - 1];
            var currentEdit = builder[i];

            if (lastEdit.Span.OverlapsWith(currentEdit.Span))
                return default;
        }

        // Things look good.  Can process these sorted edits.
        return builder.ToImmutableAndClear();
    }
}
