// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Shared.Collections;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Collections
{
    public class SegmentedArrayHelperTests
    {
        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        [InlineData(7)]
        [InlineData(8)]
        [InlineData(9)]
        [InlineData(10)]
        [InlineData(16)]
        [InlineData(32)]
        public void CalculateSegmentSize(int elementSize)
        {
            var expected = elementSize switch
            {
                1 => 65536,
                2 => 32768,
                3 => 16384,
                4 => 16384,
                5 => 16384,
                6 => 8192,
                7 => 8192,
                8 => 8192,
                9 => 8192,
                10 => 8192,
                16 => 4096,
                32 => 2048,
                _ => throw ExceptionUtilities.Unreachable,
            };

            Assert.Equal(expected, SegmentedArrayHelper.CalculateSegmentSize(elementSize));
        }

        [Theory]
        [InlineData(1024)]
        [InlineData(2048)]
        [InlineData(4096)]
        [InlineData(8192)]
        [InlineData(16384)]
        [InlineData(32768)]
        [InlineData(65536)]
        public void CalculateSegmentShift(int segmentSize)
        {
            var expected = segmentSize switch
            {
                1024 => 10,
                2048 => 11,
                4096 => 12,
                8192 => 13,
                16384 => 14,
                32768 => 15,
                65536 => 16,
                _ => throw ExceptionUtilities.Unreachable,
            };

            Assert.Equal(expected, SegmentedArrayHelper.CalculateSegmentShift(segmentSize));
        }

        [Theory]
        [InlineData(1024)]
        [InlineData(2048)]
        [InlineData(4096)]
        [InlineData(8192)]
        [InlineData(16384)]
        [InlineData(32768)]
        [InlineData(65536)]
        public void CalculateOffsetMask(int segmentSize)
        {
            var expected = segmentSize switch
            {
                1024 => 0x3FF,
                2048 => 0x7FF,
                4096 => 0xFFF,
                8192 => 0x1FFF,
                16384 => 0x3FFF,
                32768 => 0x7FFF,
                65536 => 0xFFFF,
                _ => throw ExceptionUtilities.Unreachable,
            };

            Assert.Equal(expected, SegmentedArrayHelper.CalculateOffsetMask(segmentSize));
        }
    }
}
