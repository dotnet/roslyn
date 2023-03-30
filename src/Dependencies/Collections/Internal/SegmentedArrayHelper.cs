// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Microsoft.CodeAnalysis.Collections.Internal
{
    internal static class SegmentedArrayHelper
    {
        // This is the threshold where Introspective sort switches to Insertion sort.
        // Empirically, 16 seems to speed up most cases without slowing down others, at least for integers.
        // Large value types may benefit from a smaller number.
        internal const int IntrosortSizeThreshold = 16;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetSegmentSize<T>()
        {
#if NETCOREAPP3_0_OR_NEWER
            return InlineCalculateSegmentSize(Unsafe.SizeOf<T>());
#else
            if (Unsafe.SizeOf<T>() == Unsafe.SizeOf<object>())
            {
                return ReferenceTypeSegmentHelper.SegmentSize;
            }
            else
            {
                return ValueTypeSegmentHelper<T>.SegmentSize;
            }
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetSegmentShift<T>()
        {
#if NETCOREAPP3_0_OR_NEWER
            return InlineCalculateSegmentShift(Unsafe.SizeOf<T>());
#else
            if (Unsafe.SizeOf<T>() == Unsafe.SizeOf<object>())
            {
                return ReferenceTypeSegmentHelper.SegmentShift;
            }
            else
            {
                return ValueTypeSegmentHelper<T>.SegmentShift;
            }
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetOffsetMask<T>()
        {
#if NETCOREAPP3_0_OR_NEWER
            return InlineCalculateOffsetMask(Unsafe.SizeOf<T>());
#else
            if (Unsafe.SizeOf<T>() == Unsafe.SizeOf<object>())
            {
                return ReferenceTypeSegmentHelper.OffsetMask;
            }
            else
            {
                return ValueTypeSegmentHelper<T>.OffsetMask;
            }
#endif
        }

        /// <summary>
        /// Calculates the maximum number of elements of size <paramref name="elementSize"/> which can fit into an array
        /// which has the following characteristics:
        /// <list type="bullet">
        /// <item><description>The array can be allocated in the small object heap.</description></item>
        /// <item><description>The array length is a power of 2.</description></item>
        /// </list>
        /// </summary>
        /// <param name="elementSize">The size of the elements in the array.</param>
        /// <returns>The segment size to use for small object heap segmented arrays.</returns>
        private static int CalculateSegmentSize(int elementSize)
        {
            // Default Large Object Heap size threshold
            // https://github.com/dotnet/runtime/blob/c9d69e38d0e54bea5d188593ef6c3b30139f3ab1/src/coreclr/src/gc/gc.h#L111
            const int Threshold = 85000;

            var segmentSize = 2;
            while (ArraySize(elementSize, segmentSize << 1) < Threshold)
            {
                segmentSize <<= 1;
            }

            return segmentSize;

            static int ArraySize(int elementSize, int segmentSize)
            {
                // Array object header, plus space for the elements
                return (2 * IntPtr.Size) + (elementSize * segmentSize);
            }
        }

        /// <summary>
        /// Calculates a shift which can be applied to an absolute index to get the page index within a segmented array.
        /// </summary>
        /// <param name="segmentSize">The number of elements in each page of the segmented array. Must be a power of 2.</param>
        /// <returns>The shift to apply to the absolute index to get the page index within a segmented array.</returns>
        private static int CalculateSegmentShift(int segmentSize)
        {
            var segmentShift = 0;
            while (0 != (segmentSize >>= 1))
            {
                segmentShift++;
            }

            return segmentShift;
        }

        /// <summary>
        /// Calculates a mask, which can be applied to an absolute index to get the index within a page of a segmented
        /// array.
        /// </summary>
        /// <param name="segmentSize">The number of elements in each page of the segmented array. Must be a power of 2.</param>
        /// <returns>The bit mask to obtain the index within a page from an absolute index within a segmented array.</returns>
        private static int CalculateOffsetMask(int segmentSize)
        {
            Debug.Assert(segmentSize == 1 || (segmentSize & (segmentSize - 1)) == 0, "Expected size of 1, or a power of 2");
            return segmentSize - 1;
        }

        // Faster inline implementation for NETCOREAPP to avoid static constructors and non-inlineable
        // generics with runtime lookups
#if NETCOREAPP3_0_OR_NEWER
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int InlineCalculateSegmentSize(int elementSize)
        {
            return 1 << InlineCalculateSegmentShift(elementSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int InlineCalculateSegmentShift(int elementSize)
        {
            // Default Large Object Heap size threshold
            // https://github.com/dotnet/runtime/blob/c9d69e38d0e54bea5d188593ef6c3b30139f3ab1/src/coreclr/src/gc/gc.h#L111
            const uint Threshold = 85000;
            return System.Numerics.BitOperations.Log2((uint)((Threshold / elementSize) - (2 * Unsafe.SizeOf<object>())));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int InlineCalculateOffsetMask(int elementSize)
        {
            return InlineCalculateSegmentSize(elementSize) - 1;
        }
#endif

        internal static class TestAccessor
        {
            public static int CalculateSegmentSize(int elementSize)
                => SegmentedArrayHelper.CalculateSegmentSize(elementSize);

            public static int CalculateSegmentShift(int elementSize)
                => SegmentedArrayHelper.CalculateSegmentShift(elementSize);

            public static int CalculateOffsetMask(int elementSize)
                => SegmentedArrayHelper.CalculateOffsetMask(elementSize);
        }

        private static class ReferenceTypeSegmentHelper
        {
            public static readonly int SegmentSize = CalculateSegmentSize(Unsafe.SizeOf<object>());
            public static readonly int SegmentShift = CalculateSegmentShift(SegmentSize);
            public static readonly int OffsetMask = CalculateOffsetMask(SegmentSize);
        }

        private static class ValueTypeSegmentHelper<T>
        {
            public static readonly int SegmentSize = CalculateSegmentSize(Unsafe.SizeOf<T>());
            public static readonly int SegmentShift = CalculateSegmentShift(SegmentSize);
            public static readonly int OffsetMask = CalculateOffsetMask(SegmentSize);
        }
    }
}
