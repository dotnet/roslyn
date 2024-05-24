// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Collections.Internal;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Collections
{
    public class SegmentedArrayHelperTests
    {
        [StructLayout(LayoutKind.Sequential, Size = 2)]
        private struct Size2 { }

        [StructLayout(LayoutKind.Sequential, Size = 4)]
        private struct Size4 { }

        [StructLayout(LayoutKind.Sequential, Size = 8)]
        private struct Size8 { }

        [StructLayout(LayoutKind.Sequential, Size = 12)]
        private struct Size12 { }

        [StructLayout(LayoutKind.Sequential, Size = 16)]
        private struct Size16 { }

        [StructLayout(LayoutKind.Sequential, Size = 24)]
        private struct Size24 { }

        [StructLayout(LayoutKind.Sequential, Size = 28)]
        private struct Size28 { }

        [StructLayout(LayoutKind.Sequential, Size = 32)]
        private struct Size32 { }

        [StructLayout(LayoutKind.Sequential, Size = 40)]
        private struct Size40 { }

        public static IEnumerable<object[]> ExplicitSizeTypes
        {
            get
            {
                yield return new object[] { typeof(Size2) };
                yield return new object[] { typeof(Size4) };
                yield return new object[] { typeof(Size8) };
                yield return new object[] { typeof(Size12) };
                yield return new object[] { typeof(Size16) };
                yield return new object[] { typeof(Size24) };
                yield return new object[] { typeof(Size28) };
                yield return new object[] { typeof(Size32) };
                yield return new object[] { typeof(Size40) };
            }
        }

        [Theory]
        [MemberData(nameof(ExplicitSizeTypes))]
        public void ExplicitSizesAreCorrect(Type type)
        {
            Assert.Equal(int.Parse(type.Name[4..]), InvokeUnsafeSizeOf(type));
        }

        [Theory]
        [MemberData(nameof(ExplicitSizeTypes))]
        public void GetSegmentSize(Type type)
        {
            var getSegmentSizeMethod = typeof(SegmentedArrayHelper).GetMethod(nameof(SegmentedArrayHelper.GetSegmentSize), BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod(type);
            Assert.Equal(SegmentedArrayHelper.TestAccessor.CalculateSegmentSize(InvokeUnsafeSizeOf(type)), (int)getSegmentSizeMethod.Invoke(null, null));
        }

        [Theory]
        [MemberData(nameof(ExplicitSizeTypes))]
        public void GetSegmentShift(Type type)
        {
            var getSegmentShiftMethod = typeof(SegmentedArrayHelper).GetMethod(nameof(SegmentedArrayHelper.GetSegmentShift), BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod(type);
            var segmentSize = SegmentedArrayHelper.TestAccessor.CalculateSegmentSize(InvokeUnsafeSizeOf(type));
            Assert.Equal(SegmentedArrayHelper.TestAccessor.CalculateSegmentShift(segmentSize), (int)getSegmentShiftMethod.Invoke(null, null));
        }

        [Theory]
        [MemberData(nameof(ExplicitSizeTypes))]
        public void GetOffsetMask(Type type)
        {
            var getOffsetMaskMethod = typeof(SegmentedArrayHelper).GetMethod(nameof(SegmentedArrayHelper.GetOffsetMask), BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod(type);
            var segmentSize = SegmentedArrayHelper.TestAccessor.CalculateSegmentSize(InvokeUnsafeSizeOf(type));
            Assert.Equal(SegmentedArrayHelper.TestAccessor.CalculateOffsetMask(segmentSize), (int)getOffsetMaskMethod.Invoke(null, null));
        }

        private static int InvokeUnsafeSizeOf(Type type)
        {
            var unsafeSizeOfMethod = typeof(Unsafe).GetMethod(nameof(Unsafe.SizeOf)).MakeGenericMethod(type);
            return (int)unsafeSizeOfMethod.Invoke(null, null);
        }

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
                _ => throw ExceptionUtilities.Unreachable(),
            };

            Assert.Equal(expected, SegmentedArrayHelper.TestAccessor.CalculateSegmentSize(elementSize));
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
                _ => throw ExceptionUtilities.Unreachable(),
            };

            Assert.Equal(expected, SegmentedArrayHelper.TestAccessor.CalculateSegmentShift(segmentSize));
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
                _ => throw ExceptionUtilities.Unreachable(),
            };

            Assert.Equal(expected, SegmentedArrayHelper.TestAccessor.CalculateOffsetMask(segmentSize));
        }
    }
}
