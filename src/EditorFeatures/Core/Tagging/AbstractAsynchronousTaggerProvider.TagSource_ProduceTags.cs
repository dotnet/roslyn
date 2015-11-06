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
                this._workQueue.AssertIsForeground();
                _previousCachedTagTrees = CachedTagTrees;

                RaisePaused();
            }

            private void OnUIUpdatesResumed(object sender, EventArgs e)
            {
                this._workQueue.AssertIsForeground();
                _previousCachedTagTrees = null;

                RaiseResumed();
            }

            private void OnChanged(object sender, TaggerEventArgs e)
            {
                RecalculateTagsOnChanged(e);
            }

            private void OnCaretPositionChanged(object sender, CaretPositionChangedEventArgs e)
            {
                this.AssertIsForeground();

                Debug.Assert(_dataSource.CaretChangeBehavior.HasFlag(TaggerCaretChangeBehavior.RemoveAllTagsOnCaretMoveOutsideOfTag));

                var caret = _dataSource.GetCaretPoint(_textViewOpt, _subjectBuffer);
                if (caret.HasValue)
                {
                    // If it changed position and we're still in a tag, there's nothing more to do
                    var currentTags = GetTagIntervalTreeForBuffer(caret.Value.Snapshot.TextBuffer);
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

                var snapshot = this._subjectBuffer.CurrentSnapshot;
                var oldTagTree = GetTagTree(snapshot, oldTagTrees);
                var newTagTree = GetTagTree(snapshot, this.CachedTagTrees);

                var difference = ComputeDifference(snapshot, newTagTree, oldTagTree);
                RaiseTagsChanged(snapshot.TextBuffer, difference);
            }

            private void OnSubjectBufferChanged(object sender, TextContentChangedEventArgs e)
            {
                this._workQueue.AssertIsForeground();
                UpdateTagsForTextChange(e);
                AccumulateTextChanges(e);
            }

            private void AccumulateTextChanges(TextContentChangedEventArgs contentChanged)
            {
                this._workQueue.AssertIsForeground();
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
                        for (int i = 0; i < count; i++)
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
                this._workQueue.AssertIsForeground();

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
                TagSpanIntervalTree<TTag> treeForBuffer;
                if (!this.CachedTagTrees.TryGetValue(buffer, out treeForBuffer))
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

                var oldTagTrees = this.CachedTagTrees;
                this.CachedTagTrees = oldTagTrees.SetItem(snapshot.TextBuffer, newTagTree);

                // Grab our old tags. We might not have any, so in this case we'll just pretend it's
                // empty
                var oldTagTree = GetTagTree(snapshot, oldTagTrees);

                var difference = ComputeDifference(snapshot, newTagTree, oldTagTree);
                RaiseTagsChanged(snapshot.TextBuffer, difference);
            }

            private TagSpanIntervalTree<TTag> GetTagTree(ITextSnapshot snapshot, ImmutableDictionary<ITextBuffer, TagSpanIntervalTree<TTag>> tagTrees)
            {
                TagSpanIntervalTree<TTag> tagTree = null;
                return tagTrees.TryGetValue(snapshot.TextBuffer, out tagTree)
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
            /// Called on the foreground thread.
            /// </summary>
            private void RecomputeTagsForeground()
            {
                this._workQueue.AssertIsForeground();

                using (Logger.LogBlock(FunctionId.Tagger_TagSource_RecomputeTags, CancellationToken.None))
                {
                    // Stop any existing work we're currently engaged in
                    this._workQueue.CancelCurrentWork();

                    // Mark that we're not up to date. We'll remain in that state until the next 
                    // tag production stage finally completes.
                    this.UpToDate = false;
                    var cancellationToken = this._workQueue.CancellationToken;

                    var spansToTag = GetSpansAndDocumentsToTag();

                    // Make a copy of all the data we need while we're on the foreground.  Then
                    // pass it along everywhere needed.  Finally, once new tags have been computed,
                    // then we update our state again on the foreground.
                    var caretPosition = _dataSource.GetCaretPoint(_textViewOpt, _subjectBuffer);
                    var textChangeRange = this.AccumulatedTextChanges;
                    var oldTagTrees = this.CachedTagTrees;
                    var oldState = this.State;

                    this._workQueue.EnqueueBackgroundTask(
                        ct => this.RecomputeTagsAsync(oldState, caretPosition, textChangeRange, spansToTag, oldTagTrees, ct),
                        GetType().Name + ".RecomputeTags", cancellationToken);
                }
            }

            private List<DocumentSnapshotSpan> GetSpansAndDocumentsToTag()
            {
                this._workQueue.AssertIsForeground();

                // TODO: Update to tag spans from all related documents.

                var snapshotToDocumentMap = new Dictionary<ITextSnapshot, Document>();
                var spansToTag = _dataSource.GetSpansToTag(_textViewOpt, _subjectBuffer);

                var spansAndDocumentsToTag = spansToTag.Select(span =>
                {
                    Document document = null;
                    if (!snapshotToDocumentMap.TryGetValue(span.Snapshot, out document))
                    {
                        CheckSnapshot(span.Snapshot);

                        document = span.Snapshot.GetOpenDocumentInCurrentContextWithChanges();
                        snapshotToDocumentMap[span.Snapshot] = document;
                    }

                    // document can be null if the buffer the given span is part of is not part of our workspace.
                    return new DocumentSnapshotSpan(document, span);
                }).ToList();

                Debug.Assert(spansAndDocumentsToTag.Count > 0);
                return spansAndDocumentsToTag;
            }

            [Conditional("DEBUG")]
            private void CheckSnapshot(ITextSnapshot snapshot)
            {
                var container = snapshot.TextBuffer.AsTextContainer();

                Workspace dummy;
                if (Workspace.TryGetWorkspace(container, out dummy))
                {
                    // if the buffer is part of our workspace, it must be the latest.
                    Contract.Assert(snapshot.Version.Next == null, "should be on latest snapshot");
                }
            }

            private ImmutableDictionary<ITextBuffer, TagSpanIntervalTree<TTag>> ConvertToTagTrees(
                ImmutableDictionary<ITextBuffer, TagSpanIntervalTree<TTag>> oldTagTrees,
                ILookup<ITextBuffer, ITagSpan<TTag>> newTagsByBuffer,
                IEnumerable<DocumentSnapshotSpan> spansTagged)
            {
                // NOTE: we assume that the following list is already realized and is _not_ lazily
                // computed. It's not clear what the contract is of this API.

                // common case where we only tagged a single range of a document.
                if (spansTagged.IsSingle())
                {
                    return ConvertToTagTree(oldTagTrees, newTagsByBuffer, spansTagged.Single().SnapshotSpan);
                }

                // heavy linq case 
                var spansToInvalidateByBuffer = spansTagged.Select(ss => ss.SnapshotSpan).ToLookup(ss => ss.Snapshot.TextBuffer);

                var buffers = oldTagTrees.Keys.Concat(newTagsByBuffer.Select(g => g.Key))
                                              .Concat(spansTagged.Select(dss => dss.SnapshotSpan.Snapshot.TextBuffer))
                                              .Distinct();

                // Walk through each relevant buffer and decide what the interval tree should be
                // for that buffer.  In general this will work by keeping around old tags that
                // weren't in the range that was re-tagged, and merging them with the new tags
                // produced for the range that was re-tagged.
                var newTagTrees = ImmutableDictionary<ITextBuffer, TagSpanIntervalTree<TTag>>.Empty;
                foreach (var buffer in buffers)
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

                TagSpanIntervalTree<TTag> oldTagTree;
                oldTagTrees.TryGetValue(textBuffer, out oldTagTree);

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

            private ImmutableDictionary<ITextBuffer, TagSpanIntervalTree<TTag>> ConvertToTagTree(
                ImmutableDictionary<ITextBuffer, TagSpanIntervalTree<TTag>> oldTagTrees,
                ILookup<ITextBuffer, ITagSpan<TTag>> newTagsByBuffer,
                SnapshotSpan spanTagged)
            {
                var allBuffers = oldTagTrees.Keys.Concat(newTagsByBuffer.Select(g => g.Key))
                                                 .Concat(spanTagged.Snapshot.TextBuffer).Distinct();

                var newTagTrees = ImmutableDictionary<ITextBuffer, TagSpanIntervalTree<TTag>>.Empty;
                foreach (var buffer in allBuffers)
                {
                    var newTagTree = ComputeNewTagTree(spanTagged, oldTagTrees, buffer, newTagsByBuffer[buffer]);
                    if (newTagTree != null)
                    {
                        newTagTrees = newTagTrees.Add(buffer, newTagTree);
                    }
                }

                return newTagTrees;
            }

            /// <summary>
            /// This is the same as <see cref="ComputeNewTagTree(ImmutableDictionary{ITextBuffer, TagSpanIntervalTree{TTag}}, ITextBuffer, IEnumerable{ITagSpan{TTag}}, IEnumerable{SnapshotSpan})"/>,
            /// just optimized for the case where we were tagging a single snapshot span.
            /// </summary>
            private TagSpanIntervalTree<TTag> ComputeNewTagTree(
                SnapshotSpan spanToInvalidate,
                ImmutableDictionary<ITextBuffer, TagSpanIntervalTree<TTag>> oldTagTrees,
                ITextBuffer textBuffer,
                IEnumerable<ITagSpan<TTag>> newTags)
            {
                TagSpanIntervalTree<TTag> oldTagTree;
                oldTagTrees.TryGetValue(textBuffer, out oldTagTree);

                if (oldTagTree == null)
                {
                    if (newTags.IsEmpty())
                    {
                        // We have no new tags, and no old tags either.  No need to store anything
                        // for this buffer.
                        return null;
                    }

                    // If we don't have any old tags then we just need to return the new tags.
                    return new TagSpanIntervalTree<TTag>(textBuffer, _dataSource.SpanTrackingMode, newTags);
                }

                // If we're examining a text buffer other than the one we tagged.
                if (textBuffer != spanToInvalidate.Snapshot.TextBuffer)
                {
                    // If we have no new tags produced for it, we can just use the old tags for it.
                    if (newTags.IsEmpty())
                    {
                        return oldTagTree;
                    }

                    // Otherwise, merge the old and new tags.
                    var finalTags = oldTagTree.GetSpans(newTags.First().Span.Snapshot).Concat(newTags);
                    return new TagSpanIntervalTree<TTag>(textBuffer, _dataSource.SpanTrackingMode, finalTags);
                }
                else
                {
                    // We're examining the buffer we tagged.  If we tagged just a portion in the middle
                    // then produce the new set of tags by taking the old set before the retagged portion,
                    // then the new tags, then the old tags after the retagged portion.  This keeps the
                    // tags mostly sorted, which helps with producing an accurate diff.

                    // These lists only live as long as this method.  So it's fine for us to do whatever
                    // we want with them (including mutating them) inside this body.
                    var beforeTagsToKeep = new List<ITagSpan<TTag>>();
                    var afterTagsToKeep = new List<ITagSpan<TTag>>();
                    GetTagsToKeep(oldTagTrees, spanToInvalidate, beforeTagsToKeep, afterTagsToKeep);

                    // Then union those with the new tags to produce the final tag tree.
                    var finalTags = beforeTagsToKeep;
                    finalTags.AddRange(newTags);
                    finalTags.AddRange(afterTagsToKeep);

                    return new TagSpanIntervalTree<TTag>(textBuffer, _dataSource.SpanTrackingMode, finalTags);
                }
            }

            /// <summary>
            /// Returns all that tags that fully precede 'spanToInvalidate' and all the tags that
            /// fully follow it.  All the tag spans are normalized to the snapshot passed in through
            /// 'spanToInvalidate'.
            /// </summary>
            private void GetTagsToKeep(
                ImmutableDictionary<ITextBuffer, TagSpanIntervalTree<TTag>> oldTagTrees,
                SnapshotSpan spanTagged,
                List<ITagSpan<TTag>> beforeTags,
                List<ITagSpan<TTag>> afterTags)
            {
                var fullRefresh = spanTagged.Length == spanTagged.Snapshot.Length;
                if (fullRefresh)
                {
                    return;
                }

                // we actually have span to invalidate from old tree
                TagSpanIntervalTree<TTag> treeForBuffer;
                if (!oldTagTrees.TryGetValue(spanTagged.Snapshot.TextBuffer, out treeForBuffer))
                {
                    return;
                }

                treeForBuffer.GetNonIntersectingSpans(spanTagged, beforeTags, afterTags);
            }

            private async Task RecomputeTagsAsync(
                object oldState,
                SnapshotPoint? caretPosition,
                TextChangeRange? textChangeRange,
                List<DocumentSnapshotSpan> spansToTag,
                ImmutableDictionary<ITextBuffer, TagSpanIntervalTree<TTag>> oldTagTrees,
                CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var context = new TaggerContext<TTag>(
                    oldState, spansToTag, caretPosition, textChangeRange, oldTagTrees, cancellationToken);
                await ProduceTagsAsync(context).ConfigureAwait(false);

                ProcessContext(spansToTag, oldTagTrees, context);
            }

            private Task ProduceTagsAsync(TaggerContext<TTag> context)
            {
                var options = _dataSource.Options ?? SpecializedCollections.EmptyEnumerable<Option<bool>>();
                var perLanguageOptions = _dataSource.PerLanguageOptions ?? SpecializedCollections.EmptyEnumerable<PerLanguageOption<bool>>();

                if (options.Any(option => !_subjectBuffer.GetOption(option)) ||
                    perLanguageOptions.Any(option => !_subjectBuffer.GetOption(option)))
                {
                    // If the feature is disabled, then just produce no tags.
                    return SpecializedTasks.EmptyTask;
                }

                return _dataSource.ProduceTagsAsync(context);
            }

            private void ProcessContext(
                List<DocumentSnapshotSpan> spansToTag,
                ImmutableDictionary<ITextBuffer, TagSpanIntervalTree<TTag>> oldTagTrees,
                TaggerContext<TTag> context)
            {
                var buffersToTag = spansToTag.Select(dss => dss.SnapshotSpan.Snapshot.TextBuffer).ToSet();

                // Ignore any tag spans reported for any buffers we weren't interested in.
                var newTagsByBuffer = context.tagSpans.Where(ts => buffersToTag.Contains(ts.Span.Snapshot.TextBuffer))
                                                      .ToLookup(t => t.Span.Snapshot.TextBuffer);


                var newTagTrees = ConvertToTagTrees(oldTagTrees, newTagsByBuffer, context._spansTagged);
                ProcessNewTagTrees(spansToTag, oldTagTrees, newTagTrees, context.State, context.CancellationToken);
            }

            private void ProcessNewTagTrees(
                List<DocumentSnapshotSpan> spansToTag,
                ImmutableDictionary<ITextBuffer, TagSpanIntervalTree<TTag>> oldTagTrees,
                ImmutableDictionary<ITextBuffer, TagSpanIntervalTree<TTag>> newTagTrees,
                object newState,
                CancellationToken cancellationToken)
            {
                var bufferToChanges = new Dictionary<ITextBuffer, NormalizedSnapshotSpanCollection>();
                using (Logger.LogBlock(FunctionId.Tagger_TagSource_ProcessNewTags, cancellationToken))
                {
                    foreach (var latestBuffer in newTagTrees.Keys)
                    {
                        var snapshot = spansToTag.First(s => s.SnapshotSpan.Snapshot.TextBuffer == latestBuffer).SnapshotSpan.Snapshot;

                        if (oldTagTrees.ContainsKey(latestBuffer))
                        {
                            var difference = ComputeDifference(snapshot, newTagTrees[latestBuffer], oldTagTrees[latestBuffer]);
                            bufferToChanges[latestBuffer] = difference;
                        }
                        else
                        {
                            // It's a new buffer, so report all spans are changed
                            var allSpans = new NormalizedSnapshotSpanCollection(newTagTrees[latestBuffer].GetSpans(snapshot).Select(t => t.Span));
                            bufferToChanges[latestBuffer] = allSpans;
                        }
                    }

                    foreach (var oldBuffer in oldTagTrees.Keys)
                    {
                        if (!newTagTrees.ContainsKey(oldBuffer))
                        {
                            // This buffer disappeared, so let's notify that the old tags are gone
                            var allSpans = new NormalizedSnapshotSpanCollection(oldTagTrees[oldBuffer].GetSpans(oldBuffer.CurrentSnapshot).Select(t => t.Span));
                            bufferToChanges[oldBuffer] = allSpans;
                        }
                    }
                }

                if (_workQueue.IsForeground())
                {
                    // If we're on the foreground already, we can just update our internal state directly.
                    UpdateStateAndReportChanges(newTagTrees, bufferToChanges, newState);
                }
                else
                {
                    // Otherwise report back on the foreground asap to update the state and let our 
                    // clients know about the change.
                    RegisterNotification(() => UpdateStateAndReportChanges(newTagTrees, bufferToChanges, newState), 0, cancellationToken);
                }
            }

            private void UpdateStateAndReportChanges(
                ImmutableDictionary<ITextBuffer, TagSpanIntervalTree<TTag>> newTagTrees,
                Dictionary<ITextBuffer, NormalizedSnapshotSpanCollection> bufferToChanges,
                object newState)
            {
                this._workQueue.AssertIsForeground();

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
                RaiseTagsChanged(bufferToChanges);
            }

            private NormalizedSnapshotSpanCollection ComputeDifference(
                ITextSnapshot snapshot,
                TagSpanIntervalTree<TTag> latestSpans,
                TagSpanIntervalTree<TTag> previousSpans)
            {
                return new NormalizedSnapshotSpanCollection(
                    Difference(latestSpans.GetSpans(snapshot), previousSpans.GetSpans(snapshot), _dataSource.TagComparer));
            }

            /// <summary>
            /// Returns the TagSpanIntervalTree containing the tags for the given buffer. If no tags
            /// exist for the buffer at all, null is returned.
            /// </summary>
            public TagSpanIntervalTree<TTag> GetTagIntervalTreeForBuffer(ITextBuffer buffer)
            {
                this._workQueue.AssertIsForeground();

                // If we're currently pausing updates to the UI, then just use the tags we had before we
                // were paused so that nothing changes.  
                //
                // We're on the UI thread, so it's safe to access these variables.
                var map = _previousCachedTagTrees ?? this.CachedTagTrees;

                TagSpanIntervalTree<TTag> tags;
                map.TryGetValue(buffer, out tags);
                return tags;
            }

            public TagSpanIntervalTree<TTag> GetAccurateTagIntervalTreeForBuffer(ITextBuffer buffer, CancellationToken cancellationToken)
            {
                this._workQueue.AssertIsForeground();

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
                    this._workQueue.CancelCurrentWork();

                    var spansToTag = GetSpansAndDocumentsToTag();

                    // Safe to access _cachedTagTrees here.  We're on the UI thread.
                    var oldTagTrees = this.CachedTagTrees;
                    var caretPoint = _dataSource.GetCaretPoint(_textViewOpt, _subjectBuffer);

                    var context = new TaggerContext<TTag>(
                        this.State, spansToTag, caretPoint, this.AccumulatedTextChanges, oldTagTrees, cancellationToken);
                    ProduceTagsAsync(context).Wait();

                    ProcessContext(spansToTag, oldTagTrees, context);
                }

                Debug.Assert(this.UpToDate);

                TagSpanIntervalTree<TTag> tags;
                this.CachedTagTrees.TryGetValue(buffer, out tags);
                return tags;
            }
        }
    }
}
