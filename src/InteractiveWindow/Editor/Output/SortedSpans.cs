// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.InteractiveWindow
{
    // TODO (tomat): could we avoid locking by using an ImmutableList?

    /// <summary>
    /// Thread safe sequence of disjoint spans ordered by start position.
    /// </summary>
    internal sealed class SortedSpans
    {
        private readonly object _mutex = new object();
        private List<Span> _spans = new List<Span>();

        public void Clear()
        {
            _spans = new List<Span>();
        }

        public void Add(Span span)
        {
            Debug.Assert(_spans.Count == 0 || span.Start >= _spans.Last().End);
            Debug.Assert(span.Length > 0);

            lock (_mutex)
            {
                int last = _spans.Count - 1;
                if (last >= 0 && _spans[last].End == span.Start)
                {
                    // merge adjacent spans:
                    _spans[last] = new Span(_spans[last].Start, _spans[last].Length + span.Length);
                }
                else
                {
                    _spans.Add(span);
                }
            }
        }

        public IEnumerable<Span> GetOverlap(Span span)
        {
            List<Span> result = null;
            var comparer = SpanStartComparer.Instance;

            lock (_mutex)
            {
                var count = _spans.Count;

                // _span is empty, no overlap.
                if (count == 0)
                {
                    return Enumerable.Empty<Span>();
                }

                int startIndex = _spans.BinarySearch(span, comparer);

                // If span is not found in _span, BinarySearch returns a negative number that is the 
                // bitwise complement of the index of the next span with larger starting index, or if
                // there is no such span, the bitwise complement of _span.Count.

                // Try get next span in _span with larger starting index, in case the span starts 
                // before the first one in _span, e.g. span starts within the logo of interactive window.
                // If there's truly no overlap, then the for-loop will stop before first iteration.
                if (startIndex < 0)
                {
                    startIndex = ~startIndex;
                }

                // It still possible that the it overlaps with last span in _span,
                // so we will only check the last one.
                if (startIndex == count)
                {
                    startIndex = startIndex - 1;
                }

                Debug.Assert(startIndex >= 0);

                int spanEnd = span.End;
                for (int i = startIndex; i < count && _spans[i].Start < spanEnd; i++)
                {
                    var overlap = span.Overlap(_spans[i]);
                    if (overlap.HasValue)
                    {
                        if (result == null)
                        {
                            result = new List<Span>();
                        }

                        result.Add(overlap.Value);
                    }
                }
            }

            return result ?? Enumerable.Empty<Span>();
        }

        private sealed class SpanStartComparer : IComparer<Span>
        {
            internal static readonly SpanStartComparer Instance = new SpanStartComparer();

            public int Compare(Span x, Span y)
            {
                return x.Start - y.Start;
            }
        }
    }
}
