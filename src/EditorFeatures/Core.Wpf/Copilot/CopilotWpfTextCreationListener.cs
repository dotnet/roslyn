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
    private readonly IThreadingContext _threadingContext;
    private readonly Lazy<SuggestionServiceBase> _suggestionServiceBase;
    private readonly IAsynchronousOperationListener _listener;

    private readonly AsyncBatchingWorkQueue<SuggestionAcceptedEventArgs> _workQueue;

    private int _started;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CopilotWpfTextViewCreationListener(
        IThreadingContext threadingContext,
        Lazy<SuggestionServiceBase> suggestionServiceBase,
        IAsynchronousOperationListenerProvider listenerProvider)
    {
        _threadingContext = threadingContext;
        _suggestionServiceBase = suggestionServiceBase;
        _listener = listenerProvider.GetListener(FeatureAttribute.CopilotChangeAnalysis);
        _workQueue = new AsyncBatchingWorkQueue<SuggestionAcceptedEventArgs>(
            DelayTimeSpan.Idle,
            ProcessEventsAsync,
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
                suggestionService.SuggestionAccepted += OnSuggestionAccepted;
            }).CompletesAsyncOperation(token);
        }
    }

    private void OnSuggestionAccepted(object sender, SuggestionAcceptedEventArgs e)
    {
        if (e.FinalProposal.Edits.Count == 0)
            return;

        _workQueue.AddWork(e);
    }

    private async ValueTask ProcessEventsAsync(
        ImmutableSegmentedList<SuggestionAcceptedEventArgs> list, CancellationToken cancellationToken)
    {
        foreach (var eventArgs in list)
            await ProcessEventAsync(eventArgs, cancellationToken).ConfigureAwait(false);
    }

    private static async ValueTask ProcessEventAsync(
        SuggestionAcceptedEventArgs eventArgs, CancellationToken cancellationToken)
    {
        var proposal = eventArgs.FinalProposal;
        var proposalId = proposal.ProposalId;

        foreach (var editGroup in proposal.Edits.GroupBy(e => e.Span.Snapshot))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var snapshot = editGroup.Key;
            var document = snapshot.GetOpenDocumentInCurrentContextWithChanges();

            if (document is null)
                continue;

            var normalizedEdits = Normalize(editGroup);
            if (normalizedEdits.IsDefaultOrEmpty)
                continue;

            var changeAnalysisService = document.Project.Solution.Services.GetRequiredService<ICopilotChangeAnalysisService>();
            await changeAnalysisService.AnalyzeChangeAsync(
                document, normalizedEdits, proposalId, cancellationToken).ConfigureAwait(false);
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
