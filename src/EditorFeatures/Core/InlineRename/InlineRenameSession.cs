// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.BackgroundWorkIndicator;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Undo;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.InlineRename;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename;

internal partial class InlineRenameSession : IInlineRenameSession, IFeatureController
{
    private readonly IUIThreadOperationExecutor _uiThreadOperationExecutor;

    private readonly ITextBufferAssociatedViewService _textBufferAssociatedViewService;
    private readonly ITextBufferFactoryService _textBufferFactoryService;
    private readonly ITextBufferCloneService _textBufferCloneService;

    private readonly IFeatureService _featureService;
    private readonly IFeatureDisableToken _completionDisabledToken;
    private readonly IEnumerable<IRefactorNotifyService> _refactorNotifyServices;
    private readonly IAsynchronousOperationListener _asyncListener;
    private readonly Solution _baseSolution;
    private readonly ITextView _triggerView;
    private readonly IDisposable _inlineRenameSessionDurationLogBlock;
    private readonly IThreadingContext _threadingContext;
    public readonly InlineRenameService RenameService;

    private bool _dismissed;
    private bool _isApplyingEdit;
    private string _replacementText;
    private readonly Dictionary<ITextBuffer, OpenTextBufferManager> _openTextBuffers = [];

    /// <summary>
    /// The original <see cref="Document"/> where rename was triggered
    /// </summary>
    public Document TriggerDocument { get; }

    /// <summary>
    /// The original <see cref="SnapshotSpan"/> for the identifier that rename was triggered on
    /// </summary>
    public SnapshotSpan TriggerSpan { get; }

    /// <summary>
    /// If non-null, the current text of the replacement. Linked spans added will automatically be updated with this
    /// text.
    /// </summary>
    public string ReplacementText
    {
        get
        {
            return _replacementText;
        }
        private set
        {
            _replacementText = value;
            ReplacementTextChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Information about whether a file rename should be allowed as part
    /// of the rename operation, as determined by the language
    /// </summary>
    public InlineRenameFileRenameInfo FileRenameInfo { get; }

    /// <summary>
    /// Information about this rename session.
    /// </summary>
    public IInlineRenameInfo RenameInfo { get; }

    /// <summary>
    /// The task which computes the main rename locations against the original workspace
    /// snapshot.
    /// </summary>
    public JoinableTask<IInlineRenameLocationSet> AllRenameLocationsTask { get; private set; }

    /// <summary>
    /// Keep-alive session held alive with the OOP server.  This allows us to pin the initial solution snapshot over on
    /// the oop side, which is valuable for preventing it from constantly being dropped/synced on every conflict
    /// resolution step.
    /// </summary>
    private readonly RemoteKeepAliveSession _keepAliveSession;

    /// <summary>
    /// The cancellation token for most work being done by the inline rename session. This
    /// includes the <see cref="AllRenameLocationsTask"/> tasks.
    /// </summary>
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    /// <summary>
    /// This task is a continuation of the <see cref="AllRenameLocationsTask"/> that is the result of computing
    /// the resolutions of the rename spans for the current replacementText.
    /// </summary>
    private JoinableTask<IInlineRenameReplacementInfo> _conflictResolutionTask;

    /// <summary>
    /// The cancellation source for <see cref="_conflictResolutionTask"/>.
    /// </summary>
    private CancellationTokenSource _conflictResolutionTaskCancellationSource = new();

    /// <summary>
    /// Task to track the commit operation of the session. Null if commit operation has never started.
    /// </summary>
    private Task<bool> _commitTask;

    public bool IsCommitInProgress => !_dismissed && _commitTask is { IsCompleted: false };

    /// <summary>
    /// The initial text being renamed.
    /// </summary>
    private readonly string _initialRenameText;

    public InlineRenameSession(
        IThreadingContext threadingContext,
        InlineRenameService renameService,
        Workspace workspace,
        SnapshotSpan triggerSpan,
        IInlineRenameInfo renameInfo,
        SymbolRenameOptions options,
        bool previewChanges,
        IUIThreadOperationExecutor uiThreadOperationExecutor,
        ITextBufferAssociatedViewService textBufferAssociatedViewService,
        ITextBufferFactoryService textBufferFactoryService,
        ITextBufferCloneService textBufferCloneService,
        IFeatureServiceFactory featureServiceFactory,
        IEnumerable<IRefactorNotifyService> refactorNotifyServices,
        IAsynchronousOperationListener asyncListener)
    {
        // This should always be touching a symbol since we verified that upon invocation
        _threadingContext = threadingContext;
        RenameInfo = renameInfo;

        TriggerSpan = triggerSpan;
        TriggerDocument = triggerSpan.Snapshot.GetOpenDocumentInCurrentContextWithChanges();
        if (TriggerDocument == null)
        {
            throw new InvalidOperationException(EditorFeaturesResources.The_triggerSpan_is_not_included_in_the_given_workspace);
        }

        _inlineRenameSessionDurationLogBlock = Logger.LogBlock(FunctionId.Rename_InlineSession, CancellationToken.None);

        Workspace = workspace;
        Workspace.WorkspaceChanged += OnWorkspaceChanged;

        _textBufferFactoryService = textBufferFactoryService;
        _textBufferCloneService = textBufferCloneService;
        _textBufferAssociatedViewService = textBufferAssociatedViewService;
        _textBufferAssociatedViewService.SubjectBuffersConnected += OnSubjectBuffersConnected;

        // Disable completion when an inline rename session starts
        _featureService = featureServiceFactory.GlobalFeatureService;
        _completionDisabledToken = _featureService.Disable(PredefinedEditorFeatureNames.Completion, this);
        RenameService = renameService;
        _uiThreadOperationExecutor = uiThreadOperationExecutor;
        _refactorNotifyServices = refactorNotifyServices;
        _asyncListener = asyncListener;
        _triggerView = textBufferAssociatedViewService.GetAssociatedTextViews(triggerSpan.Snapshot.TextBuffer).FirstOrDefault(v => v.HasAggregateFocus) ??
            textBufferAssociatedViewService.GetAssociatedTextViews(triggerSpan.Snapshot.TextBuffer).First();

        Options = options;
        PreviewChanges = previewChanges;

        _initialRenameText = triggerSpan.GetText();
        this.ReplacementText = _initialRenameText;

        _baseSolution = TriggerDocument.Project.Solution;
        this.UndoManager = workspace.Services.GetService<IInlineRenameUndoManager>();

        FileRenameInfo = RenameInfo.GetFileRenameInfo();

        // Open a session to oop, syncing our solution to it and pinning it there.  The connection will close once
        // _cancellationTokenSource is canceled (which we always do when the session is finally ended).
        _keepAliveSession = RemoteKeepAliveSession.Create(_baseSolution, asyncListener);
        InitializeOpenBuffers(triggerSpan);
    }

    public string OriginalSymbolName => RenameInfo.DisplayName;

    // Used to aid the investigation of https://github.com/dotnet/roslyn/issues/7364
    private class NullTextBufferException(Document document, SourceText text) : Exception("Cannot retrieve textbuffer from document.")
    {
#pragma warning disable IDE0052 // Remove unread private members
        private readonly Document _document = document;
        private readonly SourceText _text = text;
    }

    private void InitializeOpenBuffers(SnapshotSpan triggerSpan)
    {
        using (Logger.LogBlock(FunctionId.Rename_CreateOpenTextBufferManagerForAllOpenDocs, CancellationToken.None))
        {
            var openBuffers = new HashSet<ITextBuffer>();
            foreach (var d in Workspace.GetOpenDocumentIds())
            {
                var document = _baseSolution.GetDocument(d);
                if (document == null)
                {
                    continue;
                }

                Contract.ThrowIfFalse(document.TryGetText(out var text));
                Contract.ThrowIfNull(text);

                var textSnapshot = text.FindCorrespondingEditorTextSnapshot();
                if (textSnapshot == null)
                {
                    FatalError.ReportAndCatch(new NullTextBufferException(document, text));
                    continue;
                }

                Contract.ThrowIfNull(textSnapshot.TextBuffer);

                openBuffers.Add(textSnapshot.TextBuffer);
            }

            foreach (var buffer in openBuffers)
            {
                TryPopulateOpenTextBufferManagerForBuffer(buffer);
            }
        }

        var startingSpan = triggerSpan.Span;

        // Select this span if we didn't already have something selected
        var selections = _triggerView.Selection.GetSnapshotSpansOnBuffer(triggerSpan.Snapshot.TextBuffer);
        if (!selections.Any() ||
            selections.First().IsEmpty ||
            !startingSpan.Contains(selections.First()))
        {
            _triggerView.SetSelection(new SnapshotSpan(triggerSpan.Snapshot, startingSpan));
        }

        this.UndoManager.CreateInitialState(this.ReplacementText, _triggerView.Selection, new SnapshotSpan(triggerSpan.Snapshot, startingSpan));
        _openTextBuffers[triggerSpan.Snapshot.TextBuffer].SetReferenceSpans([startingSpan.ToTextSpan()]);

        UpdateReferenceLocationsTask();

        RenameTrackingDismisser.DismissRenameTracking(Workspace, Workspace.GetOpenDocumentIds());
    }

    private bool TryPopulateOpenTextBufferManagerForBuffer(ITextBuffer buffer)
    {
        _threadingContext.ThrowIfNotOnUIThread();
        VerifyNotDismissed();

        if (Workspace.Kind == WorkspaceKind.Interactive)
        {
            Debug.Assert(buffer.GetRelatedDocuments().Count() == 1);
            Debug.Assert(buffer.IsReadOnly(0) == buffer.IsReadOnly(VisualStudio.Text.Span.FromBounds(0, buffer.CurrentSnapshot.Length))); // All or nothing.
            if (buffer.IsReadOnly(0))
            {
                return false;
            }
        }

        if (!_openTextBuffers.ContainsKey(buffer) && buffer.SupportsRename())
        {
            _openTextBuffers[buffer] = new OpenTextBufferManager(this, Workspace, _textBufferFactoryService, _textBufferCloneService, buffer);
            return true;
        }

        return _openTextBuffers.ContainsKey(buffer);
    }

    private void OnSubjectBuffersConnected(object sender, SubjectBuffersConnectedEventArgs e)
    {
        _threadingContext.ThrowIfNotOnUIThread();
        foreach (var buffer in e.SubjectBuffers)
        {
            if (buffer.GetWorkspace() == Workspace)
            {
                if (TryPopulateOpenTextBufferManagerForBuffer(buffer))
                {
                    _openTextBuffers[buffer].ConnectToView(e.TextView);
                }
            }
        }
    }

    private void UpdateReferenceLocationsTask()
    {
        _threadingContext.ThrowIfNotOnUIThread();

        var asyncToken = _asyncListener.BeginAsyncOperation("UpdateReferencesTask");

        var currentOptions = Options;
        var currentRenameLocationsTask = AllRenameLocationsTask;
        var cancellationToken = _cancellationTokenSource.Token;

        AllRenameLocationsTask = _threadingContext.JoinableTaskFactory.RunAsync(async () =>
        {
            // Join prior work before proceeding, since it performs a required state update.
            // https://github.com/dotnet/roslyn/pull/34254#discussion_r267024593
            if (currentRenameLocationsTask != null)
                await AllRenameLocationsTask.JoinAsync(cancellationToken).ConfigureAwait(false);

            await TaskScheduler.Default;
            var inlineRenameLocations = await RenameInfo.FindRenameLocationsAsync(currentOptions, cancellationToken).ConfigureAwait(false);

            // It's unfortunate that _allRenameLocationsTask has a UI thread dependency (prevents continuations
            // from running prior to the completion of the UI operation), but the implementation does not currently
            // follow the originally-intended design.
            // https://github.com/dotnet/roslyn/issues/40890
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(alwaysYield: true, cancellationToken);

            RaiseSessionSpansUpdated([.. inlineRenameLocations.Locations]);

            return inlineRenameLocations;
        });

        AllRenameLocationsTask.Task.CompletesAsyncOperation(asyncToken);

        UpdateConflictResolutionTask();
        QueueApplyReplacements();
    }

    public Workspace Workspace { get; }
    public SymbolRenameOptions Options { get; private set; }
    public bool PreviewChanges { get; private set; }
    public bool HasRenameOverloads => RenameInfo.HasOverloads;
    public bool MustRenameOverloads => RenameInfo.MustRenameOverloads;

    public IInlineRenameUndoManager UndoManager { get; }

    public event EventHandler<ImmutableArray<InlineRenameLocation>> ReferenceLocationsChanged;
    public event EventHandler<IInlineRenameReplacementInfo> ReplacementsComputed;
    public event EventHandler ReplacementTextChanged;

    /// <summary>
    /// True if commit operation starts, False if commit operation ends.
    /// </summary>
    public event EventHandler<bool> CommitStateChange;

    internal OpenTextBufferManager GetBufferManager(ITextBuffer buffer)
        => _openTextBuffers[buffer];

    internal bool TryGetBufferManager(ITextBuffer buffer, out OpenTextBufferManager bufferManager)
        => _openTextBuffers.TryGetValue(buffer, out bufferManager);

    public void RefreshRenameSessionWithOptionsChanged(SymbolRenameOptions newOptions)
    {
        if (Options == newOptions)
        {
            return;
        }

        _threadingContext.ThrowIfNotOnUIThread();
        VerifyNotDismissed();

        Options = newOptions;
        UpdateReferenceLocationsTask();
    }

    public void SetPreviewChanges(bool value)
    {
        _threadingContext.ThrowIfNotOnUIThread();
        VerifyNotDismissed();

        PreviewChanges = value;
    }

    private void VerifyNotDismissed()
    {
        if (_dismissed)
        {
            throw new InvalidOperationException(EditorFeaturesResources.This_session_has_already_been_dismissed);
        }
    }

    private void OnWorkspaceChanged(object sender, WorkspaceChangeEventArgs args)
    {
        if (args.Kind != WorkspaceChangeKind.DocumentChanged)
        {
            // Make sure only call Cancel() when there is real document changes.
            // Sometimes, WorkspaceChangeKind.SolutionChanged might be triggered because of SourceGeneratorVersion get updated.
            // We don't want to cancel rename when there is no changed documents.
            var changedDocuments = args.NewSolution.GetChangedDocuments(args.OldSolution);
            if (changedDocuments.Any())
            {
                Logger.Log(FunctionId.Rename_InlineSession_Cancel_NonDocumentChangedWorkspaceChange, KeyValueLogMessage.Create(m =>
                {
                    m["Kind"] = Enum.GetName(typeof(WorkspaceChangeKind), args.Kind);
                }));

                Cancel();
            }
        }
    }

    private void RaiseSessionSpansUpdated(ImmutableArray<InlineRenameLocation> locations)
    {
        _threadingContext.ThrowIfNotOnUIThread();
        SetReferenceLocations(locations);

        // It's OK to call SetReferenceLocations with all documents, including unchangeable ones,
        // because they can't be opened, so the _openTextBuffers loop won't matter. In fact, the entire
        // inline rename is oblivious to unchangeable documents, we just need to filter out references
        // in them to avoid displaying them in the UI.
        // https://github.com/dotnet/roslyn/issues/41242
        if (Workspace.IgnoreUnchangeableDocumentsWhenApplyingChanges)
        {
            locations = locations.WhereAsArray(l => l.Document.CanApplyChange());
        }

        ReferenceLocationsChanged?.Invoke(this, locations);
    }

    private void SetReferenceLocations(ImmutableArray<InlineRenameLocation> locations)
    {
        _threadingContext.ThrowIfNotOnUIThread();

        var locationsByDocument = locations.ToLookup(l => l.Document.Id);

        _isApplyingEdit = true;
        foreach (var textBuffer in _openTextBuffers.Keys)
        {
            var documents = textBuffer.AsTextContainer().GetRelatedDocuments();

            if (!documents.Any(static (d, locationsByDocument) => locationsByDocument.Contains(d.Id), locationsByDocument))
            {
                _openTextBuffers[textBuffer].SetReferenceSpans([]);
            }
            else
            {
                var spans = documents.SelectMany(d => locationsByDocument[d.Id]).Select(l => l.TextSpan).Distinct();
                _openTextBuffers[textBuffer].SetReferenceSpans(spans);
            }
        }

        _isApplyingEdit = false;
    }

    /// <summary>
    /// Updates the replacement text for the rename session and propagates it to all live buffers.
    /// </summary>
    internal void ApplyReplacementText(string replacementText, bool propagateEditImmediately, bool updateSelection = true)
    {
        _threadingContext.ThrowIfNotOnUIThread();
        VerifyNotDismissed();
        this.ReplacementText = RenameInfo.GetFinalSymbolName(replacementText);

        var asyncToken = _asyncListener.BeginAsyncOperation(nameof(ApplyReplacementText));

        Action propagateEditAction = delegate
        {
            _threadingContext.ThrowIfNotOnUIThread();

            if (_dismissed)
            {
                asyncToken.Dispose();
                return;
            }

            _isApplyingEdit = true;
            using (Logger.LogBlock(FunctionId.Rename_ApplyReplacementText, replacementText, _cancellationTokenSource.Token))
            {
                foreach (var openBuffer in _openTextBuffers.Values)
                {
                    openBuffer.ApplyReplacementText(updateSelection);
                }
            }

            _isApplyingEdit = false;

            // We already kicked off UpdateConflictResolutionTask below (outside the delegate).
            // Now that we are certain the replacement text has been propagated to all of the
            // open buffers, it is safe to actually apply the replacements it has calculated.
            // See https://devdiv.visualstudio.com/DevDiv/_workitems?_a=edit&id=227513
            QueueApplyReplacements();

            asyncToken.Dispose();
        };

        // Start the conflict resolution task but do not apply the results immediately. The
        // buffer changes performed in propagateEditAction can cause source control modal
        // dialogs to show. Those dialogs pump, and yield the UI thread to whatever work is
        // waiting to be done there, including our ApplyReplacements work. If ApplyReplacements
        // starts running on the UI thread while propagateEditAction is still updating buffers
        // on the UI thread, we crash because we try to enumerate the undo stack while an undo
        // transaction is still in process. Therefore, we defer QueueApplyReplacements until
        // after the buffers have been edited, and any modal dialogs have been completed.
        // In addition to avoiding the crash, this also ensures that the resolved conflict text
        // is applied after the simple text change is propagated.
        // See https://devdiv.visualstudio.com/DevDiv/_workitems?_a=edit&id=227513
        UpdateConflictResolutionTask();

        if (propagateEditImmediately)
        {
            propagateEditAction();
        }
        else
        {
            // When responding to a text edit, we delay propagating the edit until the first transaction completes.
            _threadingContext.JoinableTaskFactory.RunAsync(async () =>
            {
                await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(alwaysYield: true);
                propagateEditAction();
            });
        }
    }

    private void UpdateConflictResolutionTask()
    {
        _threadingContext.ThrowIfNotOnUIThread();

        _conflictResolutionTaskCancellationSource.Cancel();
        _conflictResolutionTaskCancellationSource = new CancellationTokenSource();

        // If the replacement text is empty, we do not update the results of the conflict
        // resolution task. We instead wait for a non-empty identifier.
        if (this.ReplacementText == string.Empty)
        {
            return;
        }

        var replacementText = this.ReplacementText;
        var options = Options;
        var cancellationToken = _conflictResolutionTaskCancellationSource.Token;

        var asyncToken = _asyncListener.BeginAsyncOperation(nameof(UpdateConflictResolutionTask));

        _conflictResolutionTask = _threadingContext.JoinableTaskFactory.RunAsync(async () =>
        {
            // Join prior work before proceeding, since it performs a required state update.
            // https://github.com/dotnet/roslyn/pull/34254#discussion_r267024593
            //
            // If cancellation of the conflict resolution task is requested before the rename locations task
            // completes, we do not need to wait for rename before cancelling. The next conflict resolution task
            // will wait on the latest rename location task if/when necessary.
            var result = await AllRenameLocationsTask.JoinAsync(cancellationToken).ConfigureAwait(false);
            await TaskScheduler.Default;

            return await result.GetReplacementsAsync(replacementText, options, cancellationToken).ConfigureAwait(false);
        });

        _conflictResolutionTask.Task.CompletesAsyncOperation(asyncToken);
    }

    [SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "False positive in methods using JTF: https://github.com/dotnet/roslyn-analyzers/issues/4283")]
    private void QueueApplyReplacements()
    {
        // If the replacement text is empty, we do not update the results of the conflict
        // resolution task. We instead wait for a non-empty identifier.
        if (this.ReplacementText == string.Empty)
        {
            return;
        }

        var cancellationToken = _conflictResolutionTaskCancellationSource.Token;
        var asyncToken = _asyncListener.BeginAsyncOperation(nameof(QueueApplyReplacements));
        var replacementOperation = _threadingContext.JoinableTaskFactory.RunAsync(async () =>
        {
            var replacementInfo = await _conflictResolutionTask.JoinAsync(CancellationToken.None).ConfigureAwait(false);
            if (replacementInfo == null || cancellationToken.IsCancellationRequested)
            {
                return;
            }

            // Switch to a background thread for expensive work
            await TaskScheduler.Default;
            var computedMergeResult = await ComputeMergeResultAsync(replacementInfo, cancellationToken);
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(alwaysYield: true, cancellationToken);
            ApplyReplacements(computedMergeResult.replacementInfo, computedMergeResult.mergeResult, cancellationToken);
        });
        replacementOperation.Task.CompletesAsyncOperation(asyncToken);
    }

    private async Task<(IInlineRenameReplacementInfo replacementInfo, LinkedFileMergeSessionResult mergeResult)> ComputeMergeResultAsync(IInlineRenameReplacementInfo replacementInfo, CancellationToken cancellationToken)
    {
        var diffMergingSession = new LinkedFileDiffMergingSession(_baseSolution, replacementInfo.NewSolution, replacementInfo.NewSolution.GetChanges(_baseSolution));
        var mergeResult = await diffMergingSession.MergeDiffsAsync(mergeConflictHandler: null, cancellationToken: cancellationToken).ConfigureAwait(false);
        return (replacementInfo, mergeResult);
    }

    private void ApplyReplacements(IInlineRenameReplacementInfo replacementInfo, LinkedFileMergeSessionResult mergeResult, CancellationToken cancellationToken)
    {
        _threadingContext.ThrowIfNotOnUIThread();
        cancellationToken.ThrowIfCancellationRequested();

        RaiseReplacementsComputed(replacementInfo);

        _isApplyingEdit = true;
        foreach (var textBuffer in _openTextBuffers.Keys)
        {
            var documents = textBuffer.CurrentSnapshot.GetRelatedDocumentsWithChanges();
            if (documents.Any())
            {
                var textBufferManager = _openTextBuffers[textBuffer];
                textBufferManager.ApplyConflictResolutionEdits(replacementInfo, mergeResult, documents, cancellationToken);
            }
        }

        _isApplyingEdit = false;
    }

    private void RaiseReplacementsComputed(IInlineRenameReplacementInfo resolution)
    {
        _threadingContext.ThrowIfNotOnUIThread();
        ReplacementsComputed?.Invoke(this, resolution);
    }

    private void LogRenameSession(RenameLogMessage.UserActionOutcome outcome, bool previewChanges)
    {
        if (_conflictResolutionTask == null)
        {
            return;
        }

        var conflictResolutionFinishedComputing = _conflictResolutionTask.Task.Status == TaskStatus.RanToCompletion;

        if (conflictResolutionFinishedComputing)
        {
            var result = _conflictResolutionTask.Task.Result;
            var replacementKinds = result.GetAllReplacementKinds().ToList();

            Logger.Log(FunctionId.Rename_InlineSession_Session, RenameLogMessage.Create(
                Options,
                outcome,
                conflictResolutionFinishedComputing,
                previewChanges,
                replacementKinds));
        }
        else
        {
            Debug.Assert(outcome.HasFlag(RenameLogMessage.UserActionOutcome.Canceled));
            Logger.Log(FunctionId.Rename_InlineSession_Session, RenameLogMessage.Create(
                Options,
                outcome,
                conflictResolutionFinishedComputing,
                previewChanges,
                replacementKinds: []));
        }
    }

    public void Cancel()
    {
        _threadingContext.ThrowIfNotOnUIThread();

        // This wait is safe.  We are not passing the async callback to DismissUIAndRollbackEditsAndEndRenameSessionAsync.
        // So everything in that method will happen synchronously.
        DismissUIAndRollbackEditsAndEndRenameSessionAsync(
            RenameLogMessage.UserActionOutcome.Canceled, previewChanges: false).Wait();
    }

    private async Task DismissUIAndRollbackEditsAndEndRenameSessionAsync(
        RenameLogMessage.UserActionOutcome outcome,
        bool previewChanges,
        Func<Task> finalCommitAction = null)
    {
        // Note: this entire sequence of steps is not cancellable.  We must perform it all to get back to a correct
        // state for all the editors the user is interacting with.
        var cancellationToken = CancellationToken.None;
        await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        if (_dismissed)
        {
            return;
        }

        _dismissed = true;

        // Remove all our adornments and restore all buffer texts to their initial state.
        DismissUIAndRollbackEdits();

        // We're about to perform the final commit action.  No need to do any of our BG work to find-refs or compute conflicts.
        _cancellationTokenSource.Cancel();
        _conflictResolutionTaskCancellationSource.Cancel();

        // Close the keep alive session we have open with OOP, allowing it to release the solution it is holding onto.
        _keepAliveSession.Dispose();

        // Perform the actual commit step if we've been asked to.
        if (finalCommitAction != null)
        {
            // ConfigureAwait(true) so we come back to the UI thread to finish work.
            await finalCommitAction().ConfigureAwait(true);
        }

        // Log the result so we know how well rename is going in practice.
        LogRenameSession(outcome, previewChanges);

        // Remove all our rename trackers from the text buffer properties.
        RenameTrackingDismisser.DismissRenameTracking(Workspace, Workspace.GetOpenDocumentIds());

        // Log how long the full rename took.
        _inlineRenameSessionDurationLogBlock.Dispose();

        return;

        void DismissUIAndRollbackEdits()
        {
            Workspace.WorkspaceChanged -= OnWorkspaceChanged;
            _textBufferAssociatedViewService.SubjectBuffersConnected -= OnSubjectBuffersConnected;

            // Reenable completion now that the inline rename session is done
            _completionDisabledToken.Dispose();

            foreach (var textBuffer in _openTextBuffers.Keys)
            {
                var document = textBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
                var isClosed = document == null;

                var openBuffer = _openTextBuffers[textBuffer];
                openBuffer.DisconnectAndRollbackEdits(isClosed);
            }

            this.UndoManager.Disconnect();

            if (_triggerView != null && !_triggerView.IsClosed)
            {
                _triggerView.Selection.Clear();
            }

            RenameService.ActiveSession = null;
        }
    }

    /// <remarks>
    /// Caller should pass in the IUIThreadOperationContext if it is called from editor so rename commit operation could set up the its own context correctly.
    /// </remarks>
    public void Commit(bool previewChanges = false, IUIThreadOperationContext editorOperationContext = null)
        => CommitSynchronously(previewChanges, editorOperationContext);

    /// <returns><see langword="true"/> if the rename operation was committed, <see
    /// langword="false"/> otherwise</returns>
    private bool CommitSynchronously(bool previewChanges, IUIThreadOperationContext editorOperationContext = null)
    {
        // We're going to synchronously block the UI thread here.  So we can't use the background work indicator (as
        // it needs the UI thread to update itself.  This will force us to go through the Threaded-Wait-Dialog path
        // which at least will allow the user to cancel the rename if they want.
        //
        // In the future we should remove this entrypoint and have all callers use CommitAsync instead.
        return _threadingContext.JoinableTaskFactory.Run(() => CommitWorkerAsync(previewChanges, canUseBackgroundWorkIndicator: false, editorOperationContext));
    }

    /// <remarks>
    /// Caller should pass in the IUIThreadOperationContext if it is called from editor so rename commit operation could set up the its own context correctly.
    /// </remarks>
    public async Task CommitAsync(bool previewChanges, IUIThreadOperationContext editorOperationContext = null)
    {
        if (this.RenameService.GlobalOptions.GetOption(InlineRenameSessionOptionsStorage.RenameAsynchronously))
        {
            await StartCommitAsync(previewChanges, canUseBackgroundWorkIndicator: true, editorOperationContext).ConfigureAwait(false);
        }
        else
        {
            CommitSynchronously(previewChanges, editorOperationContext);
        }
    }

    private async Task<bool> StartCommitAsync(bool previewChanges, bool canUseBackgroundWorkIndicator, IUIThreadOperationContext editorOperationContext)
    {
        await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync();
        // We are going to commit in async manner.
        // 1. If the session is dismissed, stop and do not start another commit task
        if (_dismissed)
        {
            return false;
        }

        // One session should only start one commit task.
        // Start the task if 
        // 1. commit never starts in this session. (_commitTask is null)
        // 2. There is no in-progress task. This would include
        //    a. Commit operation starts before, user previews the result, and cancel the commit
        //    b. In async commit operation while waiting the background task completed (user triggers the commit by enter key), then user's input might trigger Commit() again. Example: SaveCommandHandler.
        //       In this case we don't want to trigger commit again.
        if (_commitTask is null || !IsCommitInProgress)
        {
            _commitTask = CommitWorkerAsync(previewChanges, canUseBackgroundWorkIndicator, editorOperationContext);
        }

        return await _commitTask.ConfigureAwait(false);
    }

    /// <returns><see langword="true"/> if the rename operation was committed, <see
    /// langword="false"/> otherwise</returns>
    private async Task<bool> CommitWorkerAsync(bool previewChanges, bool canUseBackgroundWorkIndicator, IUIThreadOperationContext editorUIOperationContext)
    {
        await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync();
        VerifyNotDismissed();

        // If the identifier was deleted (or didn't change at all) then cancel the operation.
        // Note: an alternative approach would be for the work we're doing (like detecting
        // conflicts) to quickly bail in the case of no change.  However, that involves deeper
        // changes to the system and is less easy to validate that nothing happens.
        //
        // The only potential downside here would be if there was a language that wanted to
        // still 'rename' even if the identifier went away (or was unchanged).  But that isn't
        // a case we're aware of, so it's fine to be opinionated here that we can quickly bail
        // in these cases.
        if (this.ReplacementText == string.Empty ||
            this.ReplacementText == _initialRenameText)
        {
            Cancel();
            return false;
        }

        previewChanges = previewChanges || PreviewChanges;

        if (editorUIOperationContext is not null)
        {
            // Prevent Editor's typing responsiveness auto canceling the rename operation.
            // InlineRenameSession will call IUIThreadOperationExecutor to sets up our own IUIThreadOperationContext
            editorUIOperationContext.TakeOwnership();
        }

        try
        {
            if (canUseBackgroundWorkIndicator)
            {
                // We do not cancel on edit because as part of the rename system we have asynchronous work still
                // occurring that itself may be asynchronously editing the buffer (for example, updating reference
                // locations with the final renamed text).  Ideally though, once we start comitting, we would cancel
                // any of that work and then only have the work of rolling back to the original state of the world
                // and applying the desired edits ourselves.
                var factory = Workspace.Services.GetRequiredService<IBackgroundWorkIndicatorFactory>();
                using var context = factory.Create(
                    _triggerView, TriggerSpan, EditorFeaturesResources.Computing_Rename_information,
                    cancelOnEdit: false, cancelOnFocusLost: false);

                await CommitCoreAsync(context, previewChanges).ConfigureAwait(true);
            }
            else
            {
                using var context = _uiThreadOperationExecutor.BeginExecute(
                    title: EditorFeaturesResources.Rename,
                    defaultDescription: EditorFeaturesResources.Computing_Rename_information,
                    allowCancellation: true,
                    showProgress: false);

                // .ConfigureAwait(true); so we can return to the UI thread to dispose the operation context.  It
                // has a non-JTF threading dependency on the main thread.  So it can deadlock if you call it on a BG
                // thread when in a blocking JTF call.
                await CommitCoreAsync(context, previewChanges).ConfigureAwait(true);
            }
        }
        catch (OperationCanceledException)
        {
            await DismissUIAndRollbackEditsAndEndRenameSessionAsync(
                RenameLogMessage.UserActionOutcome.Canceled | RenameLogMessage.UserActionOutcome.Committed, previewChanges).ConfigureAwait(false);
            return false;
        }

        return true;
    }

    private async Task CommitCoreAsync(IUIThreadOperationContext operationContext, bool previewChanges)
    {
        // Notify the UI commit is started.
        // In legacy UI dashboard, it will disable all rename option buttons (like rename comment).
        // In renameFlyout, it will collapse the UI because renameFlyout will hide the background indicator.
        CommitStateChange?.Invoke(this, true);
        var cancellationToken = operationContext.UserCancellationToken;
        var eventName = previewChanges ? FunctionId.Rename_CommitCoreWithPreview : FunctionId.Rename_CommitCore;
        try
        {
            using (Logger.LogBlock(eventName, KeyValueLogMessage.Create(LogType.UserAction), cancellationToken))
            {
                var info = await _conflictResolutionTask.JoinAsync(cancellationToken).ConfigureAwait(true);
                var newSolution = info.NewSolution;

                if (previewChanges)
                {
                    var previewService = Workspace.Services.GetService<IPreviewDialogService>();

                    // The preview service needs to be called from the UI thread, since it's doing COM calls underneath.
                    await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                    newSolution = previewService.PreviewChanges(
                        string.Format(EditorFeaturesResources.Preview_Changes_0, EditorFeaturesResources.Rename),
                        "vs.csharp.refactoring.rename",
                        string.Format(EditorFeaturesResources.Rename_0_to_1_colon, this.OriginalSymbolName, this.ReplacementText),
                        RenameInfo.FullDisplayName,
                        RenameInfo.Glyph,
                        newSolution,
                        TriggerDocument.Project.Solution);

                    if (newSolution == null)
                    {
                        // User clicked cancel.
                        return;
                    }
                }

                // The user hasn't canceled by now, so we're done waiting for them. Off to rename!
                using var _ = operationContext.AddScope(allowCancellation: false, EditorFeaturesResources.Updating_files);

                await DismissUIAndRollbackEditsAndEndRenameSessionAsync(
                    RenameLogMessage.UserActionOutcome.Committed, previewChanges,
                    async () =>
                    {
                        var error = await TryApplyRenameAsync(newSolution, cancellationToken).ConfigureAwait(false);
                        if (error is not null)
                        {
                            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                            var notificationService = Workspace.Services.GetService<INotificationService>();
                            notificationService.SendNotification(
                                error.Value.message, EditorFeaturesResources.Rename_Symbol, error.Value.severity);
                        }
                    }).ConfigureAwait(false);
            }
        }
        finally
        {
            // Notify the rename UI the commit ends.
            // User might just preview the change so UI needs to reset its state when commit finish.
            CommitStateChange?.Invoke(this, false);
        }
    }

    /// <summary>
    /// Returns non-null error message if renaming fails.
    /// </summary>
    private async Task<(NotificationSeverity severity, string message)?> TryApplyRenameAsync(
        Solution newSolution, CancellationToken cancellationToken)
    {
        var changes = _baseSolution.GetChanges(newSolution);
        var changedDocumentIDs = changes.GetProjectChanges().SelectMany(c => c.GetChangedDocuments()).ToList();

        // Go to the background thread for initial calculation of the final solution
        await TaskScheduler.Default;
        var finalSolution = CalculateFinalSolutionSynchronously(newSolution, newSolution.Workspace, changedDocumentIDs, cancellationToken);

        await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        using var undoTransaction = Workspace.OpenGlobalUndoTransaction(EditorFeaturesResources.Inline_Rename);

        if (!RenameInfo.TryOnBeforeGlobalSymbolRenamed(Workspace, changedDocumentIDs, this.ReplacementText))
            return (NotificationSeverity.Error, EditorFeaturesResources.Rename_operation_was_cancelled_or_is_not_valid);

        if (!Workspace.TryApplyChanges(finalSolution))
        {
            // If the workspace changed in TryOnBeforeGlobalSymbolRenamed retry, this prevents rename from failing for cases
            // where text changes to other files or workspace state change doesn't impact the text changes being applied. 
            Logger.Log(FunctionId.Rename_TryApplyRename_WorkspaceChanged, message: null, LogLevel.Information);
            finalSolution = CalculateFinalSolutionSynchronously(newSolution, Workspace, changedDocumentIDs, cancellationToken);

            if (!Workspace.TryApplyChanges(finalSolution))
                return (NotificationSeverity.Error, EditorFeaturesResources.Rename_operation_could_not_complete_due_to_external_change_to_workspace);
        }

        try
        {
            // Since rename can apply file changes as well, and those file
            // changes can generate new document ids, include added documents
            // as well as changed documents. This also ensures that any document
            // that was removed is not included
            var finalChanges = Workspace.CurrentSolution.GetChanges(_baseSolution);

            var finalChangedIds = finalChanges
                .GetProjectChanges()
                .SelectMany(c => c.GetChangedDocuments().Concat(c.GetAddedDocuments()))
                .ToList();

            if (!RenameInfo.TryOnAfterGlobalSymbolRenamed(Workspace, finalChangedIds, this.ReplacementText))
                return (NotificationSeverity.Information, EditorFeaturesResources.Rename_operation_was_not_properly_completed_Some_file_might_not_have_been_updated);

            return null;
        }
        finally
        {
            // If we successfully updated the workspace then make sure the undo transaction is committed and is
            // always able to undo anything any other external listener did.
            undoTransaction.Commit();
        }

        static Solution CalculateFinalSolutionSynchronously(Solution newSolution, Workspace workspace, List<DocumentId> changedDocumentIDs, CancellationToken cancellationToken)
        {
            var finalSolution = workspace.CurrentSolution;
            foreach (var id in changedDocumentIDs)
            {
                // If the document supports syntax tree, then create the new solution from the
                // updated syntax root.  This should ensure that annotations are preserved, and
                // prevents the solution from having to reparse documents when we already have
                // the trees for them.  If we don't support syntax, then just use the text of
                // the document.
                var newDocument = newSolution.GetDocument(id);

                if (newDocument.SupportsSyntaxTree)
                {
                    var root = newDocument.GetRequiredSyntaxRootSynchronously(cancellationToken);
                    finalSolution = finalSolution.WithDocumentSyntaxRoot(id, root);
                }
                else
                {
                    var newText = newDocument.GetTextSynchronously(cancellationToken);
                    finalSolution = finalSolution.WithDocumentText(id, newText);
                }

                // Make sure to include any document rename as well
                finalSolution = finalSolution.WithDocumentName(id, newDocument.Name);
            }

            return finalSolution;
        }
    }

    internal bool TryGetContainingEditableSpan(SnapshotPoint point, out SnapshotSpan editableSpan)
    {
        editableSpan = default;
        if (!_openTextBuffers.TryGetValue(point.Snapshot.TextBuffer, out var bufferManager))
        {
            return false;
        }

        foreach (var span in bufferManager.GetEditableSpansForSnapshot(point.Snapshot))
        {
            if (span.Contains(point) || span.End == point)
            {
                editableSpan = span;
                return true;
            }
        }

        return false;
    }

    internal bool IsInOpenTextBuffer(SnapshotPoint point)
        => _openTextBuffers.ContainsKey(point.Snapshot.TextBuffer);

    internal TestAccessor GetTestAccessor()
        => new TestAccessor(this);

    public readonly struct TestAccessor(InlineRenameSession inlineRenameSession)
    {
        private readonly InlineRenameSession _inlineRenameSession = inlineRenameSession;

        public bool CommitWorker(bool previewChanges)
            => _inlineRenameSession.CommitSynchronously(previewChanges);
    }
}
