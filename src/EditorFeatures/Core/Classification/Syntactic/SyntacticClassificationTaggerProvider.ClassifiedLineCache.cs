// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Classification;

internal partial class SyntacticClassificationTaggerProvider
{
    /// <summary>
    /// Cache for storing recent classification results. This type has thread affinity on the UI thread, so it doesn't
    /// need any additional synchronization.
    /// </summary>
    /// <remarks>
    /// Empirical testing shows that when paging through a file, 25% of syntactic classification requests can be
    /// returned from a simple LRU cache.  When arrowing up and down in a file, the cache hit rate is 85%.  This can
    /// substantially speed up these requests, especially in large files, as it means we can avoid walking syntax
    /// trees and reclassifying lines we just classified.
    /// </remarks>
    private sealed class ClassifiedLineCache(IThreadingContext threadingContext)
    {
        /// <summary>
        /// Ensure that we don't cache incredibly long lines (for example, a minified JavaScript file).  This also
        /// ensures if we were asked by some party to classify a large section of the file, that we don't attempt to
        /// store that.  The primary purpose of this cache is for the common case of returning cached lines when the
        /// editor calls into us to classify them.
        /// </summary>
        private const int MaxClassificationsCount = 1024;

        /// <summary>
        /// This number was picked as a reasonable number of lines to classify given regular screen sized.  This is
        /// about 6x larger than a normal number of lines in VS at standard resolutions, and handles the case of
        /// users with vertical monitors and small font/zoom settings..
        /// </summary>
        private const int CacheSize = 256;

        private readonly IThreadingContext _threadingContext = threadingContext;

        // Mutating state.  No need for locks as we only execute on the UI thread (and throw if we're not on that thread).

        private ITextSnapshot? _snapshot;

        private readonly LinkedList<Span> _spans = [];
        private readonly Dictionary<Span, (LinkedListNode<Span> node, SegmentedList<ClassifiedSpan> classifiedSpans)> _spanToClassifiedSpansMap = [];

        private void ClearIfDifferentSnapshot(ITextSnapshot snapshot)
        {
            _threadingContext.ThrowIfNotOnUIThread();

            if (_snapshot != snapshot)
            {
                _spans.Clear();
                _spanToClassifiedSpansMap.Clear();
                _snapshot = snapshot;
            }
        }

        public bool TryUseCache(SnapshotSpan snapshotSpan, SegmentedList<ClassifiedSpan> classifications)
        {
            _threadingContext.ThrowIfNotOnUIThread();

            ClearIfDifferentSnapshot(snapshotSpan.Snapshot);

            if (!_spanToClassifiedSpansMap.TryGetValue(snapshotSpan.Span, out var tuple))
                return false;

            // Move this node to the front of the LRU list.
            _spans.Remove(tuple.node);
            _spans.AddLast(tuple.node);

            // AddRange is optimized to take a SegmentedList and copy directly from it into the result list.
            classifications.AddRange(tuple.classifiedSpans);
            return true;
        }

        public void Update(SnapshotSpan snapshotSpan, SegmentedList<ClassifiedSpan> newClassifications)
        {
            _threadingContext.ThrowIfNotOnUIThread();

            if (newClassifications.Count > MaxClassificationsCount)
                return;

            // Clear out cached data if we've moved to a different snapshot.
            var span = snapshotSpan.Span;
            ClearIfDifferentSnapshot(snapshotSpan.Snapshot);

            if (_spanToClassifiedSpansMap.TryGetValue(span, out var tuple))
            {
                UpdateExistingEntryInCache(newClassifications, existingNode: tuple.node, existingClassifications: tuple.classifiedSpans);
            }
            else
            {
                AddNewEntryToCache(span, newClassifications);
            }

            Contract.ThrowIfTrue(_spans.Count > CacheSize);
            Contract.ThrowIfTrue(_spans.Count != _spanToClassifiedSpansMap.Count);
        }

        /// <summary>
        /// Helper that allows us to reuse the <paramref name="existingClassifications"/> list, updating it to have
        /// all the classifications in <paramref name="classifications"/>.
        /// </summary>
        private static void ClearExistingClassificationsAndAddNewClassificationsToIt(
            SegmentedList<ClassifiedSpan> existingClassifications,
            SegmentedList<ClassifiedSpan> classifications)
        {
            existingClassifications.Clear();

            // AddRange is optimized to take a SegmentedList and copy directly from it into the result list.
            existingClassifications.AddRange(classifications);
        }

        private void UpdateExistingEntryInCache(
            SegmentedList<ClassifiedSpan> newClassifications, LinkedListNode<Span> existingNode, SegmentedList<ClassifiedSpan> existingClassifications)
        {
            // Was in cache.  Update the cached classifications to the new ones, and move this span to the front of
            // end of the LRU list.

            ClearExistingClassificationsAndAddNewClassificationsToIt(existingClassifications, newClassifications);

            _spans.Remove(existingNode);
            _spans.AddLast(existingNode);
        }

        private void AddNewEntryToCache(Span span, SegmentedList<ClassifiedSpan> newClassifications)
        {
            // Not in cache.  Add to the cache, and remove the oldest entry if we're at capacity.

            if (_spans.Count < CacheSize)
            {
                // We're not at capacity.  Just add this new entry.
                var node = _spans.AddLast(span);

                // The SegmentedList constructor fast paths the case where we pass in another SegmentedList.
                AddToMap(node, new SegmentedList<ClassifiedSpan>(newClassifications));
            }
            else
            {
                // we're at capacity.  Remove the oldest entry from the linked list.  Hold onto that linked list
                // node so we can reuse it without an allocation below.

                var firstNode = _spans.First;
                Contract.ThrowIfNull(firstNode);
                _spans.RemoveFirst();

                // Now, remove the entry from the map as well.
#if NET
                Contract.ThrowIfFalse(_spanToClassifiedSpansMap.Remove(firstNode.Value, out var tuple));
#else
                var tuple = _spanToClassifiedSpansMap[firstNode.Value];
                Contract.ThrowIfFalse(_spanToClassifiedSpansMap.Remove(firstNode.Value));
#endif

                var (existingNode, existingClassifications) = tuple;
                Contract.ThrowIfTrue(firstNode != existingNode);

                // Reuse the classifications array as well, so we don't incur a new allocation.
                ClearExistingClassificationsAndAddNewClassificationsToIt(existingClassifications, newClassifications);

                // Place the first node, which we removed, (with its value updated to the current span) at the end
                // of the list.  And update the map to contain this updated information.
                firstNode.Value = span;
                _spans.AddLast(firstNode);
                AddToMap(firstNode, existingClassifications);
            }

            return;

            void AddToMap(LinkedListNode<Span> node, SegmentedList<ClassifiedSpan> classificationsCopy)
                => _spanToClassifiedSpansMap.Add(node.Value, (node, classificationsCopy));
        }
    }
}
