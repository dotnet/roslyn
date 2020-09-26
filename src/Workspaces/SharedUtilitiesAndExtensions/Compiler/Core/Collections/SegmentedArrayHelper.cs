// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Shared.Collections
{
    internal static class SegmentedArrayHelper
    {
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
        internal static int CalculateSegmentSize(int elementSize)
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
        internal static int CalculateSegmentShift(int segmentSize)
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
        internal static int CalculateOffsetMask(int segmentSize)
        {
            Debug.Assert(segmentSize == 1 || (segmentSize & (segmentSize - 1)) == 0, "Expected size of 1, or a power of 2");
            return segmentSize - 1;
        }
    }
}
