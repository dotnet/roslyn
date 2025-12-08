// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Language.Suggestions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.CodeAnalysis.Editor.CSharp.EventHookup;

[Export]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed partial class EventHookupSessionManager(
    IThreadingContext threadingContext,
    IToolTipService toolTipService,
    Lazy<SuggestionServiceBase> suggestionServiceBase)
{
    public readonly IThreadingContext ThreadingContext = threadingContext;
    private readonly IToolTipService _toolTipService = toolTipService;
    internal readonly Lazy<SuggestionServiceBase> SuggestionServiceBase = suggestionServiceBase;

    private IToolTipPresenter _toolTipPresenter;
    private VisualStudio.Threading.IAsyncDisposable _suggestionBlocker;

    internal EventHookupSession CurrentSession
    {
        get
        {
            ThreadingContext.ThrowIfNotOnUIThread();
            return field;
        }

        set
        {
            ThreadingContext.ThrowIfNotOnUIThread();
            field?.CancelBackgroundTasks();
            field = value;
        }
    }

    // For test purposes only!
    internal ClassifiedTextElement[] TEST_MostRecentToolTipContent { get; set; }

    public async Task EventHookupFoundInSessionAsync(
        EventHookupSession analyzedSession, string eventName, CancellationToken cancellationToken)
    {
        await this.ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(alwaysYield: true, cancellationToken);
        if (cancellationToken.IsCancellationRequested)
            return;

        // Now that we've switched to the UI thread, the rest of the work is not cancellable.  We're about to be making
        // mutations, and we don't want to stop somewhere in the middle of that.
        cancellationToken = default;
        var caretPoint = analyzedSession.TextView.GetCaretPoint(analyzedSession.SubjectBuffer);

        // only generate tooltip if it is not already shown (_toolTipPresenter == null)
        // Ensure the analyzed session matches the current session and that the caret is still
        // in the session's tracking span.
        if (_toolTipPresenter == null &&
            CurrentSession == analyzedSession &&
            caretPoint.HasValue &&
            IsCaretWithinSpanOrAtEnd(analyzedSession.TrackingSpan, analyzedSession.SubjectBuffer.CurrentSnapshot, caretPoint.Value))
        {
            // Create a tooltip presenter that stays alive, even when the user types, without tracking the mouse.
            _toolTipPresenter = _toolTipService.CreatePresenter(analyzedSession.TextView,
                new ToolTipParameters(trackMouse: false, ignoreBufferChange: true));

            // tooltips text is: Program_MyEvents;      (Press TAB to insert)
            // GetEventNameTask() gets back the event name, only needs to add a semicolon after it.
            var textRuns = new[]
            {
                new ClassifiedTextRun(ClassificationTypeNames.MethodName, eventName, ClassifiedTextRunStyle.UseClassificationFont),
                new ClassifiedTextRun(ClassificationTypeNames.Punctuation, ";", ClassifiedTextRunStyle.UseClassificationFont),
                new ClassifiedTextRun(ClassificationTypeNames.Text, CSharpEditorResources.Press_TAB_to_insert),
            };
            var content = new[] { new ClassifiedTextElement(textRuns) };

            _toolTipPresenter.StartOrUpdate(analyzedSession.TrackingSpan, content);

            // For test purposes only!
            TEST_MostRecentToolTipContent = content;

            // Watch all text buffer changes & caret moves while this event hookup session is active
            analyzedSession.TextView.TextSnapshot.TextBuffer.Changed += TextBuffer_Changed;
            CurrentSession.Dismissed += () => { analyzedSession.TextView.TextSnapshot.TextBuffer.Changed -= TextBuffer_Changed; };

            analyzedSession.TextView.Caret.PositionChanged += Caret_PositionChanged;
            CurrentSession.Dismissed += () => { analyzedSession.TextView.Caret.PositionChanged -= Caret_PositionChanged; };

            // Dismiss and suppress gray text proposals for the duration of the event hookup session. Note we pass
            // CancellationToken.None here as we don't actually want to cancel this operation, since we've already
            // made UI changes/hookup that we now have to go through.  We are technically safe, as we've cleared
            // out cancellationToken above, but this is an extra level safety.
            //
            // Also, 'ConfigureAwait(true)' on everything here as we want to stay on the UI thread.
            _suggestionBlocker?.DisposeAsync().ConfigureAwait(true);
            _suggestionBlocker = await SuggestionServiceBase.Value.DismissAndBlockProposalsAsync(
                analyzedSession.TextView, ReasonForDismiss.DismissedAfterBufferChange, CancellationToken.None).ConfigureAwait(true);
        }
    }

    private static bool IsCaretWithinSpanOrAtEnd(ITrackingSpan trackingSpan, ITextSnapshot textSnapshot, SnapshotPoint caretPoint)
    {
        var snapshotSpan = trackingSpan.GetSpan(textSnapshot);

        // If the caret is within the span, then we want to show the tooltip
        if (snapshotSpan.Contains(caretPoint))
        {
            return true;
        }

        // Otherwise if the span is empty, and at the end of the file, and the caret
        // is also at the end of the file, then show the tooltip.
        if (snapshotSpan.IsEmpty &&
            snapshotSpan.Start.Position == caretPoint.Position &&
            caretPoint.Position == textSnapshot.Length)
        {
            return true;
        }

        return false;
    }

    internal void BeginSession(
        EventHookupCommandHandler eventHookupCommandHandler,
        ITextView textView,
        ITextBuffer subjectBuffer,
        int position,
        Document document,
        IAsynchronousOperationListener asyncListener,
        Mutex testSessionHookupMutex)
    {
        CurrentSession = new EventHookupSession(
            this, eventHookupCommandHandler, textView, subjectBuffer, position, document, asyncListener, testSessionHookupMutex);
    }

    public void DismissExistingSessions()
    {
        ThreadingContext.ThrowIfNotOnUIThread();

        _toolTipPresenter?.Dismiss();
        _toolTipPresenter = null;
        _suggestionBlocker?.DisposeAsync().Forget();
        _suggestionBlocker = null;

        CurrentSession = null;

        // For test purposes only!
        TEST_MostRecentToolTipContent = null;
    }

    /// <summary>
    /// If any text is deleted or any non-space text is entered, cancel the session.
    /// </summary>
    private void TextBuffer_Changed(object sender, TextContentChangedEventArgs e)
    {
        ThreadingContext.ThrowIfNotOnUIThread();

        foreach (var change in e.Changes)
        {
            if (change.OldText.Length > 0 || change.NewText.Any(c => c != ' '))
            {
                DismissExistingSessions();
                return;
            }
        }
    }

    /// <summary>
    /// If the caret moves outside the session's tracking span, cancel the session.
    /// </summary>
    private void Caret_PositionChanged(object sender, EventArgs e)
    {
        ThreadingContext.ThrowIfNotOnUIThread();

        if (CurrentSession == null)
        {
            DismissExistingSessions();
            return;
        }

        var caretPoint = CurrentSession.TextView.GetCaretPoint(CurrentSession.SubjectBuffer);

        if (!caretPoint.HasValue)
        {
            DismissExistingSessions();
        }

        var snapshotSpan = CurrentSession.TrackingSpan.GetSpan(CurrentSession.SubjectBuffer.CurrentSnapshot);
        if (snapshotSpan.Snapshot != caretPoint.Value.Snapshot || !snapshotSpan.Contains(caretPoint.Value))
        {
            DismissExistingSessions();
        }
    }
}
