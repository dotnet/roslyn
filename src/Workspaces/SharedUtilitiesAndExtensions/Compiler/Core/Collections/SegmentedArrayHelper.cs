// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        internal static int CalculateSegmentShift(int segmentSize)
        {
            var segmentShift = 0;
            while (0 != (segmentSize >>= 1))
            {
                segmentShift++;
            }

            return segmentShift;
        }

        internal static int CalculateOffsetMask(int segmentSize)
        {
            Debug.Assert(segmentSize <= 1 || (segmentSize & (segmentSize - 1)) != 0);
            return segmentSize - 1;
        }
    }
}
