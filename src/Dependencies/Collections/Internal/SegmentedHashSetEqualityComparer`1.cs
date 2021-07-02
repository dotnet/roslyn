// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// NOTE: This code is derived from an implementation originally in dotnet/runtime:
// https://github.com/dotnet/runtime/blob/v5.0.7/src/libraries/System.Private.CoreLib/src/System/Collections/Generic/HashSetEqualityComparer.cs
//
// See the commentary in https://github.com/dotnet/roslyn/pull/50156 for notes on incorporating changes made to the
// reference implementation.

namespace System.Collections.Generic
{
    /// <summary>Equality comparer for hashsets of hashsets</summary>
    internal sealed class HashSetEqualityComparer<T> : IEqualityComparer<HashSet<T>?>
    {
        public bool Equals(HashSet<T>? x, HashSet<T>? y)
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

            EqualityComparer<T> defaultComparer = EqualityComparer<T>.Default;

            // If both sets use the same comparer, they're equal if they're the same
            // size and one is a "subset" of the other.
            if (HashSet<T>.EqualityComparersAreEqual(x, y))
            {
                return x.Count == y.Count && y.IsSubsetOfHashSetWithSameComparer(x);
            }

            // Otherwise, do an O(N^2) match.
            foreach (T yi in y)
            {
                bool found = false;
                foreach (T xi in x)
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

        public int GetHashCode(HashSet<T>? obj)
        {
            int hashCode = 0; // default to 0 for null/empty set

            if (obj != null)
            {
                foreach (T t in obj)
                {
                    if (t != null)
                    {
                        hashCode ^= t.GetHashCode(); // same hashcode as as default comparer
                    }
                }
            }

            return hashCode;
        }

        // Equals method for the comparer itself.
        public override bool Equals(object? obj) => obj is HashSetEqualityComparer<T>;

        public override int GetHashCode() => EqualityComparer<T>.Default.GetHashCode();
    }
}
