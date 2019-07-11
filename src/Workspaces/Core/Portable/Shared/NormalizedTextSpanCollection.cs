// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Shared
{
    internal class NormalizedTextSpanCollection : ReadOnlyCollection<TextSpan>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="NormalizedTextSpanCollection"/> that is
        /// empty.
        /// </summary>
        public NormalizedTextSpanCollection()
            : base(new List<TextSpan>(0))
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="NormalizedTextSpanCollection"/> that contains the specified span.
        /// </summary>
        /// <param name="span">TextSpan contained by the span set.</param>
        public NormalizedTextSpanCollection(TextSpan span)
            : base(ListFromSpan(span))
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="NormalizedTextSpanCollection"/> that contains the specified list of spans.
        /// </summary>
        /// <param name="spans">The spans to be added.</param>
        /// <remarks>
        /// <para>The list of spans will be sorted and normalized (overlapping and adjoining spans will be combined).</para>
        /// <para>This constructor runs in O(N log N) time, where N = spans.Count.</para></remarks>
        /// <exception cref="ArgumentNullException"><paramref name="spans"/> is null.</exception>
        public NormalizedTextSpanCollection(IEnumerable<TextSpan> spans)
            : base(NormalizedTextSpanCollection.NormalizeSpans(spans))
        {
            // NormalizeSpans will throw if spans == null.
        }

        /// <summary>
        /// Finds the union of two span sets.
        /// </summary>
        /// <param name="left">
        /// The first span set.
        /// </param>
        /// <param name="right">
        /// The second span set.
        /// </param>
        /// <returns>
        /// The new span set that corresponds to the union of <paramref name="left"/> and <paramref name="right"/>.
        /// </returns>
        /// <remarks>This operator runs in O(N+M) time where N = left.Count, M = right.Count.</remarks>
        /// <exception cref="ArgumentNullException">Either <paramref name="left"/> or <paramref name="right"/> is null.</exception>
        public static NormalizedTextSpanCollection Union(NormalizedTextSpanCollection left, NormalizedTextSpanCollection right)
        {
            if (left == null)
            {
                throw new ArgumentNullException(nameof(left));
            }

            if (right == null)
            {
                throw new ArgumentNullException(nameof(right));
            }

            if (left.Count == 0)
            {
                return right;
            }

            if (right.Count == 0)
            {
                return left;
            }

            var spans = new OrderedSpanList();

            var index1 = 0;
            var index2 = 0;

            var start = -1;
            var end = int.MaxValue;
            while (index1 < left.Count && index2 < right.Count)
            {
                var span1 = left[index1];
                var span2 = right[index2];

                if (span1.Start < span2.Start)
                {
                    NormalizedTextSpanCollection.UpdateSpanUnion(span1, spans, ref start, ref end);
                    ++index1;
                }
                else
                {
                    NormalizedTextSpanCollection.UpdateSpanUnion(span2, spans, ref start, ref end);
                    ++index2;
                }
            }

            while (index1 < left.Count)
            {
                NormalizedTextSpanCollection.UpdateSpanUnion(left[index1], spans, ref start, ref end);
                ++index1;
            }

            while (index2 < right.Count)
            {
                NormalizedTextSpanCollection.UpdateSpanUnion(right[index2], spans, ref start, ref end);
                ++index2;
            }

            if (end != int.MaxValue)
            {
                spans.Add(TextSpan.FromBounds(start, end));
            }

            return new NormalizedTextSpanCollection(spans);
        }

        /// <summary>
        /// Finds the overlap of two span sets.
        /// </summary>
        /// <param name="left">The first span set.</param>
        /// <param name="right">The second span set.</param>
        /// <returns>The new span set that corresponds to the overlap of <paramref name="left"/> and <paramref name="right"/>.</returns>
        /// <remarks>This operator runs in O(N+M) time where N = left.Count, M = right.Count.</remarks>
        /// <exception cref="ArgumentNullException"><paramref name="left"/> or <paramref name="right"/> is null.</exception>
        public static NormalizedTextSpanCollection Overlap(NormalizedTextSpanCollection left, NormalizedTextSpanCollection right)
        {
            if (left == null)
            {
                throw new ArgumentNullException(nameof(left));
            }

            if (right == null)
            {
                throw new ArgumentNullException(nameof(right));
            }

            if (left.Count == 0)
            {
                return left;
            }

            if (right.Count == 0)
            {
                return right;
            }

            var spans = new OrderedSpanList();
            for (int index1 = 0, index2 = 0; index1 < left.Count && index2 < right.Count;)
            {
                var span1 = left[index1];
                var span2 = right[index2];

                if (span1.OverlapsWith(span2))
                {
                    spans.Add(span1.Overlap(span2).Value);
                }

                if (span1.End < span2.End)
                {
                    ++index1;
                }
                else if (span1.End == span2.End)
                {
                    ++index1;
                    ++index2;
                }
                else
                {
                    ++index2;
                }
            }

            return new NormalizedTextSpanCollection(spans);
        }

        /// <summary>
        /// Finds the intersection of two span sets.
        /// </summary>
        /// <param name="left">The first span set.</param>
        /// <param name="right">The second span set.</param>
        /// <returns>The new span set that corresponds to the intersection of <paramref name="left"/> and <paramref name="right"/>.</returns>
        /// <remarks>This operator runs in O(N+M) time where N = left.Count, M = right.Count.</remarks>
        /// <exception cref="ArgumentNullException"><paramref name="left"/> is null.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="right"/> is null.</exception>
        public static NormalizedTextSpanCollection Intersection(NormalizedTextSpanCollection left, NormalizedTextSpanCollection right)
        {
            if (left == null)
            {
                throw new ArgumentNullException(nameof(left));
            }

            if (right == null)
            {
                throw new ArgumentNullException(nameof(right));
            }

            if (left.Count == 0)
            {
                return left;
            }

            if (right.Count == 0)
            {
                return right;
            }

            var spans = new OrderedSpanList();
            for (int index1 = 0, index2 = 0; (index1 < left.Count) && (index2 < right.Count);)
            {
                var span1 = left[index1];
                var span2 = right[index2];

                if (span1.IntersectsWith(span2))
                {
                    spans.Add(span1.Intersection(span2).Value);
                }

                if (span1.End < span2.End)
                {
                    ++index1;
                }
                else
                {
                    ++index2;
                }
            }

            return new NormalizedTextSpanCollection(spans);
        }

        /// <summary>
        /// Finds the difference between two sets. The difference is defined as everything in the first span set that is not in the second span set.
        /// </summary>
        /// <param name="left">The first span set.</param>
        /// <param name="right">The second span set.</param>
        /// <returns>The new span set that corresponds to the difference between <paramref name="left"/> and <paramref name="right"/>.</returns>
        /// <remarks>
        /// Empty spans in the second set do not affect the first set at all. This method returns empty spans in the first set that are not contained by any set in
        /// the second set.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="left"/> is null.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="right"/> is null.</exception>
        public static NormalizedTextSpanCollection Difference(NormalizedTextSpanCollection left, NormalizedTextSpanCollection right)
        {
            if (left == null)
            {
                throw new ArgumentNullException(nameof(left));
            }

            if (right == null)
            {
                throw new ArgumentNullException(nameof(right));
            }

            if (left.Count == 0)
            {
                return left;
            }

            if (right.Count == 0)
            {
                return left;
            }

            var spans = new OrderedSpanList();

            var index1 = 0;
            var index2 = 0;
            var lastEnd = -1;
            do
            {
                var span1 = left[index1];
                var span2 = right[index2];

                if ((span2.Length == 0) || (span1.Start >= span2.End))
                {
                    ++index2;
                }
                else if (span1.End <= span2.Start)
                {
                    // lastEnd is set to the end of the previously encountered intersecting span
                    // from right when it ended before the end of span1 (so it must still be less
                    // than the end of span1).
                    Debug.Assert(lastEnd < span1.End);
                    spans.Add(TextSpan.FromBounds(Math.Max(lastEnd, span1.Start), span1.End));
                    ++index1;
                }
                else
                {
                    // The spans intersect, so add anything from span1 that extends to the left of span2.
                    if (span1.Start < span2.Start)
                    {
                        // lastEnd is set to the end of the previously encountered intersecting span
                        // on span2, so it must be less than the start of the current span on span2.
                        Debug.Assert(lastEnd < span2.Start);
                        spans.Add(TextSpan.FromBounds(Math.Max(lastEnd, span1.Start), span2.Start));
                    }

                    if (span1.End < span2.End)
                    {
                        ++index1;
                    }
                    else if (span1.End == span2.End)
                    {
                        // Both spans ended at the same place so we're done with both.
                        ++index1;
                        ++index2;
                    }
                    else
                    {
                        // span2 ends before span1, so keep track of where it ended so that we don't
                        // try to add the excluded portion the next time we add a span.
                        lastEnd = span2.End;
                        ++index2;
                    }
                }
            }
            while ((index1 < left.Count) && (index2 < right.Count));

            while (index1 < left.Count)
            {
                var span1 = left[index1++];
                spans.Add(TextSpan.FromBounds(Math.Max(lastEnd, span1.Start), span1.End));
            }

            return new NormalizedTextSpanCollection(spans);
        }

        /// <summary>
        /// Determines whether two span sets are the same. 
        /// </summary>
        /// <param name="left">The first set.</param>
        /// <param name="right">The second set.</param>
        /// <returns><c>true</c> if the two sets are equivalent, otherwise <c>false</c>.</returns>
        public static bool operator ==(NormalizedTextSpanCollection left, NormalizedTextSpanCollection right)
        {
            if (object.ReferenceEquals(left, right))
            {
                return true;
            }

            if (left is null || right is null)
            {
                return false;
            }

            if (left.Count != right.Count)
            {
                return false;
            }

            for (var i = 0; i < left.Count; ++i)
            {
                if (left[i] != right[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Determines whether two span sets are not the same.
        /// </summary>
        /// <param name="left">The first set.</param>
        /// <param name="right">The second set.</param>
        /// <returns><c>true</c> if the two sets are not equivalent, otherwise <c>false</c>.</returns>
        public static bool operator !=(NormalizedTextSpanCollection left, NormalizedTextSpanCollection right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Determines whether this span set overlaps with another span set.
        /// </summary>
        /// <param name="set">The span set to test.</param>
        /// <returns><c>true</c> if the span sets overlap, otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="set"/> is null.</exception>
        public bool OverlapsWith(NormalizedTextSpanCollection set)
        {
            if (set == null)
            {
                throw new ArgumentNullException(nameof(set));
            }

            for (int index1 = 0, index2 = 0; (index1 < this.Count) && (index2 < set.Count);)
            {
                var span1 = this[index1];
                var span2 = set[index2];

                if (span1.OverlapsWith(span2))
                {
                    return true;
                }

                if (span1.End < span2.End)
                {
                    ++index1;
                }
                else if (span1.End == span2.End)
                {
                    ++index1;
                    ++index2;
                }
                else
                {
                    ++index2;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines whether this span set overlaps with another span.
        /// </summary>
        /// <param name="span">The span to test.</param>
        /// <returns><c>true</c> if this span set overlaps with the given span, otherwise <c>false</c>.</returns>
        public bool OverlapsWith(TextSpan span)
        {
            // TODO: binary search
            for (var index = 0; index < this.Count; ++index)
            {
                if (this[index].OverlapsWith(span))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines whether this span set intersects with another span set.
        /// </summary>
        /// <param name="set">Set to test.</param>
        /// <returns><c>true</c> if the span sets intersect, otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="set"/> is null.</exception>
        public bool IntersectsWith(NormalizedTextSpanCollection set)
        {
            if (set == null)
            {
                throw new ArgumentNullException(nameof(set));
            }

            for (int index1 = 0, index2 = 0; (index1 < this.Count) && (index2 < set.Count);)
            {
                var span1 = this[index1];
                var span2 = set[index2];

                if (span1.IntersectsWith(span2))
                {
                    return true;
                }

                if (span1.End < span2.End)
                {
                    ++index1;
                }
                else
                {
                    ++index2;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines whether this span set intersects with another span.
        /// </summary>
        /// <returns><c>true</c> if this span set intersects with the given span, otherwise <c>false</c>.</returns>
        public bool IntersectsWith(TextSpan span)
        {
            // TODO: binary search
            for (var index = 0; index < this.Count; ++index)
            {
                if (this[index].IntersectsWith(span))
                {
                    return true;
                }
            }

            return false;
        }

        #region Overridden methods and operators

        /// <summary>
        /// Gets a unique hash code for the span set.
        /// </summary>
        /// <returns>A 32-bit hash code associated with the set.</returns>
        public override int GetHashCode()
        {
            var hc = 0;
            foreach (var s in this)
            {
                hc ^= s.GetHashCode();
            }

            return hc;
        }

        /// <summary>
        /// Determines whether this span set is the same as another object.
        /// </summary>
        /// <param name="obj">The object to test.</param>
        /// <returns><c>true</c> if the two objects are equal, otherwise <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            var set = obj as NormalizedTextSpanCollection;

            return this == set;
        }

        /// <summary>
        /// Provides a string representation of the set.
        /// </summary>
        /// <returns>The string representation of the set.</returns>
        public override string ToString()
        {
            var value = new StringBuilder("{");
            foreach (var s in this)
            {
                value.Append(s.ToString());
            }

            value.Append("}");

            return value.ToString();
        }

        #endregion // Overridden methods and operators

        #region Private Helpers
        private static IList<TextSpan> ListFromSpan(TextSpan span)
        {
            IList<TextSpan> list = new List<TextSpan>(1);
            list.Add(span);
            return list;
        }

        /// <summary>
        /// Private constructor for use when the span list is already normalized.
        /// </summary>
        /// <param name="normalizedSpans">An already normalized span list.</param>
        private NormalizedTextSpanCollection(OrderedSpanList normalizedSpans)
            : base(normalizedSpans)
        {
        }

        private static void UpdateSpanUnion(TextSpan span, IList<TextSpan> spans, ref int start, ref int end)
        {
            if (end < span.Start)
            {
                spans.Add(TextSpan.FromBounds(start, end));

                start = -1;
                end = int.MaxValue;
            }

            if (end == int.MaxValue)
            {
                start = span.Start;
                end = span.End;
            }
            else
            {
                end = Math.Max(end, span.End);
            }
        }

        private static IList<TextSpan> NormalizeSpans(IEnumerable<TextSpan> spans)
        {
            if (spans == null)
            {
                throw new ArgumentNullException(nameof(spans));
            }

            var sorted = new List<TextSpan>(spans);
            if (sorted.Count <= 1)
            {
                return sorted;
            }
            else
            {
                sorted.Sort(delegate (TextSpan s1, TextSpan s2) { return s1.Start.CompareTo(s2.Start); });

                IList<TextSpan> normalized = new List<TextSpan>(sorted.Count);

                var oldStart = sorted[0].Start;
                var oldEnd = sorted[0].End;
                for (var i = 1; i < sorted.Count; ++i)
                {
                    var newStart = sorted[i].Start;
                    var newEnd = sorted[i].End;
                    if (oldEnd < newStart)
                    {
                        normalized.Add(TextSpan.FromBounds(oldStart, oldEnd));
                        oldStart = newStart;
                        oldEnd = newEnd;
                    }
                    else
                    {
                        oldEnd = Math.Max(oldEnd, newEnd);
                    }
                }

                normalized.Add(TextSpan.FromBounds(oldStart, oldEnd));
                return normalized;
            }
        }

        private class OrderedSpanList : List<TextSpan>
        {
        }

        #endregion // Private Helpers
    }
}
