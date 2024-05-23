// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
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
        private readonly record struct SpanAndClassifiedSpans(Span Span, SegmentedList<ClassifiedSpan> ClassifiedSpans);

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

        /// <summary>
        /// The last document id we cached.  This can change when the user switches to another TFM (though the
        /// ITextSnapshot will stay the same).
        /// </summary>
        private DocumentId? _documentId;

        /// <summary>
        /// The last parse options we computed classifications for.  This can change when the user switches to another
        /// mode (like Debug/Release).  We want to dump classifications in that case even though the text snapshot is
        /// the same.
        /// </summary>
        private ParseOptions? _parseOptions;

        /// <summary>
        /// The last text snapshot we cached classifications for.  This can change when the user edits the file.  When
        /// that happens, we want to dump all previous classifications as they are no longer valid.
        /// </summary>
        private ITextSnapshot? _snapshot;

        /// <summary>
        /// LRU list of spans and the classifications for them.  More recent entries are placed at the end of the list.
        /// When we reach <see cref="CacheSize"/> we will start removing from the front.  Items in the middle that are
        /// used again are placed at the end of the list.
        /// </summary>
        private readonly LinkedList<SpanAndClassifiedSpans> _lruList = [];

        /// <summary>
        /// Mapping from span to the corresponding linked list node in <see cref="_lruList"/>.  Allows us to quickly
        /// lookup the cached classifications for a given span, as well as get directly to the linked list node, so we
        /// can manipulate it easily. For example, removing it from its current location (in O(1) time) and adding it to
        /// the end (also O(1)).
        /// </summary>
        private readonly Dictionary<Span, LinkedListNode<SpanAndClassifiedSpans>> _spanToLruNode = [];

        private void ClearOnMajorChange(
            DocumentId documentId,
            ParseOptions? parseOptions,
            ITextSnapshot snapshot)
        {
            _threadingContext.ThrowIfNotOnUIThread();

            if (_documentId != documentId ||
                _parseOptions != parseOptions ||
                _snapshot != snapshot)
            {
                _lruList.Clear();
                _spanToLruNode.Clear();

                _documentId = documentId;
                _parseOptions = parseOptions;
                _snapshot = snapshot;
            }
        }

        public bool TryUseCache(
            DocumentId documentId,
            ParseOptions? parseOptions,
            SnapshotSpan snapshotSpan,
            SegmentedList<ClassifiedSpan> classifications)
        {
            _threadingContext.ThrowIfNotOnUIThread();

            ClearOnMajorChange(documentId, parseOptions, snapshotSpan.Snapshot);

            if (!_spanToLruNode.TryGetValue(snapshotSpan.Span, out var node))
                return false;

            // Move this node to the front of the LRU list.
            _lruList.Remove(node);
            _lruList.AddLast(node);

            // AddRange is optimized to take a SegmentedList and copy directly from it into the result list.
            classifications.AddRange(node.Value.ClassifiedSpans);
            return true;
        }

        public void Update(
            SnapshotSpan snapshotSpan, SegmentedList<ClassifiedSpan> newClassifications)
        {
            _threadingContext.ThrowIfNotOnUIThread();

            if (newClassifications.Count > MaxClassificationsCount)
                return;

            var span = snapshotSpan.Span;

            if (_spanToLruNode.ContainsKey(span))
            {
                Debug.Fail("This should not be possible.  Caller would have seen this item when calling TryUseCache");
                return;
            }

            var node = GetLruNode(span, newClassifications);
            Contract.ThrowIfTrue(node.Value.Span != span);

            _lruList.AddLast(node);
            _spanToLruNode.Add(span, node);

            Contract.ThrowIfTrue(_lruList.Count > CacheSize);
            Contract.ThrowIfTrue(_lruList.Count != _spanToLruNode.Count);
        }

        private LinkedListNode<SpanAndClassifiedSpans> GetLruNode(Span span, SegmentedList<ClassifiedSpan> newClassifications)
        {
            if (_lruList.Count < CacheSize)
            {
                // We're not at capacity.  Create a new node to go at the end of the LRU list. Note: The SegmentedList
                // constructor fast paths the case where we pass in another SegmentedList.
                return new LinkedListNode<SpanAndClassifiedSpans>(new SpanAndClassifiedSpans(span, new SegmentedList<ClassifiedSpan>(newClassifications)));
            }
            else
            {
                // we're at capacity.  Remove the oldest entry from the linked list.  Hold onto that linked list
                // node so we can reuse it without an allocation below.

                var firstNode = _lruList.First;
                Contract.ThrowIfNull(firstNode);
                _lruList.RemoveFirst();

                // Now, remove the entry from the map as well.
#if NET
                Contract.ThrowIfFalse(_spanToLruNode.Remove(firstNode.Value.Span, out var existingNode));
#else
                var existingNode = _spanToLruNode[firstNode.Value.Span];
                Contract.ThrowIfFalse(_spanToLruNode.Remove(firstNode.Value.Span));
#endif

                Contract.ThrowIfTrue(firstNode != existingNode);

                // AddRange is optimized to take a SegmentedList and copy directly from it into the result list.
                firstNode.Value.ClassifiedSpans.Clear();
                firstNode.Value.ClassifiedSpans.AddRange(newClassifications);

                // Place the existing node (which we removed), with its value updated to the current span and new classifications, at the end of
                // the list.  And update the map to contain this updated information.
                firstNode.Value = firstNode.Value with { Span = span };
                return firstNode;
            }
        }
    }
}
