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
            return Unsafe.SizeOf<T>() switch
            {
                // Hard code common values since not all versions of the .NET JIT support reducing this computation to a
                // constant value at runtime. Values are validated against the reference implementation in
                // CalculateSegmentSize in unit tests.
                4 => 16384,
                8 => 8192,
                12 => 4096,
                16 => 4096,
                24 => 2048,
                28 => 2048,
                32 => 2048,
                40 => 2048,
#if NETCOREAPP3_0_OR_NEWER
                _ => InlineCalculateSegmentSize(Unsafe.SizeOf<T>()),
#else
                _ => FallbackSegmentHelper<T>.SegmentSize,
#endif
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetSegmentShift<T>()
        {
            return Unsafe.SizeOf<T>() switch
            {
                // Hard code common values since not all versions of the .NET JIT support reducing this computation to a
                // constant value at runtime. Values are validated against the reference implementation in
                // CalculateSegmentSize in unit tests.
                4 => 14,
                8 => 13,
                12 => 12,
                16 => 12,
                24 => 11,
                28 => 11,
                32 => 11,
                40 => 11,
#if NETCOREAPP3_0_OR_NEWER
                _ => InlineCalculateSegmentShift(Unsafe.SizeOf<T>()),
#else
                _ => FallbackSegmentHelper<T>.SegmentShift,
#endif
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetOffsetMask<T>()
        {
            return Unsafe.SizeOf<T>() switch
            {
                // Hard code common values since not all versions of the .NET JIT support reducing this computation to a
                // constant value at runtime. Values are validated against the reference implementation in
                // CalculateSegmentSize in unit tests.
                4 => 16383,
                8 => 8191,
                12 => 4095,
                16 => 4095,
                24 => 2047,
                28 => 2047,
                32 => 2047,
                40 => 2047,
#if NETCOREAPP3_0_OR_NEWER
                _ => InlineCalculateOffsetMask(Unsafe.SizeOf<T>()),
#else
                _ => FallbackSegmentHelper<T>.OffsetMask,
#endif
            };
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
                return (2 * IntPtr.Size + 8) + (elementSize * segmentSize);
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
            return System.Numerics.BitOperations.Log2((uint)((Threshold / elementSize) - (2 * Unsafe.SizeOf<object>() + 8)));
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

            public static int CalculateSegmentShift(int segmentSize)
                => SegmentedArrayHelper.CalculateSegmentShift(segmentSize);

            public static int CalculateOffsetMask(int segmentSize)
                => SegmentedArrayHelper.CalculateOffsetMask(segmentSize);
        }

#if !NETCOREAPP3_0_OR_NEWER
        private static class FallbackSegmentHelper<T>
        {
            public static readonly int SegmentSize = CalculateSegmentSize(Unsafe.SizeOf<T>());
            public static readonly int SegmentShift = CalculateSegmentShift(SegmentSize);
            public static readonly int OffsetMask = CalculateOffsetMask(SegmentSize);
        }
#endif
    }
}
