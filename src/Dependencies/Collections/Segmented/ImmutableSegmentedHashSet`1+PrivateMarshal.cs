// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Threading;

namespace Microsoft.CodeAnalysis.Collections
{
    internal readonly partial struct ImmutableSegmentedHashSet<T>
    {
        /// <summary>
        /// Private helper class for use only by <see cref="RoslynImmutableInterlocked"/> and
        /// <see cref="SegmentedCollectionsMarshal"/>.
        /// </summary>
        internal static class PrivateMarshal
        {
            internal static ImmutableSegmentedHashSet<T> VolatileRead(in ImmutableSegmentedHashSet<T> location)
            {
                var set = Volatile.Read(ref Unsafe.AsRef(in location._set));
                if (set is null)
                    return default;

                return new ImmutableSegmentedHashSet<T>(set);
            }

            internal static ImmutableSegmentedHashSet<T> InterlockedExchange(ref ImmutableSegmentedHashSet<T> location, ImmutableSegmentedHashSet<T> value)
            {
                var set = Interlocked.Exchange(ref Unsafe.AsRef(in location._set), value._set);
                if (set is null)
                    return default;

                return new ImmutableSegmentedHashSet<T>(set);
            }

            internal static ImmutableSegmentedHashSet<T> InterlockedCompareExchange(ref ImmutableSegmentedHashSet<T> location, ImmutableSegmentedHashSet<T> value, ImmutableSegmentedHashSet<T> comparand)
            {
                var set = Interlocked.CompareExchange(ref Unsafe.AsRef(in location._set), value._set, comparand._set);
                if (set is null)
                    return default;

                return new ImmutableSegmentedHashSet<T>(set);
            }

            /// <inheritdoc cref="SegmentedCollectionsMarshal.AsImmutableSegmentedHashSet{T}(SegmentedHashSet{T}?)"/>
            internal static ImmutableSegmentedHashSet<T> AsImmutableSegmentedHashSet(SegmentedHashSet<T>? set)
                => set is not null ? new ImmutableSegmentedHashSet<T>(set) : default;

            /// <inheritdoc cref="SegmentedCollectionsMarshal.AsSegmentedHashSet{T}(ImmutableSegmentedHashSet{T})"/>
            internal static SegmentedHashSet<T>? AsSegmentedHashSet(ImmutableSegmentedHashSet<T> set)
                => set._set;
        }
    }
}
