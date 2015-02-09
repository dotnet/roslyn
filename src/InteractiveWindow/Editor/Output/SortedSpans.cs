using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.InteractiveWindow
{
    // TODO (tomat): could we avoid locking by using an ImmutableList?

    /// <summary>
    /// Thread safe sequence of disjoint spans ordered by start position.
    /// </summary>
    internal sealed class SortedSpans
    {
        private readonly object mutex = new object();
        private List<Span> spans = new List<Span>();

        public void Clear()
        {
            spans = new List<Span>();
        }

        public void Add(Span span)
        {
            Debug.Assert(spans.Count == 0 || span.Start >= spans.Last().End);
            Debug.Assert(span.Length > 0);

            lock (mutex)
            {
                int last = spans.Count - 1;
                if (last >= 0 && spans[last].End == span.Start)
                {
                    // merge adjacent spans:
                    spans[last] = new Span(spans[last].Start, spans[last].Length + span.Length);
                }
                else
                {
                    spans.Add(span);
                }
            }
        }

        public IEnumerable<Span> GetOverlap(Span span)
        {
            List<Span> result = null;
            var comparer = SpanStartComparer.Instance;

            lock (mutex)
            {
                int startIndex = spans.BinarySearch(span, comparer);
                if (startIndex < 0)
                {
                    startIndex = ~startIndex - 1;
                }

                if (startIndex < 0)
                {
                    return SpecializedCollections.EmptyEnumerable<Span>();
                }

                int spanEnd = span.End;
                for (int i = startIndex; i < spans.Count && spans[i].Start < spanEnd; i++)
                {
                    var overlap = span.Overlap(spans[i]);
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

            return result ?? SpecializedCollections.EmptyEnumerable<Span>();
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
