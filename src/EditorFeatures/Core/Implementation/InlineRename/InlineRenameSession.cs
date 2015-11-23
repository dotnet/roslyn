// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Undo;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    internal partial class InlineRenameSession : ForegroundThreadAffinitizedObject, IInlineRenameSession
    {
        private readonly Workspace _workspace;
        private readonly InlineRenameService _renameService;
        private readonly IWaitIndicator _waitIndicator;
        private readonly ITextBufferAssociatedViewService _textBufferAssociatedViewService;
        private readonly ITextBufferFactoryService _textBufferFactoryService;
        private readonly IEnumerable<IRefactorNotifyService> _refactorNotifyServices;
        private readonly IEditAndContinueWorkspaceService _editAndContinueWorkspaceService;
        private readonly IAsynchronousOperationListener _asyncListener;
        private readonly Solution _baseSolution;
        private readonly Document _triggerDocument;
        private readonly IWpfTextView _triggerView;
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
            InlineRenameService renameService,
            Workspace workspace,
            SnapshotSpan triggerSpan,
            IInlineRenameInfo renameInfo,
            IWaitIndicator waitIndicator,
            ITextBufferAssociatedViewService textBufferAssociatedViewService,
            ITextBufferFactoryService textBufferFactoryService,
            IEnumerable<IRefactorNotifyService> refactorNotifyServices,
            IAsynchronousOperationListener asyncListener) : base(assertIsForeground: true)
        {
            // This should always be touching a symbol since we verified that upon invocation
            _renameInfo = renameInfo;

            _triggerDocument = triggerSpan.Snapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (_triggerDocument == null)
            {
                throw new InvalidOperationException(EditorFeaturesResources.TheTriggerspanIsNotIncludedInWorkspace);
            }

            _inlineRenameSessionDurationLogBlock = Logger.LogBlock(FunctionId.Rename_InlineSession, CancellationToken.None);

            _workspace = workspace;
            _workspace.WorkspaceChanged += OnWorkspaceChanged;

            _textBufferFactoryService = textBufferFactoryService;
            _textBufferAssociatedViewService = textBufferAssociatedViewService;
            _textBufferAssociatedViewService.SubjectBuffersConnected += OnSubjectBuffersConnected;

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

            _editAndContinueWorkspaceService = workspace.Services.GetService<IEditAndContinueWorkspaceService>();
            _editAndContinueWorkspaceService.BeforeDebuggingStateChanged += OnBeforeDebuggingStateChanged;

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

        public string OriginalSymbolName
        {
            get
            {
                return _renameInfo.DisplayName;
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
                    SourceText text;
                    Contract.ThrowIfFalse(document.TryGetText(out text));
                    Contract.ThrowIfNull(text);

                    var textSnapshot = text.FindCorrespondingEditorTextSnapshot();
                    Contract.ThrowIfNull(textSnapshot);
                    Contract.ThrowIfNull(textSnapshot.TextBuffer);

                    openBuffers.Add(textSnapshot.TextBuffer);
                }

                foreach (var buffer in openBuffers)
                {
                    TryPopulateOpenTextBufferManagerForBuffer(buffer, buffer.AsTextContainer().GetRelatedDocuments());
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

        private bool TryPopulateOpenTextBufferManagerForBuffer(ITextBuffer buffer, IEnumerable<Document> documents)
        {
            AssertIsForeground();
            VerifyNotDismissed();

            if (_workspace.Kind == WorkspaceKind.Interactive)
            {
                Debug.Assert(documents.Count() == 1); // No linked files.
                Debug.Assert(buffer.IsReadOnly(0) == buffer.IsReadOnly(Span.FromBounds(0, buffer.CurrentSnapshot.Length))); // All or nothing.
                if (buffer.IsReadOnly(0))
                {
                    return false;
                }
            }

            var documentSupportsFeatureService = _workspace.Services.GetService<IDocumentSupportsFeatureService>();

            if (!_openTextBuffers.ContainsKey(buffer) && documents.All(d => documentSupportsFeatureService.SupportsRename(d)))
            {
                _openTextBuffers[buffer] = new OpenTextBufferManager(this, buffer, _workspace, documents, _textBufferFactoryService);
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
                    var documents = buffer.AsTextContainer().GetRelatedDocuments();
                    if (TryPopulateOpenTextBufferManagerForBuffer(buffer, documents))
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
            allRenameLocationsTask.SafeContinueWith(
                t => RaiseSessionSpansUpdated(t.Result.Locations),
                _cancellationTokenSource.Token,
                TaskContinuationOptions.OnlyOnRanToCompletion,
                ForegroundTaskScheduler).CompletesAsyncOperation(asyncToken);

            UpdateConflictResolutionTask();
        }

        public Workspace Workspace { get { return _workspace; } }
        public OptionSet OptionSet { get { return _optionSet; } }
        public bool HasRenameOverloads { get { return _renameInfo.HasOverloads; } }
        public bool ForceRenameOverloads { get { return _renameInfo.ForceRenameOverloads; } }

        public IInlineRenameUndoManager UndoManager { get; }

        public event EventHandler<IList<InlineRenameLocation>> ReferenceLocationsChanged;
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
                throw new InvalidOperationException(EditorFeaturesResources.ThisSessionHasAlreadyBeenDismissed);
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

        private void RaiseSessionSpansUpdated(IList<InlineRenameLocation> locations)
        {
            AssertIsForeground();
            SetReferenceLocations(locations);
            ReferenceLocationsChanged?.Invoke(this, locations);
        }

        private void SetReferenceLocations(IEnumerable<InlineRenameLocation> locations)
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
            this.ReplacementText = replacementText;

            Action propagateEditAction = delegate
            {
                if (_dismissed)
                {
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
            };

            if (propagateEditImmediately)
            {
                propagateEditAction();
            }
            else
            {
                // When responding to a text edit, we delay propagating the edit until the first transaction completes.
                Dispatcher.CurrentDispatcher.BeginInvoke(propagateEditAction, DispatcherPriority.Send, null);
            }

            UpdateConflictResolutionTask();
        }

        private void UpdateConflictResolutionTask()
        {
            AssertIsForeground();

            _conflictResolutionTaskCancellationSource.Cancel();
            _conflictResolutionTaskCancellationSource = new CancellationTokenSource();

            if (this.ReplacementText == string.Empty)
            {
                return;
            }

            var replacementText = this.ReplacementText;
            var optionSet = _optionSet;
            var cancellationToken = _conflictResolutionTaskCancellationSource.Token;

            _conflictResolutionTask = _allRenameLocationsTask.SafeContinueWithFromAsync(
               t => UpdateConflictResolutionTask(t.Result, replacementText, optionSet, cancellationToken),
               cancellationToken,
               TaskContinuationOptions.OnlyOnRanToCompletion,
               TaskScheduler.Default);

            var asyncToken = _asyncListener.BeginAsyncOperation("UpdateConflictResolutionTask");
            _conflictResolutionTask.CompletesAsyncOperation(asyncToken);
        }

        private Task<IInlineRenameReplacementInfo> UpdateConflictResolutionTask(IInlineRenameLocationSet locations, string replacementText, OptionSet optionSet, CancellationToken cancellationToken)
        {
            var conflictResolutionTask = Task.Run(async () =>
                await locations.GetReplacementsAsync(replacementText, optionSet, cancellationToken).ConfigureAwait(false),
                cancellationToken);

            var asyncToken = _asyncListener.BeginAsyncOperation("UpdateConflictResolutionTask");
            conflictResolutionTask.SafeContinueWith(
                t => ApplyReplacements(t.Result, cancellationToken),
                cancellationToken,
                TaskContinuationOptions.OnlyOnRanToCompletion,
                ForegroundTaskScheduler).CompletesAsyncOperation(asyncToken);

            return conflictResolutionTask;
        }

        private void ApplyReplacements(IInlineRenameReplacementInfo replacementInfo, CancellationToken cancellationToken)
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
                    textBufferManager.ApplyConflictResolutionEdits(replacementInfo, documents, cancellationToken);
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
                Contract.Assert(outcome.HasFlag(RenameLogMessage.UserActionOutcome.Canceled));
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
                message: EditorFeaturesResources.ComputingRenameInformation,
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
            _editAndContinueWorkspaceService.BeforeDebuggingStateChanged -= OnBeforeDebuggingStateChanged;
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
            using (Logger.LogBlock(previewChanges ? FunctionId.Rename_CommitCoreWithPreview : FunctionId.Rename_CommitCore, waitContext.CancellationToken))
            {
                _conflictResolutionTask.Wait(waitContext.CancellationToken);
                waitContext.AllowCancel = false;

                Solution newSolution = _conflictResolutionTask.Result.NewSolution;
                if (previewChanges)
                {
                    var previewService = _workspace.Services.GetService<IPreviewDialogService>();

                    newSolution = previewService.PreviewChanges(
                        string.Format(EditorFeaturesResources.PreviewChangesOf, EditorFeaturesResources.Rename),
                        "vs.csharp.refactoring.rename",
                        string.Format(EditorFeaturesResources.RenameToTitle, this.OriginalSymbolName, _renameInfo.GetFinalSymbolName(this.ReplacementText)),
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
                waitContext.Message = EditorFeaturesResources.UpdatingFiles;

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
                    EditorFeaturesResources.RenameOperationWasCancelled,
                    EditorFeaturesResources.RenameSymbol,
                    NotificationSeverity.Error);
                return;
            }

            using (var undoTransaction = _workspace.OpenGlobalUndoTransaction(EditorFeaturesResources.FontAndColors_InlineRename))
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
                        var root = newDocument.GetSyntaxRootAsync(waitContext.CancellationToken).WaitAndGetResult(waitContext.CancellationToken);
                        finalSolution = finalSolution.WithDocumentSyntaxRoot(id, root);
                    }
                    else
                    {
                        var newText = newDocument.GetTextAsync(waitContext.CancellationToken).WaitAndGetResult(waitContext.CancellationToken);
                        finalSolution = finalSolution.WithDocumentText(id, newText);
                    }
                }

                if (_workspace.TryApplyChanges(finalSolution))
                {
                    if (!_renameInfo.TryOnAfterGlobalSymbolRenamed(_workspace, changedDocumentIDs, this.ReplacementText))
                    {
                        var notificationService = _workspace.Services.GetService<INotificationService>();
                        notificationService.SendNotification(
                            EditorFeaturesResources.RenameOperationWasNotProperlyCompleted,
                            EditorFeaturesResources.RenameSymbol,
                            NotificationSeverity.Information);
                    }

                    undoTransaction.Commit();
                }
            }
        }

        internal bool TryGetContainingEditableSpan(SnapshotPoint point, out SnapshotSpan editableSpan)
        {
            editableSpan = default(SnapshotSpan);

            OpenTextBufferManager bufferManager;
            if (!_openTextBuffers.TryGetValue(point.Snapshot.TextBuffer, out bufferManager))
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
