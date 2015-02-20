// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Shared.Tagging
{
    /// <summary>
    /// Async thin tagger implementation.
    /// 
    /// Actual tag information is stored in TagSource and shared between multiple taggers created for same views or buffers.
    /// 
    /// It's responsibility is on interfaction between host and tagger. TagSource has responsibility on how to provide information for this tagger.
    /// </summary>
    internal sealed partial class AsynchronousTagger<TTag> : ITagger<TTag>, IDisposable
        where TTag : ITag
    {
        private const int MaxNumberOfRequestedSpans = 100;

        #region Fields that can be accessed from either thread

        private readonly ITextBuffer _subjectBuffer;
        private readonly TagSource<TTag> _tagSource;
        private readonly int _uiUpdateDelayInMS;

        #endregion

        #region Fields that can only be accessed from the foreground thread

        /// <summary>
        /// The batch change notifier that we use to throttle update to the UI.
        /// </summary>
        private readonly BatchChangeNotifier _batchChangeNotifier;

        #endregion

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        public AsynchronousTagger(
            IAsynchronousOperationListener listener,
            IForegroundNotificationService notificationService,
            TagSource<TTag> tagSource,
            ITextBuffer subjectBuffer,
            TimeSpan uiUpdateDelay)
        {
            Contract.ThrowIfNull(subjectBuffer);

            _subjectBuffer = subjectBuffer;
            _uiUpdateDelayInMS = (int)uiUpdateDelay.TotalMilliseconds;

            _batchChangeNotifier = new BatchChangeNotifier(subjectBuffer, listener, notificationService, ReportChangedSpan);

            _tagSource = tagSource;

            _tagSource.OnTaggerAdded(this);
            _tagSource.TagsChangedForBuffer += OnTagsChangedForBuffer;
            _tagSource.Paused += OnPaused;
            _tagSource.Resumed += OnResumed;
        }

        public void Dispose()
        {
            _tagSource.Resumed -= OnResumed;
            _tagSource.Paused -= OnPaused;
            _tagSource.TagsChangedForBuffer -= OnTagsChangedForBuffer;
            _tagSource.OnTaggerDisposed(this);
        }

        private void ReportChangedSpan(SnapshotSpan changeSpan)
        {
            var tagsChanged = TagsChanged;
            if (tagsChanged != null)
            {
                tagsChanged(this, new SnapshotSpanEventArgs(changeSpan));
            }
        }

        private void OnPaused(object sender, EventArgs e)
        {
            _batchChangeNotifier.Pause();
        }

        private void OnResumed(object sender, EventArgs e)
        {
            _batchChangeNotifier.Resume();
        }

        private void OnTagsChangedForBuffer(object sender, TagsChangedForBufferEventArgs args)
        {
            if (args.Buffer != _subjectBuffer)
            {
                return;
            }

            // Note: This operation is uncancellable. Once we've been notified here, our cached tags
            // in the tag source are new. If we don't update the UI of the editor then we will end
            // up in an inconsistent state between us and the editor where we have new tags but the
            // editor will never know.
            var spansChanged = args.Spans;

            _tagSource.RegisterNotification(() =>
            {
                _tagSource.WorkQueue.AssertIsForeground();

                // Now report them back to the UI on the main thread.
                _batchChangeNotifier.EnqueueChanges(spansChanged.First().Snapshot, spansChanged);
            }, _uiUpdateDelayInMS, CancellationToken.None);
        }

        public IEnumerable<ITagSpan<TTag>> GetTags(NormalizedSnapshotSpanCollection requestedSpans)
        {
            if (requestedSpans.Count == 0)
            {
                return SpecializedCollections.EmptyEnumerable<ITagSpan<TTag>>();
            }

            var buffer = requestedSpans.First().Snapshot.TextBuffer;
            var tags = _tagSource.GetTagIntervalTreeForBuffer(buffer);

            if (tags == null)
            {
                return SpecializedCollections.EmptyEnumerable<ITagSpan<TTag>>();
            }

            var result = GetTags(requestedSpans, tags);

            DebugVerifyTags(requestedSpans, result);

            return result;
        }

        private static IEnumerable<ITagSpan<TTag>> GetTags(NormalizedSnapshotSpanCollection requestedSpans, ITagSpanIntervalTree<TTag> tags)
        {
            // Special case the case where there is only one requested span.  In that case, we don't
            // need to allocate any intermediate collections
            return requestedSpans.Count == 1
                ? tags.GetIntersectingSpans(requestedSpans[0])
                : requestedSpans.Count < MaxNumberOfRequestedSpans
                    ? GetTagsForSmallNumberOfSpans(requestedSpans, tags)
                    : GetTagsForLargeNumberOfSpans(requestedSpans, tags);
        }

        private static IEnumerable<ITagSpan<TTag>> GetTagsForSmallNumberOfSpans(
            NormalizedSnapshotSpanCollection requestedSpans,
            ITagSpanIntervalTree<TTag> tags)
        {
            var result = new List<ITagSpan<TTag>>();

            foreach (var s in requestedSpans)
            {
                result.AddRange(tags.GetIntersectingSpans(s));
            }

            return result;
        }

        private static IEnumerable<ITagSpan<TTag>> GetTagsForLargeNumberOfSpans(
            NormalizedSnapshotSpanCollection requestedSpans,
            ITagSpanIntervalTree<TTag> tags)
        {
            // we are asked with bunch of spans. rather than asking same question again and again, ask once with big span
            // which will return superset of what we want. and then filter them out in O(m+n) cost. 
            // m == number of requested spans, n = number of returned spans
            var mergedSpan = new SnapshotSpan(requestedSpans[0].Start, requestedSpans[requestedSpans.Count - 1].End);
            var result = tags.GetIntersectingSpans(mergedSpan);

            int requestIndex = 0;

            var enumerator = result.GetEnumerator();

            try
            {
                if (!enumerator.MoveNext())
                {
                    return SpecializedCollections.EmptyEnumerable<ITagSpan<TTag>>();
                }

                var hashSet = new HashSet<ITagSpan<TTag>>();
                while (true)
                {
                    var currentTag = enumerator.Current;

                    var currentRequestSpan = requestedSpans[requestIndex];
                    var currentTagSpan = currentTag.Span;

                    if (currentRequestSpan.Start > currentTagSpan.End)
                    {
                        if (!enumerator.MoveNext())
                        {
                            break;
                        }
                    }
                    else if (currentTagSpan.Start > currentRequestSpan.End)
                    {
                        requestIndex++;

                        if (requestIndex >= requestedSpans.Count)
                        {
                            break;
                        }
                    }
                    else
                    {
                        if (currentTagSpan.Length > 0)
                        {
                            hashSet.Add(currentTag);
                        }

                        if (!enumerator.MoveNext())
                        {
                            break;
                        }
                    }
                }

                return hashSet;
            }
            finally
            {
                enumerator.Dispose();
            }
        }

        [Conditional("DEBUG")]
        private static void DebugVerifyTags(NormalizedSnapshotSpanCollection requestedSpans, IEnumerable<ITagSpan<TTag>> tags)
        {
            if (tags == null)
            {
                return;
            }

            foreach (var tag in tags)
            {
                var span = tag.Span;

                if (!requestedSpans.Any(s => s.IntersectsWith(span)))
                {
                    Contract.Fail(tag + " doesn't intersects with any requested span");
                }
            }
        }
    }
}
