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
    internal partial class TagComputer
    {
        /// <summary>
        /// it is a helper class that encapsulates logic on holding onto last classification result.  This type has
        /// thread affinity on the UI thread, so it doesn't need any synchronization.
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

            // mutating state

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

            public void Update(SnapshotSpan snapshotSpan, SegmentedList<ClassifiedSpan> classifications)
            {
                _threadingContext.ThrowIfNotOnUIThread();

                if (classifications.Count > MaxClassificationsCount)
                    return;

                var span = snapshotSpan.Span;
                ClearIfDifferentSnapshot(snapshotSpan.Snapshot);

                if (_spanToClassifiedSpansMap.TryGetValue(span, out var tuple))
                {
                    // Was in cache.  Update the cached classifications to the new ones, and move this span to the front
                    // of end of the LRU list.

                    var (existingNode, existingClassifications) = tuple;
                    existingClassifications.Clear();

                    // AddRange is optimized to take a SegmentedList and copy directly from it into the result list.
                    existingClassifications.AddRange(classifications);

                    _spans.Remove(existingNode);
                    _spans.AddLast(existingNode);

                    Contract.ThrowIfTrue(_spans.Count > CacheSize);
                }
                else
                {
                    // Not in cache.  Add to the cache, and remove the oldest entry if we're at capacity.
                    var node = _spans.AddLast(span);

                    // This constructor fast paths the case where we pass in another SegmentedList.
                    var copy = new SegmentedList<ClassifiedSpan>(classifications);
                    _spanToClassifiedSpansMap.Add(span, (node, copy));

                    if (_spans.Count > CacheSize)
                    {
                        var first = _spans.First;
                        Contract.ThrowIfNull(first);
                        _spans.Remove(first);
                        _spanToClassifiedSpansMap.Remove(first.Value);
                    }

                    Contract.ThrowIfTrue(_spans.Count > CacheSize);
                }
            }
        }
    }
}
