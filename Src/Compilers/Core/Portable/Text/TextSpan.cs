// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Text
{
    /// <summary>
    /// Immutable abstract representation of a span of text.  For example, in an error diagnostic that reports a
    /// location, it could come from a parsed string, text from a tool editor buffer, etc.
    /// </summary>
    public struct TextSpan : IEquatable<TextSpan>, IComparable<TextSpan>
    {
        private readonly int start;
        private readonly int length;

        /// <summary>
        /// Creates a TextSpan instance beginning with the position Start and having the Length
        /// specified with length.
        /// </summary>
        public TextSpan(int start, int length)
        {
            if (start < 0)
            {
                throw new ArgumentOutOfRangeException("start");
            }

            if (start + length < start)
            {
                throw new ArgumentOutOfRangeException("length");
            }

            this.start = start;
            this.length = length;
        }

        /// <summary>
        /// Start point of the Span.
        /// </summary>
        public int Start
        {
            get
            {
                return start;
            }
        }

        /// <summary>
        /// End of the span.
        /// </summary>
        public int End
        {
            get
            {
                return start + length;
            }
        }

        /// <summary>
        /// Length of the span.
        /// </summary>
        public int Length
        {
            get
            {
                return length;
            }
        }

        /// <summary>
        /// Determines whether or not the span is empty.
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                return this.Length == 0;
            }
        }

        /// <summary>
        /// Determines whether the position lies within the span.
        /// </summary>
        /// <param name="position">
        /// The position to check.
        /// </param>
        /// <returns>
        /// <c>true</c> if the position is greater than or equal to Start and strictly less 
        /// than End, otherwise <c>false</c>.
        /// </returns>
        public bool Contains(int position)
        {
            return unchecked((uint)(position - start) < (uint)length);
        }

        /// <summary>
        /// Determines whether <paramref name="span"/> falls completely within this span.
        /// </summary>
        /// <param name="span">
        /// The span to check.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified span falls completely within this span, otherwise <c>false</c>.
        /// </returns>
        public bool Contains(TextSpan span)
        {
            return span.Start >= start && span.End <= this.End;
        }

        /// <summary>
        /// Determines whether <paramref name="span"/> overlaps this span. Two spans are considered to overlap 
        /// if they have positions in common and neither is empty. Empty spans do not overlap with any 
        /// other span.
        /// </summary>
        /// <param name="span">
        /// The span to check.
        /// </param>
        /// <returns>
        /// <c>true</c> if the spans overlap, otherwise <c>false</c>.
        /// </returns>
        public bool OverlapsWith(TextSpan span)
        {
            int overlapStart = Math.Max(start, span.Start);
            int overlapEnd = Math.Min(this.End, span.End);

            return overlapStart < overlapEnd;
        }

        /// <summary>
        /// Returns the overlap with the given span, or null if there is no overlap.
        /// </summary>
        /// <param name="span">
        /// The span to check.
        /// </param>
        /// <returns>
        /// The overlap of the spans, or null if the overlap is empty.
        /// </returns>
        public TextSpan? Overlap(TextSpan span)
        {
            int overlapStart = Math.Max(start, span.Start);
            int overlapEnd = Math.Min(this.End, span.End);

            return overlapStart < overlapEnd
                ? TextSpan.FromBounds(overlapStart, overlapEnd)
                : (TextSpan?)null;
        }

        /// <summary>
        /// Determines whether <paramref name="span"/> intersects this span. Two spans are considered to 
        /// intersect if they have positions in common or the end of one span 
        /// coincides with the start of the other span.
        /// </summary>
        /// <param name="span">
        /// The span to check.
        /// </param>
        /// <returns>
        /// <c>true</c> if the spans intersect, otherwise <c>false</c>.
        /// </returns>
        public bool IntersectsWith(TextSpan span)
        {
            return span.Start <= this.End && span.End >= start;
        }

        /// <summary>
        /// Determines whether <paramref name="position"/> intersects this span. 
        /// A position is considered to intersect if it is between the start and
        /// end positions (inclusive) of this span.
        /// </summary>
        /// <param name="position">
        /// The position to check.
        /// </param>
        /// <returns>
        /// <c>true</c> if the position intersects, otherwise <c>false</c>.
        /// </returns>
        public bool IntersectsWith(int position)
        {
            return unchecked((uint)(position - start) <= (uint)length);
        }

        /// <summary>
        /// Returns the intersection with the given span, or null if there is no intersection.
        /// </summary>
        /// <param name="span">
        /// The span to check.
        /// </param>
        /// <returns>
        /// The intersection of the spans, or null if the intersection is empty.
        /// </returns>
        public TextSpan? Intersection(TextSpan span)
        {
            int intersectStart = Math.Max(start, span.Start);
            int intersectEnd = Math.Min(this.End, span.End);

            return intersectStart <= intersectEnd
                ? TextSpan.FromBounds(intersectStart, intersectEnd)
                : (TextSpan?)null;
        }

        /// <summary>
        /// Creates a new <see cref="T:TextSpan"/> from <param name="start" /> and <param
        /// name="end"/> positions as opposed to a position and length.
        /// </summary>
        public static TextSpan FromBounds(int start, int end)
        {
            if (start < 0)
            {
                throw new ArgumentOutOfRangeException("start", CodeAnalysisResources.StartMustNotBeNegative);
            }

            if (end < start)
            {
                throw new ArgumentException("end", CodeAnalysisResources.EndMustNotBeLessThanStart);
            }

            return new TextSpan(start, end - start);
        }

        /// <summary>
        /// Determines if two instances of <see cref="T:TextSpan"/> are the same.
        /// </summary>
        public static bool operator ==(TextSpan left, TextSpan right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines if two instances of <see cref="T:TextSpan"/> are different.
        /// </summary>
        public static bool operator !=(TextSpan left, TextSpan right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Determines if current instance of <see cref="T:TextSpan"/> is equal to another.
        /// </summary>
        public bool Equals(TextSpan other)
        {
            return start == other.start && length == other.length;
        }

        /// <summary>
        /// Determines if current instance of <see cref="T:TextSpan"/> is equal to another.
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is TextSpan && Equals((TextSpan)obj);
        }

        /// <summary>
        /// Produces a hash code for <see cref="T:TextSpan"/>.
        /// </summary>
        public override int GetHashCode()
        {
            return Hash.Combine(start, length);
        }

        /// <summary>
        /// Provides a string representation for <see cref="T:TextSpan"/>.
        /// </summary>
        public override string ToString()
        {
            return string.Format("[{0}..{1})", this.Start, this.End);
        }

        /// <summary>
        /// Compares current instance of <see cref="T:TextSpan"/> with another.
        /// </summary>
        public int CompareTo(TextSpan other)
        {
            var diff = this.start - other.start;
            if (diff != 0)
            {
                return diff;
            }

            return this.length - other.length;
        }
    }
}