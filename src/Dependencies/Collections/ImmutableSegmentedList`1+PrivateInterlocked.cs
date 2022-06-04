// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Threading;

namespace Microsoft.CodeAnalysis.Collections
{
    internal partial struct ImmutableSegmentedList<T>
    {
        /// <summary>
        /// Private helper class for use only by <see cref="RoslynImmutableInterlocked"/>.
        /// </summary>
        internal static class PrivateInterlocked
        {
            internal static ImmutableSegmentedList<T> VolatileRead(in ImmutableSegmentedList<T> location)
            {
                var list = Volatile.Read(ref Unsafe.AsRef(in location._list));
                if (list is null)
                    return default;

                return new ImmutableSegmentedList<T>(list);
            }

            internal static ImmutableSegmentedList<T> InterlockedExchange(ref ImmutableSegmentedList<T> location, ImmutableSegmentedList<T> value)
            {
                var list = Interlocked.Exchange(ref Unsafe.AsRef(in location._list), value._list);
                if (list is null)
                    return default;

                return new ImmutableSegmentedList<T>(list);
            }

            internal static ImmutableSegmentedList<T> InterlockedCompareExchange(ref ImmutableSegmentedList<T> location, ImmutableSegmentedList<T> value, ImmutableSegmentedList<T> comparand)
            {
                var list = Interlocked.CompareExchange(ref Unsafe.AsRef(in location._list), value._list, comparand._list);
                if (list is null)
                    return default;

                return new ImmutableSegmentedList<T>(list);
            }
        }
    }
}
