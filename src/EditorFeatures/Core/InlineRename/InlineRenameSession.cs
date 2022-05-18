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
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.Editor.BackgroundWorkIndicator;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Undo;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.ApplicationInsights.Extensibility.Implementation;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    internal partial class InlineRenameSession : IInlineRenameSession, IFeatureController
    {
        private readonly Workspace _workspace;
        private readonly ITextBufferAssociatedViewService _textBufferAssociatedViewService;
        private readonly ITextBufferFactoryService _textBufferFactoryService;
        private readonly IFeatureService _featureService;
        private readonly IFeatureDisableToken _completionDisabledToken;
        private readonly IEnumerable<IRefactorNotifyService> _refactorNotifyServices;
        private readonly IAsynchronousOperationListener _asyncListener;
        private readonly Solution _baseSolution;
        private readonly Document _triggerDocument;
        private readonly SnapshotSpan _triggerSpan;
        private readonly ITextView _triggerView;
        private readonly IDisposable _inlineRenameSessionDurationLogBlock;
        private readonly IThreadingContext _threadingContext;
        public readonly InlineRenameService RenameService;

        private bool _dismissed;
        private bool _isApplyingEdit;
        private string _replacementText;
        private SymbolRenameOptions _options;
        private bool _previewChanges;
        private readonly Dictionary<ITextBuffer, OpenTextBufferManager> _openTextBuffers = new Dictionary<ITextBuffer, OpenTextBufferManager>();

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
        private readonly CancellationTokenSource _cancellationTokenSource = new();

        /// <summary>
        /// This task is a continuation of the <see cref="_allRenameLocationsTask"/> that is the result of computing
        /// the resolutions of the rename spans for the current replacementText.
        /// </summary>
        private Task<IInlineRenameReplacementInfo> _conflictResolutionTask;

        /// <summary>
        /// The cancellation source for <see cref="_conflictResolutionTask"/>.
        /// </summary>
        private CancellationTokenSource _conflictResolutionTaskCancellationSource = new();

        private readonly IInlineRenameInfo _renameInfo;

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
            ITextBufferAssociatedViewService textBufferAssociatedViewService,
            ITextBufferFactoryService textBufferFactoryService,
            IFeatureServiceFactory featureServiceFactory,
            IEnumerable<IRefactorNotifyService> refactorNotifyServices,
            IAsynchronousOperationListener asyncListener)
        {
            // This should always be touching a symbol since we verified that upon invocation
            _threadingContext = threadingContext;
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
            RenameService = renameService;
            _refactorNotifyServices = refactorNotifyServices;
            _asyncListener = asyncListener;
            _triggerSpan = triggerSpan;
            _triggerView = textBufferAssociatedViewService.GetAssociatedTextViews(triggerSpan.Snapshot.TextBuffer).FirstOrDefault(v => v.HasAggregateFocus) ??
                textBufferAssociatedViewService.GetAssociatedTextViews(triggerSpan.Snapshot.TextBuffer).First();

            _options = options;
            _previewChanges = previewChanges;

            _initialRenameText = triggerSpan.GetText();
            this.ReplacementText = _initialRenameText;

            _baseSolution = _triggerDocument.Project.Solution;
            this.UndoManager = workspace.Services.GetService<IInlineRenameUndoManager>();

            if (_renameInfo is IInlineRenameInfoWithFileRename renameInfoWithFileRename)
            {
                FileRenameInfo = renameInfoWithFileRename.GetFileRenameInfo();
            }
            else
            {
                FileRenameInfo = InlineRenameFileRenameInfo.NotAllowed;
            }

            InitializeOpenBuffers(triggerSpan);
        }

        public string OriginalSymbolName => _renameInfo.DisplayName;

        // Used to aid the investigation of https://github.com/dotnet/roslyn/issues/7364
        private class NullTextBufferException : Exception
        {
#pragma warning disable IDE0052 // Remove unread private members
            private readonly Document _document;
            private readonly SourceText _text;
#pragma warning restore IDE0052 // Remove unread private members

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
                var openBuffers = new HashSet<ITextBuffer>();
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
            _openTextBuffers[triggerSpan.Snapshot.TextBuffer].SetReferenceSpans(SpecializedCollections.SingletonEnumerable(startingSpan.ToTextSpan()));

            UpdateReferenceLocationsTask(SwitchToBackgroundAndFindRenameLocationsAsync());

            _threadingContext.JoinableTaskFactory.Run(() =>
                RenameTrackingDismisser.DismissRenameTrackingAsync(
                    _threadingContext, _workspace, _workspace.GetOpenDocumentIds(), CancellationToken.None));
        }

        private async Task<IInlineRenameLocationSet> SwitchToBackgroundAndFindRenameLocationsAsync()
        {
            await TaskScheduler.Default;
            return await _renameInfo.FindRenameLocationsAsync(_options, _cancellationTokenSource.Token).ConfigureAwait(false);
        }

        private bool TryPopulateOpenTextBufferManagerForBuffer(ITextBuffer buffer)
        {
            _threadingContext.ThrowIfNotOnUIThread();
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
            _threadingContext.ThrowIfNotOnUIThread();
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

        private void UpdateReferenceLocationsTask(Task<IInlineRenameLocationSet> findRenameLocationsTask)
        {
            _threadingContext.ThrowIfNotOnUIThread();

            var asyncToken = _asyncListener.BeginAsyncOperation("UpdateReferencesTask");
            _allRenameLocationsTask = GetAllRenameLocationsAsync(findRenameLocationsTask);

            _allRenameLocationsTask.CompletesAsyncOperation(asyncToken);

            UpdateConflictResolutionTask();
            QueueApplyReplacements();
        }

        private async Task<IInlineRenameLocationSet> GetAllRenameLocationsAsync(Task<IInlineRenameLocationSet> findRenameLocationsTask)
        {
            var cancellationToken = _cancellationTokenSource.Token;
            var inlineRenameLocations = await findRenameLocationsTask.ConfigureAwaitRunInline();

            // It's unfortunate that _allRenameLocationsTask has a UI thread dependency (prevents continuations
            // from running prior to the completion of the UI operation), but the implementation does not currently
            // follow the originally-intended design.
            // https://github.com/dotnet/roslyn/issues/40890
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(alwaysYield: true, cancellationToken);

            RaiseSessionSpansUpdated(inlineRenameLocations.Locations.ToImmutableArray());

            return inlineRenameLocations;
        }

        public Workspace Workspace => _workspace;
        public SymbolRenameOptions Options => _options;
        public bool PreviewChanges => _previewChanges;
        public bool HasRenameOverloads => _renameInfo.HasOverloads;
        public bool MustRenameOverloads => _renameInfo.MustRenameOverloads;

        public IInlineRenameUndoManager UndoManager { get; }

        public event EventHandler<ImmutableArray<InlineRenameLocation>> ReferenceLocationsChanged;
        public event EventHandler<IInlineRenameReplacementInfo> ReplacementsComputed;
        public event EventHandler ReplacementTextChanged;

        internal OpenTextBufferManager GetBufferManager(ITextBuffer buffer)
            => _openTextBuffers[buffer];

        internal bool TryGetBufferManager(ITextBuffer buffer, out OpenTextBufferManager bufferManager)
            => _openTextBuffers.TryGetValue(buffer, out bufferManager);

        public void RefreshRenameSessionWithOptionsChanged(SymbolRenameOptions newOptions)
        {
            if (_options == newOptions)
            {
                return;
            }

            _threadingContext.ThrowIfNotOnUIThread();
            VerifyNotDismissed();

            _options = newOptions;

            UpdateReferenceLocationsTask(FindRenameLocationsAsync());
        }

        private async Task<IInlineRenameLocationSet> FindRenameLocationsAsync()
        {
            // Await prior work before proceeding, since it performs a required state update.
            // https://github.com/dotnet/roslyn/pull/34254#discussion_r267024593
            await _allRenameLocationsTask.ConfigureAwait(false);

            await TaskScheduler.Default;

            return await _renameInfo.FindRenameLocationsAsync(_options, _cancellationTokenSource.Token).ConfigureAwait(false);
        }

        public void SetPreviewChanges(bool value)
        {
            _threadingContext.ThrowIfNotOnUIThread();
            VerifyNotDismissed();

            _previewChanges = value;
        }

        private async Task DismissAsync(bool rollbackTemporaryEdits, CancellationToken cancellationToken)
        {
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

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

            RenameService.ActiveSession = null;
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
            _threadingContext.ThrowIfNotOnUIThread();
            SetReferenceLocations(locations);

            // It's OK to call SetReferenceLocations with all documents, including unchangeable ones,
            // because they can't be opened, so the _openTextBuffers loop won't matter. In fact, the entire
            // inline rename is oblivious to unchangeable documents, we just need to filter out references
            // in them to avoid displaying them in the UI.
            // https://github.com/dotnet/roslyn/issues/41242
            if (_workspace.IgnoreUnchangeableDocumentsWhenApplyingChanges)
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
            _threadingContext.ThrowIfNotOnUIThread();
            VerifyNotDismissed();
            this.ReplacementText = _renameInfo.GetFinalSymbolName(replacementText);

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
            var options = _options;

            var asyncToken = _asyncListener.BeginAsyncOperation(nameof(UpdateConflictResolutionTask));

            _conflictResolutionTask = GetConflictResolutionAsync(
                replacementText, options, _conflictResolutionTaskCancellationSource.Token);

            _conflictResolutionTask.CompletesAsyncOperation(asyncToken);
        }

        private async Task<IInlineRenameReplacementInfo> GetConflictResolutionAsync(
            string replacementText, SymbolRenameOptions options, CancellationToken cancellationToken)
        {
            // Await prior work before proceeding, since it performs a required state update.
            // https://github.com/dotnet/roslyn/pull/34254#discussion_r267024593
            //
            // If cancellation of the conflict resolution task is requested before the rename locations task
            // completes, we do not need to wait for rename before cancelling. The next conflict resolution task
            // will wait on the latest rename location task if/when necessary.
            var result = await _allRenameLocationsTask.WithCancellation(cancellationToken).ConfigureAwait(false);
            await TaskScheduler.Default;

            return await result.GetReplacementsAsync(replacementText, options, cancellationToken).ConfigureAwait(false);
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

            var asyncToken = _asyncListener.BeginAsyncOperation(nameof(QueueApplyReplacements));
            var replacementTask = ApplyReplacementsAsync(_conflictResolutionTaskCancellationSource.Token);
            replacementTask.CompletesAsyncOperation(asyncToken);
        }

        private async Task ApplyReplacementsAsync(CancellationToken cancellationToken)
        {
            var replacementInfo = await _conflictResolutionTask.WithCancellation(cancellationToken).ConfigureAwait(false);
            if (replacementInfo == null || cancellationToken.IsCancellationRequested)
            {
                return;
            }

            // Switch to a background thread for expensive work
            await TaskScheduler.Default;
            var computedMergeResult = await ComputeMergeResultAsync(replacementInfo, cancellationToken).ConfigureAwait(false);
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(alwaysYield: true, cancellationToken);
            ApplyReplacements(computedMergeResult.replacementInfo, computedMergeResult.mergeResult, cancellationToken);
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

            var conflictResolutionFinishedComputing = _conflictResolutionTask.Status == TaskStatus.RanToCompletion;

            if (conflictResolutionFinishedComputing)
            {
                var result = _conflictResolutionTask.Result;
                var replacementKinds = result.GetAllReplacementKinds().ToList();

                Logger.Log(FunctionId.Rename_InlineSession_Session, RenameLogMessage.Create(
                    _options,
                    outcome,
                    conflictResolutionFinishedComputing,
                    previewChanges,
                    replacementKinds));
            }
            else
            {
                Debug.Assert(outcome.HasFlag(RenameLogMessage.UserActionOutcome.Canceled));
                Logger.Log(FunctionId.Rename_InlineSession_Session, RenameLogMessage.Create(
                    _options,
                    outcome,
                    conflictResolutionFinishedComputing,
                    previewChanges,
                    SpecializedCollections.EmptyList<InlineRenameReplacementKind>()));
            }
        }

        public void Cancel()
            => Cancel(rollbackTemporaryEdits: true);

        private void Cancel(bool rollbackTemporaryEdits)
            => _threadingContext.JoinableTaskFactory.Run(() => CancelAsync(rollbackTemporaryEdits, CancellationToken.None));

        private async Task CancelAsync(bool rollbackTemporaryEdits, CancellationToken cancellationToken)
        {
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            VerifyNotDismissed();

            LogRenameSession(RenameLogMessage.UserActionOutcome.Canceled, previewChanges: false);
            await DismissAsync(rollbackTemporaryEdits, cancellationToken).ConfigureAwait(false);
            await EndRenameSessionAsync(cancellationToken).ConfigureAwait(false);
        }

        public void Commit(bool previewChanges = false)
            => CommitWorker(previewChanges);

        /// <returns><see langword="true"/> if the rename operation was commited, <see
        /// langword="false"/> otherwise</returns>
        private bool CommitWorker(bool previewChanges)
        {
            return _threadingContext.JoinableTaskFactory.Run(() => CommitWorkerAsync(previewChanges));
        }

        public Task CommitAsync(bool previewChanges = false)
            => CommitWorkerAsync(previewChanges);

        private async Task<bool> CommitWorkerAsync(bool previewChanges)
        {
            _threadingContext.ThrowIfNotOnUIThread();

            var indicatorFactory = _workspace.Services.GetRequiredService<IBackgroundWorkIndicatorFactory>();

            using var context = indicatorFactory.Create(
                _triggerView, _triggerSpan, EditorFeaturesResources.Computing_Rename_information,
                cancelOnEdit: true, cancelOnFocusLost: false);

            using var asyncToken = _asyncListener.BeginAsyncOperation(nameof(CommitWorkerAsync));

            var cancellationToken = context.UserCancellationToken;
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
                // We're about to rollback.  Make sure that the rollback doesn't trigger our own cancellation.
                context.CancelOnEdit = false;
                await CancelAsync(rollbackTemporaryEdits: true, cancellationToken).ConfigureAwait(false);
                return false;
            }

            previewChanges = previewChanges || _previewChanges;

            var canceled = false;

            try
            {
                await CommitCoreAsync(context, previewChanges).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                canceled = true;
            }

            if (canceled || context.UserCancellationToken.IsCancellationRequested)
            {
                LogRenameSession(RenameLogMessage.UserActionOutcome.Canceled | RenameLogMessage.UserActionOutcome.Committed, previewChanges);
                await DismissAsync(rollbackTemporaryEdits: true, cancellationToken).ConfigureAwait(false);
                await EndRenameSessionAsync(cancellationToken).ConfigureAwait(false);

                return false;
            }

            return true;
        }

        private async Task EndRenameSessionAsync(CancellationToken cancellationToken)
        {
            CancelAllOpenDocumentTrackingTasks();
            await RenameTrackingDismisser.DismissRenameTrackingAsync(
                _threadingContext, _workspace, _workspace.GetOpenDocumentIds(), cancellationToken).ConfigureAwait(false);
            _inlineRenameSessionDurationLogBlock.Dispose();
        }

        private void CancelAllOpenDocumentTrackingTasks()
        {
            _cancellationTokenSource.Cancel();
            _conflictResolutionTaskCancellationSource.Cancel();
        }

        private async Task CommitCoreAsync(IBackgroundWorkIndicatorContext workContext, bool previewChanges)
        {
            var cancellationToken = workContext.UserCancellationToken;

            var eventName = previewChanges ? FunctionId.Rename_CommitCoreWithPreview : FunctionId.Rename_CommitCore;
            using (Logger.LogBlock(eventName, KeyValueLogMessage.Create(LogType.UserAction), cancellationToken))
            {
                var conflictResolutionResult = await _conflictResolutionTask.WithCancellation(cancellationToken).ConfigureAwait(false);
                var newSolution = conflictResolutionResult.NewSolution;

                if (previewChanges)
                {
                    var previewService = _workspace.Services.GetService<IPreviewDialogService>();

                    await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                    newSolution = previewService.PreviewChanges(
                        string.Format(EditorFeaturesResources.Preview_Changes_0, EditorFeaturesResources.Rename),
                        "vs.csharp.refactoring.rename",
                        string.Format(EditorFeaturesResources.Rename_0_to_1_colon, this.OriginalSymbolName, this.ReplacementText),
                        _renameInfo.FullDisplayName,
                        _renameInfo.Glyph,
                        newSolution,
                        _triggerDocument.Project.Solution);

                    if (newSolution == null)
                    {
                        // User clicked cancel.
                        return;
                    }
                }

                // The user hasn't cancelled by now, so we're done waiting for them. Off to
                // rename!
                using var _ = workContext.AddScope(allowCancellation: false, EditorFeaturesResources.Updating_files);

                // we're about to make changes ourselves.  makes sure that the edits we make don't cause us to cancel ourselves.
                workContext.CancelOnEdit = false;

                await DismissAsync(rollbackTemporaryEdits: true, cancellationToken).ConfigureAwait(false);
                CancelAllOpenDocumentTrackingTasks();

                await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                _triggerView.Caret.PositionChanged += LogPositionChanged;

                await ApplyRenameAsync(newSolution, cancellationToken).ConfigureAwait(false);

                LogRenameSession(RenameLogMessage.UserActionOutcome.Committed, previewChanges);

                await EndRenameSessionAsync(cancellationToken).ConfigureAwait(false);

                await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                _triggerView.Caret.PositionChanged -= LogPositionChanged;

                return;

                void LogPositionChanged(object sender, CaretPositionChangedEventArgs e)
                {
                    try
                    {
                        throw new InvalidOperationException("Caret position changed during application of rename");
                    }
                    catch (InvalidOperationException ex) when (FatalError.ReportAndCatch(ex))
                    {
                        // Unreachable code due to ReportAndCatch
                        Contract.ThrowIfTrue(true);
                    }
                }
            }
        }

        private async Task ApplyRenameAsync(
            Solution newSolution, CancellationToken cancellationToken)
        {
            var changes = _baseSolution.GetChanges(newSolution);
            var changedDocumentIDs = changes.GetProjectChanges().SelectMany(c => c.GetChangedDocuments()).ToList();

            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            if (!_renameInfo.TryOnBeforeGlobalSymbolRenamed(_workspace, changedDocumentIDs, this.ReplacementText))
            {
                var notificationService = _workspace.Services.GetService<INotificationService>();
                notificationService.SendNotification(
                    EditorFeaturesResources.Rename_operation_was_cancelled_or_is_not_valid,
                    EditorFeaturesResources.Rename_Symbol,
                    NotificationSeverity.Error);
                return;
            }

            using var undoTransaction = _workspace.OpenGlobalUndoTransaction(EditorFeaturesResources.Inline_Rename);

            await TaskScheduler.Default;
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
                    // We pass CancellationToken.None here because we don't have a usable token to pass. The IUIThreadOperationContext
                    // passed here as a cancellation token, but the caller in CommitCore has already turned off cancellation
                    // because we're committed to the update at this point. If we ever want to pass cancellation here, we'd want to move this
                    // part back out of this method and before the point where we've already opened a global transaction.
                    var root = newDocument.GetSyntaxRootSynchronously(CancellationToken.None);
                    finalSolution = finalSolution.WithDocumentSyntaxRoot(id, root);
                }
                else
                {
                    var newText = newDocument.GetTextSynchronously(CancellationToken.None);
                    finalSolution = finalSolution.WithDocumentText(id, newText);
                }

                // Make sure to include any document rename as well
                finalSolution = finalSolution.WithDocumentName(id, newDocument.Name);
            }

            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
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

                await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
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

        public struct TestAccessor
        {
            private readonly InlineRenameSession _inlineRenameSession;

            public TestAccessor(InlineRenameSession inlineRenameSession)
                => _inlineRenameSession = inlineRenameSession;

            public bool CommitWorker(bool previewChanges)
                => _inlineRenameSession.CommitWorker(previewChanges);
        }
    }
}
