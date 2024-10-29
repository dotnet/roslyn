// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// NOTE: This code is derived from an implementation originally in dotnet/runtime:
// https://github.com/dotnet/runtime/blob/v8.0.3/src/libraries/System.Collections/tests/Generic/List/List.Generic.Tests.cs
//
// See the commentary in https://github.com/dotnet/roslyn/pull/50156 for notes on incorporating changes made to the
// reference implementation.

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
        #region IList<T> Helper Methods
        protected override bool Enumerator_Empty_UsesSingletonInstance => true;
        protected override bool Enumerator_Empty_Current_UndefinedOperation_Throws => false;
        protected override bool Enumerator_Empty_ModifiedDuringEnumeration_ThrowsInvalidOperationException => false;

        protected override IList<T> GenericIListFactory()
        {
            return GenericListFactory();
        }

        protected override IList<T> GenericIListFactory(int count)
        {
            return GenericListFactory(count);
        }

        #endregion

        #region List<T> Helper Methods

        private protected virtual SegmentedList<T> GenericListFactory()
        {
            return new SegmentedList<T>();
        }

        private protected virtual SegmentedList<T> GenericListFactory(int count)
        {
            IEnumerable<T> toCreateFrom = CreateEnumerable(EnumerableType.List, null, count, 0, 0);
            return new SegmentedList<T>(toCreateFrom);
        }

        private protected void VerifyList(SegmentedList<T> list, SegmentedList<T> expectedItems)
        {
            Assert.Equal(expectedItems.Count, list.Count);

            //Only verify the indexer. List should be in a good enough state that we
            //do not have to verify consistency with any other method.
            for (int i = 0; i < list.Count; ++i)
            {
                Assert.True(list[i] == null ? expectedItems[i] == null : list[i].Equals(expectedItems[i]));
            }
        }

        #endregion

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void CopyTo_ArgumentValidity(int count)
        {
            SegmentedList<T> list = GenericListFactory(count);
            Assert.Throws<ArgumentException>(null, () => list.CopyTo(0, new T[0], 0, count + 1));
            Assert.Throws<ArgumentException>(null, () => list.CopyTo(count, new T[0], 0, 1));
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
        public void EnsureCapacity_GrowsBySegment(int segmentCount)
        {
            var elementCount = SegmentedArray<T>.TestAccessor.SegmentSize * segmentCount;
            var list = new SegmentedList<T>(elementCount);

            for (var i = 0; i < elementCount; i++)
                list.Add(CreateT(i));

            Assert.Equal(elementCount, list.Capacity);

            list.EnsureCapacity(elementCount + 1);
            Assert.Equal(elementCount + SegmentedArray<T>.TestAccessor.SegmentSize, list.Capacity);
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
