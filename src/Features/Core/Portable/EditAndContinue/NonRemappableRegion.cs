// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
    internal readonly struct NonRemappableRegion : IEquatable<NonRemappableRegion>
    {
        /// <summary>
        /// Pre-remap PDB span.
        /// </summary>
        public readonly SourceFileSpan Span;

        /// <summary>
        /// Difference between new span and pre-remap span (new = old + delta).
        /// </summary>
        public readonly int LineDelta;

        /// <summary>
        /// True if the region represents an exception region, false if it represents an active statement.
        /// </summary>
        public readonly bool IsExceptionRegion;

        public NonRemappableRegion(SourceFileSpan span, int lineDelta, bool isExceptionRegion)
        {
            Span = span;
            LineDelta = lineDelta;
            IsExceptionRegion = isExceptionRegion;
        }

        public override bool Equals(object? obj)
            => obj is NonRemappableRegion region && Equals(region);

        public bool Equals(NonRemappableRegion other)
            => Span.Equals(other.Span) &&
               LineDelta == other.LineDelta &&
               IsExceptionRegion == other.IsExceptionRegion;

        public override int GetHashCode()
            => Hash.Combine(Span.GetHashCode(), Hash.Combine(IsExceptionRegion, LineDelta));

        public static bool operator ==(NonRemappableRegion left, NonRemappableRegion right)
            => left.Equals(right);

        public static bool operator !=(NonRemappableRegion left, NonRemappableRegion right)
            => !(left == right);

        public NonRemappableRegion WithLineDelta(int value)
            => new(Span, value, IsExceptionRegion);

        internal string GetDebuggerDisplay()
            => $"{(IsExceptionRegion ? "ER" : "AS")} {Span} δ={LineDelta}";
    }
}
