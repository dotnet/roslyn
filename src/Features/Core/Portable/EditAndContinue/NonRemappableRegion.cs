// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
    internal readonly struct NonRemappableRegion(SourceFileSpan oldSpan, SourceFileSpan newSpan, bool isExceptionRegion) : IEquatable<NonRemappableRegion>
    {
        /// <summary>
        /// PDB span in pre-remap method version.
        /// </summary>
        /// <remarks>
        /// When a thread is executing in an old version of a method before it is remapped to the new version
        /// its active statement needs to be mapped from <see cref="OldSpan"/> in the old version 
        /// to <see cref="NewSpan"/> in the new version of the method.
        /// </remarks>
        public readonly SourceFileSpan OldSpan = oldSpan;

        /// <summary>
        /// PDB span in the new method version.
        /// </summary>
        public readonly SourceFileSpan NewSpan = newSpan;

        /// <summary>
        /// True if the region represents an exception region, false if it represents an active statement.
        /// </summary>
        public readonly bool IsExceptionRegion = isExceptionRegion;

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
