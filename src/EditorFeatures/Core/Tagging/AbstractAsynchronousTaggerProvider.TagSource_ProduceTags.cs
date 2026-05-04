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
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Threading;
using Microsoft.CodeAnalysis.Utilities;
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
                if (currentTags != null && currentTags.HasSpanThatIntersects(caret.Value))
                    return;
            }

            RemoveAllTags();
        }

        private void RemoveAllTags()
        {
            _dataSource.ThreadingContext.ThrowIfNotOnUIThread();

            var oldTagTrees = Interlocked.Exchange(
                ref _cachedTagTrees_mayChangeFromAnyThread, ImmutableDictionary<ITextBuffer, TagSpanIntervalTree<TTag>>.Empty);

            var snapshot = _subjectBuffer.CurrentSnapshot;
            var oldTagTree = oldTagTrees.TryGetValue(snapshot.TextBuffer, out var tagTree)
                ? tagTree
                : TagSpanIntervalTree<TTag>.Empty;

            // everything from old tree is removed.
            RaiseTagsChanged(snapshot.TextBuffer, new DiffResult(added: null, removed: oldTagTree.GetSnapshotSpanCollection(snapshot)));
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

            using var _1 = SegmentedListPool.GetPooledList<TagSpan<TTag>>(out var tagsToRemove);
            using var _2 = SegmentedListPool.GetPooledList<TagSpan<TTag>>(out var allTagsList);
            using var _3 = _tagSpanSetPool.GetPooledObject(out var allTagsSet);

            // Everything we're passing in here is synchronous.  So we can assert that this must complete synchronously
            // as well.
            var (oldTagTrees, newTagTrees, _) = CompareAndSwapTagTreesAsync(
                static async (oldTagTrees, args, _) =>
                {
                    var (@this, e, tagsToRemove, allTagsList, allTagsSet) = args;

                    // Compare-and-swap loops until we can successfully update the tag trees.  Clear out the collections
                    // so we're back in an initial state before performing any work in this lambda.
                    tagsToRemove.Clear();
                    allTagsList.Clear();
                    allTagsSet.Clear();

                    var snapshot = e.After;
                    var buffer = snapshot.TextBuffer;

                    if (oldTagTrees.TryGetValue(buffer, out var treeForBuffer))
                    {
                        foreach (var change in e.Changes)
                            treeForBuffer.AddIntersectingTagSpans(new SnapshotSpan(snapshot, change.NewSpan), tagsToRemove);

                        if (tagsToRemove.Count > 0)
                        {
                            // Determine the final tags for the interval tree, using a set so that we can efficiently
                            // remove the intersecting tags.
                            treeForBuffer.AddAllSpans(snapshot, allTagsSet);
                            allTagsSet.RemoveAll(tagsToRemove);

                            // Then, copy into a list so we can efficiently sort them and create the interval tree from
                            // those sorted items.
                            allTagsList.AddRange(allTagsSet);

                            var newTagTree = new TagSpanIntervalTree<TTag>(
                                snapshot,
                                @this._dataSource.SpanTrackingMode,
                                allTagsList);
                            return (oldTagTrees.SetItem(buffer, newTagTree), default(VoidResult));
                        }
                    }

                    // return oldTagTrees to indicate nothing changed.
                    return (oldTagTrees, default(VoidResult));
                },
                args: (this, e, tagsToRemove, allTagsList, allTagsSet),
                _disposalTokenSource.Token).VerifyCompleted();

            // Can happen if we were canceled.  Just bail out immediate.
            if (newTagTrees is null)
                return;

            // Nothing changed.  Bail out.
            if (oldTagTrees == newTagTrees)
                return;

            // Not sure why we are diffing when we already have tagsToRemove. is it due to _tagSpanComparer might return
            // different result than GetIntersectingSpans?
            //
            // treeForBuffer basically points to oldTagTrees. case where oldTagTrees not exist is already taken cared by
            // CachedTagTrees.TryGetValue.

            var snapshot = e.After;
            var buffer = snapshot.TextBuffer;

            var difference = ComputeDifference(snapshot, newTagTrees[buffer], oldTagTrees[buffer]);

            RaiseTagsChanged(buffer, difference);
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

        /// <summary>
        /// Spins, repeatedly calling into <paramref name="callback"/> with the current state of the tag trees.  When
        /// the result of the callback can be saved without any intervening writes to <see
        /// cref="_cachedTagTrees_mayChangeFromAnyThread"/> happening on another thread, then this helper returns. This
        /// helper may also returns <see langword="null"/> in the case of cancellation.
        /// </summary>
        private async Task<(ImmutableDictionary<ITextBuffer, TagSpanIntervalTree<TTag>> oldTagTrees, ImmutableDictionary<ITextBuffer, TagSpanIntervalTree<TTag>> newTagTrees, TResult)>
            CompareAndSwapTagTreesAsync<TArgs, TResult>(
                Func<ImmutableDictionary<ITextBuffer, TagSpanIntervalTree<TTag>>, TArgs, CancellationToken, ValueTask<(ImmutableDictionary<ITextBuffer, TagSpanIntervalTree<TTag>> newTagTrees, TResult result)>> callback,
                TArgs args,
                CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var oldTagTrees = _cachedTagTrees_mayChangeFromAnyThread;

                // Compute the new tag trees, based on what the current tag trees are.  Intentionally CA(true) here so
                // we stay on the UI thread if we're in a JTF blocking call.
                var (newTagTrees, newResult) = await callback(oldTagTrees, args, cancellationToken).ConfigureAwait(true);

                // Try to update the cached tag trees to what we computed.  If we win, we're done.  Otherwise, some
                // other thread was able to do this, and we need to try again.
                if (oldTagTrees != Interlocked.CompareExchange(ref _cachedTagTrees_mayChangeFromAnyThread, newTagTrees, oldTagTrees))
                    continue;

                return (oldTagTrees, newTagTrees, newResult);
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
        /// <param name="highPriority">If this tagging request should be processed as quickly as possible with no extra
        /// delays added for it.
        /// </param>
        /// <param name="calledFromJtfRun">If this method is being called from within a JTF.Run call.  This is used to
        /// ensure we don't do unnecessary switches to the threadpool while JTF is waiting on us.</param>
        private async Task<ImmutableDictionary<ITextBuffer, TagSpanIntervalTree<TTag>>?> RecomputeTagsAsync(
            bool highPriority,
            bool frozenPartialSemantics,
            bool calledFromJtfRun,
            CancellationToken cancellationToken)
        {
            // Note: this method is called in some blocking scenarios.  Specifically, when the outlining manager blocks
            // on outlining tags.  As such, we use ConfigureAwait(true) and NoThrowAwaitable(captureContext: true) to
            // ensure we're always coming back to the calling context as much as possible.  In the blocking case, this
            // is good, so we don't have unnecessary thread switches.  In the non-blocking threadpool case, this is also
            // fine as CA(true) will just keep us on the threadpool.

            // Enqueue work to a queue that will all tagger main thread work together in the near future. This let's
            // us avoid hammering the dispatcher queue with lots of work that causes contention.  Additionally, use
            // a no-throw awaitable so that in the common case where we cancel before, we don't throw an exception
            // that can exacerbate cross process debugging scenarios.
            var valueOpt = await _dataSource.MainThreadManager.PerformWorkOnMainThreadAsync(
                GetTaggerUIData, cancellationToken).ConfigureAwait(true);

            if (valueOpt is null)
            {
                // We failed to get the UI data we need.  This can happen in cases like trying to get that during a layout pass.
                // In that case, we just bail out and try again later.
                this.EnqueueWork(highPriority);
                return null;
            }

            var (isVisible, caretPosition, snapshotSpansToTag) = valueOpt.Value;

            // Since we don't ever throw above, check and see if the await completed due to cancellation and do not
            // proceed.
            if (cancellationToken.IsCancellationRequested)
                return null;

            // if we're tagging documents that are not visible, then introduce a long delay so that we avoid
            // consuming machine resources on work the user isn't likely to see.
            //
            // Don't do this for explicit high priority requests as the caller wants the UI updated as quickly as
            // possible.
            if (!highPriority && !isVisible)
            {
                // Use NoThrow as this is a high source of cancellation exceptions.  This avoids the exception and instead
                // bails gracefully by checking below.
                await _dataSource.VisibilityTracker.DelayWhileNonVisibleAsync(
                    _dataSource.ThreadingContext, _dataSource.AsyncListener, _subjectBuffer, DelayTimeSpan.NonFocus, cancellationToken).NoThrowAwaitable(captureContext: true);
            }

            using (Logger.LogBlock(FunctionId.Tagger_TagSource_RecomputeTags, cancellationToken))
            {
                if (cancellationToken.IsCancellationRequested)
                    return null;

                // If we're being called from within a blocking JTF.Run call, we don't want to switch to the background
                // if we can avoid it.
                if (!calledFromJtfRun)
                    await TaskScheduler.Default;

                if (cancellationToken.IsCancellationRequested)
                    return null;

                // Now that we're on the threadpool, figure out what documents we need to tag corresponding to those
                // SnapshotSpan the underlying data source asked us to tag.
                var spansToTag = GetDocumentSnapshotSpansToTag(snapshotSpansToTag, frozenPartialSemantics, cancellationToken);

                // Now spin, trying to compute the updated tags.  We only need to do this as the tag state is also
                // allowed to change on the UI thread (for example, taggers can say they want tags to be immediately
                // removed when an edit happens. So, we need to keep recomputing the tags until we win and become the
                // latest tags.
                var oldState = _state_accessOnlyFromEventChangeQueueCallback;

                var (oldTagTrees, newTagTrees, context) = await CompareAndSwapTagTreesAsync(
                    static async (oldTagTrees, args, cancellationToken) =>
                    {
                        var (@this, oldState, frozenPartialSemantics, spansToTag, snapshotSpansToTag, caretPosition) = args;

                        // Create a context to store pass the information along and collect the results.
                        var context = new TaggerContext<TTag>(
                            oldState, frozenPartialSemantics, spansToTag, snapshotSpansToTag, caretPosition, oldTagTrees);
                        await @this.ProduceTagsAsync(context, cancellationToken).ConfigureAwait(true);

                        return (@this.ComputeNewTagTrees(oldTagTrees, context), context);
                    },
                    (this, oldState, frozenPartialSemantics, spansToTag, snapshotSpansToTag, caretPosition),
                    cancellationToken).ConfigureAwait(true);

                // We may get back null if we were canceled.  Immediately bail out in that case.
                if (newTagTrees is null)
                    return null;

                // Once we assign our state, we're uncancellable.  We must report the changed information to the editor.
                // The only case where it's ok not to is if the tagger itself is disposed.  Null out our token so nothing
                // accidentally attempts to use it.
                cancellationToken = CancellationToken.None;

                var bufferToChanges = ProcessNewTagTrees(spansToTag, oldTagTrees, newTagTrees);

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

            // Returns null if we we're currently in a state where we can't get the tags to span and we should bail out
            // from producing tags on this turn of the crank.
            (bool isVisible, SnapshotPoint? caretPosition, OneOrMany<SnapshotSpan> spansToTag)? GetTaggerUIData()
            {
                _dataSource.ThreadingContext.ThrowIfNotOnUIThread();

                // Make a copy of all the data we need while we're on the foreground.  Then switch to a threadpool
                // thread to do the computation. Finally, once new tags have been computed, then we update our state
                // in a threadsafe fashion in the background.

                // Grab the visibility state of the view while we're already on the UI thread.  This saves an
                // unnecessary switch below.
                var isVisible = this.IsVisible();
                var caretPosition = _dataSource.GetCaretPoint(_textView, _subjectBuffer);

                using var spansToTag = TemporaryArray<SnapshotSpan>.Empty;
                if (!_dataSource.TryAddSpansToTag(_textView, _subjectBuffer, ref spansToTag.AsRef()))
                    return null;

#if DEBUG
                foreach (var snapshotSpan in spansToTag)
                    CheckSnapshot(snapshotSpan.Snapshot);
#endif

                return (isVisible, caretPosition, spansToTag.ToOneOrManyAndClear());
            }

            static OneOrMany<DocumentSnapshotSpan> GetDocumentSnapshotSpansToTag(
                OneOrMany<SnapshotSpan> snapshotSpansToTag,
                bool frozenPartialSemantics,
                CancellationToken cancellationToken)
            {
                // We only ever have a tiny number of snapshots we're classifying.  So it's easier and faster to just store
                // the mapping from it to a particular document in an on-stack array.
                //
                // document can be null if the buffer the given span is part of is not part of our workspace.
                using var snapshotToDocument = TemporaryArray<(ITextSnapshot snapshot, Document? document)>.Empty;

                using var result = TemporaryArray<DocumentSnapshotSpan>.Empty;

                foreach (var spanToTag in snapshotSpansToTag)
                {
                    var snapshot = spanToTag.Snapshot;
                    var (foundSnapshot, document) = snapshotToDocument.FirstOrDefault(
                        static (t, snapshot) => t.snapshot == snapshot, snapshot);

                    // If this is the first time looking at this snapshot, then go fetch the document (which we may or
                    // may not have), and freeze it if necessary..
                    if (foundSnapshot is null)
                    {
                        document = snapshot.GetOpenDocumentInCurrentContextWithChanges();
                        if (frozenPartialSemantics)
                            document = document?.WithFrozenPartialSemantics(cancellationToken);

                        snapshotToDocument.Add((snapshot, document));
                    }

                    result.Add(new DocumentSnapshotSpan(document, spanToTag));
                }

                return result.ToOneOrManyAndClear();
            }

#if DEBUG
            static void CheckSnapshot(ITextSnapshot snapshot)
            {
                var container = snapshot.TextBuffer.AsTextContainer();
                if (Workspace.TryGetWorkspace(container, out _))
                {
                    // if the buffer is part of our workspace, it must be the latest.
                    Debug.Assert(snapshot.Version.Next == null, "should be on latest snapshot");
                }
            }
#endif
        }

        private ImmutableDictionary<ITextBuffer, TagSpanIntervalTree<TTag>> ComputeNewTagTrees(
            ImmutableDictionary<ITextBuffer, TagSpanIntervalTree<TTag>> oldTagTrees,
            TaggerContext<TTag> context)
        {
            using var _1 = PooledHashSet<ITextBuffer>.GetInstance(out var buffersToTag);
            foreach (var spanToTag in context.SpansToTag)
                buffersToTag.Add(spanToTag.SnapshotSpan.Snapshot.TextBuffer);

            using var _2 = SegmentedListPool.GetPooledList<TagSpan<TTag>>(out var newTagsInBuffer_safeToMutate);
            using var _3 = ArrayBuilder<SnapshotSpan>.GetInstance(out var spansToInvalidateInBuffer);

            var newTagTrees = ImmutableDictionary.CreateBuilder<ITextBuffer, TagSpanIntervalTree<TTag>>();
            foreach (var buffer in buffersToTag)
            {
                newTagsInBuffer_safeToMutate.Clear();
                spansToInvalidateInBuffer.Clear();

                // Ignore any tag spans reported for any buffers we weren't interested in.

                foreach (var tagSpan in context.TagSpans)
                {
                    if (tagSpan.Span.Snapshot.TextBuffer == buffer)
                        newTagsInBuffer_safeToMutate.Add(tagSpan);
                }

                // Invalidate all the spans that were actually tagged.  If the context doesn't have any recorded spans
                // that were tagged, then assume we tagged everything we were asked to tag.
                foreach (var span in context._spansTagged)
                {
                    if (span.Snapshot.TextBuffer == buffer)
                        spansToInvalidateInBuffer.Add(span);
                }

                // Note: newTagsInBuffer_safeToMutate will be mutated by ComputeNewTagTree.  This is fine as we don't
                // use it after this and immediately clear it on the next iteration of the loop (or dispose of it once
                // the loop finishes).
                var newTagTree = ComputeNewTagTree(oldTagTrees, buffer, newTagsInBuffer_safeToMutate, spansToInvalidateInBuffer);
                if (newTagTree != null)
                    newTagTrees.Add(buffer, newTagTree);
            }

            return newTagTrees.ToImmutable();
        }

        private TagSpanIntervalTree<TTag>? ComputeNewTagTree(
            ImmutableDictionary<ITextBuffer, TagSpanIntervalTree<TTag>> oldTagTrees,
            ITextBuffer textBuffer,
            SegmentedList<TagSpan<TTag>> newTags_safeToMutate,
            ArrayBuilder<SnapshotSpan> spansToInvalidate)
        {
            var noNewTags = newTags_safeToMutate.Count == 0;
            var noSpansToInvalidate = spansToInvalidate.IsEmpty;
            oldTagTrees.TryGetValue(textBuffer, out var oldTagTree);

            if (oldTagTree == null)
            {
                // If we have no new tags, and no old tags either.  No need to store anything for this buffer.
                if (noNewTags)
                    return null;

                // If we don't have any old tags then we just need to return the new tags.
                return new TagSpanIntervalTree<TTag>(newTags_safeToMutate[0].Span.Snapshot, _dataSource.SpanTrackingMode, newTags_safeToMutate);
            }

            // If we don't have any new tags, and there was nothing to invalidate, then we can 
            // keep whatever old tags we have without doing any additional work.
            if (noNewTags && noSpansToInvalidate)
                return oldTagTree;

            if (noSpansToInvalidate)
            {
                // If we have no spans to invalidate, then we can just keep the old tags and add the new tags.
                var snapshot = newTags_safeToMutate.First().Span.Snapshot;

                // For efficiency, just grab the old tags, remap them to the current snapshot, and place them in the
                // newTags buffer.  This is a safe mutation of this buffer as the caller doesn't use it after this point
                // and instead immediately clears it.
                oldTagTree.AddAllSpans(snapshot, newTags_safeToMutate);
                return new TagSpanIntervalTree<TTag>(
                    snapshot, _dataSource.SpanTrackingMode, newTags_safeToMutate);
            }
            else
            {
                // We do have spans to invalidate. Get the set of old tags that don't intersect with those and add the new tags.
                using var _1 = _tagSpanSetPool.GetPooledObject(out var nonIntersectingOldTags);

                var firstSpanToInvalidate = spansToInvalidate.First();
                var snapshot = firstSpanToInvalidate.Snapshot;

                // Performance: No need to fully realize spansToInvalidate or do any of the calculations below if the
                // full snapshot is being invalidated.
                if (firstSpanToInvalidate.Length != snapshot.Length)
                {
                    oldTagTree.AddAllSpans(snapshot, nonIntersectingOldTags);
                    oldTagTree.RemoveIntersectingTagSpans(spansToInvalidate, nonIntersectingOldTags);
                }

                // For efficiency, add the non-intersecting old tags to the new tags buffer.  This is a safe mutation of
                // of that buffer as it is not used by us or our caller after this point.
                newTags_safeToMutate.AddRange(nonIntersectingOldTags);
                return new TagSpanIntervalTree<TTag>(
                    snapshot, _dataSource.SpanTrackingMode, newTags_safeToMutate);
            }
        }

        private async ValueTask ProduceTagsAsync(TaggerContext<TTag> context, CancellationToken cancellationToken)
        {
            // If we have no spans to tag, there's no point in continuing.
            if (context.SpansToTag.IsEmpty)
                return;

            // If the feature is disabled, then just produce no tags.
            var languageName = _subjectBuffer.GetLanguageName();
            foreach (var option in _dataSource.Options)
            {
                if (option is Option2<bool> option2 && !_dataSource.GlobalOptions.GetOption(option2))
                    return;

                if (option is PerLanguageOption2<bool> perLanguageOption &&
                    (languageName == null || !_dataSource.GlobalOptions.GetOption(perLanguageOption, languageName)))
                {
                    return;
                }
            }

            await _dataSource.ProduceTagsAsync(context, cancellationToken).ConfigureAwait(false);
        }

        private Dictionary<ITextBuffer, DiffResult> ProcessNewTagTrees(
            OneOrMany<DocumentSnapshotSpan> spansToTag,
            ImmutableDictionary<ITextBuffer, TagSpanIntervalTree<TTag>> oldTagTrees,
            ImmutableDictionary<ITextBuffer, TagSpanIntervalTree<TTag>> newTagTrees)
        {
            using (Logger.LogBlock(FunctionId.Tagger_TagSource_ProcessNewTags, CancellationToken.None))
            {
                var bufferToChanges = new Dictionary<ITextBuffer, DiffResult>();

                foreach (var (latestBuffer, latestSpans) in newTagTrees)
                {
                    var snapshot = spansToTag.FirstOrDefault(
                        static (span, latestBuffer) => span.SnapshotSpan.Snapshot.TextBuffer == latestBuffer,
                        latestBuffer).SnapshotSpan.Snapshot;
                    Contract.ThrowIfNull(snapshot);

                    if (oldTagTrees.TryGetValue(latestBuffer, out var previousSpans))
                    {
                        var difference = ComputeDifference(snapshot, latestSpans, previousSpans);
                        bufferToChanges[latestBuffer] = difference;
                    }
                    else
                    {
                        // It's a new buffer, so report all spans are changed
                        bufferToChanges[latestBuffer] = new DiffResult(added: latestSpans.GetSnapshotSpanCollection(snapshot), removed: null);
                    }
                }

                foreach (var (oldBuffer, previousSpans) in oldTagTrees)
                {
                    if (!newTagTrees.ContainsKey(oldBuffer))
                    {
                        // This buffer disappeared, so let's notify that the old tags are gone
                        bufferToChanges[oldBuffer] = new DiffResult(added: null, removed: previousSpans.GetSnapshotSpanCollection(oldBuffer.CurrentSnapshot));
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
            using var _1 = SegmentedListPool.GetPooledList<(SnapshotSpan, TTag)>(out var latestSpans);
            using var _2 = SegmentedListPool.GetPooledList<(SnapshotSpan, TTag)>(out var previousSpans);

            using var _3 = ArrayBuilder<SnapshotSpan>.GetInstance(out var added);
            using var _4 = ArrayBuilder<SnapshotSpan>.GetInstance(out var removed);

            latestTree.AddAllSpans(snapshot, latestSpans);
            previousTree.AddAllSpans(snapshot, previousSpans);

            var latestEnumerator = latestSpans.GetEnumerator();
            var previousEnumerator = previousSpans.GetEnumerator();

            var latest = NextOrNull(ref latestEnumerator);
            var previous = NextOrNull(ref previousEnumerator);

            while (latest != null && previous != null)
            {
                var latestSpan = latest.Value.Span;
                var previousSpan = previous.Value.Span;

                if (latestSpan.Start < previousSpan.Start)
                {
                    added.Add(latestSpan);
                    latest = NextOrNull(ref latestEnumerator);
                }
                else if (previousSpan.Start < latestSpan.Start)
                {
                    removed.Add(previousSpan);
                    previous = NextOrNull(ref previousEnumerator);
                }
                else
                {
                    // If the starts are the same, but the ends are different, report the larger
                    // region to be conservative.
                    if (previousSpan.End > latestSpan.End)
                    {
                        removed.Add(previousSpan);
                        latest = NextOrNull(ref latestEnumerator);
                    }
                    else if (latestSpan.End > previousSpan.End)
                    {
                        added.Add(latestSpan);
                        previous = NextOrNull(ref previousEnumerator);
                    }
                    else
                    {
                        if (!_dataSource.TagEquals(latest.Value.Tag, previous.Value.Tag))
                            added.Add(latestSpan);

                        latest = NextOrNull(ref latestEnumerator);
                        previous = NextOrNull(ref previousEnumerator);
                    }
                }
            }

            while (latest != null)
            {
                added.Add(latest.Value.Span);
                latest = NextOrNull(ref latestEnumerator);
            }

            while (previous != null)
            {
                removed.Add(previous.Value.Span);
                previous = NextOrNull(ref previousEnumerator);
            }

            return new DiffResult(new(added), new(removed));

            static (SnapshotSpan Span, TTag Tag)? NextOrNull(ref SegmentedList<(SnapshotSpan, TTag)>.Enumerator enumerator)
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

            // We can get null back if we were canceled.
            if (tagTrees is null)
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
