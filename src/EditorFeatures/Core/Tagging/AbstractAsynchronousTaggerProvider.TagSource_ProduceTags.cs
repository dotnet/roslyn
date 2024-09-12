// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Workspaces;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Tagging;

internal partial class AbstractAsynchronousTaggerProvider<TTag>
{
    private partial class TagSource
    {
        private void OnCaretPositionChanged(object? _, CaretPositionChangedEventArgs e)
        {
            _dataSource.ThreadingContext.ThrowIfNotOnUIThread();

            Debug.Assert(_dataSource.CaretChangeBehavior.HasFlag(TaggerCaretChangeBehavior.RemoveAllTagsOnCaretMoveOutsideOfTag));

            var caret = _dataSource.GetCaretPoint(_textView, _subjectBuffer);
            if (caret.HasValue)
            {
                // If it changed position and we're still in a tag, there's nothing more to do
                var currentTags = TryGetTagIntervalTreeForBuffer(caret.Value.Snapshot.TextBuffer);
                if (currentTags != null && currentTags.GetIntersectingSpans(new SnapshotSpan(caret.Value, 0)).Count > 0)
                {
                    // Caret is inside a tag.  No need to do anything.
                    return;
                }
            }

            RemoveAllTags();
        }

        private void RemoveAllTags()
        {
            _dataSource.ThreadingContext.ThrowIfNotOnUIThread();

            var oldTagTrees = this.CachedTagTrees;
            this.CachedTagTrees = ImmutableDictionary<ITextBuffer, TagSpanIntervalTree<TTag>>.Empty;

            var snapshot = _subjectBuffer.CurrentSnapshot;
            var oldTagTree = GetTagTree(snapshot, oldTagTrees);

            // everything from old tree is removed.
            RaiseTagsChanged(snapshot.TextBuffer, new DiffResult(added: null, removed: new(oldTagTree.GetSpans(snapshot).Select(s => s.Span))));
        }

        private void OnSubjectBufferChanged(object? _, TextContentChangedEventArgs e)
        {
            _dataSource.ThreadingContext.ThrowIfNotOnUIThread();
            UpdateTagsForTextChange(e);
            AccumulateTextChanges(e);
        }

        private void AccumulateTextChanges(TextContentChangedEventArgs contentChanged)
        {
            _dataSource.ThreadingContext.ThrowIfNotOnUIThread();
            var contentChanges = contentChanged.Changes;
            var count = contentChanges.Count;

            switch (count)
            {
                case 0:
                    return;

                case 1:
                    // PERF: Optimize for the simple case of typing on a line.
                    {
                        var c = contentChanges[0];
                        var textChangeRange = new TextChangeRange(new TextSpan(c.OldSpan.Start, c.OldSpan.Length), c.NewLength);
                        this.AccumulatedTextChanges = this.AccumulatedTextChanges == null
                            ? textChangeRange
                            : this.AccumulatedTextChanges.Accumulate(SpecializedCollections.SingletonEnumerable(textChangeRange));
                    }

                    break;

                default:
                    {
                        using var _ = ArrayBuilder<TextChangeRange>.GetInstance(count, out var textChangeRanges);
                        foreach (var c in contentChanges)
                            textChangeRanges.Add(new TextChangeRange(new TextSpan(c.OldSpan.Start, c.OldSpan.Length), c.NewLength));

                        this.AccumulatedTextChanges = this.AccumulatedTextChanges.Accumulate(textChangeRanges);
                        break;
                    }
            }
        }

        private void UpdateTagsForTextChange(TextContentChangedEventArgs e)
        {
            _dataSource.ThreadingContext.ThrowIfNotOnUIThread();

            if (_dataSource.TextChangeBehavior.HasFlag(TaggerTextChangeBehavior.RemoveAllTags))
            {
                this.RemoveAllTags();
                return;
            }

            // Don't bother going forward if we're not going adjust any tags based on edits.
            if (_dataSource.TextChangeBehavior.HasFlag(TaggerTextChangeBehavior.RemoveTagsThatIntersectEdits))
            {
                RemoveTagsThatIntersectEdit(e);
                return;
            }
        }

        private void RemoveTagsThatIntersectEdit(TextContentChangedEventArgs e)
        {
            if (e.Changes.Count == 0)
                return;

            var buffer = e.After.TextBuffer;
            if (!this.CachedTagTrees.TryGetValue(buffer, out var treeForBuffer))
                return;

            var snapshot = e.After;

            var tagsToRemove = e.Changes.SelectMany(c => treeForBuffer.GetIntersectingSpans(new SnapshotSpan(snapshot, c.NewSpan)));
            if (!tagsToRemove.Any())
                return;

            var allTags = treeForBuffer.GetSpans(e.After).ToList();
            var newTagTree = new TagSpanIntervalTree<TTag>(
                buffer,
                treeForBuffer.SpanTrackingMode,
                allTags.Except(tagsToRemove, comparer: this));

            this.CachedTagTrees = this.CachedTagTrees.SetItem(snapshot.TextBuffer, newTagTree);

            // Not sure why we are diffing when we already have tagsToRemove. is it due to _tagSpanComparer might return
            // different result than GetIntersectingSpans?
            //
            // treeForBuffer basically points to oldTagTrees. case where oldTagTrees not exist is already taken cared by
            // CachedTagTrees.TryGetValue.
            var difference = ComputeDifference(snapshot, newTagTree, treeForBuffer);

            RaiseTagsChanged(snapshot.TextBuffer, difference);
        }

        private TagSpanIntervalTree<TTag> GetTagTree(ITextSnapshot snapshot, ImmutableDictionary<ITextBuffer, TagSpanIntervalTree<TTag>> tagTrees)
        {
            return tagTrees.TryGetValue(snapshot.TextBuffer, out var tagTree)
                ? tagTree
                : new TagSpanIntervalTree<TTag>(snapshot.TextBuffer, _dataSource.SpanTrackingMode);
        }

        private void OnEventSourceChanged(object? _1, TaggerEventArgs _2)
            => EnqueueWork(highPriority: false);

        private void EnqueueWork(bool highPriority)
            => _eventChangeQueue.AddWork(highPriority, _dataSource.CancelOnNewWork);

        private ValueTask<VoidResult> ProcessEventChangeAsync(ImmutableSegmentedList<bool> changes, CancellationToken cancellationToken)
        {
            // If any of the requests was high priority, then compute at that speed.
            var highPriority = changes.Contains(true);
            return new ValueTask<VoidResult>(RecomputeTagsAsync(highPriority, cancellationToken));
        }

        /// <summary>
        /// Passed a boolean to say if we're computing the
        /// initial set of tags or not.  If we're computing the initial set of tags, we lower
        /// all our delays so that we can get results to the screen as quickly as possible.
        /// <para>This gives a good experience when a document is opened as the document appears complete almost
        /// immediately.  Once open though, our normal delays come into play so as to not cause a flashy experience.</para>
        /// </summary>
        /// <remarks>
        /// In the event of a cancellation request, this method may <em>either</em> return at the next availability
        /// or throw a cancellation exception.
        /// </remarks>
        /// <param name="highPriority">
        /// If this tagging request should be processed as quickly as possible with no extra delays added for it.
        /// </param>
        private async Task<VoidResult> RecomputeTagsAsync(bool highPriority, CancellationToken cancellationToken)
        {
            await _dataSource.ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken).NoThrowAwaitable();
            if (cancellationToken.IsCancellationRequested)
                return default;

            // if we're tagging documents that are not visible, then introduce a long delay so that we avoid
            // consuming machine resources on work the user isn't likely to see.  ConfigureAwait(true) so that if
            // we're on the UI thread that we stay on it.
            //
            // Don't do this for explicit high priority requests as the caller wants the UI updated as quickly as
            // possible.
            if (!highPriority)
            {
                // Use NoThrow as this is a high source of cancellation exceptions.  This avoids the exception and instead
                // bails gracefully by checking below.
                await _visibilityTracker.DelayWhileNonVisibleAsync(
                    _dataSource.ThreadingContext, _dataSource.AsyncListener, _subjectBuffer, DelayTimeSpan.NonFocus, cancellationToken).NoThrowAwaitable(captureContext: true);

                if (cancellationToken.IsCancellationRequested)
                    return default;
            }

            _dataSource.ThreadingContext.ThrowIfNotOnUIThread();
            if (cancellationToken.IsCancellationRequested)
                return default;

            using (Logger.LogBlock(FunctionId.Tagger_TagSource_RecomputeTags, cancellationToken))
            {
                // Make a copy of all the data we need while we're on the foreground.  Then switch to a threadpool
                // thread to do the computation. Finally, once new tags have been computed, then we update our state
                // again on the foreground.
                var spansToTag = GetSpansAndDocumentsToTag();
                var caretPosition = _dataSource.GetCaretPoint(_textView, _subjectBuffer);
                var oldTagTrees = this.CachedTagTrees;
                var oldState = this.State;

                var textChangeRange = this.AccumulatedTextChanges;
                var subjectBufferVersion = _subjectBuffer.CurrentSnapshot.Version.VersionNumber;

                await TaskScheduler.Default;

                if (cancellationToken.IsCancellationRequested)
                    return default;

                // Create a context to store pass the information along and collect the results.
                var context = new TaggerContext<TTag>(
                    oldState, spansToTag, caretPosition, textChangeRange, oldTagTrees);
                await ProduceTagsAsync(context, cancellationToken).ConfigureAwait(false);

                if (cancellationToken.IsCancellationRequested)
                    return default;

                // Process the result to determine what changed.
                var newTagTrees = ComputeNewTagTrees(oldTagTrees, context);
                var bufferToChanges = ProcessNewTagTrees(spansToTag, oldTagTrees, newTagTrees, cancellationToken);

                // Then switch back to the UI thread to update our state and kick off the work to notify the editor.
                await _dataSource.ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken).NoThrowAwaitable();
                if (cancellationToken.IsCancellationRequested)
                    return default;

                // Once we assign our state, we're uncancellable.  We must report the changed information
                // to the editor.  The only case where it's ok not to is if the tagger itself is disposed.
                cancellationToken = CancellationToken.None;

                this.CachedTagTrees = newTagTrees;
                this.State = context.State;
                if (this._subjectBuffer.CurrentSnapshot.Version.VersionNumber == subjectBufferVersion)
                {
                    // Only clear the accumulated text changes if the subject buffer didn't change during the
                    // tagging operation. Otherwise, it is impossible to know which changes occurred prior to the
                    // request to tag, and which ones occurred during the tagging itself. Since
                    // AccumulatedTextChanges is a conservative representation of the work that needs to be done, in
                    // the event this value is not cleared the only potential impact will be slightly more work
                    // being done during the next classification pass.
                    this.AccumulatedTextChanges = null;
                }

                OnTagsChangedForBuffer(bufferToChanges, highPriority);

                // Once we've computed tags, pause ourselves if we're no longer visible.  That way we don't consume any
                // machine resources that the user won't even notice.
                PauseIfNotVisible();
            }

            return default;
        }

        private ImmutableArray<DocumentSnapshotSpan> GetSpansAndDocumentsToTag()
        {
            _dataSource.ThreadingContext.ThrowIfNotOnUIThread();

            // TODO: Update to tag spans from all related documents.

            using var _ = PooledDictionary<ITextSnapshot, Document?>.GetInstance(out var snapshotToDocumentMap);
            var spansToTag = _dataSource.GetSpansToTag(_textView, _subjectBuffer);

            var spansAndDocumentsToTag = spansToTag.SelectAsArray(span =>
            {
                if (!snapshotToDocumentMap.TryGetValue(span.Snapshot, out var document))
                {
                    CheckSnapshot(span.Snapshot);

                    document = span.Snapshot.GetOpenDocumentInCurrentContextWithChanges();
                    snapshotToDocumentMap[span.Snapshot] = document;
                }

                // document can be null if the buffer the given span is part of is not part of our workspace.
                return new DocumentSnapshotSpan(document, span);
            });

            return spansAndDocumentsToTag;
        }

        [Conditional("DEBUG")]
        private static void CheckSnapshot(ITextSnapshot snapshot)
        {
            var container = snapshot.TextBuffer.AsTextContainer();
            if (Workspace.TryGetWorkspace(container, out _))
            {
                // if the buffer is part of our workspace, it must be the latest.
                Debug.Assert(snapshot.Version.Next == null, "should be on latest snapshot");
            }
        }

        private ImmutableDictionary<ITextBuffer, TagSpanIntervalTree<TTag>> ComputeNewTagTrees(
            ImmutableDictionary<ITextBuffer, TagSpanIntervalTree<TTag>> oldTagTrees,
            TaggerContext<TTag> context)
        {
            // Ignore any tag spans reported for any buffers we weren't interested in.

            var spansToTag = context.SpansToTag;
            var buffersToTag = spansToTag.Select(dss => dss.SnapshotSpan.Snapshot.TextBuffer).ToSet();
            var newTagsByBuffer =
                context.TagSpans.Where(ts => buffersToTag.Contains(ts.Span.Snapshot.TextBuffer))
                                .ToLookup(t => t.Span.Snapshot.TextBuffer);
            var spansTagged = context._spansTagged;

            var spansToInvalidateByBuffer = spansTagged.ToLookup(
                keySelector: span => span.Snapshot.TextBuffer,
                elementSelector: span => span);

            // Walk through each relevant buffer and decide what the interval tree should be
            // for that buffer.  In general this will work by keeping around old tags that
            // weren't in the range that was re-tagged, and merging them with the new tags
            // produced for the range that was re-tagged.
            var newTagTrees = ImmutableDictionary<ITextBuffer, TagSpanIntervalTree<TTag>>.Empty;
            foreach (var buffer in buffersToTag)
            {
                var newTagTree = ComputeNewTagTree(oldTagTrees, buffer, newTagsByBuffer[buffer], spansToInvalidateByBuffer[buffer]);
                if (newTagTree != null)
                    newTagTrees = newTagTrees.Add(buffer, newTagTree);
            }

            return newTagTrees;
        }

        private TagSpanIntervalTree<TTag>? ComputeNewTagTree(
            ImmutableDictionary<ITextBuffer, TagSpanIntervalTree<TTag>> oldTagTrees,
            ITextBuffer textBuffer,
            IEnumerable<ITagSpan<TTag>> newTags,
            IEnumerable<SnapshotSpan> spansToInvalidate)
        {
            var noNewTags = newTags.IsEmpty();
            var noSpansToInvalidate = spansToInvalidate.IsEmpty();
            oldTagTrees.TryGetValue(textBuffer, out var oldTagTree);

            if (oldTagTree == null)
            {
                if (noNewTags)
                {
                    // We have no new tags, and no old tags either.  No need to store anything
                    // for this buffer.
                    return null;
                }

                // If we don't have any old tags then we just need to return the new tags.
                return new TagSpanIntervalTree<TTag>(textBuffer, _dataSource.SpanTrackingMode, newTags);
            }

            // If we don't have any new tags, and there was nothing to invalidate, then we can 
            // keep whatever old tags we have without doing any additional work.
            if (noNewTags && noSpansToInvalidate)
            {
                return oldTagTree;
            }

            // We either have some new tags, or we have some tags to invalidate.
            // First, determine which of the old tags we want to keep around.
            var snapshot = noNewTags ? spansToInvalidate.First().Snapshot : newTags.First().Span.Snapshot;
            var oldTagsToKeep = noSpansToInvalidate
                ? oldTagTree.GetSpans(snapshot)
                : GetNonIntersectingTagSpans(spansToInvalidate, oldTagTree);

            // Then union those with the new tags to produce the final tag tree.
            var finalTags = oldTagsToKeep.Concat(newTags);
            return new TagSpanIntervalTree<TTag>(textBuffer, _dataSource.SpanTrackingMode, finalTags);
        }

        private IEnumerable<ITagSpan<TTag>> GetNonIntersectingTagSpans(IEnumerable<SnapshotSpan> spansToInvalidate, TagSpanIntervalTree<TTag> oldTagTree)
        {
            var firstSpanToInvalidate = spansToInvalidate.First();
            var snapshot = firstSpanToInvalidate.Snapshot;

            // Performance: No need to fully realize spansToInvalidate or do any of the calculations below if the
            //   full snapshot is being invalidated.
            if (firstSpanToInvalidate.Length == snapshot.Length)
                return [];

            return oldTagTree.GetSpans(snapshot).Except(
                spansToInvalidate.SelectMany(oldTagTree.GetIntersectingSpans),
                comparer: this);
        }

        private bool ShouldSkipTagProduction()
        {
            if (_dataSource.Options.OfType<Option2<bool>>().Any(option => !_dataSource.GlobalOptions.GetOption(option)))
                return true;

            var languageName = _subjectBuffer.GetLanguageName();
            return _dataSource.Options.OfType<PerLanguageOption2<bool>>().Any(option => languageName == null || !_dataSource.GlobalOptions.GetOption(option, languageName));
        }

        private Task ProduceTagsAsync(TaggerContext<TTag> context, CancellationToken cancellationToken)
        {
            // If the feature is disabled, then just produce no tags.
            return ShouldSkipTagProduction()
                ? Task.CompletedTask
                : _dataSource.ProduceTagsAsync(context, cancellationToken);
        }

        private Dictionary<ITextBuffer, DiffResult> ProcessNewTagTrees(
            ImmutableArray<DocumentSnapshotSpan> spansToTag,
            ImmutableDictionary<ITextBuffer, TagSpanIntervalTree<TTag>> oldTagTrees,
            ImmutableDictionary<ITextBuffer, TagSpanIntervalTree<TTag>> newTagTrees,
            CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Tagger_TagSource_ProcessNewTags, cancellationToken))
            {
                var bufferToChanges = new Dictionary<ITextBuffer, DiffResult>();

                foreach (var (latestBuffer, latestSpans) in newTagTrees)
                {
                    var snapshot = spansToTag.First(s => s.SnapshotSpan.Snapshot.TextBuffer == latestBuffer).SnapshotSpan.Snapshot;

                    if (oldTagTrees.TryGetValue(latestBuffer, out var previousSpans))
                    {
                        var difference = ComputeDifference(snapshot, latestSpans, previousSpans);
                        bufferToChanges[latestBuffer] = difference;
                    }
                    else
                    {
                        // It's a new buffer, so report all spans are changed
                        bufferToChanges[latestBuffer] = new DiffResult(added: new(latestSpans.GetSpans(snapshot).Select(t => t.Span)), removed: null);
                    }
                }

                foreach (var (oldBuffer, previousSpans) in oldTagTrees)
                {
                    if (!newTagTrees.ContainsKey(oldBuffer))
                    {
                        // This buffer disappeared, so let's notify that the old tags are gone
                        bufferToChanges[oldBuffer] = new DiffResult(added: null, removed: new(previousSpans.GetSpans(oldBuffer.CurrentSnapshot).Select(t => t.Span)));
                    }
                }

                return bufferToChanges;
            }
        }

        /// <summary>
        /// Return all the spans that appear in only one of <paramref name="latestTree"/> or <paramref name="previousTree"/>.
        /// </summary>
        private DiffResult ComputeDifference(
            ITextSnapshot snapshot,
            TagSpanIntervalTree<TTag> latestTree,
            TagSpanIntervalTree<TTag> previousTree)
        {
            var latestSpans = latestTree.GetSpans(snapshot);
            var previousSpans = previousTree.GetSpans(snapshot);

            using var _1 = ArrayBuilder<SnapshotSpan>.GetInstance(out var added);
            using var _2 = ArrayBuilder<SnapshotSpan>.GetInstance(out var removed);
            using var latestEnumerator = latestSpans.GetEnumerator();
            using var previousEnumerator = previousSpans.GetEnumerator();

            var latest = NextOrNull(latestEnumerator);
            var previous = NextOrNull(previousEnumerator);

            while (latest != null && previous != null)
            {
                var latestSpan = latest.Span;
                var previousSpan = previous.Span;

                if (latestSpan.Start < previousSpan.Start)
                {
                    added.Add(latestSpan);
                    latest = NextOrNull(latestEnumerator);
                }
                else if (previousSpan.Start < latestSpan.Start)
                {
                    removed.Add(previousSpan);
                    previous = NextOrNull(previousEnumerator);
                }
                else
                {
                    // If the starts are the same, but the ends are different, report the larger
                    // region to be conservative.
                    if (previousSpan.End > latestSpan.End)
                    {
                        removed.Add(previousSpan);
                        latest = NextOrNull(latestEnumerator);
                    }
                    else if (latestSpan.End > previousSpan.End)
                    {
                        added.Add(latestSpan);
                        previous = NextOrNull(previousEnumerator);
                    }
                    else
                    {
                        if (!_dataSource.TagEquals(latest.Tag, previous.Tag))
                            added.Add(latestSpan);

                        latest = NextOrNull(latestEnumerator);
                        previous = NextOrNull(previousEnumerator);
                    }
                }
            }

            while (latest != null)
            {
                added.Add(latest.Span);
                latest = NextOrNull(latestEnumerator);
            }

            while (previous != null)
            {
                removed.Add(previous.Span);
                previous = NextOrNull(previousEnumerator);
            }

            return new DiffResult(new(added), new(removed));

            static ITagSpan<TTag>? NextOrNull(IEnumerator<ITagSpan<TTag>> enumerator)
                => enumerator.MoveNext() ? enumerator.Current : null;
        }

        /// <summary>
        /// Returns the TagSpanIntervalTree containing the tags for the given buffer. If no tags
        /// exist for the buffer at all, null is returned.
        /// </summary>
        private TagSpanIntervalTree<TTag>? TryGetTagIntervalTreeForBuffer(ITextBuffer buffer)
        {
            _dataSource.ThreadingContext.ThrowIfNotOnUIThread();

            // If we've been disposed, no need to proceed.
            if (_disposalTokenSource.Token.IsCancellationRequested)
                return null;

            // If this is the first time we're being asked for tags, and we're a tagger that requires the initial
            // tags be available synchronously on this call, and the computation of tags hasn't completed yet, then
            // force the tags to be computed now on this thread.  The singular use case for this is Outlining which
            // needs those tags synchronously computed for things like Metadata-as-Source collapsing.
            if (_firstTagsRequest &&
                _dataSource.ComputeInitialTagsSynchronously(buffer) &&
                !this.CachedTagTrees.TryGetValue(buffer, out _))
            {
                // Compute this as a high priority work item to have the lease amount of blocking as possible.
                _dataSource.ThreadingContext.JoinableTaskFactory.Run(() =>
                    this.RecomputeTagsAsync(highPriority: true, _disposalTokenSource.Token));
            }

            _firstTagsRequest = false;

            // We're on the UI thread, so it's safe to access these variables.
            this.CachedTagTrees.TryGetValue(buffer, out var tags);
            return tags;
        }

        public void AddTags(NormalizedSnapshotSpanCollection requestedSpans, SegmentedList<ITagSpan<TTag>> tags)
        {
            _dataSource.ThreadingContext.ThrowIfNotOnUIThread();

            // Some client is asking for tags.  Possible that we're becoming visible.  Preemptively start tagging
            // again so we don't have to wait for the visibility notification to come in.
            ResumeIfVisible();

            if (requestedSpans.Count == 0)
                return;

            var buffer = requestedSpans.First().Snapshot.TextBuffer;
            var tagIntervalTree = this.TryGetTagIntervalTreeForBuffer(buffer);

            tagIntervalTree?.AddIntersectingTagSpans(requestedSpans, tags);
        }
    }
}
