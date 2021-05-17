// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// NOTE: This code is derived from an implementation originally in dotnet/runtime:
// https://github.com/dotnet/runtime/blob/v5.0.2/src/libraries/System.Collections/tests/Generic/List/List.Generic.Tests.AddRange.cs
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
        // Has tests that pass a variably sized TestCollection and MyEnumerable to the AddRange function

        [Theory]
        [MemberData(nameof(EnumerableTestData))]
        public void AddRange(EnumerableType enumerableType, int listLength, int enumerableLength, int numberOfMatchingElements, int numberOfDuplicateElements)
        {
            SegmentedList<T> list = GenericListFactory(listLength);
            SegmentedList<T> listBeforeAdd = list.ToSegmentedList();
            IEnumerable<T> enumerable = CreateEnumerable(enumerableType, list, enumerableLength, numberOfMatchingElements, numberOfDuplicateElements);
            list.AddRange(enumerable);

            // Check that the first section of the List is unchanged
            Assert.All(Enumerable.Range(0, listLength), index =>
            {
                Assert.Equal(listBeforeAdd[index], list[index]);
            });

            // Check that the added elements are correct
            Assert.All(Enumerable.Range(0, enumerableLength), index =>
            {
                Assert.Equal(enumerable.ElementAt(index), list[index + listLength]);
            });
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void AddRange_NullEnumerable_ThrowsArgumentNullException(int count)
        {
            SegmentedList<T> list = GenericListFactory(count);
            SegmentedList<T> listBeforeAdd = list.ToSegmentedList();
            Assert.Throws<ArgumentNullException>(() => list.AddRange(null!));
            Assert.Equal(listBeforeAdd, list);
        }

        [Fact]
        public void AddRange_AddSelfAsEnumerable_ThrowsExceptionWhenNotEmpty()
        {
            SegmentedList<T?> list = GenericListFactory(0)!;

            // Succeeds when list is empty.
            list.AddRange(list);
            list.AddRange(list.Where(_ => true));

            // Succeeds when list has elements and is added as collection.
            list.Add(default);
            Assert.Equal(1, list.Count);
            list.AddRange(list);
            Assert.Equal(2, list.Count);
            list.AddRange(list);
            Assert.Equal(4, list.Count);

            // Fails version check when list has elements and is added as non-collection.
            Assert.Throws<InvalidOperationException>(() => list.AddRange(list.Where(_ => true)));
            Assert.Equal(5, list.Count);
            Assert.Throws<InvalidOperationException>(() => list.AddRange(list.Where(_ => true)));
            Assert.Equal(6, list.Count);
        }
    }
}
