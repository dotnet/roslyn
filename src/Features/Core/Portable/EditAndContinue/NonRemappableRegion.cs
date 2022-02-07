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
        public readonly SourceFileSpan OldSpan;

        /// <summary>
        /// New PDB span.
        /// </summary>
        public readonly SourceFileSpan NewSpan;

        /// <summary>
        /// True if the region represents an exception region, false if it represents an active statement.
        /// </summary>
        public readonly bool IsExceptionRegion;

        public NonRemappableRegion(SourceFileSpan oldSpan, SourceFileSpan newSpan, bool isExceptionRegion)
        {
            OldSpan = oldSpan;
            NewSpan = newSpan;
            IsExceptionRegion = isExceptionRegion;
        }

        public override bool Equals(object? obj)
            => obj is NonRemappableRegion region && Equals(region);

        public bool Equals(NonRemappableRegion other)
            => OldSpan.Equals(other.OldSpan) &&
               NewSpan.Equals(other.NewSpan) &&
               IsExceptionRegion == other.IsExceptionRegion;

        public override int GetHashCode()
            => Hash.Combine(OldSpan.GetHashCode(), Hash.Combine(IsExceptionRegion, NewSpan.GetHashCode()));

        public static bool operator ==(NonRemappableRegion left, NonRemappableRegion right)
            => left.Equals(right);

        public static bool operator !=(NonRemappableRegion left, NonRemappableRegion right)
            => !(left == right);

        public NonRemappableRegion WithNewSpan(SourceFileSpan newSpan)
            => new(OldSpan, newSpan, IsExceptionRegion);

        internal string GetDebuggerDisplay()
            => $"{(IsExceptionRegion ? "ER" : "AS")} {OldSpan} => {NewSpan.Span}";
    }
}
