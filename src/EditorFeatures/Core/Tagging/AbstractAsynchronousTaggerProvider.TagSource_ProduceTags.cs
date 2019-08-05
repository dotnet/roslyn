// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Tagging
{
    internal partial class AbstractAsynchronousTaggerProvider<TTag>
    {
        /// <summary>
        /// <para>The <see cref="TagSource"/> is the core part of our asynchronous
        /// tagging infrastructure. It is the coordinator between <see cref="ProduceTagsAsync(TaggerContext{TTag})"/>s,
        /// <see cref="ITaggerEventSource"/>s, and <see cref="ITagger{T}"/>s.</para>
        /// 
        /// <para>The <see cref="TagSource"/> is the type that actually owns the
        /// list of cached tags. When an <see cref="ITaggerEventSource"/> says tags need to be  recomputed,
        /// the tag source starts the computation and calls <see cref="ProduceTagsAsync(TaggerContext{TTag})"/> to build
        /// the new list of tags. When that's done, the tags are stored in <see cref="CachedTagTrees"/>. The 
        /// tagger, when asked for tags from the editor, then returns the tags that are stored in 
        /// <see cref="CachedTagTrees"/></para>
        /// 
        /// <para>There is a one-to-many relationship between <see cref="TagSource"/>s
        /// and <see cref="ITagger{T}"/>s. Special cases, like reference highlighting (which processes multiple
        /// subject buffers at once) have their own providers and tag source derivations.</para>
        /// </summary>
        private partial class TagSource
        {
            private void OnUIUpdatesPaused(object sender, EventArgs e)
            {
                _workQueue.AssertIsForeground();
                _previousCachedTagTrees = CachedTagTrees;

                RaisePaused();
            }

            private void OnUIUpdatesResumed(object sender, EventArgs e)
            {
                _workQueue.AssertIsForeground();
                _previousCachedTagTrees = null;

                RaiseResumed();
            }

            private void OnEventSourceChanged(object sender, TaggerEventArgs e)
            {
                var result = Interlocked.CompareExchange(ref _seenEventSourceChanged, value: 1, comparand: 0);
                if (result == 0)
                {
                    // this is the first time we're hearing about changes from our event-source.
                    // Don't have any delay here.  We want to just compute the tags and display
                    // them as soon as we possibly can.
                    ComputeInitialTags();
                }
                else
                {
                    // First, cancel any previous requests (either still queued, or started).  We no longer
                    // want to continue it if new changes have come in.
                    _workQueue.CancelCurrentWork();
                    RegisterNotification(
                        () => RecomputeTagsForeground(initialTags: false),
                        (int)e.Delay.ComputeTimeDelay().TotalMilliseconds,
                        GetCancellationToken(initialTags: false));
                }
            }

            private void OnCaretPositionChanged(object sender, CaretPositionChangedEventArgs e)
            {
                this.AssertIsForeground();

                Debug.Assert(_dataSource.CaretChangeBehavior.HasFlag(TaggerCaretChangeBehavior.RemoveAllTagsOnCaretMoveOutsideOfTag));

                var caret = _dataSource.GetCaretPoint(_textViewOpt, _subjectBuffer);
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
                this.AssertIsForeground();

                var oldTagTrees = this.CachedTagTrees;
                this.CachedTagTrees = ImmutableDictionary<ITextBuffer, TagSpanIntervalTree<TTag>>.Empty;

                var snapshot = _subjectBuffer.CurrentSnapshot;
                var oldTagTree = GetTagTree(snapshot, oldTagTrees);

                // everything from old tree is removed.
                RaiseTagsChanged(snapshot.TextBuffer, new DiffResult(added: null, removed: oldTagTree.GetSpans(snapshot).Select(s => s.Span)));
            }

            private void OnSubjectBufferChanged(object sender, TextContentChangedEventArgs e)
            {
                _workQueue.AssertIsForeground();
                UpdateTagsForTextChange(e);
                AccumulateTextChanges(e);
            }

            private void AccumulateTextChanges(TextContentChangedEventArgs contentChanged)
            {
                _workQueue.AssertIsForeground();
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
                        var textChangeRanges = new TextChangeRange[count];
                        for (var i = 0; i < count; i++)
                        {
                            var c = contentChanges[i];
                            textChangeRanges[i] = new TextChangeRange(new TextSpan(c.OldSpan.Start, c.OldSpan.Length), c.NewLength);
                        }

                        this.AccumulatedTextChanges = this.AccumulatedTextChanges.Accumulate(textChangeRanges);
                        break;
                }
            }

            private void UpdateTagsForTextChange(TextContentChangedEventArgs e)
            {
                _workQueue.AssertIsForeground();

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
                if (!e.Changes.Any())
                {
                    return;
                }

                // We might be able to steal the cached tags from another tag source
                if (TryStealTagsFromRelatedTagSource(e))
                {
                    return;
                }

                var buffer = e.After.TextBuffer;
                if (!this.CachedTagTrees.TryGetValue(buffer, out var treeForBuffer))
                {
                    return;
                }

                var tagsToRemove = e.Changes.SelectMany(c => treeForBuffer.GetIntersectingSpans(new SnapshotSpan(e.After, c.NewSpan)));
                if (!tagsToRemove.Any())
                {
                    return;
                }

                var allTags = treeForBuffer.GetSpans(e.After).ToList();
                var newTagTree = new TagSpanIntervalTree<TTag>(
                    buffer,
                    treeForBuffer.SpanTrackingMode,
                    allTags.Except(tagsToRemove, _tagSpanComparer));

                var snapshot = e.After;

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

            private bool TryStealTagsFromRelatedTagSource(TextContentChangedEventArgs e)
            {
                // see bug 778731
#if INTERACTIVE
            // If we don't have a way to find the related buffer, we're done immediately
            if (bufferToRelatedTagSource == null)
            {
                return false;
            }

            // We can only steal tags if we know where the edit came from, so do we?
            var editTag = e.EditTag as RestoreHistoryEditTag;

            if (editTag == null)
            {
                return false;
            }

            var originalSpan = editTag.OriginalSpan;

            var relatedTagSource = bufferToRelatedTagSource(originalSpan.Snapshot.TextBuffer);
            if (relatedTagSource == null)
            {
                return false;
            }

            // Reading the other tag source's cached tags is safe, since this field is allowed to be
            // accessed from multiple threads and is immutable. We still need to have a local copy
            // though to play it safe and be a good citizen (well, as good as a citizen that's about
            // to steal something can be...)
            var relatedCachedTags = relatedTagSource.cachedTags;
            TagSpanIntervalTree<TTag> relatedIntervalTree;

            if (!relatedCachedTags.TryGetValue(originalSpan.Snapshot.TextBuffer, out relatedIntervalTree))
            {
                return false;
            }

            // Excellent! Let's build a new interval tree with these tags mapped to our buffer
            // instead
            var tagsForThisBuffer = from tagSpan in relatedIntervalTree.GetSpans(originalSpan.Snapshot)
                                    where tagSpan.Span.IntersectsWith(originalSpan)
                                    let snapshotSpan = new SnapshotSpan(e.After, tagSpan.SpanStart - originalSpan.Start, tagSpan.Span.Length)
                                    select new TagSpan<TTag>(snapshotSpan, tagSpan.Tag);

            var intervalTreeForThisBuffer = new TagSpanIntervalTree<TTag>(e.After.TextBuffer, relatedIntervalTree.SpanTrackingMode, tagsForThisBuffer);

            // Update our cached tags
            UpdateCachedTagsForBuffer(e.After, intervalTreeForThisBuffer);
            return true;
#else
                return false;
#endif
            }

            /// <summary>
            /// Called on the foreground thread.  Passed a boolean to say if we're computing the
            /// initial set of tags or not.  If we're computing the initial set of tags, we lower
            /// all our delays so that we can get results to the screen as quickly as possible.
            /// 
            /// This gives a good experience when a document is opened as the document appears
            /// complete almost immediately.  Once open though, our normal delays come into play
            /// so as to not cause a flashy experience.
            /// </summary>
            private void RecomputeTagsForeground(bool initialTags)
            {
                _workQueue.AssertIsForeground();

                using (Logger.LogBlock(FunctionId.Tagger_TagSource_RecomputeTags, CancellationToken.None))
                {
                    // Stop any existing work we're currently engaged in
                    _workQueue.CancelCurrentWork();

                    // Mark that we're not up to date. We'll remain in that state until the next 
                    // tag production stage finally completes.
                    this.UpToDate = false;

                    var cancellationToken = GetCancellationToken(initialTags);
                    var spansToTag = GetSpansAndDocumentsToTag();

                    // Make a copy of all the data we need while we're on the foreground.  Then
                    // pass it along everywhere needed.  Finally, once new tags have been computed,
                    // then we update our state again on the foreground.
                    var caretPosition = _dataSource.GetCaretPoint(_textViewOpt, _subjectBuffer);
                    var textChangeRange = this.AccumulatedTextChanges;
                    var oldTagTrees = this.CachedTagTrees;
                    var oldState = this.State;

                    _workQueue.EnqueueBackgroundTask(
                        ct => this.RecomputeTagsAsync(
                            oldState, caretPosition, textChangeRange, spansToTag, oldTagTrees, initialTags, ct),
                        GetType().Name + ".RecomputeTags", cancellationToken);
                }
            }

            /// <summary>
            /// Get's the cancellation token that will control the processing of this set of
            /// tags. If this is the initial set of tags, we have a single cancellation token
            /// that can't be interrupted *unless* the entire tagger is shut down.  If this
            /// is anything after the initial set of tags, then we'll control things with a
            /// cancellation token that is triggered every time we hear about new changes.
            /// 
            /// This is a 'kick the can down the road' approach whereby we keep delaying
            /// producing tags (and updating the UI) until a reasonable pause has happened.
            /// This approach helps prevent flashing in the UI.
            /// </summary>
            private CancellationToken GetCancellationToken(bool initialTags)
                => initialTags
                    ? _initialComputationCancellationTokenSource.Token
                    : _workQueue.CancellationToken;

            private ImmutableArray<DocumentSnapshotSpan> GetSpansAndDocumentsToTag()
            {
                _workQueue.AssertIsForeground();

                // TODO: Update to tag spans from all related documents.

                var snapshotToDocumentMap = new Dictionary<ITextSnapshot, Document>();
                var spansToTag = _dataSource.GetSpansToTag(_textViewOpt, _subjectBuffer);

                var spansAndDocumentsToTag = spansToTag.Select(span =>
                {
                    if (!snapshotToDocumentMap.TryGetValue(span.Snapshot, out var document))
                    {
                        CheckSnapshot(span.Snapshot);

                        document = span.Snapshot.GetOpenDocumentInCurrentContextWithChanges();
                        snapshotToDocumentMap[span.Snapshot] = document;
                    }

                    // document can be null if the buffer the given span is part of is not part of our workspace.
                    return new DocumentSnapshotSpan(document, span);
                }).ToImmutableArray();

                return spansAndDocumentsToTag;
            }

            [Conditional("DEBUG")]
            private void CheckSnapshot(ITextSnapshot snapshot)
            {
                var container = snapshot.TextBuffer.AsTextContainer();
                if (Workspace.TryGetWorkspace(container, out _))
                {
                    // if the buffer is part of our workspace, it must be the latest.
                    Debug.Assert(snapshot.Version.Next == null, "should be on latest snapshot");
                }
            }

            private ImmutableDictionary<ITextBuffer, TagSpanIntervalTree<TTag>> ConvertToTagTrees(
                ImmutableDictionary<ITextBuffer, TagSpanIntervalTree<TTag>> oldTagTrees,
                ISet<ITextBuffer> buffersToTag,
                ILookup<ITextBuffer, ITagSpan<TTag>> newTagsByBuffer,
                IEnumerable<DocumentSnapshotSpan> spansTagged)
            {
                var spansToInvalidateByBuffer = spansTagged.ToLookup(
                    keySelector: span => span.SnapshotSpan.Snapshot.TextBuffer,
                    elementSelector: span => span.SnapshotSpan);

                // Walk through each relevant buffer and decide what the interval tree should be
                // for that buffer.  In general this will work by keeping around old tags that
                // weren't in the range that was re-tagged, and merging them with the new tags
                // produced for the range that was re-tagged.
                var newTagTrees = ImmutableDictionary<ITextBuffer, TagSpanIntervalTree<TTag>>.Empty;
                foreach (var buffer in buffersToTag)
                {
                    var newTagTree = ComputeNewTagTree(oldTagTrees, buffer, newTagsByBuffer[buffer], spansToInvalidateByBuffer[buffer]);
                    if (newTagTree != null)
                    {
                        newTagTrees = newTagTrees.Add(buffer, newTagTree);
                    }
                }

                return newTagTrees;
            }

            private TagSpanIntervalTree<TTag> ComputeNewTagTree(
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
                var snapshot = spansToInvalidate.First().Snapshot;

                var tagSpansToInvalidate = new List<ITagSpan<TTag>>(
                    spansToInvalidate.SelectMany(ss => oldTagTree.GetIntersectingSpans(ss)));

                return oldTagTree.GetSpans(snapshot).Except(tagSpansToInvalidate, _tagSpanComparer);
            }

            private async Task RecomputeTagsAsync(
                object oldState,
                SnapshotPoint? caretPosition,
                TextChangeRange? textChangeRange,
                ImmutableArray<DocumentSnapshotSpan> spansToTag,
                ImmutableDictionary<ITextBuffer, TagSpanIntervalTree<TTag>> oldTagTrees,
                bool initialTags,
                CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var context = new TaggerContext<TTag>(
                    oldState, spansToTag, caretPosition, textChangeRange, oldTagTrees, cancellationToken);
                await ProduceTagsAsync(context).ConfigureAwait(false);

                ProcessContext(oldTagTrees, context, initialTags);
            }

            private bool ShouldSkipTagProduction()
            {
                var options = _dataSource.Options ?? SpecializedCollections.EmptyEnumerable<Option<bool>>();
                var perLanguageOptions = _dataSource.PerLanguageOptions ?? SpecializedCollections.EmptyEnumerable<PerLanguageOption<bool>>();

                return options.Any(option => !_subjectBuffer.GetFeatureOnOffOption(option)) ||
                       perLanguageOptions.Any(option => !_subjectBuffer.GetFeatureOnOffOption(option));
            }

            private Task ProduceTagsAsync(TaggerContext<TTag> context)
            {
                if (ShouldSkipTagProduction())
                {
                    // If the feature is disabled, then just produce no tags.
                    return Task.CompletedTask;
                }

                return _dataSource.ProduceTagsAsync(context);
            }

            private void ProduceTagsSynchronously(TaggerContext<TTag> context)
            {
                if (ShouldSkipTagProduction())
                {
                    return;
                }

                _dataSource.ProduceTagsSynchronously(context);
            }

            private void ProcessContext(
                ImmutableDictionary<ITextBuffer, TagSpanIntervalTree<TTag>> oldTagTrees,
                TaggerContext<TTag> context,
                bool initialTags)
            {
                var buffersToTag = context.SpansToTag.Select(dss => dss.SnapshotSpan.Snapshot.TextBuffer).ToSet();

                // Ignore any tag spans reported for any buffers we weren't interested in.
                var newTagsByBuffer = context.tagSpans.Where(ts => buffersToTag.Contains(ts.Span.Snapshot.TextBuffer))
                                                      .ToLookup(t => t.Span.Snapshot.TextBuffer);

                var newTagTrees = ConvertToTagTrees(oldTagTrees, buffersToTag, newTagsByBuffer, context._spansTagged);
                ProcessNewTagTrees(
                    context.SpansToTag, oldTagTrees, newTagTrees,
                    context.State, initialTags, context.CancellationToken);
            }

            private void ProcessNewTagTrees(
                ImmutableArray<DocumentSnapshotSpan> spansToTag,
                ImmutableDictionary<ITextBuffer, TagSpanIntervalTree<TTag>> oldTagTrees,
                ImmutableDictionary<ITextBuffer, TagSpanIntervalTree<TTag>> newTagTrees,
                object newState,
                bool initialTags,
                CancellationToken cancellationToken)
            {
                var bufferToChanges = new Dictionary<ITextBuffer, DiffResult>();
                using (Logger.LogBlock(FunctionId.Tagger_TagSource_ProcessNewTags, cancellationToken))
                {
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
                            bufferToChanges[latestBuffer] = new DiffResult(added: latestSpans.GetSpans(snapshot).Select(t => t.Span), removed: null);
                        }
                    }

                    foreach (var (oldBuffer, previousSpans) in oldTagTrees)
                    {
                        if (!newTagTrees.ContainsKey(oldBuffer))
                        {
                            // This buffer disappeared, so let's notify that the old tags are gone
                            bufferToChanges[oldBuffer] = new DiffResult(added: null, removed: previousSpans.GetSpans(oldBuffer.CurrentSnapshot).Select(t => t.Span));
                        }
                    }
                }

                if (_workQueue.IsForeground())
                {
                    // If we're on the foreground already, we can just update our internal state directly.
                    UpdateStateAndReportChanges(newTagTrees, bufferToChanges, newState, initialTags);
                }
                else
                {
                    // Otherwise report back on the foreground asap to update the state and let our 
                    // clients know about the change.
                    RegisterNotification(() => UpdateStateAndReportChanges(
                        newTagTrees, bufferToChanges, newState, initialTags),
                        delay: 0,
                        cancellationToken: cancellationToken);
                }
            }

            private void UpdateStateAndReportChanges(
                ImmutableDictionary<ITextBuffer, TagSpanIntervalTree<TTag>> newTagTrees,
                Dictionary<ITextBuffer, DiffResult> bufferToChanges,
                object newState,
                bool initialTags)
            {
                _workQueue.AssertIsForeground();

                // Now that we're back on the UI thread, we can safely update our state with
                // what we've computed.  There is no concern with race conditions now.  For 
                // example, say that another change happened between the time when we 
                // registered for UpdateStateAndReportChanges and now.  If we processed that
                // notification (on the UI thread) first, then our cancellation token would 
                // have been triggered, and the foreground notification service would not 
                // call into this method. 
                // 
                // If, instead, we did get called into, then we will update our instance state.
                // Then when the foreground notification service runs RecomputeTagsForeground
                // it will see that state and use it as the new basis on which to compute diffs
                // and whatnot.
                this.CachedTagTrees = newTagTrees;
                this.AccumulatedTextChanges = null;
                this.State = newState;

                // Mark that we're up to date.  If any accurate taggers come along, they can use our
                // cached information.
                this.UpToDate = true;

                // Note: we're raising changes here on the UI thread.  However, this doesn't actually
                // mean we'll be notifying the editor.  Instead, these will be batched up in the 
                // AsynchronousTagger's BatchChangeNotifier.  If we tell it about enough changes
                // to a file, it will coalesce them into one large change to keep chattiness with
                // the editor down.
                RaiseTagsChanged(bufferToChanges, initialTags);
            }

            private DiffResult ComputeDifference(
                ITextSnapshot snapshot,
                TagSpanIntervalTree<TTag> latestSpans,
                TagSpanIntervalTree<TTag> previousSpans)
            {
                return Difference(latestSpans.GetSpans(snapshot), previousSpans.GetSpans(snapshot), _dataSource.TagComparer);
            }

            /// <summary>
            /// Returns the TagSpanIntervalTree containing the tags for the given buffer. If no tags
            /// exist for the buffer at all, null is returned.
            /// </summary>
            public TagSpanIntervalTree<TTag> TryGetTagIntervalTreeForBuffer(ITextBuffer buffer)
            {
                _workQueue.AssertIsForeground();

                // If we're currently pausing updates to the UI, then just use the tags we had before we
                // were paused so that nothing changes.  
                //
                // We're on the UI thread, so it's safe to access these variables.
                var map = _previousCachedTagTrees ?? this.CachedTagTrees;
                map.TryGetValue(buffer, out var tags);
                return tags;
            }

            public TagSpanIntervalTree<TTag> GetAccurateTagIntervalTreeForBuffer(ITextBuffer buffer, CancellationToken cancellationToken)
            {
                _workQueue.AssertIsForeground();

                if (!this.UpToDate)
                {
                    // We're not up to date.  That means we have an outstanding update that we're 
                    // currently processing.  Unfortunately we have no way to track the progress of
                    // that update (i.e. a Task).  Also, even if we did, we'd have the problem that 
                    // we have delays coded into the normal tagging process.  So waiting on that Task
                    // could take a long time.
                    //
                    // So, instead, we just cancel whatever work we're currently doing, and we just
                    // compute the results synchronously in this call.

                    // We can cancel any background computations currently happening
                    _workQueue.CancelCurrentWork();

                    var spansToTag = GetSpansAndDocumentsToTag();

                    // Safe to access _cachedTagTrees here.  We're on the UI thread.
                    var oldTagTrees = this.CachedTagTrees;
                    var caretPoint = _dataSource.GetCaretPoint(_textViewOpt, _subjectBuffer);

                    var context = new TaggerContext<TTag>(
                        this.State, spansToTag, caretPoint, this.AccumulatedTextChanges, oldTagTrees, cancellationToken);

                    ProduceTagsSynchronously(context);

                    ProcessContext(oldTagTrees, context, initialTags: false);
                }

                Debug.Assert(this.UpToDate);
                this.CachedTagTrees.TryGetValue(buffer, out var tags);
                return tags;
            }
        }
    }
}
