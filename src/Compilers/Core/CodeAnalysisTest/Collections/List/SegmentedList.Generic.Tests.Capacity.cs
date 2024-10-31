// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Collections;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Collections
{
    /// <summary>
    /// Contains tests that ensure the correctness of the List class.
    /// </summary>
    public abstract partial class SegmentedList_Generic_Tests<T> : IList_Generic_Tests<T>
        where T : notnull
    {
        public static IEnumerable<object[]> TestLengthsAndSegmentCounts
        {
            get
            {
                for (var segmentsToAdd = 1; segmentsToAdd < 4; segmentsToAdd++)
                {
                    yield return new object[] { 1, segmentsToAdd };
                    yield return new object[] { 10, segmentsToAdd };
                    yield return new object[] { 100, segmentsToAdd };
                    yield return new object[] { SegmentedArray<object>.TestAccessor.SegmentSize / 2, segmentsToAdd };
                    yield return new object[] { SegmentedArray<object>.TestAccessor.SegmentSize, segmentsToAdd };
                    yield return new object[] { SegmentedArray<object>.TestAccessor.SegmentSize * 2, segmentsToAdd };
                    yield return new object[] { 100000, segmentsToAdd };
                }
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void Capacity_ArgumentValidity(int count)
        {
            var list = new SegmentedList<T>(count);
            for (var i = 0; i < count; i++)
                list.Add(CreateT(i));

            Assert.Throws<ArgumentOutOfRangeException>(() => list.Capacity = count - 1);
        }

        [Theory]
        [InlineData(0, 0, 1)]
        [InlineData(0, 0, 10)]
        [InlineData(4, 4, 6)]
        [InlineData(4, 4, 10)]
        [InlineData(4, 4, 100_000)]
        public void Capacity_MatchesSizeRequested(int initialCapacity, int initialSize, int requestedCapacity)
        {
            var list = new SegmentedList<T>(initialCapacity);

            for (var i = 0; i < initialSize; i++)
                list.Add(CreateT(i));

            list.Capacity = requestedCapacity;

            Assert.Equal(requestedCapacity, list.Capacity);
        }

        [Theory]
        [MemberData(nameof(TestLengthsAndSegmentCounts))]
        public void Capacity_ReusesSegments(
            int length,
            int addSegmentCount)
        {
            var elementCountToAdd = SegmentedArray<object>.TestAccessor.SegmentSize * addSegmentCount;
            var o = new object();

            var segmented = new SegmentedList<object>(length);
            for (var i = 0; i < length; i++)
                segmented.Add(o);

            var oldSegments = SegmentedCollectionsMarshal.AsSegments(segmented.GetTestAccessor().Items);
            var oldSegmentCount = oldSegments.Length;

            segmented.Capacity = length + elementCountToAdd;

            var resizedSegments = SegmentedCollectionsMarshal.AsSegments(segmented.GetTestAccessor().Items);
            var resizedSegmentCount = resizedSegments.Length;

            Assert.Equal(oldSegmentCount + addSegmentCount, resizedSegmentCount);

            for (var i = 0; i < oldSegmentCount - 1; i++)
                Assert.Same(resizedSegments[i], oldSegments[i]);

            for (var i = oldSegmentCount - 1; i < resizedSegmentCount - 1; i++)
                Assert.Equal(resizedSegments[i].Length, SegmentedArray<object>.TestAccessor.SegmentSize);

            Assert.NotSame(resizedSegments[resizedSegmentCount - 1], oldSegments[oldSegmentCount - 1]);
            Assert.Equal(resizedSegments[resizedSegmentCount - 1].Length, oldSegments[oldSegmentCount - 1].Length);
        }

        [Theory]
        [CombinatorialData]
        public void Capacity_InOnlySingleSegment(
            [CombinatorialValues(1, 2, 10, 100)] int length,
            [CombinatorialValues(1, 2, 10, 100)] int addItemCount)
        {
            var o = new object();

            var segmented = new SegmentedList<object>(length);
            for (var i = 0; i < length; i++)
                segmented.Add(o);

            var oldSegments = SegmentedCollectionsMarshal.AsSegments(segmented.GetTestAccessor().Items);

            segmented.Capacity = length + addItemCount;

            var resizedSegments = SegmentedCollectionsMarshal.AsSegments(segmented.GetTestAccessor().Items);

            Assert.Equal(1, oldSegments.Length);
            Assert.Equal(1, resizedSegments.Length);
            Assert.Same(resizedSegments[0], oldSegments[0]);
            Assert.Equal(segmented.Capacity, resizedSegments[0].Length);
        }

        [Theory]
        [InlineData(0, 0, 1, 4)]
        [InlineData(0, 0, 10, 10)]
        [InlineData(4, 4, 6, 8)]
        [InlineData(4, 4, 10, 10)]
        public void EnsureCapacity_ResizesAppropriately(int initialCapacity, int initialSize, int requestedCapacity, int expectedCapacity)
        {
            var list = new SegmentedList<T>(initialCapacity);

            for (var i = 0; i < initialSize; i++)
                list.Add(CreateT(i));

            list.EnsureCapacity(requestedCapacity);

            Assert.Equal(expectedCapacity, list.Capacity);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(4)]
        public void EnsureCapacity_MatchesSizeWithLargeCapacityRequest(int segmentCount)
        {
            var elementCount = SegmentedArray<T>.TestAccessor.SegmentSize * segmentCount;
            var list = new SegmentedList<T>(elementCount);

            for (var i = 0; i < elementCount; i++)
                list.Add(CreateT(i));

            Assert.Equal(elementCount, list.Capacity);

            var requestedCapacity = 2 * elementCount + 10;
            list.EnsureCapacity(requestedCapacity);
            Assert.Equal(requestedCapacity, list.Capacity);
        }
    }
}
