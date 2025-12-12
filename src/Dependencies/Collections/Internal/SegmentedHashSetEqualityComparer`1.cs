// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

// NOTE: This code is derived from an implementation originally in dotnet/runtime:
// https://github.com/dotnet/runtime/blob/v8.0.3/src/libraries/System.Private.CoreLib/src/System/Collections/Generic/HashSetEqualityComparer.cs
//
// See the commentary in https://github.com/dotnet/roslyn/pull/50156 for notes on incorporating changes made to the
// reference implementation.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.Collections.Internal
{
    /// <summary>Equality comparer for hashsets of hashsets</summary>
    internal sealed class SegmentedHashSetEqualityComparer<T> : IEqualityComparer<SegmentedHashSet<T>?>
    {
        public bool Equals(SegmentedHashSet<T>? x, SegmentedHashSet<T>? y)
        {
            // If they're the exact same instance, they're equal.
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            // They're not both null, so if either is null, they're not equal.
            if (x == null || y == null)
            {
                return false;
            }

            var defaultComparer = EqualityComparer<T>.Default;

            // If both sets use the same comparer, they're equal if they're the same
            // size and one is a "subset" of the other.
            if (SegmentedHashSet<T>.EqualityComparersAreEqual(x, y))
            {
                return x.Count == y.Count && y.IsSubsetOfHashSetWithSameComparer(x);
            }

            // Otherwise, do an O(N^2) match.
            // 🐛 This is non-symmetrical, but matches original: https://github.com/dotnet/runtime/issues/69218
            foreach (var yi in y)
            {
                var found = false;
                foreach (var xi in x)
                {
                    if (defaultComparer.Equals(yi, xi))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    return false;
                }
            }

            return true;
        }

        public int GetHashCode(SegmentedHashSet<T>? obj)
        {
            var hashCode = 0; // default to 0 for null/empty set

            if (obj != null)
            {
                foreach (var t in obj)
                {
                    if (t != null)
                    {
                        hashCode ^= t.GetHashCode(); // same hashcode as default comparer
                    }
                }
            }

            return hashCode;
        }

        // Equals method for the comparer itself.
        public override bool Equals([NotNullWhen(true)] object? obj) => obj is SegmentedHashSetEqualityComparer<T>;

        public override int GetHashCode() => EqualityComparer<T>.Default.GetHashCode();
    }
}
