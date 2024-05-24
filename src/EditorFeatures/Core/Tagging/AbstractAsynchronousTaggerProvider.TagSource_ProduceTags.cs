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

            var oldTagTrees = BufferToTagTree.InterlockedExchange(
                ref _cachedTagTrees_mayChangeFromAnyThread, BufferToTagTree.Empty);

            var snapshot = _subjectBuffer.CurrentSnapshot;
            var oldTagTree = GetTagTree(snapshot, oldTagTrees);

            // everything from old tree is removed.
            RaiseTagsChanged(snapshot.TextBuffer, new DiffResult(added: null, removed: new(oldTagTree.GetSpans(snapshot).Select(s => s.Span))));
        }

        private void OnSubjectBufferChanged(object? _, TextContentChangedEventArgs e)
        {
            _dataSource.ThreadingContext.ThrowIfNotOnUIThread();
            UpdateTagsForTextChange(e);
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

            var snapshot = e.After;
            var buffer = snapshot.TextBuffer;

            // Everything we're passing in here is synchronous.  So we can assert that this must complete synchronously
            // as well.
            var (oldTagTrees, newTagTrees) = CompareAndSwapTagTreesAsync(
                oldTagTrees =>
                {
                    if (oldTagTrees.TryGetValue(buffer, out var treeForBuffer))
                    {
                        var tagsToRemove = e.Changes.SelectMany(c => treeForBuffer.GetIntersectingSpans(new SnapshotSpan(snapshot, c.NewSpan)));
                        if (tagsToRemove.Any())
                        {
                            var allTags = treeForBuffer.GetSpans(e.After).ToList();
                            var newTagTree = new TagSpanIntervalTree<TTag>(
                                buffer,
                                treeForBuffer.SpanTrackingMode,
                                allTags.Except(tagsToRemove, comparer: this));
                            return new(oldTagTrees.SetItem(buffer, newTagTree));
                        }
                    }

                    // return oldTagTrees to indicate nothing changed.
                    return new(oldTagTrees);
                }, _dataSource.ThreadingContext.DisposalToken).VerifyCompleted();

            // Can happen if we were canceled.  Just bail out immediate.
            if (newTagTrees.IsDefault)
                return;

            // Nothing changed.  Bail out.
            if (oldTagTrees == newTagTrees)
                return;

            // Not sure why we are diffing when we already have tagsToRemove. is it due to _tagSpanComparer might return
            // different result than GetIntersectingSpans?
            //
            // treeForBuffer basically points to oldTagTrees. case where oldTagTrees not exist is already taken cared by
            // CachedTagTrees.TryGetValue.
            var difference = ComputeDifference(snapshot, newTagTrees[buffer], oldTagTrees[buffer]);

            RaiseTagsChanged(buffer, difference);
        }

        private TagSpanIntervalTree<TTag> GetTagTree(ITextSnapshot snapshot, BufferToTagTree tagTrees)
        {
            return tagTrees.TryGetValue(snapshot.TextBuffer, out var tagTree)
                ? tagTree
                : new TagSpanIntervalTree<TTag>(snapshot.TextBuffer, _dataSource.SpanTrackingMode);
        }

        private void OnEventSourceChanged(object? _1, TaggerEventArgs _2)
            => EnqueueWork(highPriority: false);

        private void EnqueueWork(bool highPriority)
        {
            // Cancel any expensive, in-flight, tagging work as there's now a request to perform lightweight tagging.
            // Note: intentionally ignoring the return value here.  We're enqueuing normal work here, so it has no
            // associated token with it.
            _ = _nonFrozenComputationCancellationSeries.CreateNext();
            EnqueueWork(highPriority, _dataSource.SupportsFrozenPartialSemantics, nonFrozenComputationToken: null);
        }

        private void EnqueueWork(bool highPriority, bool frozenPartialSemantics, CancellationToken? nonFrozenComputationToken)
            => _eventChangeQueue.AddWork(
                new TagSourceQueueItem(highPriority, frozenPartialSemantics, nonFrozenComputationToken),
                _dataSource.CancelOnNewWork);

        private async ValueTask<VoidResult> ProcessEventChangeAsync(
            ImmutableSegmentedList<TagSourceQueueItem> changes, CancellationToken cancellationToken)
        {
            Contract.ThrowIfTrue(changes.IsEmpty);

            // If any of the requests was high priority, then compute at that speed.
            var highPriority = changes.Any(x => x.HighPriority);

            // If any of the requests are for frozen partial, then we do compute with frozen partial semantics.  We
            // always want these "fast but inaccurate" passes to happen first.  That pass will then enqueue the work
            // to do the slow-but-accurate pass.
            var frozenPartialSemantics = changes.Any(t => t.FrozenPartialSemantics);

            if (!frozenPartialSemantics && _dataSource.SupportsFrozenPartialSemantics)
            {
                // We're asking for expensive tags, and this tagger supports frozen partial tags.  Kick off the work
                // to do this expensive tagging, but attach ourselves to the requested cancellation token so this
                // expensive work can be canceled if new requests for frozen partial work come in.

                // Since we're not frozen-partial, all requests must have an associated cancellation token.  And all but
                // the last *must* be already canceled (since each is canceled as new work is added).
                Contract.ThrowIfFalse(changes.All(t => !t.FrozenPartialSemantics));
                Contract.ThrowIfFalse(changes.All(t => t.NonFrozenComputationToken != null));
                Contract.ThrowIfFalse(changes.Take(changes.Count - 1).All(t => t.NonFrozenComputationToken!.Value.IsCancellationRequested));

                var lastNonFrozenComputationToken = changes[^1].NonFrozenComputationToken!.Value;

                // Need a dedicated try/catch here since we're operating on a different token than the queue's token.
                using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(lastNonFrozenComputationToken, cancellationToken);
                try
                {
                    await RecomputeTagsAsync(highPriority, frozenPartialSemantics, calledFromJtfRun: false, linkedTokenSource.Token).ConfigureAwait(false);
                    return default;
                }
                catch (OperationCanceledException ex) when (ExceptionUtilities.IsCurrentOperationBeingCancelled(ex, linkedTokenSource.Token))
                {
                    return default;
                }
            }
            else
            {
                // Normal request to either compute frozen partial tags, or compute normal tags in a tagger that does
                // *not* support frozen partial tagging.
                await RecomputeTagsAsync(highPriority, frozenPartialSemantics, calledFromJtfRun: false, cancellationToken).ConfigureAwait(false);
                return default;
            }
        }

        private async Task<(BufferToTagTree oldTagTrees, BufferToTagTree newTagTrees)> CompareAndSwapTagTreesAsync(
            Func<BufferToTagTree, ValueTask<BufferToTagTree>> callback,
            CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var oldTagTrees = _cachedTagTrees_mayChangeFromAnyThread;

                // Compute the new tag trees, based on what the current tag trees are.  Intentionally CA(true) here so
                // we stay on the UI thread if we're in a JTF blocking call.
                var newTagTrees = await callback(oldTagTrees).ConfigureAwait(true);

                // Now, try to update the cached tag trees to what we computed.  If we win, we're done.  Otherwise, some
                // other thread was able to do this, and we need to try again.
                if (oldTagTrees == BufferToTagTree.InterlockedCompareExchange(ref _cachedTagTrees_mayChangeFromAnyThread, newTagTrees, oldTagTrees))
                    return (oldTagTrees, newTagTrees);
            }

            return default;
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
        /// <param name="highPriority"> If this tagging request should be processed as quickly as possible with no extra
        /// delays added for it.
        /// </param>
        /// <param name="calledFromJtfRun">If this method is being called from within a JTF.Run call.  This is used to
        /// ensure we don't do unnecessary switches to the threadpool while JTF is waiting on us.</param>
        private async Task<BufferToTagTree> RecomputeTagsAsync(
            bool highPriority,
            bool frozenPartialSemantics,
            bool calledFromJtfRun,
            CancellationToken cancellationToken)
        {
            // Jump to the main thread, as we have to grab the spans to tag and the caret point.
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
            }

            using (Logger.LogBlock(FunctionId.Tagger_TagSource_RecomputeTags, cancellationToken))
            {
                _dataSource.ThreadingContext.ThrowIfNotOnUIThread();
                if (cancellationToken.IsCancellationRequested)
                    return default;

                // Make a copy of all the data we need while we're on the foreground.  Then switch to a threadpool
                // thread to do the computation. Finally, once new tags have been computed, then we update our state
                // again on the foreground.
                var spansToTag = GetSpansAndDocumentsToTag();
                var caretPosition = _dataSource.GetCaretPoint(_textView, _subjectBuffer);

                // If we're being called from within a blocking JTF.Run call, we don't want to switch to the background
                // if we can avoid it.
                if (!calledFromJtfRun)
                    await TaskScheduler.Default;

                if (cancellationToken.IsCancellationRequested)
                    return default;

                if (frozenPartialSemantics)
                {
                    spansToTag = spansToTag.SelectAsArray(ds => new DocumentSnapshotSpan(
                        ds.Document?.WithFrozenPartialSemantics(cancellationToken),
                        ds.SnapshotSpan));
                }

                // Now spin, trying to compute the updated tags.  We only need to do this as the tag state is also
                // allowed to change on the UI thread (for example, taggers can say they want tags to be immediately
                // removed when an edit happens. So, we need to keep recomputing the tags until we win and become the
                // latest tags.
                var oldState = _state_accessOnlyFromEventChangeQueueCallback;

                TaggerContext<TTag> context = null!;
                var (oldTagTrees, newTagTrees) = await CompareAndSwapTagTreesAsync(
                    async oldTagTrees =>
                    {
                        // Create a context to store pass the information along and collect the results.
                        context = new TaggerContext<TTag>(
                            oldState, frozenPartialSemantics, spansToTag, caretPosition, oldTagTrees.Map);
                        await ProduceTagsAsync(context, cancellationToken).ConfigureAwait(false);

                        return ComputeNewTagTrees(oldTagTrees, context);
                    }, cancellationToken).ConfigureAwait(continueOnCapturedContext: calledFromJtfRun);

                // We may get back null if we were canceled.  Immediately bail out in that case.
                if (newTagTrees.IsDefault)
                    return default;

                var bufferToChanges = ProcessNewTagTrees(spansToTag, oldTagTrees, newTagTrees, cancellationToken);

                // Note: assigning to 'State' is completely safe.  It is only ever read from the _eventChangeQueue
                // serial callbacks on the threadpool.
                _state_accessOnlyFromEventChangeQueueCallback = context.State;

                OnTagsChangedForBuffer(bufferToChanges, highPriority);

                // If we were computing with frozen partial semantics here, enqueue work to compute *without* frozen
                // partial snapshots so we move to accurate results shortly. Create and pass along a new cancellation
                // token for this expensive work so that it can be canceled by future lightweight work.
                if (frozenPartialSemantics)
                    this.EnqueueWork(highPriority, frozenPartialSemantics: false, _nonFrozenComputationCancellationSeries.CreateNext(default));

                return newTagTrees;
            }
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

        private BufferToTagTree ComputeNewTagTrees(BufferToTagTree oldTagTrees, TaggerContext<TTag> context)
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
            var newTagTrees = BufferToTagTree.Empty;
            foreach (var buffer in buffersToTag)
            {
                var newTagTree = ComputeNewTagTree(oldTagTrees, buffer, newTagsByBuffer[buffer], spansToInvalidateByBuffer[buffer]);
                if (newTagTree != null)
                    newTagTrees = newTagTrees.Add(buffer, newTagTree);
            }

            return newTagTrees;
        }

        private TagSpanIntervalTree<TTag>? ComputeNewTagTree(
            BufferToTagTree oldTagTrees,
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
            BufferToTagTree oldTagTrees,
            BufferToTagTree newTagTrees,
            CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Tagger_TagSource_ProcessNewTags, cancellationToken))
            {
                var bufferToChanges = new Dictionary<ITextBuffer, DiffResult>();

                foreach (var (latestBuffer, latestSpans) in newTagTrees.Map)
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

                foreach (var (oldBuffer, previousSpans) in oldTagTrees.Map)
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

            // If this is the first time we're being asked for tags, and we're a tagger that requires the initial tags
            // be available synchronously on this call, and the computation of tags hasn't completed yet, then force the
            // tags to be computed now on this thread.  The singular use case for this is Outlining which needs those
            // tags synchronously computed for things like Metadata-as-Source collapsing.
            var tagTrees = _cachedTagTrees_mayChangeFromAnyThread;
            if (_firstTagsRequest &&
                _dataSource.ComputeInitialTagsSynchronously(buffer) &&
                !tagTrees.TryGetValue(buffer, out _))
            {
                // Compute this as a high priority work item to have the lease amount of blocking as possible.
                tagTrees = _dataSource.ThreadingContext.JoinableTaskFactory.Run(() =>
                    this.RecomputeTagsAsync(highPriority: true, _dataSource.SupportsFrozenPartialSemantics, calledFromJtfRun: true, _disposalTokenSource.Token));
            }

            _firstTagsRequest = false;

            // We can get a defualt instance back if we were canceled.
            if (tagTrees.IsDefault)
                return null;

            tagTrees.TryGetValue(buffer, out var tags);
            return tags;
        }

        public void AddTags(NormalizedSnapshotSpanCollection requestedSpans, SegmentedList<TagSpan<TTag>> tags)
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
