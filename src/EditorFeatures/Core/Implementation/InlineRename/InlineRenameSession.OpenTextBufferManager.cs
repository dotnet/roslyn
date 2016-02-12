// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    internal partial class InlineRenameSession
    {
        /// <summary>
        /// Manages state for open text buffers.
        /// </summary>
        internal class OpenTextBufferManager : ForegroundThreadAffinitizedObject
        {
            private readonly DynamicReadOnlyRegionQuery _isBufferReadOnly;
            private readonly InlineRenameSession _session;
            private readonly ITextBuffer _subjectBuffer;
            private readonly IEnumerable<Document> _baseDocuments;
            private readonly ITextBufferFactoryService _textBufferFactoryService;

            private static readonly object s_propagateSpansEditTag = new object();
            private static readonly object s_calculateMergedSpansEditTag = new object();

            /// <summary>
            /// The list of active tracking spans that are updated with the session's replacement text.
            /// These are also the only spans the user can edit during an inline rename session.
            /// </summary>
            private readonly Dictionary<TextSpan, RenameTrackingSpan> _referenceSpanToLinkedRenameSpanMap = new Dictionary<TextSpan, RenameTrackingSpan>();

            private readonly List<RenameTrackingSpan> _conflictResolutionRenameTrackingSpans = new List<RenameTrackingSpan>();
            private readonly IList<IReadOnlyRegion> _readOnlyRegions = new List<IReadOnlyRegion>();

            private readonly IList<IWpfTextView> _textViews = new List<IWpfTextView>();

            private TextSpan? _activeSpan;

            public OpenTextBufferManager(
                InlineRenameSession session,
                ITextBuffer subjectBuffer,
                Workspace workspace,
                IEnumerable<Document> documents,
                ITextBufferFactoryService textBufferFactoryService)
            {
                _session = session;
                _subjectBuffer = subjectBuffer;
                _baseDocuments = documents;
                _textBufferFactoryService = textBufferFactoryService;
                _subjectBuffer.ChangedLowPriority += OnTextBufferChanged;

                foreach (var view in session._textBufferAssociatedViewService.GetAssociatedTextViews(_subjectBuffer))
                {
                    ConnectToView(view);
                }

                session.UndoManager.CreateStartRenameUndoTransaction(workspace, subjectBuffer, session);

                _isBufferReadOnly = new DynamicReadOnlyRegionQuery((isEdit) => !_session._isApplyingEdit);
                UpdateReadOnlyRegions();
            }

            public IWpfTextView ActiveTextview
            {
                get
                {
                    foreach (var view in _textViews)
                    {
                        if (view.HasAggregateFocus)
                        {
                            return view;
                        }
                    }

                    return _textViews.FirstOrDefault();
                }
            }

            private void UpdateReadOnlyRegions(bool removeOnly = false)
            {
                AssertIsForeground();
                if (!removeOnly && _session.ReplacementText == string.Empty)
                {
                    return;
                }

                using (var readOnlyEdit = _subjectBuffer.CreateReadOnlyRegionEdit())
                {
                    foreach (var oldReadOnlyRegion in _readOnlyRegions)
                    {
                        readOnlyEdit.RemoveReadOnlyRegion(oldReadOnlyRegion);
                    }

                    _readOnlyRegions.Clear();

                    if (!removeOnly)
                    {
                        // We will compute the new read only regions to be all spans that are not currently in an editable span
                        var editableSpans = GetEditableSpansForSnapshot(_subjectBuffer.CurrentSnapshot);
                        var entireBufferSpan = _subjectBuffer.CurrentSnapshot.GetSnapshotSpanCollection();
                        var newReadOnlySpans = NormalizedSnapshotSpanCollection.Difference(entireBufferSpan, new NormalizedSnapshotSpanCollection(editableSpans));

                        foreach (var newReadOnlySpan in newReadOnlySpans)
                        {
                            _readOnlyRegions.Add(readOnlyEdit.CreateDynamicReadOnlyRegion(newReadOnlySpan, SpanTrackingMode.EdgeExclusive, EdgeInsertionMode.Allow, _isBufferReadOnly));
                        }

                        // The spans we added allow typing at the start and end.  We'll add extra
                        // zero-width read-only regions at the start and end of the file to fix this,
                        // but only if we don't have an identifier at the start or end that _would_ let
                        // them type there.
                        if (editableSpans.All(s => s.Start > 0))
                        {
                            _readOnlyRegions.Add(readOnlyEdit.CreateDynamicReadOnlyRegion(new Span(0, 0), SpanTrackingMode.EdgeExclusive, EdgeInsertionMode.Deny, _isBufferReadOnly));
                        }

                        if (editableSpans.All(s => s.End < _subjectBuffer.CurrentSnapshot.Length))
                        {
                            _readOnlyRegions.Add(readOnlyEdit.CreateDynamicReadOnlyRegion(new Span(_subjectBuffer.CurrentSnapshot.Length, 0), SpanTrackingMode.EdgeExclusive, EdgeInsertionMode.Deny, _isBufferReadOnly));
                        }
                    }

                    readOnlyEdit.Apply();
                }
            }

            private void OnTextViewClosed(object sender, EventArgs e)
            {
                var view = sender as IWpfTextView;
                view.Closed -= OnTextViewClosed;
                _textViews.Remove(view);
                if (!_session._dismissed)
                {
                    _session.Cancel();
                }
            }

            internal void ConnectToView(IWpfTextView textView)
            {
                textView.Closed += OnTextViewClosed;
                _textViews.Add(textView);
            }

            public event Action SpansChanged;

            private void RaiseSpansChanged()
            {
                var handler = this.SpansChanged;
                if (handler != null)
                {
                    handler();
                }
            }

            internal IEnumerable<RenameTrackingSpan> GetRenameTrackingSpans()
            {
                return _referenceSpanToLinkedRenameSpanMap.Values.Where(r => r.Type != RenameSpanKind.None).Concat(_conflictResolutionRenameTrackingSpans);
            }

            internal IEnumerable<SnapshotSpan> GetEditableSpansForSnapshot(ITextSnapshot snapshot)
            {
                return _referenceSpanToLinkedRenameSpanMap.Values.Where(r => r.Type != RenameSpanKind.None).Select(r => r.TrackingSpan.GetSpan(snapshot));
            }

            internal void SetReferenceSpans(IEnumerable<TextSpan> spans)
            {
                AssertIsForeground();

                if (spans.SetEquals(_referenceSpanToLinkedRenameSpanMap.Keys))
                {
                    return;
                }

                using (new SelectionTracking(this))
                {
                    // Revert any previous edits in case we're removing spans.  Undo conflict resolution as well to avoid 
                    // handling the various edge cases where a tracking span might not map to the right span in the current snapshot
                    _session.UndoManager.UndoTemporaryEdits(_subjectBuffer, disconnect: false);

                    _referenceSpanToLinkedRenameSpanMap.Clear();
                    foreach (var span in spans)
                    {
                        var renameableSpan = _session._renameInfo.GetReferenceEditSpan(
                            new InlineRenameLocation(_baseDocuments.First(), span), CancellationToken.None);
                        var trackingSpan = new RenameTrackingSpan(
                                _subjectBuffer.CurrentSnapshot.CreateTrackingSpan(renameableSpan.ToSpan(), SpanTrackingMode.EdgeInclusive, TrackingFidelityMode.Forward),
                                RenameSpanKind.Reference);

                        _referenceSpanToLinkedRenameSpanMap[span] = trackingSpan;
                    }

                    _activeSpan = _activeSpan.HasValue && spans.Contains(_activeSpan.Value)
                        ? _activeSpan
                        : spans.Where(s => ActiveTextview.GetSpanInView(_subjectBuffer.CurrentSnapshot.GetSpan(s.ToSpan())).Count != 0)
                            .FirstOrNullable(); // filter to spans that have a projection

                    UpdateReadOnlyRegions();
                    this.ApplyReplacementText(updateSelection: false);
                }

                RaiseSpansChanged();
            }

            private void OnTextBufferChanged(object sender, TextContentChangedEventArgs args)
            {
                AssertIsForeground();

                // This might be an event fired due to our own edit
                if (args.EditTag == s_propagateSpansEditTag || _session._isApplyingEdit)
                {
                    return;
                }

                using (Logger.LogBlock(FunctionId.Rename_OnTextBufferChanged, CancellationToken.None))
                {
                    var trackingSpansAfterEdit = new NormalizedSpanCollection(GetEditableSpansForSnapshot(args.After).Select(ss => (Span)ss));
                    var spansTouchedInEdit = new NormalizedSpanCollection(args.Changes.Select(c => c.NewSpan));

                    var intersectionSpans = NormalizedSpanCollection.Intersection(trackingSpansAfterEdit, spansTouchedInEdit);
                    if (intersectionSpans.Count == 0)
                    {
                        // In Razor we sometimes get formatting changes during inline rename that
                        // do not intersect with any of our spans. Ideally this shouldn't happen at
                        // all, but if it does happen we can just ignore it.
                        return;
                    }

                    // Cases with invalid identifiers may cause there to be multiple intersection
                    // spans, but they should still all map to a single tracked rename span (e.g.
                    // renaming "two" to "one two three" may be interpreted as two distinct
                    // additions of "one" and "three").
                    var boundingIntersectionSpan = Span.FromBounds(intersectionSpans.First().Start, intersectionSpans.Last().End);
                    var trackingSpansTouched = GetEditableSpansForSnapshot(args.After).Where(ss => ss.IntersectsWith(boundingIntersectionSpan));
                    Contract.Assert(trackingSpansTouched.Count() == 1);

                    var singleTrackingSpanTouched = trackingSpansTouched.Single();
                    _activeSpan = _referenceSpanToLinkedRenameSpanMap.Where(kvp => kvp.Value.TrackingSpan.GetSpan(args.After).Contains(boundingIntersectionSpan)).Single().Key;
                    _session.UndoManager.OnTextChanged(this.ActiveTextview.Selection, singleTrackingSpanTouched);
                }
            }

            /// <summary>
            /// This is a work around for a bug in Razor where the projection spans can get out-of-sync with the
            /// identifiers.  When that bug is fixed this helper can be deleted.
            /// </summary>
            private bool AreAllReferenceSpansMappable()
            {
                return _referenceSpanToLinkedRenameSpanMap.Keys.All(s => ActiveTextview.GetSpanInView(_subjectBuffer.CurrentSnapshot.GetSpan(s.ToSpan())).Count != 0);
            }

            internal void ApplyReplacementText(bool updateSelection = true)
            {
                AssertIsForeground();

                if (!AreAllReferenceSpansMappable())
                {
                    // don't dynamically update the reference spans for documents with unmappable projections
                    return;
                }

                _session.UndoManager.ApplyCurrentState(
                    _subjectBuffer,
                    s_propagateSpansEditTag,
                    _referenceSpanToLinkedRenameSpanMap.Values.Where(r => r.Type != RenameSpanKind.None).Select(r => r.TrackingSpan));

                if (updateSelection && _activeSpan.HasValue && this.ActiveTextview != null)
                {
                    var snapshot = _subjectBuffer.CurrentSnapshot;
                    _session.UndoManager.UpdateSelection(this.ActiveTextview, _subjectBuffer, _referenceSpanToLinkedRenameSpanMap[_activeSpan.Value].TrackingSpan);
                }
            }

            internal void Disconnect(bool documentIsClosed, bool rollbackTemporaryEdits)
            {
                AssertIsForeground();

                // Detatch from the buffer; it is important that this is done before we start
                // undoing transactions, since the undos will cause buffer changes.
                _subjectBuffer.ChangedLowPriority -= OnTextBufferChanged;

                foreach (var view in _textViews)
                {
                    view.Closed -= OnTextViewClosed;
                }

                // Remove any old read only regions we had
                UpdateReadOnlyRegions(removeOnly: true);

                if (rollbackTemporaryEdits && !documentIsClosed)
                {
                    _session.UndoManager.UndoTemporaryEdits(_subjectBuffer, disconnect: true);
                }
            }

            internal void ApplyConflictResolutionEdits(IInlineRenameReplacementInfo conflictResolution, IEnumerable<Document> documents, CancellationToken cancellationToken)
            {
                AssertIsForeground();

                if (!AreAllReferenceSpansMappable())
                {
                    // don't dynamically update the reference spans for documents with unmappable projections
                    return;
                }

                using (new SelectionTracking(this))
                {
                    // 1. Undo any previous edits and update the buffer to resulting document after conflict resolution
                    _session.UndoManager.UndoTemporaryEdits(_subjectBuffer, disconnect: false);

                    var preMergeSolution = _session._baseSolution;
                    var postMergeSolution = conflictResolution.NewSolution;

                    var diffMergingSession = new LinkedFileDiffMergingSession(preMergeSolution, postMergeSolution, postMergeSolution.GetChanges(preMergeSolution), logSessionInfo: true);
                    var mergeResult = diffMergingSession.MergeDiffsAsync(mergeConflictHandler: null, cancellationToken: cancellationToken).WaitAndGetResult(cancellationToken);

                    var newDocument = mergeResult.MergedSolution.GetDocument(documents.First().Id);
                    var originalDocument = _baseDocuments.Single(d => d.Id == newDocument.Id);

                    var changes = GetTextChangesFromTextDifferencingServiceAsync(originalDocument, newDocument, cancellationToken).WaitAndGetResult(cancellationToken);

                    // TODO: why does the following line hang when uncommented?
                    // newDocument.GetTextChangesAsync(this.baseDocuments.Single(d => d.Id == newDocument.Id), cancellationToken).WaitAndGetResult(cancellationToken).Reverse();

                    _session.UndoManager.CreateConflictResolutionUndoTransaction(_subjectBuffer, () =>
                    {
                        using (var edit = _subjectBuffer.CreateEdit(EditOptions.DefaultMinimalChange, null, s_propagateSpansEditTag))
                        {
                            foreach (var change in changes)
                            {
                                edit.Replace(change.Span.Start, change.Span.Length, change.NewText);
                            }

                            edit.Apply();
                        }
                    });

                    // 2. We want to update referenceSpanToLinkedRenameSpanMap where spans were affected by conflict resolution.
                    // We also need to add the remaining document edits to conflictResolutionRenameTrackingSpans
                    // so they get classified/tagged correctly in the editor.
                    _conflictResolutionRenameTrackingSpans.Clear();

                    foreach (var document in documents)
                    {
                        var relevantReplacements = conflictResolution.GetReplacements(document.Id).Where(r => GetRenameSpanKind(r.Kind) != RenameSpanKind.None);
                        if (!relevantReplacements.Any())
                        {
                            continue;
                        }

                        var bufferContainsLinkedDocuments = documents.Skip(1).Any();
                        var mergedReplacements = bufferContainsLinkedDocuments
                            ? GetMergedReplacementInfos(
                                relevantReplacements,
                                conflictResolution.NewSolution.GetDocument(document.Id),
                                mergeResult.MergedSolution.GetDocument(document.Id),
                                cancellationToken)
                            : relevantReplacements;

                        // Show merge conflicts comments as unresolvable conflicts, and do not 
                        // show any other rename-related spans that overlap a merge conflict comment.
                        var mergeConflictComments = mergeResult.MergeConflictCommentSpans.ContainsKey(document.Id)
                            ? mergeResult.MergeConflictCommentSpans[document.Id]
                            : SpecializedCollections.EmptyEnumerable<TextSpan>();

                        foreach (var conflict in mergeConflictComments)
                        {
                            // TODO: Add these to the unresolvable conflict counts in the dashboard

                            _conflictResolutionRenameTrackingSpans.Add(new RenameTrackingSpan(
                                _subjectBuffer.CurrentSnapshot.CreateTrackingSpan(conflict.ToSpan(), SpanTrackingMode.EdgeInclusive, TrackingFidelityMode.Forward),
                                RenameSpanKind.UnresolvedConflict));
                        }

                        foreach (var replacement in mergedReplacements)
                        {
                            var kind = GetRenameSpanKind(replacement.Kind);

                            if (_referenceSpanToLinkedRenameSpanMap.ContainsKey(replacement.OriginalSpan) && kind != RenameSpanKind.Complexified)
                            {
                                var linkedRenameSpan = _session._renameInfo.GetConflictEditSpan(
                                    new InlineRenameLocation(newDocument, replacement.NewSpan), _session.ReplacementText, cancellationToken);
                                if (linkedRenameSpan.HasValue)
                                {
                                    if (!mergeConflictComments.Any(s => replacement.NewSpan.IntersectsWith(s)))
                                    {
                                        _referenceSpanToLinkedRenameSpanMap[replacement.OriginalSpan] = new RenameTrackingSpan(
                                            _subjectBuffer.CurrentSnapshot.CreateTrackingSpan(
                                                linkedRenameSpan.Value.ToSpan(),
                                                SpanTrackingMode.EdgeInclusive,
                                                TrackingFidelityMode.Forward),
                                            kind);
                                    }
                                }
                                else
                                {
                                    // We might not have a renamable span if an alias conflict completely changed the text
                                    _referenceSpanToLinkedRenameSpanMap[replacement.OriginalSpan] = new RenameTrackingSpan(
                                        _referenceSpanToLinkedRenameSpanMap[replacement.OriginalSpan].TrackingSpan,
                                        RenameSpanKind.None);

                                    if (_activeSpan.HasValue && _activeSpan.Value.IntersectsWith(replacement.OriginalSpan))
                                    {
                                        _activeSpan = null;
                                    }
                                }
                            }
                            else
                            {
                                if (!mergeConflictComments.Any(s => replacement.NewSpan.IntersectsWith(s)))
                                {
                                    _conflictResolutionRenameTrackingSpans.Add(new RenameTrackingSpan(
                                        _subjectBuffer.CurrentSnapshot.CreateTrackingSpan(replacement.NewSpan.ToSpan(), SpanTrackingMode.EdgeInclusive, TrackingFidelityMode.Forward),
                                        kind));
                                }
                            }
                        }
                    }

                    UpdateReadOnlyRegions();

                    // 3. Reset the undo state and notify the taggers.
                    this.ApplyReplacementText(updateSelection: false);
                    RaiseSpansChanged();
                }
            }

            private static async Task<IEnumerable<TextChange>> GetTextChangesFromTextDifferencingServiceAsync(Document oldDocument, Document newDocument, CancellationToken cancellationToken = default(CancellationToken))
            {
                try
                {
                    using (Logger.LogBlock(FunctionId.Workspace_Document_GetTextChanges, newDocument.Name, cancellationToken))
                    {
                        if (oldDocument == newDocument)
                        {
                            // no changes
                            return SpecializedCollections.EmptyEnumerable<TextChange>();
                        }

                        if (newDocument.Id != oldDocument.Id)
                        {
                            throw new ArgumentException(WorkspacesResources.DocumentVersionIsDifferent);
                        }

                        SourceText oldText = await oldDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
                        SourceText newText = await newDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);

                        if (oldText == newText)
                        {
                            return SpecializedCollections.EmptyEnumerable<TextChange>();
                        }

                        var textChanges = newText.GetTextChanges(oldText).ToList();

                        // if changes are significant (not the whole document being replaced) then use these changes
                        if (textChanges.Count != 1 || textChanges[0].Span != new TextSpan(0, oldText.Length))
                        {
                            return textChanges;
                        }

                        var textDiffService = oldDocument.Project.Solution.Workspace.Services.GetService<IDocumentTextDifferencingService>();
                        return await textDiffService.GetTextChangesAsync(oldDocument, newDocument, cancellationToken).ConfigureAwait(false);
                    }
                }
                catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
                {
                    throw ExceptionUtilities.Unreachable;
                }
            }

            private IEnumerable<InlineRenameReplacement> GetMergedReplacementInfos(
                IEnumerable<InlineRenameReplacement> relevantReplacements,
                Document preMergeDocument,
                Document postMergeDocument,
                CancellationToken cancellationToken)
            {
                AssertIsForeground();

                var textDiffService = preMergeDocument.Project.Solution.Workspace.Services.GetService<IDocumentTextDifferencingService>();
                var contentType = preMergeDocument.Project.LanguageServices.GetService<IContentTypeLanguageService>().GetDefaultContentType();

                // TODO: Track all spans at once

                ITextBufferCloneService textBufferCloneService = null;
                SnapshotSpan? snapshotSpanToClone = null;
                string preMergeDocumentTextString = null;

                var preMergeDocumentText = preMergeDocument.GetTextAsync(cancellationToken).WaitAndGetResult(cancellationToken);
                var snapshot = preMergeDocumentText.FindCorrespondingEditorTextSnapshot();
                if (snapshot != null)
                {
                    textBufferCloneService = preMergeDocument.Project.Solution.Workspace.Services.GetService<ITextBufferCloneService>();
                    if (textBufferCloneService != null)
                    {
                        snapshotSpanToClone = snapshot.GetFullSpan();
                    }
                }

                if (snapshotSpanToClone == null)
                {
                    preMergeDocumentTextString = preMergeDocument.GetTextAsync(cancellationToken).WaitAndGetResult(cancellationToken).ToString();
                }

                foreach (var replacement in relevantReplacements)
                {
                    var buffer = snapshotSpanToClone.HasValue ? textBufferCloneService.Clone(snapshotSpanToClone.Value) : _textBufferFactoryService.CreateTextBuffer(preMergeDocumentTextString, contentType);
                    var trackingSpan = buffer.CurrentSnapshot.CreateTrackingSpan(replacement.NewSpan.ToSpan(), SpanTrackingMode.EdgeExclusive, TrackingFidelityMode.Forward);

                    using (var edit = _subjectBuffer.CreateEdit(EditOptions.None, null, s_calculateMergedSpansEditTag))
                    {
                        foreach (var change in textDiffService.GetTextChangesAsync(preMergeDocument, postMergeDocument, cancellationToken).WaitAndGetResult(cancellationToken))
                        {
                            buffer.Replace(change.Span.ToSpan(), change.NewText);
                        }

                        edit.Apply();
                    }

                    yield return new InlineRenameReplacement(replacement.Kind, replacement.OriginalSpan, trackingSpan.GetSpan(buffer.CurrentSnapshot).Span.ToTextSpan());
                }
            }

            private static RenameSpanKind GetRenameSpanKind(InlineRenameReplacementKind kind)
            {
                switch (kind)
                {
                    case InlineRenameReplacementKind.NoConflict:
                    case InlineRenameReplacementKind.ResolvedReferenceConflict:
                        return RenameSpanKind.Reference;

                    case InlineRenameReplacementKind.ResolvedNonReferenceConflict:
                        return RenameSpanKind.None;

                    case InlineRenameReplacementKind.UnresolvedConflict:
                        return RenameSpanKind.UnresolvedConflict;

                    case InlineRenameReplacementKind.Complexified:
                        return RenameSpanKind.Complexified;

                    default:
                        throw ExceptionUtilities.Unreachable;
                }
            }

            private struct SelectionTracking : IDisposable
            {
                private readonly int? _anchor;
                private readonly int? _active;
                private readonly TextSpan _anchorSpan;
                private readonly TextSpan _activeSpan;
                private readonly OpenTextBufferManager _openTextBufferManager;

                public SelectionTracking(OpenTextBufferManager openTextBufferManager)
                {
                    _openTextBufferManager = openTextBufferManager;
                    _anchor = null;
                    _anchorSpan = default(TextSpan);
                    _active = null;
                    _activeSpan = default(TextSpan);

                    var textView = openTextBufferManager.ActiveTextview;
                    if (textView == null)
                    {
                        return;
                    }

                    var selection = textView.Selection;
                    var snapshot = openTextBufferManager._subjectBuffer.CurrentSnapshot;

                    var containingSpans = openTextBufferManager._referenceSpanToLinkedRenameSpanMap.Select(kvp =>
                    {
                        // GetSpanInView() can return an empty collection if the tracking span isn't mapped to anything
                        // in the current view, specifically a `@model SomeModelClass` directive in a Razor file.
                        var ss = textView.GetSpanInView(kvp.Value.TrackingSpan.GetSpan(snapshot)).FirstOrDefault();
                        if (ss != null && (ss.IntersectsWith(selection.ActivePoint.Position) || ss.IntersectsWith(selection.AnchorPoint.Position)))
                        {
                            return Tuple.Create(kvp.Key, ss);
                        }
                        else
                        {
                            return null;
                        }
                    }).WhereNotNull();

                    foreach (var tuple in containingSpans)
                    {
                        if (tuple.Item2.IntersectsWith(selection.AnchorPoint.Position))
                        {
                            _anchor = tuple.Item2.End - selection.AnchorPoint.Position;
                            _anchorSpan = tuple.Item1;
                        }

                        if (tuple.Item2.IntersectsWith(selection.ActivePoint.Position))
                        {
                            _active = tuple.Item2.End - selection.ActivePoint.Position;
                            _activeSpan = tuple.Item1;
                        }
                    }
                }

                public void Dispose()
                {
                    var textView = _openTextBufferManager.ActiveTextview;
                    if (textView == null)
                    {
                        return;
                    }

                    if (_anchor.HasValue || _active.HasValue)
                    {
                        var selection = textView.Selection;
                        var snapshot = _openTextBufferManager._subjectBuffer.CurrentSnapshot;

                        var anchorSpan = _anchorSpan;
                        var anchorPoint = new VirtualSnapshotPoint(textView.TextSnapshot,
                            _anchor.HasValue && _openTextBufferManager._referenceSpanToLinkedRenameSpanMap.Keys.Any(s => s.OverlapsWith(anchorSpan))
                            ? GetNewEndpoint(_anchorSpan) - _anchor.Value
                            : selection.AnchorPoint.Position);

                        var activeSpan = _activeSpan;
                        var activePoint = new VirtualSnapshotPoint(textView.TextSnapshot,
                            _active.HasValue && _openTextBufferManager._referenceSpanToLinkedRenameSpanMap.Keys.Any(s => s.OverlapsWith(activeSpan))
                            ? GetNewEndpoint(_activeSpan) - _active.Value
                            : selection.ActivePoint.Position);

                        textView.SetSelection(anchorPoint, activePoint);
                    }
                }

                private SnapshotPoint GetNewEndpoint(TextSpan span)
                {
                    var snapshot = _openTextBufferManager._subjectBuffer.CurrentSnapshot;
                    var endPoint = _openTextBufferManager._referenceSpanToLinkedRenameSpanMap.ContainsKey(span)
                        ? _openTextBufferManager._referenceSpanToLinkedRenameSpanMap[span].TrackingSpan.GetEndPoint(snapshot)
                        : _openTextBufferManager._referenceSpanToLinkedRenameSpanMap.First(kvp => kvp.Key.OverlapsWith(span)).Value.TrackingSpan.GetEndPoint(snapshot);
                    return _openTextBufferManager.ActiveTextview.BufferGraph.MapUpToBuffer(endPoint, PointTrackingMode.Positive, PositionAffinity.Successor, _openTextBufferManager.ActiveTextview.TextBuffer).Value;
                }
            }
        }
    }
}
