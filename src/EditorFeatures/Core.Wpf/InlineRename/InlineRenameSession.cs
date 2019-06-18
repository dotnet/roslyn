// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Undo;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    internal partial class InlineRenameSession : ForegroundThreadAffinitizedObject, IInlineRenameSession, IFeatureController
    {
        private readonly Workspace _workspace;
        private readonly InlineRenameService _renameService;
        private readonly IWaitIndicator _waitIndicator;
        private readonly ITextBufferAssociatedViewService _textBufferAssociatedViewService;
        private readonly ITextBufferFactoryService _textBufferFactoryService;
        private readonly IFeatureService _featureService;
        private readonly IFeatureDisableToken _completionDisabledToken;
        private readonly IEnumerable<IRefactorNotifyService> _refactorNotifyServices;
        private readonly IDebuggingWorkspaceService _debuggingWorkspaceService;
        private readonly IAsynchronousOperationListener _asyncListener;
        private readonly Solution _baseSolution;
        private readonly Document _triggerDocument;
        private readonly ITextView _triggerView;
        private readonly IDisposable _inlineRenameSessionDurationLogBlock;

        private bool _dismissed;
        private bool _isApplyingEdit;
        private string _replacementText;
        private OptionSet _optionSet;
        private Dictionary<ITextBuffer, OpenTextBufferManager> _openTextBuffers = new Dictionary<ITextBuffer, OpenTextBufferManager>();

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
        /// The task which computes the main rename locations against the original workspace
        /// snapshot.
        /// </summary>
        private Task<IInlineRenameLocationSet> _allRenameLocationsTask;

        /// <summary>
        /// The cancellation token for most work being done by the inline rename session. This
        /// includes the <see cref="_allRenameLocationsTask"/> tasks.
        /// </summary>
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        /// <summary>
        /// This task is a continuation of the allRenameLocationsTask that is the result of computing
        /// the resolutions of the rename spans for the current replacementText.
        /// </summary>
        private Task<IInlineRenameReplacementInfo> _conflictResolutionTask;

        /// <summary>
        /// The cancellation source for <see cref="_conflictResolutionTask"/>.
        /// </summary>
        private CancellationTokenSource _conflictResolutionTaskCancellationSource = new CancellationTokenSource();

        private readonly IInlineRenameInfo _renameInfo;

        public InlineRenameSession(
            IThreadingContext threadingContext,
            InlineRenameService renameService,
            Workspace workspace,
            SnapshotSpan triggerSpan,
            IInlineRenameInfo renameInfo,
            IWaitIndicator waitIndicator,
            ITextBufferAssociatedViewService textBufferAssociatedViewService,
            ITextBufferFactoryService textBufferFactoryService,
            IFeatureServiceFactory featureServiceFactory,
            IEnumerable<IRefactorNotifyService> refactorNotifyServices,
            IAsynchronousOperationListener asyncListener)
            : base(threadingContext, assertIsForeground: true)
        {
            // This should always be touching a symbol since we verified that upon invocation
            _renameInfo = renameInfo;

            _triggerDocument = triggerSpan.Snapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (_triggerDocument == null)
            {
                throw new InvalidOperationException(EditorFeaturesResources.The_triggerSpan_is_not_included_in_the_given_workspace);
            }

            _inlineRenameSessionDurationLogBlock = Logger.LogBlock(FunctionId.Rename_InlineSession, CancellationToken.None);

            _workspace = workspace;
            _workspace.WorkspaceChanged += OnWorkspaceChanged;

            _textBufferFactoryService = textBufferFactoryService;
            _textBufferAssociatedViewService = textBufferAssociatedViewService;
            _textBufferAssociatedViewService.SubjectBuffersConnected += OnSubjectBuffersConnected;

            // Disable completion when an inline rename session starts
            _featureService = featureServiceFactory.GlobalFeatureService;
            _completionDisabledToken = _featureService.Disable(PredefinedEditorFeatureNames.Completion, this);

            _renameService = renameService;
            _waitIndicator = waitIndicator;
            _refactorNotifyServices = refactorNotifyServices;
            _asyncListener = asyncListener;
            _triggerView = textBufferAssociatedViewService.GetAssociatedTextViews(triggerSpan.Snapshot.TextBuffer).FirstOrDefault(v => v.HasAggregateFocus) ??
                textBufferAssociatedViewService.GetAssociatedTextViews(triggerSpan.Snapshot.TextBuffer).First();

            _optionSet = renameInfo.ForceRenameOverloads
                ? workspace.Options.WithChangedOption(RenameOptions.RenameOverloads, true)
                : workspace.Options;

            this.ReplacementText = triggerSpan.GetText();

            _baseSolution = _triggerDocument.Project.Solution;
            this.UndoManager = workspace.Services.GetService<IInlineRenameUndoManager>();

            _debuggingWorkspaceService = workspace.Services.GetService<IDebuggingWorkspaceService>();
            _debuggingWorkspaceService.BeforeDebuggingStateChanged += OnBeforeDebuggingStateChanged;

            var experimentationService = workspace.Services.GetRequiredService<IExperimentationService>();

            if (experimentationService.IsExperimentEnabled(WellKnownExperimentNames.RoslynInlineRenameFile)
                && _renameInfo is IInlineRenameInfoWithFileRename renameInfoWithFileRename)
            {
                FileRenameInfo = renameInfoWithFileRename.GetFileRenameInfo();
            }
            else
            {
                FileRenameInfo = InlineRenameFileRenameInfo.NotAllowed;
            }

            InitializeOpenBuffers(triggerSpan);
        }

        private void OnBeforeDebuggingStateChanged(object sender, DebuggingStateChangedEventArgs args)
        {
            if (args.After == DebuggingState.Run)
            {
                // It's too late for us to change anything, which means we can neither commit nor
                // rollback changes to cancel. End the rename session but keep all open buffers in
                // their current state.
                Cancel(rollbackTemporaryEdits: false);
            }
        }

        public string OriginalSymbolName => _renameInfo.DisplayName;

        // Used to aid the investigation of https://github.com/dotnet/roslyn/issues/7364
        private class NullTextBufferException : Exception
        {
            private readonly Document _document;
            private readonly SourceText _text;

            public NullTextBufferException(Document document, SourceText text)
                : base("Cannot retrieve textbuffer from document.")
            {
                _document = document;
                _text = text;
            }
        }

        private void InitializeOpenBuffers(SnapshotSpan triggerSpan)
        {
            using (Logger.LogBlock(FunctionId.Rename_CreateOpenTextBufferManagerForAllOpenDocs, CancellationToken.None))
            {
                HashSet<ITextBuffer> openBuffers = new HashSet<ITextBuffer>();
                foreach (var d in _workspace.GetOpenDocumentIds())
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
                        FatalError.ReportWithoutCrash(new NullTextBufferException(document, text));
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
            _openTextBuffers[triggerSpan.Snapshot.TextBuffer].SetReferenceSpans(SpecializedCollections.SingletonEnumerable(startingSpan.ToTextSpan()));

            UpdateReferenceLocationsTask(_renameInfo.FindRenameLocationsAsync(_optionSet, _cancellationTokenSource.Token));
            RenameTrackingDismisser.DismissRenameTracking(_workspace, _workspace.GetOpenDocumentIds());
        }

        private bool TryPopulateOpenTextBufferManagerForBuffer(ITextBuffer buffer)
        {
            AssertIsForeground();
            VerifyNotDismissed();

            if (_workspace.Kind == WorkspaceKind.Interactive)
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
                _openTextBuffers[buffer] = new OpenTextBufferManager(this, buffer, _workspace, _textBufferFactoryService);
                return true;
            }

            return _openTextBuffers.ContainsKey(buffer);
        }

        private void OnSubjectBuffersConnected(object sender, SubjectBuffersConnectedEventArgs e)
        {
            AssertIsForeground();
            foreach (var buffer in e.SubjectBuffers)
            {
                if (buffer.GetWorkspace() == _workspace)
                {
                    if (TryPopulateOpenTextBufferManagerForBuffer(buffer))
                    {
                        _openTextBuffers[buffer].ConnectToView(e.TextView);
                    }
                }
            }
        }

        private void UpdateReferenceLocationsTask(Task<IInlineRenameLocationSet> allRenameLocationsTask)
        {
            AssertIsForeground();

            var asyncToken = _asyncListener.BeginAsyncOperation("UpdateReferencesTask");
            _allRenameLocationsTask = allRenameLocationsTask;
            allRenameLocationsTask.SafeContinueWithFromAsync(
                async t =>
                {
                    await ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(alwaysYield: true, _cancellationTokenSource.Token);
                    _cancellationTokenSource.Token.ThrowIfCancellationRequested();

                    RaiseSessionSpansUpdated(t.Result.Locations.ToImmutableArray());
                },
                _cancellationTokenSource.Token,
                TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default).CompletesAsyncOperation(asyncToken);

            UpdateConflictResolutionTask();
            QueueApplyReplacements();
        }

        public Workspace Workspace => _workspace;
        public OptionSet OptionSet => _optionSet;
        public bool HasRenameOverloads => _renameInfo.HasOverloads;
        public bool ForceRenameOverloads => _renameInfo.ForceRenameOverloads;

        public IInlineRenameUndoManager UndoManager { get; }

        public event EventHandler<ImmutableArray<InlineRenameLocation>> ReferenceLocationsChanged;
        public event EventHandler<IInlineRenameReplacementInfo> ReplacementsComputed;
        public event EventHandler ReplacementTextChanged;

        internal OpenTextBufferManager GetBufferManager(ITextBuffer buffer)
        {
            return _openTextBuffers[buffer];
        }

        internal bool TryGetBufferManager(ITextBuffer buffer, out OpenTextBufferManager bufferManager)
        {
            return _openTextBuffers.TryGetValue(buffer, out bufferManager);
        }

        public void RefreshRenameSessionWithOptionsChanged(Option<bool> renameOption, bool newValue)
        {
            AssertIsForeground();
            VerifyNotDismissed();

            // Recompute the result only if the previous result was computed with a different flag
            if (_optionSet.GetOption(renameOption) != newValue)
            {
                _optionSet = _optionSet.WithChangedOption(renameOption, newValue);

                var cancellationToken = _cancellationTokenSource.Token;

                UpdateReferenceLocationsTask(_allRenameLocationsTask.SafeContinueWithFromAsync(
                    t => _renameInfo.FindRenameLocationsAsync(_optionSet, cancellationToken),
                    cancellationToken, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default));
            }
        }

        private void Dismiss(bool rollbackTemporaryEdits)
        {
            _dismissed = true;
            _workspace.WorkspaceChanged -= OnWorkspaceChanged;
            _textBufferAssociatedViewService.SubjectBuffersConnected -= OnSubjectBuffersConnected;

            // Reenable completion now that the inline rename session is done
            _completionDisabledToken.Dispose();

            foreach (var textBuffer in _openTextBuffers.Keys)
            {
                var document = textBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
                var isClosed = document == null;

                var openBuffer = _openTextBuffers[textBuffer];
                openBuffer.Disconnect(isClosed, rollbackTemporaryEdits);
            }

            this.UndoManager.Disconnect();

            if (_triggerView != null && !_triggerView.IsClosed)
            {
                _triggerView.Selection.Clear();
            }

            _renameService.ActiveSession = null;
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
                if (!_dismissed)
                {
                    this.Cancel();
                }
            }
        }

        private void RaiseSessionSpansUpdated(ImmutableArray<InlineRenameLocation> locations)
        {
            AssertIsForeground();
            SetReferenceLocations(locations);
            ReferenceLocationsChanged?.Invoke(this, locations);
        }

        private void SetReferenceLocations(ImmutableArray<InlineRenameLocation> locations)
        {
            AssertIsForeground();

            var locationsByDocument = locations.ToLookup(l => l.Document.Id);

            _isApplyingEdit = true;
            foreach (var textBuffer in _openTextBuffers.Keys)
            {
                var documents = textBuffer.AsTextContainer().GetRelatedDocuments();

                if (!documents.Any(d => locationsByDocument.Contains(d.Id)))
                {
                    _openTextBuffers[textBuffer].SetReferenceSpans(SpecializedCollections.EmptyEnumerable<TextSpan>());
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
        internal void ApplyReplacementText(string replacementText, bool propagateEditImmediately)
        {
            AssertIsForeground();
            VerifyNotDismissed();
            this.ReplacementText = _renameInfo.GetFinalSymbolName(replacementText);

            var asyncToken = _asyncListener.BeginAsyncOperation(nameof(ApplyReplacementText));

            Action propagateEditAction = delegate
            {
                AssertIsForeground();

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
                        openBuffer.ApplyReplacementText();
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
                ThreadingContext.JoinableTaskFactory.WithPriority(Dispatcher.CurrentDispatcher, DispatcherPriority.Send).RunAsync(async () =>
                {
                    await ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(alwaysYield: true);
                    propagateEditAction();
                });
            }
        }

        private void UpdateConflictResolutionTask()
        {
            AssertIsForeground();

            _conflictResolutionTaskCancellationSource.Cancel();
            _conflictResolutionTaskCancellationSource = new CancellationTokenSource();

            // If the replacement text is empty, we do not update the results of the conflict
            // resolution task. We instead wait for a non-empty identifier.
            if (this.ReplacementText == string.Empty)
            {
                return;
            }

            var replacementText = this.ReplacementText;
            var optionSet = _optionSet;
            var cancellationToken = _conflictResolutionTaskCancellationSource.Token;

            var asyncToken = _asyncListener.BeginAsyncOperation(nameof(UpdateConflictResolutionTask));

            _conflictResolutionTask = _allRenameLocationsTask.SafeContinueWithFromAsync(
               t => t.Result.GetReplacementsAsync(replacementText, optionSet, cancellationToken),
               cancellationToken,
               TaskContinuationOptions.OnlyOnRanToCompletion,
               TaskScheduler.Default);

            _conflictResolutionTask.CompletesAsyncOperation(asyncToken);
        }

        private void QueueApplyReplacements()
        {
            // If the replacement text is empty, we do not update the results of the conflict
            // resolution task. We instead wait for a non-empty identifier.
            if (this.ReplacementText == string.Empty)
            {
                return;
            }

            var asyncToken = _asyncListener.BeginAsyncOperation(nameof(QueueApplyReplacements));
            _conflictResolutionTask
                .SafeContinueWith(
                    t => ComputeMergeResultAsync(t.Result, _conflictResolutionTaskCancellationSource.Token),
                    _conflictResolutionTaskCancellationSource.Token,
                    TaskContinuationOptions.OnlyOnRanToCompletion,
                    TaskScheduler.Default)
                .Unwrap()
                .SafeContinueWithFromAsync(
                    async t =>
                    {
                        await ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(alwaysYield: true, _conflictResolutionTaskCancellationSource.Token);
                        _conflictResolutionTaskCancellationSource.Token.ThrowIfCancellationRequested();

                        ApplyReplacements(t.Result.replacementInfo, t.Result.mergeResult, _conflictResolutionTaskCancellationSource.Token);
                    },
                    _conflictResolutionTaskCancellationSource.Token,
                    TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default)
                .CompletesAsyncOperation(asyncToken);
        }

        private async Task<(IInlineRenameReplacementInfo replacementInfo, LinkedFileMergeSessionResult mergeResult)> ComputeMergeResultAsync(IInlineRenameReplacementInfo replacementInfo, CancellationToken cancellationToken)
        {
            var diffMergingSession = new LinkedFileDiffMergingSession(_baseSolution, replacementInfo.NewSolution, replacementInfo.NewSolution.GetChanges(_baseSolution), logSessionInfo: true);
            var mergeResult = await diffMergingSession.MergeDiffsAsync(mergeConflictHandler: null, cancellationToken: cancellationToken).ConfigureAwait(false);
            return (replacementInfo, mergeResult);
        }

        private void ApplyReplacements(IInlineRenameReplacementInfo replacementInfo, LinkedFileMergeSessionResult mergeResult, CancellationToken cancellationToken)
        {
            AssertIsForeground();
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
            AssertIsForeground();
            ReplacementsComputed?.Invoke(this, resolution);
        }

        private void LogRenameSession(RenameLogMessage.UserActionOutcome outcome, bool previewChanges)
        {
            if (_conflictResolutionTask == null)
            {
                return;
            }

            var conflictResolutionFinishedComputing = _conflictResolutionTask.Status == TaskStatus.RanToCompletion;

            if (conflictResolutionFinishedComputing)
            {
                var result = _conflictResolutionTask.Result;
                var replacementKinds = result.GetAllReplacementKinds().ToList();

                Logger.Log(FunctionId.Rename_InlineSession_Session, RenameLogMessage.Create(
                    _optionSet,
                    outcome,
                    conflictResolutionFinishedComputing,
                    previewChanges,
                    replacementKinds));
            }
            else
            {
                Debug.Assert(outcome.HasFlag(RenameLogMessage.UserActionOutcome.Canceled));
                Logger.Log(FunctionId.Rename_InlineSession_Session, RenameLogMessage.Create(
                    _optionSet,
                    outcome,
                    conflictResolutionFinishedComputing,
                    previewChanges,
                    SpecializedCollections.EmptyList<InlineRenameReplacementKind>()));
            }
        }

        public void Cancel()
        {
            Cancel(rollbackTemporaryEdits: true);
        }

        private void Cancel(bool rollbackTemporaryEdits)
        {
            AssertIsForeground();
            VerifyNotDismissed();

            LogRenameSession(RenameLogMessage.UserActionOutcome.Canceled, previewChanges: false);
            Dismiss(rollbackTemporaryEdits);
            EndRenameSession();
        }

        public void Commit(bool previewChanges = false)
        {
            AssertIsForeground();
            VerifyNotDismissed();

            if (this.ReplacementText == string.Empty)
            {
                Cancel();
                return;
            }

            previewChanges = previewChanges || OptionSet.GetOption(RenameOptions.PreviewChanges);

            var result = _waitIndicator.Wait(
                title: EditorFeaturesResources.Rename,
                message: EditorFeaturesResources.Computing_Rename_information,
                allowCancel: true,
                action: waitContext => CommitCore(waitContext, previewChanges));

            if (result == WaitIndicatorResult.Canceled)
            {
                LogRenameSession(RenameLogMessage.UserActionOutcome.Canceled | RenameLogMessage.UserActionOutcome.Committed, previewChanges);
                Dismiss(rollbackTemporaryEdits: true);
                EndRenameSession();
            }
        }

        private void EndRenameSession()
        {
            _debuggingWorkspaceService.BeforeDebuggingStateChanged -= OnBeforeDebuggingStateChanged;
            CancelAllOpenDocumentTrackingTasks();
            RenameTrackingDismisser.DismissRenameTracking(_workspace, _workspace.GetOpenDocumentIds());
            _inlineRenameSessionDurationLogBlock.Dispose();
        }

        private void CancelAllOpenDocumentTrackingTasks()
        {
            _cancellationTokenSource.Cancel();
            _conflictResolutionTaskCancellationSource.Cancel();
        }

        private void CommitCore(IWaitContext waitContext, bool previewChanges)
        {
            var eventName = previewChanges ? FunctionId.Rename_CommitCoreWithPreview : FunctionId.Rename_CommitCore;
            using (Logger.LogBlock(eventName, KeyValueLogMessage.Create(LogType.UserAction), waitContext.CancellationToken))
            {
                _conflictResolutionTask.Wait(waitContext.CancellationToken);
                waitContext.AllowCancel = false;

                Solution newSolution = _conflictResolutionTask.Result.NewSolution;
                if (previewChanges)
                {
                    var previewService = _workspace.Services.GetService<IPreviewDialogService>();

                    newSolution = previewService.PreviewChanges(
                        string.Format(EditorFeaturesResources.Preview_Changes_0, EditorFeaturesResources.Rename),
                        "vs.csharp.refactoring.rename",
                        string.Format(EditorFeaturesResources.Rename_0_to_1_colon, this.OriginalSymbolName, this.ReplacementText),
                        _renameInfo.FullDisplayName,
                        _renameInfo.Glyph,
                        _conflictResolutionTask.Result.NewSolution,
                        _triggerDocument.Project.Solution);

                    if (newSolution == null)
                    {
                        // User clicked cancel.
                        return;
                    }
                }

                // The user hasn't cancelled by now, so we're done waiting for them. Off to
                // rename!
                waitContext.Message = EditorFeaturesResources.Updating_files;

                Dismiss(rollbackTemporaryEdits: true);
                CancelAllOpenDocumentTrackingTasks();

                ApplyRename(newSolution, waitContext);

                LogRenameSession(RenameLogMessage.UserActionOutcome.Committed, previewChanges);

                EndRenameSession();
            }
        }

        private void ApplyRename(Solution newSolution, IWaitContext waitContext)
        {
            var changes = _baseSolution.GetChanges(newSolution);
            var changedDocumentIDs = changes.GetProjectChanges().SelectMany(c => c.GetChangedDocuments()).ToList();

            if (!_renameInfo.TryOnBeforeGlobalSymbolRenamed(_workspace, changedDocumentIDs, this.ReplacementText))
            {
                var notificationService = _workspace.Services.GetService<INotificationService>();
                notificationService.SendNotification(
                    EditorFeaturesResources.Rename_operation_was_cancelled_or_is_not_valid,
                    EditorFeaturesResources.Rename_Symbol,
                    NotificationSeverity.Error);
                return;
            }

            using (var undoTransaction = _workspace.OpenGlobalUndoTransaction(EditorFeaturesResources.Inline_Rename))
            {
                var finalSolution = newSolution.Workspace.CurrentSolution;
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
                        var root = newDocument.GetSyntaxRootSynchronously(waitContext.CancellationToken);
                        finalSolution = finalSolution.WithDocumentSyntaxRoot(id, root);
                    }
                    else
                    {
                        var newText = newDocument.GetTextAsync(waitContext.CancellationToken).WaitAndGetResult(waitContext.CancellationToken);
                        finalSolution = finalSolution.WithDocumentText(id, newText);
                    }

                    // Make sure to include any document rename as well
                    finalSolution = finalSolution.WithDocumentName(id, newDocument.Name);
                }

                if (_workspace.TryApplyChanges(finalSolution))
                {
                    // Since rename can apply file changes as well, and those file 
                    // changes can generate new document ids, include added documents
                    // as well as changed documents. This also ensures that any document
                    // that was removed is not included
                    var finalChanges = _workspace.CurrentSolution.GetChanges(_baseSolution);

                    var finalChangedIds = finalChanges
                            .GetProjectChanges()
                            .SelectMany(c => c.GetChangedDocuments().Concat(c.GetAddedDocuments()))
                            .ToList();

                    if (!_renameInfo.TryOnAfterGlobalSymbolRenamed(_workspace, finalChangedIds, this.ReplacementText))
                    {
                        var notificationService = _workspace.Services.GetService<INotificationService>();
                        notificationService.SendNotification(
                            EditorFeaturesResources.Rename_operation_was_not_properly_completed_Some_file_might_not_have_been_updated,
                            EditorFeaturesResources.Rename_Symbol,
                            NotificationSeverity.Information);
                    }

                    undoTransaction.Commit();
                }
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
    }
}
