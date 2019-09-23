// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        {
            var result = new LineSpan();
            result.Start = start;
            result.End = end;
            return result;
        }

        public bool Equals(LineSpan other)
        {
            return this.Start == other.Start && this.End == other.End;
        }

        public override bool Equals(object? obj)
        {
            return obj is LineSpan other
                && this.Equals(other);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(this.Start, this.End);
        }
    }
}
