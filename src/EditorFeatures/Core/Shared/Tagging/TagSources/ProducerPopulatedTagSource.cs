// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Shared.Tagging
{
    /// <summary>
    /// <para>The <see cref="ProducerPopulatedTagSource{TTag}"/> is the core part of our asynchronous
    /// tagging infrastructure. It is the coordinator between  <see cref="ITagProducer{TTag}"/>s,
    /// <see cref="ITaggerEventSource"/>s, and <see cref="ITagger{T}"/>s.</para>
    /// 
    /// <para>The <see cref="ProducerPopulatedTagSource{TTag}"/> is the type that actually owns the
    /// list of cached tags. When an <see cref="ITaggerEventSource"/> says tags need to be  recomputed,
    /// the tag source starts the computation and calls the <see cref="ITagProducer{TTag}"/> to build
    /// the new list of tags. When that's done, the tags are stored in <see cref="_cachedTags"/>. The 
    /// tagger, when asked for tags from the editor, then returns the tags that are stored in 
    /// <see cref="_cachedTags"/></para>
    /// 
    /// <para>There is a one-to-many relationship between <see cref="ProducerPopulatedTagSource{TTag}"/>s
    /// and <see cref="ITagger{T}"/>s. Special cases, like reference highlighting (which processes multiple
    /// subject buffers at once) have their own providers and tag source derivations.</para>
    /// </summary>
    /// <typeparam name="TTag">The type of tag.</typeparam>
    internal partial class ProducerPopulatedTagSource<TTag> : TagSource<TTag>
        where TTag : ITag
    {
        #region Fields that can be accessed from either thread

        /// <summary>
        /// Synchronization object for assignments to the <see cref="_cachedTags"/> field. This is only used for
        /// changes; reads may be done without any locking since the data structure itself is
        /// immutable.
        /// </summary>
        private readonly object _cachedTagsGate = new object();

        private ImmutableDictionary<ITextBuffer, TagSpanIntervalTree<TTag>> _cachedTags;

        private readonly IAsynchronousTaggerDataSource<TTag> _dataSource;
        private readonly ITagProducer<TTag> _tagProducer;

        private IEqualityComparer<ITagSpan<TTag>> _tagSpanComparer;

        /// <summary>
        /// accumulated text changes since last tag calculation
        /// </summary>
        private TextChangeRange? _accumulatedTextChanges;
        #endregion

        #region Fields that can only be accessed from the foreground thread

        /// <summary>
        /// Our tagger event source that lets us know when we should call into the tag producer for
        /// new tags.
        /// </summary>
        private readonly ITaggerEventSource _eventSource;

        /// <summary>
        /// During the time that we are paused from updating the UI, we will use these tags instead.
        /// </summary>
        private ImmutableDictionary<ITextBuffer, TagSpanIntervalTree<TTag>> _previousCachedTags;
        #endregion

        public ProducerPopulatedTagSource(
            ITextView textViewOpt,
            ITextBuffer subjectBuffer,
            IAsynchronousTaggerDataSource<TTag> dataSource,
            IAsynchronousOperationListener asyncListener,
            IForegroundNotificationService notificationService)
                : base(textViewOpt, subjectBuffer, dataSource.IgnoreCaretMovementToExistingTag, notificationService, asyncListener)
        {
            if (dataSource.SpanTrackingMode == SpanTrackingMode.Custom)
            {
                throw new ArgumentException("SpanTrackingMode.Custom not allowed.", "spanTrackingMode");
            }

            _dataSource = dataSource;
            _tagProducer = dataSource.CreateTagProducer();

            _cachedTags = ImmutableDictionary.Create<ITextBuffer, TagSpanIntervalTree<TTag>>();
            _tagSpanComparer = new TagSpanComparer<TTag>(this.TagComparer);

            _eventSource = dataSource.CreateEventSource(textViewOpt, subjectBuffer);

            _accumulatedTextChanges = null;

            AttachEventHandlersAndStart();
        }

        private IEqualityComparer<TTag> TagComparer => 
            _dataSource.TagComparer ?? EqualityComparer<TTag>.Default;

        private void AttachEventHandlersAndStart()
        {
            this.WorkQueue.AssertIsForeground();

            _eventSource.Changed += OnChanged;
            _eventSource.UIUpdatesResumed += OnUIUpdatesResumed;
            _eventSource.UIUpdatesPaused += OnUIUpdatesPaused;

            // Tell the interaction object to start issuing events.
            _eventSource.Connect();
        }

        protected override void Disconnect()
        {
            this.WorkQueue.AssertIsForeground();

            base.Disconnect();

            // Tell the interaction object to stop issuing events.
            _eventSource.Disconnect();

            _eventSource.UIUpdatesPaused -= OnUIUpdatesPaused;
            _eventSource.UIUpdatesResumed -= OnUIUpdatesResumed;
            _eventSource.Changed -= OnChanged;
        }

        private void OnUIUpdatesPaused(object sender, EventArgs e)
        {
            this.WorkQueue.AssertIsForeground();
            _previousCachedTags = _cachedTags;

            RaisePaused();
        }

        private void OnUIUpdatesResumed(object sender, EventArgs e)
        {
            this.WorkQueue.AssertIsForeground();
            _previousCachedTags = null;

            RaiseResumed();
        }

        private void OnChanged(object sender, TaggerEventArgs e)
        {
            using (var token = this.Listener.BeginAsyncOperation("OnChanged"))
            {
                // First, cancel any previous requests (either still queued, or started).  We no longer
                // want to continue it if new changes have come in.
                this.WorkQueue.CancelCurrentWork();

                // We don't currently have a request issued to re-compute our tags. Issue it for some
                // time in the future

                // If we had a text buffer change, we might be able to do something smarter with the
                // tags
                if (e.TextChangeEventArgs != null)
                {
                    UpdateTagsForTextChange(e.TextChangeEventArgs);
                    AccumulateTextChanges(e.TextChangeEventArgs);
                }

                RecalculateTagsOnChanged(e);
            }
        }

        private void AccumulateTextChanges(TextContentChangedEventArgs contentChanged)
        {
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
                        lock (_cachedTagsGate)
                        {
                            if (_accumulatedTextChanges == null)
                            {
                                _accumulatedTextChanges = textChangeRange;
                            }
                            else
                            {
                                _accumulatedTextChanges = _accumulatedTextChanges.Accumulate(SpecializedCollections.SingletonEnumerable(textChangeRange));
                            }
                        }
                    }
                    break;

                default:
                    var textChangeRanges = new TextChangeRange[count];
                    for (int i = 0; i < count; i++)
                    {
                        var c = contentChanges[i];
                        textChangeRanges[i] = new TextChangeRange(new TextSpan(c.OldSpan.Start, c.OldSpan.Length), c.NewLength);
                    }

                    lock (_cachedTagsGate)
                    {
                        _accumulatedTextChanges = _accumulatedTextChanges.Accumulate(textChangeRanges);
                    }
                    break;
            }
        }

        private void UpdateTagsForTextChange(TextContentChangedEventArgs e)
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
            if (!_cachedTags.TryGetValue(buffer, out treeForBuffer))
            {
                return;
            }

            if (!_dataSource.RemoveTagsThatIntersectEdits)
            {
                return;
            }

            var tagsToRemove = e.Changes.SelectMany(c => treeForBuffer.GetIntersectingSpans(new SnapshotSpan(e.After, c.NewSpan)));
            if (!tagsToRemove.Any())
            {
                return;
            }

            var allTags = treeForBuffer.GetSpans(e.After).ToList();
            var newTreeForBuffer = new TagSpanIntervalTree<TTag>(
                buffer,
                treeForBuffer.SpanTrackingMode,
                allTags.Except(tagsToRemove, _tagSpanComparer));

            UpdateCachedTagsForBuffer(e.After, newTreeForBuffer);
        }

        private void UpdateCachedTagsForBuffer(ITextSnapshot snapshot, TagSpanIntervalTree<TTag> newTagsForBuffer)
        {
            var oldCachedTags = _cachedTags;

            lock (_cachedTagsGate)
            {
                _cachedTags = _cachedTags.SetItem(snapshot.TextBuffer, newTagsForBuffer);
            }

            // Grab our old tags. We might not have any, so in this case we'll just pretend it's
            // empty
            TagSpanIntervalTree<TTag> oldCachedTagsForBuffer = null;
            if (!oldCachedTags.TryGetValue(snapshot.TextBuffer, out oldCachedTagsForBuffer))
            {
                oldCachedTagsForBuffer = new TagSpanIntervalTree<TTag>(snapshot.TextBuffer, _dataSource.SpanTrackingMode);
            }

            var difference = ComputeDifference(snapshot, oldCachedTagsForBuffer, newTagsForBuffer);
            if (difference.Count > 0)
            {
                RaiseTagsChanged(snapshot.TextBuffer, difference);
            }
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
        protected override void RecomputeTagsForeground()
        {
            this.WorkQueue.AssertIsForeground();

            using (Logger.LogBlock(FunctionId.Tagger_TagSource_RecomputeTags, CancellationToken.None))
            {
                // Get the current valid cancellation token for this work.
                var cancellationToken = this.WorkQueue.CancellationToken;
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                var spansToTag = GetSpansAndDocumentsToTag();

                var caretPosition = this.GetCaretPoint();
                var textChangeRange = _accumulatedTextChanges;

                this.WorkQueue.EnqueueBackgroundTask(
                    ct => this.RecomputeTagsAsync(caretPosition, textChangeRange, spansToTag, ct),
                    GetType().Name + ".RecomputeTags", cancellationToken);
            }
        }

        protected List<DocumentSnapshotSpan> GetSpansAndDocumentsToTag()
        {
            this.WorkQueue.AssertIsForeground();

            // TODO: Update to tag spans from all related documents.

            var snapshotToDocumentMap = new Dictionary<ITextSnapshot, Document>();
            var spansToTag = _dataSource.GetSpansToTag(TextViewOpt, SubjectBuffer) ?? this.GetFullBufferSpan();
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

            return spansAndDocumentsToTag;
        }

        private IList<SnapshotSpan> GetFullBufferSpan()
        {
            // For a standard tagger, the spans to tag is the span of the entire snapshot.
            return new[] { SubjectBuffer.CurrentSnapshot.GetFullSpan() };
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

        protected ImmutableDictionary<ITextBuffer, TagSpanIntervalTree<TTag>> ConvertToTagTree(
            IEnumerable<ITagSpan<TTag>> tagSpans, IEnumerable<DocumentSnapshotSpan> spansToCompute = null)
        {
            // NOTE: we assume that the following list is already realized and is _not_ lazily
            // computed. It's not clear what the contract is of this API.

            // common case where there is only one buffer 
            if (spansToCompute != null && spansToCompute.IsSingle())
            {
                return ConvertToTagTree(tagSpans, spansToCompute.Single().SnapshotSpan);
            }

            // heavy generic case 
            var tagsByBuffer = tagSpans.GroupBy(t => t.Span.Snapshot.TextBuffer);
            var tagsToKeepByBuffer = GetTagsToKeepByBuffer(spansToCompute);

            var map = ImmutableDictionary<ITextBuffer, TagSpanIntervalTree<TTag>>.Empty;
            foreach (var tagsInBuffer in tagsByBuffer)
            {
                IEnumerable<ITagSpan<TTag>> tags;
                if (tagsToKeepByBuffer.TryGetValue(tagsInBuffer.Key, out tags))
                {
                    tags = tagsInBuffer.Concat(tags);
                }
                else
                {
                    tags = tagsInBuffer;
                }

                map = map.Add(tagsInBuffer.Key, new TagSpanIntervalTree<TTag>(tagsInBuffer.Key, _dataSource.SpanTrackingMode, tags));
            }

            foreach (var kv in tagsToKeepByBuffer)
            {
                if (!map.ContainsKey(kv.Key) && kv.Value.Any())
                {
                    map = map.Add(kv.Key, new TagSpanIntervalTree<TTag>(kv.Key, _dataSource.SpanTrackingMode, kv.Value));
                }
            }

            return map;
        }

        private ImmutableDictionary<ITextBuffer, TagSpanIntervalTree<TTag>> ConvertToTagTree(IEnumerable<ITagSpan<TTag>> tagSpans, SnapshotSpan spanToInvalidate)
        {
            var tagsByBuffer = tagSpans.GroupBy(t => t.Span.Snapshot.TextBuffer);

            var map = ImmutableDictionary<ITextBuffer, TagSpanIntervalTree<TTag>>.Empty;

            var invalidBuffer = spanToInvalidate.Snapshot.TextBuffer;

            // These lists only live as long as this method.  So it's fine for us to do whatever
            // we want with them (including mutating them) inside this body.
            var beforeTagsToKeep = new List<ITagSpan<TTag>>();
            var afterTagsToKeep = new List<ITagSpan<TTag>>();
            GetTagsToKeep(spanToInvalidate, beforeTagsToKeep, afterTagsToKeep);

            foreach (var tagsInBuffer in tagsByBuffer)
            {
                IEnumerable<ITagSpan<TTag>> tags;
                if (tagsInBuffer.Key == invalidBuffer)
                {
                    // Create all the tags for this buffer, using the old tags around the span, 
                    // along with all the new tags in the middle.
                    var allTags = beforeTagsToKeep;

                    // Note: we're mutating 'beforeTagsToKeep' here.  but that's ok.  We won't
                    // use it at all after this.
                    allTags.AddRange(tagsInBuffer);
                    allTags.AddRange(afterTagsToKeep);
                    tags = allTags;
                }
                else
                {
                    tags = tagsInBuffer;
                }

                map = map.Add(tagsInBuffer.Key, new TagSpanIntervalTree<TTag>(tagsInBuffer.Key, _dataSource.SpanTrackingMode, tags));
            }

            // Check if we didn't produce any new tags for this buffer.  If we didn't, but we did 
            // have old tags before/after the span we tagged, then we want to ensure that we pull
            // all those old tags forward.
            if (!map.ContainsKey(invalidBuffer) && (beforeTagsToKeep.Count > 0 || afterTagsToKeep.Count > 0))
            {
                var allTags = beforeTagsToKeep;
                allTags.AddRange(afterTagsToKeep);

                map = map.Add(invalidBuffer, new TagSpanIntervalTree<TTag>(invalidBuffer, _dataSource.SpanTrackingMode, allTags));
            }

            return map;
        }

        private ImmutableDictionary<ITextBuffer, IEnumerable<ITagSpan<TTag>>> GetTagsToKeepByBuffer(IEnumerable<DocumentSnapshotSpan> spansToCompute)
        {
            var map = ImmutableDictionary.Create<ITextBuffer, IEnumerable<ITagSpan<TTag>>>();

            if (spansToCompute == null)
            {
                return map;
            }

            var invalidSpansByBuffer = ImmutableDictionary.CreateRange<ITextBuffer, IEnumerable<SnapshotSpan>>(
                                           spansToCompute.Select(t => t.SnapshotSpan).GroupBy(s => s.Snapshot.TextBuffer).Select(g => KeyValuePair.Create(g.Key, g.AsEnumerable())));

            foreach (var kv in invalidSpansByBuffer)
            {
                TagSpanIntervalTree<TTag> treeForBuffer;
                if (!_cachedTags.TryGetValue(kv.Key, out treeForBuffer))
                {
                    continue;
                }

                var invalidSpans = new List<ITagSpan<TTag>>();
                foreach (var spanToInvalidate in kv.Value)
                {
                    invalidSpans.AddRange(treeForBuffer.GetIntersectingSpans(spanToInvalidate));
                }

                map = map.Add(kv.Key, treeForBuffer.GetSpans(kv.Key.CurrentSnapshot).Except(invalidSpans, _tagSpanComparer));
            }

            return map;
        }

        /// <summary>
        /// Returns all that tags that fully precede 'spanToInvalidate' and all the tags that
        /// fully follow it.  All the tag spans are normalized to the snapshot passed in through
        /// 'spanToInvalidate'.
        /// </summary>
        private void GetTagsToKeep(SnapshotSpan spanToInvalidate,
                                   List<ITagSpan<TTag>> beforeTags,
                                   List<ITagSpan<TTag>> afterTags)
        {
            var fullRefresh = spanToInvalidate.Length == spanToInvalidate.Snapshot.Length;
            if (fullRefresh)
            {
                return;
            }

            // we actually have span to invalidate from old tree
            TagSpanIntervalTree<TTag> treeForBuffer;
            if (!_cachedTags.TryGetValue(spanToInvalidate.Snapshot.TextBuffer, out treeForBuffer))
            {
                return;
            }

            treeForBuffer.GetNonIntersectingSpans(spanToInvalidate, beforeTags, afterTags);
        }

        protected virtual async Task RecomputeTagsAsync(
            SnapshotPoint? caretPosition,
            TextChangeRange? textChangeRange,
            IEnumerable<DocumentSnapshotSpan> spansToCompute,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var tagSpans = spansToCompute.IsEmpty() ?
                SpecializedCollections.EmptyEnumerable<ITagSpan<TTag>>() :
                await _tagProducer.ProduceTagsAsync(spansToCompute, caretPosition, cancellationToken).ConfigureAwait(false);

            var map = ConvertToTagTree(tagSpans, spansToCompute);

            ProcessNewTags(spansToCompute, textChangeRange, map);
        }

        protected virtual void ProcessNewTags(
            IEnumerable<DocumentSnapshotSpan> spansToCompute, TextChangeRange? oldTextChangeRange, ImmutableDictionary<ITextBuffer, TagSpanIntervalTree<TTag>> newTags)
        {
            using (Logger.LogBlock(FunctionId.Tagger_TagSource_ProcessNewTags, CancellationToken.None))
            {
                var oldTags = _cachedTags;

                lock (_cachedTagsGate)
                {
                    _cachedTags = newTags;

                    // This can have a race, but it is not easy to remove due to our async and cancellation nature. 
                    // so here what we do is this, first we see whether the range we used to determine span to recompute is still same, if it is
                    // then we reset the range so that we can start from scratch. if not (if there was another edit while us recomputing but
                    // we didn't get cancelled), then we keep the existing accumulated text changes so that we at least don't miss recomputing
                    // but at the same time, blindly recompute whole file.
                    _accumulatedTextChanges = Nullable.Equals(_accumulatedTextChanges, oldTextChangeRange) ? null : _accumulatedTextChanges;
                }

                // Now diff the two resultant sets and fire off the notifications
                if (!HasTagsChangedListener)
                {
                    return;
                }

                foreach (var latestBuffer in _cachedTags.Keys)
                {
                    var snapshot = spansToCompute.First(s => s.SnapshotSpan.Snapshot.TextBuffer == latestBuffer).SnapshotSpan.Snapshot;

                    if (oldTags.ContainsKey(latestBuffer))
                    {
                        var difference = ComputeDifference(snapshot, _cachedTags[latestBuffer], oldTags[latestBuffer]);
                        if (difference.Count > 0)
                        {
                            RaiseTagsChanged(latestBuffer, difference);
                        }
                    }
                    else
                    {
                        // It's a new buffer, so report all spans are changed
                        var allSpans = new NormalizedSnapshotSpanCollection(_cachedTags[latestBuffer].GetSpans(snapshot).Select(t => t.Span));
                        if (allSpans.Count > 0)
                        {
                            RaiseTagsChanged(latestBuffer, allSpans);
                        }
                    }
                }

                foreach (var oldBuffer in oldTags.Keys)
                {
                    if (!_cachedTags.ContainsKey(oldBuffer))
                    {
                        // This buffer disappeared, so let's notify that the old tags are gone
                        var allSpans = new NormalizedSnapshotSpanCollection(oldTags[oldBuffer].GetSpans(oldBuffer.CurrentSnapshot).Select(t => t.Span));
                        if (allSpans.Count > 0)
                        {
                            RaiseTagsChanged(oldBuffer, allSpans);
                        }
                    }
                }
            }
        }

        private NormalizedSnapshotSpanCollection ComputeDifference(
            ITextSnapshot snapshot,
            TagSpanIntervalTree<TTag> latestSpans,
            TagSpanIntervalTree<TTag> previousSpans)
        {
            return new NormalizedSnapshotSpanCollection(
                Difference(latestSpans.GetSpans(snapshot), previousSpans.GetSpans(snapshot), new DiffSpanComparer(this.TagComparer)));
        }

        /// <summary>
        /// Returns the TagSpanIntervalTree containing the tags for the given buffer. If no tags
        /// exist for the buffer at all, null is returned.
        /// </summary>
        public override ITagSpanIntervalTree<TTag> GetTagIntervalTreeForBuffer(ITextBuffer buffer)
        {
            this.WorkQueue.AssertIsForeground();

            // If we're currently pausing updates to the UI, then just use the tags we had before we
            // were paused so that nothing changes.
            var map = _previousCachedTags ?? _cachedTags;

            TagSpanIntervalTree<TTag> tags;
            if (!map.TryGetValue(buffer, out tags))
            {
                if (_dataSource.ComputeTagsSynchronouslyIfNoAsynchronousComputationHasCompleted && _previousCachedTags == null)
                {
                    // We can cancel any background computations currently happening
                    this.WorkQueue.CancelCurrentWork();

                    var spansToCompute = GetSpansAndDocumentsToTag();

                    // We shall synchronously compute tags.
                    var producedTags = ConvertToTagTree(
                        _tagProducer.ProduceTagsAsync(spansToCompute, GetCaretPoint(), CancellationToken.None).WaitAndGetResult(CancellationToken.None));

                    ProcessNewTags(spansToCompute, null, producedTags);

                    producedTags.TryGetValue(buffer, out tags);
                }
            }

            return tags;
        }

        private class DiffSpanComparer : IDiffSpanComparer<ITagSpan<TTag>>
        {
            private readonly IEqualityComparer<TTag> _comparer;

            public DiffSpanComparer(IEqualityComparer<TTag> comparer)
            {
                _comparer = comparer;
            }

            public bool IsDefault(ITagSpan<TTag> tagSpan)
            {
                return tagSpan == null;
            }

            public SnapshotSpan GetSpan(ITagSpan<TTag> tagSpan)
            {
                return tagSpan.Span;
            }

            public bool Equals(ITagSpan<TTag> tagSpan1, ITagSpan<TTag> tagSpan2)
            {
                return _comparer.Equals(tagSpan1.Tag, tagSpan2.Tag);
            }
        }
    }
}
