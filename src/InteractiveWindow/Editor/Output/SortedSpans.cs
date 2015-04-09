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
                int startIndex = _spans.BinarySearch(span, comparer);
                if (startIndex < 0)
                {
                    startIndex = ~startIndex - 1;
                }

                if (startIndex < 0)
                {
                    return Enumerable.Empty<Span>();
                }

                int spanEnd = span.End;
                for (int i = startIndex; i < _spans.Count && _spans[i].Start < spanEnd; i++)
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
