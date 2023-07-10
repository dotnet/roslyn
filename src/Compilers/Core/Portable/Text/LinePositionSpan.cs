// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Serialization;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Text
{
    /// <summary>
    /// Immutable span represented by a pair of line number and index within the line.
    /// </summary>
    [DataContract]
    public readonly struct LinePositionSpan : IEquatable<LinePositionSpan>
    {
        [DataMember(Order = 0)]
        private readonly LinePosition _start;

        [DataMember(Order = 1)]
        private readonly LinePosition _end;

        /// <summary>
        /// Creates <see cref="LinePositionSpan"/>.
        /// </summary>
        /// <param name="start">Start position.</param>
        /// <param name="end">End position.</param>
        /// <exception cref="ArgumentException"><paramref name="end"/> precedes <paramref name="start"/>.</exception>
        public LinePositionSpan(LinePosition start, LinePosition end)
        {
            if (end < start)
            {
                throw new ArgumentException(string.Format(CodeAnalysisResources.EndMustNotBeLessThanStart, start, end), nameof(end));
            }

            _start = start;
            _end = end;
        }

        /// <summary>
        /// Gets the start position of the span.
        /// </summary>
        public LinePosition Start
        {
            get { return _start; }
        }

        /// <summary>
        /// Gets the end position of the span.
        /// </summary>
        public LinePosition End
        {
            get { return _end; }
        }

        public override bool Equals(object? obj)
        {
            return obj is LinePositionSpan span && Equals(span);
        }

        public bool Equals(LinePositionSpan other)
        {
            return _start.Equals(other._start)
                && _end.Equals(other._end);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(_start.GetHashCode(), _end.GetHashCode());
        }

        public static bool operator ==(LinePositionSpan left, LinePositionSpan right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LinePositionSpan left, LinePositionSpan right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Provides a string representation for <see cref="LinePositionSpan"/>.
        /// </summary>
        /// <example>(0,0)-(5,6)</example>
        public override string ToString()
        {
            return string.Format("({0})-({1})", _start, _end);
        }
    }
}
