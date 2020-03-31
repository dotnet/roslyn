// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    // Like Span, except it has a start/end line instead of a start/end position.
    internal struct LineSpan : IEquatable<LineSpan>
    {
        // inclusive
        public int Start { get; private set; }

        // exclusive
        public int End { get; private set; }

        public static LineSpan FromBounds(int start, int end)
            => new LineSpan
            {
                Start = start,
                End = end
            };

        public bool Equals(LineSpan other)
            => this.Start == other.Start && this.End == other.End;

        public override bool Equals(object? obj)
        {
            return obj is LineSpan other
                && this.Equals(other);
        }

        public override int GetHashCode()
            => Hash.Combine(this.Start, this.End);
    }
}
