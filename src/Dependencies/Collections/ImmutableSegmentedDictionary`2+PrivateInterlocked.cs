// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Threading;

namespace Microsoft.CodeAnalysis.Collections
{
    internal readonly partial struct ImmutableSegmentedDictionary<TKey, TValue>
    {
        /// <summary>
        /// Private helper class for use only by <see cref="RoslynImmutableInterlocked"/>.
        /// </summary>
        internal static class PrivateInterlocked
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
        }
    }
}
