// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// NOTE: This code is derived from an implementation originally in dotnet/runtime:
// https://github.com/dotnet/runtime/blob/v5.0.2/src/libraries/System.Collections/tests/Generic/List/List.Generic.Tests.Constructor.cs
//
// See the commentary in https://github.com/dotnet/roslyn/pull/50156 for notes on incorporating changes made to the
// reference implementation.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Collections;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Collections
{
    /// <summary>
    /// Contains tests that ensure the correctness of the List class.
    /// </summary>
    public abstract partial class SegmentedList_Generic_Tests<T> : IList_Generic_Tests<T>
    {
        [Fact]
        public void Constructor_Default()
        {
            SegmentedList<T> list = new SegmentedList<T>();
            Assert.Equal(0, list.Capacity); //"Expected capacity of list to be the same as given."
            Assert.Equal(0, list.Count); //"Do not expect anything to be in the list."
            Assert.False(((IList<T>)list).IsReadOnly); //"List should not be readonly"
        }

        [Theory]
        [InlineData(0)]
        [InlineData(10)]
        [InlineData(15)]
        [InlineData(16)]
        [InlineData(17)]
        [InlineData(100)]
        public void Constructor_Capacity(int capacity)
        {
            SegmentedList<T> list = new SegmentedList<T>(capacity);
            Assert.Equal(capacity, list.Capacity); //"Expected capacity of list to be the same as given."
            Assert.Equal(0, list.Count); //"Do not expect anything to be in the list."
            Assert.False(((IList<T>)list).IsReadOnly); //"List should not be readonly"
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(int.MinValue)]
        public void Constructor_NegativeCapacity_ThrowsArgumentOutOfRangeException(int capacity)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new SegmentedList<T>(capacity));
        }

        [Theory]
        [MemberData(nameof(EnumerableTestData))]
        public void Constructor_IEnumerable(EnumerableType enumerableType, int listLength, int enumerableLength, int numberOfMatchingElements, int numberOfDuplicateElements)
        {
            _ = listLength;
            _ = numberOfMatchingElements;
            IEnumerable<T> enumerable = CreateEnumerable(enumerableType, null, enumerableLength, 0, numberOfDuplicateElements);
            SegmentedList<T> list = new SegmentedList<T>(enumerable);
            SegmentedList<T> expected = enumerable.ToSegmentedList();

            Assert.Equal(enumerableLength, list.Count); //"Number of items in list do not match the number of items given."

            for (int i = 0; i < enumerableLength; i++)
                Assert.Equal(expected[i], list[i]); //"Expected object in item array to be the same as in the list"

            Assert.False(((IList<T>)list).IsReadOnly); //"List should not be readonly"
        }

        [Fact]
        public void Constructo_NullIEnumerable_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => { SegmentedList<T> _list = new SegmentedList<T>(null!); }); //"Expected ArgumentnUllException for null items"
        }
    }
}
