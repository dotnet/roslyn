// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Runtime.CompilerServices;
using System.Threading;

namespace Microsoft.CodeAnalysis.Collections
{
    internal readonly partial struct ImmutableSegmentedDictionary<TKey, TValue>
    {
        /// <summary>
        /// Private helper class for use only by <see cref="RoslynImmutableInterlocked"/> and
        /// <see cref="SegmentedCollectionsMarshal"/>.
        /// </summary>
        internal static class PrivateMarshal
        {
            internal static ImmutableSegmentedDictionary<TKey, TValue> VolatileRead(in ImmutableSegmentedDictionary<TKey, TValue> location)
            {
                var dictionary = Volatile.Read(ref Unsafe.AsRef(in location._dictionary));
                if (dictionary is null)
                    return default;

                return new ImmutableSegmentedDictionary<TKey, TValue>(dictionary);
            }

            internal static ImmutableSegmentedDictionary<TKey, TValue> InterlockedExchange(ref ImmutableSegmentedDictionary<TKey, TValue> location, ImmutableSegmentedDictionary<TKey, TValue> value)
            {
                var dictionary = Interlocked.Exchange(ref Unsafe.AsRef(in location._dictionary), value._dictionary);
                if (dictionary is null)
                    return default;

                return new ImmutableSegmentedDictionary<TKey, TValue>(dictionary);
            }

            internal static ImmutableSegmentedDictionary<TKey, TValue> InterlockedCompareExchange(ref ImmutableSegmentedDictionary<TKey, TValue> location, ImmutableSegmentedDictionary<TKey, TValue> value, ImmutableSegmentedDictionary<TKey, TValue> comparand)
            {
                var dictionary = Interlocked.CompareExchange(ref Unsafe.AsRef(in location._dictionary), value._dictionary, comparand._dictionary);
                if (dictionary is null)
                    return default;

                return new ImmutableSegmentedDictionary<TKey, TValue>(dictionary);
            }

            /// <inheritdoc cref="SegmentedCollectionsMarshal.GetValueRefOrNullRef{TKey, TValue}(ImmutableSegmentedDictionary{TKey, TValue}, TKey)"/>
            public static ref readonly TValue FindValue(ImmutableSegmentedDictionary<TKey, TValue> dictionary, TKey key)
                => ref SegmentedCollectionsMarshal.GetValueRefOrNullRef(dictionary._dictionary, key);

            /// <inheritdoc cref="SegmentedCollectionsMarshal.AsImmutableSegmentedDictionary{TKey, TValue}(SegmentedDictionary{TKey, TValue}?)"/>
            internal static ImmutableSegmentedDictionary<TKey, TValue> AsImmutableSegmentedDictionary(SegmentedDictionary<TKey, TValue>? dictionary)
                => dictionary is not null ? new ImmutableSegmentedDictionary<TKey, TValue>(dictionary) : default;

            /// <inheritdoc cref="SegmentedCollectionsMarshal.AsSegmentedDictionary{TKey, TValue}(ImmutableSegmentedDictionary{TKey, TValue})"/>
            internal static SegmentedDictionary<TKey, TValue>? AsSegmentedDictionary(ImmutableSegmentedDictionary<TKey, TValue> dictionary)
                => dictionary._dictionary;
        }
    }
}
